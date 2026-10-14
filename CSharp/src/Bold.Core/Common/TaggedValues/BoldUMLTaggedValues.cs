// Delphi: Source/Common/TaggedValues/BoldUMLTaggedValues.pas
// Port map: CSharp/port/units/BoldUMLTaggedValues.md

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLTaggedValues", Symbol = "initialization", Note = "The unit's initialization section is empty.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldUMLTaggedValues", Symbol = "finalization", Note = "Frees the global list; the garbage collector does that in .NET.")]
namespace Bold;

/// <summary>The standard UML tagged values (documentation, derived, persistence) and their defaults.</summary>
public static partial class BoldUMLTaggedValues
{
    [Delphi(PortStatus.Ported)] public const string TAG_DOCUMENTATION = "documentation";
    [Delphi(PortStatus.Ported)] public const string TAG_DERIVED = "derived";
    [Delphi(PortStatus.Ported)] public const string ENUM_TAG_PERSISTENCE = "PersistenceEnum";
    [Delphi(PortStatus.Ported)] public const string TAG_PERSISTENCE = "persistence";
    [Delphi(PortStatus.Ported)] public const string TV_PERSISTENCE_PERSISTENT = "persistent";
    [Delphi(PortStatus.Ported)] public const string TV_PERSISTENCE_TRANSIENT = "transient";

    private static TBoldTaggedValuePerClassList? G_UMLTaggedValues;

    [Delphi(PortStatus.Ported)]
    private static void AddDefaultTaggedValues()
    {
        G_UMLTaggedValues!.ListForClassName("Class").Add(ENUM_TAG_PERSISTENCE, TAG_PERSISTENCE, TV_PERSISTENCE_PERSISTENT);

        TBoldTaggedValueList l = G_UMLTaggedValues.ListForClassName("Association");
        l.Add("Boolean", TAG_DERIVED, BoldDefaultTaggedValues.TV_FALSE);
        l.Add(ENUM_TAG_PERSISTENCE, TAG_PERSISTENCE, TV_PERSISTENCE_PERSISTENT);

        l = G_UMLTaggedValues.ListForClassName("Attribute");
        l.Add("Boolean", TAG_DERIVED, BoldDefaultTaggedValues.TV_FALSE);
        l.Add(ENUM_TAG_PERSISTENCE, TAG_PERSISTENCE, TV_PERSISTENCE_PERSISTENT);
    }

    /// <summary>The UML tagged values per element kind, created on first use.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldTaggedValuePerClassList UMLTaggedValueList()
    {
        if (G_UMLTaggedValues is null)
        {
            G_UMLTaggedValues = new TBoldTaggedValuePerClassList();
            AddDefaultTaggedValues();
        }

        return G_UMLTaggedValues;
    }
}
