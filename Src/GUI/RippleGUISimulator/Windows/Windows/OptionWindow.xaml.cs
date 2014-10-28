using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.Win32;
using Ripple.Compilers.Options;

namespace Ripple.GUISimulator.Windows.Windows
{
    /// <summary>
    /// OptionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class OptionWindow : Window
    {
        public OptionWindow()
        {
            InitializeComponent();
        }

        private void ReferenceButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "DLLファイル (*.dll)|*.dll|すべてのファイル (*.*)|*.*",
                FilterIndex = 0,
                Title = "アセンブリの出力先ファイルを指定してください",
                AddExtension = true,
            };

            var result = sfd.ShowDialog();

            if (result.Value)
            {
                FileName.Text = sfd.FileName.EndsWith(".dll") ? sfd.FileName : sfd.FileName + ".dll";
            }
        }

        private void PromoteOK()
        {
            this.DialogResult = true;
            this.Close();
        }

        private void PromoteCancel()
        {
            this.DialogResult = false;
            this.Close();
        }

        private void PromoteRestoreDefault()
        {
            this.DataContext = new CompileOption();
            this.DialogResult = true;
            this.Close();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            PromoteOK();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            PromoteCancel();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PromoteOK();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                PromoteCancel();
                e.Handled = false;
            }
        }

        private void RestoreDefault_Click(object sender, RoutedEventArgs e)
        {
            PromoteRestoreDefault();
        }
    }

    class NotConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    class ParallelizationOptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((ParallelizationOption)value & ParallelizationOption.InParallelSpecifiedCode) != 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? ParallelizationOption.InParallelSpecifiedCode : ParallelizationOption.None;
        }
    }
}
