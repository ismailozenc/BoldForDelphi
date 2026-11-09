// New in the port: the environment of a Bold client without a GUI message loop. Delphi Bold's freestanding environment
// supports no queue, so a console program could not take in what other clients changed; WinForms applications have
// TBoldVCLEnvironmentConfiguration, whose queue runs when the application is idle.
using System.Diagnostics;

namespace Bold;

/// <summary>
/// The environment of a Bold client without a WinForms message loop: a console program, a service, a worker. Bold hands
/// some work to the thread that runs the client (the dequeuers that bring in what other clients changed, the ticks of a
/// TTimer, a thread's Synchronize); here that work waits in a queue until the thread calls <see cref="ProcessMessages"/>,
/// or waits in <see cref="WaitUntil"/>. <see cref="Install"/> makes it the environment of the calling thread.
/// </summary>
public sealed class TBoldConsoleEnvironmentConfiguration : TBoldEnvironmentConfiguration
{
    /// <summary>Bold's queue: nothing to display without a GUI; queued dequeuers wake the waiting thread.</summary>
    private sealed class ConsoleQueue : TBoldQueue
    {
        public override void ActivateDisplayQueue()
        {
        }

        public override void DeActivateDisplayQueue()
        {
        }

        protected override void EnsureDequeing() => (BoldEnvironment.BoldEffectiveEnvironment() as TBoldConsoleEnvironmentConfiguration)?.Wake();
    }

    /// <summary>The thread's synchronization context: what other threads post or send runs when the thread processes messages.</summary>
    private sealed class ConsoleSynchronizationContext(TBoldConsoleEnvironmentConfiguration Environment) : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State, ManualResetEventSlim? Done)> fWork = new();

        public int ThreadId { get; } = System.Environment.CurrentManagedThreadId;

        public override void Post(SendOrPostCallback d, object? state)
        {
            lock (fWork)
            {
                fWork.Enqueue((d, state, null));
            }

            Environment.Wake();
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            if (System.Environment.CurrentManagedThreadId == ThreadId)
            {
                d(state);
                return;
            }

            using var done = new ManualResetEventSlim(false);
            lock (fWork)
            {
                fWork.Enqueue((d, state, done));
            }

            Environment.Wake();
            done.Wait();
        }

        public override SynchronizationContext CreateCopy() => this;

        /// <summary>Runs what was posted and sent; true when there was something.</summary>
        public bool RunPosted()
        {
            bool any = false;
            while (true)
            {
                (SendOrPostCallback Callback, object? State, ManualResetEventSlim? Done) work;
                lock (fWork)
                {
                    if (fWork.Count == 0)
                    {
                        return any;
                    }

                    work = fWork.Dequeue();
                }

                any = true;
                try
                {
                    work.Callback(work.State);
                }
                finally
                {
                    work.Done?.Set();
                }
            }
        }
    }

    private readonly ConsoleSynchronizationContext fContext;
    private readonly AutoResetEvent fWakeUp = new(false);
    private SynchronizationContext? fPreviousContext;
    private TBoldEnvironmentConfiguration? fPreviousConfiguration;
    private bool fInstalled;

    /// <summary>An environment for the calling thread; <see cref="Install"/> makes one and installs it.</summary>
    public TBoldConsoleEnvironmentConfiguration()
    {
        fContext = new ConsoleSynchronizationContext(this);
    }

    /// <summary>
    /// Makes a console environment the environment of Bold and of the calling thread (its synchronization context). Install
    /// it before the handles are made, as they take the thread's context when they are; dispose of it to uninstall it.
    /// </summary>
    public static TBoldConsoleEnvironmentConfiguration Install()
    {
        var environment = new TBoldConsoleEnvironmentConfiguration();
        environment.fPreviousContext = SynchronizationContext.Current;
        environment.fPreviousConfiguration = BoldEnvironment.BoldInternalCustomConfiguration;
        SynchronizationContext.SetSynchronizationContext(environment.fContext);
        BoldEnvironment.BoldInternalCustomConfiguration = environment;
        environment.fInstalled = true;
        return environment;
    }

    /// <summary>The synchronization context of the thread the environment was made on.</summary>
    public SynchronizationContext SynchronizationContext => fContext;

    protected override string GetName() => "Console";

    protected override TBoldQueueClass GetQueueClass() => typeof(ConsoleQueue);

    protected override object? GetRootComponent() => null;

    /// <summary>Wakes a thread that waits in <see cref="WaitUntil"/>: Bold has queued work for it.</summary>
    public override void TriggerQueueMechanism() => Wake();

    private void Wake() => fWakeUp.Set();

    /// <summary>
    /// Runs the work other threads posted to this thread, and Bold's queue: the dequeuers (such as the event handler that
    /// brings in other clients' changes) and what waits to be displayed. It goes on until nothing is left. Call it on the
    /// thread the environment was installed on.
    /// </summary>
    public override void ProcessMessages()
    {
        bool any;
        do
        {
            any = fContext.RunPosted();
            if (Queue is { } queue)
            {
                queue.PerformPreDisplayQueue();
                if (queue.DisplayListCount > 0)
                {
                    TBoldQueueable.DisplayAll();
                }
            }
        }
        while (any);
    }

    /// <summary>
    /// Processes messages until <paramref name="Condition"/> holds or <paramref name="Timeout"/> has passed, sleeping while
    /// there is nothing to do; true when the condition holds.
    /// </summary>
    public bool WaitUntil(Func<bool> Condition, TimeSpan Timeout)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            ProcessMessages();
            if (Condition())
            {
                return true;
            }

            TimeSpan left = Timeout - watch.Elapsed;
            if (left <= TimeSpan.Zero)
            {
                return false;
            }

            fWakeUp.WaitOne(left < TimeSpan.FromMilliseconds(50) ? left : TimeSpan.FromMilliseconds(50));
        }
    }

    /// <summary>Processes messages for <paramref name="Duration"/>, as a program's main loop does between its own work.</summary>
    public void Run(TimeSpan Duration) => WaitUntil(() => false, Duration);

    /// <summary>Processes messages until <paramref name="Token"/> is cancelled: a service's main loop.</summary>
    public void Run(CancellationToken Token)
    {
        using CancellationTokenRegistration registration = Token.Register(Wake);
        WaitUntil(() => Token.IsCancellationRequested, TimeSpan.MaxValue);
    }

    public override void BringToFront()
    {
        // No window to bring to the front.
    }

    public override void FocusMainForm()
    {
        // No form to focus.
    }

    /// <summary>Uninstalls the environment (when it was installed) and frees its queue.</summary>
    public override void Dispose()
    {
        if (fInstalled)
        {
            fInstalled = false;
            if (BoldEnvironment.BoldInternalCustomConfiguration == this)
            {
                BoldEnvironment.BoldInternalCustomConfiguration = fPreviousConfiguration;
            }

            if (SynchronizationContext.Current == fContext)
            {
                SynchronizationContext.SetSynchronizationContext(fPreviousContext);
            }
        }

        base.Dispose();
        fWakeUp.Dispose();
    }
}
