using System.ComponentModel;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>
/// A synchronization context for the test thread, standing in for a GUI thread: what other threads post waits until
/// the test pumps it.
/// </summary>
internal sealed class PumpedSynchronizationContext : SynchronizationContext
{
    private readonly Queue<(SendOrPostCallback Callback, object? State, ManualResetEventSlim? Done)> fWork = new();
    private readonly int fThreadId = Environment.CurrentManagedThreadId;

    public override void Post(SendOrPostCallback d, object? state)
    {
        lock (fWork)
        {
            fWork.Enqueue((d, state, null));
        }
    }

    public override void Send(SendOrPostCallback d, object? state)
    {
        if (Environment.CurrentManagedThreadId == fThreadId)
        {
            d(state);
            return;
        }

        using var done = new ManualResetEventSlim(false);
        lock (fWork)
        {
            fWork.Enqueue((d, state, done));
        }

        done.Wait();
    }

    /// <summary>Runs what was posted; true when there was something.</summary>
    public bool Pump()
    {
        bool any = false;
        while (true)
        {
            (SendOrPostCallback Callback, object? State, ManualResetEventSlim? Done) work;
            lock (fWork)
            {
                if (fWork.Count == 0)
                {
                    return any;
                }

                work = fWork.Dequeue();
            }

            any = true;
            try
            {
                work.Callback(work.State);
            }
            finally
            {
                work.Done?.Set();
            }
        }
    }
}

/// <summary>
/// The environment of a client application for the tests: a Bold queue that the test runs (as an application's idle
/// time would), and the test thread's synchronization context as the GUI thread's.
/// </summary>
internal sealed class ClientEnvironment : TBoldEnvironmentConfiguration
{
    private sealed class TestQueue : TBoldQueue
    {
        public override void ActivateDisplayQueue()
        {
        }

        public override void DeActivateDisplayQueue()
        {
        }
    }

    private readonly SynchronizationContext? fOldContext;
    public readonly PumpedSynchronizationContext Context = new();
    public int Triggers;

    public ClientEnvironment()
    {
        fOldContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(Context);
        BoldEnvironment.BoldInternalCustomConfiguration = this;
    }

    protected override string GetName() => "Propagation tests";

    protected override Type GetQueueClass() => typeof(TestQueue);

    protected override object? GetRootComponent() => null;

    public override void TriggerQueueMechanism() => Interlocked.Increment(ref Triggers);

    /// <summary>Runs the posted work and the Bold queue's pre-display queue (the dequeuers) until nothing is left.</summary>
    public void Pump()
    {
        bool any;
        do
        {
            any = Context.Pump();
            BoldQueue.BoldInstalledQueue()?.PerformPreDisplayQueue();
        }
        while (any);
    }

    /// <summary>Pumps until the condition holds (five seconds at most); true when it does.</summary>
    public bool PumpUntil(Func<bool> condition, int milliseconds = 5000)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (true)
        {
            Pump();
            if (condition())
            {
                return true;
            }

            if (watch.ElapsedMilliseconds > milliseconds)
            {
                return false;
            }

            Thread.Sleep(5);
        }
    }

    public override void Dispose()
    {
        BoldEnvironment.BoldInternalCustomConfiguration = null;
        SynchronizationContext.SetSynchronizationContext(fOldContext);
        base.Dispose();
    }
}

/// <summary>
/// A client application on the Building model, as its data module would hold it: the system persists through an ID
/// adder, a snooper and a database handle on a shared SQLite file; a listener and an event handler bring the other
/// clients' changes in; a locking handle gets pessimistic locks. The propagator and the lock manager are the server of
/// this process, reached in the process or, with a hub address, through the propagator hub.
/// </summary>
internal sealed class PropagationClient : IDisposable
{
    /// <summary>The Building model's region definitions for the lock tests: a person's names and assets are one region.</summary>
    public const string RegionDefinitions = "Person[Person]: firstName, lastName, assets";

    public readonly Container Components = new();
    public readonly string Name;
    public readonly TBoldAbstractModel Model;
    public readonly TADOConnection Connection;
    public readonly TBoldDatabaseAdapterADO Adapter;
    public readonly TBoldPersistenceHandleDB DatabaseHandle;
    public readonly TBoldAbstractPropagatorHandle PropagatorHandle;
    public readonly TBoldAbstractLockManagerHandle LockManagerHandle;
    public readonly TBoldSnooperHandle SnooperHandle;
    public readonly TBoldExternalObjectSpaceEventHandler EventHandler;
    public readonly TBoldListenerHandle ListenerHandle;
    public readonly TBoldIDAdderHandle IDAdderHandle;
    public readonly TBoldSystemTypeInfoHandle TypeInfoHandle;
    public readonly TBoldSystemHandle SystemHandle;
    public TBoldLockingHandle? LockingHandle;
    public readonly List<TBoldObject> Conflicts = [];

    public PropagationClient(string name, string databaseFile, bool withRegions = false, string? hubUrl = null)
    {
        Name = name;
        Model = new TBoldAbstractModel(Components) { Name = name + ".Model" };
        Model.ReadModel(ModelLines(withRegions));
        Connection = TestDatabase.NewADOConnection(databaseFile);
        Adapter = new TBoldDatabaseAdapterADO(Components) { Connection = Connection };
        Adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
        DatabaseHandle = new TBoldPersistenceHandleDB(Components) { BoldModel = Model, DatabaseAdapter = Adapter };
        if (hubUrl is null)
        {
            PropagatorHandle = new TBoldInProcessPropagatorHandle(Components) { Name = name + ".Propagator" };
            LockManagerHandle = new TBoldInProcessLockManagerHandle(Components);
        }
        else
        {
            PropagatorHandle = new TBoldSignalRPropagatorHandle(Components) { Name = name + ".Propagator", Url = hubUrl, TimeOut = 10000 };
            LockManagerHandle = new TBoldSignalRLockManagerHandle(Components) { Url = hubUrl, TimeOut = 10000 };
        }

        SnooperHandle = new TBoldSnooperHandle(Components)
        {
            BoldModel = Model,
            NextPersistenceHandle = DatabaseHandle,
            PropagatorHandle = PropagatorHandle,
            LockManagerHandle = LockManagerHandle,
        };
        TypeInfoHandle = new TBoldSystemTypeInfoHandle(Components) { BoldModel = Model };
        SystemHandle = new TBoldSystemHandle(Components) { Name = name + ".System", SystemTypeInfoHandle = TypeInfoHandle };
        EventHandler = new TBoldExternalObjectSpaceEventHandler(Components)
        {
            Name = name + ".EventHandler",
            BoldSystemHandle = SystemHandle,
            PropagatorHandle = PropagatorHandle,
            OnConflict = obj => Conflicts.Add(obj),
        };
        ListenerHandle = new TBoldListenerHandle(Components)
        {
            PropagatorHandle = PropagatorHandle,
            Dequeuer = EventHandler,
            PollingInterval = 10,
            ClientIdentifierString = name,
        };
        IDAdderHandle = new TBoldIDAdderHandle(Components) { NextPersistenceHandle = SnooperHandle, BoldListener = ListenerHandle };
        SystemHandle.PersistenceHandle = IDAdderHandle;
    }

    /// <summary>The Building model, with the region definitions when asked.</summary>
    public static TStringList ModelLines(bool withRegions)
    {
        TStringList lines = BuildingModel.Lines();
        if (withRegions)
        {
            // The model's tagged values are its sixth line.
            lines[6] = lines[6].TrimEnd('"') + ",\\\"Bold.RegionDefinitions=" + RegionDefinitions + "\\\"\"";
        }

        return lines;
    }

    /// <summary>Connects to the propagator and the lock manager and opens the system (which starts the listener).</summary>
    public void Open()
    {
        PropagatorHandle.Connected = true;
        switch (LockManagerHandle)
        {
            case TBoldInProcessLockManagerHandle inProcess:
                inProcess.Connected = true;
                break;
            case TBoldSignalRLockManagerHandle signalR:
                signalR.Connected = true;
                break;
        }

        SystemHandle.Active = true;
    }

    /// <summary>Adds a locking handle for the system, through the listener and the lock manager.</summary>
    public TBoldLockingHandle AddLocking()
    {
        LockingHandle = new TBoldLockingHandle(Components) { Listener = ListenerHandle, LockManager = LockManagerHandle, SystemHandle = SystemHandle };
        return LockingHandle;
    }

    public TBoldSystem System => SystemHandle.System!;

    public TBoldObjectList Persons => System.ClassByExpressionName("Person");

    public TPerson? Person(string firstName)
    {
        TBoldObjectList persons = Persons;
        for (int i = 0; i < persons.Count; i++)
        {
            if (persons[i] is TPerson person && person.FirstName == firstName)
            {
                return person;
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (SystemHandle.Active)
        {
            System.Discard();
            SystemHandle.Active = false;
        }

        Components.Dispose();
        Connection.Close();
        Connection.Connection.Dispose();
    }
}
