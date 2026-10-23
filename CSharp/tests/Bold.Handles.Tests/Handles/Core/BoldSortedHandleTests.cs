// Delphi: Source/Handles/Core/BoldSortedHandle.pas
// Port map: CSharp/port/units/BoldSortedHandle.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldSortedHandleTests
{
    [Test]
    [Covers(typeof(TBoldSortedHandle), "*")]
    [Covers(typeof(TBoldComparer), "*")]
    public void A_sorted_handle_orders_the_list_and_follows_what_the_comparer_reads()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann");
        s.NewPerson("Bob");
        int prepared = 0;
        var byName = new TBoldComparer(s.Components)
        {
            OnCompare = (a, b) => -string.CompareOrdinal(((TPerson)a!).FirstName, ((TPerson)b!).FirstName),
            OnSubscribe = (e, subscriber) => ((TPerson)e).M_FirstName.DefaultSubscribe(subscriber),
            OnPrepareSort = _ => prepared++,
        };
        Assert.That(byName.OnCompare, Is.Not.Null);
        Assert.That(byName.OnSubscribe, Is.Not.Null);
        Assert.That(byName.OnPrepareSort, Is.Not.Null);
        var all = new TBoldExpressionHandle(s.Components) { RootHandle = s.SystemHandle, Expression = "Person.allInstances" };
        var sorted = new TBoldSortedHandle(s.Components) { RootHandle = all, BoldComparer = byName };
        Assert.That(sorted.BoldComparer, Is.SameAs(byName));
        Assert.That(sorted.StaticBoldType!.ExpressionName, Is.EqualTo("Collection(Person)"));
        string Names() => string.Join(",", Enumerable.Range(0, ((TBoldList)sorted.Value!).Count).Select(i => ((TPerson)((TBoldList)sorted.Value!)[i]!).FirstName));
        Assert.That(Names(), Is.EqualTo("Bob,Ann"));
        Assert.That(prepared, Is.EqualTo(1));
        ann.FirstName = "Cid";
        Assert.That(Names(), Is.EqualTo("Cid,Bob"), "the comparer subscribed to the names");
        Assert.That(((TBoldList)sorted.Value!).Mutable, Is.False);

        sorted.BoldComparer = null;
        Assert.That(Names(), Is.EqualTo("Cid,Bob"), "no comparer, the list as it is");
        var list = (TBoldObjectList)s.System.EvaluateExpressionAsNewElement("Person.allInstances")!;
        new TBoldComparer().SortList(list);
        byName.SortList(list);
        Assert.That(((TPerson)list[0]!).FirstName, Is.EqualTo("Cid"));
        Assert.That(new TBoldComparer().Compare(list[0], list[1]), Is.EqualTo(0), "without OnCompare all are equal");
        var single = new TBoldSortedHandle(s.Components) { RootHandle = s.Reference(ann, "Person"), BoldComparer = byName };
        Assert.That(single.StaticBoldType!.ExpressionName, Is.EqualTo("Collection(Person)"));
        Assert.That(new TBoldSortedHandle(s.Components).StaticBoldType, Is.Null);
    }
}
