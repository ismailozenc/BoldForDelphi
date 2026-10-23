// Delphi: Source/Handles/Core/BoldCursorHandle.pas
// Port map: CSharp/port/units/BoldCursorHandle.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldCursorHandleTests
{
    [Test]
    [Covers(typeof(TBoldCursorHandle), "*")]
    public void A_cursor_walks_its_roots_list_and_keeps_its_element_when_the_list_changes()
    {
        using var s = new HandlesSample();
        TResidential_Building house = s.NewHouse("Elm St");
        TPerson ann = s.NewPerson("Ann");
        TPerson bob = s.NewPerson("Bob");
        house.Residents.Add(ann);
        house.Residents.Add(bob);
        TBoldReferenceHandle root = s.Reference(house.Residents, "Collection(Person)");
        var cursor = new TBoldCursorHandle(s.Components) { RootHandle = root, AutoFirst = false };
        Assert.That(cursor.AutoFirst, Is.False);
        Assert.That(cursor.CurrentIndex, Is.EqualTo(-1), "without AutoFirst nothing is current");
        Assert.That(cursor.Value, Is.Null);
        cursor.AutoFirst = true;
        Assert.That(cursor.Value, Is.SameAs(ann), "switching AutoFirst on makes the first current");
        Assert.That(cursor.StaticBoldType!.ExpressionName, Is.EqualTo("Person"), "the element type of the root's list");

        cursor.CurrentIndex = 1;
        house.Residents.Insert(0, s.NewPerson("Cid"));
        Assert.That(cursor.Value, Is.SameAs(bob), "the current element is followed to its new place");
        Assert.That(cursor.CurrentIndex, Is.EqualTo(2));

        house.Residents.Remove(bob);
        Assert.That(cursor.CurrentIndex, Is.EqualTo(1), "the last element when the list got shorter");
        Assert.That(cursor.Value, Is.SameAs(ann));

        TBoldReferenceHandle single = s.Reference(ann, "Person");
        var one = new TBoldCursorHandle(s.Components) { RootHandle = single };
        Assert.That(one.Count, Is.EqualTo(1), "a single element is a list of one");
        Assert.That(one.Value, Is.SameAs(ann));
        Assert.That(one.StaticBoldType!.ExpressionName, Is.EqualTo("Person"));

        root.Value = null;
        Assert.That(cursor.List, Is.Null);
        Assert.That(cursor.Count, Is.EqualTo(0));
        cursor.Enabled = false;
        Assert.That(cursor.CurrentIndex, Is.EqualTo(-1));
        Assert.That(cursor.CurrentElement, Is.Null);
        Assert.That(cursor.List, Is.Null);
    }

    [Test]
    public void A_cursor_on_a_list_that_is_destroyed_lets_go_of_it()
    {
        using var s = new HandlesSample();
        s.NewPerson("Ann");
        TBoldList list = (TBoldList)s.System.EvaluateExpressionAsNewElement("Person.allInstances->select(true)")!;
        TBoldReferenceHandle root = s.Reference(list, "Collection(Person)");
        var cursor = new TBoldCursorHandle(s.Components) { RootHandle = root };
        Assert.That(cursor.Count, Is.EqualTo(1));
        Func<int> changes = HandlesSample.CountIdentityChanges(cursor);
        list.Dispose();
        Assert.That(changes(), Is.GreaterThan(0));
        Assert.That(cursor.Count, Is.EqualTo(0));
    }
}
