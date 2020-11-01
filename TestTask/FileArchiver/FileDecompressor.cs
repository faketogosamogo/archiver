using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FileArchiver
{
    class FileDecompressor
    {
       public static string DecompressFile(string filePath)
       { 
            using var srcFile = File.OpenRead(filePath);
            using var decompressedFile = File.OpenWrite(filePath + "1");

            while (true)
            {
                int blockLen = GetBlockLen(srcFile);
                if (blockLen == 0) break;

                byte[] block = ReadBlock(srcFile, blockLen);
                if (block.Length == 0) break;
                block = DecompressBlock(block);
                WriteBlock(decompressedFile,block);
            }
            return decompressedFile.Name;
        }
        private static int GetBlockLen(FileStream fileToRead)
        {
            byte[] sizeBuffer = new byte[4];
            fileToRead.Read(sizeBuffer);
            int blockLen = BitConverter.ToInt32(sizeBuffer);
            return blockLen;
        }
        private static byte[] ReadBlock(FileStream fileToRead, int blockLen)
        {
            byte[] block = new byte[blockLen];
            int countOfReadBytes = fileToRead.Read(block);
            Array.Resize(ref block, countOfReadBytes);
            return block;
        }
       
        private static byte[] DecompressBlock(byte[] block)
        {
            using var srcBlockStream = new MemoryStream(block);
            using var decompressBlockStream = new MemoryStream();

            using (var decompressionStream = new GZipStream(srcBlockStream, CompressionMode.Decompress))
            {
                decompressionStream.CopyTo(decompressBlockStream);
            }


            return decompressBlockStream.ToArray();
        }
        private static void WriteBlock(FileStream fileToWrite, byte[] block)
        {
            fileToWrite.Write(block);
        }

    }
}
