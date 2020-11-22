using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.DataStructures
{
    public class BlockWithPosition
    {
        public byte[] Block { get; set; }
        public long Position { get; set; }
        public BlockWithPosition() { }
        public BlockWithPosition(byte[] block, long position)
        {
            Block = block;
            Position = position;
        }
    }
}
