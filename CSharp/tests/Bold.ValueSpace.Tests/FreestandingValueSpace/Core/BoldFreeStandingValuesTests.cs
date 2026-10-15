// Delphi: Source/FreestandingValueSpace/Core/BoldFreeStandingValues.pas
// Port map: CSharp/port/units/BoldFreeStandingValues.md
namespace Bold.Tests;

public class BoldFreeStandingValuesTests
{
    private static TBoldDefaultID Def(int value, int topSortedIndex = 1) => new(topSortedIndex, true) { AsInteger = value };

    private static TBoldObjectIdList Ids(params int[] values)
    {
        var list = new TBoldObjectIdList();
        foreach (int v in values)
        {
            list.Add(Def(v));
        }

        return list;
    }

    private static int nextHolder = 1000;

    /// <summary>A new value of the stream name, held by an object in <paramref name="vs"/> so translating the value space translates it.</summary>
    private static T InSpace<T>(TBoldFreeStandingValueSpace vs, string streamName) where T : TBoldFreeStandingValue
    {
        TBoldFreeStandingObjectContents holder = vs.GetEnsuredFSObjectContentsByObjectId(Def(Interlocked.Increment(ref nextHolder)));
        holder.EnsureMemberByIndex(0, streamName);
        return (T)holder.FSValueByIndex(0)!;
    }

    [Test]
    [Covers(typeof(FreeStandingValuesConst), "*")]
    public void Messages_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(FreeStandingValuesConst), "Source/FreestandingValueSpace/Core/FreeStandingValuesConst.pas");
    }

    [Test]
    [Covers(typeof(TBoldFreeStandingElementFactory), "*")]
    [Covers(typeof(BoldFreeStandingValueFactories), "*")]
    [Covers("BoldFreeStandingValueFactories", "initialization")]
    [Covers("BoldFreeStandingValues", "TBoldFreeStandingElementClass")]
    public void Factory_creates_the_registered_class_for_each_stream_name()
    {
        TBoldFreeStandingElementFactory f = BoldFreeStandingValueFactories.FreeStandingValueFactory!;
        var expected = new Dictionary<string, Type>
        {
            [BoldDefaultStreamNames.BOLDOBJECTCONTENTSNAME] = typeof(TBoldFreeStandingObjectContents),
            [BoldDefaultStreamNames.BoldContentName_String] = typeof(TBFSString),
            [BoldDefaultStreamNames.BoldContentName_Currency] = typeof(TBFSCurrency),
            [BoldDefaultStreamNames.BoldContentName_Float] = typeof(TBFSFloat),
            [BoldDefaultStreamNames.BoldContentName_Integer] = typeof(TBFSInteger),
            [BoldDefaultStreamNames.BoldContentName_Boolean] = typeof(TBFSBoolean),
            [BoldDefaultStreamNames.BoldContentName_Date] = typeof(TBFSDate),
            [BoldDefaultStreamNames.BoldContentName_Time] = typeof(TBFSTime),
            [BoldDefaultStreamNames.BoldContentName_DateTime] = typeof(TBFSDateTime),
            [BoldDefaultStreamNames.BoldContentName_Blob] = typeof(TBFSBlob),
            [BoldDefaultStreamNames.BoldContentName_TypedBlob] = typeof(TBFSTypedBlob),
            [BoldDefaultStreamNames.BoldContentName_ObjectIdRef] = typeof(TBFSObjectIDRef),
            [BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair] = typeof(TBFSObjectIDRefPair),
            [BoldDefaultStreamNames.BoldContentName_ObjectIdListRef] = typeof(TBFSObjectIdListref),
            [BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair] = typeof(TBFSObjectIdListrefPair),
        };
        foreach (var (name, type) in expected)
        {
            TBoldInterfacedObject instance = f.CreateInstance(name);
            Assert.That(instance.GetType(), Is.EqualTo(type), name);
            Assert.That(((IBoldStreamable)instance).GetStreamName(), Is.EqualTo(name), "each class streams under the name it is registered by");
        }

        var e = Assert.Throws<EBold>(() => f.CreateInstance("NoSuchContent"))!;
        Assert.That(e.Message, Is.EqualTo("TBoldFreeStandingElementFactory.CreateInstance: No freestanding class registered for name NoSuchContent"));

        using var own = new TBoldFreeStandingElementFactory();
        own.RegisterFreeStandingClass("X", typeof(TBFSInteger));
        Assert.That(own.CreateInstance("X"), Is.InstanceOf<TBFSInteger>());
        Assert.Throws<EBold>(() => own.CreateInstance("x"), "names are case-sensitive, as in Delphi");
    }

    [Test]
    [Covers(typeof(TBoldFreeStandingElement), "*")]
    [Covers(typeof(TBoldFreeStandingValue), "*")]
    [Covers(typeof(TBoldFreeStandingNullableValue), "*")]
    [Covers(typeof(TBFSInteger), "*")]
    [Covers(typeof(TBFSString), "*")]
    [Covers(typeof(TBFSCurrency), "*")]
    [Covers(typeof(TBFSFloat), "*")]
    [Covers(typeof(TBFSBoolean), "*")]
    public void Simple_values_copy_content_and_state()
    {
        var i = new TBFSInteger();
        Assert.That(i.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsInvalid), "new values are invalid");
        Assert.That(i.GetContentIsNull(), Is.False, "new values are not null");
        i.SetContentToNull();
        Assert.That(((IBoldNullableValue)i).IsNull, Is.True);
        i.AsInteger = 42;
        Assert.That(i.GetContentIsNull(), Is.False, "setting a value clears null");
        i.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;

        var copy = new TBFSInteger();
        copy.AssignContent(i);
        Assert.That(copy.AsInteger, Is.EqualTo(42));
        Assert.That(copy.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsModified), "AssignContent copies the state");
        Assert.That(copy.ContentName, Is.EqualTo("Integer"));
        Assert.That(copy.GetContentName(), Is.EqualTo("Integer"));

        i.SetContentToNull();
        copy.AssignContent(i);
        Assert.That(copy.GetContentIsNull(), Is.True);

        var e = Assert.Throws<EBold>(() => copy.AssignContent(new TBFSString()))!;
        Assert.That(e.Message, Is.EqualTo("TBFSInteger.AssignContentValue: unknown type of source"));

        var s = new TBFSString { AsString = "abc" };
        var s2 = new TBFSString();
        s2.AssignContent(s);
        Assert.That(((IBoldStringContent)s2).asString, Is.EqualTo("abc"));

        var c = new TBFSCurrency { AsCurrency = 12.3456m };
        var c2 = new TBFSCurrency();
        c2.AssignContent(c);
        Assert.That(c2.AsCurrency, Is.EqualTo(12.3456m));

        var fl = new TBFSFloat { AsFloat = 1.5 };
        var fl2 = new TBFSFloat();
        fl2.AssignContent(fl);
        Assert.That(fl2.AsFloat, Is.EqualTo(1.5));

        var b = new TBFSBoolean { AsBoolean = true };
        var b2 = new TBFSBoolean();
        b2.AssignContent(b);
        Assert.That(b2.AsBoolean, Is.True);

        Assert.Throws<EBold>(() => s2.AssignContent(c));
        Assert.Throws<EBold>(() => c2.AssignContent(fl));
        Assert.Throws<EBold>(() => fl2.AssignContent(b));
        Assert.Throws<EBold>(() => b2.AssignContent(s));
    }

    [Test]
    [Covers(typeof(TBFSDateTimeAbstract), "*")]
    [Covers(typeof(TBFSDateTime), "*")]
    [Covers(typeof(TBFSDate), "*")]
    [Covers(typeof(TBFSTime), "*")]
    [Covers(typeof(TBFSBlobAbstract), "*")]
    [Covers(typeof(TBFSBlob), "*")]
    [Covers(typeof(TBFSTypedBlob), "*")]
    public void Date_time_and_blob_values_copy_from_their_own_kind()
    {
        var when = new DateTime(2001, 2, 3, 4, 5, 6);
        var dt = new TBFSDateTime { AsDateTime = when };
        var dt2 = new TBFSDateTime();
        dt2.AssignContent(dt);
        Assert.That(dt2.AsDateTime, Is.EqualTo(when));
        Assert.That(dt2.GetContentAsDate(), Is.EqualTo(when), "the three kinds share one store");

        var d = new TBFSDate { AsDate = when.Date };
        var d2 = new TBFSDate();
        d2.AssignContent(d);
        Assert.That(d2.AsDate, Is.EqualTo(when.Date));

        var t = new TBFSTime { AsTime = when };
        var t2 = new TBFSTime();
        t2.AssignContent(t);
        Assert.That(t2.AsTime, Is.EqualTo(when));
        Assert.Throws<EBold>(() => t2.AssignContent(d), "a date is not a time");
        Assert.Throws<EBold>(() => d2.AssignContent(dt));
        Assert.Throws<EBold>(() => dt2.AssignContent(t));

        var blob = new TBFSBlob { AsBlob = "\u0001ÿ" };
        var blob2 = new TBFSBlob();
        blob2.AssignContent(blob);
        Assert.That(blob2.AsBlob, Is.EqualTo("\u0001ÿ"));
        Assert.That(blob2.GetStreamName(), Is.EqualTo("Blob"));

        var typed = new TBFSTypedBlob { AsBlob = "data", ContentTypeContent = "image/png" };
        var typed2 = new TBFSTypedBlob();
        typed2.AssignContent(typed);
        Assert.That(typed2.AsBlob, Is.EqualTo("data"));
        Assert.That(typed2.ContentTypeContent, Is.EqualTo("image/png"));
        Assert.That(typed2.GetStreamName(), Is.EqualTo("TypedBlob"));
        Assert.Throws<EBold>(() => typed2.AssignContent(blob), "a plain blob has no content type");
        blob2.AssignContent(typed);
        Assert.That(blob2.AsBlob, Is.EqualTo("data"), "a typed blob is a blob");

        typed.SetContentToNull();
        typed2.AssignContent(typed);
        Assert.That(typed2.GetContentIsNull(), Is.False, "setting the content type clears null again, as in Delphi");
    }

    [Test]
    [Covers(typeof(TBFSObjectIDRefAbstract), "*")]
    [Covers(typeof(TBFSObjectIDRef), "*")]
    [Covers(typeof(TBFSObjectIDRefPair), "*")]
    public void Single_references_hold_copies_of_ids_and_translate_them()
    {
        var internalId = new TBoldInternalObjectId(1, true);
        var r = new TBFSObjectIDRef { OrderNo = 3 };
        r.SetFromId(internalId);
        Assert.That(r.Id, Is.Not.SameAs(internalId));
        Assert.That(r.Id!.IsEqual(internalId), Is.True);
        TBoldObjectId kept = r.Id;
        r.SetFromId(internalId.Clone());
        Assert.That(r.Id, Is.SameAs(kept), "an equal id is not replaced");

        using var vs = new TBoldFreeStandingValueSpace();
        TBFSObjectIDRef r2 = InSpace<TBFSObjectIDRef>(vs, BoldDefaultStreamNames.BoldContentName_ObjectIdRef);
        r2.AssignContent(r);
        Assert.That(r2.Id!.IsEqual(internalId), Is.True);
        Assert.That(r2.OrderNo, Is.EqualTo(3));
        Assert.Throws<EBold>(() => r2.AssignContent(new TBFSInteger()));

        using var translation = new TBoldIDTranslationList();
        translation.AddTranslation(internalId, Def(100));
        vs.ApplyTranslationList(translation);
        Assert.That(r2.Id!.AsString, Is.EqualTo("100"));
        r2.SetFromId(null);
        Assert.That(r2.Id, Is.Null);
        vs.ApplyTranslationList(translation);
        Assert.That(r2.Id, Is.Null);

        var pair = new TBFSObjectIDRefPair();
        Assert.That(pair.Id1, Is.Null);
        Assert.That(pair.Id2, Is.Null);
        pair.SetFromIds(null, Def(2));
        Assert.That(pair.Id2, Is.Null, "the link id is dropped without an object id");
        pair.SetFromIds(internalId, Def(2));
        pair.OrderNo = 4;
        TBFSObjectIDRefPair pair2 = InSpace<TBFSObjectIDRefPair>(vs, BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair);
        pair2.AssignContent(pair);
        Assert.That(pair2.Id1!.IsEqual(internalId), Is.True);
        Assert.That(pair2.Id2!.AsString, Is.EqualTo("2"));
        Assert.That(pair2.OrderNo, Is.EqualTo(4));
        vs.ApplyTranslationList(translation);
        Assert.That(pair2.Id1!.AsString, Is.EqualTo("100"));
        Assert.Throws<EBold>(() => pair2.AssignContent(r));
        pair.Dispose();
    }

    [Test]
    [Covers(typeof(IBoldFreeStandingIdList), "*")]
    [Covers(typeof(IBoldFreeStandingIdListPair), "*")]
    [Covers(typeof(TBFSObjectIdListref), "*")]
    [Covers(typeof(TBFSObjectIdListrefPair), "*")]
    public void Multi_references_hold_copies_of_id_lists()
    {
        var list = new TBFSObjectIdListref();
        Assert.That(list.Count, Is.EqualTo(0));
        using (TBoldObjectIdList ids = Ids(1, 2, 3))
        {
            list.SetFromIdList(ids);
            ids.Clear();
        }

        Assert.That(list.Count, Is.EqualTo(3), "the list is copied");
        IBoldFreeStandingIdList editable = list;
        editable.AddId(Def(4));
        editable.RemoveId(Def(2));
        editable.RemoveId(Def(99));
        Assert.That(Enumerable.Range(0, list.Count).Select(i => list.IdList(i).AsString), Is.EqualTo(new[] { "1", "3", "4" }));

        using var vs = new TBoldFreeStandingValueSpace();
        TBFSObjectIdListref list2 = InSpace<TBFSObjectIdListref>(vs, BoldDefaultStreamNames.BoldContentName_ObjectIdListRef);
        list2.AddId(Def(9));
        list2.AssignContent(list);
        Assert.That(list2.Count, Is.EqualTo(3), "AssignContent replaces the ids");
        Assert.That(list2.GetIdList(2).AsString, Is.EqualTo("4"));
        Assert.Throws<EBold>(() => list2.AssignContent(new TBFSString()));

        using var translation = new TBoldIDTranslationList();
        translation.AddTranslation(Def(3), Def(30));
        _ = InSpace<TBFSObjectIdListref>(vs, BoldDefaultStreamNames.BoldContentName_ObjectIdListRef);
        _ = InSpace<TBFSObjectIdListrefPair>(vs, BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair);
        vs.ApplyTranslationList(translation);
        Assert.That(list2.IdList(1).AsString, Is.EqualTo("30"));

        var pair = new TBFSObjectIdListrefPair();
        Assert.That(pair.Count, Is.EqualTo(0));
        using (TBoldObjectIdList objects = Ids(1, 3), links = Ids(11, 13))
        {
            pair.SetFromIdLists(objects, links);
        }

        IBoldFreeStandingIdListPair editablePair = pair;
        editablePair.AddIds(Def(5), Def(15));
        editablePair.RemoveId(Def(1));
        editablePair.RemoveId(Def(99));
        Assert.That(pair.Count, Is.EqualTo(2));
        Assert.That(pair.IdList1(0).AsString, Is.EqualTo("3"));
        Assert.That(pair.IdList2(0).AsString, Is.EqualTo("13"), "the link list follows the object list");

        TBFSObjectIdListrefPair pair2 = InSpace<TBFSObjectIdListrefPair>(vs, BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair);
        pair2.AssignContent(pair);
        vs.ApplyTranslationList(translation);
        Assert.That(pair2.GetIdList1(0).AsString, Is.EqualTo("30"));
        Assert.That(pair2.GetIdList2(1).AsString, Is.EqualTo("15"));
        Assert.That(pair2.GetCount(), Is.EqualTo(2));
        Assert.Throws<EBold>(() => pair2.AssignContent(list));
        pair.Dispose();
        list.Dispose();
    }

    [Test]
    [Covers(typeof(TBoldFreeStandingObjectContents), "*")]
    [Covers("BoldFreeStandingValues", "PMemberList", "TMemberList")]
    public void Object_contents_grow_their_members_on_demand()
    {
        using var contents = new TBoldFreeStandingObjectContents();
        Assert.That(contents.BoldExistenceState, Is.EqualTo(TBoldExistenceState.besExisting));
        Assert.That(contents.GetTimeStamp(), Is.EqualTo(-1));
        Assert.That(contents.GetIsModified(), Is.True, "always true, as in Delphi");
        Assert.That(contents.MemberCount, Is.EqualTo(0));
        Assert.That(contents.ValueByIndex(0), Is.Null);
        Assert.That(contents.GetStreamName(), Is.EqualTo(BoldDefaultStreamNames.BOLDOBJECTCONTENTSNAME));

        contents.EnsureMemberByIndex(2, "Integer");
        Assert.That(contents.MemberCount, Is.EqualTo(3));
        Assert.That(contents.FSValueByIndex(0), Is.Null, "lower members stay empty");
        TBoldFreeStandingValue integer = contents.FSValueByIndex(2)!;
        Assert.That(integer, Is.InstanceOf<TBFSInteger>());
        contents.EnsureMember(new TBoldMemberID(2), "String");
        Assert.That(contents.GetFSValueByIndex(2), Is.SameAs(integer), "an existing member is kept");
        contents.EnsureMember(new TBoldMemberID(0), "String");
        Assert.That(contents.GetValueByMemberId(new TBoldMemberID(0)), Is.InstanceOf<TBFSString>());

        contents.MarkAllMembersCurrent();
        Assert.That(integer.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsCurrent));
        contents.RemoveMemberByIndex(2);
        Assert.That(contents.ValueByIndex(2), Is.Null);
        contents.RemoveMemberByIndex(10);
        Assert.That(contents.MemberCount, Is.EqualTo(3), "the count does not shrink");

        IBoldObjectContents i = contents;
        i.GlobalId = "g";
        i.IsReadOnly = true;
        i.TimeStamp = 5;
        i.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
        Assert.That(contents.GetGlobalId(), Is.EqualTo("g"));
        Assert.That(contents.GetIsReadOnly(), Is.True);
        Assert.That(contents.GetTimeStamp(), Is.EqualTo(5));
        Assert.That(contents.GetBoldMemberCount(), Is.EqualTo(3));
        Assert.That(contents.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsModified));
    }

    [Test]
    [Covers(typeof(TBoldFreeStandingObjectContents), nameof(TBoldFreeStandingObjectContents.ApplyObjectContents),
        nameof(TBoldFreeStandingObjectContents.UpdateObjectContentsFrom))]
    public void Applying_object_contents_copies_values_with_or_without_states()
    {
        using var source = new TBoldFreeStandingObjectContents();
        source.EnsureMemberByIndex(1, "String");
        var s = (TBFSString)source.FSValueByIndex(1)!;
        s.AsString = "x";
        s.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
        source.BoldExistenceState = TBoldExistenceState.besDeleted;
        source.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;

        using var keepState = new TBoldFreeStandingObjectContents();
        keepState.ApplyObjectContents(source, true, true);
        Assert.That(keepState.BoldExistenceState, Is.EqualTo(TBoldExistenceState.besDeleted));
        Assert.That(keepState.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsCurrent));
        Assert.That(((TBFSString)keepState.FSValueByIndex(1)!).AsString, Is.EqualTo("x"));
        Assert.That(keepState.FSValueByIndex(1)!.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsInvalid),
            "with IgnorePersistenceState the value keeps its own state");

        using var withState = new TBoldFreeStandingObjectContents();
        withState.ApplyObjectContents(source, true, false);
        Assert.That(withState.FSValueByIndex(1)!.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsModified));

        using var statesOnly = new TBoldFreeStandingObjectContents();
        statesOnly.ApplyObjectContents(source, false, false);
        Assert.That(statesOnly.MemberCount, Is.EqualTo(0));

        s.AsString = "y";
        withState.EnsureMemberByIndex(3, "Integer");
        withState.UpdateObjectContentsFrom(source);
        Assert.That(((TBFSString)withState.FSValueByIndex(1)!).AsString, Is.EqualTo("y"));
        Assert.That(withState.MemberCount, Is.EqualTo(4), "only members this object has are updated");
    }

    [Test]
    [Covers(typeof(TBoldFreeStandingValueSpace), "*")]
    [Covers(typeof(TBoldFSObjectContentList), "*")]
    [Covers("BoldFreeStandingValues", "TBoldFSObjectContentHashIndex", "TBoldFSObjectContentHashIndex.ItemAsBoldObjectId",
        "TBoldFSObjectContentHashIndex.HashItem", "TBoldFSObjectContentHashIndex.Match", "TBoldFSObjectContentHashIndex.Hash",
        "TBoldFSObjectContentHashIndex.FindById")]
    public void Value_space_keeps_contents_by_id()
    {
        using var vs = new TBoldFreeStandingValueSpace();
        TBoldFreeStandingObjectContents a = vs.GetEnsuredFSObjectContentsByObjectId(Def(1));
        Assert.That(vs.GetEnsuredFSObjectContentsByObjectId(Def(1)), Is.SameAs(a));
        Assert.That(a.GetObjectId().IsEqual(Def(1)), Is.True);
        Assert.That(vs.GetHasContentsForId(Def(1)), Is.True);
        Assert.That(vs.GetObjectContentsByObjectId(Def(2)), Is.Null);
        vs.EnsureObjectId(Def(2));
        vs.EnsureObjectId(Def(2));
        vs.EnsureObjectContents(Def(3));
        Assert.That(vs.GetEnsuredObjectContentsByObjectId(Def(3)), Is.Not.Null);

        using (var all = new TBoldObjectIdList())
        {
            vs.AllObjectIds(all, false);
            Assert.That(all.CommaSeparatedIdList(), Is.EqualTo("1, 2, 3"));
        }

        using (var loaded = new TBoldObjectIdList())
        {
            vs.AllObjectIds(loaded, true);
            Assert.That(loaded.CommaSeparatedIdList(), Is.EqualTo("1, 3"), "2 has an id but no contents");
        }

        a.EnsureMemberByIndex(0, "Integer");
        ((TBFSInteger)a.FSValueByIndex(0)!).AsInteger = 5;
        Assert.That(((IBoldIntegerContent)vs.GetValueForIdAndMemberIndex(Def(1), 0)!).asInteger, Is.EqualTo(5));
        Assert.That(vs.GetValueForIdAndMemberIndex(Def(2), 0), Is.Null);

        vs.MarkAllObjectsAndMembersCurrent();
        Assert.That(a.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsCurrent));
        Assert.That(a.FSValueByIndex(0)!.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsCurrent));

        // Many objects, so the hash index is used for lookups.
        for (int i = 10; i < 40; i++)
        {
            vs.EnsureObjectContents(Def(i));
        }

        Assert.That(vs.GetFSObjectContentsByObjectId(Def(25))!.GetObjectId().AsString, Is.EqualTo("25"));

        vs.GetFSObjectContentsByObjectId(Def(25))!.BoldExistenceState = TBoldExistenceState.besDeleted;
        vs.RemoveDeletedObjects();
        Assert.That(vs.GetFSObjectContentsByObjectId(Def(25)), Is.Null);
        using (var all = new TBoldObjectIdList())
        {
            vs.AllObjectIds(all, false);
            Assert.That(all.IdInList(Def(25)), Is.False, "the id goes too");
        }

        vs.RemoveFSObjectContentsByObjectId(Def(3));
        Assert.That(vs.GetHasContentsForId(Def(3)), Is.False);
        vs.ExactifyIDs(new TBoldIDTranslationList());

        vs.RemoveAllObjectContents();
        using (var loaded = new TBoldObjectIdList())
        {
            vs.AllObjectIds(loaded, true);
            Assert.That(loaded.Count, Is.EqualTo(0));
        }
    }

    [Test]
    [Covers(typeof(TBoldFreeStandingValueSpace), nameof(TBoldFreeStandingValueSpace.ApplyTranslationList))]
    public void Translating_ids_moves_objects_and_their_references()
    {
        using var vs = new TBoldFreeStandingValueSpace();
        var newObject = new TBoldInternalObjectId(1, true);
        TBoldFreeStandingObjectContents created = vs.GetEnsuredFSObjectContentsByObjectId(newObject);
        TBoldFreeStandingObjectContents other = vs.GetEnsuredFSObjectContentsByObjectId(Def(7));
        other.EnsureMemberByIndex(0, BoldDefaultStreamNames.BoldContentName_ObjectIdRef);
        ((TBFSObjectIDRef)other.FSValueByIndex(0)!).SetFromId(newObject);

        using var translation = new TBoldIDTranslationList();
        translation.AddTranslation(newObject, Def(100));
        ((IBoldValueSpace)vs).ApplytranslationList(translation);

        Assert.That(vs.GetFSObjectContentsByObjectId(newObject), Is.Null);
        Assert.That(vs.GetFSObjectContentsByObjectId(Def(100)), Is.SameAs(created), "the same contents under the new id");
        Assert.That(created.GetObjectId().AsString, Is.EqualTo("100"));
        Assert.That(((TBFSObjectIDRef)other.FSValueByIndex(0)!).Id!.AsString, Is.EqualTo("100"), "references follow");
        using var all = new TBoldObjectIdList();
        vs.AllObjectIds(all, false);
        Assert.That(all.CommaSeparatedIdList(), Is.EqualTo("100, 7"), "the id is replaced in place");
    }

    [Test]
    [Covers(typeof(TBoldFreeStandingValueSpace), nameof(TBoldFreeStandingValueSpace.ApplyValueSpace),
        nameof(TBoldFreeStandingValueSpace.UpdateOwnValuesFrom))]
    public void Applying_a_value_space_copies_loaded_objects()
    {
        using var source = new TBoldFreeStandingValueSpace();
        TBoldFreeStandingObjectContents a = source.GetEnsuredFSObjectContentsByObjectId(Def(1));
        a.EnsureMemberByIndex(0, "String");
        ((TBFSString)a.FSValueByIndex(0)!).AsString = "one";
        source.EnsureObjectId(Def(2));

        using var dest = new TBoldFreeStandingValueSpace();
        dest.ApplyValueSpace(source, true);
        Assert.That(((TBFSString)dest.GetFSObjectContentsByObjectId(Def(1))!.FSValueByIndex(0)!).AsString, Is.EqualTo("one"));
        Assert.That(dest.GetHasContentsForId(Def(2)), Is.False);

        ((TBFSString)a.FSValueByIndex(0)!).AsString = "uno";
        source.GetEnsuredFSObjectContentsByObjectId(Def(3));
        dest.UpdateOwnValuesFrom(source);
        Assert.That(((TBFSString)dest.GetFSObjectContentsByObjectId(Def(1))!.FSValueByIndex(0)!).AsString, Is.EqualTo("uno"));
        Assert.That(dest.GetHasContentsForId(Def(3)), Is.False, "only objects it has");
    }
}
