using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using FileArchiver.BlockServices;
using FileArchiver.DataStructures;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileCompressor.
    /// В своей реализации работает поблочно с файлом.    
    /// Используются потоки на чтение, сжатие и запись
    ///Вначале записывается позиция блока в исходном файле, далее длина записанного блока, далее сам блок.

    ///С организацией нагрузки не справился, думал брать из ComputerInfo.AvailableVirtualMemory, и назначать размеру блока количество свободной оперативной памяти/(количество потоков ^ 3),
        ///(количество потоков ^ 3) т.к такое количество у меня может в раз находиться в худшем раскладе(в моём представлении)в каждом потоке вызывается количество считываний равное потокам
    ///Но не нашёл его аналога, поэтому пока решил оставить так.
   
    public class MultithreadFileCompressor : IFileCompressor
    {
        private IBlockCompressor _blockCompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private FileStream _outputFile;

        //используется для поблочого чтения из файла
        private object _currentReadIndexLocker = new object();
        private int _currentReadIndex;

        private object _currentCompressedIndexLocker = new object(); 
        private int _currentCompressedIndex;//Используется подсчёта количества сжатых блоков

        private object _isFileClosedLocker = new object();
        private bool _isFileClosed;
        
        private object _isFileCompressedLocker = new object();
        private bool _isFileCompressed;      
       
        private bool _lockedIsFileClosed
        {
            get
            {
                lock (_isFileClosedLocker)
                {
                    return _isFileClosed;
                }
            }
            set
            {
                lock (_isFileClosedLocker)
                {
                    _isFileClosed = value;
                }
            }
        }
        private bool _lockedIsFileCompressed
        {
            get
            {
                lock (_isFileCompressedLocker)
                {
                    return _isFileCompressed;
                }
            }
            set
            {
                lock (_isFileCompressedLocker)
                {
                    _isFileCompressed = value;
                }
            }
        }

        //длина обрабатываемого блока
        private int _blockLen;
        //количество одновременно запускаемых потоков
        private int _threadsCount;

        private object _writeLocker = new object();        
        private string _inputFilePath;

        private object _threadsExceptionLocker = new object();
        private Exception _threadsException;
        private Exception _lockedThreadsException
        {
            get
            {
                lock (_threadsExceptionLocker)
                {
                    return _threadsException;
                }
            }
            set
            {
                lock (_threadsExceptionLocker)
                {
                    _threadsException = value;
                }
            }
        }
        private ConcurrentBlockStack _readedBlocks;
        private ConcurrentBlockStack _compressedBlocks;
        public MultithreadFileCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader, 
            int threadsCount=15, int blockLen = (1024*1024))
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
            _blockLen = blockLen;
            _threadsCount = threadsCount;

            _currentReadIndex = 0;
            _readedBlocks = new ConcurrentBlockStack();
            _compressedBlocks = new ConcurrentBlockStack();
        }    

        private void readBlockThread()
        {
            if (_lockedThreadsException != null) return;

            while (true)
            {               
                using var inputFile = File.OpenRead(_inputFilePath);
                long pos = 0;
                lock (_currentReadIndexLocker)
                {
                    pos = _currentReadIndex * _blockLen;
                    if (pos >= inputFile.Length)
                    {
                        _lockedIsFileClosed = true;
                        break;
                    }
                    _currentReadIndex++;
                }             

                byte[] block = null;
                try
                {
                    block = _blockReader.ReadBlock(inputFile, pos, _blockLen);
                }
                catch (Exception ex)
                {
                    _lockedThreadsException = ex;
                    break;
                }
                _readedBlocks.Push(new BlockWithPosition(block, pos));
            }          

        }
        private void compressBlockThread()
        {
            if (_lockedThreadsException != null) return;

            while (true)
            {
                BlockWithPosition block = null;
                while (_readedBlocks.TryPop(out block) == false && _lockedIsFileClosed == false) { }              
                try
                {
                    if (block == null)
                    {
                        break;
                    }
                    block.Block = _blockCompressor.CompressBlock(block.Block);
                }
                catch (Exception ex)
                {
                    _lockedThreadsException = ex;
                    break;
                }

                _compressedBlocks.Push(block);
            }
            lock (_currentCompressedIndexLocker)
            {
                _currentCompressedIndex++;
                if (_currentCompressedIndex == _threadsCount)
                {
                    _lockedIsFileCompressed = true;
                }
            }
        }

        private void writeBlockThread()
        {
            if (_lockedThreadsException != null) return;

            while (true)
            {
                BlockWithPosition block = null;

                while (_compressedBlocks.TryPop(out block) == false && _lockedIsFileCompressed==false) {}
                if (block == null) break;

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
                        _lockedThreadsException = ex;
                        break;
                    }
                }
            }
        }
         
      
       
        public bool CompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFilePath = inputFilePath;

            
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
         
          
            _outputFile.Dispose();
            if (_threadsException != null)
            {
                if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
                Console.WriteLine($"Во время сжатия файла произошло исключение: {_threadsException.Message}, trace: {_threadsException.StackTrace}");
                return false;

            }
            return true;
        }
    }
}
