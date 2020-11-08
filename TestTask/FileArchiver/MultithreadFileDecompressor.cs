using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileDecompressor
    /// 
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
        private void ReadBlocks()
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
                        var blockPosBuf = new byte[8];
                        _inputFile.Read(blockPosBuf);
                        long blockPos = BitConverter.ToInt64(blockPosBuf);

                        var blockLenBuf = new byte[4];
                        _inputFile.Read(blockLenBuf);
                        int blockLen = BitConverter.ToInt32(blockLenBuf);

                        var block = _blockReader.ReadBlock(_inputFile, _inputFile.Position, blockLen);
                        if (block.Length == 0) return;
                        blocks.Add(new BlockWithPosition(block, blockPos, false));
                    }
                }
              
                blocks.CompleteAdding();
                DecompressAndWriteBlocks(blocks);
            }
        }
        private void DecompressAndWriteBlocks(BlockingCollection<BlockWithPosition> blocks)
        {
            foreach (var block in blocks)
            {
                block.Block = _blockDecompressor.DecompressBlock(block.Block);

               // using var outputFile = new FileStream(_outputFilePath, FileMode.Open, FileAccess.Write, FileShare.Write));
                lock (_writeLocker)
                {
                    _blockWriter.WriteBlock(_outputFile, block.Position, block.Block);
                }
            }
        }
        public void DecompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFile = File.OpenRead(inputFilePath);
            _outputFile = File.OpenWrite(outputFilePath);
    
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < _threadsCount; i++) threads.Add(new Thread(ReadBlocks));
            foreach (var th in threads) th.Start();
            foreach (var th in threads) th.Join();

            _inputFile.Dispose();            
        }
    }
}