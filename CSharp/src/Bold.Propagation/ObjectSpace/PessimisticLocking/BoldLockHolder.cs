// Delphi: Source/ObjectSpace/PessimisticLocking/BoldLockHolder.pas
// Port map: CSharp/port/units/BoldLockHolder.md

namespace Bold;

/// <summary>A lock, by name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLock : TBoldMemoryManagedObject
{
    [Delphi(PortStatus.Ported)]
    public TBoldLock(string Name)
    {
        this.Name = Name;
    }

    [Delphi(PortStatus.Ported)]
    public string Name { get; }
}

/// <summary>A set of locks, found by name (ignoring case).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockList : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldLock)Item!).Name;

    [Delphi(PortStatus.Ported, Symbol = "TBoldLockList.Destroy")]
    public override void Dispose()
    {
        Clear(true);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public override void Add(object? Item)
    {
        if (Item is not TBoldLock)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sWrongItemType, GetType().Name, Item?.GetType().Name ?? "nil"));
        }

        base.Add(Item);
    }

    /// <summary>Adds the locks of the list that are not in this one.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddList(TBoldLockList List)
    {
        using TBoldIndexTraverser aTraverser = List.CreateTraverser()!;
        while (!aTraverser.EndOfList)
        {
            AddLock(((TBoldLock)aTraverser.Item!).Name);
            aTraverser.Next();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddLock(string Name)
    {
        if (!Includes(Name))
        {
            Add(new TBoldLock(Name));
        }
    }

    /// <summary>The lock names.</summary>
    [Delphi(PortStatus.Ported, Note = "A string array where Delphi made a variant array.")]
    public string[] AsOLEVariant()
    {
        var result = new string[Count];
        using TBoldIndexTraverser aTraverser = CreateTraverser()!;
        int i = 0;
        while (!aTraverser.EndOfList)
        {
            result[i++] = ((TBoldLock)aTraverser.Item!).Name;
            aTraverser.Next();
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool Includes(string Name) => FindByString(Name) is not null;

    /// <summary>Removes the locks of the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveList(TBoldLockList List)
    {
        if (List.Count > Count)
        {
            var toRemove = new List<string>();
            using (TBoldIndexTraverser aTraverser = CreateTraverser()!)
            {
                while (!aTraverser.EndOfList)
                {
                    var aLock = (TBoldLock)aTraverser.Item!;
                    if (List.Includes(aLock.Name))
                    {
                        toRemove.Add(aLock.Name);
                    }

                    aTraverser.Next();
                }
            }

            toRemove.ForEach(RemoveLock);
        }
        else
        {
            using TBoldIndexTraverser aTraverser = List.CreateTraverser()!;
            while (!aTraverser.EndOfList)
            {
                RemoveLock(((TBoldLock)aTraverser.Item!).Name);
                aTraverser.Next();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveLock(string Name)
    {
        if (FindByString(Name) is { } Item)
        {
            Remove(Item);
        }
    }
}

/// <summary>Holds a client's locks: gets them from a lock manager and gives them back.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractLockHolder : TBoldMemoryManagedObject
{
    [Delphi(PortStatus.Ported)]
    protected abstract TBoldLockList GetHeldExclusive();

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldLockList GetHeldShared();

    /// <summary>
    /// Gets the shared and exclusive locks not held already; false when another client holds one of them, with the
    /// conflicting locks and the clients holding them.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public abstract bool Lock(TBoldLockList Shared, TBoldLockList Exclusive, TStringList HeldLocks, TStringList ClientsHoldingRequestedLocks);

    [Delphi(PortStatus.Ported)]
    public abstract void Release(TBoldLockList Locks);

    /// <summary>True when the client still holds its locks.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract bool EnsureLocks();

    /// <summary>Adds the events received from the propagator that are still waiting to be handled.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void GetPropagationEvents(TStringList EventList);

    [Delphi(PortStatus.Ported)] public TBoldLockList HeldExclusive => GetHeldExclusive();
    [Delphi(PortStatus.Ported)] public TBoldLockList HeldShared => GetHeldShared();
}

/// <summary>
/// Holds a client's locks with the lock manager. When it gets locks it waits for the propagator's "got locks" event,
/// which comes after the events of changes made before the locks were given, so those events are queued by then.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockHolder : TBoldAbstractLockHolder, IDisposable
{
    private TBoldLockList? fHeldExclusive;
    private TBoldLockList? fHeldShared;
    private readonly TBoldListenerThread fListener;
    private IBoldLockManager? fLockManager;
    private int fTimeOut;
    private TEvent? fWakeUpEvent;
    private readonly TBoldAbstractDequeuer? fDequeuer;

    [Delphi(PortStatus.Ported)]
    public TBoldLockHolder(TBoldListenerThread Listener, TBoldAbstractDequeuer? Dequeuer, IBoldLockManager? LockManager)
    {
        fHeldExclusive = new TBoldLockList();
        fHeldShared = new TBoldLockList();
        fListener = Listener;
        fTimeOut = BoldDefs.THIRTY_MINUTES;
        fListener.OnMessage = _ListenerMessage;
        fWakeUpEvent = new TEvent();
        fLockManager = LockManager;
        fDequeuer = Dequeuer;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldLockHolder.Destroy", Note = "Also stops hearing the listener's messages; Delphi left the listener calling the freed holder.")]
    public void Dispose()
    {
        if (fListener.OnMessage == _ListenerMessage)
        {
            fListener.OnMessage = null;
        }

        SysUtils.FreeAndNil(ref fHeldExclusive);
        SysUtils.FreeAndNil(ref fHeldShared);
        SysUtils.FreeAndNil(ref fWakeUpEvent);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private void WaitForWakeup()
    {
        if (fWakeUpEvent!.WaitFor(TimeOut) != TWaitResult.wrSignaled)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sOperationTimedOut, GetType().Name));
        }

        fWakeUpEvent.ResetEvent();
    }

    [Delphi(PortStatus.Ported)]
    private void WakeUp() => fWakeUpEvent?.SetEvent();

    /// <summary>A "got locks" message wakes the waiting Lock and is not queued.</summary>
    [Delphi(PortStatus.Ported)]
    private bool _ListenerMessage(string Msg)
    {
        string ClassName = "", MemberName = "", LockName = "";
        if (TBoldObjectSpaceExternalEvent.DecodeExternalEvent(Msg, ref ClassName, ref MemberName, ref LockName, null) ==
            TBoldObjectSpaceSubscriptionType.bsGotLocks)
        {
            WakeUp();
            return true;
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldLockList GetHeldExclusive() => fHeldExclusive!;

    [Delphi(PortStatus.Ported)]
    protected override TBoldLockList GetHeldShared() => fHeldShared!;

    /// <summary>
    /// Gets the locks not held already (a shared lock held is upgraded when asked for exclusive): true, after the
    /// "got locks" event has come; false when another client holds one, with the conflicts.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override bool Lock(TBoldLockList Shared, TBoldLockList Exclusive, TStringList HeldLocks, TStringList ClientsHoldingRequestedLocks)
    {
        bool result = LockManager is not null;
        if (!result)
        {
            return result;
        }

        Shared.RemoveList(fHeldShared!);
        Shared.RemoveList(fHeldExclusive!);
        Exclusive.RemoveList(fHeldExclusive!);
        Shared.RemoveList(Exclusive);
        if (Exclusive.Count > 0 || Shared.Count > 0)
        {
            result = LockManager!.GetLocks(fListener.BoldClientID, fTimeOut, Exclusive.AsOLEVariant(), Shared.AsOLEVariant(), out string[]? vHeldLocks,
                out string[]? vClientsHoldingRequestedLocks);
            if (result)
            {
                WaitForWakeup();
                fHeldShared!.AddList(Shared);
                fHeldExclusive!.AddList(Exclusive);
                // Shared locks now held exclusive are no longer shared.
                fHeldShared.RemoveList(Exclusive);
            }
            else
            {
                BoldUtils.BoldVariantToStrings(vClientsHoldingRequestedLocks, ClientsHoldingRequestedLocks);
                BoldUtils.BoldVariantToStrings(vHeldLocks, HeldLocks);
            }
        }

        return result;
    }

    /// <summary>Takes the database lock exclusively, then handles the events received meanwhile.</summary>
    [Delphi(PortStatus.Ported)]
    public bool LockDatabase()
    {
        if (fDequeuer is null)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sNoDequeuerAvailable, GetType().Name));
        }

        using var SharedLocks = new TBoldLockList();
        using var ExclusiveLocks = new TBoldLockList();
        var Conflicts = new TStringList();
        var ConflictingUsers = new TStringList();
        ExclusiveLocks.AddLock(BoldLockingDefs.BOLD_DBLOCK_NAME);
        bool result = Lock(SharedLocks, ExclusiveLocks, Conflicts, ConflictingUsers);
        if (result)
        {
            fHeldExclusive!.AddLock(BoldLockingDefs.BOLD_DBLOCK_NAME);
        }

        fDequeuer.DequeueAll();
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override void Release(TBoldLockList Locks)
    {
        if (LockManager is not null)
        {
            LockManager.ReleaseLocks(fListener.BoldClientID, Locks.AsOLEVariant());
            fHeldExclusive!.RemoveList(Locks);
            fHeldShared!.RemoveList(Locks);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool EnsureLocks()
    {
        bool result = LockManager is not null;
        if (result && (fHeldExclusive!.Count > 0 || fHeldShared!.Count > 0))
        {
            result = fLockManager!.EnsureLocks(fListener.BoldClientID, fHeldExclusive.AsOLEVariant(), fHeldShared!.AsOLEVariant());
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override void GetPropagationEvents(TStringList EventList) => fListener.Queue.AppendToStringList(EventList);

    /// <summary>How long Lock waits for the "got locks" event, in milliseconds.</summary>
    [Delphi(PortStatus.Ported)]
    public int TimeOut
    {
        get => fTimeOut;
        set => fTimeOut = value;
    }

    [Delphi(PortStatus.Ported)]
    public IBoldLockManager? LockManager
    {
        get => fLockManager;
        set => fLockManager = value;
    }
}
