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

    private readonly List<TBoldSystem> fExtraSystems = [];

    public ObjectSpaceSample(Action<TMoldModel>? extend = null, Func<TMoldModel, TBoldPersistenceController?>? controller = null)
    {
        Types.AddDefaultMappings();
        Model.TypeNameDictionary = Types;
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

        // A model from UML gets the persistence tagged value from the defaults; a hand-made one needs it set.
        for (int c = 0; c < Model.Classes.Count; c++)
        {
            for (int a = 0; a < Model.Classes[c].Attributes.Count; a++)
            {
                Model.Classes[c].Attributes[a].SetStdTV(BoldUMLTaggedValues.TAG_PERSISTENCE, BoldUMLTaggedValues.TV_PERSISTENCE_PERSISTENT);
            }
        }

        for (int i = 0; i < Model.Associations.Count; i++)
        {
            Model.Associations[i].SetStdTV(BoldUMLTaggedValues.TAG_PERSISTENCE, BoldUMLTaggedValues.TV_PERSISTENCE_PERSISTENT);
        }
        SystemTypeInfo = new TBoldSystemTypeInfo(Model, false, false, Types);
        System = new TBoldSystem(null, SystemTypeInfo, controller?.Invoke(Model));
    }

    /// <summary>Another system on the same model; disposed with the sample.</summary>
    public TBoldSystem NewSystem(TBoldPersistenceController? controller)
    {
        var system = new TBoldSystem(null, SystemTypeInfo, controller);
        fExtraSystems.Add(system);
        return system;
    }

    public TBoldObject NewPerson() => System.CreateNewObjectByExpressionName("Person");
    public TBoldObject NewBuilding() => System.CreateNewObjectByExpressionName("Building");

    public void Dispose()
    {
        foreach (TBoldSystem extra in fExtraSystems)
        {
            extra.Discard();
            extra.Dispose();
        }

        System.Discard();
        System.Dispose();
        SystemTypeInfo.Dispose();
        Model.Dispose();
    }
}
