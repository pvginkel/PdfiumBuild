using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PdfiumBuild
{
    internal class Password
    {
        public string Value { get; }

        public Password(string value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
