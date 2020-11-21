using System;
using System.IO;
using System.IO.Compression;
using FileArchiver.BlockServices;
using NUnit.Framework;
namespace FileArchiverTest
{
    [TestFixture]
    public class BlockGziperTests
    {
        BlockGziper blockGziper;

        [SetUp]
        public void Init()
        {
            blockGziper = new BlockGziper();
        }

        [Test]
        public void CompressBlock_should_return_equal_block()
        {
            byte[] block = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            block = blockGziper.CompressBlock(block);

            byte[] blockForExpected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            using var expected = new MemoryStream();
            using (var compressionStream = new GZipStream(expected, CompressionMode.Compress))
            {
                compressionStream.Write(blockForExpected);
            }

            Assert.AreEqual(expected.ToArray(), block);
        }

        [Test]
        public void CompressBlock_should_throw_NullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => blockGziper.CompressBlock(null));
        }

        [Test]
        public void DecompressBlock_should_return_equal_block()
        {
            byte[] block = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            byte[] expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };


            using var compressedBlock = new MemoryStream();
            using (var compressionStream = new GZipStream(compressedBlock, CompressionMode.Compress))
            {
                compressionStream.Write(block);
            }

            using var decompressBlockStream = new MemoryStream();
            using var decompressedBlock = new MemoryStream(compressedBlock.ToArray());
            using (var decompressionStream = new GZipStream(decompressedBlock, CompressionMode.Decompress))
            {
                decompressionStream.CopyTo(decompressBlockStream);
            }

            Assert.AreEqual(expected, decompressBlockStream.ToArray());
        }

        [Test]
        public void DecompressBlock_should_throw_ArgumentNullException()
        {
            Assert.Throws<NullReferenceException>(()=> blockGziper.DecompressBlock(null));
        }

        [Test]
        public void DecompressBlock_after_CompressBlock_should_return_equal_block()
        {
            byte[] block = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            byte[] expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            block = blockGziper.CompressBlock(block);
            block = blockGziper.DecompressBlock(block);

            Assert.AreEqual(expected, block);
        }

    }
}
