using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfiumBuild.Gn
{
    internal class GnToken
    {
        public GnTokenType Type { get; }
        public string Text { get; }
        public string LeadingWhitespace { get; }
        public string TrailingWhitespace { get; }

        public GnToken(GnTokenType type)
            : this(type, null)
        {
        }

        public GnToken(GnTokenType type, string text)
            : this(type, text, null, null)
        {
        }

        public GnToken(GnTokenType type, string text, string leadingWhitespace, string trailingWhitespace)
        {
            Type = type;
            Text = text;
            LeadingWhitespace = leadingWhitespace;
            TrailingWhitespace = trailingWhitespace;
        }
    }
}
