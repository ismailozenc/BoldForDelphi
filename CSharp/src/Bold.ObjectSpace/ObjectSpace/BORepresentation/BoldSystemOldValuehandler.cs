// Delphi: Source/ObjectSpace/BORepresentation/BoldSystemOldValuehandler.pas
// Port map: CSharp/port/units/BoldSystemOldValuehandler.md

namespace Bold;

/// <summary>
/// Keeps, in a free-standing value space, the database values of members changed since they were fetched: optimistic
/// locking compares them with the database at save.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOldValueHandler : TBoldAbstractOldValueHandler
{
    private TBoldFreeStandingValueSpace? fOldValues;

    [Delphi(PortStatus.Ported)]
    public TBoldOldValueHandler(TBoldSystem System)
        : base(System)
    {
        fOldValues = new TBoldFreeStandingValueSpace();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOldValueHandler.Destroy")]
    public override void Dispose()
    {
        fOldValues?.Dispose();
        fOldValues = null;
        base.Dispose();
    }

    /// <summary>True when no object in the old values has a member value.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool GetIsEmpty()
    {
        bool result = true;
        var ObjectIdList = new TBoldObjectIdList();
        fOldValues!.AllObjectIds(ObjectIdList, false);
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            TBoldFreeStandingObjectContents? ObjectContents = fOldValues.GetFSObjectContentsByObjectId(ObjectIdList[i]);
            if (ObjectContents is not null)
            {
                for (int m = 0; m <= ObjectContents.MemberCount; m++)
                {
                    result = result && ObjectContents.FSValueByIndex(m) is null;
                }
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override IBoldValueSpace GetOldValues() => fOldValues!;

    /// <summary>A saved or invalidated member drops its old value (multi links may have one while current).</summary>
    [Delphi(PortStatus.Ported)]
    public override void MemberPersistenceStatePreChange(TBoldMember BoldMember, TBoldValuePersistenceState NewState)
    {
        if (BoldMember.BoldPersistenceState is TBoldValuePersistenceState.bvpsModified or TBoldValuePersistenceState.bvpsCurrent &&
            NewState is TBoldValuePersistenceState.bvpsCurrent or TBoldValuePersistenceState.bvpsInvalid)
        {
            fOldValues!.GetFSObjectContentsByObjectId(BoldMember.OwningObject!.BoldObjectLocator!.BoldObjectID)?.RemoveMemberByIndex(BoldMember.BoldMemberRTInfo!.index);
        }
    }

    /// <summary>Saves the database value of a current member of a saved object before its first change.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MemberValuePreChange(TBoldMember BoldMember)
    {
        if (!BoldMember.OwningObject!.BoldObjectIsNew && BoldMember.BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent &&
            BoldMember.BoldMemberRTInfo!.CanHaveOldValue)
        {
            CopyMemberToValueSpace(BoldMember, OldValues);
        }
    }

    [Delphi(PortStatus.Ported, Note = "Empty in Delphi too.")]
    public override void ObjectExistenceChange(TBoldObject BoldObject)
    {
    }

    [Delphi(PortStatus.Ported, Note = "Empty in Delphi too.")]
    public override void ObjectExistencePersistenceStateChange(TBoldObject BoldObject, TBoldValuePersistenceState NewState)
    {
    }

    /// <summary>After a save: drops old values of objects not loaded, of members not loaded or invalid, and of links back at their old value.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void PurgeEqualValues()
    {
        bool IdListPairEqual(TBFSObjectIdListrefPair IdList, TBoldObjectList ObjectList)
        {
            bool result = IdList.Count == ObjectList.Count;
            for (int i = 0; result && i < IdList.Count; i++)
            {
                result = ObjectList.LocatorInList(System.Locators.LocatorByID(((IBoldObjectIdListRefPair)IdList).IdList2(i)));
            }

            return result;
        }

        bool IdListEqual(IBoldObjectIdListRef IdList, TBoldObjectList ObjectList)
        {
            bool result = IdList.Count == ObjectList.Count;
            for (int i = 0; result && i < IdList.Count; i++)
            {
                result = ObjectList.LocatorInList(System.Locators.LocatorByID(IdList.IdList(i)));
            }

            return result;
        }

        static bool IdPairEqual(TBFSObjectIDRefPair IdRef, TBoldObjectReference ObjectRef)
        {
            TBoldObjectId? id2 = IdRef.GetId2();
            return id2 is not null ? ObjectRef.Locator is not null && id2.IsEqual(ObjectRef.Locator.BoldObjectID) : ObjectRef.Locator is null;
        }

        var ObjectIdList = new TBoldObjectIdList();
        fOldValues!.AllObjectIds(ObjectIdList, false);
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            TBoldObjectId Id = ObjectIdList[i];
            TBoldObjectLocator? Locator = System.Locators.LocatorByID(Id);
            if (Locator?.BoldObject is null)
            {
                fOldValues.RemoveFSObjectContentsByObjectId(Id);
                continue;
            }

            TBoldFreeStandingObjectContents? ObjectContents = fOldValues.GetFSObjectContentsByObjectId(Id);
            if (ObjectContents is null)
            {
                continue;
            }

            for (int m = 0; m <= ObjectContents.MemberCount; m++)
            {
                TBoldFreeStandingValue? value = ObjectContents.FSValueByIndex(m);
                if (value is null)
                {
                    continue;
                }

                if (!Locator.BoldObject.BoldMemberAssigned(m))
                {
                    ObjectContents.RemoveMemberByIndex(m);
                    continue;
                }

                TBoldMember Member = Locator.BoldObject.BoldMembers(m);
                if (Member.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid ||
                    (value is TBFSObjectIdListrefPair pair && IdListPairEqual(pair, (TBoldObjectList)Member)) ||
                    (value is TBFSObjectIdListref list && IdListEqual(list, (TBoldObjectList)Member)) ||
                    (value is TBFSObjectIDRefPair refPair && IdPairEqual(refPair, (TBoldObjectReference)Member)))
                {
                    ObjectContents.RemoveMemberByIndex(m);
                }
            }
        }
    }
}
