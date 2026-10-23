// Delphi: Source/Common/Subscription/BoldSubscription.pas
// Port map: CSharp/port/units/BoldSubscription.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldSubscription", Symbol = "TBoldEvent", Note = "Global using alias of int, in BoldSubscription.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSubscription", Symbol = "TBoldRequestedEvent", Note = "Global using alias of int, in BoldSubscription.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSubscription", Symbol = "TBoldSmallEvent", Note = "Global using alias of int (0..31), in BoldSubscription.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSubscription", Symbol = "TBoldSmallEventSet", Note = "Global using alias of DelphiSet<int>, in BoldSubscription.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSubscription", Symbol = "TBoldPublisherFlags", Note = "Global using alias of DelphiSet<TBoldPublisherFlag>, in BoldSubscription.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSubscription", Symbol = "TBoldSubscriptionList", Note = "Global using alias of TList, in BoldSubscription.Aliases.cs.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSubscription", Symbol = "initialization", Note = "Created the post-notify queue; each thread creates its own on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSubscription", Symbol = "finalization", Note = "Freed the post-notify queue; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Receives an event a subscriber subscribed to.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldEventHandler(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent);

/// <summary>Receives an event with its arguments.</summary>
[Delphi(PortStatus.Ported, Note = "array of const becomes object?[].")]
public delegate void TBoldExtendedEventHandler(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args);

/// <summary>Answers a query (such as "may this object be deleted?"); false vetoes.</summary>
[Delphi(PortStatus.Ported, Note = "array of const becomes object?[].")]
public delegate bool TBoldQueryHandler(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args, TBoldSubscriber? Subscriber);

[Delphi(PortStatus.Ported)]
public enum TBoldPublisherFlag { bpfNeedsPacking }

/// <summary>
/// Sends events to subscriptions. Subscriptions of subscribers that went away are removed after the outermost
/// notification ends, so sending can iterate the list safely.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPublisher : TBoldMemoryManagedObject, IDisposable
{
    private TBoldSubscriptionList? fSubscriptions;
    private TBoldPublisherFlags fPublisherFlags;

    [Delphi(PortStatus.Ported)]
    public TBoldPublisher()
    {
        fSubscriptions = new TBoldSubscriptionList();
    }

    /// <summary>Call NotifySubscribersAndClearSubscriptions first, as Delphi asserts.</summary>
    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        System.Diagnostics.Debug.Assert(fSubscriptions is null);
        if (NeedsPacking)
        {
            RemoveFromPostNotificationQueue(this);
        }

        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSubscriptionList? Subscriptions => fSubscriptions;

    [Delphi(PortStatus.Ported)]
    private bool GetHasSubscribers() => fSubscriptions is not null && fSubscriptions.Count > 0;

    [Delphi(PortStatus.Ported)]
    internal void AddToSubscriptions(TBoldSubscription Subscription) => Subscriptions!.Add(Subscription);

    [Delphi(PortStatus.Ported)]
    private bool GetNeedsPacking() => GetPublisherFlag(TBoldPublisherFlag.bpfNeedsPacking);

    /// <summary>Marks the list for packing; the first mark queues a pack for after the notification.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetNeedsPacking(bool Value)
    {
        if (!NeedsPacking)
        {
            SetPublisherFlag(TBoldPublisherFlag.bpfNeedsPacking, Value);
            DelayTillAfterNotification(PackSubscriptions, null, this);
        }
        else
        {
            SetPublisherFlag(TBoldPublisherFlag.bpfNeedsPacking, Value);
        }
    }

    /// <summary>Runs the event now when no notification is going on, otherwise after the outermost one ends.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void DelayTillAfterNotification(TNotifyEvent Event, object? Sender, object? Receiver)
    {
        if (BoldSubscription.G_NotificationNesting == 0)
        {
            Event(Sender);
        }
        else
        {
            BoldSubscription.PostNotifyQueue.Add(Event, Sender, Receiver);
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void RemoveFromPostNotificationQueue(object Receiver) =>
        BoldSubscription.G_PostNotifyQueue?.RemoveAllForReceiver(Receiver);

    /// <summary>Removes the subscriptions whose subscriber went away.</summary>
    [Delphi(PortStatus.Ported)]
    private void PackSubscriptions(object? dummy)
    {
        if (Subscriptions is not null)
        {
            for (int i = Subscriptions.Count - 1; i >= 0; i--)
            {
                var subscription = (TBoldSubscription)Subscriptions[i]!;
                if (subscription.Subscriber is null)
                {
                    subscription.UnlinkFromPublisher();
                    Subscriptions[i] = Subscriptions.Last();
                    Subscriptions.Delete(Subscriptions.Count - 1);
                }
            }
        }

        NeedsPacking = false;
    }

    [Delphi(PortStatus.Ported)]
    private bool NeedsPacking
    {
        get => GetNeedsPacking();
        set => SetNeedsPacking(value);
    }

    internal bool NeedsPackingInternal
    {
        set => NeedsPacking = value;
    }

    [Delphi(PortStatus.Ported)]
    protected void SetPublisherFlag(TBoldPublisherFlag Flag, bool Value) =>
        fPublisherFlags = Value ? fPublisherFlags + Flag : fPublisherFlags - Flag;

    [Delphi(PortStatus.Ported)]
    protected bool GetPublisherFlag(TBoldPublisherFlag Flag) => fPublisherFlags.Contains(Flag);

    [Delphi(PortStatus.Ported)]
    public static void StartNotify() => BoldSubscription.G_NotificationNesting++;

    /// <summary>Ends a notification; when the outermost ends, the post-notify queue runs.</summary>
    [Delphi(PortStatus.Ported)]
    public static void EndNotify()
    {
        BoldSubscription.G_NotificationNesting--;
        if (BoldSubscription.G_NotificationNesting == 0 && BoldSubscription.G_PostNotifyQueue is not null && !BoldSubscription.G_InPostNotification)
        {
            BoldSubscription.G_InPostNotification = true;
            try
            {
                BoldSubscription.G_PostNotifyQueue.DequeueAll(); // may add and remove entries; the queue handles it
            }
            finally
            {
                BoldSubscription.G_InPostNotification = false;
            }
        }
    }

    /// <summary>Sends beDestroying, then unlinks every subscription; the publisher takes no more subscriptions.</summary>
    [Delphi(PortStatus.Ported)]
    public void NotifySubscribersAndClearSubscriptions(object? Originator)
    {
        if (fSubscriptions is null)
        {
            return;
        }

        SendExtendedEvent(Originator, BoldSubscription.beDestroying, []);
        for (int i = 0; i < fSubscriptions.Count; i++)
        {
            ((TBoldSubscription)fSubscriptions[i]!).UnlinkFromPublisher();
        }

        fSubscriptions = null;
    }

    /// <summary>Subscribes to a set of small events (0..31); an existing small subscription with the same requested event is extended.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddSmallSubscription(TBoldSubscriber? Subscriber, TBoldSmallEventSet Events, TBoldRequestedEvent RequestedEvent)
    {
        if (Subscriber is null)
        {
            return;
        }

        // Search the shorter of the two lists.
        if (Subscriptions!.Count < Subscriber.Subscriptions.Count)
        {
            for (int i = 0; i < Subscriptions.Count; i++)
            {
                var subscription = (TBoldSubscription)Subscriptions[i]!;
                if (subscription.Subscriber == Subscriber && subscription.RequestedEvent == RequestedEvent
                    && subscription is TBoldSmallEventSubscription small)
                {
                    small.ExtendEvents(Events);
                    return;
                }
            }
        }
        else
        {
            for (int i = 0; i < Subscriber.Subscriptions.Count; i++)
            {
                var subscription = (TBoldSubscription)Subscriber.Subscriptions[i]!;
                if (subscription.Publisher == this && subscription.RequestedEvent == RequestedEvent
                    && subscription is TBoldSmallEventSubscription small)
                {
                    small.ExtendEvents(Events);
                    return;
                }
            }
        }

        _ = new TBoldSmallEventSubscription(this, Subscriber, Events, RequestedEvent);
    }

    /// <summary>Subscribes to one event; nothing happens when the same subscription exists.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddSubscription(TBoldSubscriber? Subscriber, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Subscriber is null)
        {
            return;
        }

        if (Subscriptions!.Count < Subscriber.Subscriptions.Count)
        {
            for (int i = 0; i < Subscriptions.Count; i++)
            {
                var subscription = (TBoldSubscription)Subscriptions[i]!;
                if (subscription.Subscriber == Subscriber && subscription.RequestedEvent == RequestedEvent && subscription.IsMatchingEvent(OriginalEvent))
                {
                    return;
                }
            }
        }
        else
        {
            for (int i = 0; i < Subscriber.Subscriptions.Count; i++)
            {
                var subscription = (TBoldSubscription)Subscriber.Subscriptions[i]!;
                if (subscription.Publisher == this && subscription.RequestedEvent == RequestedEvent && subscription.IsMatchingEvent(OriginalEvent))
                {
                    return;
                }
            }
        }

        _ = new TBoldEventSubscription(this, Subscriber, OriginalEvent, RequestedEvent);
    }

    /// <summary>Sends the event to every matching subscription that still has a subscriber.</summary>
    [Delphi(PortStatus.Ported)]
    public void SendExtendedEvent(object? Originator, TBoldEvent OriginalEvent, object?[] Args)
    {
        if (Subscriptions is null)
        {
            return;
        }

        StartNotify();
        try
        {
            for (int i = 0; i < Subscriptions.Count; i++)
            {
                var subscription = (TBoldSubscription)fSubscriptions![i]!;
                TBoldSubscriber? subscriber = subscription.Subscriber;
                if (subscriber is not null && subscription.IsMatchingEvent(OriginalEvent))
                {
                    if (subscriber.HandlesExtendedEvents)
                    {
                        subscriber.ReceiveExtended(Originator, OriginalEvent, subscription.RequestedEvent, Args);
                    }
                    else
                    {
                        subscriber.Receive(Originator, OriginalEvent, subscription.RequestedEvent);
                    }
                }
            }
        }
        finally
        {
            EndNotify();
        }
    }

    /// <summary>Asks every matching subscriber; the first false answer stops and returns false.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi skips EndNotify when a subscriber says no, leaving the notification nesting raised; here EndNotify always runs.")]
    public bool SendQuery(object? Originator, TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber)
    {
        if (Subscriptions is not null)
        {
            StartNotify();
            try
            {
                for (int i = 0; i < Subscriptions.Count; i++)
                {
                    var subscription = (TBoldSubscription)Subscriptions[i]!;
                    if (subscription.Subscriber is not null && subscription.IsMatchingEvent(OriginalEvent)
                        && !subscription.Subscriber.Answer(Originator, OriginalEvent, subscription.RequestedEvent, Args, Subscriber))
                    {
                        return false;
                    }
                }
            }
            finally
            {
                EndNotify();
            }
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    public bool HasSubscribers => GetHasSubscribers();
}

/// <summary>A link between a publisher and a subscriber. It is dropped when both sides have let go of it.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSubscription : TBoldMemoryManagedObject
{
    private TBoldPublisher? fPublisher;
    private readonly TBoldRequestedEvent fRequestedEvent;
    private TBoldSubscriber? fSubscriber;

    [Delphi(PortStatus.Ported)]
    protected TBoldSubscription(TBoldPublisher Publisher, TBoldSubscriber Subscriber, TBoldRequestedEvent RequestedEvent)
    {
        fPublisher = Publisher;
        fSubscriber = Subscriber;
        fRequestedEvent = RequestedEvent;
        Publisher.AddToSubscriptions(this);
        Subscriber.AddToSubscriptions(this);
    }

    [Delphi(PortStatus.Ported)]
    protected internal abstract bool IsMatchingEvent(TBoldEvent OriginalEvent);

    [Delphi(PortStatus.Ported)]
    protected internal abstract void CloneTo(TBoldSubscriber Subscriber, TBoldRequestedEvent NewRequestedEvent);

    /// <summary>The subscriber lets go; the publisher packs its list later.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi frees the subscription when both links are gone; the garbage collector does that here.")]
    protected internal void UnlinkFromSubscriber()
    {
        fSubscriber = null;
        if (Publisher is not null)
        {
            Publisher.NeedsPackingInternal = true;
        }
    }

    [Delphi(PortStatus.Ported, Note = "Delphi frees the subscription when both links are gone; the garbage collector does that here.")]
    protected internal void UnlinkFromPublisher() => fPublisher = null;

    [Delphi(PortStatus.Ported)] protected internal TBoldPublisher? Publisher => fPublisher;
    [Delphi(PortStatus.Ported)] protected internal TBoldRequestedEvent RequestedEvent => fRequestedEvent;
    [Delphi(PortStatus.Ported)] protected internal TBoldSubscriber? Subscriber => fSubscriber;
}

/// <summary>A subscription to one event.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldEventSubscription : TBoldSubscription
{
    private readonly TBoldEvent fEvent;

    [Delphi(PortStatus.Ported)]
    public TBoldEventSubscription(TBoldPublisher Publisher, TBoldSubscriber Subscriber, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
        : base(Publisher, Subscriber, RequestedEvent)
    {
        fEvent = OriginalEvent;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override bool IsMatchingEvent(TBoldEvent OriginalEvent) => OriginalEvent == fEvent;

    [Delphi(PortStatus.Ported)]
    protected internal override void CloneTo(TBoldSubscriber Subscriber, TBoldRequestedEvent NewRequestedEvent) =>
        Publisher?.AddSubscription(Subscriber, fEvent, NewRequestedEvent);
}

/// <summary>A subscription to a set of small events (0..31).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSmallEventSubscription : TBoldSubscription
{
    private TBoldSmallEventSet fEvents;

    [Delphi(PortStatus.Ported)]
    public TBoldSmallEventSubscription(TBoldPublisher Publisher, TBoldSubscriber Subscriber, TBoldSmallEventSet Events, TBoldRequestedEvent RequestedEvent)
        : base(Publisher, Subscriber, RequestedEvent)
    {
        fEvents = Events;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void ExtendEvents(TBoldSmallEventSet Events) => fEvents += Events;

    [Delphi(PortStatus.Ported)]
    protected internal override bool IsMatchingEvent(TBoldEvent OriginalEvent) =>
        OriginalEvent is >= 0 and < 32 && fEvents.Contains(OriginalEvent);

    [Delphi(PortStatus.Ported)]
    protected internal override void CloneTo(TBoldSubscriber Subscriber, TBoldRequestedEvent NewRequestedEvent) =>
        Publisher?.AddSmallSubscription(Subscriber, fEvents, NewRequestedEvent);
}

/// <summary>Receives events from publishers it subscribed to. Disposing cancels every subscription.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSubscriber : TBoldMemoryManagedObject, IDisposable
{
    private TList? fSubscriptions;

    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        if (fSubscriptions is not null)
        {
            CancelAllSubscriptions();
            fSubscriptions = null;
        }

        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TList GetSubscriptions() => fSubscriptions ??= new TList();

    [Delphi(PortStatus.Ported)]
    internal TList Subscriptions => GetSubscriptions();

    [Delphi(PortStatus.Ported)]
    internal void AddToSubscriptions(TBoldSubscription Subscription) => Subscriptions.Add(Subscription);

    [Delphi(PortStatus.Ported)]
    protected internal abstract void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent);

    [Delphi(PortStatus.Ported)]
    protected internal virtual void ReceiveExtended(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        // do nothing
    }

    /// <summary>Answers a query; a subscriber that subscribes to queries must override it.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual bool Answer(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        throw new EBold(SysUtils.Format(BoldCommonConst.sAnswerNotImplemented, GetType().Name, Originator?.GetType().Name));

    [Delphi(PortStatus.Ported)]
    protected virtual string GetContextString() => "";

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetHandlesExtendedEvents() => false;

    /// <summary>Lets go of every subscription; publishers drop them after the notification.</summary>
    [Delphi(PortStatus.Ported)]
    public void CancelAllSubscriptions()
    {
        if (Subscriptions.Count == 0)
        {
            return;
        }

        TBoldPublisher.StartNotify();
        try
        {
            for (int i = 0; i < Subscriptions.Count; i++)
            {
                ((TBoldSubscription)Subscriptions[i]!).UnlinkFromSubscriber();
            }

            fSubscriptions!.Count = 0;
        }
        finally
        {
            TBoldPublisher.EndNotify();
        }
    }

    /// <summary>Gives <paramref name="Subscriber"/> the subscriptions this one has for <paramref name="OldRequestedEvent"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public void CloneSubscriptions(TBoldSubscriber Subscriber, TBoldRequestedEvent OldRequestedEvent, TBoldRequestedEvent NewRequestedEvent)
    {
        for (int i = 0; i < Subscriptions.Count; i++)
        {
            var subscription = (TBoldSubscription)Subscriptions[i]!;
            if (subscription.RequestedEvent == OldRequestedEvent)
            {
                subscription.CloneTo(Subscriber, NewRequestedEvent);
            }
        }
    }

    [Delphi(PortStatus.Ported)] public string ContextString => GetContextString();
    [Delphi(PortStatus.Ported)] public bool HandlesExtendedEvents => GetHandlesExtendedEvents();

    /// <summary>The number of subscriptions this subscriber holds.</summary>
    public int SubscriptionCount => fSubscriptions?.Count ?? 0;
}

/// <summary>A subscriber that passes events to delegates.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPassthroughSubscriber : TBoldSubscriber
{
    private TBoldEventHandler? fReceiveFunc;
    private readonly TBoldExtendedEventHandler? fExtendedReceiveFunc;
    private readonly TBoldQueryHandler? fAnswerFunc;

    [Delphi(PortStatus.Ported)]
    public TBoldPassthroughSubscriber(TBoldEventHandler? receiveFunc)
    {
        fReceiveFunc = receiveFunc;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPassthroughSubscriber.CreateWithExtendedReceive")]
    public TBoldPassthroughSubscriber(TBoldExtendedEventHandler ExtendedReceiveFunc)
    {
        fExtendedReceiveFunc = ExtendedReceiveFunc;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPassthroughSubscriber.CreateWithReceiveAndAnswer")]
    public TBoldPassthroughSubscriber(TBoldEventHandler? ReceiveFunc, TBoldQueryHandler? AnswerFunc)
    {
        fReceiveFunc = ReceiveFunc;
        fAnswerFunc = AnswerFunc;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent) =>
        fReceiveFunc?.Invoke(Originator, OriginalEvent, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    protected internal override void ReceiveExtended(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) =>
        fExtendedReceiveFunc?.Invoke(Originator, OriginalEvent, RequestedEvent, Args);

    /// <summary>The answer delegate's answer, or true without one.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override bool Answer(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        fAnswerFunc is null || fAnswerFunc(Originator, OriginalEvent, RequestedEvent, Args, Subscriber);

    [Delphi(PortStatus.Ported)]
    protected override bool GetHandlesExtendedEvents() => fExtendedReceiveFunc is not null;

    [Delphi(PortStatus.Ported)]
    public TBoldEventHandler? receiveFunc
    {
        get => fReceiveFunc;
        set => fReceiveFunc = value;
    }
}

/// <summary>An object others can subscribe to; the publisher is created on first subscription.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubscribableObject : TBoldFlaggedObject, IDisposable
{
    private TBoldPublisher? fPublisher;

    /// <summary>Sends beDestroying to the subscribers and frees the publisher.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        FreePublisher();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private bool GetHasSubscribers() => fPublisher is not null && Publisher.HasSubscribers;

    [Delphi(PortStatus.Ported)]
    private TBoldPublisher GetPublisher() => fPublisher ??= new TBoldPublisher();

    [Delphi(PortStatus.Ported)]
    protected void FreePublisher()
    {
        if (fPublisher is not null)
        {
            fPublisher.NotifySubscribersAndClearSubscriptions(this);
            SysUtils.FreeAndNil(ref fPublisher);
        }
    }

    [Delphi(PortStatus.Ported)] protected bool HasSubscribers => GetHasSubscribers();
    [Delphi(PortStatus.Ported)] protected TBoldPublisher Publisher => GetPublisher();

    [Delphi(PortStatus.Ported)]
    public void AddSmallSubscription(TBoldSubscriber? Subscriber, TBoldSmallEventSet Events, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSmallSubscription(Subscriber, Events, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void AddSubscription(TBoldSubscriber? Subscriber, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSubscription(Subscriber, OriginalEvent, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public virtual void SendEvent(TBoldEvent OriginalEvent) => fPublisher?.SendExtendedEvent(this, OriginalEvent, []);

    [Delphi(PortStatus.Ported)]
    public virtual void SendExtendedEvent(TBoldEvent OriginalEvent, object?[] Args) => fPublisher?.SendExtendedEvent(this, OriginalEvent, Args);

    [Delphi(PortStatus.Ported)]
    public virtual bool SendQuery(TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        fPublisher is null || fPublisher.SendQuery(this, OriginalEvent, Args, Subscriber);
}

/// <summary>
/// A component that is told when components it is linked to by free notification go (Delphi's TComponent.Notification);
/// see <see cref="TBoldSubscribableComponent.FreeNotification"/>.
/// </summary>
public interface IBoldNotifiable
{
    void Notification(System.ComponentModel.IComponent AComponent, TOperation Operation);
}

/// <summary>
/// A component others can subscribe to. It also gives Bold's components what Delphi's TComponent gave them: a name,
/// the loading state (between <see cref="BeginInit"/> and <see cref="EndInit"/>, which the WinForms designer calls,
/// ending in <see cref="Loaded"/>), the destroying state, and free notification.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Derives from System.ComponentModel.Component where Delphi derives from TComponent; ISupportInitialize stands in for loading from a form file.")]
public class TBoldSubscribableComponent : System.ComponentModel.Component, System.ComponentModel.ISupportInitialize, IBoldNotifiable
{
    private TBoldPublisher? fPublisher;
    private string? fName;
    private bool fLoading;
    private bool fDestroying;
    private List<System.ComponentModel.IComponent>? fFreeNotifies;

    [Delphi(PortStatus.Ported, Symbol = "TBoldSubscribableComponent.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Destroying();
            FreePublisher();
            if (fFreeNotifies is not null)
            {
                System.ComponentModel.IComponent[] notifies = [.. fFreeNotifies];
                fFreeNotifies = null;
                foreach (System.ComponentModel.IComponent component in notifies)
                {
                    if (component is not TBoldSubscribableComponent)
                    {
                        component.Disposed -= FreeNotifyComponentDisposed;
                    }

                    (component as TBoldSubscribableComponent)?.fFreeNotifies?.Remove(this);
                    (component as IBoldNotifiable)?.Notification(this, TOperation.opRemove);
                }
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>The component's name: the name given, else the designer's name for it (its site's), else empty.</summary>
    public string Name
    {
        get => fName ?? Site?.Name ?? "";
        set => fName = value;
    }

    /// <summary>
    /// True between <see cref="BeginInit"/> and <see cref="EndInit"/>, while the designer's code sets the properties
    /// (Delphi's csLoading and csReading).
    /// </summary>
    public bool IsLoading => fLoading;

    /// <summary>True once the component is being disposed (Delphi's csDestroying).</summary>
    public bool IsDestroying => fDestroying;

    /// <summary>True in a designer (Delphi's csDesigning).</summary>
    public bool IsDesigning => DesignMode;

    /// <summary>Starts loading: property setters defer what they would do until <see cref="Loaded"/>.</summary>
    public virtual void BeginInit() => fLoading = true;

    /// <summary>Ends loading and calls <see cref="Loaded"/>.</summary>
    public virtual void EndInit()
    {
        if (fLoading)
        {
            fLoading = false;
            Loaded();
        }
    }

    /// <summary>Called when loading ends, as Delphi calls Loaded after reading a component from its form.</summary>
    protected virtual void Loaded()
    {
    }

    /// <summary>Marks the component as being destroyed (Delphi's TComponent.Destroying); disposing does this first.</summary>
    public void Destroying() => fDestroying = true;

    /// <summary>
    /// Makes <paramref name="AComponent"/> be told (through its Notification, with opRemove) when this component is
    /// disposed, and this component when it is, as Delphi's FreeNotification. A component other than a Bold component
    /// is told when it implements <see cref="IBoldNotifiable"/>, and this component is told when it is disposed (through
    /// its Disposed event).
    /// </summary>
    public void FreeNotification(System.ComponentModel.IComponent? AComponent)
    {
        if (AComponent is null || AComponent == this)
        {
            return;
        }

        fFreeNotifies ??= [];
        if (!fFreeNotifies.Contains(AComponent))
        {
            fFreeNotifies.Add(AComponent);
            if (AComponent is TBoldSubscribableComponent boldComponent)
            {
                boldComponent.FreeNotification(this);
            }
            else
            {
                AComponent.Disposed += FreeNotifyComponentDisposed;
            }
        }
    }

    /// <summary>Stops the free notification between this component and <paramref name="AComponent"/>.</summary>
    public void RemoveFreeNotification(System.ComponentModel.IComponent? AComponent)
    {
        if (AComponent is not null && fFreeNotifies is not null && fFreeNotifies.Remove(AComponent))
        {
            if (AComponent is TBoldSubscribableComponent boldComponent)
            {
                boldComponent.RemoveFreeNotification(this);
            }
            else
            {
                AComponent.Disposed -= FreeNotifyComponentDisposed;
            }
        }
    }

    /// <summary>A component other than a Bold component (a control, say) that this one is linked to was disposed.</summary>
    private void FreeNotifyComponentDisposed(object? sender, EventArgs e)
    {
        if (sender is System.ComponentModel.IComponent component && fFreeNotifies is not null && fFreeNotifies.Remove(component))
        {
            component.Disposed -= FreeNotifyComponentDisposed;
            Notification(component, TOperation.opRemove);
        }
    }

    /// <summary>Told when a component this one is linked to by free notification is removed (disposed).</summary>
    protected virtual void Notification(System.ComponentModel.IComponent AComponent, TOperation Operation)
    {
    }

    void IBoldNotifiable.Notification(System.ComponentModel.IComponent AComponent, TOperation Operation) => Notification(AComponent, Operation);

    [Delphi(PortStatus.Ported)]
    private TBoldPublisher GetPublisher() => fPublisher ??= new TBoldPublisher();

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetHasSubscribers() => fPublisher is not null && Publisher.HasSubscribers;

    [Delphi(PortStatus.Ported)] protected TBoldPublisher Publisher => GetPublisher();

    [Delphi(PortStatus.Ported)]
    protected void FreePublisher()
    {
        if (fPublisher is not null)
        {
            fPublisher.NotifySubscribersAndClearSubscriptions(this);
            SysUtils.FreeAndNil(ref fPublisher);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddSmallSubscription(TBoldSubscriber? Subscriber, TBoldSmallEventSet Events, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSmallSubscription(Subscriber, Events, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void AddSubscription(TBoldSubscriber? Subscriber, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSubscription(Subscriber, OriginalEvent, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void SendEvent(object? Originator, TBoldEvent OriginalEvent) => fPublisher?.SendExtendedEvent(Originator, OriginalEvent, []);

    [Delphi(PortStatus.Ported)]
    public void SendExtendedEvent(object? Originator, TBoldEvent OriginalEvent, object?[] Args) =>
        fPublisher?.SendExtendedEvent(Originator, OriginalEvent, Args);

    [Delphi(PortStatus.Ported)]
    public bool SendQuery(object? Originator, TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        fPublisher is null || fPublisher.SendQuery(Originator, OriginalEvent, Args, Subscriber);

    [Delphi(PortStatus.Ported)] public bool HasSubscribers => GetHasSubscribers();
}

/// <summary>A persistent (assignable) object others can subscribe to; it has a publisher from the start.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubscribablePersistent : TPersistent, IDisposable
{
    private TBoldPublisher? fPublisher;

    [Delphi(PortStatus.Ported)]
    public TBoldSubscribablePersistent()
    {
        fPublisher = new TBoldPublisher();
    }

    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        FreePublisher();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldPublisher GetPublisher() => fPublisher ??= new TBoldPublisher();

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetHasSubscribers() => fPublisher is not null && Publisher.HasSubscribers;

    [Delphi(PortStatus.Ported)] protected TBoldPublisher Publisher => GetPublisher();

    [Delphi(PortStatus.Ported)]
    protected void FreePublisher()
    {
        if (fPublisher is not null)
        {
            fPublisher.NotifySubscribersAndClearSubscriptions(this);
            SysUtils.FreeAndNil(ref fPublisher);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddSmallSubscription(TBoldSubscriber? Subscriber, TBoldSmallEventSet Events, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSmallSubscription(Subscriber, Events, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void AddSubscription(TBoldSubscriber? Subscriber, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSubscription(Subscriber, OriginalEvent, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void SendEvent(object? Originator, TBoldEvent OriginalEvent) => fPublisher?.SendExtendedEvent(Originator, OriginalEvent, []);

    [Delphi(PortStatus.Ported)]
    public void SendExtendedEvent(object? Originator, TBoldEvent OriginalEvent, object?[] Args) =>
        Publisher.SendExtendedEvent(Originator, OriginalEvent, Args);

    [Delphi(PortStatus.Ported)]
    public bool SendQuery(object? Originator, TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        fPublisher is null || fPublisher.SendQuery(Originator, OriginalEvent, Args, Subscriber);

    [Delphi(PortStatus.Ported)] public bool HasSubscribers => GetHasSubscribers();
}

/// <summary>Event numbers and the post-notify queue of BoldSubscription.</summary>
public static partial class BoldSubscription
{
    [Delphi(PortStatus.Ported)] public const int beMinSmallReserved = 0;
    [Delphi(PortStatus.Ported)] public const int beMaxSmallReserved = 23;
    [Delphi(PortStatus.Ported)] public const int beMinSmallUser = 24;
    [Delphi(PortStatus.Ported)] public const int beMaxSmallUser = 31;
    [Delphi(PortStatus.Ported)] public const int beMaxEvent = 32767;
    [Delphi(PortStatus.Ported)] public const int beMinUser = 4096;
    [Delphi(PortStatus.Ported)] public const int beMaxUser = 32767;
    [Delphi(PortStatus.Ported)] public const int bqMinQuery = 32768;
    [Delphi(PortStatus.Ported)] public const int bpeMinReserved = 1024;
    [Delphi(PortStatus.Ported)] public const int beDestroying = 0;
    [Delphi(PortStatus.Ported)] public const int beMemberChanged = 2;
    [Delphi(PortStatus.Ported)] public const int beObjectDeleted = 3;
    [Delphi(PortStatus.Ported)] public const int beObjectCreated = 11;
    [Delphi(PortStatus.Ported)] public const int beItemAdded = 4;
    [Delphi(PortStatus.Ported)] public const int beItemDeleted = 5;
    [Delphi(PortStatus.Ported)] public const int beItemReplaced = 6;
    [Delphi(PortStatus.Ported)] public const int beOrderChanged = 7;
    [Delphi(PortStatus.Ported)] public const int beValueChanged = 8;
    [Delphi(PortStatus.Ported)] public const int beValueIdentityChanged = 9;
    [Delphi(PortStatus.Ported)] public const int beValueInvalid = 12;
    [Delphi(PortStatus.Ported)] public const int beDerivedSoonDestroyed = 13;
    [Delphi(PortStatus.Ported)] public const int beLocatorDestroying = 14;
    [Delphi(PortStatus.Ported)] public const int beDirtyListInvalidOrItemDeleted = 15;
    [Delphi(PortStatus.Ported)] public const int beQualifierChanged = 16;
    [Delphi(PortStatus.Ported)] public const int beDeactivating = 17;
    [Delphi(PortStatus.Ported)] public const int beRolledBack = 18;
    [Delphi(PortStatus.Ported)] public const int bePostUpdateId = 19;
    [Delphi(PortStatus.Ported)] public const int bePreUpdateId = 20;
    [Delphi(PortStatus.Ported)] public const int beObjectFetched = 21;
    [Delphi(PortStatus.Ported)] public const int breReEvaluate = 8;
    [Delphi(PortStatus.Ported)] public const int breReSubscribe = 10;
    [Delphi(PortStatus.Ported)] public const int beServerSubscriberRemoved = 22;
    [Delphi(PortStatus.Ported, Note = "A static readonly set; C# has no set constants.")]
    public static readonly TBoldSmallEventSet beValueEvents = TBoldSmallEventSet.Of(beItemAdded, beItemDeleted, beItemReplaced, beOrderChanged, beValueChanged, beValueInvalid);
    [Delphi(PortStatus.Ported)] public const int bpeStartFetch = 1024;
    [Delphi(PortStatus.Ported)] public const int bpeEndFetch = 1025;
    [Delphi(PortStatus.Ported)] public const int bpeStartUpdate = 1026;
    [Delphi(PortStatus.Ported)] public const int bpeEndUpdate = 1027;
    [Delphi(PortStatus.Ported)] public const int bpeFetchObject = 1028;
    [Delphi(PortStatus.Ported)] public const int bpeFetchMember = 1029;
    [Delphi(PortStatus.Ported)] public const int bpeUpdateObject = 1030;
    [Delphi(PortStatus.Ported)] public const int bpeDeleteObject = 1031;
    [Delphi(PortStatus.Ported)] public const int bpeCreateObject = 1032;
    [Delphi(PortStatus.Ported)] public const int bpeStartFetchID = 1033;
    [Delphi(PortStatus.Ported)] public const int bpeEndFetchID = 1034;
    [Delphi(PortStatus.Ported)] public const int bpeFetchId = 1035;
    [Delphi(PortStatus.Ported)] public const int bpeProgressStart = 1036;
    [Delphi(PortStatus.Ported)] public const int bpeProgressEnd = 1037;
    [Delphi(PortStatus.Ported)] public const int bpeMaxReserved = 1037;
    [Delphi(PortStatus.Ported)] public const int bePrepareModify = 38;
    [Delphi(PortStatus.Ported)] public const int beCompleteModify = 39;
    [Delphi(PortStatus.Ported)] public const int bePrepareDelete = 41;
    [Delphi(PortStatus.Ported)] public const int bqMayUpdate = 32769;
    [Delphi(PortStatus.Ported)] public const int bqMayModify = 32770;
    [Delphi(PortStatus.Ported)] public const int bqMayDelete = 32771;
    [Delphi(PortStatus.Ported)] public const int bqMayCommit = 32772;
    [Delphi(PortStatus.Ported)] public const int bqMaxSubscription = 32774;

    // Notification state. Delphi keeps it in unit variables; here each thread has its own, as an object space is
    // used from one thread.
    [ThreadStatic] internal static int G_NotificationNesting;
    [ThreadStatic] internal static TBoldEventQueue? G_PostNotifyQueue;
    [ThreadStatic] internal static bool G_InPostNotification;

    internal static TBoldEventQueue PostNotifyQueue => G_PostNotifyQueue ??= new TBoldEventQueue();

    /// <summary>Runs the queued post-notify events now.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldForcedDequeuePostNotify()
    {
        if (G_PostNotifyQueue is not null)
        {
            bool oldInPostNotification = G_InPostNotification;
            G_InPostNotification = true;
            try
            {
                G_PostNotifyQueue.DequeueAll();
            }
            finally
            {
                G_InPostNotification = oldInPostNotification;
            }
        }
    }

    /// <summary>Runs the event after the current notification, or now when none is going on.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldAddEventToPostNotifyQueue(TNotifyEvent Event, object? Sender, object? Receiver) =>
        TBoldPublisher.DelayTillAfterNotification(Event, Sender, Receiver);
}
