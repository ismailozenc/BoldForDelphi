// Delphi: Source/Propagator/Enterprise/BoldLockManagerAdmin.pas
// Port map: CSharp/port/units/BoldLockManagerAdmin.md

namespace Bold;

/// <summary>Administration of the lock manager: its clients, their locks and how long they have held them, and suspending it.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockManagerAdmin
{
    private readonly TBoldLockManager fLockManager;

    [Delphi(PortStatus.Ported)]
    public TBoldLockManagerAdmin(TBoldLockManager LockManager)
    {
        fLockManager = LockManager;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetLockManagerSuspended() => LockManager.Suspended;

    [Delphi(PortStatus.Ported)]
    private void SetLockManagerSuspended(bool Value) => LockManager.Suspended = Value;

    /// <summary>Unregisters the client; true when it was registered.</summary>
    [Delphi(PortStatus.Ported)]
    public bool KillClient(TBoldClientID ClientID) =>
        LockManager.Propagator.ClientHandler!.GetRegistrationTime(ClientID, out TTimeStamp RegistrationTime) &&
        LockManager.Propagator.ClientHandler.UnRegisterClient(ClientID, RegistrationTime) == Windows.S_OK;

    /// <summary>Sets <paramref name="Clients"/> to the registered clients, "id=name".</summary>
    [Delphi(PortStatus.Ported)]
    public void ListAllClients(TStringList Clients)
    {
        var RegisteredClients = new TStringList();
        LockManager.Propagator.ClientHandler!.GetRegisteredClientIDs(RegisteredClients);
        Clients.Assign(RegisteredClients);
    }

    /// <summary>Adds the registered clients that hold locks, "id=name".</summary>
    [Delphi(PortStatus.Ported)]
    public void ListLockingClients(TStringList Clients)
    {
        var RegisteredClients = new TStringList();
        LockManager.Propagator.ClientHandler!.GetRegisteredClientIDs(RegisteredClients);
        for (int i = 0; i < RegisteredClients.Count; i++)
        {
            TBoldClientID ClientId = SysUtils.StrToInt(RegisteredClients.Names(i));
            if (LockManager.HasLocks(ClientId))
            {
                Clients.Add(RegisteredClients[i]);
            }
        }
    }

    /// <summary>
    /// For each client (an id, or "id=name"), adds "id=lock" to <paramref name="Locks"/> and how long it has been
    /// held ("hh:nn:ss:zzz", with "n day(s) " before when longer) to <paramref name="LockDurations"/>.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void LocksForClients(TStringList? ClientIds, TStringList? Locks, TStringList? LockDurations)
    {
        if (ClientIds is null)
        {
            throw new EBold(SysUtils.Format(PropagatorConsts.sClientIDsNotAssigned, GetType().Name, "LocksForClients"));
        }

        if (Locks is null)
        {
            throw new EBold(SysUtils.Format(PropagatorConsts.sLocksNotAssigned, GetType().Name));
        }

        if (LockDurations is null)
        {
            throw new EBold(SysUtils.Format(PropagatorConsts.sLockDurationNotAssigned, GetType().Name));
        }

        var ClientLocks = new TStringList();
        TTimeStamp CurrentTime = SysUtils.DateTimeToTimeStamp(DateTime.Now);
        lock (LockManager.Locker)
        {
            for (int i = 0; i < ClientIds.Count; i++)
            {
                TBoldClientID CurrentClientId = ClientIds.Names(i) != ""
                    ? SysUtils.StrToInt(ClientIds.Names(i).Trim())
                    : SysUtils.StrToInt(ClientIds[i].Trim());
                LockManager.HandedLocks.GetLocksByClientId(CurrentClientId, ClientLocks);
                for (int j = 0; j < ClientLocks.Count; j++)
                {
                    TBoldLockNode CurrentLockNode = LockManager.HandedLocks.Items(CurrentClientId, ClientLocks[j])!;
                    long temp = (long)CurrentLockNode.GetLockDuration(CurrentTime);
                    long MSec = GetPart(ref temp, 1000);
                    long Sec = GetPart(ref temp, 60);
                    long Min = GetPart(ref temp, 60);
                    long Hour = GetPart(ref temp, 24);
                    string LockDur = SysUtils.Format("%.2d:%.2d:%.2d:%.3d", Hour, Min, Sec, MSec);
                    if (temp > 0)
                    {
                        LockDur = SysUtils.Format("%d day(s) ", temp) + LockDur;
                    }

                    LockDurations.Add(LockDur);
                    Locks.Add(SysUtils.Format("%d=%s", CurrentClientId, ClientLocks[j]));
                }

                ClientLocks.Clear();
            }
        }

        static long GetPart(ref long source, int factor)
        {
            long whole = source / factor;
            long result = source - whole * factor;
            source = whole;
            return result;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveLocksForClient(TBoldClientID ClientId, TStringList Locks) => LockManager.ReleaseLocks(ClientId, Locks);

    [Delphi(PortStatus.Ported)]
    public TBoldLockManager LockManager => fLockManager;

    [Delphi(PortStatus.Ported)]
    public bool LockManagerSuspended
    {
        get => GetLockManagerSuspended();
        set => SetLockManagerSuspended(value);
    }
}
