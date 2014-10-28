using System;
using System.CodeDom.Compiler;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeDom.Providers.DotNetCompilerPlatform;

namespace Ripple.GUISimulator
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Roslynをバックグラウンドで動作させる
            var _ = Task.Run(() =>
            {
                var p = new CSharpCodeProvider();
                p.CompileAssemblyFromSource(new CompilerParameters { GenerateInMemory = true },
                    new[] { "public class Class { public static int Func() { return 10; } public int Func2(int x) { return 20; } }" });
            });

            // http://okazuki.hatenablog.com/entry/20110507/1304772201

            if (!Directory.Exists("Plugins"))
            {
                Directory.CreateDirectory("Plugins");
            }

            // コンテナの作成
            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new DirectoryCatalog(@".\Plugins"));
            var container = new CompositionContainer(catalog);

            // プラグインコンテナを作成する
            var plugins = new ImportedPlugins();

            // プラグインを設定する
            container.SatisfyImportsOnce(plugins);

            // コマンドライン引数を確認する
            var args = Environment.GetCommandLineArgs();
            string scriptPath = null;
            if (args.Count() >= 2)
            {
                scriptPath = args[1];
            }

            //new MainWindow(plugins, scriptPath).Show();
            await new ProgramContext(plugins, scriptPath).ShowFirstWindow();
        }
    }
}
