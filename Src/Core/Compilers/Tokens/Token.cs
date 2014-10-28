using Ripple.Compilers.LexicalAnalysis;

namespace Ripple.Compilers.Tokens
{
    public abstract class Token
    {
        protected readonly CharPosition position;

        public CharPosition Position { get { return position; } }

        /// <summary>
        /// このトークンのソースコード中での表記を取得します。
        /// </summary>
        public abstract string Original { get; }

        public Token(CharPosition position)
        {
            this.position = position;
        }

        public override string ToString()
        {
            return position.ToString() + " " + base.ToString();
        }

        public bool IsSameKeywordType(KeywordToken.KeywordType type)
        {
            if (this is KeywordToken)
            {
                return ((KeywordToken)this).Type == type;
            }
            else
            {
                return false;
            }
        }

        public bool IsSameSymbolTokenType(SymbolToken.SymbolType type)
        {
            if (this is SymbolToken)
            {
                return ((SymbolToken)this).Type == type;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 無効なトークンです。
    /// </summary>
    public class InvalidToken : Token
    {
        private InvalidToken()
            : base(CharPosition.InvalidPosition)
        { }

        public override string Original
        {
            get { return "@Invalid"; }
        }

        public static readonly InvalidToken Instance = new InvalidToken();
    }

    public class EOFToken : Token
    {
        private EOFToken()
            : base(CharPosition.EOFPosition)
        { }

        public override string Original
        {
            get { return "@EOF"; }
        }

        public static readonly EOFToken Instance = new EOFToken();
    }

    ////////////////////////////////////////////////

    public class SymbolToken : Token
    {
        public enum SymbolType
        {
            OpeningBracket, ClosingBracket,
            OpeningBrace, ClosingBrace,
            OpeningSquare, ClosingSquare,
            Comma, Semicolon, Dot,

            Plus, Minus,
            PlusPlus, MinusMinus,
            Mult, Div,
            GreaterThan, GreaterOrEquals, LessThan, LessOrEquals,
            Equals, NotEquals,
            Question, Colon, Exclamation,
            AddAssign, SubAssign, MultAssign, DivAssign,
            AddStageAssign, SubStageAssign, MultStageAssign, DivStageAssign,
        }

        private readonly SymbolType type;
        public SymbolType Type { get { return type; } }

        public SymbolToken(SymbolType type, CharPosition position)
            : base(position)
        {
            this.type = type;
        }

        public override string Original
        {
            get { return GetOriginalText(type); }
        }

        public override string ToString()
        {
            return base.ToString() + " " + type.ToString() + " \"" + Original + "\"";
        }

        public static string GetOriginalText(SymbolType type)
        {
            return Lexer.GetSymbolOriginalToken(type);
        }
    }

    public class KeywordToken : Token
    {
        public enum KeywordType
        {
            Func, Struct, Operation, Stage, Param, Init, Holds,
            While, If, Else, Each, For, Parallel, Return, Do, At, In, To, Break, Continue,
            Var, As, Bool, SByte, UByte, Int, Long, Float, Nothing,
            True, False, Now, Next,
            Idiv, Mod, Or, And, Not,
        }

        private readonly KeywordType type;
        public KeywordType Type { get { return type; } }

        public KeywordToken(KeywordType type, CharPosition position)
            : base(position)
        {
            this.type = type;
        }

        public override string Original
        {
            get { return GetOriginalText(type); }
        }

        public override string ToString()
        {
            return base.ToString() + " " + type.ToString();
        }

        public static string GetOriginalText(KeywordType type)
        {
            return type.ToString().ToLower();
        }
    }

    public class IdentifierToken : Token
    {
        private readonly string name;
        public string Name { get { return name; } }

        public IdentifierToken(string name, CharPosition position)
            : base(position)
        {
            this.name = name;
        }

        public override string Original
        {
            get { return name; }
        }

        public override string ToString()
        {
            return base.ToString() + " " + name;
        }
    }

    public class IntegerLiteralToken : Token
    {
        private readonly long value;
        public long Value { get { return value; } }

        public IntegerLiteralToken(long value, CharPosition position)
            : base(position)
        {
            this.value = value;
        }

        public override string Original
        {
            get { return value.ToString(); }
        }

        public override string ToString()
        {
            return base.ToString() + " " + value.ToString();
        }
    }

    public class FloatLiteralToken : Token
    {
        private readonly double value;
        public double Value { get { return value; } }

        public FloatLiteralToken(double value, CharPosition position)
            : base(position)
        {
            this.value = value;
        }

        public override string Original
        {
            get { return value.ToString(); }
        }

        public override string ToString()
        {
            return base.ToString() + " " + value.ToString();
        }
    }
}
