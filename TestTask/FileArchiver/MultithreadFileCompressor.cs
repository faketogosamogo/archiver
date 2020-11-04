using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

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
    public class MultithreadFileCompressor : IFileCompressor
    {
        private IBlockCompressor _blockCompressor;
        private IBlockStreamReader _blockReader;
        private IBlockStreamWriter _blockWriter;

        private static FileStream _outputFile;

        private string _inputFilePath;

        //используется для поблочого чтения из файла
        private static int _currentReadIndex = 0;
        //длина обрабатываемого блока
        private static int _blockLen = (1024 * 1024) * 10;
        //количество одновременно запускаемых потоков
        private static int _threadsCount = 5;

        private static object _currentIndexLocker = new object();
        private static object _writeLocker = new object();

        
        public MultithreadFileCompressor(IBlockCompressor blockCompressor, IBlockStreamWriter blockWriter, IBlockStreamReader blockReader)
        {
            _blockCompressor = blockCompressor;
            _blockReader = blockReader;
            _blockWriter = blockWriter;
        }
        private void oneThreadBlockOperations()
        {
            while (true)//Выбрал цикл, чтобы каждый раз не создавать поток
            {
                int startPos = 0;
                lock (_currentIndexLocker)
                {
                    startPos = _blockLen * _currentReadIndex;
                    _currentReadIndex++;
                }
                using var file = File.OpenRead(_inputFilePath);
                var block = _blockReader.ReadBlock(file, startPos, _blockLen);
                if (block.Length == 0)
                {                    
                    return;
                }

                block = _blockCompressor.CompressBlock(block);

                var blockSize = BitConverter.GetBytes(block.Length);
                var blockWithSize = new byte[blockSize.Length + block.Length];
                blockSize.CopyTo(blockWithSize, 0);
                block.CopyTo(blockWithSize, blockSize.Length);//Записываем длину сжатого блока для дальнейшего расжатия              

                lock (_writeLocker)
                {
                    _blockWriter.WriteBlock(_outputFile, _outputFile.Position, blockWithSize);
                }
            }
        }
        public void CompressFile(string inputFilePath, string outputFilePath)
        {
            _inputFilePath = inputFilePath;
            _outputFile = File.OpenWrite(outputFilePath);

            _blockWriter.WriteBlock(_outputFile, _outputFile.Position, BitConverter.GetBytes(_blockLen));//Записываем длину обрабатываемых блоков для дальнейшего расжатия 

            List<Thread> threads = new List<Thread>();
             for (int i = 0; i < _threadsCount; i++) threads.Add(new Thread(oneThreadBlockOperations));
            foreach (var th in threads) th.Start();
            foreach (var th in threads) th.Join();
          
            _outputFile.Dispose();       
        }
    }
}
