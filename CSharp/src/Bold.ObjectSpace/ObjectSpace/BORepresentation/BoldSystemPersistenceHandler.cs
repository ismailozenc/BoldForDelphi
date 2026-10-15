// Delphi: Source/ObjectSpace/BORepresentation/BoldSystemPersistenceHandler.pas
// Port map: CSharp/port/units/BoldSystemPersistenceHandler.md

using static Bold.BoldCoreConsts;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>
/// Fetches and saves the objects of a system through its persistence controller: fetching objects, members, classes
/// and links, queries with conditions, the update with its enclosure and optimistic locks, and time mapping.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemPersistenceHandler : TBoldAbstractSystemPersistenceHandler
{
    private TBoldTimeStampType fTimeStampOfLatestUpdate;
    private readonly TBoldTimeMappingCache fTimestampToTimeCache;
    private readonly TBoldTimeMappingCache fTimeToTimestampCache;

    [Delphi(PortStatus.Ported)]
    public TBoldSystemPersistenceHandler(TBoldSystem System)
        : base(System)
    {
        fTimeStampOfLatestUpdate = -1;
        fTimestampToTimeCache = new TBoldTimeMappingCache(100);
        fTimeToTimestampCache = new TBoldTimeMappingCache(20);
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldSystemPersistenceHandler.Destroy", Note = "Freed the caches; the garbage collector does that in .NET.")]
    public override void Dispose() => base.Dispose();

    [Delphi(PortStatus.Ported)]
    private void DoPreUpdate() => OnPreUpdate?.Invoke(this);

    /// <summary>
    /// Adds to the list the objects that must be saved with it (other ends of changed single links stored in them, old
    /// link ends, optimistic locking enclosure); true when the list already was its enclosure.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override bool EnsureEnclosure(TBoldObjectList ObjectList, bool ValidateOnly)
    {
        bool ListIsEnclosure = true;

        void AddLocatorToEnclosure(TBoldObjectLocator? Locator)
        {
            if (Locator is not null && Locator.ObjectIsPersistent && !ObjectList.LocatorInList(Locator))
            {
                if (!ValidateOnly)
                {
                    if (Locator.BoldObjectID is TBoldInternalObjectId && Locator.BoldObject is null)
                    {
                        throw new EBoldInternal("Internal Error, a deleted non-saved object got into the enclosure, try to figure out why...");
                    }

                    ObjectList.AddLocator(Locator);
                }

                ListIsEnclosure = false;
            }
        }

        void AddUnsavedLocatorToEnclosure(TBoldObjectLocator? Locator)
        {
            if (Locator is not null && !Locator.BoldObjectID.IsStorable)
            {
                AddLocatorToEnclosure(Locator);
            }
        }

        void AddIdToEnclosure(TBoldObjectId? Id)
        {
            if (Id is not null)
            {
                AddLocatorToEnclosure(System.EnsuredLocatorByID(Id));
            }
        }

        void AddOldValuesForRole(TBoldMember Role, TBoldRoleRTInfo RoleRTInfo)
        {
            IBoldValue? OldValue = Role.OldValue;
            if (OldValue is null)
            {
                return;
            }

            if (RoleRTInfo.IsIndirect)
            {
                if (RoleRTInfo.IsMultiRole)
                {
                    var pair = (IBoldObjectIdListRefPair)OldValue;
                    for (int i = 0; i < pair.Count; i++)
                    {
                        AddIdToEnclosure(pair.IdList1(i));
                    }
                }
                else
                {
                    AddIdToEnclosure(((IBoldObjectIdRefPair)OldValue).Id1);
                }
            }
            else if (RoleRTInfo.IsMultiRole)
            {
                var list = (IBoldObjectIdListRef)OldValue;
                for (int i = 0; i < list.Count; i++)
                {
                    AddIdToEnclosure(list.IdList(i));
                }
            }
            else if (RoleRTInfo.RoleRTInfoOfOtherEnd!.IsStoredInObject)
            {
                AddIdToEnclosure(((IBoldObjectIdRef)OldValue).Id);
            }
        }

        int I = 0;
        while (I < ObjectList.Count)
        {
            TBoldObject ExamineBoldObject = ObjectList[I]!;
            for (int M = 0; M < ExamineBoldObject.BoldClassTypeInfo.AllMembers.Count; M++)
            {
                if (ExamineBoldObject.BoldClassTypeInfo.AllMembers[M] is not TBoldRoleRTInfo RoleRTInfo)
                {
                    continue;
                }

                if (RoleRTInfo.IsSingleRole && RoleRTInfo.Persistent && RoleRTInfo.IsStoredInObject &&
                    ExamineBoldObject.BoldMemberAssigned(M) &&
                    ExamineBoldObject.BoldMembers(M).BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
                {
                    var ObjectReference = (TBoldObjectReference)ExamineBoldObject.BoldMembers(M);
                    if (RoleRTInfo.RoleRTInfoOfOtherEnd!.IsStoredInObject)
                    {
                        AddLocatorToEnclosure(ObjectReference.Locator);
                        AddOldValuesForRole(ObjectReference, RoleRTInfo);
                    }
                    else
                    {
                        AddUnsavedLocatorToEnclosure(ObjectReference.Locator);
                        if (RoleRTInfo.RoleRTInfoOfOtherEnd.IsSingleRole && ObjectReference.Locator?.BoldObject is { } otherObject)
                        {
                            var OtherEnd = (TBoldObjectReference)otherObject.BoldMembers(RoleRTInfo.IndexOfOtherEnd);
                            AddIdToEnclosure(OtherEnd.OldEmbeddingOtherEndId);
                        }
                    }
                }

                if (ExamineBoldObject.BoldObjectIsDeleted)
                {
                    AddOldValuesForRole(ExamineBoldObject.BoldMembers(M), RoleRTInfo);
                }
            }

            System.OptimisticLockHandler!.EnsureEnclosure(ExamineBoldObject, ObjectList, ValidateOnly, ref ListIsEnclosure);
            I++;
        }

        return ListIsEnclosure;
    }

    /// <summary>Fetches, in two rounds, the link of all objects in the list (when not stored in them) and the objects it reaches.</summary>
    [Delphi(PortStatus.Ported)]
    public override void FetchLinksWithObjects(TBoldObjectList ObjectList, string LinkName, TBoldObjectList? FetchedObjects)
    {
        if (!System.BoldPersistent || ObjectList.Count == 0)
        {
            return;
        }

        var FetchList = new TBoldObjectList();
        var MemberIdList = new TBoldMemberIdList();
        try
        {
            void InternalAddLocator(TBoldObjectLocator? Locator)
            {
                if (Locator is not null && Locator.BoldObject is null)
                {
                    FetchList.AddLocator(Locator);
                }
            }

            ObjectList.EnsureObjects();

            TBoldClassTypeInfo? CommonClass = ObjectList[0]!.BoldClassTypeInfo;
            for (int I = 1; I < ObjectList.Count; I++)
            {
                CommonClass = CommonClass?.LeastCommonSuperClass(ObjectList[I]!.BoldClassTypeInfo);
                if (ObjectList[I]!.BoldSystem != System)
                {
                    throw new EBoldFeatureNotImplementedYet(sCannotFetchWithLinksFromMultipleSystems);
                }
            }

            if (CommonClass is null)
            {
                throw new EBold(SysUtils.Format(sNoCommonSuperClass, GetType().Name));
            }

            if (CommonClass.MemberRTInfoByExpressionName(LinkName) is not TBoldRoleRTInfo roleRTInfo)
            {
                throw new EBold(SysUtils.Format(sNoRoleCalledX, GetType().Name, CommonClass.ExpressionName, LinkName));
            }

            if (!roleRTInfo.IsStoredInObject && roleRTInfo.Persistent && roleRTInfo.RoleType == TBoldRoleType.rtRole)
            {
                for (int I = 0; I < ObjectList.Count; I++)
                {
                    if (ObjectList[I]!.BoldMembers(roleRTInfo.index).BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid)
                    {
                        FetchList.Add(ObjectList[I]);
                    }
                }

                if (FetchList.Count > 0)
                {
                    MemberIdList.Add(new TBoldMemberID(roleRTInfo.index));
                    PMFetch(FetchList, MemberIdList);
                }
            }

            FetchList.Clear();
            for (int I = 0; I < ObjectList.Count; I++)
            {
                if (roleRTInfo.IsMultiRole)
                {
                    var MultiLink = (TBoldObjectList)ObjectList[I]!.BoldMembers(roleRTInfo.index);
                    for (int J = 0; J < MultiLink.Count; J++)
                    {
                        InternalAddLocator(MultiLink.Locators(J));
                    }
                }
                else
                {
                    InternalAddLocator(((TBoldObjectReference)ObjectList[I]!.BoldMembers(roleRTInfo.index)).Locator);
                }
            }

            if (FetchList.Count > 0)
            {
                PMFetch(FetchList, null);
            }

            FetchedObjects?.Assign(FetchList);
        }
        finally
        {
            FetchList.Dispose();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void FetchObjectById(TBoldObjectId BoldObjectId)
    {
        var ObjectList = new TBoldObjectList();
        try
        {
            ObjectList.AddLocator(System.EnsuredLocatorByID(BoldObjectId));
            PMFetch(ObjectList, null);
        }
        finally
        {
            ObjectList.Dispose();
        }
    }

    /// <summary>Adds the objects matching the condition (except those deleted in memory) to the list.</summary>
    [Delphi(PortStatus.Ported)]
    public override void GetAllWithCondition(TBoldObjectList aList, TBoldCondition Condition)
    {
        if (PersistenceController is null)
        {
            throw new EBold(sNoPersistenceController);
        }

        var ObjectIdList = new TBoldObjectIdList();
        PersistenceController.PMFetchIDListWithCondition(ObjectIdList, System.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepPMIn), BoldDefs.fmNormal,
            Condition, BoldPersistenceController.NOTVALIDCLIENTID);
        for (int I = ObjectIdList.Count - 1; I >= 0; I--)
        {
            if (System.Locators.LocatorByID(ObjectIdList[I])?.BoldObject is { BoldExistenceState: TBoldExistenceState.besDeleted })
            {
                ObjectIdList.RemoveByIndex(I);
            }
        }

        aList.FillFromIDList(ObjectIdList, System);
    }

    /// <summary>Adds the objects of the class matching the SQL where clause to the list.</summary>
    [Delphi(PortStatus.Ported)]
    public override void GetAllInClassWithSQL(TBoldObjectList aList, TBoldObjectClass AClass, string WhereClause, string OrderByClause, TParams? Params,
        bool JoinInheritedTables, int MaxAnswers, int Offset)
    {
        var Condition = new TBoldSQLCondition
        {
            WhereFragment = WhereClause,
            OrderBy = OrderByClause,
            JoinInheritedTables = JoinInheritedTables,
            MaxAnswers = MaxAnswers,
            Offset = Offset,
        };

        if (AClass is null)
        {
            throw new EBold(SysUtils.Format(sClassParameterMissing, GetType().Name));
        }

        TBoldClassTypeInfo ClassTypeInfo = System.BoldSystemTypeInfo.TopSortedClasses.ItemsByObjectClass(AClass) ??
            throw new EBold(SysUtils.Format(sNoSuchClassInModel, GetType().Name, AClass.Name));
        Condition.TopSortedIndex = ClassTypeInfo.TopSortedIndex;
        Condition.Params = Params ?? new TParams();
        GetAllWithCondition(aList, Condition);
    }

    /// <summary>
    /// Saves the persistent, loaded objects of the list with their enclosure, optimistic locks and pessimistic locks;
    /// a failed optimistic lock goes to OnOptimisticLockingFailed or raises EBoldOperationFailedForObjectList.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void UpdateDatabaseWithList(TBoldObjectList ObjectList)
    {
        if (System.InTransaction())
        {
            throw new EBold(sCannotUpdateWhileInTransaction);
        }

        System.DelayObjectDestruction();
        var ObjectsToUpdate = (TBoldObjectList)ObjectList.Clone();
        try
        {
            for (int i = ObjectsToUpdate.Count - 1; i >= 0; i--)
            {
                if (ObjectsToUpdate.Locators(i)!.BoldSystem != System)
                {
                    throw new EBold(SysUtils.Format(sForeignObjectInUpdate, GetType().Name));
                }

                if (ObjectsToUpdate.Locators(i)!.BoldObject is null || !ObjectsToUpdate[i]!.BoldPersistent)
                {
                    ObjectsToUpdate.RemoveByIndex(i);
                }
            }

            if (ObjectsToUpdate.Count > 0)
            {
                DoPreUpdate();
                EnsureEnclosure(ObjectsToUpdate, false);
                TBoldObjectIdList ObjectIdList = ObjectsToUpdate.CreateObjectIdList();
                var aTranslationList = new TBoldIDTranslationList();
                TBoldOptimisticLockingPrecondition? Precondition = null;
                if (System.BoldSystemTypeInfo.OptimisticLocking != TBoldOptimisticLockingMode.bolmOff)
                {
                    Precondition = new TBoldOptimisticLockingPrecondition();
                    System.OptimisticLockHandler!.AddOptimisticLocks(ObjectsToUpdate, Precondition);
                    if (!Precondition.HasOptimisticLocks)
                    {
                        Precondition.Dispose();
                        Precondition = null;
                    }
                }

                try
                {
                    if (System.PessimisticLockHandler is not null && !System.PessimisticLockHandler.EnsureLocks())
                    {
                        throw new EBold(SysUtils.Format(sRequiredLocksNotHeld, GetType().Name));
                    }

                    BoldClearLastFailure();

                    try
                    {
                        if (!StartUpdateForAll(ObjectsToUpdate))
                        {
                            BoldRaiseLastFailure(System, "UpdateDatabaseWithList", sStartUpdateFailed);
                        }

                        PersistenceController!.PMUpdate(ObjectIdList, System.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepPMOut), null, Precondition,
                            aTranslationList, ref fTimeStampOfLatestUpdate, BoldPersistenceController.NOTVALIDCLIENTID);
                        if (Precondition is not null && Precondition.Failed)
                        {
                            if (System.OnOptimisticLockingFailed is not null)
                            {
                                var FailureList = new TBoldObjectList();
                                try
                                {
                                    FailureList.FillFromIDList(Precondition.FailureList, System);
                                    System.OnOptimisticLockingFailed(ObjectsToUpdate, FailureList, Precondition.FailureReason);
                                }
                                finally
                                {
                                    FailureList.Dispose();
                                }
                            }
                            else
                            {
                                throw new EBoldOperationFailedForObjectList(Precondition.FailureReason, [], Precondition.FailureList, System);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < ObjectsToUpdate.Count; i++)
                            {
                                ObjectsToUpdate[i]!.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepPMIn).TimeStamp = TimeStampOfLatestUpdate;
                            }

                            EndUpdateForAll(ObjectsToUpdate, aTranslationList);
                        }
                    }
                    catch (Exception e) when (GetBoldLastFailureReason() is not null)
                    {
                        BoldRaiseLastFailure(System, "UpdateDatabaseWithlist", e.Message);
                    }
                }
                finally
                {
                    aTranslationList.Dispose();
                    Precondition?.Dispose();
                }
            }
        }
        finally
        {
            System.PessimisticLockHandler?.ReleaseUnneededRegions();
            ObjectsToUpdate.Dispose();
            System.AllowObjectDestruction();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldTimeStampType GetTimeStampOfLatestUpdate() => fTimeStampOfLatestUpdate;

    [Delphi(PortStatus.Ported)]
    private TBoldPersistenceController? GetPersistenceController() => System.PersistenceController;

    /// <summary>Fetches the member's object (just the member when its fetch is delayed).</summary>
    [Delphi(PortStatus.Ported)]
    public override void FetchMember(TBoldMember Member)
    {
        var ObjectList = new TBoldObjectList();
        try
        {
            ObjectList.Add(Member.OwningObject);
            TBoldMemberIdList? MemberIdList = null;
            if (Member.BoldMemberRTInfo!.DelayedFetch)
            {
                MemberIdList = new TBoldMemberIdList();
                MemberIdList.Add(new TBoldMemberID(Member.BoldMemberRTInfo.index));
            }

            PMFetch(ObjectList, MemberIdList);
        }
        finally
        {
            ObjectList.Dispose();
        }
    }

    /// <summary>Fetches the ids of the class's objects (at the time) and sets the class list from them.</summary>
    [Delphi(PortStatus.Ported)]
    public override void FetchClass(TBoldObjectList ClassList, TBoldTimeStampType Time)
    {
        if (PersistenceController is null)
        {
            throw new EBold(sNoPersistenceController);
        }

        var ClassTypeInfo = (TBoldClassTypeInfo)((TBoldListTypeInfo)ClassList.BoldType!).ListElementTypeInfo!;
        var ObjectIdList = new TBoldObjectIdList();
        var Condition = new TBoldConditionWithClass { TopSortedIndex = ClassTypeInfo.TopSortedIndex, Time = Time };
        PersistenceController.PMFetchIDListWithCondition(ObjectIdList, System.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepPMIn), BoldDefs.fmNormal,
            Condition, BoldPersistenceController.NOTVALIDCLIENTID);
        ClassList.ProxyInterface(typeof(IBoldObjectIdListRef), TBoldDomainElementProxyMode.bdepPMIn, out object? ListInterface);
        ((IBoldObjectIdListRef)ListInterface!).SetFromIdList(ObjectIdList);
    }

    [Delphi(PortStatus.Ported)]
    public override TDateTime GetTimeForTimestamp(TBoldTimeStampType Timestamp)
    {
        TDateTime result = default;
        if (!fTimestampToTimeCache.TimeForTimestamp(Timestamp, ref result))
        {
            PersistenceController!.PMTimeForTimestamp(Timestamp, ref result);
            fTimestampToTimeCache.Add(Timestamp, result);
        }

        return result;
    }

    [Delphi(PortStatus.Ported, Note = "Does not use its cache, as in Delphi.")]
    public override TBoldTimeStampType GetTimestampForTime(TDateTime ClockTime)
    {
        TBoldTimeStampType result = 0;
        PersistenceController!.PMTimestampForTime(ClockTime, ref result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override void FetchList(TBoldObjectList FetchList) => PMFetch(FetchList, null);

    /// <summary>Fetches the objects (or just the members) of the list into the system and ends their fetch.</summary>
    [Delphi(PortStatus.Ported)]
    private void PMFetch(TBoldObjectList ObjectList, TBoldMemberIdList? MemberIdList)
    {
        if (ObjectList.Count > 0)
        {
            var ObjectIdList = new TBoldObjectIdList();
            for (int i = 0; i < ObjectList.Count; i++)
            {
                ObjectIdList.Add(ObjectList.Locators(i)!.BoldObjectID);
            }

            PersistenceController!.PMFetch(ObjectIdList, System.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepPMIn), MemberIdList, BoldDefs.fmNormal,
                BoldPersistenceController.NOTVALIDCLIENTID);
            EndFetchForAll(ObjectList, MemberIdList);
        }
    }

    [Delphi(PortStatus.Ported)] protected TBoldPersistenceController? PersistenceController => GetPersistenceController();

    // The time-to-timestamp cache is created as in Delphi but not used there either.
    internal TBoldTimeMappingCache TimeToTimestampCache => fTimeToTimestampCache;
}

/// <summary>A small ring buffer of timestamp and clock time pairs.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTimeMappingCache : TBoldMemoryManagedObject
{
    private readonly TBoldTimeStampType[] fTimestamps;
    private readonly TDateTime[] fClocktimes;
    private int fNext;
    private int fCount;

    [Delphi(PortStatus.Ported)]
    public TBoldTimeMappingCache(int MaxCount)
    {
        fTimestamps = new TBoldTimeStampType[MaxCount];
        fClocktimes = new TDateTime[MaxCount];
    }

    [Delphi(PortStatus.Ported)]
    public bool TimeForTimestamp(TBoldTimeStampType Timestamp, ref TDateTime ClockTime)
    {
        for (int i = 0; i <= Math.Min(fTimestamps.Length - 1, fCount - 1); i++)
        {
            if (fTimestamps[i] == Timestamp)
            {
                ClockTime = fClocktimes[i];
                return true;
            }
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    public bool TimestampForTime(TDateTime ClockTime, ref TBoldTimeStampType Timestamp)
    {
        for (int i = 0; i <= Math.Min(fClocktimes.Length - 1, fCount - 1); i++)
        {
            if (fClocktimes[i] == ClockTime)
            {
                Timestamp = fTimestamps[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>Adds the pair, overwriting the oldest when full.</summary>
    [Delphi(PortStatus.Ported)]
    public void Add(TBoldTimeStampType Timestamp, TDateTime ClockTime)
    {
        fTimestamps[fNext] = Timestamp;
        fClocktimes[fNext] = ClockTime;
        fNext = (fNext + 1) % fTimestamps.Length;
        if (fCount < fTimestamps.Length)
        {
            fCount++;
        }
    }
}
