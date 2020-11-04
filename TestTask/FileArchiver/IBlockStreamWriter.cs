using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileArchiver
{
    public interface IBlockStreamWriter
    {
        /// <summary>
        /// Записывает блок
        /// Для записи всех блоков используется один поток, в связи с незнанием конечного размера сжимаемого блока
        /// (Резервировать место, а после убирать пробелы посчитал нецелесообразным)
        /// </summary>
        /// <param name="streamToWrite">Поток для записи</param>
        /// <param name="block">Записываемый блок</param>
        void WriteBlock(Stream streamToWrite, byte[] block);
    }

    public class BlockFileStreamWriter : IBlockStreamWriter
    {
        public void WriteBlock(Stream streamToWrite, byte[] block)
        {
            Console.WriteLine($"write block: block lenght: {block.Length}");
            streamToWrite.Write(block);
        }
    }
}
