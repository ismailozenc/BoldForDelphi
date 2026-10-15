// Delphi: Source/ObjectSpace/BORepresentation/BoldSystem.pas
// Port map: CSharp/port/units/BoldSystem.md
// The lists: TBoldList, TBoldMemberList and TBoldObjectList.

using static Bold.BoldCoreConsts;
using static Bold.BoldElements;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>A list member: elements, duplicate handling, the May... queries for list changes; a controller holds the elements.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldList : TBoldMember
{
    private TBoldListController? fListController;

    protected TBoldList()
    {
    }

    protected TBoldList(Action<TBoldMember> Construction)
        : base(Construction)
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldList.Destroy")]
    public override void Dispose()
    {
        PrepareToDestroy();
        fListController?.Dispose();
        fListController = null;
        base.Dispose();
        FreeData();
    }

    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        if (Mutable)
        {
            AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beItemAdded, beItemDeleted, beItemReplaced, beOrderChanged, beValueInvalid), RequestedEvent);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void AllocateData() => throw new EBold(SysUtils.Format(sLocatedAbstractError, GetType().Name, "AllocateData"));

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldAbstractController? GetController() => fListController;

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => ListController!.GetStringrepresentation();

    /// <summary>Removes all elements in a transaction, when CanClear allows it.</summary>
    [Delphi(PortStatus.Ported)]
    public void Clear()
    {
        BoldClearLastFailure();
        PrepareClear();
        if (Mutable && !CanClear(null))
        {
            BoldRaiseLastFailure(this, "Clear", "");
        }

        BoldSystem?.StartTransaction();
        try
        {
            InternalClear();
            BoldSystem?.CommitTransaction();
        }
        catch
        {
            BoldSystem?.RollbackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool Includes(TBoldElement? Item) => IncludesElement(Item);

    /// <summary>Changes the duplicate mode; switching to merge removes the duplicates.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetDuplicateMode(TBoldListDupMode NewMode)
    {
        TBoldListDupMode OldMode = GetDuplicateMode();
        if (NewMode != OldMode && NewMode == TBoldListDupMode.bldmMerge)
        {
            int i = 0;
            while (i < Count)
            {
                int j = i + 1;
                while (j < Count)
                {
                    if (this[i]!.IsEqual(this[j]))
                    {
                        RemoveByIndex(j);
                    }
                    else
                    {
                        j++;
                    }
                }

                i++;
            }
        }

        SetInternalState(unchecked((uint)BoldDuplicateModeMask), BoldDMShift, (uint)NewMode);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void Remove(TBoldElement? Item)
    {
        EnsureContentsCurrent();
        int I = IndexOf(Item);
        if (I == -1)
        {
            throw new EBold(SysUtils.Format(sItemNotInList, GetType().Name));
        }

        RemoveByIndex(I);
    }

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldElement? Element)
    {
        if (!Mutable)
        {
            MutableError("AddToList");
        }

        AddElement(Element);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void AddList(TBoldList List)
    {
        if (!Mutable)
        {
            MutableError("AddListToList");
        }

        for (int I = 0; I < List.Count; I++)
        {
            AddElement(List[I]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public int IndexOf(TBoldElement? Item) => IndexOfElement(Item);

    [Delphi(PortStatus.Ported)]
    public void Insert(int index, TBoldElement? Element)
    {
        if (!Mutable)
        {
            MutableError("InsertElementToList");
        }

        InsertElement(index, Element);
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetCanCreateNew() => CanModify() && ListController is not null && ListController.CanCreateNew;

    [Delphi(PortStatus.Ported)]
    protected void EnsureCanCreateNew()
    {
        BoldClearLastFailure();
        if (!CanCreateNew)
        {
            BoldRaiseLastFailure(this, "EnsureCanCreateNew", sCannotCreateNewElement);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldElement CreateNew()
    {
        EnsureCanCreateNew();
        return ListController!.CreateNew();
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElement AddNew() => InternalAddNew();

    [Delphi(PortStatus.Ported)]
    public void ToStrings(TBoldRepresentation Representation, TStrings S)
    {
        S.Clear();
        AddToStrings(Representation, S);
    }

    [Delphi(PortStatus.Ported)]
    public void AddToStrings(TBoldRepresentation Representation, TStrings S)
    {
        for (int I = 0; I < Count; I++)
        {
            TBoldElement element = this[I]!;
            S.AddObject(element.StringRepresentation(Representation), element);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ToStringsWithNil(TBoldRepresentation Representation, TStrings S, string nilString)
    {
        S.Clear();
        S.AddObject(nilString, null);
        AddToStrings(Representation, S);
    }

    [Delphi(PortStatus.Ported)]
    public override void GetAsList(TBoldIndirectElement ResultList) => ResultList.SetReferenceValue(this);

    [Delphi(PortStatus.Ported)]
    public virtual void EnsureRange(int FromIndex, int ToIndex)
    {
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool CanInsert(int index, TBoldElement? Element, TBoldSubscriber? Subscriber) => SendQuery(bqMayInsert, [index, Element], Subscriber);

    [Delphi(PortStatus.Ported)]
    public virtual bool CanRemove(int index, TBoldSubscriber? Subscriber) => SendQuery(bqMayRemove, [index], Subscriber);

    [Delphi(PortStatus.Ported)]
    public virtual bool CanMove(int CurIndex, int NewIndex, TBoldSubscriber? Subscriber = null) =>
        NewIndex >= 0 && NewIndex < Count && SendQuery(bqMayMove, [CurIndex, NewIndex], Subscriber);

    [Delphi(PortStatus.Ported)]
    public virtual bool CanSet(int index, TBoldElement? Item, TBoldSubscriber? Subscriber) => SendQuery(bqMayReplace, [index, Item], Subscriber);

    /// <summary>Sorts the list with the comparison (quicksort, moving elements so subscribers see the order change).</summary>
    [Delphi(PortStatus.Ported)]
    public void Sort(TBoldElementCompare CompareFunc)
    {
        void QuickSort(int L, int R)
        {
            int I;
            do
            {
                I = L;
                int J = R;
                TBoldElement? P = this[(L + R) >> 1];
                do
                {
                    while (CompareFunc(this[I], P) < 0)
                    {
                        I++;
                    }

                    while (CompareFunc(this[J], P) > 0)
                    {
                        J--;
                    }

                    if (I <= J)
                    {
                        if (I != J)
                        {
                            Move(I, J);
                            Move(J - 1, I);
                        }

                        I++;
                        J--;
                    }
                }
                while (I <= J);

                if (L < J)
                {
                    QuickSort(L, J);
                }

                L = I;
            }
            while (I < R);
        }

        if (Count > 0)
        {
            QuickSort(0, Count - 1);
        }
    }

    /// <summary>Whether to add a duplicate: yes (allow), no (merge), or raise (error).</summary>
    [Delphi(PortStatus.Ported)]
    protected bool DuplicateControl() => DuplicateMode switch
    {
        TBoldListDupMode.bldmAllow => true,
        TBoldListDupMode.bldmMerge => false,
        TBoldListDupMode.bldmError => throw new EBold(SysUtils.Format(sDuplicateInList, GetType().Name)),
        _ => throw new EBoldInternal(SysUtils.Format(sUnknownDuplicationMode, GetType().Name)),
    };

    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        AllocateData();
    }

    [Delphi(PortStatus.Ported)]
    public bool CanClear(TBoldSubscriber? Subscriber) => SendQuery(bqMayClear, [], Subscriber);

    [Delphi(PortStatus.Ported)]
    private TBoldListDupMode GetDuplicateMode() => (TBoldListDupMode)GetInternalState(unchecked((uint)BoldDuplicateModeMask), BoldDMShift);

    [Delphi(PortStatus.Ported)]
    internal virtual void PrepareClear()
    {
    }

    [Delphi(PortStatus.Ported)]
    public void MakeContentsImmutable()
    {
        for (int I = 0; I < Count; I++)
        {
            this[I]?.MakeImmutable();
        }
    }

    [Delphi(PortStatus.Ported)] protected abstract void AddElement(TBoldElement? Element);
    [Delphi(PortStatus.Ported)] protected abstract void FreeData();
    [Delphi(PortStatus.Ported)] protected abstract int GetCount();
    [Delphi(PortStatus.Ported)] protected abstract TBoldElement? GetElement(int index);
    [Delphi(PortStatus.Ported)] protected abstract bool IncludesElement(TBoldElement? Item);
    [Delphi(PortStatus.Ported)] protected abstract int IndexOfElement(TBoldElement? Item);
    [Delphi(PortStatus.Ported)] protected abstract void InsertElement(int index, TBoldElement? Element);
    [Delphi(PortStatus.Ported)] protected abstract TBoldElement InternalAddNew();
    [Delphi(PortStatus.Ported)] protected abstract void SetElement(int index, TBoldElement? Value);
    [Delphi(PortStatus.Ported)] protected abstract void InternalClear();
    [Delphi(PortStatus.Ported)] public abstract void InsertNew(int index);
    [Delphi(PortStatus.Ported)] public abstract void Move(int CurIndex, int NewIndex);
    [Delphi(PortStatus.Ported)] public abstract void RemoveByIndex(int index);

    [Delphi(PortStatus.Ported)]
    protected internal TBoldListController? ListController
    {
        get => fListController;
        set => fListController = value;
    }

    [Delphi(PortStatus.Ported)] public bool CanCreateNew => GetCanCreateNew();
    [Delphi(PortStatus.Ported)] public int Count => GetCount();

    [Delphi(PortStatus.Ported)]
    public TBoldListDupMode DuplicateMode
    {
        get => GetDuplicateMode();
        set => SetDuplicateMode(value);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldList.Elements")]
    public TBoldElement? this[int index]
    {
        get => GetElement(index);
        set => SetElement(index, value);
    }
}

/// <summary>A list of members; by default it holds clones of what is added.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberList : TBoldList
{
    private TList? faList;
    private bool FCloneMembers;

    public TBoldMemberList()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override void AllocateData() => faList = new TList();

    [Delphi(PortStatus.Ported)]
    protected override void FreeData()
    {
        if (CloneMembers && faList is not null)
        {
            for (int I = 0; I < faList.Count; I++)
            {
                ((TBoldMember?)faList[I])?.Dispose();
            }
        }

        faList = null;
    }

    /// <summary>Adds (clones of) the members of the source list.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is not TBoldMemberList SourceList)
        {
            throw new EBold(SysUtils.Format(sSourceNotBoldMemberList, GetType().Name, Source?.GetType().Name ?? "nil"));
        }

        for (int I = 0; I < SourceList.Count; I++)
        {
            Add(SourceList[I]);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void AddElement(TBoldElement? Element)
    {
        EnsureContentsCurrent();
        BoldClearLastFailure();
        if (!CanInsert(-1, Element, null))
        {
            BoldRaiseLastFailure(this, "AddElement", "");
        }

        if (Element is not TBoldMember member)
        {
            throw new EBold(SysUtils.Format(sElementNotBoldMember, GetType().Name, "AddElement"));
        }

        Add(member);
    }

    [Delphi(PortStatus.Ported)]
    protected override void InsertElement(int index, TBoldElement? Element)
    {
        EnsureContentsCurrent();
        BoldClearLastFailure();
        if (!CanInsert(index, Element, null))
        {
            BoldRaiseLastFailure(this, "InsertElement", "");
        }

        if (Element is not TBoldMember member)
        {
            throw new EBold(SysUtils.Format(sElementNotBoldMember, GetType().Name, "InsertElement"));
        }

        Insert(index, member);
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetElement(int index)
    {
        EnsureContentsCurrent();
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetElement", "");
        }

        return (TBoldElement?)List[index];
    }

    /// <summary>Replaces the member (with a clone when cloning); an element already in the list is moved there instead.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetElement(int index, TBoldElement? Value)
    {
        EnsureContentsCurrent();
        var member = (TBoldMember?)Value;
        if (CheckReplace(index, member))
        {
            BoldClearLastFailure();
            if (!CanSet(index, Value, null))
            {
                BoldRaiseLastFailure(this, "SetElement", "");
            }

            if (CloneMembers)
            {
                ((TBoldMember?)List[index])?.Dispose();
                List[index] = member!.Clone();
            }
            else
            {
                List[index] = Value;
            }
        }
        else
        {
            Move(IndexOf(member!), index);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetCount()
    {
        EnsureContentsCurrent();
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetCount", "");
        }

        return List.Count;
    }

    [Delphi(PortStatus.Ported)]
    public int IndexOf(TBoldMember Item) => IndexOfElement(Item);

    [Delphi(PortStatus.Ported)]
    public override void RemoveByIndex(int index)
    {
        if (!Mutable)
        {
            MutableError(index.ToString());
        }

        EnsureContentsCurrent();
        var temp = (TBoldMember?)faList![index];
        faList.Delete(index);
        if (CloneMembers)
        {
            temp?.Dispose();
        }

        Changed(beItemDeleted, [index]);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMember GetBoldMember(int index)
    {
        EnsureContentsCurrent();
        return (TBoldMember)GetElement(index)!;
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldMember(int index, TBoldMember Value) => SetElement(index, Value);

    /// <summary>Adds a clone of the member when cloning, else the member itself (unless a duplicate the mode drops).</summary>
    [Delphi(PortStatus.Ported)]
    public void Add(TBoldMember? Item)
    {
        if (CloneMembers)
        {
            List.Add(Item!.Clone());
        }
        else if (CheckAdd(Item))
        {
            List.Add(Item);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Move(int CurIndex, int NewIndex)
    {
        if (!Mutable)
        {
            MutableError("MoveElementInList");
        }

        EnsureContentsCurrent();
        List.Move(CurIndex, NewIndex);
        Changed(beOrderChanged, []);
    }

    [Delphi(PortStatus.Ported)]
    internal void InternalAddWithoutCloning(TBoldMember Item) => List.Add(Item);

    [Delphi(PortStatus.Ported)]
    public void Insert(int index, TBoldMember? Item)
    {
        if (Item is not null)
        {
            if (CloneMembers)
            {
                List.Insert(index, Item.Clone());
            }
            else if (CheckInsert(index, Item))
            {
                List.Insert(index, Item);
            }
            else
            {
                Move(IndexOf(Item), index);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() =>
        throw new EBoldFeatureNotImplementedYet(SysUtils.Format(sMethodNotImplemented, GetType().Name, "GetStreamName"));

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => Count.ToString();

    [Delphi(PortStatus.Ported)]
    protected override int IndexOfElement(TBoldElement? Item) => Item is TBoldMember ? List.IndexOf(Item) : -1;

    [Delphi(PortStatus.Ported)]
    protected override bool IncludesElement(TBoldElement? Item) => IndexOfElement(Item) != -1;

    [Delphi(PortStatus.Ported)]
    private void SetCloneMembers(bool Value)
    {
        if (Count != 0 && FCloneMembers != Value)
        {
            throw new EBold(SysUtils.Format(sOnlyAllowedOnEmptyLists, GetType().Name));
        }

        FCloneMembers = Value;
    }

    [Delphi(PortStatus.Ported)]
    private bool CheckAdd(TBoldMember? NewMember)
    {
        if (NewMember is null)
        {
            return false;
        }

        return !Includes(NewMember) || DuplicateControl();
    }

    [Delphi(PortStatus.Ported)]
    private bool CheckInsert(int index, TBoldMember NewMember) => !Includes(NewMember) || DuplicateControl();

    [Delphi(PortStatus.Ported)]
    private bool CheckReplace(int index, TBoldMember? NewMember)
    {
        if (NewMember is null || !Includes(NewMember))
        {
            return true;
        }

        return IndexOf(NewMember) != index && DuplicateControl();
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        DuplicateMode = TBoldListDupMode.bldmAllow;
        FCloneMembers = true;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement CreateNew()
    {
        EnsureCanCreateNew();
        return TBoldMemberFactory.CreateMemberFromBoldType(((TBoldListTypeInfo)BoldType!).ListElementTypeInfo);
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetCanCreateNew() => BoldType is TBoldListTypeInfo { ListElementTypeInfo: TBoldAttributeTypeInfo };

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement InternalAddNew()
    {
        var result = (TBoldMember)CreateNew();
        InternalAddWithoutCloning(result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override void InsertNew(int index)
    {
        var Elem = (TBoldMember)CreateNew();
        try
        {
            Insert(index, Elem);
            if (CloneMembers)
            {
                Elem.Dispose();
            }
        }
        catch
        {
            Elem.Dispose();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => throw new EBoldInternal("No proxy for TBoldMemberList yet");

    [Delphi(PortStatus.Ported)]
    protected override void InternalClear()
    {
        for (int I = Count - 1; I >= 0; I--)
        {
            RemoveByIndex(I);
        }
    }

    [Delphi(PortStatus.Ported)] private TList List => faList!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberList.BoldMembers")]
    public new TBoldMember this[int index]
    {
        get => GetBoldMember(index);
        set => SetBoldMember(index, value);
    }

    [Delphi(PortStatus.Ported)]
    public bool CloneMembers
    {
        get => FCloneMembers;
        set => SetCloneMembers(value);
    }
}

/// <summary>
/// A list of objects, held as locators: a multi link, a class list (all objects of a class) or a free-standing list. The
/// controller that fits decides how elements are kept and linked.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectList : TBoldList
{
    public TBoldObjectList()
    {
    }

    protected TBoldObjectList(Action<TBoldMember> Construction)
        : base(Construction)
    {
    }

    /// <summary>A free-standing list of objects of the class (in the default system).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectList.CreateTypedList")]
    public TBoldObjectList(TBoldObjectClass ObjectClass)
        : base(m => ((TBoldObjectList)m).CreateTypedList(ObjectClass))
    {
    }

    private void CreateTypedList(TBoldObjectClass ObjectClass)
    {
        TBoldSystem aSystem = TBoldSystem.DefaultSystem() ?? throw new EBold(SysUtils.Format(sCannotFindSystem, GetType().Name));
        TBoldClassTypeInfo ClassTypeInfo = aSystem.BoldSystemTypeInfo.TopSortedClasses.ItemsByObjectClass(ObjectClass) ??
            throw new EBold(SysUtils.Format(sClassIsNotBusinessClass, GetType().Name, ObjectClass.Name));
        InitializeMember(null, aSystem.BoldSystemTypeInfo.ListTypeInfoByElement(ClassTypeInfo));
    }

    /// <summary>The class list (all objects of the class in the system) of the list class; used by the system.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the list class, as Delphi calls it on a class reference.")]
    public static TBoldObjectList InternalCreateClassList(TBoldObjectListClass ListClass, TBoldSystem System, TBoldListTypeInfo ListTypeInfo) =>
        (TBoldObjectList)CreateInstance(ListClass, m =>
        {
            var list = (TBoldObjectList)m;
            list.InitializeMember(System, ListTypeInfo);
            list.DuplicateMode = TBoldListDupMode.bldmMerge;
            list.InitializeStateToInvalid();
        });

    [Delphi(PortStatus.Ported)]
    protected override void AllocateData()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement InternalAddNew()
    {
        var Obj = (TBoldObject)CreateNew();
        Obj.BoldSystem.StartTransaction();
        try
        {
            Add(Obj);
            Obj.BoldSystem.CommitTransaction();
        }
        catch
        {
            Obj.BoldSystem.RollbackTransaction();
            throw;
        }

        return Obj;
    }

    [Delphi(PortStatus.Ported)]
    public override void InsertNew(int index)
    {
        var Obj = (TBoldObject)CreateNew();
        Obj.BoldSystem.StartTransaction();
        try
        {
            Insert(index, Obj);
            Obj.BoldSystem.CommitTransaction();
        }
        catch
        {
            Obj.BoldSystem.RollbackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void FreeData()
    {
    }

    [Delphi(PortStatus.Ported)]
    public override bool ObserverMayModify(object? Observer) =>
        base.ObserverMayModify(Observer) && (BoldRoleRTInfo is null || BoldRoleRTInfo.RoleType == TBoldRoleType.rtRole);

    [Delphi(PortStatus.Ported)]
    protected override void AddElement(TBoldElement? Element)
    {
        if (Element is not null)
        {
            if (Element is not TBoldObject obj)
            {
                throw new EBold(SysUtils.Format(sElementNotBoldObject, GetType().Name, "AddElement"));
            }

            AddLocator(obj.BoldObjectLocator);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void AddList(TBoldList List)
    {
        if (!Mutable)
        {
            MutableError("AddListToList");
        }

        if (List is not TBoldObjectList ObjectList)
        {
            throw new EBold(SysUtils.Format(sListNotObjectList, GetType().Name));
        }

        for (int I = 0; I < ObjectList.Count; I++)
        {
            AddLocator(ObjectList.Locators(I));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void InsertElement(int index, TBoldElement? Element)
    {
        if (Element is not null)
        {
            if (Element is not TBoldObject obj)
            {
                throw new EBold(SysUtils.Format(sElementNotBoldObject, GetType().Name, "InsertElement"));
            }

            InsertLocator(index, obj.BoldObjectLocator);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElement? GetElement(int index)
    {
        TBoldObjectLocator? aLocator = GetLocator(index);
        if (aLocator is null)
        {
            return null;
        }

        aLocator.EnsureBoldObject();
        return aLocator.BoldObject;
    }

    [Delphi(PortStatus.Ported)]
    protected override void SetElement(int index, TBoldElement? Value)
    {
        switch (Value)
        {
            case TBoldObject obj:
                SetLocator(index, obj.BoldObjectLocator);
                break;
            case null:
                throw new EBold(SysUtils.Format(sElementIsNil, GetType().Name));
            default:
                throw new EBold(SysUtils.Format(sElementNotBoldObject, GetType().Name, "SetElement"));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override int IndexOfElement(TBoldElement? Item) => Item is TBoldObject obj ? IndexOfLocator(obj.BoldObjectLocator) : -1;

    [Delphi(PortStatus.Ported)]
    protected override bool IncludesElement(TBoldElement? Item) => Item is TBoldObject obj && LocatorInList(obj.BoldObjectLocator);

    [Delphi(PortStatus.Ported)]
    protected override int GetCount()
    {
        EnsureContentsCurrent();
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetCount", "");
        }

        return ListController!.Count;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator? GetLocator(int index)
    {
        EnsureContentsCurrent();
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetLocator", "");
        }

        return ObjectListController.GetLocator(index);
    }

    /// <summary>Clears the list and fills it with the source list's locators.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is not TBoldObjectList SourceList)
        {
            throw new EBold(SysUtils.Format(sSourceNotObjectList, GetType().Name, Source?.GetType().Name ?? "nil"));
        }

        Clear();
        for (int i = SourceList.Count - 1; i >= 0; i--)
        {
            InsertLocator(0, SourceList.Locators(i));
        }
    }

    /// <summary>Replaces the locator at the index; one already in the list is moved there instead.</summary>
    [Delphi(PortStatus.Ported, Note = "Public: the setter of Locators, an indexed property C# has no form for.")]
    public void SetLocator(int index, TBoldObjectLocator? NewLocator)
    {
        EnsureContentsCurrent();
        if (CheckReplace(index, NewLocator))
        {
            BoldClearLastFailure();
            if (!CanSetLocator(index, NewLocator, null))
            {
                BoldRaiseLastFailure(this, "SetLocator", "");
            }

            ObjectListController.SetLocator(index, NewLocator!);
        }
        else
        {
            Move(IndexOfLocator(NewLocator), index);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObject? GetBoldObject(int index) => (TBoldObject?)GetElement(index);

    [Delphi(PortStatus.Ported)]
    private void SetBoldObject(int index, TBoldObject? NewObject) => SetElement(index, NewObject);

    /// <summary>Adds the locator, unless nil, of another system, or a duplicate the mode drops.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddLocator(TBoldObjectLocator? NewLocator)
    {
        EnsureContentsCurrent();
        if (CheckAdd(NewLocator))
        {
            BoldClearLastFailure();
            if (!CanInsertLocator(-1, NewLocator, null))
            {
                BoldRaiseLastFailure(this, "AddLocator", "");
            }

            ObjectListController.AddLocator(NewLocator!);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldObject? BoldObject) => AddElement(BoldObject);

    [Delphi(PortStatus.Ported)]
    public override void Move(int CurIndex, int NewIndex)
    {
        if (!Mutable)
        {
            MutableError("MoveElementInList");
        }

        BoldClearLastFailure();
        if (!CanMove(CurIndex, NewIndex, null))
        {
            BoldRaiseLastFailure(this, "Move", "");
        }

        EnsureContentsCurrent();
        ListController!.Move(CurIndex, NewIndex);
    }

    /// <summary>Inserts the locator; one already in the list is moved there instead.</summary>
    [Delphi(PortStatus.Ported)]
    public void InsertLocator(int index, TBoldObjectLocator? Locator)
    {
        EnsureContentsCurrent();
        if (Locator is not null)
        {
            if (CheckInsert(index, Locator))
            {
                BoldClearLastFailure();
                if (!CanInsertLocator(index, Locator, null))
                {
                    BoldRaiseLastFailure(this, "InsertLocator", "");
                }

                ObjectListController.InsertLocator(index, Locator);
            }
            else
            {
                Move(IndexOfLocator(Locator), index);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Insert(int index, TBoldObject? BoldObject) => InsertElement(index, BoldObject);

    [Delphi(PortStatus.Ported)]
    public int IndexOf(TBoldObject? BoldObject) => IndexOfElement(BoldObject);

    [Delphi(PortStatus.Ported)]
    private void InternalRemoveByIndex(int index)
    {
        EnsureContentsCurrent();
        ListController!.RemoveByIndex(index);
    }

    [Delphi(PortStatus.Ported)]
    public override void RemoveByIndex(int index)
    {
        if (!Mutable)
        {
            MutableError(index.ToString());
        }

        BoldClearLastFailure();
        if (!CanRemove(index, null))
        {
            BoldRaiseLastFailure(this, "RemoveByIndex", "");
        }

        InternalRemoveByIndex(index);
    }

    [Delphi(PortStatus.Ported)]
    public int IndexOfLocator(TBoldObjectLocator? Locator)
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "IndexOfLocator", "");
        }

        EnsureContentsCurrent();
        return ObjectListController.IndexOfLocator(Locator);
    }

    [Delphi(PortStatus.Ported)]
    public bool Includes(TBoldObject? BoldObject) => IncludesElement(BoldObject);

    [Delphi(PortStatus.Ported)]
    public bool LocatorInList(TBoldObjectLocator? NewLocator)
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "LocatorInList", "");
        }

        EnsureContentsCurrent();
        return ObjectListController.IncludesLocator(NewLocator);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectIdList CreateObjectIdList()
    {
        var result = new TBoldObjectIdList();
        for (int I = 0; I < Count; I++)
        {
            result.Add(Locators(I)!.BoldObjectID);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void FillFromIDList(TBoldObjectIdList ObjectIdList, TBoldSystem BoldSystem)
    {
        for (int I = 0; I < ObjectIdList.Count; I++)
        {
            AddLocator(BoldSystem.EnsuredLocatorByID(ObjectIdList[I]));
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObject? GetByIndex(TBoldMemberList MemberList) => GetByIndexAndSubscribe(MemberList, null);

    /// <summary>The object whose qualifiers have the values in the list (qualified links).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObject? GetByIndexAndSubscribe(TBoldMemberList MemberList, TBoldSubscriber? Subscriber)
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetByIndex", "");
        }

        return ObjectListController.GetLocatorByQualifiersAndSubscribe(MemberList, Subscriber)?.EnsuredBoldObject;
    }

    /// <summary>True when the locator's class conforms to the list's element class (or could, when not known exactly).</summary>
    [Delphi(PortStatus.Ported)]
    private bool VerifyClass(TBoldObjectLocator? aLocator)
    {
        if (BoldType is not null && aLocator is not null)
        {
            var AllowedClass = (TBoldClassTypeInfo)((TBoldListTypeInfo)BoldType).ListElementTypeInfo!;
            TBoldClassTypeInfo KnownClass = ((TBoldSystemTypeInfo)BoldType.SystemTypeInfo!).TopSortedClasses[aLocator.BoldObjectID.TopSortedIndex];
            bool result;
            if (KnownClass.ConformsTo(AllowedClass))
            {
                result = true;
            }
            else if (aLocator.BoldObjectID.TopSortedIndexExact)
            {
                result = false;
            }
            else
            {
                result = AllowedClass.ConformsTo(KnownClass);
            }

            if (!result)
            {
                SetBoldLastFailureReason(new TBoldFailureReason(sItemNotAllowedInList, [KnownClass.ExpressionName, AllowedClass.ExpressionName], this));
            }

            return result;
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAbstractObjectListController GetObjectListController() => (TBoldAbstractObjectListController)ListController!;

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (ListController!.ProxyInterface(IId, Mode, out Obj))
        {
            return true;
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    internal override void MakeDbCurrent() => ObjectListController.MakeDbCurrent();

    [Delphi(PortStatus.Ported)]
    protected internal override string GetStreamName() => ObjectListController.StreamName;

    [Delphi(PortStatus.Ported)]
    public void EnsureObjects() => EnsureRange(0, Count - 1);

    /// <summary>Fetches, in one go, the objects in the range that are not loaded.</summary>
    [Delphi(PortStatus.Ported)]
    public override void EnsureRange(int FromIndex, int ToIndex)
    {
        static int RestrictRange(int value, int min, int max) => value < min ? min : value > max ? max : value;

        if (Count > 0)
        {
            TBoldSystem? aSystem = null;
            var FetchList = new TBoldObjectList();
            try
            {
                for (int i = RestrictRange(FromIndex, 0, Count - 1); i <= RestrictRange(ToIndex, 0, Count - 1); i++)
                {
                    TBoldObjectLocator Locator = Locators(i)!;
                    if (Locator.BoldObject is null)
                    {
                        aSystem ??= Locator.BoldSystem;
                        FetchList.AddLocator(Locator);
                    }
                }

                aSystem?.SystemPersistenceHandler.FetchList(FetchList);
            }
            finally
            {
                FetchList.Dispose();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool CanInsertLocator(int index, TBoldObjectLocator? Locator, TBoldSubscriber? Subscriber)
    {
        if (Locator is null || (OwnedByObject && Locator.BoldSystem != OwningObject!.BoldSystem))
        {
            return false;
        }

        return VerifyClass(Locator) && SendQuery(bqMayInsert, [index, Locator], Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    public override bool CanInsert(int index, TBoldElement? Element, TBoldSubscriber? Subscriber) =>
        Element is null || (Element is TBoldObject obj && CanInsertLocator(index, obj.BoldObjectLocator, Subscriber));

    [Delphi(PortStatus.Ported)]
    public override bool CanSet(int index, TBoldElement? Element, TBoldSubscriber? Subscriber) =>
        Element is TBoldObject obj && CanSetLocator(index, obj.BoldObjectLocator, Subscriber);

    [Delphi(PortStatus.Ported)]
    public bool CanSetLocator(int index, TBoldObjectLocator? Locator, TBoldSubscriber? Subscriber)
    {
        if (Locator is null || (OwnedByObject && Locator.BoldSystem != OwningObject!.BoldSystem))
        {
            return false;
        }

        return VerifyClass(Locator) && SendQuery(bqMayReplace, [index, Locator], Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    private bool CheckAdd(TBoldObjectLocator? NewLocator)
    {
        if (NewLocator is null || (OwningObject is not null && OwningObject.BoldSystem != NewLocator.BoldSystem))
        {
            return false;
        }

        return !ObjectListController.IncludesLocator(NewLocator) || DuplicateControl();
    }

    [Delphi(PortStatus.Ported)]
    private bool CheckReplace(int index, TBoldObjectLocator? NewLocator)
    {
        if (NewLocator is null || (OwningObject is not null && OwningObject.BoldSystem != NewLocator.BoldSystem))
        {
            return false;
        }

        if (!ObjectListController.IncludesLocator(NewLocator))
        {
            return true;
        }

        return IndexOfLocator(NewLocator) != index && DuplicateControl();
    }

    [Delphi(PortStatus.Ported)]
    private bool CheckInsert(int index, TBoldObjectLocator NewLocator)
    {
        if (OwningObject is not null && OwningObject.BoldSystem != NewLocator.BoldSystem)
        {
            return false;
        }

        return !ObjectListController.IncludesLocator(NewLocator) || DuplicateControl();
    }

    /// <summary>Also creates the controller that fits: class list, free-standing list, derived, link object, direct or indirect multi link.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        DuplicateMode = TBoldListDupMode.bldmMerge;
        if (BoldMemberRTInfo is null)
        {
            if (AOwningElement is TBoldSystem)
            {
                ListController = new TBoldClassListController(this);
            }
            else
            {
                ListController = new TBoldObjectListController(this);
                SubscribeToObjectsInList = true;
            }
        }
        else if (BoldMemberRTInfo is TBoldRoleRTInfo role)
        {
            if (role.IsDerived)
            {
                ListController = new TBoldObjectListController(this);
            }
            else if (role.RoleType == TBoldRoleType.rtLinkRole)
            {
                ListController = new TBoldLinkObjectListController(this);
            }
            else if (role.IsIndirect)
            {
                ListController = new TBoldIndirectMultiLinkController(this);
            }
            else
            {
                ListController = new TBoldDirectMultiLinkController(this);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sCannotCreateController, GetType().Name));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldRoleRTInfo? GetBoldRoleRTInfo() => (TBoldRoleRTInfo?)BoldMemberRTInfo;

    [Delphi(PortStatus.Ported)]
    private bool GetSubscribeToObjectsInList() => GetElementFlag(befSubscribeToObjectsInList);

    [Delphi(PortStatus.Ported)]
    private void SetSubscribeToObjectsInList(bool Value)
    {
        if (ListController!.GetType() != typeof(TBoldObjectListController))
        {
            throw new EBold(SysUtils.Format(sCanOnlyChangeForStandAloneLists, GetType().Name, "SetSubscribeToObjectsInList"));
        }

        if (Value != SubscribeToObjectsInList)
        {
            SetElementFlag(befSubscribeToObjectsInList, Value);
            var controller = (TBoldObjectListController)ListController;
            controller.DropSubscriptions();
            controller.Resubscribe();
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetSubscribeToLocatorsInList() => GetElementFlag(befSubscribeToLocatorsInList);

    [Delphi(PortStatus.Ported)]
    private void SetSubscribeToLocatorsInList(bool Value)
    {
        if (ListController!.GetType() != typeof(TBoldObjectListController))
        {
            throw new EBold(SysUtils.Format(sCanOnlyChangeForStandAloneLists, GetType().Name, "SetSubscribeToLocatorsInList"));
        }

        if (Value != SubscribeToLocatorsInList)
        {
            SetElementFlag(befSubscribeToLocatorsInList, Value);
            var controller = (TBoldObjectListController)ListController;
            controller.DropSubscriptions();
            controller.Resubscribe();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => ObjectListController.ProxyClass();

    [Delphi(PortStatus.Ported)]
    public override TBoldMember AtTime(TBoldTimeStampType Time) =>
        ObjectListController.HandlesAtTime() ? ObjectListController.AtTime(Time) : base.AtTime(Time);

    [Delphi(PortStatus.Ported)]
    protected internal override void FreeContent()
    {
        base.FreeContent();
        ObjectListController.FreeContent();
    }

    /// <summary>A derived free-standing list takes the source list's locators directly.</summary>
    [Delphi(PortStatus.Ported)]
    internal override void AssignContentValueFromElement(TBoldElement? source)
    {
        if (source is TBoldObjectList SourceList && ListController is TBoldObjectListController Controller)
        {
            Controller.LocatorList.Clear();
            for (int i = 0; i < SourceList.Count; i++)
            {
                Controller.LocatorList.Add(SourceList.Locators(i));
            }
        }
        else
        {
            base.AssignContentValueFromElement(source);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal override void PrepareClear() => ObjectListController.PrepareClear();

    [Delphi(PortStatus.Ported)]
    protected override void InternalClear() => ObjectListController.Clear();

    /// <summary>The list type with this class's Delphi name in the system the list belongs to (or the default system).</summary>
    [Delphi(PortStatus.Ported)]
    internal override TBoldElementTypeInfo? GetElementTypeInfoForType() => FindASystem()?.BoldSystemTypeInfo.ListTypes.ItemsByDelphiName(GetType().Name);

    [Delphi(PortStatus.Ported)] internal TBoldAbstractObjectListController ObjectListController => GetObjectListController();

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectList.BoldObjects")]
    public new TBoldObject? this[int index]
    {
        get => GetBoldObject(index);
        set => SetBoldObject(index, value);
    }

    [Delphi(PortStatus.Ported)] public TBoldRoleRTInfo? BoldRoleRTInfo => GetBoldRoleRTInfo();
    [Delphi(PortStatus.Ported)] public TBoldObjectLocator? Locators(int index) => GetLocator(index);

    [Delphi(PortStatus.Ported)]
    public bool SubscribeToObjectsInList
    {
        get => GetSubscribeToObjectsInList();
        set => SetSubscribeToObjectsInList(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool SubscribeToLocatorsInList
    {
        get => GetSubscribeToLocatorsInList();
        set => SetSubscribeToLocatorsInList(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool Adjusted
    {
        get => GetElementFlag(befAdjusted);
        set => SetElementFlag(befAdjusted, value);
    }
}
