using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Linq;

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
        public string Id { get; set; }
        public byte[] Block { get; set; }
        public long Position { get; set; }
        public bool IsCompress { get; set; }
        public BlockWithPosition(byte[] block, long position, bool isCompress)
        {
            Id = Guid.NewGuid().ToString();
            Block = block;
            Position = position;
            IsCompress = isCompress;
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
        private static int _blockLen = (1024*1024)*10;
        //количество одновременно запускаемых потоков
        private static int _threadsCount = 15;

        private static object _currentReadIndexLocker = new object();
        private static object _writeLocker = new object();
        private static object _readLocker = new object();
        private static object _compressLocker = new object();
        private static string _inputFilePath;
        private static string _outputFilePath;
        private static long _writePosition = 0;

        private BlockingCollection<BlockWithPosition> _blocks;
        public MultithreadStreamCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader)
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
            _blocks = new BlockingCollection<BlockWithPosition>();
        }
        private void oneThreadBlockOperations()
        {
            
         
        }      
        private void ReadAndCompress()
        {
            long pos = 0; 
            using var inputFile = File.OpenRead(_inputFilePath);

            lock (_currentReadIndexLocker)
            {
                pos = _currentReadIndex * _blockLen;
                _currentReadIndex++;
                if (pos >= inputFile.Length) return;
                Console.WriteLine(pos);
            }
           
            var block = _blockReader.ReadBlock(inputFile, pos, _blockLen);
            if (block.Length == 0) return;
            block = _blockCompressor.CompressBlock(block);
            _blocks.Add(new BlockWithPosition(block, pos, true));            
        }

        private void Write()
        {
            foreach (var block in _blocks)
            {
                var blockWithPosLen = new byte[8 + 4 + block.Block.Length];
                BitConverter.GetBytes(block.Position).CopyTo(blockWithPosLen, 0);
                BitConverter.GetBytes(block.Block.Length).CopyTo(blockWithPosLen, 8);
                block.Block.CopyTo(blockWithPosLen, 12);
                lock (_writeLocker)
                {
                    _blockWriter.WriteBlock(_outputFile, _outputFile.Position, blockWithPosLen);
                }
            }
        }
       
        public void CompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFilePath = inputFilePath;
            _outputFilePath = outputFilePath;
           // _inputFile = File.OpenRead(inputFilePath);
            _outputFile = File.OpenWrite(outputFilePath);
           // oneThreadBlockOperations();
            List<Thread> threads = new List<Thread>();
            List<Thread> readThreads = new List<Thread>();

            for (int i = 0; i < _threadsCount; i++) threads.Add(new Thread(Write));
            for (int i = 0; i < _threadsCount; i++) readThreads.Add(new Thread(ReadAndCompress));

            foreach (var readTh in readThreads) readTh.Start();
            foreach (var readTh in readThreads) readTh.Join();
            _blocks.CompleteAdding();

            Write();
           // foreach (var th in threads) th.Start();

           // foreach (var th in threads) th.Join();
           
           // _inputFile.Dispose();       
            _outputFile.Dispose();       
        }
    }
}
