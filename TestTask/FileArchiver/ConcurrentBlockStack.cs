using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver
{
    class ConcurrentBlockStack
    {
        private Stack<BlockWithPosition> _blocks;
        private object _blocksLocker;
        public ConcurrentBlockStack()
        {
            _blocks = new Stack<BlockWithPosition>();
            _blocksLocker = new object();
        }

        public void Push(BlockWithPosition block)
        {
            lock (_blocksLocker)
            {
                _blocks.Push(block);
            }
        }
        public bool TryPop(out BlockWithPosition block)
        {
            var _block = new BlockWithPosition();
            bool isOk = false;
            lock (_blocksLocker)
            {
                isOk = _blocks.TryPop(out _block);
            }
            block = _block;
            return isOk;
        }
        public int Count()
        {
            int count = 0;
            lock (_blocksLocker)
            {
                count =  _blocks.Count;
            }
            return count;
        }
    }
}
