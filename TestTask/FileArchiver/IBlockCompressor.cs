using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FileArchiver { 

    public interface IBlockCompressor
    {
        /// <summary>
        /// Сжимает блок
        /// </summary>
        /// <param name="block">Блок для сжатия</param>
        /// <returns>Сжатый блок</returns>
        byte[] CompressBlock(byte[] block);
    }

    public class BlockGzipCompressor : IBlockCompressor
    {
        public byte[] CompressBlock(byte[] block)
        {
            using var compressBlockStream = new MemoryStream();
            using (var compressionStream = new GZipStream(compressBlockStream, CompressionMode.Compress))
            {
                compressionStream.Write(block);
            }
            var compressedBlock = compressBlockStream.ToArray();

            Console.WriteLine($"compress block: block lenght: {block.Length}, after compress: {compressedBlock.Length}");
            return compressedBlock;
        }
    }
}
