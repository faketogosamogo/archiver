using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileArchiver
{
    /// <summary>
    /// Интерфейс записи блока в поток
    /// </summary>
    public interface IBlockStreamWriter
    {
        /// <summary>
        /// Записывает блок
        /// Для записи всех блоков используется один поток, в связи с незнанием конечного размера сжимаемого блока
        /// (Резервировать место, а после убирать пробелы посчитал нецелесообразным)
        /// </summary>
        /// <param name="streamToWrite">Поток для записи</param>
        /// /// <param name="startPos">Позиция начала старта записи</param>
        /// <param name="block">Записываемый блок</param>
        void WriteBlock(Stream streamToWrite, long startPos, byte[] block);
    }
    /// <summary>
    /// Реализация IBlockStreamWriter
    /// </summary>
    public class BlockStreamWriter : IBlockStreamWriter
    {
        public void WriteBlock(Stream streamToWrite, long startPos, byte[] block)
        {
            //Console.WriteLine($"writer: pos: {streamToWrite.Position}, startPos: {startPos}, blocksize: {block.Length}");
            streamToWrite.Write(block);
        }
    }
}
