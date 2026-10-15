namespace Bold.Tests;

/// <summary>
/// A small model and a transient system on it, for object space tests: Person with attributes of every basic type,
/// Building with an address, Person.home 0..1 to Building.residents 0..*, and Person.friends to Person.friendOf (both
/// many, ordered on friends, with the link class Friendship).
/// </summary>
internal sealed class ObjectSpaceSample : IDisposable
{
    public readonly TMoldModel Model = new(null, "Town");
    public readonly TBoldTypeNameDictionary Types = new(null);
    public readonly TBoldSystemTypeInfo SystemTypeInfo;
    public readonly TBoldSystem System;

    public ObjectSpaceSample(Action<TMoldModel>? extend = null)
    {
        Types.AddDefaultMappings();
        var person = new TMoldClass(Model, "Person") { SuperClass = Model.RootClass };
        var building = new TMoldClass(Model, "Building") { SuperClass = Model.RootClass };
        new TMoldAttribute(person, "firstName").SetBoldTV(BoldDefaultTaggedValues.TAG_LENGTH, "20");
        new TMoldAttribute(person, "age") { BoldType = "Integer" }.SetBoldTV(BoldDefaultTaggedValues.TAG_ALLOWNULL, "True");
        _ = new TMoldAttribute(person, "salary") { BoldType = "Currency" };
        _ = new TMoldAttribute(person, "weight") { BoldType = "Float" };
        _ = new TMoldAttribute(person, "born") { BoldType = "Date" };
        _ = new TMoldAttribute(person, "wakes") { BoldType = "Time" };
        _ = new TMoldAttribute(person, "seen") { BoldType = "DateTime" };
        _ = new TMoldAttribute(person, "married") { BoldType = "Boolean" };
        new TMoldAttribute(person, "photo") { BoldType = "Blob" }.SetBoldTV(BoldDefaultTaggedValues.TAG_ALLOWNULL, "True");
        _ = new TMoldAttribute(building, "address");

        var residence = new TMoldAssociation(Model, "Residence");
        _ = new TMoldRole(residence, "home") { MoldClass = person, Multiplicity = "0..1" };
        _ = new TMoldRole(residence, "residents") { MoldClass = building, Multiplicity = "0..*" };

        var friendshipClass = new TMoldClass(Model, "Friendship") { SuperClass = Model.RootClass };
        var friendship = new TMoldAssociation(Model, "Friendship") { LinkClass = friendshipClass };
        _ = new TMoldRole(friendship, "friendOf") { MoldClass = person, Multiplicity = "0..*" };
        _ = new TMoldRole(friendship, "friends") { MoldClass = person, Multiplicity = "0..*", Ordered = true };

        extend?.Invoke(Model);
        SystemTypeInfo = new TBoldSystemTypeInfo(Model, false, false, Types);
        System = new TBoldSystem(null, SystemTypeInfo, null);
    }

    public TBoldObject NewPerson() => System.CreateNewObjectByExpressionName("Person");
    public TBoldObject NewBuilding() => System.CreateNewObjectByExpressionName("Building");

    public void Dispose()
    {
        System.Discard();
        System.Dispose();
        SystemTypeInfo.Dispose();
        Model.Dispose();
    }
}
