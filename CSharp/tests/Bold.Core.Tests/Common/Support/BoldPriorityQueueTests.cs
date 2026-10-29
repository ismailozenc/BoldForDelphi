// Delphi: Source/Common/Support/BoldPriorityQueue.pas
// Port map: CSharp/port/units/BoldPriorityQueue.md
namespace Bold.Tests;

[Covers(typeof(TBoldPriorityQueue), "*")]
[Covers(typeof(TBoldPriorityQueueItem), "*")]
public class BoldPriorityQueueTests
{
    private sealed class Job(int priority) : TBoldPriorityQueueItem, IDisposable
    {
        public int Priority => priority;
        public bool Disposed;
        public override bool HasHigherPriorityThan(TBoldPriorityQueueItem Item) => priority > ((Job)Item).Priority;
        public void Dispose() => Disposed = true;
    }

    [Test]
    public void The_head_is_always_the_item_with_the_highest_priority()
    {
        using var queue = new TBoldPriorityQueue();
        int headChanges = 0;
        queue.OnHeadChanged = _ => headChanges++;
        Assert.That(queue.Head, Is.Null);
        Assert.That(queue.ChopHead(), Is.Null);

        int[] priorities = [5, 3, 9, 1, 7, 9, 2, 8, 6, 4];
        foreach (int p in priorities)
        {
            queue.Add(new Job(p));
        }

        Assert.That(queue.Count, Is.EqualTo(priorities.Length));
        Assert.That(((Job)queue.Head!).Priority, Is.EqualTo(9));
        Assert.That(headChanges, Is.GreaterThanOrEqualTo(2), "the 5 and the first 9 came to the head");

        var chopped = new List<int>();
        while (queue.Count > 0)
        {
            chopped.Add(((Job)queue.ChopHead()!).Priority);
        }

        Assert.That(chopped, Is.EqualTo(priorities.OrderByDescending(p => p).ToArray()));
        Assert.That(queue.Head, Is.Null);
    }

    [Test]
    public void Removing_the_head_tells_when_another_item_comes_to_the_head_and_can_free_it()
    {
        using var queue = new TBoldPriorityQueue();
        var low = new Job(1);
        var high = new Job(2);
        queue.Add(low);
        queue.Add(high);
        var heads = new List<TBoldPriorityQueueItem?>();
        queue.OnHeadChanged = _ => heads.Add(queue.Head);
        Assert.That(queue.OnHeadChanged, Is.Not.Null);

        queue.RemoveAndFreeHead();
        Assert.That(high.Disposed, Is.True);
        Assert.That(heads, Is.EqualTo(new[] { low }));
        queue.RemoveHead();
        Assert.That(low.Disposed, Is.False, "RemoveHead leaves the item alone");
        Assert.That(queue.Count, Is.EqualTo(0));
        queue.RemoveHead();
        Assert.That(heads, Has.Count.EqualTo(2), "an empty queue has no head to change");
    }
}
