using System.Windows;
using System.Windows.Interactivity;

namespace Ripple.GUISimulator.Behaviors
{
    class ShowFileDialogAction : TriggerAction<DependencyObject>
    {
        protected override void Invoke(object parameter)
        {
            var message = parameter as FileDialogMessage;

            if (message.Dialog.ShowDialog() ?? false)
            {
                message.ProcessCallback();
            }
        }
    }
}
