// Delphi: Source/ObjectSpace/BORepresentation/BoldObjectListControllers.pas
// Port map: CSharp/port/units/BoldObjectListControllers.md

using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>The controller of a free-standing or derived object list: a locator list, optionally following object deletion.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectListController : TBoldAbstractObjectListController
{
    private TBoldObjectAttributeIndexList? FList;
    private TBoldPassthroughSubscriber? FSubscriber;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectListController(TBoldObjectList OwningList)
        : base(OwningList)
    {
        FList = new TBoldObjectLocatorList();
        FSubscriber = new TBoldPassthroughSubscriber(_ReceiveObjectDeleted);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectListController.Destroy")]
    public override void Dispose()
    {
        FList?.Dispose();
        FList = null;
        FSubscriber?.Dispose();
        FSubscriber = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public override void AddLocator(TBoldObjectLocator Locator)
    {
        if (!StartModify())
        {
            BoldRaiseLastFailure(OwningList, "AddLocator", "");
        }

        LocatorList.Add(Locator);
        SubscribeToObjectDeleted(Locator);
        Changed(beItemAdded, [Locator]);

        EndModify();
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetCount() => LocatorList.Count;

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocator(int index) => LocatorList[index];

    /// <summary>The object with the qualifier values; a qualified derived role builds the index on first use.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocatorByQualifiersAndSubscribe(TBoldMemberList MemberList, TBoldSubscriber? Subscriber)
    {
        if (!LocatorList.HasMembersIndex)
        {
            if (OwningObjectList.BoldRoleRTInfo is { IsQualified: true } role)
            {
                OwningObjectList.EnsureContentsCurrent();
                LocatorList.InitMembersIndex(OwningObjectList, role.Qualifiers!);
            }
            else
            {
                throw new EBold(SysUtils.Format(sRolenotQualified, GetType().Name));
            }
        }

        return FList!.GetLocatorByAttributesAndSubscribe(MemberList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocatorList GetLocatorList() => (TBoldObjectLocatorList)FList!;

    [Delphi(PortStatus.Ported)]
    public override bool IncludesLocator(TBoldObjectLocator? Locator) => LocatorList.LocatorInList(Locator);

    [Delphi(PortStatus.Ported)]
    public override int IndexOfLocator(TBoldObjectLocator? Locator) => LocatorList.IndexOf(Locator);

    [Delphi(PortStatus.Ported)]
    public override void InsertLocator(int index, TBoldObjectLocator Locator)
    {
        if (!StartModify())
        {
            BoldRaiseLastFailure(OwningList, "InsertLocator", "");
        }

        LocatorList.Insert(index, Locator);
        SubscribeToObjectDeleted(Locator);
        Changed(beItemAdded, [Locator]);

        EndModify();
    }

    [Delphi(PortStatus.Ported)]
    public override void Move(int CurrentIndex, int NewIndex)
    {
        if (!StartModify())
        {
            BoldRaiseLastFailure(OwningList, "Move", "");
        }

        LocatorList.Move(CurrentIndex, NewIndex);
        Changed(beOrderChanged, []);

        EndModify();
    }

    [Delphi(PortStatus.Ported)]
    private void InternalRemoveByIndex(int index)
    {
        TBoldObjectLocator? Locator = LocatorList[index];
        LocatorList.RemoveByIndex(index);
        Changed(beItemDeleted, [Locator]);
    }

    [Delphi(PortStatus.Ported)]
    public override void RemoveByIndex(int index)
    {
        if (!StartModify())
        {
            BoldRaiseLastFailure(OwningList, "RemoveByIndex", "");
        }

        InternalRemoveByIndex(index);
        EndModify();
    }

    [Delphi(PortStatus.Ported)]
    public override void SetLocator(int index, TBoldObjectLocator? Locator)
    {
        if (!StartModify())
        {
            BoldRaiseLastFailure(OwningList, "SetLocator", "");
        }

        if (Locator is null)
        {
            RemoveByIndex(index);
        }
        else
        {
            LocatorList[index] = Locator;
            SubscribeToObjectDeleted(Locator);
            Changed(beItemReplaced, [Locator, index]);
        }

        EndModify();
    }

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdListRef;

    /// <summary>Nothing: object lists are not persistent and so always current.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent()
    {
    }

    /// <summary>Removes deleted (or destroyed) objects, and the objects of a destroyed system.</summary>
    [Delphi(PortStatus.Ported)]
    private void _ReceiveObjectDeleted(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        switch (RequestedEvent)
        {
            case beLocatorDestroying:
            case beObjectDeleted:
                TBoldObjectLocator? DeletedLocator = ((TBoldObject)Args[0]!).BoldObjectLocator;
                if (LocatorList.LocatorInList(DeletedLocator))
                {
                    LocatorList.Remove(DeletedLocator);
                    Changed(beItemDeleted, [DeletedLocator]);
                }

                break;
            case beDestroying:
                var System = (TBoldSystem)Originator!;
                for (int i = Count - 1; i >= 0; i--)
                {
                    if (LocatorList[i]!.BoldSystem == System)
                    {
                        InternalRemoveByIndex(i);
                    }
                }

                break;
            default:
                throw new EBoldInternal(SysUtils.Format(sUnknownEvent, GetType().Name));
        }
    }

    /// <summary>Subscribes to the locator's system as the list's subscription flags say.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void SubscribeToObjectDeleted(TBoldObjectLocator Locator)
    {
        if (OwningObjectList.SubscribeToObjectsInList)
        {
            Locator.BoldSystem.AddSmallSubscription(FSubscriber, TBoldSmallEventSet.Of(beObjectDeleted), beObjectDeleted);
            Locator.BoldSystem.AddSmallSubscription(FSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
        }

        if (OwningObjectList.SubscribeToLocatorsInList)
        {
            Locator.BoldSystem.AddSmallSubscription(FSubscriber, TBoldSmallEventSet.Of(beLocatorDestroying), beLocatorDestroying);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void DropSubscriptions() => FSubscriber!.CancelAllSubscriptions();

    /// <summary>Subscribes again (once per run of locators of the same system).</summary>
    [Delphi(PortStatus.Ported)]
    public void Resubscribe()
    {
        TBoldSystem? LastSystem = null;
        for (int i = 0; i < Count; i++)
        {
            TBoldObjectLocator Locator = LocatorList[i]!;
            if (Locator.BoldSystem != LastSystem)
            {
                SubscribeToObjectDeleted(Locator);
                LastSystem = Locator.BoldSystem;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AssignContentValue(IBoldValue? Source)
    {
        if (Source is not null && Source is not IBoldObjectIdListRef)
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, "AssignContentValue"));
        }

        PreChange();
        LocatorList.Clear();
        if (Source is IBoldObjectIdListRef s)
        {
            for (int i = 0; i < s.Count; i++)
            {
                LocatorList.Add(BoldSystem!.EnsuredLocatorByID(s.IdList(i)));
            }
        }

        Changed(beValueChanged, []);
    }

    [Delphi(PortStatus.Ported)]
    public override void FreeContent() => LocatorList.Clear();

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBoldObjectList_Proxy);

    /// <summary>A new object of the list's element class.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override TBoldElement CreateNew()
    {
        var ListTypeInfo = (TBoldListTypeInfo)OwningList.BoldType!;
        var ClassTypeInfo = (TBoldClassTypeInfo)ListTypeInfo.ListElementTypeInfo!;
        return TBoldObject.InternalCreateNewWithClassAndSystem(ClassTypeInfo.ObjectClass!, ClassTypeInfo, OwningList.BoldSystem!, true);
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectLocatorList LocatorList => GetLocatorList();
}

/// <summary>
/// The controller of a class list (all objects of a class and its subclasses): filled from the database, from a loaded
/// superclass list or from the transient objects, and kept up to date by object creation and deletion.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldClassListController : TBoldObjectListController
{
    private TBoldTimeStampType fTimestamp;
    private TList? fAtTimeList;

    [Delphi(PortStatus.Ported)]
    public TBoldClassListController(TBoldObjectList OwningList)
        : base(OwningList)
    {
        fTimestamp = BoldDefs.BOLDMAXTIMESTAMP;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClassListController.Destroy")]
    public override void Dispose()
    {
        base.Dispose();
        if (fAtTimeList is not null)
        {
            for (int i = 0; i < fAtTimeList.Count; i++)
            {
                ((TBoldObjectList?)fAtTimeList[i])?.Dispose();
            }
        }

        fAtTimeList = null;
    }

    [Delphi(PortStatus.Ported)]
    private TList GetAtTimeList() => fAtTimeList ??= new TList();

    [Delphi(PortStatus.Ported)]
    private void FillFromClassList(TBoldObjectList ObjectList)
    {
        for (int i = 0; i < ObjectList.Count; i++)
        {
            if (ObjectList[i]!.BoldClassTypeInfo.BoldIsA(ClassTypeInfo))
            {
                OwningList.Add(ObjectList[i]);
            }
        }
    }

    /// <summary>Adds the transient, existing objects of the class from the system.</summary>
    [Delphi(PortStatus.Ported)]
    private void FillFromSystem()
    {
        using TBoldLocatorListTraverser Traverser = OwningList.BoldSystem!.Locators.CreateTraverser();
        while (!Traverser.EndOfList)
        {
            TBoldObjectLocator Locator = Traverser.locator;
            if (Locator.BoldObject is { BoldPersistent: false } obj && obj.BoldExistenceState == TBoldExistenceState.besExisting &&
                obj.BoldClassTypeInfo.BoldIsA(ClassTypeInfo))
            {
                LocatorList.Add(Locator);
            }

            Traverser.Next();
        }
    }

    /// <summary>The nearest superclass list that is loaded (current), or nil (always nil for an old version).</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldObjectList? ClosestLoadedClassList()
    {
        if (fTimestamp != BoldDefs.BOLDMAXTIMESTAMP)
        {
            return null;
        }

        TBoldClassTypeInfo? SuperClass = ClassTypeInfo.SuperClassTypeInfo;
        while (SuperClass is not null && OwningList.BoldSystem!.Classes(SuperClass.TopSortedIndex)!.BoldPersistenceState != TBoldValuePersistenceState.bvpsCurrent)
        {
            SuperClass = SuperClass.SuperClassTypeInfo;
        }

        return SuperClass is not null ? OwningList.BoldSystem!.Classes(SuperClass.TopSortedIndex) : null;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectId GetIdList(int Index) => LocatorList[Index]!.BoldObjectID;

    /// <summary>
    /// Sets the list from the fetched ids: for PMIn it drops objects deleted in memory and adds new and transient ones,
    /// then changes the locator list as little as it can.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    internal void SetFromIdList(TBoldObjectIdList? List, TBoldDomainElementProxyMode Mode)
    {
        void InternalAddLocator(TBoldObjectLocator NewLocator)
        {
            if (!IncludesLocator(NewLocator))
            {
                PreChange();
                LocatorList.Add(NewLocator);
                Changed(beItemAdded, [NewLocator]);
            }
        }

        void AddTransientFromSystem(TBoldObjectIdList list)
        {
            using TBoldLocatorListTraverser Traverser = OwningList.BoldSystem!.Locators.CreateTraverser();
            while (!Traverser.EndOfList)
            {
                TBoldObjectLocator Locator = Traverser.locator;
                if (Locator.BoldObject is { BoldPersistent: false } obj && obj.BoldClassTypeInfo.BoldIsA(ClassTypeInfo))
                {
                    list.Add(Locator.BoldObjectID);
                }

                Traverser.Next();
            }
        }

        TBoldObjectIdList NewList = List is not null ? List.Clone() : new TBoldObjectIdList();

        if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
        {
            for (int I = NewList.Count - 1; I >= 0; I--)
            {
                TBoldObjectLocator? Locator = BoldSystem!.Locators.LocatorByID(NewList[I]);
                if (Locator?.BoldObject is { BoldExistenceState: TBoldExistenceState.besDeleted })
                {
                    NewList.RemoveByIndex(I);
                }
            }

            for (int I = 0; I < BoldSystem!.DirtyObjects.Count; I++)
            {
                var TheObject = (TBoldObject)BoldSystem.DirtyObjects[I]!;
                if (TheObject.BoldClassTypeInfo.BoldIsA(ClassTypeInfo) && TheObject.BoldExistenceState == TBoldExistenceState.besExisting &&
                    TheObject.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
                {
                    NewList.Add(TheObject.BoldObjectLocator!.BoldObjectID);
                }
            }

            AddTransientFromSystem(NewList);
        }

        for (int I = GetCount() - 1; I >= 0; I--)
        {
            if (!NewList.IdInList(GetLocator(I)!.BoldObjectID))
            {
                LocatorList.RemoveByIndex(I);
            }
        }

        for (int I = 0; I < NewList.Count; I++)
        {
            InternalAddLocator(BoldSystem!.EnsuredLocatorByID(NewList[I]));
        }
    }

    /// <summary>True unless the class is abstract or a link class.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool GetCanCreateNew()
    {
        if (ClassTypeInfo.IsAbstract)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sClassIsAbstract, [ClassTypeInfo.ExpressionName], OwningList));
            return false;
        }

        if (ClassTypeInfo.IsLinkClass)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sClassIsLinkClass, [ClassTypeInfo.ExpressionName], OwningList));
            return false;
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldClassTypeInfo GetClassTypeInfo() => (TBoldClassTypeInfo)((TBoldListTypeInfo)OwningList.BoldType!).ListElementTypeInfo!;

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStringrepresentation() => ClassTypeInfo.ModelName;

    /// <summary>Nothing: class lists follow objects through ReceiveClassEvent.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void SubscribeToObjectDeleted(TBoldObjectLocator Locator)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBoldClassList_Proxy);

    /// <summary>Follows object creation, deletion and fetching (passing it to the superclass list first).</summary>
    [Delphi(PortStatus.Ported)]
    public void ReceiveClassEvent(TBoldObject BoldObject, TBoldEvent EVENT)
    {
        TBoldClassTypeInfo? SuperClassTypeInfo = ClassTypeInfo.SuperClassTypeInfo;
        if (SuperClassTypeInfo is not null)
        {
            TBoldObjectList SuperClassList = BoldSystem!.Classes(SuperClassTypeInfo.TopSortedIndex)!;
            ((TBoldClassListController)GetControllerForMember(SuperClassList)!).ReceiveClassEvent(BoldObject, EVENT);
        }

        if (OwningList.BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid)
        {
            switch (EVENT)
            {
                case beObjectCreated:
                    AddLocator(BoldObject.BoldObjectLocator!);
                    break;
                case beObjectDeleted:
                    if (ClassTypeInfo.Persistent && OwningList.BoldSystem!.BoldPersistent)
                    {
                        LocatorList.Remove(BoldObject.BoldObjectLocator);
                    }
                    else
                    {
                        OwningList.Invalidate();
                    }

                    break;
                case beObjectFetched:
                    OwningList.SendEvent(beObjectFetched);
                    break;
                default:
                    throw new EBoldInternal(SysUtils.Format("%s.ReceiveClassEvent: Unknown event", GetType().Name));
            }
        }
        else if (EVENT == beObjectFetched)
        {
            OwningList.SendEvent(beObjectFetched);
        }

        switch (EVENT)
        {
            case beObjectCreated:
                OwningList.SendExtendedEvent(beItemAdded, [BoldObject.BoldObjectLocator]);
                break;
            case beObjectDeleted:
                OwningList.SendExtendedEvent(beItemDeleted, [BoldObject.BoldObjectLocator]);
                break;
        }
    }

    /// <summary>Gives the class list as IBoldObjectIdListRef.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldObjectIdListRef))
        {
            Obj = Activator.CreateInstance(ProxyClass(), OwningList, Mode);
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    /// <summary>The class list at the time: this system's for now, else one kept per time.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldMember AtTime(TBoldTimeStampType Time)
    {
        var listTypeInfo = (TBoldListTypeInfo)OwningList.BoldType!;
        if (Time == BoldDefs.BOLDMAXTIMESTAMP)
        {
            return BoldSystem!.Classes(((TBoldClassTypeInfo)listTypeInfo.ListElementTypeInfo!).TopSortedIndex)!;
        }

        for (int i = 0; i < AtTimeList.Count; i++)
        {
            var list = (TBoldObjectList)AtTimeList[i]!;
            if (((TBoldClassListController)GetControllerForMember(list)!).fTimestamp == Time)
            {
                return list;
            }
        }

        TBoldObjectList result = TBoldObjectList.InternalCreateClassList(typeof(TBoldObjectList), BoldSystem!, listTypeInfo);
        ((TBoldClassListController)GetControllerForMember(result)!).fTimestamp = Time;
        AtTimeList.Add(result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override bool HandlesAtTime() => true;

    /// <summary>Fills a persistent class list from a loaded superclass list or the database; a transient one from the system.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent()
    {
        if (ClassTypeInfo.Persistent && OwningList.BoldSystem!.BoldPersistent)
        {
            TBoldObjectList? SourceList = ClosestLoadedClassList();
            if (SourceList is not null)
            {
                FillFromClassList(SourceList);
            }
            else
            {
                DbFetchClassForMember(fTimestamp);
            }
        }
        else
        {
            FillFromSystem();
        }

        OwningList.BoldPersistenceState = TBoldValuePersistenceState.bvpsTransient;
    }

    /// <summary>Deletes the object (removing from a class list means deleting).</summary>
    [Delphi(PortStatus.Ported)]
    public override void RemoveByIndex(int index) => LocatorList[index]!.EnsuredBoldObject!.Delete();

    [Delphi(PortStatus.Ported)] private TList AtTimeList => GetAtTimeList();
    [Delphi(PortStatus.Ported)] protected TBoldClassTypeInfo ClassTypeInfo => GetClassTypeInfo();

    internal TBoldObjectId IdList(int Index) => GetIdList(Index);
}

/// <summary>Gives a free-standing object list as IBoldValue (contents only).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectList_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode) : TBoldMember_Proxy(aProxedElement, aMode)
{
    [Delphi(PortStatus.Ported)]
    private TBoldObjectListController GetObjectListController() => (TBoldObjectListController)ProxedController!;

    [Delphi(PortStatus.Ported)]
    protected override void AssignContentValue(IBoldValue Source)
    {
        if (Mode == TBoldDomainElementProxyMode.bdepContents)
        {
            ObjectListController.AssignContentValue(Source);
        }
        else
        {
            UnsupportedMode(Mode, "AssignContentValue");
        }
    }

    [Delphi(PortStatus.Ported)] protected TBoldObjectListController ObjectListController => GetObjectListController();
}

/// <summary>Gives a class list as IBoldObjectIdListRef: the persistence layer sets it from fetched ids.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClassList_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldMember_Proxy(aProxedElement, aMode), IBoldObjectIdListRef
{
    [Delphi(PortStatus.Ported)]
    private TBoldClassListController GetClassListController() => (TBoldClassListController)ProxedController!;

    [Delphi(PortStatus.Ported)]
    public void SetFromIdList(TBoldObjectIdList? IdLIst)
    {
        if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
        {
            ClassListController.SetFromIdList(IdLIst, Mode);
        }
        else
        {
            UnsupportedMode(Mode, "SetFromIdList");
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId GetIdList(int Index) => ClassListController.IdList(Index);

    [Delphi(PortStatus.Ported)]
    public int GetCount() => ClassListController.Count;

    [Delphi(PortStatus.Ported)]
    protected override void AssignContentValue(IBoldValue Source)
    {
        if (Mode == TBoldDomainElementProxyMode.bdepContents)
        {
            ClassListController.AssignContentValue(Source);
        }
        else
        {
            UnsupportedMode(Mode, "AssignContentValue");
        }
    }

    [Delphi(PortStatus.Ported)] protected TBoldClassListController ClassListController => GetClassListController();
}
