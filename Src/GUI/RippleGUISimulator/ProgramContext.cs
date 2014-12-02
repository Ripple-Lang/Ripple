using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonControlsOnCLI.TaskDialogs;
using Ripple.Compilers.CodeGenerations;
using Ripple.Compilers.Options;
using Ripple.GUISimulator.Scripts;
using Ripple.GUISimulator.Windows;
using Ripple.GUISimulator.Windows.Windows;

namespace Ripple.GUISimulator
{
    class ProgramContext
    {
        public ImportedPlugins ImportedPlugins { get; private set; }

        private string scriptPath;
        private Script script = null;

        public bool IsOpeningScript
        {
            get { return scriptPath != null; }
        }

        public ProgramContext(ImportedPlugins importedPlugins, string scriptPath = null)
        {
            this.ImportedPlugins = importedPlugins;
            this.scriptPath = scriptPath;
        }

        public async Task ShowFirstWindow()
        {
            if (!this.IsOpeningScript)
            {
                new CodeEditorWindow()
                {
                    DataContext = new CodeEditorWindowViewModel(this)
                }.Show();
            }
            else
            {
                var progressWindow = new ProgressWindow() { DataContext = new { What = "スクリプトを読み込んで" } };
                progressWindow.Show();

                // スクリプトを読み込む
                using (var stream = File.Open(scriptPath, FileMode.Open, FileAccess.Read))
                {
                    using (var scriptFile = new ScriptFile(stream, ScriptFile.FileOpenMode.Read))
                    {
                        script = scriptFile.ReadScript();
                    }
                }

                // この場でコンパイルする
                var compilationResult = await CompileAsync(script.RippleSrc, new CompileOption());

                if (compilationResult != null && !compilationResult.HasErrors)
                {
                    // シミュレーション開始画面の表示
                    new SimulationStartingWindow()
                    {
                        DataContext = new SimulationStartingPaneViewModel(this, script.RippleSrc, compilationResult)
                    }.Show();
                }
                else
                {
                    Environment.Exit(1);
                }

                progressWindow.Close();
            }
        }

        public void ShowVisualizationWindow(string rippleSrc, object simulationResult, int maxTime, TimeSpan elapsed, IEnumerable<string> stages, CompileOption compileOption)
        {
            var viewModel = new VisualizationWindowViewModel(rippleSrc, simulationResult, maxTime, elapsed, stages, ImportedPlugins, compileOption);

            if (script != null)
            {
                using (var file = new FileStream(scriptPath, FileMode.Open, FileAccess.Read))
                using (var scriptFile = new ScriptFile(file, ScriptFile.FileOpenMode.Read))
                using (var stream = scriptFile.GetVisualizationToolDataStream())
                {
                    viewModel.Visualize(
                        ImportedPlugins.Visualizers.First(p => p.Value.Name == script.VisualizationInfo.VisualizationToolsName).Value,
                        script.VisualizationInfo.Stages,
                        stream);
                }
            }

            new VisualizationWindow()
            {
                DataContext = viewModel
            }.Show();
        }

        public async Task<CompilationResult> CompileAsync(string src, CompileOption option)
        {
            using (var provider = new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider())
            {
                Compiler compiler = new Compiler(provider);
                try
                {
                    var compilationResult = await compiler.CompileFromRippleSrcAsync(src, option);

                    if (compilationResult.HasErrors)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var item in compilationResult.ErrorsAndWarnings)
                        {
                            sb.AppendLine(item.Detail);
                        }
                        if (compilationResult.CSharpCompilerResults != null)
                        {
                            foreach (var item in compilationResult.CSharpCompilerResults.Errors)
                            {
                                sb.AppendLine(item.ToString());
                            }
                        }

                        // エラー処理
                        TaskDialogCLI.Show("コンパイルエラー", "コンパイル中にエラーが発生しました。", sb.ToString(), TaskDialogButtons.OK, TaskDialogIcons.ERROR);
                    }

                    return compilationResult;
                }
                catch (Exception e)
                {
                    TaskDialogCLI.ShowIndirect(new TaskDialogConfig()
                    {
                        Flags = TaskDialogFlags.EXPAND_FOOTER_AREA,
                        WindowTitle = "コンパイルエラー",
                        MainInstruction = "コンパイル中に深刻なエラーが発生しました。",
                        MainIcon = TaskDialogIcons.ERROR,
                        CommonButtons = TaskDialogButtons.OK,
                        ExpandedInformation = e.ToString() + Environment.NewLine + e.StackTrace,
                    });
                    return null;
                }
            }
        }
    }
}
