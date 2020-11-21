using FileArchiver;
using FileArchiver.DataStructures;
using NUnit.Framework;
namespace FileArchiverTest.DataStructures
{

    [TestFixture]
    class ConcurrencyBlockStackTest
    {
        
        [Test]
        public void Pop_should_return_last_pushed_block_item()
        {
            var stack = new ConcurrencyBlockStack();
            var expected = new BlockWithPosition(new byte[] { 0, 1 }, 400);

            stack.Push(new BlockWithPosition(new byte[] { 0, 1 }, 0));
            stack.Push(expected);


            Assert.AreEqual(expected, stack.Pop());
        }
        [Test]
        public void Pop_after_stop_writing_should_return_null()
        {
            var stack = new ConcurrencyBlockStack();

            stack.Push(new BlockWithPosition(new byte[] { 0, 1 }, 0));
            stack.Pop();

            stack.StopWriting();
            Assert.IsNull(stack.Pop());            
        }
    }
}
