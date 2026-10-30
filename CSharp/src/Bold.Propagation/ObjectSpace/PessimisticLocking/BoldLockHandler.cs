// Delphi: Source/ObjectSpace/PessimisticLocking/BoldLockHandler.pas
// Port map: CSharp/port/units/BoldLockHandler.md

using static Bold.BoldSubscription;

namespace Bold;

/// <summary>Why getting locks failed: the conflicting regions and the clients holding them.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFailureGetLocksFailed : TBoldFailureReason
{
    private readonly TStringList fClientIds;
    private readonly TBoldRegionList fConflictingRegions;

    [Delphi(PortStatus.Ported)]
    public TBoldFailureGetLocksFailed(string reason, TBoldDomainElement? Originator, TBoldRegionList? ConflictingRegions, TStringList? ClientIds)
        : base(reason, Originator)
    {
        fClientIds = new TStringList();
        if (ClientIds is not null)
        {
            fClientIds.Assign(ClientIds);
        }

        fConflictingRegions = new TBoldRegionList();
        if (ConflictingRegions is not null)
        {
            fConflictingRegions.Assign(ConflictingRegions);
        }
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldFailureGetLocksFailed.Destroy", Note = "Freed the lists; the garbage collector does that in .NET.")]
    public override void Dispose() => base.Dispose();

    [Delphi(PortStatus.Ported)]
    protected override EBoldFailure GetException(string Msg) => new EBoldGetLocksFailed(Msg, fClientIds, fConflictingRegions);
}

/// <summary>Raised when locks could not be had; says which regions conflict and which clients hold them ("id=name").</summary>
[Delphi(PortStatus.Ported)]
public class EBoldGetLocksFailed : EBoldFailure
{
    [Delphi(PortStatus.Ported)]
    [Delphi(PortStatus.Replaced, Symbol = "EBoldGetLocksFailed.Destroy", Note = "Freed the lists; the garbage collector does that in .NET.")]
    public EBoldGetLocksFailed(string msg, TStrings? ClientIds, TBoldRegionList? ConflictingRegions)
        : base(msg)
    {
        this.ClientIds = new TStringList();
        if (ClientIds is not null)
        {
            this.ClientIds.Assign(ClientIds);
        }

        this.ConflictingRegions = new TBoldRegionList();
        if (ConflictingRegions is not null)
        {
            this.ConflictingRegions.Assign(ConflictingRegions);
        }
    }

    [Delphi(PortStatus.Ported)] public TStringList ClientIds { get; }
    [Delphi(PortStatus.Ported)] public TBoldRegionList ConflictingRegions { get; }
}

/// <summary>A lock holder that holds nothing and gets everything; the lock handler expands regions against it.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldEmptyLockHolder : TBoldAbstractLockHolder, IDisposable
{
    private TBoldLockList? fLockList;

    [Delphi(PortStatus.Ported)]
    public TBoldEmptyLockHolder()
    {
        fLockList = new TBoldLockList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldEmptyLockHolder.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fLockList);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)] protected override TBoldLockList GetHeldExclusive() => fLockList!;
    [Delphi(PortStatus.Ported)] protected override TBoldLockList GetHeldShared() => fLockList!;
    [Delphi(PortStatus.Ported)] public override bool Lock(TBoldLockList Shared, TBoldLockList Exclusive, TStringList HeldLocks, TStringList ClientsHoldingRequestedLocks) => true;

    [Delphi(PortStatus.Ported)]
    public override void Release(TBoldLockList Locks)
    {
    }

    [Delphi(PortStatus.Ported)] public override bool EnsureLocks() => true;

    [Delphi(PortStatus.Ported)]
    public override void GetPropagationEvents(TStringList EventList)
    {
    }
}

/// <summary>
/// Pessimistic locking of an object space: an element about to change needs the locks of its regions (exclusive) and
/// of the regions they depend on (shared). The locks are got at once outside a transaction and at commit inside one;
/// changes other clients made that touch the required regions make the locking fail, as do locks other clients hold.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPessimisticLockHandler : TBoldAbstractPessimisticLockHandler
{
    private TBoldRegionList? fRequiredShared;
    private TBoldRegionList? fRequiredExclusive;
    private TBoldRegionList? fRequiredExplicit;
    private TBoldRegionLookup? fKnownRequiredParents;
    private TBoldRegionLookup? fKnownRequiredSubregions;
    private TBoldRegionList? fParentsChangedRegions;
    private TBoldRegionList? fSubregionsChangedRegions;
    private TBoldAbstractLockHolder fLockHolder;
    private TBoldPassthroughSubscriber? fSubscriber;

    /// <summary>A lock handler for the system, getting its locks through <paramref name="LockHolder"/>; it becomes the system's lock handler.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldPessimisticLockHandler.CreateWithLockHolder")]
    public TBoldPessimisticLockHandler(TBoldSystem System, TBoldAbstractLockHolder LockHolder)
        : base(System)
    {
        fRequiredShared = new TBoldRegionList();
        fRequiredExclusive = new TBoldRegionList();
        fRequiredExplicit = new TBoldRegionList();
        fKnownRequiredParents = new TBoldRegionLookup();
        fKnownRequiredSubregions = new TBoldRegionLookup();
        fParentsChangedRegions = new TBoldRegionList();
        fSubregionsChangedRegions = new TBoldRegionList();
        fSubscriber = new TBoldPassthroughSubscriber(_ReceiveRolledBack, _AnswerMayCommit);
        System.AddSubscription(fSubscriber, bqMayCommit, bqMayCommit);
        System.AddSubscription(fSubscriber, beRolledBack, beRolledBack);
        System.PessimisticLockHandler = this;
        fLockHolder = LockHolder;
        // This should be replaced with a subscription mechanism.
        Factory.OnRegionChanged = _RegionChanged;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPessimisticLockHandler.Destroy", Note = "Also stops being the system's lock handler and the factory's listener.")]
    public override void Dispose()
    {
        if (fSubscriber is null)
        {
            return;
        }

        if (System.PessimisticLockHandler == this)
        {
            System.PessimisticLockHandler = null;
        }

        if (GetFactory() is { } factory && factory.OnRegionChanged == _RegionChanged)
        {
            factory.OnRegionChanged = null;
        }

        SysUtils.FreeAndNil(ref fSubscriber);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private void SignalActivityStart() => OnActivityStart?.Invoke(this);

    [Delphi(PortStatus.Ported)]
    private void SignalActivityEnd() => OnActivityEnd?.Invoke(this);

    [Delphi(PortStatus.Ported)]
    private TBoldRegionFactory? GetFactory() => System?.RegionFactory as TBoldRegionFactory;

    /// <summary>A region the handler requires or holds changed: it is expanded again before the next locking.</summary>
    [Delphi(PortStatus.Ported)]
    private void _RegionChanged(TBoldRegion Region, int Event)
    {
        if (fRequiredExplicit!.IndexOf(Region) != -1 || fRequiredExclusive!.IndexOf(Region) != -1 ||
            fKnownRequiredParents!.FindByID(Region.AsString) is not null || fKnownRequiredSubregions!.FindByID(Region.AsString) is not null ||
            fLockHolder.HeldExclusive.Includes(Region.AsString) || fLockHolder.HeldShared.Includes(Region.AsString))
        {
            if (Event == BoldLockRegions.breParentsChanged)
            {
                fParentsChangedRegions!.Add(Region);
            }
            else if (Event == BoldLockRegions.breSubregionsChanged)
            {
                fSubregionsChangedRegions!.Add(Region);
            }
            else
            {
                throw new EBoldInternal(SysUtils.Format("%s._RegionChanged: Unexpected event %d", GetType().Name, Event));
            }
        }
    }

    /// <summary>Adds the held region locks (exclusive, and shared too when asked) to the lookup.</summary>
    [Delphi(PortStatus.Ported, Note = "Region locks have a '.'; Delphi's check was the same.")]
    private void AddHeldLocksToRegionLookup(TBoldRegionLookup RegionLookup, bool AddSharedRegions)
    {
        void Add(TBoldLockList List)
        {
            using TBoldIndexTraverser Traverser = List.CreateTraverser()!;
            while (!Traverser.EndOfList)
            {
                string RegionId = ((TBoldLock)Traverser.Item!).Name;
                if (RegionId.Contains('.') && RegionLookup.FindByID(RegionId) is null)
                {
                    RegionLookup.Add(Factory.GetRegionByName(RegionId));
                }

                Traverser.Next();
            }
        }

        Add(fLockHolder.HeldExclusive);
        if (AddSharedRegions)
        {
            Add(fLockHolder.HeldShared);
        }
    }

    /// <summary>Expands the regions that changed or were required into the parent regions and subregions they need.</summary>
    [Delphi(PortStatus.Ported)]
    private void EnsureAllRequiredRegions()
    {
        var Expander = new TBoldRegionExpander { OnProgress = OnProgress };
        var RegionsToExpand = new TBoldRegionLookup();
        var KnownRequiredOrHeldSubregions = new TBoldRegionLookup();
        var KnownRequiredOrHeldParentRegions = new TBoldRegionLookup();
        SignalActivityStart();
        try
        {
            for (int i = 0; i < fParentsChangedRegions!.Count; i++)
            {
                if (IsRequiredAsExplicitOrParent(fParentsChangedRegions[i]) || IsHeldAsExplicitOrParent(fParentsChangedRegions[i]))
                {
                    RegionsToExpand.Add(fParentsChangedRegions[i]);
                }
            }

            KnownRequiredOrHeldParentRegions.AddRegionLookup(fKnownRequiredParents!);
            // The explicit regions are parent regions...
            AddHeldLocksToRegionLookup(KnownRequiredOrHeldParentRegions, false);
            KnownRequiredOrHeldSubregions.AddRegionLookup(fKnownRequiredSubregions!);
            // ...and all held regions are at least subregions.
            AddHeldLocksToRegionLookup(KnownRequiredOrHeldSubregions, true);
            Expander.ExpandParentRegions(RegionsToExpand, KnownRequiredOrHeldParentRegions, KnownRequiredOrHeldSubregions);
            RegionsToExpand.Clear();
            RegionsToExpand.AddRegionLookup(Expander.NewParentRegions);
            RegionsToExpand.AddRegionList(fSubregionsChangedRegions!);
            Expander.ExpandSubregions(RegionsToExpand, KnownRequiredOrHeldSubregions);
            fRequiredExclusive!.AddList(fRequiredExplicit);
            fRequiredShared!.AddRegionLookup(Expander.NewParentRegions);
            fRequiredShared.AddRegionLookup(Expander.NewSubRegions);
            fKnownRequiredSubregions!.AddRegionLookup(Expander.NewSubRegions);
            fKnownRequiredParents!.AddRegionLookup(Expander.NewParentRegions);
        }
        finally
        {
            SignalActivityEnd();
        }

        fRequiredExplicit!.Clear();
        fParentsChangedRegions.Clear();
        fSubregionsChangedRegions!.Clear();
    }

    [Delphi(PortStatus.Ported)]
    private bool IsRequiredAsExplicitOrParent(TBoldRegion Region) =>
        fRequiredExplicit!.IndexOf(Region) != -1 || fKnownRequiredParents!.FindByID(Region.AsString) is not null;

    /// <summary>
    /// Gets the required locks from the lock holder. Fails when another client holds one, or when changes other
    /// clients made (events not yet handled) touch the required regions; the failure reason says which.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private bool GetRequiredLocks()
    {
        using var SharedLocks = new TBoldLockList();
        using var ExclusiveLocks = new TBoldLockList();
        var HeldLocks = new TStringList();
        var ClientsHoldingRequestedLocks = new TStringList();
        EnsureAllRequiredRegions();
        BoldLockHandler.RegionListToLockList(fRequiredShared!, SharedLocks);
        BoldLockHandler.RegionListToLockList(fRequiredExclusive!, ExclusiveLocks);
        bool result = fLockHolder.Lock(SharedLocks, ExclusiveLocks, HeldLocks, ClientsHoldingRequestedLocks);
        if (result)
        {
            result = !ArePropagationEventsInConflictWithRequiredRegions();
            if (!result)
            {
                SharedLocks.AddList(ExclusiveLocks);
                fLockHolder.Release(SharedLocks);
            }
        }

        fRequiredShared!.Clear();
        fRequiredExclusive!.Clear();
        fRequiredExplicit!.Clear();
        fKnownRequiredParents!.Clear();
        fKnownRequiredSubregions!.Clear();
        if (!result)
        {
            TBoldRegionList ConflictingRegions = BoldLockHandler.NewRegionListFromStrings(HeldLocks, Factory);
            BoldSystem.SetBoldLastFailureReason(new TBoldFailureGetLocksFailed(BoldLockHandler.BOLD_GET_LOCKS_FAILED_ERROR, null, ConflictingRegions,
                ClientsHoldingRequestedLocks));
        }

        return result;
    }

    /// <summary>The system asks whether it may commit: it may when the required locks can be had.</summary>
    [Delphi(PortStatus.Ported)]
    private bool _AnswerMayCommit(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args, TBoldSubscriber? Subscriber) =>
        OriginalEvent != bqMayCommit || Originator != System || GetRequiredLocks();

    /// <summary>A rollback forgets what was required.</summary>
    [Delphi(PortStatus.Ported)]
    private void _ReceiveRolledBack(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == System && OriginalEvent == beRolledBack)
        {
            fRequiredExplicit!.Clear();
            fKnownRequiredParents!.Clear();
            fKnownRequiredSubregions!.Clear();
            fRequiredExclusive!.Clear();
            fRequiredShared!.Clear();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void RequireRegionExplicit(TBoldRegion Region)
    {
        fRequiredExplicit!.Add(Region);
        fParentsChangedRegions!.Add(Region);
        fSubregionsChangedRegions!.Add(Region);
    }

    /// <summary>True when the region is held exclusive, or held shared as the parent of a region held so.</summary>
    [Delphi(PortStatus.Ported)]
    private bool IsHeldAsExplicitOrParent(TBoldRegion Region)
    {
        bool IsParent()
        {
            var RegionList = new TBoldRegionList();
            Region.FillDependentSubregions(RegionList);
            for (int i = 0; i < RegionList.Count; i++)
            {
                if (IsHeldAsExplicitOrParent(RegionList[i]))
                {
                    return true;
                }
            }

            Region.FillIndependentSubregions(RegionList);
            for (int i = 0; i < RegionList.Count; i++)
            {
                if (IsHeldAsExplicitOrParent(RegionList[i]))
                {
                    return true;
                }
            }

            return false;
        }

        return fLockHolder.HeldExclusive.Includes(Region.AsString) || (fLockHolder.HeldShared.Includes(Region.AsString) && IsParent());
    }

    [Delphi(PortStatus.Ported)]
    private TBoldRegionList GetRequiredShared()
    {
        EnsureAllRequiredRegions();
        return fRequiredShared!;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldRegionList GetRequiredExclusive()
    {
        EnsureAllRequiredRegions();
        return fRequiredExclusive!;
    }

    /// <summary>True when a change another client made (an event not handled yet) touches a required region.</summary>
    [Delphi(PortStatus.Ported)]
    private bool ArePropagationEventsInConflictWithRequiredRegions()
    {
        bool result = false;
        var EventList = new TStringList();
        var ObjectID = new TBoldDefaultID(0, false);
        fLockHolder.GetPropagationEvents(EventList);
        for (int i = 0; i < EventList.Count && !result; i++)
        {
            string ClassName = "", MemberName = "", LockName = "";
            TBoldObjectSpaceSubscriptionType EventType =
                TBoldObjectSpaceExternalEvent.DecodeExternalEvent(EventList[i], ref ClassName, ref MemberName, ref LockName, ObjectID);
            switch (EventType)
            {
                case TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged:
                {
                    TBoldObject CurrObj = System.EnsuredLocatorByID(ObjectID).EnsuredBoldObject!;
                    for (int j = 0; j < CurrObj.BoldMemberCount && !result; j++)
                    {
                        TBoldMember CurrMember = CurrObj.BoldMembers(j);
                        if (CurrMember.BoldMemberRTInfo!.IsStoredInObject)
                        {
                            result = IsElementInAnyRequiredRegion(CurrMember);
                        }
                    }

                    break;
                }

                case TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged:
                {
                    TBoldObject CurrObj = System.EnsuredLocatorByID(ObjectID).EnsuredBoldObject!;
                    result = IsElementInAnyRequiredRegion(CurrObj.BoldMemberByExpressionName(MemberName));
                    break;
                }
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private bool IsElementInAnyRequiredRegion(TBoldMember Element)
    {
        var RegionList = new TBoldRegionList();
        Factory.GetRegionsForElement(Element, RegionList);
        for (int i = 0; i < RegionList.Count; i++)
        {
            if (fRequiredExclusive!.IndexOf(RegionList[i]) != -1 || fRequiredShared!.IndexOf(RegionList[i]) != -1)
            {
                return true;
            }
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    private static bool ElementListContainsDirtyElements(TList Elements)
    {
        for (int i = 0; i < Elements.Count; i++)
        {
            bool dirty = Elements[i] switch
            {
                TBoldMember member => member.BoldDirty,
                TBoldObject obj => obj.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified,
                _ => false,
            };
            if (dirty)
            {
                return true;
            }
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldRegionFactory Factory => GetFactory()!;

    /// <summary>
    /// The element is about to change: its regions are required. Outside a transaction the locks are got now; inside
    /// one, at commit. A dirty attribute needs nothing more.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override bool LockElement(TBoldDomainElement Element)
    {
        if (Element is TBoldAttribute && Element.BoldDirty)
        {
            return true;
        }

        var Regions = new TBoldRegionList();
        Factory.GetRegionsForElement(Element, Regions);
        for (int i = 0; i < Regions.Count; i++)
        {
            RequireRegionExplicit(Regions[i]);
        }

        return System.InTransaction() || GetRequiredLocks();
    }

    [Delphi(PortStatus.Ported)]
    public override bool EnsureLocks() => fLockHolder.EnsureLocks();

    /// <summary>
    /// Releases the locks no longer needed: those not required by the regions with dirty elements (expanded against an
    /// empty lock holder, as the expansion stops at held locks).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void ReleaseUnneededRegions()
    {
        var Elements = new TList();
        using var UnRequiredLocks = new TBoldLockList();
        using var RequiredSharedLocks = new TBoldLockList();
        using var RequiredExclusiveLocks = new TBoldLockList();
        TBoldAbstractLockHolder TrueLockHolder = fLockHolder;
        try
        {
            using (TBoldIndexTraverser aTraverser = fLockHolder.HeldExclusive.CreateTraverser()!)
            {
                while (!aTraverser.EndOfList)
                {
                    string LockName = ((TBoldLock)aTraverser.Item!).Name;
                    if (LockName != BoldLockingDefs.BOLD_DBLOCK_NAME)
                    {
                        TBoldRegion CurrentRegion = Factory.GetRegionByName(LockName);
                        Elements.Clear();
                        CurrentRegion.GetElements(Elements);
                        if (ElementListContainsDirtyElements(Elements))
                        {
                            RequireRegionExplicit(CurrentRegion);
                        }
                    }

                    aTraverser.Next();
                }
            }

            using (var emptyHolder = new TBoldEmptyLockHolder())
            {
                fLockHolder = emptyHolder;
                BoldLockHandler.RegionListToLockList(RequiredExclusive, RequiredExclusiveLocks);
                BoldLockHandler.RegionListToLockList(RequiredShared, RequiredSharedLocks);
                fLockHolder = TrueLockHolder;
            }

            UnRequiredLocks.AddList(fLockHolder.HeldExclusive);
            UnRequiredLocks.AddList(fLockHolder.HeldShared);
            UnRequiredLocks.RemoveList(RequiredExclusiveLocks);
            UnRequiredLocks.RemoveList(RequiredSharedLocks);
            fLockHolder.Release(UnRequiredLocks);
        }
        finally
        {
            fLockHolder = TrueLockHolder;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldRegionList RequiredShared => GetRequiredShared();
    [Delphi(PortStatus.Ported)] public TBoldRegionList RequiredExclusive => GetRequiredExclusive();
    [Delphi(PortStatus.Ported)] public TNotifyEvent? OnActivityStart { get; set; }
    [Delphi(PortStatus.Ported)] public TNotifyEvent? OnActivityEnd { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldLockManagerProgressEvent? OnProgress { get; set; }
}

/// <summary>Messages and routines of BoldLockHandler.</summary>
public static class BoldLockHandler
{
    [Delphi(PortStatus.Ported)] public const string BOLD_GET_LOCKS_FAILED_ERROR = "Failed to get locks";

    [Delphi(PortStatus.Ported)]
    internal static TBoldRegionList NewRegionListFromStrings(TStrings Locks, TBoldRegionFactory Factory)
    {
        var result = new TBoldRegionList();
        for (int i = 0; i < Locks.Count; i++)
        {
            result.Add(Factory.GetRegionByName(Locks[i]));
        }

        return result;
    }

    /// <summary>Adds the lock names of the regions whose roots are stored objects.</summary>
    [Delphi(PortStatus.Ported)]
    internal static void RegionListToLockList(TBoldRegionList RegionList, TBoldLockList LockList)
    {
        for (int i = 0; i < RegionList.Count; i++)
        {
            if (RegionList[i].Root.BoldObjectLocator!.BoldObjectID.IsStorable)
            {
                LockList.AddLock(RegionList[i].AsString);
            }
        }
    }
}
