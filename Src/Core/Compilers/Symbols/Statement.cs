using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ripple.Compilers.CodeGenerations.CSharp;
using Ripple.Compilers.ConstantValues;
using Ripple.Compilers.Exceptions;
using Ripple.Compilers.Expressions;
using Ripple.Compilers.Options;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.Symbols
{
    internal abstract class Statement : ISyntaxNode
    {
        public abstract string ToCSharpCode(CompileOption option);
        public abstract IEnumerable<ISyntaxNode> Children { get; }
    }

    internal class BlockStatement : Statement, IScope
    {
        #region フィールドやプロパティ

        private Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();

        private readonly List<Statement> statements = new List<Statement>();
        public IReadOnlyList<Statement> Statements
        {
            get { return statements.AsReadOnly(); }
        }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Enumerable.Concat<ISyntaxNode>(Statements, symbols.Values); }
        }

        public IScope Enclosing { get; private set; }

        #endregion

        public BlockStatement(IScope enclosing)
        {
            this.Enclosing = enclosing;
        }

        internal void AddStatement(Statement statement)
        {
            this.statements.Add(statement);
        }

        public override string ToCSharpCode(CompileOption option)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");

            foreach (var s in statements)
            {
                sb.AppendLine(s.ToCSharpCode(option));
            }

            sb.Append("}");

            return sb.ToString();
        }

        #region シンボルの追加・削除

        public void Define(Symbol symbol)
        {
            try
            {
                symbols.Add(symbol.Name, symbol);
            }
            catch (ArgumentException e)
            {
                throw new SymbolAlreadyExistsException(symbol.Name, e);
            }
        }

        public Symbol Resolve(string name)
        {
            try
            {
                return symbols[name];
            }
            catch (KeyNotFoundException)
            {
                return Enclosing.Resolve(name);
            }
        }

        #endregion
    }

    internal class IfStatement : Statement
    {
        public Expression Condition { get; private set; }
        public BlockStatement IfTrue { get; private set; }
        public BlockStatement IfFalse { get; private set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return new ISyntaxNode[] { IfTrue, IfFalse }; }
        }

        public IfStatement(Expression condition, BlockStatement ifTrue, BlockStatement ifFalse)
        {
            this.Condition = condition;
            this.IfTrue = ifTrue;
            this.IfFalse = ifFalse;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("if (" + Condition.ToCSharpCode(option) + ")");
            sb.AppendLine(IfTrue.ToCSharpCode(option));
            if (IfFalse != null)
            {
                BlockStatement blockScope = IfFalse as BlockStatement;

                if (blockScope != null && blockScope.Statements.Count == 1 && blockScope.Statements[0] is IfStatement)
                {
                    // else のあとにifだけが続く
                    sb.Append("else " + blockScope.Statements[0].ToCSharpCode(option));
                }
                else
                {
                    sb.Append("else " + IfFalse.ToCSharpCode(option));
                }
            }

            return sb.ToString();
        }
    }

    internal class WhileStatement : Statement
    {
        public Expression Condition { get; private set; }
        public BlockStatement Content { get; private set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return new[] { Content }; }
        }

        public WhileStatement(Expression condition, BlockStatement content)
        {
            this.Condition = condition;
            this.Content = content;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return "while (" + Condition.ToCSharpCode(option) + ")" + Environment.NewLine
                + Content.ToCSharpCode(option);
        }
    }

    internal class BlockStatementInEachAt : BlockStatement
    {
        public EachAtStatement EachAtStatement { get; private set; }

        public BlockStatementInEachAt(IScope enclosing, EachAtStatement eachAtStatement)
            : base(enclosing)
        {
            this.EachAtStatement = eachAtStatement;
        }
    }

    internal class EachAtStatement : Statement
    {
        public IScope Enclosing { get; set; }
        public IList<LocalVariableSymbol> Indices { get; private set; }
        public Expression IndexableObject { get; private set; }
        public bool IsParallel { get; private set; }

        public BlockStatementInEachAt BlockStatement { get; private set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Indices.Concat<ISyntaxNode>(new ISyntaxNode[] { BlockStatement }); }
        }

        public EachAtStatement(IScope enclosing, IList<string> indices, Expression indexableObject, bool isParallel)
        {
            // ブロックステートメントの生成
            this.BlockStatement = new BlockStatementInEachAt(enclosing, this);

            // フィールド初期化
            this.Enclosing = enclosing;
            this.Indices = indices.Select(name => new LocalVariableSymbol(name, null, this.BlockStatement)
            {
                Type = BuiltInNumericType.Int32,
            }).ToList();
            this.IndexableObject = indexableObject;
            this.IsParallel = isParallel;

            // ブロックステートメントにdefine
            foreach (var index in Indices)
            {
                this.BlockStatement.Define(index);
            }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            // TODO : IndexableObjectがインデックスをサポートするかのエラーチェック

            string objectVariableName = IndexableObject.ToCSharpCode(option);
            var indexInfos = IndexableObject.ReturnType.GetIndexInfos(objectVariableName, Indices.Select(i => i.ToCSharpName(option)));
            return InternalToCSharpCode(indexInfos, 0, option);
        }

        private string InternalToCSharpCode(IList<TypeData.IndexInfo> indexInfos, int beginIndex, CompileOption option)
        {
            if (beginIndex >= indexInfos.Count)
            {
                return BlockStatement.ToCSharpCode(option);
            }

            string indexName = "@" + Indices[beginIndex].ToCSharpName(option);
            string lower = indexInfos[beginIndex].Begin, upper = indexInfos[beginIndex].End;
            if (!indexInfos[beginIndex].BeginIncluded)
                lower = "(" + lower + ") + 1";
            if (indexInfos[beginIndex].EndIncluded)
                upper = "{" + upper + ") + 1";

            bool useParallelFor =
                beginIndex == 0
                && (option.ParallelizationOption & ParallelizationOption.InParallelSpecifiedCode) != 0
                && this.IsParallel;

            return CSharpCodeGenHelper.CreateSimpleForStatement(
                Constants.IndexerType.ToCSharpCode(option), indexName, lower, upper,
                InternalToCSharpCode(indexInfos, beginIndex + 1, option), useParallelFor);
        }
    }

    internal class BlockStatementInFor : BlockStatement
    {
        public ForStatement ForStatement { get; private set; }

        public BlockStatementInFor(IScope enclosing, ForStatement forStatement)
            : base(enclosing)
        {
            this.ForStatement = forStatement;
        }
    }

    internal class ForStatement : Statement
    {
        public VariableSymbol Indexer { get; private set; }
        public Expression IndexerInitialValue { get; private set; }
        public Expression IndexerFinalValue { get; private set; }
        public bool IsParallel { get; private set; }

        public BlockStatementInFor BlockStatement { get; private set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return new ISyntaxNode[] { Indexer, BlockStatement }; }
        }

        public ForStatement(IScope enclosing, string indexerName, Expression indexerInitialValue, Expression indexerFinalValue, bool isParallel)
        {
            // ブロックステートメントの生成
            this.BlockStatement = new BlockStatementInFor(enclosing, this);

            this.Indexer = new LocalVariableSymbol(indexerName, indexerInitialValue, this.BlockStatement)
            {
                Type = Constants.IndexerType
            };
            this.IndexerInitialValue = indexerInitialValue;
            this.IndexerFinalValue = indexerFinalValue;
            this.IsParallel = isParallel;

            // ブロックステートメントにdefine
            this.BlockStatement.Define(Indexer);
        }

        public override string ToCSharpCode(CompileOption option)
        {
            bool useParallelFor =
                (option.ParallelizationOption & ParallelizationOption.InParallelSpecifiedCode) != 0 && this.IsParallel;

            if (useParallelFor)
            {
                return CodeGenerations.CSharp.CSharpCodeGenHelper.CreateSimpleForStatement(
                    Indexer.Type.ToCSharpCode(option), "@" + Indexer.ToCSharpName(option),
                    IndexerInitialValue.ToCSharpCode(option), "(" + IndexerFinalValue.ToCSharpCode(option) + ") + 1",
                    BlockStatement.ToCSharpCode(option), true);
            }
            else
            {
                return string.Format("for ({0} @{1} = {2}; @{1} <= {3}; @{1}++)",
                                        Indexer.Type.ToCSharpCode(option),
                                        Indexer.ToCSharpName(option),
                                        IndexerInitialValue.ToCSharpCode(option),
                                        IndexerFinalValue.ToCSharpCode(option))
                    + BlockStatement.ToCSharpCode(option);
            }
        }
    }

    internal class ReturnOnlyBlockStatement : BlockStatement
    {
        public Expression Expression { get; private set; }

        public ReturnOnlyBlockStatement(IScope enclosing, Expression expression)
            : base(enclosing)
        {
            this.Expression = expression;
            base.AddStatement(new ReturnStatement(expression));
        }
    }

    internal class VariableDeclarationStatement : Statement
    {
        private readonly VariableSymbol variableSymbol;
        private readonly Expression initialValue;

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Enumerable.Empty<ISyntaxNode>(); }
        }

        public VariableDeclarationStatement(VariableSymbol variableSymbol, Expression initialValue)
        {
            this.variableSymbol = variableSymbol;
            this.initialValue = initialValue;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return variableSymbol.Type.ToCSharpCode(option) + " " + variableSymbol.ToCSharpName(option)
                + " = (" + initialValue.ToCSharpCode(option) + ");";
        }
    }

    internal class ReturnStatement : Statement
    {
        public Expression Expression { get; private set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Enumerable.Empty<ISyntaxNode>(); }
        }

        public ReturnStatement(Expression expression)
        {
            this.Expression = expression;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            if (Expression == null)
            {
                return "return;";
            }
            else if (Expression.ReturnType == BuiltInNumericType.Nothing)
            {
                return Expression.ToCSharpCode(option) + ";" + Environment.NewLine + "return;";
            }
            else
            {
                return "return " + Expression.ToCSharpCode(option) + ";";
            }
        }
    }

    internal class BreakStatement : Statement
    {
        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Enumerable.Empty<ISyntaxNode>(); }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return "break;";
        }
    }

    internal class ContinueStatement : Statement
    {
        public bool IsCompiledToBeReturn { get; private set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Enumerable.Empty<ISyntaxNode>(); }
        }

        public ContinueStatement(bool isCompiledToBeReturn)
        {
            this.IsCompiledToBeReturn = isCompiledToBeReturn;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return IsCompiledToBeReturn && (option.ParallelizationOption & ParallelizationOption.InParallelSpecifiedCode) != 0
                ? "return;" : "continue;";
        }
    }

    internal class ExpressionStatement : Statement
    {
        public Expression Expression { get; private set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Enumerable.Empty<ISyntaxNode>(); }
        }

        public ExpressionStatement(Expression expression)
        {
            this.Expression = expression;
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return Expression.ToCSharpCode(option) + ";";
        }
    }
}
