// Delphi: Source/Persistence/ExternalPersistence/BoldAbstractPartiallyExternalPC.pas
// Port map: CSharp/port/units/BoldAbstractPartiallyExternalPC.md
// The controller of classes stored partly in the internal database and partly externally: each object has an
// internal row holding its external key; objects found in the external storage get internal rows, and objects gone
// from it are listed as deleted.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAbstractPartiallyExternalPC", Symbol = "TBoldAbstractPartiallyExternalPC.Destroy", Note = "Freed the id lists; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>The base of the controllers of classes stored partly externally, matched to internal objects by external key.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractPartiallyExternalPC : TBoldAbstractExternalPersistenceController
{
    private readonly TBoldObjectIdList fDeletedExternalObjects = new();
    private readonly TBoldObjectIdList fNewExternalObjects = new();

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractPartiallyExternalPC(TMoldModel MoldModel, TBoldTypeNameDictionary TypeNameDictionary, TNotifyEvent? OnStartUpdates, TNotifyEvent? OnEndUpdates,
        TNotifyEvent? OnFailUpdates)
        : base(MoldModel, TypeNameDictionary, OnStartUpdates, OnEndUpdates, OnFailUpdates)
    {
    }

    /// <summary>Saves a new internal object with the external key, and adds its id to Ids and to the new external objects.</summary>
    [Delphi(PortStatus.Ported)]
    protected void CreateInternalObject(TMoldClass MoldClass, TBoldObjectId ExternalKey, IBoldValueSpace ValueSpace, TBoldObjectIdList Ids)
    {
        var TranslationList = new TBoldIDTranslationList();
        var UpdateIdList = new TBoldObjectIdList();
        int TopSortedIndex = MoldClass.TopSortedIndex;
        // FIXME: how can we be sure that the object is actually of the type MoldClass?
        var NewId = new TBoldInternalObjectId(TopSortedIndex, MoldClass.SubClasses.Count == 0);
        IBoldObjectContents NewObject = ValueSpace.GetEnsuredObjectContentsByObjectId(NewId);
        for (int j = 0; j < MoldClass.AllBoldMembers.Count; j++)
        {
            if (MoldClass.AllBoldMembers[j].Storage is TBoldStorage.bsInternal or TBoldStorage.bsExternalKey)
            {
                EnsureMember(NewObject, MoldClass.AllBoldMembers[j], j);
                NewObject.ValueByIndex(j)!.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
            }
        }

        NewObject.BoldPersistenceState = TBoldValuePersistenceState.bvpsModified;
        NewObject.BoldExistenceState = TBoldExistenceState.besExisting;
        // insert the external key to the new object so that it can be saved to the internal database
        AssignKeyToObject(MoldClass, NewObject, ExternalKey, ValueSpace);
        UpdateIdList.Add(NewId);
        TBoldTimeStampType TimeStamp = 0;
        NextPersistenceController.PMUpdate(UpdateIdList, ValueSpace, null, null, TranslationList, ref TimeStamp, -1);
        // return the translated ID!
        TBoldObjectId Id = TranslationList.TranslateToNewId(NewId)!.Clone();
        Ids.Add(Id);
        // indicate that this is a newly created object if anyone wants to initialize any internal-attributes.
        NewExternalObjects.Add(Id);
    }

    /// <summary>A where fragment selecting the internal rows of the external keys (one key member only).</summary>
    [Delphi(PortStatus.Ported, Note = "Quotes string keys with single quotes (doubling embedded ones); Delphi used double quotes, which SQL reads as identifiers.")]
    protected virtual string ExternalKeysToInternalSQL(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys)
    {
        TMoldAttribute? ExternalKey = null;
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i] is TMoldAttribute { Storage: TBoldStorage.bsExternalKey } Attribute)
            {
                if (ExternalKey is not null)
                {
                    throw new EBold(SysUtils.Format(ExPeConsts.sNotSupportedWithMultipleKeys, GetType().Name, MoldClass.ExpandedExpressionName));
                }

                ExternalKey = Attribute;
            }
        }

        if (ExternalKey is null)
        {
            throw new EBold(SysUtils.Format(ExPeConsts.sNotSupportedWithNoKeys, GetType().Name, MoldClass.ExpandedExpressionName));
        }

        var result = new System.Text.StringBuilder(SysUtils.Format("%s.%s in (",
            BoldNameExpander.BoldExpandName(ExternalKey.MoldClass!.TableName, ExternalKey.MoldClass.Name, TExpansionType.xtSQL, -1, TBoldNationalCharConversion.nccFalse),
            BoldNameExpander.BoldExpandName(ExternalKey.ColumnName, ExternalKey.Name, TExpansionType.xtSQL, -1, TBoldNationalCharConversion.nccFalse)));
        TBoldTypeNameMapping? Mapping = TypeNameDictionary.MappingForModelName(ExternalKey.BoldType);
        bool KeyIsString = Mapping is not null && SysUtils.SameText(Mapping.ExpandedContentsName, BoldDefaultStreamNames.BoldContentName_String);
        for (int i = 0; i < ExternalKeys.Count; i++)
        {
            if (i != 0)
            {
                result.Append(", ");
            }

            result.Append(KeyIsString ? "'" + ExternalKeys[i].AsString.Replace("'", "''", StringComparison.Ordinal) + "'" : ExternalKeys[i].AsString);
        }

        return result.Append(')').ToString();
    }

    /// <summary>Fetches the external key members of the internal objects.</summary>
    [Delphi(PortStatus.Ported)]
    protected void FetchExternalKeysForIDs(TBoldObjectIdList InternalObjectIdList, IBoldValueSpace ValueSpace, TMoldClass MoldClass)
    {
        var MemberIdList = new TBoldMemberIdList();
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                MemberIdList.Add(new TBoldMemberID(i));
            }
        }

        NextPersistenceController.PMFetch(InternalObjectIdList, ValueSpace, MemberIdList, BoldDefs.fmNormal, -1);
    }

    /// <summary>The role with the expression name in the object's class, and its member index.</summary>
    [Delphi(PortStatus.Ported)]
    protected void FindMoldRoleByName(IBoldObjectContents ObjectContents, string ExpressionName, out TMoldRole? MoldRole, out int Index)
    {
        MoldRole = null;
        Index = -1;
        TMoldClass MoldClass = MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex];
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i] is TMoldRole Role && SysUtils.SameText(Role.ExpandedExpressionName, ExpressionName))
            {
                MoldRole = Role;
                Index = i;
            }
        }

        if (Index == -1)
        {
            throw new EBold(SysUtils.Format(ExPeConsts.sNoSuchRole, GetType().Name, ExpressionName, MoldClass.ExpandedExpressionName));
        }
    }

    /// <summary>All instances: the internal objects matched to the external keys, with internal objects made for new keys.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void HandleAllInstances(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldConditionWithClass Condition, TMoldClass MoldClass)
    {
        using var TempValueSpace = new TBoldFreeStandingValueSpace();
        var InternalObjectIdList = new TBoldObjectIdList();
        var ExternalKeys = new TBoldObjectIdList();
        // send the condition query to the internal database and fetch the objects to a local valuespace
        NextPersistenceController.PMFetchIDListWithCondition(InternalObjectIdList, TempValueSpace, BoldDefs.fmNormal, Condition, -1);
        // Get the external keys
        GetExternalKeys(MoldClass, ExternalKeys);
        // match the external keys and the internal objects.
        MatchObjectsByKeys(MoldClass, TempValueSpace, InternalObjectIdList, ExternalKeys, ObjectIdList);
    }

    /// <summary>
    /// Adds to FoundObjects the internal object of each external key, made if missing; internal objects whose key is
    /// gone are listed as deleted.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected void MatchObjectsByKeys(TMoldClass MoldClass, IBoldValueSpace ValueSpace, TBoldObjectIdList InternalIds, TBoldObjectIdList ExternalKeys, TBoldObjectIdList FoundObjects)
    {
        // The Internal ids and External keys should be expected to contain references to the same objects.
        FetchExternalKeysForIDs(InternalIds, ValueSpace, MoldClass);
        var TranslationList = new TBoldIDTranslationList();
        for (int i = 0; i < InternalIds.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(InternalIds[i])!;
            TBoldObjectId ExternalKey = GetExternalKeyFromObject(ObjectContents, ValueSpace);
            TranslationList.AddTranslation(ExternalKey, InternalIds[i]);
            // Objects that have been deleted in external database are just logged...
            if (!ExternalKeys.IdInList(ExternalKey))
            {
                DeletedExternalObjects.Add(InternalIds[i]);
            }
        }

        for (int i = 0; i < ExternalKeys.Count; i++)
        {
            TBoldObjectId? InternalId = TranslationList.TranslateToNewId(ExternalKeys[i]);
            if (InternalId is not null && ExternalKeys[i] != InternalId)
            {
                // the objects that already exist in the internal database can be returned directly
                FoundObjects.Add(InternalId);
            }
            else
            {
                // the missing objects has to be created in the internal database
                CreateInternalObject(MoldClass, ExternalKeys[i], ValueSpace, FoundObjects);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool ExternalKeyExistsInExternalStorage(TMoldClass MoldClass, TBoldObjectId ExternalKey) => true;

    /// <summary>The external keys of the objects; for a member fetch, objects whose key is gone are listed as deleted.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void PrepareFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TMoldClass MoldClass, TBoldMemberIdList? MemberIdList, ref object? FetchContext)
    {
        var ExternalKeys = new TBoldObjectIdList();
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!;
            TBoldObjectId ExternalKey = GetExternalKeyFromObject(ObjectContents, ValueSpace);
            // only perform the existencetest if this is the default-fetch of an object
            // when fetching custommembers we assume this test has already been performed
            if (MemberIdList is null || ExternalKeyExistsInExternalStorage(MoldClass, ExternalKey))
            {
                ExternalKeys.Add(ExternalKey);
            }
            else
            {
                DeletedExternalObjects.Add(ObjectIdList[i]);
            }
        }

        PrepareFetchExternal(ExternalKeys, ValueSpace, MoldClass, MemberIdList, ref FetchContext);
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void PrepareFetchExternal(TBoldObjectIdList ExternalKeys, IBoldValueSpace ValueSpace, TMoldClass MoldClass, TBoldMemberIdList? MemberIdList,
        ref object? FetchContext)
    {
        // intentionally left blank
    }

    /// <summary>Sets a multi link to the internal objects of the external keys.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetMultiLink(IBoldObjectIdListRef MultiLink, TBoldObjectIdList ExternalKeys, TMoldClass MoldClassOfOtherEnd)
    {
        var InternalIds = new TBoldObjectIdList();
        TranslateExternalKeysToInternalIds(MoldClassOfOtherEnd, ExternalKeys, InternalIds);
        MultiLink.SetFromIdList(InternalIds);
    }

    /// <summary>Sets a single link to the internal object of the external key, or to nothing.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetSingleLink(IBoldObjectIdRef SingleLink, TBoldObjectId? ExternalKey, TMoldClass MoldClassOfOtherEnd)
    {
        // set the member to point to the ID
        if (ExternalKey is not null)
        {
            var ExternalKeys = new TBoldObjectIdList();
            ExternalKeys.Add(ExternalKey);
            var InternalIds = new TBoldObjectIdList();
            TranslateExternalKeysToInternalIds(MoldClassOfOtherEnd, ExternalKeys, InternalIds);
            if (InternalIds.Count == 1)
            {
                SingleLink.SetFromId(InternalIds[0]);
            }
        }
        else
        {
            SingleLink.SetFromId(null);
        }
    }

    /// <summary>The internal ids of the external keys (made if missing), looked up in blocks.</summary>
    [Delphi(PortStatus.Ported)]
    public void TranslateExternalKeysToInternalIds(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys, TBoldObjectIdList InternalIds)
    {
        using var ValueSpace = new TBoldFreeStandingValueSpace();
        var Condition = new TBoldSQLCondition { TopSortedIndex = MoldClass.TopSortedIndex };
        var ActionList = new TBoldObjectIdList();
        var TempInternalIds = new TBoldObjectIdList();

        void ProcessActionlist()
        {
            Condition.WhereFragment = ExternalKeysToInternalSQL(MoldClass, ActionList);
            NextPersistenceController.PMFetchIDListWithCondition(TempInternalIds, ValueSpace, BoldDefs.fmNormal, Condition, -1);
            ActionList.Clear();
        }

        for (int i = 0; i < ExternalKeys.Count; i++)
        {
            ActionList.Add(ExternalKeys[i]);
            if (ActionList.Count == GetMaxFetchBlockSize())
            {
                ProcessActionlist();
            }
        }

        if (ActionList.Count > 0)
        {
            ProcessActionlist();
        }

        MatchObjectsByKeys(MoldClass, ValueSpace, TempInternalIds, ExternalKeys, InternalIds);
    }

    [Delphi(PortStatus.Ported)]
    protected void TranslateInternalIdsToExternal(TBoldObjectIdList InternalIds, TBoldObjectIdList ExternalKeys, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < InternalIds.Count; i++)
        {
            ExternalKeys.Add(GetExternalKeyFromObject(ValueSpace.ObjectContentsByObjectId(InternalIds[i])!, ValueSpace));
        }
    }

    [Delphi(PortStatus.Ported)] protected abstract void GetExternalKeys(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys);
    [Delphi(PortStatus.Ported)] protected abstract void AssignKeyToObject(TMoldClass MoldClass, IBoldObjectContents ObjectContents, TBoldObjectId ExternalKey, IBoldValueSpace ValueSpace);
    [Delphi(PortStatus.Ported)] protected abstract TBoldObjectId GetExternalKeyFromObject(IBoldObjectContents ObjectContents, IBoldValueSpace ValueSpace);
    [Delphi(PortStatus.Ported)] public TBoldObjectIdList DeletedExternalObjects => fDeletedExternalObjects;
    [Delphi(PortStatus.Ported)] public TBoldObjectIdList NewExternalObjects => fNewExternalObjects;
}
