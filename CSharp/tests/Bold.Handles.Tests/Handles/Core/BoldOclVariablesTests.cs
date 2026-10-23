// Delphi: Source/Handles/Core/BoldOclVariables.pas
// Port map: CSharp/port/units/BoldOclVariables.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldOclVariablesTests
{
    [Test]
    [Covers(typeof(TBoldOclVariables), "*")]
    [Covers(typeof(TBoldHandleBasedExternalVariable), "*")]
    [Covers(typeof(TBoldVariableTuple), "*")]
    [Covers(typeof(TBoldVariableTupleList), "*")]
    public void Variables_resolve_to_handle_values_and_a_new_value_reevaluates_the_expression()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann", "Smith");
        TPerson bob = s.NewPerson("Bob", "Smith");
        s.NewPerson("Cid", "Jones");
        TBoldReferenceHandle chosen = s.Reference(ann, "Person");
        var surname = new TBoldVariableHandle(s.Components) { Name = "Surname", StaticSystemHandle = s.SystemHandle, ValueTypeName = "String" };
        surname.InitialValues.Add("Smith");

        var variables = new TBoldOclVariables(s.Components) { Name = "Vars" };
        var name = (TBoldVariableTuple)variables.Variables.Add();
        Assert.That(name.VariableName, Is.EqualTo("Variable1"), "a new variable gets a unique name");
        name.VariableName = "surname";
        name.BoldHandle = surname;
        var who = (TBoldVariableTuple)variables.Variables.Add();
        who.VariableName = "chosen";
        who.BoldHandle = chosen;

        var family = new TBoldExpressionHandle(s.Components)
        {
            RootHandle = s.SystemHandle,
            Expression = "Person.allInstances->select(p | p.lastName = surname)->excluding(chosen)",
            Variables = variables,
        };
        Assert.That(family.StaticBoldType!.ExpressionName, Is.EqualTo("Collection(Person)"), "the variables have types");
        var list = (TBoldObjectList)family.Value!;
        Assert.That(list.Count, Is.EqualTo(1));
        Assert.That(list[0], Is.SameAs(bob));

        Func<int> changes = HandlesSample.CountIdentityChanges(family);
        chosen.Value = bob;
        Assert.That(changes(), Is.EqualTo(1), "a new value of a variable's handle makes the value out of date");
        Assert.That(((TBoldObjectList)family.Value!)[0], Is.SameAs(ann));

        surname.InitialValues = new TStringList { Text = "Jones" };
        Assert.That(((TBoldObjectList)family.Value!).Count, Is.EqualTo(1));
        Assert.That(((TPerson)((TBoldObjectList)family.Value!)[0]!).FirstName, Is.EqualTo("Cid"));

        TBoldExternalVariableList variableList = variables.VariableList;
        Assert.That(variableList.Count, Is.EqualTo(2));
        Assert.That(variableList[1].Value, Is.SameAs(bob));
        Assert.That(variableList[1].ValueType!.ExpressionName, Is.EqualTo("Person"));
        Assert.That(variables.LinksToHandle(chosen), Is.True);
        Assert.That(variables.LinksToHandle(s.SystemHandle), Is.False);
        Assert.That(name.LinksToHandle(surname), Is.True);
        Assert.That(name.TupleList, Is.SameAs(variables.Variables));
        Assert.That(name.DisplayName, Is.EqualTo("surname: Surname"));

        chosen.Dispose();
        Assert.That(who.BoldHandle, Is.Null, "a disposed handle leaves its variable");
        Assert.That(who.DisplayName, Is.EqualTo("chosen: Not Connected"));
        Assert.That(variables.VariableList[1].Value, Is.Null);
    }

    [Test]
    public void A_variable_on_a_list_handle_can_be_the_whole_list()
    {
        using var s = new HandlesSample();
        s.NewPerson("Ann");
        s.NewPerson("Bob");
        var people = new TBoldListHandle(s.Components) { Name = "People", RootHandle = s.SystemHandle, Expression = "Person.allInstances" };
        var variables = new TBoldOclVariables(s.Components);
        var tuple = (TBoldVariableTuple)variables.Variables.Add();
        tuple.VariableName = "everyone";
        tuple.BoldHandle = people;
        Assert.That(tuple.EffectiveUseListElement, Is.False);
        Assert.That(variables.VariableList[0].Value, Is.InstanceOf<TPerson>(), "the current element");
        tuple.UseListElement = true;
        Assert.That(tuple.UseListElement, Is.True);
        Assert.That(tuple.EffectiveUseListElement, Is.True);
        Assert.That(tuple.DisplayName, Is.EqualTo("everyone: People (list)"));
        Assert.That(variables.VariableList[0].Value, Is.InstanceOf<TBoldObjectList>(), "the whole list");
        Assert.That(variables.VariableList[0].ValueType!.ExpressionName, Is.EqualTo("Collection(Person)"));
        var count = new TBoldExpressionHandle(s.Components) { RootHandle = s.SystemHandle, Expression = "everyone->size", Variables = variables };
        Assert.That(count.Value!.AsString, Is.EqualTo("2"));
        Assert.That(((IBoldOCLComponent)count).GetVariableList(), Is.SameAs(variables.VariableList));
    }

    [Test]
    [Covers(typeof(TBoldVariableDefinition))]
    public void Variable_names_are_unique_and_valid_and_a_deleted_variable_leaves_the_list()
    {
        using var s = new HandlesSample();
        var variables = new TBoldVariableDefinition(s.Components) { Name = "Vars" };
        var first = (TBoldVariableTuple)variables.Variables.Add();
        var second = (TBoldVariableTuple)variables.Variables.Add();
        Assert.That(second.VariableName, Is.EqualTo("Variable2"));
        Assert.That(() => second.VariableName = "Variable1", Throws.InstanceOf<EBold>().With.Message.Contains("Not Unique"));
        Assert.That(() => second.VariableName = "not valid", Throws.InstanceOf<EBold>().With.Message.EqualTo(HandlesConst.sIllegalCharsInName));
        Assert.That(variables.Variables.NameIsUnique("x"), Is.True);
        Assert.That(variables.Variables.NameIsValid("a_1"), Is.True);
        Assert.That(variables.Variables.GetUniqueName(), Is.EqualTo("Variable3"));
        Assert.That(variables.Variables[0], Is.SameAs(first));

        int changes = 0;
        variables.AddSmallSubscription(new TBoldPassthroughSubscriber((_, _, _) => changes++), TBoldSmallEventSet.Of(BoldSubscription.beValueChanged), 0);
        Assert.That(variables.VariableList.Count, Is.EqualTo(2));
        variables.Variables.Delete(0);
        Assert.That(changes, Is.EqualTo(1), "deleting a variable is a change");
        Assert.That(variables.VariableList.Count, Is.EqualTo(1));
        Assert.That(variables.VariableList[0].Name, Is.EqualTo("Variable2"));

        second.UseListElement = true;
        var other = new TBoldOclVariables(s.Components);
        var copy = (TBoldVariableTuple)other.Variables.Add();
        copy.Assign(second);
        Assert.That(copy.VariableName, Is.EqualTo("Variable2"));
        Assert.That(copy.UseListElement, Is.True);
        other.Variables.Assign(variables.Variables);
        Assert.That(other.Variables.Count, Is.EqualTo(1));
        Assert.That(other.Variables[0].VariableName, Is.EqualTo("Variable2"));
        Assert.That(() => copy.Assign(new TPersistent()), Throws.InstanceOf<EConvertError>());
        other.Variables = other.Variables;

        var people = new TBoldListHandle(s.Components) { RootHandle = s.SystemHandle, Expression = "Person.allInstances", Variables = variables };
        Assert.That(() => second.BoldHandle = people, Throws.InstanceOf<EBold>().With.Message.Contains("Circular"), "a handle that uses the variables");
        variables.SubscribeToHandles(null);
    }
}
