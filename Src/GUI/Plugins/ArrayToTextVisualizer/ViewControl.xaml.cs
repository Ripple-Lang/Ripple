using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ripple.VisualizationInterfaces;

namespace Ripple.Plugins.ArrayToTextVisualizer
{
    /// <summary>
    /// UserControl1.xaml の相互作用ロジック
    /// </summary>
    public partial class ViewControl : UserControl, IStageView
    {
        public ViewControl()
        {
            InitializeComponent();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModel).CurrentPage++;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as ViewModel).CurrentPage--;
        }

        public void SerializeCurrentState(System.IO.Stream stream)
        { }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText((DataContext as ViewModel).CurrentText);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ((ViewModel)DataContext).ExecuteSave();
        }
    }

    public class BoolVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
