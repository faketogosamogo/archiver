using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace FileArchiver
{
    public class FileDecompressor
    {
        static object writeLocker = new object();

        private static void decompressAndWriteBlocks(FileStream fileToWrite, List<byte[]> blocks)
        {
            List<Thread> threads = new List<Thread>();
            
            foreach(var block in blocks)
            {
                Thread thread = new Thread(() =>
                {
                    var tempBlock = decompressBlock(block);
                    writeBlock(fileToWrite, tempBlock);
                });
                thread.Start();
                thread.Join();
            }
           // foreach (var thread in threads) thread.Start();
            //foreach (var thread in threads) thread.Join();
        }
        private static void prepareThread(FileStream fileToWrite, List<byte[]> blocks)
        {
            Thread thread = new Thread(() =>
            {
                decompressAndWriteBlocks(fileToWrite, blocks);
            });
            thread.Start();
            thread.Join();
        }
        public static string DecompressFile(string filePath)
        { 
            using var srcFile = File.OpenRead(filePath);
            using var decompressedFile = File.OpenWrite(filePath + "1");


            List<byte[]> blocks = new List<byte[]>();
            while (true)
            {
                int blockLen = getBlockLen(srcFile);               
                byte[] block = readBlock(srcFile, blockLen);
                if (block.Length == 0)
                {
                    if (blocks.Count > 0)
                    {
                        prepareThread(decompressedFile, blocks);
                        blocks = new List<byte[]>();
                    }
                    break;
                }

                blocks.Add(block);

                if (blocks.Count == 5)
                {
                    prepareThread(decompressedFile, blocks);
                    blocks = new List<byte[]>();
                }
            }
            return decompressedFile.Name;
        }
        private static int getBlockLen(FileStream fileToRead)
        {
            byte[] sizeBuffer = new byte[4];
            fileToRead.Read(sizeBuffer);
            int blockLen = BitConverter.ToInt32(sizeBuffer);
            return blockLen;
        }
        private static byte[] readBlock(FileStream fileToRead, int blockLen)
        {
            byte[] block = new byte[blockLen];
            int countOfReadBytes = fileToRead.Read(block);
            Array.Resize(ref block, countOfReadBytes);
            return block;
        }
       
        private static byte[] decompressBlock(byte[] block)
        {
            using var srcBlockStream = new MemoryStream(block);
            using var decompressBlockStream = new MemoryStream();

            using (var decompressionStream = new GZipStream(srcBlockStream, CompressionMode.Decompress))
            {
                decompressionStream.CopyTo(decompressBlockStream);
            }


            return decompressBlockStream.ToArray();
        }
        private static void writeBlock(FileStream fileToWrite, byte[] block)
        {
            lock (writeLocker)
            {
                fileToWrite.Write(block);
            }
        }

    }
}
