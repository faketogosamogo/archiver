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
    /// Мультипоточная реализация IFileDecompressor       
    /// </summary>
    public class MultithreadFileDecompressor : IFileDecompressor
    {
        private IBlockDecompressor _blockDecompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private FileStream _inputFile;
        private FileStream _outputFile;

        //количество одновременно запускаемых потоков
        private int _threadsCount;

        private object _isFileClosedLocker = new object();
        private bool _isFileClosed;

        private object _isFileDecompressedLocker = new object();
        private bool _isFileDecompressed;

        private object _currentDecompressedIndexLocker = new object();
        private int _currentDecompressedIndex;

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
        private bool _lockedIsFileDecompressed
        {
            get
            {
                lock (_isFileDecompressedLocker)
                {
                    return _isFileDecompressed;
                }
            }
            set
            {
                lock (_isFileDecompressedLocker)
                {
                    _isFileDecompressed = value;
                }
            }
        }

        private object _readLocker = new object();
        private object _writeLocker = new object();

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
        private ConcurrentBlockStack _decompressedBlocks;
        public MultithreadFileDecompressor(IBlockDecompressor blockDecompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader,
                                           int threadsCount = 15)
        {
            _blockDecompressor = blockDecompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
            _threadsCount = threadsCount;

            _readedBlocks = new ConcurrentBlockStack();
            _decompressedBlocks = new ConcurrentBlockStack();
        }

        private void readBlocksThread()
        {
            
            if (_lockedThreadsException != null) return;
            
            while (true)
            {
               
                lock (_readLocker)
                {
                    var block = new BlockWithPosition();
                    try
                    {
                        if (_inputFile.Position >= _inputFile.Length)
                        {
                            _lockedIsFileClosed = true;
                            break;
                        }
                        var blockPosBuf = new byte[8];
                        _inputFile.Read(blockPosBuf);
                        long blockPos = BitConverter.ToInt64(blockPosBuf);
                        var blockLenBuf = new byte[4];
                        _inputFile.Read(blockLenBuf);
                        int blockLen = BitConverter.ToInt32(blockLenBuf);

                        block.Position = blockPos;
                        block.Block = _blockReader.ReadBlock(_inputFile, _inputFile.Position, blockLen);                        
                    }
                    catch (Exception ex)
                    {
                        _lockedThreadsException = ex;                        
                        break;
                    }
                    _readedBlocks.Push(block);
                }
            }
        }

        private void decompressBlockThread()
        {
           
            if (_lockedThreadsException != null) return;
            
            while (true)
            {
                BlockWithPosition block = null;
                while (_readedBlocks.TryPop(out block) == false && _lockedIsFileClosed == false){}
                try
                {
                    if (block == null)
                    {
                        break;
                    }
                    block.Block = _blockDecompressor.DecompressBlock(block.Block);
                }
                catch (Exception ex)
                {
                    _lockedThreadsException = ex;                    
                    break;
                }

                _decompressedBlocks.Push(block);
            }
            lock (_currentDecompressedIndexLocker)
            {
                _currentDecompressedIndex++; 
                if (_currentDecompressedIndex == _threadsCount)
                {
                    _lockedIsFileDecompressed = true;
                }
            }           
        }

        private void writeBlocksThread()
        {
            if (_lockedThreadsException != null) return;
           
            while (true)
            {                
                var block = new BlockWithPosition();

                while (_decompressedBlocks.TryPop(out block) == false && _lockedIsFileDecompressed == false){}
                if (block == null) break;
                lock (_writeLocker)
                {
                    try
                    {
                        _blockWriter.WriteBlock(_outputFile, block.Position, block.Block);
                    }
                    catch (Exception ex)
                    {
                        _lockedThreadsException = ex;                        
                        break;
                    }
                }
            }
        }
        public bool DecompressFile(string inputFilePath, string outputFilePath)
        {
          
                _inputFile = File.OpenRead(inputFilePath);
                _outputFile = File.OpenWrite(outputFilePath);

                var readThreads = new List<Thread>();
                var decompressThreads = new List<Thread>();
                var writeThreads = new List<Thread>();

                for (int i = 0; i < _threadsCount; i++)
                {
                    readThreads.Add(new Thread(readBlocksThread));
                    decompressThreads.Add(new Thread(decompressBlockThread));
                    writeThreads.Add(new Thread(writeBlocksThread));
                }
                for (int i = 0; i < _threadsCount; i++)
                {
                    readThreads[i].Start();
                    decompressThreads[i].Start();
                    writeThreads[i].Start();
                }
                for (int i = 0; i < _threadsCount; i++)
                {
                    readThreads[i].Join();
                    decompressThreads[i].Join();
                    writeThreads[i].Join();
                }
                _inputFile.Dispose();
                _outputFile.Dispose();

                if (_lockedThreadsException != null)
                {
                    if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
                    Console.WriteLine($"Во время расжатия файла произошло исключение: {_threadsException.Message}, trace: {_threadsException.StackTrace}");
                    return false;
                }

            return true;
        }
    }
}

