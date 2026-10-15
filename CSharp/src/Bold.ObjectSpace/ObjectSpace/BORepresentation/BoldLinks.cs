// Delphi: Source/ObjectSpace/BORepresentation/BoldLinks.pas
// Port map: CSharp/port/units/BoldLinks.md
//
// The link controllers: they keep both ends of an association consistent. Split over files by class: this one has the
// proxies and the direct multi link; BoldLinks.Single.cs and BoldLinks.Multi.cs the rest.

using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>The controller of a multi link stored in the objects at the other end (each holds a direct single link back).</summary>
[Delphi(PortStatus.Ported)]
public partial class TBoldDirectMultiLinkController : TBoldMultiLinkController
{
    private TBoldObjectLocatorList? fLocatorList;

    [Delphi(PortStatus.Ported)]
    public TBoldDirectMultiLinkController(TBoldObjectList OwningList)
        : base(OwningList)
    {
        fLocatorList = new TBoldObjectLocatorList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldDirectMultiLinkController.Destroy")]
    public override void Dispose()
    {
        fLocatorList?.Dispose();
        fLocatorList = null;
        base.Dispose();
    }

    /// <summary>Adds the object and links it back to the owner, numbering it when the role is ordered.</summary>
    [Delphi(PortStatus.Ported)]
    public override void AddLocator(TBoldObjectLocator Locator)
    {
        EnsureOrder();
        BoldSystem!.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningList, "AddLocator", "");
            }

            PreChange();
            LocatorList.Add(Locator);
            TBoldDirectSingleLinkController OtherEndController = GetOtherEndController(Locator)!;
            OtherEndController.LinkTo(OwningList.OwningObject!.BoldObjectLocator, true, TBoldLinkUnlinkMode.blulMarkModified);
            if (OwningObjectList.BoldRoleRTInfo!.IsOrdered)
            {
                OtherEndController.SetAndModifyOrderNo(LocatorList.IndexOf(Locator));
            }

            Changed(beItemAdded, [Locator]);

            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent()
    {
        EnsureOrder();
        DbFetchOwningMember();
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetCount() => LocatorList.Count;

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocator(int index)
    {
        EnsureOrder();
        return LocatorList[index];
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocatorByQualifiersAndSubscribe(TBoldMemberList MemberList, TBoldSubscriber? Subscriber)
    {
        EnsureOrder();
        if (!LocatorList.HasMembersIndex)
        {
            if (OwningObjectList.BoldRoleRTInfo is { IsQualified: true } role)
            {
                OwningObjectList.EnsureObjects();
                LocatorList.InitMembersIndex(OwningObjectList, role.Qualifiers!);
            }
            else
            {
                throw new EBold(SysUtils.Format(sRolenotQualified, GetType().Name));
            }
        }

        return LocatorList.GetLocatorByAttributesAndSubscribe(MemberList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    public override bool IncludesLocator(TBoldObjectLocator? Locator) => LocatorList.LocatorInList(Locator);

    [Delphi(PortStatus.Ported)]
    public override int IndexOfLocator(TBoldObjectLocator? Locator)
    {
        EnsureOrder();
        return LocatorList.IndexOf(Locator);
    }

    [Delphi(PortStatus.Ported)]
    public override void InsertLocator(int index, TBoldObjectLocator Locator)
    {
        EnsureOrder();
        BoldSystem!.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningList, "InsertLocator", "");
            }

            PreChange();
            LocatorList.Insert(index, Locator);
            GetOtherEndController(Locator)!.LinkTo(OwningList.OwningObject!.BoldObjectLocator, true, TBoldLinkUnlinkMode.blulMarkModified);
            ReOrder();
            Changed(beItemAdded, [Locator]);
            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    /// <summary>The other end linked to the owner: adds the object (numbering it when asked and the role is ordered).</summary>
    [Delphi(PortStatus.Ported)]
    public override void LinkTo(TBoldObjectLocator? NewLocator, bool updateOrderNo, TBoldLinkUnlinkMode Mode)
    {
        BoldClearLastFailure();
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified && !StartModify())
        {
            BoldRaiseLastFailure(OwningList, "Linkto", "");
        }

        PreChange();
        LocatorList.Add(NewLocator);
        if (Mode == TBoldLinkUnlinkMode.blulMarkAdjusted)
        {
            OwningObjectList.Adjusted = true;
        }

        if (updateOrderNo && OwningObjectList.BoldRoleRTInfo!.IsOrdered)
        {
            GetOtherEndController(NewLocator!)!.SetAndModifyOrderNo(LocatorList.IndexOf(NewLocator));
        }

        Changed(beItemAdded, [NewLocator]);
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified)
        {
            EndModify();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Move(int CurrentIndex, int NewIndex)
    {
        EnsureOrder();
        BoldSystem!.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningList, "Move", "");
            }

            PreChange();
            LocatorList.Move(CurrentIndex, NewIndex);
            ReOrder();
            Changed(beOrderChanged, []);

            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    /// <summary>Removes the object and unlinks it from the owner.</summary>
    [Delphi(PortStatus.Ported)]
    public override void RemoveByIndex(int index)
    {
        EnsureOrder();
        BoldSystem!.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningList, "RemoveByIndex", "");
            }

            TBoldObjectLocator Locator = LocatorList[index]!;
            PreChange();
            GetOtherEndController(Locator)!.Unlink(OwningList.OwningObject!.BoldObjectLocator, TBoldLinkUnlinkMode.blulMarkModified);
            LocatorList.RemoveByIndex(index);
            ReOrder();
            Changed(beItemDeleted, [Locator]);
            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    /// <summary>Gives each object of an ordered role its position as order number.</summary>
    [Delphi(PortStatus.Ported)]
    private void ReOrder()
    {
        if (!OwningObjectList.BoldRoleRTInfo!.IsOrdered)
        {
            return;
        }

        for (int I = 0; I < LocatorList.Count; I++)
        {
            TBoldObjectLocator Locator = LocatorList[I]!;
            Locator.EnsureBoldObject();
            GetOtherEndController(Locator)!.SetAndModifyOrderNo(I);
        }
    }

    /// <summary>
    /// Sets the list from fetched (or rolled back) ids. Fetching keeps links changed in memory (objects deleted or moved
    /// away, objects newly pointing here) and saves the database value for optimistic locking when it adjusts.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    internal void SetFromIdList(TBoldObjectIdList? ListOfOtherEnd, TBoldDomainElementProxyMode Mode)
    {
        TBoldObjectIdList NewListOfOtherEnd;

        bool AdjustList()
        {
            void SafeCopyOptimisticValues()
            {
                if (NewValueInOptimisticLocking() is IBoldObjectIdListRef Value)
                {
                    Value.SetFromIdList(ListOfOtherEnd);
                }
            }

            TBoldRoleRTInfo role = OwningObjectList.BoldRoleRTInfo!;
            TBoldClassTypeInfo BoldClassTypeInfoOfOtherEnd = role.ClassTypeInfoOfOtherEnd!;
            int IndexOfOtherEnd = role.IndexOfOtherEnd;
            bool result = false;
            for (int I = NewListOfOtherEnd.Count - 1; I >= 0; I--)
            {
                TBoldObject? BoldObject = BoldSystem!.Locators.ObjectByID(NewListOfOtherEnd[I]);
                if (BoldObject is not null)
                {
                    var AnObjectRef = BoldObject.BoldMemberAssigned(IndexOfOtherEnd) ? (TBoldObjectReference)BoldObject.BoldMembers(IndexOfOtherEnd) : null;

                    if (BoldObject.BoldExistenceState == TBoldExistenceState.besDeleted ||
                        (AnObjectRef is not null && AnObjectRef.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified &&
                         AnObjectRef.Locator != OwningList.OwningObject!.BoldObjectLocator))
                    {
                        if (!result)
                        {
                            SafeCopyOptimisticValues();
                            result = true;
                        }

                        NewListOfOtherEnd.RemoveByIndex(I);
                    }
                }
            }

            for (int I = 0; I < BoldSystem!.DirtyObjects.Count; I++)
            {
                var BoldObject = (TBoldObject)BoldSystem.DirtyObjects[I]!;
                if (BoldObject.BoldClassTypeInfo.BoldIsA(BoldClassTypeInfoOfOtherEnd) &&
                    BoldObject.BoldMemberAssigned(IndexOfOtherEnd) &&
                    BoldObject.BoldMembers(IndexOfOtherEnd).BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid &&
                    ((TBoldObjectReference)BoldObject.BoldMembers(IndexOfOtherEnd)).BoldObject == OwningList.OwningObject)
                {
                    if (!result)
                    {
                        SafeCopyOptimisticValues();
                        result = true;
                    }

                    NewListOfOtherEnd.Add(BoldObject.BoldObjectLocator!.BoldObjectID);
                }
            }

            return result;
        }

        // Delphi starts with PreChangeCalled = True, so PreChangeIfNeeded never calls PreChange; kept as it is.
        bool PreChangeCalled = true;

        void PreChangeIfNeeded()
        {
            if (!PreChangeCalled)
            {
                PreChange();
                PreChangeCalled = true;
            }
        }

        if (Mode == TBoldDomainElementProxyMode.bdepPMIn && OwningMember.OwningObject!.BoldObjectLocator!.BoldObjectID.TimeStamp != BoldDefs.BOLDMAXTIMESTAMP)
        {
            Mode = TBoldDomainElementProxyMode.bdepContents;
        }

        NewListOfOtherEnd = ListOfOtherEnd is not null ? ListOfOtherEnd.Clone() : new TBoldObjectIdList();
        bool WasAdjusted = Mode == TBoldDomainElementProxyMode.bdepPMIn && AdjustList();
        if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
        {
            ClearNoLongerReferring(NewListOfOtherEnd);
        }

        TBoldLinkUnlinkMode LinkUnlinkMode = Mode == TBoldDomainElementProxyMode.bdepPMIn ? TBoldLinkUnlinkMode.blulMarkAdjusted : TBoldLinkUnlinkMode.blulNone;
        bool PreserveOrder = Mode == TBoldDomainElementProxyMode.bdepContents ||
            (Mode == TBoldDomainElementProxyMode.bdepPMIn && OwningObjectList.BoldRoleRTInfo!.IsOrdered && !WasAdjusted);
        TBoldObjectLocator OwnLocator = OwningList.OwningObject!.BoldObjectLocator!;

        for (int I = GetCount() - 1; I >= 0; I--)
        {
            TBoldObjectLocator OldLocator = LocatorList[I]!;
            if (!NewListOfOtherEnd.IdInList(OldLocator.BoldObjectID))
            {
                PreChangeIfNeeded();
                if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
                {
                    SingleLinkUnlink(OldLocator, OwnLocator, LinkUnlinkMode);
                }

                LocatorList.RemoveByIndex(I);
                Changed(beItemDeleted, [OldLocator]);
            }
        }

        for (int I = 0; I < NewListOfOtherEnd.Count; I++)
        {
            TBoldObjectLocator NewLocator = AssertedLocatorForID(NewListOfOtherEnd[I]);
            if (PreserveOrder)
            {
                if (I >= LocatorList.Count)
                {
                    PreChangeIfNeeded();
                    if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
                    {
                        SingleLinkLinkTo(NewLocator, OwnLocator, false, LinkUnlinkMode);
                    }

                    LocatorList.Add(NewLocator);
                    Changed(beItemAdded, [NewLocator]);
                }
                else if (NewLocator == LocatorList[I])
                {
                    // in order
                }
                else if (LocatorList.IndexOf(NewLocator) != -1)
                {
                    PreChangeIfNeeded();
                    LocatorList.Move(LocatorList.IndexOf(NewLocator), I);
                    Changed(beOrderChanged, []);
                }
                else
                {
                    PreChangeIfNeeded();
                    if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
                    {
                        SingleLinkLinkTo(NewLocator, OwnLocator, false, LinkUnlinkMode);
                    }

                    LocatorList.Insert(I, NewLocator);
                    Changed(beItemAdded, [NewLocator]);
                }
            }
            else if (!IncludesLocator(NewLocator))
            {
                PreChangeIfNeeded();
                if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
                {
                    SingleLinkLinkTo(NewLocator, OwnLocator, false, LinkUnlinkMode);
                }

                LocatorList.Add(NewLocator);
                Changed(beItemAdded, [NewLocator]);
            }
        }
    }

    /// <summary>Replaces the object: unlinks the old one, links the new one.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetLocator(int index, TBoldObjectLocator Locator)
    {
        EnsureOrder();
        BoldSystem!.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningList, "SetLocator", "");
            }

            PreChange();
            TBoldObjectLocator OwnLocator = OwningList.OwningObject!.BoldObjectLocator!;
            GetOtherEndController(LocatorList[index]!)!.Unlink(OwnLocator, TBoldLinkUnlinkMode.blulMarkModified);
            GetOtherEndController(Locator)!.LinkTo(OwnLocator, true, TBoldLinkUnlinkMode.blulMarkModified);
            LocatorList[index] = Locator;
            ReOrder();
            Changed(beItemReplaced, [Locator, index]);

            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdListRef;

    /// <summary>The other end unlinked from the owner: removes the object.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Unlink(TBoldObjectLocator? OldLocator, TBoldLinkUnlinkMode Mode)
    {
        BoldClearLastFailure();
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified && !StartModify())
        {
            BoldRaiseLastFailure(OwningList, "Unlink", "");
        }

        PreChange();
        LocatorList.Remove(OldLocator);
        if (Mode == TBoldLinkUnlinkMode.blulMarkAdjusted)
        {
            OwningObjectList.Adjusted = true;
        }

        Changed(beItemDeleted, [OldLocator]);
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified)
        {
            EndModify();
        }
    }

    /// <summary>Gives the link as IBoldObjectIdListRef.</summary>
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

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBoldDirectMultiLinkController_Proxy);

    [Delphi(PortStatus.Ported)]
    public override void FreeContent() => LocatorList.Clear();

    [Delphi(PortStatus.Ported)]
    protected internal override void PrepareClear() => OwningObjectList.EnsureObjects();

    [Delphi(PortStatus.Ported)]
    protected override void Resort()
    {
        BoldSorter.BoldSort(0, LocatorList.Count - 1, CompareOrderNo, Exchange);
        Changed(beOrderChanged, []);
    }

    [Delphi(PortStatus.Ported)]
    protected override bool IsInOrder()
    {
        if (OwningObjectList.BoldRoleRTInfo!.IsOrdered)
        {
            for (int i = 0; i < LocatorList.Count - 1; i++)
            {
                if (GetOtherEndController(LocatorList[i]!)!.FOrderno > GetOtherEndController(LocatorList[i + 1]!)!.FOrderno)
                {
                    return false;
                }
            }
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    protected int CompareOrderNo(int Index1, int Index2)
    {
        if (!OwningObjectList.BoldRoleRTInfo!.RoleRTInfoOfOtherEnd!.IsSingleRole)
        {
            throw new EBold(sOtherEndMustBeSingle);
        }

        return GetOtherEndController(LocatorList[Index1]!)!.FOrderno - GetOtherEndController(LocatorList[Index2]!)!.FOrderno;
    }

    [Delphi(PortStatus.Ported)]
    protected void Exchange(int Index1, int Index2) => LocatorList.Exchange(Index1, Index2);

    /// <summary>
    /// Before a fetch: clears the single links of loaded objects (or the embedded links kept in locators) that point to the
    /// owner but are not in the fetched list.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected void ClearNoLongerReferring(TBoldObjectIdList NewList)
    {
        TBoldRoleRTInfo role = OwningObjectList.BoldRoleRTInfo!;
        int EmbeddedIndex = role.RoleRTInfoOfOtherEnd!.EmbeddedLinkIndex;
        int IndexOfOtherEnd = role.IndexOfOtherEnd;
        TBoldClassTypeInfo BoldClassTypeInfoOfOtherEnd = role.ClassTypeInfoOfOtherEnd!;

        void GetAllPointingAtSelf(TBoldObjectList ObjectList)
        {
            using TBoldLocatorListTraverser Traverser = BoldSystem!.Locators.CreateTraverser();
            TBoldObjectLocator OwnLocator = OwningMember.OwningObject!.BoldObjectLocator!;
            while (!Traverser.EndOfList)
            {
                TBoldObjectLocator Locator = Traverser.locator;
                if (BoldSystem.BoldSystemTypeInfo.TopSortedClasses[Locator.BoldObjectID.TopSortedIndex].BoldIsA(BoldClassTypeInfoOfOtherEnd))
                {
                    if (Locator.BoldObject is not null)
                    {
                        if (Locator.BoldObject.BoldMemberAssigned(IndexOfOtherEnd))
                        {
                            TBoldDirectSingleLinkController OtherEndController = GetOtherEndController(Locator)!;
                            TBoldObjectReference Reference = OtherEndController.OwningReference;
                            if ((Reference.BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent ||
                                 (Reference.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid && Reference.HasOldValues)) &&
                                OtherEndController.fLocator == OwnLocator)
                            {
                                ObjectList.AddLocator(Locator);
                            }
                        }
                    }
                    else if (EmbeddedIndex != -1 && Locator.EmbeddedSingleLinks(EmbeddedIndex) == OwnLocator)
                    {
                        ObjectList.AddLocator(Locator);
                    }
                }

                Traverser.Next();
            }
        }

        TBoldObjectList ListOfReferring;
        if (OwningList.BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent)
        {
            ListOfReferring = OwningObjectList;
        }
        else
        {
            ListOfReferring = new TBoldObjectList();
            if (BoldLinks.BoldPerformMultilinkConsistencyCheck || BoldLinks.BoldPerformMultiLinkConsistenceCheckLimit > BoldSystem!.Locators.Count)
            {
                GetAllPointingAtSelf(ListOfReferring);
            }
        }

        try
        {
            for (int i = 0; i < ListOfReferring.Count; i++)
            {
                TBoldObjectLocator aLocator = ListOfReferring.Locators(i)!;
                if (!NewList.IdInList(aLocator.BoldObjectID))
                {
                    TBoldDirectSingleLinkController? OtherEndController = GetOtherEndController(aLocator);
                    if (OtherEndController is not null)
                    {
                        OtherEndController.fLocator = null;
                        if (OtherEndController.OwningMember.BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid)
                        {
                            OtherEndController.OwningMember.BoldPersistenceState = TBoldValuePersistenceState.bvpsInvalid;
                            OtherEndController.OwningMember.SendEvent(beValueInvalid);
                        }

                        OtherEndController.OwningReference.HasOldValues = false;
                    }
                    else if (EmbeddedIndex != -1 && aLocator.EmbeddedSingleLinks(EmbeddedIndex) is not null)
                    {
                        aLocator.SetEmbeddedSingleLinks(EmbeddedIndex, null);
                    }
                }
            }
        }
        finally
        {
            if (ListOfReferring != OwningObjectList)
            {
                ListOfReferring.Dispose();
            }
        }
    }

    /// <summary>Removes all objects, unlinking each from the owner, in a transaction.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Clear()
    {
        BoldSystem!.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningList, "Clear", "");
            }

            PreChange();
            while (Count > 0)
            {
                int ix = Count - 1;
                TBoldObjectLocator Locator = LocatorList[ix]!;
                GetOtherEndController(Locator)!.Unlink(OwningList.OwningObject!.BoldObjectLocator, TBoldLinkUnlinkMode.blulMarkModified);
                LocatorList.RemoveByIndex(ix);
                Changed(beItemDeleted, [Locator]);
            }

            ReOrder();
            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)] internal TBoldObjectLocatorList LocatorList => fLocatorList!;
}

/// <summary>Gives a direct single link as IBoldObjectIdRef (id and order number).</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldDirectSingleLinkController_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldMember_Proxy(aProxedElement, aMode), IBoldObjectIdRef
{
    [Delphi(PortStatus.Ported)]
    private TBoldDirectSingleLinkController GetDirectSingleLinkController() => (TBoldDirectSingleLinkController)ProxedController!;

    /// <summary>Sets the id (and, except for contents, the order number); undo and contents also set whether old values exist.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldObjectIdRef s)
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, "AssignContentValue"));
        }

        SetFromId(s.Id);
        if (Mode != TBoldDomainElementProxyMode.bdepContents)
        {
            SetOrderNo(s.OrderNo);
        }

        if (Mode is TBoldDomainElementProxyMode.bdepUnDo or TBoldDomainElementProxyMode.bdepContents)
        {
            ((TBoldObjectReference)ProxedMember).HasOldValues = s.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid && s.Id is not null;
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId? GetId() => DirectSingleLInkController.fLocator?.BoldObjectID;

    [Delphi(PortStatus.Ported)]
    public int GetOrderNo() => DirectSingleLInkController.OrderNo;

    [Delphi(PortStatus.Ported)]
    public void SetFromId(TBoldObjectId? Id)
    {
        if (Mode is TBoldDomainElementProxyMode.bdepPMIn or TBoldDomainElementProxyMode.bdepContents or TBoldDomainElementProxyMode.bdepUnDo
            or TBoldDomainElementProxyMode.bdepInternalInitialize)
        {
            DirectSingleLInkController.SetFromId(Id, Mode);
        }
        else
        {
            UnsupportedMode(Mode, "SetFromId");
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SetOrderNo(int NewOrder)
    {
        if (Mode is TBoldDomainElementProxyMode.bdepPMIn or TBoldDomainElementProxyMode.bdepUnDo or TBoldDomainElementProxyMode.bdepContents)
        {
            DirectSingleLInkController.SetOrderNo(NewOrder, Mode);
        }
        else
        {
            UnsupportedMode(Mode, "SetOrderNo");
        }
    }

    [Delphi(PortStatus.Ported)] protected TBoldDirectSingleLinkController DirectSingleLInkController => GetDirectSingleLinkController();
}

/// <summary>Gives an indirect single link as IBoldObjectIdRefPair (link object and other end).</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldIndirectSingleLinkController_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldMember_Proxy(aProxedElement, aMode), IBoldObjectIdRefPair
{
    [Delphi(PortStatus.Ported)]
    private TBoldIndirectSingleLinkController GetInDirectSingleLinkController() => (TBoldIndirectSingleLinkController)ProxedController!;

    [Delphi(PortStatus.Ported)]
    protected override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldObjectIdRefPair s)
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, "AssignContentValue"));
        }

        SetFromIds(s.Id1, s.Id2);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId? GetId1() => InDirectSingleLInkController.fLinkObjectLocator?.BoldObjectID;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId? GetId2() => InDirectSingleLInkController.fOtherEndLocator?.BoldObjectID;

    [Delphi(PortStatus.Ported, Note = "An indirect link has no order number.")]
    public int GetOrderNo() => 0;

    [Delphi(PortStatus.Ported)]
    public void SetFromIds(TBoldObjectId? Id1, TBoldObjectId? Id2)
    {
        if (Mode is TBoldDomainElementProxyMode.bdepContents or TBoldDomainElementProxyMode.bdepPMIn)
        {
            InDirectSingleLInkController.SetFromIds(Id1, Id2, Mode);
        }
        else
        {
            UnsupportedMode(Mode, "SetFromId");
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SetOrderNo(int NewOrder) => UnsupportedMode(Mode, "SetOrderNo");

    [Delphi(PortStatus.Ported)] protected TBoldIndirectSingleLinkController InDirectSingleLInkController => GetInDirectSingleLinkController();
}

/// <summary>Gives a direct multi link as IBoldObjectIdListRef.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldDirectMultiLinkController_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldMember_Proxy(aProxedElement, aMode), IBoldObjectIdListRef
{
    [Delphi(PortStatus.Ported)]
    private TBoldDirectMultiLinkController GetDirectMultiLinkController() => (TBoldDirectMultiLinkController)ProxedController!;

    [Delphi(PortStatus.Ported)]
    protected override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldObjectIdListRef s)
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, "AssignContentValue"));
        }

        var anIdList = new TBoldObjectIdList();
        for (int i = 0; i < s.Count; i++)
        {
            anIdList.Add(s.IdList(i));
        }

        SetFromIdList(anIdList);
    }

    [Delphi(PortStatus.Ported)]
    public int GetCount() => DirectMultiLinkController.Count;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId GetIdList(int Index) => DirectMultiLinkController.LocatorList[Index]!.BoldObjectID;

    [Delphi(PortStatus.Ported)]
    public void SetFromIdList(TBoldObjectIdList? IdLIst)
    {
        if (Mode is TBoldDomainElementProxyMode.bdepContents or TBoldDomainElementProxyMode.bdepPMIn)
        {
            DirectMultiLinkController.SetFromIdList(IdLIst, Mode);
        }
        else
        {
            UnsupportedMode(Mode, "SetFromIdList");
        }
    }

    [Delphi(PortStatus.Ported)] protected TBoldDirectMultiLinkController DirectMultiLinkController => GetDirectMultiLinkController();
}

/// <summary>Gives an indirect multi link as IBoldObjectIdListRefPair (link objects and other ends).</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldInDirectMultiLinkController_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldMember_Proxy(aProxedElement, aMode), IBoldObjectIdListRefPair
{
    [Delphi(PortStatus.Ported)]
    private TBoldIndirectMultiLinkController GetInDirectMultiLinkController() => (TBoldIndirectMultiLinkController)ProxedController!;

    [Delphi(PortStatus.Ported)]
    protected override void AssignContentValue(IBoldValue Source)
    {
        if (Source is not IBoldObjectIdListRefPair s)
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, "AssignContentValue"));
        }

        var anIdList1 = new TBoldObjectIdList();
        var anIdList2 = new TBoldObjectIdList();
        for (int i = 0; i < s.Count; i++)
        {
            anIdList1.Add(s.IdList1(i));
            anIdList2.Add(s.IdList2(i));
        }

        SetFromIdLists(anIdList1, anIdList2);
    }

    [Delphi(PortStatus.Ported)]
    public int GetCount() => InDirectMultiLinkController.Count;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId GetIdList1(int Index) => InDirectMultiLinkController.LinkLocatorList[Index]!.BoldObjectID;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId GetIdList2(int Index) => InDirectMultiLinkController.ReferredLocatorList[Index]!.BoldObjectID;

    [Delphi(PortStatus.Ported)]
    public void SetFromIdLists(TBoldObjectIdList? IdList1, TBoldObjectIdList? IdList2)
    {
        if (Mode is TBoldDomainElementProxyMode.bdepPMIn or TBoldDomainElementProxyMode.bdepContents)
        {
            InDirectMultiLinkController.SetFromIDLists(IdList1, IdList2, Mode);
        }
        else
        {
            UnsupportedMode(Mode, "SetFromIdLists");
        }
    }

    [Delphi(PortStatus.Ported)] protected TBoldIndirectMultiLinkController InDirectMultiLinkController => GetInDirectMultiLinkController();
}

/// <summary>Unit-level variables of BoldLinks.</summary>
public static partial class BoldLinks
{
    /// <summary>Whether fetching a multi link that is not loaded looks through all locators for objects still pointing to its owner.</summary>
    [Delphi(PortStatus.Ported)] public static bool BoldPerformMultilinkConsistencyCheck = true;

    /// <summary>Below this many locators the consistency check runs even when it is turned off.</summary>
    [Delphi(PortStatus.Ported)] public static int BoldPerformMultiLinkConsistenceCheckLimit = 5000;
}
