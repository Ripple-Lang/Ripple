using System;
using System.Diagnostics;
using System.Text;

namespace Ripple.Compilers.LexicalAnalysis
{
    /// <summary>
    /// 複数の行を含む文字列中での文字の位置を表します。
    /// </summary>
    public class CharPosition : IComparable<CharPosition>
    {
        private readonly int lineNo;
        private readonly int charNoInLine;

        /// <summary>
        /// 行の番号です。番号は、0から始まります。
        /// </summary>
        public int LineNo { get { return lineNo; } }

        /// <summary>
        /// ある行内での列の番号です。番号は、0から始まります。
        /// </summary>
        public int CharNoInLine { get { return charNoInLine; } }

        /// <summary>
        /// このオブジェクトが有効な位置を指しているか取得します。
        /// </summary>
        /// <returns>有効なとき true、それ以外 false</returns>
        public bool IsValid { get { return lineNo >= 0 && charNoInLine >= 0; } }

        /// <summary>
        /// このオブジェクトがファイルの終端を指しているか取得します。
        /// </summary>
        public bool IsEOF { get { return this.Equals(EOFPosition); } }

        /// <summary>
        /// 無効な位置を指す CharPosition です。
        /// </summary>
        public static readonly CharPosition InvalidPosition = new CharPosition(-1, -1, false);

        public static readonly CharPosition EOFPosition = new CharPosition(-2, -2, false);

        private CharPosition(int lineNo, int charNoInLine, bool checkValues)
        {
            if (checkValues)
            {
                if (lineNo < 0 || charNoInLine < 0)
                {
                    throw new ArgumentOutOfRangeException(lineNo < 0 ? "lineNo" : "charNoInLine");
                }
            }

            this.lineNo = lineNo;
            this.charNoInLine = charNoInLine;
        }

        public CharPosition(int lineNo, int charNoInLine)
            : this(lineNo, charNoInLine, true)
        { }

        /// <summary>
        /// このオブジェクトが指し示す位置を表す文字列を取得します。
        /// 行番号と列番号は、1から始まるよう補正されます。
        /// </summary>
        public override string ToString()
        {
            if (this.IsEOF)
            {
                return "[EOF]";
            }
            else
            {
                return "[" + (lineNo + 1) + ", " + (charNoInLine + 1) + "]";
            }
        }

        /// <summary>
        /// このオブジェクトが指し示す位置を表す日本語表記の文字列を取得します。
        /// 行番号と列番号は、1から始まるよう補正されます。
        /// </summary>
        public string ToJapaneseString()
        {
            if (this.IsEOF)
            {
                return "EOF";
            }
            else
            {
                return (lineNo + 1) + "行 " + (charNoInLine + 1) + "列";
            }
        }

        public int CompareTo(CharPosition other)
        {
            if (this.lineNo != other.lineNo)
            {
                return this.lineNo.CompareTo(other.lineNo);
            }
            else
            {
                return this.charNoInLine.CompareTo(other.charNoInLine);
            }
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null) || !(obj is CharPosition))
            {
                return false;
            }
            else
            {
                return this.CompareTo(obj as CharPosition) == 0;
            }
        }

        public override int GetHashCode()
        {
            return unchecked(lineNo << 8 + charNoInLine).GetHashCode();
        }
    }

    internal class StringScanner
    {
        public delegate bool CharPredicade(char c);

        private readonly string text;
        private readonly int length;
        private int offset;

        public string Text { get { return text; } }

        public int LineNo { get; private set; }
        public int CharNoInLine { get; private set; }
        public CharPosition CurrentPosition
        {
            get { return new CharPosition(LineNo, CharNoInLine); }
        }

        public bool EOF
        {
            get { return offset >= length; }
        }

        public StringScanner(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            this.text = text;
            this.length = text.Length;
        }

        public int Peek()
        {
            if (offset >= length)
                return -1;

            return text[offset];
        }

        public int Read()
        {
            if (offset >= length)
                return -1;

            char c = text[offset];
            offset++;

            if (c == '\r' || c == '\n')
            {
                LineNo++;
                CharNoInLine = 0;

                if (c == '\r' && (offset < length && text[offset] == '\n'))
                {
                    offset++;
                }
            }
            else
            {
                CharNoInLine++;
            }

            return c;
        }

        protected string PeekOrReadAwhileBase(bool restrictLength, int maxLength, CharPredicade predicade, bool isRead)
        {
            Debug.Assert(!(restrictLength && maxLength < 0));

            StringBuilder sb = new StringBuilder(restrictLength ? maxLength : 0);

            if (!restrictLength)
                maxLength = int.MaxValue;

            int savedOffset = offset, savedLineNo = LineNo, savedCharNoInLine = CharNoInLine;

            for (int i = 0; i < maxLength; i++)
            {
                int p = Peek();
                if (p == -1 || !predicade((char)p))
                {
                    break;
                }
                else
                {
                    sb.Append((char)Read());
                }
            }

            if (!isRead)
            {
                // StringScanner の状態を復元する
                offset = savedOffset;
                LineNo = savedLineNo;
                CharNoInLine = savedCharNoInLine;
            }

            return sb.ToString();
        }

        public string PeekString(int maxLength)
        {
            return PeekOrReadAwhileBase(true, maxLength, c => true, false);
        }

        public string ReadString(int maxLength)
        {
            return PeekOrReadAwhileBase(true, maxLength, c => true, true);
        }

        public string PeekAwhile(CharPredicade predicade)
        {
            return PeekOrReadAwhileBase(false, 0, predicade, false);
        }

        public string ReadAwhile(CharPredicade predicade)
        {
            return PeekOrReadAwhileBase(false, 0, predicade, true);
        }

        public void SkipAwhile(CharPredicade predicade)
        {
            while (Peek() != -1 && predicade((char)Peek()))
            {
                Read();
            }
        }
    }
}
