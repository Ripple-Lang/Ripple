using System.Windows;

namespace Ripple.GUISimulator.Windows
{
    /// <summary>
    /// SimulationStartingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SimulationStartingWindow : Window
    {
        public SimulationStartingWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Pane.FocusFirst();
        }
    }
}
