// Delphi: Source/ObjectSpace/BORepresentation/BoldOptimisticLockingSupport.pas
// Port map: CSharp/port/units/BoldOptimisticLockingSupport.md

using static Bold.BoldCoreConsts;

namespace Bold;

/// <summary>
/// Optimistic locking: puts into the update precondition the database values the saved objects must still have (by
/// the class's locking mode, timestamps and, with lock regions, the values of whole regions), and widens the enclosure
/// to the dirty objects of the regions touched.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOptimisticLockHandler(TBoldSystem System) : TBoldAbstractOptimisticLockHandler(System)
{
    [Delphi(PortStatus.Ported)]
    public override void AddOptimisticLocks(TBoldObjectList ObjectList, TBoldOptimisticLockingPrecondition PreCondition)
    {
        RetrieveOptimisticLockingvalues(ObjectList, PreCondition);
        if (System.RegionFactory is not null)
        {
            AddOptimisticRegionLocks(ObjectList, PreCondition);
        }
    }

    /// <summary>Locks the values of the regions of the changed members and other ends, expanded to their enclosure.</summary>
    [Delphi(PortStatus.Ported)]
    private void AddOptimisticRegionLocks(TBoldObjectList ObjectList, TBoldOptimisticLockingPrecondition PreCondition)
    {
        var Regions = new TBoldRegionLookup();
        var RegionExpander = new TBoldRegionExpander();
        GetRegionsForDirtyOtherEnds(ObjectList, Regions);
        GetRegionsForDirtyMembersInList(ObjectList, Regions);
        RegionExpander.ExpandRegionEnclosure(Regions);
        GetLockingValuesForRegions(Regions, PreCondition);
    }

    /// <summary>Copies the value into the target value space as current, creating the object contents when needed.</summary>
    [Delphi(PortStatus.Ported)]
    private static void CopyValue(IBoldValueSpace TargetVS, ref IBoldObjectContents? TargetObjectContents, TBoldObjectId TargetObjectId, int MemberIndex,
        IBoldValue Value, string StreamName)
    {
        TargetObjectContents ??= TargetVS.EnsuredObjectContentsByObjectId(TargetObjectId);
        TargetObjectContents.EnsureMember(new TBoldMemberID(MemberIndex), StreamName);
        IBoldValue NewValue = TargetObjectContents.ValueByIndex(MemberIndex)!;
        NewValue.AssignContent(Value);
        NewValue.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
    }

    /// <summary>The regions of the multi links at the old and new other ends of the changed single links.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetRegionsForDirtyOtherEnds(TBoldObjectList ObjectList, TBoldRegionLookup Regions)
    {
        for (int ObjIx = 0; ObjIx < ObjectList.Count; ObjIx++)
        {
            TBoldObject Obj = ObjectList[ObjIx]!;
            for (int MemberIx = 0; MemberIx < Obj.BoldMemberCount; MemberIx++)
            {
                if (Obj.BoldMemberAssigned(MemberIx) && Obj.BoldMembers(MemberIx) is TBoldObjectReference { BoldDirty: true } ObjRef &&
                    ObjRef.BoldMemberRTInfo!.IsSingleRole)
                {
                    TBoldObjectId? OldRemoteId = GetRemotedfromIdRefValue(ObjRef.OldValue);
                    if (OldRemoteId is not null)
                    {
                        GetRegionsForRemoteMember(OldRemoteId, Regions, ObjRef.BoldRoleRTInfo!);
                    }

                    if (ObjRef.Locator is not null)
                    {
                        GetRegionsForRemoteMember(ObjRef.Locator.BoldObjectID, Regions, ObjRef.BoldRoleRTInfo!);
                    }
                }
            }
        }
    }

    /// <summary>Puts the region's values into the precondition: object existence, timestamps, old or current member values.</summary>
    [Delphi(PortStatus.Ported)]
    private static void GetLockingValuesForRegion(TBoldRegion Region, TBoldOptimisticLockingPrecondition PreCondition)
    {
        var ElementList = new TList();
        Region.GetElements(ElementList);
        for (int i = 0; i < ElementList.Count; i++)
        {
            switch (ElementList[i])
            {
                case TBoldObject Obj:
                    if (!Obj.BoldObjectIsNew)
                    {
                        PreCondition.ValueSpace.EnsureObjectContents(Obj.BoldObjectLocator!.BoldObjectID);
                    }

                    break;
                case TBoldMember Member:
                    IBoldObjectContents? ObjectContents = null;
                    if (!Member.OwningObject!.BoldObjectIsNew && Member.BoldMemberRTInfo!.CanHaveOldValue &&
                        Member.BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid)
                    {
                        TBoldObjectId ObjectId = Member.OwningObject.BoldObjectLocator!.BoldObjectID;
                        string contentName = Member.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).ContentName;
                        if (Member.BoldMemberRTInfo.IsStoredInObject && Member.OwningObject.BoldClassTypeInfo.OptimisticLocking == TBoldOptimisticLockingMode.bolmTimeStamp)
                        {
                            PreCondition.ValueSpace.EnsuredObjectContentsByObjectId(ObjectId).TimeStamp =
                                Member.OwningObject.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepContents).TimeStamp;
                        }
                        else if (Member.OldValue is { } old)
                        {
                            CopyValue(PreCondition.ValueSpace, ref ObjectContents, ObjectId, Member.BoldMemberRTInfo.index, old, contentName);
                        }
                        else if (Member.BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent)
                        {
                            CopyValue(PreCondition.ValueSpace, ref ObjectContents, ObjectId, Member.BoldMemberRTInfo.index,
                                Member.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents), contentName);
                        }
                    }

                    break;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void GetLockingValuesForRegions(TBoldRegionLookup Regions, TBoldOptimisticLockingPrecondition PreCondition)
    {
        using TBoldIndexableListTraverser Traverser = Regions.CreateTraverser()!;
        while (!Traverser.EndOfList)
        {
            GetLockingValuesForRegion((TBoldRegion)Traverser.Item!, PreCondition);
            Traverser.Next();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void GetRegionsForDirtyMembersInList(TBoldObjectList ObjectList, TBoldRegionLookup Regions)
    {
        for (int ObjIx = 0; ObjIx < ObjectList.Count; ObjIx++)
        {
            GetRegionsForDirtyMembers(ObjectList[ObjIx]!, Regions);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void GetRegionsForElement(TBoldDomainElement Element, TBoldRegionLookup Regions)
    {
        var RegionList = new TList();
        System.RegionFactory!.GetRegionsForElement(Element, RegionList);
        for (int i = 0; i < RegionList.Count; i++)
        {
            var Region = (TBoldRegion)RegionList[i]!;
            if (Regions.FindByID(Region.AsString) is null)
            {
                Regions.Add(Region);
            }
        }
    }

    /// <summary>The other end's id in an IBoldObjectIdRef (or the second id of an IBoldObjectIdRefPair).</summary>
    [Delphi(PortStatus.Ported)]
    private static TBoldObjectId? GetRemotedfromIdRefValue(IBoldValue? Value) => Value switch
    {
        IBoldObjectIdRef IdRef => IdRef.Id,
        IBoldObjectIdRefPair IdRefPair => IdRefPair.Id2,
        _ => null,
    };

    /// <summary>
    /// Puts into the precondition, per the class's mode: old (or current) member values, multi links of deleted objects,
    /// the existence of linked objects and, for timestamp locking, the object's timestamp.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void RetrieveOptimisticLockingvalues(TBoldObjectList ObjectList, TBoldOptimisticLockingPrecondition PreCondition)
    {
        for (int ObjIx = 0; ObjIx < ObjectList.Count; ObjIx++)
        {
            TBoldObject BoldObject = ObjectList[ObjIx]!;
            TBoldOptimisticLockingMode Mode = BoldObject.BoldClassTypeInfo.OptimisticLocking;
            IBoldObjectContents? NewObjectContents = null;
            if (Mode is not (TBoldOptimisticLockingMode.bolmTimeStamp or TBoldOptimisticLockingMode.bolmModifiedMembers or TBoldOptimisticLockingMode.bolmAllMembers))
            {
                continue;
            }

            TBoldObjectId ObjectId = BoldObject.BoldObjectLocator!.BoldObjectID;
            IBoldObjectContents? ObjectContents = OldValues.ObjectContentsByObjectId(ObjectId);

            for (int MemberIx = 0; MemberIx < BoldObject.BoldMemberCount; MemberIx++)
            {
                TBoldMemberRTInfo MemberRTInfo = BoldObject.BoldClassTypeInfo.AllMembers[MemberIx];
                if (!MemberRTInfo.CanHaveOldValue)
                {
                    continue;
                }

                IBoldValue? value = null;
                if (ObjectContents is not null &&
                    (Mode is TBoldOptimisticLockingMode.bolmModifiedMembers or TBoldOptimisticLockingMode.bolmAllMembers || MemberRTInfo.EncouragesOptimisticLockingOnDeletedOnly))
                {
                    value = ObjectContents.ValueByIndex(MemberIx);
                }

                if (value is null && BoldObject.BoldMemberAssigned(MemberIx) &&
                    BoldObject.BoldMembers(MemberIx).BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent &&
                    ((Mode == TBoldOptimisticLockingMode.bolmModifiedMembers && BoldObject.BoldObjectIsDeleted) || Mode == TBoldOptimisticLockingMode.bolmAllMembers ||
                     MemberRTInfo.EncouragesOptimisticLockingOnDeletedOnly))
                {
                    value = BoldObject.BoldMembers(MemberIx).AsIBoldValue(TBoldDomainElementProxyMode.bdepContents);
                }

                if (value is not null && MemberRTInfo.EncouragesOptimisticLockingOnDeletedOnly && !BoldObject.BoldObjectIsDeleted)
                {
                    value = null;
                }

                if (value is not null && MemberRTInfo.IsSingleRole && ((TBoldRoleRTInfo)MemberRTInfo).RoleType == TBoldRoleType.rtInnerLinkRole)
                {
                    NewObjectContents = PreCondition.ValueSpace.EnsuredObjectContentsByObjectId(ObjectId);
                    value = null;
                }

                if (!BoldObject.BoldObjectIsNew && value is not null)
                {
                    CopyValue(PreCondition.ValueSpace, ref NewObjectContents, ObjectId, MemberIx, value,
                        BoldObject.BoldMembers(MemberIx).AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).ContentName);
                }

                if (MemberRTInfo.IsSingleRole && MemberRTInfo.IsStoredInObject)
                {
                    var RoleRTInfo = (TBoldRoleRTInfo)MemberRTInfo;
                    if (RoleRTInfo.RoleType == TBoldRoleType.rtRole && BoldObject.BoldMemberAssigned(MemberIx) && BoldObject.BoldMembers(MemberIx).BoldDirty &&
                        ((TBoldObjectReference)BoldObject.BoldMembers(MemberIx)).BoldObject is { BoldObjectIsNew: false } RelatedObject)
                    {
                        TBoldObjectId relatedId = RelatedObject.BoldObjectLocator!.BoldObjectID;
                        IBoldObjectContents? RelatedObjectcontents = PreCondition.ValueSpace.EnsuredObjectContentsByObjectId(relatedId);
                        if (RoleRTInfo.RoleRTInfoOfOtherEnd!.IsSingleRole && !RoleRTInfo.RoleRTInfoOfOtherEnd.IsStoredInObject &&
                            RelatedObject.BoldMembers(RoleRTInfo.IndexOfOtherEnd).OldValue is { } otherOld)
                        {
                            CopyValue(PreCondition.ValueSpace, ref RelatedObjectcontents, relatedId, RoleRTInfo.IndexOfOtherEnd, otherOld,
                                RelatedObject.BoldMembers(RoleRTInfo.IndexOfOtherEnd).AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).ContentName);
                        }
                    }
                }
            }

            if (!BoldObject.BoldObjectIsNew && Mode == TBoldOptimisticLockingMode.bolmTimeStamp)
            {
                NewObjectContents ??= PreCondition.ValueSpace.EnsuredObjectContentsByObjectId(ObjectId);
                NewObjectContents.TimeStamp = BoldObject.AsIBoldObjectContents(TBoldDomainElementProxyMode.bdepContents).TimeStamp;
            }
        }
    }

    /// <summary>The regions of the other end's role in the (loaded) object; raises when a forced other end is not loaded.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetRegionsForRemoteMember(TBoldObjectId? OwningId, TBoldRegionLookup Regions, TBoldRoleRTInfo OwningRoleRTInfo)
    {
        if (OwningId is null)
        {
            return;
        }

        TBoldObject? RemoteObj = System.EnsuredLocatorByID(OwningId).BoldObject;
        if (RemoteObj is not null)
        {
            TBoldMember OtherEnd = RemoteObj.BoldMembers(OwningRoleRTInfo.IndexOfOtherEnd);
            if (OtherEnd.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid)
            {
                if (OwningRoleRTInfo.ForceOtherEnd)
                {
                    throw new EBold(SysUtils.Format(sRelatedRoleNotLoaded, GetType().Name, OwningRoleRTInfo.AsString));
                }
            }
            else
            {
                GetRegionsForElement(OtherEnd, Regions);
            }
        }
        else if (OwningRoleRTInfo.ForceOtherEnd)
        {
            throw new EBold(SysUtils.Format(sRelatedObjectNotLoaded, GetType().Name, OwningRoleRTInfo.AsString));
        }
    }

    /// <summary>Adds the dirty objects of the regions around the object's changes to the enclosure.</summary>
    [Delphi(PortStatus.Ported)]
    public override void EnsureEnclosure(TBoldObject Obj, TBoldObjectList Enclosure, bool ValidateOnly, ref bool ListIsEnclosure)
    {
        if (RegionFactory is not null)
        {
            var Regions = new TBoldRegionLookup();
            var Expander = new TBoldRegionExpander();
            var EmptyLookup = new TBoldRegionLookup();
            GetRegionsForDirtyMembers(Obj, Regions);
            Expander.ExpandParentRegions(Regions, EmptyLookup, EmptyLookup);
            Expander.ExpandSubregions(Expander.NewParentRegions, EmptyLookup);
            AddRegionsObjectsToEnclosure(Expander.NewParentRegions, Enclosure, ValidateOnly, ref ListIsEnclosure);
            AddRegionsObjectsToEnclosure(Expander.NewSubRegions, Enclosure, ValidateOnly, ref ListIsEnclosure);
        }
    }

    /// <summary>The regions of the object, when it or one of its members is modified.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetRegionsForDirtyMembers(TBoldObject Obj, TBoldRegionLookup Regions)
    {
        if (Obj.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
        {
            GetRegionsForElement(Obj, Regions);
        }

        for (int MemberIx = 0; MemberIx < Obj.BoldMemberCount; MemberIx++)
        {
            if (Obj.BoldMemberAssigned(MemberIx) && Obj.BoldMembers(MemberIx).BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
            {
                GetRegionsForElement(Obj, Regions);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void AddRegionObjectsToEnclosure(TBoldRegion Region, TBoldObjectList Enclosure, bool ValidateOnly, ref bool ListIsEnclosure)
    {
        var Elements = new TList();
        Region.GetElements(Elements);
        for (int i = 0; i < Elements.Count; i++)
        {
            TBoldObject? Obj = Elements[i] switch
            {
                TBoldObject o => o,
                TBoldMember m => m.OwningObject,
                _ => null,
            };

            if (Obj is not null && Obj.BoldDirty && !Enclosure.Includes(Obj))
            {
                if (!ValidateOnly)
                {
                    Enclosure.Add(Obj);
                }

                ListIsEnclosure = false;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void AddRegionsObjectsToEnclosure(TBoldRegionLookup Regions, TBoldObjectList Enclosure, bool ValidateOnly, ref bool ListIsEnclosure)
    {
        using TBoldIndexableListTraverser Traverser = Regions.CreateTraverser()!;
        while (!Traverser.EndOfList)
        {
            AddRegionObjectsToEnclosure((TBoldRegion)Traverser.Item!, Enclosure, ValidateOnly, ref ListIsEnclosure);
            Traverser.Next();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldRegionFactory? GetRegionFactory() => (TBoldRegionFactory?)System.RegionFactory;

    [Delphi(PortStatus.Ported)] public TBoldRegionFactory? RegionFactory => GetRegionFactory();
}
