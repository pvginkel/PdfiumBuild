using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfiumBuild.Gn
{
    internal enum GnTokenType
    {
        AmpersandAmpersand,
        BarBar,
        BraceClose,
        BraceOpen,
        BracketClose,
        BracketOpen,
        Comma,
        Dot,
        Equals,
        EqualsEquals,
        GreaterThan,
        GreaterThanOrEquals,
        HexInteger,
        Identifier,
        Integer,
        LessThan,
        LessThanOrEquals,
        Minus,
        MinusEquals,
        Not,
        NotEquals,
        ParenClose,
        ParenOpen,
        Plus,
        PlusEquals,
        String,
        WhiteSpace,
    }
}
