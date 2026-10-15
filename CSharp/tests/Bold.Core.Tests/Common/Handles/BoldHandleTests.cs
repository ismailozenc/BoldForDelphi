// Delphi: Source/Common/Handles/BoldHandle.pas
// Port map: CSharp/port/units/BoldHandle.md
namespace Bold.Tests;

public class BoldHandleTests
{
    private sealed class Handle(System.ComponentModel.IContainer? owner, object? handled) : TBoldHandle(owner)
    {
        protected override object? GetHandledObject() => handled;
    }

    [Test]
    [Covers(typeof(TBoldHandle), "*")]
    [Covers(typeof(BoldHandle), "*")]
    [Covers("BoldHandle", "initialization", "finalization")]
    public void Handles_are_listed_while_alive()
    {
        using var container = new System.ComponentModel.Container();
        var handled = new object();
        var h = new Handle(container, handled);
        Assert.That(h.HandledObject, Is.SameAs(handled));
        Assert.That(BoldHandle.BoldHandleList!.IndexOf(h), Is.GreaterThanOrEqualTo(0));
        Assert.That(container.Components.Cast<object>(), Does.Contain(h));
        int destroying = 0;
        h.AddSmallSubscription(new TBoldPassthroughSubscriber((_, _, _) => destroying++), TBoldSmallEventSet.Of(BoldSubscription.beDestroying), 0);
        h.Dispose();
        Assert.That(destroying, Is.EqualTo(1));
        Assert.That(BoldHandle.BoldHandleList.IndexOf(h), Is.EqualTo(-1));
        using var noOwner = new Handle(null, null);
        Assert.That(noOwner.HandledObject, Is.Null);
    }
}
