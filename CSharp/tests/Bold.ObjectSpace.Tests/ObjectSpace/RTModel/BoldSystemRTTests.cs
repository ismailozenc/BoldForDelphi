// Delphi: Source/ObjectSpace/RTModel/BoldSystemRT.pas
// Port map: CSharp/port/units/BoldSystemRT.md
namespace Bold.Tests;

public class BoldSystemRTTests
{
    /// <summary>
    /// A model with inheritance, a derived attribute overridden in a subclass, a method, a constraint, a tagged value, an
    /// association with a link class and a qualified association.
    /// </summary>
    private sealed class Sample : IDisposable
    {
        public readonly TMoldModel Model = new(null, "Org");
        public readonly TBoldTypeNameDictionary Types = new(null);
        public readonly TMoldClass Person, Manager, Company, Job, Building;

        public Sample()
        {
            Types.AddDefaultMappings();
            Model.SetBoldTV(BoldDefaultTaggedValues.TAG_GENERATEMULTIPLICITYCONSTRAINTS, "True");
            Person = new TMoldClass(Model, "Person") { SuperClass = Model.RootClass };
            Manager = new TMoldClass(Model, "Manager") { SuperClass = Person };
            Company = new TMoldClass(Model, "Company") { SuperClass = Model.RootClass };
            Job = new TMoldClass(Model, "Job") { SuperClass = Model.RootClass };
            Building = new TMoldClass(Model, "Building") { SuperClass = Model.RootClass };

            _ = new TMoldAttribute(Person, "firstName");
            _ = new TMoldAttribute(Person, "age") { BoldType = "Integer", InitialValue = "18" };
            var fullName = new TMoldAttribute(Person, "fullName") { Derived = true };
            fullName.SetBoldTV("DerivationOCL", "firstName");
            Manager.SetBoldTV(BoldDefaultTaggedValues.TAG_DERIVATIONEXPRESSIONS, "  fullName=firstName + ' boss'  ");
            _ = new TMoldAttribute(Company, "name");
            _ = new TMoldAttribute(Building, "address");
            Person.Constraints.Add("adult=age >= 18");
            Person.SetTV("Doc", "people");
            _ = new TMoldMethod(Person, "Greet") { Signature = "who: Person; times, count: Integer", ReturnType = "String" };
            _ = new TMoldMethod(Manager, "Promote");

            var employment = new TMoldAssociation(Model, "Employment") { LinkClass = Job };
            _ = new TMoldRole(employment, "employer") { MoldClass = Person, Multiplicity = "0..1" };
            _ = new TMoldRole(employment, "employees") { MoldClass = Company, Multiplicity = "1..3" };

            var residence = new TMoldAssociation(Model, "Residence");
            _ = new TMoldRole(residence, "home") { MoldClass = Person, Multiplicity = "0..1" };
            var residents = new TMoldRole(residence, "residents") { MoldClass = Building, Multiplicity = "0..*" };
            _ = new TMoldQualifier(residents, "firstName");
        }

        public TBoldSystemTypeInfo Build() => new(Model, false, false, Types);

        public void Dispose() => Model.Dispose();
    }

    [Test]
    [Covers(typeof(BoldSystemRT), nameof(BoldSystemRT.LITE_VERSION_CLASS_LIMIT), nameof(BoldSystemRT.CLASS_TYPE_INFO_MEM_SIZE))]
    public void Constants_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldSystemRT), "Source/ObjectSpace/RTModel/BoldSystemRT.pas");
    }

    [Test]
    [Covers(typeof(TBoldSystemTypeInfo), "*")]
    [Covers(typeof(TBoldClassTypeInfoList), "*")]
    [Covers(typeof(TBoldTypeTypeInfo), "*")]
    [Covers("BoldSystemRT", "TObjectClassIndex", "TObjectClassIndex.ItemAsKeyClass")]
    public void System_type_holds_classes_types_and_lists()
    {
        using var s = new Sample();
        using TBoldSystemTypeInfo system = s.Build();
        Assert.That(system.SystemTypeInfo, Is.SameAs(system), "the system is its own system type");
        Assert.That(system.BoldValueType, Is.EqualTo(TBoldValueTypeSet.bvtSystem));
        Assert.That(system.ModelName, Is.EqualTo("Org"));
        Assert.That(system.Persistent, Is.True);
        Assert.That(system.GenerateMultiplicityConstraints, Is.True);
        Assert.That(system.UseGeneratedCode, Is.False);
        Assert.That(system.UseClockLog, Is.True, "the UseClockLog tagged value defaults to True");
        Assert.That(system.MethodsInstalled, Is.False);
        Assert.That(system.UpdateWholeObjects, Is.False);
        Assert.That(system.GenerateDefaultRegions, Is.False);
        Assert.That(system.OptimisticLocking, Is.EqualTo(s.Model.OptimisticLocking));
        Assert.That(system.Stereotype, Is.EqualTo(""));
        Assert.That(system.ConformsTo(system), Is.True);
        Assert.That(system.ConformsTo(system.TypeTypeInfo), Is.False);

        // BoldAttributes registers the attribute classes, so every attribute finds its class.
        Assert.That(system.SystemIsRunnable, Is.True, string.Join("\n", system.InitializationLog.ToStringArray()));
        Assert.That(system.InitializationLog.ToStringArray(), Has.None.Contains("has no registered DelphiType"));

        string[] classNames = Enumerable.Range(0, system.TopSortedClasses.Count).Select(i => system.TopSortedClasses[i].ExpressionName).ToArray();
        Assert.That(classNames[0], Is.EqualTo(system.RootClassTypeInfo.ExpressionName));
        Assert.That(classNames.Length, Is.EqualTo(6));
        for (int i = 0; i < system.TopSortedClasses.Count; i++)
        {
            Assert.That(system.TopSortedClasses[i].TopSortedIndex, Is.EqualTo(i));
            Assert.That(s.Model.Classes[i].ExpandedExpressionName, Is.EqualTo(classNames[i]), "same order as the Mold model");
        }

        TBoldClassTypeInfo person = system.ClassTypeInfoByExpressionName("person")!;
        Assert.That(system.ClassTypeInfoByModelName("Person"), Is.SameAs(person));
        Assert.That(system.ClassTypeInfoByModelName("person"), Is.Null, "model names are case-sensitive");
        Assert.That(system.TopSortedClasses.ItemsByModelName("Person"), Is.SameAs(person));
        Assert.That(system.TopSortedClasses.ItemsByObjectClass(typeof(object)), Is.Null);

        TBoldTypeTypeInfo meta = system.TypeTypeInfo!;
        Assert.That(meta.ExpressionName, Is.EqualTo("MetaType"));
        Assert.That(meta.BoldType, Is.SameAs(meta));
        Assert.That(meta.ConformsTo(meta), Is.True);
        Assert.That(meta.BoldValueType, Is.EqualTo(TBoldValueTypeSet.bvtType));
        Assert.That(system.BoldType, Is.SameAs(meta));
        Assert.That(person.BoldType, Is.SameAs(meta));

        Assert.That(system.AttributeTypeInfoByExpressionName("Integer"), Is.Not.Null);
        Assert.That(system.AttributeTypes.Count, Is.LessThanOrEqualTo(s.Types.Count), "mappings with the same expression name share a type");
        TBoldAttributeTypeInfo integer = system.AttributeTypeInfoByExpressionName("Integer")!;
        Assert.That(system.AttributeTypeInfoByDelphiName(integer.DelphiName), Is.SameAs(integer));

        // MetaType list, one per attribute type, the untyped list, one per class.
        Assert.That(system.ListTypes.Count, Is.EqualTo(1 + system.AttributeTypes.Count + 1 + 6));
        Assert.That(system.ListTypeInfoByElement(person)!.ExpressionName, Is.EqualTo("Collection(Person)"));
        Assert.That(system.ListTypeInfoByElement(null), Is.Null);

        Assert.That(system.ElementTypeInfoByExpressionName("Collection(Person)"), Is.SameAs(person.ListTypeInfo));
        Assert.That(system.ElementTypeInfoByExpressionName("MetaType"), Is.SameAs(meta));
        Assert.That(system.ElementTypeInfoByDelphiName("TPerson"), Is.SameAs(person));
        Assert.That(system.ValueTypeNameList.Count, Is.EqualTo(1 + 6 + system.AttributeTypes.Count + 1 + system.ListTypes.Count));

        var names = new TStringList();
        system.GetValueTypeNames(names, true, false, false, false, false);
        Assert.That(names.Count, Is.EqualTo(6));
        system.GetValueTypeNames(names, false, false, true, true, false);
        Assert.That(names.ToStringArray(), Is.EqualTo(new[] { "Org", "MetaType" }));
        system.GetValueTypeNames(names, false, true, false, false, true);
        Assert.That(names.Count, Is.EqualTo(system.AttributeTypes.Count + system.ListTypes.Count));

        TBoldNilTypeInfo nil = system.NilTypeInfo!;
        Assert.That(nil.TopSortedIndex, Is.EqualTo(-1));
        Assert.That(nil.AsString, Is.EqualTo("nil"));
        Assert.That(nil.ConformsTo(person) && nil.BoldIsA(person), Is.True, "nil conforms to every class");
        Assert.That(nil.ConformsTo(integer), Is.False);

        var e = Assert.Throws<EBoldFeatureNotImplementedYet>(() => _ = system.Evaluator)!;
        Assert.That(e.Message, Does.Contain("TBoldOCL"), "the OCL evaluator is ported in M4");
        system.ReleaseEvaluator();
    }

    [Test]
    [Covers(typeof(TBoldClassTypeInfo), "*")]
    [Covers(typeof(TBoldNilTypeInfo), "*")]
    [Covers(typeof(TBoldElementTypeInfoWithConstraint), "*")]
    [Covers(typeof(TBoldConstraintRTInfo), "*")]
    [Covers(typeof(TBoldConstraintRTInfoList), "*")]
    public void Classes_know_their_hierarchy_members_constraints_and_tagged_values()
    {
        using var s = new Sample();
        using TBoldSystemTypeInfo system = s.Build();
        TBoldClassTypeInfo root = system.RootClassTypeInfo;
        TBoldClassTypeInfo person = system.ClassTypeInfoByExpressionName("Person")!;
        TBoldClassTypeInfo manager = system.ClassTypeInfoByExpressionName("Manager")!;
        TBoldClassTypeInfo company = system.ClassTypeInfoByExpressionName("Company")!;
        TBoldClassTypeInfo job = system.ClassTypeInfoByExpressionName("Job")!;

        Assert.That(manager.SuperClassTypeInfo, Is.SameAs(person));
        Assert.That(person.HasSubclasses && root.HasSubclasses, Is.True);
        Assert.That(manager.HasSubclasses, Is.False);
        Assert.That(manager.BoldIsA(person) && manager.BoldIsA(root), Is.True);
        Assert.That(person.BoldIsA(manager), Is.False);
        Assert.That(person.BoldIsA(system.AttributeTypeInfoByExpressionName("String")), Is.False);
        Assert.That(manager.ConformsTo(person.ListTypeInfo), Is.True, "a class conforms to lists of its superclasses");
        Assert.That(manager.ConformsTo(system.ListTypeInfoByElement(company)), Is.False);
        Assert.That(manager.LeastCommonSuperClass(person), Is.SameAs(person));
        Assert.That(manager.LeastCommonSuperClass(company), Is.SameAs(root));
        Assert.That(job.IsLinkClass, Is.True);
        Assert.That(person.IsLinkClass || person.IsAbstract || person.IsImported || person.ToBeRemoved || person.Versioned, Is.False);
        Assert.That(person.Persistent, Is.True);
        Assert.That(person.GenerateDefaultRegion, Is.EqualTo(s.Person.GenerateDefaultRegion));
        Assert.That(person.OptimisticLocking, Is.EqualTo(s.Person.EffectiveOptimisticLocking));
        Assert.That(person.DefaultStringRepresentation, Is.EqualTo(s.Person.EffectiveDefaultStringRepresentation));
        Assert.That(person.Stereotype, Is.EqualTo(""));
        Assert.That(person.QualifiedName, Is.EqualTo("Person"));
        Assert.That(person.ObjectClass, Is.EqualTo(typeof(TBoldObject)), "no generated code: plain objects");
        Assert.That(person.SystemTypeInfo, Is.SameAs(system));
        Assert.That(person.BoldValueType, Is.EqualTo(TBoldValueTypeSet.bvtClass));

        Assert.That(person.AllMembersCount, Is.EqualTo(s.Person.AllBoldMembers.Count));
        Assert.That(manager.AllMembers.Count, Is.EqualTo(s.Manager.AllBoldMembers.Count));
        Assert.That(manager.FirstOwnMemberIndex, Is.EqualTo(s.Manager.FirstOwnBoldMemberIndex));
        Assert.That(person.MemberIndexByExpressionName("age"), Is.EqualTo(person.MemberRTInfoByExpressionName("AGE")!.index));
        Assert.That(person.MemberIndexByExpressionName("nothing"), Is.EqualTo(-1));
        Assert.That(person.MemberRTInfoByModelName("firstName")!.ExpressionName, Is.EqualTo("firstName"));
        for (int i = 0; i < manager.AllMembers.Count; i++)
        {
            Assert.That(manager.AllMembers[i].index, Is.EqualTo(i));
            Assert.That(manager.AllMembers[i].ClassTypeInfo, Is.SameAs(manager));
        }

        Assert.That(person.ConstraintCount, Is.EqualTo(1));
        TBoldConstraintRTInfo adult = person.Constraint("adult")!;
        Assert.That(person.ConstraintByIndex(0), Is.SameAs(adult));
        Assert.That(adult.ConstraintExpression, Is.EqualTo("age >= 18"));
        Assert.That(adult.ConstraintMessage, Is.EqualTo(""));
        Assert.That(adult.BoldType, Is.SameAs(system.TypeTypeInfo));
        Assert.That(adult.ConstraintCount, Is.EqualTo(1), "Delphi gives a class constraint the class's constraints too");
        Assert.That(person.TaggedValues("Doc"), Is.EqualTo("people"));
        Assert.That(person.TaggedValueCount, Is.EqualTo(1));
        Assert.That(person.TaggedValueByIndex(0), Is.EqualTo("Doc=people"));
        Assert.That(manager.TaggedValues("Doc"), Is.EqualTo(""));
        Assert.That(manager.ConstraintCount, Is.EqualTo(0));
        Assert.That(manager.Constraint("adult"), Is.Null);
        Assert.That(manager.ConstraintByIndex(0), Is.Null);
        Assert.That(manager.TaggedValueByIndex(0), Is.EqualTo(""));

        // Multiplicity constraints for the navigable role 1..3: at most 3 and at least 1.
        Assert.That(company.ConstraintCount, Is.EqualTo(2));
        TBoldConstraintRTInfo atMost = company.ConstraintByIndex(0)!;
        Assert.That(atMost.ModelName, Is.EqualTo("employees multiplicity constraint"));
        Assert.That(atMost.ConstraintExpression, Is.EqualTo("employees->size <= 3"));
        Assert.That(atMost.ConstraintMessage, Is.EqualTo("Role employees must have at most 3 Person"));
        Assert.That(company.ConstraintByIndex(1)!.ConstraintExpression, Is.EqualTo("employees->size >= 1"));
        var list = new TBoldConstraintRTInfoList();
        list.Add(atMost);
        Assert.That(list[0], Is.SameAs(atMost));
        Assert.That(list.ItemsByModelName("employees multiplicity constraint"), Is.SameAs(atMost));
    }

    [Test]
    [Covers(typeof(TBoldMemberRTInfo), "*")]
    [Covers(typeof(TBoldAttributeRTInfo), "*")]
    [Covers(typeof(TBoldMemberRTInfoList), "*")]
    [Covers(typeof(TBoldMetaElementWithConstraint), "*")]
    public void Attributes_carry_type_stream_name_and_derivation()
    {
        using var s = new Sample();
        using TBoldSystemTypeInfo system = s.Build();
        TBoldClassTypeInfo person = system.ClassTypeInfoByExpressionName("Person")!;
        TBoldClassTypeInfo manager = system.ClassTypeInfoByExpressionName("Manager")!;

        var age = (TBoldAttributeRTInfo)person.MemberRTInfoByExpressionName("age")!;
        Assert.That(age.IsAttribute && !age.IsRole && !age.IsSingleRole && !age.IsMultiRole, Is.True);
        Assert.That(age.BoldType, Is.SameAs(system.AttributeTypeInfoByExpressionName("Integer")));
        Assert.That(age.StreamName, Is.EqualTo("Integer"));
        Assert.That(age.MemberClass, Is.EqualTo(typeof(TBAInteger)));
        Assert.That(age.HasInitialValue, Is.True);
        Assert.That(age.InitialValue, Is.EqualTo("18"));
        Assert.That(age.Length, Is.EqualTo(s.Person.Attributes.ItemsByName("age")!.Length));
        Assert.That(age.AllowNull, Is.False);
        Assert.That(age.AsString, Is.EqualTo("Person.age"));
        Assert.That(age.Persistent, Is.False, "attributes are transient unless the model says otherwise");
        Assert.That(age.IsStoredInObject, Is.False);
        Assert.That(age.CanHaveOldValue, Is.False);
        Assert.That(age.EncouragesOptimisticLockingOnDeletedOnly, Is.False);
        Assert.That(age.Visibility, Is.EqualTo(s.Person.Attributes.ItemsByName("age")!.Visibility));
        Assert.That(age.EmbeddedLinkIndex, Is.EqualTo(-1));
        Assert.That(age.DelayedFetch || age.IsDerived || age.IsReverseDerived || age.ToBeRemoved || age.IsNonVersionedInVersionedClass, Is.False);
        Assert.That(age.Stereotype, Is.EqualTo(""));
        Assert.That(age.DispId, Is.EqualTo(s.Person.Attributes.ItemsByName("age")!.DispId));
        Assert.That(age.DeriveExpression, Is.EqualTo(""));
        Assert.That(age.TaggedValueCount, Is.EqualTo(0));
        Assert.That(age.TaggedValues("x"), Is.EqualTo(""));
        Assert.That(age.TaggedValueByIndex(0), Is.EqualTo(""));
        Assert.That(age.ConstraintCount, Is.EqualTo(0));
        Assert.That(age.Constraint("x"), Is.Null);
        Assert.That(age.ConstraintByIndex(0), Is.Null);
        var extra = new TBoldConstraintRTInfo(null, "c", "", "", system, "true", "msg");
        age.AddConstraint(extra);
        Assert.That(age.Constraint("c"), Is.SameAs(extra));
        Assert.That(age.ConstraintByIndex(0), Is.SameAs(extra));

        Assert.That(person.MemberRTInfoByExpressionName("fullName")!.DeriveExpression, Is.EqualTo("firstName"));
        Assert.That(manager.MemberRTInfoByExpressionName("fullName")!.DeriveExpression, Is.EqualTo("firstName + ' boss'"),
            "a subclass overrides the derivation in its DerivationExpressions tagged value");
        Assert.That(manager.MemberRTInfoByExpressionName("fullName")!.IsDerived, Is.True);

        TBoldMemberRTInfoList members = person.AllMembers;
        Assert.That(members.ItemsByModelName("age"), Is.SameAs(age));
        Assert.That(members.ItemsByExpressionName("AGE"), Is.SameAs(age));
        Assert.That(members[age.index], Is.SameAs(age));
    }

    [Test]
    [Covers(typeof(TBoldRoleRTInfo), "*")]
    public void Roles_link_to_their_other_end_and_link_class()
    {
        using var s = new Sample();
        using TBoldSystemTypeInfo system = s.Build();
        TBoldClassTypeInfo person = system.ClassTypeInfoByExpressionName("Person")!;
        TBoldClassTypeInfo manager = system.ClassTypeInfoByExpressionName("Manager")!;
        TBoldClassTypeInfo company = system.ClassTypeInfoByExpressionName("Company")!;
        TBoldClassTypeInfo job = system.ClassTypeInfoByExpressionName("Job")!;
        TBoldClassTypeInfo building = system.ClassTypeInfoByExpressionName("Building")!;

        var employer = (TBoldRoleRTInfo)person.MemberRTInfoByExpressionName("employer")!;
        var employees = (TBoldRoleRTInfo)company.MemberRTInfoByExpressionName("employees")!;
        Assert.That(employer.IsSingleRole && employer.IsRole && !employer.IsAttribute, Is.True);
        Assert.That(employer.RoleType, Is.EqualTo(TBoldRoleType.rtRole));
        Assert.That(employer.IsIndirect, Is.True, "an end with a link class");
        Assert.That(employer.LinkClassTypeInfo, Is.SameAs(job));
        Assert.That(employer.ClassTypeInfoOfOtherEnd, Is.SameAs(company));
        Assert.That(employer.BoldType, Is.SameAs(company));
        Assert.That(employees.IsMultiRole && employees.DelayedFetch, Is.True);
        Assert.That(employees.BoldType, Is.SameAs(person.ListTypeInfo));
        Assert.That(employees.MemberClass, Is.EqualTo(typeof(TBoldObjectList)));
        Assert.That(employer.MemberClass, Is.EqualTo(typeof(TBoldObjectReference)));
        Assert.That(employees.Mandatory, Is.True);
        Assert.That(employees.Multiplicity, Is.EqualTo("1..3"));
        Assert.That(employer.AsString, Is.EqualTo("Person.employer"));

        // An indirect end refers to the link class's role for the other end.
        TBoldRoleRTInfo jobEmployees = employer.RoleRTInfoOfOtherEnd!;
        Assert.That(jobEmployees.ClassTypeInfo, Is.SameAs(job));
        Assert.That(jobEmployees.RoleType, Is.EqualTo(TBoldRoleType.rtInnerLinkRole));
        Assert.That(jobEmployees.ModelName, Is.EqualTo("employees"));
        Assert.That(jobEmployees.Changeability, Is.EqualTo(TChangeableKind.ckFrozen));
        Assert.That(jobEmployees.IsSingleRole, Is.True);
        Assert.That(employer.OwnIndexInLinkClass, Is.EqualTo(jobEmployees.index));
        Assert.That(employer.OtherIndexInLinkClass, Is.EqualTo(job.MemberRTInfoByModelName("employer")!.index));
        Assert.That(jobEmployees.OwnIndexInLinkClass, Is.EqualTo(jobEmployees.index));
        Assert.That(jobEmployees.ClassTypeInfoOfOtherEnd, Is.SameAs(person));

        // The link object role follows the main role.
        Assert.That(employer.IndexOfLinkObjectRole, Is.EqualTo(employer.index + 1));
        var linkObjects = (TBoldRoleRTInfo)person.AllMembers[employer.IndexOfLinkObjectRole];
        Assert.That(linkObjects.RoleType, Is.EqualTo(TBoldRoleType.rtLinkRole));
        Assert.That(linkObjects.ExpressionName, Is.EqualTo("job"), "named after the link class, lower case first");
        Assert.That(linkObjects.ModelName, Is.EqualTo("Job"));
        Assert.That(linkObjects.Persistent, Is.False);
        Assert.That(linkObjects.IndexOfMainRole, Is.EqualTo(employer.index));
        Assert.That(linkObjects.BoldType, Is.SameAs(job));
        Assert.That(linkObjects.ClassTypeInfoOfOtherEnd, Is.SameAs(job));
        Assert.That(linkObjects.DefaultRegionMode, Is.EqualTo(TBoldAssociationEndDefaultRegionMode.aedrmNone));
        Assert.That(linkObjects.CanHaveOldValue, Is.False, "link object roles have no old values");
        Assert.That(employer.IndexOfMainRole, Is.EqualTo(-1));
        Assert.That(jobEmployees.IndexOfLinkObjectRole, Is.EqualTo(-1));

        // The qualified, non-indirect association.
        var home = (TBoldRoleRTInfo)person.MemberRTInfoByExpressionName("home")!;
        var residents = (TBoldRoleRTInfo)building.MemberRTInfoByExpressionName("residents")!;
        Assert.That(home.RoleRTInfoOfOtherEnd, Is.SameAs(residents));
        Assert.That(home.IndexOfOtherEnd, Is.EqualTo(residents.index));
        Assert.That(home.ForceOtherEnd, Is.False, "the other end is a multi role");
        Assert.That(residents.ForceOtherEnd, Is.True, "the other end is single");
        Assert.That(residents.IsQualified && residents.IsQualifiedMulti, Is.True);
        Assert.That(residents.Qualifiers![0], Is.SameAs(person.MemberRTInfoByModelName("firstName")));
        Assert.That(home.IsQualified, Is.False);
        Assert.That(home.Qualifiers, Is.Null);
        Assert.That(home.IsIndirect || home.IsOrdered || home.OtherEndOrdered, Is.False);
        Assert.That(home.IsNavigable, Is.True);
        Assert.That(home.Aggregation, Is.EqualTo(TAggregationKind.akNone));
        Assert.That(home.DeleteAction, Is.EqualTo(s.Person.AllBoldMembers.ItemsByName("home") is TMoldRole r ? r.EffectiveDeleteAction : TDeleteAction.daDefault));
        Assert.That(home.Changeability, Is.EqualTo(TChangeableKind.ckChangeable));
        Assert.That(home.AssociationStereotype, Is.EqualTo(""));
        Assert.That(home.EncouragesOptimisticLockingOnDeletedOnly, Is.False, "not persistent");
        Assert.That(home.IsStoredInObject, Is.False);
        Assert.That(home.EmbeddedLinkIndex, Is.EqualTo(-1));
        Assert.That(person.EmbeddedSingleLinkCount, Is.EqualTo(0));

        // Inherited ends in subclasses get the same other-end information.
        var managerHome = (TBoldRoleRTInfo)manager.MemberRTInfoByModelName("home")!;
        Assert.That(managerHome, Is.Not.SameAs(home));
        Assert.That(managerHome.RoleRTInfoOfOtherEnd, Is.SameAs(residents));
        Assert.That(managerHome.BoldType, Is.SameAs(building));
        var managerEmployer = (TBoldRoleRTInfo)manager.MemberRTInfoByModelName("employer")!;
        Assert.That(managerEmployer.LinkClassTypeInfo, Is.SameAs(job));
        Assert.That(((TBoldRoleRTInfo)manager.AllMembers[managerEmployer.index + 1]).BoldType, Is.SameAs(job));

        Assert.That(employer.ForceOtherEnd, Is.False,
            "as in Delphi, set from the first other end (the multi end) and not recomputed when the link class role replaces it");
        home.SetForceOtherEnd();
        Assert.That(home.ForceOtherEnd, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldListTypeInfo), "*")]
    [Covers(typeof(TBoldListTypeInfoList), "*")]
    [Covers(typeof(TBoldAttributeTypeInfo), "*")]
    [Covers("BoldSystemRT", "TElementIndex", "TElementIndex.ItemAsKeyObject")]
    public void List_and_attribute_types_conform_as_in_OCL()
    {
        using var s = new Sample();
        using TBoldSystemTypeInfo system = s.Build();
        TBoldClassTypeInfo person = system.ClassTypeInfoByExpressionName("Person")!;
        TBoldClassTypeInfo manager = system.ClassTypeInfoByExpressionName("Manager")!;
        TBoldListTypeInfo persons = person.ListTypeInfo!;
        TBoldListTypeInfo managers = manager.ListTypeInfo!;
        var untyped = (TBoldListTypeInfo)system.ListTypes[1 + system.AttributeTypes.Count];
        Assert.That(untyped.ListElementTypeInfo, Is.Null);
        Assert.That(untyped.ExpressionName, Is.EqualTo("Collection()"));
        Assert.That(untyped.AsString, Is.EqualTo("Collection()"));
        Assert.That(persons.ModelName, Is.EqualTo("PersonList"));
        Assert.That(persons.DelphiName, Is.EqualTo("TPersonList"));
        Assert.That(persons.AsString, Is.EqualTo("Collection(Person)"));
        Assert.That(persons.ListElementTypeInfo, Is.SameAs(person));
        Assert.That(persons.ListClass, Is.EqualTo(typeof(TBoldObjectList)));
        Assert.That(persons.BoldType, Is.SameAs(system.TypeTypeInfo));
        Assert.That(persons.BoldValueType, Is.EqualTo(TBoldValueTypeSet.bvtList));
        Assert.That(managers.ConformsTo(persons) && managers.ConformsTo(untyped), Is.True);
        Assert.That(persons.ConformsTo(managers), Is.False);
        Assert.That(untyped.ConformsTo(persons), Is.False);
        Assert.That(persons.ConformsTo(person), Is.False, "a list is not its element");
        Assert.That(system.ListTypes.ItemByElement(person), Is.SameAs(persons));

        TBoldAttributeTypeInfo integer = system.AttributeTypeInfoByExpressionName("Integer")!;
        TBoldAttributeTypeInfo floatType = system.AttributeTypeInfoByExpressionName("Float")!;
        TBoldAttributeTypeInfo str = system.AttributeTypeInfoByExpressionName("String")!;
        Assert.That(integer.ConformsTo(floatType), Is.True, "integer conforms to float and currency");
        Assert.That(integer.ConformsTo(system.AttributeTypeInfoByExpressionName("Currency")), Is.True);
        Assert.That(floatType.ConformsTo(integer), Is.False);
        Assert.That(str.ConformsTo(str), Is.True, "the same attribute class conforms");
        Assert.That(str.ConformsTo(integer), Is.False);
        Assert.That(str.ConformsTo(untyped), Is.True);
        Assert.That(str.ConformsTo(system.ListTypeInfoByElement(str)), Is.True, "an attribute conforms to a list of its type");
        Assert.That(str.AttributeClass, Is.EqualTo(typeof(TBAString)));
        Assert.That(str.IsAbstract, Is.False, "TBAString is registered as concrete");
        Assert.That(system.AttributeTypeInfoByExpressionName("Numeric")!.IsAbstract, Is.True, "TBANumeric is registered as abstract");
        Assert.That(integer.SuperAttributeTypeInfo, Is.SameAs(system.AttributeTypeInfoByExpressionName("Numeric")));
        Assert.That(str.BoldIsA(str), Is.True);
        Assert.That(str.BoldIsA(integer) || str.BoldIsA(person), Is.False);
        Assert.That(str.BoldType, Is.SameAs(system.TypeTypeInfo));
        Assert.That(str.BoldValueType, Is.EqualTo(TBoldValueTypeSet.bvtAttr));
        Assert.That(str.DelphiName, Is.EqualTo(s.Types.MappingForModelName("String")!.ExpandedDelphiName), "falls back to the mapping's Delphi name");
        Assert.That(system.ListTypeInfoByElement(str)!.ExpressionName, Is.EqualTo("Collection(String)"));
    }

    [Test]
    [Covers(typeof(TBoldMethodRTInfo), "*")]
    [Covers(typeof(TBoldMethodRTInfoList), "*")]
    public void Methods_parse_their_signature_and_are_inherited()
    {
        using var s = new Sample();
        using TBoldSystemTypeInfo system = s.Build();
        TBoldClassTypeInfo person = system.ClassTypeInfoByExpressionName("Person")!;
        TBoldClassTypeInfo manager = system.ClassTypeInfoByExpressionName("Manager")!;
        TBoldMethodRTInfo greet = person.Methods.ItemsByExpressionName("greet")!;
        Assert.That(person.Methods.ItemsByModelName("Greet"), Is.SameAs(greet));
        Assert.That(person.Methods[0], Is.SameAs(greet));
        Assert.That(greet.NumberOfParameters, Is.EqualTo(3));
        Assert.That(greet.Parameters(0), Is.EqualTo("who"));
        Assert.That(greet.ParameterTypes(0), Is.EqualTo("TPerson"), "model class names become Delphi names");
        Assert.That(greet.Parameters(1) + greet.Parameters(2), Is.EqualTo("timescount"));
        Assert.That(greet.ParameterTypes(2), Is.EqualTo("Integer"));
        Assert.That(greet.Signature, Is.EqualTo("who: TPerson; times: Integer; count: Integer"));
        Assert.That(greet.AsString, Is.EqualTo("Person.Greet(who: TPerson; times: Integer; count: Integer)"));
        Assert.Throws<EBoldInternal>(() => greet.Parameters(3));
        Assert.Throws<EBoldInternal>(() => greet.ParameterTypes(3));
        Assert.That(greet.ReturnType, Is.EqualTo("String"));
        Assert.That(greet.BoldType, Is.SameAs(system.AttributeTypeInfoByExpressionName("String")));
        Assert.That(greet.IsInherited, Is.False);
        Assert.That(greet.Inheritedfrom, Is.EqualTo(-1));
        Assert.That(greet.IsClassmethod, Is.False);
        Assert.That(greet.FuncType, Is.EqualTo(s.Person.Methods[0].FuncType));
        Assert.That(greet.ClassTypeInfo, Is.SameAs(person));
        Assert.That(greet.DispId, Is.EqualTo(s.Person.Methods[0].HasDispId ? s.Person.Methods[0].DispId : -1));

        Assert.That(manager.Methods.Count, Is.EqualTo(2), "own methods first, then inherited ones");
        Assert.That(manager.Methods[0].ExpressionName, Is.EqualTo("Promote"));
        TBoldMethodRTInfo inherited = manager.Methods.ItemsByExpressionName("greet")!;
        Assert.That(inherited.IsInherited, Is.True);
        Assert.That(inherited.Inheritedfrom, Is.EqualTo(person.TopSortedIndex));
        TBoldMethodRTInfo promote = manager.Methods[0];
        Assert.That(promote.NumberOfParameters, Is.EqualTo(0));
        Assert.That(promote.Signature, Is.EqualTo(""));
        Assert.That(promote.BoldType, Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldRTEvaluator), "*")]
    public void RT_evaluator_is_an_evaluator()
    {
        Assert.That(typeof(TBoldRTEvaluator).IsSubclassOf(typeof(TBoldEvaluator)), Is.True);
        Assert.That(typeof(TBoldRTEvaluator).GetMethod(nameof(TBoldRTEvaluator.RTInfo))!.IsAbstract, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldSystemTypeInfo), ".ctor")]
    public void Model_errors_install_only_the_root_class()
    {
        using var s = new Sample();
        s.Model.SetTV(BoldDefaultTaggedValues.BOLDINTERALTVPREFIX + BoldDefaultTaggedValues.TV_MODELERRORS, "\"first error\",second");
        using TBoldSystemTypeInfo system = s.Build();
        Assert.That(system.SystemIsRunnable, Is.False);
        Assert.That(system.InitializationLog.ToStringArray(), Is.EqualTo(new[] { "first error", "second" }));
        Assert.That(system.TopSortedClasses.Count, Is.EqualTo(1));
        Assert.That(system.RootClassTypeInfo.AllMembers.Count, Is.EqualTo(0));
        Assert.That(system.NilTypeInfo, Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldSystemTypeInfo), ".ctor")]
    public void Generated_code_with_another_CRC_is_reported()
    {
        using var s = new Sample();
        BoldGeneratedCodeDictionary.GeneratedCodes().AddGeneratedCodeDescriptorWithFunc(s.Model.ExpandedExpressionName, null, null, "not-the-crc");
        using TBoldSystemTypeInfo system = new(s.Model, true, true, s.Types);
        string[] log = system.InitializationLog.ToStringArray();
        Assert.That(log, Has.Some.StartsWith("Generated CRC differs from Model CRC"));
        Assert.That(log, Has.Some.EqualTo("Generated code for Person not registered with framework, ensure that it is included in project"),
            "without generated classes every class is reported");
    }

    public static IEnumerable<FixtureModelInfo> Fixtures => FixturePaths.Manifest;

    /// <summary>Every fixture model builds a runtime model with all classes and members; the only errors are unregistered attribute classes.</summary>
    [TestCaseSource(nameof(Fixtures))]
    [Covers(typeof(TBoldSystemTypeInfo), ".ctor")]
    public void Fixture_models_build_a_runtime_model(FixtureModelInfo info)
    {
        using TMoldModel model = TMoldBLDRW.ModelFromFile(FixturePaths.Model(info.File));
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        using var system = new TBoldSystemTypeInfo(model, false, false, types);
        var modelErrors = new TStringList { CommaText = model.TVByName(BoldDefaultTaggedValues.BOLDINTERALTVPREFIX + BoldDefaultTaggedValues.TV_MODELERRORS) };
        if (modelErrors.Count > 0)
        {
            Assert.That(system.InitializationLog.ToStringArray(), Is.EqualTo(modelErrors.ToStringArray()), "a model with errors logs them");
            Assert.That(system.TopSortedClasses.Count, Is.EqualTo(1), "and installs only the root class");
            return;
        }

        string[] unexpected = system.InitializationLog.ToStringArray()
            .Where(l => !l.Contains(" has no registered DelphiType") && !l.Contains("Unable to find Mapping") && !l.Contains("Unable to find BoldType")).ToArray();
        Assert.That(unexpected, Is.Empty);
        Assert.That(system.TopSortedClasses.Count, Is.EqualTo(model.Classes.Count));
        for (int i = 0; i < model.Classes.Count; i++)
        {
            TBoldClassTypeInfo c = system.TopSortedClasses[i];
            Assert.That(c.ModelName, Is.EqualTo(model.Classes[i].Name));
            Assert.That(c.AllMembers.Count, Is.EqualTo(model.Classes[i].AllBoldMembers.Count), c.ModelName);
            for (int m = 0; m < c.AllMembers.Count; m++)
            {
                if (c.AllMembers[m] is TBoldRoleRTInfo role && role.RoleType != TBoldRoleType.rtInnerLinkRole && role.IsNavigable)
                {
                    Assert.That(role.ClassTypeInfoOfOtherEnd, Is.Not.Null, c.ModelName + "." + role.ModelName);
                    Assert.That(role.BoldType, Is.Not.Null, c.ModelName + "." + role.ModelName);
                }
            }
        }
    }
}
