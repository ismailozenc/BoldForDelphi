// Delphi: Source/Persistence/ExternalPersistence/BoldAbstractExternalPersistenceController.pas
// Port map: CSharp/port/units/BoldAbstractExternalPersistenceController.md
// A pass-through controller that handles the classes and members stored externally (in another system) itself and
// passes the rest to the next controller: fetches split by storage, updates split into create, delete and update.

namespace Bold;

/// <summary>The base of the controllers of external storage.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractExternalPersistenceController : TBoldPersistenceControllerPassthrough
{
    private readonly TMoldModel fMoldModel;
    private readonly TBoldTypeNameDictionary fTypeNameDictionary;
    private readonly TNotifyEvent? FOnStartUpdates;
    private readonly TNotifyEvent? FOnEndUpdates;
    private readonly TNotifyEvent? fOnFailUpdates;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractExternalPersistenceController(TMoldModel MoldModel, TBoldTypeNameDictionary TypeNameDictionary, TNotifyEvent? OnStartUpdates,
        TNotifyEvent? OnEndUpdates, TNotifyEvent? OnFailUpdates)
    {
        fMoldModel = MoldModel;
        fMoldModel.EnsureTopSorted();
        fTypeNameDictionary = TypeNameDictionary;
        FOnStartUpdates = OnStartUpdates;
        fOnFailUpdates = OnFailUpdates;
        FOnEndUpdates = OnEndUpdates;
    }

    [Delphi(PortStatus.Ported)]
    public override void PMExactifyIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList) => base.PMExactifyIds(ObjectIdList, TranslationList);

    /// <summary>Fetches the internal part through the next controller and the external part here.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode, TBoldClientID BoldClientID)
    {
        var ObjectsToHandle = new TBoldObjectIdList();
        var ObjectsToPassAlong = new TBoldObjectIdList();
        TBoldMemberIdList? MembersToHandle = null;
        TBoldMemberIdList? MembersToPassAlong = null;
        SplitObjects(ObjectIdList, ValueSpace, ObjectsToPassAlong, ObjectsToHandle, out TMoldClass? SuperClass);
        if (MemberIdList is not null)
        {
            MembersToHandle = new TBoldMemberIdList();
            MembersToPassAlong = new TBoldMemberIdList();
            for (int i = 0; i < MemberIdList.Count; i++)
            {
                TMoldMember Member = SuperClass!.AllBoldMembers[MemberIdList[i].MemberIndex];
                if (Member.Storage == TBoldStorage.bsExternal)
                {
                    MembersToHandle.Add(new TBoldMemberID(MemberIdList[i].MemberIndex));
                }
                else
                {
                    MembersToPassAlong.Add(new TBoldMemberID(MemberIdList[i].MemberIndex));
                }
            }

            if (MembersToHandle.Count == 0)
            {
                ObjectsToHandle.Clear();
            }
        }

        // only call inherited fetch if there is anything to fetch from the internal database
        if (ObjectsToPassAlong.Count > 0 && (MembersToPassAlong is null || MembersToPassAlong.Count > 0))
        {
            base.PMFetch(ObjectsToPassAlong, ValueSpace, MembersToPassAlong, FetchMode, BoldClientID);
        }

        EnsureObjectsforFetch(ObjectsToHandle, ValueSpace, MembersToHandle);
        FetchObjects(ObjectsToHandle, ValueSpace, MembersToHandle);
    }

    /// <summary>All instances of an external class come from here; other conditions pass on.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetchIDListWithCondition(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldCondition Condition, TBoldClientID BoldClientID)
    {
        if (Condition.GetType() == typeof(TBoldConditionWithClass))
        {
            var ClassCondition = (TBoldConditionWithClass)Condition;
            TMoldClass MoldClass = MoldModel.Classes[ClassCondition.TopSortedIndex];
            if (MoldClass.Storage != TBoldStorage.bsInternal && HandlesClass(MoldClass))
            {
                HandleAllInstances(ObjectIdList, ValueSpace, FetchMode, ClassCondition, MoldClass);
                return;
            }
        }

        base.PMFetchIDListWithCondition(ObjectIdList, ValueSpace, FetchMode, Condition, BoldClientID);
    }

    /// <summary>Creates, deletes and updates the external objects, then passes the rest on, between the update events.</summary>
    [Delphi(PortStatus.Ported, Note = "A failure is raised after the fail event; Delphi swallowed it, so a failed update looked successful.")]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldUpdatePrecondition? Precondition,
        TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID)
    {
        var ObjectsToHandle = new TBoldObjectIdList();
        var ObjectsToPassAlong = new TBoldObjectIdList();
        var ObjectsToCreate = new TBoldObjectIdList();
        var ObjectsToDelete = new TBoldObjectIdList();
        var ObjectsToUpdate = new TBoldObjectIdList();
        SplitObjects(ObjectIdList, ValueSpace, ObjectsToPassAlong, ObjectsToHandle, out _);
        for (int i = 0; i < ObjectsToHandle.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectsToHandle[i])!;
            if (ObjectContents.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
            {
                if (ObjectContents.BoldExistenceState == TBoldExistenceState.besDeleted)
                {
                    ObjectsToDelete.Add(ObjectsToHandle[i]);
                }
                else
                {
                    ObjectsToCreate.Add(ObjectsToHandle[i]);
                }
            }
            else
            {
                ObjectsToUpdate.Add(ObjectsToHandle[i]);
            }
        }

        StartUpdates();
        try
        {
            CreateObjects(ObjectsToCreate, ValueSpace);
            DeleteObjects(ObjectsToDelete, ValueSpace);
            UpdateObjects(ObjectsToUpdate, ValueSpace);
            base.PMUpdate(ObjectsToPassAlong, ValueSpace, Old_Values, Precondition, TranslationList, ref TimeStamp, BoldClientID);
            EndUpdates();
        }
        catch
        {
            FailUpdates();
            throw;
        }
    }

    /// <summary>Makes the contents and the external (or the asked) members of the objects.</summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsureObjectsforFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.EnsuredObjectContentsByObjectId(ObjectIdList[i]);
            TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
            if (MemberIdList is not null)
            {
                for (int j = 0; j < MemberIdList.Count; j++)
                {
                    EnsureMember(ObjectContents, MoldClass.AllBoldMembers[MemberIdList[j].MemberIndex], MemberIdList[j].MemberIndex);
                }
            }
            else
            {
                for (int j = 0; j < MoldClass.AllBoldMembers.Count; j++)
                {
                    if (MoldClass.AllBoldMembers[j].Storage is TBoldStorage.bsExternal or TBoldStorage.bsPartiallyExternal)
                    {
                        EnsureMember(ObjectContents, MoldClass.AllBoldMembers[j], j);
                    }
                }
            }
        }
    }

    /// <summary>The value of the class's (first) external key member.</summary>
    [Delphi(PortStatus.Ported)]
    public IBoldValue? KeyForObject(IBoldObjectContents ObjectContents)
    {
        TMoldClass MoldClass = MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex];
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                return ObjectContents.ValueByIndex(i);
            }
        }

        return null;
    }

    /// <summary>The value of the member with the expression name.</summary>
    [Delphi(PortStatus.Ported)]
    public IBoldValue? ValueForObject(IBoldObjectContents ObjectContents, string MemberExpressionName)
    {
        TMoldClass MoldClass = MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex];
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (SysUtils.SameText(MoldClass.AllBoldMembers[i].ExpandedExpressionName, MemberExpressionName))
            {
                return ObjectContents.ValueByIndex(i);
            }
        }

        return null;
    }

    /// <summary>Makes the member's value, of the content its role or attribute type has.</summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsureMember(IBoldObjectContents ObjectContents, TMoldMember MoldMember, int MemberIndex)
    {
        string Contentname = "";
        if (MoldMember is TMoldRole MoldRole)
        {
            if (MoldRole.RoleType == TBoldRoleType.rtInnerLinkRole)
            {
                Contentname = BoldDefaultStreamNames.BoldContentName_ObjectIdRef;
            }
            else if (MoldRole.Multi)
            {
                Contentname = MoldRole.Association!.LinkClass is not null ? BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair : BoldDefaultStreamNames.BoldContentName_ObjectIdListRef;
            }
            else
            {
                Contentname = MoldRole.Association!.LinkClass is not null ? BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair : BoldDefaultStreamNames.BoldContentName_ObjectIdRef;
            }
        }
        else if (MoldMember is TMoldAttribute MoldAttribute)
        {
            Contentname = TypeNameDictionary.MappingForModelName(MoldAttribute.BoldType)!.ExpandedContentsName;
        }

        ObjectContents.EnsureMember(new TBoldMemberID(MemberIndex), Contentname);
    }

    /// <summary>
    /// Sorts the objects: internal classes and classes not handled here pass along, external ones are handled here,
    /// partially external ones both; also gives the lowest common superclass.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void SplitObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectsToPassAlong, TBoldObjectIdList ObjectsToHandle,
        out TMoldClass? CommonClass)
    {
        CommonClass = null;
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            TBoldObjectId Id = ObjectIdList[i];
            TMoldClass MoldClass = MoldModel.Classes[Id.TopSortedIndex];
            CommonClass = CommonClass is not null ? CommonClass.LowestCommonSuperClass(MoldClass) : MoldClass;
            if (MoldClass.Storage == TBoldStorage.bsInternal || !HandlesClass(MoldClass))
            {
                ObjectsToPassAlong.Add(Id);
            }
            else if (MoldClass.Storage == TBoldStorage.bsExternal)
            {
                ObjectsToHandle.Add(Id);
            }
            else if (MoldClass.Storage == TBoldStorage.bsPartiallyExternal)
            {
                ObjectsToPassAlong.Add(Id);
                ObjectsToHandle.Add(Id);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void EndUpdates() => FOnEndUpdates?.Invoke(this);

    [Delphi(PortStatus.Ported)]
    protected virtual void StartUpdates() => FOnStartUpdates?.Invoke(this);

    /// <summary>Fetches the objects class by class, in blocks of at most MaxFetchBlockSize.</summary>
    [Delphi(PortStatus.Ported)]
    protected void FetchObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList)
    {
        var ActionList = new TBoldObjectIdList();
        var TranslationList = new TBoldIDTranslationList();
        PMExactifyIds(ObjectIdList, TranslationList);
        TBoldObjectIdList IdList = ObjectIdList.Clone();
        IdList.ApplyTranslationList(TranslationList);

        void AddToActionList(int index)
        {
            ActionList.Add(IdList[index]);
            IdList.RemoveByIndex(index);
        }

        while (IdList.Count > 0)
        {
            AddToActionList(0);
            TMoldClass MoldClass = MoldModel.Classes[ActionList[0].TopSortedIndex];
            for (int i = IdList.Count - 1; i >= 0; i--)
            {
                if (IdList[i].TopSortedIndex == ActionList[0].TopSortedIndex)
                {
                    AddToActionList(i);
                    if (ActionList.Count > MaxFetchBlockSize)
                    {
                        break;
                    }
                }
            }

            object? FetchContext = null;
            PrepareFetch(ActionList, ValueSpace, MoldClass, MemberIdList, ref FetchContext);
            for (int i = 0; i < ActionList.Count; i++)
            {
                FetchObject(ValueSpace.ObjectContentsByObjectId(ActionList[i])!, MemberIdList, FetchContext, ValueSpace);
            }

            PostFetch(FetchContext, MoldClass);
            ActionList.Clear();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void PostFetch(object? FetchContext, TMoldClass MoldClass)
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void PrepareFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TMoldClass MoldClass, TBoldMemberIdList? MemberIdList, ref object? FetchContext)
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    protected virtual int GetMaxFetchBlockSize() => 250;

    [Delphi(PortStatus.Ported)]
    protected virtual void FailUpdates() => fOnFailUpdates?.Invoke(this);

    [Delphi(PortStatus.Ported)] protected abstract bool HandlesClass(TMoldClass MoldClass);
    [Delphi(PortStatus.Ported)] protected abstract void FetchObject(IBoldObjectContents ObjectContent, TBoldMemberIdList? MemberIdList, object? FetchContext, IBoldValueSpace ValueSpace);
    [Delphi(PortStatus.Ported)] protected abstract void CreateObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace);
    [Delphi(PortStatus.Ported)] protected abstract void DeleteObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace);
    [Delphi(PortStatus.Ported)] protected abstract void UpdateObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace);

    [Delphi(PortStatus.Ported)]
    protected abstract void HandleAllInstances(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldConditionWithClass Condition, TMoldClass MoldClass);

    [Delphi(PortStatus.Ported)] protected TMoldModel MoldModel => fMoldModel;
    [Delphi(PortStatus.Ported)] protected TBoldTypeNameDictionary TypeNameDictionary => fTypeNameDictionary;
    [Delphi(PortStatus.Ported)] protected int MaxFetchBlockSize => GetMaxFetchBlockSize();
}
