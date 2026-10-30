namespace Bold;

/// <summary>
/// The parts of Delphi's Windows unit the propagator code uses: HRESULT and wait constants, thread messages and the
/// "windows" messages are dispatched to. Messages are managed: each <see cref="TThread"/> has its own queue, and a
/// <see cref="TThreadWindow"/> stands for a window that belongs to a thread.
/// </summary>
public static class Windows
{
    public const int S_OK = 0;
    public const int S_FALSE = 1;
    public const int E_FAIL = unchecked((int)0x80004005);

    public const int WM_QUIT = 0x0012;
    public const int WM_TIMER = 0x0113;
    public const int WM_USER = 0x0400;

    public const int INFINITE = Timeout.Infinite;
    public const uint WAIT_OBJECT_0 = 0;
    public const uint WAIT_ABANDONED = 0x80;
    public const uint WAIT_TIMEOUT = 0x102;
    public const uint WAIT_FAILED = 0xFFFFFFFF;

    /// <summary>Passes a message to the window it was sent to (<see cref="TMsg.hwnd"/>); messages without a window are dropped.</summary>
    public static void DispatchMessage(ref TMsg Msg) => Msg.hwnd?.WndProc(ref Msg);

    /// <summary>Posts a message to a window: it goes to the queue of the thread the window belongs to. False when that thread has ended.</summary>
    public static bool PostMessage(TThreadWindow? Window, int Msg, nint wParam, nint lParam) =>
        Window is not null && Window.Owner.PostMessage(new TMsg { hwnd = Window, message = Msg, wParam = wParam, lParam = lParam });

    /// <summary>Waits for a handle, as WaitForSingleObject: WAIT_OBJECT_0 when signaled, WAIT_TIMEOUT when the time ran out.</summary>
    public static uint WaitForSingleObject(WaitHandle Handle, int Milliseconds)
    {
        try
        {
            return Handle.WaitOne(Milliseconds) ? WAIT_OBJECT_0 : WAIT_TIMEOUT;
        }
        catch (AbandonedMutexException)
        {
            return WAIT_ABANDONED;
        }
        catch (ObjectDisposedException)
        {
            return WAIT_FAILED;
        }
    }

    /// <summary>The id of the calling thread, comparable with <see cref="TThread.ThreadID"/>.</summary>
    public static int GetCurrentThreadId() => Environment.CurrentManagedThreadId;
}

/// <summary>A thread message, as Windows' MSG.</summary>
public struct TMsg
{
    public TThreadWindow? hwnd;
    public int message;
    public nint wParam;
    public nint lParam;
}

/// <summary>Handles the messages sent to a window.</summary>
public delegate void TWndMethod(ref TMsg Message);

/// <summary>
/// A window that belongs to a thread, as a hidden window created by a thread: messages posted to it go to that
/// thread's queue, and <see cref="Windows.DispatchMessage"/> passes them to its window procedure there.
/// </summary>
public sealed class TThreadWindow(TThread Owner, TWndMethod WndProc)
{
    public TThread Owner { get; } = Owner;

    private readonly TWndMethod fWndProc = WndProc;

    public void WndProc(ref TMsg Message) => fWndProc(ref Message);
}

/// <summary>
/// Delphi's TTimer for a thread with a message loop: every Interval milliseconds a WM_TIMER message goes to the
/// owning thread's queue, and OnTimer runs on that thread when the message is dispatched. A tick is not posted while
/// the previous one waits in the queue.
/// </summary>
public sealed class TThreadTimer : IDisposable
{
    private readonly TThreadWindow fWindow;
    private readonly object fLock = new();
    private Timer? fTimer;
    private bool fEnabled = true;
    private int fInterval = 1000;
    private bool fTickPending;
    private bool fDisposed;

    public TThreadTimer(TThread Owner)
    {
        fWindow = new TThreadWindow(Owner, TimerWndProc);
    }

    public TNotifyEvent? OnTimer { get; set; }

    public bool Enabled
    {
        get => fEnabled;
        set
        {
            lock (fLock)
            {
                fEnabled = value;
                UpdateTimer();
            }
        }
    }

    /// <summary>The time between ticks in milliseconds; 0 stops the timer.</summary>
    public int Interval
    {
        get => fInterval;
        set
        {
            lock (fLock)
            {
                fInterval = value;
                UpdateTimer();
            }
        }
    }

    private void UpdateTimer()
    {
        fTimer?.Dispose();
        fTimer = null;
        fTickPending = false;
        if (fEnabled && fInterval > 0 && !fDisposed)
        {
            fTimer = new Timer(Tick, null, fInterval, fInterval);
        }
    }

    private void Tick(object? state)
    {
        lock (fLock)
        {
            if (fTickPending || !fEnabled || fDisposed)
            {
                return;
            }

            fTickPending = Windows.PostMessage(fWindow, Windows.WM_TIMER, 0, 0);
        }
    }

    private void TimerWndProc(ref TMsg Message)
    {
        if (Message.message != Windows.WM_TIMER)
        {
            return;
        }

        lock (fLock)
        {
            if (!fTickPending)
            {
                return;
            }

            fTickPending = false;
            if (!fEnabled || fDisposed)
            {
                return;
            }
        }

        OnTimer?.Invoke(this);
    }

    public void Dispose()
    {
        lock (fLock)
        {
            fDisposed = true;
            fTimer?.Dispose();
            fTimer = null;
        }
    }
}
