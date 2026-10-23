// Delphi: Source/Handles/Actions/BoldHandleAction.pas
// Port map: CSharp/port/units/BoldHandleAction.md

using System.ComponentModel;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>
/// An action on an element handle: enabled when it has a handle (descendants add conditions in CheckAllowEnable),
/// and it forgets a handle that is disposed. It handles every target.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldHandleAction : TAction
{
    private const int breFreeHandle = 44;

    private TBoldPassthroughSubscriber? fHandleSubscriber;
    private TBoldElementHandle? fBoldElementHandle;

    /// <summary>Adds the action to <paramref name="AOwner"/> when given.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldHandleAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        fHandleSubscriber = new TBoldPassthroughSubscriber(_HandleSubscriberReceive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldHandleAction.Destroy")]
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fHandleSubscriber);
        }
    }

    /// <summary>Forgets the handle when it is disposed.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void _HandleSubscriberReceive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        System.Diagnostics.Debug.Assert(Originator == BoldElementHandle);
        if (RequestedEvent == breFreeHandle)
        {
            BoldElementHandle = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool HandlesTarget(object? Target) => true;

    [Delphi(PortStatus.Ported)]
    private void SetBoldElementHandle(TBoldElementHandle? Value)
    {
        if (fBoldElementHandle != Value)
        {
            fHandleSubscriber!.CancelAllSubscriptions();
            fBoldElementHandle = Value;
            BoldElementHandle?.AddSmallSubscription(fHandleSubscriber, TBoldSmallEventSet.Of(beDestroying), breFreeHandle);
        }
    }

    /// <summary>Enables the action when CheckAllowEnable allows it.</summary>
    [Delphi(PortStatus.Ported)]
    public override void UpdateTarget(object? Target)
    {
        base.UpdateTarget(Target);
        bool EnableAction = true;
        CheckAllowEnable(ref EnableAction);
        Enabled = EnableAction;
    }

    /// <summary>Whether the action may be enabled: here, when it has a handle.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void CheckAllowEnable(ref bool EnableAction) => EnableAction = BoldElementHandle is not null;

    [Delphi(PortStatus.Ported)]
    protected TBoldElementHandle? BoldElementHandle
    {
        get => fBoldElementHandle;
        set => SetBoldElementHandle(value);
    }
}
