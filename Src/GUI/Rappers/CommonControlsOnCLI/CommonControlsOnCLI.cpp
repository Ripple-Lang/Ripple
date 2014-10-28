// これは メイン DLL ファイルです。

#include "stdafx.h"

#include "CommonControlsOnCLI.h"
#include <vector>
#include <cassert>
#include <Windows.h>
#include <CommCtrl.h>
#include <tchar.h>
#include <msclr/marshal_cppstd.h>

#ifdef DEBUG
#define NODEFAULT assert(false)
#else
#define NODEFAULT __assume(false)
#endif // DEBUG

using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace msclr::interop;
using namespace CommonControlsOnCLI::TaskDialogs;


namespace
{

PCWSTR ConvertIcon(TaskDialogIcons icon)
{
    switch (icon)
    {
    case TaskDialogIcons::NONE:
        return nullptr;
        break;
    case TaskDialogIcons::WARNING:
        return TD_WARNING_ICON;
        break;
    case TaskDialogIcons::ERROR:
        return TD_ERROR_ICON;
        break;
    case TaskDialogIcons::INFORMATION:
        return TD_INFORMATION_ICON;
        break;
    case TaskDialogIcons::SHIELD:
        return TD_SHIELD_ICON;
        break;
    default:
        NODEFAULT;
        return nullptr;
        break;
    }
}

}

TaskDialogResult TaskDialogCLI::Show(String^ windowTitle, String^ mainInstruction, String^ content, TaskDialogButtons buttons, TaskDialogIcons icon)
{
    Module ^module = Assembly::GetEntryAssembly()->GetModules()[0];
    HINSTANCE hInst = reinterpret_cast<HINSTANCE>(Marshal::GetHINSTANCE(module).ToPointer());

    marshal_context mc;
    int result = 0;
    TaskDialog(nullptr, hInst,
        mc.marshal_as<PCWSTR>(windowTitle),
        mc.marshal_as<PCWSTR>(mainInstruction),
        mc.marshal_as<PCWSTR>(content),
        static_cast<TASKDIALOG_COMMON_BUTTON_FLAGS>(buttons), ConvertIcon(icon), &result);

    return static_cast<TaskDialogResult>(result);
}

TaskDialogResult TaskDialogCLI::ShowIndirect(TaskDialogConfig ^config)
{
    // ネイティブ型への変換
    Module ^module = Assembly::GetEntryAssembly()->GetModules()[0];
    HINSTANCE hInst = reinterpret_cast<HINSTANCE>(Marshal::GetHINSTANCE(module).ToPointer());
    marshal_context mc;

    TASKDIALOGCONFIG cf = { 0 };

    cf.cbSize = sizeof(cf);
    cf.hInstance = hInst;
    cf.dwFlags = static_cast<TASKDIALOG_FLAGS>(config->Flags);
    cf.dwCommonButtons = static_cast<TASKDIALOG_COMMON_BUTTON_FLAGS>(config->CommonButtons);
    cf.pszWindowTitle = mc.marshal_as<PCWSTR>(config->WindowTitle);
    cf.pszMainIcon = ConvertIcon(config->MainIcon);
    cf.pszMainInstruction = mc.marshal_as<PCWSTR>(config->MainInstruction);
    cf.pszContent = mc.marshal_as<PCWSTR>(config->Content);

    // ボタン
    std::vector<TASKDIALOG_BUTTON> nativeButtons;
    if (config->Buttons != nullptr)
    {
        for each (TaskDialogButton ^b in config->Buttons)
        {
            nativeButtons.push_back(TASKDIALOG_BUTTON{ b->ButtonID, mc.marshal_as<PCWSTR>(b->ButtonText) });
        }
        cf.cButtons = static_cast<UINT>(nativeButtons.size());
        cf.pButtons = nativeButtons.data();
    }

    cf.pszVerificationText = mc.marshal_as<PCWSTR>(config->VerificationText);
    cf.pszExpandedInformation = mc.marshal_as<PCWSTR>(config->ExpandedInformation);
    cf.pszExpandedControlText = mc.marshal_as<PCWSTR>(config->ExpandedControlText);
    cf.pszCollapsedControlText = mc.marshal_as<PCWSTR>(config->CollapsedControlText);

    cf.pszFooterIcon = ConvertIcon(config->FooterIcon);
    cf.pszFooter = mc.marshal_as<PCWSTR>(config->Footer);

    // ダイアログの表示    
    int result = 0;
    TaskDialogIndirect(&cf, &result, nullptr, nullptr);
    return static_cast<TaskDialogResult>(result);
}
