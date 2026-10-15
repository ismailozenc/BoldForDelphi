// Delphi: Source/ObjectSpace/BORepresentation/BoldLinks.pas
// Port map: CSharp/port/units/BoldLinks.md
// The single link controllers.

using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>
/// The controller of a single link that holds the other end's locator itself (and an order number for ordered other
/// ends); changing it changes the other end too.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldDirectSingleLinkController(TBoldObjectReference Owner) : TBoldAbstractObjectReferenceController(Owner)
{
    internal int FOrderno;
    internal TBoldObjectLocator? fLocator;

    /// <summary>Links the owner into the other end (only a loaded, current other end when adjusting after a fetch).</summary>
    [Delphi(PortStatus.Ported)]
    internal void AddToOtherEnd(TBoldLinkUnlinkMode Mode)
    {
        TBoldAbstractController? OtherEndController = GetOtherEndController(fLocator, Mode != TBoldLinkUnlinkMode.blulMarkAdjusted);
        if (OtherEndController is not null &&
            (Mode != TBoldLinkUnlinkMode.blulMarkAdjusted || OtherEndController.OwningMember.BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent))
        {
            OtherEndController.LinkTo(OwningReference.OwningObject!.BoldObjectLocator, Mode != TBoldLinkUnlinkMode.blulMarkAdjusted, Mode);
        }
    }

    /// <summary>Sets the order number, making the member modified (without sending an event).</summary>
    [Delphi(PortStatus.Ported)]
    public void SetAndModifyOrderNo(int index)
    {
        if (index != FOrderno)
        {
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningReference, "SetAndModifyOrderNo", "");
            }

            PreChange();
            FOrderno = index;
            EndModify();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocator() => fLocator;

    /// <summary>
    /// The controller of the other end's role in the object of the locator, when that member is loaded and valid; a
    /// forced other end is fetched first when allowed.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public TBoldAbstractController? GetOtherEndController(TBoldObjectLocator? aLocator, bool AllowForceOtherEnd)
    {
        TBoldRoleRTInfo role = OwningReference.BoldRoleRTInfo!;
        if (aLocator is null || role.IndexOfOtherEnd == -1)
        {
            return null;
        }

        if (AllowForceOtherEnd && role.ForceOtherEnd)
        {
            aLocator.EnsureBoldObject();
            aLocator.BoldObject!.BoldMembers(role.IndexOfOtherEnd).EnsureContentsCurrent();
        }

        if (aLocator.BoldObject is null)
        {
            return null;
        }

        TBoldMember aMember = aLocator.BoldObject.BoldMembers(role.IndexOfOtherEnd);
        if (aMember.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid)
        {
            return null;
        }

        return GetControllerForMember(aMember);
    }

    /// <summary>The other end linked to the owner: unlinks the old other end and points here (keeping old values when invalid).</summary>
    [Delphi(PortStatus.Ported)]
    public override void LinkTo(TBoldObjectLocator? NewLocator, bool updateOrderNo, TBoldLinkUnlinkMode Mode)
    {
        BoldClearLastFailure();
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified && !StartModify())
        {
            BoldRaiseLastFailure(OwningReference, "linkto", "");
        }

        if (fLocator != NewLocator)
        {
            RemoveFromOtherEnd(Mode);
            InternalSetLocator(NewLocator);
            if (OwningMember.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid)
            {
                OwningReference.HasOldValues = true;
            }
        }

        if (Mode == TBoldLinkUnlinkMode.blulMarkModified)
        {
            EndModify();
        }
    }

    /// <summary>Unlinks the owner from the current other end, when the old locator matters (valid, or kept as old value).</summary>
    [Delphi(PortStatus.Ported)]
    internal void RemoveFromOtherEnd(TBoldLinkUnlinkMode Mode)
    {
        bool OldLocatorRelevant = OwningMember.BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid || OwningReference.HasOldValues;
        if (OldLocatorRelevant)
        {
            GetOtherEndController(fLocator, Mode != TBoldLinkUnlinkMode.blulMarkAdjusted)?.Unlink(OwningReference.OwningObject!.BoldObjectLocator, Mode);
        }
    }

    /// <summary>
    /// Sets the link from a fetched (or restored) id. A fetch of the non-embedded end of a one-to-one keeps a link
    /// changed in memory at the other end; fetches and undo also update the other ends.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetFromId(TBoldObjectId? Id, TBoldDomainElementProxyMode Mode)
    {
        TBoldObjectLocator? NewLocator;

        void SafeCopyOptimisticValues()
        {
            if (NewValueInOptimisticLocking() is IBoldObjectIdRef Value)
            {
                Value.SetFromId(Id);
            }
        }

        void AdjustNewLocator()
        {
            TBoldSystem system = OwningMember.BoldSystem!;
            for (int i = 0; i < system.DirtyObjects.Count; i++)
            {
                var Obj = (TBoldObject)system.DirtyObjects[i]!;
                if (Obj.BoldClassTypeInfo.ConformsTo(OwningReference.BoldRoleRTInfo!.ClassTypeInfoOfOtherEnd) &&
                    GetOtherEndController(Obj.BoldObjectLocator, false) is TBoldDirectSingleLinkController other &&
                    other.fLocator == OwningReference.OwningObject!.BoldObjectLocator)
                {
                    SafeCopyOptimisticValues();
                    NewLocator = Obj.BoldObjectLocator;
                    break;
                }
            }
        }

        if (Mode == TBoldDomainElementProxyMode.bdepPMIn && OwningMember.OwningObject!.BoldObjectLocator!.BoldObjectID.TimeStamp != BoldDefs.BOLDMAXTIMESTAMP)
        {
            Mode = TBoldDomainElementProxyMode.bdepContents;
        }

        NewLocator = LocatorForID(Id);

        if (Mode == TBoldDomainElementProxyMode.bdepPMIn && !OwningReference.BoldRoleRTInfo!.IsStoredInObject)
        {
            AdjustNewLocator();
        }

        TBoldLinkUnlinkMode LinkUnlinkMode = Mode == TBoldDomainElementProxyMode.bdepPMIn ? TBoldLinkUnlinkMode.blulMarkAdjusted : TBoldLinkUnlinkMode.blulNone;
        if ((OwningMember.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid && !OwningReference.HasOldValues) || fLocator != NewLocator)
        {
            if (Mode != TBoldDomainElementProxyMode.bdepInternalInitialize)
            {
                PreChange();
            }

            bool updateOtherEnds = Mode is TBoldDomainElementProxyMode.bdepPMIn or TBoldDomainElementProxyMode.bdepUnDo;
            if (updateOtherEnds)
            {
                RemoveFromOtherEnd(LinkUnlinkMode);
            }

            fLocator = NewLocator;
            if (updateOtherEnds)
            {
                AddToOtherEnd(LinkUnlinkMode);
            }

            Changed(beValueChanged, [NewLocator]);
        }
    }

    /// <summary>Points to another object in a transaction: unlinks the owner from the old other end and links it into the new one.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetLocator(TBoldObjectLocator? NewLocator)
    {
        TBoldSystem system = OwningMember.BoldSystem!;
        system.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningReference, "SetLocator", "");
            }

            GetOtherEndController(NewLocator, true);
            RemoveFromOtherEnd(TBoldLinkUnlinkMode.blulMarkModified);
            InternalSetLocator(NewLocator);
            AddToOtherEnd(TBoldLinkUnlinkMode.blulMarkModified);
            if (fLocator is null)
            {
                SetAndModifyOrderNo(-1);
            }

            EndModify();
            system.CommitTransaction();
        }
        catch
        {
            system.RollbackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void InternalSetLocator(TBoldObjectLocator? NewLocator)
    {
        if (fLocator != NewLocator)
        {
            PreChange();
            fLocator = NewLocator;
            Changed(beValueChanged, [NewLocator]);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdRef;

    /// <summary>The other end unlinked from the owner: clears the link and its order number.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Unlink(TBoldObjectLocator? OldLocator, TBoldLinkUnlinkMode Mode)
    {
        BoldClearLastFailure();
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified && !StartModify())
        {
            BoldRaiseLastFailure(OwningReference, "Unlink", "");
        }

        InternalSetLocator(null);
        OwningReference.HasOldValues = false;
        SetAndModifyOrderNo(-1);
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified)
        {
            EndModify();
        }
    }

    /// <summary>Before a discard: unlinks the owner from the other end and invalidates the old other end (when not stored in it).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void PreDiscard()
    {
        void InvalidateNonembeddedOtherEnd(TBoldObjectLocator? Locator)
        {
            if (Locator is not null && GetOtherEndController(Locator, false) is { } OtherEndController &&
                !OtherEndController.OwningMember.BoldMemberRTInfo!.IsStoredInObject &&
                !OtherEndController.OwningMember.OwningObject!.BoldObjectIsNew &&
                !OtherEndController.OwningMember.BoldDirty)
            {
                OtherEndController.OwningMember.Invalidate();
            }
        }

        RemoveFromOtherEnd(TBoldLinkUnlinkMode.blulNone);

        if (OwningReference.OldValue is IBoldObjectIdRef { Id: { } oldId })
        {
            InvalidateNonembeddedOtherEnd(OwningReference.BoldSystem!.EnsuredLocatorByID(oldId));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBoldDirectSingleLinkController_Proxy);

    /// <summary>Gives the link as IBoldObjectIdRef.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldObjectIdRef))
        {
            Obj = Activator.CreateInstance(ProxyClass(), OwningReference, Mode);
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    /// <summary>A link stored in the object may only be saved when the object it points to is persistent.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override bool MayUpdate() =>
        !OwningReference.BoldRoleRTInfo!.IsStoredInObject || fLocator is null || fLocator.ObjectIsPersistent;

    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent() => DbFetchOwningMember();

    /// <summary>Sets the order number from the database; an ordered other end may then need resorting.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void SetOrderNo(int NewOrderNo, TBoldDomainElementProxyMode Mode)
    {
        if (NewOrderNo != FOrderno)
        {
            FOrderno = NewOrderNo;
            if (GetOtherEndController(fLocator, false) is TBoldMultiLinkController multi)
            {
                multi.MarkPossiblyOutOfOrder();
            }
        }
    }

    [Delphi(PortStatus.Ported)] public int OrderNo => FOrderno;
}

/// <summary>The controller of a single link through a link object (association class): holds the link object and the other end.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIndirectSingleLinkController(TBoldObjectReference Owner) : TBoldAbstractObjectReferenceController(Owner)
{
    internal TBoldObjectLocator? fLinkObjectLocator;
    internal TBoldObjectLocator? fOtherEndLocator;

    /// <summary>Unlinks and deletes the current link object.</summary>
    [Delphi(PortStatus.Ported)]
    protected void DeleteLink(TBoldLinkUnlinkMode Mode)
    {
        if (fLinkObjectLocator is not null)
        {
            TBoldObject OldLinkObject = fLinkObjectLocator.EnsuredBoldObject!;
            TBoldLinkObjectSingleLinkController other = GetLinkObjectOtherLinkController(OldLinkObject);
            other.RemoveFromOtherEnd(Mode);
            other.Unlink(other.fLocator, TBoldLinkUnlinkMode.blulMarkModified);
            TBoldLinkObjectSingleLinkController own = GetLinkObjectOwnLinkController(OldLinkObject);
            own.PreChange();
            own.fLocator = null;
            OldLinkObject.Delete();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldLinkObjectSingleLinkController GetLinkObjectOwnLinkController(TBoldObject LinkObject) =>
        (TBoldLinkObjectSingleLinkController)GetControllerForMember(LinkObject.BoldMembers(OwningReference.BoldRoleRTInfo!.OwnIndexInLinkClass))!;

    [Delphi(PortStatus.Ported)]
    private TBoldLinkObjectSingleLinkController GetLinkObjectOtherLinkController(TBoldObject LinkObject) =>
        (TBoldLinkObjectSingleLinkController)GetControllerForMember(LinkObject.BoldMembers(OwningReference.BoldRoleRTInfo!.OtherIndexInLinkClass))!;

    [Delphi(PortStatus.Ported)]
    private TBoldLinkObjectReferenceController GetLinkObjectRoleController() => (TBoldLinkObjectReferenceController)ControllerForLinkRole();

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectLocator? GetLocator() => fOtherEndLocator;

    /// <summary>A link object was linked to the owner: takes it (deleting the old one) and its other end.</summary>
    [Delphi(PortStatus.Ported)]
    public override void LinkTo(TBoldObjectLocator? NewLocator, bool updateOrderNo, TBoldLinkUnlinkMode Mode)
    {
        BoldClearLastFailure();
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified && !StartModify())
        {
            BoldRaiseLastFailure(OwningReference, "Linkto", "");
        }

        if (fLinkObjectLocator != NewLocator)
        {
            if (fLinkObjectLocator is not null)
            {
                DeleteLink(Mode);
            }

            PreChange();
            fLinkObjectLocator = NewLocator;
            fOtherEndLocator = GetLinkObjectOtherLinkController(NewLocator!.EnsuredBoldObject!).fLocator;
        }

        Changed(beValueChanged, [fOtherEndLocator]);
        GetLinkObjectRoleController().Changed(beValueChanged, [fLinkObjectLocator]);
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified)
        {
            EndModify();
        }
    }

    /// <summary>Creates a link object between the owner and the other object, and links it into the other end.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldObject NewLink(TBoldObjectLocator OtherLocator, TBoldLinkUnlinkMode Mode)
    {
        TBoldClassTypeInfo LinkClassTypeInfo = OwningReference.BoldRoleRTInfo!.LinkClassTypeInfo!;
        TBoldObject LinkObject = TBoldObject.InternalCreateNewWithClassAndSystem(LinkClassTypeInfo.ObjectClass!, LinkClassTypeInfo, OwningReference.BoldSystem!,
            OtherLocator.ObjectIsPersistent && OwningReference.OwningObject!.BoldPersistent);
        TBoldAbstractController? OtherEndController = GetLinkObjectOtherLinkController(LinkObject).GetOtherEndController(OtherLocator, true);
        GetLinkObjectOwnLinkController(LinkObject).LinkTo(OwningReference.OwningObject!.BoldObjectLocator, true, Mode);
        GetLinkObjectOtherLinkController(LinkObject).LinkTo(OtherLocator, true, Mode);
        OtherEndController?.LinkTo(LinkObject.BoldObjectLocator, false, Mode);
        return LinkObject;
    }

    /// <summary>Sets the link object and other end from fetched ids, keeping a link object changed or deleted in memory.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void SetFromIds(TBoldObjectId? Id1, TBoldObjectId? Id2, TBoldDomainElementProxyMode Mode)
    {
        TBoldObjectLocator? NewLinkLocator;
        TBoldObjectLocator? NewOtherEndLocator;

        void SafeCopyOptimisticValues()
        {
            if (NewValueInOptimisticLocking() is IBoldObjectIdRefPair Value)
            {
                Value.SetFromIds(Id1, Id2);
            }
        }

        void AdjustLocators()
        {
            TBoldRoleRTInfo role = OwningReference.BoldRoleRTInfo!;
            int IndexOfOwnEnd = role.OwnIndexInLinkClass;
            int IndexOfOtherEnd = role.OtherIndexInLinkClass;
            if (NewLinkLocator is not null)
            {
                TBoldObject? BoldObject = NewLinkLocator.BoldObject;
                if (BoldObject is not null)
                {
                    if (BoldObject.BoldExistenceState == TBoldExistenceState.besDeleted)
                    {
                        SafeCopyOptimisticValues();
                        NewLinkLocator = null;
                        NewOtherEndLocator = null;
                    }
                }
                else
                {
                    int EmbeddedIndexOfOwnEnd = role.LinkClassTypeInfo!.AllMembers[IndexOfOwnEnd].EmbeddedLinkIndex;
                    int EmbeddedIndexOfOtherEnd = role.LinkClassTypeInfo.AllMembers[IndexOfOtherEnd].EmbeddedLinkIndex;
                    if (EmbeddedIndexOfOwnEnd != -1)
                    {
                        NewLinkLocator.SetEmbeddedSingleLinks(EmbeddedIndexOfOwnEnd, OwningMember.OwningObject!.BoldObjectLocator);
                    }

                    if (EmbeddedIndexOfOtherEnd != -1)
                    {
                        NewLinkLocator.SetEmbeddedSingleLinks(EmbeddedIndexOfOtherEnd, NewOtherEndLocator);
                    }
                }
            }

            TBoldSystem system = OwningMember.BoldSystem!;
            for (int I = 0; I < system.DirtyObjects.Count; I++)
            {
                TBoldClassTypeInfo BoldLinkClassTypeInfo = role.LinkClassTypeInfo!;
                var BoldObject = (TBoldObject)system.DirtyObjects[I]!;
                if (BoldObject.BoldClassTypeInfo.BoldIsA(BoldLinkClassTypeInfo) && BoldObject.BoldExistenceState == TBoldExistenceState.besExisting &&
                    ((TBoldObjectReference)BoldObject.BoldMembers(IndexOfOwnEnd)).BoldObject == OwningMember.OwningObject)
                {
                    SafeCopyOptimisticValues();
                    NewLinkLocator = BoldObject.BoldObjectLocator;
                    NewOtherEndLocator = ((TBoldObjectReference)BoldObject.BoldMembers(IndexOfOtherEnd)).Locator;
                }
            }
        }

        if (Mode == TBoldDomainElementProxyMode.bdepPMIn && OwningMember.OwningObject!.BoldObjectLocator!.BoldObjectID.TimeStamp != BoldDefs.BOLDMAXTIMESTAMP)
        {
            Mode = TBoldDomainElementProxyMode.bdepContents;
        }

        NewLinkLocator = LocatorForID(Id1);
        NewOtherEndLocator = LocatorForID(Id2);
        if (Mode == TBoldDomainElementProxyMode.bdepPMIn)
        {
            AdjustLocators();
        }

        if (fLinkObjectLocator != NewLinkLocator)
        {
            PreChange();
            fLinkObjectLocator = NewLinkLocator;
            fOtherEndLocator = NewOtherEndLocator;
            Changed(beValueChanged, [NewOtherEndLocator]);
            GetLinkObjectRoleController().Changed(beValueChanged, [NewLinkLocator]);
        }
    }

    /// <summary>Points to another object in a transaction: deletes the old link object and creates a new one.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetLocator(TBoldObjectLocator? NewLocator)
    {
        if (NewLocator == fLinkObjectLocator)
        {
            return;
        }

        TBoldSystem system = OwningReference.BoldSystem!;
        system.StartTransaction();
        try
        {
            BoldClearLastFailure();
            if (!StartModify())
            {
                BoldRaiseLastFailure(OwningReference, "SetLocator", "");
            }

            DeleteLink(TBoldLinkUnlinkMode.blulMarkModified);
            PreChange();
            fOtherEndLocator = NewLocator;
            fLinkObjectLocator = NewLocator is not null ? NewLink(NewLocator, TBoldLinkUnlinkMode.blulMarkModified).BoldObjectLocator : null;
            Changed(beValueChanged, [NewLocator]);
            GetLinkObjectRoleController().Changed(beValueChanged, [fLinkObjectLocator]);
            EndModify();
            system.CommitTransaction();
        }
        catch
        {
            system.RollbackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair;

    /// <summary>The link object was unlinked: clears the link object and the other end.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Unlink(TBoldObjectLocator? OldLocator, TBoldLinkUnlinkMode Mode)
    {
        BoldClearLastFailure();
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified && !StartModify())
        {
            BoldRaiseLastFailure(OwningReference, "Unlink", "");
        }

        PreChange();
        fLinkObjectLocator = null;
        fOtherEndLocator = null;
        Changed(beValueChanged, [null]);
        GetLinkObjectRoleController().Changed(beValueChanged, [null]);
        if (Mode == TBoldLinkUnlinkMode.blulMarkModified)
        {
            EndModify();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBoldIndirectSingleLinkController_Proxy);

    /// <summary>Gives the link as IBoldObjectIdRefPair.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldObjectIdRefPair))
        {
            Obj = Activator.CreateInstance(ProxyClass(), OwningReference, Mode);
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    protected internal override bool MayUpdate() =>
        !OwningReference.BoldRoleRTInfo!.IsStoredInObject || fLinkObjectLocator is null || fLinkObjectLocator.ObjectIsPersistent;

    /// <summary>Checks that the link object and other end agree with the link object's own links.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's assertions raise EBoldInternal here, also in release builds.")]
    public override bool AssertIntegrity()
    {
        if (fLinkObjectLocator is null)
        {
            BoldLinks.CheckIntegrity(fOtherEndLocator is null);
        }
        else
        {
            BoldLinks.CheckIntegrity(fOtherEndLocator is not null);
            if (fLinkObjectLocator.BoldObject is { } linkObject)
            {
                BoldLinks.CheckIntegrity(linkObject.BoldExistenceState == TBoldExistenceState.besExisting);
                BoldLinks.CheckIntegrity(GetLinkObjectOwnLinkController(linkObject).fLocator == OwningMember.OwningObject!.BoldObjectLocator);
                BoldLinks.CheckIntegrity(GetLinkObjectOtherLinkController(linkObject).fLocator == fOtherEndLocator);
            }
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    public override void MakeDbCurrent() => DbFetchOwningMember();
}

public static partial class BoldLinks
{
    /// <summary>Delphi's Assert in the AssertIntegrity methods: raises EBoldInternal when the link structure is broken.</summary>
    internal static void CheckIntegrity(bool Condition, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(Condition))] string Expression = "")
    {
        if (!Condition)
        {
            throw new EBoldInternal("Link integrity check failed: " + Expression);
        }
    }
}
