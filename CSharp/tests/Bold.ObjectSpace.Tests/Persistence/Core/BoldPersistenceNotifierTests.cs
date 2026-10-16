// Delphi: Source/Persistence/Core/BoldPersistenceNotifier.pas
// Port map: CSharp/port/units/BoldPersistenceNotifier.md
namespace Bold.Tests;

public class BoldPersistenceNotifierTests
{
    /// <summary>A handle whose controller is a file controller on the sample's model.</summary>
    private sealed class FileHandle(TMoldModel model, string file) : TBoldPersistenceHandle
    {
        protected override TBoldPersistenceController CreatePersistenceController() => new TBoldPersistenceControllerFileXML(file, true, model);
    }

    /// <summary>A pass-through handle chaining a pass-through controller to the next handle's.</summary>
    private sealed class ChainHandle : TBoldPersistenceHandlePassthroughWithModel
    {
        public int Inits;

        protected override TBoldPersistenceController CreatePersistenceController()
        {
            var controller = new TBoldPersistenceControllerPassthrough();
            ChainPersistenceController(controller);
            return controller;
        }

        protected override void InitNextPHandle(TBoldPersistenceHandle? NextPHandle) => Inits++;
    }

    [Test]
    [Covers(typeof(TBoldAbstractPersistenceNotifier), "*")]
    [Covers(typeof(TBoldPersistenceNotifier), "*")]
    [Covers(typeof(TBoldAlertExcessiveFetchEvent))]
    public void The_notifier_calls_handlers_for_persistence_events()
    {
        string file = Path.Combine(Path.GetTempPath(), $"bold-{Guid.NewGuid():N}.xml");
        try
        {
            using var s = new ObjectSpaceSample();
            using var handle = new FileHandle(s.Model, file);
            using var notifier = new TBoldPersistenceNotifier();
            var seen = new List<int>();
            notifier.OnStartUpdate = (o, e, r, a) => seen.Add(e);
            notifier.OnEndUpdate = (o, e, r, a) => seen.Add(e);
            notifier.OnProgressStart = (o, e, r, a) => seen.Add(-1);
            notifier.OnProgressEnd = (o, e, r, a) => seen.Add(-2);
            notifier.OnCreateObject = (o, e, r, a) => seen.Add(e);
            Assert.That(notifier.OnFetchObject, Is.Null);
            notifier.PersistenceHandle = handle;
            Assert.That(notifier.PersistenceHandle, Is.SameAs(handle));

            // The controller sends start and end of updates; the notifier passes them to the handlers.
            handle.PersistenceController.SendExtendedEvent(BoldSubscription.bpeStartUpdate, [new TBoldObjectIdList()]);
            handle.PersistenceController.SendExtendedEvent(BoldSubscription.bpeEndUpdate, []);
            Assert.That(seen, Is.EqualTo(new[] { -1, BoldSubscription.bpeStartUpdate, BoldSubscription.bpeEndUpdate, -2 }));

            // Fetching the same object one at a time more than ten times in ten seconds raises the alert.
            int alerts = 0;
            notifier.OnAlertExcessiveFetch = (id, count) => alerts++;
            var oneId = new TBoldObjectIdList();
            oneId.Add(new TBoldDefaultID(0, true) { AsInteger = 7 });
            for (int i = 0; i < 11; i++)
            {
                handle.PersistenceController.SendExtendedEvent(BoldSubscription.bpeStartFetch, [oneId]);
            }

            Assert.That(alerts, Is.EqualTo(1));
            Assert.That(notifier.FetchLog.Count, Is.EqualTo(11));

            handle.Dispose();
            Assert.That(notifier.PersistenceHandle, Is.Null, "a destroyed handle is dropped");
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Test]
    [Covers(typeof(TBoldPersistenceHandlePassthrough), "*")]
    [Covers(typeof(TBoldPersistenceHandlePassthroughWithModel), "*")]
    public void Pass_through_handles_chain_controllers_activation_and_models()
    {
        using var s = new ObjectSpaceSample();
        using var next = new FileHandle(s.Model, Path.Combine(Path.GetTempPath(), "unused.xml"));
        using var chain = new ChainHandle();
        chain.NextPersistenceHandle = next;
        Assert.That(chain.Inits, Is.EqualTo(1));
        var controller = (TBoldPersistenceControllerPassthrough)chain.PersistenceController;
        Assert.That(controller.NextPersistenceController, Is.SameAs(next.PersistenceController));
        chain.Active = true;
        Assert.That(next.Active, Is.True, "activation passes on");
        chain.Active = false;
        Assert.That(next.Active, Is.False);

        int identityChanges = 0;
        var subscriber = new TBoldPassthroughSubscriber((o, e, r) => identityChanges++);
        chain.AddSubscription(subscriber, BoldSubscription.beValueIdentityChanged, BoldSubscription.beValueIdentityChanged);
        using var model = new TBoldAbstractModelForTests(s.Model);
        chain.BoldModel = model;
        Assert.That(identityChanges, Is.EqualTo(1), "a new model counts as an identity change");
        Assert.That(chain.PersistenceController, Is.Not.SameAs(controller), "and releases the controller");
        model.ChangeModel(s.Model);
        Assert.That(identityChanges, Is.EqualTo(2));
        model.Dispose();
        Assert.That(chain.BoldModel, Is.Null);

        next.Dispose();
        Assert.That(chain.NextPersistenceHandle, Is.Null, "a destroyed next handle is dropped");
    }
}
