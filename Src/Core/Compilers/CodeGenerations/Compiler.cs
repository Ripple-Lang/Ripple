using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Ripple.Compilers.ConstantValues;
using Ripple.Compilers.ErrorsAndWarnings;
using Ripple.Compilers.Options;
using Ripple.Compilers.Symbols;

namespace Ripple.Compilers.CodeGenerations
{
    public class Compiler
    {
        private readonly string ComponentsDllPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), Constants.ComponentDllFileName);

        public CodeDomProvider Provider { get; private set; }

        public Compiler(CodeDomProvider provider)
        {
            this.Provider = provider;
        }

        public async Task<CompilationResult> CompileFromRippleSrcAsync(string rippleCode, CompileOption option)
        {
            var codegenResult = await new CSharpCodeGenerator().GenerateCodeAsync(rippleCode, option, null);

            if (codegenResult.ErrorsAndWarnings.HasErrors)
            {
                // エラーがある場合は続行しない
                return new CompilationResult(codegenResult.ProgramUnit, codegenResult.NamespaceName, codegenResult.ErrorsAndWarnings, option);
            }

            return await CompileFromCSharpCodeAsync(codegenResult.GeneratedCode, option, codegenResult.ProgramUnit, codegenResult.ErrorsAndWarnings);
        }

        public Task<CompilationResult> CompileFromCSharpCodeAsync(string csharpCode, CompileOption option)
        {
            return CompileFromCSharpCodeAsync(csharpCode, option, null, new ErrorsAndWarningsContainer().AsReadonly());
        }

        internal Task<CompilationResult> CompileFromCSharpCodeAsync(string csharpCode, CompileOption option, ProgramUnit unit, IErrorsAndWarningsContainer errorsAndWarnings)
        {
            return Task<CompilationResult>.Run(() =>
            {
                CompilerParameters parameters = new CompilerParameters()
                {
                    GenerateInMemory = option.GenerateInMemory,
                    OutputAssembly = option.OutputAssembly,
                    CompilerOptions = option.Optimize ? "/o+" : "/o-"
                };
                parameters.ReferencedAssemblies.Add(ComponentsDllPath);

                var csharpCompilationResult = Provider.CompileAssemblyFromSource(parameters, new[] { csharpCode });

                return new CompilationResult(unit, csharpCode, errorsAndWarnings, option, csharpCompilationResult);
            });
        }
    }

    public class CompilationResult
    {
        public ProgramUnit Unit { get; private set; }
        public string GeneratedCSharpCode { get; private set; }
        public IErrorsAndWarningsContainer ErrorsAndWarnings { get; private set; }
        public CompilerResults CSharpCompilerResults { get; private set; }
        public CompileOption CompileOption { get; private set; }

        public bool HasErrors
        {
            get { return ErrorsAndWarnings.HasErrors || CSharpCompilerResults.Errors.HasErrors; }
        }

        internal CompilationResult(ProgramUnit unit, string generatedCSharpCode, IErrorsAndWarningsContainer errorsAndWarnings, CompileOption CompileOption, CompilerResults cSharpCompilerResults = null)
        {
            this.Unit = unit;
            this.GeneratedCSharpCode = generatedCSharpCode;
            this.ErrorsAndWarnings = errorsAndWarnings;
            this.CSharpCompilerResults = cSharpCompilerResults;
            this.CompileOption = CompileOption;
        }
    }
}
