using System.Text.Json;
using static Bold.BoldPropagatorHubContract;

namespace Bold;

/// <summary>
/// The propagator's interfaces over a connection to the propagator hub: each call is an invocation of the hub's method
/// of the same name, and the hub's calls of ReceiveEvents go to the listener that registered. This is what Delphi's
/// DCOM proxies were for the clients. A call raises <see cref="EBold"/> when the connection fails.
/// </summary>
public sealed class TBoldSignalRPropagatorConnection : IBoldClientHandler, IBoldEventPropagator, IBoldLockManager, IBoldLockManagerAdmin
{
    private volatile IBoldListener? fListener;

    public TBoldSignalRPropagatorConnection(TBoldHubConnection Hub)
    {
        this.Hub = Hub;
        Hub.On(BoldPropagatorHubContract.ReceiveEvents,
            args => fListener?.ReceiveEvents(args.Length > 0 ? args[0].Deserialize<string[]?>() : null));
    }

    public TBoldHubConnection Hub { get; }

    public int RegisterClient(int LeaseDuration, int PollingInterval, IBoldListener BoldClientListener, string ClientIDString, out int BoldClientID)
    {
        // The listener is known before the hub can send it events.
        fListener = BoldClientListener;
        TBoldRegisterClientResult result = Hub.Invoke<TBoldRegisterClientResult>(BoldPropagatorHubContract.RegisterClient, LeaseDuration, PollingInterval,
            ClientIDString)!;
        BoldClientID = result.BoldClientID;
        return result.Result;
    }

    public int ExtendLease(int BoldClientID, int LeaseDuration, out bool ExtensionOK)
    {
        TBoldExtendLeaseHubResult result = Hub.Invoke<TBoldExtendLeaseHubResult>(BoldPropagatorHubContract.ExtendLease, BoldClientID, LeaseDuration)!;
        ExtensionOK = result.ExtensionOK;
        return result.Result;
    }

    public int UnRegisterClient(int BoldClientID) => Hub.Invoke<int>(BoldPropagatorHubContract.UnRegisterClient, BoldClientID);

    public int SendEvents(int BoldClientID, string[]? Events) => Hub.Invoke<int>(BoldPropagatorHubContract.SendEvents, BoldClientID, Events);

    public int AddSubscriptions(int BoldClientID, string[]? Subscriptions) =>
        Hub.Invoke<int>(BoldPropagatorHubContract.AddSubscriptions, BoldClientID, Subscriptions);

    public int CancelSubscriptions(int BoldClientID, string[]? Subscriptions) =>
        Hub.Invoke<int>(BoldPropagatorHubContract.CancelSubscriptions, BoldClientID, Subscriptions);

    public bool GetLocks(int ClientId, int TimeOut, string[]? RequestedExclusiveLocks, string[]? RequestedSharedLocks, out string[]? HeldLocks,
        out string[]? ClientsHoldingRequestedLocks)
    {
        TBoldGetLocksResult result = Hub.Invoke<TBoldGetLocksResult>(BoldPropagatorHubContract.GetLocks, ClientId, TimeOut, RequestedExclusiveLocks,
            RequestedSharedLocks)!;
        HeldLocks = result.HeldLocks;
        ClientsHoldingRequestedLocks = result.ClientsHoldingRequestedLocks;
        return result.Result;
    }

    public int ReleaseLocks(int ClientId, string[]? Locks) => Hub.Invoke<int>(BoldPropagatorHubContract.ReleaseLocks, ClientId, Locks);

    public bool EnsureLocks(int ClientId, string[]? RequestedExclusiveLocks, string[]? RequestedSharedLocks) =>
        Hub.Invoke<bool>(BoldPropagatorHubContract.EnsureLocks, ClientId, RequestedExclusiveLocks, RequestedSharedLocks);

    public int ListAllClients(out string[]? Clients)
    {
        TBoldClientListResult result = Hub.Invoke<TBoldClientListResult>(BoldPropagatorHubContract.ListAllClients)!;
        Clients = result.Clients;
        return result.Result;
    }

    public int RemoveLocksForClient(int ClientId, string[]? Locks) => Hub.Invoke<int>(BoldPropagatorHubContract.RemoveLocksForClient, ClientId, Locks);

    public int KillClient(int ClientId) => Hub.Invoke<int>(BoldPropagatorHubContract.KillClient, ClientId);

    public int LocksForClients(string[]? ClientIds, out string[]? Locks, out string[]? LockDurations)
    {
        TBoldLocksForClientsResult result = Hub.Invoke<TBoldLocksForClientsResult>(BoldPropagatorHubContract.LocksForClients, (object?)ClientIds)!;
        Locks = result.Locks;
        LockDurations = result.LockDurations;
        return result.Result;
    }

    public int ListLockingClients(out string[]? Clients)
    {
        TBoldClientListResult result = Hub.Invoke<TBoldClientListResult>(BoldPropagatorHubContract.ListLockingClients)!;
        Clients = result.Clients;
        return result.Result;
    }

    public bool LockManagerSuspended
    {
        get => Hub.Invoke<bool>(GetLockManagerSuspended);
        set => Hub.Invoke(SetLockManagerSuspended, value);
    }
}
