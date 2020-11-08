using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver
{
    class ThreadBlockReadState
    {
        public int BlockLen { get; set; }
        public int Iter { get; set; }

        public ThreadBlockReadState(int blockLen, int iter)
        {
            BlockLen = blockLen;
            Iter = iter;
        }
    }
}
