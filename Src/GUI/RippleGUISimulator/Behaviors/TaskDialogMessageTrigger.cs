using System.Windows;
using System.Windows.Interactivity;
using GalaSoft.MvvmLight.Messaging;

namespace Ripple.GUISimulator.Behaviors
{
    class TaskDialogMessageTrigger : TriggerBase<DependencyObject>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            Messenger.Default.Register<TaskDialogMessage>(AssociatedObject, InvokeActions);
        }

        protected override void OnDetaching()
        {
            Messenger.Default.Unregister<TaskDialogMessage>(AssociatedObject);
            base.OnDetaching();
        }
    }
}
