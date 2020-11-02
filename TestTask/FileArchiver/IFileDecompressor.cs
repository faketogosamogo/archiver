using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver
{
    /// <summary>
    /// Интерфейс разжатия файла
    /// </summary>
    public interface IFileDecompressor
    {
        /// <summary>
        /// Метод разжатия файла
        /// </summary>
        /// <param name="filePath">Путь к файлу для расжатия</param>
        /// <returns>Расжатый файл</returns>
        public string DecompressFile(string filePath);
    }
}
