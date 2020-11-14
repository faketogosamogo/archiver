using System;
using System.Diagnostics;
using System.IO;

namespace FileArchiver
{

    //Certutil -hashfileme

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 3)
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
                            if (fileCompressor.CompressFile(args[1], args[2]))
                            {
                                Console.WriteLine("Файл сжат!");
                                return 1;
                            }
                            else
                            {
                                Console.WriteLine("Ошибка расжатия, возможно у вас открыт файл для сжатия.");
                                return 0;
                            }
                        }
                        else
                        {
                            MultithreadFileDecompressor fileDecompressor = new MultithreadFileDecompressor(new BlockGziper(), new BlockStreamWriter(), new BlockStreamReader());
                            if(fileDecompressor.DecompressFile(args[1], args[2]))
                            {
                                Console.WriteLine("Файл расжат");
                                return 1;
                            }
                            else
                            {
                                Console.WriteLine("Ошибка расжатия, возможно у вас открыт файл для расжатия.");
                                return 0;
                            }
                        }
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
