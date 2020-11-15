using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using FileArchiver.BlockServices;
using FileArchiver.BlockServices.Exceptions;

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

        public MultithreadFileDecompressor(IBlockDecompressor blockDecompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader, int threadsCount=5)
        {
            _blockDecompressor = blockDecompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
            _threadsCount = threadsCount;
        }
        private void readBlocks()
        {
            while (!_isFileClosed)
            {
                BlockingCollection<BlockWithPosition> blocks = new BlockingCollection<BlockWithPosition>();//Чтобы количество занимаемой памяти приложения не привышало норму
                for (int i = 0; i < _threadsCount; i++)
                {
                    lock (_readLocker)
                    {
                        if (_inputFile.Position >= _inputFile.Length)
                        {
                            _isFileClosed = true;
                            continue;
                        }
                        try
                        {
                            var blockPosBuf = new byte[8];
                            _inputFile.Read(blockPosBuf);
                            long blockPos = BitConverter.ToInt64(blockPosBuf);

                            var blockLenBuf = new byte[4];
                            _inputFile.Read(blockLenBuf);
                            int blockLen = BitConverter.ToInt32(blockLenBuf);

                            var block = _blockReader.ReadBlock(_inputFile, _inputFile.Position, blockLen);
                            if (block.Length == 0) return;
                            blocks.Add(new BlockWithPosition(block, blockPos));
                        }catch(Exception ex)
                        {
                            throw new ReadBlockException($"Ошибка чтения блока", ex);
                        }
                    }
                }
              
                blocks.CompleteAdding();
                decompressAndWriteBlocks(blocks);
            }
        }
        private void decompressAndWriteBlocks(BlockingCollection<BlockWithPosition> blocks)
        {
            foreach (var block in blocks)
            {
                try
                {
                    block.Block = _blockDecompressor.DecompressBlock(block.Block);
                }catch(Exception ex)
                {
                    throw new DecompressBlockException($"Ошибка расжатия блока", ex);
                }
               // using var outputFile = new FileStream(_outputFilePath, FileMode.Open, FileAccess.Write, FileShare.Write));
                lock (_writeLocker)
                {
                    try
                    {
                        _blockWriter.WriteBlock(_outputFile, block.Position, block.Block);
                    }catch(Exception ex)
                    {
                        throw new WriteBlockException($"Ошибка записи блока", ex);
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

                List<Thread> threads = new List<Thread>();
                for (int i = 0; i < _threadsCount; i++) threads.Add(new Thread(readBlocks));
                foreach (var th in threads) th.Start();
                foreach (var th in threads) th.Join();
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
                _inputFile.Dispose();
                _outputFile.Dispose();
            }
            return true;
        }
    }
}