// Delphi: Source/Persistence/System/BoldPersistenceControllerSystem.pas
// Port map: CSharp/port/units/BoldPersistenceControllerSystem.md

using static Bold.BoldSubscription;

namespace Bold;

/// <summary>
/// A persistence controller whose storage is another object space: fetches read the other system's objects, updates
/// write into them. Each locator of the other system gets a default id of its own.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceControllerSystem : TBoldPersistenceController
{
    private TBoldSystem? fBoldSystem;
    private TBoldPassthroughSubscriber? fLocatorSubscriber;
    private TBoldIdLocatorMapping? fMapping;

    [Delphi(PortStatus.Ported)]
    public TBoldPersistenceControllerSystem()
    {
        fLocatorSubscriber = new TBoldPassthroughSubscriber(_LocatorDestroyedReceived);
        fMapping = new TBoldIdLocatorMapping();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPersistenceControllerSystem.Destroy")]
    public override void Dispose()
    {
        fLocatorSubscriber?.Dispose();
        fLocatorSubscriber = null;
        fMapping?.Dispose();
        fMapping = null;
        base.Dispose();
    }

    private TBoldIdLocatorMapping Mapping => fMapping!;

    /// <summary>Forgets the id of a locator the other system destroys.</summary>
    [Delphi(PortStatus.Ported)]
    private void _LocatorDestroyedReceived(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args)
    {
        System.Diagnostics.Debug.Assert(Args.Length == 1, "Error in Args");
        Mapping.RemoveByLocator(((TBoldObject)Args[0]!).BoldObjectLocator!);
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldSystem(TBoldSystem? System)
    {
        if (fBoldSystem != System)
        {
            fBoldSystem = System;
            fLocatorSubscriber!.CancelAllSubscriptions();
            fBoldSystem?.AddSmallSubscription(fLocatorSubscriber, TBoldSmallEventSet.Of(beLocatorDestroying), beLocatorDestroying);
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldObjectLocator? GetLocatorById(TBoldObjectId ObjectId) => Mapping.LocatorById(ObjectId);
    [Delphi(PortStatus.Ported)] private TBoldObjectId? GetIdByLocator(TBoldObjectLocator Locator) => Mapping.IdByLocator(Locator);

    /// <summary>Has the other system's controller exactify the ids that are not exact, and gives this side's new ids.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMExactifyIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
        var anIdList = new TBoldObjectIdList();
        var aTranslationList = new TBoldIDTranslationList();
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            if (!ObjectIdList[i].TopSortedIndexExact)
            {
                anIdList.Add(Mapping.LocatorById(ObjectIdList[i])!.BoldObjectID);
            }
        }

        if (anIdList.Count > 0)
        {
            BoldSystem!.PersistenceController!.PMExactifyIds(anIdList, aTranslationList);
            BoldSystem.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepContents).ApplytranslationList(aTranslationList);
            for (int i = 0; i < anIdList.Count; i++)
            {
                TBoldObjectLocator aLocator = BoldSystem.Locators.LocatorByID(anIdList[i])!;
                TBoldObjectId OldId = Mapping.IdByLocator(aLocator)!.Clone();
                Mapping.ExactifyClassForLocator(aLocator, aTranslationList.TranslateToNewId(anIdList[i])!.TopSortedIndex);
                TranslationList.AddTranslation(OldId, Mapping.IdByLocator(aLocator));
            }
        }
    }

    /// <summary>Copies the members of the other system's objects into the value space; missing objects come back deleted.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldClientID BoldClientID)
    {
        TBoldObjectIdList ExtractNewIdList(TBoldObjectList ObjectList)
        {
            var result = new TBoldObjectIdList();
            for (int i = 0; i < ObjectList.Count; i++)
            {
                result.Add(Mapping.EnsuredIdByLocator(ObjectList.Locators(i)));
            }

            return result;
        }

        void FetchMember(IBoldObjectContents ObjectContents, int MemberIndex, TBoldMember BoldMember)
        {
            ObjectContents.EnsureMember(new TBoldMemberID(MemberIndex), BoldMember.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).ContentName);
            IBoldValue aValue = ObjectContents.ValueByIndex(MemberIndex)!;
            if (aValue.BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid && BoldMember.BoldMemberRTInfo!.Persistent)
            {
                BoldMember.EnsureContentsCurrent();
                if (BoldMember.BoldMemberRTInfo is not TBoldRoleRTInfo aRoleRT)
                {
                    aValue.AssignContent(BoldMember.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents));
                }
                else if (aRoleRT.IsMultiRole)
                {
                    if (aRoleRT.IsIndirect)
                    {
                        ((IBoldObjectIdListRefPair)aValue).SetFromIdLists(
                            ExtractNewIdList((TBoldObjectList)BoldMember.OwningObject!.BoldMembers(aRoleRT.IndexOfLinkObjectRole)),
                            ExtractNewIdList((TBoldObjectList)BoldMember));
                    }
                    else
                    {
                        ((IBoldObjectIdListRef)aValue).SetFromIdList(ExtractNewIdList((TBoldObjectList)BoldMember));
                    }
                }
                else if (aRoleRT.IsIndirect)
                {
                    ((IBoldObjectIdRefPair)aValue).SetFromIds(
                        Mapping.EnsuredIdByLocator(((TBoldObjectReference)BoldMember.OwningObject!.BoldMembers(aRoleRT.IndexOfLinkObjectRole)).Locator),
                        Mapping.EnsuredIdByLocator(((TBoldObjectReference)BoldMember).Locator));
                }
                else
                {
                    ((IBoldObjectIdRef)aValue).SetFromId(Mapping.EnsuredIdByLocator(((TBoldObjectReference)BoldMember).Locator));
                }
            }
        }

        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents anObjectContent = ValueSpace.EnsuredObjectContentsByObjectId(ObjectIdList[i]);
            TBoldObjectLocator? aLocator = Mapping.LocatorById(ObjectIdList[i]);
            if (aLocator is null)
            {
                anObjectContent.BoldExistenceState = TBoldExistenceState.besDeleted;
            }
            else
            {
                TBoldObject anObject = aLocator.EnsuredBoldObject!;
                anObjectContent.BoldExistenceState = anObject.BoldExistenceState;
                if (MemberIdList is not null)
                {
                    for (int j = 0; j < MemberIdList.Count; j++)
                    {
                        int aMemberIndex = MemberIdList[j].MemberIndex;
                        FetchMember(anObjectContent, aMemberIndex, anObject.BoldMembers(aMemberIndex));
                    }
                }
                else
                {
                    for (int j = 0; j < anObject.BoldMemberCount; j++)
                    {
                        TBoldMember aMember = anObject.BoldMembers(j);
                        if (!aMember.BoldMemberRTInfo!.DelayedFetch)
                        {
                            FetchMember(anObjectContent, j, aMember);
                        }
                    }
                }
            }
        }
    }

    /// <summary>The ids of the objects of the class; other conditions give nothing.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition,
        TBoldClientID BoldClientID)
    {
        if (Condition.GetType() == typeof(TBoldConditionWithClass))
        {
            TBoldObjectList anObjectList = BoldSystem!.Classes(((TBoldConditionWithClass)Condition).TopSortedIndex)!;
            for (int i = 0; i < anObjectList.Count; i++)
            {
                ObjectIdList.Add(Mapping.EnsuredIdByLocator(anObjectList.Locators(i)));
            }
        }
    }

    /// <summary>Creates new objects in the other system, deletes deleted ones and copies the changed stored members.</summary>
    [Delphi(PortStatus.Ported, Note = "Preconditions are not supported, as in Delphi.")]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values,
        TBoldUpdatePrecondition? Precondition, TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID)
    {
        TranslationList ??= new TBoldIDTranslationList();
        TBoldIDTranslationList translations = TranslationList;

        void CopyValue(IBoldValue Value, TBoldMember Member)
        {
            if (Member is TBoldObjectReference reference)
            {
                if (Value is not IBoldObjectIdRef anIdRef)
                {
                    throw new EBoldInternal(SysUtils.Format("%s.PMUpdate: value does not support IBoldObjectIdRef.", GetType().Name));
                }

                TBoldObjectId? anId = anIdRef.GetId();
                reference.Locator = anId is not null ? Mapping.LocatorById(translations.TranslateToNewId(anId)!) : null;
            }
            else
            {
                Member.AsIBoldValue(TBoldDomainElementProxyMode.bdRemove).AssignContent(Value);
            }
        }

        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            TBoldObjectId anId = ObjectIdList[i];
            TBoldObject? anObject;
            if (!anId.IsStorable)
            {
                string aClassName = BoldSystem!.BoldSystemTypeInfo.TopSortedClasses[anId.TopSortedIndex].ExpressionName;
                anObject = BoldSystem.CreateNewObjectByExpressionName(aClassName);
                TBoldObjectId? NewId = Mapping.EnsuredIdByLocator(anObject.BoldObjectLocator);
                TranslationList.AddTranslation(anId, NewId);
            }
            else
            {
                anObject = Mapping.LocatorById(anId)?.BoldObject;
            }

            if (anObject is not null)
            {
                IBoldObjectContents anObjectContents = ValueSpace.EnsuredObjectContentsByObjectId(anId);
                if (anObjectContents.BoldExistenceState == TBoldExistenceState.besDeleted)
                {
                    anObject.Delete();
                }
                else
                {
                    for (int j = 0; j < anObject.BoldMemberCount; j++)
                    {
                        TBoldMember aMember = anObject.BoldMembers(j);
                        IBoldValue? aValue = anObjectContents.ValueByIndex(j);
                        if (aMember.BoldMemberRTInfo!.Persistent && aMember.BoldMemberRTInfo.IsStoredInObject && aValue is not null &&
                            (aValue.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified || BoldSystem!.BoldSystemTypeInfo.UpdateWholeObjects))
                        {
                            CopyValue(aValue, aMember);
                        }
                    }
                }
            }
        }

        ValueSpace.ApplytranslationList(TranslationList);
    }

    [Delphi(PortStatus.Ported, Note = "Calls the abstract inherited method in Delphi (an abstract error); raises EAbstractError here.")]
    public override void PMTranslateToGlobalIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sLocatedAbstractError, GetType().Name, "PMTranslateToGlobalIds"));

    [Delphi(PortStatus.Ported, Note = "Calls the abstract inherited method in Delphi (an abstract error).")]
    public override void PMTranslateToLocalIds(TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList) =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sLocatedAbstractError, GetType().Name, "PMTranslateToLocalIds"));

    [Delphi(PortStatus.Ported, Note = "Calls the abstract inherited method in Delphi (an abstract error).")]
    public override void PMSetReadOnlyness(TBoldObjectIdList ReadOnlyList, TBoldObjectIdList WriteableList) =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sLocatedAbstractError, GetType().Name, "PMSetReadOnlyness"));

    /// <summary>Does nothing: new objects get ids when the update creates them.</summary>
    [Delphi(PortStatus.Ported)]
    public override void ReserveNewIds(IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSystem? BoldSystem
    {
        get => fBoldSystem;
        set => SetBoldSystem(value);
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a method.")]
    public TBoldObjectLocator? LocatorById(TBoldObjectId ObjectId) => GetLocatorById(ObjectId);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a method.")]
    public TBoldObjectId? IdByLocator(TBoldObjectLocator Locator) => GetIdByLocator(Locator);
}

/// <summary>An id of this side and the locator of the other system it stands for; owns a copy of the id.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldIdLocatorPair
{
    internal readonly TBoldObjectLocator fLocator;
    internal TBoldObjectId fId;

    [Delphi(PortStatus.Ported)]
    public TBoldIdLocatorPair(TBoldObjectId Id, TBoldObjectLocator Locator)
    {
        fLocator = Locator;
        fId = Id.Clone();
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldIdLocatorPair.Destroy", Note = "Freed the id; the garbage collector does that in .NET.")]
    internal TBoldObjectId Id => fId;
}

/// <summary>Indexes pairs by id.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldMappingIdIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)] protected override uint HashItem(object? Item) => ((TBoldIdLocatorPair)Item!).fId.Hash;
    [Delphi(PortStatus.Ported)] protected override bool Match(object Key, object? Item) => ((TBoldObjectId)Key).IsEqual(((TBoldIdLocatorPair)Item!).fId);
    [Delphi(PortStatus.Ported)] protected override uint Hash(object Key) => ((TBoldObjectId)Key).Hash;
}

/// <summary>Indexes pairs by locator (identity).</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldMappingLocatorIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)] protected override uint HashItem(object? Item) => ((TBoldIdLocatorPair)Item!).fLocator.Hash();
    [Delphi(PortStatus.Ported)] protected override bool Match(object Key, object? Item) => ReferenceEquals(Key, ((TBoldIdLocatorPair)Item!).fLocator);
    [Delphi(PortStatus.Ported)] protected override uint Hash(object Key) => ((TBoldObjectLocator)Key).Hash();
}

/// <summary>The id-locator pairs, indexed both ways; new ids are default ids numbered from 1.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldIdLocatorMapping : TBoldIndexableList
{
    private static int IX_MappingIdIndex = -1;
    private static int IX_MappingLocatorIndex = -1;
    private int fNextId;

    [Delphi(PortStatus.Ported)]
    public TBoldIdLocatorMapping()
    {
        BoldIndexableList.SetIndexVariable(ref IX_MappingIdIndex, AddIndex(new TBoldMappingIdIndex()));
        BoldIndexableList.SetIndexVariable(ref IX_MappingLocatorIndex, AddIndex(new TBoldMappingLocatorIndex()));
        fNextId = 1;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator? GetLocatorById(TBoldObjectId ID) => ((TBoldIdLocatorPair?)Indexes(IX_MappingIdIndex)!.Find(ID))?.fLocator;

    [Delphi(PortStatus.Ported)]
    private TBoldObjectId? GetIdByLocator(TBoldObjectLocator Locator) => GetPairByLocator(Locator)?.fId;

    [Delphi(PortStatus.Ported)]
    private TBoldIdLocatorPair? GetPairByLocator(TBoldObjectLocator Locator) => (TBoldIdLocatorPair?)Indexes(IX_MappingLocatorIndex)!.Find(Locator);

    [Delphi(PortStatus.Ported)]
    public void AddPair(TBoldObjectId Id, TBoldObjectLocator Locator) => Add(new TBoldIdLocatorPair(Id, Locator));

    /// <summary>Replaces the locator's id by one with the exact class.</summary>
    [Delphi(PortStatus.Ported, Note = "Re-indexes the pair around the change; Delphi swaps the id in place.")]
    public void ExactifyClassForLocator(TBoldObjectLocator Locator, int ExactClassId)
    {
        TBoldIdLocatorPair aPair = GetPairByLocator(Locator)!;
        RemoveFromAllIndexes(aPair);
        aPair.fId = aPair.fId.CloneWithClassId(ExactClassId, true);
        AddToAllIndexes(aPair);
    }

    /// <summary>The locator's id, giving it the next default id when it has none; null for no locator.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObjectId? EnsuredIdByLocator(TBoldObjectLocator? Locator)
    {
        if (Locator is null)
        {
            return null;
        }

        TBoldObjectId? result = IdByLocator(Locator);
        if (result is null)
        {
            var NewId = new TBoldDefaultID(Locator.BoldObjectID.TopSortedIndex, Locator.BoldObjectID.TopSortedIndexExact) { AsInteger = fNextId };
            fNextId++;
            AddPair(NewId, Locator);
            result = IdByLocator(Locator);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveByLocator(TBoldObjectLocator Locator)
    {
        TBoldIdLocatorPair? aPair = GetPairByLocator(Locator);
        if (aPair is not null)
        {
            Remove(aPair);
        }
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a method.")]
    public TBoldObjectLocator? LocatorById(TBoldObjectId ID) => GetLocatorById(ID);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; here a method.")]
    public TBoldObjectId? IdByLocator(TBoldObjectLocator Locator) => GetIdByLocator(Locator);
}
