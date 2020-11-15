using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.BlockServices
{
    public interface IBlockDecompressor
    { /// <summary>
      /// Расжимает блок
      /// </summary>
      /// <param name="block">Блок для расжатия</param>
      /// <returns>Расжатый блок</returns>
      byte[] DecompressBlock(byte[] block);

    }
}
