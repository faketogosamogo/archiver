using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FileArchiver
{
    public static class FileCompressor
    {
        public static string CompressFile(string filePath)
        {
            using var srcFile = File.OpenRead(filePath);
            using var compressedFile = File.Create(filePath + ".gz");

            while (true)
            {
                byte[] block =  ReadBlock(srcFile, 1024 * 1024);
                if (block.Length == 0) break;
                block = CompressBlock(block);

                WriteBlock(compressedFile, block);
            }

            return "";
        }

        private static byte[] ReadBlock(FileStream fileToRead, int blockLen)
        {
            byte[] block = new byte[blockLen];
            int countOfReadBytes = fileToRead.Read(block);

            Array.Resize(ref block, countOfReadBytes);
            return block;
        }

        private static byte[] CompressBlock(byte[] block)
        {
            using var compressBlockStream = new MemoryStream();
            using (var compressionStream = new GZipStream(compressBlockStream, CompressionMode.Compress))
            {
                compressionStream.Write(block);

            }

            return compressBlockStream.ToArray();
        }
        private static void WriteBlock(FileStream fileToWrite, byte[] block)
        {
            fileToWrite.Write(BitConverter.GetBytes(block.Length));
            fileToWrite.Write(block);
        }
        
    }
}
