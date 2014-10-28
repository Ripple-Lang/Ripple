using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using Ripple.GUISimulator.Behaviors;

namespace Ripple.GUISimulator.Windows
{
    /// <summary>
    /// CodeEditorWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CodeEditorWindow : Window
    {
        private const int RightPaneMinimumWidth = 450;

        private int no = WindowNumberFactory.GetNextNo();
        private GridLength savedRightPaneWidth = new GridLength(0);

        public CodeEditorWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<CloseWindowMessage>(this, no, CloseWindow);
            Messenger.Default.Register<ShowSimulationStartingPaneMessage>(this, no,
                msg => ShowSimulationStartingPane(msg.Content));
            Messenger.Default.Register<CollapseSimulationStartingPaneMessage>(this, no,
                msg => CollapseSimulationStartingPane());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ((CodeEditorWindowViewModel)DataContext).ViewWindowNo = no;
        }

        private void CloseWindow(CloseWindowMessage obj)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((DataContext as CodeEditorWindowViewModel).PromoteFileSaveIfNotSaved())
            { }
            else
            {
                e.Cancel = true;
            }
        }

        private void ShowSimulationStartingPane(SimulationStartingPaneViewModel viewModel)
        {
            viewModel.IsPane = true;
            viewModel.ViewWindowNo = no;

            RightPaneCol.MinWidth = RightPaneMinimumWidth;
            RightPaneCol.Width = savedRightPaneWidth;
            RightPane.Visibility = System.Windows.Visibility.Visible;
            RightPane.DataContext = viewModel;
            RightPane.Focus();
            RightPane.FocusFirst();
        }

        private void CollapseSimulationStartingPane()
        {
            RightPaneCol.MinWidth = 0;
            savedRightPaneWidth = RightPaneCol.Width;
            RightPaneCol.Width = new GridLength(0);
            RightPane.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    class ShowSimulationStartingPaneMessage : GenericMessage<SimulationStartingPaneViewModel>
    {
        public ShowSimulationStartingPaneMessage(SimulationStartingPaneViewModel viewModel)
            : base(viewModel)
        { }
    }

    class CollapseSimulationStartingPaneMessage
    { }
}
