// Delphi: Source/Common/Subscription/BoldDeriver.pas
// Port map: CSharp/port/units/BoldDeriver.md
namespace Bold.Tests;

public class BoldDeriverTests
{
    private sealed class Source : TBoldSubscribableObject
    {
        public int Value;
    }

    [Test]
    [Covers(typeof(TBoldDeriverState))]
    [Covers(typeof(BoldDeriver), "*")]
    public void States_have_the_Delphi_members()
    {
        PortAssert.EnumsMatchDelphi("Source/Common/Subscription/BoldDeriver.pas", typeof(TBoldDeriverState));
        PortAssert.DelegatesMatchDelphi("Source/Common/Subscription/BoldDeriver.pas", typeof(TBoldDeriveAndResubscribe), typeof(TBoldReverseDerive));
        Assert.That(BoldDeriver.bdsIsDeriving.ToArray(), Is.EqualTo(new[] { TBoldDeriverState.bdsDeriving, TBoldDeriverState.bdsDerivingAndSubscribing }));
    }

    [Test]
    [Covers(typeof(TBoldAbstractDeriver), "*")]
    [Covers(typeof(TBoldEventPluggedDeriver), "*")]
    [Covers(typeof(TBoldDeriver), "*")]
    [Covers(typeof(TBoldDeriveAndResubscribe))]
    [Covers(typeof(TBoldReverseDerive))]
    public void Deriver_rederives_when_a_subscription_fires()
    {
        var source = new Source { Value = 2 };
        int derived = 0, derivations = 0, notified = 0, subscribedRuns = 0;
        var deriver = new TBoldDeriver(null);
        deriver.OnDeriveAndSubscribe = (obj, subscriber) =>
        {
            derivations++;
            derived = source.Value * 10;
            if (subscriber is not null)
            {
                subscribedRuns++;
                source.AddSubscription(subscriber, BoldSubscription.beValueChanged, BoldSubscription.breReEvaluate);
            }
        };
        deriver.OnNotifyOutOfdate = () => notified++;
        Assert.That(deriver.Subscribe, Is.True);
        deriver.EnsureCurrent();
        Assert.That((derived, derivations, subscribedRuns), Is.EqualTo((20, 1, 1)));
        deriver.EnsureCurrent();
        Assert.That(derivations, Is.EqualTo(1), "current: nothing to do");

        source.Value = 3;
        source.SendEvent(BoldSubscription.beValueChanged);
        Assert.That(notified, Is.EqualTo(1), "the handler was set after the constructor's own notification");
        deriver.EnsureCurrent();
        Assert.That((derived, derivations, subscribedRuns), Is.EqualTo((30, 2, 1)), "re-evaluate keeps the subscriptions");

        deriver.MarkSubscriptionOutOfdate();
        deriver.EnsureCurrent();
        Assert.That(subscribedRuns, Is.EqualTo(2));
        deriver.MarkOutOfdate();
        Assert.That(deriver.IsDeriving, Is.False);
        deriver.Derive();
        Assert.That(derivations, Is.EqualTo(4));

        Assert.That(deriver.CanReverseDerive, Is.False);
        deriver.ReverseDerive(); // without a handler the plugged deriver only changes state, as in Delphi
        deriver.OnReverseDerive = obj => source.Value = derived / 10 + 1;
        deriver.ReverseDerive();
        Assert.That(source.Value, Is.EqualTo(4));
        deriver.EnsureCurrent();
        Assert.That(derived, Is.EqualTo(40));

        deriver.Subscribe = false;
        source.SendEvent(BoldSubscription.beValueChanged);
        deriver.EnsureCurrent();
        Assert.That(derivations, Is.EqualTo(5), "unsubscribed: the event no longer marks it");
        source.AddSubscription(deriver, BoldSubscription.beItemAdded, 999);
        Assert.Throws<EBold>(() => source.SendEvent(BoldSubscription.beItemAdded), "an unknown requested event");
    }
}
