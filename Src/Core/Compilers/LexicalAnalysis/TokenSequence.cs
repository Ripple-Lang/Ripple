using System.Collections.Generic;
using Ripple.Compilers.Tokens;

namespace Ripple.Compilers.LexicalAnalysis
{
    public class TokenSequence
    {
        private readonly Token[] tokens;
        public int Position { get; internal set; }

        public bool EOF
        {
            get { return Position >= tokens.Length; }
        }

        public TokenSequence(Token[] tokens)
        {
            this.tokens = tokens;
        }

        public Token Peek()
        {
            if (Position >= tokens.Length)
            {
                return EOFToken.Instance;
            }

            return tokens[Position];
        }

        public Token Read()
        {
            if (Position >= tokens.Length)
            {
                return EOFToken.Instance;
            }

            return tokens[Position++];
        }

        public Token LookAhead(int count)
        {
            if (Position + count < 0 || Position + count >= tokens.Length)
            {
                return EOFToken.Instance;
            }

            return tokens[Position + count];
        }

        public void Reset()
        {
            Position = 0;
        }

        public IEnumerator<Token> GetEnumerator()
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                yield return tokens[i];
            }
        }
    }

}
