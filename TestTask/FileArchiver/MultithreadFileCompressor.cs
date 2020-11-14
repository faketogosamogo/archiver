using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using FileArchiver.Exceptions;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileCompressor.
    /// В своей реализации работает поблочно с файлом.
    /// (Считывает и Сжимает) блоки -> Записывает блоки.
    ///Запись в файл происходит постепенно(не мультипоточно).
    ///Это было выбрано в связи с тем, что в конечном итоге при использовании нескольких потоков для ускорения сжатия, минимально возможное время сводится к записи в файл,
    ///т.к разбивать поблочно файл для записи нецелесообразно в связи с неизвестностью размера сжатых блоков
    ///Вначале записывается позиция блока в исходном файле, далее длина записанного блока, далее сам блок.

    ///С организацией нагрузки не справился, думал брать из ComputerInfo.AvailableVirtualMemory, и назначать размеру блока количество свободной оперативной памяти/(количество потоков ^ 3),
        ///(количество потоков ^ 3) т.ктакое количество у меня может в раз находиться в худшем раскладе(в моём представлении)в каждом потоке вызывается количество считываний равное потокам
    ///Но не нашёл его аналога, поэтому пока решил оставить так.
    class BlockWithPosition
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
        private static object _notCompressedBlocksLocker = new object();
        private static object _compressedBlocksLocker = new object();
        
        private static string _inputFilePath;  

        private static bool _isFileClosed;
        private static bool _isFileCompressed;

        ConcurrentStack<BlockWithPosition> _notCompressedBlocks;
        ConcurrentStack<BlockWithPosition> _compressedBlocks;
        public MultithreadFileCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader, int threadsCount=5, int blockLen = (1024*1024)*10)
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
            _blockLen = blockLen;
            _threadsCount = threadsCount;
            _notCompressedBlocks = new ConcurrentStack<BlockWithPosition>();
            _compressedBlocks = new ConcurrentStack<BlockWithPosition>();
        }     
        private void readBlockThread()
        {
            while (true)
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
                _notCompressedBlocks.Push(new BlockWithPosition(block, pos));
            }

        }
        private void compressBlockThread()
        {
            while (true)
            {
                lock (_compressedBlocksLocker)
                    if (_notCompressedBlocks.Count == 0 && _isFileClosed)
                    {
                        _isFileCompressed = true;
                        break;
                    }
                var block = new BlockWithPosition();
                while (true)
                {
                    if (_notCompressedBlocks.TryPop(out block)) break;
                }
                block.Block = _blockCompressor.CompressBlock(block.Block);
                _compressedBlocks.Push(block);
            }
        }
        private void writeBlockThread()
        {
            while (true)
            {
                lock (_writeLocker)
                    if (_compressedBlocks.Count == 0 && _isFileCompressed) break;
                var block = new BlockWithPosition();
                while (true)
                {
                    if (_compressedBlocks.TryPop(out block)) break;
                }
                var blockWithPosLen = new byte[8 + 4 + block.Block.Length];
                BitConverter.GetBytes(block.Position).CopyTo(blockWithPosLen, 0);
                BitConverter.GetBytes(block.Block.Length).CopyTo(blockWithPosLen, 8);

                block.Block.CopyTo(blockWithPosLen, 12);
                lock (_writeLocker)
                {
                    try
                    {
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
                var writeThreads = new List<Thread>();

                for (int i = 0; i < _threadsCount; i++)
                {
                    readThreads.Add(new Thread(readBlockThread));
                    compressThreads.Add(new Thread(compressBlockThread));
                    writeThreads.Add(new Thread(writeBlockThread));
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
                Console.WriteLine($"{ex.InnerException.Message}, {ex.InnerException.StackTrace}");

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
