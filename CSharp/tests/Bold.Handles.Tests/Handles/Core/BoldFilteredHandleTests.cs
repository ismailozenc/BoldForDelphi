// Delphi: Source/Handles/Core/BoldFilteredHandle.pas
// Port map: CSharp/port/units/BoldFilteredHandle.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldFilteredHandleTests
{
    [Test]
    [Covers(typeof(TBoldFilteredHandle), "*")]
    [Covers(typeof(TBoldFilter), "*")]
    [Covers("BoldFilteredHandle", "TBoldElementFilter")]
    public void A_filtered_handle_keeps_the_elements_that_pass()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann");
        s.NewPerson("Bob");
        var married = new TBoldFilter(s.Components) { OnFilter = e => ((TPerson)e!).IsMarried };
        married.OnSubscribe = (e, subscriber) => ((TPerson)e).M_IsMarried.DefaultSubscribe(subscriber);
        Assert.That(married.OnFilter, Is.Not.Null);
        Assert.That(married.OnSubscribe, Is.Not.Null);
        var all = new TBoldExpressionHandle(s.Components) { RootHandle = s.SystemHandle, Expression = "Person.allInstances" };
        var filtered = new TBoldFilteredHandle(s.Components) { RootHandle = all, BoldFilter = married };
        Assert.That(filtered.BoldFilter, Is.SameAs(married));
        Assert.That(filtered.StaticBoldType!.ExpressionName, Is.EqualTo("Collection(Person)"));
        Assert.That(((TBoldList)filtered.Value!).Count, Is.EqualTo(0));
        ann.IsMarried = true;
        Assert.That(((TBoldList)filtered.Value!).Count, Is.EqualTo(1), "the filter subscribed to what it read");
        s.NewPerson("Cid").IsMarried = true;
        Assert.That(((TBoldList)filtered.Value!).Count, Is.EqualTo(2), "the source list is followed");

        filtered.BoldFilter = null;
        Assert.That(((TBoldList)filtered.Value!).Count, Is.EqualTo(3), "no filter, the whole list");
        var single = new TBoldFilteredHandle(s.Components) { RootHandle = s.Reference(ann, "Person"), BoldFilter = married };
        Assert.That(single.StaticBoldType!.ExpressionName, Is.EqualTo("Collection(Person)"), "the list type of a single element");
        Assert.That(((TBoldList)single.Value!).Count, Is.EqualTo(1));

        var list = (TBoldList)s.System.EvaluateExpressionAsNewElement("Person.allInstances")!;
        new TBoldFilter().FilterList(list);
        Assert.That(list.Count, Is.EqualTo(3), "without OnFilter everything passes");
        married.FilterList(list);
        Assert.That(list.Count, Is.EqualTo(2));
        married.PreFetchRoles = new TStringList { Text = "home" };
        Assert.That(married.PreFetchRoles.Count, Is.EqualTo(1));
        Assert.That(((TBoldList)filtered.Value!).Count, Is.EqualTo(3));
        filtered.BoldFilter = married;
        Assert.That(((TBoldList)filtered.Value!).Count, Is.EqualTo(2), "prefetching the roles first");
        var unrooted = new TBoldFilteredHandle(s.Components);
        Assert.That(unrooted.StaticBoldType, Is.Null);
        Assert.That(unrooted.Value, Is.Null);
    }
}
