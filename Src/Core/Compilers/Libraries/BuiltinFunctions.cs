using System;
using System.Collections.Generic;
using Ripple.Compilers.Symbols;

namespace Ripple.Compilers.Libraries
{
    /// <summary>
    /// 組み込み関数を提供する静的クラスです。
    /// </summary>
    internal static class BuiltinFunctions
    {
        public static readonly ProgramUnit ProgramUnit;

        static BuiltinFunctions()
        {
            GlobalScope globalScope = new GlobalScope("");

            foreach (var method in BuiltinMethods)
            {
                globalScope.Define(method);
            }

            ProgramUnit = new ProgramUnit(globalScope, "", null);
        }

        private static IEnumerable<DotNetMethodSymbol> BuiltinMethods
            = Array.AsReadOnly(
                new DotNetMethodSymbol[] {
                    new DotNetMethodSymbol("print",     typeof(Console).GetMethod   ("Write",       new[]{ typeof(object) })),
                    new DotNetMethodSymbol("println",   typeof(Console).GetMethod   ("WriteLine",   new[]{ typeof(object) })),
                    new DotNetMethodSymbol("sin",       typeof(Math).GetMethod      ("Sin",         new[]{ typeof(double) })),
                    new DotNetMethodSymbol("cos",       typeof(Math).GetMethod      ("Cos",         new[]{ typeof(double) })),
                    new DotNetMethodSymbol("tan",       typeof(Math).GetMethod      ("Tan",         new[]{ typeof(double) })),
                    new DotNetMethodSymbol("sqrt",      typeof(Math).GetMethod      ("Sqrt",        new[]{ typeof(double) })),
                    new DotNetMethodSymbol("arcsin",    typeof(Math).GetMethod      ("Asin",        new[]{ typeof(double) })),
                    new DotNetMethodSymbol("arccos",    typeof(Math).GetMethod      ("Acos",        new[]{ typeof(double) })),
                    new DotNetMethodSymbol("arctan",    typeof(Math).GetMethod      ("Atan",        new[]{ typeof(double) })),
                    new DotNetMethodSymbol("log",       typeof(Math).GetMethod      ("Log",         new[]{ typeof(double) })),
                    new DotNetMethodSymbol("log10",     typeof(Math).GetMethod      ("Log10",       new[]{ typeof(double) })),
                    new DotNetMethodSymbol("randomint",
                        typeof(Ripple.Components.Libraries.Randoms).GetMethod("GetRandomInt", new[]{ typeof(int), typeof(int) })),
                    new DotNetMethodSymbol("randomfloat",
                        typeof(Ripple.Components.Libraries.Randoms).GetMethod("GetRandomDouble", new[]{ typeof(double), typeof(double) })),
                }
            );
    }
}
