// Delphi: Source/Common/Subscription/BoldSubscription.pas, Source/Common/Queue/BoldEventQueue.pas, Source/Common/Core/BoldStreams.pas
// Port map: CSharp/port/units/BoldSubscription.md
namespace Bold.Tests;

public class BoldSubscriptionTests
{
    private sealed class Subscribable : TBoldSubscribableObject
    {
        public bool HasSubs => HasSubscribers;
    }

    private sealed class Recorder
    {
        public readonly List<string> Log = [];
        public TBoldPassthroughSubscriber Make(string name) =>
            new((originator, original, requested) => Log.Add($"{name}:{original}->{requested}"));
    }

    [Test]
    [Covers(typeof(BoldSubscription), "*")]
    [Covers(typeof(TBoldPublisherFlag))]
    public void Event_numbers_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldSubscription), "Source/Common/Subscription/BoldSubscription.pas", "beValueEvents");
        Assert.That(BoldSubscription.beValueEvents.ToArray(), Is.EqualTo(new[] { 4, 5, 6, 7, 8, 12 }));
        PortAssert.EnumsMatchDelphi("Source/Common/Subscription/BoldSubscription.pas", typeof(TBoldPublisherFlag));
        PortAssert.DelegatesMatchDelphi("Source/Common/Subscription/BoldSubscription.pas",
            typeof(TBoldEventHandler), typeof(TBoldExtendedEventHandler), typeof(TBoldQueryHandler));
    }

    [Test]
    [Covers(typeof(TBoldPublisher), "*")]
    [Covers(typeof(TBoldEventSubscription), "*")]
    [Covers(typeof(TBoldSmallEventSubscription), "*")]
    [Covers(typeof(TBoldSubscribableObject), "*")]
    [Covers(typeof(TBoldPassthroughSubscriber), "*")]
    [Covers("BoldSubscription", "TBoldEvent", "TBoldRequestedEvent", "TBoldSmallEvent", "TBoldSmallEventSet", "TBoldPublisherFlags", "TBoldSubscriptionList")]
    public void Events_reach_matching_subscriptions_with_their_requested_event()
    {
        var rec = new Recorder();
        var obj = new Subscribable();
        TBoldPassthroughSubscriber a = rec.Make("a");
        TBoldPassthroughSubscriber b = rec.Make("b");
        obj.AddSmallSubscription(a, TBoldSmallEventSet.Of(BoldSubscription.beValueChanged), 100);
        obj.AddSmallSubscription(a, TBoldSmallEventSet.Of(BoldSubscription.beItemAdded), 100);
        obj.AddSubscription(b, BoldSubscription.bpeStartFetch, 200);
        obj.AddSubscription(b, BoldSubscription.bpeStartFetch, 200);
        Assert.That(a.SubscriptionCount, Is.EqualTo(1), "small subscriptions with the same requested event merge");
        Assert.That(b.SubscriptionCount, Is.EqualTo(1), "duplicates are ignored");
        Assert.That(obj.HasSubs, Is.True);

        obj.SendEvent(BoldSubscription.beValueChanged);
        obj.SendEvent(BoldSubscription.beItemAdded);
        obj.SendEvent(BoldSubscription.bpeStartFetch);
        obj.SendEvent(BoldSubscription.beItemDeleted);
        Assert.That(rec.Log, Is.EqualTo(new[] { "a:8->100", "a:4->100", "b:1024->200" }));

        a.Dispose();
        rec.Log.Clear();
        obj.SendEvent(BoldSubscription.beValueChanged);
        Assert.That(rec.Log, Is.Empty, "a disposed subscriber gets nothing");

        obj.Dispose();
        Assert.That(rec.Log, Is.Empty, "b did not subscribe to beDestroying");
    }

    [Test]
    [Covers(typeof(TBoldSubscriber), "*")]
    public void Destroying_is_sent_and_subscriptions_clone()
    {
        var rec = new Recorder();
        var obj = new Subscribable();
        TBoldPassthroughSubscriber a = rec.Make("a");
        TBoldPassthroughSubscriber copy = rec.Make("copy");
        obj.AddSmallSubscription(a, TBoldSmallEventSet.Of(BoldSubscription.beDestroying, BoldSubscription.beValueChanged), 1);
        a.CloneSubscriptions(copy, 1, 7);
        obj.SendEvent(BoldSubscription.beValueChanged);
        Assert.That(rec.Log, Is.EqualTo(new[] { "a:8->1", "copy:8->7" }));
        rec.Log.Clear();
        obj.Dispose();
        Assert.That(rec.Log, Is.EqualTo(new[] { "a:0->1", "copy:0->7" }));
        Assert.That(a.ContextString, Is.EqualTo(""));
        Assert.That(a.HandlesExtendedEvents, Is.False);
    }

    [Test]
    [Covers(typeof(TBoldPublisher), nameof(TBoldPublisher.SendQuery), nameof(TBoldPublisher.SendExtendedEvent))]
    public void Queries_stop_at_the_first_no_and_extended_events_carry_arguments()
    {
        var obj = new Subscribable();
        int asked = 0;
        var yes = new TBoldPassthroughSubscriber(null, (o, e, r, args, s) => { asked++; return true; });
        var no = new TBoldPassthroughSubscriber(null, (o, e, r, args, s) => { asked++; return (string?)args[0] != "forbidden"; });
        obj.AddSubscription(yes, BoldSubscription.bqMayDelete, 0);
        obj.AddSubscription(no, BoldSubscription.bqMayDelete, 0);
        Assert.That(obj.SendQuery(BoldSubscription.bqMayDelete, ["fine"], null), Is.True);
        Assert.That(obj.SendQuery(BoldSubscription.bqMayDelete, ["forbidden"], null), Is.False);
        Assert.That(asked, Is.EqualTo(4));

        object?[]? received = null;
        var ext = new TBoldPassthroughSubscriber((TBoldExtendedEventHandler)((o, e, r, args) => received = args));
        Assert.That(ext.HandlesExtendedEvents, Is.True);
        obj.AddSubscription(ext, BoldSubscription.beMemberChanged, 0);
        obj.SendExtendedEvent(BoldSubscription.beMemberChanged, [42, "x"]);
        Assert.That(received, Is.EqualTo(new object?[] { 42, "x" }));

        var plain = new PlainSubscriber();
        obj.AddSubscription(plain, BoldSubscription.bqMayCommit, 0);
        Assert.Throws<EBold>(() => obj.SendQuery(BoldSubscription.bqMayCommit, [], null), "a subscriber without Answer");
    }

    private sealed class PlainSubscriber : TBoldSubscriber
    {
        protected override void Receive(object? Originator, int OriginalEvent, int RequestedEvent)
        {
        }
    }

    [Test]
    [Covers(typeof(BoldSubscription), nameof(BoldSubscription.BoldAddEventToPostNotifyQueue), nameof(BoldSubscription.BoldForcedDequeuePostNotify))]
    [Covers(typeof(TBoldEventQueue), "*")]
    [Covers(typeof(TBoldEventQueueItem), "*")]
    [Covers(typeof(TBoldEventQueueItemReceiverIndex), "*")]
    public void Post_notify_events_wait_for_the_outermost_notification()
    {
        var log = new List<string>();
        BoldSubscription.BoldAddEventToPostNotifyQueue(_ => log.Add("now"), null, null);
        Assert.That(log, Is.EqualTo(new[] { "now" }));
        TBoldPublisher.StartNotify();
        TBoldPublisher.StartNotify();
        BoldSubscription.BoldAddEventToPostNotifyQueue(s => log.Add("later:" + s), "x", this);
        TBoldPublisher.EndNotify();
        Assert.That(log, Has.Count.EqualTo(1));
        TBoldPublisher.EndNotify();
        Assert.That(log, Is.EqualTo(new[] { "now", "later:x" }));

        var queue = new TBoldEventQueue();
        object receiver = new();
        queue.Add(_ => log.Add("a"), null, receiver);
        queue.Add(_ => log.Add("b"), null, receiver);
        queue.Add(_ => log.Add("c"), null, null);
        queue.RemoveAllForReceiver(receiver);
        Assert.That(queue.Count, Is.EqualTo(1));
        queue.DequeueAll();
        Assert.That(log[^1], Is.EqualTo("c"));
        queue.Dispose();
    }

    [Test]
    [Covers(typeof(TBoldSubscribableComponent), "*")]
    [Covers(typeof(TBoldSubscribablePersistent), "*")]
    public void Components_and_persistents_publish_too()
    {
        var rec = new Recorder();
        var comp = new TBoldSubscribableComponent();
        var pers = new TBoldSubscribablePersistent();
        TBoldPassthroughSubscriber s = rec.Make("s");
        comp.AddSubscription(s, BoldSubscription.beValueChanged, 1);
        pers.AddSmallSubscription(s, TBoldSmallEventSet.Of(BoldSubscription.beValueChanged, BoldSubscription.beDestroying), 2);
        comp.SendEvent(comp, BoldSubscription.beValueChanged);
        pers.SendEvent(pers, BoldSubscription.beValueChanged);
        Assert.That(comp.HasSubscribers && pers.HasSubscribers, Is.True);
        Assert.That(comp.SendQuery(comp, BoldSubscription.bqMayUpdate, [], null), Is.True, "no matching subscription");
        pers.Dispose();
        comp.Dispose();
        Assert.That(rec.Log, Is.EqualTo(new[] { "s:8->1", "s:8->2", "s:0->2" }));
    }

    private sealed class Streamable : IBoldStreamable
    {
        public string GetStreamName() => "Name";
    }

    [Test]
    [Covers(typeof(IBoldStreamable), "*")]
    public void Streamable_names_its_stream()
    {
        IBoldStreamable s = new Streamable();
        Assert.That(s.StreamName, Is.EqualTo("Name"));
    }

    private sealed class Linked(List<string> log, string name) : TBoldSubscribableComponent
    {
        public int Loads;

        protected override void Loaded()
        {
            base.Loaded();
            Loads++;
        }

        protected override void Notification(System.ComponentModel.IComponent AComponent, TOperation Operation) =>
            log.Add($"{name} told {(AComponent as TBoldSubscribableComponent)?.Name ?? AComponent.GetType().Name} {Operation}");
    }

    private sealed class Listener(List<string> log) : System.ComponentModel.Component, IBoldNotifiable
    {
        public void Notification(System.ComponentModel.IComponent AComponent, TOperation Operation) => log.Add("listener " + Operation);
    }

    [Test]
    public void Components_have_a_name_a_loading_state_and_free_notification()
    {
        var log = new List<string>();
        var a = new Linked(log, "a") { Name = "A" };
        var b = new Linked(log, "b") { Name = "B" };
        var c = new Linked(log, "c") { Name = "C" };
        Assert.That(a.Name, Is.EqualTo("A"));
        Assert.That(new TBoldSubscribableComponent().Name, Is.EqualTo(""), "no name and no site");
        Assert.That(a.IsDesigning, Is.False);

        a.BeginInit();
        Assert.That(a.IsLoading, Is.True);
        a.EndInit();
        a.EndInit();
        Assert.That(a.IsLoading, Is.False);
        Assert.That(a.Loads, Is.EqualTo(1), "Loaded once, at the end of loading");

        a.FreeNotification(b);
        a.FreeNotification(b);
        a.FreeNotification(a);
        a.FreeNotification(null);
        a.FreeNotification(c);
        a.RemoveFreeNotification(c);
        var listener = new Listener(log);
        a.FreeNotification(listener);
        b.Dispose();
        Assert.That(log, Is.EqualTo(new[] { "a told B opRemove" }), "free notification works both ways");
        log.Clear();
        a.Dispose();
        Assert.That(a.IsDestroying, Is.True);
        Assert.That(log, Is.EqualTo(new[] { "listener opRemove" }), "b went and c was removed; any IBoldNotifiable is told");
        c.Dispose();
        Assert.That(log, Has.Count.EqualTo(1));
    }

    [Test]
    public void A_component_is_told_when_a_plain_component_it_is_linked_to_is_disposed()
    {
        var log = new List<string>();
        var a = new Linked(log, "a") { Name = "A" };
        var control = new System.ComponentModel.Component();
        var other = new System.ComponentModel.Component();
        a.FreeNotification(control);
        a.FreeNotification(other);
        a.RemoveFreeNotification(other);
        other.Dispose();
        Assert.That(log, Is.Empty, "a removed link is not told");
        control.Dispose();
        Assert.That(log, Is.EqualTo(new[] { "a told Component opRemove" }), "the plain component's Disposed event tells");
        log.Clear();
        a.Dispose();
        Assert.That(log, Is.Empty, "a gone link is not told again");
    }
}
