// Delphi: Source/ObjectSpace/Core/BoldMetaElementList.pas
// Port map: CSharp/port/units/BoldMetaElementList.md
namespace Bold.Tests;

public class BoldMetaElementListTests
{
    private sealed class TypeInfo(string modelName, string expressionName, string delphiName)
        : TBoldElementTypeInfo(modelName, expressionName, delphiName, null)
    {
        protected override TBoldElementTypeInfo? GetBoldType() => null;
        public override bool ConformsTo(TBoldElementTypeInfo? Element) => Element == this;
    }

    [Test]
    [Covers(typeof(TBoldMetaElementList), "*")]
    [Covers(typeof(TBoldElementTypeInfoList), "*")]
    [Covers("BoldMetaElementList", "TBoldValueTypeNameIndex", "TBoldValueTypeNameIndex.ItemAsKeyString", "TDelphiNameIndex",
        "TDelphiNameIndex.ItemAsKeyString", "TModelNameIndex", "TModelNameIndex.ItemAsKeyString")]
    public void Finds_elements_by_their_three_names()
    {
        var person = new TypeInfo("Person", "Person", "TPerson");
        var building = new TypeInfo("The Building", "Building", "TBuilding");
        var types = new TBoldElementTypeInfoList();
        types.Add(person);
        types.Add(building);
        Assert.That(types.Count, Is.EqualTo(2));
        Assert.That(types[1], Is.SameAs(building));
        Assert.That(types.ItemsByExpressionName("building"), Is.SameAs(building), "expression names ignore case");
        Assert.That(types.ItemsByDelphiName("tperson"), Is.SameAs(person), "Delphi names ignore case");
        Assert.That(types.ItemsByModelName("The Building"), Is.SameAs(building));
        Assert.That(types.ItemsByModelName("the building"), Is.Null, "model names are case-sensitive");
        Assert.That(types.ItemsByExpressionName("Nobody"), Is.Null);

        TBoldMetaElementList asMeta = types;
        Assert.That(asMeta[0], Is.SameAs(person));
        Assert.That(asMeta.ItemsByExpressionName("PERSON"), Is.SameAs(person));
        Assert.That(asMeta.ItemsByDelphiName("TBuilding"), Is.SameAs(building));
        Assert.That(asMeta.ItemsByModelName("Person"), Is.SameAs(person));

        var plain = new TBoldMetaElementList();
        plain.Add(person);
        Assert.That(plain.ItemsByDelphiName("TPerson"), Is.SameAs(person));
    }
}
