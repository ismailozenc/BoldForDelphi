// Delphi: Source/Persistence/Propagation (BoldSnooper, BoldIDAdder, BoldListenerHandle), Source/ObjectSpace/BORepresentation/BoldExternalObjectSpaceEventHandler.pas,
// Source/ObjectSpace/PessimisticLocking (BoldLockHolder, BoldLockHandler), Source/Handles/PessimisticLocking/BoldLockingHandles.pas
// Port map: CSharp/port/units/BoldSnooper.md
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
        PropagationScenarios.ObjectSpacesStayInStep(env, a, b);
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
        PropagationScenarios.LockConflictsAreReported(env, a, b);
    }
}
