namespace Bold.Tests;

/// <summary>The handles and the connection for a propagator server in the same process.</summary>
[NonParallelizable]
public class InProcessHandlesTests
{
    private sealed class Listener : IBoldListener
    {
        public int ReceiveEvents(string[]? Events) => Windows.S_OK;
    }

    [Test]
    public void A_propagator_handle_connects_only_to_an_initialized_server_and_reports_failed_calls()
    {
        TBoldPropagatorServer.FreeSingleton();
        using (var handle = new TBoldInProcessPropagatorHandle())
        {
            Assert.That(() => handle.Connected = true, Throws.TypeOf<EBold>().With.Message.Contains("not initialized"));
            TBoldPropagatorServer.FreeSingleton();
        }

        using var propagator = new PropagatorSample();
        var events = new List<int>();
        using var propagatorHandle = new TBoldInProcessPropagatorHandle();
        var subscriber = new TBoldPassthroughSubscriber((_, e, _) => events.Add(e));
        foreach (int e in new[] { BoldDefs.beConnected, BoldDefs.beDisconnecting, BoldDefs.beDisconnected })
        {
            propagatorHandle.AddSubscription(subscriber, e, 0);
        }

        propagatorHandle.Connected = true;
        Assert.That((propagatorHandle.Connected, propagatorHandle.Connection?.Server), Is.EqualTo((true, propagator.Server)));
        Assert.That(propagatorHandle.ClientHandler, Is.SameAs(propagatorHandle.Connection));
        Assert.That(propagatorHandle.EventPropagator, Is.SameAs(propagatorHandle.Connection));
        propagatorHandle.Connected = false;
        Assert.That(propagatorHandle.Connection, Is.Null);
        Assert.That(events, Is.EqualTo(new[] { BoldDefs.beConnected, BoldDefs.beDisconnecting, BoldDefs.beDisconnected }));

        (object? Sender, string Message)? failure = null;
        propagatorHandle.DoPropagatorCallFailed(this, "no answer");
        propagatorHandle.OnPropagatorCallFailed = (s, m) => failure = (s, m);
        propagatorHandle.DoPropagatorCallFailed(this, "no answer");
        Assert.That(failure, Is.EqualTo(((object?)this, "no answer")));
        subscriber.Dispose();
    }

    [Test]
    public void A_connection_remembers_its_client_and_can_disconnect_it()
    {
        using var propagator = new PropagatorSample();
        var connection = new TBoldPropagatorConnection();
        Assert.That(connection.Server, Is.SameAs(propagator.Server));
        Assert.That(connection.Disconnect(), Is.False, "no client yet");
        Assert.That(connection.RegisterClient(60000, 10, new Listener(), "Ann", out int id), Is.EqualTo(Windows.S_OK));
        Assert.That(connection.ClientId, Is.EqualTo(id));
        Assert.That(connection.Disconnect(), Is.True);
        Assert.That(propagator.ClientHandler.IsRegistered(id), Is.False);

        Assert.That(connection.RegisterClient(60000, 10, new Listener(), "Ann", out id), Is.EqualTo(Windows.S_OK));
        Assert.That(connection.UnRegisterClient(id), Is.EqualTo(Windows.S_OK));
        Assert.That(connection.ClientId, Is.EqualTo(BoldDefs.NOTVALIDCLIENTID));

        using var lockManagerHandle = new TBoldInProcessLockManagerHandle { Connected = true };
        Assert.That(lockManagerHandle.LockManager, Is.Not.Null);
        lockManagerHandle.Connected = false;
        Assert.That(lockManagerHandle.LockManager, Is.Null);
    }
}
