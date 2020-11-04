using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FileArchiver
{
    public interface IBlockStreamReader
    {  /// <summary>
       /// Считывает блок из файла
       /// </summary>
       /// <param name="streamForRead">Поток для чтения блока</param>
       /// <param name="start">Позиция начала чтения</param>
       /// <param name="blockLen">Длина считываемого блока</param>
       /// <returns>Считанный блок(может быть меньше ожидаемого, если длина считываемого отрезка меньше чем blockLen)</returns>
        byte[] ReadBlock(Stream streamForRead, int start, int blockLen);
    }

    public class BlockFileStreamReader : IBlockStreamReader
    {
        public byte[] ReadBlock(Stream streamForRead, int start, int blockLen)
        {
            byte[] block = new byte[blockLen];
            streamForRead.Position = start;

            int countOfReadedBytes = streamForRead.Read(block);
            Console.WriteLine($"read block, start index: {start}, lenght of block: {blockLen}, count of readed bytes: {countOfReadedBytes}");
            Array.Resize(ref block, countOfReadedBytes);
            return block;
        }
    }
}
