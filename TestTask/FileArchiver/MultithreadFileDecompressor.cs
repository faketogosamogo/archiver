using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using FileArchiver.BlockServices;
using FileArchiver.BlockServices.Exceptions;
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

        private static FileStream _inputFile;
        private static FileStream _outputFile;

        //количество одновременно запускаемых потоков
        private static int _threadsCount;

        private static bool _isFileClosed;
        private static object _readLocker = new object();
        private static object _writeLocker = new object();

        private ConcurrencyBlockStack _readedBlocks;
        private ConcurrencyBlockStack _decompressedBlocks;
        public MultithreadFileDecompressor(IBlockDecompressor blockDecompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader, 
                                           int threadsCount=5)
        {
            _blockDecompressor = blockDecompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
            _threadsCount = threadsCount;

            _readedBlocks = new ConcurrencyBlockStack();
            _decompressedBlocks = new ConcurrencyBlockStack();
        }

        private void readBlocksThread()
        {
            while (true)
            {
                if (_readedBlocks.Count() >= _threadsCount) continue; //Регулирование одновременно хранимых считанных блоков
                var block = new BlockWithPosition();
                lock (_readLocker)
                {
                    if (_inputFile.Position >= _inputFile.Length)
                    {
                        _readedBlocks.StopWriting();
                        break;
                    }
                    try
                    {
                        var blockPosBuf = new byte[8];
                        _inputFile.Read(blockPosBuf);
                        long blockPos = BitConverter.ToInt64(blockPosBuf);

                        var blockLenBuf = new byte[4];
                        _inputFile.Read(blockLenBuf);
                        int blockLen = BitConverter.ToInt32(blockLenBuf);
                        block.Position = blockPos;
                        block.Block = _blockReader.ReadBlock(_inputFile, _inputFile.Position, blockLen);
                        if (block.Block.Length == 0) return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ex: {ex.Message}");
                    }
                }
                _readedBlocks.Push(block);

            }
        }

        private void decompressBlockThread()
        {
            while (true)
            {
                if (_decompressedBlocks.Count() >= _threadsCount) continue; //Регулирование одновременно хранимых сжатых блоков

                var block = _readedBlocks.Pop();
                if (block == null)
                {
                    _decompressedBlocks.StopWriting();
                    break;
                }

                block.Block = _blockDecompressor.DecompressBlock(block.Block);
                _decompressedBlocks.Push(block);
            }

        }

        private void writeBlocksThread()
        {
            while (true)
            {
                var block = _decompressedBlocks.Pop();
                if (block == null)
                {
                    return;
                }
                                
                lock (_writeLocker)
                {
                    try
                    {
                        _blockWriter.WriteBlock(_outputFile, block.Position, block.Block);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ex: {ex.Message}");
                    }
                }
            }
        }
        public bool DecompressFile(string inputFilePath, string outputFilePath)
        {
            try
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex.StackTrace}");
               // Console.WriteLine($"{ex.InnerException.Message}, {ex.InnerException.StackTrace}");
                if (File.Exists(outputFilePath)) File.Delete(outputFilePath);
                return false;
            }
            finally
            {
                Console.WriteLine("5");
                _inputFile.Dispose();
                _outputFile.Dispose();
            }
            return true;
        }
    }
}