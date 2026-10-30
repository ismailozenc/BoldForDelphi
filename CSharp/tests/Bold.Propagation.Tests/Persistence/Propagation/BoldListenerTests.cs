// Delphi: Source/Persistence/Propagation/BoldListenerCOM.pas, BoldListenerThread.pas, BoldListenerHandle.pas
// Port map: CSharp/port/units/BoldListenerHandle.md
namespace Bold.Tests;

[NonParallelizable]
public class BoldListenerTests
{
    private ClientEnvironment env = null!;
    private PropagatorSample propagator = null!;

    [SetUp]
    public void Start()
    {
        env = new ClientEnvironment();
        propagator = new PropagatorSample();
    }

    [TearDown]
    public void Stop()
    {
        propagator.Dispose();
        env.Dispose();
    }

    private sealed class Dequeuer : TBoldAbstractDequeuer
    {
        public readonly List<string> Handled = [];
        protected override void HandleMessage(string aMsg) => Handled.Add(aMsg);
    }

    [Test]
    [Covers(typeof(TBoldListenerHandle), "*")]
    [Covers(typeof(TBoldListenerThread), "*")]
    [Covers(typeof(TBoldListenerCOM), "*")]
    [Covers(typeof(IBoldListenerAdmin), "*")]
    [Covers(typeof(TBoldMessageEvent))]
    [Covers(typeof(TBoldAbstractPropagatorHandle), "*")]
    public void A_listener_registers_receives_events_for_its_dequeuer_and_unregisters()
    {
        using var propagatorHandle = new TBoldInProcessPropagatorHandle();
        using var dequeuer = new Dequeuer();
        using var listener = new TBoldListenerHandle
        {
            PropagatorHandle = propagatorHandle,
            Dequeuer = dequeuer,
            PollingInterval = 10,
            LeaseDuration = 60000,
            ClientIdentifierString = "Listener",
        };
        Assert.That((listener.AutoStart, listener.ExtendLeaseAfter, listener.AutoExtendLease), Is.EqualTo((true, BoldPropagatorConstants.DEFAULT_EXTEND_LEASE_AFTER, false)));
        Assert.That(() => listener.ExtendLeaseAfter = 95, Throws.TypeOf<EBold>(), "between 10 and 90");
        Assert.That(listener.HandledObject, Is.SameAs(listener.ListenerThread));
        Assert.That(dequeuer.Queue, Is.SameAs(listener.ListenerThread.Queue));
        Assert.That(listener.Connected, Is.False);
        Assert.That(() => listener.StartListenerThread(), Throws.TypeOf<EBold>(), "the propagator handle is not connected");

        // Activating does nothing until the propagator handle connects; then the listener registers.
        listener.SetActive(true);
        Assert.That(listener.Connected, Is.False);
        propagatorHandle.Connected = true;
        Assert.That(listener.Connected, Is.True);
        int id = listener.BoldClientID;
        Assert.That(propagator.ClientHandler.IsRegistered(id), Is.True);
        Assert.That(propagator.ClientHandler.HasInfoForClient(id, out string name, out int lease, out int polling, out TTimeStamp _, out bool _), Is.True);
        Assert.That((name, lease, polling), Is.EqualTo(("Listener", 60000, 10)));
        Assert.That(listener.ListenerThread.Propagator, Is.SameAs(propagatorHandle.Connection));

        // Events from the propagator are queued and handed to the dequeuer on the handle's thread.
        var (sender, senderId, _) = propagator.Register("Sender");
        propagatorHandle.EventPropagator!.AddSubscriptions(id, ["E:1"]);
        propagator.WaitForDequeuer();
        sender.SendEvents(senderId, ["E:1"]);
        Assert.That(env.PumpUntil(() => dequeuer.Handled.Count == 1), Is.True);
        Assert.That(dequeuer.Handled, Is.EqualTo(new[] { "E:1" }));
        Assert.That(env.Triggers, Is.GreaterThan(0), "the Bold queue was triggered");

        // A message the listener's OnMessage handles is not queued.
        listener.ListenerThread.OnMessage = m => m == "E:2";
        Assert.That(listener.ListenerThread.OnMessage, Is.Not.Null);
        propagatorHandle.EventPropagator.AddSubscriptions(id, ["E:2", "E:3"]);
        propagator.WaitForDequeuer();
        sender.SendEvents(senderId, ["E:2", "E:3"]);
        Assert.That(env.PumpUntil(() => dequeuer.Handled.Count == 2), Is.True);
        Assert.That(dequeuer.Handled[1], Is.EqualTo("E:3"));
        listener.ListenerThread.OnMessage = null;

        Assert.That(listener.ExtendLease(), Is.True);
        var queued = new TStringList();
        listener.DebugGetQueueContents(queued);
        Assert.That(queued.Count, Is.EqualTo(0));

        // Disconnecting the propagator handle unregisters and suspends the listener; connecting starts it again.
        propagatorHandle.Connected = false;
        Assert.That(listener.ListenerThread.Suspended, Is.True);
        Assert.That(propagator.ClientHandler.IsRegistered(id), Is.False);
        propagatorHandle.Connected = true;
        Assert.That(listener.Connected, Is.True);
        Assert.That(listener.BoldClientID, Is.Not.EqualTo(id), "a new registration");
        listener.SetActive(false);
        Assert.That(listener.Connected, Is.False);
        Assert.That(() => listener.PropagatorHandle = null, Throws.Nothing);
    }

    [Test]
    [Covers(typeof(TBoldExtendLeaseResult))]
    [Covers(typeof(TBoldExtendLeaseFailureEvent))]
    [Covers(typeof(EBoldPropagatorTypeLib))]
    public void The_lease_is_extended_on_a_timer_and_a_disconnected_client_stays_away()
    {
        PortAssert.EnumsMatchDelphi("Source/Persistence/Propagation/BoldListenerCOM.pas", typeof(TBoldExtendLeaseResult));
        using var propagatorHandle = new TBoldInProcessPropagatorHandle { Connected = true };
        using var dequeuer = new Dequeuer();
        var failures = new List<TBoldExtendLeaseResult>();
        using var listener = new TBoldListenerHandle
        {
            PropagatorHandle = propagatorHandle,
            Dequeuer = dequeuer,
            LeaseDuration = 400,
            ExtendLeaseAfter = 50,
            AutoExtendLease = true,
            OnExtendLeaseFailed = (res, _) => failures.Add(res),
        };
        listener.SetActive(true);
        int id = listener.BoldClientID;
        Thread.Sleep(1200);
        Assert.That(propagator.ClientHandler.IsRegistered(id), Is.True, "the lease of 400 ms was kept extended");
        Assert.That(failures, Is.Empty);

        // A denied extension leaves the listener unregistered and is reported on the handle's thread.
        propagator.ClientHandler.Enabled = false;
        Assert.That(env.PumpUntil(() => failures.Count > 0), Is.True);
        Assert.That(failures[0], Is.EqualTo(TBoldExtendLeaseResult.elrDenied));
        Assert.That(listener.Connected, Is.False);
        propagator.ClientHandler.Enabled = true;
        listener.StartListenerThread();
        Assert.That(listener.Connected, Is.True, "registered again");
        id = listener.BoldClientID;

        // The propagator disconnects the client for a while: a DISCONNECT message is queued and registering waits.
        propagator.ClientHandler.SendDisconnectRequest(id, "maintenance", 60000);
        Assert.That(env.PumpUntil(() => dequeuer.Handled.Count == 1), Is.True);
        Assert.That(dequeuer.Handled[0], Is.EqualTo("DISCONNECT:60000:maintenance"));
        listener.SuspendListenerThread();
        Assert.That(listener.Connected, Is.False);
        var registrationFailed = false;
        listener.OnRegistrationFailed = _ => registrationFailed = true;
        listener.StartListenerThread();
        Assert.That(registrationFailed, Is.True, "the listener stays away for the time asked");
        Assert.That(listener.Connected, Is.False);
    }

    [Test]
    public void A_listener_on_its_own_registers_with_a_propagator_and_queues_what_it_receives()
    {
        var queue = new TBoldThreadSafeStringQueue("Test");
        var notified = 0;
        using var host = new TBoldNotifiableThread(false);
        using var listener = new TBoldListenerCOM(queue, 10, 60000, 80, false, "Solo", host)
        {
            QueueNotEmptyNotifyEvent = _ => notified++,
            Propagator = new TBoldPropagatorConnection(propagator.Server),
        };
        Assert.That((listener.PollingInterval, listener.LeaseDuration, listener.ExtendLeaseAfter, listener.AutoExtendLease), Is.EqualTo((10, 60000, 80, false)));
        Assert.That(listener.ExtendLease(), Is.EqualTo(TBoldExtendLeaseResult.elrNotRegistered));
        listener.RegisterWithPropagator();
        Assert.That(listener.Registered, Is.True);
        Assert.That(listener.ClientID, Is.Not.EqualTo(BoldDefs.NOTVALIDCLIENTID));
        Assert.That(listener.Ping(), Is.EqualTo(1));
        Assert.That(listener.ReceiveEvents(["a", "b"]), Is.EqualTo(Windows.S_OK));
        Assert.That(notified, Is.EqualTo(1));
        var contents = new TStringList();
        listener.DebugGetQueueContents(contents);
        Assert.That(contents.Count, Is.EqualTo(2));
        Assert.That(listener.ExtendLease(), Is.EqualTo(TBoldExtendLeaseResult.elrExtended));
        Assert.That(listener.ExtendErrorMessage, Is.Empty);
        listener.LeaseDuration = 1000;
        listener.ExtendLeaseAfter = 20;
        listener.ClientIdentifierString = "Solo2";
        Assert.That((listener.LeaseDuration, listener.ExtendLeaseAfter, listener.ClientIdentifierString), Is.EqualTo((1000, 20, "Solo2")));
        listener.UnregisterWithPropagator();
        Assert.That(listener.Registered, Is.False);
        listener.Propagator = null;
        listener.RegisterWithPropagator();
        Assert.That(listener.Registered, Is.False, "nothing to register with");
        host.Quit(true);
    }
}
