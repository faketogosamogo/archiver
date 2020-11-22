using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.BlockServices.Exceptions
{
    class DecompressBlockException : Exception
    {
        public DecompressBlockException(string mes, Exception innerException) : base(mes, innerException) { }
    }
}
