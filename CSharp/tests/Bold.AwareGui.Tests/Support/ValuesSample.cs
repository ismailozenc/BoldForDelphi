namespace Bold.Tests;

/// <summary>
/// A small model with a value of each kind the control packs show (Thing: name, count, weight, seen, born, married and
/// photo, the last two allowing null) and a transient system on it, with the GUI test environment installed.
/// </summary>
internal sealed class ValuesSample : IDisposable
{
    public readonly GuiTestEnvironment Environment = new();
    public readonly TMoldModel Model = new(null, "Values");
    public readonly TBoldTypeNameDictionary Types = new(null);
    public readonly TBoldSystemTypeInfo SystemTypeInfo;
    public readonly TBoldSystem System;

    public ValuesSample()
    {
        Types.AddDefaultMappings();
        Model.TypeNameDictionary = Types;
        var thing = new TMoldClass(Model, "Thing") { SuperClass = Model.RootClass };
        _ = new TMoldAttribute(thing, "name");
        _ = new TMoldAttribute(thing, "count") { BoldType = "Integer" };
        _ = new TMoldAttribute(thing, "weight") { BoldType = "Float" };
        _ = new TMoldAttribute(thing, "seen") { BoldType = "DateTime" };
        _ = new TMoldAttribute(thing, "born") { BoldType = "Date" };
        new TMoldAttribute(thing, "married") { BoldType = "Boolean" }.SetBoldTV(BoldDefaultTaggedValues.TAG_ALLOWNULL, "True");
        new TMoldAttribute(thing, "photo") { BoldType = "TypedBlob" }.SetBoldTV(BoldDefaultTaggedValues.TAG_ALLOWNULL, "True");
        for (int a = 0; a < thing.Attributes.Count; a++)
        {
            thing.Attributes[a].SetStdTV(BoldUMLTaggedValues.TAG_PERSISTENCE, BoldUMLTaggedValues.TV_PERSISTENCE_PERSISTENT);
        }

        SystemTypeInfo = new TBoldSystemTypeInfo(Model, false, false, Types);
        System = new TBoldSystem(null, SystemTypeInfo, null);
    }

    public TBoldObject NewThing() => System.CreateNewObjectByExpressionName("Thing");

    /// <summary>The member of the object with the expression name.</summary>
    public static T Member<T>(TBoldObject o, string name) where T : TBoldMember => (T)o.BoldMemberByExpressionName(name)!;

    public void Dispose()
    {
        System.Discard();
        System.Dispose();
        SystemTypeInfo.Dispose();
        Environment.Dispose();
    }
}
