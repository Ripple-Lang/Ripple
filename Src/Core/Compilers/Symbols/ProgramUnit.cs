using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ripple.Compilers.CodeGenerations.CSharp;
using Ripple.Compilers.ConstantValues;
using Ripple.Compilers.Options;
using Ripple.Compilers.Types;

namespace Ripple.Compilers.Symbols
{
    public class ProgramUnit
    {
        internal GlobalScope GlobalScope { get; private set; }
        public string CSharpLocation { get; private set; }
        public IEnumerable<ProgramUnit> ExternalUnits { get; private set; }

        internal ProgramUnit(string csharpLocation, IEnumerable<ProgramUnit> externalUnits)
            : this(new GlobalScope(csharpLocation), csharpLocation, externalUnits)
        { }

        internal ProgramUnit(GlobalScope globalScope, string csharpLocation, IEnumerable<ProgramUnit> externalUnits)
        {
            this.GlobalScope = globalScope;
            this.CSharpLocation = csharpLocation;
            this.ExternalUnits = externalUnits;
        }

        public IEnumerable<Stage> GetStages(CompileOption option)
        {
            return from s in GlobalScope.Symbols.OfType<StageSymbol>()
                   select new Stage(s.Name, s.Type);
        }

        public IEnumerable<Parameter> GetParameters(CompileOption option)
        {
            return from p in GlobalScope.Symbols.OfType<ParameterSymbol>()
                   select new Parameter(p.Name, p.ToCSharpName(option), p.Type, p.IsInitializationNeeded);
        }

        internal Symbol FindSymbolInThisUnit(string name)
        {
            try
            {
                return GlobalScope.SymbolsDictionary[name];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        internal Symbol FindSymbol(string name)
        {
            Symbol symbol = FindSymbolInThisUnit(name);

            if (symbol == null)
            {
                foreach (var external in ExternalUnits)
                {
                    symbol = external.FindSymbol(name);
                    if (symbol != null)
                    {
                        break;
                    }
                }
            }

            return symbol;
        }

        internal Type DefineClassTo(NameSpace ns, CompileOption option, string className)
        {
            Type classType = ns.DefineType(className, TypeKind.Class, AccessLevel.Public, new[] { Constants.ISimulationFullName });
            var symbols = GlobalScope.Symbols;

            // イベント
            DefineEvents(classType);

            // 自動生成されたイニシャライズメソッド
            DefineInitializeMethod(option, classType, symbols);

            // シンボルの定義を生成
            foreach (var symbol in symbols.OfType<IDefinableToClass>())
            {
                symbol.DefineToType(classType, option);
            }

            // __Runメソッド
            DefineRunMethod(option, classType, symbols);

            return classType;
        }

        private static void DefineEvents(Type classType)
        {
            //classType.DefineDelegate(Constants.OnTimeChangedEventHandlerName, AccessLevel.Public, BuiltInNumericType.Nothing, false, Constants.OnTimeChangedEventHandlerParameters);
            classType.DefineEvent(Constants.OnTimeChangedEventName, AccessLevel.Public, null, false, Constants.OnTimeChangedEventHandlerName);
        }

        private static void DefineInitializeMethod(CompileOption option, Type classType, IEnumerable<Symbol> symbols)
        {
            // 自動生成されるイニシャライズメソッドのコード生成
            StringBuilder methodBody = new StringBuilder();

            methodBody.AppendLine("{");

            // 各ステージを初期化
            foreach (var stage in symbols.OfType<StageSymbol>())
            {
                methodBody.AppendLine(stage.StageHoldState.GetInitializeAllocationCode(option));
            }

            // ステージの最初の時刻のアロケーションコード(時刻は-1)
            methodBody.AppendLine(string.Format("{0} {1} = -1;", Constants.TimeType.ToCSharpCode(option), Constants.NowVariableName));
            foreach (var stage in symbols.OfType<StageSymbol>().Where(s => s.Type.IsObjectNewNeeded))
            {
                methodBody.AppendLine(stage.StageHoldState.GetMoveNextAllocationCode(option));
            }

            // この時点でnowは-1であり、そのままにした上でユーザーによる初期化コード
            if (option.CacheStages)
            {
                AppendLineComment("ステージのキャッシュコード", methodBody);
                AppendStagesCacheCode(methodBody, symbols.OfType<StageSymbol>(), option, false /* nowはキャッシュしない */);
            }
            AppendLineComment("ユーザーによるinitコード", methodBody);
            methodBody.AppendLine(symbols.OfType<InitialiationSymbol>().First().Body.ToCSharpCode(option));

            methodBody.Append("}");

            // メソッドを追加
            classType.DefineTextBasedMethod(
                Constants.InitializeMethodName,
                AccessLevel.Public,
                BuiltInNumericType.Nothing,
                false,
                new List<FunctionParameterSymbol>()
                {
                    new FunctionParameterSymbol(Constants.MaxTimeVariableName){Type = Constants.TimeType}
                },
                methodBody.ToString());
        }

        private static void DefineRunMethod(CompileOption option, Type classType, IEnumerable<Symbol> symbols)
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine("{");

            // シミュレーション中変化しないパラメーターのキャッシュ
            if (option.CacheParameters)
            {
                AppendLineComment("シミュレーション中変化しないパラメーターのキャッシュコード", body);
                foreach (var parameter in symbols.OfType<ParameterSymbol>().Where(p => p.IsConstant))
                {
                    body.AppendLine(string.Format("var @{0} = this.@{0};", parameter.ToCSharpName(option)));
                }
            }

            // 各時刻で実行するコード
            AppendLineComment("各時刻で実行するコード", body);
            body.AppendLine(string.Format("for (int @{0} = 0; @{0} < @{1}; @{0}++)", Constants.NowVariableName, Constants.MaxTimeVariableName));
            body.AppendLine("{");
            {
                // イベントコード
                AppendRaiseEventCode(body);

                // ステージのアロケーションコード
                AppendLineComment("各ステージのメモリー領域を確保するコード", body);
                foreach (var stage in symbols.OfType<StageSymbol>().Where(s => s.Type.IsObjectNewNeeded))
                {
                    body.AppendLine(stage.StageHoldState.GetMoveNextAllocationCode(option));
                }

                // Operationコード
                if (option.CacheStages)
                {
                    AppendLineComment("ステージのキャッシュコード", body);
                    AppendStagesCacheCode(body, symbols.OfType<StageSymbol>(), option);
                }

                var operationSymbol = (OperationSymbol)symbols.Single(s => s is OperationSymbol);
                AppendLineComment("ユーザーによるoperationコード", body);
                body.AppendLine(operationSymbol.Body.ToCSharpCode(option));
            }
            body.AppendLine("}");

            body.Append("}");

            classType.DefineTextBasedMethod(
                Constants.RunMethodName,
                AccessLevel.Public,
                BuiltInNumericType.Nothing,
                false,
                new FunctionParameterSymbol[] { new FunctionParameterSymbol(Constants.MaxTimeVariableName) { Type = Constants.TimeType } },
                body.ToString());
        }

        private static void AppendStagesCacheCode(StringBuilder sb, IEnumerable<StageSymbol> stages, CompileOption option, bool cacheNow = true)
        {
            // 変数にキャッシュ
            foreach (var stage in stages)
            {
                // キャッシュされた変数を取得する(あれば)
                string cachedNow = cacheNow ? stage.GetCachedVariableNameIfCached(0, option) : null;
                string cachedNext = stage.GetCachedVariableNameIfCached(1, option);

                if (cachedNow != null)
                {
                    sb.AppendLine(string.Format(
                        "var @{0} = {1};", cachedNow,
                        "@" + stage.ToCSharpName(option) + "[" + stage.StageHoldState.GetTimeSpecifierCode(Constants.NowVariableName) + "]"));
                }

                if (cachedNext != null)
                {
                    sb.AppendLine(string.Format(
                        "var @{0} = {1};", cachedNext,
                        "@" + stage.ToCSharpName(option) + "[" + stage.StageHoldState.GetTimeSpecifierCode(Constants.NowVariableName + " + 1") + "]"));
                }
            }
        }

        private static void AppendRaiseEventCode(StringBuilder sb)
        {
            sb.AppendLine("{");
            sb.AppendFormat(
                "var {0} = {1};{3}"
                + "if ({0} != null) {{{3}"
                + "{0}(this, {2});{3}"
                + "}}{3}",
                "__event_OnTimeChanged", Constants.OnTimeChangedEventName, Constants.NowVariableName, System.Environment.NewLine);
            sb.Append("}");
        }

        private static void AppendLineComment(string comment, StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("// コンパイラが生成したコメント - " + comment);
        }
    }

    public class Stage
    {
        public string Name { get; private set; }
        public TypeData Type { get; private set; }

        public Stage(string name, TypeData type)
        {
            this.Name = name;
            this.Type = type;
        }
    }

    public class Parameter
    {
        public string Name { get; private set; }
        public string CSharpPropertyName { get; private set; }
        public TypeData Type { get; private set; }
        public bool IsInitializationNeeded { get; private set; }

        public Parameter(string name, string csharpPropertyName, TypeData type, bool isInitializationNeeded)
        {
            this.Name = name;
            this.CSharpPropertyName = csharpPropertyName;
            this.Type = type;
            this.IsInitializationNeeded = isInitializationNeeded;
        }
    }
}
