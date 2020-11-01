using System;

namespace FileArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
             FileCompressor.CompressFile(@"H:\5d1f09e185e17.vid");
             FileDecompressor.DecompressFile(@"H:\5d1f09e185e17.vid.gz");
        }
    }
}
