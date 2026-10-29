// Delphi: Source/Common/Core/BoldThreadSafeQueue.pas
// Port map: CSharp/port/units/BoldThreadSafeQueue.md
namespace Bold.Tests;

public class BoldThreadSafeQueueTests
{
    private sealed class Owned : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    [Test]
    [Covers(typeof(TBoldThreadSafeStringQueue), "*")]
    [Covers(typeof(TBoldThreadSafeQueue), "*")]
    [Covers(typeof(TBoldThreadSafeQueueEntry), "*")]
    [Covers(typeof(TBoldQueueEvent))]
    [Covers("BoldThreadSafeQueue", "TBoldThreadSafeQueueStringEntry", "TBoldThreadSafeQueueStringEntry.*")]
    public void A_string_queue_is_first_in_first_out_and_says_when_it_stops_being_empty()
    {
        using var queue = new TBoldThreadSafeStringQueue("Strings");
        var notified = new List<TBoldThreadSafeQueue>();
        queue.OnQueueNotEmpty = q => notified.Add(q);
        Assert.That(queue.OnQueueNotEmpty, Is.Not.Null);
        Assert.That(queue.Empty, Is.True);
        Assert.That(queue.Dequeue(), Is.EqualTo(""), "an empty queue gives an empty string");

        queue.Enqueue("a");
        queue.Enqueue("b");
        queue.Enqueue("c");
        Assert.That(notified, Is.EqualTo(new[] { queue }), "told once, when the first string came");
        Assert.That(queue.Count, Is.EqualTo(3));
        Assert.That(queue.Empty, Is.False);
        var list = new TStringList();
        queue.AppendToStringList(list);
        Assert.That(list.ToArray(), Is.EqualTo(new[] { "c", "b", "a" }), "newest first, and the strings stay queued");
        Assert.That(queue.Dequeue(), Is.EqualTo("a"));
        Assert.That(queue.Dequeue(), Is.EqualTo("b"));
        queue.Enqueue("d");
        Assert.That(notified, Has.Count.EqualTo(1), "the queue was not empty");
        Assert.That(queue.Dequeue(), Is.EqualTo("c"));
        Assert.That(queue.Dequeue(), Is.EqualTo("d"));
        Assert.That(queue.Empty, Is.True);
        queue.Enqueue("e");
        Assert.That(notified, Has.Count.EqualTo(2));
        queue.Clear();
        Assert.That(queue.Count, Is.EqualTo(0));
        Assert.Throws<ArgumentException>(() => queue.Enqueue(""), "an empty string cannot be told from an empty queue");
    }

    [Test]
    [Covers(typeof(TBoldThreadSafeObjectQueue), "*")]
    [Covers("BoldThreadSafeQueue", "TBoldThreadSafeQueueObjectEntry", "TBoldThreadSafeQueueObjectEntry.*")]
    public void An_object_queue_counts_its_objects_and_frees_those_it_owns()
    {
        var owned = new Owned[5];
        for (int i = 0; i < owned.Length; i++)
        {
            owned[i] = new Owned();
        }

        var queue = new TBoldThreadSafeObjectQueue("Objects");
        Assert.That(queue.OwnsObjects, Is.True);
        Assert.That(queue.Dequeue(), Is.Null);
        foreach (Owned o in owned)
        {
            queue.Enqueue(o);
        }

        Assert.That((queue.Count, queue.MaxCount), Is.EqualTo((5, 5)));
        Assert.That(queue.Dequeue(), Is.SameAs(owned[0]));
        Assert.That(owned[0].Disposed, Is.False, "a dequeued object is the caller's");
        var batch = new List<object?> { "already there" };
        queue.DequeueList(batch, 3);
        Assert.That(batch, Is.EqualTo(new object[] { "already there", owned[1], owned[2] }), "up to the maximum, counting what the list held");
        Assert.That((queue.Count, queue.MaxCount), Is.EqualTo((2, 5)));

        queue.Clear();
        Assert.That(owned[3].Disposed && owned[4].Disposed, Is.True, "cleared objects are freed");
        Assert.That(queue.Count, Is.EqualTo(0));
        Assert.That(owned[1].Disposed, Is.False);

        var last = new Owned();
        queue.Enqueue(last);
        queue.Dispose();
        Assert.That(last.Disposed, Is.True, "the queue frees what it still holds");

        var keep = new Owned();
        using var notOwning = new TBoldThreadSafeObjectQueue("Kept", OwnsObjects: false);
        notOwning.Enqueue(keep);
        notOwning.Clear();
        Assert.That(keep.Disposed, Is.False);
        Assert.Throws<ArgumentNullException>(() => notOwning.Enqueue(null!));
    }

    [Test]
    [Covers(typeof(TBoldThreadSafeInterfaceQueue), "*")]
    [Covers("BoldThreadSafeQueue", "TBoldThreadSafeQueueInterfaceEntry", "TBoldThreadSafeQueueInterfaceEntry.*")]
    public void An_interface_queue_hands_back_the_references_in_order()
    {
        using var queue = new TBoldThreadSafeInterfaceQueue("Interfaces");
        int notified = 0;
        queue.OnQueueNotEmpty = _ => notified++;
        IDisposable first = new Owned(), second = new Owned();
        queue.Enqueue(first);
        queue.Enqueue(second);
        Assert.That(notified, Is.EqualTo(1));
        Assert.That(queue.Dequeue(), Is.SameAs(first));
        Assert.That(queue.Dequeue(), Is.SameAs(second));
        Assert.That(queue.Dequeue(), Is.Null);
    }

    [Test]
    public void Threads_can_fill_a_queue_that_another_empties()
    {
        using var queue = new TBoldThreadSafeStringQueue("Shared");
        const int perThread = 2000;
        Task[] writers = [.. Enumerable.Range(0, 4).Select(t => Task.Run(() =>
        {
            for (int i = 0; i < perThread; i++)
            {
                queue.Enqueue($"{t}:{i}");
            }
        }))];
        var read = new List<string>();
        var reader = Task.Run(() =>
        {
            while (read.Count < 4 * perThread)
            {
                string s = queue.Dequeue();
                if (s != "")
                {
                    read.Add(s);
                }
            }
        });
        Assert.That(Task.WaitAll([.. writers, reader], TimeSpan.FromSeconds(30)), Is.True);
        Assert.That(read, Is.Unique);
        for (int t = 0; t < 4; t++)
        {
            int[] order = [.. read.Where(s => s.StartsWith($"{t}:", StringComparison.Ordinal)).Select(s => int.Parse(s[2..], System.Globalization.CultureInfo.InvariantCulture))];
            Assert.That(order, Is.Ordered, "each writer's strings come out in its order");
        }
    }
}
