using System;
using System.Diagnostics;

namespace FileArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            MultithreadFileCompressor fileCompressor = new MultithreadFileCompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());
            MultithreadFileDecompressor fileDecompressor = new MultithreadFileDecompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());
            Stopwatch stopwatch = Stopwatch.StartNew();
        //    fileCompressor.CompressFile(@"H:\123.avi", @"H:\123.aviz");
            stopwatch.Stop();
            //Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000);

            // FileCompressor.CompressFile(@"H:\5d1f09e185e17.vid");
            stopwatch.Start();
           // FileDecompressor.DecompressFile(@"H:\5d1f09e185e1.vidgz");
            fileDecompressor.DecompressFile(@"H:\123.aviz", @"H:\123.avizaa");
            stopwatch.Stop();


            Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000);
        }
    }
}
