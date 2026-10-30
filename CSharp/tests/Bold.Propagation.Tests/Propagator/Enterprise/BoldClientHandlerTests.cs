// Delphi: Source/Propagator/Enterprise/BoldClientHandler.pas
// Port map: CSharp/port/units/BoldClientHandler.md
namespace Bold.Tests;

[Covers(typeof(TBoldClientHandler), "*")]
[Covers(typeof(TBoldClientInfo), "*")]
[Covers(typeof(TBoldClientInfoList), "*")]
[Covers(typeof(TBoldClientReceiveStatus))]
[Covers(typeof(TBoldRemoveClientReasonType))]
public class BoldClientHandlerTests
{
    /// <summary>A client handler on its own: the removed clients' queues are recorded instead of going to a dequeuer.</summary>
    private sealed class Handler : TBoldClientHandler
    {
        public readonly List<int> RemovedQueues = [];
        protected override void EnqueueRemoveClientQueueEvent(TBoldClientID ClientId) => RemovedQueues.Add(ClientId);
    }

    private sealed class InfoList : TBoldClientInfoList
    {
        public int Free() => GetFreeClientID();
        public void Return(int ClientID) => ReturnFreeClientID(ClientID);
    }

    private static List<(int Event, object?[] Args)> Record(TBoldClientHandler handler, params int[] events)
    {
        var received = new List<(int, object?[])>();
        var subscriber = new TBoldPassthroughSubscriber((_, e, _, args) => received.Add((e, args)));
        foreach (int e in events)
        {
            handler.AddSubscription(subscriber, e, e);
        }

        return received;
    }

    [Test]
    public void Clients_register_extend_their_leases_and_unregister()
    {
        PortAssert.EnumsMatchDelphi("Source/Propagator/Enterprise/BoldClientHandler.pas", typeof(TBoldClientReceiveStatus), typeof(TBoldRemoveClientReasonType));
        using var handler = new Handler();
        var events = Record(handler, BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_REGISTERED, BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_UNREGISTERED,
            BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_REMOVED, BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_LEASE_CHANGED);
        Assert.That(handler.Enabled, Is.True);
        var listener = new RecordingListener();
        Assert.That(handler.RegisterClient(60000, 100, listener, "Ann", out int ann, out TTimeStamp annTime), Is.EqualTo(Windows.S_OK));
        Assert.That(handler.RegisterClient(1000, 100, new RecordingListener(), "Bob", out int bob, out TTimeStamp bobTime), Is.EqualTo(Windows.S_OK));
        Assert.That((ann, bob), Is.EqualTo((0, 1)), "ids start at 0");
        Assert.That(handler.NOConnectedClients, Is.EqualTo(2));
        Assert.That(handler.IsRegistered(ann) && handler.IsRegistered(bob), Is.True);
        Assert.That(handler.IsRegistered(5) || handler.IsRegistered(-1) || handler.IsRegistered(100000), Is.False);
        Assert.That(events.Select(e => e.Event), Does.Contain(BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_REGISTERED));
        (int Event, object?[] Args) leaseChanged = events.Last(e => e.Event == BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_LEASE_CHANGED);
        Assert.That(leaseChanged.Args[0], Is.EqualTo(bob), "Bob's lease runs out first");

        Assert.That(handler.HasInfoForClient(ann, out string name, out int lease, out int polling, out TTimeStamp timeout, out bool initialized), Is.True);
        Assert.That((name, lease, polling, initialized), Is.EqualTo(("Ann", 60000, 100, true)));
        Assert.That(SysUtils.TimeStampToDateTime(timeout), Is.GreaterThan(DateTime.Now.AddSeconds(50)));
        Assert.That(handler.HasInfoForClient(ann, out name, out TTimeStamp registered, out initialized, out TBoldClientReceiveStatus status), Is.True);
        Assert.That(BoldUtils.TimeStampComp(registered, annTime), Is.EqualTo(0));
        Assert.That(status, Is.EqualTo(TBoldClientReceiveStatus.crsOK));
        Assert.That(handler.GetRegistrationTime(bob, out TTimeStamp bobRegistered) && BoldUtils.TimeStampComp(bobRegistered, bobTime) == 0, Is.True);
        Assert.That(handler.GetListener(ann, annTime, out IBoldListener? found) && found == listener, Is.True);
        Assert.That(handler.GetListener(ann, bobTime, out _) && BoldUtils.TimeStampComp(annTime, bobTime) != 0, Is.False, "the listener of a client registered then");
        Assert.That(handler.IsThereAClientTimingOutSoon(out int soon, out _, out _), Is.True);
        Assert.That(soon, Is.EqualTo(bob));

        var ids = new TStringList();
        handler.GetRegisteredClientIDs(ids);
        Assert.That(ids.ToArray(), Is.EqualTo(new[] { $"{ann}=Ann", $"{bob}=Bob" }));
        var infos = new TStringList();
        handler.GetRegisteredClientInfos(infos);
        Assert.That(infos[0], Is.EqualTo("TotalClients=2,PeakClients=2,TotalLostEvents=0"));
        Assert.That(infos[1], Does.StartWith($"ID={ann},IDString=Ann,\"RegistrationTime="));
        Assert.That(infos[1], Does.Contain("\"LongestInterval=1899-12-30 00:00:00\""), "a TDateTime difference, as in Delphi");
        Assert.That(infos[1], Does.EndWith("LostEvents=0,Status=OK"));

        Assert.That(handler.ExtendLease(bob, 120000, out bool extended), Is.EqualTo(Windows.S_OK));
        Assert.That(extended, Is.True);
        Assert.That(handler.IsThereAClientTimingOutSoon(out soon, out _, out _) && soon == ann, Is.True, "Ann's lease now runs out first");
        Assert.That(handler.ExtendLease(42, 1000, out extended), Is.EqualTo(Windows.S_FALSE));
        Assert.That(extended, Is.False);

        // Unregistering needs the registration time of the client (a later client with the id is left alone).
        Assert.That(handler.UnRegisterClient(ann, bobTime), Is.EqualTo(BoldUtils.TimeStampComp(annTime, bobTime) == 0 ? Windows.S_OK : Windows.S_FALSE));
        Assert.That(handler.UnRegisterClient(ann, annTime), Is.AnyOf(Windows.S_OK, Windows.S_FALSE));
        Assert.That(handler.IsRegistered(ann), Is.False);
        Assert.That(handler.RemovedQueues, Is.EqualTo(new[] { ann }), "the removed client's queue goes");
        Assert.That(events.Select(e => e.Event), Does.Contain(BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_UNREGISTERED));
        Assert.That(handler.NOConnectedClients, Is.EqualTo(1));
        Assert.That(handler.LockTime(), Is.EqualTo(TimeSpan.Zero), "the lock is free");

        // Freed ids are handed out again only after the others.
        Assert.That(handler.RegisterClient(1000, 100, new RecordingListener(), "Cid", out int cid, out _), Is.EqualTo(Windows.S_OK));
        Assert.That(cid, Is.EqualTo(2));
    }

    [Test]
    public void Statistics_status_disconnects_and_expired_leases()
    {
        using var handler = new Handler();
        var events = Record(handler, BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_CONNECTION_LOST, BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_LEASE_EXPIRED);
        var annListener = new RecordingListener();
        handler.RegisterClient(60000, 100, annListener, "Ann", out int ann, out TTimeStamp annTime);
        handler.RegisterClient(1, 100, new RecordingListener(), "Bob", out int bob, out TTimeStamp bobTime);

        handler.MarkFailedToReceiveEvents(ann, 3);
        handler.HasInfoForClient(ann, out _, out TTimeStamp _, out _, out TBoldClientReceiveStatus status);
        Assert.That(status, Is.EqualTo(TBoldClientReceiveStatus.crsNotReceiving));
        handler.MarkHasReceivedEvents(ann, 2);
        handler.HasInfoForClient(ann, out _, out TTimeStamp _, out _, out status);
        Assert.That(status, Is.EqualTo(TBoldClientReceiveStatus.crsRecovered));
        var infos = new TStringList();
        handler.GetRegisteredClientInfos(infos);
        Assert.That(infos[0], Is.EqualTo("TotalClients=2,PeakClients=2,TotalLostEvents=3"));
        Assert.That(infos[1], Does.Contain("Packages=1,Events=2,"));
        Assert.That(infos[1], Does.EndWith("LostEvents=3,Status=Recovered"));

        handler.SendDisconnectRequest(ann, "bye", 500);
        Assert.That(annListener.Disconnects, Is.EqualTo(new[] { ("bye", 500) }));
        Assert.That(handler.DisconnectClient(ann, annTime), Is.True);
        Assert.That(handler.DisconnectClient(ann, annTime), Is.False, "already gone");
        Assert.That(events.Select(e => e.Event), Is.EqualTo(new[] { BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_CONNECTION_LOST }));

        Thread.Sleep(20);
        handler.RemoveExpiredLease(bob, annTime);
        Assert.That(handler.IsRegistered(bob) || BoldUtils.TimeStampComp(annTime, bobTime) == 0, Is.True, "not the registration of Bob");
        handler.RemoveExpiredLease(bob, bobTime);
        Assert.That(handler.IsRegistered(bob), Is.False, "Bob's lease of 1 ms ran out");
        Assert.That(events.Select(e => e.Event), Does.Contain(BoldPropagatorConstants.BOLD_PROPAGATOR_CLIENT_LEASE_EXPIRED));
        Assert.That(handler.IsThereAClientTimingOutSoon(out _, out _, out _), Is.False, "no clients");

        // Disabling drops the clients and refuses everything.
        handler.RegisterClient(60000, 100, new RecordingListener(), "Cid", out int cid, out _);
        handler.Enabled = false;
        Assert.That(handler.IsRegistered(cid), Is.False);
        Assert.That(handler.RegisterClient(60000, 100, new RecordingListener(), "Dan", out _, out _), Is.EqualTo(Windows.S_FALSE));
    }

    [Test]
    public void The_client_info_list_grows_and_hands_out_free_ids_in_turn()
    {
        using var list = new InfoList();
        Assert.That(list.Count, Is.EqualTo(0));
        Assert.That(list.GetExistingClientInfo(0), Is.Null);
        int first = list.Free();
        Assert.That(first, Is.EqualTo(0));
        Assert.That(list.Count, Is.GreaterThanOrEqualTo(BoldPropagatorConstants.MINIMAL_FREE_CLIENTID_COUNT));
        var info = new TBoldClientInfo(first);
        list[first] = info;
        Assert.That(list[first], Is.SameAs(info));
        Assert.That(list.GetExistingClientInfo(first), Is.SameAs(info));
        Assert.That(list.IndexOf(info), Is.EqualTo(first));
        Assert.That(list.Free(), Is.EqualTo(1));
        list.Return(first);
        Assert.That(list.Free(), Is.EqualTo(2), "a returned id waits behind the others");
        Assert.That(list[100000], Is.Null, "reading an id grows the list");
        Assert.That(list.Count, Is.GreaterThan(100000));
        var other = new TBoldClientInfo(9);
        list.Insert(0, other);
        Assert.That(list[0], Is.SameAs(other));
        list.Delete(0);
        Assert.That(list.Add(other), Is.EqualTo(list.Count - 1));
        Assert.That(list.Last(), Is.SameAs(other));
        Assert.That(list.Remove(other), Is.GreaterThanOrEqualTo(0));
        list.Clear();
        Assert.That(list.Count, Is.EqualTo(0));

        var client = new TBoldClientInfo(5);
        Assert.That((client.ClientID, client.Initialized, client.ClientStatusString), Is.EqualTo((5, false, "OK")));
        client.Initialize(1000, 50, new RecordingListener(), "Eve");
        Assert.That((client.LeaseDuration, client.PollingInterval, client.ClientIdentifierString, client.Initialized), Is.EqualTo((1000, 50, "Eve", true)));
        Assert.That(SysUtils.TimeStampToDateTime(client.RegistrationTime), Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(5)));
        client.ExtendLease(2 * 24 * 3600 * 1000);
        Assert.That(SysUtils.TimeStampToDateTime(client.LeaseTimeOut), Is.GreaterThan(DateTime.Now.AddDays(1.9)), "leases of days work");
        client.UnInitialize();
        Assert.That((client.Initialized, client.LeaseDuration), Is.EqualTo((false, 0)));
        client.Dispose();
    }
}
