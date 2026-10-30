// Delphi: Source/Common/SupportWin/BoldThread.pas
// Port map: CSharp/port/units/BoldThread.md

namespace Bold;

/// <summary>
/// A thread with a message loop that others notify with messages (<see cref="Notify"/>) and stop with
/// <see cref="Quit"/>. Subclasses handle messages in <see cref="ProcessMessage"/> or run their own loop in
/// <see cref="Execute"/>; a subclass that needs a window for messages creates it in <see cref="CreateQueueWindow"/>.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldNotifiableThread : TThread
{
    private TEvent? fReadyEvent;
    private int FQuitWaitTimeOut;
    private TThreadWindow? FQueueWindow;

    [Delphi(PortStatus.Ported)]
    public TBoldNotifiableThread(bool CreateSuspended)
        : base(true)
    {
        fReadyEvent = new TEvent();
        QuitWaitTimeOut = BoldPropagatorConstants.TIMEOUT * 5;
        ReturnValue = -10;
        Suspended = CreateSuspended;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldNotifiableThread.Destroy")]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fReadyEvent);
        base.Dispose();
    }

    /// <summary>Tells those waiting in <see cref="WaitUntilReady"/> that the thread runs its message loop.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SignalReady() => fReadyEvent?.SetEvent();

    /// <summary>Every thread has its message queue from the start; nothing to do.</summary>
    [Delphi(PortStatus.Replaced, Note = "A TThread has its message queue from the start; Windows created one on the first message call.")]
    protected void EnsureMessageQueue()
    {
    }

    /// <summary>Handles a message of the default message loop; true when handled.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual bool ProcessMessage(ref TMsg Msg) => false;

    /// <summary>Creates the window that receives this thread's window messages; none by default.</summary>
    [Delphi(PortStatus.Ported, Note = "An instance method that gives a TThreadWindow, where Delphi's class method created a hidden window.")]
    protected virtual void CreateQueueWindow(ref TThreadWindow? ServerWindow)
    {
    }

    /// <summary>Creates the queue window, or drops it.</summary>
    [Delphi(PortStatus.Ported, Note = "Drops the window; Delphi sent it WM_QUIT, which a window procedure leaves to DefWindowProc.")]
    protected void InitServerWindow(bool bInit)
    {
        if (bInit)
        {
            if (FQueueWindow is null)
            {
                CreateQueueWindow(ref FQueueWindow);
            }
        }
        else
        {
            FQueueWindow = null;
        }
    }

    [Delphi(PortStatus.Ported, Note = "Keeps the ready event for later waiters; Delphi freed it here, before Destroy.")]
    protected override void DoTerminate()
    {
    }

    /// <summary>The default message loop: messages go to <see cref="ProcessMessage"/> until WM_QUIT.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Execute()
    {
        EnsureMessageQueue();
        SignalReady();
        while (!Terminated)
        {
            int res = GetMessage(out TMsg rMsg);
            if (res == -1 || res == 0)
            {
                Terminate();
            }
            else
            {
                ProcessMessage(ref rMsg);
            }
        }
    }

    /// <summary>Waits until the thread runs its message loop; false when the time ran out.</summary>
    [Delphi(PortStatus.Ported)]
    public bool WaitUntilReady(int dwMilliseconds) => fReadyEvent is { } ready && ready.WaitFor(dwMilliseconds) == TWaitResult.wrSignaled;

    /// <summary>Waits until the thread has ended: WAIT_OBJECT_0, or WAIT_TIMEOUT.</summary>
    [Delphi(PortStatus.Ported)]
    public uint WaitUntilSignaled(int dwMilliseconds) => Windows.WaitForSingleObject(Handle, dwMilliseconds);

    /// <summary>Posts a message to the thread.</summary>
    [Delphi(PortStatus.Ported)]
    public void Notify(int Msg) => PostThreadMessage(Msg, 0, 0);

    /// <summary>
    /// Stops the thread: posts WM_QUIT (resuming the thread first if it is suspended) and, when <paramref name="Wait"/>,
    /// waits for it to end (<see cref="WaitForQuit"/>); otherwise waits two seconds at most. True when it ended.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool Quit(bool Wait)
    {
        InitServerWindow(false);
        bool result = false;
        if (Suspended)
        {
            Resume();
            WaitUntilReady(BoldPropagatorConstants.TIMEOUT);
            Thread.Yield();
        }

        if (!Terminated)
        {
            PostThreadMessage(Windows.WM_QUIT, 0, 0);
            result = Wait ? WaitForQuit() : BoldThread.WaitForObject(Handle, BoldPropagatorConstants.TIMEOUT * 2) == TWaitResult.wrSignaled;
        }

        return result;
    }

    /// <summary>
    /// Waits for the thread to end, five seconds at most; a thread that does not end is logged (Delphi killed it; a .NET
    /// thread cannot be killed, and it is a background thread that does not keep the process alive).
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Logs a thread that does not end; Delphi terminated it.")]
    public bool WaitForQuit()
    {
        bool result = false;
        try
        {
            if (ThreadID == Windows.GetCurrentThreadId())
            {
                throw new EBoldInternal("Message queue thread cannot be terminated from within its own thread!!!");
            }

            TWaitResult wr = BoldThread.WaitForObject(Handle, BoldPropagatorConstants.TIMEOUT * 5);
            result = wr == TWaitResult.wrSignaled;
            if (wr != TWaitResult.wrSignaled)
            {
                BoldThreadSafeLog.BoldLogError(BoldCommonConst.sThreadWasForcedTerminated, GetType().Name);
            }
        }
        catch (Exception e)
        {
            BoldThreadSafeLog.BoldLogError(BoldCommonConst.sErrorWaitForQuit, GetType().Name, e.Message);
        }

        return result;
    }

    /// <summary>How long <see cref="Quit"/> waits; kept from Delphi, where it was not used either.</summary>
    [Delphi(PortStatus.Ported)]
    public int QuitWaitTimeOut
    {
        get => FQuitWaitTimeOut;
        set => FQuitWaitTimeOut = value;
    }

    [Delphi(PortStatus.Ported)]
    public TThreadWindow? QueueWindow => FQueueWindow;
}

/// <summary>Routines of BoldThread.</summary>
public static class BoldThread
{
    /// <summary>Waits for a handle: wrSignaled, wrTimeout, wrAbandoned or wrError.</summary>
    [Delphi(PortStatus.Ported)]
    public static TWaitResult WaitForObject(WaitHandle iHandle, int iTimeOut) =>
        Windows.WaitForSingleObject(iHandle, iTimeOut) switch
        {
            Windows.WAIT_OBJECT_0 => TWaitResult.wrSignaled,
            Windows.WAIT_TIMEOUT => TWaitResult.wrTimeout,
            Windows.WAIT_ABANDONED => TWaitResult.wrAbandoned,
            _ => TWaitResult.wrError,
        };
}
