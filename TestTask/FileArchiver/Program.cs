using System;
using System.Diagnostics;

namespace FileArchiver
{
    class Program
    {
        static void Main(string[] args)
        {
            MultithreadFileCompressor fileCompressor = new MultithreadFileCompressor(new BlockGzipCompressor(), new BlockFileStreamWriter(), new BlockFileStreamReader());
            Stopwatch stopwatch = Stopwatch.StartNew();
            fileCompressor.CompressFile(@"H:\5d1f09e185e17.vid", @"H:\5d1f09e185e17.vidgz");
            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000);

            // FileCompressor.CompressFile(@"H:\5d1f09e185e17.vid");
            stopwatch.Start();
            FileDecompressor.DecompressFile(@"H:\5d1f09e185e17.vidgz");
            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000);
        }
    }
}
