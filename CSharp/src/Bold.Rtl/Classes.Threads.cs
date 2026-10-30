namespace Bold;

/// <summary>
/// Delphi's TThread, with the Windows thread message queue the Bold threads use: <see cref="PostThreadMessage"/> from
/// any thread, <see cref="GetMessage"/>, <see cref="PeekMessage"/> and <see cref="WaitMessage"/> on the thread itself.
/// </summary>
/// <remarks>
/// <para>
/// The thread starts when it is resumed (<see cref="Resume"/>, or <see cref="Suspended"/> set to false); a constructor
/// that should start it ends with <c>Suspended = CreateSuspended</c>, as Delphi's Bold threads do.
/// </para>
/// <para>
/// Suspending a running thread is cooperative: it stops before it takes its next message. Delphi suspended the thread
/// wherever it was.
/// </para>
/// </remarks>
public abstract class TThread : IDisposable
{
    private readonly Thread fThread;
    private readonly ManualResetEvent fFinished = new(false);
    private readonly ManualResetEventSlim fNotSuspended;
    private readonly Queue<TMsg> fMessages = new();
    private readonly SynchronizationContext? fContext;
    private volatile bool fTerminated;
    private volatile bool fSuspended;
    private volatile bool fStarted;
    private volatile bool fEnded;

    protected TThread(bool CreateSuspended)
    {
        fContext = SynchronizationContext.Current;
        fSuspended = true;
        fNotSuspended = new ManualResetEventSlim(false);
        fThread = new Thread(Run) { IsBackground = true, Name = GetType().Name };
        _ = CreateSuspended;
    }

    /// <summary>The thread's id, which <see cref="Windows.GetCurrentThreadId"/> returns on the thread.</summary>
    public int ThreadID => fThread.ManagedThreadId;

    /// <summary>Signaled when the thread has ended.</summary>
    public WaitHandle Handle => fFinished;

    public int ReturnValue { get; set; }

    /// <summary>Kept for the ported code; the garbage collector frees a thread object.</summary>
    public bool FreeOnTerminate { get; set; }

    public bool Terminated => fTerminated;

    /// <summary>True when the thread has not been started or has been suspended; setting it suspends or resumes.</summary>
    public bool Suspended
    {
        get => fSuspended;
        set
        {
            if (value)
            {
                Suspend();
            }
            else
            {
                Resume();
            }
        }
    }

    /// <summary>True once the thread has run to its end.</summary>
    public bool Finished => fEnded;

    protected abstract void Execute();

    /// <summary>Called on the thread when Execute has returned.</summary>
    protected virtual void DoTerminate()
    {
    }

    /// <summary>Starts the thread, or lets a suspended thread go on.</summary>
    public void Resume()
    {
        fSuspended = false;
        fNotSuspended.Set();
        if (!fStarted)
        {
            fStarted = true;
            fThread.Start();
        }
    }

    /// <summary>Stops the thread before it takes its next message.</summary>
    public void Suspend()
    {
        fSuspended = true;
        fNotSuspended.Reset();
    }

    /// <summary>Asks the thread to end: sets <see cref="Terminated"/>, which Execute checks.</summary>
    public void Terminate() => fTerminated = true;

    /// <summary>Waits until the thread has ended and returns its <see cref="ReturnValue"/>.</summary>
    public int WaitFor()
    {
        if (fStarted)
        {
            fFinished.WaitOne();
        }

        return ReturnValue;
    }

    /// <summary>
    /// Runs <paramref name="Method"/> on the thread that created this thread object, through its synchronization context
    /// (the GUI thread in an application), and waits for it; without a synchronization context it runs here.
    /// </summary>
    protected void Synchronize(Action Method)
    {
        if (fContext is null || SynchronizationContext.Current == fContext)
        {
            Method();
        }
        else
        {
            fContext.Send(_ => Method(), null);
        }
    }

    private void Run()
    {
        try
        {
            Execute();
        }
        catch (Exception e)
        {
            FatalException = e;
        }
        finally
        {
            try
            {
                DoTerminate();
            }
            finally
            {
                fEnded = true;
                fFinished.Set();
            }
        }
    }

    /// <summary>The exception that ended Execute, if one did (Delphi's FatalException).</summary>
    public Exception? FatalException { get; private set; }

    /// <summary>Posts a message to the thread's queue. False once the thread has ended.</summary>
    public bool PostThreadMessage(int Msg, nint wParam, nint lParam) =>
        PostMessage(new TMsg { message = Msg, wParam = wParam, lParam = lParam });

    internal bool PostMessage(TMsg Msg)
    {
        if (fEnded)
        {
            return false;
        }

        lock (fMessages)
        {
            fMessages.Enqueue(Msg);
            Monitor.PulseAll(fMessages);
        }

        return true;
    }

    /// <summary>
    /// Waits for the next message and takes it, as Windows' GetMessage: 0 for WM_QUIT, otherwise 1. A suspended thread
    /// waits until it is resumed.
    /// </summary>
    protected int GetMessage(out TMsg Msg)
    {
        fNotSuspended.Wait();
        lock (fMessages)
        {
            while (fMessages.Count == 0)
            {
                Monitor.Wait(fMessages);
            }

            Msg = fMessages.Dequeue();
        }

        return Msg.message == Windows.WM_QUIT ? 0 : 1;
    }

    /// <summary>Looks at the next message without waiting; takes it when <paramref name="Remove"/> is true. False when there is none.</summary>
    protected bool PeekMessage(out TMsg Msg, bool Remove)
    {
        fNotSuspended.Wait();
        lock (fMessages)
        {
            if (fMessages.Count == 0)
            {
                Msg = default;
                return false;
            }

            Msg = Remove ? fMessages.Dequeue() : fMessages.Peek();
            return true;
        }
    }

    /// <summary>Waits until a message is in the queue.</summary>
    protected void WaitMessage()
    {
        lock (fMessages)
        {
            while (fMessages.Count == 0)
            {
                Monitor.Wait(fMessages);
            }
        }
    }

    /// <summary>The number of messages waiting.</summary>
    public int MessageCount
    {
        get
        {
            lock (fMessages)
            {
                return fMessages.Count;
            }
        }
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
