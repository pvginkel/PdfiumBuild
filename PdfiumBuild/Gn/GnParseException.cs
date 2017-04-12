using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfiumBuild.Gn
{
    internal class GnParseException : Exception
    {
        public GnParseException()
        {
        }

        public GnParseException(string message)
            : base(message)
        {
        }

        public GnParseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
