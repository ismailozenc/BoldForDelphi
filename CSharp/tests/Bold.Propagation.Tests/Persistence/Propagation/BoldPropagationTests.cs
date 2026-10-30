// Delphi: Source/Persistence/Propagation (BoldSnooper, BoldIDAdder, BoldListenerHandle), Source/ObjectSpace/BORepresentation/BoldExternalObjectSpaceEventHandler.pas,
// Source/ObjectSpace/PessimisticLocking (BoldLockHolder, BoldLockHandler), Source/Handles/PessimisticLocking/BoldLockingHandles.pas
// Port map: CSharp/port/units/BoldSnooper.md
using BuildingClasses;

namespace Bold.Tests;

/// <summary>Two client applications on one database, kept in step through the propagator.</summary>
[NonParallelizable]
public class BoldPropagationTests
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
        file = Path.Combine(Path.GetTempPath(), $"propagation-{Guid.NewGuid():N}.db");
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

    private PropagationClient Client(string name, bool withRegions = false, bool createSchema = false)
    {
        var client = new PropagationClient(name, file, withRegions);
        clients.Add(client);
        if (createSchema)
        {
            client.DatabaseHandle.CreateDataBaseSchema();
        }

        client.Open();
        return client;
    }

    [Test]
    [Covers(typeof(TBoldSnooper), "*")]
    [Covers(typeof(TBoldSnooperHandle), "*")]
    [Covers(typeof(TBoldIDAdder), "*")]
    [Covers(typeof(TBoldIDAdderHandle), "*")]
    [Covers(typeof(TBoldExternalObjectSpaceEventHandler), "*")]
    public void Two_object_spaces_stay_in_step_through_the_propagator()
    {
        PropagationClient a = Client("A", createSchema: true);
        PropagationClient b = Client("B");
        Assert.That(a.ListenerHandle.Connected && b.ListenerHandle.Connected, Is.True, "both listeners registered");
        Assert.That(a.ListenerHandle.BoldClientID, Is.Not.EqualTo(b.ListenerHandle.BoldClientID));
        Assert.That(a.SnooperHandle.Connected, Is.True);

        // A new person in A is seen by B, which subscribes to persons by loading them.
        var ann = new TPerson(a.System) { FirstName = "Ann", LastName = "Lee", Assets = 100 };
        a.System.UpdateDatabase();
        Assert.That(b.Persons.Count, Is.EqualTo(1));
        TPerson annInB = b.Person("Ann")!;
        Assert.That(annInB.LastName, Is.EqualTo("Lee"));

        // A changes her: B's copy is invalidated and fetched again.
        ann.FirstName = "Anne";
        a.System.UpdateDatabase();
        Assert.That(env.PumpUntil(() => annInB.M_FirstName.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid), Is.True,
            "B hears of the change");
        Assert.That(annInB.FirstName, Is.EqualTo("Anne"));

        // A second person in A: B's list of persons is invalidated.
        var bob = new TPerson(a.System) { FirstName = "Bob", LastName = "Ray" };
        a.System.UpdateDatabase();
        Assert.That(env.PumpUntil(() => b.Persons.Count == 2), Is.True, "B sees the new person");

        // A deletes Bob: B's copy goes too.
        TPerson bobInB = b.Person("Bob")!;
        bob.Delete();
        a.System.UpdateDatabase();
        Assert.That(env.PumpUntil(() => bobInB.BoldObjectIsDeleted), Is.True, "B hears of the deletion");
        Assert.That(b.Persons.Count, Is.EqualTo(1));

        // B changes Anne without saving while A changes her too: a conflict for B.
        annInB.Assets = 5;
        ann.Assets = 7;
        a.System.UpdateDatabase();
        Assert.That(env.PumpUntil(() => b.Conflicts.Count == 1), Is.True, "B is told of the conflict");
        Assert.That(b.Conflicts[0], Is.SameAs(annInB));
        Assert.That(annInB.Assets, Is.EqualTo(5m), "B keeps its own change");
    }

    [Test]
    [Covers(typeof(TBoldLockingHandle), "*")]
    [Covers(typeof(TBoldPessimisticLockHandler), "*")]
    [Covers(typeof(TBoldLockHolder), "*")]
    [Covers(typeof(TBoldAbstractLockHolder), "*")]
    [Covers(typeof(TBoldLockList), "*")]
    [Covers(typeof(TBoldLock), "*")]
    [Covers(typeof(EBoldGetLocksFailed), "*")]
    [Covers(typeof(TBoldFailureGetLocksFailed), "*")]
    [Covers(typeof(TBoldEmptyLockHolder), "*")]
    [Covers(typeof(TBoldAbstractLockManagerHandle), "*")]
    [Covers(typeof(BoldLockHandler), "*")]
    public void Lock_conflicts_are_reported()
    {
        PropagationClient a = Client("A", withRegions: true, createSchema: true);
        PropagationClient b = Client("B", withRegions: true);
        var ann = new TPerson(a.System) { FirstName = "Ann", LastName = "Lee" };
        a.System.UpdateDatabase();
        TPerson annInB = b.Person("Ann")!;
        TBoldLockingHandle lockingA = a.AddLocking();
        TBoldLockingHandle lockingB = b.AddLocking();
        Assert.That(lockingA.Active && lockingB.Active, Is.True);
        Assert.That(a.System.PessimisticLockHandler, Is.SameAs(lockingA.LockHandler));

        // A changes Ann: it gets the lock of her region first.
        ann.FirstName = "Anne";
        Assert.That(lockingA.LockHolder.HeldExclusive.Count, Is.EqualTo(1), "A holds Ann's region");
        string annRegion = lockingA.LockHolder.HeldExclusive.AsOLEVariant()[0];
        Assert.That(lockingA.LockHandler!.EnsureLocks(), Is.True);

        // B cannot change her while A holds the lock, and is told who holds it.
        var conflict = Assert.Throws<EBoldGetLocksFailed>(() => annInB.LastName = "Smith")!;
        Assert.That(conflict.ClientIds.ToArray(), Is.EqualTo(new[] { $"{a.ListenerHandle.BoldClientID}=A" }));
        Assert.That(conflict.ConflictingRegions.Count, Is.EqualTo(1));
        Assert.That(conflict.ConflictingRegions[0].Root, Is.SameAs(annInB));
        Assert.That(conflict.ConflictingRegions[0].AsString, Is.EqualTo(annRegion), "the same region in both object spaces");
        Assert.That(annInB.LastName, Is.EqualTo("Lee"));

        // A saves, which releases the locks it no longer needs.
        a.System.UpdateDatabase();
        Assert.That(lockingA.LockHolder.HeldExclusive.Count, Is.EqualTo(0), "A's lock is released after saving");

        // B gets the lock now, but A's change has not been handled in B yet: that is a conflict too.
        Assert.Throws<EBoldGetLocksFailed>(() => annInB.LastName = "Smith");
        Assert.That(lockingB.LockHolder.HeldExclusive.Count, Is.EqualTo(0), "the locks were given back");

        // Once B has handled A's change, B can change her.
        Assert.That(env.PumpUntil(() => annInB.M_FirstName.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid), Is.True);
        annInB.LastName = "Smith";
        Assert.That(annInB.FirstName, Is.EqualTo("Anne"));
        Assert.That(lockingB.LockHolder.HeldExclusive.Count, Is.EqualTo(1));
        b.System.UpdateDatabase();
        Assert.That(env.PumpUntil(() => ann.M_LastName.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid), Is.True);
        Assert.That(ann.LastName, Is.EqualTo("Smith"));
    }
}
