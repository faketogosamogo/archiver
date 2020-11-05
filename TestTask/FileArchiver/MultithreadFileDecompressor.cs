﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

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

        private static FileStream _outputFile;
        private static FileStream _inputFile;
        
        //количество одновременно запускаемых потоков
        private static int _threadsCount = 5;


        private static object _currentIndexLocker = new object();
        private static object _readLocker = new object();

        public MultithreadFileDecompressor(IBlockDecompressor blockDecompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader)
        {
            _blockDecompressor = blockDecompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
        }

        private static int getNextBlockLen(FileStream fileToRead)
        {
            byte[] sizeBuffer = new byte[4];
            fileToRead.Read(sizeBuffer);
            int blockLen = BitConverter.ToInt32(sizeBuffer);
            return blockLen;
        }

        private void oneThreadBlockOperations()
        {

            while (true)
            {
                lock (_readLocker)
                {
                    var block = new BlockWithPosition(new byte[0], 0);
               
                    int nextBlockLen = getNextBlockLen(_inputFile);
                    var b = new byte[8];
                    _inputFile.Read(b);

                    block.Position = BitConverter.ToInt64(b); 
                    if (nextBlockLen == 0) return;
                    block.Block = _blockReader.ReadBlock(_inputFile, _inputFile.Position, nextBlockLen);
                    if (block.Block.Length == 0) return;                

                    block.Block = _blockDecompressor.DecompressBlock(block.Block);

                    _blockWriter.WriteBlock(_outputFile, block.Position, block.Block);

               }
              
            }
        }

        public void DecompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFile = File.OpenRead(inputFilePath);
            _outputFile = File.OpenWrite(outputFilePath);
            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < _threadsCount; i++) threads.Add(new Thread(oneThreadBlockOperations));
            
            foreach (var th in threads) th.Start();
            foreach (var th in threads) th.Join();   
            
            _inputFile.Dispose();
            _outputFile.Dispose();
        }
    }
}
