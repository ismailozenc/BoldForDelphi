// Delphi: Source/Common/Core/BoldAggeregates.pas
// Port map: CSharp/port/units/BoldAggeregates.md
namespace Bold.Tests;

public class BoldAggeregatesTests
{
    private interface IPart;

    private interface IWhole;

    private sealed class Whole : TBoldRefCountedObject, IWhole
    {
        public int Destroyed;

        protected override void Destroy() => Destroyed++;
    }

    private sealed class Part(object? controller) : TBoldAggregateableObject(controller), IPart
    {
        public int Destroyed;

        public int AddRef() => _AddRef();

        public int Release() => _Release();

        protected override void Destroy() => Destroyed++;
    }

    private sealed class Contained(object? controller) : TBoldAggregatedObject(controller), IPart
    {
        public int AddRef() => _AddRef();

        public int Release() => _Release();
    }

    [Test]
    [Covers(typeof(TBoldAggregateableObject))]
    [Covers(typeof(TBoldContainedObject))]
    [Covers(typeof(TBoldAggregatedObject))]
    public void Classes_have_the_Delphi_parents()
    {
        PortAssert.ClassParentsMatchDelphi("Source/Common/Core/BoldAggeregates.pas",
            typeof(TBoldAggregateableObject), typeof(TBoldContainedObject), typeof(TBoldAggregatedObject));
    }

    [Test]
    [Covers(typeof(TBoldAggregateableObject), ".ctor", nameof(TBoldAggregateableObject.Controller), "GetController", nameof(TBoldAggregateableObject.QueryInterface),
        "NonDelegatingQueryInterface", "_AddRef", "_Release", "NonDelegatingAddRef", "NonDelegatingRelease")]
    public void An_aggregated_object_counts_and_answers_through_its_controller()
    {
        var whole = new Whole();
        var part = new Part(whole);
        Assert.That(part.Controller, Is.SameAs(whole));
        Assert.That(part.QueryInterface(typeof(IWhole)), Is.SameAs(whole), "the controller answers");
        Assert.That(part.QueryInterface(typeof(IPart)), Is.Null, "the controller does not implement the part's interface");
        Assert.That(part.AddRef(), Is.EqualTo(1));
        Assert.That((whole.RefCount, part.RefCount), Is.EqualTo((1, 0)), "the controller's count");
        Assert.That(part.Release(), Is.Zero);
        Assert.That((whole.Destroyed, part.Destroyed), Is.EqualTo((1, 0)), "the last release destroys the controller");

        var alone = new Part(null);
        Assert.That(alone.QueryInterface(typeof(IPart)), Is.SameAs(alone), "without a controller, its own");
        Assert.That(alone.QueryInterface(typeof(IWhole)), Is.Null);
        Assert.That(alone.AddRef(), Is.EqualTo(1));
        Assert.That(alone.Release(), Is.Zero);
        Assert.That(alone.Destroyed, Is.EqualTo(1));
    }

    [Test]
    [Covers(typeof(TBoldContainedObject), ".ctor", nameof(TBoldContainedObject.Controller), "GetController", "_AddRef", "_Release")]
    [Covers(typeof(TBoldAggregatedObject), nameof(TBoldAggregatedObject.QueryInterface))]
    public void A_contained_object_is_counted_and_queried_on_its_controller()
    {
        var whole = new Whole();
        var contained = new Contained(whole);
        Assert.That(contained.Controller, Is.SameAs(whole));
        Assert.That(contained.QueryInterface(typeof(IWhole)), Is.SameAs(whole));
        Assert.That(contained.QueryInterface(typeof(IPart)), Is.Null, "the controller answers, not the contained object");
        Assert.That(contained.AddRef(), Is.EqualTo(1));
        Assert.That(whole.RefCount, Is.EqualTo(1));
        Assert.That(contained.Release(), Is.Zero);
        Assert.That(new Contained(new object()).AddRef(), Is.EqualTo(-1), "a controller that is not counted");
    }
}
