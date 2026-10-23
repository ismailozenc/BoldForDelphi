// Delphi: Source/Handles/Core/BoldListHandle.pas
// Port map: CSharp/port/units/BoldListHandle.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldListHandleTests
{
    private static string Names(TBoldAbstractListHandle handle) =>
        string.Join(",", Enumerable.Range(0, handle.Count).Select(i => ((TPerson)handle.List![i]!).FirstName));

    [Test]
    [Covers(typeof(TBoldListHandle), "*")]
    [Covers(typeof(TBoldAbstractListHandle), "*")]
    public void The_list_follows_objects_created_and_deleted_and_the_current_element_stays_put()
    {
        using var s = new HandlesSample();
        var people = new TBoldListHandle(s.Components) { Name = "People", RootHandle = s.SystemHandle, Expression = "Person.allInstances" };
        Assert.That(people.Count, Is.EqualTo(0), "the class list is current from here, so it keeps the order objects are made in");
        Assert.That(people.CurrentIndex, Is.EqualTo(-1));
        Assert.That(people.CurrentElement, Is.Null);
        Assert.That(people.StaticBoldType!.ExpressionName, Is.EqualTo("Person"));
        Assert.That(people.StaticListType!.ExpressionName, Is.EqualTo("Collection(Person)"));
        Assert.That(people.ListElementType!.ExpressionName, Is.EqualTo("Person"));

        TPerson ann = s.NewPerson("Ann");
        Assert.That(people.Count, Is.EqualTo(1), "a new object shows up");
        Assert.That(people.CurrentBoldObject, Is.SameAs(ann), "AutoFirst makes the first element current");
        TPerson bob = s.NewPerson("Bob");
        TPerson cid = s.NewPerson("Cid");
        Assert.That(Names(people), Is.EqualTo("Ann,Bob,Cid"));
        Assert.That(people.ListType!.ExpressionName, Is.EqualTo("Collection(Person)"));
        Assert.That(people.ObjectList, Is.Not.Null);

        people.CurrentIndex = 1;
        Assert.That(people.CurrentBoldObject, Is.SameAs(bob));
        ann.Delete();
        Assert.That(people.Count, Is.EqualTo(2), "a deleted object goes");
        Assert.That(people.CurrentBoldObject, Is.SameAs(bob), "the current element stays the current element");
        Assert.That(people.CurrentIndex, Is.EqualTo(0));

        bob.Delete();
        Assert.That(people.CurrentBoldObject, Is.SameAs(cid), "the current index is kept when the current element goes");
        cid.Delete();
        Assert.That(people.CurrentIndex, Is.EqualTo(-1));
    }

    [Test]
    public void First_last_next_and_prior_move_the_current_element()
    {
        using var s = new HandlesSample();
        s.NewPerson("Ann");
        s.NewPerson("Bob");
        s.NewPerson("Cid");
        var people = new TBoldListHandle(s.Components) { RootHandle = s.SystemHandle, Expression = "Person.allInstances" };
        Func<int> changes = HandlesSample.CountIdentityChanges(people);
        people.Last();
        Assert.That(people.CurrentBoldObject, Is.SameAs(people.List![2]));
        Assert.That(people.HasNext, Is.False);
        Assert.That(() => people.Next(), Throws.InstanceOf<EBold>());
        people.Prior();
        Assert.That(people.CurrentBoldObject, Is.SameAs(people.List![1]));
        people.First();
        Assert.That(people.HasPrior, Is.False);
        Assert.That(() => people.Prior(), Throws.InstanceOf<EBold>());
        people.Next();
        Assert.That(people.CurrentIndex, Is.EqualTo(1));
        Assert.That(changes(), Is.EqualTo(4), "each move is an identity change");
        Assert.That(() => people.CurrentIndex = 3, Throws.InstanceOf<EBold>().With.Message.Contains("out of bounds"));
        people.CurrentIndex = -1;
        Assert.That(people.CurrentElement, Is.Null);
        Assert.That(() => people.RemoveCurrentElement(), Throws.InstanceOf<EBold>());
    }

    [Test]
    public void A_filter_and_a_comparer_shape_the_list_and_follow_what_they_read()
    {
        using var s = new HandlesSample();
        s.System.ClassByExpressionName("Person").EnsureObjects(); // a current class list keeps the objects in the order they are made
        TPerson ann = s.NewPerson("Ann");
        TPerson bob = s.NewPerson("Bob");
        TPerson cid = s.NewPerson("Cid");
        ann.Assets = 10;
        bob.Assets = 300;
        cid.Assets = 200;
        var rich = new TBoldFilter(s.Components)
        {
            OnFilter = e => ((TPerson)e!).Assets > 100,
            OnSubscribe = (e, subscriber) => ((TPerson)e).M_Assets.DefaultSubscribe(subscriber),
        };
        rich.PreFetchRoles.Add("home");
        var byAssets = new TBoldComparer(s.Components)
        {
            OnCompare = (a, b) => ((TPerson)a!).Assets.CompareTo(((TPerson)b!).Assets),
            OnSubscribe = (e, subscriber) => ((TPerson)e).M_Assets.DefaultSubscribe(subscriber),
        };
        var people = new TBoldListHandle(s.Components) { Name = "People", RootHandle = s.SystemHandle, Expression = "Person.allInstances" };
        people.BoldFilter = rich;
        Assert.That(Names(people), Is.EqualTo("Bob,Cid"));
        people.BoldComparer = byAssets;
        Assert.That(Names(people), Is.EqualTo("Cid,Bob"));
        Assert.That(people.BoldFilter, Is.SameAs(rich));
        Assert.That(people.BoldComparer, Is.SameAs(byAssets));
        Assert.That(people.List!.Mutable, Is.False, "a filtered list is a copy");
        Assert.That(people.MutableList, Is.Null, "the list before sorting is filtered, so not mutable either");

        ann.Assets = 250;
        Assert.That(Names(people), Is.EqualTo("Cid,Ann,Bob"), "the filter and comparer subscribed to the assets");

        rich.Dispose();
        Assert.That(people.BoldFilter, Is.Null, "a disposed filter is removed");
        Assert.That(Names(people), Is.EqualTo("Cid,Ann,Bob"), "still sorted");
        Assert.That(people.MutableList, Is.SameAs(s.System.ClassByExpressionName("Person")), "the list before sorting is mutable");
        people.BoldComparer = null;
        Assert.That(Names(people), Is.EqualTo("Ann,Bob,Cid"));
        Assert.That(people.List!.Mutable, Is.True);
        Assert.That(people.MutableList, Is.SameAs(people.List));
        Assert.That(people.MutableObjectList, Is.SameAs(people.List));
        Assert.That(people.RefersToComponent(s.SystemHandle), Is.True);
    }

    [Test]
    public void RemoveCurrentElement_removes_from_the_list_or_from_the_mutable_list_behind_it()
    {
        using var s = new HandlesSample();
        TResidential_Building house = s.NewHouse("Elm St");
        TPerson ann = s.NewPerson("Ann");
        TPerson bob = s.NewPerson("Bob");
        house.Residents.Add(ann);
        house.Residents.Add(bob);
        TBoldReferenceHandle root = s.Reference(house, "Residential_Building");
        var residents = new TBoldListHandle(s.Components) { RootHandle = root, Expression = "residents" };
        residents.CurrentIndex = 1;
        residents.RemoveCurrentElement();
        Assert.That(house.Residents.Count, Is.EqualTo(1));
        Assert.That(bob.Home, Is.Null);

        var sorted = new TBoldListHandle(s.Components)
        {
            RootHandle = root,
            Expression = "residents->select(true)",
            MutableListExpression = "residents",
        };
        Assert.That(sorted.List!.Mutable, Is.False);
        Assert.That(sorted.MutableListExpression, Is.EqualTo("residents"));
        Assert.That(sorted.MutableList, Is.SameAs(house.Residents), "the mutable list expression gives the list to change");
        sorted.RemoveCurrentElement();
        Assert.That(house.Residents.Count, Is.EqualTo(0));
        Assert.That(sorted.Count, Is.EqualTo(0));

        var allNames = new TBoldListHandle(s.Components) { Name = "Names", RootHandle = s.SystemHandle, Expression = "Person.allInstances.firstName" };
        Assert.That(allNames.Count, Is.EqualTo(2));
        Assert.That(() => allNames.CurrentBoldObject, Throws.InstanceOf<EBold>(), "a string is not an object");
        Assert.That(() => allNames.RemoveCurrentElement(), Throws.InstanceOf<EBold>().With.Message.Contains("immutable"));
    }

    [Test]
    public void Variables_evaluate_in_PS_and_the_list_handle_passes_settings_to_its_inner_handles()
    {
        using var s = new HandlesSample();
        s.NewPerson("Ann");
        var name = new TBoldVariableHandle(s.Components) { StaticSystemHandle = s.SystemHandle, ValueTypeName = "String" };
        name.InitialValues.Add("Ann");
        var variables = new TBoldOclVariables(s.Components);
        var tuple = (TBoldVariableTuple)variables.Variables.Add();
        tuple.VariableName = "wanted";
        tuple.BoldHandle = name;
        var people = new TBoldListHandle(s.Components)
        {
            RootHandle = s.SystemHandle,
            Expression = "Person.allInstances->select(firstName = wanted)",
            Variables = variables,
        };
        Assert.That(people.Variables, Is.SameAs(variables));
        Assert.That(((IBoldOCLComponent)people).GetVariableList(), Is.Not.Null);
        Assert.That(((IBoldOCLComponent)people).ContextType, Is.SameAs(s.TypeInfoHandle.StaticSystemTypeInfo));
        ((IBoldOCLComponent)people).Expression = "Person.allInstances->select(firstName = wanted)";
        Assert.That(people.Count, Is.EqualTo(1));
        Assert.That(people.RefersToComponent(variables), Is.True);
        Assert.That(() => people.Variables = MakeLoop(s, people), Throws.InstanceOf<EBold>());

        people.EvaluateInPS = true;
        Assert.That(people.EvaluateInPS, Is.True);
        people.EvaluateInPS = false;
        people.Subscribe = false;
        Assert.That(people.Subscribe, Is.False);
        people.MarkOutOfDate();
        Assert.That(people.Count, Is.EqualTo(1));
        people.StaticSystemHandle = s.SystemHandle;
        Assert.That(people.RootHandle, Is.SameAs(s.SystemHandle));
    }

    private static TBoldOclVariables MakeLoop(HandlesSample s, TBoldListHandle people)
    {
        var loop = new TBoldOclVariables(s.Components);
        var cursor = new TBoldCursorHandle(s.Components) { RootHandle = people };
        ((TBoldVariableTuple)loop.Variables.Add()).BoldHandle = cursor;
        return loop;
    }

    [Test]
    public void Loading_defers_enabling_until_Loaded()
    {
        using var s = new HandlesSample();
        s.NewPerson("Ann");
        var people = new TBoldListHandle(s.Components) { Name = "People" };
        people.BeginInit();
        people.RootHandle = s.SystemHandle;
        people.Expression = "Person.allInstances";
        Assert.That(people.Enabled, Is.False, "disabled while loading");
        Assert.That(people.Count, Is.EqualTo(0));
        people.EndInit();
        Assert.That(people.Enabled, Is.True);
        Assert.That(people.Count, Is.EqualTo(1));

        var disabled = new TBoldListHandle(s.Components) { RootHandle = s.SystemHandle, Expression = "Person.allInstances" };
        disabled.BeginInit();
        disabled.Enabled = false;
        disabled.EndInit();
        Assert.That(disabled.Enabled, Is.False, "Enabled set false while loading stays false");
        disabled.Enabled = true;
        Assert.That(disabled.Count, Is.EqualTo(1));
    }
}
