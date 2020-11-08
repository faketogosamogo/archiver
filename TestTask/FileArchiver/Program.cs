using System;
using System.Diagnostics;
using System.IO;

namespace FileArchiver
{
    //Certutil -hashfile
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                MultithreadFileCompressor fileCompressor = new MultithreadFileCompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());
                MultithreadFileDecompressor fileDecompressor = new MultithreadFileDecompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());

                Stopwatch stopwatch = Stopwatch.StartNew();

                fileCompressor.CompressFile(@"H:\1.jpg", @"H:\1.jpgz");
                stopwatch.Stop();

                Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000);

                stopwatch.Start();
                fileDecompressor.DecompressFile(@"H:\1.jpgz", @"H:\11.jpg");
                stopwatch.Stop();

                Console.WriteLine(stopwatch.ElapsedMilliseconds / 1000);
            }else if (args.Length == 3)
            {

                while (true)
                {
                    bool allRight = true;

                    switch (args[0])
                    {
                        case "compress": break;
                        case "decompress": break;
                        default:
                            {
                                allRight = false;
                                Console.WriteLine("Не распознана операция!");
                                break;
                            }
                    }
                    if (!File.Exists(args[1]))
                    {
                        allRight = false;
                        Console.WriteLine("Не найден файл для обработки!");
                    }
                    if (File.Exists(args[2]))
                    {
                        allRight = false;
                        Console.WriteLine("Путь для расположения результирующего файла уже занят!");
                    }

                    if (allRight)
                    {
                        if(args[0]== "compress")
                        {
                            MultithreadFileCompressor fileCompressor = new MultithreadFileCompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());
                            fileCompressor.CompressFile(args[1], args[2]);

                            Console.WriteLine("Файл сжат!");
                        }
                        else
                        {
                            MultithreadFileDecompressor fileDecompressor = new MultithreadFileDecompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());
                            fileDecompressor.DecompressFile(args[1], args[2]);

                            Console.WriteLine("Файл расжат");
                        }
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }
            return 0;
        }
    }
}
