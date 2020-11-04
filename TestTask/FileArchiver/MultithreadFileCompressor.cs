using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileCompressor
    /// 
    /// </summary>
    public class MultithreadFileCompressor : IFileCompressor
    {
        private IBlockCompressor _blockCompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private static FileStream _outputFile;

        private string _inputFilePath;

        private static int _currentIndex = 0;
        private static object _currentIndexLocker = new object();

        private static int _blockLen = (1024 * 1024)*10;
        
        public MultithreadFileCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader)
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
        }

        private void oneBlockOperation()
        {
            while (true)
            {
                int startPos = 0;
                lock (_currentIndexLocker)
                {
                    startPos = _blockLen * _currentIndex;
                    _currentIndex++;
                }
                using var file = File.OpenRead(_inputFilePath);
                var block = _blockReader.ReadBlock(file, startPos, _blockLen);
                if (block.Length == 0)
                {                    
                    return;
                }
                block = _blockCompressor.CompressBlock(block);

                var blockSize = BitConverter.GetBytes(block.Length);//получаем длину блока для дальнейшего расжатия и склеиваем с блоком
                var blockWithSize = new byte[blockSize.Length + block.Length];
                blockSize.CopyTo(blockWithSize, 0);
                block.CopyTo(blockWithSize, blockSize.Length);

                _blockWriter.WriteBlock(_outputFile, blockWithSize);
                
            }
        }
        public void CompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFilePath = inputFilePath;
            _outputFile = File.OpenWrite(outputFilePath);

            List<Thread> threads = new List<Thread>();
             for (int i = 0; i < 5; i++) threads.Add(new Thread(oneBlockOperation));
            foreach (var th in threads) th.Start();
            foreach (var th in threads) th.Join();
          
            _outputFile.Dispose();       
        }
    }
}
