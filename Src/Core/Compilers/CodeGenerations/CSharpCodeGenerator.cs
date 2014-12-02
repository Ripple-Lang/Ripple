using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Ripple.Compilers.CodeGenerations.CSharp;
using Ripple.Compilers.ErrorsAndWarnings;
using Ripple.Compilers.LexicalAnalysis;
using Ripple.Compilers.Libraries;
using Ripple.Compilers.Options;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.SyntaxAnalysis;

namespace Ripple.Compilers.CodeGenerations
{
    public class CSharpCodeGenerator
    {
        #region static readonly 定数

        private static readonly string Header;

        static CSharpCodeGenerator()
        {
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();

            Header = string.Format(@"///
/// このコードは、{0} によって生成されました。
///", VersionInfos.VersionInformation.ProductName + " " + VersionInfos.VersionInformation.Version);
        }

        #endregion

        public Task<CodeGenerationResult> GenerateCodeAsync(string source, CompileOption option, IEnumerable<ProgramUnit> externalUnits)
        {
            return Task<CodeGenerationResult>.Run(() =>
            {
                // 組み込み関数のシンボルを追加する
                if (option.UseBuiltinMethods)
                {
                    if (externalUnits == null)
                        externalUnits = Enumerable.Empty<ProgramUnit>();

                    externalUnits = externalUnits.Concat(new[] { BuiltinFunctions.ProgramUnit });
                }

                ErrorsAndWarningsContainer errorsAndWarnings = new ErrorsAndWarningsContainer();

                // 字句・構文解析
                var tokens = new Lexer(source, errorsAndWarnings).Lex();
                ProgramUnit programUnit =
                    new Parser(tokens, errorsAndWarnings, option, externalUnits).ParseProgramUnit();

                // エラーがあれば、コード生成を中断
                if (errorsAndWarnings.HasErrors)
                {
                    return new CodeGenerationResult(programUnit, string.Empty, errorsAndWarnings.AsReadonly(), string.Empty, string.Empty);
                }

                // 型の決定
                programUnit.GlobalScope.InferChildlensType(errorsAndWarnings);

                // もしオペレーションメソッドがなければ追加する
                if (programUnit.GlobalScope.Symbols.Count(s => s is OperationSymbol) == 0)
                {
                    programUnit.GlobalScope.Define(new OperationSymbol(programUnit.GlobalScope)
                    {
                        Body = new BlockStatement(null)
                    });
                }

                // もしイニシャライズメソッドがなければ追加する
                if (programUnit.GlobalScope.Symbols.Count(s => s is InitialiationSymbol) == 0)
                {
                    programUnit.GlobalScope.Define(new InitialiationSymbol(programUnit.GlobalScope)
                    {
                        Body = new BlockStatement(null)
                    });
                }

                // 名前空間とクラスを生成する
                NameSpace ns = new NameSpace(option.NameSpaceName);
                Ripple.Compilers.CodeGenerations.CSharp.Type classType =
                    programUnit.DefineClassTo(
                    ns,
                    option,
                    option.ClassName);

                // C#のメソッドコードを生成する
                string generatedCode = Header + Environment.NewLine + Environment.NewLine + ns.ToCSharpCode(option);

                return new CodeGenerationResult(programUnit, generatedCode, errorsAndWarnings, option.NameSpaceName, option.ClassName);
            });
        }
    }

    public class CodeGenerationResult
    {
        public ProgramUnit ProgramUnit { get; private set; }
        public string GeneratedCode { get; private set; }
        public IErrorsAndWarningsContainer ErrorsAndWarnings { get; private set; }

        public string NamespaceName { get; private set; }
        public string ClassName { get; private set; }

        public CodeGenerationResult(
            ProgramUnit programUnit,
            string generatedCode,
            IErrorsAndWarningsContainer errorsAndWarnings,
            string namespaceName,
            string className)
        {
            this.ProgramUnit = programUnit;
            this.GeneratedCode = generatedCode;
            this.ErrorsAndWarnings = errorsAndWarnings;
            this.NamespaceName = namespaceName;
            this.ClassName = className;
        }
    }
}
