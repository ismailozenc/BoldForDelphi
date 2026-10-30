// Delphi: Source/Persistence/Propagation/BoldListenerCOM.pas
// Port map: CSharp/port/units/BoldListenerCOM.md
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldListenerCOM", Symbol = "TBoldListenerCOMFactory", Note = "COM class factory; the listener thread creates its listener.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldListenerCOM", Symbol = "TBoldListenerCOMFactory.Create", Note = "COM class factory.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public class EBoldPropagatorTypeLib(string? message = null, Exception? innerException = null) : EBold(message, innerException);

/// <summary>How extending a lease went.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldExtendLeaseResult { elrExtended, elrFailed, elrFailedExpired, elrDenied, elrNotRegistered }

[Delphi(PortStatus.Ported)]
public delegate void TBoldExtendLeaseFailureEvent(TBoldExtendLeaseResult res, string Msg);

/// <summary>Sees a message before it is queued; true when it handled the message (which is then not queued).</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldMessageEvent(string aMessage);

/// <summary>
/// The listener the propagator calls back: it registers the client with the propagator, extends the lease on a
/// timer, and queues the events it receives (after <see cref="OnMessage"/> has seen them) for the dequeuer.
/// </summary>
/// <remarks>
/// It lives on the listener thread, whose message loop runs its timer. The propagator calls
/// <see cref="ReceiveEvents"/> and <see cref="DisconnectClient"/> on its own threads.
/// </remarks>
[Delphi(PortStatus.Ported, Note = "A plain C# object implementing IBoldListener and IBoldListenerAdmin, where Delphi had a COM object.")]
public class TBoldListenerCOM : IBoldListener, IBoldListenerAdmin, IDisposable
{
    private volatile int fID;
    private readonly TBoldThreadSafeStringQueue fQueue;
    private TNotifyEvent? fQueueNotEmptyNotifyEvent;
    private IBoldClientHandler? fPropagator;
    private int fLeaseDuration;
    private int fPollingInterval;
    private bool fAutoExtendLease;
    private string fClientIdentifierString;
    private TThreadTimer? fTimer;
    private readonly TThread fTimerThread;
    private volatile bool fRegistered;
    private TBoldMessageEvent? fOnMessage;
    private TCriticalSection? fLocker;
    private TBoldExtendLeaseFailureEvent? FOnExtendLeaseFailed;
    private int fExtendLeaseAfter;
    private DateTime fLastSuccessFulExtend;
    private string fExtendError = "";
    private DateTime fConnectAllowed;

    /// <summary>A listener that queues in <paramref name="aQueue"/>; its lease timer runs on <paramref name="TimerThread"/> (the listener thread).</summary>
    [Delphi(PortStatus.Ported, Note = "Also takes the thread whose message loop runs the lease timer.")]
    public TBoldListenerCOM(TBoldThreadSafeStringQueue aQueue, int PollingInterval, int LeaseDuration, int ExtendLeaseAfter, bool AutoExtendLease,
        string ClientIdentifierString, TThread TimerThread)
    {
        fID = BoldDefs.NOTVALIDCLIENTID;
        fQueue = aQueue ?? throw new ArgumentNullException(nameof(aQueue), "COMListeners queue not assigned!");
        fTimerThread = TimerThread;
        fLocker = new TCriticalSection();
        fPollingInterval = PollingInterval;
        fLeaseDuration = LeaseDuration;
        fExtendLeaseAfter = ExtendLeaseAfter;
        // The lease duration and ExtendLeaseAfter come first, for the timer's interval.
        this.AutoExtendLease = AutoExtendLease;
        fClientIdentifierString = ClientIdentifierString;
        Registered = false;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldListenerCOM.Destroy")]
    public void Dispose()
    {
        fPropagator = null;
        SysUtils.FreeAndNil(ref fTimer);
        fLocker = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>Queues a message unless <see cref="OnMessage"/> handles it.</summary>
    [Delphi(PortStatus.Ported)]
    private void PushMessage(string aMessage)
    {
        bool Handled = fOnMessage is { } onMessage && onMessage(aMessage);
        if (!Handled)
        {
            Queue.Enqueue(aMessage);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void DecodeAndPush(string[]? Incoming)
    {
        if (Incoming is not null)
        {
            foreach (string message in Incoming)
            {
                PushMessage(message);
            }
        }

        fQueueNotEmptyNotifyEvent?.Invoke(this);
    }

    [Delphi(PortStatus.Ported)]
    private void setAutoExtendLease(bool Value)
    {
        if (fTimer is null)
        {
            fTimer = new TThreadTimer(fTimerThread) { Enabled = false };
            fTimer.OnTimer = OnTimer;
        }

        fAutoExtendLease = Value;
        if (fAutoExtendLease)
        {
            fTimer.Enabled = false;
            fTimer.Interval = DefaultExtendLeaseInterval();
            fTimer.Enabled = true;
        }
        else
        {
            fTimer.Enabled = false;
        }
    }

    /// <summary>
    /// Extends the lease when registered. After a failure it tries again in a fifth of the time left (at most half the
    /// usual interval, at least a second).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void OnTimer(object? Sender)
    {
        fTimer!.Enabled = false;
        if (Registered)
        {
            switch (ExtendLease())
            {
                case TBoldExtendLeaseResult.elrExtended:
                    fTimer.Interval = DefaultExtendLeaseInterval();
                    break;
                case TBoldExtendLeaseResult.elrFailed:
                    int interval = (int)Math.Round(LeaseTimeLeft() * 0.2);
                    interval = Math.Min(interval, DefaultExtendLeaseInterval() / 2);
                    fTimer.Interval = Math.Max(interval, 1000);
                    break;
                case TBoldExtendLeaseResult.elrFailedExpired:
                case TBoldExtendLeaseResult.elrDenied:
                    fTimer.Interval = DefaultExtendLeaseInterval();
                    break;
            }
        }

        fTimer.Enabled = true;
    }

    /// <summary>Takes the propagator's events; S_OK, or S_FALSE when they could not be taken.</summary>
    [Delphi(PortStatus.Ported)]
    public int ReceiveEvents(string[]? Events)
    {
        try
        {
            DecodeAndPush(Events);
            return Windows.S_OK;
        }
        catch (Exception)
        {
            return Windows.S_FALSE;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetLeaseDuration(int Value)
    {
        if (Value != fLeaseDuration)
        {
            Lock();
            fLeaseDuration = Value;
            UnLock();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetExtendLeaseAfter(int Value)
    {
        if (Value != fExtendLeaseAfter)
        {
            Lock();
            fExtendLeaseAfter = Value;
            UnLock();
        }
    }

    /// <summary>The usual time between lease extensions: ExtendLeaseAfter percent of the lease.</summary>
    [Delphi(PortStatus.Ported)]
    private int DefaultExtendLeaseInterval() => (int)Math.Round(fLeaseDuration * (fExtendLeaseAfter / 100.0));

    /// <summary>The milliseconds left of the lease since the last extension (or the registration).</summary>
    [Delphi(PortStatus.Ported, Note = "Counts from the registration until the first extension, and counts whole days; Delphi took the time of day of the difference.")]
    private int LeaseTimeLeft() => (int)Math.Max(int.MinValue, LeaseDuration - (DateTime.Now - fLastSuccessFulExtend).TotalMilliseconds);

    [Delphi(PortStatus.Ported)]
    public int Ping() => 1;

    /// <summary>
    /// The propagator disconnects the client: a "DISCONNECT:ms:message" message is queued for the event handler, and the
    /// listener does not register again for <paramref name="RemainDisconnected"/> milliseconds.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void DisconnectClient(string aMessage, int RemainDisconnected)
    {
        fConnectAllowed = DateTime.Now.AddMilliseconds(RemainDisconnected);
        Queue.Enqueue(SysUtils.Format("DISCONNECT:%d:%s", RemainDisconnected, aMessage));
        fQueueNotEmptyNotifyEvent?.Invoke(this);
    }

    [Delphi(PortStatus.Ported)]
    protected void Lock() => fLocker!.Acquire();

    [Delphi(PortStatus.Ported)]
    protected void UnLock() => fLocker!.Release();

    [Delphi(PortStatus.Ported)]
    protected void DoExtendLeaseFailed(TBoldExtendLeaseResult res) => FOnExtendLeaseFailed?.Invoke(res, fExtendError);

    /// <summary>Registering failed: the listener is not registered.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the exception; Delphi took the COM error.")]
    protected void DoRegisterWithPropagatorFailed(Exception E)
    {
        fID = BoldDefs.NOTVALIDCLIENTID;
        Registered = false;
    }

    /// <summary>Adds the queued messages to the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void DebugGetQueueContents(TStringList aList) => Queue.AppendToStringList(aList);

    /// <summary>
    /// Extends the lease. A denied extension, or a failed one after the lease ran out, leaves the listener unregistered.
    /// Failures are reported through <see cref="OnExtendLeaseFailed"/>.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Gives elrNotRegistered when not registered; Delphi read an unset result.")]
    public TBoldExtendLeaseResult ExtendLease()
    {
        TBoldExtendLeaseResult result;
        fExtendError = "";
        if (!Registered || fPropagator is null)
        {
            return TBoldExtendLeaseResult.elrNotRegistered;
        }

        try
        {
            fPropagator.ExtendLease(fID, LeaseDuration, out bool Extended);
            if (Extended)
            {
                result = TBoldExtendLeaseResult.elrExtended;
                fLastSuccessFulExtend = DateTime.Now;
            }
            else
            {
                result = TBoldExtendLeaseResult.elrDenied;
            }
        }
        catch (Exception E)
        {
            fExtendError = E.Message;
            result = LeaseTimeLeft() > 0 ? TBoldExtendLeaseResult.elrFailed : TBoldExtendLeaseResult.elrFailedExpired;
        }

        if (result is TBoldExtendLeaseResult.elrDenied or TBoldExtendLeaseResult.elrFailedExpired)
        {
            Registered = false;
            fID = BoldDefs.NOTVALIDCLIENTID;
        }

        if (result != TBoldExtendLeaseResult.elrExtended)
        {
            DoExtendLeaseFailed(result);
        }

        return result;
    }

    /// <summary>Registers with the propagator, unless registered or told to stay disconnected for a while.</summary>
    [Delphi(PortStatus.Ported)]
    public void RegisterWithPropagator()
    {
        if (fPropagator is not null)
        {
            if (!Registered)
            {
                try
                {
                    if (fConnectAllowed == default || DateTime.Now > fConnectAllowed)
                    {
                        int res = fPropagator.RegisterClient(LeaseDuration, PollingInterval, this, ClientIdentifierString, out int id);
                        fID = id;
                        Registered = res == Windows.S_OK;
                        if (!Registered)
                        {
                            throw new EBoldPropagatorTypeLib(SysUtils.Format("RegisterClient failed (%d)", res));
                        }

                        fLastSuccessFulExtend = DateTime.Now;
                    }
                }
                catch (Exception E)
                {
                    DoRegisterWithPropagatorFailed(E);
                }
            }
        }
        else
        {
            UnregisterWithPropagator();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void UnregisterWithPropagator()
    {
        try
        {
            if (Registered && fPropagator is not null)
            {
                try
                {
                    fPropagator.UnRegisterClient(fID);
                }
                catch (Exception)
                {
                    // The propagator is gone; the client is unregistered here anyway.
                }
            }
        }
        finally
        {
            Registered = false;
            fID = BoldDefs.NOTVALIDCLIENTID;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldThreadSafeStringQueue Queue => fQueue;

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? QueueNotEmptyNotifyEvent
    {
        get => fQueueNotEmptyNotifyEvent;
        set => fQueueNotEmptyNotifyEvent = value;
    }

    [Delphi(PortStatus.Ported)]
    public int LeaseDuration
    {
        get => fLeaseDuration;
        set => SetLeaseDuration(value);
    }

    [Delphi(PortStatus.Ported)]
    public int ExtendLeaseAfter
    {
        get => fExtendLeaseAfter;
        set => SetExtendLeaseAfter(value);
    }

    [Delphi(PortStatus.Ported)]
    public int PollingInterval
    {
        get => fPollingInterval;
        set => fPollingInterval = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool AutoExtendLease
    {
        get => fAutoExtendLease;
        set => setAutoExtendLease(value);
    }

    [Delphi(PortStatus.Ported)]
    public string ClientIdentifierString
    {
        get => fClientIdentifierString;
        set => fClientIdentifierString = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool Registered
    {
        get => fRegistered;
        set => fRegistered = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldClientID ClientID => fID;
    [Delphi(PortStatus.Ported)] public string ExtendErrorMessage => fExtendError;

    [Delphi(PortStatus.Ported)]
    public TBoldMessageEvent? OnMessage
    {
        get => fOnMessage;
        set => fOnMessage = value;
    }

    [Delphi(PortStatus.Ported)]
    public IBoldClientHandler? Propagator
    {
        get => fPropagator;
        set => fPropagator = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldExtendLeaseFailureEvent? OnExtendLeaseFailed
    {
        get => FOnExtendLeaseFailed;
        set => FOnExtendLeaseFailed = value;
    }
}
