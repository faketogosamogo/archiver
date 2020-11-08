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
    /// </summary>
    public class MultithreadFileDecompressor : IFileDecompressor
    {
        private IBlockDecompressor _blockDecompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private static FileStream _inputFile;

        //количество одновременно запускаемых потоков
        private static int _threadsCount = 15;
        private static int _blockLen = 0;
        private static long _currentWriteIndex = 0;
        private static object _currentWriteIndexLocker = new object();
        private static object _readLocker = new object();
        private static bool _isFileClosed;
        private string _outputFilePath;

        private static byte[] _lastBlock;
        public MultithreadFileDecompressor(IBlockDecompressor blockDecompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader)
        {
            _blockDecompressor = blockDecompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
        }
        private void ReadBlocks()
        {
            while (!_isFileClosed)
            {
                BlockingCollection<BlockWithPosition> blocks = new BlockingCollection<BlockWithPosition>();//Чтобы количество занимаемой памяти приложения не привышало норму
                for (int i = 0; i < _threadsCount; i++)
                {
                  //  lock (_readLocker)
                   // {

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

               // }
                blocks.CompleteAdding();
                DecompressAndWriteBlocks(blocks);
            }
        }
        private void DecompressAndWriteBlocks(BlockingCollection<BlockWithPosition> blocks)
        {
            foreach (var block in blocks)
            {              
                block.Block = _blockDecompressor.DecompressBlock(block.Block);
                if (block.Block.Length < _blockLen)
                {
                    _lastBlock = block.Block;
                }
                else
                {
                    long pos = 0;
                    lock (_currentWriteIndexLocker)
                    {
                        pos = _currentWriteIndex * _blockLen;
                        _currentWriteIndex++;
                    }
                    MD5 md = MD5.Create();

                    Console.WriteLine(Convert.ToBase64String(md.ComputeHash(block.Block)) + " " + pos);

                    using var outputFile = File.OpenWrite(_outputFilePath);
                    _blockWriter.WriteBlock(outputFile, block.Position, block.Block);
                }
            }
        }
        public void DecompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFile = File.OpenRead(inputFilePath);
            _outputFilePath = outputFilePath;
            var blockLenBuf = new byte[4];
            _inputFile.Read(blockLenBuf);

            _blockLen = BitConverter.ToInt32(blockLenBuf);
            Console.WriteLine(_blockLen);
            List<Thread> threads = new List<Thread>();
            ReadBlocks();
        
            _inputFile.Dispose();

            if (_lastBlock != null)
            {
                using (var outputFile = File.OpenWrite(outputFilePath))
                {

                    outputFile.Position = outputFile.Length;
                    MD5 md = MD5.Create();

                    Console.WriteLine(Convert.ToBase64String(md.ComputeHash(_lastBlock)) + " " + outputFile.Position);

                    Console.WriteLine(_lastBlock.Length+ " " +  outputFile.Position);
                    outputFile.Write(_lastBlock);
                }
            }
        }
    }
}