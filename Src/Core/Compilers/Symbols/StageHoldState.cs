using System;
using System.Linq;
using Ripple.Compilers.CodeGenerations.CSharp;
using Ripple.Compilers.ConstantValues;
using Ripple.Compilers.Options;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.Symbols
{
    internal abstract class StageHoldState
    {
        public TypeData Type { get; private set; }
        public StageSymbol StageSymbol { get; private set; }

        public StageHoldState(TypeData type, StageSymbol stageSymbol)
        {
            this.Type = type;
            this.StageSymbol = stageSymbol;
        }

        public abstract string NumNeededAllocationCode { get; }

        /// <summary>
        /// 時刻を確保するために必要な配列の生成コードを取得します。
        /// このコードは、__Initialize()メソッド内で使用されます。
        /// </summary>
        public string GetInitializeAllocationCode(CompileOption option)
        {
            string newCode;

            if (this.Type is ArrayType)
            {
                var array = (ArrayType)Type;
                newCode = "new " + array.BaseElementType.ToCSharpCode(option)
                    + "[" + NumNeededAllocationCode + "]" + string.Concat(Enumerable.Repeat("[]", array.NumDimensions));
            }
            else
            {
                newCode = "new " + Type.ToCSharpCode(option) + "[" + NumNeededAllocationCode + "]";
            }

            return "@" + StageSymbol.ToCSharpName(option) + " = " + newCode + ";";
        }

        /// <summary>
        /// 次の時刻に移るときに行うべき操作のコードを取得します。
        /// このコードは、__MoveNext()メソッド内で使用されます。
        /// </summary>
        public abstract string GetMoveNextAllocationCode(CompileOption option);

        public abstract string GetTimeSpecifierCode(string time);
    }

    internal class AllStageHoldedState : StageHoldState
    {
        public AllStageHoldedState(TypeData type, StageSymbol stageSymbol)
            : base(type, stageSymbol)
        { }

        public override string NumNeededAllocationCode
        {
            get
            {
                return Constants.MaxTimeVariableName + " + 1";
            }
        }

        public override string GetMoveNextAllocationCode(CompileOption option)
        {
            return Type.ToCSharpObjectNewAndAssignCode("@" + StageSymbol.ToCSharpName(option) + "[" + Constants.NowVariableName + " + 1]", option);
        }

        public override string GetTimeSpecifierCode(string time)
        {
            return time;
        }
    }

    internal class PartialyStageHoldState : StageHoldState
    {
        public int NumHolds { get; private set; }

        public PartialyStageHoldState(TypeData type, StageSymbol stageSymbol, int numHolds)
            : base(type, stageSymbol)
        {
            this.NumHolds = numHolds;
        }

        public override string NumNeededAllocationCode
        {
            get
            {
                return (NumHolds + 1).ToString();
            }
        }

        public override string GetTimeSpecifierCode(string time)
        {
            return "(" + time + ")" + "%" + (NumHolds + 1).ToString();
        }

        public override string GetMoveNextAllocationCode(CompileOption option)
        {
            string allocationCode = Type.ToCSharpObjectNewAndAssignCode(
                "@" + StageSymbol.ToCSharpName(option)
                + "["
                + GetTimeSpecifierCode(Constants.NowVariableName + " + 1")
                + "]", option);

            if (Type.IsObjectReusable)
            {
                return string.Format("if ({0} <= {1})", Constants.NowVariableName, NumHolds)
                    + Environment.NewLine + "{"
                    + allocationCode
                    + Environment.NewLine + "}";
            }
            else
            {
                return allocationCode;
            }
        }
    }
}
