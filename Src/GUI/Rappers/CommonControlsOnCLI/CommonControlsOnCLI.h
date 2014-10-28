// CommonControlsOnCLI.h

#pragma once

#include <Windows.h>
#include <CommCtrl.h>

using namespace System;

namespace CommonControlsOnCLI
{
namespace TaskDialogs
{

[System::FlagsAttribute]
public enum class TaskDialogButtons
{
    OK = TDCBF_OK_BUTTON,
    YES = TDCBF_YES_BUTTON,
    NO = TDCBF_NO_BUTTON,
    CANCEL = TDCBF_CANCEL_BUTTON,
    RETRY = TDCBF_RETRY_BUTTON,
    CLOSE = TDCBF_CLOSE_BUTTON
};

#ifdef ERROR
# undef ERROR
#endif // ERROR

public enum class TaskDialogIcons
{
    NONE, WARNING, ERROR, INFORMATION, SHIELD
};

public enum class TaskDialogResult
{
    CANCEL = IDCANCEL,
    NO = IDNO,
    OK = IDOK,
    RETRY = IDRETRY,
    YES = IDYES,
};

[System::FlagsAttribute]
public enum class TaskDialogFlags
{
    ENABLE_HYPERLINKS = 0x0001,
    USE_HICON_MAIN = 0x0002,
    USE_HICON_FOOTER = 0x0004,
    ALLOW_DIALOG_CANCELLATION = 0x0008,
    USE_COMMAND_LINKS = 0x0010,
    USE_COMMAND_LINKS_NO_ICON = 0x0020,
    EXPAND_FOOTER_AREA = 0x0040,
    EXPANDED_BY_DEFAULT = 0x0080,
    VERIFICATION_FLAG_CHECKED = 0x0100,
    SHOW_PROGRESS_BAR = 0x0200,
    SHOW_MARQUEE_PROGRESS_BAR = 0x0400,
    CALLBACK_TIMER = 0x0800,
    POSITION_RELATIVE_TO_WINDOW = 0x1000,
    RTL_LAYOUT = 0x2000,
    NO_DEFAULT_RADIO_BUTTON = 0x4000,
    CAN_BE_MINIMIZED = 0x8000,    
};

public ref struct TaskDialogButton
{
    property int ButtonID;
    property System::String^ ButtonText;
};

public ref struct TaskDialogConfig
{
    property TaskDialogFlags Flags;
    property TaskDialogButtons CommonButtons;
    property System::String^ WindowTitle;
    property TaskDialogIcons MainIcon;
    property System::String^ MainInstruction;
    property System::String^ Content;
    property array<TaskDialogButton^>^ Buttons;
    property System::String^ VerificationText;
    property System::String^ ExpandedInformation;
    property System::String^ ExpandedControlText;
    property System::String^ CollapsedControlText;
    property TaskDialogIcons FooterIcon;
    property System::String^ Footer;
};

public ref class TaskDialogCLI
{
public:
    static TaskDialogResult Show(System::String^ windowTitle, System::String^ mainInstruction, System::String^ content, TaskDialogButtons buttons, TaskDialogIcons icon);
    static TaskDialogResult ShowIndirect(TaskDialogConfig ^config);

private:
    TaskDialogCLI()
    {}
    TaskDialogCLI(const TaskDialogCLI^ t)
    {}
};


}
}
