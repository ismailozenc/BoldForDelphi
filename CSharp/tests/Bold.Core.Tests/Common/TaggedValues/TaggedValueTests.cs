// Delphi: Source/Common/TaggedValues/*.pas
// Port map: CSharp/port/units/BoldDefaultTaggedValues.md, BoldTaggedValueList.md, BoldTaggedValueSupport.md, BoldUMLTaggedValues.md
namespace Bold.Tests;

public class TaggedValueTests
{
    private const string Support = "Source/Common/TaggedValues/BoldTaggedValueSupport.pas";

    [Test]
    [Covers(typeof(BoldDefaultTaggedValues), "*")]
    public void Default_tagged_value_constants_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldDefaultTaggedValues), "Source/Common/TaggedValues/BoldDefaultTaggedValues.pas");
    }

    [Test]
    [Covers(typeof(BoldUMLTaggedValues), "*")]
    public void UML_tagged_value_constants_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldUMLTaggedValues), "Source/Common/TaggedValues/BoldUMLTaggedValues.pas");
    }

    [TestCase("Model", "UnitName", "BusinessClasses", "String")]
    [TestCase("Model", "OptimisticLocking", "Off", "OptimisticLockingSet")]
    [TestCase("Class", "DelphiName", "T<Name>", "String")]
    [TestCase("Class", "TableMapping", "Own", "TableMappingSet")]
    [TestCase("Attribute", "Length", "255", "Integer")]
    [TestCase("AssociationEnd", "DeleteAction", "<Default>", "DeleteActions")]
    [TestCase("Operation", "OperationKind", "Common", "BoldOperationKindSet")]
    [TestCase("Parameter", "IsConst", "False", "Boolean")]
    [Covers(typeof(BoldDefaultTaggedValues), nameof(BoldDefaultTaggedValues.BoldDefaultTaggedValueList), "AddDefaultTaggedValues")]
    [Covers(typeof(TBoldTaggedValuePerClassList), "*")]
    [Covers(typeof(TBoldTaggedValueList), "*")]
    [Covers(typeof(TBoldTaggedValueDefinition), "*")]
    [Covers(typeof(TBoldTaggedValueDefinitionIndex), "*")]
    public void Bold_defaults_per_element_kind(string kind, string tag, string defaultValue, string typeName)
    {
        TBoldTaggedValuePerClassList list = BoldDefaultTaggedValues.BoldDefaultTaggedValueList();
        Assert.That(list.DefaultForClassAndTag(kind, tag), Is.EqualTo(defaultValue));
        TBoldTaggedValueDefinition def = list.ListForClassName(kind).DefinitionForTag(tag)!;
        Assert.That(def.TypeName, Is.EqualTo(typeName));
        Assert.That(def.Tag, Is.EqualTo(tag));
    }

    [Test]
    [Covers(typeof(TBoldTaggedValueList), "DefaultValueForTag", "Count", "Definition")]
    public void Lists_have_the_Delphi_counts_and_unknown_tags_raise()
    {
        TBoldTaggedValuePerClassList list = BoldDefaultTaggedValues.BoldDefaultTaggedValueList();
        Assert.That(list.ListForClassName("Model").Count, Is.EqualTo(23));
        Assert.That(list.ListForClassName("Class").Count, Is.EqualTo(19));
        Assert.That(list.ListForClassName("Association").Count, Is.EqualTo(4));
        Assert.That(list.ListForClassName("Attribute").Count, Is.EqualTo(19));
        Assert.That(list.ListForClassName("AssociationEnd").Count, Is.EqualTo(11));
        Assert.That(list.ListForClassName("Operation").Count, Is.EqualTo(5));
        Assert.That(list.ListForClassName("Parameter").Count, Is.EqualTo(1));
        Assert.That(list.ListForClassName("Model").Definition(0).Tag, Is.EqualTo("GenerateMultiplicityConstraints"));
        Assert.Throws<EBoldInternal>(() => list.DefaultForClassAndTag("Class", "NoSuchTag"));
    }

    [Test]
    [Covers(typeof(BoldUMLTaggedValues), nameof(BoldUMLTaggedValues.UMLTaggedValueList), "AddDefaultTaggedValues")]
    public void UML_defaults()
    {
        TBoldTaggedValuePerClassList list = BoldUMLTaggedValues.UMLTaggedValueList();
        Assert.That(list.DefaultForClassAndTag("Class", "persistence"), Is.EqualTo("persistent"));
        Assert.That(list.DefaultForClassAndTag("Attribute", "derived"), Is.EqualTo("False"));
        Assert.That(list.ListForClassName("Association").Count, Is.EqualTo(2));
    }

    [Test]
    [Covers(typeof(TBoldOptimisticLockingMode))]
    [Covers(typeof(TBoldAttributeKind))]
    [Covers(typeof(TDeleteAction))]
    [Covers(typeof(TDelphiFunctionType))]
    [Covers(typeof(TTableMapping))]
    [Covers(typeof(TDelphiPropertyAccessKind))]
    [Covers(typeof(TBoldEvolutionState))]
    [Covers(typeof(TBoldNationalCharConversion))]
    [Covers(typeof(TBoldAssociationEndDefaultRegionMode))]
    public void Enums_have_the_Delphi_members_in_order()
    {
        PortAssert.EnumsMatchDelphi(Support, typeof(TBoldOptimisticLockingMode), typeof(TBoldAttributeKind), typeof(TDeleteAction),
            typeof(TDelphiFunctionType), typeof(TTableMapping), typeof(TDelphiPropertyAccessKind), typeof(TBoldEvolutionState),
            typeof(TBoldNationalCharConversion), typeof(TBoldAssociationEndDefaultRegionMode));
    }

    private static void RoundTrips<T>(Action<TStrings> add, Func<T, string> toString, Func<string, T> fromString) where T : struct, Enum
    {
        var strings = new TStringList();
        add(strings);
        Assert.That(strings.Count, Is.EqualTo(Enum.GetValues<T>().Length), typeof(T).Name);
        foreach (T value in Enum.GetValues<T>())
        {
            Assert.That(fromString(toString(value)), Is.EqualTo(value), $"{typeof(T).Name}.{value}");
        }
    }

    [Test]
    [Covers(typeof(TBoldTaggedValueSupport), "*")]
    public void Every_enum_value_round_trips_through_its_string()
    {
        RoundTrips<TBoldEvolutionState>(TBoldTaggedValueSupport.AddEvolutionStates, TBoldTaggedValueSupport.EvolutionStateToString, TBoldTaggedValueSupport.StringToEvolutionState);
        RoundTrips<TBoldNationalCharConversion>(TBoldTaggedValueSupport.AddNationalCharConversions, TBoldTaggedValueSupport.NationalCharConversionToString, TBoldTaggedValueSupport.StringToNationalCharConversion);
        RoundTrips<TBoldAssociationEndDefaultRegionMode>(TBoldTaggedValueSupport.AddDefaultRegionModes, TBoldTaggedValueSupport.DefaultRegionModeToString, TBoldTaggedValueSupport.StringToDefaultRegionMode);
        RoundTrips<TBoldOptimisticLockingMode>(TBoldTaggedValueSupport.AddOptimisticLockingModes, TBoldTaggedValueSupport.OptimisticLockingModeToString, TBoldTaggedValueSupport.StringToOptimisticLockingMode);
        RoundTrips<TDeleteAction>(TBoldTaggedValueSupport.AddDeleteActions, TBoldTaggedValueSupport.DeleteActionToString, TBoldTaggedValueSupport.StringToDeleteAction);
        RoundTrips<TDelphiFunctionType>(TBoldTaggedValueSupport.AddDelphiFunctionTypes, TBoldTaggedValueSupport.DelphiFunctionTypeToString, TBoldTaggedValueSupport.StringToDelphiFunctionType);
        RoundTrips<TDelphiPropertyAccessKind>(TBoldTaggedValueSupport.AddPropertyAccessKinds, TBoldTaggedValueSupport.DelphiPropertyAccessKindToString, TBoldTaggedValueSupport.StringToDelphiPropertyAccessKind);
        RoundTrips<TBoldAttributeKind>(TBoldTaggedValueSupport.AddAttributeKinds, TBoldTaggedValueSupport.AttributeKindToString, TBoldTaggedValueSupport.StringToAttributeKind);
        RoundTrips<TTableMapping>(TBoldTaggedValueSupport.AddTableMappings, TBoldTaggedValueSupport.TableMappingToString, TBoldTaggedValueSupport.StringToTableMapping);

        Assert.That(TBoldTaggedValueSupport.StringToOptimisticLockingMode("member"), Is.EqualTo(TBoldOptimisticLockingMode.bolmModifiedMembers), "old name");
        Assert.That(TBoldTaggedValueSupport.StringToOptimisticLockingMode("Class"), Is.EqualTo(TBoldOptimisticLockingMode.bolmAllMembers), "old name");
        Assert.That(TBoldTaggedValueSupport.StringToDelphiFunctionType("AbstractVirtual"), Is.EqualTo(TDelphiFunctionType.dfAbstractVirtual), "legacy");
        Assert.That(TBoldTaggedValueSupport.StringToTableMapping("bogus"), Is.EqualTo(TTableMapping.tmOwn), "unknown strings give the default");
        Assert.That(TBoldTaggedValueSupport.StringToStorage("ExternalKey"), Is.EqualTo(TBoldStorage.bsExternalKey));
        Assert.That(TBoldTaggedValueSupport.StringToStorage("bogus"), Is.EqualTo(TBoldStorage.bsInternal));
        Assert.That(TBoldTaggedValueSupport.StringToBoolean("TRUE"), Is.True);
        Assert.Throws<EBold>(() => TBoldTaggedValueSupport.StringToBoolean("yes"));
        Assert.Throws<EBold>(() => TBoldTaggedValueSupport.TableMappingToString((TTableMapping)99));
        Assert.That(BoldDefaultTaggedValues.TVIsTrue("true"), Is.True);
        Assert.That(BoldDefaultTaggedValues.TVIsFalse("FALSE"), Is.True);
    }
}
