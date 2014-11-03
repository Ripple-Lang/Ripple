using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace Ripple.GUISimulator.Windows.Windows
{
    /// <summary>
    /// VersionInfoWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class VersionInfoWindow : Window
    {
        public VersionInfoWindow()
        {
            InitializeComponent();

            var asm = Assembly.GetExecutingAssembly();

            this.DataContext =
                new
                {
                    GUIToolName = GetAttribute<AssemblyTitleAttribute>(asm).Title,
                    GUIToolVersion = asm.GetName().Version,
                    CompilerName = Ripple.Compilers.VersionInfos.VersionInformation.ProductName,
                    CompilerVersion = Ripple.Compilers.VersionInfos.VersionInformation.Version,
                    Copyright = GetAttribute<AssemblyCopyrightAttribute>(asm).Copyright,
                };
        }

        private static T GetAttribute<T>(Assembly assembly) where T : class
        {
            return Attribute.GetCustomAttribute(assembly, typeof(T)) as T;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
                e.Handled = true;
            }
        }
    }
}
