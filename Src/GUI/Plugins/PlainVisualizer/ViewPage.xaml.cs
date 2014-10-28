using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using PlainVisualizerHelper;
using Ripple.VisualizationInterfaces;

namespace Ripple.Plugins.PlainVisualizer
{
    /// <summary>
    /// ViewPage.xaml の相互作用ロジック
    /// </summary>
    public partial class ViewPage : System.Windows.Controls.UserControl, IStageView
    {
        private class ViewModel
        {
            public string NowText { get; set; }
        }

        private const int AnimationDuration = 10;

        private readonly IStageContainer container;
        private readonly Array[] plainsArray;
        private readonly int minTime, maxTime;
        private int time;
        private Color color = Colors.Black;
        private System.Timers.Timer timer;

        public ViewPage(IStageContainer container)
            : this(container, ShowSelectColor())
        { }

        public ViewPage(IStageContainer container, State state)
            : this(container, new Color() { R = state.R, G = state.G, B = state.B })
        { }

        private ViewPage(IStageContainer container, Color color)
        {
            this.container = container;

            this.plainsArray = (Array[])container.Stages;
            if (ArrayCaster.GetDimensions(container.Stages) >= 3)
            {
                this.minTime = container.SupportedTime.First();
                this.maxTime = container.SupportedTime.Last();
                this.time = minTime;
            }
            else
            {
                this.minTime = this.maxTime = this.time = 0;
            }

            InitializeComponent();

            this.color = color;
            this.DataContext = new ViewModel();
            ChangeTimeTo(minTime);
        }

        private unsafe void ChangeTimeTo(int time)
        {
            this.time = time;
            this.TimeTextBox.Text = time.ToString();

            dynamic array = plainsArray[container.GetArrayIndex(time)];
            int height = array.Length, width = array[0].Length;

            WriteableBitmap wb;
            if (this.Image2D.Source == null)
            {
                wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            }
            else
            {
                wb = (WriteableBitmap)this.Image2D.Source;
            }

            wb.Lock();

            ArrayConverter.CreateImage(array, wb.BackBuffer, color.R, color.G, color.B);

            wb.AddDirtyRect(new Int32Rect(0, 0, width, height));
            wb.Unlock();

            this.Image2D.Source = wb;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            --time;
            if (time < minTime)
                time = maxTime;

            ChangeTimeTo(time);
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            ++time;
            if (time > maxTime)
                time = minTime;

            ChangeTimeTo(time);
        }

        private static Color ShowSelectColor()
        {
            ColorDialog cd = new ColorDialog();
            var result = cd.ShowDialog();
            var color = cd.Color;

            return new Color() { R = color.R, G = color.G, B = color.B };
        }

        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
            color = ShowSelectColor();
            ChangeTimeTo(time);
        }

        private void TimeTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    int time = int.Parse(TimeTextBox.Text);
                    if (time < minTime || time > maxTime)
                        throw new Exception();
                    ChangeTimeTo(time);
                }
                catch
                {
                    TimeTextBox.Text = this.time.ToString();
                }
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(timer == null);

            timer = new System.Timers.Timer(AnimationDuration / 1000.0)
            {
                AutoReset = true,
                Enabled = true
            };
            timer.Elapsed += TimerCallBack;
            timer.Start();

            this.PlayButton.Visibility = System.Windows.Visibility.Collapsed;
            this.PoseButton.Visibility = System.Windows.Visibility.Visible;
        }

        void TimerCallBack(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.Image2D.Dispatcher.Invoke(() =>
            {
                int next = time + 1;
                if (next > maxTime)
                    next = minTime;
                ChangeTimeTo(next);
            });
        }

        private void PoseButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(timer != null);
            timer.Stop();
            timer.Elapsed -= TimerCallBack;
            timer.Dispose();
            timer = null;

            this.PlayButton.Visibility = System.Windows.Visibility.Visible;
            this.PoseButton.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void SerializeCurrentState(System.IO.Stream stream)
        {
            var state = new State() { R = color.R, G = color.G, B = color.B };
            new XmlSerializer(typeof(State)).Serialize(stream, state);
        }
    }

    [Serializable]
    public struct State
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }
}
