// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
// The proxies that give the system as IBoldValueSpace, objects as IBoldObjectContents and references as IBoldValue.

using static Bold.BoldCoreConsts;

namespace Bold;

/// <summary>Gives a system as IBoldValueSpace: the persistence layer reads and writes objects through it.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldSystem_Proxy(TBoldSystem aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldDomainElement_Proxy(aProxedElement, aMode), IBoldValueSpace
{
    [Delphi(PortStatus.Ported)]
    private TBoldSystem GetProxedSystem() => (TBoldSystem)ProxedElement;

    [Delphi(PortStatus.Ported)]
    public void AllObjectIds(TBoldObjectIdList resultList, bool OnlyLoaded)
    {
        using TBoldLocatorListTraverser Traverser = ProxedSystem.Locators.CreateTraverser();
        while (!Traverser.EndOfList)
        {
            if (!OnlyLoaded || Traverser.locator.BoldObject is not null)
            {
                resultList.Add(Traverser.locator.BoldObjectID);
            }

            Traverser.Next();
        }
    }

    /// <summary>Gives locators their new ids, creates locators for ids loaded, and passes the list to undo and the old values.</summary>
    [Delphi(PortStatus.Ported)]
    public void ApplytranslationList(TBoldIDTranslationList IdTranslationList)
    {
        for (int I = 0; I < IdTranslationList.Count; I++)
        {
            TBoldObjectId? oldId = IdTranslationList.OldIds(I);
            if (oldId is not null)
            {
                TBoldObjectLocator? Locator = ProxedSystem.Locators.LocatorByID(oldId);
                TBoldObjectId? newId = IdTranslationList.NewIds(I);
                if (newId is not null)
                {
                    ProxedSystem.Locators.UpdateID(Locator!, newId);
                }
            }
            else
            {
                ProxedSystem.EnsuredLocatorByID(IdTranslationList.NewIds(I));
            }
        }

        ProxedSystem.UndoHandler.ApplytranslationList(IdTranslationList);
        ProxedSystem.fOldValueHandler.OldValues.ApplytranslationList(IdTranslationList);
    }

    /// <summary>Copies the values of the loaded objects in the value space into the system's objects, creating the missing ones.</summary>
    [Delphi(PortStatus.Ported)]
    public void ApplyValueSpace(IBoldValueSpace ValueSpace, bool IgnorePersistenceState)
    {
        static void ApplyObjectContents(TBoldObject BoldObject, IBoldObjectContents ObjectContents)
        {
            BoldObject.SetBoldExistenceState(ObjectContents.BoldExistenceState);
            BoldObject.SetBoldPersistenceState(ObjectContents.BoldPersistenceState);
            for (int i = 0; i < BoldObject.BoldMemberCount; i++)
            {
                IBoldValue? aValue = ObjectContents.ValueByIndex(i);
                if (aValue is not null)
                {
                    BoldObject.BoldMembers(i).AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).AssignContent(aValue);
                }
            }
        }

        var anIdList = new TBoldObjectIdList();
        ValueSpace.AllObjectIds(anIdList, true);
        for (int i = 0; i < anIdList.Count; i++)
        {
            TBoldObjectId ObjectId = anIdList[i];
            TBoldObject BoldObject = ProxedSystem.Locators.ObjectByID(ObjectId) ?? ProxedSystem.CreateExistingObjectByID(ObjectId);
            ApplyObjectContents(BoldObject, ValueSpace.ObjectContentsByObjectId(ObjectId)!);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void EnsureObjectContents(TBoldObjectId ObjectId)
    {
        if (ProxedSystem.Locators.ObjectByID(ObjectId) is null)
        {
            ProxedSystem.CreateExistingObjectByID(ObjectId);
        }
    }

    /// <summary>Ensures a locator for the id, making its class exact when the id knows it exactly.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureObjectId(TBoldObjectId ObjectId)
    {
        TBoldObjectLocator ObjectLocator = ProxedSystem.EnsuredLocatorByID(ObjectId);
        if (!ObjectLocator.BoldObjectID.TopSortedIndexExact && ObjectId.TopSortedIndexExact)
        {
            ProxedSystem.Locators.UpdateID(ObjectLocator, ObjectId);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ExactifyIDs(TBoldIDTranslationList TranslationList)
    {
        for (int i = 0; i < TranslationList.Count; i++)
        {
            ProxedSystem.Locators.UpdateID(ProxedSystem.Locators.LocatorByID(TranslationList.OldIds(i))!, TranslationList.NewIds(i));
        }
    }

    [Delphi(PortStatus.Ported)]
    public IBoldObjectContents GetEnsuredObjectContentsByObjectId(TBoldObjectId ObjectId) =>
        GetObjectContentsByObjectId(ObjectId) ?? ProxedSystem.CreateExistingObjectByID(ObjectId).AsIBoldObjectContents(Mode);

    [Delphi(PortStatus.Ported)]
    public bool GetHasContentsForId(TBoldObjectId ObjectId) => ProxedSystem.Locators.ObjectByID(ObjectId) is not null;

    [Delphi(PortStatus.Ported)]
    public IBoldObjectContents? GetObjectContentsByObjectId(TBoldObjectId ObjectId) =>
        ProxedSystem.Locators.LocatorByID(ObjectId)?.BoldObject?.AsIBoldObjectContents(Mode);

    [Delphi(PortStatus.Ported)] protected TBoldSystem ProxedSystem => GetProxedSystem();
}

/// <summary>Gives an object as IBoldObjectContents.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldObject_Proxy(TBoldObject aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldDomainElement_Proxy(aProxedElement, aMode), IBoldObjectContents
{
    [Delphi(PortStatus.Ported)]
    private TBoldObject GetProxedObject() => (TBoldObject)ProxedElement;

    /// <summary>Checks that the member exists with that stream name; objects have all their members.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureMember(TBoldMemberID MemberId, string ContentName)
    {
        if (MemberId.MemberIndex >= ProxedObject.BoldMemberCount)
        {
            throw new EBold(SysUtils.Format(sNotEnoughMembers, GetType().Name, MemberId.MemberIndex));
        }

        TBoldMember Member = ProxedObject.BoldMembers(MemberId.MemberIndex);
        if (SysUtils.CompareText(Member.GetStreamName(), ContentName) != 0)
        {
            throw new EBold(SysUtils.Format(sUnexpectedStreamType, GetType().Name, Member.DisplayName, ContentName, Member.GetStreamName()));
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldExistenceState GetBoldExistenceState() => ProxedObject.BoldExistenceState;
    [Delphi(PortStatus.Ported)] public int GetBoldMemberCount() => ProxedObject.BoldMemberCount;
    [Delphi(PortStatus.Ported)] public TBoldValuePersistenceState GetBoldPersistenceState() => ProxedObject.BoldPersistenceState;
    [Delphi(PortStatus.Ported)] public string GetGlobalId() => ProxedObject.GetGlobalId();
    [Delphi(PortStatus.Ported)] public bool GetIsModified() => ProxedObject.GetIsModified();
    [Delphi(PortStatus.Ported)] public bool GetIsReadOnly() => ProxedObject.IsReadOnly;
    [Delphi(PortStatus.Ported)] public TBoldObjectId GetObjectId() => ProxedObject.BoldObjectLocator!.BoldObjectID;
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType GetTimeStamp() => ProxedObject.GetTimeStamp();

    /// <summary>
    /// The member's value; for PMOut (saving) only valid, non-derived attributes and ordinary or inner link roles. Members
    /// of old objects that are not created are left out.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public IBoldValue? GetValueByIndex(int I)
    {
        TBoldMember? Member = ProxedObject.BoldObjectIsNew || ProxedObject.BoldMemberAssigned(I) ? ProxedObject.BoldMembers(I) : null;

        bool IncludeInPmOut() =>
            Member!.BoldPersistenceState != TBoldValuePersistenceState.bvpsInvalid &&
            !Member.Derived &&
            (Member.BoldMemberRTInfo is TBoldAttributeRTInfo ||
             ((TBoldRoleRTInfo)Member.BoldMemberRTInfo!).RoleType is TBoldRoleType.rtRole or TBoldRoleType.rtInnerLinkRole);

        if (Member is not null && (Mode != TBoldDomainElementProxyMode.bdepPMOut || IncludeInPmOut()))
        {
            return Member.AsIBoldValue(Mode);
        }

        return null;
    }

    [Delphi(PortStatus.Ported)] public IBoldValue? GetValueByMemberId(TBoldMemberID MemberId) => GetValueByIndex(MemberId.MemberIndex);
    [Delphi(PortStatus.Ported)] public void SetBoldExistenceState(TBoldExistenceState Value) => ProxedObject.SetBoldExistenceState(Value);
    [Delphi(PortStatus.Ported)] public void SetBoldPersistenceState(TBoldValuePersistenceState Value) => ProxedObject.SetBoldPersistenceState(Value);
    [Delphi(PortStatus.Ported)] public void SetGlobalId(string NewValue) => ProxedObject.SetGlobalId(NewValue);
    [Delphi(PortStatus.Ported)] public void SetIsReadOnly(bool NewValue) => ProxedObject.SetIsReadOnly(NewValue);
    [Delphi(PortStatus.Ported)] public void SetTimeStamp(TBoldTimeStampType NewValue) => ProxedObject.SetTimeStamp(NewValue);

    [Delphi(PortStatus.Ported)] protected TBoldObject ProxedObject => GetProxedObject();
}

/// <summary>Gives a free-standing or derived reference as IBoldValue (contents only).</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldObjectReference_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldMember_Proxy(aProxedElement, aMode)
{
    [Delphi(PortStatus.Ported)]
    protected override void AssignContentValue(IBoldValue Source)
    {
        if (Mode == TBoldDomainElementProxyMode.bdepContents)
        {
            ((TBoldObjectReferenceController)ProxedController!).AssignContentValue(Source);
        }
        else
        {
            UnsupportedMode(Mode, "AssignContentValue");
        }
    }
}
