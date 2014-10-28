using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Ripple.Compilers.CodeGenerations;
using Ripple.Compilers.Options;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.Tools.Interpretation;
using Ripple.Compilers.Tools.Simulations;
using Ripple.Compilers.Types;
using Ripple.Components;

namespace Ripple.CUISimulator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Ripple CUI Simulator");
            Console.WriteLine();

            Run(new CommandLineArguments() { SourcePath = args[0] }).Wait();
        }

        static async Task Run(CommandLineArguments arguments)
        {
            var result = await Compile(arguments);
            var simulated = await Execute(result);
            for (; ; )
            {
                try
                {
                    Visualize(result, simulated.Item1, simulated.Item2);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Console.WriteLine();
                }
            }
        }

        static async Task<CompilationResult> Compile(CommandLineArguments arguments)
        {
            Console.WriteLine("ファイル : " + arguments.SourcePath);

            // ソースコードの読み取り
            string src;
            using (var reader = new StreamReader(arguments.SourcePath))
            {
                src = reader.ReadToEnd();
            }

            // コンパイル
            Console.WriteLine("コンパイルしています...");
            CompilationResult result;
            using (var compiler = new Compiler(new CSharpCodeProvider()))
            {
                result = await compiler.CompileFromRippleSrcAsync(src, arguments.Option);
            }
            Console.WriteLine("コンパイルが完了しました");
            Console.WriteLine();

            return result;
        }

        static async Task<Tuple<ISimulation, DateTime>> Execute(CompilationResult result)
        {
            // 計算する最大の時刻
            Console.Write("時刻いくつまで計算しますか? : ");
            int maxTime = int.Parse(Console.ReadLine());
            Console.WriteLine();

            // 入力が必要なパラメーター
            var inputParams = from p in result.Unit.GetParameters(result.CompileOption)
                              where p.IsInitializationNeeded
                              select new InputParameter(p);
            inputParams = inputParams.ToArray();

            if (inputParams.Count() > 0)
            {
                Console.WriteLine("パラメーターを入力してください");
                foreach (var p in inputParams)
                {
                    Console.Write("param {0} as {1} = ", p.Parameter.Name, p.Parameter.Type.RippleName);
                    p.Value = Console.ReadLine();
                }
            }

            // インスタンスの生成
            var simulator = new Simulator();
            var instance = simulator.CreateInstance(result, maxTime);

            // 入力が必要なパラメーターの設定
            foreach (var p in inputParams)
            {
                var interpreted = await new Interpreter(new CompileOption()).InterpretAsync(p.Value + ";");
                instance.GetType().GetProperty(p.Parameter.Name).SetValue(instance, interpreted.Result);
            }
            Console.WriteLine();

            // 時刻変化のイベントハンドラ
            int onePercent = maxTime / 100;
            simulator.OnTimeChanged += (_, time) =>
            {
                if (time % onePercent == 0)
                {
                    Console.Write("{0,3}%完了しました...\r", time / onePercent);
                }
            };

            // シミュレーションの実行
            Console.WriteLine("シミュレーションしています...");
            await simulator.SimulateAsync(instance, maxTime);
            Console.WriteLine("シミュレーションが完了しました");
            Console.WriteLine();

            return new Tuple<ISimulation, DateTime>(instance, DateTime.Now);
        }

        static void Visualize(CompilationResult result, ISimulation simulatedInstance, DateTime date)
        {
            Console.Write("結果を表示したいステージを入力してください : ");
            string stageName = Console.ReadLine();
            Console.WriteLine();

            var stage = result.Unit.GetStages(result.CompileOption).Single(s => s.Name == stageName);
            var stageObject = simulatedInstance.GetType().GetField(stage.Name).GetValue(simulatedInstance);

            string fileName;

            if (stage.Type is ArrayType)
            {
                Console.Write("選択されたステージは配列です。どの時刻を表示しますか : ");
                int time = int.Parse(Console.ReadLine());
                Console.WriteLine();

                fileName = GetFileName(stageName, time, date);

                using (var writer = new StreamWriter(fileName))
                {
                    int numDimemsions = GetDimensions(stageObject);
                    if (numDimemsions == 1)
                    {
                        ArrayFormatter.Format1DText(writer, ((dynamic)stageObject)[time], "\t");
                    }
                    else
                    {
                        ArrayFormatter.Format2DText(writer, ((dynamic)stageObject)[time], "\t");
                    }
                }
            }
            else
            {
                fileName = GetFileName(stageName, -1, date);
                using (var writer = new StreamWriter(fileName))
                {
                    foreach (var item in (Array)stageObject)
                    {
                        writer.WriteLine(item);
                    }
                }
            }

            Console.WriteLine("結果はファイル\"" + fileName + "\"に出力されました");
            Console.WriteLine();
        }

        private static string GetFileName(string stageName, int time, DateTime date)
        {
            string dateString = date.ToString("yyyyMMdd_HHmmss");
            if (time >= 0)
            {
                return dateString + "_" + stageName + "_" + time + ".txt";
            }
            else
            {
                return dateString + "_" + stageName + ".txt";
            }
        }

        private static int GetDimensions(object array)
        {
            int dimension = 0;
            while (array is Array)
            {
                array = ((Array)array).GetValue(0);
                dimension++;
            }
            return dimension;
        }
    }

    class CommandLineArguments
    {
        public string SourcePath { get; set; }
        public CompileOption Option { get; private set; }

        public CommandLineArguments()
        {
            this.Option = new CompileOption();
        }
    }

    class InputParameter
    {
        public Parameter Parameter { get; private set; }
        public string Value { get; set; }

        public InputParameter(Parameter parameter)
        {
            this.Parameter = parameter;
        }
    }
}
