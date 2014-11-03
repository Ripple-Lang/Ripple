using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommonControlsOnCLI.TaskDialogs;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Ripple.Compilers.CodeGenerations;
using Ripple.Compilers.Options;
using Ripple.GUISimulator.Behaviors;
using Ripple.GUISimulator.Windows.Windows;

namespace Ripple.GUISimulator.Windows
{
    class CodeEditorWindowViewModel : ViewModelBase
    {
        private static readonly string FileDialogFilter =
            string.Format("{0} (*.{1})|*.{1}|{2} (*.*)|*.*",
                Constants.FileKindDescriptions.RippleSrcFile, Constants.FileExtensions.RippleSrcFile,
                Constants.FileKindDescriptions.AllKindFile);

        public CodeEditorWindowViewModel(ProgramContext programContext)
        {
            this.ProgramContext = programContext;
            this.Code = string.Empty;
            this.CompileOption = new CompileOption();

            this.CodeWasSaved = true;
            this.Compiling = false;

            this.IsRightPaneShown = false;
        }

        #region フィールドやプロパティ

        public int ViewWindowNo { get; set; }

        public ProgramContext ProgramContext { get; private set; }
        public CompileOption CompileOption { get; set; }

        private string fileName = null;
        public string FileName
        {
            get { return fileName; }
            set
            {
                if (fileName != value)
                {
                    fileName = value;
                    RaisePropertyChanged("FileName");
                    RaisePropertyChanged("FileTitle");
                }
            }
        }

        public string FileTitle
        {
            get { return FileName != null ? Path.GetFileNameWithoutExtension(FileName) : "無題"; }
        }

        private string code;
        public string Code
        {
            get { return code; }
            set
            {
                code = value;
                CodeWasSaved = false;
            }
        }

        private bool codeWasSaved;
        public bool CodeWasSaved
        {
            get { return codeWasSaved; }
            set
            {
                if (codeWasSaved != value)
                {
                    codeWasSaved = value;
                    RaisePropertyChanged("CodeWasSaved");
                }
            }
        }

        private bool compiling;
        public bool Compiling
        {
            get { return compiling; }
            private set
            {
                if (compiling != value)
                {
                    compiling = value;
                    RaisePropertyChanged("Compiling");
                    RaisePropertyChanged("ProgressBarVisibility");
                    ((RelayCommand)CompileCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public Visibility ProgressBarVisibility
        {
            get { return Compiling ? Visibility.Visible : Visibility.Hidden; }
        }

        private string statusBarString;
        public string StatusBarString
        {
            get { return statusBarString; }
            set
            {
                if (statusBarString != value)
                {
                    statusBarString = value;
                    RaisePropertyChanged("StatusBarString");
                }
            }
        }

        private bool isRightPaneShown;
        public bool IsRightPaneShown
        {
            get { return isRightPaneShown; }
            set
            {
                if (isRightPaneShown != value)
                {
                    isRightPaneShown = value;
                    RaisePropertyChanged("IsRightPaneShown");
                    ((RelayCommand)CloseSimulationStartingPaneCommand).RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        #region コマンド

        private ICommand compileCommand;
        public ICommand CompileCommand
        {
            get
            {
                if (compileCommand == null)
                {
                    compileCommand = new RelayCommand(
                        async () => { await Compile(); },
                        () => { return !compiling; });
                }

                return compileCommand;
            }
        }

        private async Task Compile()
        {
            Compiling = true;
            StatusBarString = "コンパイルしています...";

            CompilationResult compilationResult = await ProgramContext.CompileAsync(Code, CompileOption);

            if (compilationResult != null && !compilationResult.HasErrors)
            {
                MessengerInstance.Send(new ShowSimulationStartingPaneMessage(
                    new SimulationStartingPaneViewModel(ProgramContext, Code, compilationResult)), ViewWindowNo);
                IsRightPaneShown = true;

                StatusBarString = "コンパイルが正常に完了しました。";
            }
            else
            {
                StatusBarString = "コンパイルは失敗しました。";
            }

            Compiling = false;
        }

        private ICommand newWindowCommand;
        public ICommand NewWindowCommand
        {
            get
            {
                return newWindowCommand ??
                    (newWindowCommand = new RelayCommand(
                        async () => await new ProgramContext(ProgramContext.ImportedPlugins).ShowFirstWindow()));
            }
        }

        private ICommand showOptionWindowCommand;
        public ICommand ShowOptionWindowCommand
        {
            get
            {
                if (showOptionWindowCommand == null)
                {
                    showOptionWindowCommand = new RelayCommand(() =>
                    {
                        var cloned = (CompileOption)CompileOption.Clone();
                        var dialog = new Windows.OptionWindow()
                        {
                            DataContext = cloned
                        };

                        if (dialog.ShowDialog().Value)
                        {
                            CompileOption = (CompileOption)dialog.DataContext;
                        }
                    });
                }

                return showOptionWindowCommand;
            }
        }

        private ICommand openSourceCommand;
        public ICommand OpenSourceCommand
        {
            get
            {
                if (openSourceCommand == null)
                {
                    openSourceCommand = new RelayCommand(OpenSourceFile);
                }

                return openSourceCommand;
            }
        }

        private ICommand saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (saveCommand == null)
                {
                    saveCommand = new RelayCommand(SaveFile);
                }

                return saveCommand;
            }
        }

        private void OpenSourceFile()
        {
            MessengerInstance.Send(new FileDialogMessage(this, GetOpenFileDialog(), fd =>
            {
                try
                {
                    FileName = fd.FileName;
                    InternalReadFile();
                }
                catch
                {
                    // 何もしない
                }
            }));
        }

        private void SaveFile()
        {
            MessengerInstance.Send(new FileDialogMessage(this, GetSaveFileDialog(), fd =>
            {
                try
                {
                    FileName = fd.FileName;
                    InternalSaveFile();
                }
                catch
                {
                    // 何もしない
                }
            }));
        }

        private void InternalReadFile()
        {
            try
            {
                using (var reader = new StreamReader(FileName))
                {
                    this.Code = reader.ReadToEnd();
                    this.CodeWasSaved = true;
                    RaisePropertyChanged("Code");
                }
            }
            catch (Exception e)
            {
                MessengerInstance.Send(new TaskDialogMessage(this, new TaskDialogConfig()
                {
                    WindowTitle = "エラー",
                    MainInstruction = "ファイルの読み込み中にエラーが発生しました。",
                    Content = e.Message,
                    CommonButtons = TaskDialogButtons.OK,
                    MainIcon = TaskDialogIcons.ERROR,
                }, null));

                throw;
            }
        }

        private void InternalSaveFile()
        {
            try
            {
                using (var writer = new StreamWriter(FileName))
                {
                    writer.Write(Code);
                    this.CodeWasSaved = true;
                }
            }
            catch (Exception e)
            {
                MessengerInstance.Send(new TaskDialogMessage(this, new TaskDialogConfig()
                {
                    WindowTitle = "エラー",
                    MainInstruction = "ファイルの書き込み中にエラーが発生しました。",
                    Content = e.Message,
                    CommonButtons = TaskDialogButtons.OK,
                    MainIcon = TaskDialogIcons.ERROR,
                }, null));

                throw;
            }
        }

        private ICommand openScriptFileCommand;
        public ICommand OpenScriptFileCommand
        {
            get
            {
                return openScriptFileCommand ?? (openScriptFileCommand = new RelayCommand(OpenScriptFile));
            }
        }

        private void OpenScriptFile()
        {
            if (PromoteFileSaveIfNotSaved())
            {
                OpenFileDialog ofd = new OpenFileDialog()
                {
                    Title = "スクリプトファイルを開く",
                    Filter = string.Format("{0} (*.{1})|*.{1}|{2} (*.*)|*.*",
                        Constants.FileKindDescriptions.ScriptFile, Constants.FileExtensions.ScriptFile, Constants.FileKindDescriptions.AllKindFile)
                };

                MessengerInstance.Send(new FileDialogMessage(this, ofd, async fd =>
                {
                    await new ProgramContext(this.ProgramContext.ImportedPlugins, fd.FileName).ShowFirstWindow();
                    //MessengerInstance.Send(new CloseWindowMessage(), ViewWindowNo);
                }));
            }
        }

        private ICommand closeSimulationStartingPaneCommand;
        public ICommand CloseSimulationStartingPaneCommand
        {
            get
            {
                return closeSimulationStartingPaneCommand ??
                    (closeSimulationStartingPaneCommand = new RelayCommand(
                        () =>
                        {
                            MessengerInstance.Send(new CollapseSimulationStartingPaneMessage(), ViewWindowNo);
                            this.IsRightPaneShown = false;
                        },
                        () => this.IsRightPaneShown));
            }
        }

        private ICommand showVersionInfoCommand;
        public ICommand ShowVersionInfoCommand
        {
            get
            {
                return showVersionInfoCommand ??
                    (showVersionInfoCommand = new RelayCommand(
                        () => new VersionInfoWindow().ShowDialog()));
            }
        }

        #region ファイルダイアログ関連

        private OpenFileDialog GetOpenFileDialog()
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = FileDialogFilter,
                FileName = FileName,
            };

            SetFileDialogLocation(ofd, FileName);
            return ofd;
        }

        private SaveFileDialog GetSaveFileDialog()
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = FileDialogFilter,
                FileName = FileName,
            };

            SetFileDialogLocation(sfd, FileName);
            return sfd;
        }

        private static void SetFileDialogLocation(FileDialog dialog, string path)
        {
            if (path != null)
            {
                dialog.InitialDirectory = Path.GetDirectoryName(path);
                dialog.FileName = Path.GetFileName(path);
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns>プログラムを終了する場合はtrue</returns>
        internal bool PromoteFileSaveIfNotSaved()
        {
            if (codeWasSaved)
            {
                return true;
            }

            string fileTitle = FileName == null ? "無題" : Path.GetFileName(FileName);

            const int ID_Save = 0, ID_NotSave = 1, ID_Cancel = 2;

            var result = TaskDialogCLI.ShowIndirect(new TaskDialogConfig()
            {
                Flags = TaskDialogFlags.ALLOW_DIALOG_CANCELLATION,
                WindowTitle = "ファイル保存の確認",
                MainInstruction = fileTitle + " への変更内容を保存しますか?",
                Buttons = new TaskDialogButton[] 
                { 
                    new TaskDialogButton(){ ButtonID = ID_Save, ButtonText = "保存する(&S)" },
                    new TaskDialogButton(){ ButtonID = ID_NotSave, ButtonText = "保存しない(&N)" },
                    new TaskDialogButton(){ ButtonID = ID_Cancel, ButtonText = "キャンセル(&C)" },
                },
            });

            if ((int)result == ID_Cancel)
            {
                return false;
            }
            else if ((int)result == ID_Save)
            {
                try
                {
                    if (FileName == null)
                    {
                        var sfd = GetSaveFileDialog();

                        if (!sfd.ShowDialog().Value)
                            return false;

                        FileName = sfd.FileName;
                    }

                    InternalSaveFile();
                }
                catch
                {
                    // ファイル書き込みは失敗したので、プログラムを終了しない
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
