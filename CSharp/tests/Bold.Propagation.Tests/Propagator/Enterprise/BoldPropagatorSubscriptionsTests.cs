// Delphi: Source/Propagator/Enterprise/BoldPropagatorSubscriptions.pas, BoldIndexedList.pas, BoldIndexList.pas, BoldListNodes.pas
// Port map: CSharp/port/units/BoldPropagatorSubscriptions.md
namespace Bold.Tests;

public class BoldPropagatorSubscriptionsTests
{
    private static int[] Ints(TList list) => [.. list.Select(i => (int)i!)];

    [Test]
    [Covers(typeof(TBoldSortedIntegerList), "*")]
    public void A_sorted_integer_list_keeps_distinct_values_in_order()
    {
        var list = new TBoldSortedIntegerList();
        Assert.That(list.Add(5), Is.EqualTo(0));
        Assert.That(list.Add(1), Is.EqualTo(0));
        Assert.That(list.Add(9), Is.EqualTo(2));
        Assert.That(list.Add(3), Is.EqualTo(1));
        Assert.That(list.Add(7), Is.EqualTo(3));
        Assert.That(list.Add(5), Is.EqualTo(-1), "already there");
        Assert.That(list.Add(1), Is.EqualTo(-1));
        Assert.That(Ints(list), Is.EqualTo(new[] { 1, 3, 5, 7, 9 }));
    }

    [Test]
    [Covers(typeof(TBoldSubscriptionList), "*")]
    [Covers(typeof(TBoldSubscriptionNode), "*")]
    [Covers(typeof(TBoldClientIDList), "*")]
    [Covers(typeof(TBoldEventNameList), "*")]
    [Covers(typeof(TBoldExternalEventHashTable), "*")]
    [Covers(typeof(TBoldExternalEventIndex), "*")]
    [Covers(typeof(BoldIndexedList), "*")]
    [Covers(typeof(TBoldIndexList), "*")]
    [Covers(typeof(TBoldAbstractMultiIndexedList), "*")]
    [Covers(typeof(TBoldMultiIndexedListNode))]
    [Covers(typeof(TBoldIndexNode), "*")]
    [Covers(typeof(TBoldEventNameIndexNode), "*")]
    [Covers(typeof(TBoldAbstractLinkNode), "*")]
    public void Subscriptions_are_found_by_client_and_by_event_and_removed_either_way()
    {
        using var subscriptions = new TBoldSubscriptionList();
        Assert.That(subscriptions.NumberOfIndices, Is.EqualTo(2));
        subscriptions.AddSubscription(1, "E:10");
        subscriptions.AddSubscription(2, "E:10");
        subscriptions.AddSubscription(1, "C:Person");
        subscriptions.AddSubscription(3, "e:10");

        var events = new TStringList();
        subscriptions.GetEventsByClientID(1, events);
        Assert.That(events.ToArray(), Is.EquivalentTo(new[] { "E:10", "C:Person" }));
        Assert.That(subscriptions.GetEventCountByClientID(1), Is.EqualTo(2));
        Assert.That(subscriptions.GetEventCountByClientID(42), Is.EqualTo(0), "a client without subscriptions");

        var clients = new TList();
        subscriptions.GetClientsSubscribedToEvent("E:10", clients);
        Assert.That(Ints(clients), Is.EquivalentTo(new[] { 1, 2, 3 }), "event names ignore case");
        var others = new TBoldSortedIntegerList();
        subscriptions.GetAllSubscribedClientsExcept("E:10", 2, others);
        Assert.That(Ints(others), Is.EqualTo(new[] { 1, 3 }));
        Assert.That(subscriptions.Events("E:10")!.EventName, Is.EqualTo("E:10"));
        Assert.That(subscriptions.Events("E:99"), Is.Null);
        Assert.That(subscriptions.Clients(1).Next, Is.InstanceOf<TBoldSubscriptionNode>());
        var node = (TBoldSubscriptionNode)subscriptions.Clients(1).Next!;
        Assert.That(node.NumberOfIndices, Is.EqualTo(2));
        Assert.That(node.IndexNode(0), Is.SameAs(subscriptions.Clients(1)));
        Assert.That(node.IndexNode(1), Is.InstanceOf<TBoldEventNameIndexNode>());

        subscriptions.RemoveEvent("E:10");
        clients.Clear();
        subscriptions.GetClientsSubscribedToEvent("E:10", clients);
        Assert.That(clients.Count, Is.EqualTo(0));
        Assert.That(subscriptions.GetEventCountByClientID(1), Is.EqualTo(1), "client 1 keeps its other subscription");
        Assert.That(subscriptions.GetEventCountByClientID(2), Is.EqualTo(0));

        subscriptions.RemoveClient(1);
        clients.Clear();
        subscriptions.GetClientsSubscribedToEvent("C:Person", clients);
        Assert.That(clients.Count, Is.EqualTo(0));
        Assert.That(() => subscriptions.GetAllSubscribedClientsExcept("E:10", 2, null!), Throws.TypeOf<EBold>());
    }

    private sealed class Node(int clientId) : TBoldAbstractLinkNode
    {
        private readonly TBoldMultiIndexedListNode?[] next = new TBoldMultiIndexedListNode?[1];
        private readonly TBoldMultiIndexedListNode?[] previous = new TBoldMultiIndexedListNode?[1];
        public int ClientId => clientId;
        protected override TBoldMultiIndexedListNode? getNext(int Index) => next[Index];
        protected override void setNext(int Index, TBoldMultiIndexedListNode? Value) => next[Index] = Value;
        protected override TBoldMultiIndexedListNode? getPrevious(int Index) => previous[Index];
        protected override void setPrevious(int Index, TBoldMultiIndexedListNode? Value) => previous[Index] = Value;
        protected override int getNumberOfIndices() => 1;
    }

    private static int[] Chain(TBoldIndexNode head)
    {
        var result = new List<int>();
        for (var n = (Node?)head.Next; n is not null; n = (Node?)n.Next(0))
        {
            result.Add(n.ClientId);
        }

        return [.. result];
    }

    [Test]
    public void Link_nodes_are_inserted_before_and_after_and_removed_from_their_chain()
    {
        using var index = new TBoldClientIDList(0);
        Assert.That(index.IndexOrder, Is.EqualTo(0));
        Assert.That(index.Count, Is.EqualTo(BoldIndexedList.Initial_Client_Count - BoldIndexedList.Initial_Client_Count), "no slots yet");
        var b = new Node(2);
        index.InsertNode(7, b);
        TBoldIndexNode head = index[7]!;
        Assert.That(index.Count, Is.GreaterThan(7), "the slots grew to hold the key");
        b.InsertBefore(0, new Node(1));
        b.InsertAfter(0, new Node(3));
        Assert.That(Chain(head), Is.EqualTo(new[] { 1, 2, 3 }));
        b.Remove();
        Assert.That(Chain(head), Is.EqualTo(new[] { 1, 3 }));
        index.RemoveKey(7);
        Assert.That(Chain(head), Is.Empty);

        index[8] = new TBoldIndexNode();
        Assert.That(index[8], Is.Not.Null);
        index.InsertNode(8, new Node(4));
        index.DeleteNodes();
        Assert.That(index[8]!.Next, Is.Null);
    }
}
