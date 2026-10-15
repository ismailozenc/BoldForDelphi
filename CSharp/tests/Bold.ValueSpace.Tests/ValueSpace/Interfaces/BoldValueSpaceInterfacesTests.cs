// Delphi: Source/ValueSpace/Interfaces/BoldValueSpaceInterfaces.pas, BoldValueInterfaces.pas
// Port map: CSharp/port/units/BoldValueSpaceInterfaces.md, CSharp/port/units/BoldValueInterfaces.md
namespace Bold.Tests;

public class BoldValueSpaceInterfacesTests
{
    private static TBoldDefaultID Def(int value) => new(1, true) { AsInteger = value };

    private static TBoldFreeStandingValueSpace Source()
    {
        var vs = new TBoldFreeStandingValueSpace();
        foreach (int id in new[] { 1, 2 })
        {
            TBoldFreeStandingObjectContents o = vs.GetEnsuredFSObjectContentsByObjectId(Def(id));
            o.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
            o.EnsureMemberByIndex(0, "String");
            o.EnsureMemberByIndex(1, "Integer");
            o.EnsureMemberByIndex(2, "Float");
            ((TBFSString)o.FSValueByIndex(0)!).AsString = "s" + id;
            ((TBFSInteger)o.FSValueByIndex(1)!).AsInteger = id;
            o.FSValueByIndex(0)!.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
            o.FSValueByIndex(1)!.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
            // Member 2 stays invalid and is not copied by default.
        }

        vs.EnsureObjectId(Def(3));
        return vs;
    }

    [Test]
    [Covers(typeof(BoldValueSpaceInterfaces), "*")]
    [Covers(typeof(TBoldExistenceState))]
    public void Constants_and_enums_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldValueSpaceInterfaces), "Source/ValueSpace/Interfaces/BoldValueSpaceInterfaces.pas");
        PortAssert.EnumsMatchDelphi("Source/ValueSpace/Interfaces/BoldValueSpaceInterfaces.pas", typeof(TBoldExistenceState));
    }

    [Test]
    [Covers(typeof(BoldValueSpaceInterfaces), nameof(BoldValueSpaceInterfaces.BoldApplyPartialValueSpace))]
    [Covers(typeof(IBoldValueSpace), "*")]
    [Covers(typeof(IBoldObjectContents), "*")]
    public void Partial_apply_copies_all_objects_and_valid_members()
    {
        using TBoldFreeStandingValueSpace source = Source();
        using var dest = new TBoldFreeStandingValueSpace();
        BoldValueSpaceInterfaces.BoldApplyPartialValueSpace(dest, source, null, null, false);

        IBoldValueSpace d = dest;
        Assert.That(d.HasContentsForId(Def(3)), Is.False, "ids without contents are skipped");
        IBoldObjectContents o = d.ObjectContentsByObjectId(Def(2))!;
        Assert.That(o.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsModified));
        Assert.That(((IBoldStringContent)o.ValueByIndex(0)!).asString, Is.EqualTo("s2"));
        Assert.That(o.ValueByIndex(0)!.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsModified));
        Assert.That(((IBoldIntegerContent)o.ValueByMemberId(new TBoldMemberID(1))!).asInteger, Is.EqualTo(2));
        Assert.That(o.ValueByIndex(2), Is.Null, "invalid values are ignored by default");
        Assert.That(o.MemberCount, Is.EqualTo(2));
        Assert.That(o.IsModified, Is.True);
        Assert.That(o.ObjectId.AsString, Is.EqualTo("2"));
        Assert.That(o.TimeStamp, Is.EqualTo(-1));
        Assert.That(o.GlobalId, Is.EqualTo(""));
        Assert.That(o.IsReadOnly, Is.False);
        Assert.That(o.BoldExistenceState, Is.EqualTo(TBoldExistenceState.besExisting));
        Assert.That(d.EnsuredObjectContentsByObjectId(Def(2)), Is.SameAs(o));
    }

    [Test]
    [Covers(typeof(BoldValueSpaceInterfaces), nameof(BoldValueSpaceInterfaces.BoldApplyPartialValueSpace))]
    public void Partial_apply_limits_objects_and_members_and_can_force_current()
    {
        using TBoldFreeStandingValueSpace source = Source();
        using var dest = new TBoldFreeStandingValueSpace();
        using var objects = new TBoldObjectIdList();
        objects.Add(Def(1));
        objects.Add(Def(3));
        using var members = new TBoldMemberIdList();
        members.Add(new TBoldMemberID(1));
        BoldValueSpaceInterfaces.BoldApplyPartialValueSpace(dest, source, objects, members, true,
            TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsModified));

        Assert.That(dest.GetHasContentsForId(Def(2)), Is.False);
        TBoldFreeStandingObjectContents o = dest.GetFSObjectContentsByObjectId(Def(1))!;
        Assert.That(o.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsCurrent), "forced current");
        Assert.That(o.FSValueByIndex(0), Is.Null, "member 0 was not asked for");
        Assert.That(((TBFSInteger)o.FSValueByIndex(1)!).AsInteger, Is.EqualTo(1));
        Assert.That(o.FSValueByIndex(1)!.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsCurrent));

        using var none = new TBoldFreeStandingValueSpace();
        members.Clear();
        members.Add(new TBoldMemberID(0));
        BoldValueSpaceInterfaces.BoldApplyPartialValueSpace(none, source, objects, members, false,
            TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsModified));
        Assert.That(none.GetFSObjectContentsByObjectId(Def(1))!.FSValueByIndex(0), Is.Null, "modified values ignored on request");
    }

    [Test]
    [Covers(typeof(IBoldValue), "*")]
    [Covers(typeof(IBoldNullableValue), "*")]
    [Covers(typeof(IBoldStringContent), "*")]
    [Covers(typeof(IBoldIntegerContent), "*")]
    [Covers(typeof(IBoldFloatContent), "*")]
    [Covers(typeof(IBoldCurrencyContent), "*")]
    [Covers(typeof(IBoldBooleanContent), "*")]
    [Covers(typeof(IBoldDateContent), "*")]
    [Covers(typeof(IBoldTimeContent), "*")]
    [Covers(typeof(IBoldDateTimeContent), "*")]
    [Covers(typeof(IBoldBlobContent), "*")]
    [Covers(typeof(IBoldTypedBlob), "*")]
    [Covers(typeof(IBoldObjectIdRef), "*")]
    [Covers(typeof(IBoldObjectIdRefPair), "*")]
    [Covers(typeof(IBoldObjectIdListRef), "*")]
    [Covers(typeof(IBoldObjectIdListRefPair), "*")]
    public void Value_interface_properties_reach_the_accessors()
    {
        IBoldStringContent s = new TBFSString();
        s.asString = "a";
        Assert.That(s.asString, Is.EqualTo("a"));
        Assert.That(s.ContentName, Is.EqualTo("String"));
        s.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
        Assert.That(s.GetBoldPersistenceState(), Is.EqualTo(TBoldValuePersistenceState.bvpsModified));
        s.SetContentToNull();
        Assert.That(s.IsNull, Is.True);

        IBoldIntegerContent i = new TBFSInteger();
        i.asInteger = 2;
        Assert.That(i.asInteger, Is.EqualTo(2));
        IBoldFloatContent f = new TBFSFloat();
        f.asFloat = 2.5;
        Assert.That(f.asFloat, Is.EqualTo(2.5));
        IBoldCurrencyContent c = new TBFSCurrency();
        c.asCurrency = 1.25m;
        Assert.That(c.asCurrency, Is.EqualTo(1.25m));
        IBoldBooleanContent b = new TBFSBoolean();
        b.asBoolean = true;
        Assert.That(b.asBoolean, Is.True);
        var when = new DateTime(2020, 1, 2, 3, 4, 5);
        IBoldDateContent d = new TBFSDate();
        d.asDate = when.Date;
        Assert.That(d.asDate, Is.EqualTo(when.Date));
        IBoldTimeContent t = new TBFSTime();
        t.asTime = when;
        Assert.That(t.asTime, Is.EqualTo(when));
        IBoldDateTimeContent dt = new TBFSDateTime();
        dt.asDateTime = when;
        Assert.That(dt.asDateTime, Is.EqualTo(when));
        IBoldBlobContent blob = new TBFSBlob();
        blob.asBlob = "x";
        Assert.That(blob.asBlob, Is.EqualTo("x"));
        IBoldTypedBlob typed = new TBFSTypedBlob();
        typed.ContentTypeContent = "text/plain";
        Assert.That(typed.ContentTypeContent, Is.EqualTo("text/plain"));

        IBoldObjectIdRef r = new TBFSObjectIDRef();
        r.SetFromId(Def(5));
        r.OrderNo = 2;
        Assert.That(r.Id!.AsString, Is.EqualTo("5"));
        Assert.That(r.OrderNo, Is.EqualTo(2));

        IBoldObjectIdRefPair rp = new TBFSObjectIDRefPair();
        rp.SetFromIds(Def(5), Def(6));
        rp.OrderNo = 3;
        Assert.That(rp.Id1!.AsString + rp.Id2!.AsString + rp.OrderNo, Is.EqualTo("563"));

        using var ids = new TBoldObjectIdList();
        ids.Add(Def(7));
        IBoldObjectIdListRef l = new TBFSObjectIdListref();
        l.SetFromIdList(ids);
        Assert.That(l.Count, Is.EqualTo(1));
        Assert.That(l.IdList(0).AsString, Is.EqualTo("7"));

        IBoldObjectIdListRefPair lp = new TBFSObjectIdListrefPair();
        lp.SetFromIdLists(ids, ids);
        Assert.That(lp.Count, Is.EqualTo(1));
        Assert.That(lp.IdList1(0).AsString + lp.IdList2(0).AsString, Is.EqualTo("77"));
    }
}
