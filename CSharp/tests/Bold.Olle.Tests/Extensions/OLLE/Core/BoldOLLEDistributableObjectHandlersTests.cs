// Delphi: Source/Extensions/OLLE/Core/BoldOLLEDistributableObjectHandlers.pas
// Port map: CSharp/port/units/BoldOLLEDistributableObjectHandlers.md
using BuildingClasses;
using Bold.Olle;

namespace Bold.Tests;

/// <summary>
/// Objects lent between two storages, A (their owner) and B (the holder): A gives them, B takes them in, changes them
/// and checks them in to A again.
/// </summary>
[NonParallelizable]
public class BoldOLLEDistributableObjectHandlersTests
{
    private OlleStorage A = null!;
    private OlleStorage B = null!;

    [SetUp]
    public void SetUp()
    {
        A = new OlleStorage("A");
        B = new OlleStorage("B");
    }

    [TearDown]
    public void TearDown()
    {
        B.Dispose();
        A.Dispose();
    }

    /// <summary>A gives the persons to B, to hold or not; B takes them in. The ids come back from A global, and from B local to B.</summary>
    private (TBoldObjectIdList GlobalIds, TBoldObjectIdList HeldGlobalIds) Lend(TBoldObject[] objects, TBoldObject[] held)
    {
        TBoldObjectIdList ids = OlleStorage.Ids(objects);
        TBoldObjectIdList holdIds = OlleStorage.Ids(held);
        using var valueSpace = new TBoldFreeStandingValueSpace();
        A.Own.Get(ids, holdIds, B.Name, valueSpace);
        TBoldObjectIdList globalIds = ids.Clone(), heldGlobalIds = holdIds.Clone();
        B.Foreign.Put(valueSpace, ids, holdIds, A.Name);
        return (globalIds, heldGlobalIds);
    }

    [Test]
    [Covers(typeof(TBoldOwnObjectHandler), nameof(TBoldOwnObjectHandler.Get), "InternalGet", "GetObjects", "ReserveObjects")]
    [Covers(typeof(TBoldForeignObjectHandler), nameof(TBoldForeignObjectHandler.Put), "PutObjects")]
    [Covers(typeof(TBoldDistributableObjectHandler), "Fetch", "MakeGlobalTranslationListFor", "MakeLocalizingTranslationList", "ExtractAllIds", "Update",
        "LockAndFreeObjects", "VerifyAssociations", "GetInfoObjectsFor", "NewOwnInfoObjectsFor", "NewForeignInfoObjectsFor", "GetForeignPSInfo", "TheMapping",
        "AddToMapping", "LookupInfoByLocalId", "SearchByOcl", "StartTransaction", "CommitTransaction")]
    public void Lent_objects_are_copied_with_their_global_ids_and_held_by_the_holder()
    {
        TPerson ann = A.NewPerson("Ann", "Archer");
        TPerson bob = A.NewPerson("Bob", "Baker");
        string annGlobalId = A.GlobalIdInDatabase(OlleStorage.Id(ann));

        (TBoldObjectIdList globalIds, _) = Lend([ann, bob], [ann]);

        Assert.That(globalIds[0], Is.InstanceOf<TBoldGlobalId>(), "A gives global ids");
        Assert.That(A.IsReadOnlyInDatabase(OlleStorage.Id(ann)), Is.True, "what B holds, A cannot change");
        Assert.That(A.IsReadOnlyInDatabase(OlleStorage.Id(bob)), Is.False);
        TPerson[] inB = B.Persons();
        TPerson annInB = inB.Single(p => p.FirstName == "Ann");
        TPerson bobInB = inB.Single(p => p.FirstName == "Bob");
        Assert.That((annInB.LastName, bobInB.LastName), Is.EqualTo(("Archer", "Baker")));
        Assert.That(B.GlobalIdInDatabase(OlleStorage.Id(annInB)), Is.EqualTo(annGlobalId), "the copy has the global id of the original");
        Assert.That(B.IsReadOnlyInDatabase(OlleStorage.Id(annInB)), Is.False, "B may change what it holds");
        Assert.That(B.IsReadOnlyInDatabase(OlleStorage.Id(bobInB)), Is.True, "and only that");

        TOwnObjectInfo[] own = A.OLLEObjects<TOwnObjectInfo>("OwnObjectInfo");
        Assert.That(own.Select(o => o.LocalId), Is.EquivalentTo(new[] { ((TBoldDefaultID)OlleStorage.Id(ann)).AsInteger, ((TBoldDefaultID)OlleStorage.Id(bob)).AsInteger }));
        TOwnObjectInfo annInfo = own.Single(o => o.LocalId == ((TBoldDefaultID)OlleStorage.Id(ann)).AsInteger);
        Assert.That(annInfo.CheckedOutObjectInfo?.Holder?.GlobalID, Is.EqualTo("B"), "Ann is checked out to B");
        Assert.That(own.Single(o => o != annInfo).CheckedOutObjectInfo, Is.Null);

        TForeignObjectInfo[] foreign = B.OLLEObjects<TForeignObjectInfo>("ForeignObjectInfo");
        Assert.That(foreign.Select(f => f.Owner?.GlobalID).Distinct(), Is.EqualTo(new[] { "A" }));
        TForeignObjectInfo annForeign = foreign.Single(f => f.LocalId == ((TBoldDefaultID)OlleStorage.Id(annInB)).AsInteger);
        Assert.That(annForeign.HeldObjectInfo, Is.Not.Null, "B holds Ann");
        Assert.That(foreign.Single(f => f != annForeign).HeldObjectInfo, Is.Null);
        Assert.That(annForeign.ForeignTimeStamp, Is.GreaterThan(0), "the version A gave");

        var held = new TBoldObjectIdList();
        B.Foreign.HeldObjectsFrom(A.Name, held);
        Assert.That(OlleStorage.Strings(held), Is.EqualTo(new[] { OlleStorage.Id(annInB).AsString }));
        var all = new TBoldObjectIdList();
        B.Foreign.ObjectsFrom(A.Name, all);
        Assert.That(OlleStorage.Strings(all), Is.EquivalentTo(new[] { OlleStorage.Id(annInB).AsString, OlleStorage.Id(bobInB).AsString }));
    }

    [Test]
    [Covers(typeof(TBoldForeignObjectHandler), nameof(TBoldForeignObjectHandler.StartCheckIn), "StartCheckInObjects", "ReleaseObjects",
        nameof(TBoldForeignObjectHandler.AcknowledgeCheckIn), nameof(TBoldForeignObjectHandler.ModifiedObjectsFrom))]
    [Covers(typeof(TBoldOwnObjectHandler), nameof(TBoldOwnObjectHandler.CheckIn), "CheckInObjects")]
    public void The_holder_checks_its_changes_in_to_the_owner()
    {
        TPerson ann = A.NewPerson("Ann", "Archer");
        TBoldObjectId annInA = OlleStorage.Id(ann);
        Lend([ann], [ann]);

        TPerson annInB = B.Person("Ann");
        annInB.LastName = "Abbot";
        B.System.UpdateDatabase();
        var modified = new TBoldObjectIdList();
        B.Foreign.ModifiedObjectsFrom(A.Name, modified);
        Assert.That(OlleStorage.Strings(modified), Is.EqualTo(new[] { OlleStorage.Id(annInB).AsString }), "B changed Ann since it came");

        TBoldObjectIdList ids = OlleStorage.Ids(annInB);
        TBoldObjectIdList release = OlleStorage.Ids(annInB);
        using var valueSpace = new TBoldFreeStandingValueSpace();
        B.Foreign.StartCheckIn(ids, release, valueSpace, A.Name);
        Assert.That(B.IsReadOnlyInDatabase(OlleStorage.Id(annInB)), Is.True, "released: B no longer changes Ann");
        Assert.That(B.OLLEObjects("CheckingInObjectInfo").Length, Is.EqualTo(1));

        int newTimeStamp = 0;
        A.Own.CheckIn(valueSpace, ids, release, B.Name, ref newTimeStamp);
        Assert.That(newTimeStamp, Is.GreaterThan(0));
        Assert.That(A.Person("Ann").LastName, Is.EqualTo("Abbot"), "A has B's change");
        Assert.That(A.IsReadOnlyInDatabase(annInA), Is.False, "A may change Ann again");
        Assert.That(A.OLLEObjects("CheckedOutObjectInfo").Length, Is.Zero, "Ann is no longer checked out");

        B.Foreign.AcknowledgeCheckIn(A.Name, newTimeStamp);
        Assert.That(B.OLLEObjects("CheckingInObjectInfo").Length, Is.Zero);
        Assert.That(B.OLLEObjects("HeldObjectInfo").Length, Is.Zero, "B holds Ann no longer");
        TForeignObjectInfo annForeign = B.OLLEObjects<TForeignObjectInfo>("ForeignObjectInfo").Single();
        Assert.That(annForeign.ForeignTimeStamp, Is.EqualTo(newTimeStamp), "B knows the version A has now");
    }
    [Test]
    [Covers(typeof(TBoldOwnObjectHandler), nameof(TBoldOwnObjectHandler.UnCheckOut), "UnCheckOutObjects")]
    public void The_holder_gives_objects_back_unchanged()
    {
        TPerson ann = A.NewPerson("Ann", "Archer");
        (TBoldObjectIdList globalIds, _) = Lend([ann], [ann]);
        A.Own.UnCheckOut(globalIds, B.Name);
        Assert.That(A.IsReadOnlyInDatabase(OlleStorage.Id(ann)), Is.False, "A may change Ann again");
        Assert.That(A.OLLEObjects("CheckedOutObjectInfo"), Has.Length.EqualTo(1), "as in Delphi, the check-out stays on record (TOwnObjectInfo.UnCheckOut does nothing)");
    }

    [Test]
    [Covers(typeof(TBoldForeignObjectHandler), nameof(TBoldForeignObjectHandler.FailCheckIn), "UnReleaseObjects")]
    [Covers(typeof(TBoldDistributableObjectHandler), "RollbackTransaction")]
    [Covers(typeof(OlleConsts), nameof(OlleConsts.sObjectsAlreadyBeingCheckedIn))]
    public void A_failed_check_in_gives_the_holder_its_objects_back()
    {
        TPerson ann = A.NewPerson("Ann", "Archer");
        TPerson bob = A.NewPerson("Bob", "Baker");
        Lend([ann, bob], [ann, bob]);
        TPerson[] inB = B.Persons();
        TPerson annInB = inB.Single(p => p.FirstName == "Ann"), bobInB = inB.Single(p => p.FirstName == "Bob");
        using var valueSpace = new TBoldFreeStandingValueSpace();
        B.Foreign.StartCheckIn(OlleStorage.Ids(annInB, bobInB), OlleStorage.Ids(annInB), valueSpace, A.Name);
        Assert.That(B.IsReadOnlyInDatabase(OlleStorage.Id(annInB)), Is.True, "Ann is released");
        using var again = new TBoldFreeStandingValueSpace();
        Assert.That(() => B.Foreign.StartCheckIn(OlleStorage.Ids(bobInB), OlleStorage.Ids(bobInB), again, A.Name),
            Throws.InstanceOf<EBold>().With.Message.EqualTo("TBoldForeignObjectHandler.StartCheckIn: Already checking in objects for this persistent storage"));

        B.Foreign.FailCheckIn(A.Name);
        Assert.That(B.IsReadOnlyInDatabase(OlleStorage.Id(annInB)), Is.False, "B may change Ann again");
        Assert.That(B.IsReadOnlyInDatabase(OlleStorage.Id(bobInB)), Is.False);
        Assert.That(B.OLLEObjects("CheckingInObjectInfo"), Is.Empty);
        Assert.That(B.OLLEObjects("HeldObjectInfo"), Has.Length.EqualTo(2), "B still holds both");
    }

    [Test]
    [Covers(typeof(TForeignObjectInfo), nameof(TForeignObjectInfo.InitializeHolding))]
    public void An_object_the_holder_made_is_checked_in_to_the_owner_as_its_own()
    {
        TPerson ann = A.NewPerson("Ann", "Archer");
        Lend([ann], [ann]);
        TPerson cid = B.NewPerson("Cid", "Cooper");
        int cidInB = ((TBoldDefaultID)OlleStorage.Id(cid)).AsInteger;
        TBoldObjectIdList ids = OlleStorage.Ids(cid), release = OlleStorage.Ids(cid);
        using var valueSpace = new TBoldFreeStandingValueSpace();
        B.Foreign.StartCheckIn(ids, release, valueSpace, A.Name);
        int newTimeStamp = 0;
        A.Own.CheckIn(valueSpace, ids, release, B.Name, ref newTimeStamp);
        B.Foreign.AcknowledgeCheckIn(A.Name, newTimeStamp);

        TPerson cidInA = A.Person("Cid");
        Assert.That(cidInA.LastName, Is.EqualTo("Cooper"));
        Assert.That(A.GlobalIdInDatabase(OlleStorage.Id(cidInA)), Is.EqualTo(B.GlobalIdInDatabase(new TBoldDefaultID { AsInteger = cidInB })));
        TOwnObjectInfo cidInfo = A.OLLEObjects<TOwnObjectInfo>("OwnObjectInfo").Single(o => o.LocalId == ((TBoldDefaultID)OlleStorage.Id(cidInA)).AsInteger);
        Assert.That(cidInfo.CheckedOutObjectInfo, Is.Null, "released by B");
        TForeignObjectInfo cidForeign = B.OLLEObjects<TForeignObjectInfo>("ForeignObjectInfo").Single(f => f.LocalId == cidInB);
        Assert.That((cidForeign.Owner?.GlobalID, cidForeign.HeldObjectInfo, cidForeign.ForeignTimeStamp), Is.EqualTo(("A", (THeldObjectInfo?)null, newTimeStamp)),
            "Cid is A's object now");
    }

    [Test]
    [Covers(typeof(TBoldOwnObjectHandler), nameof(TBoldOwnObjectHandler.GetSynch), nameof(TBoldOwnObjectHandler.AcknowledgeSynch), nameof(TBoldOwnObjectHandler.FailSynch))]
    [Covers(typeof(TBoldDistributableObjectHandler), "GetLocalIdsFor")]
    [Covers(typeof(OlleConsts), nameof(OlleConsts.sSynchInProgress))]
    public void A_synch_gives_the_own_objects_changed_since_the_last_one()
    {
        TPerson ann = A.NewPerson("Ann", "Archer");
        TPerson bob = A.NewPerson("Bob", "Baker");
        string[] Synch()
        {
            var ids = new TBoldObjectIdList();
            using var valueSpace = new TBoldFreeStandingValueSpace();
            A.Own.GetSynch(B.Name, ids, valueSpace);
            Assert.That(Enumerable.Range(0, ids.Count).All(i => ids[i] is TBoldGlobalId), Is.True, "with global ids");
            return OlleStorage.Strings(ids);
        }

        string[] first = Synch();
        Assert.That(first, Has.Length.EqualTo(2), "the first synch gives every object");
        TForeignPSInfo b = A.OLLEObjects<TForeignPSInfo>("ForeignPSInfo").Single();
        Assert.That(b.IsSynching(), Is.True);
        using (var valueSpace = new TBoldFreeStandingValueSpace())
        {
            Assert.That(() => A.Own.GetSynch(B.Name, new TBoldObjectIdList(), valueSpace),
                Throws.InstanceOf<EBold>().With.Message.EqualTo("TBoldOwnObjectHandler.GetSynch: There is already an ongoing synch that must either be acknowledged or failed."));
        }

        int ongoing = b.OngoingSynchTimestamp;
        A.Own.AcknowledgeSynch(B.Name);
        Assert.That((b.LastSynchTimestamp, b.IsSynching()), Is.EqualTo((ongoing, false)));
        Assert.That(Synch(), Is.Empty, "nothing changed since");
        A.Own.FailSynch(B.Name);
        Assert.That(b.IsSynching(), Is.False);

        bob.LastName = "Brown";
        A.System.UpdateDatabase();
        string[] second = Synch();
        Assert.That(second, Has.Length.EqualTo(1), "only Bob changed since");
        Assert.That(first, Does.Contain(second[0]));
        A.Own.FailSynch(B.Name);
        Assert.That(Synch(), Is.EqualTo(second), "after a failed synch the next gives the same");
    }

    [Test]
    [Covers(typeof(TBoldBrokenLinkResolver), nameof(TBoldBrokenLinkResolver.ResolveBrokenLink), nameof(TBoldBrokenLinkResolver.HeldObjectAction),
        nameof(TBoldBrokenLinkResolver.NonheldObjectAction))]
    [Covers(typeof(TBoldDistributableObjectHandler), nameof(TBoldDistributableObjectHandler.BrokenLinkResolver))]
    [Covers(typeof(OlleConsts), nameof(OlleConsts.sUnresolvedLink), nameof(OlleConsts.sCannotFailIndividualObjects))]
    public void A_link_to_an_object_that_did_not_come_along_is_resolved_or_stops_the_lending()
    {
        TPerson ann = A.NewPerson("Ann", "Archer");
        ann.Home = new TResidential_Building(A.System) { Address = "1 Main Street" };
        A.System.UpdateDatabase();
        TBoldObjectIdList ids = OlleStorage.Ids(ann), hold = OlleStorage.Ids(ann);
        using var valueSpace = new TBoldFreeStandingValueSpace();
        A.Own.Get(ids, hold, B.Name, valueSpace);

        Assert.That(() => B.Foreign.Put(valueSpace, ids, hold, A.Name), Throws.InstanceOf<EBold>().With.Message.EqualTo("Operation failed: Unresolved link"));
        B.Foreign.BrokenLinkResolver = new TBoldBrokenLinkResolver
        {
            HeldObjectAction = TBoldLinkResolveAction.blraFailObject,
            NonheldObjectAction = TBoldLinkResolveAction.blraAbort,
        };
        Assert.That(() => B.Foreign.Put(valueSpace, ids, hold, A.Name),
            Throws.InstanceOf<EBoldFeatureNotImplementedYet>().With.Message.EqualTo("TBoldForeignObjectHandler.VerifyAssociations: Failing inidividual objects not implemented"));
        Assert.That(B.Persons(), Is.Empty, "nothing was taken in");

        B.Foreign.BrokenLinkResolver.HeldObjectAction = TBoldLinkResolveAction.blraCut;
        B.Foreign.Put(valueSpace, ids, hold, A.Name);
        TPerson annInB = B.Person("Ann");
        Assert.That((annInB.LastName, annInB.Home), Is.EqualTo(("Archer", (TResidential_Building?)null)), "Ann came without her home");
    }

    [Test]
    [Covers(typeof(OlleConsts), nameof(OlleConsts.sWrongOwner), nameof(OlleConsts.sObjectNotForeign), nameof(OlleConsts.sObjectNotOwned))]
    public void Objects_are_taken_in_from_their_owner_and_checked_in_by_their_holder_only()
    {
        TPerson ann = A.NewPerson("Ann", "Archer");
        (TBoldObjectIdList globalIds, _) = Lend([ann], []);

        TBoldObjectIdList ids = OlleStorage.Ids(ann), none = new();
        using var valueSpace = new TBoldFreeStandingValueSpace();
        A.Own.Get(ids, none, B.Name, valueSpace);
        Assert.That(() => B.Foreign.Put(valueSpace, ids, none, "C"),
            Throws.InstanceOf<EBold>().With.Message.EqualTo("TBoldForeignObjectHandler.EnsureForeignInfo: Wrong owner"), "Ann is A's");

        TBoldObjectIdList ownIds = OlleStorage.Ids(ann);
        using var ownValueSpace = new TBoldFreeStandingValueSpace();
        A.Own.Get(ownIds, none, B.Name, ownValueSpace);
        Assert.That(() => A.Foreign.Put(ownValueSpace, ownIds, none, B.Name),
            Throws.InstanceOf<EBold>().With.Message.EqualTo("TBoldForeignObjectHandler.EnsureForeignInfo: Object is not a foreign object"), "A owns Ann");

        Assert.That(() => B.Own.UnCheckOut(globalIds.Clone(), A.Name),
            Throws.InstanceOf<EBold>().With.Message.EqualTo("TBoldOwnObjectHandler.UnCheckOutObjects: Object is not an owned object"), "B does not own Ann");
    }
}
