using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileArchiver;
using NUnit.Framework;
namespace FileArchiverTest
{
    [TestFixture]
    class BlockStreamReaderTests
    {
        BlockStreamReader blockReader;

        [SetUp]
        public void Init()
        {
            blockReader = new BlockStreamReader();
        }

        [Test]
        public void ReadBlock_should_return_equal_block()
        {
            using MemoryStream stream = new MemoryStream();
            byte[] buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            stream.Write(buffer);

            byte[] equalFirst = new byte[4] { 0, 1, 2, 3};
            byte[] equalSecond = new byte[4] { 4, 5, 6, 7 };
            byte[] equalThird = new byte[0];
            Assert.AreEqual(equalFirst, blockReader.ReadBlock(stream, 0, 4));
            Assert.AreEqual(equalSecond, blockReader.ReadBlock(stream, stream.Position, 4));
            Assert.AreEqual(equalThird, blockReader.ReadBlock(stream, stream.Position, 4));
        }

        [Test]
        public void ReadBlock_should_throw_NullReferenceException()
        {
            Assert.Throws<NullReferenceException>(()=>blockReader.ReadBlock(null, 1, 3));
        }
       
        [Test]
        public void ReadBlock_should_throw_OverflowException()
        {
            using MemoryStream stream = new MemoryStream();
            byte[] buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            stream.Write(buffer);

            Assert.Throws<OverflowException>(() => blockReader.ReadBlock(stream, 50, -11));            
        }
        [Test]
        public void ReadBlock_should_throw_ArgumentOutOfRangeException()
        {
            using MemoryStream stream = new MemoryStream();            
            byte[] buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };
            stream.Write(buffer);

            Assert.Throws<ArgumentOutOfRangeException>(() => blockReader.ReadBlock(stream, -11, 40));
        }
        [Test]
        public void ReadBlock_should_throw_NotSupportedException()
        {
            using var file = File.OpenWrite(@"123"); //STREAM OPEN TO WRITE
            Assert.Throws<NotSupportedException>(()=> blockReader.ReadBlock(file, 1, 10));
            file.Close();
            File.Delete(@"123");
        }

    }
}
