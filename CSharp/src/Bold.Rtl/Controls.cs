namespace Bold;

/// <summary>Delphi's modal results (TModalResult, an integer): what closed a modal form or dialog.</summary>
public static class Controls
{
    public const int mrNone = 0;
    public const int mrOk = 1;
    public const int mrCancel = 2;
    public const int mrAbort = 3;
    public const int mrRetry = 4;
    public const int mrIgnore = 5;
    public const int mrYes = 6;
    public const int mrNo = 7;
    public const int mrAll = 8;
    public const int mrNoToAll = 9;
    public const int mrYesToAll = 10;
    public const int mrClose = 11;
}

/// <summary>
/// A control linked to an action (Delphi's TControl.Action). Delphi passes the control as the target of the action's
/// HandlesTarget, UpdateTarget and ExecuteTarget; a GUI toolkit's action links pass a target that implements this.
/// </summary>
public interface IActionClient
{
    TBasicAction? Action { get; }
}

/// <summary>A target that shows one line of text, as Delphi's TStatusBar does with SimpleText.</summary>
public interface ISimpleTextTarget
{
    string SimpleText { get; set; }
}
