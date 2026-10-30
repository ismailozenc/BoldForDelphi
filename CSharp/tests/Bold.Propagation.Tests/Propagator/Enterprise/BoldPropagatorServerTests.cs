// Delphi: Source/Propagator/Enterprise/BoldPropagatorServer.pas, BoldAdvancedPropagator.pas, BoldEnqueuer.pas, BoldSubscriptionHandler.pas,
// BoldOutputQueueHandler.pas, BoldPriorityListEnlister.pas, BoldClientNotifierHandler.pas, BoldPropagatorCleanup.pas
// Port map: CSharp/port/units/BoldPropagatorServer.md
namespace Bold.Tests;

[NonParallelizable]
public class BoldPropagatorServerTests
{
    [Test]
    public void Events_go_to_the_other_subscribed_clients_whose_subscriptions_then_end()
    {
        using var p = new PropagatorSample();
        var (a, idA, listenerA) = p.Register("A");
        var (b, idB, listenerB) = p.Register("B");
        var (c, idC, listenerC) = p.Register("C");
        Assert.That(new[] { idA, idB, idC }, Is.Unique);

        Assert.That(b.AddSubscriptions(idB, ["E:1", "C:Person"]), Is.EqualTo(Windows.S_OK));
        Assert.That(c.AddSubscriptions(idC, ["E:1"]), Is.EqualTo(Windows.S_OK));
        p.WaitForDequeuer();
        Assert.That(a.SendEvents(idA, ["E:1", "E:2"]), Is.EqualTo(Windows.S_OK));
        Assert.That(listenerB.WaitFor(1), Is.True);
        Assert.That(listenerC.WaitFor(1), Is.True);
        Assert.That(listenerB.Events, Is.EqualTo(new[] { "E:1" }));
        Assert.That(listenerC.Events, Is.EqualTo(new[] { "E:1" }));
        Assert.That(listenerA.Events, Is.Empty, "the sender gets nothing back");

        // The subscriptions of B and C ended and the sender is subscribed instead.
        listenerB.Clear();
        b.SendEvents(idB, ["E:1"]);
        Assert.That(listenerA.WaitFor(1), Is.True);
        Assert.That(listenerA.Events, Is.EqualTo(new[] { "E:1" }));
        Thread.Sleep(100);
        Assert.That(listenerC.Events, Has.Length.EqualTo(1), "C must subscribe again");

        // A parameter after the delimiter is passed on; the subscription is to the event name before it.
        b.SendEvents(idB, ["C:Person|deleted"]);
        p.WaitForDequeuer();
        a.AddSubscriptions(idA, ["C:Person"]);
        p.WaitForDequeuer();
        c.SendEvents(idC, ["C:Person|x"]);
        Assert.That(listenerA.WaitFor(2), Is.True);
        Assert.That(listenerA.Events[1], Is.EqualTo("C:Person|x"));
        Assert.That(listenerB.WaitFor(1), Is.True, "the sender of an event is subscribed to it");
        Assert.That(listenerB.Events, Is.EqualTo(new[] { "C:Person|x" }));
    }

    [Test]
    public void Unregistered_clients_are_refused_and_leases_run_out()
    {
        using var p = new PropagatorSample();
        var (a, idA, _) = p.Register("A", leaseDuration: 300);
        Assert.That(p.ClientHandler.IsRegistered(idA), Is.True);
        Assert.That(a.SendEvents(idA + 1000, ["E:1"]), Is.EqualTo(BoldPropagatorConstants.E_CLIENT_NOT_REGISTERED));
        Assert.That(a.SendEvents(idA, null), Is.EqualTo(BoldPropagatorConstants.E_INVALID_PARAMETER));

        Assert.That(a.ExtendLease(idA, 300, out bool extended), Is.EqualTo(Windows.S_OK));
        Assert.That(extended, Is.True);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (p.ClientHandler.IsRegistered(idA) && watch.ElapsedMilliseconds < 5000)
        {
            Thread.Sleep(20);
        }

        Assert.That(p.ClientHandler.IsRegistered(idA), Is.False, "the cleanup removed the client whose lease ran out");
        Assert.That(watch.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(150));
        Assert.That(a.ExtendLease(idA, 300, out extended), Is.EqualTo(Windows.S_FALSE));
        Assert.That(extended, Is.False);
    }

    [Test]
    public void Locks_go_to_one_client_at_a_time_and_a_conflict_says_who_holds_them()
    {
        using var p = new PropagatorSample();
        var (a, idA, listenerA) = p.Register("Ann");
        var (b, idB, _) = p.Register("Bob");
        Assert.That(a.GetLocks(idA, 1000, ["X"], ["S"], out string[]? held, out string[]? holders), Is.True);
        Assert.That((held, holders), Is.EqualTo(((string[]?)null, (string[]?)null)));
        Assert.That(listenerA.WaitFor(1), Is.True, "the client is told it got the locks");
        Assert.That(listenerA.Events[0], Is.EqualTo(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsGotLocks, "", "", "", null)));
        Assert.That(a.EnsureLocks(idA, ["X"], ["S"]), Is.True);
        Assert.That(a.EnsureLocks(idA, ["S"], null), Is.False, "S is held shared");

        Assert.That(b.GetLocks(idB, 1000, null, ["S"], out held, out holders), Is.True, "shared locks are shared");
        Assert.That(b.GetLocks(idB, 1000, null, ["X"], out held, out holders), Is.False);
        Assert.That(held, Is.EqualTo(new[] { "X" }));
        Assert.That(holders, Is.EqualTo(new[] { $"{idA}=Ann" }));

        a.ReleaseLocks(idA, ["X"]);
        Assert.That(b.GetLocks(idB, 1000, ["X"], null, out _, out _), Is.True);
        Assert.That(a.UnRegisterClient(idA), Is.EqualTo(Windows.S_OK));
        Assert.That(b.GetLocks(idB, 1000, ["S"], null, out _, out _), Is.True, "Ann's shared lock went when she unregistered");
    }
}
