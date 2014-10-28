using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using Ripple.Compilers.CodeGenerations;
using Ripple.Compilers.Symbols;
using Ripple.Compilers.Tools.Interpretation;
using Ripple.Compilers.Tools.Simulations;
using Ripple.Compilers.Types;
using Ripple.Components;
using Ripple.GUISimulator.Windows.Windows;

namespace Ripple.GUISimulator.Windows
{
    class SimulationStartingPaneViewModel : ViewModelBase
    {
        #region フィールドやプロパティ

        public int ViewWindowNo { get; set; }

        #region コンストラクタにより初期化される

        public ProgramContext ProgramContext { get; private set; }
        public string RippleSrc { get; private set; }
        public CompilationResult CompilationResult { get; private set; }
        public List<StageData> Stages { get; private set; }

        private int maxTime;
        public int MaxTime
        {
            get { return maxTime; }
            set
            {
                if (maxTime != value)
                {
                    maxTime = value;
                    RaisePropertyChanged("MaxTime");
                }
            }
        }

        private bool simulating;
        public bool Simulating
        {
            get { return simulating; }
            private set
            {
                if (simulating != value)
                {
                    simulating = value;
                    RaisePropertyChanged("Simulating");
                    ((RelayCommand)StartSimulationCommand).RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        private int currentProgress;
        public int CurrentProgress
        {
            get { return currentProgress; }
            set
            {
                if (currentProgress != value)
                {
                    currentProgress = value;
                    RaisePropertyChanged("CurrentProgress");
                }
            }
        }

        private int maxProgress;
        public int MaxProgress
        {
            get { return maxProgress; }
            set
            {
                if (maxProgress != value)
                {
                    maxProgress = value;
                    RaisePropertyChanged("MaxProgress");
                }
            }
        }

        private List<ParameterData> parameterItems;
        public List<ParameterData> ParameterItems
        {
            get
            {
                if (parameterItems == null)
                {
                    parameterItems =
                        CompilationResult.Unit.GetParameters(CompilationResult.CompileOption).Where(p => p.IsInitializationNeeded)
                        .Select(p => new ParameterData()
                        {
                            Name = p.Name,
                            Type = p.Type,
                            Value = ""
                        })
                        .ToList();
                }

                return parameterItems;
            }
        }

        public bool IsPane { get; set; }

        public Visibility ModifyCodeButtonVisibility
        {
            get { return ProgramContext.IsOpeningScript ? Visibility.Visible : Visibility.Hidden; }
        }

        #endregion

        public SimulationStartingPaneViewModel(ProgramContext programContext, string rippleSrc, CompilationResult compilationResult)
        {
            this.ProgramContext = programContext;
            this.RippleSrc = rippleSrc;
            this.CompilationResult = compilationResult;
            this.Stages = compilationResult.Unit.GetStages(compilationResult.CompileOption).Select(s => new StageData(s)).ToList();
            this.MaxTime = 1;
            this.Simulating = false;
            this.IsPane = false;    // 既定ではウィンドウと見なす
        }

        #region コマンド

        private ICommand startSimulationCommand;
        public ICommand StartSimulationCommand
        {
            get
            {
                if (startSimulationCommand == null)
                {
                    startSimulationCommand = new RelayCommand(async () =>
                    {
                        Simulating = true;

                        try
                        {
                            Stopwatch sw = new Stopwatch();
                            sw.Start();

                            var returned = await StartSimulation();

                            sw.Stop();

                            object instance = returned.Item1;
                            ProgramContext.ShowVisualizationWindow(RippleSrc, instance, maxTime, sw.Elapsed, CompilationResult.Unit.GetStages(CompilationResult.CompileOption).Select(s => s.Name), CompilationResult.CompileOption);
                        }
                        catch (Exception excep)
                        {
                            MessageBox.Show(excep.ToString());
                        }

                        Simulating = false;
                    });
                }

                return startSimulationCommand;
            }
        }

        private async Task<Tuple<object, int>> StartSimulation()
        {
            const int DefaultProgressMax = 50;

            // シミュレーターインスタンスの生成
            var simulator = new Simulator();

            // 時刻変化のイベントハンドラー
            if (MaxTime <= DefaultProgressMax)
            {
                MaxProgress = MaxTime;
                CurrentProgress = 0;
                simulator.OnTimeChanged += (_, time) =>
                {
                    this.CurrentProgress = time;
                };
            }
            else
            {
                int numShifts = GetNumShiftsNearGreater(MaxTime, DefaultProgressMax);
                MaxProgress = MaxTime >> numShifts;
                CurrentProgress = 0;
                simulator.OnTimeChanged += (_, time) =>
                {
                    this.CurrentProgress = time >> numShifts;
                };
            }

            // シミュレーションオブジェクトを生成する
            var instance = simulator.CreateInstance(CompilationResult, MaxTime);

            // 入力されたパラメーターを解釈する
            Interpreter itp = new Interpreter(new Microsoft.CodeDom.Providers.DotNetCompilerPlatform.CSharpCodeProvider(), new Compilers.Options.CompileOption());
            foreach (var p in ParameterItems)
            {
                var interpreted = await itp.InterpretAsync(p.Value);
                if (interpreted.Result != null)
                {
                    instance.GetType().GetProperty(p.Name).SetValue(instance, interpreted.Result);
                }
                else
                {
                    throw new Exception("入力されたパラメーター" + p.Name + "は正しくありません。"
                        + Environment.NewLine + interpreted.Message);
                }
            }

            // ステージをファイルから読み込むかどうかの設定を処理する
            var stageInitInfos = from s in Stages
                                 where s.IsUsingFile
                                 select new StageInitInfo(s.Stage.Name, CreateArrayReadDelegate(s));

            // シミュレーションを開始し結果インスタンスを返す
            await simulator.SimulateAsync(instance, MaxTime, stageInitInfos);
            return new Tuple<object, int>(instance, MaxTime);
        }

        private ICommand showCSharpCodeCommand;
        public ICommand ShowCSharpCodeCommand
        {
            get
            {
                if (showCSharpCodeCommand == null)
                {
                    showCSharpCodeCommand = new RelayCommand(() =>
                    {
                        new CSharpCodeViewWindow(CompilationResult.GeneratedCSharpCode).Show();
                    });
                }

                return showCSharpCodeCommand;
            }
        }

        private ICommand modifyCodeCommand;
        public ICommand ModifyCodeCommand
        {
            get
            {
                return modifyCodeCommand ??
                    (modifyCodeCommand = new RelayCommand(
                        () =>
                        {
                            new CodeEditorWindow()
                            {
                                DataContext = new CodeEditorWindowViewModel(new ProgramContext(ProgramContext.ImportedPlugins))
                                {
                                    Code = RippleSrc
                                }
                            }.Show();
                        },
                        () => ProgramContext.IsOpeningScript));
            }
        }

        private ICommand closeThisPaneCommand;
        public ICommand CloseThisPaneCommand
        {
            get
            {
                return closeThisPaneCommand ??
                    (closeThisPaneCommand = new RelayCommand(
                        () => MessengerInstance.Send(new CollapseSimulationStartingPaneMessage(), ViewWindowNo),
                        () => this.IsPane));
            }
        }

        #endregion

        private int GetNumShiftsNearGreater(int value, int goal)
        {
            int num = 0;

            while (value > goal)
            {
                num++;
                value >>= 1;
            }

            return num - 1;
        }

        private Func<object> CreateArrayReadDelegate(StageData stageData)
        {
            if (stageData.InputFileType == StageData.FileType.Binary)
            {
                return CreateArrayReadDelegateInBinary(stageData);
            }
            else
            {
                return CreateArrayReadDelegateInText(stageData);
            }
        }

        private Func<object> CreateArrayReadDelegateInText(StageData stageData)
        {
            string delim = stageData.InputFileType == StageData.FileType.CSV ? "," : "\t";

            if (stageData.Stage.Type is ArrayType)
            {
                var arrayType = stageData.Stage.Type as ArrayType;
                var elemType = arrayType.BaseElementType as BuiltInNumericType; // TODO : 構造体を考慮しない

                if (arrayType.NumDimensions == 1)
                {
                    if (elemType == BuiltInNumericType.SByte)
                    {
                        return () =>
                        {
                            using (var reader = new StreamReader(stageData.FileName))
                                return ArrayFormatter.Unformat1DText<sbyte>(reader, s => sbyte.Parse(s), delim);
                        };
                    }
                    else if (elemType == BuiltInNumericType.UByte)
                    {
                        return () =>
                        {
                            using (var reader = new StreamReader(stageData.FileName))
                                return ArrayFormatter.Unformat1DText<byte>(reader, s => byte.Parse(s), delim);
                        };
                    }
                    else if (elemType == BuiltInNumericType.Int32)
                    {
                        return () =>
                        {
                            using (var reader = new StreamReader(stageData.FileName))
                                return ArrayFormatter.Unformat1DText<int>(reader, s => int.Parse(s), delim);
                        };
                    }
                    else if (elemType == BuiltInNumericType.Float64)
                    {
                        return () =>
                        {
                            using (var reader = new StreamReader(stageData.FileName))
                                return ArrayFormatter.Unformat1DText<double>(reader, s => double.Parse(s), delim);
                        };
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else if (arrayType.NumDimensions == 2)
                {
                    if (elemType == BuiltInNumericType.SByte)
                    {
                        return () =>
                        {
                            using (var reader = new StreamReader(stageData.FileName))
                                return ArrayFormatter.Unformat2DText<sbyte>(reader, s => sbyte.Parse(s), delim);
                        };
                    }
                    else if (elemType == BuiltInNumericType.UByte)
                    {
                        return () =>
                        {
                            using (var reader = new StreamReader(stageData.FileName))
                                return ArrayFormatter.Unformat2DText<byte>(reader, s => byte.Parse(s), delim);
                        };
                    }
                    else if (elemType == BuiltInNumericType.Int32)
                    {
                        return () =>
                        {
                            using (var reader = new StreamReader(stageData.FileName))
                                return ArrayFormatter.Unformat2DText<int>(reader, s => int.Parse(s), delim);
                        };
                    }
                    else if (elemType == BuiltInNumericType.Float64)
                    {
                        return () =>
                        {
                            using (var reader = new StreamReader(stageData.FileName))
                                return ArrayFormatter.Unformat2DText<double>(reader, s => double.Parse(s), delim);
                        };
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                throw new Exception();
            }
        }

        private Func<object> CreateArrayReadDelegateInBinary(StageData stageData)
        {
            string delim = stageData.InputFileType == StageData.FileType.CSV ? "," : "\t";

            if (stageData.Stage.Type is ArrayType)
            {
                var arrayType = stageData.Stage.Type as ArrayType;
                var elemType = arrayType.BaseElementType as BuiltInNumericType; // TODO : 構造体を考慮しない

                if (arrayType.NumDimensions == 1)
                {
                    if (elemType == BuiltInNumericType.SByte)
                    {
                        return () =>
                        {
                            using (var stream = File.OpenRead(stageData.FileName))
                                return ArrayFormatter.Unformat1DBinary<sbyte>(stream);
                        };
                    }
                    else if (elemType == BuiltInNumericType.UByte)
                    {
                        return () =>
                        {
                            using (var stream = File.OpenRead(stageData.FileName))
                                return ArrayFormatter.Unformat1DBinary<byte>(stream);
                        };
                    }
                    else if (elemType == BuiltInNumericType.Int32)
                    {
                        return () =>
                        {
                            using (var stream = File.OpenRead(stageData.FileName))
                                return ArrayFormatter.Unformat1DBinary<int>(stream);
                        };
                    }
                    else if (elemType == BuiltInNumericType.Float64)
                    {
                        return () =>
                        {
                            using (var stream = File.OpenRead(stageData.FileName))
                                return ArrayFormatter.Unformat1DBinary<double>(stream);
                        };
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else if (arrayType.NumDimensions == 2)
                {
                    if (elemType == BuiltInNumericType.SByte)
                    {
                        return () =>
                        {
                            using (var stream = File.OpenRead(stageData.FileName))
                                return ArrayFormatter.Unformat2DBinary<sbyte>(stream);
                        };
                    }
                    else if (elemType == BuiltInNumericType.UByte)
                    {
                        return () =>
                        {
                            using (var stream = File.OpenRead(stageData.FileName))
                                return ArrayFormatter.Unformat2DBinary<byte>(stream);
                        };
                    }
                    else if (elemType == BuiltInNumericType.Int32)
                    {
                        return () =>
                        {
                            using (var stream = File.OpenRead(stageData.FileName))
                                return ArrayFormatter.Unformat2DBinary<int>(stream);
                        };
                    }
                    else if (elemType == BuiltInNumericType.Float64)
                    {
                        return () =>
                        {
                            using (var stream = File.OpenRead(stageData.FileName))
                                return ArrayFormatter.Unformat2DBinary<double>(stream);
                        };
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            else
            {
                throw new Exception();
            }
        }
    }

    class ParameterData
    {
        public string Name { get; set; }
        public TypeData Type { get; set; }
        public string Value { get; set; }
        public string TypeName
        {
            get { return Type.RippleName; }
        }
    }

    class StageData : INotifyPropertyChanged
    {
        public enum FileType
        {
            CSV, TSV, Binary,
        }

        #region フィールドやプロパティ

        public Stage Stage { get; private set; }

        private bool isUsingFile;
        public bool IsUsingFile
        {
            get { return isUsingFile; }
            set
            {
                if (isUsingFile != value)
                {
                    isUsingFile = value;
                    RaisePropertyChanged("IsUsingFile");
                }
            }
        }

        private string fileName;
        public string FileName
        {
            get { return fileName; }
            set
            {
                if (fileName != value)
                {
                    fileName = value;
                    RaisePropertyChanged("FileName");
                }
            }
        }

        public FileType InputFileType { get; private set; }

        public StageData(Stage stage)
        {
            this.Stage = stage;
            this.IsUsingFile = false;
        }

        #endregion

        #region コマンド

        private ICommand showFileDialogCommand;
        public ICommand ShowFileDialogCommand
        {
            get
            {
                if (showFileDialogCommand == null)
                {
                    showFileDialogCommand = new RelayCommand(ShowFileDialog);
                }

                return showFileDialogCommand;
            }
        }

        private void ShowFileDialog()
        {
            const string Filter = "初期値ファイル (*.csv;*.tsv;*.txt;*.bin)|*.csv;*.tsv;*.txt;*.bin|テキストファイル - カンマ区切り (*.csv)|*.csv|テキストファイル - タブ区切り (*.tsv;*.txt)|*.tsv;*.txt|Ripple バイナリファイル (*.bin)|*.bin|すべてのファイル (*.*)|*.*";

            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = Filter
            };

            if (ofd.ShowDialog().Value)
            {
                FileName = ofd.FileName;
                switch (Path.GetExtension(FileName).ToLower())
                {
                    case ".csv":
                        InputFileType = FileType.CSV;
                        break;
                    case ".tsv":
                    case ".txt":
                        InputFileType = FileType.TSV;
                        break;
                    case ".bin":
                    default:
                        InputFileType = FileType.Binary;
                        break;
                }
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string name)
        {
            var d = PropertyChanged;
            if (d != null)
                d(this, new PropertyChangedEventArgs(name));
        }
    }
}
