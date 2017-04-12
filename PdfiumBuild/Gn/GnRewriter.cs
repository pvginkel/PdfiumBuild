using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfiumBuild.Gn
{
    internal abstract class GnRewriter
    {
        private readonly GnLexer _lexer;

        public GnRewriter(GnLexer lexer)
        {
            _lexer = lexer;
        }

        protected GnToken GetToken()
        {
            return _lexer.Next();
        }

        protected abstract IEnumerable<GnToken> GetTokens();

        public string Rewrite()
        {
            var sb = new StringBuilder();

            foreach (var token in GetTokens())
            {
                sb.Append(token.LeadingWhitespace);
                sb.Append(token.Text ?? GetTokenText(token.Type));
                sb.Append(token.TrailingWhitespace);
            }

            return sb.ToString();
        }

        private string GetTokenText(GnTokenType type)
        {
            switch (type)
            {
                case GnTokenType.AmpersandAmpersand:
                    return "&&";
                case GnTokenType.BarBar:
                    return "||";
                case GnTokenType.BraceClose:
                    return "}";
                case GnTokenType.BraceOpen:
                    return "{";
                case GnTokenType.BracketClose:
                    return "]";
                case GnTokenType.BracketOpen:
                    return "[";
                case GnTokenType.Comma:
                    return ",";
                case GnTokenType.Dot:
                    return ".";
                case GnTokenType.Equals:
                    return "=";
                case GnTokenType.EqualsEquals:
                    return "==";
                case GnTokenType.GreaterThan:
                    return ">";
                case GnTokenType.GreaterThanOrEquals:
                    return ">=";
                case GnTokenType.LessThan:
                    return "<";
                case GnTokenType.LessThanOrEquals:
                    return "<=";
                case GnTokenType.Minus:
                    return "-";
                case GnTokenType.MinusEquals:
                    return "-=";
                case GnTokenType.Not:
                    return "!";
                case GnTokenType.NotEquals:
                    return "!=";
                case GnTokenType.ParenClose:
                    return ")";
                case GnTokenType.ParenOpen:
                    return "(";
                case GnTokenType.Plus:
                    return "+";
                case GnTokenType.PlusEquals:
                    return "+=";
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
