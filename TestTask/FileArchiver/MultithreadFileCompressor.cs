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
        int _blockLen = 64;

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
                for (int i = 0; i < _threadsCount; i++)
                {
                    if (!fileIsReaded)
                    {
                        Console.WriteLine(lenIter * _blockLen);

                      
                        lenIter++;
                    }
                }
          
                _readingThreads = new List<Thread>();
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
            //Console.WriteLine(start);
            var fileToRead = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] block = new byte[blockLen];
            fileToRead.Position = start;

            int countOfReadedBytes = fileToRead.Read(block);

            Array.Resize(ref block, countOfReadedBytes);
            return block;
        }

        /// <summary>
        /// Сжимает блок
        /// </summary>
        /// <param name="block">Блок для сжатия</param>
        /// <returns>Сжатый блок</returns>
        private byte[] сompressBlock(byte[] block)
        {
            using var compressBlockStream = new MemoryStream();
            using (var compressionStream = new GZipStream(compressBlockStream, CompressionMode.Compress))
            {
                compressionStream.Write(block);
            }
            return compressBlockStream.ToArray();
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
            }
        }
    }
}
