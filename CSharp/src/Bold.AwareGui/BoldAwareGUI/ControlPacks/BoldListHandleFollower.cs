// Delphi: Source/BoldAwareGUI/ControlPacks/BoldListHandleFollower.pas
// Port map: CSharp/port/units/BoldListHandleFollower.md

using static Bold.BoldControlPackDefs;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>
/// Connects a list handle to a list follower: the follower's element is the handle's list and its current row the
/// handle's current index; with HandleIndexLock, moving the follower's current row moves the handle's current index.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldListHandleFollower : TBoldQueueable
{
    private TBoldAbstractListHandle? fBoldHandle;
    private TBoldFollower? fFollower;
    private bool fFollowerValueCurrent;
    private bool fHandleIndexLock;
    private TBoldSubscriber? fSubscriber;

    /// <summary>A handle follower with a list follower for the controller, matched by the object (the control).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldListHandleFollower(object? MatchObject, TBoldAbstractListAsFollowerListController Controller)
        : base(null)
    {
        fSubscriber = new TBoldPassthroughSubscriber(Receive);
        fFollower = new TBoldFollower(MatchObject, Controller);
        fFollower.PrioritizedQueuable = this;
        fFollowerValueCurrent = true;
        fHandleIndexLock = true;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldListHandleFollower.Destroy")]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fFollower);
        SysUtils.FreeAndNil(ref fSubscriber);
        base.Dispose();
    }

    /// <summary>A disposed handle is forgotten; a new list or current index makes the follower out of date.</summary>
    [Delphi(PortStatus.Ported)]
    protected void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        switch (RequestedEvent)
        {
            case breHandleNil:
                BoldHandle = null;
                break;
            case breListIdentityChanged:
            case breHandleIndexChanged:
                FollowerValueCurrent = false;
                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Display() => FollowerValueCurrent = true;

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldAbstractListHandle? value)
    {
        if (value != BoldHandle)
        {
            fBoldHandle = value;
            // will force subscription on Handle
            FollowerValueCurrent = false;
        }
    }

    /// <summary>
    /// Makes the follower current (it follows the handle's list and current index, and the handle's changes are
    /// subscribed to) or out of date (no element, queued for display).
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested PropagateValue, Subscribe and SubscribeToHandleReference are private helpers.")]
    private void SetFollowerValueCurrent(bool value)
    {
        if (value != FollowerValueCurrent)
        {
            if (value)
            {
                PropagateValue();
                RemoveFromDisplayList();
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
        if (BoldHandle is not null)
        {
            Follower.Element = BoldHandle.List;
            if (HandleIndexLock)
            {
                SetFollowerIndex(((TBoldAbstractListAsFollowerListController)Follower.Controller!).ListIndex(BoldHandle.CurrentIndex));
            }
        }
        else
        {
            Follower.Element = null;
        }
    }

    private void SubscribeToHandle()
    {
        if (BoldHandle is not null)
        {
            BoldHandle.AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beValueIdentityChanged), breListIdentityChanged); // FIXME
            BoldHandle.AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beValueIdentityChanged), breHandleIndexChanged); // FIXME
        }
    }

    private void SubscribeToHandleReference() =>
        BoldHandle?.AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beDestroying), breHandleNil);

    [Delphi(PortStatus.Ported)]
    private bool FollowerValueCurrent
    {
        get => fFollowerValueCurrent;
        set => SetFollowerValueCurrent(value);
    }

    [Delphi(PortStatus.Ported)]
    public override void Apply() => throw new EBoldInternal(SysUtils.Format("%s.Apply called", GetType().Name));

    [Delphi(PortStatus.Ported)]
    public override void DiscardChange() => throw new EBoldInternal(SysUtils.Format("%s.DiscardChange called", GetType().Name));

    /// <summary>Makes the row current, and with HandleIndexLock the handle's current index follows (when in the list).</summary>
    [Delphi(PortStatus.Ported)]
    public void SetFollowerIndex(int index)
    {
        Follower.CurrentIndex = index;
        int NewHandleIndex = ((TBoldAbstractListAsFollowerListController)Follower.Controller!).GetListIndex(Follower);
        if (HandleIndexLock && BoldHandle is not null && BoldHandle.List is not null && BoldHandle.List.Count > NewHandleIndex)
        {
            BoldHandle.CurrentIndex = NewHandleIndex;
        }
    }

    [Delphi(PortStatus.Ported)] protected TBoldSubscriber Subscriber => fSubscriber!;

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractListHandle? BoldHandle
    {
        get => fBoldHandle;
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool HandleIndexLock
    {
        get => fHandleIndexLock;
        set => fHandleIndexLock = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldFollower Follower => fFollower!;
}
