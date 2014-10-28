using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GalaSoft.MvvmLight.Command;
using Ripple.Compilers.Options;
using Ripple.GUISimulator.Controls;
using Ripple.GUISimulator.StageContainers;
using Ripple.VisualizationInterfaces;

namespace Ripple.GUISimulator.Windows
{
    class VisualizationWindowViewModel : INotifyPropertyChanged
    {
        public string RippleSrc { get; private set; }
        public object SimulationResult { get; private set; }
        public int MaxTime { get; private set; }
        public TimeSpan Elapsed { get; private set; }
        public IEnumerable<string> Stages { get; private set; }
        public ImportedPlugins ImportedPlugins { get; private set; }
        public CompileOption CompileOption { get; private set; }

        public Lazy<IStageVisualizer> SelectedPlugin { get; set; }
        public IList SelectedStages { get; set; }

        public ObservableCollection<VisualizedTabItem> VisualizedControls { get; set; }
        public VisualizedTabItem SelectedTabControl { get; set; }

        public VisualizationWindowViewModel(string rippleSrc, object simulationResult, int maxTime, TimeSpan elapsed, IEnumerable<string> stages, ImportedPlugins importedPlugins, CompileOption compileOption)
        {
            this.RippleSrc = rippleSrc;
            this.SimulationResult = simulationResult;
            this.MaxTime = maxTime;
            this.Elapsed = elapsed;
            this.Stages = stages;
            this.ImportedPlugins = importedPlugins;
            this.CompileOption = compileOption;

            if (importedPlugins.Visualizers != null && importedPlugins.Visualizers.Count() > 0)
            {
                this.SelectedPlugin = importedPlugins.Visualizers.First();
            }

            this.VisualizedControls = new ObservableCollection<VisualizedTabItem>();
        }

        private ICommand visualizeCommand;
        public ICommand VisualizeCommand
        {
            get
            {
                if (visualizeCommand == null)
                {
                    visualizeCommand = new RelayCommand(() =>
                    {
                        try
                        {
                            Visualize(SelectedPlugin.Value, SelectedStages.Cast<string>(), null);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("ビジュアル化中にエラーが発生しました。" + Environment.NewLine + e.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    });
                }

                return visualizeCommand;
            }
        }

        public void Visualize(IStageVisualizer visualizer, IEnumerable<string> stages, Stream stream)
        {
            var containers = stages.Select(s => CreateIStageContainer(s));

            IStageView view;
            if (stream == null)
            {
                view = visualizer.CreateView(containers);
            }
            else
            {
                view = visualizer.CreateView(containers, stream);
            }

            var control = new VisualizedTabItem()
            {
                DataContext =
                    new VisualizedTabItemViewModel(stages, view, SelectedPlugin.Value.GetType().ToString(), RippleSrc, CompileOption)
            };

            // ビューを追加する
            this.VisualizedControls.Add(control);
            this.SelectedTabControl = control;
            RaisePropertyChanged("SelectedTabControl");
        }

        private IStageContainer CreateIStageContainer(string stageName)
        {
            dynamic stages = SimulationResult.GetType().GetField(stageName).GetValue(SimulationResult);
            if (stages.Length == MaxTime + 1)
            {
                return new AllHoldedStageContainer(stages, MaxTime, stageName);
            }
            else
            {
                return new PartiallyHoldedStageContainer(stages, MaxTime, stageName);
            }
        }

        public string ElapsedString
        {
            get { return Elapsed.TotalSeconds.ToString("0.000"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string name)
        {
            var d = PropertyChanged;
            if (d != null)
                d(this, new PropertyChangedEventArgs(name));
        }
    }
}
