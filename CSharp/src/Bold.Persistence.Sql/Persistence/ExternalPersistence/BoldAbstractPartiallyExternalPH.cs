// Delphi: Source/Persistence/ExternalPersistence/BoldAbstractPartiallyExternalPH.pas
// Port map: CSharp/port/units/BoldAbstractPartiallyExternalPH.md
// The handle of a partially external controller: objects found deleted or new in the external storage, links set
// from external keys, and the internal id of an object with an external key.

namespace Bold;

/// <summary>The base of the handles of classes stored partly externally.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractpartiallyExternalPH(System.ComponentModel.IContainer? Owner = null) : TBoldAbstractExternalPersistenceHandle(Owner)
{
    [Delphi(PortStatus.Ported)]
    private TBoldObjectIdList? GetDeletedObjects() => HasPersistenceController ? PersistenceController.DeletedExternalObjects : null;

    [Delphi(PortStatus.Ported)]
    private bool GetHasDeletedObjects() => DeletedObjects is { Count: > 0 };

    [Delphi(PortStatus.Ported)]
    private bool GetHasNewObjects() => NewObjects is { Count: > 0 };

    [Delphi(PortStatus.Ported)]
    private TBoldObjectIdList? GetNewObjects() => HasPersistenceController ? PersistenceController.NewExternalObjects : null;

    /// <summary>The internal id of the object of the class with the external key (made if missing), or null.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldObjectId? GetObjectIdByExternalKey(string ExpressionName, TBoldObjectId ExternalKey)
    {
        TMoldClass MoldClass = BoldModel!.MoldModel.Classes.ItemsByExpressionName(ExpressionName)
            ?? throw new EBold(SysUtils.Format(ExPeConsts.sInvalidClassName, GetType().Name, ExpressionName));
        var ExternalKeys = new TBoldObjectIdList();
        var InternalIds = new TBoldObjectIdList();
        ExternalKeys.Add(ExternalKey);
        PersistenceController.TranslateExternalKeysToInternalIds(MoldClass, ExternalKeys, InternalIds);
        return InternalIds.Count > 0 ? InternalIds[0].Clone() : null;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId? GetObjectIdByExternalKeyInteger(string ExpressionName, int ExternalKey) =>
        GetObjectIdByExternalKey(ExpressionName, new TBoldDefaultID { AsInteger = ExternalKey });

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId? GetObjectIdByExternalKeyString(string ExpressionName, string ExternalKey) =>
        GetObjectIdByExternalKey(ExpressionName, new TBoldStringID { AsString = ExternalKey });

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractPartiallyExternalPC GetPersistenceController() => (TBoldAbstractPartiallyExternalPC)base.PersistenceController;

    [Delphi(PortStatus.Ported)]
    public void SetMultiLink(IBoldObjectIdListRef MultiLink, TBoldObjectIdList ExternalKeys, TMoldClass MoldClassOfOtherEnd) =>
        PersistenceController.SetMultiLink(MultiLink, ExternalKeys, MoldClassOfOtherEnd);

    [Delphi(PortStatus.Ported)]
    public void SetSingleLink(IBoldObjectIdRef SingleLink, TBoldObjectId? ExternalKey, TMoldClass MoldClassOfOtherEnd) =>
        PersistenceController.SetSingleLink(SingleLink, ExternalKey, MoldClassOfOtherEnd);

    [Delphi(PortStatus.Ported)] public new TBoldAbstractPartiallyExternalPC PersistenceController => GetPersistenceController();
    [Delphi(PortStatus.Ported)] public TBoldObjectIdList? DeletedObjects => GetDeletedObjects();
    [Delphi(PortStatus.Ported)] public bool HasDeletedObjects => GetHasDeletedObjects();
    [Delphi(PortStatus.Ported)] public TBoldObjectIdList? NewObjects => GetNewObjects();
    [Delphi(PortStatus.Ported)] public bool HasNewObjects => GetHasNewObjects();
}
