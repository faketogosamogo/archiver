using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace FileArchiver
{
    //TODO: что я понял и что мне нужно реализовать
    //Запись в файл разбивать смысла нет, т.к это в один конечный файл(это в любом случае lock и будет только имитация многопоточности, и выигрыша нет, 
        //если только записывать в определённые места, но мы не знаем конечный размер сжатого блока, можно резервировать, а после убирать пробелы, но это геморойно и долго
    //Все упирается в запись, её не ускорить
    //Чтение + сжате по логике дольше, чем запись
    //Поэтому следует перед записью, эти 2 операции распаралелить

    
    public static class FileCompressor
    {
        static object writeLocker = new object();

       
        private static void compressAndWriteBlocks(FileStream fileToWrite, List<byte[]> blocks)
        {
            List<Thread> threads = new List<Thread>();

            foreach (var block in blocks)
            {
               Thread thread = new Thread(() =>
                {
                    var tempBlock = compressBlock(block);
                    writeBlock(fileToWrite, tempBlock);
                });
                thread.Start();
                thread.Join();
            }       
        }
        private static void prepareThread(FileStream fileToWrite, List<byte[]> blocks)
        {
            Thread thread = new Thread(() =>
            {
                compressAndWriteBlocks(fileToWrite, blocks);
            });
            thread.Start();
            thread.Join();
        }
        public static string CompressFile(string filePath)
        {
            using var srcFile = File.OpenRead(filePath);
            using var compressedFile = File.Create(filePath + ".gz");

            List<byte[]> blocks = new List<byte[]>();
            while (true)
            {
                byte[] block = readBlock(srcFile, 1024 * 1024);
                if (block.Length == 0)
                {
                    if (blocks.Count > 0)
                    {
                        prepareThread(compressedFile, blocks);
                        blocks = new List<byte[]>();
                    }
                    break;
                }

                blocks.Add(block);

                if (blocks.Count == 5)
                {
                    prepareThread(compressedFile, blocks);
                    blocks = new List<byte[]>();
                }
            }

            return "";
        }

        private static byte[] readBlock(FileStream fileToRead, int blockLen)
        {
            byte[] block = new byte[blockLen];
            int countOfReadBytes = fileToRead.Read(block);
            
            Array.Resize(ref block, countOfReadBytes);
            return block;
        }

        private static byte[] compressBlock(byte[] block)
        {
            using var compressBlockStream = new MemoryStream();
            using (var compressionStream = new GZipStream(compressBlockStream, CompressionMode.Compress))
            {
                compressionStream.Write(block);

            }

            return compressBlockStream.ToArray();
        }
        private static void writeBlock(FileStream fileToWrite, byte[] block)
        {
            lock (writeLocker)
            {
                fileToWrite.Write(BitConverter.GetBytes(block.Length));
                fileToWrite.Write(block);
            }
        }
        
    }
}
