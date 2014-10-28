using System.Windows.Controls;

namespace Ripple.GUISimulator.Controls
{
    /// <summary>
    /// SimulationStartingPane.xaml の相互作用ロジック
    /// </summary>
    public partial class SimulationStartingPane : UserControl
    {
        public SimulationStartingPane()
        {
            InitializeComponent();
        }

        public void FocusFirst()
        {
            MaxTimeTextBox.Focus();
            MaxTimeTextBox.SelectAll();
        }
    }
}
