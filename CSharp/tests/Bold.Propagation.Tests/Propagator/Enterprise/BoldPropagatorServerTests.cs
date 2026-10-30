// Delphi: Source/Propagator/Enterprise/BoldPropagatorServer.pas, BoldAdvancedPropagator.pas, BoldEnqueuer.pas, BoldSubscriptionHandler.pas,
// BoldOutputQueueHandler.pas, BoldPriorityListEnlister.pas, BoldClientNotifierHandler.pas, BoldPropagatorCleanup.pas
// Port map: CSharp/port/units/BoldPropagatorServer.md
namespace Bold.Tests;

[NonParallelizable]
[Covers(typeof(TBoldPropagatorServer), "*")]
[Covers(typeof(TBoldAdvancedPropagator), "*")]
[Covers(typeof(TBoldEnqueuer), "*")]
[Covers(typeof(TBoldExternalEvent), "*")]
[Covers(typeof(TBoldExternalEventType))]
[Covers(typeof(TBoldExternalEventThreadSafeObjectQueue), "*")]
[Covers(typeof(TBoldDequeuer), "*")]
[Covers(typeof(TBoldSubscriptionHandler), "*")]
[Covers(typeof(TClientInfoChangedEvent))]
[Covers(typeof(TGlobalInfoChangedEvent))]
[Covers(typeof(TBoldAbstractOutputQueueHandler), "*")]
[Covers(typeof(TBoldOutputQueueHandler), "*")]
[Covers(typeof(BoldOutputQueueHandler), "*")]
[Covers(typeof(TBoldClientQueue), "*")]
[Covers(typeof(TBoldAbstractPriorityListEnlister), "*")]
[Covers(typeof(TBoldPriorityListEnlister), "*")]
[Covers(typeof(TBoldClientQueueInfo), "*")]
[Covers(typeof(TBoldClientNotifierHandler), "*")]
[Covers(typeof(TBoldClientNotifier), "*")]
[Covers(typeof(TBoldClientNotifierPool), "*")]
[Covers(typeof(TBoldClientNotifierData), "*")]
[Covers(typeof(TBoldCleanUpThread), "*")]
[Covers(typeof(TBoldCleanupSubscriber), "*")]
[Covers(typeof(TBoldLockManager), "*")]
[Covers(typeof(IBoldEventPropagator), "*")]
[Covers(typeof(IBoldClientHandler), "*")]
[Covers(typeof(IBoldListener), "*")]
[Covers(typeof(IBoldLockManager), "*")]
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

    [Test]
    public void A_client_that_cannot_take_its_events_is_disconnected()
    {
        using var p = new PropagatorSample();
        Assert.That(p.Propagator.DisconnectClientsOnSendFailure, Is.True);
        var (a, idA, _) = p.Register("A");
        var (b, idB, listenerB) = p.Register("B");
        b.AddSubscriptions(idB, ["E:1"]);
        p.WaitForDequeuer();
        listenerB.Fail = true;
        a.SendEvents(idA, ["E:1"]);
        Assert.That(SpinWait.SpinUntil(() => !p.ClientHandler.IsRegistered(idB), 5000), Is.True, "B was disconnected");
        Assert.That(p.ClientHandler.IsRegistered(idA), Is.True);
        Assert.That(a.SendEvents(idB, ["E:2"]), Is.EqualTo(BoldPropagatorConstants.E_CLIENT_NOT_REGISTERED));
    }

    [Test]
    public void Cancelled_subscriptions_end_and_the_dequeuer_reports_its_counts()
    {
        using var p = new PropagatorSample();
        var (a, idA, _) = p.Register("A");
        var (b, idB, listenerB) = p.Register("B");
        b.AddSubscriptions(idB, ["E:1", "E:2"]);
        b.CancelSubscriptions(idB, ["E:2"]);
        p.WaitForDequeuer();
        a.SendEvents(idA, ["E:2", "E:1"]);
        Assert.That(listenerB.WaitFor(1), Is.True);
        Thread.Sleep(50);
        Assert.That(listenerB.Events, Is.EqualTo(new[] { "E:1" }));

        TBoldDequeuer dequeuer = p.Propagator.Dequeuer!;
        int[] global = [];
        (int Client, int Events, int Queues) client = default;
        var reported = new ManualResetEventSlim(false);
        dequeuer.OnGlobalInfoChanged = (inQ, added, sent) =>
        {
            global = [inQ, added, sent];
            reported.Set();
        };
        dequeuer.OnClientInfoChanged = (id, events, queues) => client = (id, events, queues);
        dequeuer.PostThreadMessage(BoldPropagatorConstants.BM_REQUEST_CLIENT_INFO, 1, idA);
        dequeuer.PostThreadMessage(BoldPropagatorConstants.BM_REQUEST_GLOBAL_INFO, 0, 0);
        Assert.That(reported.Wait(5000), Is.True);
        Assert.That(global, Is.EqualTo(new[] { 0, 2, 1 }), "nothing waiting, two subscriptions added, one event sent");
        Assert.That(client, Is.EqualTo((idA, 2, 0)), "A is subscribed to its two events, and has nothing queued");
        Assert.That(dequeuer.InQueueCount, Is.EqualTo(0));
        Assert.That(dequeuer.InQueueCountPeak, Is.GreaterThanOrEqualTo(1));
        Assert.That(dequeuer.InQueueCountLast, Is.GreaterThanOrEqualTo(0));
        dequeuer.OnStartDequeue = null;
        dequeuer.OnDoneDequeue = null;
    }

    [Test]
    public void A_lock_that_timed_out_is_taken_from_its_holder_who_is_told()
    {
        using var p = new PropagatorSample();
        var (a, idA, listenerA) = p.Register("A");
        var (b, idB, _) = p.Register("B");
        Assert.That(a.GetLocks(idA, 1, ["X"], null, out _, out _), Is.True);
        Assert.That(listenerA.WaitFor(1), Is.True);
        p.Server.LockManager.HandedLocks.Items(idA, "X")!.EnsureLock(false);
        Thread.Sleep(20);
        Assert.That(b.GetLocks(idB, 1000, ["X"], null, out _, out _), Is.True, "A's lock timed out");
        Assert.That(listenerA.WaitFor(2), Is.True);
        Assert.That(listenerA.Events[1], Is.EqualTo(TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsLockLost, "", "", "X", null)));
        Assert.That(a.EnsureLocks(idA, ["X"], null), Is.False);
    }

    [Test]
    [Covers(typeof(TBoldLockManagerAdmin), "*")]
    [Covers(typeof(IBoldLockManagerAdmin), "*")]
    [Covers(typeof(TBoldAbstractLockManagerAdminHandle), "*")]
    public void The_lock_manager_administration_lists_clients_and_locks_and_can_suspend_and_kill()
    {
        using var p = new PropagatorSample();
        var (a, idA, _) = p.Register("Ann");
        var (b, idB, _) = p.Register("Bob");
        a.GetLocks(idA, 1000, ["X"], ["S"], out _, out _);
        using var adminHandle = new TBoldInProcessLockManagerAdminHandle();
        IBoldLockManagerAdmin admin = adminHandle.LockManagerAdmin!;
        Assert.That(admin.ListAllClients(out string[]? all), Is.EqualTo(Windows.S_OK));
        Assert.That(all, Is.EqualTo(new[] { $"{idA}=Ann", $"{idB}=Bob" }));
        admin.ListLockingClients(out string[]? locking);
        Assert.That(locking, Is.EqualTo(new[] { $"{idA}=Ann" }));
        admin.LocksForClients([$"{idA}=Ann", $"{idB}"], out string[]? locks, out string[]? durations);
        Assert.That(locks, Is.EquivalentTo(new[] { $"{idA}=X", $"{idA}=S" }));
        Assert.That(durations, Has.All.Match(@"^\d\d:\d\d:\d\d:\d\d\d$"));
        Assert.That(p.Server.LockManagerAdmin.LockManager, Is.SameAs(p.Server.LockManager));

        admin.LockManagerSuspended = true;
        Assert.That(admin.LockManagerSuspended && p.Server.LockManager.Suspended, Is.True);
        Assert.That(b.GetLocks(idB, 1000, ["Y"], null, out _, out _), Is.False, "a suspended lock manager gives no locks");
        Assert.That(b.EnsureLocks(idB, null, null), Is.False);
        admin.LockManagerSuspended = false;

        admin.RemoveLocksForClient(idA, ["X"]);
        Assert.That(a.EnsureLocks(idA, ["X"], null), Is.False);
        Assert.That(p.Server.LockManager.HasLocks(idA), Is.True, "Ann still holds S");
        Assert.That(admin.KillClient(idA), Is.EqualTo(Windows.S_OK));
        Assert.That(p.ClientHandler.IsRegistered(idA), Is.False);
        Assert.That(p.Server.LockManager.HasLocks(idA), Is.False, "a killed client's locks go");
        Assert.That(admin.KillClient(idA), Is.EqualTo(Windows.S_FALSE));
        Assert.That(() => p.Server.LockManagerAdmin.LocksForClients(null, new TStringList(), new TStringList()), Throws.TypeOf<EBold>());
    }

    [Test]
    [Covers(typeof(BoldPropagatorApplication), "*")]
    [Covers(typeof(TBoldPropagatorSvc), "*")]
    public void The_server_reads_its_configuration_and_starts_as_an_application_or_a_hosted_service()
    {
        TBoldPropagatorServer.FreeSingleton();
        string ini = Path.Combine(Path.GetTempPath(), $"propagator-{Guid.NewGuid():N}.ini");
        File.WriteAllText(ini, "[FILE LOGGING]\r\nENABLED=0\r\nMAXFILESIZE=1000\r\n[CONFIG PARAMS]\r\nTHREADPOOLSIZE=2\r\nDISCONNECTCLIENTSONSENDFAILURE=0\r\n[COM SERVER]\r\nSERVERNAME=Mine\r\n");
        try
        {
            TBoldPropagatorServer server = TBoldPropagatorServer.Instance();
            Assert.That(TBoldPropagatorServer.Instance(), Is.SameAs(server), "a singleton");
            Assert.That(server.ModuleName, Is.Not.Empty);
            Assert.That(server.ConfigFileName, Is.EqualTo(SysUtils.ChangeFileExt(server.ModuleName, ".ini")));
            server.ConfigFileName = ini;
            BoldPropagatorApplication.Initialization();
            Assert.That(server.Initialized, Is.True);
            Assert.That(server.ServerName, Is.EqualTo("Mine"));
            Assert.That(server.AdvancedPropagator!.Initialized, Is.True);
            Assert.That(server.AdvancedPropagator.DisconnectClientsOnSendFailure, Is.False);
            Assert.That(server.AdvancedPropagator.ClientNotifierHandler, Is.Not.Null);
            Assert.That(server.AdvancedPropagator.CleanUpSubscriber, Is.Not.Null);
            BoldPropagatorApplication.Finalization();
            Assert.That(TBoldPropagatorServer.Instance(), Is.Not.SameAs(server), "freed");

            var service = new TBoldPropagatorSvc();
            TBoldPropagatorServer.Instance().ConfigFileName = ini;
            service.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(TBoldPropagatorServer.Instance().Initialized, Is.True);
            server = TBoldPropagatorServer.Instance();
            service.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(server.Initialized, Is.False);
        }
        finally
        {
            TBoldPropagatorServer.FreeSingleton();
            File.Delete(ini);
        }
    }

    [Test]
    [Covers(typeof(TBoldInternalPropagator), "*")]
    [Covers(typeof(TBoldClientPropagator), "*")]
    public void The_low_end_propagator_sends_every_event_to_all_other_clients()
    {
        TBoldInternalPropagator.FreeInternalPropagator();
        var shown = new List<int>();
        TBoldInternalPropagator.InternalPropagator.OnRefreshDisplay = (count, _) => shown.Add(count);
        var a = new TBoldClientPropagator();
        var b = new TBoldClientPropagator();
        var listenerA = new RecordingListener();
        var listenerB = new RecordingListener();
        Assert.That(a.RegisterClient(0, 0, listenerA, "A", out int idA), Is.EqualTo(Windows.S_OK));
        Assert.That(b.RegisterClient(0, 0, listenerB, "B", out int idB), Is.EqualTo(Windows.S_OK));
        Assert.That((idA, idB), Is.EqualTo((1, 2)));
        Assert.That(a.AddSubscriptions(idA, ["E:1"]), Is.EqualTo(Windows.S_OK), "subscriptions are not needed");
        Assert.That(a.SendEvents(idA, ["E:5"]), Is.EqualTo(Windows.S_OK));
        Assert.That(listenerB.Events, Is.EqualTo(new[] { "E:5" }));
        Assert.That(listenerA.Events, Is.Empty);
        Assert.That(a.ExtendLease(idA, 1000, out _), Is.EqualTo(Windows.S_OK));
        Assert.That(a.CancelSubscriptions(idA, ["E:1"]), Is.EqualTo(Windows.S_OK));
        Assert.That(a.UnRegisterClient(idA), Is.EqualTo(Windows.S_OK));
        Assert.That(TBoldInternalPropagator.InternalPropagator.PublicClientList[0], Is.Null);
        var c = new TBoldClientPropagator();
        c.RegisterClient(0, 0, new RecordingListener(), "C", out int idC);
        Assert.That(idC, Is.EqualTo(1), "a new client takes a free slot");
        Assert.That(shown, Is.EqualTo(new[] { 1, 2, 1, 2 }));
        Assert.That(c.UnRegisterClient(99), Is.EqualTo(Windows.S_FALSE));
        TBoldInternalPropagator.FreeInternalPropagator();
    }
}
