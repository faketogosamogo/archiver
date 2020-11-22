using System;
using System.Collections.Generic;
using System.Text;

namespace FileArchiver.BlockServices.Exceptions
{
    class WriteBlockException : Exception
    {
        public WriteBlockException(string mes, Exception innerException) : base(mes, innerException) { }
    }
}
