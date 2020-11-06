using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FileArchiver
{
    /// <summary>
    /// Интерфейс чтения блока из потока
    /// </summary>
    public interface IBlockStreamReader
    {  /// <summary>
       /// Считывает блок из файла
       /// </summary>
       /// <param name="streamForRead">Поток для чтения блока</param>
       /// <param name="startPos">Позиция начала чтения</param>
       /// <param name="blockLen">Длина считываемого блока</param>
       /// <returns>Считанный блок(может быть меньше ожидаемого, если длина считываемого отрезка меньше чем blockLen)</returns>
        byte[] ReadBlock(Stream streamForRead, long startPos, int blockLen);
    }

    /// <summary>
    /// Реализация IBlockStreamReader
    /// </summary>
    public class BlockStreamReader : IBlockStreamReader
    {
        public byte[] ReadBlock(Stream streamForRead, long startPos, int blockLen)
        {
            if (streamForRead.Length <= startPos) return new byte[0];
            byte[] block = new byte[blockLen];
            streamForRead.Position = startPos;

            int countOfReadedBytes = streamForRead.Read(block);
            Array.Resize(ref block, countOfReadedBytes);

            MD5 md = MD5.Create();

           // Console.WriteLine(Convert.ToBase64String(md.ComputeHash(block)));
            //  Console.WriteLine($"reader: pos: {streamForRead.Position}, startpos: {startPos}, blockLen: {blockLen}, countOfReadedBytes: {countOfReadedBytes}");
            return block;
        }
    }
}
