// Delphi: Source/Handles/Core/BoldReferenceHandle.pas
// Port map: CSharp/port/units/BoldReferenceHandle.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldReferenceHandleTests
{
    [Test]
    [Covers(typeof(TBoldReferenceHandle), "*")]
    public void A_reference_handle_holds_a_value_and_lets_go_when_the_object_is_deleted()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann");
        var handle = new TBoldReferenceHandle(s.Components) { StaticSystemHandle = s.SystemHandle, StaticValueTypeName = "Person" };
        Assert.That(handle.StaticValueTypeName, Is.EqualTo("Person"));
        Assert.That(handle.StaticBoldType!.ExpressionName, Is.EqualTo("Person"));
        Func<int> changes = HandlesSample.CountIdentityChanges(handle);
        handle.Value = ann;
        Assert.That(changes(), Is.EqualTo(1));
        Assert.That(handle.Value, Is.SameAs(ann));
        handle.Value = ann;
        Assert.That(changes(), Is.EqualTo(1), "the same value is no change");

        ann.Delete();
        Assert.That(handle.Value, Is.Null, "a deleted object is dropped");

        TPerson bob = s.NewPerson("Bob");
        int deleted = 0;
        handle.OnObjectDeleted = _ => deleted++;
        handle.Value = bob;
        bob.Delete();
        Assert.That(deleted, Is.EqualTo(1), "OnObjectDeleted decides what happens to a deleted object");
        Assert.That(handle.Value, Is.Null, "a deleted new object is also destroyed, and a destroyed value is dropped");
        Assert.That(handle.OnObjectDeleted, Is.Not.Null);

        int destroyed = 0;
        handle.OnValueDestroyed = _ => destroyed++;
        var attribute = new TBAString();
        handle.Value = attribute;
        attribute.Dispose();
        Assert.That(handle.Value, Is.Null, "a destroyed value is always dropped");
        Assert.That(destroyed, Is.EqualTo(1));
        Assert.That(handle.OnValueDestroyed, Is.Not.Null);
    }
}
