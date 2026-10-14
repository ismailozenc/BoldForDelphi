// Delphi: Source/Common/Core/BoldContainers.pas
// Port map: CSharp/port/units/BoldContainers.md
[assembly: Delphi(PortStatus.Ported, Unit = "BoldContainers", Symbol = "TBoldContainerOptions", Note = "Global using alias of DelphiSet<TBoldContainerOption>, in BoldContainers.Aliases.cs.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldContainers", Symbol = "PByteArray", Note = "The arrays hold .NET references, not raw bytes.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldContainers", Symbol = "TByteArray", Note = "The arrays hold .NET references, not raw bytes.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public class EBoldContainerError(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public enum TBoldContainerOption { bcoDataOwner, bcoThreadSafe }

/// <summary>Compares two array items for <see cref="TBoldArray.Sort"/>.</summary>
[Delphi(PortStatus.Ported)]
public delegate int TBoldArraySortCompare(object? Item1, object? Item2);

/// <summary>Messages of BoldContainers.</summary>
public static partial class BoldContainers
{
    [Delphi(PortStatus.Ported)] public const string ECapacityLessThanCount = "New capacity cannot be less than count";
    [Delphi(PortStatus.Ported)] public const string ECountLessThanCount = "New count cannot be less than count";
    [Delphi(PortStatus.Ported)] public const string EIndexOutOfBounds = "Index out of bounds";
}

/// <summary>A container with options: <c>bcoDataOwner</c> makes it dispose its items when they are removed.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldContainer : TBoldNonRefCountedObject
{
    [Delphi(PortStatus.Ported)]
    protected TBoldContainer(TBoldContainerOptions Options)
    {
        this.Options = Options;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldContainerOptions Options { get; }

    [Delphi(PortStatus.Ported)]
    protected abstract int GetCount();

    [Delphi(PortStatus.Ported)]
    protected abstract void SetCount(int Value);

    [Delphi(PortStatus.Ported)]
    public int Count
    {
        get => GetCount();
        set => SetCount(value);
    }

    [Delphi(PortStatus.Ported)]
    public abstract void Clear();
}

/// <summary>
/// A growable array. Delphi stores raw items of ItemSize bytes; here each slot holds a .NET value (an object, an
/// interface or a boxed integer), so ItemSize is kept for reference only.
/// </summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Replaced, Symbol = "TBoldArray.AddItems", Note = "AddArray and Slice copy slots directly.")]
[Delphi(PortStatus.Replaced, Symbol = "TBoldArray.EnsureCapacity", Note = "List<T> grows by itself.")]
[Delphi(PortStatus.Replaced, Symbol = "TBoldArray.MoveItems", Note = "List<T> moves the slots.")]
public abstract class TBoldArray : TBoldContainer, IDisposable
{
    private readonly List<object?> FArray = [];

    [Delphi(PortStatus.Ported)]
    protected TBoldArray(int InitialCapacity, TBoldContainerOptions Options)
        : base(Options)
    {
        SetCapacity(InitialCapacity);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldArray.Destroy")]
    public void Dispose()
    {
        Clear();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public int Capacity
    {
        get => GetCapacity();
        set => SetCapacity(value);
    }

    [Delphi(PortStatus.Ported)]
    public int ItemSize => GetItemSize();

    [Delphi(PortStatus.Ported)]
    protected abstract int GetItemSize();

    [Delphi(PortStatus.Ported)]
    private int GetCapacity() => FArray.Capacity;

    [Delphi(PortStatus.Ported)]
    private void SetCapacity(int Value)
    {
        if (Value < FArray.Count)
        {
            throw new EBoldContainerError(BoldContainers.ECapacityLessThanCount);
        }

        FArray.Capacity = Value;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual int GetGrowDelta() => FArray.Capacity >= 32 ? FArray.Capacity / 4 : 8;

    [Delphi(PortStatus.Ported)]
    protected override int GetCount() => FArray.Count;

    /// <summary>Grows with empty slots, or shrinks without disposing, as Delphi does.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetCount(int Value)
    {
        if (Value < 0)
        {
            throw new EBoldContainerError(BoldContainers.ECountLessThanCount);
        }

        if (Value < FArray.Count)
        {
            FArray.RemoveRange(Value, FArray.Count - Value);
        }
        else
        {
            while (FArray.Count < Value)
            {
                FArray.Add(EmptyItem);
            }
        }
    }

    /// <summary>The value of a new or cleared slot: null, or 0 for integer arrays.</summary>
    protected virtual object? EmptyItem => null;

    private void CheckIndex(int Index)
    {
        if (Index < 0 || Index >= FArray.Count)
        {
            throw new EBoldContainerError(BoldContainers.EIndexOutOfBounds);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected int Add(object? Item)
    {
        FArray.Add(Item);
        return FArray.Count - 1;
    }

    [Delphi(PortStatus.Ported)]
    protected int AddArray(TBoldArray BoldArray)
    {
        FArray.AddRange(BoldArray.FArray);
        return BoldArray.Count;
    }

    /// <summary>Releases the item in a slot when the array owns its data.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void Dispose(int Index)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected object? Get(int Index)
    {
        CheckIndex(Index);
        return FArray[Index];
    }

    [Delphi(PortStatus.Ported, Note = "Compares references (or values for integers) where Delphi compares the item bytes.")]
    protected int IndexOf(object? Item)
    {
        for (int i = 0; i < FArray.Count; i++)
        {
            if (SameItem(FArray[i], Item))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool SameItem(object? a, object? b) => ReferenceEquals(a, b) || (a is ValueType && Equals(a, b));

    [Delphi(PortStatus.Ported)]
    protected void Insert(int Index, object? Item)
    {
        if (Index < 0 || Index > FArray.Count)
        {
            throw new EBoldContainerError(BoldContainers.EIndexOutOfBounds);
        }

        FArray.Insert(Index, Item);
    }

    [Delphi(PortStatus.Ported)]
    protected void Put(int Index, object? Item)
    {
        CheckIndex(Index);
        if (Options.Contains(TBoldContainerOption.bcoDataOwner))
        {
            Dispose(Index);
        }

        FArray[Index] = Item;
    }

    [Delphi(PortStatus.Ported)]
    protected int Remove(object? Item)
    {
        int result = IndexOf(Item);
        if (result != -1)
        {
            Delete(result);
        }

        return result;
    }

    /// <summary>Empties the slot of the item, keeping the slot, and returns its index (or -1).</summary>
    [Delphi(PortStatus.Ported)]
    protected int RemoveWithNil(object? Item)
    {
        int result = IndexOf(Item);
        if (result != -1)
        {
            if (Options.Contains(TBoldContainerOption.bcoDataOwner))
            {
                Dispose(result);
            }

            FArray[result] = EmptyItem;
        }

        return result;
    }

    /// <summary>Appends <paramref name="NumItems"/> items from <paramref name="StartIndex"/> to <paramref name="BoldArray"/>.</summary>
    [Delphi(PortStatus.Ported)]
    protected void Slice(int StartIndex, int NumItems, TBoldArray BoldArray)
    {
        CheckIndex(StartIndex);
        if (NumItems > 0)
        {
            if (StartIndex + NumItems > FArray.Count)
            {
                throw new EBoldContainerError(BoldContainers.EIndexOutOfBounds);
            }

            BoldArray.FArray.AddRange(FArray.GetRange(StartIndex, NumItems));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Clear()
    {
        if (Options.Contains(TBoldContainerOption.bcoDataOwner))
        {
            for (int i = 0; i < FArray.Count; i++)
            {
                Dispose(i);
            }
        }

        FArray.Clear();
        FArray.Capacity = 0;
    }

    [Delphi(PortStatus.Ported)]
    public void Delete(int Index)
    {
        CheckIndex(Index);
        if (Options.Contains(TBoldContainerOption.bcoDataOwner))
        {
            Dispose(Index);
        }

        FArray.RemoveAt(Index);
    }

    /// <summary>Deletes the items from <paramref name="FromIndex"/> to <paramref name="ToIndex"/>, both included.</summary>
    [Delphi(PortStatus.Ported, Note = "The Delphi version moves the wrong number of items after the range; this removes exactly the range.")]
    public void DeleteRange(int FromIndex, int ToIndex)
    {
        CheckIndex(FromIndex);
        CheckIndex(ToIndex);
        if (Options.Contains(TBoldContainerOption.bcoDataOwner))
        {
            for (int i = FromIndex; i <= ToIndex; i++)
            {
                Dispose(i);
            }
        }

        if (ToIndex >= FromIndex)
        {
            FArray.RemoveRange(FromIndex, ToIndex - FromIndex + 1);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Exchange(int Index1, int Index2)
    {
        CheckIndex(Index1);
        CheckIndex(Index2);
        (FArray[Index1], FArray[Index2]) = (FArray[Index2], FArray[Index1]);
    }

    [Delphi(PortStatus.Ported)]
    public void Move(int FromIndex, int ToIndex)
    {
        if (FromIndex != ToIndex)
        {
            object? item = Get(FromIndex);
            FArray.RemoveAt(FromIndex);
            Insert(ToIndex, item);
        }
    }

    /// <summary>Shrinks the capacity to the count.</summary>
    [Delphi(PortStatus.Ported)]
    public void Pack() => FArray.Capacity = FArray.Count;

    [Delphi(PortStatus.Ported, Note = "Delphi's quicksort is not stable; this sort keeps equal items in order.")]
    public void Sort(TBoldArraySortCompare Compare)
    {
        var items = FArray.Select((item, index) => (item, index)).ToList();
        items.Sort((a, b) =>
        {
            int r = Compare(a.item, b.item);
            return r != 0 ? r : a.index.CompareTo(b.index);
        });
        for (int i = 0; i < items.Count; i++)
        {
            FArray[i] = items[i].item;
        }
    }
}

/// <summary>An array of untyped references (Delphi pointers).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPointerArray(int InitialCapacity, TBoldContainerOptions Options) : TBoldArray(InitialCapacity, Options)
{
    [Delphi(PortStatus.Ported)]
    protected override int GetItemSize() => IntPtr.Size;

    [Delphi(PortStatus.Ported)]
    public new int Add(object? Item) => base.Add(Item);

    [Delphi(PortStatus.Ported)]
    public new int IndexOf(object? Item) => base.IndexOf(Item);

    [Delphi(PortStatus.Ported)]
    public new void Insert(int Index, object? Item) => base.Insert(Index, Item);

    [Delphi(PortStatus.Ported)]
    public new int Remove(object? Item) => base.Remove(Item);

    [Delphi(PortStatus.Ported)]
    public new int RemoveWithNil(object? Item) => base.RemoveWithNil(Item);

    [Delphi(PortStatus.Ported, Symbol = "TBoldPointerArray.Items")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldPointerArray.Get")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldPointerArray.Put")]
    public object? this[int Index]
    {
        get => base.Get(Index);
        set => base.Put(Index, value);
    }
}

/// <summary>An array of objects; with bcoDataOwner it disposes the objects it removes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectArray(int InitialCapacity, TBoldContainerOptions Options) : TBoldArray(InitialCapacity, Options)
{
    [Delphi(PortStatus.Ported)]
    protected override int GetItemSize() => IntPtr.Size;

    /// <summary>Frees the object in the slot: disposes it when it is IDisposable.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Dispose(int Index) => (base.Get(Index) as IDisposable)?.Dispose();

    [Delphi(PortStatus.Ported)]
    public new int Add(object? Item) => base.Add(Item);

    [Delphi(PortStatus.Ported)]
    public new int IndexOf(object? Item) => base.IndexOf(Item);

    [Delphi(PortStatus.Ported)]
    public new void Insert(int Index, object? Item) => base.Insert(Index, Item);

    [Delphi(PortStatus.Ported)]
    public new int Remove(object? Item) => base.Remove(Item);

    [Delphi(PortStatus.Ported)]
    public new int RemoveWithNil(object? Item) => base.RemoveWithNil(Item);

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectArray.Items")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectArray.Get")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectArray.Put")]
    public object? this[int Index]
    {
        get => base.Get(Index);
        set => base.Put(Index, value);
    }
}

/// <summary>An array of interface references; Delphi's reference counting has no .NET counterpart.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldInterfaceArray(int InitialCapacity, TBoldContainerOptions Options) : TBoldArray(InitialCapacity, Options)
{
    [Delphi(PortStatus.Ported)]
    protected override int GetItemSize() => IntPtr.Size;

    [Delphi(PortStatus.Replaced, Note = "Releasing an interface reference needs nothing in .NET.")]
    protected override void Dispose(int Index)
    {
    }

    [Delphi(PortStatus.Ported)]
    public new int Add(object? Item) => base.Add(Item);

    [Delphi(PortStatus.Ported)]
    public new int IndexOf(object? Item) => base.IndexOf(Item);

    [Delphi(PortStatus.Ported)]
    public new void Insert(int Index, object? Item) => base.Insert(Index, Item);

    [Delphi(PortStatus.Ported)]
    public new int Remove(object? Item) => base.Remove(Item);

    [Delphi(PortStatus.Ported)]
    public new int RemoveWithNil(object? Item) => base.RemoveWithNil(Item);

    [Delphi(PortStatus.Ported, Symbol = "TBoldInterfaceArray.Items")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldInterfaceArray.Get")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldInterfaceArray.Put")]
    public object? this[int Index]
    {
        get => base.Get(Index);
        set => base.Put(Index, value);
    }
}

/// <summary>An array of integers.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIntegerArray(int InitialCapacity, TBoldContainerOptions Options) : TBoldArray(InitialCapacity, Options)
{
    protected override object? EmptyItem => 0;

    [Delphi(PortStatus.Ported)]
    protected override int GetItemSize() => sizeof(int);

    [Delphi(PortStatus.Ported)]
    protected override void Dispose(int Index)
    {
    }

    [Delphi(PortStatus.Ported)]
    public int Add(int Item) => base.Add(Item);

    [Delphi(PortStatus.Ported, Note = "The Delphi version stops at the first item that differs (= instead of <>); this finds the item.")]
    public int IndexOf(int Item) => base.IndexOf(Item);

    [Delphi(PortStatus.Ported)]
    public void Insert(int Index, int Item) => base.Insert(Index, Item);

    [Delphi(PortStatus.Ported)]
    public int Remove(int Item) => base.Remove(Item);

    [Delphi(PortStatus.Ported, Symbol = "TBoldIntegerArray.Items")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldIntegerArray.Get")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldIntegerArray.Put")]
    public int this[int Index]
    {
        get => (int)base.Get(Index)!;
        set => base.Put(Index, value);
    }
}
