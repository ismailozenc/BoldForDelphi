// Delphi: Source/Handles/Core/BoldPlaceableSubscriber.pas
// Port map: CSharp/port/units/BoldPlaceableSubscriber.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldPlaceableSubscriberTests
{
    [Test]
    [Covers(typeof(TBoldPlaceableSubscriber), "*")]
    [Covers("BoldPlaceableSubscriber", "TBoldSubscribeToElementEvent", "TBoldPlaceableSubcriberReceive")]
    public void The_subscriber_subscribes_to_the_handles_value_again_whenever_it_changes()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann");
        TPerson bob = s.NewPerson("Bob");
        TBoldReferenceHandle handle = s.Reference(ann, "Person");
        var received = new List<string>();
        var subscriber = new TBoldPlaceableSubscriber(s.Components)
        {
            OnSubscribeToElement = (element, sub) => ((TPerson)element).M_FirstName.DefaultSubscribe(sub, BoldSubscription.breReEvaluate),
            OnReceive = (sender, originator, originalEvent, requestedEvent) => received.Add(requestedEvent == BoldSubscription.breReEvaluate ? "reevaluate" : "other"),
        };
        Assert.That(subscriber.OnSubscribeToElement, Is.Not.Null);
        Assert.That(subscriber.OnReceive, Is.Not.Null);
        subscriber.BoldHandle = handle;
        Assert.That(subscriber.BoldHandle, Is.SameAs(handle));

        ann.FirstName = "Anna";
        Assert.That(received, Has.Count.EqualTo(1), "a change of the value");
        handle.Value = bob;
        Assert.That(received, Has.Count.EqualTo(2), "the handle's new value");
        ann.FirstName = "Ann";
        Assert.That(received, Has.Count.EqualTo(2), "the old value is no longer followed");
        bob.FirstName = "Bobby";
        Assert.That(received, Has.Count.EqualTo(3), "the new value is");

        subscriber.DelayEventsUntilPostNotify = true;
        Assert.That(subscriber.DelayEventsUntilPostNotify, Is.True);
        handle.Value = ann;
        Assert.That(received, Has.Count.EqualTo(4));

        handle.Dispose();
        Assert.That(subscriber.BoldHandle, Is.Null, "a disposed handle is forgotten");
    }
}
