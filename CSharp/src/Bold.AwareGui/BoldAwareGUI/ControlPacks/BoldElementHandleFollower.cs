// Delphi: Source/BoldAwareGUI/ControlPacks/BoldElementHandleFollower.pas
// Port map: CSharp/port/units/BoldElementHandleFollower.md

using static Bold.BoldControlPackDefs;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>
/// Connects a handle to a follower: the follower's element is the handle's value, and a new value of the handle makes
/// the follower follow it (through the display queue, ahead of the follower).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldElementHandleFollower : TBoldQueueable
{
    private TBoldElementHandle? fBoldHandle;
    private TBoldFollower? fFollower;
    private bool fFollowerValueCurrent;
    private TBoldSubscriber? fSubscriber;

    /// <summary>A handle follower with a follower for the controller, matched by the object (the control).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldElementHandleFollower(object? MatchObject, TBoldFollowerController Controller)
        : base(null)
    {
        fSubscriber = new TBoldPassthroughSubscriber(Receive);
        fFollower = new TBoldFollower(MatchObject, Controller);
        fFollower.PrioritizedQueuable = this;
        fFollowerValueCurrent = true;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldElementHandleFollower.Destroy")]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fFollower);
        SysUtils.FreeAndNil(ref fSubscriber);
        base.Dispose();
    }

    /// <summary>
    /// Makes the follower current (it follows the handle's value, and the handle's identity changes are subscribed to)
    /// or out of date (no element, queued for display).
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested PropagateValue, Subscribe and SubscribeToHandleReference are private helpers.")]
    private void SetFollowerValueCurrent(bool value)
    {
        if (value != FollowerValueCurrent)
        {
            if (value)
            {
                RemoveFromDisplayList();
                PropagateValue();
                SubscribeToHandle();
            }
            else
            {
                Follower.Element = null;
                Subscriber.CancelAllSubscriptions();
                AddToDisplayList();
            }

            fFollowerValueCurrent = value;
        }

        SubscribeToHandleReference();
    }

    private void PropagateValue()
    {
        System.Diagnostics.Debug.Assert(fFollower is not null);
        Follower.Element = BoldHandle?.Value;
    }

    private void SubscribeToHandle() =>
        BoldHandle?.AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beValueIdentityChanged), breValueIdentityChanged);

    private void SubscribeToHandleReference() =>
        BoldHandle?.AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beDestroying), breHandleNil);

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? value)
    {
        if (value != BoldHandle)
        {
            fBoldHandle = value;
            // will force subscription on Handle
            FollowerValueCurrent = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool FollowerValueCurrent
    {
        get => fFollowerValueCurrent;
        set => SetFollowerValueCurrent(value);
    }

    /// <summary>A disposed handle is forgotten; any other event makes the follower out of date.</summary>
    [Delphi(PortStatus.Ported)]
    protected void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == breHandleNil)
        {
            BoldHandle = null;
        }
        else
        {
            FollowerValueCurrent = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Display() => FollowerValueCurrent = true;

    [Delphi(PortStatus.Ported)] protected TBoldSubscriber Subscriber => fSubscriber!;

    [Delphi(PortStatus.Ported)]
    public override void Apply() => throw new EBoldInternal(SysUtils.Format("%s.Apply called (abstract at this  level)", GetType().Name));

    [Delphi(PortStatus.Ported)]
    public override void DiscardChange() => throw new EBoldInternal(SysUtils.Format("%s.DiscardChange called (abstract at this  level)", GetType().Name));

    [Delphi(PortStatus.Ported)]
    public TBoldElementHandle? BoldHandle
    {
        get => fBoldHandle;
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldFollower Follower => fFollower!;
}
