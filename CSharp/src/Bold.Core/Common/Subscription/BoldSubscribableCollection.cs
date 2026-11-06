// Delphi: Source/Common/Subscription/BoldSubscribableCollection.pas
// Port map: CSharp/port/units/BoldSubscribableCollection.md

namespace Bold;

/// <summary>A collection that others can subscribe to: it sends Bold events.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubscribableCollection : TCollection, IDisposable
{
    private TBoldPublisher? fPublisher;

    [Delphi(PortStatus.Ported)]
    public TBoldSubscribableCollection(Type ItemClass)
        : base(ItemClass)
    {
        fPublisher = new TBoldPublisher();
    }

    /// <summary>Tells the subscribers the collection goes, and stops sending events.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldSubscribableCollection.Destroy")]
    public virtual void Dispose()
    {
        FreePublisher();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    protected void FreePublisher()
    {
        if (fPublisher is not null)
        {
            fPublisher.NotifySubscribersAndClearSubscriptions(this);
            fPublisher = null;
        }
    }

    [Delphi(PortStatus.Ported, Note = "Raises EBold once the collection is disposed, where Delphi read a freed publisher.")]
    protected TBoldPublisher Publisher => fPublisher ?? throw new EBold($"{GetType().Name}: the collection is disposed");

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetHasSubscribers() => fPublisher is not null && fPublisher.HasSubscribers;

    [Delphi(PortStatus.Ported)]
    public bool HasSubscribers => GetHasSubscribers();

    [Delphi(PortStatus.Ported)]
    public void AddSmallSubscription(TBoldSubscriber? Subscriber, TBoldSmallEventSet Events, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSmallSubscription(Subscriber, Events, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void AddSubscription(TBoldSubscriber? Subscriber, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSubscription(Subscriber, OriginalEvent, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void SendEvent(TBoldEvent OriginalEvent) => fPublisher?.SendExtendedEvent(this, OriginalEvent, []);

    [Delphi(PortStatus.Ported)]
    public void SendExtendedEvent(TBoldEvent OriginalEvent, object?[] Args) => Publisher.SendExtendedEvent(this, OriginalEvent, Args);

    [Delphi(PortStatus.Ported)]
    public bool SendQuery(TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        fPublisher is null || fPublisher.SendQuery(this, OriginalEvent, Args, Subscriber);
}

/// <summary>An item of a collection that others can subscribe to: it sends Bold events.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubscribableCollectionItem : TCollectionItem, IDisposable
{
    private TBoldPublisher? fPublisher;

    [Delphi(PortStatus.Ported)]
    public TBoldSubscribableCollectionItem(TCollection? Collection)
        : base(Collection)
    {
        fPublisher = new TBoldPublisher();
    }

    /// <summary>Tells the subscribers the item goes, stops sending events and leaves its collection.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldSubscribableCollectionItem.Destroy", Note = "Leaves the collection, as Delphi's inherited destructor did.")]
    public virtual void Dispose()
    {
        FreePublisher();
        Collection = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    protected void FreePublisher()
    {
        if (fPublisher is not null)
        {
            fPublisher.NotifySubscribersAndClearSubscriptions(this);
            fPublisher = null;
        }
    }

    [Delphi(PortStatus.Ported, Note = "Raises EBold once the item is disposed, where Delphi read a freed publisher.")]
    protected TBoldPublisher Publisher => fPublisher ?? throw new EBold($"{GetType().Name}: the item is disposed");

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetHasSubscribers() => fPublisher is not null && fPublisher.HasSubscribers;

    [Delphi(PortStatus.Ported)]
    public bool HasSubscribers => GetHasSubscribers();

    [Delphi(PortStatus.Ported)]
    public void AddSmallSubscription(TBoldSubscriber? Subscriber, TBoldSmallEventSet Events, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSmallSubscription(Subscriber, Events, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void AddSubscription(TBoldSubscriber? Subscriber, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent) =>
        Publisher.AddSubscription(Subscriber, OriginalEvent, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    public void SendEvent(TBoldEvent OriginalEvent) => fPublisher?.SendExtendedEvent(this, OriginalEvent, []);

    [Delphi(PortStatus.Ported)]
    public void SendExtendedEvent(TBoldEvent OriginalEvent, object?[] Args) => Publisher.SendExtendedEvent(this, OriginalEvent, Args);

    [Delphi(PortStatus.Ported)]
    public bool SendQuery(TBoldEvent OriginalEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        fPublisher is null || fPublisher.SendQuery(this, OriginalEvent, Args, Subscriber);
}
