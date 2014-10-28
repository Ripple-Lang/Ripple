using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.Compilers.CodeGenerations;
using Ripple.Compilers.Options;

namespace UnitTest.Errors
{
    [TestClass]
    public class InvalidInputs
    {
        private static async Task CompileAndAssertHasErrors(string src)
        {
            var result = await new Compiler(new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider())
                            .CompileFromRippleSrcAsync(src, new CompileOption());
            Assert.IsTrue(result.HasErrors);
        }

        [TestMethod]
        public async Task 定義されていない変数の使用()
        {
            string src = @"
init {
    y;
    x = 3;    
}

operation {
    var y as int = f(2+3);
}
";

            await CompileAndAssertHasErrors(src);
        }

        #region 同一名での多重定義

        [TestMethod]
        public async Task 同一名での多重定義_ステージ()
        {
            string src = @"
stage x as int;
stage x as int;
";

            await CompileAndAssertHasErrors(src);
        }

        [TestMethod]
        public async Task 同一名での多重定義_パラメーター()
        {
            string src = @"
param yyy as int;
param yyy = 123;
";

            await CompileAndAssertHasErrors(src);
        }

        [TestMethod]
        public async Task 同一名での多重定義_関数()
        {
            string src = @"
func f(x) = x * x;
func f(x, y) as int {
    return (x + y) as int;
}
";

            await CompileAndAssertHasErrors(src);
        }

        [TestMethod]
        public async Task 同一名での多重定義_init内()
        {
            string src = @"
init {
    var x as int = 3;
    var y as int = 4;

    var y as float = 5;
    var x = 234.55;
}
";

            await CompileAndAssertHasErrors(src);
        }

        [TestMethod]
        public async Task 同一名での多重定義_operation内()
        {
            string src = @"
operation {
    var x as int = 3;
    var y as int = 4;

    var y as float = 5;
    var x = 234.55;
}
";

            await CompileAndAssertHasErrors(src);
        }

        [TestMethod]
        public async Task 同一名での多重定義_関数内()
        {
            string src = @"
func f() as nothing {
    var x as int = 3;
    var y as int = 4;

    var y as float = 5;
    var x = 234.55;
}
";

            await CompileAndAssertHasErrors(src);
        }

        #endregion

        [TestMethod]
        public async Task Initの複数回定義()
        {
            string src = @"
stage x as int;

init {
    x<next> = 123;
}

init {
    x<next> = 0;
}

init {
}
";

            await CompileAndAssertHasErrors(src);
        }

        [TestMethod]
        public async Task Operationの複数回定義()
        {
            string src = @"
stage x as int;

operation {
    x<next> = 123;
}

operation {
    x<next> = 0;
}

operation {
}
";

            await CompileAndAssertHasErrors(src);
        }

        [TestMethod]
        public async Task 同一名の関数引数()
        {
            string src = @"
func f(x, y as int, x as float, z, y as bool) = x + y;
";

            await CompileAndAssertHasErrors(src);
        }
    }
}
