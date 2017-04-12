using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PdfiumBuild.Gn
{
    internal class GnLexer
    {
        private readonly string _source;
        private int _offset;
        private GnToken _token;

        public GnLexer(string source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            _source = source;
        }

        public GnToken Peek()
        {
            if (_offset >= _source.Length)
                return null;
            if (_token == null)
                _token = ParseToken();
            return _token;
        }

        public GnToken Next()
        {
            if (_token == null)
                Peek();
            var result = _token;
            _token = null;
            return result;
        }

        private GnToken ParseToken()
        {
            string leadingWhitespace = SkipWs();
            int offset = _offset;
            var type = Parse();
            if (!type.HasValue)
                return new GnToken(GnTokenType.WhiteSpace, null, leadingWhitespace, null);
            string token = _source.Substring(offset, _offset - offset);
            string trailingWhitespace = SkipWs();

            return new GnToken(type.Value, token, leadingWhitespace, trailingWhitespace);
        }

        private GnTokenType? Parse()
        {
            if (_offset >= _source.Length)
                return null;

            char c = NextChar();

            switch (c)
            {
                case '+':
                    if (NextChar('='))
                        return GnTokenType.PlusEquals;
                    return GnTokenType.Plus;
                case '-':
                    if (NextChar('='))
                        return GnTokenType.MinusEquals;
                    return GnTokenType.Minus;
                case '!':
                    if (NextChar('='))
                        return GnTokenType.NotEquals;
                    return GnTokenType.Not;
                case '=':
                    if (NextChar('='))
                        return GnTokenType.EqualsEquals;
                    return GnTokenType.Equals;
                case '<':
                    if (NextChar('='))
                        return GnTokenType.LessThanOrEquals;
                    return GnTokenType.LessThan;
                case '>':
                    if (NextChar('='))
                        return GnTokenType.GreaterThanOrEquals;
                    return GnTokenType.GreaterThan;
                case '&':
                    if (!NextChar('&'))
                        throw new GnParseException("Unexpected token");
                    return GnTokenType.AmpersandAmpersand;
                case '|':
                    if (!NextChar('|'))
                        throw new GnParseException("Unexpected token");
                    return GnTokenType.BarBar;
                case '(':
                    return GnTokenType.ParenOpen;
                case ')':
                    return GnTokenType.ParenClose;
                case '{':
                    return GnTokenType.BraceOpen;
                case '}':
                    return GnTokenType.BraceClose;
                case '[':
                    return GnTokenType.BracketOpen;
                case ']':
                    return GnTokenType.BracketClose;
                case '.':
                    return GnTokenType.Dot;
                case ',':
                    return GnTokenType.Comma;
                case '"':
                    return ParseString();
                default:
                    if (c == '0' && _offset < _source.Length && (_source[_offset] == 'x' || _source[_offset] == 'X'))
                        return ParseHex();
                    if (c >= '0' && c <= '9')
                        return ParseInteger();
                    if (IsIdentifierStart(c))
                        return ParseIdentifier();
                    throw new GnParseException("Unexpected token");
            }
        }

        private GnTokenType ParseHex()
        {
            _offset++;

            while (_offset < _source.Length)
            {
                char c = _source[_offset];
                if (!IsHexDigit(c))
                    break;
                _offset++;
            }

            return GnTokenType.HexInteger;

        }

        private GnTokenType ParseIdentifier()
        {
            while (_offset < _source.Length)
            {
                char c = _source[_offset];
                if (!IsIdentifier(c))
                    break;
                _offset++;
            }

            return GnTokenType.Identifier;
        }

        private GnTokenType ParseString()
        {
            while (_offset < _source.Length)
            {
                switch (_source[_offset++])
                {
                    case '\\':
                        _offset++;
                        break;
                    case '"':
                        return GnTokenType.String;
                }
            }

            throw new GnParseException("Cannot parse string");
        }

        private GnTokenType ParseInteger()
        {
            while (_offset < _source.Length)
            {
                char c = _source[_offset];
                if (!IsDigit(c))
                    break;
                _offset++;
            }

            return GnTokenType.Integer;
        }

        private char NextChar()
        {
            return _source[_offset++];
        }

        private bool NextChar(char c)
        {
            if (_offset < _source.Length && _source[_offset] == c)
            {
                _offset++;
                return true;
            }

            return false;
        }

        private string SkipWs()
        {
            int start = _offset;

            while (true)
            {
                int offset = _offset;

                while (_offset < _source.Length && Char.IsWhiteSpace(_source[_offset]))
                    _offset++;

                if (_offset < _source.Length && _source[_offset] == '#')
                {
                    while (_offset < _source.Length && _source[_offset] != '\n')
                        _offset++;
                }

                if (offset == _offset)
                    break;
            }

            if (_offset != start)
                return _source.Substring(start, _offset - start);

            return null;
        }

        private bool IsIdentifier(char c)
        {
            return
                IsIdentifierStart(c) ||
                c >= '0' && c <= '9';
        }

        private bool IsIdentifierStart(char c)
        {
            return
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';
        }

        private bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private bool IsHexDigit(char c)
        {
            return
                IsDigit(c) ||
                (c >= 'a' && c <= 'f') ||
                (c >= 'A' && c <= 'F');
        }
    }
}
