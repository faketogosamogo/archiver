using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using FileArchiver.Exceptions;

namespace FileArchiver
{
    /// <summary>
    /// Мультипоточная реализация IFileCompressor.
    /// В своей реализации работает поблочно с файлом.
    /// (Считывает и Сжимает) блоки -> Записывает блоки.
    ///Запись в файл происходит постепенно(не мультипоточно).
    ///Это было выбрано в связи с тем, что в конечном итоге при использовании нескольких потоков для ускорения сжатия, минимально возможное время сводится к записи в файл,
    ///т.к разбивать поблочно файл для записи нецелесообразно в связи с неизвестностью размера сжатых блоков
    ///Вначале записывается позиция блока в исходном файле, далее длина записанного блока, далее сам блок.

    ///С организацией нагрузки не справился, думал брать из ComputerInfo.AvailableVirtualMemory, и назначать размеру блока количество свободной оперативной памяти/(количество потоков ^ 3),
        ///(количество потоков ^ 3) т.ктакое количество у меня может в раз находиться в худшем раскладе(в моём представлении)в каждом потоке вызывается количество считываний равное потокам
    ///Но не нашёл его аналога, поэтому пока решил оставить так.
    class BlockWithPosition
    {
        public byte[] Block { get; set; }
        public long Position { get; set; }
        public BlockWithPosition(byte[] block, long position)
        {
            Block = block;
            Position = position;
        }
    }
    public class MultithreadFileCompressor : IFileCompressor
    {
        private IBlockCompressor _blockCompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private static FileStream _outputFile;

        //используется для поблочого чтения из файла
        private static long _currentReadIndex = 0;
        private static object _currentReadIndexLocker = new object();

        //длина обрабатываемого блока
        private static int _blockLen;
        //количество одновременно запускаемых потоков
        private static int _threadsCount;

        private static object _writeLocker = new object();
        
        private static string _inputFilePath;  

        private static bool _isFileClosed;

       
        public MultithreadFileCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader, int threadsCount=5, int blockLen = (1024*1024)*10)
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
            _blockLen = blockLen;
            _threadsCount = threadsCount;
        }     
        private void readAndCompressBlocks()
        {
            while (!_isFileClosed) {//Чтобы потоки не пересоздавались

                BlockingCollection<BlockWithPosition> blocks = new BlockingCollection<BlockWithPosition>();//Чтобы количество занимаемой памяти приложения не привышало норму
                for (int i = 0; i < _threadsCount; i++)
                {
                    long pos = 0;
                    using var inputFile = File.OpenRead(_inputFilePath);

                    lock (_currentReadIndexLocker)
                    {
                        pos = _currentReadIndex * _blockLen;
                        _currentReadIndex++;
                        if (pos >= inputFile.Length)
                        {
                            _isFileClosed = true;
                            continue;
                        }
                    }

                    byte[] block = null;
                    try
                    {
                        block = _blockReader.ReadBlock(inputFile, pos, _blockLen);
                        if (block.Length == 0) return;
                    }catch(Exception ex)
                    {
                        throw new ReadBlockException($"Ошибка чтения блока: {ex.Message}, {ex.StackTrace}", ex);
                    }
                    try
                    {
                        block =  _blockCompressor.CompressBlock(block);
                    }catch(Exception ex)
                    {
                        throw new CompressBlockException($"Ошибка сжатия блока", ex);
                    }
                    blocks.Add(new BlockWithPosition(block, pos));
                }

                blocks.CompleteAdding();
                writeBlocks(blocks);//После считывания сжатия порции блоков записываем их в файл
            }
        }
        private void writeBlocks(BlockingCollection<BlockWithPosition> blocks)
        {
           foreach (var block in blocks)
           {
                var blockWithPosLen = new byte[8 + 4 + block.Block.Length];
                BitConverter.GetBytes(block.Position).CopyTo(blockWithPosLen, 0);
                BitConverter.GetBytes(block.Block.Length).CopyTo(blockWithPosLen, 8);

                block.Block.CopyTo(blockWithPosLen, 12);
                lock (_writeLocker)
                {
                    try
                    {
                        _blockWriter.WriteBlock(_outputFile, _outputFile.Position, blockWithPosLen);
                    }catch(Exception ex)
                    {
                        throw new WriteBlockException($"Ошибка записи блока", ex);
                    }
                }
           }
        }
       
        public bool CompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFilePath = inputFilePath;

            try
            {
                _outputFile = File.OpenWrite(outputFilePath);
          
          
                List<Thread> threads = new List<Thread>();

                for (int i = 0; i < _threadsCount; i++) threads.Add(new Thread(readAndCompressBlocks));

                foreach (var th in threads) th.Start();
                foreach (var th in threads) th.Join();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}, {ex.StackTrace}"); 
                Console.WriteLine($"{ex.InnerException.Message}, {ex.InnerException.StackTrace}");

                if (File.Exists(outputFilePath)) File.Delete(outputFilePath);                
                return false;
            }
            finally
            {
                _outputFile.Dispose();
            }
            return true;
        }
    }
}
