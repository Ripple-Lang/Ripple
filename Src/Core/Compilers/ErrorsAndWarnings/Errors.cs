using Ripple.Compilers.LexicalAnalysis;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.Tokens;

namespace Ripple.Compilers.ErrorsAndWarnings
{
    public class InvalidSymbolError : Error
    {
        internal InvalidSymbolError(CharPosition position, string text)
            : base(position, text + "は未知のトークンです。")
        { }
    }

    public class ExpectedError : Error
    {
        internal ExpectedError(CharPosition position, string what)
            : base(position, what + "が必要です。")
        { }
    }

    public class UnknowTypeError : Error
    {
        internal UnknowTypeError(CharPosition position, string typeName)
            : base(position, "\"" + typeName + "\"を型名として認識できません。")
        { }
    }

    public class UndeclaredIdentifierError : Error
    {
        internal UndeclaredIdentifierError(CharPosition position, IdentifierToken token)
            : base(position, "識別子\"" + token.Original + "\"は、定義されていません。")
        { }
    }

    public class FunctionUndeclaredError : Error
    {
        internal FunctionUndeclaredError(CharPosition position, FunctionSymbol function)
            : base(position, "関数\"" + function.Name + "\"は定義されませんでした。")
        { }
    }

    public class UnexpectedTokenError : Error
    {
        internal UnexpectedTokenError(CharPosition position, Token token)
            : base(position, position.IsEOF ? ("コンパイル中に予期せずEOFに到達しました。") :
                ("予期せぬトークン" + token.Original + "です。"))
        { }
    }

    public class IsNotVariableError : Error
    {
        internal IsNotVariableError(CharPosition position, Token token)
            : base(position, token.Original + "は変数ではありません。")
        { }
    }

    public class IsNotFunctionError : Error
    {
        internal IsNotFunctionError(CharPosition position, Token token)
            : base(position, token.Original + "は関数ではありません。")
        { }
    }

    public class IdentifierAlreadyDeclaredError : Error
    {
        internal IdentifierAlreadyDeclaredError(CharPosition position, string name)
            : base(position, "識別子\"" + name + "\"はすでに定義されています。")
        { }
    }

    public class IsNotUsableInThisContextError : Error
    {
        internal IsNotUsableInThisContextError(CharPosition position, string kindName)
            : base(position, kindName + "はこの文脈では使用できません。")
        { }
    }
}
