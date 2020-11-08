using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileArchiver
{
    public enum BlockState
    {
                       
    }
    public class ConcurrencyBlock
    {
        public byte[] Block { get; set; }
        public long Position { get; set; }
        public bool IsProcessed { get; set; }
        public bool IsChanged { get; set; }
    }
    public class ConcurrencyBlockList
    {
        private List<ConcurrencyBlock> _blocks;
        private static object _isListLocked;
        public ConcurrencyBlockList()
        {
            _blocks = new List<ConcurrencyBlock>();
            _isListLocked = new object();
        }
        public void Add(ConcurrencyBlock block)
        {
            lock (_isListLocked)
            {
                _blocks.Add(block);
            }
        }
        public bool Remove(ConcurrencyBlock block)
        {
            lock (_isListLocked)
            {
                return _blocks.Remove(block);
            }
        }
        public ConcurrencyBlock GetBlockForChange()
        {
            lock (_isListLocked)
            {
                var block = _blocks.FirstOrDefault(b => b.IsProcessed == false && b.IsChanged == false);
                if (block != null)
                {
                    block.IsProcessed = true;
                }
                return block;
            }
        }
        public ConcurrencyBlock GetBlockForWrite()
        {
            lock (_isListLocked)
            {
                var block = _blocks.FirstOrDefault(b => b.IsProcessed == false && b.IsChanged == true);
                if (block != null)
                {
                    block.IsProcessed = true;
                }
                return block;
            }
            
        }
    }
}
