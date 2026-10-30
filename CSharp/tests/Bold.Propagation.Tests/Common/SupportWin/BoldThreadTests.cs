// Delphi: Source/Common/SupportWin/BoldThread.pas
// Port map: CSharp/port/units/BoldThread.md
namespace Bold.Tests;

[Covers(typeof(TBoldNotifiableThread), "*")]
[Covers(typeof(BoldThread), "*")]
public class BoldThreadTests
{
    private const int Ping = Windows.WM_USER + 100;

    private sealed class Worker() : TBoldNotifiableThread(false)
    {
        public readonly List<int> Handled = [];
        public readonly List<int> WindowMessages = [];
        public int HandlerThread;

        protected override bool ProcessMessage(ref TMsg Msg)
        {
            HandlerThread = Windows.GetCurrentThreadId();
            if (Msg.hwnd is not null)
            {
                Windows.DispatchMessage(ref Msg);
                return true;
            }

            lock (Handled)
            {
                Handled.Add(Msg.message);
            }

            return true;
        }

        protected override void CreateQueueWindow(ref TThreadWindow? ServerWindow) =>
            ServerWindow = new TThreadWindow(this, (ref TMsg m) => WindowMessages.Add(m.message));

        public void MakeWindow() => InitServerWindow(true);
    }

    private sealed class Stuck() : TBoldNotifiableThread(false)
    {
        public readonly ManualResetEventSlim Release = new(false);

        protected override void Execute()
        {
            SignalReady();
            Release.Wait();
        }
    }

    [Test]
    public void A_notifiable_thread_handles_its_messages_on_its_thread_and_quits()
    {
        using var worker = new Worker();
        Assert.That(worker.Suspended, Is.False, "created running");
        Assert.That(worker.WaitUntilReady(5000), Is.True);
        Assert.That(worker.QuitWaitTimeOut, Is.EqualTo(BoldPropagatorConstants.TIMEOUT * 5));
        worker.QuitWaitTimeOut = 100;
        Assert.That(worker.QuitWaitTimeOut, Is.EqualTo(100));
        worker.Notify(Ping);
        worker.Notify(Ping + 1);
        Assert.That(SpinWait.SpinUntil(() => worker.Handled.Count == 2, 5000), Is.True);
        Assert.That(worker.Handled, Is.EqualTo(new[] { Ping, Ping + 1 }));
        Assert.That(worker.HandlerThread, Is.EqualTo(worker.ThreadID));
        Assert.That(worker.HandlerThread, Is.Not.EqualTo(Windows.GetCurrentThreadId()));

        Assert.That(worker.QueueWindow, Is.Null);
        worker.MakeWindow();
        Assert.That(worker.QueueWindow, Is.Not.Null);
        Windows.PostMessage(worker.QueueWindow, Ping + 2, 0, 0);
        Assert.That(SpinWait.SpinUntil(() => worker.WindowMessages.Count == 1, 5000), Is.True, "the window's procedure gets its messages");

        Assert.That(worker.WaitUntilSignaled(0), Is.EqualTo(Windows.WAIT_TIMEOUT));
        Assert.That(worker.Quit(true), Is.True);
        Assert.That(worker.QueueWindow, Is.Null, "quitting drops the window");
        Assert.That(worker.WaitUntilSignaled(0), Is.EqualTo(Windows.WAIT_OBJECT_0));
        Assert.That(BoldThread.WaitForObject(worker.Handle, 0), Is.EqualTo(TWaitResult.wrSignaled));
        Assert.That(worker.ReturnValue, Is.EqualTo(-10));
    }

    [Test]
    public void A_suspended_thread_is_started_to_quit_and_a_thread_that_does_not_end_is_reported()
    {
        using (var suspended = new TBoldNotifiableThread(true))
        {
            Assert.That(suspended.Suspended, Is.True);
            Assert.That(suspended.WaitUntilReady(50), Is.False, "not started");
            Assert.That(suspended.Quit(false), Is.True, "resumed, then quit");
        }

        using var stuck = new Stuck();
        Assert.That(stuck.WaitUntilReady(5000), Is.True);
        Assert.That(BoldThread.WaitForObject(stuck.Handle, 10), Is.EqualTo(TWaitResult.wrTimeout));
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Assert.That(stuck.WaitForQuit(), Is.False, "the thread did not end within five seconds");
        Assert.That(watch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(4000));
        stuck.Release.Set();
        Assert.That(stuck.WaitUntilSignaled(5000), Is.EqualTo(Windows.WAIT_OBJECT_0));
    }
}
