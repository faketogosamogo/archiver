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
        /// <param name="inputFilePath">Путь к сжимаемому файлу</param>
        /// <param name="outputFilePath">Путь к сжатому файлу</param>
        /// <returns>Успешно ли сжатие</returns>
        public bool CompressFile(string inputFilePath, string outputFilePath);
    }
}
