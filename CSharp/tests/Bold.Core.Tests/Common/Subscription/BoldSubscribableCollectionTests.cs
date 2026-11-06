// Delphi: Source/Common/Subscription/BoldSubscribableCollection.pas
// Port map: CSharp/port/units/BoldSubscribableCollection.md
namespace Bold.Tests;

public class BoldSubscribableCollectionTests
{
    private const int beSomethingHappened = BoldSubscription.beMinUser + 1;
    private const int bqMayIGo = BoldSubscription.bqMinQuery + 1;

    private sealed class Item(TCollection? collection) : TBoldSubscribableCollectionItem(collection)
    {
        public bool Subscribed => HasSubscribers;
    }

    private sealed class Events
    {
        public readonly List<(object? Originator, int Event, object?[] Args)> Received = [];

        public TBoldPassthroughSubscriber Subscriber() =>
            new((TBoldExtendedEventHandler)((o, e, r, args) => Received.Add((o, e, args))));
    }

    [Test]
    [Covers(typeof(TBoldSubscribableCollection))]
    [Covers(typeof(TBoldSubscribableCollectionItem))]
    public void Classes_have_the_Delphi_parents()
    {
        PortAssert.ClassParentsMatchDelphi("Source/Common/Subscription/BoldSubscribableCollection.pas",
            typeof(TBoldSubscribableCollection), typeof(TBoldSubscribableCollectionItem));
    }

    [Test]
    [Covers(typeof(TBoldSubscribableCollection), ".ctor", nameof(TBoldSubscribableCollection.Dispose), nameof(TBoldSubscribableCollection.AddSmallSubscription),
        nameof(TBoldSubscribableCollection.AddSubscription), nameof(TBoldSubscribableCollection.SendEvent), nameof(TBoldSubscribableCollection.SendExtendedEvent),
        nameof(TBoldSubscribableCollection.SendQuery), nameof(TBoldSubscribableCollection.HasSubscribers), "GetHasSubscribers", "Publisher", "FreePublisher")]
    public void The_collection_sends_events_to_its_subscribers()
    {
        var collection = new TBoldSubscribableCollection(typeof(Item));
        var events = new Events();
        Assert.That(collection.HasSubscribers, Is.False);
        collection.AddSubscription(events.Subscriber(), beSomethingHappened, beSomethingHappened);
        collection.AddSmallSubscription(events.Subscriber(), TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldSubscription.beDestroying);
        Assert.That(collection.HasSubscribers, Is.True);
        collection.SendEvent(beSomethingHappened);
        collection.SendExtendedEvent(beSomethingHappened, ["detail"]);
        Assert.That(events.Received.Select(r => (r.Originator, r.Event)), Is.EqualTo(new[] { ((object?)collection, beSomethingHappened), (collection, beSomethingHappened) }));
        Assert.That(events.Received[1].Args, Is.EqualTo(new object?[] { "detail" }));

        var refuse = new TBoldPassthroughSubscriber(null, (o, e, r, args, s) => false);
        collection.AddSubscription(refuse, bqMayIGo, bqMayIGo);
        Assert.That(collection.SendQuery(bqMayIGo, [], null), Is.False, "a subscriber says no");

        collection.Dispose();
        Assert.That(events.Received.Select(r => r.Event), Does.Contain(BoldSubscription.beDestroying), "the subscribers hear it goes");
        Assert.That(collection.HasSubscribers, Is.False);
        Assert.That(collection.SendQuery(bqMayIGo, [], null), Is.True, "nobody to ask any more");
        Assert.That(() => collection.AddSubscription(events.Subscriber(), beSomethingHappened, beSomethingHappened), Throws.InstanceOf<EBold>());
    }

    [Test]
    [Covers(typeof(TBoldSubscribableCollectionItem), ".ctor", nameof(TBoldSubscribableCollectionItem.Dispose), nameof(TBoldSubscribableCollectionItem.AddSmallSubscription),
        nameof(TBoldSubscribableCollectionItem.AddSubscription), nameof(TBoldSubscribableCollectionItem.SendEvent),
        nameof(TBoldSubscribableCollectionItem.SendExtendedEvent), nameof(TBoldSubscribableCollectionItem.SendQuery),
        nameof(TBoldSubscribableCollectionItem.HasSubscribers), "GetHasSubscribers", "Publisher", "FreePublisher")]
    public void An_item_sends_events_and_leaves_its_collection_when_it_goes()
    {
        var collection = new TBoldSubscribableCollection(typeof(Item));
        var item = (Item)collection.Add();
        var events = new Events();
        item.AddSubscription(events.Subscriber(), beSomethingHappened, beSomethingHappened);
        item.AddSmallSubscription(events.Subscriber(), TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldSubscription.beDestroying);
        Assert.That(item.Subscribed, Is.True);
        item.SendEvent(beSomethingHappened);
        item.SendExtendedEvent(beSomethingHappened, [42]);
        Assert.That(events.Received.Select(r => r.Originator), Is.All.SameAs(item));
        Assert.That(item.SendQuery(bqMayIGo, [], null), Is.True, "nobody says no");

        item.Dispose();
        Assert.That(events.Received.Select(r => r.Event), Does.Contain(BoldSubscription.beDestroying));
        Assert.That((collection.Count, item.Collection), Is.EqualTo((0, (TCollection?)null)), "the item left its collection");
        collection.Dispose();
    }
}
