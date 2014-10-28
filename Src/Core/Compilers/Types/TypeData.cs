using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Ripple.Compilers.Expressions;
using Ripple.Compilers.Options;
using Ripple.Compilers.Symbols;

namespace Ripple.Compilers.Types
{
    public abstract class TypeData
    {
        public struct IndexInfo
        {
            public string Begin { get; set; }
            public string End { get; set; }
            public bool BeginIncluded { get; set; }
            public bool EndIncluded { get; set; }
        }

        public abstract string RippleName { get; }

        internal abstract bool IsObjectNewNeeded { get; }

        internal abstract bool IsSimpleNewSupported { get; }

        public abstract bool IsObjectReusable { get; }

        public virtual bool IsIndicesSupported
        {
            get { return false; }
        }

        public virtual IList<IndexInfo> GetIndexInfos(string variableName, IEnumerable<string> indices)
        {
            throw new NotSupportedException("インデックスアクセスはサポートされていません。");
        }

        public virtual string ToCSharpCode(CompileOption option)
        {
            return AsType().ToString();
        }

        internal virtual string ToCSharpObjectNewCode(CompileOption option, IList<string> parameters = null)
        {
            if (IsSimpleNewSupported)
            {
                return "new " + ToCSharpCode(option) + "()";
            }
            else
            {
                throw new Exception("newはサポートされていない");
            }
        }

        internal virtual string ToCSharpObjectNewAndAssignCode(string destVariableName, CompileOption option, IList<string> parameters = null)
        {
            return destVariableName + " = " + ToCSharpObjectNewCode(option, parameters) + ";";
        }

        /// <summary>
        /// 現在の型をSystem.Typeオブジェクトとして返します。 
        /// </summary>
        public abstract Type AsType();

        internal static TypeData FromType(Type type)
        {
            if (type == typeof(void))
            {
                return TypeData.Nothing;
            }
            else if (type == typeof(Boolean))
            {
                return BuiltInNumericType.Bool;
            }
            else if (type == typeof(Int32))
            {
                return BuiltInNumericType.Int32;
            }
            else if (type == typeof(Int64))
            {
                return BuiltInNumericType.Int64;
            }
            else if (type == typeof(Double))
            {
                return BuiltInNumericType.Float64;
            }
            else
            {
                return new VariantTypeData(type);
            }
        }

        #region 任意の型を格納できるTypeData

        private class VariantTypeData : TypeData
        {
            private readonly Type type;

            public VariantTypeData(Type type)
            {
                this.type = type;
            }

            public override string RippleName
            {
                get { return type.ToString(); }
            }

            internal override bool IsObjectNewNeeded
            {
                get { return true; }
            }

            internal override bool IsSimpleNewSupported
            {
                get { return false; }
            }

            public override bool IsObjectReusable
            {
                get { return false; }
            }

            public override Type AsType()
            {
                return type;
            }
        }

        #endregion

        #region 組み込み型

        /// <summary>
        /// 戻り値がないことを示すTypeDataです。
        /// </summary>
        public static readonly TypeData Nothing = new NothingType();

        private class NothingType : TypeData
        {
            public override Type AsType()
            {
                return typeof(void);
            }

            public override string RippleName
            {
                get { return "nothing"; }
            }

            internal override bool IsObjectNewNeeded
            {
                get { return false; }
            }

            public override bool IsObjectReusable
            {
                get { return true; }
            }

            internal override bool IsSimpleNewSupported
            {
                get { throw new NotImplementedException(); }
            }

            public override bool Equals(object obj)
            {
                return obj is NothingType;
            }

            public override int GetHashCode()
            {
                if (this is NothingType)
                    return 0;
                else
                    return base.GetHashCode();
            }

            public override string ToCSharpCode(CompileOption option)
            {
                return "void";
            }
        }

        #endregion

        #region 無効な方を表すTypeData

        public static readonly TypeData Invalid = new _InvalidType();

        private class _InvalidType : TypeData
        {
            public override Type AsType()
            {
                return null;
            }

            public override string RippleName
            {
                get { return "@InvalidType"; }
            }

            internal override bool IsObjectNewNeeded
            {
                get { return false; }
            }

            public override bool IsObjectReusable
            {
                get { return false; /* 意味が無い(どちらでもよい) */ }
            }

            internal override bool IsSimpleNewSupported
            {
                get { return false; /* 意味が無い(どちらでもよい) */ }
            }
        }

        #endregion
    }

    /// <summary>
    /// 組み込みの数値型を表します。
    /// </summary>
    public abstract class BuiltInNumericType : TypeData, IComparable<BuiltInNumericType>
    {
        /// <summary>
        /// 現在の<typeparamref name="TypeData"/>が数値型であるかを示す値を取得します。
        /// </summary>
        public bool IsNumeric
        {
            get { return true; }
        }

        internal override bool IsObjectNewNeeded
        {
            get { return false; }
        }

        public override bool IsObjectReusable
        {
            get { return true; }
        }

        /// <summary>
        /// 現在の<typeparamref name="TypeData"/>が符号あり整数型であるかを示す値を取得します。
        /// </summary>
        public abstract bool IsSignedInteger { get; }

        /// <summary>
        /// 現在の<typeparamref name="TypeData"/>が符号なし整数型であるかを示す値を取得します。
        /// </summary>
        public abstract bool IsUnsignedInteger { get; }

        protected abstract int TypeOrder { get; }

        internal override bool IsSimpleNewSupported
        {
            get { return true; }
        }

        internal override string ToCSharpObjectNewCode(CompileOption option, IList<string> parameters = null)
        {
            return "default(" + ToCSharpCode(option) + ")";
        }

        public int CompareTo(BuiltInNumericType other)
        {
            return this.TypeOrder.CompareTo(other.TypeOrder);
        }

        public override bool Equals(object obj)
        {
            BuiltInNumericType casted = obj as BuiltInNumericType;
            return casted != null && this.TypeOrder == casted.TypeOrder;
        }

        public override int GetHashCode()
        {
            return TypeOrder;
        }

        protected enum TypeOrders
        {
            Bool,
            SByte,
            UByte,
            Int32,
            Int64,
            Bigint,
            Float64,
        }

        public static BuiltInNumericType LargerOf(BuiltInNumericType a, BuiltInNumericType b)
        {
            return a >= b ? a : b;
        }

        public static BuiltInNumericType SmallerOf(BuiltInNumericType a, BuiltInNumericType b)
        {
            return a < b ? a : b;
        }

        #region 演算子

        public static bool operator <(BuiltInNumericType a, BuiltInNumericType b)
        {
            return a.CompareTo(b) < 0;
        }

        public static bool operator <=(BuiltInNumericType a, BuiltInNumericType b)
        {
            return a.CompareTo(b) <= 0;
        }

        public static bool operator >=(BuiltInNumericType a, BuiltInNumericType b)
        {
            return a.CompareTo(b) >= 0;
        }

        public static bool operator >(BuiltInNumericType a, BuiltInNumericType b)
        {
            return a.CompareTo(b) > 0;
        }

        public static bool operator ==(BuiltInNumericType a, BuiltInNumericType b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            return a.CompareTo(b) == 0;
        }

        public static bool operator !=(BuiltInNumericType a, BuiltInNumericType b)
        {
            return !(a == b);
        }

        #endregion

        #region 定数

        public readonly static BuiltInNumericType Bool = new BoolType();

        public readonly static BuiltInNumericType SByte = new SByteType();

        public readonly static BuiltInNumericType UByte = new UByteType();

        public readonly static BuiltInNumericType Int32 = new Int32Type();

        public readonly static BuiltInNumericType Int64 = new Int64Type();

        public readonly static BuiltInNumericType Float64 = new Float64Type();

        #endregion

        #region 具象クラス

        private class BoolType : BuiltInNumericType
        {
            public override bool IsSignedInteger
            {
                get { return false; }
            }

            public override bool IsUnsignedInteger
            {
                get { return false; }
            }

            public override Type AsType()
            {
                return typeof(bool);
            }

            public override string RippleName
            {
                get { return "bool"; }
            }

            protected override int TypeOrder
            {
                get { return (int)TypeOrders.Bool; }
            }
        }

        private class SByteType : BuiltInNumericType
        {
            public override bool IsSignedInteger
            {
                get { return true; }
            }

            public override bool IsUnsignedInteger
            {
                get { return false; }
            }

            public override Type AsType()
            {
                return typeof(SByte);
            }

            public override string RippleName
            {
                get { return "sbyte"; }
            }

            protected override int TypeOrder
            {
                get { return (int)TypeOrders.SByte; }
            }
        }

        private class UByteType : BuiltInNumericType
        {
            public override bool IsSignedInteger
            {
                get { return false; }
            }

            public override bool IsUnsignedInteger
            {
                get { return true; }
            }

            public override Type AsType()
            {
                return typeof(Byte);
            }

            public override string RippleName
            {
                get { return "ubyte"; }
            }

            protected override int TypeOrder
            {
                get { return (int)TypeOrders.UByte; }
            }
        }

        private class Int32Type : BuiltInNumericType
        {
            public override bool IsSignedInteger
            {
                get { return true; }
            }

            public override bool IsUnsignedInteger
            {
                get { return false; }
            }

            public override Type AsType()
            {
                return typeof(Int32);
            }

            public override string RippleName
            {
                get { return "int"; }
            }

            protected override int TypeOrder
            {
                get { return (int)TypeOrders.Int32; }
            }
        }

        private class Int64Type : BuiltInNumericType
        {
            public override bool IsSignedInteger
            {
                get { return true; }
            }

            public override bool IsUnsignedInteger
            {
                get { return false; }
            }

            public override Type AsType()
            {
                return typeof(Int64);
            }

            public override string RippleName
            {
                get { return "long"; }
            }

            protected override int TypeOrder
            {
                get { return (int)TypeOrders.Int64; }
            }
        }

        private class Float64Type : BuiltInNumericType
        {
            public override bool IsSignedInteger
            {
                get { return false; }
            }

            public override bool IsUnsignedInteger
            {
                get { return false; }
            }

            public override Type AsType()
            {
                return typeof(Double);
            }

            public override string RippleName
            {
                get { return "float"; }
            }

            protected override int TypeOrder
            {
                get { return (int)TypeOrders.Float64; }
            }
        }

        private class BigintType : BuiltInNumericType
        {
            public override bool IsSignedInteger
            {
                get { return true; }
            }

            public override bool IsUnsignedInteger
            {
                get { return false; }
            }

            public override Type AsType()
            {
                return typeof(BigInteger);
            }

            public override string RippleName
            {
                get { return "bigint"; }
            }

            protected override int TypeOrder
            {
                get { return (int)TypeOrders.Bigint; }
            }
        }

        #endregion

    }

    public class ArrayType : TypeData
    {
        public TypeData ElementType { get; private set; }
        internal Expression NumElements { get; private set; }
        public int NumDimensions { get; private set; }

        internal ArrayType(TypeData elementType, Expression numElements)
        {
            this.ElementType = elementType;
            this.NumElements = numElements;

            if (elementType is ArrayType)
            {
                this.NumDimensions = ((ArrayType)elementType).NumDimensions + 1;
            }
            else
            {
                this.NumDimensions = 1;
            }
        }

        public TypeData BaseElementType
        {
            get
            {
                if (ElementType is ArrayType)
                {
                    return ((ArrayType)ElementType).BaseElementType;
                }
                else
                {
                    return ElementType;
                }
            }
        }

        public override string RippleName
        {
            get { return ElementType.RippleName + "[" + new string(',', NumDimensions - 1) + "]"; }
        }

        internal override bool IsObjectNewNeeded
        {
            get { return true; }
        }

        internal override bool IsSimpleNewSupported
        {
            get
            {
                return !ElementType.IsObjectNewNeeded;
            }
        }

        public override bool IsObjectReusable
        {
            get { return ElementType.IsObjectReusable && NumElements.IsConstant; }
        }

        public override bool IsIndicesSupported
        {
            get { return true; }
        }

        public override IList<TypeData.IndexInfo> GetIndexInfos(string variableName, IEnumerable<string> indices)
        {
            IndexInfo concat = new IndexInfo()
            {
                Begin = "0",
                End = string.Concat(new[] { variableName }.Concat(indices.Take(NumDimensions - 1).Select(i => "[" + i + "]"))) + ".Length",
                BeginIncluded = true,
                EndIncluded = false,
            };

            if (ElementType is ArrayType)
            {
                return ((ArrayType)ElementType).GetIndexInfos(variableName, indices).Concat(new[] { concat }).ToList();
            }
            else
            {
                return new[] { concat };
            }
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return ElementType.ToCSharpCode(option) + "[]";
        }

        internal override string ToCSharpObjectNewCode(CompileOption option, IList<string> parameters = null)
        {
            return "new " + ElementType.ToCSharpCode(option) + "[" + NumElements.ToCSharpCode(option) + "]";
        }

        internal override string ToCSharpObjectNewAndAssignCode(string destVariableName, CompileOption option, IList<string> parameters = null)
        {
            if (IsSimpleNewSupported)
            {
                return base.ToCSharpObjectNewAndAssignCode(destVariableName, option, parameters);
            }
            else
            {
                // 配列オブジェクト生成のコード
                string objectNewCode = string.Format("{0} = new {1}[{2}]{3};",
                    destVariableName,
                    BaseElementType.ToCSharpCode(option),
                    NumElements.ToCSharpCode(option),
                    RepeatString("[]", NumDimensions - 1));

                // 初期化コード
                string temporaryIndexerName =
                    CodeGenerations.CSharp.TemporaryVariableNameFactory.GetTemporaryVariableName();
                string initializeCode = CodeGenerations.CSharp.CSharpCodeGenHelper.CreateSimpleForStatement(
                    "int", temporaryIndexerName, "0", NumElements.ToCSharpCode(option),
                    ElementType.ToCSharpObjectNewAndAssignCode(destVariableName + "[" + temporaryIndexerName + "]", option, parameters),
                    false);

                return objectNewCode + Environment.NewLine + initializeCode;
            }
        }

        public override Type AsType()
        {
            throw new NotImplementedException();
        }

        internal string CreateArrayIndexerCode(IList<Expression> expressions, CompileOption option)
        {
            if (expressions.Count != NumDimensions)
            {
                throw new ArgumentException("expressions");
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < expressions.Count; i++)
            {
                sb.Append("[" + expressions[i].ToCSharpCode(option) + "]");
            }

            return sb.ToString();
        }

        private string RepeatString(string str, int num)
        {
            return string.Concat(Enumerable.Repeat(str, num));
        }
    }

    public class StructType : TypeData
    {
        private readonly string name;
        private readonly IList<FunctionParameterSymbol> constructorParameters;

        internal StructType(string name, IList<FunctionParameterSymbol> constructorParameters)
        {
            this.name = name;
            this.constructorParameters = constructorParameters;
        }

        public override Type AsType()
        {
            throw new NotImplementedException();
        }

        public override string ToCSharpCode(CompileOption option)
        {
            return name;
        }

        public override string RippleName
        {
            get { return name; }
        }

        internal override bool IsObjectNewNeeded
        {
            get { return false; }
        }

        public override bool IsObjectReusable
        {
            get { return true; }
        }

        internal override bool IsSimpleNewSupported
        {
            get { return true; }
        }

        internal override string ToCSharpObjectNewCode(CompileOption option, IList<string> parameters = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("new ");
            sb.Append(this.ToCSharpCode(option));

            sb.Append("(");

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");

                    sb.Append(parameters[i]);
                }
            }

            sb.Append(")");

            return sb.ToString();
        }

        internal FunctionParameterSymbol GetField(string fieldName)
        {
            return constructorParameters.Single(p => p.Name == fieldName);
        }
    }
}
