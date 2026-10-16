// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceControllerConfig.pas
// Port map: CSharp/port/units/BoldExternalPersistenceControllerConfig.md
// The configuration of the event-driven external persistence: per class, the events that read, create, update and
// delete objects in the external storage and list and match their keys.

namespace Bold;

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistenceFetchEvent(IPersistentBoldObject Obj, TBoldObjectId ExternalKey, object? FetchContext);

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistenceCreateEvent(IPersistentBoldObject Obj, TBoldObjectId? ExternalKey, IBoldValueSpace ValueSpace);

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistenceUpdateEvent(IPersistentBoldObject Obj, TBoldObjectId ExternalKey, IBoldValueSpace ValueSpace);

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistenceDeleteEvent(IPersistentBoldObject Obj, TBoldObjectId ExternalKey);

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistenceGetListEvent(TBoldObjectIdList ExternalKeys);

[Delphi(PortStatus.Ported)]
public delegate string TBoldExternalPersistenceGetInternalSQLForKeysEvent(TBoldObjectIdList ExternalKeys);

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistencePrepareFetchEvent(TBoldObjectIdList ExternalKeys, TBoldMemberIdList? Members, ref object? FetchContext);

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistencePostFetchEvent(object? FetchContext);

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistenceReadMemberEvent(IPersistentBoldObject Obj, TBoldObjectId ExternalKey, TMoldMember Member, object? FetchContext);

[Delphi(PortStatus.Ported)]
public delegate bool TBoldExternalPersistenceGetExistsEvent(TBoldObjectId ExternalKey);

[Delphi(PortStatus.Ported)]
public delegate void TBoldExternalPersistenceAssignKeyToObjectEvent(IPersistentBoldObject Obj, TBoldObjectId ExternalKey);

[Delphi(PortStatus.Ported)]
public delegate TBoldObjectId TBoldExternalPersistenceGetKeyFromObject(IPersistentBoldObject Obj);

/// <summary>The events of one class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceConfigItem(TCollection? Collection) : TBoldUniquelyNamedCollectionItemWithNameStorage(Collection)
{
    [Delphi(PortStatus.Ported)] private void SetExpressionName(string Value) => UniqueName = Value;
    [Delphi(PortStatus.Ported)] private string GetExpressionName() => UniqueName;
    [Delphi(PortStatus.Ported)] protected override string GetDisplayName() => ExpressionName;

    [Delphi(PortStatus.Ported)] public string ExpressionName { get => GetExpressionName(); set => SetExpressionName(value); }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceCreateEvent? OnCreateObject { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceFetchEvent? OnReadObject { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceUpdateEvent? OnUpdateObject { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceReadMemberEvent? OnReadMember { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceDeleteEvent? OnDeleteObject { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceGetExistsEvent? OnExists { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceGetListEvent? OnGetKeyList { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceGetKeyFromObject? OnGetKeyFromObject { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceAssignKeyToObjectEvent? OnAssignKeyToObject { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistencePrepareFetchEvent? OnPrepareFetch { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistencePostFetchEvent? OnPostFetch { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceGetInternalSQLForKeysEvent? OnGetInternalSQLForKeys { get; set; }
}

/// <summary>The configuration items, by class expression name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceConfigItems : TBoldCollectionWithUniquelyNamedItems
{
    [Delphi(PortStatus.Ported, Note = "The owning handle is a .NET component, not a TPersistent, so the collection has no owner.")]
    public TBoldExternalPersistenceConfigItems(TPersistent? AOwner = null) : base(AOwner, typeof(TBoldExternalPersistenceConfigItem))
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldExternalPersistenceConfigItem GetItem(int Index) => (TBoldExternalPersistenceConfigItem)base[Index];

    [Delphi(PortStatus.Ported)]
    public new TBoldExternalPersistenceConfigItem Add() => (TBoldExternalPersistenceConfigItem)base.Add();

    [Delphi(PortStatus.Ported)]
    public TBoldExternalPersistenceConfigItem? FindExpressionName(string ExpressionName) => (TBoldExternalPersistenceConfigItem?)ItemByName(ExpressionName);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldExternalPersistenceConfigItem Items(int Index) => GetItem(Index);
}
