// Delphi: Source/ObjectSpace/BORepresentation/BoldLinks.pas
// Port map: CSharp/port/units/BoldLinks.md
// The multi link base, the rest of the direct multi link, the indirect multi link and the link object controllers.

using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>A multi link controller: creates objects of the other end's class and keeps an ordered role sorted by order number.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMultiLinkController(TBoldObjectList aOwningList) : TBoldAbstractObjectListController(aOwningList)
{
    private bool fMayBeOutOfOrder;

    [Delphi(PortStatus.Ported)]
    protected override bool GetCanCreateNew() => !OwningObjectList.BoldRoleRTInfo!.ClassTypeInfoOfOtherEnd!.IsAbstract;

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldElement CreateNew() =>
        OwningList.BoldSystem!.CreateNewObjectByExpressionName(OwningObjectList.BoldRoleRTInfo!.ClassTypeInfoOfOtherEnd!.ExpressionName);

    [Delphi(PortStatus.Ported)] protected abstract bool IsInOrder();

    /// <summary>Sorts the list by the other ends' order numbers.</summary>
    [Delphi(PortStatus.Ported)] protected abstract void Resort();

    /// <summary>An order number changed (from the database): the list is sorted before its next use.</summary>
    [Delphi(PortStatus.Ported)]
    public void MarkPossiblyOutOfOrder()
    {
        if (OwningObjectList.BoldRoleRTInfo!.IsOrdered)
        {
            fMayBeOutOfOrder = true;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void EnsureOrder()
    {
        if (fMayBeOutOfOrder)
        {
            if (!IsInOrder())
            {
                Resort();
            }

            fMayBeOutOfOrder = false;
        }
    }
}

public partial class TBoldDirectMultiLinkController
{
    /// <summary>The single link back to the owner in the object of the locator (fetching it).</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldDirectSingleLinkController? GetOtherEndController(TBoldObjectLocator Locator) =>
        (TBoldDirectSingleLinkController?)GetControllerForMember(Locator.EnsuredBoldObject!.BoldMembers(OwningObjectList.BoldRoleRTInfo!.IndexOfOtherEnd));

    /// <summary>Unlinks the other end of a loaded object, or clears the link kept in the locator of an unloaded one.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SingleLinkUnlink(TBoldObjectLocator Locator, TBoldObjectLocator OldLocator, TBoldLinkUnlinkMode Mode)
    {
        if (Locator.BoldObject is not null)
        {
            GetOtherEndController(Locator)!.Unlink(OldLocator, Mode);
        }
        else
        {
            int EmbeddedIndex = OwningObjectList.BoldRoleRTInfo!.RoleRTInfoOfOtherEnd!.EmbeddedLinkIndex;
            if (EmbeddedIndex != -1 && Locator.EmbeddedSingleLinks(EmbeddedIndex) is not null)
            {
                Locator.SetEmbeddedSingleLinks(EmbeddedIndex, null);
            }
        }
    }

    /// <summary>Links the other end of a loaded object, or keeps the link in the locator of an unloaded one.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SingleLinkLinkTo(TBoldObjectLocator Locator, TBoldObjectLocator NewLocator, bool updateOrderNo, TBoldLinkUnlinkMode Mode)
    {
        if (Locator.BoldObject is not null)
        {
            GetOtherEndController(Locator)!.LinkTo(NewLocator, updateOrderNo, Mode);
        }
        else
        {
            int EmbeddedIndex = OwningObjectList.BoldRoleRTInfo!.RoleRTInfoOfOtherEnd!.EmbeddedLinkIndex;
            Locator.SetEmbeddedSingleLinks(EmbeddedIndex, NewLocator);
        }
    }
}

/// <summary>The controller of a multi link through link objects: keeps the link objects and the objects they refer to, in step.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIndirectMultiLinkController : TBoldMultiLinkController
{
    internal TBoldObjectLocatorList? fLinkLocatorList;
    internal TBoldObjectLocatorList? fReferredList;

    [Delphi(PortStatus.Ported)]
    public TBoldIndirectMultiLinkController(TBoldObjectList OwningList)
        : base(OwningList)
    {
        fLinkLocatorList = new TBoldObjectLocatorList();
        fReferredList = new TBoldObjectLocatorList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldIndirectMultiLinkController.Destroy")]
    public override void Dispose()
    {
        fLinkLocatorList?.Dispose();
        fLinkLocatorList = null;
        fReferredList?.Dispose();
        fReferredList = null;
        base.Dispose();
    }

    /// <summary>Creates a link object to the object and adds both, in a transaction.</summary>
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
            TBoldObject LinkObject = NewLink(Locator, TBoldLinkUnlinkMode.blulMarkModified);
            LinkLocatorList.Add(LinkObject.BoldObjectLocator);
            ReferredLocatorList.Add(Locator);
            if (OwningObjectList.BoldRoleRTInfo!.IsOrdered)
            {
                GetLinkObjectOwnLinkController(LinkObject).SetAndModifyOrderNo(LinkLocatorList.IndexOf(LinkObject.BoldObjectLocator));
            }

            Changed(beItemAdded, [Locator]);
            GetLinkObjectListController().Changed(beItemAdded, [LinkObject.BoldObjectLocator]);
            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    /// <summary>Unlinks the link object from both ends and deletes it (the owner's own lists are left to the caller).</summary>
    [Delphi(PortStatus.Ported)]
    private void DeleteLink(TBoldObjectLocator LinkObjectLocator, TBoldLinkUnlinkMode Mode)
    {
        TBoldObject OldLinkObject = LinkObjectLocator.EnsuredBoldObject!;
        TBoldLinkObjectSingleLinkController other = GetLinkObjectOtherLinkController(OldLinkObject);
        other.RemoveFromOtherEnd(Mode);
        other.Unlink(other.fLocator, Mode);
        TBoldLinkObjectSingleLinkController own = GetLinkObjectOwnLinkController(OldLinkObject);
        own.PreChange();
        own.Unlink(OwningMember.OwningObject!.BoldObjectLocator, Mode);
        OldLinkObject.Delete();
    }

    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent()
    {
        EnsureOrder();
        DbFetchOwningMember();
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetCount() => LinkLocatorList.Count;

    [Delphi(PortStatus.Ported)]
    private TBoldLinkObjectSingleLinkController GetLinkObjectOwnLinkController(TBoldObject LinkObject) =>
        (TBoldLinkObjectSingleLinkController)GetControllerForMember(LinkObject.BoldMembers(OwningObjectList.BoldRoleRTInfo!.OwnIndexInLinkClass))!;

    [Delphi(PortStatus.Ported)]
    private TBoldLinkObjectSingleLinkController GetLinkObjectOtherLinkController(TBoldObject LinkObject) =>
        (TBoldLinkObjectSingleLinkController)GetControllerForMember(LinkObject.BoldMembers(OwningObjectList.BoldRoleRTInfo!.OtherIndexInLinkClass))!;

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocator(int index)
    {
        EnsureOrder();
        return ReferredLocatorList[index];
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocatorByQualifiersAndSubscribe(TBoldMemberList MemberList, TBoldSubscriber? Subscriber)
    {
        EnsureOrder();
        if (!ReferredLocatorList.HasMembersIndex)
        {
            if (OwningObjectList.BoldRoleRTInfo is { IsQualified: true } role)
            {
                OwningObjectList.EnsureObjects();
                ReferredLocatorList.InitMembersIndex(OwningObjectList, role.Qualifiers!);
            }
            else
            {
                throw new EBold(SysUtils.Format(sRolenotQualified, GetType().Name));
            }
        }

        return ReferredLocatorList.GetLocatorByAttributesAndSubscribe(MemberList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    public override bool IncludesLocator(TBoldObjectLocator? Locator) => ReferredLocatorList.LocatorInList(Locator);

    [Delphi(PortStatus.Ported)]
    public override int IndexOfLocator(TBoldObjectLocator? Locator)
    {
        EnsureOrder();
        return ReferredLocatorList.IndexOf(Locator);
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
            TBoldObjectLocator NewLinkLocator = NewLink(Locator, TBoldLinkUnlinkMode.blulMarkModified).BoldObjectLocator!;
            LinkLocatorList.Insert(index, NewLinkLocator);
            ReferredLocatorList.Insert(index, Locator);
            ReOrder();
            Changed(beItemAdded, [Locator]);
            GetLinkObjectListController().Changed(beItemAdded, [NewLinkLocator]);
            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    /// <summary>A link object was linked to the owner: adds it and the object it refers to.</summary>
    [Delphi(PortStatus.Ported)]
    public override void LinkTo(TBoldObjectLocator? NewLocator, bool updateOrderNo, TBoldLinkUnlinkMode Mode)
    {
        BoldClearLastFailure();
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified && !StartModify())
        {
            BoldRaiseLastFailure(OwningList, "Linkto", "");
        }

        PreChange();
        TBoldObjectLocator? NewReferredLocator = GetLinkObjectOtherLinkController(NewLocator!.EnsuredBoldObject!).fLocator;
        LinkLocatorList.Add(NewLocator);
        ReferredLocatorList.Add(NewReferredLocator);
        if (updateOrderNo && OwningObjectList.BoldRoleRTInfo!.IsOrdered)
        {
            GetLinkObjectOwnLinkController(NewLocator.BoldObject!).SetAndModifyOrderNo(LinkLocatorList.IndexOf(NewLocator));
        }

        if (Mode == TBoldLinkUnlinkMode.blulMarkAdjusted)
        {
            OwningObjectList.Adjusted = true;
        }

        Changed(beItemAdded, [NewReferredLocator]);
        GetLinkObjectListController().Changed(beItemAdded, [NewLocator]);
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified)
        {
            EndModify();
        }
    }

    /// <summary>Moves an element of an ordered role (nothing for an unordered one).</summary>
    [Delphi(PortStatus.Ported)]
    public override void Move(int CurrentIndex, int NewIndex)
    {
        EnsureOrder();
        if (!OwningObjectList.BoldRoleRTInfo!.IsOrdered)
        {
            return;
        }

        BoldSystem!.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningList, "Move", "");
            }

            PreChange();
            LinkLocatorList.Move(CurrentIndex, NewIndex);
            ReferredLocatorList.Move(CurrentIndex, NewIndex);
            ReOrder();
            Changed(beOrderChanged, []);
            GetLinkObjectListController().Changed(beOrderChanged, []);
            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    /// <summary>Creates a link object between the owner and the other object, and links it into the other end.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldObject NewLink(TBoldObjectLocator OtherLocator, TBoldLinkUnlinkMode Mode)
    {
        TBoldClassTypeInfo LinkClassTypeInfo = OwningObjectList.BoldRoleRTInfo!.LinkClassTypeInfo!;
        TBoldObject LinkObject = TBoldObject.InternalCreateNewWithClassAndSystem(LinkClassTypeInfo.ObjectClass!, LinkClassTypeInfo, OwningMember.BoldSystem!,
            OtherLocator.ObjectIsPersistent && OwningMember.OwningObject!.BoldPersistent);
        TBoldAbstractController? OtherEndController = GetLinkObjectOtherLinkController(LinkObject).GetOtherEndController(OtherLocator, true);
        GetLinkObjectOwnLinkController(LinkObject).LinkTo(OwningMember.OwningObject!.BoldObjectLocator, true, Mode);
        GetLinkObjectOtherLinkController(LinkObject).LinkTo(OtherLocator, true, Mode);
        OtherEndController?.LinkTo(LinkObject.BoldObjectLocator, false, Mode);
        return LinkObject;
    }

    /// <summary>Removes the element: deletes its link object (destruction delayed until the lists are updated).</summary>
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

            PreChange();
            TBoldObjectLocator? Locator = ReferredLocatorList[index];
            TBoldObjectLocator LinkLocator = LinkLocatorList[index]!;
            BoldSystem.DelayObjectDestruction();
            try
            {
                DeleteLink(LinkLocator, TBoldLinkUnlinkMode.blulMarkModified);
                LinkLocatorList.RemoveByIndex(index);
                ReferredLocatorList.RemoveByIndex(index);
            }
            finally
            {
                BoldSystem.AllowObjectDestruction();
            }

            ReOrder();
            Changed(beItemDeleted, [Locator]);
            GetLinkObjectListController().Changed(beItemDeleted, [LinkLocator]);
            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    /// <summary>Gives each link object of an ordered role its position as order number (loading them first).</summary>
    [Delphi(PortStatus.Ported)]
    private void ReOrder()
    {
        if (OwningObjectList.BoldRoleRTInfo!.IsOrdered)
        {
            for (int I = 0; I < LinkLocatorList.Count; I++)
            {
                LinkLocatorList[I]!.EnsureBoldObject();
            }

            for (int I = 0; I < LinkLocatorList.Count; I++)
            {
                GetLinkObjectOwnLinkController(LinkLocatorList[I]!.BoldObject!).SetAndModifyOrderNo(I);
            }
        }
    }

    /// <summary>Sets the lists from fetched (or rolled back) ids, keeping link objects deleted or created in memory.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void SetFromIDLists(TBoldObjectIdList? ListOfLinkObjects, TBoldObjectIdList? ListOfOtherEnd, TBoldDomainElementProxyMode Mode)
    {
        TBoldObjectIdList NewListOfLinkObjects;
        TBoldObjectIdList NewListOfOtherEnd;

        void SafeCopyOptimisticValues()
        {
            if (NewValueInOptimisticLocking() is IBoldObjectIdListRefPair Value)
            {
                Value.SetFromIdLists(ListOfLinkObjects, ListOfOtherEnd);
            }
        }

        bool AdjustLists()
        {
            bool result = false;
            TBoldRoleRTInfo role = OwningObjectList.BoldRoleRTInfo!;
            TBoldClassTypeInfo BoldLinkClassTypeInfo = role.LinkClassTypeInfo!;
            int IndexOfOwnEnd = role.OwnIndexInLinkClass;
            int IndexOfOtherEnd = role.OtherIndexInLinkClass;
            for (int I = NewListOfLinkObjects.Count - 1; I >= 0; I--)
            {
                TBoldObject? AObject = BoldSystem!.Locators.ObjectByID(NewListOfLinkObjects[I]);
                if (AObject is not null)
                {
                    if (AObject.BoldExistenceState == TBoldExistenceState.besDeleted)
                    {
                        if (!result)
                        {
                            SafeCopyOptimisticValues();
                            result = true;
                        }

                        NewListOfLinkObjects.RemoveByIndex(I);
                        NewListOfOtherEnd.RemoveByIndex(I);
                    }
                }
                else
                {
                    TBoldObjectLocator LinkObjectLocator = AssertedLocatorForID(NewListOfLinkObjects[I]);
                    int EmbeddedIndexOfOwnEnd = BoldLinkClassTypeInfo.AllMembers[IndexOfOwnEnd].EmbeddedLinkIndex;
                    int EmbeddedIndexOfOtherEnd = BoldLinkClassTypeInfo.AllMembers[IndexOfOtherEnd].EmbeddedLinkIndex;
                    if (EmbeddedIndexOfOwnEnd != -1)
                    {
                        LinkObjectLocator.SetEmbeddedSingleLinks(EmbeddedIndexOfOwnEnd, OwningList.OwningObject!.BoldObjectLocator);
                    }

                    if (EmbeddedIndexOfOtherEnd != -1)
                    {
                        LinkObjectLocator.SetEmbeddedSingleLinks(EmbeddedIndexOfOtherEnd, AssertedLocatorForID(NewListOfOtherEnd[I]));
                    }
                }
            }

            for (int I = 0; I < BoldSystem!.DirtyObjects.Count; I++)
            {
                var AObject = (TBoldObject)BoldSystem.DirtyObjects[I]!;
                if (AObject.BoldClassTypeInfo.BoldIsA(BoldLinkClassTypeInfo) && AObject.BoldExistenceState == TBoldExistenceState.besExisting &&
                    ((TBoldObjectReference)AObject.BoldMembers(IndexOfOwnEnd)).BoldObject == OwningList.OwningObject)
                {
                    if (!result)
                    {
                        SafeCopyOptimisticValues();
                        result = true;
                    }

                    NewListOfLinkObjects.Add(AObject.BoldObjectLocator!.BoldObjectID);
                    NewListOfOtherEnd.Add(((TBoldObjectReference)AObject.BoldMembers(IndexOfOtherEnd)).Locator!.BoldObjectID);
                }
            }

            return result;
        }

        bool PreChangeCalled = false;

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

        if (ListOfLinkObjects is not null)
        {
            NewListOfLinkObjects = ListOfLinkObjects.Clone();
            NewListOfOtherEnd = ListOfOtherEnd!.Clone();
        }
        else
        {
            NewListOfLinkObjects = new TBoldObjectIdList();
            NewListOfOtherEnd = new TBoldObjectIdList();
        }

        bool WasAdjusted = Mode == TBoldDomainElementProxyMode.bdepPMIn && AdjustLists();
        if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
        {
            ClearNoLongerReferring(NewListOfOtherEnd);
        }

        bool PreserveOrder = Mode == TBoldDomainElementProxyMode.bdepContents ||
            (Mode == TBoldDomainElementProxyMode.bdepPMIn && OwningObjectList.BoldRoleRTInfo!.IsOrdered && !WasAdjusted);

        for (int I = GetCount() - 1; I >= 0; I--)
        {
            if (!NewListOfLinkObjects.IdInList(LinkLocatorList[I]!.BoldObjectID))
            {
                PreChangeIfNeeded();
                TBoldObjectLocator? OldLocator = LinkLocatorList[I];
                LinkLocatorList.RemoveByIndex(I);
                Changed(beItemDeleted, [OldLocator]);
                OldLocator = ReferredLocatorList[I];
                ReferredLocatorList.RemoveByIndex(I);
                GetLinkObjectListController().Changed(beItemDeleted, [OldLocator]);
            }
        }

        if (PreserveOrder)
        {
            for (int I = 0; I < NewListOfOtherEnd.Count; I++)
            {
                TBoldObjectLocator ObjectLocator = AssertedLocatorForID(NewListOfOtherEnd[I]);
                if (I >= ReferredLocatorList.Count)
                {
                    PreChangeIfNeeded();
                    TBoldObjectLocator NewLocator = AssertedLocatorForID(NewListOfLinkObjects[I]);
                    LinkLocatorList.Add(NewLocator);
                    Changed(beItemAdded, [NewLocator]);
                    ReferredLocatorList.Add(ObjectLocator);
                    GetLinkObjectListController().Changed(beItemDeleted, [ObjectLocator]);
                }
                else if (ObjectLocator == ReferredLocatorList[I])
                {
                    // in order
                }
                else if (ReferredLocatorList.IndexOf(ObjectLocator) != -1)
                {
                    PreChangeIfNeeded();
                    LinkLocatorList.Move(ReferredLocatorList.IndexOf(ObjectLocator), I);
                    ReferredLocatorList.Move(ReferredLocatorList.IndexOf(ObjectLocator), I);
                    Changed(beOrderChanged, []);
                    GetLinkObjectListController().Changed(beOrderChanged, []);
                }
                else
                {
                    PreChangeIfNeeded();
                    TBoldObjectLocator NewLocator = AssertedLocatorForID(NewListOfLinkObjects[I]);
                    LinkLocatorList.Insert(I, NewLocator);
                    ReferredLocatorList.Insert(I, ObjectLocator);
                    Changed(beItemAdded, [NewLocator]);
                }
            }
        }
        else
        {
            for (int I = 0; I < NewListOfLinkObjects.Count; I++)
            {
                TBoldObjectLocator ObjectLocator = AssertedLocatorForID(NewListOfOtherEnd[I]);
                if (!ReferredLocatorList.LocatorInList(ObjectLocator))
                {
                    PreChangeIfNeeded();
                    TBoldObjectLocator NewLocator = AssertedLocatorForID(NewListOfLinkObjects[I]);
                    LinkLocatorList.Add(NewLocator);
                    Changed(beItemAdded, [NewLocator]);
                    ReferredLocatorList.Add(ObjectLocator);
                    GetLinkObjectListController().Changed(beItemDeleted, [ObjectLocator]);
                }
            }
        }
    }

    /// <summary>Replaces the element: creates a link object to the new object and deletes the old one.</summary>
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

            TBoldObjectLocator OldLinkLocator = LinkLocatorList[index]!;
            PreChange();
            LinkLocatorList[index] = NewLink(Locator, TBoldLinkUnlinkMode.blulMarkModified).BoldObjectLocator;
            ReferredLocatorList[index] = Locator;
            DeleteLink(OldLinkLocator, TBoldLinkUnlinkMode.blulMarkModified);
            Changed(beItemReplaced, [Locator, index]);
            GetLinkObjectListController().Changed(beItemReplaced, [OldLinkLocator, index]);
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
    protected internal override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair;

    /// <summary>A link object was unlinked from the owner: removes it and the object it referred to.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Unlink(TBoldObjectLocator? OldLocator, TBoldLinkUnlinkMode Mode)
    {
        BoldClearLastFailure();
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified && !StartModify())
        {
            BoldRaiseLastFailure(OwningList, "Unlink", "");
        }

        PreChange();
        int OldIndex = LinkLocatorList.IndexOf(OldLocator);
        LinkLocatorList.Remove(OldLocator);
        TBoldObjectLocator? OldLinkLocator = ReferredLocatorList[OldIndex];
        ReferredLocatorList.RemoveByIndex(OldIndex);
        if (Mode == TBoldLinkUnlinkMode.blulMarkAdjusted)
        {
            OwningObjectList.Adjusted = true;
        }

        Changed(beItemDeleted, [OldLocator]);
        GetLinkObjectListController().Changed(beItemDeleted, [OldLinkLocator]);
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified)
        {
            EndModify();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldLinkObjectListController GetLinkObjectListController() => (TBoldLinkObjectListController)ControllerForLinkMember();

    /// <summary>Gives the link as IBoldObjectIdListRefPair.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldObjectIdListRefPair))
        {
            Obj = Activator.CreateInstance(ProxyClass(), OwningList, Mode);
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBoldInDirectMultiLinkController_Proxy);

    [Delphi(PortStatus.Ported)]
    public override void FreeContent()
    {
        LinkLocatorList.Clear();
        ReferredLocatorList.Clear();
    }

    /// <summary>Fetches the link objects before clearing.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void PrepareClear() =>
        ((TBoldObjectList)OwningList.OwningObject!.BoldMembers(OwningObjectList.BoldRoleRTInfo!.IndexOfLinkObjectRole)).EnsureObjects();

    [Delphi(PortStatus.Ported)]
    protected override void Resort()
    {
        BoldSorter.BoldSort(0, LinkLocatorList.Count - 1, CompareOrderNo, Exchange);
        Changed(beOrderChanged, []);
    }

    /// <summary>True when the loaded link objects are in order-number order.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool IsInOrder()
    {
        bool result = true;
        if (OwningObjectList.BoldRoleRTInfo!.IsOrdered)
        {
            int i = 0;
            TBoldObject? CurrentObj = null;
            while (result && i < LinkLocatorList.Count - 1)
            {
                if (LinkLocatorList[i]!.BoldObject is { } obj)
                {
                    TBoldObject? PreviousObj = CurrentObj;
                    CurrentObj = obj;
                    if (PreviousObj is not null)
                    {
                        result = GetLinkObjectOwnLinkController(PreviousObj).FOrderno <= GetLinkObjectOwnLinkController(CurrentObj).FOrderno;
                    }
                }

                i++;
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected int CompareOrderNo(int Index1, int Index2)
    {
        if (!OwningObjectList.BoldRoleRTInfo!.RoleRTInfoOfOtherEnd!.IsSingleRole)
        {
            throw new EBold(sOtherEndMustBeSingle);
        }

        return GetLinkObjectOwnLinkController(LinkLocatorList[Index1]!.BoldObject!).FOrderno -
               GetLinkObjectOwnLinkController(LinkLocatorList[Index2]!.BoldObject!).FOrderno;
    }

    [Delphi(PortStatus.Ported)]
    protected void Exchange(int Index1, int Index2)
    {
        LinkLocatorList.Exchange(Index1, Index2);
        ReferredLocatorList.Exchange(Index1, Index2);
    }

    /// <summary>Checks that the two lists match and agree with the loaded link objects' own links.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's assertions raise EBoldInternal here, also in release builds.")]
    public override bool AssertIntegrity()
    {
        BoldLinks.CheckIntegrity(LinkLocatorList.Count == ReferredLocatorList.Count);
        for (int i = 0; i < LinkLocatorList.Count; i++)
        {
            BoldLinks.CheckIntegrity(LinkLocatorList[i] is not null);
            BoldLinks.CheckIntegrity(ReferredLocatorList[i] is not null);
            if (LinkLocatorList[i]!.BoldObject is { } linkObject)
            {
                BoldLinks.CheckIntegrity(linkObject.BoldExistenceState == TBoldExistenceState.besExisting);
                BoldLinks.CheckIntegrity(GetLinkObjectOwnLinkController(linkObject).fLocator == OwningObjectList.OwningObject!.BoldObjectLocator);
                BoldLinks.CheckIntegrity(GetLinkObjectOtherLinkController(linkObject).fLocator == ReferredLocatorList[i]);
            }
        }

        return true;
    }

    [Delphi(PortStatus.Ported, Note = "Empty in Delphi too (marked TODO there).")]
    private void ClearNoLongerReferring(TBoldObjectIdList NewList)
    {
    }

    /// <summary>Removes all elements, deleting their link objects, in a transaction.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Clear()
    {
        BoldSystem!.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningList, "RemoveByIndex", "");
            }

            PreChange();
            BoldSystem.DelayObjectDestruction();
            try
            {
                while (Count > 0)
                {
                    int ix = Count - 1;
                    TBoldObjectLocator? Locator = ReferredLocatorList[ix];
                    TBoldObjectLocator LinkLocator = LinkLocatorList[ix]!;
                    DeleteLink(LinkLocator, TBoldLinkUnlinkMode.blulMarkModified);
                    LinkLocatorList.RemoveByIndex(ix);
                    ReferredLocatorList.RemoveByIndex(ix);
                    Changed(beItemDeleted, [Locator]);
                    GetLinkObjectListController().Changed(beItemDeleted, [LinkLocator]);
                }
            }
            finally
            {
                BoldSystem.AllowObjectDestruction();
            }

            EndModify();
            BoldSystem.CommitTransaction();
        }
        catch
        {
            BoldSystem.RollbackTransaction();
            throw;
        }
    }

    /// <summary>The controller of the link object role that goes with this role (the member after it).</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldAbstractObjectListController ControllerForLinkMember() =>
        (TBoldAbstractObjectListController)GetControllerForMember(OwningList.OwningObject!.BoldMembers(OwningObjectList.BoldRoleRTInfo!.index + 1))!;

    [Delphi(PortStatus.Ported)] internal TBoldObjectLocatorList LinkLocatorList => fLinkLocatorList!;
    [Delphi(PortStatus.Ported)] internal TBoldObjectLocatorList ReferredLocatorList => fReferredList!;
}

/// <summary>The controller of the list of link objects of an indirect multi link: a read-only view of the main role's link objects.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLinkObjectListController(TBoldObjectList aOwningList) : TBoldAbstractObjectListController(aOwningList)
{
    [Delphi(PortStatus.Ported)]
    public override void AddLocator(TBoldObjectLocator Locator) =>
        throw new EBold(SysUtils.Format(sInvalidForListOfLinkObjects, GetType().Name, "AddLocator"));

    /// <summary>Fetches the main role; this list is then current.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent()
    {
        GetMainList().EnsureContentsCurrent();
        GetObjectList().BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetCount() => LocatorList.Count;

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocator(int index) => LocatorList[index];

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocatorByQualifiersAndSubscribe(TBoldMemberList MemberList, TBoldSubscriber? Subscriber) =>
        throw new EBoldFeatureNotImplementedYet(SysUtils.Format("%s.GetLocatorByQualifiersAndSubscribe", GetType().Name));

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocatorList GetLocatorList() => GetMainListController().LinkLocatorList;

    [Delphi(PortStatus.Ported)]
    private TBoldIndirectMultiLinkController GetMainListController() =>
        (TBoldIndirectMultiLinkController)GetControllerForMember(OwningList.OwningObject!.BoldMembers(OwningObjectList.BoldRoleRTInfo!.IndexOfMainRole))!;

    [Delphi(PortStatus.Ported)]
    public override bool IncludesLocator(TBoldObjectLocator? Locator) => LocatorList.IndexOf(Locator) != -1;

    [Delphi(PortStatus.Ported)]
    public override int IndexOfLocator(TBoldObjectLocator? Locator) => LocatorList.IndexOf(Locator);

    [Delphi(PortStatus.Ported)]
    public override void InsertLocator(int index, TBoldObjectLocator Locator) =>
        throw new EBold(SysUtils.Format(sInvalidForListOfLinkObjects, GetType().Name, "InsertLocator"));

    [Delphi(PortStatus.Ported)]
    public override void Move(int CurrentIndex, int NewIndex) =>
        throw new EBold(SysUtils.Format(sInvalidForListOfLinkObjects, GetType().Name, "Move"));

    [Delphi(PortStatus.Ported)]
    public override void RemoveByIndex(int index) =>
        throw new EBold(SysUtils.Format(sInvalidForListOfLinkObjects, GetType().Name, "RemoveByIndex"));

    [Delphi(PortStatus.Ported)]
    public override void SetLocator(int index, TBoldObjectLocator Locator) =>
        throw new EBold(SysUtils.Format(sInvalidForListOfLinkObjects, GetType().Name, "SetLocator"));

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => throw new EBold(SysUtils.Format(sLocatedAbstractError, GetType().Name, "GetStreamName"));

    [Delphi(PortStatus.Ported)]
    private TBoldObjectList GetMainList() => (TBoldObjectList)OwningList.OwningObject!.BoldMembers(OwningObjectList.BoldRoleRTInfo!.IndexOfMainRole);

    /// <summary>No value interface: the list is not persisted (answers nil for IBoldValue and IBoldStreamable).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        Obj = null;
        return IId == typeof(IBoldValue) || IId == typeof(IBoldStreamable);
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => throw new EBoldInternal("Can't access Link Object List directly");

    [Delphi(PortStatus.Ported)] private TBoldObjectLocatorList LocatorList => GetLocatorList();
}

/// <summary>The controller of an inner link role of a link object (to one end); its locator cannot be changed, only cleared.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLinkObjectSingleLinkController(TBoldObjectReference Owner) : TBoldDirectSingleLinkController(Owner)
{
    [Delphi(PortStatus.Ported)]
    private TBoldLinkObjectSingleLinkController OtherInnerLinkController() =>
        (TBoldLinkObjectSingleLinkController)GetControllerForMember(
            OwningReference.OwningObject!.BoldMembers(OwningReference.BoldRoleRTInfo!.RoleRTInfoOfOtherEnd!.OtherIndexInLinkClass))!;

    /// <summary>
    /// Also completes the outer link of the other end, which could not know this end when the other inner link was
    /// fetched first (both inner links are fetched in one operation).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void SetFromId(TBoldObjectId? Id, TBoldDomainElementProxyMode Mode)
    {
        base.SetFromId(Id, Mode);
        TBoldLinkObjectSingleLinkController OtherInnerLink = OtherInnerLinkController();
        if (OtherInnerLink.fLocator is not null)
        {
            TBoldAbstractController? OtherOuterLink = OtherInnerLink.GetOtherEndController(OtherInnerLink.fLocator, false);
            switch (OtherOuterLink)
            {
                case null:
                    break;
                case TBoldIndirectMultiLinkController OuterMulti:
                    int index = OuterMulti.fLinkLocatorList!.IndexOf(OwningReference.OwningObject!.BoldObjectLocator);
                    if (index >= 0 && OuterMulti.fReferredList![index] is null)
                    {
                        OuterMulti.fReferredList[index] = fLocator;
                    }

                    break;
                case TBoldIndirectSingleLinkController OuterSingle:
                    if (OuterSingle.fLinkObjectLocator == OwningReference.OwningObject!.BoldObjectLocator && OuterSingle.fOtherEndLocator is null)
                    {
                        OuterSingle.fOtherEndLocator = fLocator;
                    }

                    break;
                default:
                    throw new EBold(SysUtils.Format(sUnexpectedControllerType, GetType().Name, OtherOuterLink.GetType().Name));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void SetLocator(TBoldObjectLocator? NewLocator)
    {
        if (NewLocator is not null)
        {
            throw new EBold(SysUtils.Format(sCannotChangeLinkObjectSingleLink, GetType().Name));
        }

        base.SetLocator(NewLocator);
    }
}

/// <summary>The controller of the link object role of an indirect single link: reads the main role's link object; read only.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLinkObjectReferenceController(TBoldObjectReference Owner) : TBoldAbstractObjectReferenceController(Owner)
{
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocator() => ((TBoldIndirectSingleLinkController)ControllerForMainRole()).fLinkObjectLocator;

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => throw new EBold(SysUtils.Format(sLocatedAbstractError, GetType().Name, "GetStreamName"));

    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent()
    {
        ControllerForMainRole().OwningReference.EnsureContentsCurrent();
        OwningReference.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => throw new EBoldInternal("Can't access objectreference directly");

    /// <summary>No value interface: the role is not persisted (answers nil for IBoldValue and IBoldStreamable).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        Obj = null;
        return IId == typeof(IBoldValue) || IId == typeof(IBoldStreamable);
    }

    [Delphi(PortStatus.Ported)]
    public override void SetLocator(TBoldObjectLocator? NewLocator) => throw new EBold(SysUtils.Format(sCannotSetLinkObjectReference, GetType().Name));
}
