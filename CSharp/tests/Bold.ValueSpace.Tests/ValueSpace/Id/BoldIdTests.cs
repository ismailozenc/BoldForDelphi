// Delphi: Source/ValueSpace/Id/BoldId.pas
// Port map: CSharp/port/units/BoldId.md
namespace Bold.Tests;

public class BoldIdTests
{
    private static TBoldDefaultID Def(int value, int topSortedIndex = 1, bool exact = true) =>
        new(topSortedIndex, exact) { AsInteger = value };

    [Test]
    [Covers(typeof(ValueSpaceConst), "*")]
    public void Messages_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(ValueSpaceConst), "Source/ValueSpace/Id/ValueSpaceConst.pas");
    }

    [Test]
    [Covers(typeof(TBoldID), "*")]
    [Covers(typeof(TBoldMemberID), "*")]
    [Covers(typeof(TBoldSubMemberID), "*")]
    public void Member_ids_carry_their_index()
    {
        var id = new TBoldMemberID(3);
        Assert.That(id.MemberIndex, Is.EqualTo(3));
        Assert.That(id.GetStreamName(), Is.EqualTo(BoldDefaultStreamNames.BOLDMEMBERIDNAME));
        using var sub = new TBoldSubMemberID(id, true, 7);
        Assert.That(sub.MemberIndex, Is.EqualTo(7));
    }

    [Test]
    [Covers(typeof(TBoldObjectId), "*")]
    [Covers(typeof(TBoldInternalObjectId), "*")]
    [Covers(typeof(TBoldExternalObjectID), "*")]
    public void Internal_ids_are_numbered_and_not_storable()
    {
        var a = new TBoldInternalObjectId(2, true);
        var b = new TBoldInternalObjectId(2, true);
        Assert.That(a.IsEqual(b), Is.False, "each id gets the next number");
        Assert.That(a.IsStorable, Is.False);
        Assert.That(a.TopSortedIndex, Is.EqualTo(2));
        Assert.That(a.TopSortedIndexExact, Is.True);
        Assert.That(a.TimeStamp, Is.EqualTo(BoldDefs.BOLDMAXTIMESTAMP));

        TBoldObjectId inexact = a.CloneWithClassId(5, false);
        Assert.That(inexact.IsEqual(a), Is.True, "a clone keeps the number");
        Assert.That(inexact.TopSortedIndex, Is.EqualTo(5));
        Assert.That(inexact.TopSortedIndexExact, Is.False);
        Assert.That(a.Clone().Hash, Is.EqualTo(a.Hash));
        Assert.That(a.AsString, Is.EqualTo(a.ToString()));

        var fixedNumber = new TBoldInternalObjectId(42, 1, true);
        Assert.That(fixedNumber.AsString, Is.EqualTo("42"));
        Assert.That(fixedNumber.GetStreamName(), Is.EqualTo(BoldDefaultStreamNames.BOLDINTERNALIDNAME));
        Assert.That(fixedNumber.IsEqual(Def(42)), Is.False, "an internal id never equals a stored id");
        Assert.That(fixedNumber.IsEqual(null), Is.False);
    }

    [Test]
    [Covers(typeof(TBoldIdList), "*")]
    [Covers(typeof(TBoldObjectIdHashIndex), "*")]
    [Covers(typeof(TBoldObjectIdList), "*")]
    public void Object_id_list_holds_clones_and_finds_equal_ids()
    {
        using var list = new TBoldObjectIdList();
        TBoldDefaultID five = Def(5);
        list.Add(five);
        Assert.That(list[0], Is.Not.SameAs(five), "Add stores a clone");
        Assert.That(list.IdInList(Def(5)), Is.True);
        Assert.That(list.IdInList(null), Is.False);
        Assert.That(list.GetStreamName(), Is.EqualTo(BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME));

        list.AddIfNotInList(Def(5));
        Assert.That(list.Count, Is.EqualTo(1));

        // Past ten ids the list looks up through its hash index.
        for (int i = 10; i < 30; i++)
        {
            list.Add(Def(i));
        }

        Assert.That(list.IndexByID(Def(17)), Is.EqualTo(8));
        Assert.That(list.IDByID(Def(17)), Is.SameAs(list[8]));
        Assert.That(list.IDByID(Def(99)), Is.Null);
        Assert.That(list.IndexByID(Def(99)), Is.EqualTo(-1));

        list.Insert(0, Def(1));
        Assert.That(list[0].AsString, Is.EqualTo("1"));

        list.ReplaceID(Def(17), Def(77));
        Assert.That(list.IndexByID(Def(77)), Is.EqualTo(9), "replaced in place");
        Assert.That(list.IdInList(Def(17)), Is.False);

        using TBoldObjectIdList copy = list.Clone();
        Assert.That(copy.ContainsSameIDs(list), Is.True);
        copy.remove(copy[0]);
        Assert.That(copy.ContainsSameIDs(list), Is.False);
        copy.remove(Def(5));
        Assert.That(copy.Count, Is.EqualTo(list.Count - 1), "remove takes the id object itself, not an equal one");

        using var small = new TBoldObjectIdList();
        small.AddList(copy);
        Assert.That(small.Count, Is.EqualTo(copy.Count));
        small.Clear();
        small.Add(Def(3));
        small.Add(Def(4));
        Assert.That(small.CommaSeparatedIdList(), Is.EqualTo("3, 4"));
    }

    [Test]
    [Covers(typeof(TBoldIDTranslationList), "*")]
    [Covers(typeof(TBoldObjectIdList), nameof(TBoldObjectIdList.ApplyTranslationList), nameof(TBoldObjectIdList.ExactifyIds))]
    public void Translation_list_maps_old_ids_to_new_ones()
    {
        var internalId = new TBoldInternalObjectId(1, true);
        using var translation = new TBoldIDTranslationList();
        translation.AddTranslation(internalId, Def(100));
        translation.AddTranslation(Def(7, 1, false), Def(7, 3, true));
        Assert.That(translation.Count, Is.EqualTo(2));
        Assert.That(translation.GetStreamName(), Is.EqualTo(BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME));
        Assert.That(translation.TranslateToNewId(internalId)!.AsString, Is.EqualTo("100"));
        Assert.That(translation.TranslateToOldId(Def(100))!.IsEqual(internalId), Is.True);
        TBoldDefaultID untranslated = Def(8);
        Assert.That(translation.TranslateToNewId(untranslated), Is.SameAs(untranslated), "an id without translation maps to itself");
        Assert.That(translation.TranslateToOldId(untranslated), Is.SameAs(untranslated));
        Assert.That(translation.TranslateToNewId(null), Is.Null);
        Assert.That(translation.OldIds(0).IsEqual(internalId), Is.True);
        Assert.That(translation.NewIds(1).TopSortedIndex, Is.EqualTo(3));

        using var list = new TBoldObjectIdList();
        list.Add(Def(1));
        list.Add(internalId);
        list.Add(Def(7, 1, false));
        list.ApplyTranslationList(translation);
        Assert.That(list.CommaSeparatedIdList(), Is.EqualTo("1, 100, 7"));
        Assert.That(list[2].TopSortedIndex, Is.EqualTo(3), "the inexact id became exact");

        using var inexact = new TBoldObjectIdList();
        inexact.Add(Def(7, 1, false));
        inexact.Add(Def(9));
        inexact.ExactifyIds(translation);
        Assert.That(inexact[0].TopSortedIndexExact, Is.True);
        Assert.That(inexact[1].AsString, Is.EqualTo("9"));
    }

    [Test]
    [Covers(typeof(TBoldMemberIdList), "*")]
    public void Member_id_list_returns_member_ids()
    {
        using var list = new TBoldMemberIdList();
        list.Add(new TBoldMemberID(2));
        list.Add(new TBoldMemberID(4));
        Assert.That(list[1].MemberIndex, Is.EqualTo(4));
        Assert.That(list.GetStreamName(), Is.EqualTo(BoldDefaultStreamNames.BOLDMEMBERIDLISTNAME));
    }

    [Test]
    [Covers(typeof(EBoldOperationFailedForIdList), "*")]
    public void Operation_failed_exception_keeps_a_copy_of_the_ids()
    {
        using var ids = new TBoldObjectIdList();
        ids.Add(Def(3));
        var e = new EBoldOperationFailedForIdList("Failed for %d objects", [1], ids);
        ids.Clear();
        Assert.That(e.Message, Is.EqualTo("Failed for 1 objects"));
        Assert.That(e.IdList.Count, Is.EqualTo(1));
    }
}
