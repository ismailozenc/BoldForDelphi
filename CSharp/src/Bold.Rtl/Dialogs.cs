namespace Bold;

/// <summary>Delphi's TMsgDlgType: the kind of a message dialog (its icon and caption).</summary>
public enum TMsgDlgType { mtWarning, mtError, mtInformation, mtConfirmation, mtCustom }

/// <summary>Delphi's TMsgDlgBtn: a button of a message dialog.</summary>
public enum TMsgDlgBtn { mbYes, mbNo, mbOK, mbCancel, mbAbort, mbRetry, mbIgnore, mbAll, mbNoToAll, mbYesToAll, mbHelp, mbClose }

/// <summary>
/// The message dialog the Delphi code shows (Delphi's MessageDlg). The GUI toolkit supplies the dialog through
/// <see cref="MessageDlgHandler"/> (Bold.WinForms shows a message box); without one, MessageDlg raises, as code that
/// asks the user cannot run without a user interface. Tests set a handler that answers.
/// </summary>
public static class Dialogs
{
    /// <summary>Shows the message with the buttons and gives the modal result of the button pressed.</summary>
    public delegate int TMessageDlgHandler(string Msg, TMsgDlgType DlgType, DelphiSet<TMsgDlgBtn> Buttons, int HelpCtx);

    /// <summary>The dialog of the GUI toolkit; null when there is none.</summary>
    public static TMessageDlgHandler? MessageDlgHandler { get; set; }

    /// <summary>Shows a message dialog and gives the modal result (<see cref="Controls.mrYes"/> and so on).</summary>
    public static int MessageDlg(string Msg, TMsgDlgType DlgType, DelphiSet<TMsgDlgBtn> Buttons, int HelpCtx)
    {
        TMessageDlgHandler handler = MessageDlgHandler
            ?? throw new InvalidOperationException("No message dialog is available (set Dialogs.MessageDlgHandler): " + Msg);
        return handler(Msg, DlgType, Buttons, HelpCtx);
    }

    /// <summary>Shows the message with an OK button (Delphi's ShowMessage).</summary>
    public static void ShowMessage(string Msg) => MessageDlg(Msg, TMsgDlgType.mtCustom, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbOK), 0);
}
