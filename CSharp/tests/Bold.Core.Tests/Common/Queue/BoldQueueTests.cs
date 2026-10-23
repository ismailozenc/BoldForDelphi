// Delphi: Source/Common/Queue/BoldQueue.pas, Source/Common/Environment/BoldEnvironment.pas
// Port map: CSharp/port/units/BoldQueue.md
namespace Bold.Tests;

[NonParallelizable]
public class BoldQueueTests
{
    private sealed class TestQueue : TBoldQueue
    {
        public bool Active;
        public override void ActivateDisplayQueue() => Active = true;
        public override void DeActivateDisplayQueue() => Active = false;
    }

    private sealed class TestEnvironment : TBoldEnvironmentConfiguration
    {
        protected override string GetName() => "Test";
        protected override Type GetQueueClass() => typeof(TestQueue);
        protected override object? GetRootComponent() => null;
    }

    private sealed class Item(string name, List<string> log, object? match = null) : TBoldQueueable(match)
    {
        public bool FailApply;
        public void QueueDisplay() => AddToDisplayList();
        public void QueueApply() => AddToApplyList();
        public override void Display()
        {
            log.Add("display " + name);
            RemoveFromDisplayList();
        }

        public override void Apply()
        {
            log.Add("apply " + name);
            if (!FailApply)
            {
                RemoveFromApplyList();
            }
        }

        public override void DiscardChange()
        {
            log.Add("discard " + name);
            RemoveFromApplyList();
        }
    }

    private TestEnvironment env = null!;

    [SetUp]
    public void Install()
    {
        env = new TestEnvironment();
        BoldEnvironment.BoldInternalCustomConfiguration = env;
    }

    [TearDown]
    public void Uninstall()
    {
        BoldEnvironment.BoldInternalCustomConfiguration = null;
        env.Dispose();
    }

    [Test]
    [Covers(typeof(BoldQueue), "*")]
    [Covers(typeof(TBoldQueueDisplayMode))]
    public void Flags_and_modes_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldQueue), "Source/Common/Queue/BoldQueue.pas");
        PortAssert.EnumsMatchDelphi("Source/Common/Queue/BoldQueue.pas", typeof(TBoldQueueDisplayMode));
        Assert.That(BoldQueue.BoldInstalledQueue(), Is.InstanceOf<TestQueue>());
        Assert.That(((TestQueue)BoldQueue.BoldInstalledQueue()!).Active, Is.True);
        Assert.That(BoldQueue.BoldQueueFinalized(), Is.False);
    }

    [Test]
    [Covers(typeof(TBoldQueueable), "*")]
    [Covers(typeof(TBoldQueue), "*")]
    public void Display_list_shows_prioritized_items_first()
    {
        var log = new List<string>();
        var parent = new Item("parent", log);
        var child = new Item("child", log) { PrioritizedQueuable = parent };
        parent.QueueDisplay();
        child.QueueDisplay();
        Assert.That(child.IsInDisplayList, Is.True);
        Assert.That(TBoldQueueable.DisplayAll(), Is.True);
        Assert.That(log, Is.EqualTo(new[] { "display parent", "display child" }), "the child waits for its prioritized parent");
        Assert.That(TBoldQueueable.DisplayAll(), Is.False);
        Assert.That(BoldQueue.BoldInstalledQueue()!.DisplayListCount, Is.EqualTo(0));
    }

    [Test]
    [Covers(typeof(TBoldQueueable), nameof(TBoldQueueable.ApplyAll), nameof(TBoldQueueable.ApplyAllMatching),
        nameof(TBoldQueueable.DiscardChangeAll), nameof(TBoldQueueable.DiscardChangeAllMatching))]
    public void Apply_list_applies_and_keeps_failures()
    {
        var log = new List<string>();
        object m = new();
        var a = new Item("a", log, m);
        var b = new Item("b", log) { FailApply = true };
        var c = new Item("c", log, m);
        a.QueueApply();
        b.QueueApply();
        c.QueueApply();
        TBoldQueueable.ApplyAllMatching(m);
        Assert.That(log, Is.EqualTo(new[] { "apply c", "apply a" }));
        log.Clear();
        TBoldQueueable.ApplyAll();
        Assert.That(log, Is.EqualTo(new[] { "apply b" }));
        Assert.That(BoldQueue.BoldInstalledQueue()!.ApplyListCount, Is.GreaterThan(0), "the failed apply stays queued");
        log.Clear();
        TBoldQueueable.DiscardChangeAll();
        Assert.That(log, Is.EqualTo(new[] { "discard b" }));
        var d = new Item("d", log, m);
        d.QueueApply();
        TBoldQueueable.DiscardChangeAllMatching(m);
        Assert.That(log[^1], Is.EqualTo("discard d"));
    }

    [Test]
    [Covers(typeof(TBoldQueueable), nameof(TBoldQueueable.AddToPreDisplayQueue), nameof(TBoldQueueable.RemoveFromPreDisplayQueue))]
    [Covers(typeof(TBoldQueue), nameof(TBoldQueue.PerformPreDisplayQueue))]
    public void Pre_display_events_run_on_demand()
    {
        var log = new List<string>();
        object receiver = new();
        TBoldQueueable.AddToPreDisplayQueue(_ => log.Add("kept"), null, null);
        TBoldQueueable.AddToPreDisplayQueue(_ => log.Add("removed"), null, receiver);
        TBoldQueueable.RemoveFromPreDisplayQueue(receiver);
        BoldQueue.BoldInstalledQueue()!.PerformPreDisplayQueue();
        Assert.That(log, Is.EqualTo(new[] { "kept" }));
    }

    [Test]
    [Covers(typeof(TBoldEnvironmentConfiguration), "*")]
    [Covers(typeof(TBoldFreestandingEnvironmentConfiguration), "*")]
    [Covers(typeof(BoldEnvironment), nameof(BoldEnvironment.BoldEffectiveEnvironment), nameof(BoldEnvironment.BoldEnvironmentsFinalized),
        nameof(BoldEnvironment.BoldInternalCustomConfiguration), nameof(BoldEnvironment.BoldInternalFreestandingConfiguration), nameof(BoldEnvironment.BoldInternalRunningInIDE))]
    [Covers(typeof(TBoldApplicationSubscriber), "*")]
    public void Environments_choose_the_custom_one_and_free_their_queue()
    {
        Assert.That(BoldEnvironment.BoldEffectiveEnvironment(), Is.SameAs(env));
        Assert.That(env.Name, Is.EqualTo("Test"));
        Assert.That(env.RunningInIDE, Is.False);
        Assert.That(BoldEnvironment.BoldEnvironmentsFinalized(), Is.False);
        Assert.Throws<EBoldInternal>(() => env.AskUser("?"));
        Assert.Throws<EBoldInternal>(() => env.ProcessMessages());
        var free = BoldEnvironment.BoldInternalFreestandingConfiguration!;
        Assert.That(free.Name, Is.EqualTo("Freestanding"));
        Assert.DoesNotThrow(() => free.BringToFront());
        Assert.Throws<EBoldInternal>(() => _ = free.Queue, "freestanding has no queue class");

        var queue = (TestQueue)env.Queue!;
        env.Dispose();
        Assert.That(env.QueueFinalized, Is.True);
        Assert.That(env.Queue, Is.Null);
        Assert.That(BoldQueue.BoldQueueFinalized(), Is.True);

        var sub = new TBoldApplicationSubscriber();
        bool gone = false;
        sub.OnApplicationDestroyed = () => gone = true;
        sub.Dispose();
        Assert.That(gone, Is.True);
        _ = queue;
    }
}
