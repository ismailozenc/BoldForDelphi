// Delphi: Source/Common/Support/BoldIndexableList.pas
// Port map: CSharp/port/units/BoldIndexableList.md

using System.Diagnostics;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldIndexableList", Symbol = "TBoldIndexableListOptionsSet", Note = "Global using alias of DelphiSet<TBoldIndexableListOptions>, in BoldIndexableList.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldIndexableList", Symbol = "TBoldIndexableListTraverserClass", Note = "Global using alias of System.Type, in BoldIndexableList.Aliases.cs; the type must have a constructor taking a TBoldIndexTraverser.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldIndexableListOptions { iloOwnsEntries, iloDestroying, iloKnowsSupportsNil, iloSupportsNil }

/// <summary>Walks the items of an indexable list through one of its indexes, counting the position.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIndexableListTraverser : TBoldMemoryManagedObject, IDisposable
{
    private TBoldIndexTraverser? fIndexTraverser;
    private int fCurrentIndex;

    [Delphi(PortStatus.Ported)]
    public TBoldIndexableListTraverser(TBoldIndexTraverser IndexTraverser)
    {
        fIndexTraverser = IndexTraverser;
        fCurrentIndex = 0;
    }

    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        fIndexTraverser?.Dispose();
        fIndexTraverser = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    protected object? GetItem() => fIndexTraverser!.Item;

    [Delphi(PortStatus.Ported)]
    protected bool GetEol() => fIndexTraverser!.EndOfList;

    [Delphi(PortStatus.Ported)]
    public void Next()
    {
        fIndexTraverser!.Next();
        fCurrentIndex++;
    }

    [Delphi(PortStatus.Ported)]
    public object? Item => GetItem();

    [Delphi(PortStatus.Ported)]
    public bool EndOfList => GetEol();

    [Delphi(PortStatus.Ported)]
    public int CurrentIndex => fCurrentIndex;
}

/// <summary>
/// A collection kept in one or more indexes at once (hash indexes by key, an integer index for order). Every add and
/// remove goes to all indexes. When the list owns its entries it disposes them on removal.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldUnOrderedIndexableList : TBoldNonRefCountedObject, IDisposable
{
    private const int DEFAULTINDEXCAPACITY = 1;

    private TList? fIndexes;
    private TBoldIndexableListOptionsSet fOptions;

    [Delphi(PortStatus.Ported)]
    public TBoldUnOrderedIndexableList()
    {
        OwnsEntries = true;
        SupportsNil = false;
        KnowsSupportsNil = true;
    }

    /// <summary>Frees the indexes; when the list owns its entries the last index disposes them.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        if (fOptions.Contains(TBoldIndexableListOptions.iloDestroying))
        {
            return;
        }

        fOptions += TBoldIndexableListOptions.iloDestroying;
        int indicesToGo = AssignedIndexCount;
        for (int i = IndexCount - 1; i >= 0; i--)
        {
            TBoldIndex? boldIndex = GetIndex(i);
            if (boldIndex is not null)
            {
                RemoveAndFreeIndex(ref boldIndex, OwnsEntries && indicesToGo == 1);
                indicesToGo--;
            }
        }

        fIndexes = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private int GetCount()
    {
        if (IndexCount > 0)
        {
            return SupportsNil ? FirstNilSupportingIndex()!.Count : GetIndex(0)!.Count;
        }

        return 0;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldIndex? GetIndex(int Index) => fIndexes is null ? null : (TBoldIndex?)fIndexes[Index];

    [Delphi(PortStatus.Ported)]
    private int GetIndexCount() => fIndexes?.Count ?? 0;

    /// <summary>The setter of Indexes: puts an empty index at position <paramref name="I"/> and fills it.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SetIndex(int I, TBoldIndex Value)
    {
        if (fIndexes is not null)
        {
            if (Value.Count != 0)
            {
                throw new EBold(SysUtils.Format(BoldCommonConst.sCannotSetNonEmptyIndex, GetType().Name, Value.GetType().Name));
            }

            fIndexes[I] = Value;
            if (Value.SupportsNilItems)
            {
                SupportsNil = true;
                KnowsSupportsNil = true;
            }

            if (AssignedIndexCount > 1)
            {
                FirstAssignedIndex()!.FillEmptyIndex(Value);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private object? GetAny() => IndexCount > 0 ? FirstAssignedIndex()!.Any : null;

    [Delphi(PortStatus.Ported)]
    private bool GetOwnsEntries() => fOptions.Contains(TBoldIndexableListOptions.iloOwnsEntries);

    [Delphi(PortStatus.Ported)]
    private void SetOwnsEntries(bool Value) => fOptions = Value
        ? fOptions + TBoldIndexableListOptions.iloOwnsEntries
        : fOptions - TBoldIndexableListOptions.iloOwnsEntries;

    [Delphi(PortStatus.Ported)]
    private TBoldIndex? FirstNilSupportingIndex()
    {
        for (int i = 0; i < IndexCount; i++)
        {
            TBoldIndex? index = GetIndex(i);
            if (index is not null && index.SupportsNilItems)
            {
                return index;
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldIndex? FirstAssignedIndex()
    {
        for (int i = 0; i < IndexCount; i++)
        {
            TBoldIndex? index = GetIndex(i);
            if (index is not null)
            {
                return index;
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetKnowsSupportsNil() => fOptions.Contains(TBoldIndexableListOptions.iloKnowsSupportsNil);

    [Delphi(PortStatus.Ported)]
    private bool GetSupportsNil()
    {
        if (!KnowsSupportsNil)
        {
            CalculateSupportsNil();
        }

        return fOptions.Contains(TBoldIndexableListOptions.iloSupportsNil);
    }

    [Delphi(PortStatus.Ported)]
    private void SetKnowsSupportsNil(bool Value) => fOptions = Value
        ? fOptions + TBoldIndexableListOptions.iloKnowsSupportsNil
        : fOptions - TBoldIndexableListOptions.iloKnowsSupportsNil;

    [Delphi(PortStatus.Ported)]
    private void SetSupportsNil(bool Value) => fOptions = Value
        ? fOptions + TBoldIndexableListOptions.iloSupportsNil
        : fOptions - TBoldIndexableListOptions.iloSupportsNil;

    [Delphi(PortStatus.Ported)]
    private void CalculateSupportsNil()
    {
        SupportsNil = FirstNilSupportingIndex() is not null;
        KnowsSupportsNil = true;
    }

    [Delphi(PortStatus.Ported)]
    private int GetAssignedIndexCount()
    {
        int result = 0;
        for (int i = 0; i < IndexCount; i++)
        {
            if (GetIndex(i) is not null)
            {
                result++;
            }
        }

        return result;
    }

    /// <summary>Adds an index and fills it with the items already in the list.</summary>
    [Delphi(PortStatus.Ported)]
    protected int AddIndex(TBoldIndex BoldIndex)
    {
        if (fIndexes is null)
        {
            SetIndexCapacity(DEFAULTINDEXCAPACITY);
        }

        int result = fIndexes!.Add(BoldIndex);
        if (IndexCount > 1)
        {
            TBoldIndex sourceIndex = FirstNilSupportingIndex() ?? FirstAssignedIndex()!;
            sourceIndex.FillEmptyIndex(BoldIndex);
        }

        if (BoldIndex.SupportsNilItems)
        {
            SupportsNil = true;
            KnowsSupportsNil = true;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected void AddToAllIndexes(object? Item)
    {
        // Ordered indexes must get nil items too.
        for (int i = 0; i < IndexCount; i++)
        {
            GetIndex(i)?.Add(Item);
        }
    }

    /// <summary>Removes an index and frees it; the last index can only be removed while the list is destroyed.</summary>
    [Delphi(PortStatus.Ported)]
    protected void RemoveAndFreeIndex(ref TBoldIndex? BoldIndex, bool DestroyObjects)
    {
        if (!fOptions.Contains(TBoldIndexableListOptions.iloDestroying) && AssignedIndexCount == 1)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sCannotRemoveLastIndex, GetType().Name));
        }

        if (BoldIndex is not null)
        {
            if (BoldIndex.SupportsNilItems)
            {
                KnowsSupportsNil = false;
            }

            if (DestroyObjects)
            {
                BoldIndex.Clear(DestroyObjects);
            }

            fIndexes![fIndexes.IndexOf(BoldIndex)] = null;
            BoldIndex.Dispose();
            BoldIndex = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void RemoveFromAllIndexes(object? Item)
    {
        if (Item is not null)
        {
            for (int i = 0; i < IndexCount; i++)
            {
                GetIndex(i)?.Remove(Item);
            }
        }
    }

    [Delphi(PortStatus.Ported, Note = "Creates the index list; .NET lists grow as needed so the capacity is a hint.")]
    protected void SetIndexCapacity(int NewCapacity)
    {
        fIndexes ??= new TList();
        fIndexes.Capacity = NewCapacity;
    }

    /// <summary>The traverser type CreateTraverser makes; it needs a constructor taking a TBoldIndexTraverser.</summary>
    [Delphi(PortStatus.Ported, Note = "Returns a Type where Delphi returns a class reference.")]
    protected virtual TBoldIndexableListTraverserClass TraverserClass() => typeof(TBoldIndexableListTraverser);

    [Delphi(PortStatus.Ported)]
    protected int IndexCount => GetIndexCount();

    [Delphi(PortStatus.Ported)]
    protected int AssignedIndexCount => GetAssignedIndexCount();

    [Delphi(PortStatus.Ported, Symbol = "TBoldUnOrderedIndexableList.Indexes")]
    protected TBoldIndex? Indexes(int I) => GetIndex(I);

    [Delphi(PortStatus.Ported)]
    protected bool KnowsSupportsNil
    {
        get => GetKnowsSupportsNil();
        set => SetKnowsSupportsNil(value);
    }

    [Delphi(PortStatus.Ported)]
    protected bool SupportsNil
    {
        get => GetSupportsNil();
        set => SetSupportsNil(value);
    }

    /// <summary>Adds the item to every index; nil only when an index supports nil items.</summary>
    [Delphi(PortStatus.Ported)]
    public void Add(object? Item)
    {
        if (Item is null && !SupportsNil)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sNilPointersNotSupported, GetType().Name));
        }

        AddToAllIndexes(Item);
    }

    /// <summary>Removes every item; when the list owns its entries they are disposed.</summary>
    [Delphi(PortStatus.Ported)]
    public void Clear()
    {
        int indicesToGo = AssignedIndexCount;
        for (int i = 0; i < IndexCount; i++)
        {
            TBoldIndex? index = GetIndex(i);
            if (index is not null)
            {
                index.Clear(OwnsEntries && indicesToGo == 1);
                indicesToGo--;
            }
        }
    }

    /// <summary>Re-indexes an item whose key changed.</summary>
    [Delphi(PortStatus.Ported)]
    public void ItemChanged(object? Item)
    {
        for (int i = 0; i < IndexCount; i++)
        {
            GetIndex(i)?.ItemChanged(Item);
        }
    }

    /// <summary>Removes the item from every index; when the list owns its entries the item is disposed.</summary>
    [Delphi(PortStatus.Ported)]
    public void Remove(object? Item)
    {
        RemoveFromAllIndexes(Item);
        if (OwnsEntries)
        {
            (Item as IDisposable)?.Dispose();
        }
    }

    /// <summary>A traverser over the first index that supports one, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldIndexableListTraverser? CreateTraverser()
    {
        for (int i = 0; i < IndexCount; i++)
        {
            TBoldIndex? index = GetIndex(i);
            if (index is not null && index.SupportsTraverser)
            {
                return (TBoldIndexableListTraverser)Activator.CreateInstance(TraverserClass(), index.CreateTraverser())!;
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported)]
    public int Count => GetCount();

    [Delphi(PortStatus.Ported)]
    public object? Any => GetAny();

    [Delphi(PortStatus.Ported)]
    public bool OwnsEntries
    {
        get => GetOwnsEntries();
        set => SetOwnsEntries(value);
    }
}

/// <summary>An indexable list that keeps its order through an integer index, so items can be reached by position.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIndexableList : TBoldUnOrderedIndexableList
{
    private readonly TBoldIntegerIndex fIndexIndex;

    [Delphi(PortStatus.Ported)]
    public TBoldIndexableList()
    {
        fIndexIndex = new TBoldIntegerIndex();
        AddIndex(fIndexIndex);
    }

    [Delphi(PortStatus.Ported)]
    private object? GetItem(int Index) => fIndexIndex[Index];

    [Delphi(PortStatus.Ported)]
    private void SetItem(int Index, object? value)
    {
        RemoveFromAllNonOrderedIndexes(fIndexIndex[Index]);
        if (OwnsEntries)
        {
            object? temp = fIndexIndex[Index];
            fIndexIndex[Index] = value;
            (temp as IDisposable)?.Dispose();
        }
        else
        {
            fIndexIndex[Index] = value;
        }

        AddToAllNonOrderedIndexes(value);
    }

    [Delphi(PortStatus.Ported)]
    private void AddToAllNonOrderedIndexes(object? item)
    {
        if (item is not null)
        {
            for (int i = 0; i < IndexCount; i++)
            {
                TBoldIndex? index = Indexes(i);
                if (index != fIndexIndex)
                {
                    index!.Add(item);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void RemoveFromAllNonOrderedIndexes(object? item)
    {
        if (item is not null)
        {
            for (int i = 0; i < IndexCount; i++)
            {
                TBoldIndex? index = Indexes(i);
                if (index is not null && index != fIndexIndex)
                {
                    index.Remove(item);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private int GetUnorderedIndexCount() => IndexCount - 1;

    [Delphi(PortStatus.Ported, Symbol = "TBoldIndexableList.Items")]
    protected object? this[int I]
    {
        get => GetItem(I);
        set => SetItem(I, value);
    }

    [Delphi(PortStatus.Ported)]
    protected int UnorderedIndexCount => GetUnorderedIndexCount();

    [Delphi(PortStatus.Ported)]
    public void Move(int CurIndex, int NewIndex) => fIndexIndex.Move(CurIndex, NewIndex);

    /// <summary>Removes the item at <paramref name="Index"/>; when the list owns its entries the item is disposed.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveByIndex(int Index)
    {
        object? item = fIndexIndex[Index];
        RemoveFromAllNonOrderedIndexes(item);
        fIndexIndex.RemoveByIndex(Index);
        if (OwnsEntries)
        {
            (item as IDisposable)?.Dispose();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Sort(TListSortCompare Compare) => fIndexIndex.Sort(Compare);

    [Delphi(PortStatus.Ported)]
    public void Exchange(int Index1, int Index2) => fIndexIndex.Exchange(Index1, Index2);

    [Delphi(PortStatus.Ported)]
    public int IndexOf(object? Item) => fIndexIndex.IndexOf(Item);

    [Delphi(PortStatus.Ported)]
    public void Insert(int Index, object? Item)
    {
        fIndexIndex.Insert(Index, Item);
        AddToAllNonOrderedIndexes(Item);
    }
}

/// <summary>Unit-level routines of BoldIndexableList.</summary>
public static partial class BoldIndexableList
{
    /// <summary>Sets an index position variable, checking it is unset (-1) or already the same.</summary>
    [Delphi(PortStatus.Ported)]
    public static void SetIndexVariable(ref int Index, int GivenPosition)
    {
        Debug.Assert(Index == -1 || Index == GivenPosition, "Erroneous index construction, please debug!");
        Index = GivenPosition;
    }
}
