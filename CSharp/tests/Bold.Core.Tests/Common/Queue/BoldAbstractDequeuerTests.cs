// Delphi: Source/Common/Queue/BoldAbstractDequeuer.pas
// Port map: CSharp/port/units/BoldAbstractDequeuer.md
namespace Bold.Tests;

[NonParallelizable]
public class BoldAbstractDequeuerTests
{
    private sealed class TestQueue : TBoldQueue
    {
        public override void ActivateDisplayQueue()
        {
        }

        public override void DeActivateDisplayQueue()
        {
        }
    }

    private sealed class TestEnvironment : TBoldEnvironmentConfiguration
    {
        protected override string GetName() => "Test";
        protected override Type GetQueueClass() => typeof(TestQueue);
        protected override object? GetRootComponent() => null;
    }

    private sealed class Dequeuer : TBoldAbstractDequeuer
    {
        public readonly List<string> Handled = [];
        protected override void HandleMessage(string aMsg) => Handled.Add(aMsg);
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
    [Covers(typeof(TBoldAbstractDequeuer), "*")]
    public void Queued_messages_are_handled_in_order_before_the_next_display()
    {
        using var queue = new TBoldThreadSafeStringQueue("Messages");
        var dequeuer = new Dequeuer();
        Assert.DoesNotThrow(() => dequeuer.DequeueAll(), "nothing to do without a queue");
        dequeuer.Queue = queue;
        Assert.That(dequeuer.Queue, Is.SameAs(queue));
        queue.OnQueueNotEmpty = _ => dequeuer.QueueNotEmpty();

        queue.Enqueue("one");
        queue.Enqueue("two");
        Assert.That(dequeuer.Handled, Is.Empty, "handled when the Bold queue gets to it");
        BoldQueue.BoldInstalledQueue()!.PerformPreDisplayQueue();
        Assert.That(dequeuer.Handled, Is.EqualTo(new[] { "one", "two" }));
        Assert.That(queue.Empty, Is.True);

        // A dequeuer that goes takes its request with it.
        queue.Enqueue("three");
        dequeuer.Dispose();
        BoldQueue.BoldInstalledQueue()!.PerformPreDisplayQueue();
        Assert.That(dequeuer.Handled, Has.Count.EqualTo(2));
        Assert.That(queue.Dequeue(), Is.EqualTo("three"));
    }
}
