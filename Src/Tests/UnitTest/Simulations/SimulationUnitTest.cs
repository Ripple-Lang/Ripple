using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ripple.Compilers.CodeGenerations;
using Ripple.Compilers.Options;
using Ripple.Compilers.Tools.Interpretation;
using Ripple.Compilers.Tools.Simulations;
using Ripple.Components;

namespace UnitTest.Simulations
{
    [TestClass]
    public class SimulationUnitTest
    {
        [TestMethod]
        public async Task 空のシミュレーションが動作する()
        {
            string src = @"stage intValue as int;

operation {
    print(1);
}";

            const int maxTime = 1000;

            var sw = new StringWriter();
            Console.SetOut(sw);

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(0, instance.intValue[maxTime]);
            Assert.AreEqual(maxTime, sw.ToString().Count(c => c == '1'));
        }

        [TestMethod]
        public async Task 一つのステージの値を更新するシミュレーションが動作する()
        {
            string src = @"stage intValue as int;

operation {
    intValue<next> = make(intValue<now>);
}

func make(x as int) = x + 10;";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(10 * maxTime, instance.intValue[maxTime]);
        }

        [TestMethod]
        public async Task パラメーターを使用して一つのステージの値を更新するシミュレーションが動作する()
        {
            string src = @"stage intValue as int;

param addValue as int = 12;

operation {
    intValue<next> = intValue<now> + addValue;
}";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(12 * maxTime, instance.intValue[maxTime]);
        }

        [TestMethod]
        public async Task 型が省略されたパラメーターを使用して一つのステージの値を更新するシミュレーションが動作する()
        {
            string src = @"stage intValue as int;

param addValue = 12;

operation {
    intValue<next> = intValue<now> + addValue;
}";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(12 * maxTime, instance.intValue[maxTime]);
        }

        [TestMethod]
        public async Task 初期化を行い一つのステージの値を更新するシミュレーションが動作する()
        {
            string src = @"stage intValue as int;

param addValue = 12;

init {
    intValue <= -1000;
}

operation {
    intValue<next> = intValue<now> + addValue;
}";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(-1000 + 12 * maxTime, instance.intValue[maxTime]);
        }

        [TestMethod]
        public async Task 初期化_明示的に時刻0を指定_を行い一つのステージの値を更新するシミュレーションが動作する()
        {
            string src = @"stage intValue as int;

param addValue = 12;

init {
    intValue<0> = -1000;
}

operation {
    intValue<next> = intValue<now> + addValue;
}";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(-1000 + 12 * maxTime, instance.intValue[maxTime]);
        }

        [TestMethod]
        public async Task パラメーターが時刻に依存するシミュレーションが動作する()
        {
            string src = @"stage value as float;

param addValue = now * 1.5;

init {
    value <= -1000;
}

operation {
    value<next> = value<now> + addValue;
}";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(-1000 + ((maxTime - 1) * maxTime / 2) * 1.5, instance.value[maxTime]);
        }

        [TestMethod]
        public async Task hold節を指定したシミュレーションが動作する()
        {
            string src = @"stage value as float holds 10;

param addValue = now * 1.5;

init {
    value <= -1000;
}

operation {
    value<next> = value<now> + addValue;
}";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(-1000 + ((maxTime - 1) * maxTime / 2) * 1.5, instance.value[maxTime % 11]);
        }

        [TestMethod]
        public async Task CSharpの予約後を識別子名に使用したシミュレーションが動作する()
        {
            string src = @"stage byte as float;
stage char as int;

param const = 3;
param public as int;

init {
    byte <= double(const);
}

operation {
    byte<next> = double(byte<now>);
}

func double(x) = x * 2;
";

            const int maxTime = 10;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual((double)(6 << maxTime), instance.@byte[maxTime]);
        }



        [TestMethod]
        public async Task ローカル変数の型推論が正しく動作する()
        {
            string src = @"
stage stage_1 as float;
stage array as float[10, 10];

param param_1 as float {
    var value = 5;
    value = value / 2;
    return value;
}

init {
    var value = 5;
    value = value / 2;

    {
        var innnerValue = 4;
        innnerValue = 3;
    }

    stage_1<0> = value;
}

operation {
    var value = 5;
    value = value / 2;

    {
        var innnerValue = 4;
        innnerValue = 3;

        if (innnerValue >= 3) {
            var newValue = 123;

            while (newValue > 150) {
                var newWhileValue = 170;
            }
        } else if (innnerValue >= 5) {
            var newValue = 124;
        } else {
            var newValue = 125;
        }
    }

    each (at i, j in array) {
        var innnerValue = 3.4;
        array[i, j] <= innnerValue;
    }

    stage_1<next> = stage_1<now> + value;
}

func f() as float {
    var value = 5;
    value = value / 2;

    {
        var innnerValue = 4;
        innnerValue = 3;
    }

    return value;
}

func g() as int {
    var value as int = 5;
    value = value idiv 2;

    {
        var innnerValue = 4;
        innnerValue = 3;
    }

    return value;
}
";

            const int maxTime = 5;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(2.5 + 2.5 * maxTime, instance.stage_1[maxTime]);

            Interpreter itp = new Interpreter(src, new CSharpCodeProvider(), option);
            Assert.AreEqual(2.5, (await itp.InterpretAsync("f();")).Result);
            Assert.AreEqual(2, (await itp.InterpretAsync("g();")).Result);
        }

        [TestMethod]
        public async Task フィボナッチ数列を正しく計算できる()
        {
            string src = @"
stage fib as int;

init {
    fib<0> = 0;
}

operation {
    if (now = 0) {
        fib<next> = 1;
    } else {
        fib<next> = fib<now> + fib<now - 1>;
    }
}
";

            const int maxTime = 40;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            // フィボナッチ数列を計算
            int[] fib = new int[maxTime + 1];
            fib[0] = 0;
            fib[1] = 1;
            for (int i = 2; i <= maxTime; i++)
            {
                fib[i] = fib[i - 1] + fib[i - 2];
            }

            for (int i = 0; i <= maxTime; i++)
            {
                Assert.AreEqual(fib[i], instance.fib[i]);
            }
        }

        [TestMethod]
        public async Task hold節を指定してフィボナッチ数列を正しく計算できる()
        {
            string src = @"
stage fib as int holds 2;

init {
    fib<0> = 0;
}

operation {
    if (now = 0) {
        fib<next> = 1;
    } else {
        fib<next> = fib<now> + fib<now - 1>;
    }
}
";

            const int maxTime = 40;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            // フィボナッチ数列を計算
            int[] fib = new int[maxTime + 1];
            fib[0] = 0;
            fib[1] = 1;
            for (int i = 2; i <= maxTime; i++)
            {
                fib[i] = fib[i - 1] + fib[i - 2];
            }

            for (int i = maxTime - 2; i <= maxTime; i++)
            {
                Assert.AreEqual(fib[i], instance.fib[i % 3]);
            }
        }

        [TestMethod]
        public async Task 一次元配列が動作する()
        {
            string src = @"
stage array as int[x];

param x = 1000;

init {
    var i as int = 0;
    while (i < x) {
        array<0>[i] = i;
        i++;
    }
}

operation {
    var i as int = 0;
    while (i < x) {
        array<next>[i] = array<now>[i] + 1;
        i++;
    } 
}
";

            const int maxTime = 100;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(i + maxTime, instance.array[maxTime][i]);
            }
        }

        [TestMethod]
        public async Task ジャグ配列の二次元配列を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, y];

param x = 10;
param y = 20;
param initialValue = -123;

init {
    var i as int = 0;
    
    while (i < x) {
        var j as int = 0;
        while (j < y) {
            array<0>[i, j] = initialValue;
            j++;
        }
        i++;
    }
}

operation {
    var i as int = 0;
    
    while (i < x) {
        var j as int = 0;
        while (j < y) {
            array<next>[i, j] = array<now>[i, j] + 10;
            j++;
        }
        i++;
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Assert.AreEqual(-123 + 10 * maxTime, instance.array[maxTime][i][j]);
                }
            }
        }

        [TestMethod]
        public async Task hold節を使用する配列を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, y] holds 1;

param x = 10;
param y = 20;
param initialValue = -123;

init {
    var i as int = 0;
    
    while (i < x) {
        var j as int = 0;
        while (j < y) {
            array<0>[i, j] = initialValue;
            j++;
        }
        i++;
    }
}

operation {
    var i as int = 0;
    
    while (i < x) {
        var j as int = 0;
        while (j < y) {
            array<next>[i, j] = array<now>[i, j] + 10;
            j++;
        }
        i++;
    }
}
";

            const int maxTime = 100;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Assert.AreEqual(-123 + 10 * maxTime, instance.array[maxTime % 2][i][j]);
                }
            }
        }

        [TestMethod]
        public async Task hold節を使用して巨大な配列を使い回すシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, y] holds 1;

param x = 1000;
param y = 2000;
param initialValue = -123;

init {
    var i as int = 0;
    
    while (i < x)  {
        var j as int = 0;
        while (j < y) {
            array<0>[i, j] = initialValue;
            j++;
        }
        i++;
    }
}

operation {
    var i as int = 0;
    
    while (i < x) {
        var j as int = 0;
        while (j < y) {
            array<next>[i, j] = array<now>[i, j] + 10;
            j++;
        }
        i++;
    }
}
";

            const int maxTime = 10;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Assert.AreEqual(-123 + 10 * maxTime, instance.array[maxTime % 2][i][j]);
                }
            }
        }

        [TestMethod]
        public async Task パラメーターを後から指定するシミュレーションが動作する()
        {
            string src = @"
stage st as float;

param x as int;
param y as long;
param z as float;

init {
    st<0> = 0;
}

operation {
    st<next> = st<now> + x + y + z;
}
";

            const int maxTime = 1000;
            var initials = new object[] { 1, (long)int.MaxValue + 10, 2.3 };
            double add = 1 + (long)int.MaxValue + 10 + 2.3;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            object instance = new Simulator().CreateInstance(compilationResult, maxTime);

            int ii = 0;
            foreach (var item in compilationResult.Unit.GetParameters(compilationResult.CompileOption).Where(p => p.IsInitializationNeeded))
            {
                instance.GetType().GetProperty(item.CSharpPropertyName).SetValue(instance, initials[ii]);
                ii++;
            }

            await new Simulator().SimulateAsync((ISimulation)instance, maxTime);

            double exprected = 0;
            for (int i = 0; i < maxTime; i++)
            {
                exprected += add;
            }

            Assert.AreEqual(exprected, ((dynamic)instance).st[maxTime]);
        }

        [TestMethod]
        public async Task 後から指定されたパラメーターを要素に持つ配列が動作する()
        {
            string src = @"
stage array as int[x, y] holds 10;

param x as int;
param y as int;
param val as int;

init {
    array<0>[x-1, y-1] = val;
}

operation {
    array<next>[x-1, y-1] = array<now>[x-1, y-1] + val;
}
";

            const int maxTime = 1000;
            int x = 200, y = 300, val = 400;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);

            instance.x = x;
            instance.y = y;
            instance.val = val;

            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(val * (maxTime + 1), instance.array[maxTime % 11][x - 1][y - 1]);
        }

        [TestMethod]
        public async Task nowやnextを省略したコードが動作する()
        {
            string src = @"stage value as float;

param addValue = now * 1.5;

init {
    value <= -1000;
}

operation {
    value <= value + addValue;
}";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(-1000 + ((maxTime - 1) * maxTime / 2) * 1.5, instance.value[maxTime]);
        }

        [TestMethod]
        public async Task nowやnextを省略したコード_配列を使用_が動作する()
        {
            string src = @"
stage array as int[x, y] holds 10;

param x = 100;
param y = 200;
param val = 123;

init {
    array[x-1,y-1] <= 0;
}

operation {
    array[x-1,y-1] <= array[x-1,y-1] + val;
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(123 * maxTime, instance.array[maxTime % 11][99][199]);
        }

        [TestMethod]
        public async Task 一次元配列に対するeach文を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x];

param x = 1000;
param val = -123;

init {
    each (at i in array) {
        array[i] <= val;
    }
}

operation {
    each (at i in array) {
        array[i] <= array[i] + val;
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(-123 * (maxTime + 1), instance.array[maxTime][i]);
            }
        }

        [TestMethod]
        public async Task 一次元配列に対するeach文_parallel指定_を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x];

param x = 1000;
param val = -123;

init {
    parallel each (at i in array) {
        array[i] <= val;
    }
}

operation {
    parallel each (at i in array) {
        array[i] <= array[i] + val;
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption() { ParallelizationOption = ParallelizationOption.InParallelSpecifiedCode };
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(-123 * (maxTime + 1), instance.array[maxTime][i]);
            }
        }

        [TestMethod]
        public async Task 一次元配列に対するfor文_parallel指定_を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x];

param x = 1000;
param val = -123;

init {
    parallel for (i = 0 to x - 1) {
        array[i] <= val;
    }
}

operation {
    parallel for (i = 0 to x - 1) {
        array[i] <= array[i] + val;
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption() { ParallelizationOption = ParallelizationOption.InParallelSpecifiedCode };
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(-123 * (maxTime + 1), instance.array[maxTime][i]);
            }
        }

        [TestMethod]
        public async Task 二時元配列に対するeach文を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, y];

param x = 10;
param y = 20;
param val = -123;

init {
    each (at i, j in array) {
        array[i, j] <= val;
    }
}

operation {
    each (at i, j in array) {
        array[i, j] <= array[i, j] + val;
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Assert.AreEqual(-123 * (maxTime + 1), instance.array[maxTime][i][j]);
                }
            }
        }

        [TestMethod]
        public async Task 二時元配列に対するeach文_parallel指定_を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, y];

param x = 10;
param y = 20;
param val = -123;

init {
    parallel each (at i, j in array) {
        array[i, j] <= val;
    }
}

operation {
    parallel each (at i, j in array) {
        if (i = j) {
            array[i, j] <= val * 2;
            continue;
        }
        array[i, j] <= array[i, j] + val;
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption() { ParallelizationOption = ParallelizationOption.InParallelSpecifiedCode };
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Assert.AreEqual(i == j ? -123 * 2 : -123 * (maxTime + 1), instance.array[maxTime][i][j]);
                }
            }
        }

        [TestMethod]
        public async Task 二時元配列に対するfor文_parallel指定_を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, y];

param x = 10;
param y = 20;
param val = -123;

init {
    parallel for (i = 0 to x - 1) {
        for (j = 0 to y - 1) {
            array[i, j] <= val;
        }
    }
}

operation {
    parallel for (i = 0 to x - 1) {
        for (j = 0 to y - 1) {
            if (i = j) {
                array[i, j] <= val * 2;
                continue;
            }
            array[i, j] <= array[i, j] + val;
        }
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption() { ParallelizationOption = ParallelizationOption.InParallelSpecifiedCode };
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Assert.AreEqual(i == j ? -123 * 2 : -123 * (maxTime + 1), instance.array[maxTime][i][j]);
                }
            }
        }

        [TestMethod]
        public async Task 二時元配列に対するfor文_parallel指定_で複雑にcontinueを使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, y];

param x = 10;
param y = 20;
param val = -123;

init {
    parallel for (i = 0 to x - 1) {
        for (j = 0 to y - 1) {
            array[i, j] <= val;
        }
    }
}

operation {
    parallel for (i = 0 to x - 1) {
        if (i = 5) {
            continue;
        } else if (i = 6) {
            continue;
        }

        {
            if (i = 7) {
                continue;
            }
        }

        for (j = 0 to y - 1) {
            if (i = j) {
                array[i, j] <= val * 2;
                continue;
            }

            array[i, j] <= array[i, j] + val;
        }
    }
}
";

            const int maxTime = 1000;

            Func<int, int, int> expected = (i, j) =>
            {
                if (i == 5 || i == 6 || i == 7)
                {
                    return 0;
                }
                else if (i == j)
                {
                    return -123 * 2;
                }
                else
                {
                    return -123 * (maxTime + 1);
                }
            };

            CompileOption option = new CompileOption() { ParallelizationOption = ParallelizationOption.InParallelSpecifiedCode };
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Assert.AreEqual(expected(i, j), instance.array[maxTime][i][j]);
                }
            }
        }

        [TestMethod]
        public async Task 二次元配列に対するeach文内でifを使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, y];

param x = 10;
param y = 20;
param val = -123;

init {
    each (at i, j in array) {
        if (i mod 2 = 0) {
            array[i, j] <= val;
        }
    }
}

operation {
    each (at i, j in array) {
        if (i mod 2 = 0) {
            array[i, j] <= array[i, j] + val;
        }
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    Assert.AreEqual(i % 2 == 0 ? -123 * (maxTime + 1) : 0, instance.array[maxTime][i][j]);
                }
            }
        }

        [TestMethod]
        public async Task 高次元配列に対するeach文を使用したシミュレーションが動作する()
        {
            string src = @"
stage array as int[x, x, x, x, x];

param x = 3;
param val = -123;

init {
    each (at a, b, c, d, e in array) {
        array[a, b, c, d, e] <= val;
    }
}

operation {
    each (at a, b, c, d, e in array) {
        array[a, b, c, d, e] <= array[a, b, c, d, e] + val;
    }
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            int x = 3;

            for (int a = 0; a < x; a++)
            {
                for (int b = 0; b < x; b++)
                {
                    for (int c = 0; c < x; c++)
                    {
                        for (int d = 0; d < x; d++)
                        {
                            for (int e = 0; e < x; e++)
                            {
                                Assert.AreEqual(-123 * (maxTime + 1), instance.array[maxTime][a][b][c][d][e]);
                            }
                        }
                    }
                }
            }
        }

        [TestMethod]
        public async Task ステージ用の複合代入演算子_加算_が正しく動作する()
        {
            string src = @"
stage x as float;

operation {
    x <= 1.23;
    x +<= 1.5;
}
";

            const int maxTime = 5;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(1.23 + 1.5, ((dynamic)instance).x[maxTime]);
        }

        [TestMethod]
        public async Task ステージ用の複合代入演算子_減算_が正しく動作する()
        {
            string src = @"
stage x as float;

operation {
    x <= 1.23;
    x -<= 1.5;
}
";

            const int maxTime = 5;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(1.23 - 1.5, ((dynamic)instance).x[maxTime]);
        }

        [TestMethod]
        public async Task ステージ用の複合代入演算子_乗算_が正しく動作する()
        {
            string src = @"
stage x as float;

operation {
    x <= 1.23;
    x *<= 1.5;
}
";

            const int maxTime = 5;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(1.23 * 1.5, ((dynamic)instance).x[maxTime]);
        }

        [TestMethod]
        public async Task ステージ用の複合代入演算子_除算_が正しく動作する()
        {
            string src = @"
stage x as float;

operation {
    x <= 1.23;
    x /<= 1.5;
}
";

            const int maxTime = 5;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            Assert.AreEqual(1.23 / 1.5, ((dynamic)instance).x[maxTime]);
        }

        [TestMethod]
        public async Task 簡単な構造体を用いたコードが正しく動作する()
        {
            string src = @"
stage p as Point;

struct Point = (x, y);

init {
    p.x <= 1;
    p.y <= 2;
}

operation {
    p<next>.x = p<now>.x + 2;
    p<next>.y = p<now>.y + 3;
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            dynamic point = ((dynamic)instance).p[maxTime];
            Assert.AreEqual(1.0 + 2.0 * maxTime, point.x);
            Assert.AreEqual(2.0 + 3.0 * maxTime, point.y);
        }

        [TestMethod]
        public async Task 構造体がネストするコードが正しく動作する()
        {
            string src = @"
stage c as C;

struct A = (x as int, y as float);
struct B = (a as A, z as long);
struct C = (a as A, b as B);

init {
    c.b.a.x <= -123;
}

operation {
    c<next>.b.a.x = c<now>.b.a.x + 10;
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            dynamic c = ((dynamic)instance).c[maxTime];
            Assert.AreEqual(-123 + 10 * maxTime, c.b.a.x);
        }

        [TestMethod]
        public async Task 構造体がネストするコードが正しく動作する_定義順によらない_()
        {
            string src = @"
stage c as C;

struct C = (a as A, b as B);
struct B = (a as A, z as long);
struct A = (x as int, y as float);

init {
    c.b.a.x <= -123;
}

operation {
    c<next>.b.a.x = c<now>.b.a.x + 10;
}
";

            const int maxTime = 1000;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime);

            dynamic c = ((dynamic)instance).c[maxTime];
            Assert.AreEqual(-123 + 10 * maxTime, c.b.a.x);
        }

        [TestMethod]
        public async Task 外部からステージを初期化するコードが動作する()
        {
            string src = @"
stage x as float;

operation {
    x <= x + 2.0;
}
";

            const int maxTime = 10;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime, new[] { new StageInitInfo("x", () => -123.456) });

            Assert.AreEqual(-123.456 + 2.0 * maxTime, ((dynamic)instance).x[maxTime]);
        }

        [TestMethod]
        public async Task 外部からステージ_配列_を初期化するコードが動作する()
        {
            string src = @"
stage x as int[10];

operation {
    each (at i in x) {
        x[i] <= x[i] + 2;
    }
}
";

            const int maxTime = 10;

            CompileOption option = new CompileOption();
            var compilationResult = await new Compiler(new CSharpCodeProvider()).CompileFromRippleSrcAsync(src, option);
            dynamic instance = new Simulator().CreateInstance(compilationResult, maxTime);
            await new Simulator().SimulateAsync(instance, maxTime,
                new[] { new StageInitInfo("x", () => new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }) });

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i + 1 + 2 * maxTime, ((dynamic)instance).x[maxTime][i]);
            }
        }
    }
}
