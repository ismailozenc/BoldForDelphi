// Delphi: Source/Extensions/OLLE/Core/DistributableInfo.pas
// Port map: CSharp/port/units/DistributableInfo.md
using Bold.Olle;

namespace Bold.Tests;

/// <summary>The OLLE information objects on their own: what they allow, and what they record, in the OLLE system of a storage.</summary>
[NonParallelizable]
public class DistributableInfoTests
{
    private OlleStorage fStorage = null!;

    [SetUp]
    public void SetUp() => fStorage = new OlleStorage("S");

    [TearDown]
    public void TearDown() => fStorage.Dispose();

    private TBoldSystem Olle => fStorage.OLLESystem;

    /// <summary>The contents of the object of the local id in the value space, at the timestamp.</summary>
    private static IBoldObjectContents Contents(TBoldFreeStandingValueSpace valueSpace, int localId, int timeStamp)
    {
        IBoldObjectContents contents = ((IBoldValueSpace)valueSpace).EnsuredObjectContentsByObjectId(new TBoldDefaultID { AsInteger = localId });
        contents.TimeStamp = timeStamp;
        return contents;
    }

    [Test]
    [Covers("DistributableInfo", "InstallBusinessClasses", "InstallObjectListClasses", "GeneratedCodeCRC", "initialization")]
    [Covers(typeof(TPSInfo), nameof(TPSInfo.GlobalID))]
    public void The_OLLE_system_has_the_generated_classes()
    {
        var b = new TForeignPSInfo(Olle) { GlobalID = "B" };
        Assert.That(Olle.ClassByExpressionName("ForeignPSInfo"), Is.InstanceOf<TForeignPSInfoList>());
        Assert.That(Olle.ClassByExpressionName("PSInfo")[0], Is.SameAs(b));
        Assert.That(Olle.ClassByExpressionName("OwnObjectInfo"), Is.InstanceOf<TOwnObjectInfoList>());
        Assert.That(b.M_GlobalID.AsString, Is.EqualTo("B"));
    }

    [Test]
    [Covers(typeof(TMapping), nameof(TMapping.M_ObjectInfo), nameof(TMapping.Q_ObjectInfo))]
    [Covers(typeof(TDistributableObjectInfo), nameof(TDistributableObjectInfo.LocalId), nameof(TDistributableObjectInfo.mapping))]
    public void The_mapping_finds_an_information_object_by_its_local_id()
    {
        var mapping = new TMapping(Olle);
        var info = new TOwnObjectInfo(Olle) { LocalId = 7 };
        mapping.ObjectInfo.Add(info);
        Assert.That(mapping.Q_ObjectInfo(7), Is.SameAs(info));
        Assert.That(mapping.Q_ObjectInfo(8), Is.Null);
        Assert.That(info.mapping, Is.SameAs(mapping));
    }

    [Test]
    [Covers(typeof(TOwnObjectInfo), nameof(TOwnObjectInfo.Get), nameof(TOwnObjectInfo.CheckOut), nameof(TOwnObjectInfo.CheckIn), nameof(TOwnObjectInfo.UnCheckOut),
        nameof(TOwnObjectInfo.StartCheckIn), nameof(TOwnObjectInfo.CheckedOutObjectInfo))]
    [Covers(typeof(TCheckedOutObjectInfo), nameof(TCheckedOutObjectInfo.Holder))]
    public void An_own_object_is_checked_out_to_one_holder_at_a_time()
    {
        var b = new TForeignPSInfo(Olle) { GlobalID = "B" };
        var c = new TForeignPSInfo(Olle) { GlobalID = "C" };
        var info = new TOwnObjectInfo(Olle) { LocalId = 7 };
        using var valueSpace = new TBoldFreeStandingValueSpace();
        info.Get(valueSpace, false, b);
        Assert.That(info.CheckedOutObjectInfo, Is.Null, "given, not checked out");
        info.Get(valueSpace, true, b);
        Assert.That(info.CheckedOutObjectInfo?.Holder, Is.SameAs(b));
        Assert.That(() => info.CheckOut(c), Throws.InstanceOf<EBold>().With.Message.EqualTo("TOwnObjectInfo.CheckOut: Object is already checked out"));
        Assert.That(() => info.CheckIn(valueSpace, true, c), Throws.InstanceOf<EBold>().With.Message.EqualTo("TOwnObjectInfo.CheckIn: Object checked out to another holder"));

        info.UnCheckOut(b);
        Assert.That(info.CheckedOutObjectInfo, Is.Not.Null, "UnCheckOut changes nothing, as in Delphi");
        info.CheckIn(valueSpace, false, b);
        Assert.That(info.CheckedOutObjectInfo, Is.Not.Null, "checked in, and still checked out");
        info.CheckIn(valueSpace, true, b);
        Assert.That(info.CheckedOutObjectInfo, Is.Null, "released");
        Assert.That(() => info.CheckIn(valueSpace, true, b), Throws.InstanceOf<EBold>().With.Message.EqualTo("TOwnObjectInfo.CheckIn: Object not checked out"));
        Assert.That(() => info.StartCheckIn(valueSpace, true), Throws.InstanceOf<EBold>().With.Message.EqualTo("TOwnObjectInfo.StartCheckIn: Object is own object"));
    }

    [Test]
    [Covers(typeof(TForeignObjectInfo), nameof(TForeignObjectInfo.Put), nameof(TForeignObjectInfo.Hold), nameof(TForeignObjectInfo.Get),
        nameof(TForeignObjectInfo.StartCheckIn), nameof(TForeignObjectInfo.ForeignTimeStamp), nameof(TForeignObjectInfo.Owner), nameof(TForeignObjectInfo.HeldObjectInfo))]
    [Covers(typeof(THeldObjectInfo), nameof(THeldObjectInfo.StartCheckIn), nameof(THeldObjectInfo.OriginalLocalTimeStamp), nameof(THeldObjectInfo.ForeignObjectInfo),
        nameof(THeldObjectInfo.CheckingInObjectInfo))]
    [Covers(typeof(TForeignPSInfo), nameof(TForeignPSInfo.OwnedObjectInfos))]
    public void A_foreign_object_is_held_and_checked_in_as_its_owner_lent_it()
    {
        var a = new TForeignPSInfo(Olle) { GlobalID = "A" };
        var info = new TForeignObjectInfo(Olle) { LocalId = 7, Owner = a };
        Assert.That(a.OwnedObjectInfos.Includes(info), Is.True);
        using var valueSpace = new TBoldFreeStandingValueSpace();
        IBoldObjectContents contents = Contents(valueSpace, 7, 12);
        info.Put(valueSpace, false, 30);
        Assert.That((info.ForeignTimeStamp, info.HeldObjectInfo), Is.EqualTo((12, (THeldObjectInfo?)null)), "the version A gave; not held");
        Assert.That(() => info.StartCheckIn(valueSpace, true), Throws.InstanceOf<EBold>().With.Message.EqualTo("TForeignObjectInfo.StartCheckIn: Object is not held"));
        contents.TimeStamp = 10;
        Assert.That(() => info.Put(valueSpace, true, 31),
            Throws.InstanceOf<EBold>().With.Message.EqualTo("TForeignObjectInfo.EnsureForeignInfo: New timestamp is older than current"));

        contents.TimeStamp = 14;
        info.Put(valueSpace, true, 31);
        THeldObjectInfo held = info.HeldObjectInfo!;
        Assert.That((info.ForeignTimeStamp, held.OriginalLocalTimeStamp, held.ForeignObjectInfo), Is.EqualTo((14, 31, info)));
        Assert.That(() => info.Get(valueSpace, false, a), Throws.InstanceOf<EBold>().With.Message.EqualTo("TForeignObjectInfo.Get: Object is a Foreign object"));

        contents.TimeStamp = 40; // changed here since
        info.StartCheckIn(valueSpace, true);
        TCheckingInObjectInfo checkingIn = held.CheckingInObjectInfo!;
        Assert.That((checkingIn.KeepHold, checkingIn.LocalTimeStampAtCheckIn, contents.TimeStamp), Is.EqualTo((true, 40, 14)),
            "the owner gets the version it lent");
        Assert.That(checkingIn.HeldObjectInfo, Is.SameAs(held));
        Assert.That(() => held.StartCheckIn(valueSpace, true), Throws.InstanceOf<EBold>().With.Message.EqualTo("THeldObjectInfo.StartCheckIn: Object already checking in"));
    }

    [Test]
    [Covers(typeof(TCheckingInObjectInfo), nameof(TCheckingInObjectInfo.AcknowledgeCheckIn), nameof(TCheckingInObjectInfo.FailCheckIn), nameof(TCheckingInObjectInfo.KeepHold),
        nameof(TCheckingInObjectInfo.LocalTimeStampAtCheckIn), nameof(TCheckingInObjectInfo.HeldObjectInfo))]
    [Covers(typeof(TForeignPSInfo), nameof(TForeignPSInfo.AcknowledgeCheckIn), nameof(TForeignPSInfo.FailCheckIn), nameof(TForeignPSInfo.IsCheckingIn))]
    [Covers(typeof(TForeignObjectInfo), nameof(TForeignObjectInfo.InitializeHolding))]
    public void A_check_in_is_acknowledged_or_fails()
    {
        var a = new TForeignPSInfo(Olle) { GlobalID = "A" };
        using var valueSpace = new TBoldFreeStandingValueSpace();
        TForeignObjectInfo Held(int localId)
        {
            var info = new TForeignObjectInfo(Olle);
            info.InitializeHolding(new TBoldDefaultID { AsInteger = localId }, a);
            Contents(valueSpace, localId, 5);
            info.Put(valueSpace, false, 20);
            return info;
        }

        TForeignObjectInfo kept = Held(1), released = Held(2);
        Assert.That((kept.LocalId, kept.Owner, kept.HeldObjectInfo?.OriginalLocalTimeStamp), Is.EqualTo((1, a, (int?)0)), "held from the start");
        void StartCheckIn()
        {
            Contents(valueSpace, 1, 9); // changed here
            Contents(valueSpace, 2, 9);
            kept.StartCheckIn(valueSpace, true);
            released.StartCheckIn(valueSpace, false);
        }

        Assert.That(a.IsCheckingIn(), Is.False);
        StartCheckIn();
        Assert.That(a.IsCheckingIn(), Is.True);
        Assert.That(() => kept.HeldObjectInfo!.CheckingInObjectInfo!.AcknowledgeCheckIn(4),
            Throws.InstanceOf<EBold>().With.Message.EqualTo("TCheckingInObjectInfo.AcknowledgeCheckIn: New timestamp is older than current"));

        var failed = new TBoldObjectIdList();
        a.FailCheckIn(failed);
        Assert.That(OlleStorage.Strings(failed), Is.EqualTo(new[] { "2" }), "the object that was to be released");
        Assert.That(a.IsCheckingIn(), Is.False);
        Assert.That(kept.HeldObjectInfo, Is.Not.Null, "a failed check-in keeps the holds");
        Assert.That(released.HeldObjectInfo, Is.Not.Null);

        StartCheckIn();
        a.AcknowledgeCheckIn(25);
        Assert.That((kept.ForeignTimeStamp, released.ForeignTimeStamp), Is.EqualTo((25, 25)), "the owner's version now");
        Assert.That(kept.HeldObjectInfo?.OriginalLocalTimeStamp, Is.EqualTo(9), "still held, as it was here at the check-in");
        Assert.That(released.HeldObjectInfo, Is.Null, "no longer held");
        Assert.That(a.IsCheckingIn(), Is.False);
        Assert.That(Olle.ClassByExpressionName("CheckingInObjectInfo").Count, Is.Zero);
    }

    [Test]
    [Covers(typeof(TForeignPSInfo), nameof(TForeignPSInfo.IsSynching), nameof(TForeignPSInfo.OngoingSynchTimestamp), nameof(TForeignPSInfo.LastSynchTimestamp))]
    public void A_storage_is_synching_while_a_synch_timestamp_is_set()
    {
        var b = new TForeignPSInfo(Olle) { GlobalID = "B" };
        Assert.That((b.OngoingSynchTimestamp, b.LastSynchTimestamp, b.IsSynching()), Is.EqualTo((-1, 0, false)), "the model's initial values");
        b.OngoingSynchTimestamp = 3;
        Assert.That(b.IsSynching(), Is.True);
    }
}
