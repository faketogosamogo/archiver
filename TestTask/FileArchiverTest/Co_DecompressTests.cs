using System;
using System.IO;
using FileArchiver;
using FileArchiver.BlockServices;
using NUnit.Framework;
namespace FileArchiverTest
{
    [TestFixture]
    class Co_DecompressTests
    {
        MultithreadFileCompressor fileCompressor = new MultithreadFileCompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());
        MultithreadFileDecompressor fileDecompressor = new MultithreadFileDecompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());

        [Test]
        public void Decompressed_file_should_be_equal_source_file()
        {
           
                int len = (1024 * 1024);
                byte[] expectedData = new byte[len];
                new Random().NextBytes(expectedData);

                using (var sourceFile = File.OpenWrite("forTest.test"))
                {
                    sourceFile.Write(expectedData);
                }

                fileCompressor.CompressFile("forTest.test", "forTest.gzip");
                fileDecompressor.DecompressFile("forTest.gzip", "forTestAfter.gzip");
                byte[] buf = new byte[len];

                using (var decompressedFile = File.OpenRead("forTestAfter.gzip"))
                {
                    decompressedFile.Read(buf);
                }


                Assert.AreEqual(expectedData, buf);

                File.Delete("forTest.test");
                File.Delete("forTest.gzip");
                File.Delete("forTestAfter.gzip");
           
        }
    }
}
