using System;

namespace Ripple.Compilers.CodeGenerations.CSharp
{
    internal static class CSharpCodeGenHelper
    {
        private const string ParallelForMethodFullName = "System.Threading.Tasks.Parallel.For";

        public static string CreateSimpleForStatement(
            string indexerType, string indexerName, string fromInclusive, string toExclusive, string body, bool isParallel)
        {
            if (!body.StartsWith("{") || !body.EndsWith("}"))
            {
                body = "{" + Environment.NewLine + body + Environment.NewLine + "}";
            }

            if (isParallel)
            {
                return string.Format(
                    "{0}({1}, {2}, {3} =>"
                    + Environment.NewLine + "{4});",
                    ParallelForMethodFullName, fromInclusive, toExclusive, indexerName, body);
            }
            else
            {
                return string.Format(
                    "for ({0} {1} = {2}; {1} < {3}; {1}++)"
                    + Environment.NewLine + "{4}",
                    indexerType, indexerName, fromInclusive, toExclusive, body);
            }
        }
    }
}
