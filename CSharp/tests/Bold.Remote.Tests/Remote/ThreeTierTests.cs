// Remote persistence with the propagator on the same host (decision D8): client applications that reach the database
// only through the server, and hear of each other's changes through its propagator hub.
using System.ComponentModel;
using BuildingClasses;

namespace Bold.Tests;

[NonParallelizable]
public class ThreeTierTests
{
    /// <summary>A client application with no database of its own: the server persists, the hub tells it of others' changes.</summary>
    private sealed class ClientApplication : IDisposable
    {
        public readonly Container Components = new();
        public readonly TBoldHttpClientPersistenceHandle PersistenceHandle;
        public readonly TBoldSignalRPropagatorHandle PropagatorHandle;
        public readonly TBoldListenerHandle ListenerHandle;
        public readonly TBoldSystemHandle SystemHandle;
        public readonly List<TBoldObject> Conflicts = [];

        public ClientApplication(string name, RemoteServerSample server)
        {
            var model = new TBoldAbstractModel(Components) { Name = name + ".Model" };
            model.ReadModel(BuildingModel.Lines());
            PersistenceHandle = new TBoldHttpClientPersistenceHandle(Components) { BoldModel = model, Url = server.PersistenceUrl };
            PropagatorHandle = new TBoldSignalRPropagatorHandle(Components) { Url = server.PropagatorUrl, TimeOut = 10000 };
            var typeInfoHandle = new TBoldSystemTypeInfoHandle(Components) { BoldModel = model };
            SystemHandle = new TBoldSystemHandle(Components) { Name = name + ".System", SystemTypeInfoHandle = typeInfoHandle };
            var events = new TBoldExternalObjectSpaceEventHandler(Components)
            {
                BoldSystemHandle = SystemHandle,
                PropagatorHandle = PropagatorHandle,
                OnConflict = obj => Conflicts.Add(obj),
            };
            ListenerHandle = new TBoldListenerHandle(Components)
            {
                PropagatorHandle = PropagatorHandle,
                Dequeuer = events,
                PollingInterval = 10,
                ClientIdentifierString = name,
            };
            SystemHandle.PersistenceHandle = new TBoldIDAdderHandle(Components) { NextPersistenceHandle = PersistenceHandle, BoldListener = ListenerHandle };
            PropagatorHandle.Connected = true;
            SystemHandle.Active = true;
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
        }
    }

    [Test]
    public void Client_applications_on_the_server_stay_in_step_through_its_propagator()
    {
        using var env = new ClientEnvironment();
        using var propagator = new PropagatorSample();
        string file = Path.Combine(Path.GetTempPath(), $"three-tier-{Guid.NewGuid():N}.db");
        var serverModel = new TBoldAbstractModel { Name = "Server.Model" };
        serverModel.ReadModel(BuildingModel.Lines());
        try
        {
            using var setup = new TBoldRemotePersistenceSetup(serverModel, TestDatabase.NewConnection(file), TBoldDataBaseEngine.dbeSQLite, WithPropagator: true);
            setup.DatabaseHandle.CreateDataBaseSchema();
            setup.ConnectToPropagator();
            using var server = new RemoteServerSample(setup.PersistenceHandle, setup.Model.MoldModel, withPropagatorHub: true);
            using var a = new ClientApplication("A", server);
            using var b = new ClientApplication("B", server);
            Assert.That(a.ListenerHandle.Connected && b.ListenerHandle.Connected, Is.True);

            // A new person saved by A, through the server, is seen by B, which loads her through the server.
            var ann = new TPerson(a.System) { FirstName = "Ann", LastName = "Lee", Assets = 100 };
            a.System.UpdateDatabase();
            Assert.That(b.Persons.Count, Is.EqualTo(1));
            TPerson annInB = b.Person("Ann")!;
            propagator.WaitForDequeuer();

            // A changes her: the server's snooper tells the propagator, which tells B.
            ann.FirstName = "Anne";
            a.System.UpdateDatabase();
            Assert.That(env.PumpUntil(() => annInB.M_FirstName.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid), Is.True,
                "B hears of the change");
            Assert.That(annInB.FirstName, Is.EqualTo("Anne"));

            // A new person in A: B's list of persons is invalidated.
            _ = new TPerson(a.System) { FirstName = "Bob" };
            a.System.UpdateDatabase();
            Assert.That(env.PumpUntil(() => b.Persons.Count == 2), Is.True, "B sees the new person");

            // B changes Anne without saving while A changes her too: a conflict for B.
            annInB.Assets = 5;
            ann.Assets = 7;
            a.System.UpdateDatabase();
            Assert.That(env.PumpUntil(() => b.Conflicts.Count == 1), Is.True, "B is told of the conflict");
        }
        finally
        {
            serverModel.Dispose();
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
