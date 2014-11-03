using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Ripple.Compilers.CodeGenerations.CSharp;
using Ripple.Compilers.ConstantValues;
using Ripple.Compilers.Exceptions;
using Ripple.Compilers.Expressions;
using Ripple.Compilers.Options;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.Symbols
{
    internal abstract class Symbol : ISyntaxNode
    {
        public string Name { get; private set; }
        public virtual TypeData Type { get; internal set; }

        public Symbol(string name)
        {
            this.Name = name;
        }

        public abstract IEnumerable<ISyntaxNode> Children { get; }

        public virtual string ToCSharpName(CompileOption option)
        {
            return Name;
        }
    }

    internal interface IDefinableToClass
    {
        string CSharpLocation { get; }
        void DefineToType(Ripple.Compilers.CodeGenerations.CSharp.Type type, CompileOption option);
    }

    #region 変数

    internal abstract class VariableSymbol : Symbol
    {
        public VariableSymbol(string name)
            : base(name)
        { }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Enumerable.Empty<ISyntaxNode>(); }
        }

        public virtual bool IsConstant
        {
            get { return false; }
        }
    }

    internal class FunctionParameterSymbol : VariableSymbol
    {
        public FunctionParameterSymbol(string name)
            : base(name)
        { }
    }

    internal class LocalVariableSymbol : VariableSymbol, ITypeInferable
    {
        private readonly Expression initialValue;
        public IScope DeclaredScope { get; private set; }

        private static int nextID = 0;
        private readonly int id = nextID++;

        public LocalVariableSymbol(string name, Expression initialValue, IScope declaredScope)
            : base(name)
        {
            this.initialValue = initialValue;
            this.DeclaredScope = declaredScope;
        }

        public override string ToCSharpName(CompileOption option)
        {
            return Name + (option.AddUniqueNoToVariable ? ("_" + id.ToString()) : "");
        }

        public void InferType(List<ISyntaxNode> undecidedNodes)
        {
            if (this.Type == null)
            {
                this.Type = initialValue.ReturnType;

                if (this.Type is BuiltInNumericType)
                {
                    // ローカル変数の特別な型推論
                    this.Type = BuiltInNumericType.LargerOf(this.Type as BuiltInNumericType, BuiltInNumericType.Float64);
                }

                if (this.Type == null)
                {
                    undecidedNodes.Add(this);
                }
            }
        }
    }

    internal class StageSymbol : VariableSymbol, IDefinableToClass
    {
        public StageHoldState StageHoldState { get; internal set; }
        public string CSharpLocation { get; private set; }

        public StageSymbol(string name, string csharpLocation)
            : base(name)
        {
            this.CSharpLocation = csharpLocation;
        }

        public void DefineToType(CodeGenerations.CSharp.Type type, CompileOption option)
        {
            type.DefineField(
                ToCSharpName(option),
                AccessLevel.Public,
                new ArrayType(StageHoldState.Type, null),
                isStatic: false,
                variable: this);
        }

        public bool IsCacheable
        {
            get { return this.Type is ArrayType; }
        }

        public string GetCachedVariableNameIfCached(int offsetFromNow, CompileOption option)
        {
            if (option.CacheStages && this.IsCacheable)
            {
                return CreateCachedVariableName(this, offsetFromNow);
            }
            else
            {
                return null;
            }
        }

        private static string CreateCachedVariableName(StageSymbol stage, int offsetFromNow)
        {
            return "__cached_" + stage.Name + "_" + offsetFromNow.ToString().Replace('-', 'M');
        }
    }

    internal class ParameterSymbol : VariableSymbol, IDefinableToClass, ITypeInferable
    {
        public BlockStatement Body { get; internal set; }
        public string CSharpLocation { get; private set; }

        public ParameterSymbol(string name, string csharpLocation)
            : base(name)
        {
            this.CSharpLocation = csharpLocation;
        }

        public bool IsInitializationNeeded
        {
            get { return Body == null; }
        }

        public bool IsCompiledAsMethod
        {
            get { return !IsConstant; }
        }

        public override bool IsConstant
        {
            get
            {
                if (this.IsInitializationNeeded)
                {
                    // シミュレーション開始時に入力されるので定数
                    return true;
                }
                else
                {
                    var asExpressionScope = this.Body as ReturnOnlyBlockStatement;

                    if (asExpressionScope != null)
                    {
                        return ((ReturnOnlyBlockStatement)this.Body).Expression.IsConstant;
                    }
                }

                return false;
            }
        }

        public override IEnumerable<ISyntaxNode> Children
        {
            get
            {
                return base.Children.Concat(new[] { Body });
            }
        }

        public void DefineToType(CodeGenerations.CSharp.Type type, CompileOption option)
        {
            if (IsInitializationNeeded)
            {
                type.DefineAutoImplementedProperty(
                    ToCSharpName(option),
                    AccessLevel.Public,
                    this.Type,
                    isStatic: false,
                    getterAccessLevel: AccessLevel.NoSpecified,
                    setterAccessLevel: AccessLevel.NoSpecified);
            }
            else if (IsCompiledAsMethod)
            {
                // 関数シンボルの生成
                var functionSymbol = new FunctionSymbol(ToCSharpName(option), null, this.CSharpLocation)
                {
                    Body = this.Body,
                    Parameters = new[] { new FunctionParameterSymbol(Constants.NowVariableName) { Type = Constants.TimeType } },
                };

                type.DefineMethod(
                    ToCSharpName(option),
                    AccessLevel.Private,
                    Type,
                    true,
                    functionSymbol);
            }
            else
            {
                type.DefineReadonlyProperty(
                    ToCSharpName(option),
                    AccessLevel.Private,
                    Type,
                    false /* non static */,
                    AccessLevel.NoSpecified,
                    Body);
            }
        }

        public void InferType(List<ISyntaxNode> undecidedNodes)
        {
            if (this.Type == null)
            {
                Debug.Assert(Body is ReturnOnlyBlockStatement);
                this.Type = ((ReturnOnlyBlockStatement)Body).Expression.ReturnType;
                if (this.Type == null)
                {
                    undecidedNodes.Add(this);
                }
            }
        }
    }

    internal class NowVariableSymbol : VariableSymbol
    {
        private NowVariableSymbol()
            : base(Constants.NowVariableName)
        { }

        public static readonly NowVariableSymbol Instance = new NowVariableSymbol();

        public override TypeData Type
        {
            get
            {
                return BuiltInNumericType.Int32;
            }
            internal set
            {
                throw new Exception();
            }
        }
    }

    #endregion

    #region メソッド

    internal abstract class MethodSymbol : Symbol
    {
        public MethodSymbol(string name)
            : base(name)
        { }

        public abstract string ToCSharpFullName(CompileOption option);
    }

    internal interface IMethodDefnition
    {
        IList<FunctionParameterSymbol> Parameters { get; }
        BlockStatement Body { get; }
    }

    internal class DotNetMethodSymbol : MethodSymbol
    {
        private readonly MethodInfo methodInfo;
        private readonly TypeData returnType;

        public DotNetMethodSymbol(string name, MethodInfo methodInfo)
            : base(name)
        {
            this.methodInfo = methodInfo;
            this.returnType = TypeData.FromType(methodInfo.ReturnType);
            this.Type = returnType;
        }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Enumerable.Empty<ISyntaxNode>(); }
        }

        public override string ToCSharpName(CompileOption option)
        {
            return methodInfo.Name;
        }

        public override string ToCSharpFullName(CompileOption option)
        {
            return methodInfo.DeclaringType.FullName + "." + methodInfo.Name;
        }
    }

    internal class FunctionSymbol : MethodSymbol, IScope, IDefinableToClass, IMethodDefnition, ITypeInferable
    {
        public IScope Enclosing { get; private set; }
        public string CSharpLocation { get; private set; }

        public FunctionSymbol(string name, IScope enclosing, string csharpLocation)
            : base(name)
        {
            this.Enclosing = enclosing;
            this.CSharpLocation = csharpLocation;
        }

        public BlockStatement Body { get; internal set; }
        public IList<FunctionParameterSymbol> Parameters { get; internal set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return new ISyntaxNode[] { Body }.Concat(Parameters); }
        }

        public override string ToCSharpFullName(CompileOption option)
        {
            return CSharpLocation + ".@" + ToCSharpName(option);
        }

        public void Define(Symbol symbol)
        {
            // TODO : 例外作成
            throw new Exception();
        }

        public Symbol Resolve(string name)
        {
            VariableSymbol symbol = Parameters.FirstOrDefault(s => s.Name == name);

            if (symbol != null)
            {
                return symbol;
            }
            else
            {
                return Enclosing.Resolve(name);
            }
        }

        public void DefineToType(CodeGenerations.CSharp.Type type, CompileOption option)
        {
            type.DefineMethod(Name, AccessLevel.Public, Type, true, this);
        }

        public void InferType(List<ISyntaxNode> undecidedNodes)
        {
            if (this.Type == null)
            {
                Debug.Assert(Body is ReturnOnlyBlockStatement);
                this.Type = ((ReturnOnlyBlockStatement)Body).Expression.ReturnType;
                if (this.Type == null)
                {
                    undecidedNodes.Add(this);
                }
            }
        }
    }

    internal abstract class NoParameterMethodSymbol : MethodSymbol, IScope, IMethodDefnition
    {
        public BlockStatement Body { get; internal set; }
        public IScope Enclosing { get; private set; }
        public string CSharpLocation { get; internal set; }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return new ISyntaxNode[] { Body }.Concat(Parameters); }
        }

        private List<Symbol> symbols;

        public override TypeData Type
        {
            get
            {
                return BuiltInNumericType.Nothing;
            }
            internal set
            {
                throw new Exception();
            }
        }

        public NoParameterMethodSymbol(string methodName, IScope enclosing)
            : base(methodName)
        {
            this.symbols = new List<Symbol>()
            {
                NowVariableSymbol.Instance
            };
            this.Enclosing = enclosing;
        }

        public void Define(Symbol symbol)
        {
            if (symbols.FindAll(s => s.Name == symbol.Name).Count != 0)
            {
                throw new SymbolAlreadyExistsException(symbol.Name);
            }

            symbols.Add(symbol);
        }

        public Symbol Resolve(string name)
        {
            Symbol symbol = symbols.Find(s => s.Name == name);

            if (symbol != null)
            {
                return symbol;
            }
            else
            {
                return Enclosing.Resolve(name);
            }
        }

        public IList<FunctionParameterSymbol> Parameters
        {
            get { return new FunctionParameterSymbol[] { }; }
        }
    }

    internal class InitialiationAndOperationSymbolBase : NoParameterMethodSymbol
    {
        private readonly bool cacheNow;

        public InitialiationAndOperationSymbolBase(string name, IScope enclosing, bool cacheNow)
            : base(name, enclosing)
        {
            this.cacheNow = cacheNow;
        }

        public override string ToCSharpFullName(CompileOption option)
        {
            return "this." + ToCSharpName(option);
        }
    }

    internal class OperationSymbol : InitialiationAndOperationSymbolBase
    {
        public OperationSymbol(IScope enclosing)
            : base(Constants.OperationMethodName, enclosing, true)
        { }
    }

    internal class InitialiationSymbol : InitialiationAndOperationSymbolBase
    {
        public InitialiationSymbol(IScope enclosing)
            : base(Constants.UserInitializeMethodName, enclosing, false)
        { }
    }

    #endregion

    #region 構造体

    internal class StructSymbol : Symbol, IDefinableToClass
    {
        public IList<FunctionParameterSymbol> Fields { get; internal set; }

        public StructSymbol(string name)
            : base(name)
        { }

        public override IEnumerable<ISyntaxNode> Children
        {
            get { return Fields; }
        }

        public TypeData ToTypeData()
        {
            return new StructType(Name, Fields);
        }

        public string CSharpLocation
        {
            // TODO : 要検討
            get { return null; }
        }

        public void DefineToType(CodeGenerations.CSharp.Type type, CompileOption option)
        {
            var structType = type.DefineType(Name, TypeKind.Struct, AccessLevel.Public);
            foreach (var field in Fields)
            {
                structType.DefineField(field.ToCSharpName(option), AccessLevel.Public, field.Type, false, field);
            }
        }
    }

    #endregion

}
