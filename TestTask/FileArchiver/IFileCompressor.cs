using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver
{
    /// <summary>
    /// Интерфейс сжатия файла
    /// </summary>
    public interface IFileCompressor
    {
        /// <summary>
        /// Метод сжатия файла
        /// </summary>
        /// <param name="filePath">Путь к сжимаемому файлу</param>
        /// <returns>Путь к сжатому файлу</returns>
        public string CompressFile(string filePath);
    }
}
