using System.Windows;
using System.Windows.Controls;

namespace Ripple.GUISimulator.Windows
{
    /// <summary>
    /// VisualizationWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class VisualizationWindow : Window
    {
        public VisualizationWindow()
        {
            InitializeComponent();
        }

        private void StagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            (DataContext as VisualizationWindowViewModel).SelectedStages = StagesListBox.SelectedItems;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as VisualizationWindowViewModel;

            vm.SelectedTabControl = null;
            vm.VisualizedControls.Clear();
        }
    }
}
