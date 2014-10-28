
namespace Ripple.Compilers.CodeGenerations.CSharp
{
    internal static class TemporaryVariableNameFactory
    {
        private static int nextNo = 0;

        internal static string GetTemporaryVariableName()
        {
            return "__tempver_" + nextNo++;
        }
    }
}
