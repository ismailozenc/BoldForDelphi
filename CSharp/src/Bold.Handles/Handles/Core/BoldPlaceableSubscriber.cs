// Delphi: Source/Handles/Core/BoldPlaceableSubscriber.pas
// Port map: CSharp/port/units/BoldPlaceableSubscriber.md

using System.ComponentModel;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>Places the subscriptions a placeable subscriber wants on the handle's value.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSubscribeToElementEvent(TBoldElement element, TBoldSubscriber Subscriber);

/// <summary>Receives the events a placeable subscriber got.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldPlaceableSubcriberReceive(TBoldPlaceableSubscriber sender, object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent);

/// <summary>
/// A component that subscribes to the value of a handle through events: OnSubscribeToElement places the subscriptions
/// and OnReceive gets the events, again whenever the handle's value changes.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPlaceableSubscriber : TBoldSubscribableComponentViaBoldElem
{
    private TBoldElementHandle? FBoldHandle;
    private TBoldPlaceableSubcriberReceive? FOnReceive;
    private TBoldSubscribeToElementEvent? FOnSubscribeToElement;
    private TBoldPassthroughSubscriber? FHandleSubscriber;
    private TBoldPassthroughSubscriber? FValueSubscriber;
    private bool FDelayEventsUntilPostNotify;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldPlaceableSubscriber(IContainer? Owner = null)
    {
        Owner?.Add(this);
        FHandleSubscriber = new TBoldPassthroughSubscriber(HandleSubscriberReceive);
        FValueSubscriber = new TBoldPassthroughSubscriber(ValueSubscriberReceive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPlaceableSubscriber.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref FHandleSubscriber);
            SysUtils.FreeAndNil(ref FValueSubscriber);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? Value)
    {
        if (FBoldHandle != Value)
        {
            FHandleSubscriber!.CancelAllSubscriptions();
            FBoldHandle = Value;
            if (Value is not null)
            {
                Value.FreeNotification(this);
                Value.AddSmallSubscription(FHandleSubscriber, TBoldSmallEventSet.Of(beDestroying, beValueIdentityChanged), breReEvaluate);
            }

            HandleValueChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void HandleSubscriberReceive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        Receive(Originator, OriginalEvent, RequestedEvent, []);
        HandleValueChanged();
    }

    /// <summary>Resubscribes to the value now, or after the current notifications when events are delayed.</summary>
    [Delphi(PortStatus.Ported)]
    private void HandleValueChanged()
    {
        if (DelayEventsUntilPostNotify)
        {
            BoldAddEventToPostNotifyQueue(ActOnHandleValueChanged, this, this);
        }
        else
        {
            ActOnHandleValueChanged(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ActOnHandleValueChanged(object? Sender)
    {
        FValueSubscriber?.CancelAllSubscriptions();
        if (BoldHandle?.Value is { } value)
        {
            SubscribeToElement(value, FValueSubscriber!);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ValueSubscriberReceive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        Receive(Originator, OriginalEvent, RequestedEvent, Args);
        if (RequestedEvent == breReSubscribe)
        {
            FValueSubscriber!.CancelAllSubscriptions();
            if (BoldHandle?.Value is { } value)
            {
                SubscribeToElement(value, FValueSubscriber);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDelayEventsUntilPostNotify(bool Value) => FDelayEventsUntilPostNotify = Value;

    /// <summary>Forgets the handle when it goes.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Notification(IComponent AComponent, TOperation Operation)
    {
        base.Notification(AComponent, Operation);
        if (Operation == TOperation.opRemove && AComponent == BoldHandle)
        {
            BoldHandle = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void SubscribeToElement(TBoldElement Element, TBoldSubscriber Subscriber) => FOnSubscribeToElement?.Invoke(Element, Subscriber);

    [Delphi(PortStatus.Ported)]
    protected virtual void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) =>
        FOnReceive?.Invoke(this, Originator, OriginalEvent, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public TBoldElementHandle? BoldHandle
    {
        get => FBoldHandle;
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPlaceableSubcriberReceive? OnReceive
    {
        get => FOnReceive;
        set => FOnReceive = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSubscribeToElementEvent? OnSubscribeToElement
    {
        get => FOnSubscribeToElement;
        set => FOnSubscribeToElement = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool DelayEventsUntilPostNotify
    {
        get => FDelayEventsUntilPostNotify;
        set => SetDelayEventsUntilPostNotify(value);
    }
}
