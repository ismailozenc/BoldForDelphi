namespace Bold;

/// <summary>
/// The propagator hub's contract, shared by the server (<c>TBoldPropagatorHub</c> in Bold.Propagator.Server) and the
/// clients: the hub's path, its methods (one for each method of the propagator's interfaces) and the results of the
/// methods that give more than one value. The hub speaks SignalR's JSON hub protocol, so any SignalR client can use it.
/// </summary>
public static class BoldPropagatorHubContract
{
    /// <summary>Where the hub is mapped by default.</summary>
    public const string DefaultPath = "/propagator";

    /// <summary>The method the hub calls on a registered client with the events for it (a string array).</summary>
    public const string ReceiveEvents = nameof(IBoldListener.ReceiveEvents);

    public const string RegisterClient = nameof(IBoldClientHandler.RegisterClient);
    public const string ExtendLease = nameof(IBoldClientHandler.ExtendLease);
    public const string UnRegisterClient = nameof(IBoldClientHandler.UnRegisterClient);
    public const string SendEvents = nameof(IBoldEventPropagator.SendEvents);
    public const string AddSubscriptions = nameof(IBoldEventPropagator.AddSubscriptions);
    public const string CancelSubscriptions = nameof(IBoldEventPropagator.CancelSubscriptions);
    public const string GetLocks = nameof(IBoldLockManager.GetLocks);
    public const string ReleaseLocks = nameof(IBoldLockManager.ReleaseLocks);
    public const string EnsureLocks = nameof(IBoldLockManager.EnsureLocks);
    public const string ListAllClients = nameof(IBoldLockManagerAdmin.ListAllClients);
    public const string RemoveLocksForClient = nameof(IBoldLockManagerAdmin.RemoveLocksForClient);
    public const string KillClient = nameof(IBoldLockManagerAdmin.KillClient);
    public const string LocksForClients = nameof(IBoldLockManagerAdmin.LocksForClients);
    public const string ListLockingClients = nameof(IBoldLockManagerAdmin.ListLockingClients);
    public const string GetLockManagerSuspended = "GetLockManagerSuspended";
    public const string SetLockManagerSuspended = "SetLockManagerSuspended";
}

/// <summary>The result of RegisterClient: the HRESULT and the client's id.</summary>
public sealed record TBoldRegisterClientResult(int Result, int BoldClientID);

/// <summary>The result of ExtendLease: the HRESULT and whether the lease was extended.</summary>
public sealed record TBoldExtendLeaseHubResult(int Result, bool ExtensionOK);

/// <summary>The result of GetLocks: whether the locks were given, or the conflicting locks and the clients holding them.</summary>
public sealed record TBoldGetLocksResult(bool Result, string[]? HeldLocks, string[]? ClientsHoldingRequestedLocks);

/// <summary>The result of ListAllClients and ListLockingClients: the HRESULT and the clients ("id=name").</summary>
public sealed record TBoldClientListResult(int Result, string[]? Clients);

/// <summary>The result of LocksForClients: the HRESULT, the locks and how long each has been held.</summary>
public sealed record TBoldLocksForClientsResult(int Result, string[]? Locks, string[]? LockDurations);
