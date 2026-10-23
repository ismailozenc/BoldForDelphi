// Delphi: Source/Handles/Actions/BoldListActions.pas
// Port map: CSharp/port/units/BoldListActions.md

using System.ComponentModel;
using static Bold.HandlesConst;

namespace Bold;

/// <summary>An action on a list handle; enabled when the handle has a list.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleAction : TBoldHandleAction
{
    /// <summary>Adds the action to <paramref name="AOwner"/> when given.</summary>
    public TBoldListHandleAction(IContainer? AOwner = null)
        : base(AOwner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldHandle!.List is not null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractListHandle? GetBoldHandle()
    {
        System.Diagnostics.Debug.Assert(BoldElementHandle is null || BoldElementHandle is TBoldAbstractListHandle);
        return (TBoldAbstractListHandle?)BoldElementHandle;
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldAbstractListHandle? Value) => BoldElementHandle = Value;

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractListHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }
}

/// <summary>Moves the list handle to the next element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleNextAction : TBoldListHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldListHandleNextAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sNext;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldHandle!.HasNext;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldHandle!.Next();
    }
}

/// <summary>Moves the list handle to the previous element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandlePrevAction : TBoldListHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldListHandlePrevAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sPrev;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldHandle!.HasPrior;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldHandle!.Prior();
    }
}

/// <summary>Moves the list handle to the first element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleFirstAction : TBoldListHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldListHandleFirstAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sFirst;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldHandle!.Count > 0;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldHandle!.First();
    }
}

/// <summary>Moves the list handle to the last element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleLastAction : TBoldListHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldListHandleLastAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sLast;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldHandle!.Count > 0;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldHandle!.Last();
    }
}

/// <summary>Removes the current element from the list, or deletes its object, as BoldDeleteMode says.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleDeleteAction : TBoldListHandleAction
{
    private TBoldDeleteMode FDeleteMode;

    [Delphi(PortStatus.Ported)]
    public TBoldListHandleDeleteAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sDelete;
    }

    /// <summary>Whether the delete mode allows deleting, given whether the object can be deleted.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool AllowDelete(bool CanDeleteObject) =>
        BoldDeleteMode switch
        {
            TBoldDeleteMode.dmDefault or TBoldDeleteMode.dmRemoveFromList => true,
            TBoldDeleteMode.dmDelete => CanDeleteObject,
            TBoldDeleteMode.dmUnlinkAllAndDelete => true,
            _ => throw new EBold(SysUtils.Format(sUnknownDeleteMode, GetType().Name)),
        };

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            TBoldObject? BoldObject = BoldHandle!.CurrentBoldObject;
            EnableAction = BoldObject is not null && AllowDelete(BoldObject.CanDelete());
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        switch (BoldDeleteMode)
        {
            case TBoldDeleteMode.dmDefault:
            case TBoldDeleteMode.dmRemoveFromList:
                BoldHandle!.RemoveCurrentElement();
                break;
            case TBoldDeleteMode.dmDelete:
                BoldHandle!.CurrentBoldObject!.Delete();
                break;
            case TBoldDeleteMode.dmUnlinkAllAndDelete:
                TBoldObject obj = BoldHandle!.CurrentBoldObject!;
                obj.UnLinkAll();
                obj.Delete();
                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldDeleteMode BoldDeleteMode
    {
        get => FDeleteMode;
        set => FDeleteMode = value;
    }
}

/// <summary>Adds a new object to the list.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleAddNewAction : TBoldListHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldListHandleAddNewAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sAddNew;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldHandle!.List!.CanCreateNew;
        }
    }

    /// <summary>Adds the new object to the handle's mutable list when it has one, else to its list.</summary>
    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        if (BoldHandle!.MutableList is not null)
        {
            BoldHandle.MutableList.AddNew();
        }
        else
        {
            BoldHandle.List!.AddNew();
        }
    }
}

/// <summary>Moves the current element up in the list.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleMoveUpAction : TBoldListHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldListHandleMoveUpAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sMoveUp;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldHandle!.List!.CanMove(BoldHandle.CurrentIndex, BoldHandle.CurrentIndex - 1);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldHandle!.List!.Move(BoldHandle.CurrentIndex, BoldHandle.CurrentIndex - 1);
    }
}

/// <summary>Moves the current element down in the list.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleMoveDownAction : TBoldListHandleAction
{
    [Delphi(PortStatus.Ported)]
    public TBoldListHandleMoveDownAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sMoveDown;
    }

    [Delphi(PortStatus.Ported)]
    protected override void CheckAllowEnable(ref bool EnableAction)
    {
        base.CheckAllowEnable(ref EnableAction);
        if (EnableAction)
        {
            EnableAction = BoldHandle!.List!.CanMove(BoldHandle.CurrentIndex, BoldHandle.CurrentIndex + 1);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        BoldHandle!.List!.Move(BoldHandle.CurrentIndex, BoldHandle.CurrentIndex + 1);
    }
}
