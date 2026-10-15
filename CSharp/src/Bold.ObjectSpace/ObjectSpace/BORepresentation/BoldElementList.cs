// Delphi: Source/ObjectSpace/BORepresentation/BoldElementList.pas
// Port map: CSharp/port/units/BoldElementList.md

using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>A list of any elements (objects, values, types), not owning them; OCL results of mixed types use it.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldElementList : TBoldList
{
    public TBoldElementList()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override void AddElement(TBoldElement? Element)
    {
        if (ListController!.IndexOfElement(Element!) == -1 || DuplicateControl())
        {
            ListController.AddElement(Element!);
            Changed(beItemAdded, [Element]);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void AllocateData()
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBoldElementList list)
        {
            AddList(list);
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void FreeData()
    {
    }

    [Delphi(PortStatus.Ported)] protected override bool GetCanCreateNew() => false;
    [Delphi(PortStatus.Ported)] protected override int GetCount() => ListController!.Count;
    [Delphi(PortStatus.Ported)] protected override TBoldElement? GetElement(int index) => ListController!.GetElement(index);
    [Delphi(PortStatus.Ported)] protected override bool IncludesElement(TBoldElement? Item) => ListController!.IncludesElement(Item!);
    [Delphi(PortStatus.Ported)] protected override int IndexOfElement(TBoldElement? Item) => ListController!.IndexOfElement(Item!);

    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        ListController = new TBoldElementListController(this);
        DuplicateMode = TBoldListDupMode.bldmAllow;
        base.InitializeMember(AOwningElement, ElementTypeInfo);
    }

    [Delphi(PortStatus.Ported)] protected override void InsertElement(int index, TBoldElement? Element) => ListController!.InsertElement(index, Element!);

    [Delphi(PortStatus.Ported)]
    public override void InsertNew(int index) => throw new EBold(SysUtils.Format(sOperationNotAllowed, GetType().Name, "InsertNew"));

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement InternalAddNew() => throw new EBold(SysUtils.Format(sOperationNotAllowed, GetType().Name, "InternalAddNew"));

    [Delphi(PortStatus.Ported)] public override void Move(int CurIndex, int NewIndex) => ListController!.Move(CurIndex, NewIndex);

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => throw new EBold(SysUtils.Format(sOperationNotAllowed, GetType().Name, "ProxyClass"));

    [Delphi(PortStatus.Ported)]
    public override void RemoveByIndex(int index)
    {
        ListController!.RemoveByIndex(index);
        Changed(beItemDeleted, []);
    }

    [Delphi(PortStatus.Ported)] protected override void SetElement(int index, TBoldElement? Value) => ListController!.SetElement(index, Value!);

    /// <summary>Removes all elements (abstract in Delphi's TBoldList and not overridden in TBoldElementList; C# needs a body).</summary>
    protected override void InternalClear()
    {
        for (int I = Count - 1; I >= 0; I--)
        {
            RemoveByIndex(I);
        }
    }
}

/// <summary>Creates element lists.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldElementListFactory
{
    [Delphi(PortStatus.Ported)]
    public static TBoldElementList CreateList(TBoldSystem aSystem) =>
        (TBoldElementList)TBoldMember.CreateWithTypeInfo(typeof(TBoldElementList), aSystem.BoldSystemTypeInfo.ListTypeInfoByElement(null));
}

/// <summary>An indexable list of elements.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldIndexableElementList : TBoldIndexableList
{
    internal new object? this[int I] => base[I];
}

/// <summary>Holds an element list's elements in an indexable list that does not own them.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldElementListController : TBoldListController
{
    private readonly TBoldIndexableElementList fList = new() { OwnsEntries = false };

    [Delphi(PortStatus.Ported)]
    public TBoldElementListController(TBoldList OwningList)
        : base(OwningList)
    {
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldElementListController.Destroy", Note = "Freed the list; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    protected TBoldIndexableElementList List => fList;

    [Delphi(PortStatus.Ported)] protected override int GetCount() => List.Count;
    [Delphi(PortStatus.Ported)] protected override bool GetCanCreateNew() => false;
    [Delphi(PortStatus.Ported)] protected internal override TBoldElement CreateNew() => throw new EBold(sNotImplemented);
    [Delphi(PortStatus.Ported)] protected internal override string GetStreamName() => throw new EBold(sNotImplemented);
    [Delphi(PortStatus.Ported)] public override void AddElement(TBoldElement Element) => List.Add(Element);
    [Delphi(PortStatus.Ported)] public override TBoldElement? GetElement(int index) => (TBoldElement?)List[index];
    [Delphi(PortStatus.Ported)] public override bool IncludesElement(TBoldElement Item) => List.IndexOf(Item) != -1;
    [Delphi(PortStatus.Ported)] public override int IndexOfElement(TBoldElement Item) => List.IndexOf(Item);
    [Delphi(PortStatus.Ported)] public override void InsertElement(int index, TBoldElement Element) => List.Insert(index, Element);
    [Delphi(PortStatus.Ported)] public override void Move(int CurrentIndex, int NewIndex) => List.Move(CurrentIndex, NewIndex);
    [Delphi(PortStatus.Ported)] public override void RemoveByIndex(int index) => List.RemoveByIndex(index);
    [Delphi(PortStatus.Ported)] public override void SetElement(int index, TBoldElement Value) => throw new EBold(sCannotSetElementsInTypeLists);
}
