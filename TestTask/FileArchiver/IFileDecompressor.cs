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
        /// Метод сжатия файла
        /// </summary>
        /// <param name="inputFilePath">Путь к сжатому файлу</param>
        /// <param name="outputFilePath">Путь к расжатому файлу</param>
        /// <returns>Успешно ли расжатие</returns>
        public bool DecompressFile(string inputFilePath, string outputFilePath);
    }
}
