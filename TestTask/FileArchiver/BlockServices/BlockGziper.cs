using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FileArchiver.BlockServices
{
    /// <summary>
    /// Реализация IBlockCompressor и IBlockDecompressor с использованием GipStream
    /// </summary>
    public class BlockGziper : IBlockCompressor, IBlockDecompressor
    {
        public byte[] CompressBlock(byte[] block)
        {
            if (block.Length == 0) return new byte[0];
            using var compressBlockStream = new MemoryStream();
            using (var compressionStream = new GZipStream(compressBlockStream, CompressionMode.Compress))
            {
                compressionStream.Write(block);
            }
            var compressedBlock = compressBlockStream.ToArray();

            return compressedBlock;
        }

        public byte[] DecompressBlock(byte[] block)
        {
            if (block.Length == 0) return new byte[0];

            using var srcBlockStream = new MemoryStream(block);
            using var decompressBlockStream = new MemoryStream();

            using (var decompressionStream = new GZipStream(srcBlockStream, CompressionMode.Decompress))
            {
                decompressionStream.CopyTo(decompressBlockStream);
            }
            var decompressedBlock = decompressBlockStream.ToArray();
            return decompressedBlock;
        }
    }
}
