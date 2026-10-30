using Microsoft.AspNetCore.SignalR;

namespace Bold;

/// <summary>
/// The propagator server's SignalR hub: the propagator, the lock manager and its administration for clients on other
/// machines (decision D8), where Delphi had DCOM. Each SignalR connection has its own
/// <see cref="TBoldPropagatorConnection"/>; when the connection closes, its client is removed at once. Events for a
/// registered client go to it as calls of ReceiveEvents. The methods and results are in
/// <see cref="BoldPropagatorHubContract"/>.
/// </summary>
public sealed class TBoldPropagatorHub(IHubContext<TBoldPropagatorHub> HubContext) : Hub
{
    private const string ConnectionKey = "Bold.PropagatorConnection";

    private TBoldPropagatorConnection Connection
    {
        get
        {
            if (Context.Items.TryGetValue(ConnectionKey, out object? value) && value is TBoldPropagatorConnection connection)
            {
                return connection;
            }

            connection = new TBoldPropagatorConnection(TBoldPropagatorServer.Instance());
            Context.Items[ConnectionKey] = connection;
            return connection;
        }
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue(ConnectionKey, out object? value) && value is TBoldPropagatorConnection connection)
        {
            connection.Disconnect();
        }

        return base.OnDisconnectedAsync(exception);
    }

    public TBoldRegisterClientResult RegisterClient(int LeaseDuration, int PollingInterval, string ClientIDString)
    {
        int result = Connection.RegisterClient(LeaseDuration, PollingInterval, new TBoldHubListener(HubContext, Context.ConnectionId), ClientIDString,
            out int id);
        return new TBoldRegisterClientResult(result, id);
    }

    public TBoldExtendLeaseHubResult ExtendLease(int BoldClientID, int LeaseDuration)
    {
        int result = Connection.ExtendLease(BoldClientID, LeaseDuration, out bool extended);
        return new TBoldExtendLeaseHubResult(result, extended);
    }

    public int UnRegisterClient(int BoldClientID) => Connection.UnRegisterClient(BoldClientID);

    public int SendEvents(int BoldClientID, string[]? Events) => Connection.SendEvents(BoldClientID, Events);

    public int AddSubscriptions(int BoldClientID, string[]? Subscriptions) => Connection.AddSubscriptions(BoldClientID, Subscriptions);

    public int CancelSubscriptions(int BoldClientID, string[]? Subscriptions) => Connection.CancelSubscriptions(BoldClientID, Subscriptions);

    public TBoldGetLocksResult GetLocks(int ClientId, int TimeOut, string[]? RequestedExclusiveLocks, string[]? RequestedSharedLocks)
    {
        bool result = Connection.GetLocks(ClientId, TimeOut, RequestedExclusiveLocks, RequestedSharedLocks, out string[]? held, out string[]? clients);
        return new TBoldGetLocksResult(result, held, clients);
    }

    public int ReleaseLocks(int ClientId, string[]? Locks) => Connection.ReleaseLocks(ClientId, Locks);

    public bool EnsureLocks(int ClientId, string[]? RequestedExclusiveLocks, string[]? RequestedSharedLocks) =>
        Connection.EnsureLocks(ClientId, RequestedExclusiveLocks, RequestedSharedLocks);

    public TBoldClientListResult ListAllClients()
    {
        int result = Connection.ListAllClients(out string[]? clients);
        return new TBoldClientListResult(result, clients);
    }

    public int RemoveLocksForClient(int ClientId, string[]? Locks) => Connection.RemoveLocksForClient(ClientId, Locks);

    public int KillClient(int ClientId) => Connection.KillClient(ClientId);

    public TBoldLocksForClientsResult LocksForClients(string[]? ClientIds)
    {
        int result = Connection.LocksForClients(ClientIds, out string[]? locks, out string[]? durations);
        return new TBoldLocksForClientsResult(result, locks, durations);
    }

    public TBoldClientListResult ListLockingClients()
    {
        int result = Connection.ListLockingClients(out string[]? clients);
        return new TBoldClientListResult(result, clients);
    }

    public bool GetLockManagerSuspended() => Connection.LockManagerSuspended;

    public void SetLockManagerSuspended(bool Value) => Connection.LockManagerSuspended = Value;
}

/// <summary>A registered client's listener on the server: the events go to the client's connection as a call of ReceiveEvents.</summary>
internal sealed class TBoldHubListener(IHubContext<TBoldPropagatorHub> HubContext, string ConnectionId) : IBoldListener
{
    public int ReceiveEvents(string[]? Events)
    {
        try
        {
            HubContext.Clients.Client(ConnectionId).SendCoreAsync(BoldPropagatorHubContract.ReceiveEvents, new object?[] { Events }).GetAwaiter().GetResult();
            return Windows.S_OK;
        }
        catch (Exception)
        {
            return Windows.S_FALSE;
        }
    }
}
