// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceControllerEventDriven.pas
// Port map: CSharp/port/units/BoldExternalPersistenceControllerEventDriven.md
// The partially external controller whose external storage is reached through the events of its configuration.

namespace Bold;

/// <summary>A partially external controller driven by the configured events of each class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceControllerEventDriven : TBoldAbstractPartiallyExternalPC
{
    private readonly TBoldExternalPersistenceConfigItems FConfig;
    private readonly int fMaxFetchBlockSize;

    [Delphi(PortStatus.Ported)]
    public TBoldExternalPersistenceControllerEventDriven(TMoldModel MoldModel, TBoldExternalPersistenceConfigItems Config, TBoldTypeNameDictionary TypeNameDictionary,
        TNotifyEvent? OnStartUpdates, TNotifyEvent? OnEndUpdates, TNotifyEvent? OnFailUpdates, int MaxFetchBlockSize)
        : base(MoldModel, TypeNameDictionary, OnStartUpdates, OnEndUpdates, OnFailUpdates)
    {
        FConfig = Config;
        fMaxFetchBlockSize = MaxFetchBlockSize;
    }

    /// <summary>The class's OnAssignKeyToObject, or the key written to the one string or integer key member.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void AssignKeyToObject(TMoldClass MoldClass, IBoldObjectContents ObjectContents, TBoldObjectId ExternalKey, IBoldValueSpace ValueSpace)
    {
        // Make sure that the object contains the data of the external key
        TBoldExternalPersistenceConfigItem? ConfigItem = Config.FindExpressionName(MoldClass.ExpandedExpressionName);
        if (ConfigItem?.OnAssignKeyToObject is { } AssignKey)
        {
            AssignKey(PersistentObjectFromObjectContents(ObjectContents, ValueSpace, MoldClass), ExternalKey);
            return;
        }

        IBoldValue? KeyValue = null;
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                if (KeyValue is not null)
                {
                    throw new EBold(SysUtils.Format(ExPeConsts.sAssignKeyValueRequiresOneKey, MoldClass.Name));
                }

                KeyValue = ObjectContents.ValueByIndex(i);
                if (KeyValue is IBoldStringContent StrContent)
                {
                    StrContent.asString = ExternalKey.AsString;
                }
                else if (KeyValue is IBoldIntegerContent IntContent)
                {
                    IntContent.asInteger = SysUtils.StrToInt(ExternalKey.AsString);
                }
                else
                {
                    throw new EBold(SysUtils.Format(ExPeConsts.sKeyTypeNotAutoHandled, MoldClass.Name, MoldClass.AllBoldMembers[i].Name));
                }
            }
        }
    }

    /// <summary>Calls the class's OnCreateObject for each object; a class without one may not create objects.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void CreateObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!;
            TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
            if (ConfigItemByObjectContents(ObjectContents) is { } ConfigItem)
            {
                TBoldExternalPersistenceCreateEvent OnCreate = ConfigItem.OnCreateObject ?? throw new EBold(SysUtils.Format(ExPeConsts.sCreateObjectsNotAllowed, ConfigItem.ExpressionName));
                OnCreate(PersistentObjectFromObjectContents(ObjectContents, ValueSpace, MoldClass), GetExternalKeyFromObject(ObjectContents, ValueSpace), ValueSpace);
            }
        }
    }

    /// <summary>Calls the class's OnDeleteObject for each object; a class without one may not delete objects.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DeleteObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!;
            TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
            if (ConfigItemByObjectContents(ObjectContents) is { } ConfigItem)
            {
                TBoldExternalPersistenceDeleteEvent OnDelete = ConfigItem.OnDeleteObject ?? throw new EBold(SysUtils.Format(ExPeConsts.sDeleteObjectsNotAllowed, ConfigItem.ExpressionName));
                OnDelete(PersistentObjectFromObjectContents(ObjectContents, ValueSpace, MoldClass), GetExternalKeyFromObject(ObjectContents, ValueSpace));
            }
        }
    }

    /// <summary>Reads the asked members (OnReadMember) or the object (OnReadObject); a class that cannot be changed is read-only.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void FetchObject(IBoldObjectContents ObjectContents, TBoldMemberIdList? MemberIdList, object? FetchContext, IBoldValueSpace ValueSpace)
    {
        TBoldExternalPersistenceConfigItem ConfigItem = ConfigItemByObjectContents(ObjectContents)!;
        TMoldClass MoldClass = MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex];
        IPersistentBoldObject PersistentObject = PersistentObjectFromObjectContents(ObjectContents, ValueSpace, MoldClass);
        TBoldObjectId ExternalKey = GetExternalKeyFromObject(ObjectContents, ValueSpace);
        if (MemberIdList is not null && ConfigItem.OnReadMember is { } ReadMember)
        {
            for (int i = 0; i < MemberIdList.Count; i++)
            {
                ReadMember(PersistentObject, ExternalKey, MoldClass.AllBoldMembers[MemberIdList[i].MemberIndex], FetchContext);
            }
        }
        else if (ConfigItem.OnReadObject is { } ReadObject)
        {
            ReadObject(PersistentObject, ExternalKey, FetchContext);
        }
        else
        {
            throw new EBold(SysUtils.Format(ExPeConsts.sReadObjectNotImplementedForClass, MoldClass.Name));
        }

        if (ConfigItem.OnUpdateObject is null && ConfigItem.OnDeleteObject is null)
        {
            ObjectContents.IsReadOnly = true;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void GetExternalKeys(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys)
    {
        if (Config.FindExpressionName(MoldClass.ExpandedExpressionName)?.OnGetKeyList is { } GetKeyList)
        {
            GetKeyList(ExternalKeys);
        }
        else
        {
            throw new EBold(SysUtils.Format(ExPeConsts.sGetExternalKeyNotImplementedForClass, MoldClass.Name));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override bool HandlesClass(TMoldClass MoldClass) => true;

    [Delphi(PortStatus.Ported)]
    protected override bool ExternalKeyExistsInExternalStorage(TMoldClass MoldClass, TBoldObjectId ExternalKey) =>
        Config.FindExpressionName(MoldClass.ExpandedExpressionName)?.OnExists is not { } Exists || Exists(ExternalKey);

    [Delphi(PortStatus.Ported)]
    protected TBoldExternalPersistenceConfigItem? ConfigItemByObjectContents(IBoldObjectContents ObjectContents) =>
        Config.FindExpressionName(MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex].ExpandedExpressionName);

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToPeristenceEvents(TBoldSubscriber Subscriber) => base.SubscribeToPeristenceEvents(Subscriber);

    /// <summary>Calls the class's OnUpdateObject for each object; a class without one may not modify objects.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void UpdateObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIdList[i])!;
            TMoldClass MoldClass = MoldModel.Classes[ObjectIdList[i].TopSortedIndex];
            if (ConfigItemByObjectContents(ObjectContents) is { } ConfigItem)
            {
                TBoldExternalPersistenceUpdateEvent OnUpdate = ConfigItem.OnUpdateObject ?? throw new EBold(SysUtils.Format(ExPeConsts.sModifyObjectsNotAllowed, ConfigItem.ExpressionName));
                OnUpdate(PersistentObjectFromObjectContents(ObjectContents, ValueSpace, MoldClass), GetExternalKeyFromObject(ObjectContents, ValueSpace), ValueSpace);
            }
        }
    }

    /// <summary>The class's OnGetKeyFromObject, or a string or default id of the one key member's value.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldObjectId GetExternalKeyFromObject(IBoldObjectContents ObjectContents, IBoldValueSpace ValueSpace)
    {
        TBoldExternalPersistenceConfigItem? ConfigItem = ConfigItemByObjectContents(ObjectContents);
        TMoldClass MoldClass = MoldModel.Classes[ObjectContents.ObjectId.TopSortedIndex];
        if (ConfigItem?.OnGetKeyFromObject is { } GetKey)
        {
            return GetKey(PersistentObjectFromObjectContents(ObjectContents, ValueSpace, MoldClass));
        }

        TBoldObjectId? Result = null;
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            if (MoldClass.AllBoldMembers[i].Storage == TBoldStorage.bsExternalKey)
            {
                if (Result is not null)
                {
                    throw new EBold(SysUtils.Format(ExPeConsts.sAssignKeyValueRequiresOneKey, MoldClass.Name));
                }

                Result = ObjectContents.ValueByIndex(i) switch
                {
                    null => throw new EBoldInternal(SysUtils.Format("Keyvalue not assigned: %s", MoldClass.Name)),
                    IBoldStringContent StrContent => new TBoldStringID { AsString = StrContent.asString },
                    IBoldIntegerContent IntContent => new TBoldDefaultID { AsInteger = IntContent.asInteger },
                    _ => throw new EBold(SysUtils.Format(ExPeConsts.sKeyTypeNotAutoHandled, MoldClass.Name, MoldClass.AllBoldMembers[i].Name)),
                };
            }
        }

        return Result!;
    }

    [Delphi(PortStatus.Ported)]
    protected override void PostFetch(object? FetchContext, TMoldClass MoldClass)
    {
        base.PostFetch(FetchContext, MoldClass);
        Config.FindExpressionName(MoldClass.ExpandedExpressionName)?.OnPostFetch?.Invoke(FetchContext);
    }

    [Delphi(PortStatus.Ported)]
    protected override void PrepareFetchExternal(TBoldObjectIdList ExternalKeys, IBoldValueSpace ValueSpace, TMoldClass MoldClass, TBoldMemberIdList? MemberIdList,
        ref object? FetchContext)
    {
        base.PrepareFetchExternal(ExternalKeys, ValueSpace, MoldClass, MemberIdList, ref FetchContext);
        Config.FindExpressionName(MoldClass.ExpandedExpressionName)?.OnPrepareFetch?.Invoke(ExternalKeys, MemberIdList, ref FetchContext);
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetMaxFetchBlockSize() => fMaxFetchBlockSize;

    /// <summary>The object as the adapter registered for its class, or a plain adapter.</summary>
    [Delphi(PortStatus.Ported)]
    private IPersistentBoldObject PersistentObjectFromObjectContents(IBoldObjectContents Obj, IBoldValueSpace ValueSpace, TMoldClass MoldClass)
    {
        Type AdapterClass = TBoldObjectPersistenceAdapter.FindRegisteredPersistenceInterface(MoldModel.Classes[Obj.ObjectId.TopSortedIndex].ExpandedExpressionName)
            ?? typeof(TBoldObjectPersistenceAdapter);
        return (IPersistentBoldObject)Activator.CreateInstance(AdapterClass, Obj, ValueSpace, MoldClass)!;
    }

    /// <summary>The class's OnGetInternalSQLForKeys, or the generated fragment.</summary>
    [Delphi(PortStatus.Ported)]
    protected override string ExternalKeysToInternalSQL(TMoldClass MoldClass, TBoldObjectIdList ExternalKeys) =>
        Config.FindExpressionName(MoldClass.ExpandedExpressionName)?.OnGetInternalSQLForKeys is { } GetSQL
            ? GetSQL(ExternalKeys)
            : base.ExternalKeysToInternalSQL(MoldClass, ExternalKeys);

    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceConfigItems Config => FConfig;
}
