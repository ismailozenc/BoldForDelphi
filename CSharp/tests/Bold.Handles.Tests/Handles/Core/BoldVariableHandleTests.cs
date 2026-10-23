// Delphi: Source/Handles/Core/BoldVariableHandle.pas
// Port map: CSharp/port/units/BoldVariableHandle.md
namespace Bold.Tests;

public class BoldVariableHandleTests
{
    [Test]
    [Covers(typeof(TBoldVariableHandle), "*")]
    public void A_variable_handle_owns_an_attribute_or_list_with_initial_values()
    {
        using var s = new HandlesSample();
        var number = new TBoldVariableHandle(s.Components) { Name = "Number", StaticSystemHandle = s.SystemHandle, ValueTypeName = "Integer" };
        number.InitialValues.Add("42");
        Assert.That(number.ValueTypeName, Is.EqualTo("Integer"));
        Assert.That(number.Value, Is.InstanceOf<TBAInteger>());
        Assert.That(number.Value!.AsString, Is.EqualTo("42"));
        Assert.That(number.ObjectList, Is.Null);

        Func<int> changes = HandlesSample.CountIdentityChanges(number);
        number.InitialValues = new TStringList { Text = "7" };
        Assert.That(changes(), Is.EqualTo(1), "new initial values give a new value");
        Assert.That(number.Value!.AsString, Is.EqualTo("7"));

        var names = new TBoldVariableHandle(s.Components) { StaticSystemHandle = s.SystemHandle, ValueTypeName = "Collection(String)" };
        names.InitialValues.Add("a");
        names.InitialValues.Add("b");
        Assert.That(((TBoldList)names.Value!).Count, Is.EqualTo(2));

        var people = new TBoldVariableHandle(s.Components) { StaticSystemHandle = s.SystemHandle, ValueTypeName = "Collection(Person)" };
        Assert.That(people.ObjectList, Is.Not.Null);
        people.ObjectList!.Add(s.NewPerson("Ann"));
        people.ValueTypeName = "Collection(Building)";
        Assert.That(people.ObjectList!.Count, Is.EqualTo(0), "a new type gives a new value");
    }

    [Test]
    public void A_variable_handle_needs_a_system_handle_and_an_attribute_or_list_type()
    {
        using var s = new HandlesSample();
        var none = new TBoldVariableHandle(s.Components) { Name = "None", ValueTypeName = "Integer" };
        Assert.That(() => none.Value, Throws.InstanceOf<EBold>().With.Message.Contains("has no SystemHandle"));
        var bad = new TBoldVariableHandle(s.Components) { Name = "Bad", StaticSystemHandle = s.SystemHandle, ValueTypeName = "NoSuchType" };
        Assert.That(() => bad.Value, Throws.InstanceOf<EBold>().With.Message.Contains("NoSuchType"));
        var person = new TBoldVariableHandle(s.Components) { StaticSystemHandle = s.SystemHandle, ValueTypeName = "Person" };
        Assert.That(() => person.StaticBoldType, Throws.InstanceOf<EBold>().With.Message.Contains("Only lists and attributes"));
    }
}
