using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.BlockServices.Exceptions
{
    class CompressBlockException : Exception
    {
        public CompressBlockException(string mes, Exception innerException) : base(mes, innerException) { }
    }
}
