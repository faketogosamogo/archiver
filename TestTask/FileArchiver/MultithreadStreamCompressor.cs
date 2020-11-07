using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Security.Cryptography;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileCompressor.
    /// В своей реализации работает поблочно с файлом.
    /// Считывает->Сжимает->Записывает блок.
    /// В данной реализации перечисленные выше операции производятся в одном потоке.
    /// Это было выбрано в связи с тем, что в конечном итоге при использовании нескольких потоков для ускорения сжатия, минимально возможное время сводится к записи в файл,
        ///т.к разбивать поблочно файл для записи нецелесообразно в связи с неизвестностью размера сжатых блоков
        ///(в случае если оставлять пробелы, а после их убирать склеивая файл. Возможно есть и другие способы, но их я не придумал).
    /// Если разбивать эти операции на разные потоки, то всё равно Считывание + Сжатие занимают больше времени, чем запись и это не даст ускорения(в моём представлении).
    /// </summary>
    
    class BlockWithPosition
    {
        public byte[] Block { get; set; }
        public long Position { get; set; }
        public BlockWithPosition(byte[] block, int position)
        {
            Block = block;
            Position = position;
        }
    }
    public class MultithreadStreamCompressor : IFileCompressor
    {
        private IBlockCompressor _blockCompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private static FileStream _outputFile;
        private static FileStream _inputFile;

        //используется для поблочого чтения из файла
        private static int _currentReadIndex = 0;
        //длина обрабатываемого блока
        private static int _blockLen = (1024 * 1024) * 20;
        //количество одновременно запускаемых потоков
        private static int _threadsCount = 10;


        private static object _currentIndexLocker = new object();
        private static object _writeLocker = new object();
        private static object _readLocker = new object();

        
        public MultithreadStreamCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader)
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
        }
        private void oneThreadBlockOperations()
        {

            while (true)//Выбрал цикл, чтобы каждый раз не создавать поток
            {
                var block = new BlockWithPosition(new byte[0], 0);
                
                lock (_currentIndexLocker)
                {
                    int pos = _currentReadIndex * _blockLen;
                    _currentReadIndex++;
                    if (pos > _inputFile.Length) return;
                    block.Position = pos;
                    block.Block = _blockReader.ReadBlock(_inputFile, pos, _blockLen);
              
                    if (block.Block.Length == 0)
                    {
                        return;
                    }
                    MD5 md = MD5.Create();
                    Console.WriteLine($"{block.Position} {block.Block.Length} md: {Convert.ToBase64String(md.ComputeHash(block.Block))}");

                }
                block.Block = _blockCompressor.CompressBlock(block.Block);
                var blockLen = BitConverter.GetBytes(block.Block.Length);
                var blockPos = BitConverter.GetBytes(block.Position);
                var blockToWrite = new byte[blockLen.Length + blockPos.Length + block.Block.Length];
                blockLen.CopyTo(blockToWrite, 0);
                blockPos.CopyTo(blockToWrite, blockLen.Length);
                block.Block.CopyTo(blockToWrite, blockLen.Length + blockPos.Length);
                lock (_writeLocker)
                {
                    long pos = _outputFile.Position;              
                    _blockWriter.WriteBlock(_outputFile, pos, blockToWrite);
                }
            }
        }
        public void CompressFile(string inputFilePath, string outputFilePath)
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
