using System.Windows;

namespace Ripple.GUISimulator.Windows.Windows
{
    /// <summary>
    /// CSharpCodeViewWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CSharpCodeViewWindow : Window
    {
        private readonly string code;

        public CSharpCodeViewWindow(string code)
        {
            this.code = code;
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string formattedCode = await CSharpCodeFormatter.FormatAsync(code);

                this.Processing.Visibility = System.Windows.Visibility.Hidden;
                this.CSharpCodeText.Text = formattedCode;
                this.CSharpCodeText.Visibility = System.Windows.Visibility.Visible;
                this.ThisIsFormatedByRoslyn.Visibility = System.Windows.Visibility.Visible;
            }
            catch
            {
                this.Processing.Visibility = System.Windows.Visibility.Hidden;
                this.CSharpCodeText.Text = code;
                this.CSharpCodeText.Visibility = System.Windows.Visibility.Visible;
                this.CodeWasNotFormatted.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
