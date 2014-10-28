using System.Windows;
using System.Windows.Interactivity;
using GalaSoft.MvvmLight.Messaging;

namespace Ripple.GUISimulator.Behaviors
{
    public class FileDialogTrigger : TriggerBase<DependencyObject>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            Messenger.Default.Register<FileDialogMessage>(AssociatedObject, InvokeActions);
        }

        protected override void OnDetaching()
        {
            Messenger.Default.Unregister<FileDialogMessage>(AssociatedObject);
            base.OnDetaching();
        }
    }
}
