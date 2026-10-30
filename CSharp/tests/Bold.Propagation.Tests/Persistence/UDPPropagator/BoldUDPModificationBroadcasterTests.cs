// Delphi: Source/Persistence/UDPPropagator/BoldUDPModificationBroadcaster.pas, BoldAbstractModificationPropagator.pas
// Port map: CSharp/port/units/BoldUDPModificationBroadcaster.md
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BuildingClasses;

namespace Bold.Tests;

[NonParallelizable]
public class BoldUDPModificationBroadcasterTests
{
    private ClientEnvironment env = null!;
    private int port;

    [SetUp]
    public void Start()
    {
        env = new ClientEnvironment();
        port = Random.Shared.Next(41000, 49000);
    }

    [TearDown]
    public void Stop() => env.Dispose();

    private sealed class TestBroadcaster : TBoldUDPModificationBroadcaster
    {
        public TestBroadcaster(IContainer? owner = null)
            : base(owner)
        {
        }

        public void Send(string anEvent) => EnqueEvent(anEvent);
    }

    /// <summary>A client application that tells the others of its changes by UDP broadcasts.</summary>
    private sealed class UdpClientApplication : IDisposable
    {
        public readonly Container Components = new();
        public readonly TADOConnection Connection;
        public readonly TBoldPersistenceHandleDB DatabaseHandle;
        public readonly TBoldUDPModificationBroadcaster Broadcaster;
        public readonly TBoldExternalObjectSpaceEventHandler EventHandler;
        public readonly TBoldSystemHandle SystemHandle;
        public readonly List<string> Received = [];

        public UdpClientApplication(string name, string databaseFile, int port)
        {
            var model = new TBoldAbstractModel(Components) { Name = name + ".Model" };
            model.ReadModel(BuildingModel.Lines());
            Connection = TestDatabase.NewADOConnection(databaseFile);
            var adapter = new TBoldDatabaseAdapterADO(Components) { Connection = Connection };
            adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
            DatabaseHandle = new TBoldPersistenceHandleDB(Components) { BoldModel = model, DatabaseAdapter = adapter };
            var typeInfoHandle = new TBoldSystemTypeInfoHandle(Components) { BoldModel = model };
            SystemHandle = new TBoldSystemHandle(Components) { Name = name + ".System", SystemTypeInfoHandle = typeInfoHandle };
            EventHandler = new TBoldExternalObjectSpaceEventHandler(Components) { BoldSystemHandle = SystemHandle };
            Broadcaster = new TBoldUDPModificationBroadcaster(Components)
            {
                Port = port,
                NextPersistenceHandle = DatabaseHandle,
                SystemHandle = SystemHandle,
                Dequeuer = EventHandler,
                OnReceiveEvent = (_, e) => Received.Add(e),
            };
            SystemHandle.PersistenceHandle = Broadcaster;
        }

        public TBoldSystem System => SystemHandle.System!;

        public TBoldObjectList Persons => System.ClassByExpressionName("Person");

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

    /// <summary>True when a broadcast sent here comes back to two sockets on its port (as it does unless a firewall stops it).</summary>
    private bool BroadcastsComeBack()
    {
        UdpClient Listener()
        {
            var listener = new UdpClient(AddressFamily.InterNetwork);
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            listener.Client.ReceiveTimeout = 1000;
            return listener;
        }

        using UdpClient first = Listener(), second = Listener();
        using var sender = new UdpClient { EnableBroadcast = true };
        sender.Send([1], 1, "255.255.255.255", port);
        try
        {
            IPEndPoint? from = null;
            first.Receive(ref from);
            second.Receive(ref from);
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    [Test]
    [Covers(typeof(TBoldUDPModificationBroadcaster), "*")]
    [Covers(typeof(TActivationErrorEvent))]
    [Covers(typeof(TReceivePropagatorEvent))]
    [Covers(typeof(BoldAbstractModificationPropagator), "*")]
    public void A_broadcaster_sends_its_events_and_receives_those_with_its_signature()
    {
        Assert.That(BoldAbstractModificationPropagator.SPropagatorIdentification, Is.EqualTo("#BOLDEVENT#\r\n"));

        // Sending: one datagram for each event, to the broadcast address and the port.
        using (var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, port)))
        using (var sender = new TestBroadcaster { Port = port, BroadcastAddress = "127.0.0.1" })
        {
            listener.Client.ReceiveTimeout = 5000;
            Assert.That((sender.Port, sender.Active), Is.EqualTo((port, false)));
            sender.Send("C:Person");
            IPEndPoint? from = null;
            Assert.That(Encoding.UTF8.GetString(listener.Receive(ref from)), Is.EqualTo("#BOLDUDP#\r\nC:Person"));
        }

        // Receiving: datagrams without the signature are ignored; the events go to OnReceiveEvent on this thread.
        var received = new List<(object? Sender, string Event, int Thread)>();
        using var broadcaster = new TBoldUDPModificationBroadcaster { Port = port };
        broadcaster.OnReceiveEvent = (s, e) => received.Add((s, e, Environment.CurrentManagedThreadId));
        broadcaster.Active = true;
        Assert.That(broadcaster.Active, Is.True);
        using (var client = new UdpClient())
        {
            foreach (string datagram in new[] { "hello", "#boldudp#\r\nE:1", "#BOLDUDP#\r\nE:2" })
            {
                byte[] bytes = Encoding.UTF8.GetBytes(datagram);
                client.Send(bytes, bytes.Length, "127.0.0.1", port);
            }
        }

        Assert.That(env.PumpUntil(() => received.Count == 2), Is.True);
        Assert.That(received.Select(r => r.Event), Is.EqualTo(new[] { "E:1", "E:2" }), "the signature in any case");
        Assert.That(received[0].Sender, Is.SameAs(broadcaster));
        Assert.That(received[0].Thread, Is.EqualTo(Environment.CurrentManagedThreadId));

        // Changing the port deactivates; a failure to listen goes to OnActivationError, or is raised without one.
        broadcaster.Port = port + 1;
        Assert.That((broadcaster.Active, broadcaster.Port), Is.EqualTo((false, port + 1)));
        broadcaster.Port = 70000;
        Assert.That(() => broadcaster.Active = true, Throws.InstanceOf<ArgumentOutOfRangeException>());
        Exception? failure = null;
        broadcaster.OnActivationError = (s, e) => failure = e;
        broadcaster.Active = true;
        Assert.That(failure, Is.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    [Covers(typeof(TBoldAbstractNotificationPropagator), "*")]
    [Covers(typeof(TBoldNotificationPropagatorPersistenceControllerPassthrough), "*")]
    public void Two_object_spaces_stay_in_step_through_UDP_broadcasts()
    {
        if (!BroadcastsComeBack())
        {
            Assert.Inconclusive("UDP broadcasts do not come back on this machine.");
        }

        string file = Path.Combine(Path.GetTempPath(), $"udp-{Guid.NewGuid():N}.db");
        try
        {
            using var b = new UdpClientApplication("B", file, port);
            b.DatabaseHandle.CreateDataBaseSchema();
            b.SystemHandle.Active = true;
            using var a = new UdpClientApplication("A", file, port);
            a.SystemHandle.Active = true;
            Assert.That(a.Broadcaster.Active && b.Broadcaster.Active, Is.True, "the open systems listen");
            Assert.That(a.Broadcaster.ClientId, Is.EqualTo(0));
            Assert.That(b.Persons.Count, Is.EqualTo(0));

            // A new person in A: B's list of persons is invalidated and fetched again.
            var ann = new TPerson(a.System) { FirstName = "Ann", LastName = "Lee" };
            a.System.UpdateDatabase();
            Assert.That(env.PumpUntil(() => b.Persons.Count == 1), Is.True, "B hears of the new person");
            Assert.That(b.Received, Has.Some.EqualTo(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(
                TBoldObjectSpaceSubscriptionType.bsClassChanged, "Person", "", "", null)));
            var annInB = (TPerson)b.Persons[0]!;

            // A changes her: B's copy is invalidated.
            ann.FirstName = "Anne";
            a.System.UpdateDatabase();
            Assert.That(env.PumpUntil(() => annInB.M_FirstName.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid), Is.True,
                "B hears of the change");
            Assert.That(annInB.FirstName, Is.EqualTo("Anne"));
        }
        finally
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
