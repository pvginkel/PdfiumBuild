using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using PdfiumBuild.Gn;

namespace PdfiumBuild
{
    internal class BuildScriptRewriter : GnRewriter
    {
        private readonly List<string> _contribs;

        public static void Rewrite(string fileName, List<string> contribs)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            if (contribs == null)
                throw new ArgumentNullException(nameof(contribs));

            string rewritten = new BuildScriptRewriter(new GnLexer(File.ReadAllText(fileName)), contribs).Rewrite();

            File.WriteAllText(fileName, rewritten);
        }

        private BuildScriptRewriter(GnLexer lexer, List<string> contribs)
            : base(lexer)
        {
            _contribs = contribs;
        }

        protected override IEnumerable<GnToken> GetTokens()
        {
            int braceNesting = 0;
            string currentConfig = null;
            bool inPdfiumLibrary = false;
            GnToken token;

            while ((token = GetToken()) != null)
            {
                switch (token.Type)
                {
                    case GnTokenType.BraceOpen:
                        yield return token;

                        braceNesting++;
                        break;

                    case GnTokenType.BraceClose:
                        yield return token;

                        braceNesting--;
                        if (braceNesting == 0)
                        {
                            currentConfig = null;
                            inPdfiumLibrary = false;
                        }
                        break;

                    case GnTokenType.Identifier:
                        if (braceNesting == 0 && token.Text == "config")
                        {
                            yield return token;
                            yield return GetToken(GnTokenType.ParenOpen);
                            yield return token = GetToken(GnTokenType.String);
                            currentConfig = ParseString(token.Text);
                            yield return GetToken(GnTokenType.ParenClose);
                        }
                        else if (currentConfig == "pdfium_common_config" && token.Text == "include_dirs")
                        {
                            yield return token;
                            yield return token = GetToken();

                            if (token.Type == GnTokenType.Equals)
                            {
                                foreach (var child in AddToList(new GnToken(GnTokenType.String, "\"v8/include\"")))
                                {
                                    yield return child;
                                }
                            }
                        }
                        else if (currentConfig == "pdfium_common_config" && token.Text == "defines")
                        {
                            yield return token;
                            yield return token = GetToken();

                            if (token.Type == GnTokenType.Equals)
                            {
                                foreach (var child in AddToList(new GnToken(GnTokenType.String, "\"FPDFSDK_EXPORTS\"")))
                                {
                                    yield return child;
                                }
                            }
                        }
                        else if (inPdfiumLibrary && token.Text == "sources" && _contribs.Count > 0)
                        {
                            yield return token;
                            yield return token = GetToken();

                            if (token.Type == GnTokenType.Equals)
                            {
                                var extraTokens = new List<GnToken>();

                                foreach (var contrib in _contribs)
                                {
                                    if (extraTokens.Count > 0)
                                        extraTokens.Add(new GnToken(GnTokenType.Comma));

                                    extraTokens.Add(new GnToken(
                                        GnTokenType.String,
                                        "\"" + contrib.Replace(Path.DirectorySeparatorChar, '/') + "\""
                                    ));
                                }

                                foreach (var child in AddToList(extraTokens))
                                {
                                    yield return child;
                                }
                            }
                        }
                        else if (braceNesting == 0 && token.Text == "static_library")
                        {
                            var tokens = new List<GnToken>();
                            tokens.Add(token);

                            tokens.Add(GetToken(GnTokenType.ParenOpen));
                            token = GetToken(GnTokenType.String);
                            tokens.Add(token);
                            inPdfiumLibrary = ParseString(token.Text) == "pdfium";
                            tokens.Add(GetToken(GnTokenType.ParenClose));

                            if (inPdfiumLibrary)
                                tokens[0] = new GnToken(GnTokenType.Identifier, "shared_library");

                            foreach (var child in tokens)
                            {
                                yield return child;
                            }
                        }
                        else
                        {
                            yield return token;
                        }
                        break;

                    default:
                        yield return token;
                        break;
                }
            }
        }

        private IEnumerable<GnToken> AddToList(GnToken token)
        {
            return AddToList(new[] { token });
        }

        private IEnumerable<GnToken> AddToList(IEnumerable<GnToken> tokens)
        {
            yield return GetToken(GnTokenType.BracketOpen);

            bool hadOne = false;
            bool lastComma = false;

            while (true)
            {
                var child = GetToken();
                if (child.Type == GnTokenType.BracketClose)
                {
                    if (hadOne && !lastComma)
                        yield return new GnToken(GnTokenType.Comma);

                    foreach (var token in tokens)
                    {
                        yield return token;
                    }

                    yield return child;
                    break;
                }

                hadOne = true;
                lastComma = child.Type == GnTokenType.Comma;
                yield return child;
            }
        }

        private string ParseString(string text)
        {
            return text.Substring(1, text.Length - 2);
        }

        private GnToken GetToken(GnTokenType type)
        {
            var token = GetToken();
            if (token == null || token.Type != type)
                throw new InvalidOperationException("Expected token " + type);
            return token;
        }
    }
}
