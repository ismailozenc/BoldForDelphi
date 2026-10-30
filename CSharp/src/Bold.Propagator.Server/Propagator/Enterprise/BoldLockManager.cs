// Delphi: Source/Propagator/Enterprise/BoldLockManager.pas
// Port map: CSharp/port/units/BoldLockManager.md

using static Bold.BoldPropagatorConstants;

namespace Bold;

/// <summary>
/// The lock manager: hands out shared and exclusive locks (lock names) to registered clients, and takes a client's
/// locks back when the client handler removes the client. A client that gets its locks is sent a "got locks" event
/// after the events already queued for it, so it sees every change made before it got them.
/// </summary>
/// <remarks>
/// It is safe to call from any thread: it has a lock of its own, and calls the client handler and the enqueuer
/// outside it (they call it with their lock held when a client goes). Delphi's lock manager had no lock.
/// </remarks>
[Delphi(PortStatus.Ported)]
public class TBoldLockManager : TBoldPassthroughSubscriber
{
    private TBoldServerLockList? fHandedLocks;
    private TBoldAdvancedPropagator fPropagator;
    private bool fSuspended;

    /// <summary>The lock that guards <see cref="HandedLocks"/>.</summary>
    internal readonly object Locker = new();

    [Delphi(PortStatus.Ported)]
    public TBoldLockManager(TBoldAdvancedPropagator Propagator)
        : base((TBoldExtendedEventHandler)null!)
    {
        fHandedLocks = new TBoldServerLockList();
        fPropagator = Propagator;
        fPropagator.ClientHandler!.AddSubscription(this, BOLD_PROPAGATOR_CLIENT_REMOVED, breReleaseClientLocks);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldLockManager.Destroy")]
    public override void Dispose()
    {
        lock (Locker)
        {
            SysUtils.FreeAndNil(ref fHandedLocks);
        }

        base.Dispose();
    }

    /// <summary>
    /// True when no other client holds any of the locks in a conflicting way (any lock for an exclusive request, an
    /// exclusive lock for a shared one); otherwise adds the conflicting locks and the clients holding them. Locks that
    /// timed out are taken from their holders, whose lock-lost events are added to <paramref name="LockLostEvents"/>.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Collects the lock-lost events for the caller to send outside the lock.")]
    protected bool CanAcquireLocks(TBoldClientID ClientId, TStringList? Locks, TBoldLockType LockType, TBoldSortedIntegerList ClientsHoldingRequestedLocks,
        TStringList HeldLocks, List<(TBoldClientID ClientId, string Event)> LockLostEvents)
    {
        bool result = true;
        if (Locks is null)
        {
            return result;
        }

        for (int i = 0; i < Locks.Count; i++)
        {
            TBoldLockNameIndexNode? INode = HandedLocks.Locks(Locks[i]);
            if (INode is not null && (LockType == TBoldLockType.bltExclusive || INode.ExclusiveLock))
            {
                var CurrentNode = (TBoldLockNode?)INode.Next;
                while (CurrentNode is not null)
                {
                    var NextNode = (TBoldLockNode?)CurrentNode.Next(HandedLocks.LockNameIndexOrder);
                    if (CurrentNode.ClientId != ClientId)
                    {
                        if (CurrentNode.HasTimedOut)
                        {
                            // This is where locks get lost.
                            CurrentNode.Remove();
                            LockLostEvents.Add((CurrentNode.ClientId,
                                TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsLockLost, "", "", Locks[i], null)));
                        }
                        else
                        {
                            result = false;
                            if (HeldLocks.IndexOf(Locks[i]) == -1)
                            {
                                HeldLocks.Add(Locks[i]);
                            }

                            ClientsHoldingRequestedLocks.Add(CurrentNode.ClientId);
                        }
                    }

                    CurrentNode = NextNode;
                }
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected void AcquireLocks(TBoldClientID ClientId, int TimeOut, TStringList? Locks, TBoldLockType LockType)
    {
        TTimeStamp CurrentTime = SysUtils.DateTimeToTimeStamp(DateTime.Now);
        if (Locks is not null)
        {
            for (int i = 0; i < Locks.Count; i++)
            {
                HandedLocks.AddLock(ClientId, TimeOut, CurrentTime, Locks[i], LockType);
            }
        }
    }

    /// <summary>
    /// Gives a registered client all the locks, or none: when another client holds any of them in a conflicting way,
    /// <paramref name="HeldLocks"/> gets the conflicting locks and <paramref name="ClientsHoldingRequestedLocks"/> the
    /// clients ("id=name") holding them. A suspended lock manager gives no locks.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "String arrays (null when empty) where Delphi gave variant arrays (Null when empty).")]
    public bool GetLocks(TBoldClientID ClientID, int TimeOut, TStringList? RequestedExclusiveLocks, TStringList? RequestedSharedLocks,
        out string[]? HeldLocks, out string[]? ClientsHoldingRequestedLocks)
    {
        bool result = false;
        var ClientIds = new TBoldSortedIntegerList();
        var aHeldLocks = new TStringList();
        var LockLostEvents = new List<(TBoldClientID ClientId, string Event)>();
        bool registered = Propagator.ClientHandler!.IsRegistered(ClientID);
        lock (Locker)
        {
            bool resCanAcquireExclusive = CanAcquireLocks(ClientID, RequestedExclusiveLocks, TBoldLockType.bltExclusive, ClientIds, aHeldLocks, LockLostEvents);
            bool resCanAcquireShared = CanAcquireLocks(ClientID, RequestedSharedLocks, TBoldLockType.bltShared, ClientIds, aHeldLocks, LockLostEvents);
            if (!Suspended && registered && resCanAcquireExclusive && resCanAcquireShared)
            {
                AcquireLocks(ClientID, TimeOut, RequestedExclusiveLocks, TBoldLockType.bltExclusive);
                AcquireLocks(ClientID, TimeOut, RequestedSharedLocks, TBoldLockType.bltShared);
                result = true;
            }
        }

        foreach ((TBoldClientID lostClient, string lostEvent) in LockLostEvents)
        {
            Propagator.Enqueuer!.SendLockEvent(lostClient, lostEvent, false);
        }

        if (result)
        {
            Propagator.Enqueuer!.SendLockEvent(ClientID,
                TBoldObjectSpaceExternalEvent.EncodeExternalEvent(TBoldObjectSpaceSubscriptionType.bsGotLocks, "", "", "", null), true);
        }

        HeldLocks = null;
        ClientsHoldingRequestedLocks = null;
        if (!result)
        {
            if (aHeldLocks.Count > 0)
            {
                HeldLocks = aHeldLocks.ToArray();
            }

            if (ClientIds.Count > 0)
            {
                ClientsHoldingRequestedLocks = new string[ClientIds.Count];
                for (int i = 0; i < ClientIds.Count; i++)
                {
                    var CurrentClientId = (TBoldClientID)ClientIds[i]!;
                    Propagator.ClientHandler!.HasInfoForClient(CurrentClientId, out string ClientIdString, out int _, out int _, out TTimeStamp _, out bool _);
                    ClientsHoldingRequestedLocks[i] = SysUtils.Format("%d=%s", CurrentClientId, ClientIdString);
                }
            }
        }

        return result;
    }

    /// <summary>Takes back the client's locks (unless the lock manager is suspended).</summary>
    [Delphi(PortStatus.Ported)]
    public void ReleaseLocks(TBoldClientID ClientID, TStringList RequestedLocks)
    {
        lock (Locker)
        {
            if (!Suspended)
            {
                for (int i = 0; i < RequestedLocks.Count; i++)
                {
                    HandedLocks.RemoveLockForClient(ClientID, RequestedLocks[i]);
                }
            }
        }
    }

    /// <summary>True when the client holds all the locks, each of the kind asked for (and the lock manager is not suspended).</summary>
    [Delphi(PortStatus.Ported)]
    public bool EnsureLocks(TBoldClientID ClientID, TStringList? RequestedExclusiveLocks, TStringList? RequestedSharedLocks)
    {
        lock (Locker)
        {
            bool result = !Suspended;
            int i = 0;
            if (RequestedExclusiveLocks is not null)
            {
                while (i < RequestedExclusiveLocks.Count && result)
                {
                    TBoldLockNode? CurrentLock = HandedLocks.Items(ClientID, RequestedExclusiveLocks[i]);
                    result = CurrentLock is not null && CurrentLock.LockType == TBoldLockType.bltExclusive;
                    i++;
                }
            }

            i = 0;
            if (RequestedSharedLocks is not null)
            {
                while (i < RequestedSharedLocks.Count && result)
                {
                    TBoldLockNode? CurrentLock = HandedLocks.Items(ClientID, RequestedSharedLocks[i]);
                    result = CurrentLock is not null && CurrentLock.LockType == TBoldLockType.bltShared;
                    i++;
                }
            }

            return result;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ReleaseAllLocksForClient(TBoldClientID ClientID)
    {
        lock (Locker)
        {
            if (!Suspended)
            {
                fHandedLocks?.RemoveClient(ClientID);
            }
        }
    }

    /// <summary>A client was removed: its locks are released.</summary>
    [Delphi(PortStatus.Ported)]
    public void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        if (RequestedEvent == breReleaseClientLocks)
        {
            ReleaseAllLocksForClient((TBoldClientID)Args[0]!);
        }
    }

    protected override bool GetHandlesExtendedEvents() => true;

    protected override void ReceiveExtended(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) =>
        _Receive(Originator, OriginalEvent, RequestedEvent, Args);

    [Delphi(PortStatus.Ported)]
    public bool HasLocks(TBoldClientID ClientId)
    {
        lock (Locker)
        {
            return HandedLocks.Clients(ClientId).Next is not null;
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldServerLockList HandedLocks
    {
        get => fHandedLocks ?? throw new ObjectDisposedException(GetType().Name);
        set => fHandedLocks = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAdvancedPropagator Propagator
    {
        get => fPropagator;
        set => fPropagator = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool Suspended
    {
        get => fSuspended;
        set => fSuspended = value;
    }
}
