using System;
using CommonControlsOnCLI.TaskDialogs;
using GalaSoft.MvvmLight.Messaging;

namespace Ripple.GUISimulator.Behaviors
{
    class TaskDialogMessage : GenericMessage<TaskDialogConfig>
    {
        public TaskDialogConfig Config { get; private set; }
        public Action<TaskDialogResult> Callback { get; private set; }

        public TaskDialogMessage(object sender, TaskDialogConfig config, Action<TaskDialogResult> callback)
            : base(sender, config)
        {
            this.Callback = callback;
        }

        public void ProcessCallback(TaskDialogResult result)
        {
            var d = Callback;
            if (d != null)
                d(result);
        }
    }
}
