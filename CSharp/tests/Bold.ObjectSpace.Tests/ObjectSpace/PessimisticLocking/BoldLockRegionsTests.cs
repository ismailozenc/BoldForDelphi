// Delphi: Source/ObjectSpace/PessimisticLocking/BoldLockRegions.pas
// Port map: CSharp/port/units/BoldLockRegions.md
// Lock regions: definitions parsed from text, regions found for objects and members, and subregions through roles.
namespace Bold.Tests;

public class BoldLockRegionsTests
{
    private static TBoldMember M(TBoldObject o, string name) => o.BoldMemberByExpressionName(name);

    [Test]
    [Covers(typeof(TBoldRegionParser), "*")]
    [Covers(typeof(TBoldRegionDefinitions), "*")]
    [Covers(typeof(TBoldRegionCoreDefinition), "*")]
    [Covers(typeof(TBoldConcreteRegionDefinition), "*")]
    [Covers(typeof(TBoldSubregionReference), "*")]
    [Covers(typeof(TBoldRegionElementInclusion), "*")]
    public void Definitions_parse_with_members_subregions_and_errors()
    {
        using var s = new ObjectSpaceSample();
        var definitions = new TBoldRegionDefinitions();
        var parser = new TBoldRegionParser(definitions, s.SystemTypeInfo);
        var lines = new TStringList();
        lines.Add("Home[Building]: address | Resident[residents]");
        lines.Add("Resident[Person]: firstName, age | Resident[friends]-");
        Assert.That(parser.Parse(lines), Is.True, string.Join("; ", parser.Errors.ToStringArray()));

        TBoldRegionCoreDefinition home = definitions.CoreDefinition("Home")!;
        TBoldClassTypeInfo building = s.SystemTypeInfo.ClassTypeInfoByExpressionName("Building")!;
        TBoldClassTypeInfo person = s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!;
        TBoldConcreteRegionDefinition homeOfBuilding = home.ConcreteDefinitions.FindByRootClass(building)!;
        Assert.That(homeOfBuilding.RootClass, Is.SameAs(building));
        Assert.That(definitions.CoreDefinition("Resident")!.ConcreteDefinitions.FindByRootClass(person), Is.Not.Null);
        Assert.That(definitions.CoreDefinition("Nothing"), Is.Null);
        Assert.That(definitions.RegionInclusionsByMember(person.MemberRTInfoByExpressionName("age")!).Count, Is.EqualTo(1), "age is in the Resident region");
        Assert.That(definitions.RegionInclusionsByMember(person.MemberRTInfoByExpressionName("salary")!).Count, Is.EqualTo(0));

        lines.Clear();
        lines.Add("Bad[Person firstName");
        lines.Add("Odd[Nobody]: name");
        lines.Add("Wrong[Person]: nothing | Sub[age]");
        Assert.That(parser.Parse(lines), Is.False);
        string[] errors = parser.Errors.ToStringArray();
        Assert.That(errors, Has.Some.StartsWith("\"Bad[Person firstName\" (12): "), "a syntax error names the line and position");
        Assert.That(errors, Has.Some.StartsWith("\"Odd[Nobody]: name\" (5): "), "an unknown class is reported");
        Assert.That(errors.Count(e => e.StartsWith("\"Wrong[Person]", StringComparison.Ordinal)), Is.EqualTo(2), "an unknown member and a subregion over an attribute");
    }

    [Test]
    [Covers(typeof(TBoldRegionFactory), "*")]
    [Covers(typeof(TBoldRegion), "*")]
    [Covers(typeof(TBoldRegionList), "*")]
    [Covers(typeof(TBoldRegionLookup), "FindByID", "AddRegionList")]
    public void Regions_are_found_for_objects_and_members()
    {
        using var s = new ObjectSpaceSample();
        var definitions = new TBoldRegionDefinitions();
        var parser = new TBoldRegionParser(definitions, s.SystemTypeInfo);
        var lines = new TStringList();
        lines.Add("Home[Building]: address | Resident[residents]");
        lines.Add("Resident[Person]: firstName, age");
        Assert.That(parser.Parse(lines), Is.True);

        TBoldObject ann = s.NewPerson(), bob = s.NewPerson(), house = s.NewBuilding();
        ((TBoldObjectReference)M(ann, "home")).BoldObject = house;
        ((TBoldObjectReference)M(bob, "home")).BoldObject = house;
        using var factory = new TBoldRegionFactory(definitions);

        var found = new TList();
        factory.GetRegionsForElement(M(ann, "age"), found);
        Assert.That(found.Count, Is.EqualTo(1));
        var annRegion = (TBoldRegion)found[0]!;
        Assert.That(annRegion.Root, Is.SameAs(ann));
        Assert.That(annRegion.Definition.CoreDefinition.Name, Is.EqualTo("Resident"));
        Assert.That(factory.GetRegion(definitions.CoreDefinition("Resident")!, ann.BoldObjectLocator!), Is.SameAs(annRegion), "a region is made once");
        Assert.That(factory.GetRegionByName(annRegion.AsString), Is.SameAs(annRegion), "and found by its id");

        var elements = new TList();
        annRegion.GetElements(elements);
        Assert.That(elements.Count, Is.EqualTo(3), "the root, firstName and age");
        Assert.That(elements[0], Is.SameAs(ann));

        found.Clear();
        factory.GetRegionsForElement(house, found);
        var homeRegion = (TBoldRegion)found[0]!;
        var subregions = new TBoldRegionList();
        homeRegion.FillDependentSubregions(subregions);
        Assert.That(subregions.Count, Is.EqualTo(2), "a resident region for each resident");
        Assert.That(new[] { subregions[0].Root, subregions[1].Root }, Is.EquivalentTo(new[] { ann, bob }));
        var independent = new TBoldRegionList();
        homeRegion.FillIndependentSubregions(independent);
        Assert.That(independent.Count, Is.EqualTo(0));
    }
}
