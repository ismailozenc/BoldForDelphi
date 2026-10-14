// Delphi: Source/Common/Support/BoldIndex.pas
// Port map: CSharp/port/units/BoldIndex.md
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldIndex", Symbol = "PItemEntry", Note = "A reference to TBoldHashIndexItemEntryRec, which is a class here.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldIndex", Symbol = "PPItemEntry", Note = "Bucket heads are array slots; no pointer to pointer is needed.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldIndex", Symbol = "PPItemEntryList", Note = "The bucket array is a .NET array.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldIndex", Symbol = "TPItemEntryList", Note = "The bucket array is a .NET array.")]

namespace Bold;

/// <summary>One entry in a hash bucket chain.</summary>
[Delphi(PortStatus.Ported, Note = "A class instead of a record reached through pointers.")]
public sealed class TBoldHashIndexItemEntryRec
{
    public TBoldHashIndexItemEntryRec? Next;
    public object? Item;
}

[Delphi(PortStatus.Ported, Note = "Unused in Bold; kept for the name.")]
public class TBoldHashIndexBucketEntry : TBoldMemoryManagedObject
{
    public TBoldHashIndexItemEntryRec? Next;
}

/// <summary>Walks the items of an index; dispose it when done so the index can resize again.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldIndexTraverser : TBoldMemoryManagedObject, IDisposable
{
    [Delphi(PortStatus.Ported)]
    protected abstract object? GetItem();

    [Delphi(PortStatus.Ported)]
    protected abstract bool GetEol();

    [Delphi(PortStatus.Ported)]
    public abstract void Next();

    [Delphi(PortStatus.Ported)]
    public object? Item => GetItem();

    [Delphi(PortStatus.Ported)]
    public bool EndOfList => GetEol();

    public virtual void Dispose() => GC.SuppressFinalize(this);
}

/// <summary>
/// An index over the items of a <see cref="TBoldIndexableList"/>. Keys are untyped in Delphi (<c>const Key</c>);
/// here they are objects, and each index knows which key type it takes.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldIndex : TBoldMemoryManagedObject, IDisposable
{
    /// <summary>Releases the index. Subclasses override it where Delphi overrides Destroy.</summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);

    [Delphi(PortStatus.Ported)]
    protected abstract object? GetAny();

    [Delphi(PortStatus.Ported)]
    protected abstract int GetCount();

    [Delphi(PortStatus.Ported)]
    protected abstract bool Match(object Key, object? Item);

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetSupportsTraverser() => false;

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetSupportsNilItems() => false;

    [Delphi(PortStatus.Ported)]
    public abstract void FillEmptyIndex(TBoldIndex BoldIndex);

    [Delphi(PortStatus.Ported)]
    public abstract void Add(object? Item);

    [Delphi(PortStatus.Ported)]
    public abstract void Clear(bool DestroyObjects = false);

    [Delphi(PortStatus.Ported)]
    public abstract object? Find(object Key);

    [Delphi(PortStatus.Ported)]
    public abstract void FindAll(object Key, TList Result);

    [Delphi(PortStatus.Ported)]
    public abstract bool IsCorrectlyIndexed(object? Item);

    /// <summary>Re-indexes an item whose key changed.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void ItemChanged(object? Item)
    {
        if (!IsCorrectlyIndexed(Item))
        {
            RemoveChanged(Item);
            Add(Item);
        }
    }

    [Delphi(PortStatus.Ported)]
    public abstract void Remove(object? Item);

    /// <summary>Removes an item whose key changed since it was added, so it cannot be found by its key.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void RemoveChanged(object? Item);

    [Delphi(PortStatus.Ported)]
    public virtual TBoldIndexTraverser? CreateTraverser() => null;

    [Delphi(PortStatus.Ported)]
    public int Count => GetCount();

    [Delphi(PortStatus.Ported)]
    public object? Any => GetAny();

    [Delphi(PortStatus.Ported)]
    public bool SupportsTraverser => GetSupportsTraverser();

    [Delphi(PortStatus.Ported)]
    public bool SupportsNilItems => GetSupportsNilItems();
}

/// <summary>An index by position: the key is an integer index into the list. It keeps the list order.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIntegerIndex : TBoldIndex
{
    private readonly TList fList = new();

    [Delphi(PortStatus.Ported)]
    public TBoldIntegerIndex()
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldIntegerIndex.Destroy")]
    public override void Dispose()
    {
        fList.Clear();
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    protected override object? GetAny() => Count > 0 ? fList[0] : null;

    [Delphi(PortStatus.Ported)]
    protected override int GetCount() => fList.Count;

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item)
    {
        int index = Convert.ToInt32(Key, System.Globalization.CultureInfo.InvariantCulture);
        return (uint)index < (uint)Count && ReferenceEquals(Item, fList[index]);
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetSupportsNilItems() => true;

    [Delphi(PortStatus.Ported)]
    public override void FillEmptyIndex(TBoldIndex BoldIndex)
    {
        for (int i = 0; i < Count; i++)
        {
            BoldIndex.Add(this[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Add(object? Item) => fList.Add(Item);

    [Delphi(PortStatus.Ported)]
    public override void Clear(bool DestroyObjects = false)
    {
        if (DestroyObjects)
        {
            for (int i = 0; i < Count; i++)
            {
                object? temp = fList[i];
                fList[i] = null;
                (temp as IDisposable)?.Dispose();
            }
        }

        fList.Clear();
    }

    [Delphi(PortStatus.Ported)]
    public override void ItemChanged(object? Item)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override object? Find(object Key)
    {
        int index = Convert.ToInt32(Key, System.Globalization.CultureInfo.InvariantCulture);
        return (uint)index < (uint)Count ? fList[index] : null;
    }

    [Delphi(PortStatus.Ported)]
    public override void FindAll(object Key, TList Result)
    {
        object? res = Find(Key);
        if (res is not null)
        {
            Result.Add(res);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool IsCorrectlyIndexed(object? Item) => true;

    [Delphi(PortStatus.Ported)]
    public override void Remove(object? Item) => fList.Remove(Item);

    [Delphi(PortStatus.Ported)]
    public override void RemoveChanged(object? Item) => fList.Remove(Item);

    [Delphi(PortStatus.Ported)]
    public void Move(int CurIndex, int NewIndex) => fList.Move(CurIndex, NewIndex);

    [Delphi(PortStatus.Ported)]
    public void Sort(TListSortCompare Compare) => fList.Sort(Compare);

    [Delphi(PortStatus.Ported)]
    public void Insert(int Index, object? Item) => fList.Insert(Index, Item);

    [Delphi(PortStatus.Ported)]
    public void Exchange(int Index1, int Index2) => fList.Exchange(Index1, Index2);

    [Delphi(PortStatus.Ported)]
    public void RemoveByIndex(int Index) => fList.Delete(Index);

    [Delphi(PortStatus.Ported)]
    public int IndexOf(object? Item) => fList.IndexOf(Item);

    [Delphi(PortStatus.Ported)]
    public void SetItems(int index, object? Value) => fList[index] = Value;

    [Delphi(PortStatus.Ported, Symbol = "TBoldIntegerIndex.Items")]
    [Delphi(PortStatus.Ported, Symbol = "TBoldIntegerIndex.GetItems")]
    public object? this[int index]
    {
        get => fList[index];
        set => fList[index] = value;
    }
}

/// <summary>Walks a hash index bucket by bucket. The index does not resize while a traverser is alive.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldHashIndexTraverser : TBoldIndexTraverser
{
    private readonly TBoldHashIndex fHashIndex;
    private TBoldHashIndexItemEntryRec? fCurrentItem;
    private TBoldHashIndexItemEntryRec? fNextItem;
    private int fBucketIndex;
    private bool fDisposed;

    [Delphi(PortStatus.Ported)]
    public TBoldHashIndexTraverser(TBoldHashIndex HashIndex)
    {
        fHashIndex = HashIndex;
        fBucketIndex = -1;
        fNextItem = FirstItemOfNextBucket();
        Next();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldHashIndexTraverser.Destroy")]
    public override void Dispose()
    {
        if (!fDisposed)
        {
            fDisposed = true;
            fHashIndex.DecreaseTraverser();
        }

        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private TBoldHashIndexItemEntryRec? FirstItemOfNextBucket()
    {
        TBoldHashIndexItemEntryRec? result = null;
        while (result is null && fBucketIndex < fHashIndex.fBucketCount - 1)
        {
            fBucketIndex++;
            result = fHashIndex.BucketHead(fBucketIndex);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetEol() => fCurrentItem is null;

    [Delphi(PortStatus.Ported)]
    protected override object? GetItem() => fCurrentItem?.Item;

    [Delphi(PortStatus.Ported)]
    public override void Next()
    {
        fCurrentItem = fNextItem;
        fNextItem = fNextItem?.Next ?? FirstItemOfNextBucket();
    }
}

/// <summary>Options of a hash index: resizing state and the number of live traversers.</summary>
[Delphi(PortStatus.Ported)]
public struct TBoldHashIndexOptions
{
    public bool AutoResize;
    public bool PendingResize;
    public bool IsResizing;
    public int TraverserCount;
}

/// <summary>
/// A hash index: items hash into buckets by key. Subclasses say how to hash an item and a key and when a key matches
/// an item. The bucket count follows the item count (about two items per bucket) unless AutoResize is off.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldHashIndex : TBoldIndex
{
    private const int AVERAGEBUCKETLENGTH = 2;
    private const int MINIMUMHASHBUCKETS = 8;

    private TBoldHashIndexOptions fOptions;
    internal int fBucketCount;
    private TBoldHashIndexItemEntryRec?[]? fBucketArray;
    private int fItemCount;
    private int fLastIndexForAny;

    [Delphi(PortStatus.Ported)]
    protected TBoldHashIndex()
    {
        fBucketCount = MinimumBucketCount();
        fOptions.AutoResize = true;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldHashIndex.Destroy")]
    public override void Dispose()
    {
        Clear();
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private TBoldHashIndexItemEntryRec?[] GetBucketArray() => fBucketArray ??= new TBoldHashIndexItemEntryRec?[fBucketCount];

    [Delphi(PortStatus.Ported)]
    private TBoldHashIndexItemEntryRec?[] BucketArray => GetBucketArray();

    internal TBoldHashIndexItemEntryRec? BucketHead(int Index) => BucketArray[Index];

    [Delphi(PortStatus.Ported)]
    private void InsertEntry(TBoldHashIndexItemEntryRec Entry)
    {
        int bucket = BucketIndexForHash(HashItem(Entry.Item));
        Entry.Next = BucketArray[bucket];
        BucketArray[bucket] = Entry;
        fItemCount++;
    }

    [Delphi(PortStatus.Ported)]
    private int MinimumBucketCount() => PrefferedBucketCount() / 2;

    [Delphi(PortStatus.Ported)]
    private int MaximumBucketCount() => PrefferedBucketCount() * 4;

    [Delphi(PortStatus.Ported)]
    private int PrefferedBucketCount() => (MINIMUMHASHBUCKETS << 2) + fItemCount / AVERAGEBUCKETLENGTH;

    [Delphi(PortStatus.Ported)]
    private void SetAutoResize(bool Value)
    {
        fOptions.AutoResize = Value;
        if (Value)
        {
            Resize();
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void DecreaseTraverser()
    {
        fOptions.TraverserCount--;
        if (fOptions.TraverserCount == 0 && fOptions.PendingResize)
        {
            Resize();
        }
    }

    [Delphi(PortStatus.Ported, Note = "Returns the bucket index; the bucket itself is an array slot.")]
    protected int PBucketForHash(uint Hash) => BucketIndexForHash(Hash);

    private int BucketIndexForHash(uint Hash) => (int)(Hash % (uint)fBucketCount);

    [Delphi(PortStatus.Ported, Note = "Returns the bucket's first entry.")]
    protected TBoldHashIndexItemEntryRec? PBucketForIndex(int Index) => BucketArray[Index];

    [Delphi(PortStatus.Ported)]
    protected override int GetCount() => fItemCount;

    [Delphi(PortStatus.Ported)]
    protected abstract uint HashItem(object? Item);

    [Delphi(PortStatus.Ported)]
    protected abstract uint Hash(object Key);

    [Delphi(PortStatus.Ported)]
    protected override object? GetAny()
    {
        if (Count == 0)
        {
            return null;
        }

        int bucketIndex = fLastIndexForAny;
        while (true)
        {
            if (bucketIndex >= fBucketCount)
            {
                bucketIndex = 0;
            }

            var head = BucketArray[bucketIndex];
            if (head is not null)
            {
                fLastIndexForAny = bucketIndex;
                return head.Item;
            }

            bucketIndex++;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetSupportsTraverser() => true;

    [Delphi(PortStatus.Ported)]
    public override void FillEmptyIndex(TBoldIndex BoldIndex)
    {
        for (int i = 0; i < fBucketCount; i++)
        {
            for (var current = BucketArray[i]; current is not null; current = current.Next)
            {
                BoldIndex.Add(current.Item);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Add(object? Item)
    {
        if (Item is not null)
        {
            InsertEntry(new TBoldHashIndexItemEntryRec { Item = Item });
        }

        if (AutoResize)
        {
            Resize();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Clear(bool DestroyObjects = false)
    {
        if (fBucketArray is not null)
        {
            for (int i = 0; i < fBucketCount; i++)
            {
                BoldIndex.ReturnItemEntry(fBucketArray[i], DestroyObjects);
            }
        }

        fBucketArray = null;
        fItemCount = 0;
        fBucketCount = MinimumBucketCount();
    }

    [Delphi(PortStatus.Ported)]
    public override object? Find(object Key)
    {
        var current = BucketArray[BucketIndexForHash(Hash(Key))];
        while (current is not null && !Match(Key, current.Item))
        {
            current = current.Next;
        }

        return current?.Item;
    }

    [Delphi(PortStatus.Ported)]
    public override void FindAll(object Key, TList Result)
    {
        for (var current = BucketArray[BucketIndexForHash(Hash(Key))]; current is not null; current = current.Next)
        {
            if (Match(Key, current.Item))
            {
                Result.Add(current.Item);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool IsCorrectlyIndexed(object? Item)
    {
        var current = BucketArray[BucketIndexForHash(HashItem(Item))];
        while (current is not null && !ReferenceEquals(Item, current.Item))
        {
            current = current.Next;
        }

        return current is not null;
    }

    /// <summary>Removes the item from the bucket its key hashes to now.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Remove(object? Item)
    {
        RemoveFromBucket(BucketIndexForHash(HashItem(Item)), Item);
        if (AutoResize)
        {
            Resize();
        }
    }

    private bool RemoveFromBucket(int bucket, object? Item)
    {
        TBoldHashIndexItemEntryRec? previous = null;
        for (var current = BucketArray[bucket]; current is not null; previous = current, current = current.Next)
        {
            if (ReferenceEquals(current.Item, Item))
            {
                if (previous is null)
                {
                    BucketArray[bucket] = current.Next;
                }
                else
                {
                    previous.Next = current.Next;
                }

                fItemCount--;
                return true;
            }
        }

        return false;
    }

    /// <summary>Removes the item from whichever bucket holds it: its key may have changed since it was added.</summary>
    [Delphi(PortStatus.Ported)]
    public override void RemoveChanged(object? Item)
    {
        for (int i = 0; i < fBucketCount; i++)
        {
            RemoveFromBucket(i, Item);
        }

        if (AutoResize)
        {
            Resize();
        }
    }

    /// <summary>Rehashes into the preferred bucket count when the count is outside its bounds; waits while traversers live.</summary>
    [Delphi(PortStatus.Ported)]
    public void Resize()
    {
        if (fOptions.IsResizing || (fBucketCount >= MinimumBucketCount() && fBucketCount <= MaximumBucketCount()))
        {
            return;
        }

        if (fOptions.TraverserCount > 0)
        {
            fOptions.PendingResize = true;
            return;
        }

        try
        {
            fOptions.IsResizing = true;
            fOptions.PendingResize = false;
            var entries = new List<TBoldHashIndexItemEntryRec>();
            if (fBucketArray is not null)
            {
                foreach (var head in fBucketArray)
                {
                    for (var current = head; current is not null; current = current.Next)
                    {
                        entries.Add(current);
                    }
                }
            }

            fBucketArray = null;
            fBucketCount = PrefferedBucketCount();
            fItemCount = 0;
            foreach (var entry in entries)
            {
                entry.Next = null;
                InsertEntry(entry);
            }
        }
        finally
        {
            fOptions.IsResizing = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override TBoldIndexTraverser CreateTraverser()
    {
        fOptions.TraverserCount++;
        return new TBoldHashIndexTraverser(this);
    }

    [Delphi(PortStatus.Ported)]
    public bool AutoResize
    {
        get => fOptions.AutoResize;
        set => SetAutoResize(value);
    }

    /// <summary>Checks that every item is in the bucket its key hashes to; throws when not.</summary>
    [Delphi(PortStatus.Ported)]
    public void AssertIndex()
    {
        int count = 0;
        for (int i = 0; i < fBucketCount; i++)
        {
            for (var current = BucketArray[i]; current is not null; current = current.Next)
            {
                count++;
                if (!IsCorrectlyIndexed(current.Item))
                {
                    throw new EBoldInternal($"{GetType().Name}.AssertIndex: an item is in the wrong bucket");
                }
            }
        }

        if (count != Count)
        {
            throw new EBoldInternal(SysUtils.Format("%d, %d", Count, count));
        }
    }
}

/// <summary>Unit-level routines of BoldIndex.</summary>
public static partial class BoldIndex
{
    /// <summary>Releases a chain of entries; with <paramref name="DestroyObjects"/> it disposes their items.</summary>
    [Delphi(PortStatus.Ported)]
    public static void ReturnItemEntry(TBoldHashIndexItemEntryRec? Entry, bool DestroyObjects = false)
    {
        for (var current = Entry; current is not null; current = current.Next)
        {
            if (DestroyObjects)
            {
                (current.Item as IDisposable)?.Dispose();
                current.Item = null;
            }
        }
    }
}
