// Delphi: Source/Persistence/Propagation/BoldListenerThread.pas
// Port map: CSharp/port/units/BoldListenerThread.md

using static Bold.BoldPropagatorConstants;

namespace Bold;

/// <summary>
/// The thread of a client's listener: it creates the listener (at its first message), and registers, unregisters and
/// extends the lease with the propagator when asked. Others wait for the outcome on events.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldListenerThread : TThread
{
    private const string GenericSysErrorMessage = "%s.%s: %s";

    private readonly TEvent fDoneRegister = new(true, false);
    private readonly TEvent fDoneUnregister = new(true, false);
    private readonly TEvent fDoneInit = new(true, false);
    private readonly TEvent fDoneUnmarshalInterface = new(true, false);
    private readonly TEvent fDoneExtendLease = new(true, false);
    private readonly TEvent fExtendLeaseSucceeded = new(true, false);
    private volatile TBoldListenerCOM? fBoldListenerCOM;
    private TBoldThreadSafeStringQueue? fQueue;
    private int fLeaseDuration;
    private int fPollingInterval;
    private bool fAutoExtendLease;
    private string fClientIdentifierString = "";
    private TBoldMessageEvent? fOnMessage;
    private TBoldObjectMarshaler? FInterfaceStream;
    private string FErrorMessage = "";
    private TBoldExtendLeaseResult fExtendResult;
    private int fExtendLeaseAfter;

    [Delphi(PortStatus.Ported)]
    public TBoldListenerThread(bool CreateSuspended)
        : base(true)
    {
        FreeOnTerminate = false;
        fLeaseDuration = DEFAULT_LEASE_DURATION;
        fPollingInterval = DEFAULT_POLLING_INTERVAL;
        fExtendLeaseAfter = DEFAULT_EXTEND_LEASE_AFTER;
        Suspended = CreateSuspended;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldListenerThread.Destroy")]
    public override void Dispose()
    {
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private TBoldThreadSafeStringQueue GetQueue() => LazyInitializer.EnsureInitialized(ref fQueue, () => new TBoldThreadSafeStringQueue("Listener InQueue"));

    [Delphi(PortStatus.Ported)]
    private void setLeaseDuration(int Value)
    {
        fLeaseDuration = Value;
        if (fBoldListenerCOM is { } listener)
        {
            listener.LeaseDuration = fLeaseDuration;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void setPollingInterval(int Value)
    {
        fPollingInterval = Value;
        if (fBoldListenerCOM is { } listener)
        {
            listener.PollingInterval = fPollingInterval;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void setAutoExtendLease(bool Value)
    {
        fAutoExtendLease = Value;
        if (fBoldListenerCOM is { } listener)
        {
            listener.AutoExtendLease = fAutoExtendLease;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void RegisterWithPropagator()
    {
        fBoldListenerCOM?.RegisterWithPropagator();
        fDoneRegister.SetEvent();
    }

    [Delphi(PortStatus.Ported)]
    private void UnRegisterWithPropagator()
    {
        fBoldListenerCOM?.UnregisterWithPropagator();
        fDoneUnregister.SetEvent();
    }

    [Delphi(PortStatus.Ported)]
    private void SignalInitialized() => fDoneInit.SetEvent();

    [Delphi(PortStatus.Replaced, Note = "A TThread has its message queue from the start.")]
    private void EnsureMessageQueue()
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldClientID getBoldClientID() => fBoldListenerCOM?.ClientID ?? BoldDefs.NOTVALIDCLIENTID;

    [Delphi(PortStatus.Ported)]
    private void setClientIdentifierString(string Value)
    {
        fClientIdentifierString = Value;
        if (fBoldListenerCOM is { } listener)
        {
            listener.ClientIdentifierString = fClientIdentifierString;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetOnMessage(TBoldMessageEvent? Value)
    {
        fOnMessage = Value;
        if (fBoldListenerCOM is { } listener)
        {
            listener.OnMessage = Value;
        }
    }

    [Delphi(PortStatus.Ported)]
    private IBoldClientHandler? GetPropagator() => fBoldListenerCOM?.Propagator;

    /// <summary>Hands the propagator to the listener on its thread and waits until it has it; null takes it away.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetPropagator(IBoldClientHandler? Value)
    {
        if (Value is not null)
        {
            FInterfaceStream = new TBoldObjectMarshaler(typeof(IBoldClientHandler), Value);
            if (!PostThreadMessage(BM_UNMARSHAL_INTERFACE, 0, 0))
            {
                RaiseSysErrorMessage("SetPropagator");
            }

            WaitUntilInterfaceUnmarshaled();
        }
        else if (fBoldListenerCOM is { } listener)
        {
            listener.Propagator = null;
        }
    }

    [Delphi(PortStatus.Ported, Note = "Does not report twice: the listener reports a failed extension itself.")]
    private void InternalExtendLease()
    {
        TBoldExtendLeaseResult res = TBoldExtendLeaseResult.elrFailed;
        TBoldListenerCOM? listener = fBoldListenerCOM;
        try
        {
            if (listener is not null && Registered)
            {
                res = listener.ExtendLease();
            }
            else
            {
                DoExtendLeaseFailedSynchronized(res, listener?.ExtendErrorMessage ?? "");
            }
        }
        catch (Exception E)
        {
            FErrorMessage = E.Message;
            DoPropagatorFailureSynchronized();
        }

        if (res == TBoldExtendLeaseResult.elrExtended)
        {
            fExtendLeaseSucceeded.SetEvent();
        }

        fDoneExtendLease.SetEvent();
    }

    [Delphi(PortStatus.Ported)]
    private void DoPropagatorFailure() => OnPropagatorFailure?.Invoke(this, FErrorMessage);

    /// <summary>A denied extension unregisters the listener; the failure is reported.</summary>
    [Delphi(PortStatus.Ported)]
    private void DoExtendLeaseFailed()
    {
        if (fExtendResult == TBoldExtendLeaseResult.elrDenied)
        {
            UnRegister();
        }

        OnExtendLeaseFailed?.Invoke(fExtendResult, FErrorMessage);
    }

    [Delphi(PortStatus.Ported)]
    private bool GetRegistered() => fBoldListenerCOM is { Registered: true };

    [Delphi(PortStatus.Ported)]
    private bool GetInitialized() => fDoneInit.WaitFor(0) == TWaitResult.wrSignaled;

    [Delphi(PortStatus.Ported)]
    private void DoThreadErrorSynchronized(string msg)
    {
        FErrorMessage = msg;
        Synchronize(DoThreadError);
    }

    [Delphi(PortStatus.Ported)]
    private void DoThreadError() => OnThreadError?.Invoke(FErrorMessage);

    [Delphi(PortStatus.Ported)]
    private void SetExtendLeaseAfter(int Value)
    {
        fExtendLeaseAfter = Value;
        if (fBoldListenerCOM is { } listener)
        {
            listener.ExtendLeaseAfter = fExtendLeaseAfter;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void WaitUntilInterfaceUnmarshaled()
    {
        fDoneUnmarshalInterface.WaitFor(3 * TIMEOUT);
        fDoneUnmarshalInterface.ResetEvent();
    }

    /// <summary>Gives the listener the propagator handed over by <see cref="Propagator"/>.</summary>
    [Delphi(PortStatus.Ported)]
    protected void UnMarshalInterface()
    {
        TBoldObjectMarshaler? marshaler = Interlocked.Exchange(ref FInterfaceStream, null);
        if (marshaler is not null)
        {
            marshaler.UnMarshalObject(out object? Obj);
            if (fBoldListenerCOM is { } listener)
            {
                listener.Propagator = (IBoldClientHandler?)Obj;
            }

            marshaler.Dispose();
            fDoneUnmarshalInterface.SetEvent();
        }
    }

    [Delphi(PortStatus.Ported, Note = "Without a system error: posting to a thread fails only when it has ended.")]
    protected void RaiseSysErrorMessage(string CausedIn) =>
        throw new EBold(SysUtils.Format(GenericSysErrorMessage, GetType().Name, CausedIn, "the listener thread has ended"));

    [Delphi(PortStatus.Ported)]
    public void SetQueueNotEmptyNotifyEvent(TNotifyEvent? Event)
    {
        if (fBoldListenerCOM is { } listener)
        {
            listener.QueueNotEmptyNotifyEvent = Event;
        }
    }

    /// <summary>The message loop: the listener is created at the first message; then register, unregister, hand over the propagator and extend the lease.</summary>
    [Delphi(PortStatus.Ported, Note = "Leaves the queue to the dequeuer that still uses it; Delphi freed it.")]
    protected override void Execute()
    {
        EnsureMessageQueue();
        SignalInitialized();
        try
        {
            while (!Terminated)
            {
                int res = GetMessage(out TMsg rMsg);
                if (res != -1 && res != 0 && fBoldListenerCOM is null)
                {
                    try
                    {
                        fBoldListenerCOM = new TBoldListenerCOM(Queue, PollingInterval, LeaseDuration, ExtendLeaseAfter, AutoExtendLease,
                            ClientIdentifierString, this)
                        {
                            OnMessage = OnMessage,
                            OnExtendLeaseFailed = DoExtendLeaseFailedSynchronized,
                        };
                    }
                    catch (Exception E)
                    {
                        FErrorMessage = SysUtils.Format(BoldPMConsts.sInitializationLineMissing, E.Message);
                        DoThreadErrorSynchronized(FErrorMessage);
                    }
                }

                try
                {
                    if (res == -1 || res == 0)
                    {
                        Terminate();
                    }
                    else
                    {
                        switch (rMsg.message)
                        {
                            case BM_THRD_UNREGISTER:
                                UnRegisterWithPropagator();
                                break;
                            case BM_THRD_REGISTER:
                                RegisterWithPropagator();
                                break;
                            case BM_UNMARSHAL_INTERFACE:
                                UnMarshalInterface();
                                break;
                            case BM_EXTEND_LEASE:
                                InternalExtendLease();
                                break;
                            default:
                                Windows.DispatchMessage(ref rMsg);
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    DoThreadErrorSynchronized(e.Message);
                    throw;
                }
            }
        }
        finally
        {
            fBoldListenerCOM?.Dispose();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void DebugGetQueueContents(TStringList aList) => fBoldListenerCOM?.DebugGetQueueContents(aList);

    /// <summary>Unregisters and stops the thread; when <paramref name="Wait"/>, waits three seconds at most for it to end.</summary>
    [Delphi(PortStatus.Ported, Note = "Logs a thread that does not end; Delphi terminated it.")]
    public void Quit(bool Wait)
    {
        if (ThreadID == Windows.GetCurrentThreadId())
        {
            throw new EBoldInternal("The listener thread cannot quit itself.");
        }

        if (!Terminated && !Finished)
        {
            if (Suspended)
            {
                Resume();
                WaitUntilInitialized();
            }

            UnRegister();
            if (!PostThreadMessage(Windows.WM_QUIT, 0, 0))
            {
                RaiseSysErrorMessage("Quit");
            }

            if (Wait && Windows.WaitForSingleObject(Handle, 3 * TIMEOUT) != Windows.WAIT_OBJECT_0)
            {
                BoldThreadSafeLog.BoldLogError(BoldCommonConst.sThreadWasForcedTerminated, GetType().Name);
            }
        }
    }

    /// <summary>Waits five seconds at most for the registration; true when registered.</summary>
    [Delphi(PortStatus.Ported)]
    public bool WaitUntilRegistered()
    {
        bool result = fDoneRegister.WaitFor(5 * TIMEOUT) == TWaitResult.wrSignaled && Registered;
        fDoneRegister.ResetEvent();
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void WaitUntilUnregistered()
    {
        fDoneUnregister.WaitFor(3 * TIMEOUT);
        fDoneUnregister.ResetEvent();
    }

    [Delphi(PortStatus.Ported)]
    public void WaitUntilInitialized() => fDoneInit.WaitFor(Windows.INFINITE);

    /// <summary>Extends the lease on the listener thread and waits for it (three seconds at most); true when extended.</summary>
    [Delphi(PortStatus.Ported)]
    public bool ExtendLease()
    {
        bool result = Initialized && !Suspended;
        if (result)
        {
            try
            {
                if (!PostThreadMessage(BM_EXTEND_LEASE, 0, 0))
                {
                    RaiseSysErrorMessage("ExtendLease");
                }

                fDoneExtendLease.WaitFor(3 * TIMEOUT);
                result = fExtendLeaseSucceeded.WaitFor(0) == TWaitResult.wrSignaled;
            }
            finally
            {
                fDoneExtendLease.ResetEvent();
                fExtendLeaseSucceeded.ResetEvent();
            }
        }

        return result;
    }

    /// <summary>Unregisters on the listener thread and waits for it.</summary>
    [Delphi(PortStatus.Ported)]
    public void UnRegister()
    {
        if (Initialized && Registered && !Suspended)
        {
            if (ThreadID == Windows.GetCurrentThreadId())
            {
                UnRegisterWithPropagator();
                fDoneUnregister.ResetEvent();
                return;
            }

            if (!PostThreadMessage(BM_THRD_UNREGISTER, 0, 0))
            {
                RaiseSysErrorMessage("UnRegister");
            }

            WaitUntilUnregistered();
        }
    }

    /// <summary>Asks the listener thread to register; <see cref="WaitUntilRegistered"/> waits for it.</summary>
    public void Register() => PostThreadMessage(BM_THRD_REGISTER, 0, 0);

    [Delphi(PortStatus.Ported)]
    public void DoPropagatorFailureSynchronized() => Synchronize(DoPropagatorFailure);

    [Delphi(PortStatus.Ported)]
    public void DoExtendLeaseFailedSynchronized(TBoldExtendLeaseResult res, string Msg)
    {
        fExtendResult = res;
        FErrorMessage = Msg;
        Synchronize(DoExtendLeaseFailed);
    }

    [Delphi(PortStatus.Ported)] public TBoldClientID BoldClientID => getBoldClientID();
    [Delphi(PortStatus.Ported)] public bool Registered => GetRegistered();
    [Delphi(PortStatus.Ported)] public bool Initialized => GetInitialized();
    [Delphi(PortStatus.Ported)] public TBoldThreadSafeStringQueue Queue => GetQueue();

    [Delphi(PortStatus.Ported)]
    public int LeaseDuration
    {
        get => fLeaseDuration;
        set => setLeaseDuration(value);
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
        set => setPollingInterval(value);
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
        set => setClientIdentifierString(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldMessageEvent? OnMessage
    {
        get => fOnMessage;
        set => SetOnMessage(value);
    }

    [Delphi(PortStatus.Ported)]
    public IBoldClientHandler? Propagator
    {
        get => GetPropagator();
        set => SetPropagator(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldNotifyEventWithErrorMessage? OnPropagatorFailure { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExtendLeaseFailureEvent? OnExtendLeaseFailed { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldMessageEvent? OnThreadError { get; set; }
}
