using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Linq;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileCompressor.
    /// В своей реализации работает поблочно с файлом.
    /// (Считывает и Сжимает) блоки -> Записывает блоки.
    ///Запись в файл происходит постепенно(не мультипоточно).
    ///Это было выбрано в связи с тем, что в конечном итоге при использовании нескольких потоков для ускорения сжатия, минимально возможное время сводится к записи в файл,
        ///т.к разбивать поблочно файл для записи нецелесообразно в связи с неизвестностью размера сжатых блоков

    class BlockWithPosition
    {
        public string Id { get; set; }
        public byte[] Block { get; set; }
        public long Position { get; set; }
        public bool IsCompress { get; set; }
        public BlockWithPosition(byte[] block, long position, bool isCompress)
        {
            Id = Guid.NewGuid().ToString();
            Block = block;
            Position = position;
            IsCompress = isCompress;
        }
    }
    public class MultithreadStreamCompressor : IFileCompressor
    {
        private IBlockCompressor _blockCompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private static FileStream _outputFile;

        //используется для поблочого чтения из файла
        private static long _currentReadIndex = 0;
        private static object _currentReadIndexLocker = new object();

        //длина обрабатываемого блока
        private static int _blockLen = (1024 * 1024) * 10;
        //количество одновременно запускаемых потоков
        private static int _threadsCount = 5;


        private static object _writeLocker = new object();
        
        private static string _inputFilePath;  
        private static bool _isFileClosed;

        public MultithreadStreamCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader)
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
        }     
        private void ReadAndCompressBlocks()
        {
            while (!_isFileClosed) {//Чтобы потоки не пересоздавались

                BlockingCollection<BlockWithPosition> blocks = new BlockingCollection<BlockWithPosition>();//Чтобы количество занимаемой памяти приложения не привышало норму
                for (int i = 0; i < _threadsCount; i++)
                {
                    long pos = 0;
                    using var inputFile = File.OpenRead(_inputFilePath);

                    lock (_currentReadIndexLocker)
                    {
                        pos = _currentReadIndex * _blockLen;
                        _currentReadIndex++;
                        if (pos >= inputFile.Length)
                        {
                            _isFileClosed = true;
                            continue;
                        }
                    }

                    var block = _blockReader.ReadBlock(inputFile, pos, _blockLen);
                    if (block.Length == 0) return;
                    MD5 md = MD5.Create();

                    Console.WriteLine(Convert.ToBase64String(md.ComputeHash(block)) + " " + pos);

                    block = _blockCompressor.CompressBlock(block);
                    blocks.Add(new BlockWithPosition(block, pos, true));
                }

                blocks.CompleteAdding();
                WriteBlocks(blocks);//После считывания сжатия порции блоков записываем их в файл
            }
        }
        private void WriteBlocks(BlockingCollection<BlockWithPosition> blocks)
        {
           foreach (var block in blocks)
           {
                var blockWithPosLen = new byte[8 + 4 + block.Block.Length];
                BitConverter.GetBytes(block.Position).CopyTo(blockWithPosLen, 0);
                BitConverter.GetBytes(block.Block.Length).CopyTo(blockWithPosLen, 8);

                block.Block.CopyTo(blockWithPosLen, 12);
                lock (_writeLocker)
                {
                    _blockWriter.WriteBlock(_outputFile, _outputFile.Position, blockWithPosLen);
                }
           }
        }
       
        public void CompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFilePath = inputFilePath;
            _outputFile = File.OpenWrite(outputFilePath);
            Console.WriteLine(_blockLen);

            _outputFile.Write(BitConverter.GetBytes(_blockLen));

            List<Thread> threads = new List<Thread>();

            for (int i = 0; i < _threadsCount; i++) threads.Add(new Thread(ReadAndCompressBlocks));
           
            foreach (var th in threads) th.Start(); 
            foreach (var th in threads) th.Join();
        
            _outputFile.Dispose();       
        }
    }
}
