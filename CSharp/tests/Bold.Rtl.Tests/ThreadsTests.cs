namespace Bold.Tests;

/// <summary>Delphi's TThread with a Windows thread message queue, TThreadTimer and the SyncObjs classes, as the Bold threads use them.</summary>
public class ThreadsTests
{
    /// <summary>A thread with a message loop, as the Bold threads have: it records what it gets and ends at WM_QUIT.</summary>
    private sealed class LoopThread : TThread
    {
        public readonly List<int> Messages = [];
        public readonly ManualResetEventSlim Took = new(false);
        public Action? OnMessage;
        public int RunOn;

        public LoopThread(bool CreateSuspended)
            : base(CreateSuspended)
        {
            Suspended = CreateSuspended;
        }

        protected override void Execute()
        {
            RunOn = Windows.GetCurrentThreadId();
            while (GetMessage(out TMsg msg) != 0)
            {
                if (msg.hwnd is not null)
                {
                    Windows.DispatchMessage(ref msg);
                }
                else
                {
                    lock (Messages)
                    {
                        Messages.Add(msg.message);
                    }

                    OnMessage?.Invoke();
                }

                Took.Set();
            }

            ReturnValue = 7;
        }

        public void RunSynchronized(Action action) => Synchronize(action);
    }

    /// <summary>A thread that waits for a message, looks at it, takes it and looks again.</summary>
    private sealed class PeekThread() : TThread(false)
    {
        public readonly List<(bool Found, TMsg Msg, int Count)> Peeks = [];

        protected override void Execute()
        {
            WaitMessage();
            for (int i = 0; i < 3; i++)
            {
                bool found = PeekMessage(out TMsg msg, i > 0);
                Peeks.Add((found, msg, MessageCount));
            }
        }
    }

    private sealed class FailingThread() : TThread(false)
    {
        public bool Terminating;

        protected override void Execute() => throw new InvalidOperationException("failed");

        protected override void DoTerminate() => Terminating = true;
    }

    private sealed class QueueContext : SynchronizationContext
    {
        public readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback, object?, ManualResetEventSlim)> Work = [];

        public override void Send(SendOrPostCallback d, object? state)
        {
            using var done = new ManualResetEventSlim(false);
            Work.Add((d, state, done));
            done.Wait();
        }

        public void RunOne()
        {
            var (callback, state, done) = Work.Take();
            callback(state);
            done.Set();
        }
    }

    [Test]
    public void A_thread_starts_when_resumed_takes_its_messages_in_order_and_ends_at_WM_QUIT()
    {
        var thread = new LoopThread(true);
        Assert.That((thread.Suspended, thread.Finished, thread.Terminated), Is.EqualTo((true, false, false)));
        Assert.That(thread.PostThreadMessage(Windows.WM_USER + 1, 0, 0), Is.True, "messages wait for the thread");
        Assert.That(thread.PostThreadMessage(Windows.WM_USER + 2, 0, 0), Is.True);
        Assert.That(thread.MessageCount, Is.EqualTo(2));
        thread.Resume();
        Assert.That(SpinWait.SpinUntil(() => thread.MessageCount == 0, 5000), Is.True);
        thread.Terminate();
        Assert.That(thread.Terminated, Is.True);
        thread.PostThreadMessage(Windows.WM_QUIT, 0, 0);
        Assert.That(thread.WaitFor(), Is.EqualTo(7));
        Assert.That(thread.Finished, Is.True);
        Assert.That(Windows.WaitForSingleObject(thread.Handle, 0), Is.EqualTo(Windows.WAIT_OBJECT_0));
        Assert.That(thread.Messages, Is.EqualTo(new[] { Windows.WM_USER + 1, Windows.WM_USER + 2 }));
        Assert.That(thread.RunOn, Is.EqualTo(thread.ThreadID));
        Assert.That(thread.RunOn, Is.Not.EqualTo(Windows.GetCurrentThreadId()));
        Assert.That(thread.PostThreadMessage(Windows.WM_USER, 0, 0), Is.False, "the thread has ended");
        thread.Dispose();
    }

    [Test]
    public void Suspending_stops_the_thread_before_its_next_message()
    {
        var thread = new LoopThread(false);
        thread.PostThreadMessage(Windows.WM_USER, 0, 0);
        Assert.That(thread.Took.Wait(5000), Is.True);
        thread.Took.Reset();
        thread.Suspended = true;
        thread.PostThreadMessage(Windows.WM_USER + 1, 0, 0);
        Assert.That(thread.Took.Wait(200), Is.False, "suspended");
        Assert.That(thread.MessageCount, Is.EqualTo(1));
        thread.Suspended = false;
        Assert.That(thread.Took.Wait(5000), Is.True, "resumed");
        thread.PostThreadMessage(Windows.WM_QUIT, 0, 0);
        thread.WaitFor();
    }

    [Test]
    public void PeekMessage_looks_without_waiting_and_a_window_gets_its_messages_on_its_thread()
    {
        var peeker = new PeekThread();
        peeker.PostThreadMessage(Windows.WM_USER, 3, 4);
        peeker.Resume();
        peeker.WaitFor();
        Assert.That(peeker.Peeks.Select(p => (p.Found, p.Msg.message, p.Msg.wParam, p.Msg.lParam, p.Count)), Is.EqualTo(new[]
        {
            (true, Windows.WM_USER, (nint)3, (nint)4, 1),
            (true, Windows.WM_USER, (nint)3, (nint)4, 0),
            (false, 0, (nint)0, (nint)0, 0),
        }));

        var thread = new LoopThread(true);
        int windowThread = 0;
        var received = new ManualResetEventSlim(false);
        var window = new TThreadWindow(thread, (ref TMsg m) =>
        {
            windowThread = Windows.GetCurrentThreadId();
            received.Set();
        });
        Assert.That(window.Owner, Is.SameAs(thread));
        Assert.That(Windows.PostMessage(null, Windows.WM_USER, 0, 0), Is.False);
        Assert.That(Windows.PostMessage(window, Windows.WM_USER, 0, 0), Is.True);
        thread.Resume();
        Assert.That(received.Wait(5000), Is.True);
        Assert.That(windowThread, Is.EqualTo(thread.ThreadID));
        thread.PostThreadMessage(Windows.WM_QUIT, 0, 0);
        thread.WaitFor();
        Assert.That(Windows.PostMessage(window, Windows.WM_USER, 0, 0), Is.False);
    }

    [Test]
    public void A_timer_ticks_on_its_thread_until_disabled()
    {
        var thread = new LoopThread(false);
        int ticks = 0, tickThread = 0;
        using var timer = new TThreadTimer(thread) { Enabled = false, Interval = 20 };
        timer.OnTimer = _ =>
        {
            tickThread = Windows.GetCurrentThreadId();
            Interlocked.Increment(ref ticks);
        };
        Assert.That((timer.Enabled, timer.Interval), Is.EqualTo((false, 20)));
        Thread.Sleep(100);
        Assert.That(ticks, Is.EqualTo(0));
        timer.Enabled = true;
        Assert.That(SpinWait.SpinUntil(() => ticks >= 3, 5000), Is.True);
        Assert.That(tickThread, Is.EqualTo(thread.ThreadID));
        timer.Interval = 0;
        Thread.Sleep(50);
        int stopped = ticks;
        Thread.Sleep(100);
        Assert.That(ticks, Is.EqualTo(stopped), "an interval of 0 stops it");
        thread.PostThreadMessage(Windows.WM_QUIT, 0, 0);
        thread.WaitFor();
    }

    [Test]
    public void Synchronize_runs_on_the_creating_threads_context_and_an_exception_ends_the_thread()
    {
        var context = new QueueContext();
        SynchronizationContext? old = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        LoopThread thread;
        try
        {
            thread = new LoopThread(false);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(old);
        }

        int ranOn = 0;
        thread.OnMessage = () => thread.RunSynchronized(() => ranOn = Windows.GetCurrentThreadId());
        thread.PostThreadMessage(Windows.WM_USER, 0, 0);
        context.RunOne();
        Assert.That(thread.Took.Wait(5000), Is.True);
        Assert.That(ranOn, Is.EqualTo(Windows.GetCurrentThreadId()), "on the context's thread");
        thread.PostThreadMessage(Windows.WM_QUIT, 0, 0);
        thread.WaitFor();

        var failing = new FailingThread();
        failing.Resume();
        failing.WaitFor();
        Assert.That(failing.FatalException, Is.TypeOf<InvalidOperationException>());
        Assert.That((failing.Terminating, failing.Finished), Is.EqualTo((true, true)));
    }

    [Test]
    public void Events_and_critical_sections()
    {
        using var manual = new TEvent(true, false);
        Assert.That(manual.WaitFor(0), Is.EqualTo(TWaitResult.wrTimeout));
        manual.SetEvent();
        Assert.That((manual.WaitFor(0), manual.WaitFor(0)), Is.EqualTo((TWaitResult.wrSignaled, TWaitResult.wrSignaled)), "stays set");
        manual.ResetEvent();
        Assert.That(Windows.WaitForSingleObject(manual.Handle, 0), Is.EqualTo(Windows.WAIT_TIMEOUT));
        using var auto = new TEvent(false, true);
        Assert.That((auto.WaitFor(0), auto.WaitFor(0)), Is.EqualTo((TWaitResult.wrSignaled, TWaitResult.wrTimeout)), "resets itself");

        using var section = new TCriticalSection();
        section.Enter();
        bool otherGotIt = true;
        var other = new Thread(() => otherGotIt = section.TryEnter());
        other.Start();
        other.Join();
        Assert.That(otherGotIt, Is.False);
        section.Leave();
        section.Acquire();
        Assert.That(section.TryEnter(), Is.True, "reentrant");
        section.Release();
        section.Release();
    }
}
