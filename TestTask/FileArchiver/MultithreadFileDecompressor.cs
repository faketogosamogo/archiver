using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

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
        private string _outputFilePath;

        //используется для поблочой записи в файл
        private static int _currentWriteIndex = 0;
        //длина обрабатываемого блока
        private static int _blockLen = 0;
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
                int nextBlockLen = getNextBlockLen(_inputFile);
                if (nextBlockLen == 0) return;
                byte[] block = new byte[0];
                lock (_readLocker)
                {
                    block = _blockReader.ReadBlock(_inputFile, _inputFile.Position, nextBlockLen);
                }
                if (block.Length == 0) return;
                block = _blockDecompressor.DecompressBlock(block);
                using var outputFile = File.OpenWrite(_outputFilePath);

                int startPos = 0;
                lock (_currentIndexLocker)
                {
                    startPos = _blockLen * _currentWriteIndex;
                    _currentWriteIndex++;
                }

                _blockWriter.WriteBlock(outputFile, startPos, block);
            }
        }

        public void DecompressFile(string inputFilePath, string outputFilePath)
        {
            _outputFilePath = outputFilePath;
            _inputFile = File.OpenRead(inputFilePath);

            byte[] sizeBuffer = new byte[4];
            _inputFile.Read(sizeBuffer);
            _blockLen = BitConverter.ToInt32(sizeBuffer);

            List<Thread> threads = new List<Thread>();
            for (int i = 0; i < _threadsCount; i++) threads.Add(new Thread(oneThreadBlockOperations));
            foreach (var th in threads) th.Start();
            foreach (var th in threads) th.Join();

            _inputFile.Dispose();
        }
    }
}
