// Delphi: Source/MoldModel/Core/BoldMeta.pas
// Port map: CSharp/port/units/BoldMeta.md
namespace Bold.Tests;

/// <summary>The Mold model: building, tagged values, link roles, member lists, dispatch ids, checksums and removal.</summary>
public class BoldMetaTests
{
    /// <summary>
    /// Person.employer (0..1) to Company.employees (*), with Job as link class; Manager subclasses Person. An end's
    /// MoldClass is the class that has it as a member.
    /// </summary>
    private sealed class Sample : IDisposable
    {
        public readonly TMoldModel Model = new(null, "Org");
        public readonly TMoldClass Person;
        public readonly TMoldClass Manager;
        public readonly TMoldClass Company;
        public readonly TMoldClass Job;
        public readonly TMoldAssociation Employment;
        public readonly TMoldRole Employer;
        public readonly TMoldRole Employees;

        public Sample()
        {
            Person = new TMoldClass(Model, "Person") { SuperClass = Model.RootClass };
            Manager = new TMoldClass(Model, "Manager") { SuperClass = Person };
            Company = new TMoldClass(Model, "Company") { SuperClass = Model.RootClass };
            Job = new TMoldClass(Model, "Job") { SuperClass = Model.RootClass };
            _ = new TMoldAttribute(Person, "firstName");
            var age = new TMoldAttribute(Person, "age") { BoldType = "Integer" };
            age.SetBoldTV(BoldDefaultTaggedValues.TAG_ATTRIBUTEKIND, BoldDefaultTaggedValues.TV_ATTRIBUTEKIND_DELPHI);
            age.SetBoldTV(BoldDefaultTaggedValues.TAG_DPREAD, BoldDefaultTaggedValues.TV_DPPRIVATEMETHOD);
            _ = new TMoldAttribute(Company, "name");
            Employment = new TMoldAssociation(Model, "Employment") { LinkClass = Job };
            Employer = new TMoldRole(Employment, "employer") { MoldClass = Person, Multiplicity = "0..1" };
            Employees = new TMoldRole(Employment, "employees") { MoldClass = Company, Multiplicity = "0..*" };
        }

        public void Dispose() => Model.Dispose();
    }

    [Test]
    [Covers(typeof(BoldMeta), nameof(BoldMeta.BOLD_FIRSTUSERDISPID), nameof(BoldMeta.BOLD_DISPID_ROUND))]
    [Covers(typeof(TBoldDispIdAssigningState))]
    public void Constants_and_enums_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldMeta), "Source/MoldModel/Core/BoldMeta.pas");
        PortAssert.EnumsMatchDelphi("Source/MoldModel/Core/BoldMeta.pas", typeof(TBoldDispIdAssigningState));
    }

    [Test]
    [Covers(typeof(TMoldElement), "*")]
    [Covers(typeof(TMoldModel), nameof(TMoldModel.Classes), nameof(TMoldModel.RootClass), nameof(TMoldModel.GetClassByName),
        nameof(TMoldModel.DefaultDeleteAction), nameof(TMoldModel.UseXFiles), nameof(TMoldModel.UseTimestamp), nameof(TMoldModel.UseGlobalId),
        nameof(TMoldModel.UseReadOnly), nameof(TMoldModel.UseModelVersion), nameof(TMoldModel.ModelVersion), nameof(TMoldModel.UseClockLog),
        nameof(TMoldModel.UnitName), nameof(TMoldModel.ExpandedUnitName), nameof(TMoldModel.OptimisticLocking), nameof(TMoldModel.NationalCharConversion),
        nameof(TMoldModel.InterfaceUses), nameof(TMoldModel.ImplementationUses), nameof(TMoldModel.GUID), nameof(TMoldModel.TypeLibVersion),
        nameof(TMoldModel.UpdateWholeObjects), nameof(TMoldModel.RegionDefinitions), nameof(TMoldModel.GenerateDefaultRegions))]
    public void New_model_has_a_root_class_and_Bold_defaults()
    {
        using var model = new TMoldModel(null, "Shop");
        Assert.That(model.RootClass.Name, Is.EqualTo("ShopRoot"));
        Assert.That(model.Classes.Count, Is.EqualTo(1));
        Assert.That(model.RootClass.IsRootClass, Is.True);
        Assert.That(model.GetClassByName("ShopRoot"), Is.SameAs(model.RootClass));
        Assert.That(model.DefaultDeleteAction(TAggregationKind.akNone), Is.EqualTo(TDeleteAction.daAllow));
        Assert.That(model.DefaultDeleteAction(TAggregationKind.akAggregate), Is.EqualTo(TDeleteAction.daProhibit));
        Assert.That(model.DefaultDeleteAction(TAggregationKind.akComposite), Is.EqualTo(TDeleteAction.daCascade));
        Assert.That((model.UseXFiles, model.UseTimestamp, model.UseGlobalId, model.UseReadOnly, model.UseClockLog), Is.EqualTo((true, true, true, true, true)));
        Assert.That(model.UseModelVersion, Is.False);
        Assert.That(model.ModelVersion, Is.EqualTo(0));
        Assert.That(model.UnitName, Is.EqualTo("BusinessClasses"));
        Assert.That(model.ExpandedUnitName, Is.EqualTo("BusinessClasses"));
        Assert.That(model.OptimisticLocking, Is.EqualTo(TBoldOptimisticLockingMode.bolmOff));
        Assert.That(model.NationalCharConversion, Is.EqualTo(TBoldNationalCharConversion.nccDefault));
        Assert.That(model.TypeLibVersion, Is.EqualTo("1.0"));
        Assert.That((model.InterfaceUses, model.ImplementationUses, model.GUID, model.RegionDefinitions), Is.EqualTo(("", "", "", "")));
        Assert.That(model.UpdateWholeObjects || model.GenerateDefaultRegions, Is.False);
        Assert.That(model.DelphiName, Is.EqualTo("<Name>"), "the model stores DelphiName=<Name>, as Delphi");
        Assert.That(model.ExpandedDelphiName, Is.EqualTo("Shop"));
        Assert.That(model.PMapperName, Is.EqualTo("<Default>"));

        // The root class's Bold defaults.
        TMoldClass root = model.RootClass;
        Assert.That(root.DelphiName, Is.EqualTo("T<Name>"));
        Assert.That(root.ExpandedDelphiName, Is.EqualTo("TShopRoot"));
        Assert.That(root.ExpressionName, Is.EqualTo("<Name>"));
        Assert.That(root.ExpandedExpressionName, Is.EqualTo("ShopRoot"));
        Assert.That(root.TableMapping, Is.EqualTo(TTableMapping.tmOwn));
        Assert.That(root.HasDispId, Is.False);
        Assert.Throws<EBold>(() => _ = root.DispId);
    }

    [Test]
    [Covers(typeof(TMoldElement), nameof(TMoldElement.BoldTVByName), nameof(TMoldElement.StdTVByName), nameof(TMoldElement.TVByName),
        nameof(TMoldElement.NonDefaultTaggedValuesCommaText), nameof(TMoldElement.AddAllTaggedValues), nameof(TMoldElement.GetAllNonBoldTaggedValues))]
    public void Tagged_values_store_only_what_differs_from_the_default()
    {
        using var s = new Sample();
        TMoldAttribute firstName = s.Person.Attributes.ItemsByName("firstName")!;
        Assert.That(firstName.GetAllNonBoldTaggedValues(), Is.Null);
        firstName.SetBoldTV("Length", " 255 ");
        Assert.That(firstName.NonDefaultTaggedValuesCommaText, Is.EqualTo(""), "a default value is not stored");
        firstName.SetTV("Bold.Length", "40");
        firstName.SetTV("note", " hello ");
        firstName.SetStdTV("persistence", "transient");
        Assert.That(firstName.Length, Is.EqualTo(40));
        Assert.That(firstName.TVByName("note"), Is.EqualTo("hello"), "values are trimmed");
        Assert.That(firstName.TVByName("Bold.Length"), Is.EqualTo("40"));
        Assert.That(firstName.TVByName("missing"), Is.EqualTo(""));
        Assert.That(firstName.StdTVByName("persistence"), Is.EqualTo("transient"));
        Assert.That(firstName.Persistent, Is.False);
        Assert.That(firstName.NonDefaultTaggedValuesCommaText, Is.EqualTo("note=hello,persistence=transient,Bold.Length=40"));

        var copy = new TMoldAttribute(s.Person, "copy") { NonDefaultTaggedValuesCommaText = "Persistence=persistent,Bold.Length=12" };
        Assert.That(copy.StdTVByName("persistence"), Is.EqualTo("persistent"), "the old tag Persistence is read as persistence");
        Assert.That(copy.Length, Is.EqualTo(12));
        var tags = new TStringList();
        var values = new TStringList();
        copy.AddAllTaggedValues(tags, values);
        Assert.That(tags.Count, Is.EqualTo(BoldDefaultTaggedValues.BoldDefaultTaggedValueList().ListForClassName("Attribute").Count + 1));
        Assert.That(values[tags.IndexOf("Bold.Length")], Is.EqualTo("12"));
        Assert.That(values[tags.IndexOf("Bold.AllowNULL")], Is.EqualTo("False"));
        Assert.That(copy.GetAllNonBoldTaggedValues()!.CommaText, Is.EqualTo("persistence=persistent"));
    }

    [Test]
    [Covers(typeof(TMoldClass), nameof(TMoldClass.SuperClass), nameof(TMoldClass.SubClasses), nameof(TMoldClass.ChildTo),
        nameof(TMoldClass.LowestCommonSuperClass), nameof(TMoldClass.Attributes), nameof(TMoldClass.ExpandedInterfaceName))]
    [Covers(typeof(TMoldClassList), "*")]
    [Covers(typeof(TMoldElementList), "*")]
    public void Class_hierarchy_and_lookups()
    {
        using var s = new Sample();
        Assert.That(s.Manager.ChildTo(s.Person), Is.True);
        Assert.That(s.Person.ChildTo(s.Manager), Is.False);
        Assert.That(s.Manager.LowestCommonSuperClass(s.Company), Is.SameAs(s.Model.RootClass));
        Assert.That(s.Person.LowestCommonSuperClass(s.Manager), Is.SameAs(s.Person));
        Assert.That(s.Person.SubClasses.ItemsByName("Manager"), Is.SameAs(s.Manager));
        Assert.Throws<EBold>(() => s.Person.SuperClass = s.Manager, "recursive assignment");
        Assert.That(s.Model.Classes.ItemsByExpressionName("person"), Is.SameAs(s.Person), "expression names ignore case");
        Assert.That(s.Model.Classes.ItemsByDelphiName("TCOMPANY"), Is.SameAs(s.Company));
        Assert.That(s.Model.Classes.ItemsByName("person"), Is.Null, "names are case-sensitive");
        Assert.That(s.Model.Classes.MakeUniqueName("Person"), Is.EqualTo("Person1"));
        Assert.That(s.Person.ExpandedInterfaceName, Is.EqualTo("IPerson"));
        s.Person.Name = "Human";
        Assert.That(s.Model.Classes.ItemsByName("Human"), Is.SameAs(s.Person), "renaming re-indexes");
        Assert.That(s.Model.RootClass.SubClasses.ItemsByName("Human"), Is.SameAs(s.Person));
    }

    [Test]
    [Covers(typeof(TMoldMember), "*")]
    [Covers(typeof(TMoldAttribute), "*")]
    public void Member_expression_names_start_in_lower_case()
    {
        using var s = new Sample();
        var attr = new TMoldAttribute(s.Person, "LastName");
        Assert.That(attr.ExpandedExpressionName, Is.EqualTo("lastName"));
        Assert.That(attr.ExpandedDelphiName, Is.EqualTo("LastName"));
        Assert.That(attr.ColumnName, Is.EqualTo("<Name>"));
        Assert.That(attr.BoldType, Is.EqualTo("String"));
        Assert.That(attr.AttributeKind, Is.EqualTo(TBoldAttributeKind.bastBold));
        Assert.That(attr.AllowNull || attr.HasDelphiField || attr.Derived || attr.ReverseDerived || attr.ManuallyDerived, Is.False);
        Assert.That(attr.Length, Is.EqualTo(255));
        Assert.That(attr.EvolutionState, Is.EqualTo(TBoldEvolutionState.esNormal));
        Assert.That(attr.Storage, Is.EqualTo(TBoldStorage.bsInternal));
        Assert.That(attr.DefaultDBValue, Is.EqualTo(""));
        Assert.That(attr.MemberExists, Is.True);
        attr.SetStdTV("persistence", "persistent");
        Assert.That(attr.EffectivePersistent, Is.True);
        attr.Derived = true;
        Assert.That(attr.ManuallyDerived, Is.True, "derived without OCL");
        Assert.That(attr.EffectivePersistent, Is.False);
        attr.SetBoldTV("DerivationOCL", "firstName");
        Assert.That(attr.DerivationOCL, Is.EqualTo("firstName"));
        Assert.That(attr.ManuallyDerived, Is.False);
        Assert.That(attr.EffectiveDelayedFetch, Is.False);
        attr.SetBoldTV(BoldDefaultTaggedValues.TAG_EVOLUTIONSTATE, "ToBeRemoved");
        Assert.That(attr.EffectiveDelayedFetch, Is.True);
        var model = s.Model;
        model.TypeNameDictionary = new TBoldTypeNameDictionary(null);
        model.TypeNameDictionary.AddDefaultMappings();
        Assert.That(attr.TypeStreamName, Is.EqualTo("String"));
        s.Person.Attributes.ItemsByName("age")!.BoldType = "Word";
        Assert.That(s.Person.Attributes.ItemsByName("age")!.TypeStreamName, Is.EqualTo("Integer"));
    }

    [Test]
    [Covers(typeof(TMoldRole), "*")]
    [Covers(typeof(TMoldAssociation), "*")]
    [Covers(typeof(TMoldModel), nameof(TMoldModel.EnsureLinkRoles), nameof(TMoldModel.Associations), nameof(TMoldModel.FindRoleByClassNameAndName))]
    [Covers(typeof(TMoldRoleList), "*")]
    [Covers(typeof(TMoldAssociationList), "*")]
    public void Link_class_gets_link_roles_and_inner_link_roles()
    {
        using var s = new Sample();
        Assert.That(s.Employer.OtherEnd, Is.SameAs(s.Employees));
        Assert.That(s.Employees.Multi, Is.True);
        Assert.That(s.Employer.Multi || s.Employer.Mandatory, Is.False);
        Assert.That(s.Model.FindRoleByClassNameAndName("Person", "employer"), Is.SameAs(s.Employer), "an end is in the role list of its class");
        Assert.That(s.Job.Association, Is.SameAs(s.Employment));
        Assert.That(s.Employer.HasLinkRole, Is.True);

        s.Model.EnsureLinkRoles();
        TMoldRole linkRole = s.Employer.LinkRole!;
        Assert.That(linkRole.Name, Is.EqualTo("Job"));
        Assert.That(linkRole.RoleType, Is.EqualTo(TBoldRoleType.rtLinkRole));
        Assert.That(linkRole.MoldClass, Is.SameAs(s.Person));
        Assert.That(linkRole.MainRole, Is.SameAs(s.Employer));
        Assert.That(linkRole.Embed, Is.False);
        TMoldRole inner = linkRole.OtherEnd!;
        Assert.That(inner.RoleType, Is.EqualTo(TBoldRoleType.rtInnerLinkRole));
        Assert.That(inner.Name, Is.EqualTo("employees"));
        Assert.That(inner.MoldClass, Is.SameAs(s.Job));
        Assert.That(inner.Multi, Is.False);
        Assert.That(inner.EffectiveEmbedded, Is.True);
        Assert.That(inner.OtherEnd, Is.SameAs(linkRole));
        Assert.That(s.Employer.EffectiveEmbedded, Is.False, "ends with a link class are not embedded");

        Assert.That(s.Employer.TypeStreamName, Is.EqualTo("ObjectIdRefPair"));
        Assert.That(s.Employees.TypeStreamName, Is.EqualTo("ObjectIdListRefPair"));
        Assert.That(inner.TypeStreamName, Is.EqualTo("ObjectIdRef"));

        Assert.That(s.Employer.EffectiveDeleteAction, Is.EqualTo(TDeleteAction.daAllow));
        s.Employer.Aggregation = TAggregationKind.akComposite;
        Assert.That(s.Employer.EffectiveDeleteAction, Is.EqualTo(TDeleteAction.daCascade));
        Assert.That(s.Employer.EffectiveDefaultRegionMode, Is.EqualTo(TBoldAssociationEndDefaultRegionMode.aedrmCascade));
        Assert.That(s.Employees.EffectiveDefaultRegionMode, Is.EqualTo(TBoldAssociationEndDefaultRegionMode.aedrmNone));
        Assert.That(s.Employees.EffectiveOrdered, Is.False);
        s.Employees.Ordered = true;
        Assert.That(s.Employees.EffectiveOrdered, Is.True);
        Assert.That(s.Employees.EffectiveDelayedFetch, Is.True, "multi ends are fetched on demand");
    }

    [Test]
    [Covers(typeof(TMoldClass), nameof(TMoldClass.AllBoldMembers), nameof(TMoldClass.AllNativeAttributes), nameof(TMoldClass.FirstOwnBoldMemberIndex),
        nameof(TMoldClass.FirstOwnNativeAttributeIndex), nameof(TMoldClass.HasDelphiAttributesWithAccessorFunctions), nameof(TMoldClass.HasCodeStubs),
        nameof(TMoldClass.IntroducesManuallyDerivedMembers), nameof(TMoldClass.HasManuallyDerivedMembers))]
    [Covers(typeof(TMoldMemberList), "*")]
    [Covers(typeof(TMoldAttributeList), "*")]
    public void All_members_list_inherited_members_first()
    {
        using var s = new Sample();
        string Names(TMoldMemberList l) => string.Join(",", Enumerable.Range(0, l.Count).Select(i => l[i].Name));
        Assert.That(Names(s.Person.AllBoldMembers), Is.EqualTo("firstName,employer,Job"));
        Assert.That(Names(s.Manager.AllBoldMembers), Is.EqualTo("firstName,employer,Job"));
        Assert.That(s.Manager.FirstOwnBoldMemberIndex, Is.EqualTo(3));
        Assert.That(Names(s.Job.AllBoldMembers), Is.EqualTo("employees,employer"), "the inner link roles of the link class");
        Assert.That(s.Person.AllNativeAttributes.Count, Is.EqualTo(1));
        Assert.That(s.Person.FirstOwnNativeAttributeIndex, Is.EqualTo(0));
        Assert.That(s.Manager.FirstOwnNativeAttributeIndex, Is.EqualTo(1));
        Assert.That(s.Person.HasDelphiAttributesWithAccessorFunctions, Is.True);
        Assert.That(s.Manager.HasDelphiAttributesWithAccessorFunctions, Is.False, "only own native attributes count");
        Assert.That(s.Person.HasCodeStubs, Is.True);
        Assert.That(s.Company.HasCodeStubs, Is.False);
        var derived = new TMoldAttribute(s.Company, "total") { Derived = true };
        Assert.That(s.Company.IntroducesManuallyDerivedMembers, Is.True);
        Assert.That(derived.ManuallyDerived, Is.True);
    }

    [Test]
    [Covers(typeof(TMoldMethod), "*")]
    [Covers(typeof(TMoldParameter), "*")]
    [Covers(typeof(TMoldMethodList), "*")]
    [Covers(typeof(TMoldClass), nameof(TMoldClass.Methods), nameof(TMoldClass.AllAutoOverrideMethods))]
    public void Method_signatures_parse_into_parameters()
    {
        using var s = new Sample();
        var m = new TMoldMethod(s.Person, "Hire") { ReturnType = "Company" };
        m.Signature = "a, b: Integer; const s: string; var x: Double; out y: Boolean; who: Person";
        Assert.That(m.Parameters.Count, Is.EqualTo(6));
        Assert.That(m.CallSignature, Is.EqualTo("a, b, s, x, y, who"));
        Assert.That(m.Signature, Is.EqualTo("a: Integer; b: Integer; const s: string; var x: Double; out y: Boolean; who: Person"));
        var who = (TMoldParameter)m.Parameters[5]!;
        Assert.That(who.DelphiParameterType, Is.EqualTo("TPerson"));
        Assert.That(((TMoldParameter)m.Parameters[3]!).ParameterKind, Is.EqualTo(TBoldParameterDirectionKind.pdInout));
        Assert.That(((TMoldParameter)m.Parameters[2]!).IsConst, Is.True);
        Assert.That(m.DelphiReturnType, Is.EqualTo("TCompany"));
        Assert.That(m.HasReturnValue, Is.True);
        Assert.That(m.FuncType, Is.EqualTo(TDelphiFunctionType.dfNormal));
        Assert.That(m.CanCallInherited, Is.False);

        var baseMethod = new TMoldMethod(s.Person, "Describe");
        baseMethod.SetBoldTV(BoldDefaultTaggedValues.TAG_OVERRIDEINALLSUBCLASSES, "True");
        baseMethod.SetBoldTV(BoldDefaultTaggedValues.TAG_DELPHIOPERATIONKIND, "Virtual");
        Assert.That(s.Manager.AllAutoOverrideMethods.ItemsByName("Describe"), Is.SameAs(baseMethod));
        var over = new TMoldMethod(s.Manager, "Describe");
        over.SetBoldTV(BoldDefaultTaggedValues.TAG_DELPHIOPERATIONKIND, "Override");
        Assert.That(over.CanCallInherited, Is.True);
        baseMethod.SetBoldTV(BoldDefaultTaggedValues.TAG_DELPHIOPERATIONKIND, "Abstract");
        Assert.That(over.CanCallInherited, Is.False, "the inherited method is abstract");
        Assert.That(s.Person.Methods.ItemsByName("Hire"), Is.SameAs(m));
    }

    [Test]
    [Covers(typeof(TMoldModel), nameof(TMoldModel.EnsureTopSorted), nameof(TMoldModel.TopSorted), nameof(TMoldModel.DispIdsAssigned))]
    [Covers(typeof(TMoldClass), nameof(TMoldClass.FirstDispId), nameof(TMoldClass.LastDispId), nameof(TMoldClass.TopSortedIndex))]
    public void Dispatch_ids_follow_the_class_tree()
    {
        using var s = new Sample();
        var m = new TMoldMethod(s.Person, "Hire") { Visibility = TVisibilityKind.vkPublic, Signature = "x: Integer" };
        var hidden = new TMoldMethod(s.Person, "Secret") { Visibility = TVisibilityKind.vkPrivate };
        var badType = new TMoldMethod(s.Person, "Odd") { Visibility = TVisibilityKind.vkPublic, Signature = "p: Pointer" };
        TMoldAttribute firstName = s.Person.Attributes.ItemsByName("firstName")!;
        // As in Delphi, each class numbers all its members, inherited ones too, so an inherited member keeps the id
        // the last subclass gave it: Manager's numbering starts at 1280.
        Assert.That(firstName.DispId, Is.EqualTo(1280));
        Assert.That(s.Model.DispIdsAssigned, Is.True);
        Assert.That(s.Model.TopSorted, Is.True);
        Assert.That(s.Model.RootClass.TopSortedIndex, Is.EqualTo(0));
        Assert.That(s.Model.Classes[1].Name, Is.EqualTo("Company"), "subclasses sorted by name, breadth first");
        Assert.That(s.Person.FirstDispId, Is.EqualTo(1024));
        Assert.That(s.Person.Attributes.ItemsByName("age")!.HasDispId, Is.False, "native attributes have none");
        Assert.That(m.HasDispId, Is.True);
        Assert.That(hidden.HasDispId || badType.HasDispId, Is.False);
        Assert.That(s.Person.LastDispId, Is.EqualTo(s.Person.FirstDispId + 3), "firstName, employer, Job, Hire");
        Assert.That(s.Manager.FirstDispId, Is.EqualTo(1280), "the superclass's last id rounded up to the next 256");
    }

    [Test]
    [Covers(typeof(TMoldModel), nameof(TMoldModel.CRC), nameof(TMoldModel.EnsureCRC))]
    [Covers(typeof(BoldMeta), "CRCHash", "SumCRCs", "ClassesComparer")]
    public void CRC_follows_the_Delphi_formula()
    {
        static uint Hash(string str)
        {
            uint h = 0;
            foreach (char c in str)
            {
                h = (((h << 3) & 2147483647) | (h >> 29)) ^ c;
            }

            return h;
        }

        static uint Sum(uint a, uint b) => (((a << 3) & 2147483647) | (a >> 29)) ^ b;

        using var model = new TMoldModel(null, "M");
        var c = new TMoldClass(model, "Item") { SuperClass = model.RootClass };
        var attr = new TMoldAttribute(c, "Weight") { BoldType = "Float" };
        uint root = Hash("TMRoot");
        uint item = Sum(Hash("TItem"), Sum(Hash("Weight"), Hash("Float")));
        uint expected = Sum(Sum(0, root), item);
        Assert.That(model.CRC(), Is.EqualTo(expected.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        attr.BoldType = "Integer";
        Assert.That(model.CRC(), Is.EqualTo(expected.ToString(System.Globalization.CultureInfo.InvariantCulture)), "kept once computed");
    }

    [Test]
    [Covers(typeof(TMoldClass), nameof(TMoldClass.EffectivePersistent), nameof(TMoldClass.Persistent), nameof(TMoldClass.IsAbstract),
        nameof(TMoldClass.EffectiveOptimisticLocking), nameof(TMoldClass.OptimisticLocking), nameof(TMoldClass.Versioned),
        nameof(TMoldClass.EffectiveDefaultStringRepresentation), nameof(TMoldClass.DefaultStringRepresentation), nameof(TMoldClass.EffectiveEvolutionState),
        nameof(TMoldClass.EvolutionState), nameof(TMoldClass.Storage), nameof(TMoldClass.GenerateDefaultRegion), nameof(TMoldClass.Imported),
        nameof(TMoldClass.TableName), nameof(TMoldClass.TableMapping), nameof(TMoldClass.HasNotNullMembers), nameof(TMoldClass.GUID), nameof(TMoldClass.ListGUID))]
    public void Class_settings_inherit_where_Delphi_inherits()
    {
        using var s = new Sample();
        s.Model.SetBoldTV(BoldDefaultTaggedValues.TAG_OPTIMISTICLOCKING, "TimeStamp");
        Assert.That(s.Manager.EffectiveOptimisticLocking, Is.EqualTo(TBoldOptimisticLockingMode.bolmTimeStamp));
        s.Person.SetBoldTV(BoldDefaultTaggedValues.TAG_OPTIMISTICLOCKING, "AllMembers");
        Assert.That(s.Manager.EffectiveOptimisticLocking, Is.EqualTo(TBoldOptimisticLockingMode.bolmAllMembers));
        Assert.That(s.Manager.Versioned, Is.False);
        s.Person.SetBoldTV(BoldDefaultTaggedValues.TAG_VERSIONED, "True");
        s.Manager.SetBoldTV(BoldDefaultTaggedValues.TAG_VERSIONED, "<Default>");
        Assert.That(s.Manager.Versioned, Is.True);
        s.Person.SetBoldTV(BoldDefaultTaggedValues.TAG_DEFAULTSTRINGREPRESENTATION, "firstName");
        Assert.That(s.Manager.EffectiveDefaultStringRepresentation, Is.EqualTo("firstName"));
        Assert.That(s.Manager.DefaultStringRepresentation, Is.EqualTo(""));
        s.Employment.SetBoldTV(BoldDefaultTaggedValues.TAG_EVOLUTIONSTATE, "ToBeRemoved");
        Assert.That(s.Job.EvolutionState, Is.EqualTo(TBoldEvolutionState.esNormal));
        Assert.That(s.Job.EffectiveEvolutionState, Is.EqualTo(TBoldEvolutionState.esToBeRemoved));
        Assert.That(s.Person.Storage, Is.EqualTo(TBoldStorage.bsInternal));
        Assert.That(s.Person.GenerateDefaultRegion, Is.True);
        Assert.That(s.Person.Imported, Is.False);
        Assert.That(s.Person.TableName, Is.EqualTo("<Name>"));
        Assert.That(s.Person.HasNotNullMembers, Is.True, "AllowNULL defaults to False");
        Assert.That((s.Person.GUID, s.Person.ListGUID), Is.EqualTo(("", "")));

        foreach (TMoldElement e in new TMoldElement[] { s.Employment })
        {
            e.SetStdTV("persistence", "persistent");
        }

        Assert.That(s.Person.EffectivePersistent && s.Company.EffectivePersistent && s.Job.EffectivePersistent, Is.True);
        Assert.That(s.Employment.EffectivePersistent, Is.True);
        Assert.That(s.Employer.EffectivePersistent, Is.True);
        s.Company.Persistent = false;
        Assert.That(s.Employment.EffectivePersistent, Is.False, "an end class is transient");
        Assert.That(s.Job.EffectivePersistent, Is.False, "a link class follows its association");
        s.Person.IsAbstract = true;
        s.Manager.Persistent = false;
        Assert.That(s.Person.EffectivePersistent, Is.False, "abstract with only transient subclasses");
    }

    [Test]
    [Covers(typeof(TMoldClass), nameof(TMoldClass.Component), nameof(TMoldClass.UnitName), nameof(TMoldClass.ExpandedUnitName),
        nameof(TMoldClass.IncFileName), nameof(TMoldClass.EffectiveIncFileName), nameof(TMoldClass.LowestVisibleAncestor))]
    [Covers(typeof(TMoldComponent), "*")]
    [Covers(typeof(TMoldComponentList), "*")]
    [Covers(typeof(TMoldModel), nameof(TMoldModel.Components), nameof(TMoldModel.MainComponent), nameof(TMoldModel.FindComponent),
        nameof(TMoldModel.EnsureComponent), nameof(TMoldModel.RenameComponent))]
    public void Classes_live_in_components()
    {
        using var s = new Sample();
        Assert.That(s.Person.Component, Is.SameAs(s.Model.MainComponent));
        Assert.That(s.Model.MainComponent.Name, Is.EqualTo("BusinessClasses"));
        s.Company.UnitName = "<Name>Unit";
        Assert.That(s.Company.ExpandedUnitName, Is.EqualTo("CompanyUnit"));
        Assert.That(s.Company.Component.Name, Is.EqualTo("CompanyUnit"));
        Assert.That(s.Model.Components.Count, Is.EqualTo(2));
        Assert.That(s.Model.FindComponent("companyunit"), Is.SameAs(s.Company.Component), "component names ignore case");
        Assert.That(s.Person.EffectiveIncFileName(".inc"), Is.EqualTo("BusinessClasses.inc"));
        Assert.That(s.Manager.EffectiveIncFileName(".inc"), Is.EqualTo("BusinessClasses.inc"));
        s.Person.IncFileName = "<Name>Code.pas";
        Assert.That(s.Person.EffectiveIncFileName(".inc"), Is.EqualTo("PersonCode.pas"));
        Assert.That(s.Manager.EffectiveIncFileName(".inc"), Is.EqualTo("PersonCode.pas"), "the superclass's file in the same component");

        TMoldComponent main = s.Model.MainComponent;
        TMoldComponent unit = s.Company.Component;
        Assert.That(s.Company.LowestVisibleAncestor(s.Manager), Is.Null, "the company unit does not see the main unit");
        unit.Dependencies.Add(main);
        Assert.That(unit.DependentOf(main), Is.True);
        Assert.That(s.Company.LowestVisibleAncestor(s.Manager), Is.SameAs(s.Manager));
        var deps = new TStringList();
        main.GetImplementationDependencies(deps);
        Assert.That(deps.CommaText, Is.EqualTo("CompanyUnit"));
        unit.GetImplementationDependencies(deps);
        Assert.That(deps.Count, Is.EqualTo(0));
        Assert.That(s.Model.RenameComponent("CompanyUnit", "Firms")!.Name, Is.EqualTo("Firms"));
        s.Company.Component = main;
        Assert.That(s.Company.UnitName, Is.EqualTo("BusinessClasses"));
    }

    [Test]
    [Covers(typeof(TMoldClass), nameof(TMoldClass.FindStoringClass))]
    public void Storing_class_follows_the_table_mapping()
    {
        using var s = new Sample();
        TMoldAttribute firstName = s.Person.Attributes.ItemsByName("firstName")!;
        Assert.That(s.Manager.FindStoringClass(s.Person, false, firstName), Is.SameAs(s.Person));
        var intern = new TMoldClass(s.Model, "Intern") { SuperClass = s.Person };
        intern.SetBoldTV(BoldDefaultTaggedValues.TAG_TABLEMAPPING, "Parent");
        var stipend = new TMoldAttribute(intern, "stipend");
        Assert.That(intern.FindStoringClass(intern, false, stipend), Is.SameAs(s.Person));

        // As in Delphi, setting a Bold tagged value back to its default does not clear a stored value.
        intern.SetBoldTV(BoldDefaultTaggedValues.TAG_TABLEMAPPING, "Own");
        Assert.That(intern.TableMapping, Is.EqualTo(TTableMapping.tmParent));

        s.Person.SetBoldTV(BoldDefaultTaggedValues.TAG_TABLEMAPPING, "Children");
        Assert.That(s.Manager.FindStoringClass(s.Person, false, firstName), Is.SameAs(s.Manager));
        Assert.That(s.Person.FindStoringClass(s.Person, false, firstName), Is.Null);
    }

    [Test]
    [Covers(typeof(TMoldClass), nameof(TMoldClass.AllPossibleNames), nameof(TMoldClass.FormerNames))]
    [Covers(typeof(TMoldAssociation), nameof(TMoldAssociation.AllPossibleNames), nameof(TMoldAssociation.FormerNames))]
    public void Possible_names_include_former_names()
    {
        using var model = new TMoldModel(null, "M");
        var a = new TMoldClass(model, "A") { SuperClass = model.RootClass };
        var b = new TMoldClass(model, "B") { SuperClass = model.RootClass };
        a.SetBoldTV(BoldDefaultTaggedValues.TAG_FORMERNAMES, "OldA,Older");
        Assert.That(a.AllPossibleNames.CommaText, Is.EqualTo("A,OldA,Older"));
        var assoc = new TMoldAssociation(model, "xy");
        var x = new TMoldRole(assoc, "x") { MoldClass = a };
        var y = new TMoldRole(assoc, "y") { MoldClass = b };
        x.SetBoldTV(BoldDefaultTaggedValues.TAG_FORMERNAMES, "w");
        Assert.That(assoc.AllPossibleNames.CommaText, Is.EqualTo("wy,xy,yw"), "the implicit name of the association from former role names");
    }

    [Test]
    [Covers(typeof(TMoldModel), nameof(TMoldModel.TrimRemoved), "Dispose")]
    [Covers(typeof(TMoldClass), "Dispose", "NameChanged")]
    [Covers(typeof(TMoldElementList), nameof(TMoldElementList.RemoveEntryReference))]
    public void Removed_elements_are_trimmed_with_what_depends_on_them()
    {
        using var s = new Sample();
        s.Company.SetBoldTV(BoldDefaultTaggedValues.TAG_EVOLUTIONSTATE, "Removed");
        s.Person.Attributes.ItemsByName("firstName")!.SetBoldTV(BoldDefaultTaggedValues.TAG_EVOLUTIONSTATE, "Removed");
        s.Model.TrimRemoved();
        Assert.That(s.Model.Classes.ItemsByName("Company"), Is.Null);
        Assert.That(s.Model.Classes.ItemsByName("Job"), Is.Null, "the link class of a removed association goes too");
        Assert.That(s.Model.Associations.Count, Is.EqualTo(0));
        Assert.That(s.Person.Roles.Count, Is.EqualTo(0), "the other end left the class");
        Assert.That(s.Person.Attributes.ItemsByName("firstName"), Is.Null);
        Assert.That(s.Model.Classes.Count, Is.EqualTo(3));

        s.Person.Dispose();
        Assert.That(s.Manager.SuperClass, Is.SameAs(s.Model.RootClass), "subclasses move to the superclass");
        Assert.That(s.Model.Classes.ItemsByName("Person"), Is.Null);

        using var linked = new Sample();
        linked.Model.EnsureLinkRoles();
        Assert.Throws<EBold>(() => linked.Model.TrimRemoved());
    }

    [Test]
    [Covers(typeof(TMoldQualifier), "*")]
    [Covers(typeof(TMoldQualifierList), "*")]
    [Covers(typeof(TMoldRole), nameof(TMoldRole.Qualifiers), nameof(TMoldRole.QualifiedMulti))]
    public void Qualifiers_make_an_end_multi()
    {
        using var s = new Sample();
        var q = new TMoldQualifier(s.Employer, "code") { BoldType = "String" };
        Assert.That(s.Employer.Qualifiers.ItemsByName("code"), Is.SameAs(q));
        Assert.That(q.MoldRole, Is.SameAs(s.Employer));
        Assert.That(q.Model, Is.SameAs(s.Model));
        Assert.That(s.Employer.Multi, Is.True);
        Assert.That(s.Employer.QualifiedMulti, Is.False, "the upper bound is 1");
        q.Name = "key";
        Assert.That(s.Employer.Qualifiers.ItemsByName("key"), Is.SameAs(q));
    }

    [Test]
    [Covers(typeof(TMoldModel), nameof(TMoldModel.Classes), nameof(TMoldModel.Associations))]
    public void UML_model_loads_its_122_classes_with_members()
    {
        using TMoldModel model = TMoldBLDRW.ModelFromFile(FixturePaths.Model("BoldUMLModel.bld"));
        Assert.That(model.Classes.Count, Is.EqualTo(122));
        Assert.That(model.Associations.Count, Is.EqualTo(116));
        TMoldClass element = model.Classes.ItemsByName("ModelElement")!;
        Assert.That(element.Attributes.ItemsByName("name"), Is.Not.Null);
        Assert.That(model.Classes.ItemsByName("Classifier")!.ChildTo(element), Is.True);
        int attributes = Enumerable.Range(0, model.Classes.Count).Sum(i => model.Classes[i].Attributes.Count);
        int roles = Enumerable.Range(0, model.Associations.Count).Sum(i => model.Associations[i].Roles.Count);
        Assert.That(roles, Is.EqualTo(232));
        Assert.That(attributes, Is.EqualTo(83), "the (Attribute blocks in the file");
        model.EnsureLinkRoles();
        Assert.That(element.AllBoldMembers.Count, Is.GreaterThan(5));
        Assert.That(model.CRC(), Is.Not.Empty);
    }
}
