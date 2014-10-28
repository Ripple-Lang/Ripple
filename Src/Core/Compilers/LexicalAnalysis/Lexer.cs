using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ripple.Compilers.ErrorsAndWarnings;
using Ripple.Compilers.Tokens;

namespace Ripple.Compilers.LexicalAnalysis
{
    public sealed partial class Lexer
    {
        private readonly StringScanner scanner;
        private readonly ErrorsAndWarningsContainer errorsAndWarnings;

        public Lexer(string input, ErrorsAndWarningsContainer errorsAndWarnings)
        {
            scanner = new StringScanner(input);
            this.errorsAndWarnings = errorsAndWarnings;
        }

        public TokenSequence Lex()
        {
            List<Token> tokensList = new List<Token>();

            for (; ; )
            {
                SkipWhiteSpacesAndComments();
                if (scanner.EOF)
                    break;

                char peek = (char)scanner.Peek();
                var position = scanner.CurrentPosition;

                Token token;

                if (IsIdentifierOrKeywordStart(peek))
                {
                    token = LexIdentifierOrKeyword(position);
                }
                else if (IsNumericLiteralStart(peek))
                {
                    token = LexNumericLiteral(position);
                }
                else
                {
                    token = LexSymbolAndOperatorToken(position);
                }

                Debug.Assert(token != null);
                tokensList.Add(token);
            }

            return new TokenSequence(tokensList.ToArray());
        }

        private void SkipWhiteSpacesAndComments()
        {
            for (; ; )
            {
                scanner.SkipAwhile(IsWhiteSpace);

                if (scanner.PeekString(2) == "//")
                {
                    scanner.ReadString(2);
                    scanner.SkipAwhile(c => !IsNewLine(c));
                }
                else
                {
                    break;
                }
            }
        }

        private Token LexIdentifierOrKeyword(CharPosition position)
        {
            string text = scanner.ReadAwhile(IsIdentifierOrKeyword);

            if (KeywordTypes.ContainsKey(text))
            {
                return new KeywordToken(KeywordTypes[text], position);
            }
            else
            {
                return new IdentifierToken(text, position);
            }
        }

        private Token LexNumericLiteral(CharPosition position)
        {
            string text = scanner.ReadAwhile(IsNumericLiteral);

            if (text.Contains('.'))
            {
                return new FloatLiteralToken(double.Parse(text), position);
            }
            else
            {
                return new IntegerLiteralToken(long.Parse(text), position);
            }
        }

        private Token LexSymbolAndOperatorToken(CharPosition position)
        {
            Debug.Assert(!scanner.EOF);

            Token token;

            if (ThreeLenSymbols.ContainsKey(scanner.PeekString(3)))
            {
                token = new SymbolToken(ThreeLenSymbols[scanner.ReadString(3)], position);
            }
            else if (TwoLenSymbols.ContainsKey(scanner.PeekString(2)))
            {
                token = new SymbolToken(TwoLenSymbols[scanner.ReadString(2)], position);
            }
            else if (OneLenSymbols.ContainsKey((char)scanner.Peek()))
            {
                token = new SymbolToken(OneLenSymbols[(char)scanner.Read()], position);
            }
            else
            {
                errorsAndWarnings.AddError(new InvalidSymbolError(position, ((char)scanner.Read()).ToString()));
                return InvalidToken.Instance;
            }

            return token;
        }

        private static bool IsWhiteSpace(char c)
        {
            return char.IsWhiteSpace(c);
        }

        private static bool IsNewLine(char c)
        {
            return c == '\r' || c == '\n';
        }

        private static bool IsIdentifierOrKeywordStart(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierOrKeyword(char c)
        {
            return IsIdentifierOrKeywordStart(c) || char.IsDigit(c);
        }

        private static bool IsNumericLiteralStart(char c)
        {
            return char.IsDigit(c);
        }

        private static bool IsNumericLiteral(char c)
        {
            return IsNumericLiteralStart(c) || c == '.';
        }
    }
}
