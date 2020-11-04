using System;
using System.Diagnostics;

namespace FileArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            MultithreadFileCompressor fileCompressor = new MultithreadFileCompressor();
            Stopwatch stopwatch = Stopwatch.StartNew();
            //fileCompressor.CompressFile(@"H:\5d1f09e185e17.vid");

            
            FileCompressor.CompressFile(@"H:\5d1f09e185e17.vid");
            //FileDecompressor.DecompressFile(@"H:\5d1f09e185e17.vid.gz");
            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000);
        }
    }
}
