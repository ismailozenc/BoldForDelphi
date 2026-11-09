// New in the port: Source/Common/Environment/BoldConsoleEnvironment.cs, the environment of a Bold client without a GUI.
namespace Bold.Tests;

[NonParallelizable]
public class BoldConsoleEnvironmentTests
{
    private sealed class Worker(Action action) : TThread(true)
    {
        protected override void Execute() => Synchronize(action);
    }

    private TBoldConsoleEnvironmentConfiguration fEnvironment = null!;
    private SynchronizationContext? fOldContext;

    [SetUp]
    public void Install()
    {
        fOldContext = SynchronizationContext.Current;
        fEnvironment = TBoldConsoleEnvironmentConfiguration.Install();
    }

    [TearDown]
    public void Uninstall() => fEnvironment.Dispose();

    [Test]
    public void Installed_it_is_Bolds_environment_and_the_threads_context_until_disposed()
    {
        Assert.That(BoldEnvironment.BoldEffectiveEnvironment(), Is.SameAs(fEnvironment));
        Assert.That(SynchronizationContext.Current, Is.SameAs(fEnvironment.SynchronizationContext));
        Assert.That(fEnvironment.Name, Is.EqualTo("Console"));
        Assert.That(BoldQueue.BoldInstalledQueue(), Is.Not.Null, "it has a queue");
        Assert.DoesNotThrow(fEnvironment.BringToFront);
        Assert.DoesNotThrow(fEnvironment.FocusMainForm);

        fEnvironment.Dispose();
        Assert.That(BoldEnvironment.BoldInternalCustomConfiguration, Is.Null);
        Assert.That(SynchronizationContext.Current, Is.SameAs(fOldContext));
        fEnvironment = TBoldConsoleEnvironmentConfiguration.Install();
    }

    [Test]
    public void Work_posted_from_another_thread_runs_on_this_one_when_it_processes_messages()
    {
        int thread = Environment.CurrentManagedThreadId;
        int ranOn = 0;
        Task.Run(() => fEnvironment.SynchronizationContext.Post(_ => ranOn = Environment.CurrentManagedThreadId, null)).Wait();
        Assert.That(ranOn, Is.Zero, "not before the thread processes messages");
        fEnvironment.ProcessMessages();
        Assert.That(ranOn, Is.EqualTo(thread));
    }

    [Test]
    public void A_threads_Synchronize_waits_until_this_thread_runs_it()
    {
        bool ran = false;
        using var worker = new Worker(() => ran = true);
        worker.Resume();
        Assert.That(fEnvironment.WaitUntil(() => ran, TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(worker.Handle.WaitOne(TimeSpan.FromSeconds(10)), Is.True, "the thread goes on once its method has run");
    }

    [Test]
    public void Bolds_pre_display_queue_runs_with_the_messages()
    {
        var ran = new List<object?>();
        TBoldQueueable.AddToPreDisplayQueue(sender => ran.Add(sender), "dequeuer", this);
        Assert.That(ran, Is.Empty);
        fEnvironment.ProcessMessages();
        Assert.That(ran, Is.EqualTo(new object?[] { "dequeuer" }));
    }

    [Test]
    public void Triggering_the_queue_wakes_a_waiting_thread()
    {
        bool woken = false;
        Task.Run(() =>
        {
            Thread.Sleep(100);
            fEnvironment.SynchronizationContext.Post(_ => woken = true, null);
            fEnvironment.TriggerQueueMechanism();
        });
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Assert.That(fEnvironment.WaitUntil(() => woken, TimeSpan.FromSeconds(10)), Is.True);
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void A_timer_ticks_while_the_thread_waits_in_the_environment()
    {
        int ticks = 0;
        using var timer = new TTimer { Interval = 20, OnTimer = _ => ticks++ };
        timer.Enabled = true;
        Assert.That(fEnvironment.WaitUntil(() => ticks >= 3, TimeSpan.FromSeconds(10)), Is.True);
        timer.Enabled = false;
    }

    [Test]
    public void Waiting_ends_when_the_time_is_up_or_the_token_is_cancelled()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        Assert.That(fEnvironment.WaitUntil(() => false, TimeSpan.FromMilliseconds(200)), Is.False);
        Assert.That(watch.Elapsed, Is.GreaterThanOrEqualTo(TimeSpan.FromMilliseconds(190)));
        fEnvironment.Run(TimeSpan.FromMilliseconds(50));

        using var cancel = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        watch.Restart();
        fEnvironment.Run(cancel.Token);
        Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
    }
}
