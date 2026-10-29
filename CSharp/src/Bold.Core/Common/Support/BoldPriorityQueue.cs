// Delphi: Source/Common/Support/BoldPriorityQueue.pas
// Port map: CSharp/port/units/BoldPriorityQueue.md

namespace Bold;

/// <summary>An item of a <see cref="TBoldPriorityQueue"/>: it says whether it goes before another item.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldPriorityQueueItem : TBoldMemoryManagedObject
{
    [Delphi(PortStatus.Ported)]
    public abstract bool HasHigherPriorityThan(TBoldPriorityQueueItem Item);
}

/// <summary>
/// A heap of items, the item with the highest priority at its head; threads can share it. <see cref="OnHeadChanged"/>
/// is called when another item comes to the head (and when an item is added at the head).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldPriorityQueue : TBoldMemoryManagedObject, IDisposable
{
    private List<TBoldPriorityQueueItem>? fHeap;
    private TNotifyEvent? fOnHeadChanged;
    private TBoldLoggableCriticalSection? fLocker;

    [Delphi(PortStatus.Ported)]
    public TBoldPriorityQueue()
    {
        fHeap = [];
        fLocker = new TBoldLoggableCriticalSection("PriQ");
    }

    /// <summary>Frees the heap; the items in it are left alone, as in Delphi.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldPriorityQueue.Destroy")]
    public void Dispose()
    {
        fHeap = null;
        SysUtils.FreeAndNil(ref fLocker);
        GC.SuppressFinalize(this);
    }

    // Positions in the heap are 1-based: the head is at 1, the children of n at 2n and 2n+1.

    [Delphi(PortStatus.Ported)]
    private void SiftUp(int pos)
    {
        int parentpos = Parent(pos);
        if (parentpos == 0)
        {
            fOnHeadChanged?.Invoke(this);
            return;
        }

        if (Items(pos)!.HasHigherPriorityThan(Items(parentpos)!))
        {
            swap(pos, parentpos);
            SiftUp(parentpos);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SiftDown(int pos)
    {
        int childpos = LeftChild(pos);
        if (Items(childpos) is null)
        {
            return;
        }

        if (Items(RightChild(pos)) is { } right && right.HasHigherPriorityThan(Items(LeftChild(pos))!))
        {
            childpos = RightChild(pos);
        }

        if (Items(childpos)!.HasHigherPriorityThan(Items(pos)!))
        {
            swap(pos, childpos);
            SiftDown(childpos);
        }
    }

    [Delphi(PortStatus.Ported)]
    private static int RightChild(int pos) => pos * 2 + 1;

    [Delphi(PortStatus.Ported)]
    private static int LeftChild(int pos) => pos * 2;

    [Delphi(PortStatus.Ported)]
    private static int Parent(int pos) => pos / 2;

    [Delphi(PortStatus.Ported)]
    private void swap(int pos1, int pos2)
    {
        fLocker!.Acquire();
        try
        {
            (fHeap![pos1 - 1], fHeap[pos2 - 1]) = (fHeap[pos2 - 1], fHeap[pos1 - 1]);
        }
        finally
        {
            fLocker.Release();
        }
    }

    [Delphi(PortStatus.Ported)]
    private int GetCount()
    {
        fLocker!.Acquire();
        try
        {
            return fHeap!.Count;
        }
        finally
        {
            fLocker.Release();
        }
    }

    /// <summary>The item at a 1-based heap position; null past the end.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldPriorityQueueItem? GetItems(int pos) => pos <= fHeap!.Count ? fHeap[pos - 1] : null;

    [Delphi(PortStatus.Ported)]
    private void InternalRemoveHead()
    {
        TBoldPriorityQueueItem? oldHead = Head;
        if (oldHead is not null)
        {
            fHeap![0] = fHeap[^1];
            fHeap.RemoveAt(fHeap.Count - 1);
            SiftDown(1);
            if (Head != oldHead)
            {
                fOnHeadChanged?.Invoke(this);
            }
        }
    }

    [Delphi(PortStatus.Ported, Note = "Takes a 1-based position, as GetItems; Delphi's (unused) setter took a 0-based one.")]
    private void SetItems(int pos, TBoldPriorityQueueItem Value)
    {
        fLocker!.Acquire();
        try
        {
            fHeap![pos - 1] = Value;
        }
        finally
        {
            fLocker.Release();
        }
    }

    [Delphi(PortStatus.Ported, Note = "A private method: C# has no indexed properties with a name.")]
    private TBoldPriorityQueueItem? Items(int pos) => GetItems(pos);

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldPriorityQueueItem Item)
    {
        fLocker!.Acquire();
        try
        {
            fHeap!.Add(Item);
            SiftUp(fHeap.Count);
        }
        finally
        {
            fLocker.Release();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveHead()
    {
        fLocker!.Acquire();
        try
        {
            InternalRemoveHead();
        }
        finally
        {
            fLocker.Release();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveAndFreeHead()
    {
        fLocker!.Acquire();
        try
        {
            TBoldPriorityQueueItem? obj = Head;
            InternalRemoveHead();
            (obj as IDisposable)?.Dispose();
        }
        finally
        {
            fLocker.Release();
        }
    }

    /// <summary>Takes the head out of the queue and returns it; null when the queue is empty.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldPriorityQueueItem? ChopHead()
    {
        fLocker!.Acquire();
        try
        {
            TBoldPriorityQueueItem? result = Head;
            RemoveHead();
            return result;
        }
        finally
        {
            fLocker.Release();
        }
    }

    /// <summary>The item with the highest priority; null when the queue is empty.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldPriorityQueueItem? Head => GetItems(1);

    [Delphi(PortStatus.Ported)]
    public TNotifyEvent? OnHeadChanged
    {
        get => fOnHeadChanged;
        set => fOnHeadChanged = value;
    }

    [Delphi(PortStatus.Ported)]
    public int Count => GetCount();
}
