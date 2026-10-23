// Delphi: Source/Handles/Actions/BoldUndoActions.pas
// Port map: CSharp/port/units/BoldUndoActions.md

using System.ComponentModel;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>Sets a check-point in the system's undo handler (where undo stops); enabled when the system is active.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSetCheckPointAction : TBoldSystemHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldSetCheckPointAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sSetCheckPoint;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldSystemHandle!.Active;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldSystemHandle!.System!.UndoHandlerInterface.SetCheckPoint();
    }
}

/// <summary>Undoes the latest changes; enabled when there is a change to undo.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUndoAction : TBoldSystemHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldUndoAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sUndo;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldSystemHandle!.Active &&
                           BoldSystemHandle.System!.UndoHandlerInterface.UndoList.Count > 0 &&
                           (BoldSystemHandle.System.UndoHandlerInterface.UndoList.Count > 1 ||
                            BoldSystemHandle.System.UndoHandlerInterface.UndoList.TopBlock!.ContainsChanges);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldSystemHandle!.System!.UndoHandlerInterface.UndoLatest();
    }
}

/// <summary>Redoes the latest undone changes; enabled when there is something to redo.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRedoAction : TBoldSystemHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldRedoAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sRedo;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldSystemHandle!.Active && BoldSystemHandle.System!.UndoHandlerInterface.RedoList.Count > 0;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldSystemHandle!.System!.UndoHandlerInterface.RedoLatest();
    }
}
