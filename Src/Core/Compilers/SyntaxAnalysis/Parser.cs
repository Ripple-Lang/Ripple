using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Ripple.Compilers.ConstantValues;
using Ripple.Compilers.ErrorsAndWarnings;
using Ripple.Compilers.Exceptions;
using Ripple.Compilers.Expressions;
using Ripple.Compilers.LexicalAnalysis;
using Ripple.Compilers.Options;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.Tokens;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.SyntaxAnalysis
{
    public sealed partial class Parser
    {
        /// <summary>
        /// 型名を省略したときに暗黙的に指定される型
        /// </summary>
        internal static readonly TypeData DefaultVariableType = BuiltInNumericType.Float64;

        private readonly TokenSequence tokens;
        private readonly ErrorsAndWarningsContainer errorsAndWarnings;
        private readonly Stack<IScope> scopes;
        private readonly CompileOption option;
        private readonly IEnumerable<ProgramUnit> externalUnits;
        private string CSharpLocation
        {
            get { return option.NameSpaceName + "." + option.ClassName; }
        }

        private ProgramUnit programUnit;

        public Parser(TokenSequence tokens, ErrorsAndWarningsContainer errorsAndWarnings, CompileOption option)
            : this(tokens, errorsAndWarnings, option, null)
        { }

        public Parser(TokenSequence tokens, ErrorsAndWarningsContainer errorsAndWarnings, CompileOption option, IEnumerable<ProgramUnit> externalUnits)
        {
            this.tokens = tokens;
            this.errorsAndWarnings = errorsAndWarnings;
            this.scopes = new Stack<IScope>();
            this.option = option;
            this.externalUnits = externalUnits;
        }

        #region シンボルの収集

        internal GlobalScope GatherSymbols()
        {
            tokens.Reset();

            GlobalScope globalScope = new GlobalScope(CSharpLocation);
            Token token;

            while (!tokens.EOF)
            {
                if (IsUnitMemberDefnisionStart(token = tokens.Read()))
                {
                    try
                    {
                        globalScope.Define(CreateSymbol(token, globalScope));
                    }
                    catch (SymbolAlreadyExistsException e)
                    {
                        errorsAndWarnings.AddError(new IdentifierAlreadyDeclaredError(token.Position, e.SymbolName));
                    }
                }
            }

            return globalScope;
        }

        private bool IsUnitMemberDefnisionStart(Token token)
        {
            return token.IsSameKeywordType(KeywordToken.KeywordType.Stage)
                || token.IsSameKeywordType(KeywordToken.KeywordType.Param)
                || token.IsSameKeywordType(KeywordToken.KeywordType.Init)
                || token.IsSameKeywordType(KeywordToken.KeywordType.Operation)
                || token.IsSameKeywordType(KeywordToken.KeywordType.Func)
                || token.IsSameKeywordType(KeywordToken.KeywordType.Struct);
        }

        private Symbol CreateSymbol(Token token, GlobalScope globalScope)
        {
            if (token is KeywordToken)
            {
                switch (((KeywordToken)token).Type)
                {
                    case KeywordToken.KeywordType.Stage:
                        {
                            string name = ParseIdentifierName();
                            return new StageSymbol(name, CSharpLocation);
                        }
                    case KeywordToken.KeywordType.Param:
                        {
                            string name = ParseIdentifierName();
                            return new ParameterSymbol(name, CSharpLocation);
                        }
                    case KeywordToken.KeywordType.Init:
                        {
                            return new InitialiationSymbol(globalScope);
                        }
                    case KeywordToken.KeywordType.Operation:
                        {
                            return new OperationSymbol(globalScope);
                        }
                    case KeywordToken.KeywordType.Func:
                        {
                            string name = ParseIdentifierName();
                            return new FunctionSymbol(name, globalScope, CSharpLocation);
                        }
                    case KeywordToken.KeywordType.Struct:
                        {
                            string name = ParseIdentifierName();
                            return new StructSymbol(name);
                        }
                }
            }

            return null;
        }

        #endregion

        public ProgramUnit ParseProgramUnit()
        {
            // シンボルを収集する
            GlobalScope globalScope = GatherSymbols();

            // プログラムユニットの設定
            programUnit = new ProgramUnit(globalScope, CSharpLocation, externalUnits);

            // パース
            ParseGlobal();

            return programUnit;
        }

        private void ParseGlobal()
        {
            tokens.Reset();

            scopes.Push(programUnit.GlobalScope);

            while (!tokens.EOF)
            {
                Token token = tokens.Read();

                if (token.IsSameKeywordType(KeywordToken.KeywordType.Stage))
                {
                    ParseStage(ParseIdentifierName());
                }
                else if (token.IsSameKeywordType(KeywordToken.KeywordType.Param))
                {
                    ParseParameter(ParseIdentifierName());
                }
                else if (token.IsSameKeywordType(KeywordToken.KeywordType.Init))
                {
                    ParseInit(Constants.UserInitializeMethodName);
                }
                else if (token.IsSameKeywordType(KeywordToken.KeywordType.Operation))
                {
                    ParseOperation(Constants.OperationMethodName);
                }
                else if (token.IsSameKeywordType(KeywordToken.KeywordType.Func))
                {
                    ParseFunction(ParseIdentifierName());
                }
                else if (token.IsSameKeywordType(KeywordToken.KeywordType.Struct))
                {
                    ParseStruct(ParseIdentifierName());
                }
                else
                {
                    errorsAndWarnings.AddError(new UnexpectedTokenError(token.Position, token));
                }
            }

            Debug.Assert(scopes.Count == 1 && object.ReferenceEquals(scopes.Peek(), programUnit.GlobalScope));
            scopes.Pop();
        }

        #region トップレベルメンバーのパース

        private void ParseStage(string name)
        {
            StageSymbol symbol = FindTopLevelSymbol<StageSymbol>(name);

            // 型名
            EnsureKeywordToken(KeywordToken.KeywordType.As);
            symbol.Type = ParseType();

            // holds節があるか確認する
            if (tokens.Peek().IsSameKeywordType(KeywordToken.KeywordType.Holds))
            {
                tokens.Read();
                int numHolds = checked((int)ParseInt64());
                symbol.StageHoldState = new PartialyStageHoldState(symbol.Type, symbol, numHolds);
            }
            else
            {
                symbol.StageHoldState = new AllStageHoldedState(symbol.Type, symbol);
            }

            EnsureSymbolToken(SymbolToken.SymbolType.Semicolon);
        }

        private void ParseParameter(string name)
        {
            ParameterSymbol symbol = FindTopLevelSymbol<ParameterSymbol>(name);

            symbol.Type = ParseReturnTypeIfExists();
            symbol.Body = ParseFunctionBody(symbol.Type);
        }

        private void ParseInit(string name)
        {
            InitialiationSymbol symbol = FindTopLevelSymbol<InitialiationSymbol>(name);
            ParseNoParameterMethod(name, symbol);
        }

        private void ParseOperation(string name)
        {
            OperationSymbol symbol = FindTopLevelSymbol<OperationSymbol>(name);
            ParseNoParameterMethod(name, symbol);
        }

        private void ParseFunction(string name)
        {
            // シンボルを探す
            FunctionSymbol function = FindTopLevelSymbol<FunctionSymbol>(name);

            // スタックにスコープを追加
            scopes.Push(function);

            // パラメーター
            function.Parameters = ParseFunctionParameters();

            function.Type = ParseReturnTypeIfExists();
            function.Body = ParseFunctionBody(function.Type);

            Debug.Assert(scopes.Count >= 1 && object.ReferenceEquals(scopes.Peek(), function));
            scopes.Pop();
        }

        private void ParseStruct(string name)
        {
            // シンボルを探す
            StructSymbol structSymbol = FindTopLevelSymbol<StructSymbol>(name);

            EnsureSymbolToken(SymbolToken.SymbolType.Equals);

            // コンストラクタのパラメーター(= フィールド)
            structSymbol.Fields = ParseFunctionParameters();

            EnsureSymbolToken(SymbolToken.SymbolType.Semicolon);
        }

        #endregion

        #region ヘルパーメソッド

        private SymbolType FindTopLevelSymbol<SymbolType>(string name) where SymbolType : class
        {
            SymbolType symbol = programUnit.FindSymbolInThisUnit(name) as SymbolType;
            Debug.Assert(symbol != null);
            return symbol;
        }

        private void ParseNoParameterMethod<SymbolType>(string name, SymbolType symbol) where SymbolType : NoParameterMethodSymbol
        {
            // シンボルをプッシュする
            scopes.Push(symbol);

            // 定義部分
            BlockStatement body = ParseAndAddStatementsTo(new BlockStatement(scopes.Peek()));
            symbol.Body = body;

            scopes.Pop();
        }

        private TypeData ParseReturnTypeIfExists()
        {
            // 戻り値(あれば)
            if (tokens.Peek().IsSameKeywordType(KeywordToken.KeywordType.As))
            {
                tokens.Read();
                return ParseType();
            }
            else
            {
                return null;
            }
        }

        private BlockStatement ParseFunctionBody(TypeData returnType)
        {
            // 関数定義の解析
            if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.Semicolon))
            {
                // 定義または初期化の省略
                tokens.Read();
                return null;
            }
            else if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.Equals))
            {
                // 数式的な関数
                tokens.Read();
                return ParseExpressionScope();
            }
            else
            {
                // ブロック構造により定義される関数

                if (returnType == null)
                {
                    errorsAndWarnings.AddError(new ExpectedError(tokens.Peek().Position, "戻り値"));
                }

                return ParseAndAddStatementsTo(new BlockStatement(scopes.Peek()));
            }
        }

        private string ParseIdentifierName()
        {
            var peek = tokens.Peek();
            var nameToken = peek as IdentifierToken;
            string name;
            if (nameToken == null)
            {
                if (peek is IdentifierToken || peek is IntegerLiteralToken || peek is FloatLiteralToken)
                {
                }
                else
                {
                    /* 変数名らしくないので消費する */
                    tokens.Read();
                }

                errorsAndWarnings.AddError(new ExpectedError(tokens.Read().Position, "関数名"));
                name = "@noname";
            }
            else
            {
                tokens.Read();
                name = nameToken.Name;
            }
            return name;
        }

        private long ParseInt64()
        {
            var token = tokens.Read();
            var integerToken = token as IntegerLiteralToken;

            if (integerToken == null)
            {
                errorsAndWarnings.AddError(new ExpectedError(token.Position, "整数値"));
                return 0;   // 無意味な値
            }
            else
            {
                return integerToken.Value;
            }
        }

        private ReturnOnlyBlockStatement ParseExpressionScope()
        {
            Expression expression = ParseExpression();

            // セミコロン記号
            EnsureSymbolToken(SymbolToken.SymbolType.Semicolon);

            return new ReturnOnlyBlockStatement(scopes.Peek(), expression);
        }

        private IList<FunctionParameterSymbol> ParseFunctionParameters()
        {
            EnsureSymbolToken(SymbolToken.SymbolType.OpeningBracket);

            var parameters = new List<FunctionParameterSymbol>();

            for (; ; )
            {
                if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.ClosingBracket))
                {
                    break;
                }

                var variable = ParseLocalVariable(true /* 型名は省略できる */);
                if (variable != null)
                    parameters.Add(variable);

                if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.Comma))
                {
                    tokens.Read();
                }
                else
                {
                    break;
                }
            }

            EnsureSymbolToken(SymbolToken.SymbolType.ClosingBracket);

            // 重複がないことを確認
            {
                var names = new HashSet<string>();
                foreach (var p in parameters)
                {
                    if (!names.Add(p.Name))
                    {
                        // TODO : CharPositionを正しく指定する
                        errorsAndWarnings.AddError(new IdentifierAlreadyDeclaredError(CharPosition.InvalidPosition, p.Name));
                    }
                }
            }

            return new ReadOnlyCollection<FunctionParameterSymbol>(parameters);
        }

        private FunctionParameterSymbol ParseLocalVariable(bool allowsOmmisionOfType)
        {
            // 変数名
            string variableName = ParseIdentifierName();

            // 型名(as)
            TypeData type = DefaultVariableType;
            if (tokens.Peek().IsSameKeywordType(KeywordToken.KeywordType.As))
            {
                tokens.Read();
                type = ParseType();
            }
            else
            {
                if (!allowsOmmisionOfType)
                    errorsAndWarnings.AddError(new ExpectedError(tokens.Peek().Position, "型名"));
            }

            return new FunctionParameterSymbol(variableName) { Type = type };
        }

        private TypeData ParseType()
        {
            TypeData type = ParseNonArrayType();

            if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.OpeningSquare))
            {
                var dimensions =
                    ParseExpressionsSeparatedByComma(
                    SymbolToken.SymbolType.OpeningSquare,
                    SymbolToken.SymbolType.ClosingSquare);

                foreach (var numElements in dimensions.Reverse<Expression>())
                {
                    type = new ArrayType(type, numElements);
                }
            }

            return type;
        }

        private TypeData ParseNonArrayType()
        {
            Token peek = tokens.Peek();

            if (peek is KeywordToken)
            {
                // トークンを消費する
                tokens.Read();
                var type = ((KeywordToken)peek).Type;

                if (type == KeywordToken.KeywordType.Nothing)
                {
                    return BuiltInNumericType.Nothing;
                }
                else if (type == KeywordToken.KeywordType.Bool)
                {
                    return BuiltInNumericType.Bool;
                }
                else if (type == KeywordToken.KeywordType.SByte)
                {
                    return BuiltInNumericType.SByte;
                }
                else if (type == KeywordToken.KeywordType.UByte)
                {
                    return BuiltInNumericType.UByte;
                }
                else if (type == KeywordToken.KeywordType.Int)
                {
                    return BuiltInNumericType.Int32;
                }
                else if (type == KeywordToken.KeywordType.Long)
                {
                    return BuiltInNumericType.Int64;
                }
                else if (type == KeywordToken.KeywordType.Float)
                {
                    return BuiltInNumericType.Float64;
                }
                else
                {
                    errorsAndWarnings.AddError(new UnknowTypeError(peek.Position, peek.Original));
                    return null;
                }
            }
            else if (peek is IdentifierToken)
            {
                IdentifierToken identifierToken = tokens.Read() as IdentifierToken;

                // 構造体を探す
                try
                {
                    var structSymbol =
                        programUnit.GlobalScope.Symbols.OfType<StructSymbol>().Single(s => s.Name == identifierToken.Name);
                    return structSymbol.ToTypeData();
                }
                catch (InvalidOperationException)
                {
                    errorsAndWarnings.AddError(new UnknowTypeError(identifierToken.Position, identifierToken.Name));
                    return null;
                }
            }
            else
            {
                if (peek is IntegerLiteralToken || peek is FloatLiteralToken)
                    tokens.Read();

                errorsAndWarnings.AddError(new UnknowTypeError(peek.Position, peek.Original));
                return null;
            }
        }

        private List<Expression> ParseExpressionsSeparatedByComma(SymbolToken.SymbolType openingSymbol, SymbolToken.SymbolType closingSymbol)
        {
            EnsureSymbolToken(openingSymbol);

            var expressions = new List<Expression>();

            for (; ; )
            {
                if (tokens.Peek().IsSameSymbolTokenType(closingSymbol))
                {
                    break;
                }

                expressions.Add(ParseExpression());

                if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.Comma))
                {
                    tokens.Read();
                }
                else
                {
                    break;
                }
            }

            EnsureSymbolToken(closingSymbol);

            return expressions;
        }

        private Symbol Resolve(string name)
        {
            Symbol symbol = scopes.Count > 0 ? scopes.Peek().Resolve(name) : null;

            if (symbol != null)
            {
                return symbol;
            }
            else
            {
                if (externalUnits != null)
                {
                    foreach (var unit in externalUnits)
                    {
                        Symbol externalSymbol = unit.GlobalScope.Resolve(name);
                        if (externalSymbol != null)
                            return externalSymbol;
                    }
                }

                return null;
            }
        }

        private bool IsParallelScope()
        {
            foreach (var s in scopes)
            {
                if (s is BlockStatementInFor && ((BlockStatementInFor)s).ForStatement.IsParallel
                    || s is BlockStatementInEachAt && ((BlockStatementInEachAt)s).EachAtStatement.IsParallel)
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldContinueToBeReturn()
        {
            foreach (var scope in scopes)
            {
                if (scope is BlockStatementInFor)
                {
                    return ((BlockStatementInFor)scope).ForStatement.IsParallel;
                }
                else if (scope is BlockStatementInEachAt)
                {
                    var eachScope = (BlockStatementInEachAt)scope;
                    return eachScope.EachAtStatement.IsParallel && eachScope.EachAtStatement.Indices.Count == 1;
                }
            }

            return false;
        }

        /// <summary>
        /// 要求されたトークンが存在することを確かにします。
        /// 存在する場合には、そのトークンを消費し、trueを返します。
        /// そうでない場合には、消費せず、falseを返します。
        /// </summary>
        /// <param name="predicade"></param>
        /// <param name="expectedName"></param>
        /// <returns></returns>
        private bool EnsureToken(Func<Token, bool> predicade, string expectedName)
        {
            Token peek = tokens.Peek();

            if (predicade(peek))
            {
                tokens.Read();
                return true;
            }
            else
            {
                errorsAndWarnings.AddError(new ExpectedError(peek.Position, expectedName));
                return false;
            }
        }

        private bool EnsureSymbolToken(SymbolToken.SymbolType type)
        {
            return EnsureToken(token => token.IsSameSymbolTokenType(type), SymbolToken.GetOriginalText(type));
        }

        private bool EnsureKeywordToken(KeywordToken.KeywordType type)
        {
            return EnsureToken(token => token.IsSameKeywordType(type), KeywordToken.GetOriginalText(type));
        }

        private void ReadToSemicolon()
        {
            for (; ; )
            {
                if (!tokens.Read().IsSameSymbolTokenType(SymbolToken.SymbolType.Semicolon))
                {
                    break;
                }
            }
        }

        #endregion
    }
}
