// Delphi: Source/Persistence/Propagation/BoldSnooper.pas
// Port map: CSharp/port/units/BoldSnooper.md

using static Bold.BoldPropagatorConstants;
using static Bold.PersistenceConsts;

namespace Bold;

/// <summary>
/// The snooper of a client: it sends the events and subscriptions the persistence layer's calls give (see
/// TBoldAbstractSnooper) to the propagator of its snooper handle, and holds the database lock around updates when
/// the handle checks it.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSnooper : TBoldAbstractSnooper
{
    private readonly object fOwner;

    [Delphi(PortStatus.Ported)]
    public TBoldSnooper(TMoldModel MoldModel, object aOwner)
        : base(MoldModel)
    {
        fOwner = aOwner ?? throw new ArgumentNullException(nameof(aOwner));
    }

    private static string[] DatabaseLock_AsOLEVariant() => [BoldLockingDefs.BOLD_DBLOCK_NAME];

    /// <summary>Raises when the database lock is checked and the client does not hold it.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void EnsureDataBaseLock(TBoldClientID ClientID)
    {
        if (!CheckDatabaseLock)
        {
            return;
        }

        if (!LockManager.EnsureLocks(ClientID, DatabaseLock_AsOLEVariant(), null))
        {
            throw new EBoldEnsureDatabaseLockError(SysUtils.Format(sCannotAcquireLock, GetType().Name, "EnsureDatabaseLock"));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void ReleaseDataBaseLock(TBoldClientID ClientID)
    {
        if (!CheckDatabaseLock)
        {
            return;
        }

        try
        {
            LockManager.ReleaseLocks(ClientID, DatabaseLock_AsOLEVariant());
        }
        catch (Exception e)
        {
            throw new EBoldLockManagerError(SysUtils.Format(sCannotAcquireLock, GetType().Name, "ReleaseDataBaseLock"), e);
        }
    }

    /// <summary>The lock manager of the snooper handle's lock manager handle.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual IBoldLockManager GetLockManager() =>
        ((TBoldSnooperHandle)fOwner).LockManagerHandle?.LockManager
        ?? throw new EBoldLockManagerError(SysUtils.Format(sLockManagerNotAssigned, GetType().Name));

    [Delphi(PortStatus.Ported)]
    protected virtual IBoldEventPropagator? GetPropagator() => ((TBoldSnooperHandle)fOwner).PropagatorHandle?.EventPropagator;

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetCheckDatabaseLock() => ((TBoldSnooperHandle)fOwner).CheckDatabaseLock;

    /// <summary>
    /// Sends the events, subscriptions and cancelled subscriptions of the client to the propagator, then forgets them.
    /// A call the propagator refuses is reported through OnPropagatorFailure.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void TransmitEvents(TBoldClientID ClientID)
    {
        void CheckError(int Res, string Action)
        {
            if (Res != Windows.S_OK)
            {
                string s = Res switch
                {
                    E_ENQUEUER_NOT_ENABLED => sEnqueuerNotEnabled,
                    E_CLIENT_NOT_REGISTERED => sClientNotRegistered,
                    E_INVALID_PARAMETER => sInvalidParameter,
                    W_CLIENT_NOT_RECEIVING => sClientNotReceivingEvents,
                    _ => SysUtils.Format(sCallFailed, "%s", SysUtils.IntToHex(Res, 8)),
                };
                DoPropagatorFailure(this, SysUtils.Format(s, Action));
            }
        }

        try
        {
            if (ClientID != BoldDefs.NOTVALIDCLIENTID && Propagator is { } propagator)
            {
                try
                {
                    if (Events.Count != 0)
                    {
                        CheckError(propagator.SendEvents(ClientID, Events.ToArray()), "SendEvents");
                    }

                    if (Subscriptions.Count != 0)
                    {
                        CheckError(propagator.AddSubscriptions(ClientID, Subscriptions.ToArray()), "AddSubscriptions");
                    }

                    if (CancelledSubscriptions.Count != 0)
                    {
                        CheckError(propagator.CancelSubscriptions(ClientID, CancelledSubscriptions.ToArray()), "CancelSubscriptions");
                    }
                }
                catch (Exception E) when (E is not EBold)
                {
                    DoPropagatorFailure(this, E.Message);
                }
            }
        }
        finally
        {
            ClearEvents();
        }
    }

    [Delphi(PortStatus.Ported)] public IBoldEventPropagator? Propagator => GetPropagator();
    [Delphi(PortStatus.Ported)] public IBoldLockManager LockManager => GetLockManager();
    [Delphi(PortStatus.Ported)] public bool CheckDatabaseLock => GetCheckDatabaseLock();
}
