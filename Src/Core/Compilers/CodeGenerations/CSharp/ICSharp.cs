using Ripple.Compilers.Options;

namespace Ripple.Compilers.CodeGenerations.CSharp
{
    internal interface ICSharp
    {
        string ToCSharpCode(CompileOption option);
        string Name { get; }
        string CSharpLocation { get; }
    }
}
