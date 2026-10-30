// Delphi: Source/Propagator/Enterprise/BoldClientNotifierHandler.pas
// Port map: CSharp/port/units/BoldClientNotifierHandler.md

using static Bold.BoldPropagatorConstants;
using static Bold.PropagatorConsts;

namespace Bold;

/// <summary>
/// The thread that sends the clients their events. It watches the priority list of client queues: when the head's
/// time comes, the queue's events are scheduled on a notifier of the pool, which calls the client's listener.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientNotifierHandler : TBoldNotifiableThread
{
    private TThreadTimer? fTimer;
    private TBoldClientNotifierPool? fClientNotifierPool;
    private readonly int fClientNotifierPoolSize;
    private readonly Queue<TBoldClientNotifierData> fScheduledClients = new();
    private TBoldPriorityQueue? fPriorityList;
    private readonly TBoldClientHandler fClientHandler;
    internal readonly bool fDisconnectClientsOnSendFailure;

    [Delphi(PortStatus.Ported)]
    public TBoldClientNotifierHandler(int ClientNotifierPoolSize, TBoldClientHandler aClientHandler, TBoldPriorityQueue aPriorityList,
        bool DisconnectClientsOnSendFailure)
        : base(true)
    {
        fClientNotifierPoolSize = ClientNotifierPoolSize;
        fClientNotifierPool = new TBoldClientNotifierPool(this, fClientNotifierPoolSize);
        fClientHandler = aClientHandler;
        fDisconnectClientsOnSendFailure = DisconnectClientsOnSendFailure;
        PriorityList = aPriorityList;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientNotifierHandler.Destroy")]
    public override void Dispose()
    {
        PriorityList = null;
        SysUtils.FreeAndNil(ref fClientNotifierPool);
        fScheduledClients.Clear();
        base.Dispose();
    }

    /// <summary>
    /// The message loop: a change of the priority list's head is handled first, then the scheduled clients are handed
    /// to notifiers; with nothing to do the thread waits for a message.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Yields while every notifier is busy; Delphi spun.")]
    protected override void Execute()
    {
        try
        {
            EnsureMessageQueue();
            BoldThreadSafeLog.BoldLogThread("ID=ClientNotifierHandler");
            SignalReady();
            while (!Terminated)
            {
                if (PeekMessage(out TMsg rMsg, true))
                {
                    if (rMsg.message == Windows.WM_QUIT)
                    {
                        Terminate();
                    }
                    else if (rMsg.message == BM_PRIORITY_CHANGED)
                    {
                        ProcessPriorityListHead();
                    }
                    else
                    {
                        Windows.DispatchMessage(ref rMsg);
                    }
                }
                else if (fScheduledClients.Count > 0)
                {
                    int before = fScheduledClients.Count;
                    ProcessScheduledClientsHead();
                    if (fScheduledClients.Count == before)
                    {
                        Thread.Yield();
                    }
                }
                else
                {
                    WaitMessage();
                }
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "Execute", E.Message);
        }

        SysUtils.FreeAndNil(ref fTimer);
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldClientHandler getClientHandler() => fClientHandler;

    [Delphi(PortStatus.Ported)]
    private TBoldClientNotifierPool getClientNotifierPool() => fClientNotifierPool!;

    /// <summary>The timer for the head's time; it belongs to this thread.</summary>
    [Delphi(PortStatus.Ported)]
    private TThreadTimer getTimer()
    {
        if (Windows.GetCurrentThreadId() != ThreadID)
        {
            throw new EBoldInternal(SysUtils.Format("%s.getTimer: Timer should be accessed from %s's thread", GetType().Name, GetType().Name));
        }

        if (fTimer is null)
        {
            fTimer = new TThreadTimer(this) { Enabled = false };
            fTimer.OnTimer = OnTimer;
        }

        return fTimer;
    }

    /// <summary>The priority list's head changed: handle it on this thread.</summary>
    [Delphi(PortStatus.Ported)]
    public void OnPriorityChanged(object? Sender)
    {
        var PriorityQueue = (TBoldPriorityQueue)Sender!;
        if (PriorityQueue.Head is not null)
        {
            Notify(BM_PRIORITY_CHANGED);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void OnTimer(object? Sender)
    {
        Timer.Enabled = false;
        SendToClient();
    }

    /// <summary>Takes the head of the priority list and schedules its events for the client, if it is still registered.</summary>
    [Delphi(PortStatus.Ported, Note = "Yields while every notifier is busy; Delphi spun.")]
    private void SendToClient()
    {
        if (PriorityList is null)
        {
            return;
        }

        var ClientQueueInfo = (TBoldClientQueueInfo?)PriorityList.ChopHead();
        try
        {
            if (ClientQueueInfo is not null && ClientQueueInfo.Count != 0 &&
                ClientHandler.HasInfoForClient(ClientQueueInfo.ClientID, out string _, out TTimeStamp RegistrationTime, out bool Initialized,
                    out TBoldClientReceiveStatus _) && Initialized)
            {
                try
                {
                    var aData = new TBoldClientNotifierData(ClientQueueInfo.ClientID, RegistrationTime, ClientQueueInfo.QueueAsVarArray);
                    fScheduledClients.Enqueue(aData);
                    while (fScheduledClients.Count > 0)
                    {
                        int before = fScheduledClients.Count;
                        ProcessScheduledClientsHead();
                        if (fScheduledClients.Count == before)
                        {
                            Thread.Yield();
                        }
                    }
                }
                catch (Exception)
                {
                    throw new EBold(SysUtils.Format(sFailedToSendToClient, GetType().Name));
                }
            }
        }
        finally
        {
            ClientQueueInfo?.Dispose();
        }
    }

    /// <summary>An empty queue at the head is dropped; one whose time has come is sent; otherwise the timer waits for its time.</summary>
    [Delphi(PortStatus.Ported, Note = "Compares whole time stamps; Delphi compared dates and times apart (its FIXME).")]
    protected void ProcessPriorityListHead()
    {
        if (PriorityList?.Head is TBoldClientQueueInfo ClientQueueInfo)
        {
            if (ClientQueueInfo.Count == 0)
            {
                PriorityList.RemoveAndFreeHead();
            }
            else
            {
                double untilTimeOut = SysUtils.TimeStampToMSecs(ClientQueueInfo.TimeOut) - SysUtils.TimeStampToMSecs(SysUtils.DateTimeToTimeStamp(DateTime.Now));
                if (untilTimeOut <= 1)
                {
                    SendToClient();
                }
                else
                {
                    SetTimer(ClientQueueInfo.TimeOut);
                }
            }
        }
    }

    /// <summary>Hands the first scheduled client to a free notifier; true while clients are scheduled.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool ProcessScheduledClientsHead()
    {
        bool result = fScheduledClients.Count > 0;
        if (result)
        {
            TBoldClientNotifierData aData = fScheduledClients.Peek();
            if (ClientNotifierPool.ScheduleClientNotifier(aData))
            {
                fScheduledClients.Dequeue();
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported, Note = "Waits the whole time to the time stamp; Delphi took the times of day only.")]
    private void SetTimer(TTimeStamp TimePoint)
    {
        Timer.Enabled = false;
        double interval = SysUtils.TimeStampToMSecs(TimePoint) - SysUtils.TimeStampToMSecs(SysUtils.DateTimeToTimeStamp(DateTime.Now));
        Timer.Interval = (int)Math.Clamp(Math.Truncate(interval), 1, int.MaxValue);
        Timer.Enabled = true;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void DisconnectClient(TBoldClientID ClientId, TTimeStamp RegistrationTime) => ClientHandler.DisconnectClient(ClientId, RegistrationTime);

    [Delphi(PortStatus.Ported)]
    private TBoldPriorityQueue? getPriorityList() => fPriorityList;

    [Delphi(PortStatus.Ported, Note = "Unhooks the old list; Delphi hooked it again.")]
    private void SetPriorityList(TBoldPriorityQueue? Value)
    {
        if (Value != fPriorityList)
        {
            if (fPriorityList is not null)
            {
                fPriorityList.OnHeadChanged = null;
            }

            fPriorityList = Value;
            if (fPriorityList is not null)
            {
                fPriorityList.OnHeadChanged = OnPriorityChanged;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldClientNotifierPool ClientNotifierPool => getClientNotifierPool();

    [Delphi(PortStatus.Ported)]
    protected TThreadTimer Timer => getTimer();

    [Delphi(PortStatus.Ported)]
    protected internal TBoldClientHandler ClientHandler => getClientHandler();

    [Delphi(PortStatus.Ported)]
    public TBoldPriorityQueue? PriorityList
    {
        get => getPriorityList();
        set => SetPriorityList(value);
    }
}

/// <summary>The events for one client, with the registration time of the client they are for.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientNotifierData
{
    internal readonly TBoldClientID FClientID;
    internal readonly TTimeStamp FRegistrationTime;
    internal readonly string[]? FEvents;

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientNotifierData.CreateInitialized", Note = "Takes a string array where Delphi took a variant.")]
    public TBoldClientNotifierData(TBoldClientID ClientID, TTimeStamp RegistrationTime, string[]? Events)
    {
        FEvents = Events;
        FClientID = ClientID;
        FRegistrationTime = RegistrationTime;
    }
}

/// <summary>
/// A thread of the notifier pool: it calls a client's listener with the client's events. When the call fails the
/// events are counted as lost and, when the propagator is set to, the client is disconnected.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientNotifier : TBoldNotifiableThread
{
    private TEvent? fAvailableEvent;
    private readonly TBoldClientNotifierHandler fOwner;
    private TBoldClientNotifierData? fClientData;

    [Delphi(PortStatus.Ported)]
    public TBoldClientNotifier(TBoldClientNotifierHandler AOwner)
        : base(true)
    {
        fAvailableEvent = new TEvent();
        fOwner = AOwner;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientNotifier.Destroy")]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fAvailableEvent);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private bool GetAvailable() => fAvailableEvent is { } e && e.WaitFor(0) == TWaitResult.wrSignaled;

    [Delphi(PortStatus.Ported)]
    protected internal bool IsAvailable => GetAvailable();

    /// <summary>Takes the work; the notifier is busy until it is done.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void SetClientData(TBoldClientNotifierData aData)
    {
        fAvailableEvent!.ResetEvent();
        fClientData = aData;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void DoWork() => Notify(BM_THRD_DOWORK);

    [Delphi(PortStatus.Ported)]
    protected TBoldClientNotifierHandler Owner => fOwner;

    [Delphi(PortStatus.Ported)]
    protected override void Execute()
    {
        EnsureMessageQueue();
        SignalReady();
        BoldThreadSafeLog.BoldLogThread("ID=ClientNotifier");
        fAvailableEvent!.SetEvent();
        while (!Terminated)
        {
            int res = GetMessage(out TMsg rMsg);
            if (res == -1 || res == 0)
            {
                Terminate();
            }
            else if (rMsg.message == BM_THRD_DOWORK)
            {
                TBoldClientNotifierData data = fClientData!;
                int eventCount = data.FEvents?.Length ?? 0;
                try
                {
                    if (Owner.ClientHandler.GetListener(data.FClientID, data.FRegistrationTime, out IBoldListener? aListener) && aListener is not null)
                    {
                        try
                        {
                            aListener.ReceiveEvents(data.FEvents);
                            Owner.ClientHandler.MarkHasReceivedEvents(data.FClientID, eventCount);
                        }
                        catch (Exception E)
                        {
                            Owner.ClientHandler.HasInfoForClient(data.FClientID, out string ClientIdentifier, out TTimeStamp RegistrationTime, out bool _,
                                out TBoldClientReceiveStatus _);
                            Owner.ClientHandler.HasInfoForClient(data.FClientID, out ClientIdentifier, out int _, out int _, out TTimeStamp LeaseTimeout, out bool _);
                            DateTime RegistrationTimeDT = SysUtils.TimeStampToDateTime(RegistrationTime);
                            DateTime LeaseTimeoutDT = SysUtils.TimeStampToDateTime(LeaseTimeout);
                            Owner.ClientHandler.MarkFailedToReceiveEvents(data.FClientID, eventCount);
                            string DisconnectMsg = Owner.fDisconnectClientsOnSendFailure ? sDisconnectMsg : "";
                            BoldThreadSafeLog.BoldLogError(sClientFailure, GetType().Name, ClientIdentifier, data.FClientID, DisconnectMsg, eventCount,
                                E.Message, SysUtils.DateTimeToStr(RegistrationTimeDT),
                                TBoldClientHandler.IntervalToStr(DateTime.Now - RegistrationTimeDT), SysUtils.TimeToStr(LeaseTimeoutDT),
                                TBoldClientHandler.IntervalToStr(DateTime.Now - LeaseTimeoutDT));
                            if (Owner.fDisconnectClientsOnSendFailure)
                            {
                                Owner.DisconnectClient(data.FClientID, data.FRegistrationTime);
                            }
                        }
                    }
                }
                finally
                {
                    fClientData = null;
                    fAvailableEvent!.SetEvent();
                }
            }
            else
            {
                Windows.DispatchMessage(ref rMsg);
            }
        }

        fClientData = null;
    }
}

/// <summary>The notifier threads, handed work in turn.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientNotifierPool : IDisposable
{
    private List<TBoldClientNotifier>? fThreadList;
    private int CurrentThread;
    private readonly int fPoolSize;

    [Delphi(PortStatus.Ported)]
    public TBoldClientNotifierPool(TBoldClientNotifierHandler ClientHandler, int PoolSize)
    {
        fPoolSize = PoolSize;
        fThreadList = [];
        for (int i = 0; i < fPoolSize; i++)
        {
            var ClientNotifier = new TBoldClientNotifier(ClientHandler);
            fThreadList.Add(ClientNotifier);
            ClientNotifier.Resume();
            ClientNotifier.WaitUntilReady(TIMEOUT * 2);
        }

        CurrentThread = 0;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientNotifierPool.Destroy")]
    public void Dispose()
    {
        if (fThreadList is null)
        {
            return;
        }

        foreach (TBoldClientNotifier ClientNotifier in fThreadList)
        {
            ClientNotifier.Quit(true);
        }

        foreach (TBoldClientNotifier ClientNotifier in fThreadList)
        {
            ClientNotifier.Dispose();
        }

        fThreadList = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>Gives the work to the next notifier if it is free; true when given. The next call tries the notifier after it.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal bool ScheduleClientNotifier(TBoldClientNotifierData mData)
    {
        bool result = false;
        try
        {
            TBoldClientNotifier ClientNotifier = fThreadList![CurrentThread];
            if (ClientNotifier.IsAvailable)
            {
                ClientNotifier.SetClientData(mData);
                ClientNotifier.DoWork();
                result = true;
            }

            CurrentThread = (CurrentThread + 1) % fPoolSize;
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "ScheduleClientNotifier", E.Message);
        }

        return result;
    }
}
