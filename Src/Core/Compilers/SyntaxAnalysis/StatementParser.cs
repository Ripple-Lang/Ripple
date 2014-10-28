using System.Collections.Generic;
using System.Diagnostics;
using Ripple.Compilers.ErrorsAndWarnings;
using Ripple.Compilers.Expressions;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.Tokens;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.SyntaxAnalysis
{
    public partial class Parser
    {
        private BlockStatement ParseAndAddStatementsTo(BlockStatement statement)
        {
            //EnsureKeywordToken(KeywordToken.KeywordType.Begin);
            EnsureToken(IsBlockOpening, "ブロックの開始");

            scopes.Push(statement);

            while (!IsBlockClosing(tokens.Peek()))
            {
                if (tokens.EOF)
                {
                    errorsAndWarnings.AddError(new UnexpectedTokenError(tokens.Peek().Position, tokens.Peek()));
                    break;
                }

                statement.AddStatement(ParseStatement());
            }

            tokens.Read();  // "end" または "}"

            Debug.Assert(scopes.Count > 0 && object.ReferenceEquals(scopes.Peek(), statement));
            scopes.Pop();

            return statement;
        }

        private Statement ParseStatement()
        {
            Token peek = tokens.Peek();

            if (peek.IsSameKeywordType(KeywordToken.KeywordType.If))
            {
                return ParseIfStatement();
            }
            else if (peek.IsSameKeywordType(KeywordToken.KeywordType.While))
            {
                return ParseWhileStatement();
            }
            else if (peek.IsSameKeywordType(KeywordToken.KeywordType.Each))
            {
                return ParseEachAtStatement(false);
            }
            else if (peek.IsSameKeywordType(KeywordToken.KeywordType.For))
            {
                return ParseForStatement(false);
            }
            else if (peek.IsSameKeywordType(KeywordToken.KeywordType.Var))
            {
                return ParseVariableDeclarationStatement();
            }
            else if (peek.IsSameKeywordType(KeywordToken.KeywordType.Return))
            {
                return ParseReturnStatement();
            }
            else if (peek.IsSameKeywordType(KeywordToken.KeywordType.Break))
            {
                return ParseBreakStatement();
            }
            else if (peek.IsSameKeywordType(KeywordToken.KeywordType.Continue))
            {
                return ParseContinueStatement();
            }
            else if (peek.IsSameKeywordType(KeywordToken.KeywordType.Parallel))
            {
                tokens.Read();
                Token nextPeek = tokens.Peek();

                if (nextPeek.IsSameKeywordType(KeywordToken.KeywordType.Each))
                {
                    return ParseEachAtStatement(true);
                }
                else if (nextPeek.IsSameKeywordType(KeywordToken.KeywordType.For))
                {
                    return ParseForStatement(true);
                }
                else
                {
                    errorsAndWarnings.AddError(new UnexpectedTokenError(peek.Position, peek));
                    return null;
                }
            }
            else if (IsBlockOpening(peek))
            {
                return ParseAndAddStatementsTo(new BlockStatement(scopes.Peek()));
            }
            else if (peek is EOFToken)
            {
                errorsAndWarnings.AddError(new UnexpectedTokenError(peek.Position, peek));
                return null;
            }
            else
            {
                return ParseExpressionStatement();
            }
        }

        #region 各ステートメントのパース

        private BlockStatement ParseIfScope()
        {
            if (tokens.Peek().IsSameKeywordType(KeywordToken.KeywordType.If))
            {
                BlockStatement scope = new BlockStatement(scopes.Peek());

                scopes.Push(scope);
                scope.AddStatement(ParseIfStatement());
                scopes.Pop();

                return scope;
            }
            else
            {
                return ParseAndAddStatementsTo(new BlockStatement(scopes.Peek()));
            }
        }

        private IfStatement ParseIfStatement()
        {
            EnsureKeywordToken(KeywordToken.KeywordType.If);

            EnsureSymbolToken(SymbolToken.SymbolType.OpeningBracket);
            Expression condition = ParseExpression();
            EnsureSymbolToken(SymbolToken.SymbolType.ClosingBracket);

            BlockStatement ifTrue = ParseAndAddStatementsTo(new BlockStatement(scopes.Peek()));
            BlockStatement ifFalse = null;

            if (tokens.Peek().IsSameKeywordType(KeywordToken.KeywordType.Else))
            {
                tokens.Read();
                ifFalse = ParseIfScope();
            }

            return new IfStatement(condition, ifTrue, ifFalse);
        }

        private WhileStatement ParseWhileStatement()
        {
            EnsureKeywordToken(KeywordToken.KeywordType.While);

            // 条件
            EnsureSymbolToken(SymbolToken.SymbolType.OpeningBracket);
            Expression condition = ParseExpression();
            EnsureSymbolToken(SymbolToken.SymbolType.ClosingBracket);

            BlockStatement scope = ParseAndAddStatementsTo(new BlockStatement(scopes.Peek()));

            return new WhileStatement(condition, scope);
        }

        private EachAtStatement ParseEachAtStatement(bool isParallel)
        {
            EnsureKeywordToken(KeywordToken.KeywordType.Each);
            EnsureSymbolToken(SymbolToken.SymbolType.OpeningBracket);
            EnsureKeywordToken(KeywordToken.KeywordType.At);

            List<string> indices = new List<string>();

            for (; ; )
            {
                indices.Add(ParseIdentifierName());
                if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.Comma))
                {
                    tokens.Read();
                }
                else
                {
                    break;
                }
            }

            EnsureKeywordToken(KeywordToken.KeywordType.In);

            Expression destObject = ParseExpression();  // TODO : 適切か検討
            // destObjectは右辺値扱い
            destObject.AssignmentInfo = new Expression._AssignmentInfo
            {
                IsLeftValue = true,
                IsStageAssignment = true,   // TODO : 暫定的
            };

            EnsureSymbolToken(SymbolToken.SymbolType.ClosingBracket);

            // ステートメントを生成
            EachAtStatement statement = new EachAtStatement(scopes.Peek(), indices, destObject, isParallel);
            ParseAndAddStatementsTo(statement.BlockStatement);

            return statement;
        }

        private ForStatement ParseForStatement(bool isParallel)
        {
            EnsureKeywordToken(KeywordToken.KeywordType.For);

            EnsureSymbolToken(SymbolToken.SymbolType.OpeningBracket);

            string indexerName = ParseIdentifierName();
            EnsureSymbolToken(SymbolToken.SymbolType.Equals);
            Expression indexerInitialValue = ParseExpression();
            EnsureKeywordToken(KeywordToken.KeywordType.To);
            Expression indexerFinalValue = ParseExpression();

            EnsureSymbolToken(SymbolToken.SymbolType.ClosingBracket);

            // ステートメントを生成
            ForStatement statement = new ForStatement(scopes.Peek(), indexerName, indexerInitialValue, indexerFinalValue, isParallel);
            ParseAndAddStatementsTo(statement.BlockStatement);

            return statement;
        }

        private VariableDeclarationStatement ParseVariableDeclarationStatement()
        {
            EnsureKeywordToken(KeywordToken.KeywordType.Var);

            // 変数名
            Token token = tokens.Read();
            IdentifierToken identifierToken = token as IdentifierToken;
            if (identifierToken == null)
            {
                errorsAndWarnings.AddError(new UnexpectedTokenError(token.Position, token));
            }

            string name = identifierToken.Name;

            // 変数がすでにシンボルとして存在する場合はエラーにする(オプションで指定されているときのみ)
            var previousSymbol = Resolve(name);
            if (previousSymbol != null)
            {
                if (!(previousSymbol is VariableSymbol)
                    || (previousSymbol is LocalVariableSymbol && object.ReferenceEquals(((LocalVariableSymbol)previousSymbol).DeclaredScope, scopes.Peek()))
                    || option.ProhibitOverloadingOfVariable)
                {
                    errorsAndWarnings.AddError(new IdentifierAlreadyDeclaredError(token.Position, name));
                    return null;
                }
            }

            // 型名            
            TypeData type = ParseReturnTypeIfExists();

            // イコール記号(初期化は必須)
            EnsureSymbolToken(SymbolToken.SymbolType.Equals);

            // 初期化値
            Expression initialValue = ParseExpression();

            // セミコロン記号
            EnsureSymbolToken(SymbolToken.SymbolType.Semicolon);

            // シンボルの生成と定義
            VariableSymbol variable = new LocalVariableSymbol(name, initialValue, scopes.Peek())
            {
                Type = type
            };
            scopes.Peek().Define(variable);

            return new VariableDeclarationStatement(variable, initialValue);
        }

        private ReturnStatement ParseReturnStatement()
        {
            EnsureKeywordToken(KeywordToken.KeywordType.Return);

            if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.Semicolon))
            {
                tokens.Read();
                return new ReturnStatement(null);
            }
            else
            {
                ReturnStatement statement = new ReturnStatement(ParseExpression());
                EnsureSymbolToken(SymbolToken.SymbolType.Semicolon);
                return statement;
            }
        }

        private BreakStatement ParseBreakStatement()
        {
            if (IsParallelScope())
            {
                errorsAndWarnings.AddError(new IsNotUsableInThisContextError(tokens.Peek().Position, "break"));
            }

            EnsureKeywordToken(KeywordToken.KeywordType.Break);
            EnsureSymbolToken(SymbolToken.SymbolType.Semicolon);
            return new BreakStatement();
        }

        private ContinueStatement ParseContinueStatement()
        {
            EnsureKeywordToken(KeywordToken.KeywordType.Continue);
            EnsureSymbolToken(SymbolToken.SymbolType.Semicolon);
            return new ContinueStatement(ShouldContinueToBeReturn());
        }

        private ExpressionStatement ParseExpressionStatement()
        {
            ExpressionStatement statement = new ExpressionStatement(ParseAssignmentExpression());
            EnsureSymbolToken(SymbolToken.SymbolType.Semicolon);
            return statement;
        }

        #endregion

        private static bool IsBlockOpening(Token token)
        {
            return token.IsSameSymbolTokenType(SymbolToken.SymbolType.OpeningBrace);
        }

        private static bool IsBlockClosing(Token token)
        {
            return token.IsSameSymbolTokenType(SymbolToken.SymbolType.ClosingBrace);
        }
    }
}
