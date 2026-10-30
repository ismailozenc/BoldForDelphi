// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
//
// BoldSystem is split by class over several files: this one has the constants, the system and its extensions;
// BoldSystem.Locators.cs, BoldSystem.Object.cs, BoldSystem.Member.cs, BoldSystem.Lists.cs, BoldSystem.Controllers.cs
// and BoldSystem.Proxies.cs have the rest.

using static Bold.BoldCoreConsts;
using static Bold.BoldElements;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldSystem", Symbol = "TBoldObjectClass", Note = "Global using alias of System.Type, in BoldSystem.Aliases.cs; the constructors it is used for go through TBoldObject.CreateInstance.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSystem", Symbol = "TBoldAttributeClass", Note = "Global using alias of System.Type, in BoldSystem.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSystem", Symbol = "TBoldMemberClass", Note = "Global using alias of System.Type, in BoldSystem.Aliases.cs; the constructors it is used for go through TBoldMember.CreateInstance.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSystem", Symbol = "TBoldListClass", Note = "Global using alias of System.Type, in BoldSystem.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSystem", Symbol = "TBoldObjectListClass", Note = "Global using alias of System.Type, in BoldSystem.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSystem", Symbol = "TBoldMember_ProxyClass", Note = "Global using alias of System.Type, in BoldSystem.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldSystem", Symbol = "initialization", Note = "Creates G_ExternalDerivers, in its static initializer.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSystem", Symbol = "finalization", Note = "Freed G_ExternalDerivers; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Raised when a null attribute is read as a value.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldAccessNullValue(string? message = null, Exception? innerException = null) : EBold(message, innerException);

/// <summary>stmUnsafe runs no transactions; stmNormal wraps each change in one.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldSystemTransactionMode { stmUnsafe, stmNormal }

/// <summary>What a list does with a duplicate: drop it, keep it or raise.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldListDupMode { bldmMerge, bldmAllow, bldmError }

[Delphi(PortStatus.Ported)]
public enum TBoldTransactionState { btsNone, btsInTransaction, btsRollingBack }

/// <summary>How a link change marks the other end: not at all, modified, or adjusted.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldLinkUnlinkMode { blulNone, blulMarkModified, blulMarkAdjusted }

[Delphi(PortStatus.Ported)]
public delegate void TBoldPrepareListOperation(TBoldList List);

[Delphi(PortStatus.Ported)]
public delegate int TBoldElementCompare(TBoldElement? Item1, TBoldElement? Item2);

[Delphi(PortStatus.Ported)]
public delegate void TBoldOptimisticLockingFailedEvent(TBoldObjectList UpdateList, TBoldObjectList FailureList, string FailureReason);

[Delphi(PortStatus.Ported)]
public delegate void TBoldCreateApproximateObjectError(TBoldObject Obj);

/// <summary>A helper object that belongs to one system.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemExtension : TBoldNonRefCountedObject, IDisposable
{
    private readonly TBoldSystem fSystem;

    [Delphi(PortStatus.Ported)]
    public TBoldSystemExtension(TBoldSystem System)
    {
        fSystem = System;
    }

    /// <summary>Frees what the extension owns; nothing here.</summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);

    [Delphi(PortStatus.Ported)] public TBoldSystem System => fSystem;
}

/// <summary>Takes pessimistic locks for elements about to change.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractPessimisticLockHandler(TBoldSystem System) : TBoldSystemExtension(System)
{
    [Delphi(PortStatus.Ported)] public abstract bool LockElement(TBoldDomainElement Element);
    [Delphi(PortStatus.Ported)] public abstract void ReleaseUnneededRegions();
    [Delphi(PortStatus.Ported)] public abstract bool EnsureLocks();
}

/// <summary>Adds optimistic locks (old values) to an update and finds the objects that must be saved together.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractOptimisticLockHandler(TBoldSystem System) : TBoldSystemExtension(System)
{
    [Delphi(PortStatus.Ported)]
    private IBoldValueSpace GetOldValues() => System.fOldValueHandler.OldValues;

    [Delphi(PortStatus.Ported)]
    public abstract void AddOptimisticLocks(TBoldObjectList ObjectList, TBoldOptimisticLockingPrecondition PreCondition);

    [Delphi(PortStatus.Ported)]
    public abstract void EnsureEnclosure(TBoldObject Obj, TBoldObjectList Enclosure, bool ValidateOnly, ref bool ListIsEnclosure);

    [Delphi(PortStatus.Ported)] public IBoldValueSpace OldValues => GetOldValues();
}

/// <summary>Records changes so they can be undone and redone.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractUndoHandler(TBoldSystem System) : TBoldSystemExtension(System)
{
    [Delphi(PortStatus.Ported)]
    protected static TBoldAbstractController? GetControllerForMember(TBoldMember Member) => Member.GetController();

    /// <summary>Destroys the object unless it is modified, and adds it to the running transaction.</summary>
    [Delphi(PortStatus.Ported)]
    protected void DeleteObject(TBoldObject BoldObject)
    {
        if (BoldObject.BoldPersistenceState != TBoldValuePersistenceState.bvpsModified)
        {
            System.DestroyObject(BoldObject);
        }

        System.AddToTransaction(BoldObject);
    }

    [Delphi(PortStatus.Ported)] public abstract void HandleMember(IBoldObjectContents ObjectContents, int MemberIndex, IBoldValue MemberValue);
    [Delphi(PortStatus.Ported)] public abstract void HandleObject(IBoldObjectContents Obj, bool RegardAsExisting);
    [Delphi(PortStatus.Ported)] public abstract void PrepareUpdate(TBoldObjectList ObjectList);
    [Delphi(PortStatus.Ported)] public abstract void ApplytranslationList(TBoldIDTranslationList IdTranslationList);
}

/// <summary>Keeps the values objects had in the database, for optimistic locking.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractOldValueHandler(TBoldSystem System) : TBoldSystemExtension(System)
{
    [Delphi(PortStatus.Ported)] protected internal abstract IBoldValueSpace GetOldValues();
    [Delphi(PortStatus.Ported)] protected abstract bool GetIsEmpty();
    [Delphi(PortStatus.Ported)] protected internal abstract void PurgeEqualValues();

    /// <summary>A new, empty value for the member in the value space; nil when the space already has one.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal static IBoldValue? NewValueInValueSpace(TBoldMember BoldMember, IBoldValueSpace ValueSpace)
    {
        IBoldObjectContents ObjectContents = ValueSpace.EnsuredObjectContentsByObjectId(BoldMember.OwningObject!.BoldObjectLocator!.BoldObjectID);
        var MemberId = new TBoldMemberID(BoldMember.BoldMemberRTInfo!.index);
        if (ObjectContents.ValueByMemberId(MemberId) is not null)
        {
            return null;
        }

        ObjectContents.EnsureMember(MemberId, BoldMember.GetStreamName());
        return ObjectContents.ValueByMemberId(MemberId);
    }

    /// <summary>Copies the member's value into the value space unless it already has one.</summary>
    [Delphi(PortStatus.Ported)]
    protected static void CopyMemberToValueSpace(TBoldMember BoldMember, IBoldValueSpace ValueSpace)
    {
        TBoldObject BoldObject = BoldMember.OwningObject!;
        CopyObjectToValueSpace(BoldObject, ValueSpace);
        IBoldValue? Value = NewValueInValueSpace(BoldMember, ValueSpace);
        Value?.AssignContent(BoldMember.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)!);
    }

    /// <summary>Adds the object, with its existence and persistence state, to the value space unless it is there.</summary>
    [Delphi(PortStatus.Ported)]
    protected static void CopyObjectToValueSpace(TBoldObject BoldObject, IBoldValueSpace ValueSpace)
    {
        TBoldObjectId anObjectId = BoldObject.BoldObjectLocator!.BoldObjectID;
        IBoldObjectContents? anObjectContents = ValueSpace.ObjectContentsByObjectId(anObjectId);
        if (anObjectContents is null)
        {
            ValueSpace.EnsureObjectContents(anObjectId);
            anObjectContents = ValueSpace.ObjectContentsByObjectId(anObjectId)!;
            anObjectContents.BoldExistenceState = BoldObject.BoldExistenceState;
            anObjectContents.BoldPersistenceState = BoldObject.BoldPersistenceState;
        }
    }

    [Delphi(PortStatus.Ported)] public abstract void MemberValuePreChange(TBoldMember BoldMember);
    [Delphi(PortStatus.Ported)] public abstract void MemberPersistenceStatePreChange(TBoldMember BoldMember, TBoldValuePersistenceState NewState);
    [Delphi(PortStatus.Ported)] public abstract void ObjectExistenceChange(TBoldObject BoldObject);
    [Delphi(PortStatus.Ported)] public abstract void ObjectExistencePersistenceStateChange(TBoldObject BoldObject, TBoldValuePersistenceState NewState);
    [Delphi(PortStatus.Ported)] public IBoldValueSpace OldValues => GetOldValues();
    [Delphi(PortStatus.Ported)] public bool IsEmpty => GetIsEmpty();
}

/// <summary>An old value handler that also runs transactions.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractTransActionHandler(TBoldSystem System) : TBoldAbstractOldValueHandler(System)
{
    [Delphi(PortStatus.Ported)] protected abstract TBoldSystemTransactionMode GetTransactionMode();
    [Delphi(PortStatus.Ported)] protected abstract TBoldTransactionState GetTransactionState();
    [Delphi(PortStatus.Ported)] public abstract void StartTransaction(TBoldSystemTransactionMode MinimalMode = TBoldSystemTransactionMode.stmNormal);
    [Delphi(PortStatus.Ported)] public abstract void CommitTransaction(TBoldSystemTransactionMode MinimalMode = TBoldSystemTransactionMode.stmNormal);
    [Delphi(PortStatus.Ported)] public abstract void RollbackTransaction(TBoldSystemTransactionMode MinimalMode = TBoldSystemTransactionMode.stmNormal);
    [Delphi(PortStatus.Ported)] public TBoldSystemTransactionMode TransactionMode => GetTransactionMode();
    [Delphi(PortStatus.Ported)] public TBoldTransactionState TransactionState => GetTransactionState();
}

/// <summary>Fetches objects and members and writes changes through the system's persistence controller.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractSystemPersistenceHandler(TBoldSystem System) : TBoldSystemExtension(System)
{
    private TNotifyEvent? fOnPreUpdate;

    [Delphi(PortStatus.Ported)] protected abstract TBoldTimeStampType GetTimeStampOfLatestUpdate();
    [Delphi(PortStatus.Ported)] public abstract bool EnsureEnclosure(TBoldObjectList ObjectList, bool ValidateOnly);
    [Delphi(PortStatus.Ported)] public abstract void FetchLinksWithObjects(TBoldObjectList ObjectList, string LinkName, TBoldObjectList? FetchedObjects);
    [Delphi(PortStatus.Ported)] public abstract void FetchObjectById(TBoldObjectId BoldObjectId);
    [Delphi(PortStatus.Ported)] public abstract void FetchMember(TBoldMember Member);
    [Delphi(PortStatus.Ported)] public abstract void FetchList(TBoldObjectList FetchList);
    [Delphi(PortStatus.Ported)] public abstract void FetchClass(TBoldObjectList ClassList, TBoldTimeStampType Time);
    [Delphi(PortStatus.Ported)] public abstract void GetAllWithCondition(TBoldObjectList aList, TBoldCondition Condition);

    [Delphi(PortStatus.Ported)]
    public abstract void GetAllInClassWithSQL(TBoldObjectList aList, TBoldObjectClass AClass, string WhereClause, string OrderByClause, TParams? Params,
        bool JoinInheritedTables, int MaxAnswers, int Offset);

    [Delphi(PortStatus.Ported)] public abstract void UpdateDatabaseWithList(TBoldObjectList ObjectList);
    [Delphi(PortStatus.Ported)] public abstract TDateTime GetTimeForTimestamp(TBoldTimeStampType Timestamp);
    [Delphi(PortStatus.Ported)] public abstract TBoldTimeStampType GetTimestampForTime(TDateTime ClockTime);

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? OnPreUpdate
    {
        get => fOnPreUpdate;
        set => fOnPreUpdate = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldTimeStampType TimeStampOfLatestUpdate => GetTimeStampOfLatestUpdate();

    /// <summary>Tells each fetched object which members were fetched.</summary>
    [Delphi(PortStatus.Ported)]
    public void EndFetchForAll(TBoldObjectList ObjectList, TBoldMemberIdList? MemberIdList)
    {
        for (int i = 0; i < ObjectList.Count; i++)
        {
            ObjectList[i]?.EndFetchMembers(MemberIdList);
        }
    }

    /// <summary>Finishes the update of each saved object, and destroys the ones that were deleted.</summary>
    [Delphi(PortStatus.Ported)]
    public void EndUpdateForAll(TBoldObjectList ObjectList, TBoldIDTranslationList? Translationlist)
    {
        for (int i = 0; i < ObjectList.Count; i++)
        {
            TBoldObject? anObject = ObjectList[i];
            if (anObject is not null)
            {
                if (anObject.GetIsModified() || System.BoldSystemTypeInfo.UpdateWholeObjects)
                {
                    anObject.EndUpdate(Translationlist);
                }

                anObject.EndUpdateMembers(Translationlist);
                anObject.CompleteUpdate();
                if (anObject.BoldObjectIsDeleted)
                {
                    System.DestroyObject(anObject);
                }
            }
        }
    }

    /// <summary>True, after starting the update of each dirty object, when every object may be saved.</summary>
    [Delphi(PortStatus.Ported)]
    public bool StartUpdateForAll(TBoldObjectList ObjectList)
    {
        bool ObjectProhibitsUpdate(TBoldObject anObject) =>
            ((anObject.BoldDirty || System.BoldSystemTypeInfo.UpdateWholeObjects) && !anObject.CanUpdate()) || !anObject.MayUpdateMembers();

        for (int i = 0; i < ObjectList.Count; i++)
        {
            if (ObjectProhibitsUpdate(ObjectList[i]!))
            {
                return false;
            }
        }

        for (int i = 0; i < ObjectList.Count; i++)
        {
            TBoldObject? anObject = ObjectList[i];
            if (anObject is not null && (anObject.BoldDirty || System.BoldSystemTypeInfo.UpdateWholeObjects))
            {
                anObject.DoStartUpdate();
            }
        }

        return true;
    }
}

/// <summary>Gives the lock regions an element belongs to.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractRegionFactory : TBoldMemoryManagedObject
{
    internal TBoldSystem? fSystem;

    [Delphi(PortStatus.Ported)] protected TBoldSystem? System => fSystem;

    [Delphi(PortStatus.Ported)] public abstract void GetRegionsForElement(TBoldDomainElement Element, TList ResultList);
}

/// <summary>
/// The object space: all objects of one model, found through their locators, with the class lists, the dirty list,
/// transactions, undo, fetching and saving.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystem : TBoldDomainElement
{
    private TBoldSystemTransactionMode fTransactionMode;
    private readonly TBoldSystemTypeInfo fBoldSystemTypeInfo = null!;
    private TBoldMemberList? fClasses;
    private int fDelayedDestructionCount;
    private TList? fDelayedDestructionList;
    private TList? fDirtyObjects;
    private bool fDirtyObjectsInvalid;
    private TBoldEvaluator? fEvaluator;
    private TBoldSystemLocatorList? fLocators;
    private TBoldObjectList? FNewDirtyList;
    private TBoldObjectList? fNewModifiedList;
    private readonly TBoldPersistenceController? fPersistenceController;
    private TBoldFreeStandingValueSpace? fRollbackArea;
    private TBoldFreeStandingValueSpace? fValidValueArea;
    private int fTransactionNesting;
    private bool fTransactionRollbackOnly;
    private TBoldDomainElementCollection? fTransactionList;
    private TBoldAbstractPessimisticLockHandler? fPessimisticLockHandler;
    private TBoldAbstractOptimisticLockHandler? fOptimisticLockHandler;
    internal TBoldAbstractOldValueHandler fOldValueHandler = null!;
    private readonly TBoldAbstractRegionFactory? fRegionFactory;
    private TBoldOptimisticLockingFailedEvent? fOptimisticLockingFailed;
    internal TBoldAbstractSystemPersistenceHandler fSystemPersistenceHandler = null!;
    private TBoldAbstractUndoHandler? fUndoHandler;
    private TBoldCreateApproximateObjectError? fOnCreateApproximateObjectError;

    /// <summary>Raises: a system needs a type info, use the other constructor.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldSystem.Create")]
    public TBoldSystem(TBoldDomainElement? AOwningElement)
        : base(AOwningElement)
    {
        throw new EBold(SysUtils.Format(sIllegalConstruction, GetType().Name));
    }

    /// <summary>Creates the system for the model, with one class list per class.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldSystem.CreateWithTypeInfo")]
    public TBoldSystem(TBoldDomainElement? AOwningElement, TBoldSystemTypeInfo SystemTypeInfo, TBoldPersistenceController? PersistenceController,
        TBoldAbstractRegionFactory? RegionFactory = null)
        : base(AOwningElement)
    {
        fTransactionMode = TBoldSystemTransactionMode.stmNormal;
        fOldValueHandler = new TBoldOldValueHandler(this);
        fBoldSystemTypeInfo = SystemTypeInfo;
        SetElementFlag(befPersistent, SystemTypeInfo.Persistent && PersistenceController is not null);
        fPersistenceController = PersistenceController;
        fDirtyObjects = new TList();
        fDirtyObjectsInvalid = false;
        fClasses = new TBoldMemberList();
        fClasses.fBoldMetaType = BoldSystemTypeInfo.ListTypeInfoByElement(null);
        fLocators = new TBoldSystemLocatorList();
        fLocators.OwnsEntries = true;
        fDelayedDestructionList = new TList();
        fTransactionList = new TBoldDomainElementCollection();

        for (int I = 0; I < SystemTypeInfo.TopSortedClasses.Count; I++)
        {
            TBoldListTypeInfo ListTypeInfo = SystemTypeInfo.ListTypeInfoByElement(SystemTypeInfo.TopSortedClasses[I])!;
            TBoldObjectListClass ListClass = ListTypeInfo.ListClass!;
            fClasses.InternalAddWithoutCloning(TBoldObjectList.InternalCreateClassList(ListClass, this, ListTypeInfo));
        }

        fClasses.MakeImmutable();
        fOptimisticLockHandler = new TBoldOptimisticLockHandler(this);
        fSystemPersistenceHandler = new TBoldSystemPersistenceHandler(this);
        fRegionFactory = RegionFactory;

        if (fRegionFactory is not null)
        {
            fRegionFactory.fSystem = this;
        }
    }

    /// <summary>Discards derived members, unloads every object and frees the lists and handlers; raises when there are unsaved changes.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldSystem.Destroy")]
    public override void Dispose()
    {
        EnsureCanDestroy();
        PrepareToDestroy();
        IsDefault = false;
        SysUtils.FreeAndNil(ref fEvaluator);
        fClasses?.Dispose();
        fClasses = null;

        if (fLocators is not null)
        {
            using (TBoldLocatorListTraverser Traverser = Locators.CreateTraverser())
            {
                while (!Traverser.EndOfList)
                {
                    TBoldObject? bo = Traverser.locator.BoldObject;
                    if (bo is not null)
                    {
                        for (int i = 0; i < bo.BoldMemberCount; i++)
                        {
                            if (bo.BoldMemberAssigned(i) && bo.BoldMembers(i).Derived &&
                                bo.BoldMembers(i).BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid)
                            {
                                bo.BoldMembers(i).Invalidate();
                                bo.BoldMembers(i).Deriver.MarkSubscriptionOutOfdate();
                            }
                        }
                    }

                    Traverser.Next();
                }
            }

            using (TBoldLocatorListTraverser Traverser = Locators.CreateTraverser())
            {
                while (!Traverser.EndOfList)
                {
                    if (Traverser.locator.BoldObject is not null)
                    {
                        Traverser.locator.UnloadBoldObject();
                    }

                    Traverser.Next();
                }
            }
        }

        fLocators?.Dispose();
        fLocators = null;
        fDirtyObjects = null;
        fDelayedDestructionList = null;
        fTransactionList?.Dispose();
        fTransactionList = null;
        fOldValueHandler.Dispose();
        fOptimisticLockHandler?.Dispose();
        fOptimisticLockHandler = null;
        fSystemPersistenceHandler.Dispose();
        fUndoHandler?.Dispose();
        fUndoHandler = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private IBoldValueSpace GetAsIBoldvalueSpace(TBoldDomainElementProxyMode Mode)
    {
        ProxyInterface(typeof(IBoldValueSpace), Mode, out object? result);
        return (IBoldValueSpace)result!;
    }

    [Delphi(PortStatus.Ported)]
    public static TBoldSystem? DefaultSystem() => G_DefaultBoldSystem;

    [Delphi(PortStatus.Ported)]
    private TBoldObjectList? GetClassByIndex(int index) => (TBoldObjectList?)fClasses?[index];

    /// <summary>A new object of the class with the expression name.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObject CreateNewObjectByExpressionName(string ExpressionName, bool Persistent = true)
    {
        TBoldClassTypeInfo? ClassTypeInfo = BoldSystemTypeInfo.ClassTypeInfoByExpressionName(ExpressionName);
        if (ClassTypeInfo is null)
        {
            throw new EBold(SysUtils.Format(sNoSuchClass, GetType().Name, "CreateNewObjectByExpressionName", ExpressionName));
        }

        return TBoldObject.InternalCreateNewWithClassAndSystem(ClassTypeInfo.ObjectClass!, ClassTypeInfo, this, Persistent);
    }

    /// <summary>The object with the id, created (not fetched) when it is not in memory.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObject CreateExistingObjectByID(TBoldObjectId BoldObjectID)
    {
        TBoldClassTypeInfo ClassTypeInfo = BoldSystemTypeInfo.TopSortedClasses[BoldObjectID.TopSortedIndex];

        if (!BoldObjectID.TopSortedIndexExact && (ClassTypeInfo.IsAbstract || OnCreateApproximateObjectError is null))
        {
            throw new EBold(SysUtils.Format(sCannotCreateInexact, BoldObjectID.AsString, ClassTypeInfo.ModelName));
        }

        TBoldObjectLocator Locator = EnsuredLocatorByID(BoldObjectID);

        if (!Locator.BoldObjectID.TopSortedIndexExact)
        {
            Locators.UpdateID(Locator, BoldObjectID);
        }

        TBoldObject result = Locator.BoldObject ?? TBoldObject.InternalCreateWithClassAndLocator(ClassTypeInfo.ObjectClass!, ClassTypeInfo, Locator);

        if (!BoldObjectID.TopSortedIndexExact && OnCreateApproximateObjectError is not null)
        {
            OnCreateApproximateObjectError(result);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectList? FindClassByExpressionName(string ExpressionName)
    {
        TBoldClassTypeInfo? ClassTypeInfo = BoldSystemTypeInfo.ClassTypeInfoByExpressionName(ExpressionName);
        return ClassTypeInfo is null ? null : Classes(ClassTypeInfo.TopSortedIndex);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectList GetClassByExpressionName(string ExpressionName) =>
        FindClassByExpressionName(ExpressionName) ??
        throw new EBold(SysUtils.Format(sNoSuchClass, GetType().Name, "GetClassByExpressionName", ExpressionName));

    /// <summary>Adds a persistent object to the dirty list (and to NewDirtyList and NewModifiedList when set).</summary>
    [Delphi(PortStatus.Ported)]
    internal void MarkObjectDirty(TBoldObject BoldObject)
    {
        if (BoldObject.BoldPersistent)
        {
            NewDirtyList?.Add(BoldObject);
            NewModifiedList?.Add(BoldObject);

            if (!BoldObject.InDirtyList)
            {
                fDirtyObjects!.Add(BoldObject);
                BoldObject.InDirtyList = true;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void MarkObjectClean(TBoldObject BoldObject)
    {
        if (BoldObject.InDirtyList && BoldObject.BoldPersistent)
        {
            fDirtyObjects!.Remove(BoldObject);
            BoldObject.InDirtyList = false;

            if (NewDirtyList is not null && NewDirtyList.Includes(BoldObject))
            {
                NewDirtyList.Remove(BoldObject);
            }

            if (!fDirtyObjectsInvalid)
            {
                SendEvent(beDirtyListInvalidOrItemDeleted);
            }
        }
    }

    /// <summary>Adds all objects of the class (and its subclasses) to the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetAllInClass(TBoldObjectList aList, TBoldObjectClass AClass)
    {
        TBoldClassTypeInfo? ClassTypeInfo = BoldSystemTypeInfo.TopSortedClasses.ItemsByObjectClass(AClass);
        if (ClassTypeInfo is null)
        {
            throw new EBold(SysUtils.Format(sClassDoesNotBelongHere, GetType().Name, AClass.Name));
        }

        aList.AddList(Classes(ClassTypeInfo.TopSortedIndex)!);
    }

    [Delphi(PortStatus.Ported)]
    public void GetAllWithCondition(TBoldObjectList aList, TBoldCondition Condition) => SystemPersistenceHandler.GetAllWithCondition(aList, Condition);

    [Delphi(PortStatus.Ported)]
    public void GetAllInClassWithSQL(TBoldObjectList aList, TBoldObjectClass AClass, string WhereClause, string OrderByClause, TParams? Params = null,
        bool JoinInheritedTables = true, int MaxAnswers = -1, int Offset = -1) =>
        SystemPersistenceHandler.GetAllInClassWithSQL(aList, AClass, WhereClause, OrderByClause, Params, JoinInheritedTables, MaxAnswers, Offset);

    [Delphi(PortStatus.Ported)]
    public void FetchLinksWithObjects(TBoldObjectList ObjectList, string LinkName, TBoldObjectList? FetchedObjects = null) =>
        SystemPersistenceHandler.FetchLinksWithObjects(ObjectList, LinkName, FetchedObjects);

    [Delphi(PortStatus.Ported)]
    private void DirtyAsObjectList(TBoldObjectList ObjectList)
    {
        for (int i = 0; i < DirtyObjects.Count; i++)
        {
            ObjectList.Add((TBoldObject)DirtyObjects[i]!);
        }
    }

    /// <summary>Saves all dirty objects.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpdateDatabase()
    {
        var aList = (TBoldObjectList)TBoldMember.CreateWithTypeInfo(typeof(TBoldObjectList),
            BoldSystemTypeInfo.ListTypeInfoByElement(BoldSystemTypeInfo.RootClassTypeInfo)!);
        try
        {
            DirtyAsObjectList(aList);
            UpdateDatabaseWithList(aList);
        }
        finally
        {
            aList.Dispose();
        }
    }

    /// <summary>Saves the objects in the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpdateDatabaseWithList(TBoldObjectList ObjectList)
    {
        fUndoHandler?.PrepareUpdate(ObjectList);
        SystemPersistenceHandler.UpdateDatabaseWithList(ObjectList);
        fOldValueHandler.PurgeEqualValues();
    }

    /// <summary>Discards all dirty objects, also those made dirty by discarding others.</summary>
    [Delphi(PortStatus.Ported)]
    private void DiscardPersistent()
    {
        DelayObjectDestruction();
        try
        {
            while (DirtyObjects.Count > 0)
            {
                TList LocalDirtyObjects = DirtyObjects;
                for (int i = LocalDirtyObjects.Count - 1; i >= 0; i--)
                {
                    ((TBoldObject)LocalDirtyObjects[i]!).BoldObjectLocator!.DiscardBoldObject();
                }
            }
        }
        finally
        {
            AllowObjectDestruction();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void DiscardTransient()
    {
        using TBoldLocatorListTraverser Traverser = Locators.CreateTraverser();
        while (!Traverser.EndOfList)
        {
            if (Traverser.locator.BoldObject is { BoldPersistent: false })
            {
                Traverser.locator.DiscardBoldObject();
            }

            Traverser.Next();
        }
    }

    /// <summary>Discards all changes, persistent and transient, and releases unneeded lock regions.</summary>
    [Delphi(PortStatus.Ported)]
    public void Discard()
    {
        DiscardPersistent();
        DiscardTransient();
        PessimisticLockHandler?.ReleaseUnneededRegions();
    }

    [Delphi(PortStatus.Ported, Note = "Empty in Delphi too (marked FIXME there).")]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void GetAsList(TBoldIndirectElement ResultList) => ResultList.SetReferenceValue(fClasses);

    [Delphi(PortStatus.Ported)]
    public bool EnsureEnclosure(TBoldObjectList ObjectList, bool ValidateOnly) => SystemPersistenceHandler.EnsureEnclosure(ObjectList, ValidateOnly);

    /// <summary>The dirty objects, first dropping the ones that became clean.</summary>
    [Delphi(PortStatus.Ported)]
    private TList GetDirtyObjects()
    {
        if (fDirtyObjectsInvalid)
        {
            int i = 0;
            while (i < fDirtyObjects!.Count)
            {
                if (fDirtyObjects[i] is TBoldObject o && !o.BoldDirty)
                {
                    MarkObjectClean(o);
                }
                else
                {
                    i++;
                }
            }

            fDirtyObjects.Pack();
            fDirtyObjectsInvalid = false;
        }

        return fDirtyObjects!;
    }

    [Delphi(PortStatus.Ported)]
    internal void MarkObjectPossiblyCleaner(TBoldObject BoldObject)
    {
        if (!fDirtyObjectsInvalid)
        {
            SendEvent(beDirtyListInvalidOrItemDeleted);
        }

        fDirtyObjectsInvalid = true;
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetBoldDirty() => DirtyObjects.Count != 0;

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => BoldSystemTypeInfo.ExpressionName;

    /// <summary>Ends one DelayObjectDestruction; the outermost destroys the objects that were waiting.</summary>
    [Delphi(PortStatus.Ported)]
    public void AllowObjectDestruction()
    {
        if (fDelayedDestructionCount <= 0)
        {
            throw new EBold(SysUtils.Format(sDestructionNestingMismatch, GetType().Name));
        }

        fDelayedDestructionCount--;
        if (fDelayedDestructionCount == 0)
        {
            for (int i = 0; i < fDelayedDestructionList!.Count; i++)
            {
                var anObject = (TBoldObject)fDelayedDestructionList[i]!;
                if (anObject.BoldPersistenceState != TBoldValuePersistenceState.bvpsModified &&
                    anObject.BoldExistenceState != TBoldExistenceState.besExisting)
                {
                    DestroyObject(anObject);
                }
            }

            fDelayedDestructionList.Clear();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void DelayObjectDestruction() => fDelayedDestructionCount++;

    /// <summary>Unloads the object and removes its locator, or queues it while destruction is delayed.</summary>
    [Delphi(PortStatus.Ported)]
    internal void DestroyObject(TBoldObject BoldObject)
    {
        if (BoldObject.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified ||
            BoldObject.BoldExistenceState == TBoldExistenceState.besExisting)
        {
            throw new EBold(SysUtils.Format(sObjectNotDestroyable, GetType().Name));
        }

        if (fDelayedDestructionCount > 0)
        {
            fDelayedDestructionList!.Add(BoldObject);
        }
        else
        {
            TBoldObjectLocator aLocator = BoldObject.BoldObjectLocator!;
            SendExtendedEvent(beLocatorDestroying, [BoldObject]);
            aLocator.UnloadBoldObject();
            Locators.Remove(aLocator);
        }
    }

    /// <summary>Passes object creation, deletion and fetching to the class list and to the system's subscribers.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void ReceiveEventFromOwned(object? Originator, TBoldEvent OriginalEvent)
    {
        if ((OriginalEvent == beObjectCreated || OriginalEvent == beObjectDeleted || OriginalEvent == beObjectFetched) && Originator is TBoldObject obj)
        {
            TBoldObjectList ClassList = Classes(obj.BoldClassTypeInfo.TopSortedIndex)!;
            ((TBoldClassListController)ClassList.ObjectListController).ReceiveClassEvent(obj, OriginalEvent);
            SendExtendedEvent(OriginalEvent, [Originator]);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal bool CanCreateObject(TBoldClassTypeInfo ClassTypeInfo) => SendQuery(bqMayCreateObject, [ClassTypeInfo], null);

    [Delphi(PortStatus.Ported)]
    internal bool CanDeleteObject(TBoldObject anObject) => SendQuery(bqMayDeleteObject, [anObject], null);

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => BoldSystemTypeInfo;

    [Delphi(PortStatus.Ported)]
    protected override string GetDisplayName() => BoldType!.ExpressionName;

    /// <summary>The OCL evaluator for this system, created on first use.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldEvaluator? GetEvaluator() => fEvaluator ??= new TBoldOcl(BoldSystemTypeInfo, this);

    [Delphi(PortStatus.Ported)]
    public void MakeDefault() => G_DefaultBoldSystem = this;

    [Delphi(PortStatus.Ported)]
    private bool GetIsDefault() => this == G_DefaultBoldSystem;

    [Delphi(PortStatus.Ported)]
    private void SetIsdefault(bool Value)
    {
        if (Value)
        {
            G_DefaultBoldSystem = this;
        }
        else if (IsDefault)
        {
            G_DefaultBoldSystem = null;
        }
    }

    /// <summary>Starts a (nested) transaction when the mode is at least the transaction mode.</summary>
    [Delphi(PortStatus.Ported)]
    public void StartTransaction(TBoldSystemTransactionMode MinimalMode = TBoldSystemTransactionMode.stmNormal)
    {
        if (MinimalMode <= TransactionMode)
        {
            if (!InTransaction())
            {
                fRollbackArea = new TBoldFreeStandingValueSpace();
                fValidValueArea = new TBoldFreeStandingValueSpace();
                DelayObjectDestruction();
            }

            fTransactionNesting++;
        }
    }

    /// <summary>Commits the transaction; the outermost commit checks CanCommit and records the changes for undo.</summary>
    [Delphi(PortStatus.Ported)]
    public void CommitTransaction(TBoldSystemTransactionMode MinimalMode = TBoldSystemTransactionMode.stmNormal)
    {
        static bool OrdernoDiffers(IBoldValue Value, TBoldMember member) =>
            member is TBoldObjectReference r && r.GetController() is TBoldDirectSingleLinkController c &&
            ((IBoldObjectIdRef)Value).OrderNo != c.OrderNo;

        void HandleOldValues()
        {
            var ObjectIds = new TBoldObjectIdList();
            fRollbackArea!.AllObjectIds(ObjectIds, false);
            for (int O = 0; O < ObjectIds.Count; O++)
            {
                TBoldFreeStandingObjectContents ObjectContents = fRollbackArea.GetFSObjectContentsByObjectId(ObjectIds[O])!;
                TBoldObject BoldObject = Locators.ObjectByID(ObjectIds[O])!;
                bool RegardAsExisting =
                    ((BoldObject.BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent && BoldObject.BoldExistenceState == TBoldExistenceState.besExisting) ||
                     (BoldObject.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified && BoldObject.BoldExistenceState == TBoldExistenceState.besDeleted)) &&
                    ObjectContents.BoldExistenceState == TBoldExistenceState.besNotCreated;
                UndoHandler.HandleObject(ObjectContents, RegardAsExisting);
                for (int M = 0; M < ObjectContents.MemberCount; M++)
                {
                    IBoldValue? Value = ObjectContents.ValueByIndex(M);
                    if (Value is not null && BoldObject.BoldMemberAssigned(M) && BoldObject.BoldMembers(M).StoreInUndo())
                    {
                        TBoldMember BoldMember = BoldObject.BoldMembers(M);
                        if (Value.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid &&
                            !(Value is IBoldObjectIdRef ObjIdRef && ObjIdRef.Id is not null))
                        {
                            Value = GetValueFromValuespace(fValidValueArea!, ObjectIds[O], M);
                        }

                        if (Value is not null && (!BoldMember.IsEqualToValue(Value) || OrdernoDiffers(Value, BoldMember)))
                        {
                            UndoHandler.HandleMember(ObjectContents, M, Value);
                        }
                    }
                }

                if (BoldObject.BoldExistenceState == TBoldExistenceState.besDeleted)
                {
                    for (int M = 0; M < BoldObject.BoldMemberCount; M++)
                    {
                        if (BoldObject.BoldMemberAssigned(M) && BoldObject.BoldMembers(M).StoreInUndo())
                        {
                            IBoldValue Value = BoldObject.BoldMembers(M).AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)!;
                            if (BoldObject.BoldMembers(M).BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid)
                            {
                                UndoHandler.HandleMember(ObjectContents, M, Value);
                            }
                        }
                    }
                }
            }
        }

        if (MinimalMode <= TransactionMode)
        {
            if (!InTransaction())
            {
                throw new EBold(SysUtils.Format(sUnmatchedCommit, GetType().Name));
            }

            if (fTransactionNesting == 1)
            {
                fTransactionRollbackOnly = fTransactionRollbackOnly || !CanCommitTransaction();
                fTransactionList!.Clear();
                if (!fTransactionRollbackOnly)
                {
                    if (((TBoldUndoHandler)UndoHandler).UndoState == TBoldUndoState.busNormal)
                    {
                        HandleOldValues();
                    }

                    SysUtils.FreeAndNil(ref fRollbackArea);
                    SysUtils.FreeAndNil(ref fValidValueArea);
                    fTransactionNesting--;
                    AllowObjectDestruction();
                }
                else
                {
                    BoldRaiseLastFailure(this, "CommitTransaction", sCommitNotAllowed);
                }
            }
            else
            {
                fTransactionNesting--;
            }
        }
    }

    /// <summary>Rolls back the transaction; the outermost rollback restores the values saved at its start.</summary>
    [Delphi(PortStatus.Ported)]
    public void RollbackTransaction(TBoldSystemTransactionMode MinimalMode = TBoldSystemTransactionMode.stmNormal)
    {
        if (MinimalMode <= TransactionMode)
        {
            if (!InTransaction())
            {
                throw new EBold(SysUtils.Format(sUnmatchedRollback, GetType().Name));
            }

            if (fTransactionNesting == 1)
            {
                TBoldFreeStandingValueSpace aRollbackArea = fRollbackArea!;
                fRollbackArea = null;
                TBoldFreeStandingValueSpace? aValidValueArea = fValidValueArea;
                fValidValueArea = null;
                AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepContents).ApplyValueSpace(aRollbackArea, false);
                aRollbackArea.Dispose();
                aValidValueArea?.Dispose();
                AllowObjectDestruction();
                fTransactionList!.Clear();
                fTransactionRollbackOnly = false;
                SendEvent(beRolledBack);
            }
            else
            {
                fTransactionRollbackOnly = true;
            }

            fTransactionNesting--;
        }
    }

    /// <summary>True when the system and every element changed in the transaction may commit; false when one raises.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldSystem.CanCommit", Note = "Named CanCommitTransaction: TBoldDomainElement.CanCommit is not virtual and C# cannot hide it with a private method of the same name.")]
    private bool CanCommitTransaction()
    {
        try
        {
            bool result = SendQuery(bqMayCommit, [], null);
            using TBoldDomainElementCollectionTraverser Traverser = fTransactionList!.CreateTraverser();
            while (result && !Traverser.EndOfList)
            {
                result = Traverser.Item.CanCommit();
                Traverser.Next();
            }

            return result;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Saves the member's value in the rollback area the first time it changes in the transaction.</summary>
    [Delphi(PortStatus.Ported)]
    internal void CopyMemberToRollBackBuffer(TBoldMember BoldMember)
    {
        if (RollBackAreaAssigned())
        {
            int MemberIndex = BoldMember.BoldMemberRTInfo!.index;
            TBoldFreeStandingObjectContents FSObjectContents = EnsureObjectInFsValueSpace(BoldMember.OwningObject!, fRollbackArea!);
            IBoldValue? Value = FSObjectContents.ValueByIndex(MemberIndex);
            if (Value is not null)
            {
                if (Value.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid)
                {
                    FSObjectContents = EnsureObjectInFsValueSpace(BoldMember.OwningObject!, fValidValueArea!);
                    if (FSObjectContents.ValueByIndex(MemberIndex) is null)
                    {
                        FSObjectContents.EnsureMemberByIndex(MemberIndex, BoldMember.GetStreamName());
                        FSObjectContents.ValueByIndex(MemberIndex)!.AssignContent(BoldMember.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)!);
                    }
                }
            }
            else
            {
                FSObjectContents.EnsureMemberByIndex(MemberIndex, BoldMember.GetStreamName());
                FSObjectContents.ValueByIndex(MemberIndex)!.AssignContent(BoldMember.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents)!);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void CopyObjectToRollBackBuffer(TBoldObject BoldObject)
    {
        if (RollBackAreaAssigned())
        {
            EnsureObjectInFsValueSpace(BoldObject, fRollbackArea!);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void AddToTransaction(TBoldDomainElement DomainElement)
    {
        if (RollBackAreaAssigned() && !fTransactionList!.Includes(DomainElement))
        {
            fTransactionList.Add(DomainElement);
        }
    }

    /// <summary>Commits, or rolls back and returns false when the commit raises.</summary>
    [Delphi(PortStatus.Ported)]
    public bool TryCommitTransaction(TBoldSystemTransactionMode MinimalMode = TBoldSystemTransactionMode.stmNormal)
    {
        try
        {
            CommitTransaction(MinimalMode);
            return true;
        }
        catch (Exception)
        {
            RollbackTransaction(MinimalMode);
            return false;
        }
    }

    /// <summary>Gives the system as an IBoldValueSpace.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldValueSpace))
        {
            Obj = new TBoldSystem_Proxy(this, Mode);
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public bool InTransaction() => fTransactionNesting != 0;

    [Delphi(PortStatus.Ported)]
    private bool RollBackAreaAssigned() => fRollbackArea is not null;

    /// <summary>Raises when the system is in a transaction or has unsaved objects.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureCanDestroy()
    {
        if (InTransaction())
        {
            throw new EBold(SysUtils.Format(sDestroy_TransactionNesting, GetType().Name, fTransactionNesting, BoldSystemTypeInfo.ModelName));
        }

        if (RollBackAreaAssigned())
        {
            throw new EBold(SysUtils.Format(sDestroy_RollbackAreaAssigned, GetType().Name, BoldSystemTypeInfo.ModelName));
        }

        if (BoldDirty)
        {
            throw new EBold(SysUtils.Format(sDestroy_DirtyObjects, GetType().Name, BoldSystemTypeInfo.ModelName));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TDateTime GetTimeForTimestamp(TBoldTimeStampType Timestamp) => SystemPersistenceHandler.GetTimeForTimestamp(Timestamp);

    [Delphi(PortStatus.Ported)]
    private TBoldTimeStampType GetTimestampForTime(TDateTime ClockTime) => SystemPersistenceHandler.GetTimestampForTime(ClockTime);

    [Delphi(PortStatus.Ported)]
    private void SetTransactionMode(TBoldSystemTransactionMode Value)
    {
        if (Value != fTransactionMode)
        {
            if (InTransaction())
            {
                throw new EBold(SysUtils.Format(sNotAllowedInTransaction, GetType().Name));
            }

            fTransactionMode = Value;
        }
    }

    [Delphi(PortStatus.Ported, Note = "Can be cleared, by a lock handler that goes before the system; Delphi raised on any change.")]
    private void SetPessimisticLockHandler(TBoldAbstractPessimisticLockHandler? Value)
    {
        if (fPessimisticLockHandler is not null && Value is not null)
        {
            throw new EBold(SysUtils.Format(sCannotChangeLockHandler, GetType().Name));
        }

        fPessimisticLockHandler = Value;
    }

    [Delphi(PortStatus.Ported)]
    private TNotifyEvent? GetOnPreUpdate() => SystemPersistenceHandler.OnPreUpdate;

    [Delphi(PortStatus.Ported)]
    private void SetOnPreUpdate(TNotifyEvent? Value) => SystemPersistenceHandler.OnPreUpdate = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldTimeStampType GetTimeStampOfLatestUpdate() => SystemPersistenceHandler.TimeStampOfLatestUpdate;

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractUndoHandler GetUndoHandler() => fUndoHandler ??= new TBoldUndoHandler(this);

    [Delphi(PortStatus.Ported)]
    private IBoldUndoHandler GetUndoHandlerInterface() => (IBoldUndoHandler)UndoHandler;

    /// <summary>Asks the controller of every loaded, valid member to check its links; raises (assert) on a broken one.</summary>
    [Delphi(PortStatus.Ported)]
    public bool AssertLinkIntegrity()
    {
        using TBoldLocatorListTraverser Traverser = Locators.CreateTraverser();
        while (!Traverser.EndOfList)
        {
            TBoldObject? bo = Traverser.locator.BoldObject;
            if (bo is not null)
            {
                for (int i = 0; i < bo.BoldMemberCount; i++)
                {
                    if (bo.BoldMemberAssigned(i) && bo.BoldMembers(i).BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid)
                    {
                        bo.BoldMembers(i).GetController()?.AssertIntegrity();
                    }
                }
            }

            Traverser.Next();
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator GetEnsuredLocatorByID(TBoldObjectId ObjectID) =>
        Locators.LocatorByID(ObjectID) ?? new TBoldObjectLocator(this, ObjectID);

    [Delphi(PortStatus.Ported)] protected internal TBoldAbstractSystemPersistenceHandler SystemPersistenceHandler => fSystemPersistenceHandler;

    [Delphi(PortStatus.Ported)]
    public bool IsDefault
    {
        get => GetIsDefault();
        set => SetIsdefault(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldSystemTypeInfo BoldSystemTypeInfo => fBoldSystemTypeInfo;
    [Delphi(PortStatus.Ported)] public TBoldObjectList ClassByExpressionName(string ExpressionName) => GetClassByExpressionName(ExpressionName);
    [Delphi(PortStatus.Ported)] public TBoldObjectList? Classes(int index) => GetClassByIndex(index);
    [Delphi(PortStatus.Ported)] public TList DirtyObjects => GetDirtyObjects();
    [Delphi(PortStatus.Ported)] public TBoldObjectLocator EnsuredLocatorByID(TBoldObjectId ObjectID) => GetEnsuredLocatorByID(ObjectID);
    [Delphi(PortStatus.Ported)] public TBoldSystemLocatorList Locators => fLocators!;

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractPessimisticLockHandler? PessimisticLockHandler
    {
        get => fPessimisticLockHandler;
        set => SetPessimisticLockHandler(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldAbstractOptimisticLockHandler? OptimisticLockHandler => fOptimisticLockHandler;
    [Delphi(PortStatus.Ported)] public TBoldAbstractRegionFactory? RegionFactory => fRegionFactory;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectList? NewDirtyList
    {
        get => FNewDirtyList;
        set => FNewDirtyList = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectList? NewModifiedList
    {
        get => fNewModifiedList;
        set => fNewModifiedList = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldPersistenceController? PersistenceController => fPersistenceController;
    [Delphi(PortStatus.Ported)] public IBoldValueSpace AsIBoldvalueSpace(TBoldDomainElementProxyMode Mode) => GetAsIBoldvalueSpace(Mode);
    [Delphi(PortStatus.Ported)] public TDateTime TimeForTimestamp(TBoldTimeStampType Timestamp) => GetTimeForTimestamp(Timestamp);
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType TimestampForTime(TDateTime ClockTime) => GetTimestampForTime(ClockTime);
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType TimeStampOfLatestUpdate => GetTimeStampOfLatestUpdate();

    [Delphi(PortStatus.Ported)]
    public TBoldSystemTransactionMode TransactionMode
    {
        get => fTransactionMode;
        set => SetTransactionMode(value);
    }

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? OnPreUpdate
    {
        get => GetOnPreUpdate();
        set => SetOnPreUpdate(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldOptimisticLockingFailedEvent? OnOptimisticLockingFailed
    {
        get => fOptimisticLockingFailed;
        set => fOptimisticLockingFailed = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldAbstractUndoHandler UndoHandler => GetUndoHandler();
    [Delphi(PortStatus.Ported)] public IBoldUndoHandler UndoHandlerInterface => GetUndoHandlerInterface();

    [Delphi(PortStatus.Ported)]
    public TBoldCreateApproximateObjectError? OnCreateApproximateObjectError
    {
        get => fOnCreateApproximateObjectError;
        set => fOnCreateApproximateObjectError = value;
    }
}

/// <summary>Unit-level constants, globals and routines of BoldSystem.</summary>
public static partial class BoldSystem
{
    [Delphi(PortStatus.Ported)] public const int bqBaseSystem = BoldSubscription.bqMaxSubscription + 1;
    [Delphi(PortStatus.Ported)] public const int bqMayRead = bqBaseSystem + 0;
    [Delphi(PortStatus.Ported)] public const int bqMaySetValue = bqBaseSystem + 1;
    [Delphi(PortStatus.Ported)] public const int bqMaySetToNull = bqBaseSystem + 2;
    [Delphi(PortStatus.Ported)] public const int bqMayClear = bqBaseSystem + 3;
    [Delphi(PortStatus.Ported)] public const int bqMayInsert = bqBaseSystem + 4;
    [Delphi(PortStatus.Ported)] public const int bqMayRemove = bqBaseSystem + 5;
    [Delphi(PortStatus.Ported)] public const int bqMayMove = bqBaseSystem + 6;
    [Delphi(PortStatus.Ported)] public const int bqMayReplace = bqBaseSystem + 7;
    [Delphi(PortStatus.Ported)] public const int bqMayCreateObject = bqBaseSystem + 8;
    [Delphi(PortStatus.Ported)] public const int bqMayDeleteObject = bqBaseSystem + 9;
    [Delphi(PortStatus.Ported)] public const int bqMaxSystem = bqBaseSystem + 9;

    /// <summary>The derivers of derived members, kept outside the members to save space.</summary>
    internal static readonly TBoldExternalizedReferenceList G_ExternalDerivers = new() { ManageReferencedObject = true };

    /// <summary>Why the last May... query failed; per thread, as each thread runs its own operations.</summary>
    [ThreadStatic] private static TBoldFailureReason? G_LastFailureReason;

    internal static TBoldSystem? G_DefaultBoldSystem;

    internal static int IX_BoldObjectId = -1;

    /// <summary>The object's contents in the value space, added with the object's existence and persistence state when missing.</summary>
    [Delphi(PortStatus.Ported)]
    internal static TBoldFreeStandingObjectContents EnsureObjectInFsValueSpace(TBoldObject BoldObject, TBoldFreeStandingValueSpace ValueSpace)
    {
        TBoldObjectId ObjectId = BoldObject.BoldObjectLocator!.BoldObjectID;
        TBoldFreeStandingObjectContents? result = ValueSpace.GetFSObjectContentsByObjectId(ObjectId);
        if (result is null)
        {
            result = ValueSpace.GetEnsuredFSObjectContentsByObjectId(ObjectId);
            result.BoldExistenceState = BoldObject.BoldExistenceState;
            result.BoldPersistenceState = BoldObject.BoldPersistenceState;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static IBoldValue? GetValueFromValuespace(IBoldValueSpace ValueSpace, TBoldObjectId Id, int MemberIndex) =>
        ValueSpace.ObjectContentsByObjectId(Id)?.ValueByMemberId(new TBoldMemberID(MemberIndex));

    [Delphi(PortStatus.Ported)]
    public static TBoldFailureReason? GetBoldLastFailureReason() => G_LastFailureReason;

    [Delphi(PortStatus.Ported)]
    public static void SetBoldLastFailureReason(TBoldFailureReason? Value)
    {
        G_LastFailureReason?.Dispose();
        G_LastFailureReason = Value;
    }

    /// <summary>Raises EBoldFailure with the last failure reason (which it takes over), or the default message.</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldRaiseLastFailure(TBoldDomainElement? originator, string MethodName, string DefaultMessage)
    {
        string MessageStr = "";
        string MessageFormatStr = "";
        if (G_LastFailureReason is not null)
        {
            if (G_LastFailureReason.Originator is not null)
            {
                originator = G_LastFailureReason.Originator;
            }

            MessageStr = G_LastFailureReason.Reason;
            MessageFormatStr = G_LastFailureReason.MessageFormatStr;
        }

        string OriginatorStr = originator is not null ? originator.DisplayName : "";

        if (MessageStr == "")
        {
            MessageStr = DefaultMessage;
        }

        if (MessageStr == "")
        {
            MessageStr = sReasonUnknown;
        }

        if (MessageFormatStr == "")
        {
            MessageFormatStr = sFailureMessage;
        }

        EBoldFailure failure = G_LastFailureReason is not null
            ? G_LastFailureReason.GetException(SysUtils.Format(MessageFormatStr, OriginatorStr, MethodName, MessageStr))
            : new EBoldFailure(SysUtils.Format(MessageFormatStr, OriginatorStr, MethodName, MessageStr));

        if (G_LastFailureReason is not null)
        {
            failure.ReasonObject = G_LastFailureReason;
            G_LastFailureReason = null;
        }

        throw failure;
    }

    [Delphi(PortStatus.Ported)]
    public static void BoldClearLastFailure()
    {
        if (G_LastFailureReason is not null)
        {
            SetBoldLastFailureReason(null);
        }
    }
}
