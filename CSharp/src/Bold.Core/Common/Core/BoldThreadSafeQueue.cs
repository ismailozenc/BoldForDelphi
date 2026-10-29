// Delphi: Source/Common/Core/BoldThreadSafeQueue.pas
// Port map: CSharp/port/units/BoldThreadSafeQueue.md

namespace Bold;

/// <summary>Called when an item is put in an empty queue, on the thread that put it there, with the queue locked.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldQueueEvent(TBoldThreadSafeQueue Queue);

/// <summary>
/// An entry of a ring queue: entries are linked both ways around a marker entry. New entries go in after the marker
/// (at its Next), the oldest is at the marker's Prev.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldThreadSafeQueueEntry : TBoldMemoryManagedObject, IDisposable
{
    private TBoldThreadSafeQueueEntry fPrev;
    private TBoldThreadSafeQueueEntry fNext;

    /// <summary>A marker: an entry linked to itself, the ring of an empty queue.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldThreadSafeQueueEntry.CreateAsMarker")]
    protected internal TBoldThreadSafeQueueEntry()
    {
        fPrev = this;
        fNext = this;
    }

    /// <summary>An entry linked in after <paramref name="Entry"/>.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldThreadSafeQueueEntry.CreateAfter")]
    protected TBoldThreadSafeQueueEntry(TBoldThreadSafeQueueEntry Entry)
    {
        fPrev = Entry;
        fNext = Entry.Next;
        Entry.fNext = this;
        fNext.fPrev = this;
    }

    /// <summary>Unlinks the entry from its ring.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldThreadSafeQueueEntry.Destroy")]
    public virtual void Dispose()
    {
        fPrev.fNext = fNext;
        fNext.fPrev = fPrev;
        fPrev = this;
        fNext = this;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldThreadSafeQueueEntry Prev => fPrev;

    [Delphi(PortStatus.Ported)]
    public TBoldThreadSafeQueueEntry Next => fNext;
}

/// <summary>
/// A first-in first-out queue that threads share: every operation takes the queue's lock. When an item goes into an
/// empty queue, <see cref="OnQueueNotEmpty"/> is called.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldThreadSafeQueue : TBoldMemoryManagedObject, IDisposable
{
    private TBoldLoggableCriticalSection? fLocker;
    protected TBoldThreadSafeQueueEntry fMarker;
    private TBoldQueueEvent? fOnQueueNotEmpty;

    [Delphi(PortStatus.Ported)]
    public TBoldThreadSafeQueue(string Name)
    {
        fLocker = new TBoldLoggableCriticalSection(Name);
        fMarker = new TBoldThreadSafeQueueEntry();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldThreadSafeQueue.Destroy")]
    public virtual void Dispose()
    {
        if (fLocker is null)
        {
            return;
        }

        Clear();
        SysUtils.FreeAndNil(ref fLocker);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private bool GetEmpty()
    {
        Lock();
        try
        {
            return fMarker.Next == fMarker;
        }
        finally
        {
            Unlock();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual int GetCount()
    {
        int result = 0;
        Lock();
        try
        {
            TBoldThreadSafeQueueEntry entry = fMarker;
            while (entry.Next != fMarker)
            {
                entry = entry.Next;
                result++;
            }
        }
        finally
        {
            Unlock();
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected void Lock() => fLocker!.Acquire();

    [Delphi(PortStatus.Ported)]
    protected void Unlock() => fLocker!.Release();

    [Delphi(PortStatus.Ported)]
    protected void NotifyQueueNotEmpty() => fOnQueueNotEmpty?.Invoke(this);

    /// <summary>True when the queue is empty; call it with the queue locked.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool UnsafeIsEmpty() => fMarker == fMarker.Next;

    /// <summary>Removes every entry (and frees the objects an object queue owns).</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void Clear()
    {
        Lock();
        try
        {
            while (!UnsafeIsEmpty())
            {
                fMarker.Next.Dispose();
            }
        }
        finally
        {
            Unlock();
        }
    }

    [Delphi(PortStatus.Ported)]
    public int Count => GetCount();

    [Delphi(PortStatus.Ported)]
    public bool Empty => GetEmpty();

    [Delphi(PortStatus.Ported)]
    public TBoldQueueEvent? OnQueueNotEmpty
    {
        get => fOnQueueNotEmpty;
        set => fOnQueueNotEmpty = value;
    }
}

/// <summary>A queue of objects. When it owns them, the objects still in it when it is cleared or disposed are disposed.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldThreadSafeObjectQueue : TBoldThreadSafeQueue
{
    private readonly bool fOwnsObjects;
    private int fCount;
    private int fMaxCount;

    [Delphi(PortStatus.Ported)]
    public TBoldThreadSafeObjectQueue(string Name, bool OwnsObjects = true)
        : base(Name)
    {
        fOwnsObjects = OwnsObjects;
    }

    [Delphi(PortStatus.Ported)]
    private int GetMaxCount()
    {
        Lock();
        try
        {
            return fMaxCount;
        }
        finally
        {
            Unlock();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override int GetCount()
    {
        Lock();
        try
        {
            return fCount;
        }
        finally
        {
            Unlock();
        }
    }

    /// <summary>Clears the queue; the count goes back to 0, where Delphi went on counting the cleared objects.</summary>
    public override void Clear()
    {
        Lock();
        try
        {
            base.Clear();
            fCount = 0;
        }
        finally
        {
            Unlock();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void UnsafeEnqueue(object anObject)
    {
        bool wasEmpty = UnsafeIsEmpty();
        _ = new TBoldThreadSafeQueueObjectEntry(anObject, fMarker, OwnsObjects);
        fCount++;
        if (fCount > fMaxCount)
        {
            fMaxCount = fCount;
        }

        if (wasEmpty)
        {
            NotifyQueueNotEmpty();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected object? UnsafeDequeue()
    {
        TBoldThreadSafeQueueEntry head = fMarker.Prev;
        if (UnsafeIsEmpty())
        {
            return null;
        }

        System.Diagnostics.Debug.Assert(head is TBoldThreadSafeQueueObjectEntry);
        object? result = ((TBoldThreadSafeQueueObjectEntry)head).GetObject();
        head.Dispose();
        fCount--;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void Enqueue(object anObject)
    {
        ArgumentNullException.ThrowIfNull(anObject);
        Lock();
        try
        {
            UnsafeEnqueue(anObject);
        }
        finally
        {
            Unlock();
        }
    }

    /// <summary>Moves up to <paramref name="Max"/> objects (counting what the list holds already) to <paramref name="ResultList"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public void DequeueList(System.Collections.IList ResultList, int Max)
    {
        Lock();
        try
        {
            while (!UnsafeIsEmpty() && ResultList.Count < Max)
            {
                ResultList.Add(UnsafeDequeue());
            }
        }
        finally
        {
            Unlock();
        }
    }

    /// <summary>The oldest object, taken out of the queue; null when the queue is empty.</summary>
    [Delphi(PortStatus.Ported)]
    public object? Dequeue()
    {
        Lock();
        try
        {
            return UnsafeDequeue();
        }
        finally
        {
            Unlock();
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool OwnsObjects => fOwnsObjects;

    /// <summary>The most objects the queue has held at once.</summary>
    [Delphi(PortStatus.Ported)]
    public int MaxCount => GetMaxCount();
}

/// <summary>A queue of interface references (any object in .NET).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldThreadSafeInterfaceQueue(string Name) : TBoldThreadSafeQueue(Name)
{
    [Delphi(PortStatus.Ported)]
    public void Enqueue(object anInterface)
    {
        ArgumentNullException.ThrowIfNull(anInterface);
        Lock();
        try
        {
            bool wasEmpty = UnsafeIsEmpty();
            _ = new TBoldThreadSafeQueueInterfaceEntry(anInterface, fMarker);
            if (wasEmpty)
            {
                NotifyQueueNotEmpty();
            }
        }
        finally
        {
            Unlock();
        }
    }

    /// <summary>The oldest reference, taken out of the queue; null when the queue is empty.</summary>
    [Delphi(PortStatus.Ported)]
    public object? Dequeue()
    {
        Lock();
        try
        {
            TBoldThreadSafeQueueEntry head = fMarker.Prev;
            if (UnsafeIsEmpty())
            {
                return null;
            }

            object result = ((TBoldThreadSafeQueueInterfaceEntry)head).anInterface;
            head.Dispose();
            return result;
        }
        finally
        {
            Unlock();
        }
    }
}

/// <summary>A queue of strings; empty strings cannot be queued, as an empty string is what dequeuing an empty queue gives.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldThreadSafeStringQueue(string Name) : TBoldThreadSafeQueue(Name)
{
    [Delphi(PortStatus.Ported)]
    public void Enqueue(string aString)
    {
        if (string.IsNullOrEmpty(aString))
        {
            throw new ArgumentException("An empty string cannot be queued.", nameof(aString));
        }

        Lock();
        try
        {
            bool wasEmpty = UnsafeIsEmpty();
            _ = new TBoldThreadSafeQueueStringEntry(aString, fMarker);
            if (wasEmpty)
            {
                NotifyQueueNotEmpty();
            }
        }
        finally
        {
            Unlock();
        }
    }

    /// <summary>The oldest string, taken out of the queue; '' when the queue is empty.</summary>
    [Delphi(PortStatus.Ported)]
    public string Dequeue()
    {
        Lock();
        try
        {
            TBoldThreadSafeQueueEntry head = fMarker.Prev;
            if (UnsafeIsEmpty())
            {
                return "";
            }

            string result = ((TBoldThreadSafeQueueStringEntry)head).aString;
            head.Dispose();
            return result;
        }
        finally
        {
            Unlock();
        }
    }

    /// <summary>Adds the queued strings to <paramref name="aList"/>, newest first, leaving them in the queue.</summary>
    [Delphi(PortStatus.Ported)]
    public void AppendToStringList(TStrings aList)
    {
        Lock();
        try
        {
            TBoldThreadSafeQueueEntry entry = fMarker;
            while (entry.Next != fMarker)
            {
                entry = entry.Next;
                aList.Add(((TBoldThreadSafeQueueStringEntry)entry).aString);
            }
        }
        finally
        {
            Unlock();
        }
    }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldThreadSafeQueueObjectEntry : TBoldThreadSafeQueueEntry
{
    private object? fAnObject;
    private readonly bool fOwnsObject;

    [Delphi(PortStatus.Ported, Symbol = "TBoldThreadSafeQueueObjectEntry.CreateAfter")]
    public TBoldThreadSafeQueueObjectEntry(object anObject, TBoldThreadSafeQueueEntry Entry, bool OwnsObject)
        : base(Entry)
    {
        fAnObject = anObject;
        fOwnsObject = OwnsObject;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldThreadSafeQueueObjectEntry.Destroy")]
    public override void Dispose()
    {
        if (fOwnsObject)
        {
            (fAnObject as IDisposable)?.Dispose();
        }

        fAnObject = null;
        base.Dispose();
    }

    /// <summary>The object, which the entry then no longer holds (so disposing the entry leaves it alone).</summary>
    [Delphi(PortStatus.Ported)]
    public object? GetObject()
    {
        object? result = fAnObject;
        fAnObject = null;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public object? AnObject => fAnObject;
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldThreadSafeQueueStringEntry : TBoldThreadSafeQueueEntry
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldThreadSafeQueueStringEntry.CreateAfter")]
    public TBoldThreadSafeQueueStringEntry(string aString, TBoldThreadSafeQueueEntry Entry)
        : base(Entry)
    {
        this.aString = aString;
    }

    [Delphi(PortStatus.Ported)]
    public string aString { get; }
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldThreadSafeQueueInterfaceEntry : TBoldThreadSafeQueueEntry
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldThreadSafeQueueInterfaceEntry.CreateAfter")]
    public TBoldThreadSafeQueueInterfaceEntry(object anInterface, TBoldThreadSafeQueueEntry Entry)
        : base(Entry)
    {
        this.anInterface = anInterface;
    }

    [Delphi(PortStatus.Ported)]
    public object anInterface { get; }
}
