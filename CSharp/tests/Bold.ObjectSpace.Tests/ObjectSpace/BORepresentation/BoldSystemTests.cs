// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
namespace Bold.Tests;

public class BoldSystemTests
{
    private static T Member<T>(TBoldObject o, string name) where T : TBoldMember => (T)o.BoldMemberByExpressionName(name);
    private static TBoldObjectReference Home(TBoldObject person) => Member<TBoldObjectReference>(person, "home");
    private static TBoldObjectList Residents(TBoldObject building) => Member<TBoldObjectList>(building, "residents");
    private static TBoldObjectList Friends(TBoldObject person) => Member<TBoldObjectList>(person, "friends");
    private static TBoldObjectList FriendOf(TBoldObject person) => Member<TBoldObjectList>(person, "friendOf");

    [Test]
    [Covers(typeof(TBoldSystem), "CreateNewObjectByExpressionName", "Classes", "Discard", "InTransaction", "MakeDefault", "DefaultSystem")]
    [Covers(typeof(TBoldObject), "BoldMemberByExpressionName", "BoldClassTypeInfo", "BoldObjectIsNew", "Delete", "BoldExistenceState", "BoldSystem")]
    public void Objects_are_created_in_class_lists_and_deleted()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject p = s.NewPerson();
        TBoldObject b = s.NewBuilding();
        Assert.That(p.BoldClassTypeInfo.ExpressionName, Is.EqualTo("Person"));
        Assert.That(p.BoldSystem, Is.SameAs(s.System));
        Assert.That(p.BoldObjectIsNew, Is.True);
        Assert.That(p.BoldExistenceState, Is.EqualTo(TBoldExistenceState.besExisting));
        TBoldObjectList people = s.System.Classes(p.BoldClassTypeInfo.TopSortedIndex)!;
        Assert.That(people.Count, Is.EqualTo(1));
        Assert.That(people.Includes(p), Is.True);
        Assert.That(s.System.Classes(0)!.Count, Is.EqualTo(2), "the root class list holds every object");
        Assert.That(s.System.BoldDirty, Is.False, "a transient system has no persistent changes");

        p.Delete();
        Assert.That(people.Count, Is.EqualTo(0));
        Assert.That(s.System.Classes(0)!.Includes(b), Is.True);

        s.System.MakeDefault();
        Assert.That(TBoldSystem.DefaultSystem(), Is.SameAs(s.System));
    }

    [Test]
    [Covers(typeof(TBoldObjectReference), "BoldObject", "Locator")]
    [Covers("BoldLinks", "TBoldDirectSingleLinkController", "TBoldDirectMultiLinkController", "TBoldDirectMultiLinkController.AddLocator", "TBoldDirectSingleLinkController.SetLocator")]
    public void Single_and_multi_links_keep_both_ends()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson(), bob = s.NewPerson(), cy = s.NewPerson();
        TBoldObject house = s.NewBuilding(), flat = s.NewBuilding();

        Home(ann).BoldObject = house;
        Home(bob).BoldObject = house;
        Assert.That(Residents(house).Count, Is.EqualTo(2));
        Assert.That(Residents(house)[0], Is.SameAs(ann));

        Residents(flat).Add(bob);
        Assert.That(Home(bob).BoldObject, Is.SameAs(flat), "adding to the other end moves the link");
        Assert.That(Residents(house).Count, Is.EqualTo(1));

        Home(ann).BoldObject = null;
        Assert.That(Residents(house).Count, Is.EqualTo(0));

        Friends(ann).Add(bob);
        Friends(ann).Add(cy);
        Assert.That(FriendOf(bob).Includes(ann) && FriendOf(cy).Includes(ann), Is.True);
        Friends(ann).Move(1, 0);
        Assert.That(Friends(ann)[0], Is.SameAs(cy), "friends are ordered");
        Friends(ann).Remove(cy);
        Assert.That(FriendOf(cy).Count, Is.EqualTo(0));

        Assert.That(Friends(ann).Includes(bob), Is.True);
        bob.Delete();
        Assert.That(Friends(ann).Count == 0 && Residents(flat).Count == 0, Is.True, "deleting unlinks");
        Assert.That(s.System.AssertLinkIntegrity(), Is.True);
    }

    /// <summary>A sample whose Building.residents role has the delete action <paramref name="action"/> (or the aggregation's default).</summary>
    private static ObjectSpaceSample WithResidents(string? action, TAggregationKind aggregation = TAggregationKind.akNone) =>
        new(m =>
        {
            var residents = (TMoldRole)m.Classes.ItemsByName("Building")!.AllBoldMembers.ItemsByName("residents")!;
            residents.Aggregation = aggregation;
            if (action is not null)
            {
                residents.SetBoldTV(BoldDefaultTaggedValues.TAG_DELETEACTION, action);
            }
        });

    private static int Count(ObjectSpaceSample s, string className) =>
        s.System.Classes(s.SystemTypeInfo.ClassTypeInfoByExpressionName(className)!.TopSortedIndex)!.Count;

    /// <summary>The M3 delete rules: allow unlinks, prohibit refuses while linked, cascade deletes the linked objects; link objects go with either end.</summary>
    [Test]
    [Covers(typeof(TBoldObject), "CanDelete", "Delete", "InternalCanDelete")]
    public void Delete_rules_allow_prohibit_and_cascade()
    {
        using (ObjectSpaceSample s = WithResidents(null))
        {
            TBoldObject ann = s.NewPerson(), house = s.NewBuilding();
            Home(ann).BoldObject = house;
            Assert.That(house.CanDelete(), Is.True, "allow is the default without aggregation");
            house.Delete();
            Assert.That(Home(ann).BoldObject, Is.Null, "allow unlinks");
            Assert.That(Count(s, "Person"), Is.EqualTo(1));
        }

        using (ObjectSpaceSample s = WithResidents(BoldDefaultTaggedValues.TV_DELETEACTION_PROHIBIT))
        {
            TBoldObject ann = s.NewPerson(), house = s.NewBuilding(), shed = s.NewBuilding();
            Home(ann).BoldObject = house;
            Assert.That(house.CanDelete(), Is.False, "prohibit refuses while the role has objects");
            Assert.That(BoldSystem.GetBoldLastFailureReason()?.Reason, Is.EqualTo(BoldCoreConsts.sObjectHasRelations));
            Assert.Throws<EBoldFailure>(() => house.Delete());
            Assert.That(Count(s, "Building"), Is.EqualTo(2), "the refused delete changed nothing");
            Assert.That(Home(ann).BoldObject, Is.SameAs(house));
            shed.Delete();
            Assert.That(Count(s, "Building"), Is.EqualTo(1), "an empty role allows the delete");
            Home(ann).BoldObject = null;
            house.Delete();
            Assert.That(Count(s, "Building"), Is.EqualTo(0));
        }

        using (ObjectSpaceSample s = WithResidents(BoldDefaultTaggedValues.TV_DELETEACTION_CASCADE))
        {
            TBoldObject ann = s.NewPerson(), bob = s.NewPerson(), cy = s.NewPerson(), house = s.NewBuilding();
            Home(ann).BoldObject = house;
            Home(bob).BoldObject = house;
            Friends(cy).Add(ann);
            house.Delete();
            Assert.That(Count(s, "Person"), Is.EqualTo(1), "cascade deletes the residents");
            Assert.That(Friends(cy).Count, Is.EqualTo(0), "and the residents' links");
            Assert.That(s.System.AssertLinkIntegrity(), Is.True);
        }

        using (ObjectSpaceSample s = WithResidents(null, TAggregationKind.akComposite))
        {
            TBoldObject ann = s.NewPerson(), house = s.NewBuilding();
            Home(ann).BoldObject = house;
            house.Delete();
            Assert.That(Count(s, "Person"), Is.EqualTo(0), "a composite cascades by default");
        }

        using (ObjectSpaceSample s = WithResidents(null, TAggregationKind.akAggregate))
        {
            TBoldObject ann = s.NewPerson(), house = s.NewBuilding();
            Home(ann).BoldObject = house;
            Assert.That(house.CanDelete(), Is.False, "an aggregate prohibits by default");
        }

        using (var s = new ObjectSpaceSample())
        {
            TBoldObject ann = s.NewPerson(), bob = s.NewPerson(), cy = s.NewPerson();
            Friends(ann).Add(bob);
            Friends(ann).Add(cy);
            Assert.That(Count(s, "Friendship"), Is.EqualTo(2), "each link has a link object");
            bob.Delete();
            Assert.That(Count(s, "Friendship"), Is.EqualTo(1), "deleting an end deletes its link objects");
            ann.Delete();
            Assert.That(Count(s, "Friendship"), Is.EqualTo(0));
            Assert.That(FriendOf(cy).Count, Is.EqualTo(0));
        }
    }

    [Test]
    [Covers(typeof(TBoldSystem), "StartTransaction", "CommitTransaction", "RollbackTransaction", "TryCommitTransaction")]
    public void Rollback_restores_values_links_and_objects()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson();
        TBoldObject house = s.NewBuilding();
        ann.BoldMemberByExpressionName("firstName").AsString = "Ann";

        s.System.StartTransaction();
        Assert.That(s.System.InTransaction(), Is.True);
        ann.BoldMemberByExpressionName("firstName").AsString = "Anna";
        Home(ann).BoldObject = house;
        TBoldObject bob = s.NewPerson();
        s.System.RollbackTransaction();

        Assert.That(s.System.InTransaction(), Is.False);
        Assert.That(ann.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Ann"));
        Assert.That(Home(ann).BoldObject, Is.Null);
        Assert.That(Residents(house).Count, Is.EqualTo(0));
        Assert.That(s.System.Classes(ann.BoldClassTypeInfo.TopSortedIndex)!.Includes(bob), Is.False, "objects created in the transaction are gone");

        s.System.StartTransaction();
        ann.BoldMemberByExpressionName("firstName").AsString = "Anne";
        s.System.CommitTransaction();
        Assert.That(ann.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Anne"));
    }

    [Test]
    [Covers(typeof(TBoldUndoHandler), "SetCheckPoint", "UndoLatest", "RedoLatest", "GetEnabled", "SetEnabled", "GetUndoList")]
    public void Undo_and_redo_attribute_changes()
    {
        using var s = new ObjectSpaceSample();
        TBoldObject ann = s.NewPerson();
        var undo = (TBoldUndoHandler)s.System.UndoHandler;
        Assert.That(undo.GetEnabled(), Is.False, "undo is off until enabled");
        undo.SetEnabled(true);
        TBoldMember name = ann.BoldMemberByExpressionName("firstName");
        name.AsString = "Ann";
        undo.SetCheckPoint();
        Assert.That(undo.GetUndoList().Count, Is.EqualTo(2), "the checkpoint starts a new block after the one with changes");
        name.AsString = "Anna";
        undo.UndoLatest();
        Assert.That(name.AsString, Is.EqualTo("Ann"));
        undo.RedoLatest();
        Assert.That(name.AsString, Is.EqualTo("Anna"));
    }

    [Test]
    [Covers(typeof(TBoldUndoHandler), "SetNamedCheckPoint", "UndoBlock", "RedoBlock", "CanUndoBlock", "CanRedoBlock", "HandleObject", "HandleMember", "GetRedoList")]
    [Covers(typeof(TBoldUndoBlockList), "Count", "IndexOf", "RenameBlock", "MergeBlocks", "BlocksByName")]
    public void Undo_and_redo_links_created_and_deleted_objects()
    {
        using var s = new ObjectSpaceSample();
        var undo = (TBoldUndoHandler)s.System.UndoHandler;
        undo.SetEnabled(true);
        TBoldObject ann = s.NewPerson(), house = s.NewBuilding();
        ann.BoldMemberByExpressionName("firstName").AsString = "Ann";

        undo.SetNamedCheckPoint("link");
        Home(ann).BoldObject = house;
        undo.SetNamedCheckPoint("create");
        TBoldObject bob = s.NewPerson();
        bob.BoldMemberByExpressionName("firstName").AsString = "Bob";
        Friends(ann).Add(bob);
        undo.SetNamedCheckPoint("delete");
        house.Delete();
        Assert.That(Home(ann).BoldObject, Is.Null);

        Assert.That(undo.CanUndoBlock("delete"), Is.True);
        undo.UndoBlock("delete");
        Assert.That(Count(s, "Building"), Is.EqualTo(1), "undoing a delete brings the object back");
        Assert.That(Home(ann).BoldObject, Is.SameAs(s.System.Classes(house.BoldClassTypeInfo.TopSortedIndex)![0]), "with its links");

        undo.UndoBlock("create");
        Assert.That(Count(s, "Person"), Is.EqualTo(1), "undoing a create removes the object");
        Assert.That(Friends(ann).Count, Is.EqualTo(0), "and its links");

        undo.UndoBlock("link");
        Assert.That(Home(ann).BoldObject, Is.Null, "undoing a link change restores the old link");
        Assert.That(ann.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Ann"), "changes before the block stay");

        Assert.That(undo.GetRedoList().Count, Is.EqualTo(3));
        Assert.That(undo.CanRedoBlock("link"), Is.True);
        undo.RedoBlock("link");
        Assert.That(Home(ann).BoldObject, Is.Not.Null, "redo reapplies the link");
        undo.RedoBlock("create");
        Assert.That(Count(s, "Person"), Is.EqualTo(2), "and recreates the object");
        Assert.That(Friends(ann).Count, Is.EqualTo(1));
        Assert.That(Friends(ann)[0]!.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Bob"));
        Assert.That(s.System.AssertLinkIntegrity(), Is.True);
    }
}
