// Delphi: Source/Persistence/Propagation/BoldListenerHandle.pas
// Port map: CSharp/port/units/BoldListenerHandle.md

using static Bold.BoldPropagatorConstants;
using static Bold.BoldSubscription;
using static Bold.BoldDefs;

namespace Bold;

/// <summary>
/// A client's listener: the handle starts the listener thread, which registers with the propagator of
/// <see cref="PropagatorHandle"/>, and gives the received events to <see cref="Dequeuer"/>.
/// </summary>
/// <remarks>
/// The dequeuer is told of new events on the thread that created the handle (the GUI thread), through its
/// synchronization context; Delphi told it on the listener's thread.
/// </remarks>
[Delphi(PortStatus.Ported)]
public class TBoldListenerHandle : TBoldHandle
{
    private bool fActive;
    private TBoldListenerThread? fBoldListenerThread;
    private TBoldAbstractDequeuer? fDequeuer;
    private TBoldPassthroughSubscriber? fPTSubscriber;
    private bool fAutoStart;
    private int fLeaseDuration;
    private int fPollingInterval;
    private bool fAutoExtendLease;
    private TBoldExtendLeaseFailureEvent? FOnExtendLeaseFailed;
    private string FClientIdentifierString;
    private TBoldPassthroughSubscriber? FPropagatorHandleSubscriber;
    private TBoldAbstractPropagatorHandle? fPropagatorHandle;
    private int fExtendLeaseAfter;
    private readonly SynchronizationContext? fContext;

    [Delphi(PortStatus.Ported)]
    public TBoldListenerHandle(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
        fContext = SynchronizationContext.Current;
        FClientIdentifierString = "";
        fPTSubscriber = new TBoldPassthroughSubscriber(_Receive);
        FPropagatorHandleSubscriber = new TBoldPassthroughSubscriber(_ReceivePropagatorHandleEvents);
        fAutoStart = true;
        fLeaseDuration = DEFAULT_LEASE_DURATION;
        fExtendLeaseAfter = DEFAULT_EXTEND_LEASE_AFTER;
        fPollingInterval = DEFAULT_POLLING_INTERVAL;
        fActive = false;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldListenerHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopAndFreeListenerThread();
            SysUtils.FreeAndNil(ref fPTSubscriber);
            SysUtils.FreeAndNil(ref FPropagatorHandleSubscriber);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == fDequeuer && RequestedEvent == beDestroying)
        {
            Dequeuer = null;
        }

        if (Originator == fPropagatorHandle && RequestedEvent == beDestroying)
        {
            fPropagatorHandle = null;
            if (fBoldListenerThread is not null)
            {
                fBoldListenerThread.OnPropagatorFailure = null;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldListenerThread GetListenerThread()
    {
        if (fBoldListenerThread is null)
        {
            fBoldListenerThread = new TBoldListenerThread(true)
            {
                LeaseDuration = LeaseDuration,
                PollingInterval = PollingInterval,
                AutoExtendLease = AutoExtendLease,
                ClientIdentifierString = FClientIdentifierString,
                OnExtendLeaseFailed = FOnExtendLeaseFailed,
            };
        }

        return fBoldListenerThread;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetConnected() => ListenerThread.Registered;

    [Delphi(PortStatus.Ported)]
    private void SetDequeuer(TBoldAbstractDequeuer? aDequeuer)
    {
        if (aDequeuer != fDequeuer)
        {
            fDequeuer = aDequeuer;
            if (fDequeuer is not null)
            {
                fDequeuer.Queue = ListenerThread.Queue;
            }
        }
    }

    /// <summary>New events: the dequeuer is asked to dequeue them, on the handle's thread, and the Bold queue is triggered.</summary>
    [Delphi(PortStatus.Ported, Note = "Runs on the handle's synchronization context; Delphi ran it on the listener's thread.")]
    private void NotifyDequeuer(object? Sender)
    {
        if (fContext is null || SynchronizationContext.Current == fContext)
        {
            NotifyDequeuerHere();
        }
        else
        {
            fContext.Post(_ => NotifyDequeuerHere(), null);
        }
    }

    private void NotifyDequeuerHere()
    {
        if (IsDestroying)
        {
            return;
        }

        Dequeuer?.QueueNotEmpty();
        BoldEnvironment.BoldEffectiveEnvironment().TriggerQueueMechanism();
    }

    [Delphi(PortStatus.Ported)]
    private void setLeaseDuration(int Value)
    {
        fLeaseDuration = Value;
        ListenerThread.LeaseDuration = fLeaseDuration;
    }

    [Delphi(PortStatus.Ported)]
    private void setPollingInterval(int Value)
    {
        fPollingInterval = Value;
        ListenerThread.PollingInterval = fPollingInterval;
    }

    [Delphi(PortStatus.Ported)]
    private void setAutoExtendLease(bool Value)
    {
        fAutoExtendLease = Value;
        ListenerThread.AutoExtendLease = fAutoExtendLease;
    }

    [Delphi(PortStatus.Ported)]
    private void setClientIdentifierString(string Value)
    {
        FClientIdentifierString = Value;
        if (fBoldListenerThread is not null)
        {
            fBoldListenerThread.ClientIdentifierString = FClientIdentifierString;
        }
    }

    /// <summary>The propagator handle connects (start, when active and AutoStart), disconnects (suspend) or goes (unregister).</summary>
    [Delphi(PortStatus.Ported)]
    private void _ReceivePropagatorHandleEvents(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        switch (OriginalEvent)
        {
            case beDestroying:
            case beDisconnecting:
                if (fBoldListenerThread is not null && IsLoaded)
                {
                    fBoldListenerThread.UnRegister();
                    fBoldListenerThread.Propagator = null;
                }

                break;
            case beConnected:
                if (fActive && AutoStart)
                {
                    StartListenerThread();
                }

                break;
            case beDisconnected:
                SuspendListenerThread();
                break;
            case beModified:
                if (fBoldListenerThread is not null && fPropagatorHandle is not null)
                {
                    fBoldListenerThread.OnPropagatorFailure = fPropagatorHandle.DoPropagatorCallFailed;
                }

                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetPropagatorHandle(TBoldAbstractPropagatorHandle? Value)
    {
        if (Value != fPropagatorHandle)
        {
            if (fActive)
            {
                throw new EBold(SysUtils.Format(BoldPMConsts.sCannotChangeHandleWhenActive, GetType().Name));
            }

            FPropagatorHandleSubscriber!.CancelAllSubscriptions();
            Subscribe(false);
            fPropagatorHandle = Value;
            Subscribe(true);
            if (fPropagatorHandle is not null)
            {
                fPropagatorHandle.AddSubscription(FPropagatorHandleSubscriber, beConnected, 0);
                fPropagatorHandle.AddSubscription(FPropagatorHandleSubscriber, beDisconnected, 0);
                fPropagatorHandle.AddSubscription(FPropagatorHandleSubscriber, beDisconnecting, 0);
                fPropagatorHandle.AddSubscription(FPropagatorHandleSubscriber, beModified, 0);
                FreeNotification(fPropagatorHandle);
            }
            else if (fBoldListenerThread is not null)
            {
                fBoldListenerThread.OnPropagatorFailure = null;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetOnExtendLeaseFailed(TBoldExtendLeaseFailureEvent? Value)
    {
        FOnExtendLeaseFailed = Value;
        if (fBoldListenerThread is not null)
        {
            fBoldListenerThread.OnExtendLeaseFailed = FOnExtendLeaseFailed;
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetIsLoaded() => !IsDesigning && !IsLoading;

    [Delphi(PortStatus.Ported)]
    private void StopAndFreeListenerThread()
    {
        if (fBoldListenerThread is not null)
        {
            ListenerThread.Quit(true);
            fBoldListenerThread.Dispose();
            fBoldListenerThread = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetExtendLeaseAfter(int Value)
    {
        if (Value < 10 || Value > 90)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sValueOutOfRange, GetType().Name));
        }

        fExtendLeaseAfter = Value;
        ListenerThread.ExtendLeaseAfter = Value;
    }

    [Delphi(PortStatus.Ported)]
    private void Subscribe(bool DoSubscribe)
    {
        if (DoSubscribe)
        {
            if (!IsDesigning)
            {
                fDequeuer?.AddSmallSubscription(fPTSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
                fPropagatorHandle?.AddSmallSubscription(fPTSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
            }
        }
        else
        {
            fPTSubscriber!.CancelAllSubscriptions();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override object? GetHandledObject() => ListenerThread;

    [Delphi(PortStatus.Ported)]
    protected TBoldClientID GetBoldClientID() => ListenerThread.BoldClientID;

    [Delphi(PortStatus.Ported)]
    protected override void Notification(System.ComponentModel.IComponent AComponent, TOperation Operation)
    {
        base.Notification(AComponent, Operation);
        if (AComponent == fDequeuer && Operation == TOperation.opRemove)
        {
            fDequeuer = null;
        }

        if (AComponent == fPropagatorHandle && Operation == TOperation.opRemove)
        {
            if (fBoldListenerThread is not null)
            {
                fBoldListenerThread.OnPropagatorFailure = null;
            }

            fPropagatorHandle = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected bool IsLoaded => GetIsLoaded();

    /// <summary>
    /// Starts the listener thread and registers with the propagator; when registering fails the thread is suspended
    /// and <see cref="OnRegistrationFailed"/> is called.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void StartListenerThread()
    {
        if (fPropagatorHandle is null)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sPropagatorHandleNotAssigned, GetType().Name));
        }

        if (!ListenerThread.Registered)
        {
            ListenerThread.Resume();
            ListenerThread.WaitUntilInitialized();
            if (!fPropagatorHandle.Connected)
            {
                throw new EBold(SysUtils.Format(BoldPMConsts.sPropagatorHandleNotConnected, GetType().Name));
            }

            ListenerThread.Propagator = fPropagatorHandle.ClientHandler;
            ListenerThread.OnPropagatorFailure = fPropagatorHandle.DoPropagatorCallFailed;
            ListenerThread.OnThreadError = OnThreadError;
            ListenerThread.Register();
            if (ListenerThread.WaitUntilRegistered())
            {
                ListenerThread.SetQueueNotEmptyNotifyEvent(NotifyDequeuer);
            }
            else
            {
                ListenerThread.Suspend();
                OnRegistrationFailed?.Invoke(this);
            }
        }
    }

    /// <summary>Unregisters and suspends the listener thread.</summary>
    [Delphi(PortStatus.Ported)]
    public void SuspendListenerThread()
    {
        if (fBoldListenerThread is not null && !ListenerThread.Suspended)
        {
            ListenerThread.UnRegister();
            ListenerThread.Propagator = null;
            ListenerThread.Suspend();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void DebugGetQueueContents(TStringList aList) => fBoldListenerThread?.DebugGetQueueContents(aList);

    [Delphi(PortStatus.Ported)]
    public bool ExtendLease() => ListenerThread.ExtendLease();

    /// <summary>Activating starts the listener when the propagator handle is connected; deactivating suspends it.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetActive(bool Value)
    {
        if (Value != fActive)
        {
            fActive = Value;
            if (fActive)
            {
                if (fPropagatorHandle is { Connected: true })
                {
                    StartListenerThread();
                }
            }
            else
            {
                SuspendListenerThread();
            }
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldClientID BoldClientID => GetBoldClientID();
    [Delphi(PortStatus.Ported)] public bool Connected => GetConnected();
    [Delphi(PortStatus.Ported)] public TBoldListenerThread ListenerThread => GetListenerThread();

    [Delphi(PortStatus.Ported)]
    public bool AutoStart
    {
        get => fAutoStart;
        set => fAutoStart = value;
    }

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
    public TBoldAbstractDequeuer? Dequeuer
    {
        get => fDequeuer;
        set => SetDequeuer(value);
    }

    [Delphi(PortStatus.Ported)]
    public string ClientIdentifierString
    {
        get => FClientIdentifierString;
        set => setClientIdentifierString(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractPropagatorHandle? PropagatorHandle
    {
        get => fPropagatorHandle;
        set => SetPropagatorHandle(value);
    }

    [Delphi(PortStatus.Ported)] public TNotifyEvent? OnRegistrationFailed { get; set; }

    [Delphi(PortStatus.Ported)]
    public TBoldExtendLeaseFailureEvent? OnExtendLeaseFailed
    {
        get => FOnExtendLeaseFailed;
        set => SetOnExtendLeaseFailed(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldMessageEvent? OnThreadError { get; set; }
}
