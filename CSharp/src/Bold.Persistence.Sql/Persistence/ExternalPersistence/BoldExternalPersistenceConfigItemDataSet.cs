// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceConfigItemDataSet.pas
// Port map: CSharp/port/units/BoldExternalPersistenceConfigItemDataSet.md
// The configuration of the data set external persistence: the data set that holds each class.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldExternalPersistenceConfigItemDataSet", Symbol = "TBoldExternalPersistenceConfigDataSetItem.Destroy", Note = "Did nothing beyond inherited.")]

namespace Bold;

/// <summary>The data set of one class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceConfigDataSetItem : TBoldUniquelyNamedCollectionItemWithNameStorage
{
    [Delphi(PortStatus.Ported)]
    public TBoldExternalPersistenceConfigDataSetItem(TCollection? AOwner) : base(AOwner)
    {
    }

    [Delphi(PortStatus.Ported)] protected override string GetDisplayName() => UniqueName;
    [Delphi(PortStatus.Ported)] private string GetExpressionName() => UniqueName;
    [Delphi(PortStatus.Ported)] private void SetExpressionName(string Value) => UniqueName = Value;

    [Delphi(PortStatus.Ported)] public string ClassExpressionName { get => GetExpressionName(); set => SetExpressionName(value); }
    [Delphi(PortStatus.Ported)] public TDataSet? DataSet { get; set; }
}

/// <summary>The data set items, by class expression name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceConfigDataSetItems : TBoldCollectionWithUniquelyNamedItems
{
    [Delphi(PortStatus.Ported, Note = "The owning handle is a .NET component, not a TPersistent, so the collection has no owner.")]
    public TBoldExternalPersistenceConfigDataSetItems(TPersistent? AOwner = null) : base(AOwner, typeof(TBoldExternalPersistenceConfigDataSetItem))
    {
    }

    [Delphi(PortStatus.Ported)]
    public new TBoldExternalPersistenceConfigDataSetItem Add() => (TBoldExternalPersistenceConfigDataSetItem)base.Add();

    [Delphi(PortStatus.Ported)]
    public TBoldExternalPersistenceConfigDataSetItem? FindExpressionName(string ExpressionName) => (TBoldExternalPersistenceConfigDataSetItem?)ItemByName(ExpressionName);

    [Delphi(PortStatus.Ported)]
    private TBoldExternalPersistenceConfigDataSetItem GetItem(int Index) => (TBoldExternalPersistenceConfigDataSetItem)base[Index];

    [Delphi(PortStatus.Ported, Symbol = "TBoldExternalPersistenceConfigDataSetItems.Items")]
    public new TBoldExternalPersistenceConfigDataSetItem this[int Index] => GetItem(Index);
}
