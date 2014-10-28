using System.Windows;
using System.Windows.Interactivity;
using CommonControlsOnCLI.TaskDialogs;

namespace Ripple.GUISimulator.Behaviors
{
    class ShowTaskDialogAction : TriggerAction<DependencyObject>
    {
        protected override void Invoke(object parameter)
        {
            var message = parameter as TaskDialogMessage;
            if (message != null)
            {
                var result = TaskDialogCLI.ShowIndirect(message.Config);
                message.ProcessCallback((TaskDialogResult)result);
            }
        }
    }
}
