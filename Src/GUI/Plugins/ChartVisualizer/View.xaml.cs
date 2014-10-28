using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.Integration;
using System.Xml.Serialization;
using Ripple.VisualizationInterfaces;

namespace Ripple.Plugins.ChartVisualizer
{
    /// <summary>
    /// UserControl1.xaml の相互作用ロジック
    /// </summary>
    public partial class View : UserControl, IStageView
    {
        private const int DefaultBorderWidth = 5;

        private IEnumerable<IStageContainer> stages;
        private Chart chart;

        public View(IEnumerable<IStageContainer> stages, SeriesChartType seriesChartType)
            : this(stages)
        {
            var vm = (ViewModel)DataContext;
            vm.CurrentKind = vm.ChartKinds.Single(k => k.SeriesChartType == seriesChartType);
        }

        public View(IEnumerable<IStageContainer> stages)
        {
            this.stages = stages;
            InitializeComponent();

            this.DataContext = new ViewModel();

            chart = new Chart();
            var host = new WindowsFormsHost() { Child = chart };
            this.ChartGrid.Children.Add(host);

            CreateChart();
        }

        private void CreateChart()
        {
            var area = new ChartArea();
            chart.ChartAreas.Clear();
            chart.ChartAreas.Add(area);

            chart.Legends.Clear();
            chart.Legends.Add(new Legend());

            chart.Series.Clear();
            int min = int.MaxValue, max = int.MinValue;
            foreach (var stage in stages)
            {
                CreateSeries(stage, (DataContext as ViewModel).CurrentKind.SeriesChartType);
                min = Math.Min(stage.SupportedTime.First(), min);
                max = Math.Max(stage.SupportedTime.Last(), max);
            }

            area.AxisX.Minimum = min;
            area.AxisX.Maximum = max + 1;
        }

        private void CreateSeries(IStageContainer container, SeriesChartType seriesChartType)
        {
            string name = container.Name;

            chart.Series.Add(name);
            chart.Series[name].ChartType = seriesChartType;
            //chart.Series[name].LegendText = "";
            chart.Series[name].BorderWidth = DefaultBorderWidth;

            Debug.Assert(container.Stages.Length == container.SupportedTime.Count());
            chart.Series[name].Points.DataBindXY(container.SupportedTime.ToArray(), container.Stages);
        }

        private void KindComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CreateChart();
        }

        public void SerializeCurrentState(System.IO.Stream stream)
        {
            new XmlSerializer(typeof(SeriesChartType)).Serialize(stream, ((ViewModel)DataContext).CurrentKind.SeriesChartType);
        }
    }

    class ChartKind
    {
        public string Name { get; private set; }
        public SeriesChartType SeriesChartType { get; private set; }

        public ChartKind(string name, SeriesChartType seriesChartType)
        {
            this.Name = name;
            this.SeriesChartType = seriesChartType;
        }
    }

    class ViewModel
    {
        private static readonly IEnumerable<ChartKind> chartKinds = new ChartKind[]
        {
            new ChartKind("折れ線グラフ", SeriesChartType.Line),            
            new ChartKind("折れ線グラフ(スプライン グラフ)", SeriesChartType.Spline),
            new ChartKind("折れ線グラフ(データ数が多い場合に適する)", SeriesChartType.FastLine),
            new ChartKind("横棒グラフ", SeriesChartType.Bar),
            new ChartKind("縦棒グラフ", SeriesChartType.Column),
            new ChartKind("面グラフ", SeriesChartType.Area),
        };

        public IEnumerable<ChartKind> ChartKinds
        {
            get { return chartKinds; }
        }

        public ChartKind CurrentKind { get; set; }

        public ViewModel()
        {
            this.CurrentKind = ChartKinds.First();
        }
    }

}
