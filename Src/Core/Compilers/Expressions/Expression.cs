using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Ripple.Compilers.ConstantValues;
using Ripple.Compilers.ErrorsAndWarnings;
using Ripple.Compilers.Options;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.Expressions
{
    internal abstract class Expression
    {
        internal struct _AssignmentInfo
        {
            public bool IsLeftValue { get; set; }
            public bool IsStageAssignment { get; set; }
        }

        public IScope Enclosing { get; private set; }

        private _AssignmentInfo assignmentInfo;
        internal _AssignmentInfo AssignmentInfo
        {
            get
            {
                return assignmentInfo;
            }

            set
            {
                assignmentInfo = value;
                foreach (var expr in Operands)
                {
                    expr.AssignmentInfo = value;
                }
            }
        }

        public virtual bool IsConstant
        {
            get { return Operands.All(expr => expr.IsConstant); }
        }

        public void ResolveReference(ProgramUnit unit, ErrorsAndWarningsContainer errorsAndWarnings)
        {
            foreach (var expression in Operands)
            {
                expression.ResolveReference(unit, errorsAndWarnings);
            }
        }

        public abstract TypeData ReturnType { get; }
        public abstract string ToCSharpCode(CompileOption option);

        public abstract IEnumerable<Expression> Operands { get; }

        public Expression(IScope enclosing)
        {
            this.Enclosing = enclosing;
            this.assignmentInfo = new _AssignmentInfo { IsLeftValue = false, IsStageAssignment = false };
        }

        #region 無効な式を表すExpression

        public static readonly Expression Invalid = new _InvalidExpression();

        private class _InvalidExpression : Expression
        {
            public _InvalidExpression()
                : base(null)
            { }

            public override TypeData ReturnType
            {
                get { return TypeData.Invalid; }
            }

            public override string ToCSharpCode(CompileOption option)
            {
                return string.Empty;
            }

            public override IEnumerable<Expression> Operands
            {
                get { return Enumerable.Empty<Expression>(); }
            }
        }

        #endregion
    }

    internal abstract class ZeroOperandExpression : Expression
    {
        public ZeroOperandExpression(IScope enclosing)
            : base(enclosing)
        { }

        public override IEnumerable<Expression> Operands
        {
            get
            {
                return Enumerable.Empty<Expression>();
            }
        }
    }

    internal abstract class UnaryExpression : Expression
    {
        protected readonly Expression operand;

        public UnaryExpression(IScope enclosing, Expression operand)
            : base(enclosing)
        {
            this.operand = operand;
        }

        public override IEnumerable<Expression> Operands
        {
            get
            {
                yield return operand;
            }
        }
    }

    internal abstract class BinaryExpression : Expression
    {
        protected readonly Expression left, right;

        protected abstract string CSharpOperand { get; }

        public BinaryExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing)
        {
            this.left = left;
            this.right = right;
        }

        public override IEnumerable<Expression> Operands
        {
            get
            {
                yield return left;
                yield return right;
            }
        }

        /// <summary>
        /// 左右のオペランドが型昇格したときの型を取得します。
        /// </summary>
        protected TypeData ConvertedOperandType
        {
            get
            {
                TypeData leftType = left.ReturnType;
                TypeData rightType = right.ReturnType;

                if (leftType == null || rightType == null)
                {
                    return null;
                }

                BuiltInNumericType castedLeftType = leftType as BuiltInNumericType;
                BuiltInNumericType castedRightType = rightType as BuiltInNumericType;

                if (castedLeftType != null && castedRightType != null)
                {
                    return BuiltInNumericType.LargerOf(castedLeftType, castedRightType);
                }
                else if (leftType.AsType() == rightType.AsType())
                {
                    return left.ReturnType;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return string.Format("({0}){1}({2})", left.ToCSharpCode(option), CSharpOperand, right.ToCSharpCode(option));
        }
    }

    /////////////////////////////////////////

    #region ゼロ項演算子

    internal class BoolLiteralExpression : ZeroOperandExpression
    {
        private readonly bool value;

        public BoolLiteralExpression(IScope enclosing, bool value)
            : base(enclosing)
        {
            this.value = value;
        }

        public override TypeData ReturnType
        {
            get { return BuiltInNumericType.Bool; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return value ? "true" : "false";
        }
    }

    internal class ConstantInt32Expression : ZeroOperandExpression
    {
        private readonly int value;

        public ConstantInt32Expression(IScope enclosing, int value)
            : base(enclosing)
        {
            this.value = value;
        }

        public override TypeData ReturnType
        {
            get { return BuiltInNumericType.Int32; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return value.ToString();
        }
    }

    internal class ConstantInt64Expression : ZeroOperandExpression
    {
        private readonly long value;

        public ConstantInt64Expression(IScope enclosing, long value)
            : base(enclosing)
        {
            this.value = value;
        }

        public override TypeData ReturnType
        {
            get { return BuiltInNumericType.Int64; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return value.ToString() + "L";
        }
    }

    internal class ConstantFloat64Expression : ZeroOperandExpression
    {
        private readonly double value;

        public ConstantFloat64Expression(IScope enclosing, double value)
            : base(enclosing)
        {
            this.value = value;
        }

        public override TypeData ReturnType
        {
            get { return BuiltInNumericType.Float64; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            string str = value.ToString();
            return str.Contains('.') ? str : (str + ".0");
        }
    }

    internal class NowTimeExpression : ZeroOperandExpression
    {
        public NowTimeExpression(IScope enclosing)
            : base(enclosing)
        { }

        public override bool IsConstant
        {
            get { return false; }
        }

        public override TypeData ReturnType
        {
            get { return ConstantValues.Constants.TimeType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return ConstantValues.Constants.NowVariableName;
        }
    }

    internal class NextTimeExpression : ZeroOperandExpression
    {
        public NextTimeExpression(IScope enclosing)
            : base(enclosing)
        { }

        public override bool IsConstant
        {
            get { return false; }
        }

        public override TypeData ReturnType
        {
            get { return ConstantValues.Constants.TimeType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return ConstantValues.Constants.NowVariableName + " + 1";
        }
    }

    internal class VariableExpression : ZeroOperandExpression
    {
        public VariableSymbol Variable { get; private set; }

        public VariableExpression(IScope enclosing, VariableSymbol variable)
            : base(enclosing)
        {
            this.Variable = variable;
        }

        public override bool IsConstant
        {
            get
            {
                return Variable.IsConstant;
            }
        }

        public override TypeData ReturnType
        {
            get { return Variable.Type; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            if (Variable is StageSymbol)
            {
                var assignmentInfo = this.AssignmentInfo;

                if (assignmentInfo.IsStageAssignment)
                {
                    if (assignmentInfo.IsLeftValue)
                    {
                        // nextの省略
                        return new TimeSpecifyExpression(this.Enclosing, this, new NextTimeExpression(this.Enclosing)).ToCSharpCode(option);
                    }
                    else
                    {
                        // nowの省略
                        return new TimeSpecifyExpression(this.Enclosing, this, new NowTimeExpression(this.Enclosing)).ToCSharpCode(option);
                    }
                }
                else
                {
                    if (assignmentInfo.IsLeftValue)
                    {
                        throw new Exception("時刻指定なしにステージ変数を使用することはできません。");
                    }
                    else
                    {
                        // nowの省略
                        return new TimeSpecifyExpression(this.Enclosing, this, new NowTimeExpression(this.Enclosing)).ToCSharpCode(option);
                    }
                }
            }
            else if (Variable is ParameterSymbol)
            {
                var asParameterSymbol = Variable as ParameterSymbol;
                if (asParameterSymbol.IsCompiledAsMethod)
                {
                    return "@" + Variable.ToCSharpName(option) + "(" + Constants.NowVariableName + ")";
                }
                else
                {
                    return "@" + Variable.ToCSharpName(option);
                }
            }
            else
            {
                return "@" + Variable.ToCSharpName(option);
            }
        }
    }

    internal class FunctionExpression : ZeroOperandExpression
    {
        public MethodSymbol Method { get; private set; }

        public FunctionExpression(IScope enclosing, MethodSymbol method)
            : base(enclosing)
        {
            this.Method = method;
        }

        public override TypeData ReturnType
        {
            get { throw new NotImplementedException(); }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return Method.ToCSharpFullName(option);
        }
    }

    internal class FunctionCallExpression : Expression
    {
        private readonly FunctionExpression function;
        private readonly IList<Expression> parameters;

        public FunctionCallExpression(IScope enclosing, FunctionExpression function, IList<Expression> paramaters)
            : base(enclosing)
        {
            this.function = function;
            this.parameters = paramaters;
        }

        public override bool IsConstant
        {
            get { return false; }
        }

        public override TypeData ReturnType
        {
            get { return function.Method.Type; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            StringBuilder sb = new StringBuilder();

            // 関数名と開き括弧
            sb.Append(function.ToCSharpCode(option) + '(');

            // 引数
            for (int i = 0; i < parameters.Count; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }
                sb.Append(parameters[i].ToCSharpCode(option));
            }

            // 閉じ括弧
            sb.Append(')');

            return sb.ToString();
        }

        public sealed override IEnumerable<Expression> Operands
        {
            get
            {
                return parameters;
            }
        }
    }

    #endregion

    #region 単項演算子

    internal class NegateExpression : UnaryExpression
    {
        public NegateExpression(IScope enclosing, Expression operand)
            : base(enclosing, operand)
        { }

        public override TypeData ReturnType
        {
            get { return operand.ReturnType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return string.Format("-({0})", operand.ToCSharpCode(option));
        }
    }

    internal class LogicalNotExpression : UnaryExpression
    {
        public LogicalNotExpression(IScope enclosing, Expression operand)
            : base(enclosing, operand)
        { }

        public override TypeData ReturnType
        {
            get { return operand.ReturnType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return "!(" + operand.ToCSharpCode(option) + ")";
        }
    }

    internal class IncrementDecrementExpression : UnaryExpression
    {
        public enum Kind
        {
            Increment, Decrement,
        }

        public enum OperatorPosition
        {
            Left, Right
        }

        private readonly Kind kind;
        private readonly OperatorPosition operandPosition;

        public IncrementDecrementExpression(IScope enclosing, Expression operand, Kind kind, OperatorPosition operandPosition)
            : base(enclosing, operand)
        {
            this.kind = kind;
            this.operandPosition = operandPosition;
        }

        public override TypeData ReturnType
        {
            get { return operand.ReturnType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            string operatorString = kind == Kind.Increment ? "++" : "--";

            if (operandPosition == OperatorPosition.Left)
            {
                return operatorString + "(" + operand.ToCSharpCode(option) + ")";
            }
            else
            {
                return "(" + operand.ToCSharpCode(option) + ")" + operatorString;
            }
        }
    }

    internal class TimeSpecifyExpression : UnaryExpression
    {
        private readonly Expression time;

        public TimeSpecifyExpression(IScope enclosing, VariableExpression operand, Expression time)
            : base(enclosing, operand)
        {
            this.time = time;
        }

        public override TypeData ReturnType
        {
            get { return ((VariableExpression)operand).ReturnType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            var stage = (StageSymbol)((VariableExpression)operand).Variable;

            // キャッシュされていないか確認する
            if (time is NowTimeExpression || time is NextTimeExpression)
            {
                int offsetFromNow = time is NowTimeExpression ? 0 : 1;

                string cachedVariableName = stage.GetCachedVariableNameIfCached(offsetFromNow, option);

                if (cachedVariableName != null)
                {
                    return "@" + cachedVariableName;
                }
            }

            string timeSpecifyCode = "[" + stage.StageHoldState.GetTimeSpecifierCode(time.ToCSharpCode(option)) + "]";

            return "(" + "@" + stage.ToCSharpName(option) + ")" + timeSpecifyCode;
        }

        public override IEnumerable<Expression> Operands
        {
            get
            {
                return base.Operands.Concat(new[] { time });
            }
        }
    }

    internal class IndexerExpression : UnaryExpression
    {
        public IList<Expression> Indices { get; private set; }

        public IndexerExpression(IScope enclosing, Expression operand, IList<Expression> indices)
            : base(enclosing, operand)
        {
            this.Indices = indices;
        }

        public override TypeData ReturnType
        {
            get
            {
                var arrayTypeData = operand.ReturnType as ArrayType;

                if (arrayTypeData != null)
                {
                    return arrayTypeData.BaseElementType;
                }
                else
                {
                    return null;
                }
            }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            var arrayTypeData = operand.ReturnType as ArrayType;
            if (arrayTypeData == null)
                throw new Exception("配列でない型です。");

            return operand.ToCSharpCode(option) + arrayTypeData.CreateArrayIndexerCode(Indices, option);
        }

        public override IEnumerable<Expression> Operands
        {
            get
            {
                return base.Operands.Concat(Indices);
            }
        }
    }

    internal class DotOperatorExpression : UnaryExpression
    {
        private readonly string rightIdentifier;

        public DotOperatorExpression(IScope enclosing, Expression operand, string rightIdentifier)
            : base(enclosing, operand)
        {
            this.rightIdentifier = rightIdentifier;
        }

        public override TypeData ReturnType
        {
            get
            {
                if (operand != null && operand.ReturnType is StructType)
                {
                    var structType = operand.ReturnType as StructType;
                    var field = structType.GetField(rightIdentifier);
                    return field.Type;
                }
                else
                {
                    return null;
                }
            }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return "(" + operand.ToCSharpCode(option) + ")." + rightIdentifier;
        }
    }

    #endregion

    #region 二項演算子

    internal class AssignmentExpression : BinaryExpression
    {
        public AssignmentExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        public override TypeData ReturnType
        {
            get { return left.ReturnType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return left.ToCSharpCode(option) + "=(" + right.ToCSharpCode(option) + ")";
        }

        protected override string CSharpOperand
        {
            get { return "="; }
        }
    }

    internal abstract class ArithmeticExpression : BinaryExpression
    {
        public ArithmeticExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        public override TypeData ReturnType
        {
            get { return this.ConvertedOperandType; }
        }
    }

    internal class AddExpression : ArithmeticExpression
    {
        public AddExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "+"; } }
    }

    internal class SubExpression : ArithmeticExpression
    {
        public SubExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "-"; } }
    }

    internal class MultExpression : ArithmeticExpression
    {
        public MultExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "*"; } }
    }

    internal class DivExpression : ArithmeticExpression
    {
        public DivExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        public override TypeData ReturnType
        {
            get { return BuiltInNumericType.Float64; }
        }

        protected override string CSharpOperand
        {
            get { Debug.Assert(false); return "/"; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return string.Format("(({0})({1}))/(({0})({2}))",
                BuiltInNumericType.Float64.ToCSharpCode(option), left.ToCSharpCode(option), right.ToCSharpCode(option));
        }
    }

    internal class IdivExpression : ArithmeticExpression
    {
        public IdivExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "/"; } }
    }

    internal class ModExpression : ArithmeticExpression
    {
        public ModExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "%"; } }
    }

    internal class LogicalAndExpression : BinaryExpression
    {
        public LogicalAndExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        public override TypeData ReturnType
        {
            get { return BuiltInNumericType.Bool; }
        }

        protected override string CSharpOperand { get { return "&&"; } }
    }

    internal class LogicalOrExpression : BinaryExpression
    {
        public LogicalOrExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        public override TypeData ReturnType
        {
            get { return BuiltInNumericType.Bool; }
        }

        protected override string CSharpOperand { get { return "||"; } }
    }

    #endregion

    #region 三項演算子

    internal class ConditionExpression : Expression
    {
        private readonly Expression cond;
        private readonly Expression ifTrue, ifFalse;

        public ConditionExpression(IScope enclosing, Expression cond, Expression ifTrue, Expression ifFalse)
            : base(enclosing)
        {
            this.cond = cond;
            this.ifTrue = ifTrue;
            this.ifFalse = ifFalse;
        }

        public override TypeData ReturnType
        {
            get { return ifTrue.ReturnType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return string.Format("({0})?({1}):({2})",
                cond.ToCSharpCode(option), ifTrue.ToCSharpCode(option), ifFalse.ToCSharpCode(option));
        }

        public sealed override IEnumerable<Expression> Operands
        {
            get
            {
                yield return cond;
                yield return ifTrue;
                yield return ifFalse;
            }
        }
    }

    #endregion

    #region 比較演算子

    internal abstract class ComparisonExpression : BinaryExpression
    {
        public ComparisonExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }


        public override TypeData ReturnType
        {
            get { return BuiltInNumericType.Bool; }
        }
    }

    internal class LessThanExpression : ComparisonExpression
    {
        public LessThanExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "<"; } }
    }

    internal class LessThanOrEqualsExpression : ComparisonExpression
    {
        public LessThanOrEqualsExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "<="; } }
    }

    internal class GreaterThanExpression : ComparisonExpression
    {
        public GreaterThanExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return ">"; } }
    }

    internal class GreaterThanOrEqualsExpression : ComparisonExpression
    {
        public GreaterThanOrEqualsExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return ">="; } }
    }

    internal class EqualsExpression : ComparisonExpression
    {
        public EqualsExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "=="; } }
    }

    internal class NotEqualsExpression : ComparisonExpression
    {
        public NotEqualsExpression(IScope enclosing, Expression left, Expression right)
            : base(enclosing, left, right)
        { }

        protected override string CSharpOperand { get { return "!="; } }
    }

    #endregion

    #region 型変換演算子

    internal class ConvertExpression : Expression
    {
        private readonly Expression expression;
        private readonly TypeData targetType;

        public ConvertExpression(IScope enclosing, Expression expression, TypeData targetType)
            : base(enclosing)
        {
            this.expression = expression;
            this.targetType = targetType;
        }

        public override TypeData ReturnType
        {
            get { return targetType; }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return string.Format("({0})({1})", targetType.ToCSharpCode(option), expression.ToCSharpCode(option));
        }

        public override IEnumerable<Expression> Operands
        {
            get
            {
                yield return expression;
            }
        }
    }

    #endregion
}
