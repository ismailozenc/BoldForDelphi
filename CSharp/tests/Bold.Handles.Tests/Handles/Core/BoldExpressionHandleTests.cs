// Delphi: Source/Handles/Core/BoldExpressionHandle.pas
// Port map: CSharp/port/units/BoldExpressionHandle.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldExpressionHandleTests
{
    [Test]
    [Covers(typeof(TBoldExpressionHandle), "*")]
    public void The_value_follows_the_root_and_the_objects_it_reads()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann", "Smith");
        TPerson bob = s.NewPerson("Bob", "Jones");
        TBoldReferenceHandle root = s.Reference(ann, "Person");
        var name = new TBoldExpressionHandle(s.Components) { RootHandle = root, Expression = "firstName + ' ' + lastName" };

        Assert.That(name.StaticBoldType!.ExpressionName, Is.EqualTo("String"));
        Assert.That(name.StaticRootType!.ExpressionName, Is.EqualTo("Person"));
        Assert.That(name.Value!.AsString, Is.EqualTo("Ann Smith"));

        Func<int> changes = HandlesSample.CountIdentityChanges(name);
        ann.LastName = "Brown";
        Assert.That(changes(), Is.EqualTo(1), "a change of what the expression read makes the value out of date");
        Assert.That(name.Value!.AsString, Is.EqualTo("Ann Brown"));

        root.Value = bob;
        Assert.That(name.Value!.AsString, Is.EqualTo("Bob Jones"), "a new root value");

        name.Expression = "firstName";
        Assert.That(name.Value!.AsString, Is.EqualTo("Bob"));
        Assert.That(((IBoldOCLComponent)name).Expression, Is.EqualTo("firstName"));
        Assert.That(((IBoldOCLComponent)name).ContextType!.ExpressionName, Is.EqualTo("Person"));
        ((IBoldOCLComponent)name).Expression = "lastName";
        Assert.That(name.Value!.AsString, Is.EqualTo("Jones"));
        Assert.That(((IBoldOCLComponent)name).GetVariableList(), Is.Null);

        root.Value = null;
        Assert.That(name.Value, Is.Null, "no root value, no value");
        Assert.That(name.EvaluateInPS, Is.False);
        name.EvaluateInPS = true;
        Assert.That(name.EvaluateInPS, Is.True);
        Assert.That(name.Subscribe, Is.True);
        name.Subscribe = false;
        Assert.That(name.Subscribe, Is.False);
    }

    [Test]
    public void A_single_link_gives_the_reference_and_an_object_value_is_dropped_when_the_object_is_deleted()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann");
        TResidential_Building house = s.NewHouse("Elm St");
        ann.Home = house;
        var home = new TBoldExpressionHandle(s.Components) { RootHandle = s.Reference(ann, "Person"), Expression = "home" };
        Assert.That(((TBoldObjectReference)home.Value!).BoldObject, Is.SameAs(house), "a single link gives its reference");

        TBoldReferenceHandle root = s.Reference(house, "Residential_Building");
        root.OnObjectDeleted = _ => { };
        var self = new TBoldExpressionHandle(s.Components) { RootHandle = root, Expression = "self" };
        Assert.That(self.Value, Is.SameAs(house));
        Assert.That(self.DynamicBoldType!.ExpressionName, Is.EqualTo("Residential_Building"));
        Assert.That(self.BoldType, Is.SameAs(self.DynamicBoldType));

        Func<int> changes = HandlesSample.CountIdentityChanges(self);
        house.Delete();
        Assert.That(changes(), Is.GreaterThan(0));
        Assert.That(self.Value, Is.Null, "the expression handle let go of it");
        Assert.That(self.BoldType!.ExpressionName, Is.EqualTo("Residential_Building"), "without a value the type is the static type");
    }

    [Test]
    public void A_bad_expression_raises_with_the_handle_in_the_message()
    {
        using var s = new HandlesSample();
        TBoldReferenceHandle root = s.Reference(s.NewPerson("Ann"), "Person");
        var bad = new TBoldExpressionHandle(s.Components) { Name = "Bad", RootHandle = root, Expression = "noSuchMember" };
        Assert.That(() => bad.Value, Throws.InstanceOf<EBold>().With.Message.Contains("Bad"));
    }

    [Test]
    public void A_handle_cannot_be_its_own_root_or_use_variables_that_use_it()
    {
        using var s = new HandlesSample();
        var a = new TBoldExpressionHandle(s.Components) { Name = "A", RootHandle = s.SystemHandle };
        var b = new TBoldExpressionHandle(s.Components) { Name = "B", RootHandle = a };
        Assert.That(() => a.RootHandle = a, Throws.InstanceOf<EBold>());
        Assert.That(() => a.RootHandle = b, Throws.InstanceOf<EBold>(), "a circle through another handle");
        Assert.That(b.IsRootLinkedTo(s.SystemHandle), Is.True);
        Assert.That(b.RefersToComponent(a), Is.True);

        var variables = new TBoldOclVariables(s.Components) { Name = "Vars" };
        ((TBoldVariableTuple)variables.Variables.Add()).BoldHandle = b;
        Assert.That(() => a.Variables = variables, Throws.InstanceOf<EBold>().With.Message.Contains("Circular"));
        var c = new TBoldExpressionHandle(s.Components) { RootHandle = s.SystemHandle, Variables = variables };
        Assert.That(c.RefersToComponent(variables), Is.True);
        variables.Dispose();
        Assert.That(c.Variables, Is.Null, "disposed variables are forgotten");
    }
}
