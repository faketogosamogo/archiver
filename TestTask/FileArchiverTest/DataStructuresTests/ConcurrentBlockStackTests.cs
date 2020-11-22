using System;
using System.Collections.Generic;
using System.Text;
using FileArchiver.DataStructures;
using NUnit.Framework;
namespace FileArchiverTest.DataStructuresTests
{
    [TestFixture]
    public class ConcurrentBlockStackTests
    {

        [Test]
        public void TryPop_should_return_last_Pushed_block()
        {
            var blockStack = new ConcurrentBlockStack();
            var expected = new BlockWithPosition(new byte[] { 1, 2, 3 }, 22);
            
            blockStack.Push(new BlockWithPosition(new byte[] { 2, 3, 4 }, 11));
            blockStack.Push(expected);

            var tempExpected = new BlockWithPosition();
            blockStack.TryPop(out tempExpected);

            Assert.AreEqual(expected, tempExpected);
        }

        [Test]
        public void Count_should_return_count_of_pushed_items()
        {
            int expected = 44;
            var blockStack = new ConcurrentBlockStack();
            for (int i = 0; i < expected; i++)
            {
                blockStack.Push(new BlockWithPosition(new byte[] { 2, 3, 4 }, 11));
            }
            Assert.AreEqual(expected, blockStack.Count());
        }
    }
}
