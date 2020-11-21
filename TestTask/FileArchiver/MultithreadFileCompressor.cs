using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FileArchiver.BlockServices;
using FileArchiver.DataStructures;
using FileArchiver.BlockServices.Exceptions;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileCompressor.
    /// В своей реализации работает поблочно с файлом.
    /// Считывает -> Сжимает -> Записывает блоки.  
        ///(Считывание из файла и запись в коллекцию считанных блоков) | (Считывание из коллекции считанных блоков, сжатие, запись в коллекцию сжатых блоков) |
            ///(Считывание из коллекции сжатых блоков и запись в файл)

    ///С организацией нагрузки не справился, думал брать из ComputerInfo.AvailableVirtualMemory, и назначать размеру блока количество свободной оперативной памяти/(количество потоков ^ 3),
    ///(количество потоков ^ 3) т.ктакое количество у меня может в раз находиться в худшем раскладе(в моём представлении)в каждом потоке вызывается количество считываний равное потокам
    ///Но не нашёл его аналога, поэтому пока решил оставить так.
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
    public class MultithreadFileCompressor : IFileCompressor
    {
        private IBlockCompressor _blockCompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private static FileStream _outputFile;

        //используется для поблочого чтения из файла
        private static long _currentReadIndex = 0;
        private static object _currentReadIndexLocker = new object();

        //длина обрабатываемого блока
        private static int _blockLen;
        //количество одновременно запускаемых потоков
        private static int _threadsCount;

        private static object _writeLocker = new object();        
        private static object _compressLocker = new object();        
        private static string _inputFilePath;
        private static bool _fileIsEnd;

        private ConcurrentStack<BlockWithPosition> _readedBlocks;
        private ConcurrentStack<BlockWithPosition> _compressedBlocks;
        public MultithreadFileCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader,
                                         int threadsCount=5, int blockLen = (1024*1024))
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
            _blockLen = blockLen;
            _threadsCount = threadsCount;

            _readedBlocks = new ConcurrentStack<BlockWithPosition>();
            _compressedBlocks = new ConcurrentStack<BlockWithPosition>();
        }     
        private void readBlocksThread()
        {
            while (true)
            {
                if (_readedBlocks.Count >= _threadsCount) continue; //Регулирование одновременно хранимых считанных блоков
                long pos = 0;
                using var inputFile = File.OpenRead(_inputFilePath);

                lock (_currentReadIndexLocker)
                {
                    pos = _currentReadIndex * _blockLen;
                    _currentReadIndex++;
                    if (pos >= inputFile.Length)
                    {
                        _fileIsEnd = true;
                        break;
                    }
                }

                byte[] block = null;
                try
                {
                    block = _blockReader.ReadBlock(inputFile, pos, _blockLen);
                    if (block.Length == 0) return;
                }
                catch (Exception ex)
                {
                  //  throw new ReadBlockException($"Ошибка чтения блока: {ex.Message}, {ex.StackTrace}", ex);
                    Console.WriteLine($"ex: {ex.Message}");
                }

                _readedBlocks.Push(new BlockWithPosition(block, pos));
            }

        }
        private void compressBlocksThread()
        {
            while (true)
            {
                if (_compressedBlocks.Count >= _threadsCount) continue; //Регулирование одновременно хранимых сжатых блоков
                BlockWithPosition block = new BlockWithPosition();

                lock (_compressLocker) {
                    while (_readedBlocks.TryPop(out block) == false && ) { }
                }
                if (block == null)
                {
                    return;
                }
                block.Block = _blockCompressor.CompressBlock(block.Block);
                _compressedBlocks.Push(block);
            }
        }
        private void writeBlocksThread()
        {
            while (true)
            {              
               
                var block =  _compressedBlocks.Pop();
                if (block == null) return;

                var blockWithPosLen = new byte[8 + 4 + block.Block.Length];
                BitConverter.GetBytes(block.Position).CopyTo(blockWithPosLen, 0);
                BitConverter.GetBytes(block.Block.Length).CopyTo(blockWithPosLen, 8);

                block.Block.CopyTo(blockWithPosLen, 12);
                lock (_writeLocker)
                {
                    try
                    {
                        MD5 md = MD5.Create();

                      //  Console.WriteLine($"pos: {block.Position} {Convert.ToBase64String(md.ComputeHash(block.Block))}");

                        _blockWriter.WriteBlock(_outputFile, _outputFile.Position, blockWithPosLen);
                    }
                    catch (Exception ex)
                    {
                        throw new WriteBlockException($"Ошибка записи блока", ex);
                    }
                }
            }
        }
       
      
       
        public bool CompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFilePath = inputFilePath;

            try
            {
                _outputFile = File.OpenWrite(outputFilePath);


                var readThreads = new List<Thread>();
                var compressThreads = new List<Thread>();
                var writeThreads = new List<Thread>();//Возможно следовало бы в одном потоке крутить, но решил как и остальные на несколько делить

                for (int i = 0; i < _threadsCount; i++)
                {
                    readThreads.Add(new Thread(readBlocksThread));
                    compressThreads.Add(new Thread(compressBlocksThread));
                    writeThreads.Add(new Thread(writeBlocksThread));
                }
                for (int i = 0; i < _threadsCount; i++)
                {
                    readThreads[i].Start();
                    compressThreads[i].Start();
                    writeThreads[i].Start();
                }
                for (int i = 0; i < _threadsCount; i++)
                {
                    readThreads[i].Join();
                    compressThreads[i].Join();
                    writeThreads[i].Join();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex.StackTrace}");
                if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
                return false;
            }
            finally
            {
                _outputFile.Dispose();
            }
            return true;
        }
    }
}