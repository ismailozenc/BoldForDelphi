namespace Bold;

/// <summary>
/// One client's connection to the propagator server of this process: the propagator's interfaces for it. It
/// remembers the time its client registered, so that unregistering cannot remove a later client with the same id.
/// This is what Delphi's COM objects (TBoldClientHandlerCOM, TBoldEnqueuerCOM, TBoldLockManagerCOM,
/// TBoldLockManagerAdminCOM) did for a client.
/// </summary>
public sealed class TBoldPropagatorConnection(TBoldPropagatorServer Server) : IBoldClientHandler, IBoldEventPropagator, IBoldLockManager, IBoldLockManagerAdmin
{
    private TTimeStamp fRegistrationTime;
    private int fClientId = BoldDefs.NOTVALIDCLIENTID;

    /// <summary>A connection to the server of this process (<see cref="TBoldPropagatorServer.Instance"/>).</summary>
    public TBoldPropagatorConnection()
        : this(TBoldPropagatorServer.Instance())
    {
    }

    public TBoldPropagatorServer Server { get; } = Server;

    /// <summary>The id of the client registered through this connection; NOTVALIDCLIENTID when none.</summary>
    public int ClientId => fClientId;

    private TBoldAdvancedPropagator Propagator => Server.AdvancedPropagator ?? throw new EBold("The propagator server is not initialized.");

    public int RegisterClient(int LeaseDuration, int PollingInterval, IBoldListener BoldClientListener, string ClientIDString, out int BoldClientID)
    {
        BoldThreadSafeLog.BoldLogThread("ID=ClientHandler/RegCli");
        int result = Propagator.ClientHandler!.RegisterClient(LeaseDuration, PollingInterval, BoldClientListener, ClientIDString, out BoldClientID,
            out fRegistrationTime);
        fClientId = result == Windows.S_OK ? BoldClientID : BoldDefs.NOTVALIDCLIENTID;
        return result;
    }

    public int ExtendLease(int BoldClientID, int LeaseDuration, out bool ExtensionOK)
    {
        BoldThreadSafeLog.BoldLogThread("ID=ClientHandler/ExtLease");
        return Propagator.ClientHandler!.ExtendLease(BoldClientID, LeaseDuration, out ExtensionOK);
    }

    public int UnRegisterClient(int BoldClientID)
    {
        BoldThreadSafeLog.BoldLogThread("ID=ClientHandlerH/UnReg");
        int result = Propagator.ClientHandler!.UnRegisterClient(BoldClientID, fRegistrationTime);
        if (BoldClientID == fClientId)
        {
            fClientId = BoldDefs.NOTVALIDCLIENTID;
        }

        return result;
    }

    /// <summary>Removes the connection's client at once, as one whose connection is lost (when the transport closes).</summary>
    public bool Disconnect()
    {
        int clientId = fClientId;
        fClientId = BoldDefs.NOTVALIDCLIENTID;
        return clientId != BoldDefs.NOTVALIDCLIENTID && Server.AdvancedPropagator?.ClientHandler?.DisconnectClient(clientId, fRegistrationTime) == true;
    }

    public int SendEvents(int BoldClientID, string[]? Events)
    {
        BoldThreadSafeLog.BoldLogThread("ID=Enqueuer/SendEv");
        return Propagator.Enqueuer!.SendEvents(BoldClientID, Events);
    }

    public int AddSubscriptions(int BoldClientID, string[]? Subscriptions)
    {
        BoldThreadSafeLog.BoldLogThread("ID=Enqueuer/AddS");
        return Propagator.Enqueuer!.AddSubscriptions(BoldClientID, Subscriptions);
    }

    public int CancelSubscriptions(int BoldClientID, string[]? Subscriptions)
    {
        BoldThreadSafeLog.BoldLogThread("ID=Enqueuer/CancelS");
        return Propagator.Enqueuer!.CancelSubscriptions(BoldClientID, Subscriptions);
    }

    private static TStringList Strings(string[]? Values)
    {
        var result = new TStringList();
        BoldUtils.BoldVariantToStrings(Values, result);
        return result;
    }

    public bool GetLocks(int ClientId, int TimeOut, string[]? RequestedExclusiveLocks, string[]? RequestedSharedLocks, out string[]? HeldLocks,
        out string[]? ClientsHoldingRequestedLocks) =>
        Server.LockManager.GetLocks(ClientId, TimeOut, Strings(RequestedExclusiveLocks), Strings(RequestedSharedLocks), out HeldLocks,
            out ClientsHoldingRequestedLocks);

    public int ReleaseLocks(int ClientId, string[]? Locks)
    {
        Server.LockManager.ReleaseLocks(ClientId, Strings(Locks));
        return Windows.S_OK;
    }

    public bool EnsureLocks(int ClientId, string[]? RequestedExclusiveLocks, string[]? RequestedSharedLocks) =>
        Server.LockManager.EnsureLocks(ClientId, Strings(RequestedExclusiveLocks), Strings(RequestedSharedLocks));

    public int ListAllClients(out string[]? Clients)
    {
        var list = new TStringList();
        Server.LockManagerAdmin.ListAllClients(list);
        Clients = list.Count == 0 ? null : [.. list];
        return Windows.S_OK;
    }

    public int RemoveLocksForClient(int ClientId, string[]? Locks)
    {
        Server.LockManagerAdmin.RemoveLocksForClient(ClientId, Strings(Locks));
        return Windows.S_OK;
    }

    public int KillClient(int ClientId) => Server.LockManagerAdmin.KillClient(ClientId) ? Windows.S_OK : Windows.S_FALSE;

    public int LocksForClients(string[]? ClientIds, out string[]? Locks, out string[]? LockDurations)
    {
        var locks = new TStringList();
        var durations = new TStringList();
        Server.LockManagerAdmin.LocksForClients(Strings(ClientIds), locks, durations);
        Locks = locks.Count == 0 ? null : [.. locks];
        LockDurations = durations.Count == 0 ? null : [.. durations];
        return Windows.S_OK;
    }

    public int ListLockingClients(out string[]? Clients)
    {
        var list = new TStringList();
        Server.LockManagerAdmin.ListLockingClients(list);
        Clients = list.Count == 0 ? null : [.. list];
        return Windows.S_OK;
    }

    public bool LockManagerSuspended
    {
        get => Server.LockManagerAdmin.LockManagerSuspended;
        set => Server.LockManagerAdmin.LockManagerSuspended = value;
    }
}
