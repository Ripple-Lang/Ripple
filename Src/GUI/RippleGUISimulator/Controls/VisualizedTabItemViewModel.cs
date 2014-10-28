using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using CommonControlsOnCLI.TaskDialogs;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Ripple.Compilers.Options;
using Ripple.GUISimulator.Behaviors;
using Ripple.GUISimulator.Scripts;
using Ripple.VisualizationInterfaces;

namespace Ripple.GUISimulator.Controls
{
    class VisualizedTabItemViewModel : ViewModelBase
    {
        public IEnumerable<string> Stages { get; private set; }
        public IStageView VisualizedControl { get; private set; }
        public string VisualizationToolsFullName { get; private set; }
        public string RippleSrc { get; private set; }
        public CompileOption CompileOption { get; private set; }

        public string StageName
        {
            get { return Stages.FirstOrDefault(); }
        }

        public VisualizedTabItemViewModel(IEnumerable<string> stages, IStageView visualizedControl, string visualizationToolsFullName, string rippleSrc, CompileOption compileOption)
        {
            this.Stages = stages;
            this.VisualizedControl = visualizedControl;
            this.VisualizationToolsFullName = visualizationToolsFullName;
            this.RippleSrc = rippleSrc;
            this.CompileOption = compileOption;
        }

        private ICommand createScriptCommand;
        public ICommand CreateScriptCommand
        {
            get
            {
                return createScriptCommand ?? (createScriptCommand = new RelayCommand(CreateScript));
            }
        }

        private void CreateScript()
        {
            MessengerInstance.Send(new FileDialogMessage(this,
                new SaveFileDialog()
                {
                    Filter = string.Format("{0} (*.{1})|*.{1}",
                        Constants.FileKindDescriptions.ScriptFile, Constants.FileExtensions.ScriptFile)
                },
                fd =>
                {
                    try
                    {
                        using (Stream stream = File.Open(fd.FileName, FileMode.Create))
                        {
                            using (var file = new ScriptFile(stream, ScriptFile.FileOpenMode.Create))
                            {
                                var script = new Script(RippleSrc, CompileOption, new VisualizationInfo(Stages.ToArray(), VisualizationToolsFullName));
                                file.WriteScript(script);
                                using (var vstream = file.CreateVisualizationToolDataStream())
                                {
                                    VisualizedControl.SerializeCurrentState(vstream);
                                }
                            }
                        }
                    }
                    catch (IOException e)
                    {
                        MessengerInstance.Send(new TaskDialogMessage(this, new TaskDialogConfig()
                        {
                            WindowTitle = "エラー",
                            MainInstruction = "スクリプトの保存中にエラーが発生しました。",
                            Content = e.Message,
                            MainIcon = TaskDialogIcons.ERROR,
                            CommonButtons = TaskDialogButtons.OK,
                        }, null));
                    }
                }));
        }
    }
}
