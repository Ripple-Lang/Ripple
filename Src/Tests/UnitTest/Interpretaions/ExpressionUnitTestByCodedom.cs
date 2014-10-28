using System;
using System.CodeDom.Compiler;
using System.Threading.Tasks;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.Compilers.Options;
using Ripple.Compilers.Tools.Interpretation;

namespace UnitTest.Interpretaions
{
    [TestClass]
    public class ExpressionUnitTestByCodedom
    {
        private readonly CompileOption option = new CompileOption();
        private readonly CodeDomProvider provider = new CSharpCodeProvider();

        [TestMethod]
        public async Task 四則演算()
        {
            var testcases = new[] 
            {
                new { X = "11", Y = (object) 11 },
                new { X = "11.0", Y = (object) 11.0 },
                new { X = "-2.4", Y = (object) -2.4 },
                new { X = "1+2", Y = (object) 3 },
                new { X = string.Format("{0} + {1}", int.MaxValue, 0), Y = (object) int.MaxValue },
                new { X = string.Format("{0} + {1}", int.MaxValue - 10, 10), Y = (object) int.MaxValue },
                new { X = "10-24.3", Y = (object) -14.3 },
                new { X = "-2.5 - -2.5", Y = (object) 0.0 },
                new { X = string.Format("{0} - {1}", int.MaxValue, 0), Y = (object) int.MaxValue },
                new { X = string.Format("{0} - {1}", int.MaxValue - 10, -10), Y = (object) int.MaxValue },
                new { X = string.Format("{0} + 1.0", int.MaxValue), Y = (object) ((double)int.MaxValue + 1) },
                new { X = "2*3", Y = (object) 6 },
                new { X = "3/7", Y = (object) (3.0 / 7.0) },
                new { X = "3 idiv 7", Y = (object) (3 / 7) },
                new { X = "10 mod 5", Y = (object) 0 },
                new { X = "10 mod 3", Y = (object) 1 },
                new { X = "10 mod 10", Y = (object) 0 },
                new { X = "123 mod 2", Y = (object) 1 },
                new { X = "12345678 mod 23456789", Y = (object) 12345678 },
                new { X = string.Format("{0} mod {1}", int.MaxValue, 1), Y = (object) 0 },
                new { X = string.Format("{0} mod {1}", int.MaxValue, 10), Y = (object) (int.MaxValue % 10) },
                new { X = string.Format("{0} mod {1}", (long) int.MaxValue * 2, (long) int.MaxValue + 1), Y = (object) (((long) int.MaxValue * 2) % ((long) int.MaxValue + 1)) },
                new { X = "(115 as sbyte) + (3 as sbyte)", Y = (object)(sbyte)(115 + 3) },
                new { X = "(115 as ubyte) + (23 as ubyte)", Y = (object)(byte)(115 + 23) },
            };

            Interpreter itp = new Interpreter(provider, option);

            foreach (var test in testcases)
            {
                Assert.AreEqual(test.Y, (await itp.InterpretAsync(test.X + ";")).Result);
            }
        }

        [TestMethod]
        public async Task 比較演算子やBool型()
        {
            var testcases = new[]
            {
                new { X = "1<=2", Y = true },
                new { X = "1<2", Y = true },
                new { X = "1>2", Y = false },
                new { X = "1>=2", Y = false },
                new { X = "-3<=-3", Y = true },
                new { X = "-3<-3", Y = false },
                new { X = "-3>-3", Y = false },
                new { X = "-3>=-3", Y = true },
                new { X = "1<=2 and 2<=3", Y = true },
                new { X = "true", Y = true },
                new { X = "false", Y = false },
                new { X = "true and false", Y = false },
                new { X = "true or false", Y = true },
                new { X = "not true and false", Y = false },
                new { X = "not true or false", Y = false },
                new { X = "not (false or false)", Y = true },
                new { X = "not false and true", Y = true },
                new { X = "not (false and true)", Y = true },
            };

            Interpreter itp = new Interpreter(provider, option);

            foreach (var test in testcases)
            {
                Assert.AreEqual(test.Y, (await itp.InterpretAsync(test.X + ";")).Result);
                Assert.AreEqual((test.Y ? 1 : 0) * 10, (await itp.InterpretAsync(string.Format("{0} ? 10 : 0;", test.X))).Result);
            }
        }

        [TestMethod]
        public async Task 実数引数のフィボナッチ数列()
        {
            const int maxN = 20;

            int[] fib = new int[maxN + 1];
            fib[0] = 0;
            fib[1] = 1;

            for (int i = 2; i <= maxN; i++)
            {
                fib[i] = fib[i - 1] + fib[i - 2];
            }

            Interpreter itp = new Interpreter("func fib(x) = x <= 1 ? x : fib(x-1) + fib(x-2);", provider, option);

            for (int i = 0; i <= maxN; i++)
            {
                Assert.AreEqual((double)fib[i], (await itp.InterpretAsync(string.Format("fib({0});", i))).Result);
            }
        }

        [TestMethod]
        public async Task 整数引数のフィボナッチ数列()
        {
            const int maxN = 20;

            int[] fib = new int[maxN + 1];
            fib[0] = 0;
            fib[1] = 1;

            for (int i = 2; i <= maxN; i++)
            {
                fib[i] = fib[i - 1] + fib[i - 2];
            }

            Interpreter itp = new Interpreter("func fib(x as int) = x <= 1 ? x : fib(x-1) + fib(x-2);", provider, option);

            for (int i = 0; i <= maxN; i++)
            {
                Assert.AreEqual(fib[i], (await itp.InterpretAsync(string.Format("fib({0});", i))).Result);
            }
        }

        [TestMethod]
        public async Task 関数定義順に影響されず動作する_1()
        {
            var testcases = new[]
            {
                "func double(x) = x * x; func f(x) = double(x + 10);",
                "func f(x) = double(x + 10); func double(x) = x * x;",
            };

            string input = @"f(12.3);";
            double expect = Math.Pow(12.3 + 10, 2);

            foreach (var test in testcases)
            {
                Interpreter itp = new Interpreter(test, provider, option);
                Assert.AreEqual(expect, (await itp.InterpretAsync(input)).Result);
            }
        }

        [TestMethod]
        public async Task 関数定義順に影響されず動作する_2()
        {
            var testcases = new[]
            {
                "func double(x) = x * x; func f(x) = double(x) + 10;",
                "func f(x) = double(x) + 10; func double(x) = x * x;",
            };

            string input = @"f(12.3);";
            double expect = Math.Pow(12.3, 2) + 10;

            foreach (var test in testcases)
            {
                Interpreter itp = new Interpreter(test, provider, option);
                Assert.AreEqual(expect, (await itp.InterpretAsync(input)).Result);
            }
        }

        [TestMethod]
        public async Task 関数呼び出し時の引数キャスト()
        {
            var testcases = new[]
            {
                new { X = "func f(x) = x + 5;" , Y = "f(10);", Z = (object) 15.0 },
                new { X = "func f(x as int) = x + 5;", Y = "f(10);", Z = (object) 15  },
                new { X = "func f(x, y) = x + y;", Y = "f(5, 7);", Z = (object) 12.0  },
                new { X = "func f(x, y) = x + y;", Y = "f(5.0, 7);", Z = (object) 12.0  },
                new { X = "func f(x, y) = x + y;", Y = "f(5, 7.0);", Z = (object) 12.0  },
                new { X = "func f(x, y) = x + y;", Y = "f(5.0, 7.0);", Z = (object) 12.0  },
            };

            foreach (var test in testcases)
            {
                Interpreter itp = new Interpreter(test.X, provider, option);
                Assert.AreEqual(test.Z, (await itp.InterpretAsync(test.Y)).Result);
            }
        }

        [TestMethod]
        public async Task 明示的に型指定された一行関数()
        {
            var testcases = new[]
            {
                new { X = "func f(x) as float = x;", Y = "f(10.0);", Z = (object) 10.0 },
                new { X = "func f(x as int) as float = x;", Y = "f(10);", Z = (object) 10.0 },
                new { X = "func f(x as long) as float = x;", Y = "f(10);", Z = (object) 10.0 },
                new { X = "func f(x as int) as long = x;", Y = string.Format("f({0}) + 1;", int.MaxValue), Z = (object) ((long)int.MaxValue + 1) },
            };

            foreach (var test in testcases)
            {
                Interpreter itp = new Interpreter(test.X, provider, option);
                Assert.AreEqual(test.Z, (await itp.InterpretAsync(test.Y)).Result);
            }
        }

        [TestMethod]
        public async Task 様々な数の引数を持つ関数()
        {
            var testcases = new[]
            {
                new { X = "func f() = 123;", Y = "f();", Z = (object) 123 },
                new { X = "func f(x) = x + x * 10;", Y = "f(-2.3);", Z = (object) (-2.3 + -2.3 * 10) },
                new { X = "func f(x, y) = x * x + y;", Y = "f(7.2, -2.3);", Z = (object) (7.2 * 7.2 + -2.3) },
                new { X = "func f(a, b, c, d, e, f, g) = a + b + c * d * e * f + g;",
                        Y = "f(1,2,3,4,5,6,7);", Z = (object) (1.0 + 2 + 3 * 4 * 5 * 6 + 7) },
            };

            foreach (var test in testcases)
            {
                Interpreter itp = new Interpreter(test.X, provider, option);
                Assert.AreEqual(test.Z, (await itp.InterpretAsync(test.Y)).Result);
            }
        }

        [TestMethod]
        public async Task 引数型の指定と戻り値型の推論()
        {
            var testcases = new[]
            {
                new { X = "func f(x) = x + 10.0;", Y = "f(10);", Z = (object) 20.0 },
                new { X = "func f(x as int) = x + 10;", Y = "f(10);", Z = (object) 20 },               
                new { X = "func f() = 10 as float;" , Y = "f();", Z = (object) 10.0 },
                new { X = "func f() = 10 as int;" , Y = "f();", Z = (object) 10 },
                new { X = "func f() = 12.3 as long as int;" , Y = "f();", Z = (object) 12 },
            };

            foreach (var test in testcases)
            {
                Interpreter itp = new Interpreter(test.X, provider, option);
                Assert.AreEqual(test.Z, (await itp.InterpretAsync(test.Y)).Result);
            }
        }

        [TestMethod]
        public async Task AndおよびOr演算子が正しく動作する()
        {
            var testcases = new[]
            {
                new { X = "1=2  ? 100 : 50;", Y = 50 },
                new { X = "1=2  and 2=3 ? 100 : 50;", Y = 50 },
                new { X = "1=2  or  2=3 ? 100 : 50;", Y = 50 },
                new { X = "1=2  and 2!=3 ? 100 : 50;", Y = 50 },
                new { X = "1=2  or  2!=3 ? 100 : 50;", Y = 100 },
                new { X = "1!=2 and 2!=3 ? 100 : 50;", Y = 100 },
                new { X = "1!=2 or  2!=3 ? 100 : 50;", Y = 100 },
            };

            Interpreter itp = new Interpreter(provider, option);

            foreach (var test in testcases)
            {
                Assert.AreEqual(test.Y, (await itp.InterpretAsync(test.X)).Result);
            }
        }

        [TestMethod]
        public async Task 組み込み関数が動作する()
        {
            var testcases = new[]
            {
                new { X = "sin(0);", Y = Math.Sin(0) },
                new { X = "cos(10);", Y = Math.Cos(10) },
                new { X = "tan(-1.234 + 2.345);", Y = Math.Tan(-1.234 + 2.345) },
                new { X = "sqrt(2);", Y = Math.Sqrt(2) },
                new { X = "arcsin(1.5);", Y = Math.Asin(1.5) },
                new { X = "arccos(1);", Y = Math.Acos(1) },
                new { X = "arctan(0) * 4;", Y = Math.Atan(0) * 4 },
                new { X = "log(10);", Y = Math.Log(10) },
                new { X = "log10(2);", Y = Math.Log10(2) },
            };

            Interpreter itp = new Interpreter(provider, option);

            foreach (var test in testcases)
            {
                Assert.AreEqual(test.Y, (await itp.InterpretAsync(test.X)).Result);
            }
        }

        [TestMethod]
        public async Task インクリメントとデクリメントが動作する()
        {
            int input = 123;
            var testcases = new[]
            {
                new { X = "x++", Y = input + 1 },
                new { X = "x--", Y = input - 1 },
                new { X = "++x", Y = input + 1 },
                new { X = "--x", Y = input - 1 },
            };

            foreach (var test in testcases)
            {
                string src = @"func f() as int {
    var x as int = " + input + @";
    " + test.X + @";
    return x;
}";

                Interpreter itp = new Interpreter(src, provider, option);
                Assert.AreEqual(test.Y, (await itp.InterpretAsync("f();")).Result);
            }
        }

        [TestMethod]
        public async Task インクリメントとデクリメントが動作する_前後の違い()
        {
            // "func f(x as int) = " が付加される
            int input = 123;
            var testcases = new[]
            {
                new { X = "x++", Y = input },
                new { X = "x--", Y = input },
                new { X = "++x", Y = input + 1 },
                new { X = "--x", Y = input - 1 },
            };

            foreach (var test in testcases)
            {
                Interpreter itp = new Interpreter("func f(x as int) = " + test.X + ";", provider, option);
                Assert.AreEqual(test.Y, (await itp.InterpretAsync("f(" + input + ");")).Result);
            }
        }

        [TestMethod]
        public async Task defを省略した関数定義が動作する()
        {
            string src = "func f(x as int) = x * x;";
            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(12 * 12, (await itp.InterpretAsync("f(12);")).Result);
        }
    }
}
