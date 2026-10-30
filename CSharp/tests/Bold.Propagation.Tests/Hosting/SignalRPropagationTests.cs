using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bold.Tests;

/// <summary>Client applications on other machines: the propagator server's SignalR hub and the client handles for it.</summary>
[NonParallelizable]
public class SignalRPropagationTests
{
    private ClientEnvironment env = null!;
    private PropagatorSample propagator = null!;
    private WebApplication? host;
    private string hubUrl = null!;
    private string file = null!;
    private readonly List<PropagationClient> clients = [];

    [SetUp]
    public void Start()
    {
        env = new ClientEnvironment();
        propagator = new PropagatorSample();
        host = StartHost();
        hubUrl = host.Urls.First() + BoldPropagatorHubContract.DefaultPath;
        file = Path.Combine(Path.GetTempPath(), $"signalr-{Guid.NewGuid():N}.db");
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
            StopHost();
            propagator.Dispose();
            env.Dispose();
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    /// <summary>A web host with the hub on a free port of this machine; the propagator server is the sample's.</summary>
    private static WebApplication StartHost()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddBoldPropagatorHub();
        WebApplication app = builder.Build();
        app.MapBoldPropagator();
        // Not on this thread's synchronization context, which only the test pumps.
        Task.Run(() => app.StartAsync()).GetAwaiter().GetResult();
        return app;
    }

    private void StopHost()
    {
        if (host is not null)
        {
            WebApplication app = host;
            host = null;
            Task.Run(async () =>
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }).GetAwaiter().GetResult();
        }
    }

    private PropagationClient Client(string name, bool withRegions = false, bool createSchema = false)
    {
        var client = new PropagationClient(name, file, withRegions, hubUrl);
        clients.Add(client);
        if (createSchema)
        {
            client.DatabaseHandle.CreateDataBaseSchema();
        }

        client.Open();
        return client;
    }

    [Test]
    public void Two_object_spaces_stay_in_step_through_the_propagator_hub()
    {
        PropagationClient a = Client("A", createSchema: true);
        PropagationClient b = Client("B");
        Assert.That(a.PropagatorHandle, Is.TypeOf<TBoldSignalRPropagatorHandle>());
        PropagationScenarios.ObjectSpacesStayInStep(env, a, b);
    }

    [Test]
    public void Lock_conflicts_are_reported_through_the_propagator_hub()
    {
        PropagationClient a = Client("A", withRegions: true, createSchema: true);
        PropagationClient b = Client("B", withRegions: true);
        PropagationScenarios.LockConflictsAreReported(env, a, b);
    }

    private sealed class RecordingListener : IBoldListener
    {
        public readonly List<string> Events = [];

        public int ReceiveEvents(string[]? Events)
        {
            lock (this.Events)
            {
                this.Events.AddRange(Events ?? []);
            }

            return Windows.S_OK;
        }
    }

    [Test]
    public void The_hub_offers_every_method_of_the_propagator_and_the_lock_manager()
    {
        using var hubA = new TBoldHubConnection(hubUrl);
        hubA.Start();
        using var hubB = new TBoldHubConnection(hubUrl);
        hubB.Start();
        var a = new TBoldSignalRPropagatorConnection(hubA);
        var b = new TBoldSignalRPropagatorConnection(hubB);
        var listenerB = new RecordingListener();
        Assert.That(a.RegisterClient(60000, 10, new RecordingListener(), "Ann", out int idA), Is.EqualTo(Windows.S_OK));
        Assert.That(b.RegisterClient(60000, 10, listenerB, "Bob", out int idB), Is.EqualTo(Windows.S_OK));
        Assert.That(propagator.ClientHandler.IsRegistered(idA) && propagator.ClientHandler.IsRegistered(idB), Is.True);
        Assert.That(a.ExtendLease(idA, 60000, out bool extended), Is.EqualTo(Windows.S_OK));
        Assert.That(extended, Is.True);

        // Events go to the subscribed client through the hub.
        Assert.That(b.AddSubscriptions(idB, ["E:1", "E:2"]), Is.EqualTo(Windows.S_OK));
        Assert.That(b.CancelSubscriptions(idB, ["E:2"]), Is.EqualTo(Windows.S_OK));
        propagator.WaitForDequeuer();
        Assert.That(a.SendEvents(idA, ["E:1", "E:2"]), Is.EqualTo(Windows.S_OK));
        Assert.That(SpinWait.SpinUntil(() => { lock (listenerB.Events) { return listenerB.Events.Count > 0; } }, 5000), Is.True);
        Assert.That(listenerB.Events, Is.EqualTo(new[] { "E:1" }));

        // Locks, and their administration.
        Assert.That(a.GetLocks(idA, 60000, ["X"], null, out _, out _), Is.True);
        Assert.That(b.GetLocks(idB, 60000, ["X"], null, out string[]? held, out string[]? holders), Is.False);
        Assert.That((held, holders), Is.EqualTo((new[] { "X" }, new[] { $"{idA}=Ann" })));
        Assert.That(a.EnsureLocks(idA, ["X"], null), Is.True);
        Assert.That(b.ListAllClients(out string[]? all), Is.EqualTo(Windows.S_OK));
        Assert.That(all, Is.EquivalentTo(new[] { $"{idA}=Ann", $"{idB}=Bob" }));
        Assert.That(b.ListLockingClients(out string[]? locking), Is.EqualTo(Windows.S_OK));
        Assert.That(locking, Is.EqualTo(new[] { $"{idA}=Ann" }));
        Assert.That(b.LocksForClients([$"{idA}"], out string[]? locks, out string[]? durations), Is.EqualTo(Windows.S_OK));
        Assert.That((locks, durations?.Length), Is.EqualTo((new[] { $"{idA}=X" }, 1)));
        Assert.That(b.LockManagerSuspended, Is.False);
        b.LockManagerSuspended = true;
        Assert.That(propagator.Server.LockManager.Suspended, Is.True);
        b.LockManagerSuspended = false;
        Assert.That(b.RemoveLocksForClient(idA, ["X"]), Is.EqualTo(Windows.S_OK));
        Assert.That(a.EnsureLocks(idA, ["X"], null), Is.False);
        Assert.That(a.GetLocks(idA, 60000, ["Y"], null, out _, out _), Is.True);
        Assert.That(a.ReleaseLocks(idA, ["Y"]), Is.EqualTo(Windows.S_OK));
        Assert.That(b.KillClient(idA), Is.EqualTo(Windows.S_OK));
        Assert.That(propagator.ClientHandler.IsRegistered(idA), Is.False);
        Assert.That(b.UnRegisterClient(idB), Is.EqualTo(Windows.S_OK));
        Assert.That(propagator.ClientHandler.IsRegistered(idB), Is.False);
    }

    [Test]
    public void A_client_whose_connection_closes_is_removed_at_once_and_a_closed_hub_is_reported()
    {
        // The connection goes: the client goes with it.
        var hub = new TBoldHubConnection(hubUrl);
        hub.Start();
        var connection = new TBoldSignalRPropagatorConnection(hub);
        connection.RegisterClient(60000, 10, new RecordingListener(), "Ann", out int id);
        hub.Dispose();
        Assert.That(SpinWait.SpinUntil(() => !propagator.ClientHandler.IsRegistered(id), 5000), Is.True);
        Assert.That(() => connection.SendEvents(id, ["E"]), Throws.TypeOf<EBold>(), "the connection is closed");

        // The server goes: the propagator handle reports it on the thread that connected it.
        using var handle = new TBoldSignalRPropagatorHandle { Url = hubUrl, TimeOut = 5000 };
        Assert.That(() => new TBoldSignalRPropagatorHandle().Connected = true, Throws.TypeOf<EBold>(), "no Url");
        string? failure = null;
        handle.OnPropagatorCallFailed = (_, message) => failure = message;
        handle.Connected = true;
        Assert.That(handle.ClientHandler, Is.SameAs(handle.Connection));
        Assert.That(handle.EventPropagator, Is.SameAs(handle.Connection));
        StopHost();
        Assert.That(env.PumpUntil(() => failure is not null), Is.True);
        Assert.That(failure, Does.Contain("closed"));
        Assert.That(handle.Connected, Is.True, "until it is disconnected");
        handle.Connected = false;
        Assert.That(() => handle.Connected = true, Throws.TypeOf<EBold>(), "nothing to connect to");
        Assert.That(handle.Connected, Is.False);
    }

    [Test]
    public void The_lock_manager_handles_connect_to_the_hub()
    {
        using var lockManager = new TBoldSignalRLockManagerHandle { Url = hubUrl };
        using var admin = new TBoldSignalRLockManagerAdminHandle { Url = hubUrl };
        Assert.That((lockManager.LockManager, admin.LockManagerAdmin), Is.EqualTo(((IBoldLockManager?)null, (IBoldLockManagerAdmin?)null)));
        lockManager.Connected = true;
        admin.Connected = true;
        Assert.That(lockManager.Connected && admin.Connected, Is.True);
        Assert.That(admin.LockManagerAdmin!.ListAllClients(out string[]? clients), Is.EqualTo(Windows.S_OK));
        Assert.That(clients, Is.Null);
        Assert.That(lockManager.LockManager!.EnsureLocks(1, null, null), Is.True);
        lockManager.Connected = false;
        admin.Connected = false;
        Assert.That(lockManager.LockManager, Is.Null);
    }

    [Test]
    public void A_hub_connection_turns_http_into_ws_and_fails_clearly()
    {
        Assert.That(TBoldHubConnection.WebSocketUri("http://server:5050/propagator").ToString(), Is.EqualTo("ws://server:5050/propagator"));
        Assert.That(TBoldHubConnection.WebSocketUri("https://server/propagator").Scheme, Is.EqualTo("wss"));
        using var nowhere = new TBoldHubConnection(host!.Urls.First() + "/nothing");
        Assert.That(() => nowhere.Start(5000), Throws.TypeOf<EBold>());
        using var hub = new TBoldHubConnection(hubUrl) { InvokeTimeOut = 5000, KeepAliveInterval = 100 };
        Assert.That(() => hub.Invoke<int>("UnRegisterClient", 1), Throws.TypeOf<EBold>(), "not started");
        hub.Start();
        Assert.That(hub.Connected, Is.True);
        Assert.That(() => hub.Start(), Throws.TypeOf<EBold>(), "started already");
        Assert.That(() => hub.Invoke<int>("NoSuchMethod"), Throws.TypeOf<EBold>().With.Message.Contains("NoSuchMethod"));
        Thread.Sleep(300);
        Assert.That(hub.Invoke<TBoldClientListResult>(BoldPropagatorHubContract.ListAllClients)!.Result, Is.EqualTo(Windows.S_OK), "pings keep it");
    }
}
