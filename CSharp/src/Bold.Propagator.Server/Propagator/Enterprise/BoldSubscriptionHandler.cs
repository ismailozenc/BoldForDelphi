// Delphi: Source/Propagator/Enterprise/BoldSubscriptionHandler.pas
// Port map: CSharp/port/units/BoldSubscriptionHandler.md

using static Bold.BoldPropagatorConstants;
using static Bold.PropagatorConsts;

namespace Bold;

[Delphi(PortStatus.Ported)]
public delegate void TClientInfoChangedEvent(TBoldClientID ClientId, int EventsCount, int QueuesCount);

[Delphi(PortStatus.Ported)]
public delegate void TGlobalInfoChangedEvent(int InQCount, int AddedEventsCount, int SentEventsCount);

/// <summary>
/// The thread that empties the propagator's in-queue: each external event goes to the subscription handler, which
/// keeps the subscriptions and queues events for the clients.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldDequeuer : TBoldNotifiableThread
{
    private TBoldSubscriptionHandler? fSubscriptionHandler;
    private readonly TBoldThreadSafeObjectQueue fInQueue;
    private TBoldAbstractOutputQueueHandler? fOutQueueHandler;
    private int fInQueueCountLast;

    [Delphi(PortStatus.Ported)]
    public TBoldDequeuer(TBoldThreadSafeObjectQueue InQueue)
        : base(true)
    {
        fInQueue = InQueue;
        InQueue.OnQueueNotEmpty = NotifyQueueNotEmpty;
        fSubscriptionHandler = new TBoldSubscriptionHandler { OutQueueHandler = OutQueuehandler };
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldDequeuer.Destroy")]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fSubscriptionHandler);
        (fOutQueueHandler as IDisposable)?.Dispose();
        fOutQueueHandler = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private void NotifyQueueNotEmpty(TBoldThreadSafeQueue Queue)
    {
        try
        {
            Notify(BM_QUEUE_NOT_EMPTY);
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "NotifyQueueNotEmpty", E.Message);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void DoStartDequeue() => OnStartDequeue?.Invoke(this);

    [Delphi(PortStatus.Ported)]
    private void DoDoneDequeue() => OnDoneDequeue?.Invoke(this);

    /// <summary>Processes the in-queue's events, fifty at a time, until it is empty.</summary>
    [Delphi(PortStatus.Ported)]
    private void Dequeue()
    {
        var Events = new List<object?>();
        try
        {
            fInQueueCountLast = fInQueue.Count;
            while (!fInQueue.Empty)
            {
                fInQueue.DequeueList(Events, 50);
                foreach (object? e in Events)
                {
                    fSubscriptionHandler!.ProcessExternalEvent((TBoldExternalEvent)e!);
                }

                Events.Clear();
            }
        }
        catch (OutOfMemoryException E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "Dequeue", E.Message);
        }
    }

    [Delphi(PortStatus.Ported)]
    private int GetInQueueCount() => fInQueue.Count;

    [Delphi(PortStatus.Ported)]
    private int GetInQueueCountPeak() => fInQueue.MaxCount;

    /// <summary>The output queue handler, created the first time it is asked for (virtual so tests can give another).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldAbstractOutputQueueHandler GetOutQueueHandler() => fOutQueueHandler ??= new TBoldOutputQueueHandler();

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractOutputQueueHandler OutQueuehandler => GetOutQueueHandler();

    /// <summary>The message loop: dequeues when the in-queue has events, and answers the requests for statistics.</summary>
    [Delphi(PortStatus.Ported, Note = "Logs a failure to handle a message; Delphi beeped.")]
    protected override void Execute()
    {
        EnsureMessageQueue();
        SignalReady();
        BoldThreadSafeLog.BoldLogThread("ID=Dequeuer");
        while (!Terminated)
        {
            int res = GetMessage(out TMsg rMsg);
            try
            {
                if (res == -1 || res == 0)
                {
                    Terminate();
                }
                else if (rMsg.message == BM_QUEUE_NOT_EMPTY)
                {
                    DoStartDequeue();
                    Dequeue();
                    DoDoneDequeue();
                }
                else if (rMsg.message == BM_REQUEST_GLOBAL_INFO)
                {
                    OnGlobalInfoChanged?.Invoke(fInQueue.Count, fSubscriptionHandler!.AddedSubscriptionsCount, fSubscriptionHandler.SentEventsCount);
                }
                else if (rMsg.message == BM_REQUEST_CLIENT_INFO)
                {
                    if (OnClientInfoChanged is not null)
                    {
                        var ClientId = (TBoldClientID)rMsg.lParam;
                        int ClientSubscriptions = rMsg.wParam != 0 ? fSubscriptionHandler!.GetEventCountByClientID(ClientId) : -1;
                        OnClientInfoChanged(ClientId, ClientSubscriptions, fSubscriptionHandler!.QueueCountForClient(ClientId));
                    }
                }
                else
                {
                    Windows.DispatchMessage(ref rMsg);
                }
            }
            catch (Exception E)
            {
                BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "Execute", E.Message);
            }
        }
    }

    /// <summary>Asks for the client's queue and subscriptions to be dropped, after the events queued before.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnqueueRemoveClientQueueEvent(TBoldClientID ClientID) =>
        fInQueue.Enqueue(new TBoldExternalEvent(ClientID, TBoldExternalEventType.bemRemoveClientQueue, ""));

    [Delphi(PortStatus.Ported)] public TNotifyEvent? OnStartDequeue { get; set; }
    [Delphi(PortStatus.Ported)] public TNotifyEvent? OnDoneDequeue { get; set; }
    [Delphi(PortStatus.Ported)] public TClientInfoChangedEvent? OnClientInfoChanged { get; set; }
    [Delphi(PortStatus.Ported)] public TGlobalInfoChangedEvent? OnGlobalInfoChanged { get; set; }
    [Delphi(PortStatus.Ported)] public int InQueueCount => GetInQueueCount();
    [Delphi(PortStatus.Ported)] public int InQueueCountPeak => GetInQueueCountPeak();
    [Delphi(PortStatus.Ported)] public int InQueueCountLast => fInQueueCountLast;
}

/// <summary>
/// The propagator's subscriptions. An event goes to every other client subscribed to it (the part of the event name
/// before PROPAGATOR_PARAMETER_DELIMITER_CHAR); then those subscriptions end and the sender is subscribed instead.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubscriptionHandler : IDisposable
{
    private TBoldSubscriptionList? fSubscriptions;
    private TBoldAbstractOutputQueueHandler? fOutQueueHandler;
    private int fSentEventsCount;
    private int fAddedSubscriptionsCount;

    [Delphi(PortStatus.Ported)]
    public TBoldSubscriptionHandler()
    {
        fSubscriptions = new TBoldSubscriptionList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldSubscriptionHandler.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fSubscriptions);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractOutputQueueHandler getOutQueueHandler() =>
        fOutQueueHandler ?? throw new EBold(SysUtils.Format(sOutputQueueHandlerNotAssigned, GetType().Name));

    [Delphi(PortStatus.Ported)]
    private void AddSubscription(TBoldClientID BoldClientId, string EventName)
    {
        fSubscriptions!.AddSubscription(BoldClientId, EventName);
        fAddedSubscriptionsCount++;
    }

    [Delphi(PortStatus.Ported)]
    private void RemoveSubscription(string EventName) => fSubscriptions!.RemoveEvent(EventName);

    [Delphi(PortStatus.Ported)]
    private void SendEvent(string EventName, TList? ClientIDs)
    {
        if (ClientIDs is not null)
        {
            for (int i = 0; i < ClientIDs.Count; i++)
            {
                OutQueueHandler.SendEvent((TBoldClientID)ClientIDs[i]!, EventName);
            }

            fSentEventsCount += ClientIDs.Count;
        }
    }

    [Delphi(PortStatus.Ported)]
    private int GetAddedSubscriptionsCount() => fAddedSubscriptionsCount;

    [Delphi(PortStatus.Ported)]
    private int GetSentEventsCount() => fSentEventsCount;

    [Delphi(PortStatus.Ported)]
    protected internal TBoldAbstractOutputQueueHandler OutQueueHandler
    {
        get => getOutQueueHandler();
        set => fOutQueueHandler = value;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void GetClientsSubscribedToEvent(string EventName, TList ClientIds) =>
        fSubscriptions!.GetClientsSubscribedToEvent(EventName, ClientIds);

    [Delphi(PortStatus.Ported)]
    protected internal void GetEventsByClientID(TBoldClientID ClientID, TStringList events) => fSubscriptions!.GetEventsByClientID(ClientID, events);

    [Delphi(PortStatus.Ported)]
    protected void RemoveQueueForClient(TBoldClientID ClientId)
    {
        OutQueueHandler.ClearQueueForClient(ClientId);
        fSubscriptions!.RemoveClient(ClientId);
    }

    /// <summary>Handles an event, a subscription, a cancellation, a queue removal or a lock event from the in-queue.</summary>
    [Delphi(PortStatus.Ported, Note = "Cuts the parameters off only when the event has them; Delphi's check (pos <> -1) was always true, and harmless.")]
    public void ProcessExternalEvent(TBoldExternalEvent BoldExternalEvent)
    {
        try
        {
            string EventNameWithoutParameters = BoldExternalEvent.EventName;
            int p = EventNameWithoutParameters.IndexOf(BoldDefs.PROPAGATOR_PARAMETER_DELIMITER_CHAR);
            if (p >= 0)
            {
                EventNameWithoutParameters = EventNameWithoutParameters[..p];
            }

            switch (BoldExternalEvent.EventType)
            {
                case TBoldExternalEventType.bemEvent:
                    // The event goes to all subscribed clients except the sender...
                    var Clients = new TBoldSortedIntegerList();
                    fSubscriptions!.GetAllSubscribedClientsExcept(EventNameWithoutParameters, BoldExternalEvent.BoldClientID, Clients);
                    SendEvent(BoldExternalEvent.EventName, Clients);
                    // ...whose subscriptions end; the sender is subscribed.
                    fSubscriptions.RemoveEvent(EventNameWithoutParameters);
                    fSubscriptions.AddSubscription(BoldExternalEvent.BoldClientID, EventNameWithoutParameters);
                    break;
                case TBoldExternalEventType.bemSubscription:
                    AddSubscription(BoldExternalEvent.BoldClientID, EventNameWithoutParameters);
                    break;
                case TBoldExternalEventType.bemCancelSubscription:
                    RemoveSubscription(EventNameWithoutParameters);
                    break;
                case TBoldExternalEventType.bemRemoveClientQueue:
                    RemoveQueueForClient(BoldExternalEvent.BoldClientID);
                    break;
                case TBoldExternalEventType.bemGetLock:
                    OutQueueHandler.SendEventAndFlushQueue(BoldExternalEvent.BoldClientID, BoldExternalEvent.EventName);
                    break;
                case TBoldExternalEventType.bemLockLost:
                    var LockClients = new TBoldSortedIntegerList();
                    LockClients.Add(BoldExternalEvent.BoldClientID);
                    SendEvent(BoldExternalEvent.EventName, LockClients);
                    break;
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "ProcessExternalEvent", E.Message);
        }
    }

    /// <summary>Drops the client's subscriptions and queued events.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveClient(TBoldClientID BoldClientID)
    {
        fSubscriptions!.RemoveClient(BoldClientID);
        OutQueueHandler.ClearQueueForClient(BoldClientID);
    }

    [Delphi(PortStatus.Ported)]
    public int GetEventCountByClientID(TBoldClientID ClientId) => fSubscriptions!.GetEventCountByClientID(ClientId);

    [Delphi(PortStatus.Ported)]
    public int QueueCountForClient(TBoldClientID ClientId) => OutQueueHandler.QueueCountForClient(ClientId);

    [Delphi(PortStatus.Ported)] public int AddedSubscriptionsCount => GetAddedSubscriptionsCount();
    [Delphi(PortStatus.Ported)] public int SentEventsCount => GetSentEventsCount();
}
