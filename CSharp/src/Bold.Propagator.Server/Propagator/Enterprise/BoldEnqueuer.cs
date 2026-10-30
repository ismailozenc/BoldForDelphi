// Delphi: Source/Propagator/Enterprise/BoldEnqueuer.pas
// Port map: CSharp/port/units/BoldEnqueuer.md

using static Bold.BoldPropagatorConstants;

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldExternalEventType { bemEvent, bemSubscription, bemCancelSubscription, bemRemoveClientQueue, bemGetLock, bemLockLost }

/// <summary>An event, subscription or request from a client, waiting in the propagator's in-queue.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalEvent
{
    [Delphi(PortStatus.Ported)]
    public TBoldExternalEvent(TBoldClientID BoldClientID, TBoldExternalEventType EventType, string EventName)
    {
        this.BoldClientID = BoldClientID;
        this.EventType = EventType;
        this.EventName = EventName;
    }

    [Delphi(PortStatus.Ported)] public TBoldExternalEventType EventType { get; set; }
    [Delphi(PortStatus.Ported)] public string EventName { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldClientID BoldClientID { get; set; }
}

/// <summary>The in-queue: it can take a whole list of a client's events at once.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalEventThreadSafeObjectQueue(string Name, bool OwnsObjects = true) : TBoldThreadSafeObjectQueue(Name, OwnsObjects)
{
    /// <summary>Queues an external event for each string, in one go.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes a string array where Delphi took a variant array.")]
    public void EnqueueVarArray(TBoldClientID ClientId, TBoldExternalEventType EventType, string[] Events)
    {
        Lock();
        try
        {
            foreach (string e in Events)
            {
                UnsafeEnqueue(new TBoldExternalEvent(ClientId, EventType, e));
            }
        }
        finally
        {
            Unlock();
        }
    }
}

/// <summary>
/// Takes the clients' events, subscriptions and cancellations (the propagator's IBoldEventPropagator) and the lock
/// manager's lock events, and puts them in the in-queue for the dequeuer. Only registered clients are heard.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldEnqueuer : IDisposable
{
    private TBoldExternalEventThreadSafeObjectQueue? FInQueue;
    private TBoldClientHandler FClientHandler;
    private bool fEnabled;
    private TBoldLoggableCriticalSection? fLock;

    [Delphi(PortStatus.Ported)]
    public TBoldEnqueuer(TBoldClientHandler ClientHandler)
    {
        FClientHandler = ClientHandler;
        fLock = new TBoldLoggableCriticalSection("EQ");
        fLock.Acquire();
        try
        {
            fEnabled = false;
            FInQueue = new TBoldExternalEventThreadSafeObjectQueue("InQ");
            fEnabled = true;
        }
        finally
        {
            fLock.Release();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldEnqueuer.Destroy")]
    public void Dispose()
    {
        FInQueue?.Clear();
        SysUtils.FreeAndNil(ref FInQueue);
        fLock = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public int SendEvents(int BoldClientID, string[]? Events) => Enqueue(BoldClientID, Events, TBoldExternalEventType.bemEvent, "SendEvents");

    [Delphi(PortStatus.Ported)]
    public int AddSubscriptions(int BoldClientID, string[]? Subscriptions) =>
        Enqueue(BoldClientID, Subscriptions, TBoldExternalEventType.bemSubscription, "AddSubscriptions");

    [Delphi(PortStatus.Ported)]
    public int CancelSubscriptions(int BoldClientID, string[]? Subscriptions) =>
        Enqueue(BoldClientID, Subscriptions, TBoldExternalEventType.bemCancelSubscription, "CancelSubscriptions");

    /// <summary>The body SendEvents, AddSubscriptions and CancelSubscriptions share.</summary>
    private int Enqueue(int BoldClientID, string[]? Events, TBoldExternalEventType EventType, string Method)
    {
        TBoldLoggableCriticalSection lockSection = fLock ?? throw new ObjectDisposedException(GetType().Name);
        lockSection.Acquire();
        try
        {
            int result = CheckStatus(BoldClientID, Events);
            try
            {
                if (result == Windows.S_OK || result == W_CLIENT_NOT_RECEIVING)
                {
                    FInQueue!.EnqueueVarArray(BoldClientID, EventType, Events!);
                }
            }
            catch (Exception E)
            {
                result = Windows.E_FAIL;
                BoldThreadSafeLog.BoldLogError("%s.%s ClientId=%d: %s", GetType().Name, Method, BoldClientID, E.Message);
            }

            return result;
        }
        finally
        {
            lockSection.Release();
        }
    }

    /// <summary>
    /// Queues a lock event for a registered client: got locks (sent to the client at once, after what is queued for it)
    /// or lock lost. True when queued.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public bool SendLockEvent(int BoldClientID, string Event, bool AcquiredLock)
    {
        TBoldLoggableCriticalSection lockSection = fLock ?? throw new ObjectDisposedException(GetType().Name);
        lockSection.Acquire();
        try
        {
            bool result = false;
            if (fEnabled && ClientHandler.IsRegistered(BoldClientID))
            {
                try
                {
                    FInQueue!.Enqueue(new TBoldExternalEvent(BoldClientID,
                        AcquiredLock ? TBoldExternalEventType.bemGetLock : TBoldExternalEventType.bemLockLost, Event));
                    result = true;
                }
                catch (Exception E)
                {
                    BoldThreadSafeLog.BoldLogError("%s.SendLockEvent ClientId=%d: %s", GetType().Name, BoldClientID, E.Message);
                }
            }

            return result;
        }
        finally
        {
            lockSection.Release();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldThreadSafeObjectQueue getInQueue() => FInQueue!;

    /// <summary>Disabling clears the in-queue.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetEnabled(bool Value)
    {
        fLock!.Acquire();
        try
        {
            if (Value != fEnabled)
            {
                fEnabled = Value;
                if (!fEnabled)
                {
                    FInQueue?.Clear();
                }
            }
        }
        finally
        {
            fLock.Release();
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetEnabled()
    {
        fLock!.Acquire();
        try
        {
            return fEnabled;
        }
        finally
        {
            fLock.Release();
        }
    }

    /// <summary>
    /// S_OK for a registered client and a list; E_ENQUEUER_NOT_ENABLED, E_CLIENT_NOT_REGISTERED or E_INVALID_PARAMETER
    /// otherwise. (A client that is not receiving gets S_OK too: Delphi decided not to tell it.)
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private int CheckStatus(TBoldClientID ClientId, string[]? VarArray)
    {
        if (!fEnabled)
        {
            return E_ENQUEUER_NOT_ENABLED;
        }

        if (!ClientHandler.IsRegistered(ClientId))
        {
            return E_CLIENT_NOT_REGISTERED;
        }

        if (VarArray is null)
        {
            return E_INVALID_PARAMETER;
        }

        ClientHandler.HasInfoForClient(ClientId, out string _, out TTimeStamp _, out bool _, out TBoldClientReceiveStatus _);
        return Windows.S_OK;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldThreadSafeObjectQueue InQueue => getInQueue();

    [Delphi(PortStatus.Ported)]
    public TBoldClientHandler ClientHandler
    {
        get => FClientHandler;
        set => FClientHandler = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool Enabled
    {
        get => GetEnabled();
        set => SetEnabled(value);
    }
}
