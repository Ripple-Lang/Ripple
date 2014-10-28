using Ripple.Compilers.LexicalAnalysis;

namespace Ripple.Compilers.ErrorsAndWarnings
{
    /// <summary>
    /// エラーと警告を表す基底クラスです。
    /// </summary>
    public abstract class ErrorAndWarning
    {
        private readonly CharPosition position;
        private readonly string message;

        public CharPosition Position { get { return position; } }
        public string Message { get { return message; } }
        public string Detail
        {
            get
            {
                return (IsError ? "エラー" : "警告") + " " + position.ToJapaneseString() + " : " + message;
            }
        }
        public abstract bool IsError { get; }

        public ErrorAndWarning(CharPosition position, string message)
        {
            this.position = position;
            this.message = message;
        }

        public override string ToString()
        {
            return Detail;
        }
    }

    /// <summary>
    /// エラーを表す基底クラスです。
    /// </summary>
    public class Error : ErrorAndWarning
    {
        public Error(CharPosition position, string message)
            : base(position, message)
        { }

        public override bool IsError
        {
            get { return true; }
        }
    }

    /// <summary>
    /// 警告を表す基底クラスです。
    /// </summary>
    public class Warning : ErrorAndWarning
    {
        public Warning(CharPosition position, string message)
            : base(position, message)
        { }

        public override bool IsError
        {
            get { return false; }
        }
    }
}
