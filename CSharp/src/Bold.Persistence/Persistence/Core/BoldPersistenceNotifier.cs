// Delphi: Source/Persistence/Core/BoldPersistenceNotifier.pas
// Port map: CSharp/port/units/BoldPersistenceNotifier.md

using static Bold.BoldSubscription;

namespace Bold;

/// <summary>Called when one object is fetched more than ten times in ten seconds.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldAlertExcessiveFetchEvent(TBoldObjectId ObjectId, int FetchCount);

/// <summary>
/// A component that subscribes to a persistence handle's events and calls a handler per event kind; it also watches
/// for the same object being fetched one at a time over and over.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A System.ComponentModel.Component where Delphi has a TComponent.")]
public abstract class TBoldAbstractPersistenceNotifier : System.ComponentModel.Component
{
    private const int brePersistenceHandleDestroying = 100;

    private readonly Dictionary<int, TBoldExtendedEventHandler?> fEvents = [];
    private TBoldPersistenceHandle? FPersistenceHandle;
    private TBoldSubscriber? fSubscriber;
    private TBoldIntegerArray? fFetchLog;
    private TBoldAlertExcessiveFetchEvent? fOnAlertExcessiveFetch;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    protected TBoldAbstractPersistenceNotifier(System.ComponentModel.IContainer? owner = null)
    {
        owner?.Add(this);
        fSubscriber = new TBoldPassthroughSubscriber(ReceiveExtendedEvent);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractPersistenceNotifier.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fSubscriber?.Dispose();
            fSubscriber = null;
            fFetchLog?.Dispose();
            fFetchLog = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>Subscribes to the handle's persistence events and its destruction.</summary>
    [Delphi(PortStatus.Ported, Note = "Subscribes at once; Delphi waits for Loaded when the form is streaming in.")]
    private void SetPersistenceHandle(TBoldPersistenceHandle? Value)
    {
        if (Value != FPersistenceHandle)
        {
            fSubscriber!.CancelAllSubscriptions();
            if (Value is not null)
            {
                Value.AddPersistenceSubscription(fSubscriber);
                Value.AddSmallSubscription(fSubscriber, TBoldSmallEventSet.Of(beDestroying), brePersistenceHandleDestroying);
            }

            FPersistenceHandle = Value;
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldExtendedEventHandler? GetEvent(int index) => fEvents.GetValueOrDefault(index);
    [Delphi(PortStatus.Ported)] private void SetEvent(int index, TBoldExtendedEventHandler? EventHandler) => fEvents[index] = EventHandler;

    [Delphi(PortStatus.Ported)]
    private void CallIfAssigned(int EventID, object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) =>
        GetEvent(EventID)?.Invoke(Originator, OriginalEvent, RequestedEvent, Args);

    [Delphi(PortStatus.Ported)]
    private TBoldIntegerArray GetFetchLog() => fFetchLog ??= new TBoldIntegerArray(64, TBoldContainerOptions.Of());

    [Delphi(PortStatus.Ported)] protected virtual void EndFetch(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeEndFetch, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void EndUpdate(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeEndUpdate, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void EndFetchID(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeEndFetchID, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void StartUpdate(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeStartUpdate, Originator, OriginalEvent, RequestedEvent, Args);

    /// <summary>Calls the handler, and logs a fetch of a single object.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void StartFetch(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        CallIfAssigned(bpeStartFetch, Originator, OriginalEvent, RequestedEvent, Args);
        var IdList = (TBoldObjectIdList)Args[0]!;
        if (IdList.Count == 1)
        {
            LogFetchOneObject(IdList[0]);
        }
    }

    [Delphi(PortStatus.Ported)] protected virtual void UpdateObject(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeUpdateObject, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void DeleteObject(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeDeleteObject, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void CreateObject(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeCreateObject, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void FetchObject(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeFetchObject, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void FetchMember(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeFetchMember, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void StartFetchID(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeStartFetchID, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void FetchID(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeFetchId, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void ProgressStart(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeProgressStart, Originator, OriginalEvent, RequestedEvent, Args);
    [Delphi(PortStatus.Ported)] protected virtual void ProgressEnd(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) => CallIfAssigned(bpeProgressEnd, Originator, OriginalEvent, RequestedEvent, Args);

    [Delphi(PortStatus.Replaced, Symbol = "TBoldAbstractPersistenceNotifier.Notification", Note = "No component free notifications in .NET; the beDestroying subscription clears the handle.")]
    [Delphi(PortStatus.Replaced, Symbol = "TBoldAbstractPersistenceNotifier.Loaded", Note = "No form streaming in .NET; SetPersistenceHandle subscribes at once.")]
    [Delphi(PortStatus.Ported)]
    protected virtual void ReceiveExtendedEvent(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        if (OriginalEvent == bpeStartFetch || OriginalEvent == bpeStartUpdate || OriginalEvent == bpeStartFetchID)
        {
            ProgressStart(Originator, OriginalEvent, RequestedEvent, Args);
        }

        switch (OriginalEvent)
        {
            case bpeEndFetch: EndFetch(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeEndUpdate: EndUpdate(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeEndFetchID: EndFetchID(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeStartUpdate: StartUpdate(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeStartFetch: StartFetch(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeStartFetchID: StartFetchID(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeUpdateObject: UpdateObject(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeDeleteObject: DeleteObject(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeCreateObject: CreateObject(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeFetchObject: FetchObject(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeFetchMember: FetchMember(Originator, OriginalEvent, RequestedEvent, Args); break;
            case bpeFetchId: FetchID(Originator, OriginalEvent, RequestedEvent, Args); break;
        }

        if (OriginalEvent == bpeEndFetch || OriginalEvent == bpeEndUpdate || OriginalEvent == bpeEndFetchID)
        {
            ProgressEnd(Originator, OriginalEvent, RequestedEvent, Args);
        }

        if (OriginalEvent == beDestroying && RequestedEvent == brePersistenceHandleDestroying)
        {
            PersistenceHandle = null;
            fSubscriber!.CancelAllSubscriptions();
        }
    }

    /// <summary>Logs the time of a single-object fetch, keeps the last ten seconds, and alerts at more than ten.</summary>
    [Delphi(PortStatus.Ported, Note = "Counts hours as h * 24 * 60 * 1000 ms, as Delphi does.")]
    protected void LogFetchOneObject(TBoldObjectId ObjectId)
    {
        DateTime now = SysUtils.Now();
        int MSecs = now.Millisecond + now.Second * 1000 + now.Minute * 60 * 1000 + now.Hour * 24 * 60 * 1000;
        FetchLog.Add(MSecs);
        int i = 0;
        while (i < FetchLog.Count && FetchLog[i] < MSecs - 10 * 1000)
        {
            i++;
        }

        if (i > 0)
        {
            FetchLog.DeleteRange(0, i - 1);
        }

        if (FetchLog.Count > 10)
        {
            AlertExcessiveFetch(ObjectId, FetchLog.Count);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void AlertExcessiveFetch(TBoldObjectId ObjectId, int FetchCount) => OnAlertExcessiveFetch?.Invoke(ObjectId, FetchCount);

    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnEndFetch { get => GetEvent(bpeEndFetch); set => SetEvent(bpeEndFetch, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnEndUpdate { get => GetEvent(bpeEndUpdate); set => SetEvent(bpeEndUpdate, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnEndFetchID { get => GetEvent(bpeEndFetchID); set => SetEvent(bpeEndFetchID, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnStartUpdate { get => GetEvent(bpeStartUpdate); set => SetEvent(bpeStartUpdate, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnStartFetch { get => GetEvent(bpeStartFetch); set => SetEvent(bpeStartFetch, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnUpdateObject { get => GetEvent(bpeUpdateObject); set => SetEvent(bpeUpdateObject, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnDeleteObject { get => GetEvent(bpeDeleteObject); set => SetEvent(bpeDeleteObject, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnCreateObject { get => GetEvent(bpeCreateObject); set => SetEvent(bpeCreateObject, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnFetchObject { get => GetEvent(bpeFetchObject); set => SetEvent(bpeFetchObject, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnFetchMember { get => GetEvent(bpeFetchMember); set => SetEvent(bpeFetchMember, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnStartFetchID { get => GetEvent(bpeStartFetchID); set => SetEvent(bpeStartFetchID, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnFetchID { get => GetEvent(bpeFetchId); set => SetEvent(bpeFetchId, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnProgressStart { get => GetEvent(bpeProgressStart); set => SetEvent(bpeProgressStart, value); }
    [Delphi(PortStatus.Ported)] public TBoldExtendedEventHandler? OnProgressEnd { get => GetEvent(bpeProgressEnd); set => SetEvent(bpeProgressEnd, value); }

    [Delphi(PortStatus.Ported)]
    public TBoldAlertExcessiveFetchEvent? OnAlertExcessiveFetch
    {
        get => fOnAlertExcessiveFetch;
        set => fOnAlertExcessiveFetch = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPersistenceHandle? PersistenceHandle
    {
        get => FPersistenceHandle;
        set => SetPersistenceHandle(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldIntegerArray FetchLog => GetFetchLog();
}

/// <summary>A persistence notifier with its events published (public here).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceNotifier : TBoldAbstractPersistenceNotifier
{
    public TBoldPersistenceNotifier(System.ComponentModel.IContainer? owner = null)
        : base(owner)
    {
    }
}
