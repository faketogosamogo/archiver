using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.BlockServices.Exceptions
{
    class ReadBlockException : Exception
    {
        public ReadBlockException(string mes, Exception innerException) : base(mes, innerException) { }
    }
}
