namespace Bold.Tests;

/// <summary>The TTimer emulation: it ticks through the synchronization context of the thread that started it.</summary>
public class ExtCtrlsTests
{
    private sealed class QueueContext : SynchronizationContext
    {
        public readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback, object?)> Posted = [];

        public override void Post(SendOrPostCallback d, object? state) => Posted.Add((d, state));
    }

    [Test]
    public void A_timer_ticks_on_its_threads_synchronization_context()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        var context = new QueueContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            int ticks = 0;
            using var timer = new TTimer { Interval = 10 };
            Assert.That(timer.Running, Is.False, "nothing to call yet");
            timer.OnTimer = _ => ticks++;
            Assert.That(timer.Running, Is.True);
            Assert.That(timer.Enabled, Is.True);
            Assert.That(timer.Interval, Is.EqualTo(10));
            Assert.That(timer.OnTimer, Is.Not.Null);
            Assert.That(context.Posted.TryTake(out var posted, 5000), Is.True, "the tick is posted to the context");
            posted.Item1(posted.Item2);
            Assert.That(ticks, Is.EqualTo(1), "and runs there");
            timer.Enabled = false;
            Assert.That(timer.Running, Is.False);
            posted.Item1(posted.Item2);
            Assert.That(ticks, Is.EqualTo(1), "a disabled timer does not tick");
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    [Test]
    public void Without_a_synchronization_context_a_timer_does_not_run()
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(null);
        try
        {
            using var timer = new TTimer { OnTimer = _ => { } };
            Assert.That(timer.Running, Is.False);
            timer.Interval = 0;
            Assert.That(timer.Running, Is.False);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }
}
