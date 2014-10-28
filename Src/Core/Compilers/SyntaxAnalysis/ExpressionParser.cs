using System;
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
        private Expression ParseAssignmentExpression()
        {
            Expression left = ParseLeftUnaryExpression();      // TODO : 適切かどうか確認

            AssignmentType assignmentType = GetAssignmentTypeIfItIs(tokens.Peek());

            if (assignmentType == null)
            {
                // 代入ではない
                return left;
            }
            else
            {
                tokens.Read();

                // 左辺値に当たる式のAssignmentInfoを設定
                left.AssignmentInfo = new Expression._AssignmentInfo
                {
                    IsLeftValue = true,
                    IsStageAssignment = assignmentType.IsStageAssignment,
                };

                // 右辺値のパース
                Expression right = ParseExpression();
                right.AssignmentInfo = new Expression._AssignmentInfo
                {
                    IsLeftValue = false,
                    IsStageAssignment = assignmentType.IsStageAssignment,
                };

                // 複合演算子の場合の処理
                var enclosing = scopes.Peek();
                switch (assignmentType.Kind)
                {
                    case AssignmentType._Kind.Simple:
                        return new AssignmentExpression(enclosing, left, right);
                    case AssignmentType._Kind.Add:
                        return new AssignmentExpression(enclosing, left, new AddExpression(enclosing, left, right));
                    case AssignmentType._Kind.Sub:
                        return new AssignmentExpression(enclosing, left, new SubExpression(enclosing, left, right));
                    case AssignmentType._Kind.Mult:
                        return new AssignmentExpression(enclosing, left, new MultExpression(enclosing, left, right));
                    case AssignmentType._Kind.Div:
                        return new AssignmentExpression(enclosing, left, new DivExpression(enclosing, left, right));
                    default:
                        Debug.Assert(false);
                        return null;
                }
            }
        }

        #region 代入演算子パースのためのヘルパークラス・メソッド

        private class AssignmentType
        {
            public enum _Kind
            {
                Simple, Add, Sub, Mult, Div,
            }

            public bool IsStageAssignment { get; set; }
            public _Kind Kind { get; set; }
        }

        private AssignmentType GetAssignmentTypeIfItIs(Token token)
        {
            var symbolToken = token as SymbolToken;
            if (symbolToken == null)
                return null;    // 代入演算子ではない

            switch (symbolToken.Type)
            {
                // 通常の代入

                case SymbolToken.SymbolType.Equals:
                    return new AssignmentType() { IsStageAssignment = false, Kind = AssignmentType._Kind.Simple };
                case SymbolToken.SymbolType.AddAssign:
                    return new AssignmentType() { IsStageAssignment = false, Kind = AssignmentType._Kind.Add };
                case SymbolToken.SymbolType.SubAssign:
                    return new AssignmentType() { IsStageAssignment = false, Kind = AssignmentType._Kind.Sub };
                case SymbolToken.SymbolType.MultAssign:
                    return new AssignmentType() { IsStageAssignment = false, Kind = AssignmentType._Kind.Mult };
                case SymbolToken.SymbolType.DivAssign:
                    return new AssignmentType() { IsStageAssignment = false, Kind = AssignmentType._Kind.Div };

                // ステージに対する代入

                case SymbolToken.SymbolType.LessOrEquals:
                    return new AssignmentType() { IsStageAssignment = true, Kind = AssignmentType._Kind.Simple };
                case SymbolToken.SymbolType.AddStageAssign:
                    return new AssignmentType() { IsStageAssignment = true, Kind = AssignmentType._Kind.Add };
                case SymbolToken.SymbolType.SubStageAssign:
                    return new AssignmentType() { IsStageAssignment = true, Kind = AssignmentType._Kind.Sub };
                case SymbolToken.SymbolType.MultStageAssign:
                    return new AssignmentType() { IsStageAssignment = true, Kind = AssignmentType._Kind.Mult };
                case SymbolToken.SymbolType.DivStageAssign:
                    return new AssignmentType() { IsStageAssignment = true, Kind = AssignmentType._Kind.Div };

                // 代入演算子ではない
                default:
                    return null;
            }
        }

        #endregion

        private Expression ParseExpression()
        {
            return ParseConditional();
        }

        private Expression ParseLeftAssociativeExpressionBase(
            Func<Expression> nextParser,
            Func<Token, bool> isOperator,
            Func<Expression, Token, Expression, Expression> expressionCreater)
        {
            Expression currentExpression = nextParser();

            while (isOperator(tokens.Peek()))
            {
                Token operatorToken = tokens.Read();
                Expression nextExpression = nextParser();
                currentExpression =
                    expressionCreater(currentExpression, operatorToken, nextExpression);
            }

            return currentExpression;
        }

        private Expression ParseConditional()
        {
            Expression left = ParseLogicalAndOr();

            if (tokens.Peek().IsSameSymbolTokenType(SymbolToken.SymbolType.Question))
            {
                tokens.Read();
                Expression center = ParseExpression();
                EnsureSymbolToken(SymbolToken.SymbolType.Colon);
                Expression right = ParseConditional();
                return new ConditionExpression(scopes.Peek(), left, center, right);
            }
            else
            {
                return left;
            }
        }

        private Expression ParseLogicalAndOr()
        {
            return ParseLeftAssociativeExpressionBase(
                ParseEqualsAndNotEquals,
                token =>
                {
                    return token.IsSameKeywordType(KeywordToken.KeywordType.And)
                        || token.IsSameKeywordType(KeywordToken.KeywordType.Or);
                },
                (left, token, right) =>
                {
                    Debug.Assert(token is KeywordToken);

                    switch (((KeywordToken)token).Type)
                    {
                        case KeywordToken.KeywordType.And:
                            return new LogicalAndExpression(scopes.Peek(), left, right);

                        case KeywordToken.KeywordType.Or:
                            return new LogicalOrExpression(scopes.Peek(), left, right);

                        default:
                            Debug.Assert(false);
                            return null;
                    }
                });
        }

        private Expression ParseEqualsAndNotEquals()
        {
            return ParseLeftAssociativeExpressionBase(
                ParseLessAndGreater,
                token =>
                {
                    return token.IsSameSymbolTokenType(SymbolToken.SymbolType.Equals)
                        || token.IsSameSymbolTokenType(SymbolToken.SymbolType.NotEquals);
                },
                (left, token, right) =>
                {
                    Debug.Assert(token is SymbolToken);

                    switch (((SymbolToken)token).Type)
                    {
                        case SymbolToken.SymbolType.Equals:
                            return new EqualsExpression(scopes.Peek(), left, right);

                        case SymbolToken.SymbolType.NotEquals:
                            return new NotEqualsExpression(scopes.Peek(), left, right);

                        default:
                            Debug.Assert(false);
                            return null;
                    }
                });
        }

        private Expression ParseLessAndGreater()
        {
            return ParseLeftAssociativeExpressionBase(
                ParseAs,
                token =>
                {
                    return token.IsSameSymbolTokenType(SymbolToken.SymbolType.LessThan)
                        || token.IsSameSymbolTokenType(SymbolToken.SymbolType.LessOrEquals)
                        || token.IsSameSymbolTokenType(SymbolToken.SymbolType.GreaterOrEquals)
                        || token.IsSameSymbolTokenType(SymbolToken.SymbolType.GreaterThan);
                },
                (left, token, right) =>
                {
                    Debug.Assert(token is SymbolToken);

                    switch (((SymbolToken)token).Type)
                    {
                        case SymbolToken.SymbolType.GreaterThan:
                            return new GreaterThanExpression(scopes.Peek(), left, right);

                        case SymbolToken.SymbolType.GreaterOrEquals:
                            return new GreaterThanOrEqualsExpression(scopes.Peek(), left, right);

                        case SymbolToken.SymbolType.LessThan:
                            return new LessThanExpression(scopes.Peek(), left, right);

                        case SymbolToken.SymbolType.LessOrEquals:
                            return new LessThanOrEqualsExpression(scopes.Peek(), left, right);

                        default:
                            Debug.Assert(false);
                            return null;
                    }
                });
        }

        private Expression ParseAs()
        {
            Expression expression = ParseAddAndSub();

            while (tokens.Peek().IsSameKeywordType(KeywordToken.KeywordType.As))
            {
                tokens.Read();
                TypeData targetType = ParseType();

                expression = new ConvertExpression(scopes.Peek(), expression, targetType);
            }

            return expression;
        }

        private Expression ParseAddAndSub()
        {
            return ParseLeftAssociativeExpressionBase(
                ParseMultAndDiv,
                token =>
                {
                    return token.IsSameSymbolTokenType(SymbolToken.SymbolType.Plus)
                        || token.IsSameSymbolTokenType(SymbolToken.SymbolType.Minus);
                },
                (left, token, right) =>
                {
                    Debug.Assert(token is SymbolToken);

                    switch (((SymbolToken)token).Type)
                    {
                        case SymbolToken.SymbolType.Plus:
                            return new AddExpression(scopes.Peek(), left, right);

                        case SymbolToken.SymbolType.Minus:
                            return new SubExpression(scopes.Peek(), left, right);

                        default:
                            Debug.Assert(false);
                            return null;
                    }
                });
        }

        private Expression ParseMultAndDiv()
        {
            return ParseLeftAssociativeExpressionBase(
                ParseLeftUnaryExpression,
                token =>
                {
                    return token.IsSameSymbolTokenType(SymbolToken.SymbolType.Mult)
                        || token.IsSameSymbolTokenType(SymbolToken.SymbolType.Div)
                        || token.IsSameKeywordType(KeywordToken.KeywordType.Idiv)
                        || token.IsSameKeywordType(KeywordToken.KeywordType.Mod);
                },
                (left, token, right) =>
                {
                    if (token is SymbolToken)
                    {
                        switch (((SymbolToken)token).Type)
                        {
                            case SymbolToken.SymbolType.Mult:
                                return new MultExpression(scopes.Peek(), left, right);

                            case SymbolToken.SymbolType.Div:
                                return new DivExpression(scopes.Peek(), left, right);

                            default:
                                Debug.Assert(false);
                                return null;
                        }
                    }
                    else if (token is KeywordToken)
                    {
                        switch (((KeywordToken)token).Type)
                        {
                            case KeywordToken.KeywordType.Idiv:

                                return new IdivExpression(scopes.Peek(), left, right);
                            case KeywordToken.KeywordType.Mod:
                                return new ModExpression(scopes.Peek(), left, right);

                            default:
                                Debug.Assert(false);
                                return null;
                        }
                    }
                    else
                    {
                        Debug.Assert(false);
                        return null;
                    }
                });
        }

        /// <summary>
        /// 左に演算式号が来る式(++xなど)をパースします。
        /// </summary>
        private Expression ParseLeftUnaryExpression()
        {
            Token peek = tokens.Peek();

            if (peek is SymbolToken)
            {
                switch (((SymbolToken)peek).Type)
                {
                    case SymbolToken.SymbolType.Plus:
                        tokens.Read();
                        return ParseRightUnaryExpression();

                    case SymbolToken.SymbolType.Minus:
                        tokens.Read();
                        return new NegateExpression(scopes.Peek(), ParseRightUnaryExpression());

                    case SymbolToken.SymbolType.PlusPlus:
                        tokens.Read();
                        return new IncrementDecrementExpression(scopes.Peek(),
                            ParseRightUnaryExpression(),
                            IncrementDecrementExpression.Kind.Increment,
                            IncrementDecrementExpression.OperatorPosition.Left);

                    case SymbolToken.SymbolType.MinusMinus:
                        tokens.Read();
                        return new IncrementDecrementExpression(scopes.Peek(),
                            ParseRightUnaryExpression(),
                            IncrementDecrementExpression.Kind.Decrement,
                            IncrementDecrementExpression.OperatorPosition.Left);
                }
            }
            else if (peek is KeywordToken)
            {
                switch (((KeywordToken)peek).Type)
                {
                    case KeywordToken.KeywordType.Not:
                        tokens.Read();
                        return new LogicalNotExpression(scopes.Peek(), ParseRightUnaryExpression());
                }
            }

            return ParseRightUnaryExpression();
        }

        /// <summary>
        /// 右に演算式号が来る式(x++など)をパースします。
        /// </summary>
        private Expression ParseRightUnaryExpression()
        {
            Expression expression = ParseTerm();

            bool operatorFound;
            do
            {
                Token peek = tokens.Peek();
                operatorFound = false;

                if (peek is SymbolToken)
                {
                    switch (((SymbolToken)peek).Type)
                    {
                        case SymbolToken.SymbolType.PlusPlus:
                            tokens.Read();
                            operatorFound = true;
                            expression = new IncrementDecrementExpression(scopes.Peek(),
                                expression,
                                IncrementDecrementExpression.Kind.Increment,
                                IncrementDecrementExpression.OperatorPosition.Right);
                            break;

                        case SymbolToken.SymbolType.MinusMinus:
                            tokens.Read();
                            operatorFound = true;
                            expression = new IncrementDecrementExpression(scopes.Peek(),
                                expression,
                                IncrementDecrementExpression.Kind.Decrement,
                                IncrementDecrementExpression.OperatorPosition.Right);
                            break;

                        case SymbolToken.SymbolType.LessThan:
                            // 開き山かっこ('<')
                            if (expression is VariableExpression
                                && ((VariableExpression)expression).Variable is StageSymbol)
                            {
                                tokens.Read();
                                operatorFound = true;
                                Expression time = ParseAddAndSub();    // TODO : 要改善
                                EnsureSymbolToken(SymbolToken.SymbolType.GreaterThan);  // 閉じ山かっこ('>')
                                expression = new TimeSpecifyExpression(scopes.Peek(), expression as VariableExpression, time);
                            }
                            break;

                        case SymbolToken.SymbolType.OpeningBracket:
                            // 関数呼び出し
                            operatorFound = true;
                            if (expression is FunctionExpression)
                            {
                                expression = ParseFunctionCall(expression as FunctionExpression);
                            }
                            else
                            {
                                // TODO : 暫定的な対処
                                errorsAndWarnings.AddError(new IsNotFunctionError(LexicalAnalysis.CharPosition.InvalidPosition, InvalidToken.Instance));
                                tokens.Read();
                            }
                            break;

                        case SymbolToken.SymbolType.OpeningSquare:
                            operatorFound = true;
                            var indices = ParseExpressionsSeparatedByComma(
                                SymbolToken.SymbolType.OpeningSquare, SymbolToken.SymbolType.ClosingSquare);
                            expression = new IndexerExpression(scopes.Peek(), expression, indices);
                            break;

                        case SymbolToken.SymbolType.Dot:
                            operatorFound = true;
                            tokens.Read();
                            string right = ParseIdentifierName();
                            expression = new DotOperatorExpression(scopes.Peek(), expression, right);
                            break;
                    }
                }
            } while (operatorFound);

            return expression;
        }

        private Expression ParseTerm()
        {
            Token peek = tokens.Peek();

            if (peek is IdentifierToken)
            {
                return ParseIdentifier();
            }
            else if (peek is KeywordToken)
            {
                tokens.Read();
                switch (((KeywordToken)peek).Type)
                {
                    case KeywordToken.KeywordType.True:
                        return new BoolLiteralExpression(scopes.Peek(), true);

                    case KeywordToken.KeywordType.False:
                        return new BoolLiteralExpression(scopes.Peek(), false);

                    case KeywordToken.KeywordType.Now:
                        return new NowTimeExpression(scopes.Peek());

                    case KeywordToken.KeywordType.Next:
                        return new NextTimeExpression(scopes.Peek());

                    default:
                        errorsAndWarnings.AddError(new UnexpectedTokenError(peek.Position, peek));
                        return Expression.Invalid;
                }
            }
            else if (peek is IntegerLiteralToken || peek is FloatLiteralToken)
            {
                return ParseNumericLiteral();
            }
            else if (peek.IsSameSymbolTokenType(SymbolToken.SymbolType.OpeningBracket))
            {
                tokens.Read();
                Expression expression = ParseExpression();
                EnsureSymbolToken(SymbolToken.SymbolType.ClosingBracket);
                return expression;
            }
            else
            {
                tokens.Read();
                errorsAndWarnings.AddError(new UnexpectedTokenError(peek.Position, peek));
                return Expression.Invalid;
            }
        }

        private Expression ParseIdentifier()
        {
            Token token = tokens.Read();
            IdentifierToken identifierToken = token as IdentifierToken;

            if (identifierToken == null)
            {
                errorsAndWarnings.AddError(new ExpectedError(token.Position, "識別子"));
                return Expression.Invalid;
            }

            Symbol symbol = Resolve(identifierToken.Name);

            if (symbol == null)
            {
                errorsAndWarnings.AddError(new UndeclaredIdentifierError(identifierToken.Position, identifierToken));
                return Expression.Invalid;
            }
            else if (symbol is VariableSymbol)
            {
                return new VariableExpression(scopes.Peek(), (VariableSymbol)symbol);
            }
            else if (symbol is MethodSymbol)
            {
                return new FunctionExpression(scopes.Peek(), (MethodSymbol)symbol);
            }
            else
            {
                errorsAndWarnings.AddError(new UnexpectedTokenError(token.Position, token));
                return Expression.Invalid;
            }
        }

        private Expression ParseNumericLiteral()
        {
            Token token = tokens.Read();

            if (token is IntegerLiteralToken)
            {
                long value = ((IntegerLiteralToken)token).Value;
                int valueAsInt = unchecked((int)value);

                if (valueAsInt == value)
                {
                    return new ConstantInt32Expression(scopes.Peek(), valueAsInt);
                }
                else
                {
                    return new ConstantInt64Expression(scopes.Peek(), value);
                }
            }
            else if (token is FloatLiteralToken)
            {
                return new ConstantFloat64Expression(scopes.Peek(), ((FloatLiteralToken)token).Value);
            }
            else
            {
                errorsAndWarnings.AddError(new UnexpectedTokenError(token.Position, token));
                return Expression.Invalid;
            }
        }

        private Expression ParseFunctionCall(FunctionExpression function)
        {
            var parameterExpressions =
                ParseExpressionsSeparatedByComma(SymbolToken.SymbolType.OpeningBracket, SymbolToken.SymbolType.ClosingBracket);

            return new FunctionCallExpression(scopes.Peek(), function, parameterExpressions.AsReadOnly());
        }
    }
}
