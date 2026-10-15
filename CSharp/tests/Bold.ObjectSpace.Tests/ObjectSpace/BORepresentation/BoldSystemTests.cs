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
}
