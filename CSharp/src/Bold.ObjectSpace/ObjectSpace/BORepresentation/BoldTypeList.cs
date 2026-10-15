// Delphi: Source/ObjectSpace/BORepresentation/BoldTypeList.pas
// Port map: CSharp/port/units/BoldTypeList.md

using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>A list of types (element type infos), as OCL gives for type expressions.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTypeList : TBoldList
{
    public TBoldTypeList()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override void AddElement(TBoldElement? Element)
    {
        if (ListController!.IndexOfElement(Element!) == -1 || DuplicateControl())
        {
            ListController.AddElement(Element!);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void AllocateData()
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBoldTypeList list)
        {
            AddList(list);
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate) => base.DefaultSubscribe(Subscriber, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    protected override void FreeData()
    {
    }

    [Delphi(PortStatus.Ported)] protected override int GetCount() => ListController!.Count;
    [Delphi(PortStatus.Ported)] protected override TBoldElement? GetElement(int index) => ListController!.GetElement(index);
    [Delphi(PortStatus.Ported)] protected override bool IncludesElement(TBoldElement? Item) => ListController!.IncludesElement(Item!);
    [Delphi(PortStatus.Ported)] protected override int IndexOfElement(TBoldElement? Item) => ListController!.IndexOfElement(Item!);

    /// <summary>Creates the controller (a type info list) and allows duplicates.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        ListController = new TBoldTypeListController(this);
        DuplicateMode = TBoldListDupMode.bldmAllow;
        base.InitializeMember(AOwningElement, ElementTypeInfo);
    }

    [Delphi(PortStatus.Ported)] protected override void InsertElement(int index, TBoldElement? Element) => ListController!.InsertElement(index, Element!);

    [Delphi(PortStatus.Ported)]
    public override void InsertNew(int index) => throw new EBold(SysUtils.Format(sCannotInsertTypes, GetType().Name));

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement InternalAddNew() => throw new EBold(SysUtils.Format(sCannotAddTypes, GetType().Name));

    [Delphi(PortStatus.Ported)]
    protected override void InternalClear()
    {
        for (int I = Count - 1; I >= 0; I--)
        {
            RemoveByIndex(I);
        }
    }

    [Delphi(PortStatus.Ported)] public override void Move(int CurIndex, int NewIndex) => ListController!.Move(CurIndex, NewIndex);

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => throw new EBold(SysUtils.Format(sAbstractError_InterfaceNotSupported, GetType().Name));

    [Delphi(PortStatus.Ported)] public override void RemoveByIndex(int index) => ListController!.RemoveByIndex(index);
    [Delphi(PortStatus.Ported)] protected override void SetElement(int index, TBoldElement? Value) => ListController!.SetElement(index, Value!);
}

/// <summary>Creates type lists.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTypeListFactory : TBoldMemoryManagedObject
{
    /// <summary>An empty list of types of the element's system.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldTypeList CreateList(TBoldElementTypeInfo elementTypeInfo)
    {
        var system = (TBoldSystemTypeInfo)elementTypeInfo.SystemTypeInfo!;
        return (TBoldTypeList)TBoldMember.CreateWithTypeInfo(typeof(TBoldTypeList), system.ListTypeInfoByElement(system.TypeTypeInfo));
    }
}

/// <summary>Holds a type list's elements in a type info list that does not own them.</summary>
[Delphi(PortStatus.Ported)]
internal class TBoldTypeListController : TBoldListController
{
    private readonly TBoldElementTypeInfoList fList = new() { OwnsEntries = false };

    [Delphi(PortStatus.Ported)]
    public TBoldTypeListController(TBoldList OwningList)
        : base(OwningList)
    {
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldTypeListController.Destroy", Note = "Freed the list; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    protected TBoldElementTypeInfoList List => fList;

    [Delphi(PortStatus.Ported)] protected override int GetCount() => List.Count;
    [Delphi(PortStatus.Ported)] protected override bool GetCanCreateNew() => false;
    [Delphi(PortStatus.Ported)] protected internal override TBoldElement CreateNew() => throw new EBold(sCannotCreateNewInTypeLists);
    [Delphi(PortStatus.Ported)] protected internal override string GetStreamName() => throw new EBold(sNotImplemented);

    [Delphi(PortStatus.Ported)]
    public override void AddElement(TBoldElement Element)
    {
        if (Element is not TBoldElementTypeInfo typeInfo)
        {
            throw new EBold(SysUtils.Format(sCannotAddElement, Element.GetType().Name));
        }

        List.Add(typeInfo);
    }

    [Delphi(PortStatus.Ported)] public override TBoldElement? GetElement(int index) => List[index];
    [Delphi(PortStatus.Ported)] public override bool IncludesElement(TBoldElement Item) => List.IndexOf(Item) != -1;
    [Delphi(PortStatus.Ported)] public override int IndexOfElement(TBoldElement Item) => List.IndexOf(Item);
    [Delphi(PortStatus.Ported)] public override void InsertElement(int index, TBoldElement Element) => List.Insert(index, Element);
    [Delphi(PortStatus.Ported)] public override void Move(int CurrentIndex, int NewIndex) => List.Move(CurrentIndex, NewIndex);
    [Delphi(PortStatus.Ported)] public override void RemoveByIndex(int index) => List.RemoveByIndex(index);
    [Delphi(PortStatus.Ported)] public override void SetElement(int index, TBoldElement Value) => throw new EBold(sCannotSetElementsInTypeLists);
}
