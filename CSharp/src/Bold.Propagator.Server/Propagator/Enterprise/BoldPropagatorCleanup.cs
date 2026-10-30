// Delphi: Source/Propagator/Enterprise/BoldPropagatorCleanup.pas
// Port map: CSharp/port/units/BoldPropagatorCleanup.md

using static Bold.BoldPropagatorConstants;
using static Bold.PropagatorConsts;

namespace Bold;

/// <summary>
/// The thread that removes clients whose lease ran out: its timer is set for the lease that runs out first, and when
/// it fires the client is removed unless its lease was extended meanwhile.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldCleanUpThread : TBoldNotifiableThread
{
    private TThreadTimer? fTimer;
    private readonly TBoldCleanupSubscriber fOwner;
    private readonly object fTimingOutClientLock = new();
    private TBoldClientInfoRecord fTimingOutClient;

    [Delphi(PortStatus.Ported)]
    public TBoldCleanUpThread(bool CreateSuspended, TBoldCleanupSubscriber Owner)
        : base(CreateSuspended)
    {
        fOwner = Owner;
    }

    /// <summary>The client whose lease runs out first, as the client handler last told.</summary>
    protected internal TBoldClientInfoRecord TimingOutClient
    {
        get
        {
            lock (fTimingOutClientLock)
            {
                return fTimingOutClient;
            }
        }

        set
        {
            lock (fTimingOutClientLock)
            {
                fTimingOutClient = value;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TThreadTimer getTimer()
    {
        if (fTimer is null)
        {
            fTimer = new TThreadTimer(this) { Enabled = false };
            fTimer.OnTimer = OnTimer;
        }

        return fTimer;
    }

    [Delphi(PortStatus.Ported)]
    private void OnTimer(object? Sender)
    {
        DisableTimer();
        RemoveTimedOutClient();
    }

    /// <summary>Sets the timer for the lease that runs out first; a lease that has run out is removed at once.</summary>
    [Delphi(PortStatus.Ported)]
    internal void SetTimer()
    {
        try
        {
            if (Timer.Enabled)
            {
                DisableTimer();
            }

            double CurrentTime = SysUtils.TimeStampToMSecs(SysUtils.DateTimeToTimeStamp(DateTime.Now));
            double Interval = Math.Truncate(SysUtils.TimeStampToMSecs(TimingOutClient.LeaseTimeOut) - CurrentTime + 1);
            if (Interval > 0)
            {
                Timer.Interval = (int)Math.Min(Interval, int.MaxValue);
                Timer.Enabled = true;
            }
            else
            {
                RemoveTimedOutClient();
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError("%s.SetTimer: %s", GetType().Name, E.Message);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void DisableTimer() => Timer.Enabled = false;

    [Delphi(PortStatus.Ported)]
    private TBoldCleanupSubscriber GetOwner() => fOwner;

    [Delphi(PortStatus.Ported)]
    protected override bool ProcessMessage(ref TMsg Msg)
    {
        Windows.DispatchMessage(ref Msg);
        return true;
    }

    /// <summary>The window that receives BM_PROPAGATOR_CLIENT_LEASE_CHANGED on this thread.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives a TThreadWindow whose window procedure is BoldPropagatorCleanupWndProc.")]
    protected override void CreateQueueWindow(ref TThreadWindow? ServerWindow) =>
        ServerWindow = new TThreadWindow(this, BoldPropagatorCleanupWndProc);

    /// <summary>The queue window's procedure: a changed lease sets the timer.</summary>
    [Delphi(PortStatus.Ported, Symbol = "BoldPropagatorCleanupWndProc", Note = "The window procedure of this thread's window, where Delphi passed the thread as lParam.")]
    private void BoldPropagatorCleanupWndProc(ref TMsg Message)
    {
        if (Message.message == BM_PROPAGATOR_CLIENT_LEASE_CHANGED)
        {
            try
            {
                SetTimer();
            }
            catch (Exception E)
            {
                BoldThreadSafeLog.BoldLogError(sWindowProcError, E.Message);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldCleanupSubscriber Owner => GetOwner();

    [Delphi(PortStatus.Ported)]
    protected override void Execute()
    {
        EnsureMessageQueue();
        InitServerWindow(true);
        SignalReady();
        BoldThreadSafeLog.BoldLogThread("ID=Cleanup");
        while (!Terminated)
        {
            int res = GetMessage(out TMsg rMsg);
            if (res == -1 || rMsg.message == Windows.WM_QUIT)
            {
                Terminate();
            }
            else
            {
                ProcessMessage(ref rMsg);
            }
        }

        SysUtils.FreeAndNil(ref fTimer);
    }

    /// <summary>Removes the timing out client if its lease has run out.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveTimedOutClient()
    {
        TBoldClientInfoRecord client = TimingOutClient;
        fOwner.ClientHandler?.RemoveExpiredLease(client.ClientId, client.RegistrationTime);
    }

    [Delphi(PortStatus.Ported)]
    public TThreadTimer Timer
    {
        get => getTimer();
        set => fTimer = value;
    }
}

/// <summary>Hears the client handler's lease changes and passes them to the cleanup thread.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldCleanupSubscriber : TBoldPassthroughSubscriber
{
    private TBoldCleanUpThread? fCleanUpThread;
    private TBoldClientHandler? fClientHandler;

    [Delphi(PortStatus.Ported)]
    public TBoldCleanupSubscriber(TBoldClientHandler aClientHandler)
        : base((TBoldExtendedEventHandler)null!)
    {
        fCleanUpThread = new TBoldCleanUpThread(true, this);
        fCleanUpThread.Resume();
        fCleanUpThread.WaitUntilReady(TIMEOUT);
        ClientHandler = aClientHandler;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCleanupSubscriber.Destroy")]
    public override void Dispose()
    {
        CancelAllSubscriptions();
        if (fCleanUpThread is not null)
        {
            fCleanUpThread.Quit(true);
            SysUtils.FreeAndNil(ref fCleanUpThread);
        }

        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private void DoLeaseChanged()
    {
        try
        {
            Windows.PostMessage(fCleanUpThread!.QueueWindow, BM_PROPAGATOR_CLIENT_LEASE_CHANGED, 0, 0);
        }
        catch (Exception e)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "DoLeaseChanged", e.Message);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetClientHandler(TBoldClientHandler? Value)
    {
        if (fClientHandler != Value)
        {
            fClientHandler = Value;
            fClientHandler?.AddSubscription(this, BOLD_PROPAGATOR_CLIENT_LEASE_CHANGED, BOLD_PROPAGATOR_CLIENT_LEASE_CHANGED);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldClientHandler? ClientHandler
    {
        get => fClientHandler;
        set => SetClientHandler(value);
    }

    /// <summary>A lease changed: the cleanup thread is told which client's lease runs out first, and sets its timer.</summary>
    [Delphi(PortStatus.Ported)]
    public void OnGetExtendedEvent(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        if (RequestedEvent == BOLD_PROPAGATOR_CLIENT_LEASE_CHANGED)
        {
            try
            {
                fCleanUpThread!.TimingOutClient = new TBoldClientInfoRecord
                {
                    ClientId = (TBoldClientID)Args[0]!,
                    RegistrationTime = SysUtils.MSecsToTimeStamp((double)Args[1]!),
                    LeaseTimeOut = SysUtils.MSecsToTimeStamp((double)Args[2]!),
                };
                DoLeaseChanged();
            }
            catch (Exception)
            {
                BoldThreadSafeLog.BoldLogError("%s.OnGetExtendedEvent: Bold_propagator_client_lease_changed", GetType().Name);
            }
        }
    }

    protected override bool GetHandlesExtendedEvents() => true;

    protected override void ReceiveExtended(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) =>
        OnGetExtendedEvent(Originator, OriginalEvent, RequestedEvent, Args);
}
