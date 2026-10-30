// Delphi: Source/ObjectSpace/BORepresentation/BoldExternalObjectSpaceEventHandler.pas, Source/Persistence/Propagation/BoldPropagatorUtils.pas
// Port map: CSharp/port/units/BoldExternalObjectSpaceEventHandler.md
using BuildingClasses;

namespace Bold.Tests;

/// <summary>What a client does with the propagator's messages, and reconnecting a client.</summary>
[NonParallelizable]
public class BoldExternalEventsTests
{
    private ClientEnvironment env = null!;
    private PropagatorSample propagator = null!;
    private string file = null!;
    private readonly List<PropagationClient> clients = [];

    [SetUp]
    public void Start()
    {
        env = new ClientEnvironment();
        propagator = new PropagatorSample();
        file = Path.Combine(Path.GetTempPath(), $"external-events-{Guid.NewGuid():N}.db");
    }

    [TearDown]
    public void Stop()
    {
        try
        {
            foreach (PropagationClient client in clients)
            {
                client.Dispose();
            }
        }
        finally
        {
            clients.Clear();
            propagator.Dispose();
            env.Dispose();
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    private PropagationClient Client(string name, bool createSchema = false)
    {
        var client = new PropagationClient(name, file);
        clients.Add(client);
        if (createSchema)
        {
            client.DatabaseHandle.CreateDataBaseSchema();
        }

        client.Open();
        return client;
    }

    private static string Event(TBoldObjectSpaceSubscriptionType kind, string className = "", string memberName = "", string lockName = "", TBoldObject? obj = null) =>
        TBoldObjectSpaceExternalEvent.EncodeExternalEvent(kind, className, memberName, lockName, obj?.BoldObjectLocator!.BoldObjectID);

    /// <summary>Hands the messages to the client's event handler, as its listener would.</summary>
    private static void Receive(PropagationClient client, params string[] messages)
    {
        foreach (string message in messages)
        {
            client.EventHandler.Queue!.Enqueue(message);
        }

        client.EventHandler.DequeueAll();
    }

    [Test]
    [Covers(typeof(TBoldClassChangedEvent))]
    [Covers(typeof(TBoldEmbeddedStateChangedEvent))]
    [Covers(typeof(TBoldNonEmbeddedStateChangedEvent))]
    [Covers(typeof(TBoldConflictEvent))]
    [Covers(typeof(TBoldLockLostEvent))]
    [Covers(typeof(TBoldDoDisconnectEvent))]
    public void Messages_change_the_object_space_unless_events_handle_them()
    {
        PropagationClient writer = Client("W", createSchema: true);
        _ = new TPerson(writer.System) { FirstName = "Ann", LastName = "Lee" };
        _ = new TPerson(writer.System) { FirstName = "Bob", LastName = "Ray" };
        writer.System.UpdateDatabase();
        PropagationClient a = Client("A");
        TBoldObjectList persons = a.Persons;
        Assert.That(persons.Count, Is.EqualTo(2));
        TPerson ann = a.Person("Ann")!, bob = a.Person("Bob")!;
        Assert.That(ann.OwnedBuildings.Count, Is.EqualTo(0));
        Assert.That(persons.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsTransient), "a loaded class list");

        // Without events: invalidated, marked deleted, a conflict for a dirty object.
        Receive(a, Event(TBoldObjectSpaceSubscriptionType.bsClassChanged, "Person"));
        Assert.That(persons.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsInvalid));
        Receive(a, Event(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, obj: ann));
        Assert.That(ann.M_FirstName.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsInvalid));
        Receive(a, Event(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, memberName: "ownedBuildings", obj: ann));
        Assert.That(ann.M_OwnedBuildings.BoldPersistenceState, Is.EqualTo(TBoldValuePersistenceState.bvpsInvalid));
        ann.Assets = 3;
        Receive(a, Event(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, obj: ann),
            Event(TBoldObjectSpaceSubscriptionType.bsObjectDeleted, obj: ann));
        Assert.That(a.Conflicts, Is.EqualTo(new[] { ann, ann }));
        Assert.That(ann.Assets, Is.EqualTo(3m));
        Receive(a, Event(TBoldObjectSpaceSubscriptionType.bsObjectDeleted, obj: bob));
        Assert.That(bob.BoldObjectIsDeleted, Is.True);
        Receive(a, Event(TBoldObjectSpaceSubscriptionType.bsLockLost, lockName: "X"), Event(TBoldObjectSpaceSubscriptionType.bsGotLocks));
        Assert.That(() => Receive(a, Event(TBoldObjectSpaceSubscriptionType.bsClassChanged, "Nothing")), Throws.TypeOf<EBold>());
        a.EventHandler.Queue!.Clear();

        // With events: the events get the class, object or member, and the lost lock.
        var seen = new List<object?>();
        TBoldExternalObjectSpaceEventHandler handler = a.EventHandler;
        handler.OnClassChanged = list => seen.Add(list);
        handler.OnEmbeddedStateChanged = obj => seen.Add(obj);
        handler.OnNonEmbeddedStateChanged = member => seen.Add(member);
        handler.OnObjectDeleted = obj => seen.Add(("deleted", obj));
        handler.OnLockLost = name => seen.Add(name);
        Receive(a, Event(TBoldObjectSpaceSubscriptionType.bsClassChanged, "Person"),
            Event(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, obj: ann),
            Event(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged, memberName: "ownedBuildings", obj: ann),
            Event(TBoldObjectSpaceSubscriptionType.bsObjectDeleted, obj: ann),
            Event(TBoldObjectSpaceSubscriptionType.bsLockLost, lockName: "Y"));
        Assert.That(seen, Is.EqualTo(new object?[] { a.Persons, ann, ann.M_OwnedBuildings, ("deleted", ann), "Y" }));
        Assert.That(ann.BoldObjectIsDeleted, Is.False);

        // An object not in memory is passed on as nil only when asked.
        string unknown = TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged, "", "", "",
            new TBoldDefaultID { AsInteger = 9999 });
        seen.Clear();
        Receive(a, unknown);
        Assert.That(seen, Is.Empty);
        handler.HandleNilObjects = true;
        Assert.That(handler.HandleNilObjects, Is.True);
        Receive(a, unknown);
        Assert.That(seen, Is.EqualTo(new object?[] { null }));

        // A disconnect from the propagator disconnects the propagator handle and is passed on.
        (string Message, int Time)? disconnect = null;
        handler.OnDoDisconnect = (message, time) => disconnect = (message, time);
        Receive(a, "DISCONNECT:5000:back soon");
        Assert.That(disconnect, Is.EqualTo(("back soon", 5000)));
        Assert.That(a.PropagatorHandle.Connected, Is.False);
    }

    [Test]
    public void The_event_handler_needs_an_active_system_and_forgets_handles_that_go()
    {
        using var handler = new TBoldExternalObjectSpaceEventHandler { Name = "Handler", Queue = new TBoldThreadSafeStringQueue("Test") };
        handler.Queue.Enqueue("C:Person");
        Assert.That(() => handler.DequeueAll(), Throws.TypeOf<EBold>().With.Message.Contains("Handler"), "no system handle");
        var systemHandle = new TBoldSystemHandle { Name = "System" };
        handler.BoldSystemHandle = systemHandle;
        Assert.That(handler.BoldSystemHandle, Is.SameAs(systemHandle));
        handler.Queue.Enqueue("C:Person");
        Assert.That(() => handler.DequeueAll(), Throws.TypeOf<EBold>().With.Message.Contains("System"), "the system is not active");
        var propagatorHandle = new TBoldInProcessPropagatorHandle();
        handler.PropagatorHandle = propagatorHandle;
        Assert.That(handler.PropagatorHandle, Is.SameAs(propagatorHandle));
        systemHandle.Dispose();
        propagatorHandle.Dispose();
        Assert.That((handler.BoldSystemHandle, handler.PropagatorHandle), Is.EqualTo(((TBoldSystemHandle?)null, (TBoldAbstractPropagatorHandle?)null)));
        handler.Queue.Dispose();
    }

    [Test]
    [Covers(typeof(BoldPropagatorUtils), "*")]
    public void A_reconnected_client_subscribes_again_to_what_it_has_in_memory()
    {
        PropagationClient a = Client("A", createSchema: true);
        PropagationClient b = Client("B");
        var ann = new TPerson(a.System) { FirstName = "Ann", LastName = "Lee" };
        a.System.UpdateDatabase();
        TPerson annInB = b.Person("Ann")!;
        int oldId = b.ListenerHandle.BoldClientID;

        Assert.That(BoldPropagatorUtils.ReConnectToPropagator(b.SystemHandle, b.ListenerHandle, b.PropagatorHandle, b.SnooperHandle), Is.True);
        Assert.That(b.ListenerHandle.Connected, Is.True);
        Assert.That(b.ListenerHandle.BoldClientID, Is.Not.EqualTo(oldId), "registered again");
        propagator.WaitForDequeuer();

        // B hears of A's changes to what it had in memory: the object, and the class it had loaded.
        ann.FirstName = "Anne";
        a.System.UpdateDatabase();
        Assert.That(env.PumpUntil(() => annInB.M_FirstName.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid), Is.True);
        _ = new TPerson(a.System) { FirstName = "Bob" };
        a.System.UpdateDatabase();
        Assert.That(env.PumpUntil(() => b.Persons.Count == 2), Is.True);
    }
}
