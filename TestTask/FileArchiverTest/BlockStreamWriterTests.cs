using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FileArchiver;
using NUnit.Framework;
namespace FileArchiverTest
{
    [TestFixture]
    class BlockStreamWriterTests
    {
        BlockStreamWriter blockWriter;

        [SetUp]
        public void Init()
        {
            blockWriter = new BlockStreamWriter();
        }

        [Test]
        public void WriteBlock_should_return_equal_block()
        {
            using MemoryStream stream = new MemoryStream();

            byte[] equalFirst = new byte[4] { 0, 1, 2, 3 };
            byte[] equalSecond = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };


            byte[] first = new byte[4] { 0, 1, 2, 3 };
            byte[] second = new byte[4] { 4, 5, 6, 7 };

            blockWriter.WriteBlock(stream, stream.Position, first);
            Assert.AreEqual(equalFirst, stream.ToArray());

            blockWriter.WriteBlock(stream, stream.Position, second);
            Assert.AreEqual(equalSecond, stream.ToArray());
        }   
    }
}
