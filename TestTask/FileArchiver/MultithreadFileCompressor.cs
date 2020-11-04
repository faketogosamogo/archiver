using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileCompressor
    /// </summary>
    public class MultithreadFileCompressor : IFileCompressor
    {
        //TODO: Сделать так чтобы потоки не пересоздавались

        int _threadsCount;
        int _blockLen = (1024*1024);

        List<Thread> _readingThreads;
        static object _writeLocker = new object();
        string _filePath;

        public MultithreadFileCompressor()
        {
            _readingThreads = new List<Thread>();
            _threadsCount = 5;
        }
        public string CompressFile(string filePath)
        {
            _filePath = filePath;
            using var srcFile = File.OpenRead(filePath);
            using var compressedFile = File.Create(filePath + ".gz");

            List<byte[]> blocks = new List<byte[]>();
            int lenIter = 0;
            bool fileIsReaded = false;
            while (true)
            {
                var block = readBlock(lenIter * _blockLen, _blockLen);
                if (block.Length == 0)
                {
                    if (blocks.Count > 0)
                    {
                        a(blocks, compressedFile);
                        break;
                    }
                }
                blocks.Add(block);
                if (blocks.Count == 5)
                {
                    a(blocks, compressedFile);
                    blocks = new List<byte[]>();
                    //_readingThreads = new List<Thread>();

                }
                lenIter++;
            }
            return "";
        }
        private void a(List<byte[]> blocks, FileStream toWrite)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                blocks[i] = compressBlock(blocks[i]);
                writeBlock(toWrite, blocks[i]);
            }
        }

        /// <summary>
        /// Считывает блок из файла
        /// </summary>
        /// <param name="start">Позиция начала чтения</param>
        /// <param name="blockLen">Длина считываемого блока</param>
        /// <returns>Считанный блок(может быть меньше ожидаемого, если длина считываемого отрезка меньше чем blockLen)</returns>
        private byte[] readBlock(int start, int blockLen)
        {
            var fileToRead = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] block = new byte[blockLen];
            fileToRead.Position = start;

            int countOfReadedBytes = fileToRead.Read(block);

            Console.WriteLine($"read block, start index: {start}, lenght of block: {blockLen}, count of readed bytes: {countOfReadedBytes}");

            Array.Resize(ref block, countOfReadedBytes);
            return block;
        }

        /// <summary>
        /// Сжимает блок
        /// </summary>
        /// <param name="block">Блок для сжатия</param>
        /// <returns>Сжатый блок</returns>
        private byte[] compressBlock(byte[] block)
        {
            using var compressBlockStream = new MemoryStream();
            using (var compressionStream = new GZipStream(compressBlockStream, CompressionMode.Compress))
            {
                compressionStream.Write(block);
            }
            var compressedBlock = compressBlockStream.ToArray();

            Console.WriteLine($"compress block: block lenght: {block.Length}, after compress: {block.Length}");
            return compressedBlock;
        }

        /// <summary>
        /// Записывает блок
        /// Для записи всех блоков используется один поток, в связи с незнанием конечного размера сжимаемого блока
        /// (Резервировать место, а после убирать пробелы посчитал нецелесообразным)
        /// </summary>
        /// <param name="fileToWrite">Файл для записи</param>
        /// <param name="block">Блок для записи</param>
        private void writeBlock(FileStream fileToWrite, byte[] block)
        {
            lock (_writeLocker)
            {
                fileToWrite.Write(BitConverter.GetBytes(block.Length));//Записываем длину блока перед блоком, для возможности считывать блок в дальнейшем
                fileToWrite.Write(block);
                Console.WriteLine($"write block: block lenght: {block.Length}");

            }
        }
    }
}
