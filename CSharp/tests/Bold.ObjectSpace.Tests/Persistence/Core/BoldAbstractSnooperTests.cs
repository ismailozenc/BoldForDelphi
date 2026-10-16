// Delphi: Source/Persistence/Core/BoldAbstractSnooper.pas
// Port map: CSharp/port/units/BoldAbstractSnooper.md
namespace Bold.Tests;

public class BoldAbstractSnooperTests
{
    /// <summary>Records what each transmit would send, then clears.</summary>
    private sealed class RecordingSnooper(TMoldModel model) : TBoldAbstractSnooper(model)
    {
        public readonly List<string> SentEvents = [];
        public readonly List<string> SentSubscriptions = [];
        public readonly List<string> SentCancellations = [];
        public bool Locked;
        public int LockCount;

        public override void TransmitEvents(TBoldClientID ClientID)
        {
            SentEvents.AddRange(Events);
            SentSubscriptions.AddRange(Subscriptions);
            SentCancellations.AddRange(CancelledSubscriptions);
            ClearEvents();
        }

        protected override void EnsureDataBaseLock(TBoldClientID ClientID)
        {
            Locked = true;
            LockCount++;
        }

        protected override void ReleaseDataBaseLock(TBoldClientID ClientID) => Locked = false;
    }

    private string fFile = "";

    [SetUp]
    public void SetUp() => fFile = Path.Combine(Path.GetTempPath(), $"bold-{Guid.NewGuid():N}.xml");

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(fFile))
        {
            File.Delete(fFile);
        }
    }

    private static string Encode(TBoldObjectSpaceSubscriptionType kind, string className, string member, TBoldObjectId? id) =>
        TBoldObjectSpaceExternalEvent.EncodeExternalEvent(kind, className, member, "", id);

    [Test]
    [Covers(typeof(TBoldAbstractSnooper), "*")]
    [Covers(typeof(TBoldPersistenceControllerPassthrough), "*")]
    public void The_snooper_collects_subscriptions_and_events_for_a_client()
    {
        using var s = new ObjectSpaceSample(controller: m => new TBoldPersistenceControllerFileXML(fFile, true, m));
        TBoldObject ann = s.NewPerson();
        TBoldObject house = s.NewBuilding();
        Assert.That(ann.BoldMemberByExpressionName("firstName"), Is.Not.Null);
        ((TBoldObjectReference)ann.BoldMemberByExpressionName("home")).BoldObject = house;
        s.System.UpdateDatabase();

        var snooper = new RecordingSnooper(s.Model) { NextPersistenceController = new TBoldPersistenceControllerFileXML(fFile, true, s.Model) };
        TBoldSystem client = s.NewSystem(snooper);
        IBoldValueSpace clientSpace = client.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepPMIn);
        int personIndex = s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex;
        int buildingIndex = s.SystemTypeInfo.ClassTypeInfoByExpressionName("Building")!.TopSortedIndex;

        // A class condition for a client subscribes to the class.
        var ids = new TBoldObjectIdList();
        snooper.PMFetchIDListWithCondition(ids, clientSpace, 0, new TBoldConditionWithClass { TopSortedIndex = buildingIndex }, 1);
        Assert.That(ids.Count, Is.EqualTo(1));
        Assert.That(snooper.SentSubscriptions, Is.EqualTo(new[] { Encode(TBoldObjectSpaceSubscriptionType.bsClassChanged, "Building", "", null) }));

        // Fetching a building subscribes to its embedded state; residents is delayed, so not fetched with it.
        snooper.SentSubscriptions.Clear();
        client.EnsuredLocatorByID(ids[0]);
        snooper.PMFetch(ids, clientSpace, null, 0, 1);
        Assert.That(snooper.SentSubscriptions, Is.EqualTo(new[] { Encode(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, "", "", ids[0]) }));

        // Fetching the residents subscribes to that non-embedded link only.
        snooper.SentSubscriptions.Clear();
        TMoldClass building = s.Model.Classes[buildingIndex];
        var members = new TBoldMemberIdList();
        members.Add(new TBoldMemberID(building.AllBoldMembers.IndexOf(building.AllBoldMembers.ItemsByName("residents")!)));
        snooper.PMFetch(ids, clientSpace, members, 0, 1);
        Assert.That(snooper.SentSubscriptions, Is.EqualTo(new[] { Encode(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, "", "residents", ids[0]) }));

        // Without a client nothing is collected.
        snooper.SentSubscriptions.Clear();
        snooper.PMFetch(ids, clientSpace, null, 0, BoldPersistenceController.NOTVALIDCLIENTID);
        Assert.That(snooper.SentSubscriptions, Is.Empty);

        // An update of a new person gives class events for the class and its superclasses, inside the database lock.
        TBoldObject bob = client.CreateNewObjectByExpressionName("Person");
        bob.BoldMemberByExpressionName("firstName").AsString = "Bob";
        var updateIds = new TBoldObjectIdList();
        updateIds.Add(bob.BoldObjectLocator!.BoldObjectID);
        var translations = new TBoldIDTranslationList();
        TBoldTimeStampType ts = 0;
        snooper.PMUpdate(updateIds, client.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepPMOut), null, null, translations, ref ts, 1);
        Assert.That(snooper.LockCount, Is.EqualTo(1));
        Assert.That(snooper.Locked, Is.False, "the lock is released");
        Assert.That(snooper.SentEvents, Does.Contain(Encode(TBoldObjectSpaceSubscriptionType.bsClassChanged, "Person", "", null)));
        Assert.That(snooper.SentEvents, Does.Contain(Encode(TBoldObjectSpaceSubscriptionType.bsClassChanged, s.Model.RootClass.ExpandedExpressionName, "", null)));
        Assert.That(bob.BoldObjectLocator.BoldObjectID.IsStorable, Is.True, "the new object got a stored id");
        Assert.That(snooper.SentSubscriptions, Does.Contain(Encode(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, "", "", bob.BoldObjectLocator.BoldObjectID)));
        Assert.That(snooper.MoldModel, Is.SameAs(s.Model));

        string? failure = null;
        snooper.OnPropagatorFailure = (sender, message) => failure = message;
        Assert.That(snooper.OnPropagatorFailure, Is.Not.Null);
        Assert.Catch<EBold>(() => new TBoldPersistenceControllerPassthrough().PMSetReadOnlyness(ids, ids), "no next controller");
        Assert.That(failure, Is.Null);
    }
}
