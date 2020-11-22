using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace FileArchiver.BlockServices
{ 

    /// <summary>
    /// Интерфейс сжатия блока
    /// </summary>
    public interface IBlockCompressor
    {
        /// <summary>
        /// Сжимает блок
        /// </summary>
        /// <param name="block">Блок для сжатия</param>
        /// <returns>Сжатый блок</returns>
        byte[] CompressBlock(byte[] block);        
    }
   
}
