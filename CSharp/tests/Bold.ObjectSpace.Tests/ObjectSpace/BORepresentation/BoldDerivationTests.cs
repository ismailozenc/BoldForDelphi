// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
// The M4 closing tests: derived attributes and links follow their inputs, a member derived in code can be set and
// derived back, and constraints report the rules an object breaks.
namespace Bold.Tests;

public class BoldDerivationTests
{
    private static TBoldMember M(TBoldObject o, string name) => o.BoldMemberByExpressionName(name);

    /// <summary>
    /// The town with three derived members on Person: label (firstName and age), homeAddress (through the home link)
    /// and housemates (a derived link to the other residents of the home); and two constraints.
    /// </summary>
    private static ObjectSpaceSample DerivedTown() => new(m =>
    {
        TMoldClass person = m.Classes.ItemsByName("Person")!, building = m.Classes.ItemsByName("Building")!;
        new TMoldAttribute(person, "label") { Derived = true }.SetBoldTV("DerivationOCL", "firstName.concat(' ').concat(age.asString)");
        new TMoldAttribute(person, "homeAddress") { Derived = true }.SetBoldTV("DerivationOCL", "home.address");
        var housing = new TMoldAssociation(m, "Housing") { Derived = true };
        new TMoldRole(housing, "housemates") { MoldClass = person, Multiplicity = "0..*" }
            .SetBoldTV("DerivationOCL", "home.residents->excluding(self)");
        _ = new TMoldRole(housing, "housemateOf") { MoldClass = person, Multiplicity = "0..*", Navigable = false };
        person.Constraints.Add("adult=age >= 18");
        person.Constraints.Add("named=firstName <> ''");
        building.Constraints.Add("located=address <> ''");
    });

    [Test]
    [Covers(typeof(TBoldMember), "CalculateDerivedMemberWithExpression", "EnsureContentsCurrent", "Invalidate", "Derived")]
    public void Derived_attributes_follow_their_inputs()
    {
        using ObjectSpaceSample s = DerivedTown();
        TBoldObject ann = s.NewPerson(), house = s.NewBuilding(), flat = s.NewBuilding();
        M(ann, "firstName").AsString = "Ann";
        ((TBAInteger)M(ann, "age")).AsInteger = 30;
        M(house, "address").AsString = "House";
        M(flat, "address").AsString = "Flat";

        TBoldMember label = M(ann, "label");
        Assert.That(label.Derived, Is.True);
        Assert.That(label.AsString, Is.EqualTo("Ann 30"));
        M(ann, "firstName").AsString = "Anna";
        Assert.That(label.AsString, Is.EqualTo("Anna 30"), "a changed input derives the value again");
        ((TBAInteger)M(ann, "age")).AsInteger = 31;
        Assert.That(label.AsString, Is.EqualTo("Anna 31"));

        // A subscriber hears the derived value go out of date when an input changes.
        var events = new List<TBoldEvent>();
        using var subscriber = new TBoldPassthroughSubscriber((_, e, _) => events.Add(e));
        label.DefaultSubscribe(subscriber);
        M(ann, "firstName").AsString = "Ann";
        Assert.That(events, Is.Not.Empty, "the subscriber is told");
        Assert.That(label.AsString, Is.EqualTo("Ann 31"));

        TBoldMember homeAddress = M(ann, "homeAddress");
        Assert.That(homeAddress.AsString, Is.EqualTo(""), "no home yet");
        ((TBoldObjectReference)M(ann, "home")).BoldObject = house;
        Assert.That(homeAddress.AsString, Is.EqualTo("House"), "a new link derives the value again");
        M(house, "address").AsString = "Big House";
        Assert.That(homeAddress.AsString, Is.EqualTo("Big House"), "so does a change at the end of the link");
        ((TBoldObjectReference)M(ann, "home")).BoldObject = flat;
        Assert.That(homeAddress.AsString, Is.EqualTo("Flat"));

        Assert.That(label.CanModify(), Is.False, "an OCL derived member cannot be set");
        Assert.Throws<EBoldFailure>(() => label.AsString = "x");
    }

    [Test]
    [Covers(typeof(TBoldMember), "CalculateDerivedMemberWithExpression")]
    public void Derived_links_follow_their_inputs()
    {
        using ObjectSpaceSample s = DerivedTown();
        TBoldObject ann = s.NewPerson(), bob = s.NewPerson(), cy = s.NewPerson(), house = s.NewBuilding();
        var housemates = (TBoldObjectList)M(ann, "housemates");
        Assert.That(housemates.Count, Is.EqualTo(0));
        ((TBoldObjectReference)M(ann, "home")).BoldObject = house;
        ((TBoldObjectReference)M(bob, "home")).BoldObject = house;
        Assert.That(housemates.Count, Is.EqualTo(1));
        Assert.That(housemates.Includes(bob), Is.True);
        ((TBoldObjectReference)M(cy, "home")).BoldObject = house;
        Assert.That(housemates.Count, Is.EqualTo(2), "a new resident joins the derived link");
        bob.Delete();
        Assert.That(housemates.Count, Is.EqualTo(1), "a deleted one leaves it");
        Assert.That(housemates.Includes(cy), Is.True);
        Assert.That(ann.EvaluateExpressionAsString("housemates->size", BoldDefs.brDefault), Is.EqualTo("1"), "OCL reads the derived link");
    }

    [Test]
    [Covers(typeof(TBAConstraint), "*")]
    public void Constraints_report_the_rules_an_object_breaks()
    {
        using ObjectSpaceSample s = DerivedTown();
        TBoldObject ann = s.NewPerson(), house = s.NewBuilding();
        ((TBAInteger)M(ann, "age")).AsInteger = 12;
        Assert.That(ann.EvaluateExpressionAsString("constraints->size", BoldDefs.brDefault), Is.EqualTo("2"));
        Assert.That(ann.EvaluateExpressionAsString("constraints->select(c | not c)->size", BoldDefs.brDefault), Is.EqualTo("2"), "both rules are broken");

        using var broken = new TBoldIndirectElement();
        ann.EvaluateExpression("constraints->select(c | not c)", broken);
        var list = (TBoldMemberList)broken.Value!;
        Assert.That(list[0]!.GetStringRepresentation(10), Is.EqualTo("adult"), "representation 10 is the constraint's name");
        Assert.That(list[0]!.GetStringRepresentation(12), Is.EqualTo("age >= 18"), "12 is its expression");
        Assert.That(list[1]!.GetStringRepresentation(10), Is.EqualTo("named"));

        M(ann, "firstName").AsString = "Ann";
        Assert.That(ann.EvaluateExpressionAsString("constraints->select(c | not c)->size", BoldDefs.brDefault), Is.EqualTo("1"), "naming her mends one");
        ((TBAInteger)M(ann, "age")).AsInteger = 18;
        Assert.That(ann.EvaluateExpressionAsString("constraints->forAll(c | c)", BoldDefs.brDefault), Is.EqualTo("Y"), "growing up mends the other");

        Assert.That(house.EvaluateExpressionAsString("constraints->select(c | not c)->size", BoldDefs.brDefault), Is.EqualTo("1"));
        Assert.That(s.System.EvaluateExpressionAsString("Building.allInstances->select(b | b.constraints->exists(c | not c))->size", BoldDefs.brDefault),
            Is.EqualTo("1"), "a query finds the objects that break a rule");
    }

    // A member derived in code: generated code overrides the object's derive methods. These classes stand for that code.

    private const string ReverseModel = "ReverseTown";

    public class TReverseTownRoot(TBoldDomainElement? AOwningElement, bool Persistent = true) : TBoldObject(AOwningElement, Persistent);

    /// <summary>fullName is derived from firstName and lastName, and setting it splits it back into them.</summary>
    public class TPerson(TBoldDomainElement? AOwningElement, bool Persistent = true) : TReverseTownRoot(AOwningElement, Persistent)
    {
        private TBoldMember Member(string name) => BoldMemberByExpressionName(name);

        private void FullNameDeriveAndSubscribe(object? DerivedObject, TBoldSubscriber? Subscriber)
        {
            Member("fullName").AsString = (Member("firstName").AsString + " " + Member("lastName").AsString).Trim();
            Member("firstName").DefaultSubscribe(Subscriber);
            Member("lastName").DefaultSubscribe(Subscriber);
        }

        private void FullNameReverseDerive(object? DerivedObject)
        {
            string[] parts = Member("fullName").AsString.Split(' ', 2);
            Member("firstName").AsString = parts[0];
            Member("lastName").AsString = parts.Length > 1 ? parts[1] : "";
        }

        protected override TBoldDeriveAndResubscribe? GetDeriveMethodForMember(TBoldMember Member) =>
            Member.BoldMemberRTInfo!.ExpressionName == "fullName" ? FullNameDeriveAndSubscribe : base.GetDeriveMethodForMember(Member);

        protected override TBoldReverseDerive? GetReverseDeriveMethodForMember(TBoldMember Member) =>
            Member.BoldMemberRTInfo!.ExpressionName == "fullName" ? FullNameReverseDerive : base.GetReverseDeriveMethodForMember(Member);
    }

    private static bool sRegistered;

    [Test]
    [Covers(typeof(TBoldMember), "CanModify", "EndModify")]
    [Covers(typeof(TBoldObject), "GetDeriveMethodForMember", "GetReverseDeriveMethodForMember")]
    public void A_member_derived_in_code_is_derived_back_when_set()
    {
        if (!sRegistered)
        {
            BoldGeneratedCodeDictionary.GeneratedCodes().AddGeneratedCodeDescriptorWithFunc(ReverseModel,
                classes =>
                {
                    classes.AddObjectEntry(ReverseModel + "Root", typeof(TReverseTownRoot));
                    classes.AddObjectEntry("Person", typeof(TPerson));
                }, null);
            sRegistered = true;
        }

        using var model = new TMoldModel(null, ReverseModel);
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        model.TypeNameDictionary = types;
        var person = new TMoldClass(model, "Person") { SuperClass = model.RootClass };
        _ = new TMoldAttribute(person, "firstName");
        _ = new TMoldAttribute(person, "lastName");
        var fullName = new TMoldAttribute(person, "fullName") { Derived = true };
        fullName.SetBoldTV(BoldDefaultTaggedValues.TAG_REVERSEDERIVE, "True");
        using var systemTypeInfo = new TBoldSystemTypeInfo(model, true, false, types);
        Assert.That(systemTypeInfo.InitializationLog.Count, Is.EqualTo(0), string.Join("; ", systemTypeInfo.InitializationLog.ToStringArray()));
        Assert.That(systemTypeInfo.ClassTypeInfoByExpressionName("Person")!.AllMembers.ItemsByExpressionName("fullName")!.IsReverseDerived, Is.True);

        using var system = new TBoldSystem(null, systemTypeInfo, null);
        try
        {
            TBoldObject ann = system.CreateNewObjectByExpressionName("Person");
            Assert.That(ann, Is.InstanceOf<TPerson>(), "the system makes objects of the registered class");
            M(ann, "firstName").AsString = "Ann";
            M(ann, "lastName").AsString = "Lee";
            TBoldMember full = M(ann, "fullName");
            Assert.That(full.AsString, Is.EqualTo("Ann Lee"));
            M(ann, "lastName").AsString = "Smith";
            Assert.That(full.AsString, Is.EqualTo("Ann Smith"), "it follows its inputs like an OCL derived member");

            Assert.That(full.CanModify(), Is.True, "a reverse derivable member can be set");
            full.AsString = "Bea Jones";
            Assert.That(M(ann, "firstName").AsString, Is.EqualTo("Bea"), "setting it derives the inputs back");
            Assert.That(M(ann, "lastName").AsString, Is.EqualTo("Jones"));
            Assert.That(full.AsString, Is.EqualTo("Bea Jones"));
            M(ann, "firstName").AsString = "Cy";
            Assert.That(full.AsString, Is.EqualTo("Cy Jones"), "and it still follows its inputs");
        }
        finally
        {
            system.Discard();
        }
    }
}
