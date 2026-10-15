// Delphi: Source/ObjectSpace/Core/BoldMetaElementList.pas
// Port map: CSharp/port/units/BoldMetaElementList.md

namespace Bold;

/// <summary>Meta elements indexed by expression name and Delphi name (ignoring case) and by model name (case-sensitive).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMetaElementList : TBoldIndexableList
{
    private static int IX_ExpressionName = -1;
    private static int IX_DelphiName = -1;
    private static int IX_ModelName = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldMetaElementList()
    {
        SetIndexCapacity(3);
        BoldIndexableList.SetIndexVariable(ref IX_ExpressionName, AddIndex(new TBoldValueTypeNameIndex()));
        BoldIndexableList.SetIndexVariable(ref IX_DelphiName, AddIndex(new TDelphiNameIndex()));
        BoldIndexableList.SetIndexVariable(ref IX_ModelName, AddIndex(new TModelNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private protected TBoldMetaElement GetItem(int index) => (TBoldMetaElement)base[index]!;

    [Delphi(PortStatus.Ported)]
    private protected TBoldMetaElement? GetItemByExpressionName(string ExpressionName) =>
        (TBoldMetaElement?)((TBoldValueTypeNameIndex)Indexes(IX_ExpressionName)!).FindByString(ExpressionName);

    [Delphi(PortStatus.Ported)]
    private protected TBoldMetaElement? GetItemByDelphiName(string DelphiName) =>
        (TBoldMetaElement?)((TDelphiNameIndex)Indexes(IX_DelphiName)!).FindByString(DelphiName);

    [Delphi(PortStatus.Ported)]
    private protected TBoldMetaElement? GetItemByModelName(string ModelName) =>
        (TBoldMetaElement?)((TModelNameIndex)Indexes(IX_ModelName)!).FindByString(ModelName);

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldMetaElement Item) => base.Add(Item);

    [Delphi(PortStatus.Ported, Symbol = "TBoldMetaElementList.Items")]
    public new TBoldMetaElement this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)] public TBoldMetaElement? ItemsByExpressionName(string ExpressionName) => GetItemByExpressionName(ExpressionName);
    [Delphi(PortStatus.Ported)] public TBoldMetaElement? ItemsByDelphiName(string ExpressionName) => GetItemByDelphiName(ExpressionName);
    [Delphi(PortStatus.Ported)] public TBoldMetaElement? ItemsByModelName(string ModelName) => GetItemByModelName(ModelName);

    [Delphi(PortStatus.Ported, Symbol = "TBoldValueTypeNameIndex")]
    private sealed class TBoldValueTypeNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldValueTypeNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldMetaElement)Item!).ExpressionName;
    }

    [Delphi(PortStatus.Ported, Symbol = "TDelphiNameIndex")]
    private sealed class TDelphiNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TDelphiNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldMetaElement)Item!).DelphiName;
    }

    [Delphi(PortStatus.Ported, Symbol = "TModelNameIndex")]
    private sealed class TModelNameIndex : TBoldCaseSensitiveStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TModelNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldMetaElement)Item!).ModelName;
    }
}

/// <summary>A meta element list of types.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldElementTypeInfoList : TBoldMetaElementList
{
    [Delphi(PortStatus.Ported)]
    private new TBoldElementTypeInfo GetItem(int index) => (TBoldElementTypeInfo)base.GetItem(index);

    [Delphi(PortStatus.Ported)]
    private new TBoldElementTypeInfo? GetItemByExpressionName(string ExpressionName) => (TBoldElementTypeInfo?)base.GetItemByExpressionName(ExpressionName);

    [Delphi(PortStatus.Ported)]
    private new TBoldElementTypeInfo? GetItemByDelphiName(string DelphiName) => (TBoldElementTypeInfo?)base.GetItemByDelphiName(DelphiName);

    [Delphi(PortStatus.Ported)]
    private new TBoldElementTypeInfo? GetItemByModelName(string ModelName) => (TBoldElementTypeInfo?)base.GetItemByModelName(ModelName);

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldElementTypeInfo Item) => base.Add(Item);

    [Delphi(PortStatus.Ported, Symbol = "TBoldElementTypeInfoList.Items")]
    public new TBoldElementTypeInfo this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)] public new TBoldElementTypeInfo? ItemsByExpressionName(string ExpressionName) => GetItemByExpressionName(ExpressionName);
    [Delphi(PortStatus.Ported)] public new TBoldElementTypeInfo? ItemsByDelphiName(string ExpressionName) => GetItemByDelphiName(ExpressionName);
    [Delphi(PortStatus.Ported)] public new TBoldElementTypeInfo? ItemsByModelName(string ModelName) => GetItemByModelName(ModelName);
}
