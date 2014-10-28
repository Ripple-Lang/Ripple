using System;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;

namespace Ripple.GUISimulator.Behaviors
{
    class FileDialogMessage : GenericMessage<FileDialog>
    {
        public FileDialog Dialog { get; private set; }
        public Action<FileDialog> Callback { get; private set; }

        public FileDialogMessage(object sender, FileDialog dialog, Action<FileDialog> callback)
            : base(sender, dialog)
        {
            this.Dialog = dialog;
            this.Callback = callback;
        }

        public void ProcessCallback()
        {
            var d = Callback;
            if (d != null)
                d(Dialog);
        }
    }
}
