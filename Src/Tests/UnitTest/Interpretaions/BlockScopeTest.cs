using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.Compilers.Options;
using Ripple.Compilers.Tools.Interpretation;

namespace UnitTest.Interpretaions
{
    [TestClass]
    public class BlockScopeTest
    {
        private readonly CompileOption option = new CompileOption();
        private readonly CodeDomProvider provider = new CSharpCodeProvider();

        [TestMethod]
        public async Task 変数をそのまま返すreturn文一つで構成される関数をコンパイルできる()
        {
            string src = @"func f(x) as float {
    return x;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(10.0, (await itp.InterpretAsync("f(10);")).Result);
        }

        [TestMethod]
        public async Task やや複雑なreturn文一つで構成される関数をコンパイルできる()
        {
            Func<int, long, int> compileFunction = (x, y) => ((x * x * y / 3) % 4 == 5) ? 100 : 10;

            string src = @"func f(x as int, y as long) as int {
    return x * x * y idiv 3 mod 4 = 5 ? 100 : 10;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(compileFunction(10, 15), (await itp.InterpretAsync("f(10, 15);")).Result);
        }

        [TestMethod]
        public async Task ifステートメントを含む簡単な関数をコンパイルできる()
        {
            string src = @"func f(x as int) as int {
    if (x >= 10) {
        return x * x;
    } else {
        return x;
    }
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(100, (await itp.InterpretAsync("f(10);")).Result);
            Assert.AreEqual(7, (await itp.InterpretAsync("f(7);")).Result);
        }

        [TestMethod]
        public async Task ブレースを使ったifが連なるステートメントをコンパイルできる()
        {
            string src = @"func round(x as int) as int {
    if (x < 5) {
        return 0;
    } else if (x < 15) {
        return 10;
    } else if (x < 25) {
        return 20;
    } else if (x < 35) {
        return 30;
    } else {
        return 40;
    }
}";

            Interpreter itp = new Interpreter(src, provider, option);
            for (int i = 0; i <= 44; i++)
            {
                Assert.AreEqual((int)(Math.Round(i / 10m, MidpointRounding.AwayFromZero) * 10), (await itp.InterpretAsync("round(" + i + ");")).Result);
            }
        }

        [TestMethod]
        public async Task ifの入れ子をコンパイルできる()
        {
            string src = @"func f(x as int) as int {
    if (x mod 2 = 0) {
        if (x mod 4 = 0) {
            return 0;
        } else {
            return 1;
        }
    } else {
        return 2;
    }
}";

            var testcases = new[]
            {
                new { X = 0, Y = 0 },
                new { X = 1, Y = 2 },
                new { X = 2, Y = 1 },
                new { X = 3, Y = 2 },
                new { X = 4, Y = 0 },
                new { X = 5, Y = 2 },
            };

            Interpreter itp = new Interpreter(src, provider, option);
            foreach (var t in testcases)
            {
                Assert.AreEqual(t.Y, (await itp.InterpretAsync("f(" + t.X + ");")).Result);
            }
        }

        [TestMethod]
        public async Task ifの入れ子をコンパイルできる_結婚可否()
        {
            // http://www1.bbiq.jp/takeharu/java34.html

            string src = @"func marriable(age as int, accepted as bool) as bool {
    if (accepted) {
        if (age >= 18) {
            return true;
        } else {
            return false;
        }
    } else {
        if (age >= 20) {
            return true;
        } else {
            return false;
        }
    }
}";

            Interpreter itp = new Interpreter(src, provider, option);

            for (int i = 15; i <= 22; i++)
            {
                bool[] bools = new[] { true, false };

                foreach (var ok in bools)
                {
                    Assert.AreEqual(i >= 20 || (ok && i >= 18),
                        (await itp.InterpretAsync(string.Format("marriable({0}, {1});", i, ok ? "true" : "false"))).Result);
                }
            }
        }

        [TestMethod]
        public async Task 戻り値がNothing型でprint関数を呼ぶメソッドを正しくコンパイルでき動作する()
        {
            int printNumber = 1234;
            string code = @"func main() as nothing {
    println(" + printNumber + @");
}";

            var writer = new StringWriter();
            Console.SetOut(writer);

            Interpreter itp = new Interpreter(code, provider, option);
            await itp.InterpretAsync("main();");

            Assert.AreEqual(printNumber.ToString(), writer.ToString().Trim());
        }

        [TestMethod]
        public async Task 簡単な明示的に型指定する変数宣言を含む関数をコンパイルできる()
        {
            string src = @"func f(x) as float {
    var y as float = x * 2;
    return y;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            double input = 1.235;
            Assert.AreEqual(input * 2, (await itp.InterpretAsync("f(" + input + ");")).Result);
        }

        [TestMethod]
        public async Task 別スコープで定義された同名変数に対して正しく動作する()
        {
            string src = @"func f() as int {
    var x as int = 1;
    {
        var x as int = 2;
        x = 3;
    }
    return x;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(1, (await itp.InterpretAsync("f();")).Result);
        }

        [TestMethod]
        public async Task Whileループにより定義されるgcd関数が正しく動作する()
        {
            string src = @"func gcd(x as int, y as int) as int {
    var rem as int = 1; // 0でなければ何でもよい

    while (rem != 0) {
        rem = x mod y;
        x = y;
        y = rem;
    }

    return x;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(40, (await itp.InterpretAsync("gcd(120, 80);")).Result);
            Assert.AreEqual(1, (await itp.InterpretAsync("gcd(123, 62);")).Result);
            Assert.AreEqual(1, (await itp.InterpretAsync("gcd(62, 123);")).Result);
        }

        [TestMethod]
        public async Task nothing型のreturn文が存在する関数が正しく動作する()
        {
            string src = @"
func f() as nothing {
    var x = 2.3;
    return;
}

func g() as int {
    f();
    return -123456;
}
";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(-123456, (await itp.InterpretAsync("g();")).Result);
        }

        [TestMethod]
        public async Task 複合代入演算子_加算_が正しく動作する()
        {
            string src = @"
func f() as float {
    var x = 1.2;
    x += 2.3;
    return x;
}
";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(1.2 + 2.3, (await itp.InterpretAsync("f();")).Result);
        }

        [TestMethod]
        public async Task 複合代入演算子_減算_が正しく動作する()
        {
            string src = @"
func f() as float {
    var x = 1.2;
    x -= 2.3;
    return x;
}
";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(1.2 - 2.3, (await itp.InterpretAsync("f();")).Result);
        }

        [TestMethod]
        public async Task 複合代入演算子_乗算_が正しく動作する()
        {
            string src = @"
func f() as float {
    var x = 1.2;
    x *= 2.3;
    return x;
}
";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(1.2 * 2.3, (await itp.InterpretAsync("f();")).Result);
        }

        [TestMethod]
        public async Task 複合代入演算子_除算_が正しく動作する()
        {
            string src = @"
func f() as float {
    var x = 1.2;
    x /= 2.3;
    return x;
}
";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(1.2 / 2.3, (await itp.InterpretAsync("f();")).Result);
        }

        [TestMethod]
        public async Task Forループによるイテレーションが正しく動作する()
        {
            string src = @"
func f() as int {
    var x as int = 0;

    for (i = 0 to 100) {
        x += i;
    }

    return x;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(5050, (await itp.InterpretAsync("f();")).Result);
        }

        [TestMethod]
        public async Task 二重のForループによるイテレーションが正しく動作する()
        {
            string src = @"
func f() as int {
    var x as int = 0;

    for (i = 0 to 100) {
        x += i;
        for (j = 0 to 200) {
            x += j;
        }
    }

    return x;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual(5050 + 20100 * 101, (await itp.InterpretAsync("f();")).Result);
        }

        [TestMethod]
        public async Task Forループ内のBreakが正しく動作する()
        {
            string src = @"
func f() as int {
    var x as int = 0;

    for (i = 0 to 100) {
        if (i > 50) {
            break;
        }

        x += i;
    }

    return x;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual((0 + 50) * 51 / 2, (await itp.InterpretAsync("f();")).Result);
        }

        [TestMethod]
        public async Task Forループ内のContinueが正しく動作する()
        {
            string src = @"
func f() as int {
    var x as int = 0;

    for (i = 0 to 100) {
        if (i = 50 or i = 60 or i = 70) {
            continue;
        }

        x += i;
    }

    return x;
}";

            Interpreter itp = new Interpreter(src, provider, option);
            Assert.AreEqual((0 + 100) * 101 / 2 - (50 + 60 + 70), (await itp.InterpretAsync("f();")).Result);
        }
    }
}
