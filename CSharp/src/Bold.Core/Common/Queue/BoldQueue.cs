// Delphi: Source/Common/Queue/BoldQueue.pas
// Port map: CSharp/port/units/BoldQueue.md

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldQueueDisplayMode { dmDisplayOne, dmDisplayAll }

/// <summary>
/// Work queued for later: a queueable in the display list is shown when the GUI is idle (Display), one in the apply list
/// holds an edit to write back (Apply) or drop (DiscardChange). A queueable can name another that must be displayed first.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldQueueable : TBoldFlaggedObject, IDisposable
{
    private readonly object? fMatchObject;
    private TBoldQueueable? fPrioritizedQueuable;

    [Delphi(PortStatus.Ported)]
    protected TBoldQueueable(object? aMatchObject)
    {
        fMatchObject = aMatchObject;
    }

    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        RemoveFromApplyList();
        if (IsInDisplayList)
        {
            RemoveFromDisplayList();
        }

        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    protected void AddToApplyList()
    {
        if (!BoldQueue.BoldQueueFinalized())
        {
            BoldQueue.BoldInstalledQueue()!.AddToApplyList(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void RemoveFromApplyList()
    {
        if (!BoldQueue.BoldQueueFinalized())
        {
            BoldQueue.BoldInstalledQueue()!.RemoveFromApplyList(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void AddToDisplayList()
    {
        if (!BoldQueue.BoldQueueFinalized())
        {
            System.Diagnostics.Debug.Assert(!IsInDisplayList);
            BoldQueue.BoldInstalledQueue()!.AddToDisplayList(this);
            SetElementFlag(BoldQueue.befIsInDisplayList, true);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void RemoveFromDisplayList()
    {
        if (!BoldQueue.BoldQueueFinalized())
        {
            BoldQueue.BoldInstalledQueue()!.RemoveFromDisplayList(this);
        }

        SetElementFlag(BoldQueue.befIsInDisplayList, false);
    }

    /// <summary>The queueable to display before this one, following the priority chain; null when none waits.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal TBoldQueueable? MostPrioritizedQueuable()
    {
        if (PrioritizedQueuable is null)
        {
            return null;
        }

        TBoldQueueable? result = PrioritizedQueuable.MostPrioritizedQueuableOrSelf();
        if (result is null && StronglyDependedOfPrioritized)
        {
            TList displayList = BoldQueue.BoldInstalledQueue()!.fDisplayList!;
            for (int i = 0; i < displayList.Count; i++)
            {
                if (displayList[i] is TBoldQueueable queueable
                    && queueable.AfterInPriority(PrioritizedQueuable)
                    && !StronglyPrioritizedSibbling(queueable)
                    && !queueable.AfterInPriority(this)) // also leaves out this queueable
                {
                    result = queueable.MostPrioritizedQueuableOrSelf();
                    break;
                }
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldQueueable? MostPrioritizedQueuableOrSelf()
    {
        TBoldQueueable? result = MostPrioritizedQueuable();
        return result is null && IsInDisplayList ? this : result;
    }

    /// <summary>True when <paramref name="Queueable"/> is somewhere up this queueable's priority chain.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool AfterInPriority(TBoldQueueable Queueable)
    {
        if (PrioritizedQueuable is null)
        {
            return false;
        }

        return PrioritizedQueuable == Queueable || PrioritizedQueuable.AfterInPriority(Queueable);
    }

    [Delphi(PortStatus.Ported)]
    protected internal abstract void Display();

    [Delphi(PortStatus.Ported)]
    protected bool StronglyPrioritizedSibbling(TBoldQueueable Queueable) =>
        Queueable.StronglyDependedOfPrioritized && Queueable.PrioritizedQueuable == PrioritizedQueuable;

    [Delphi(PortStatus.Ported)]
    protected static bool DisplayOne() => BoldQueue.BoldInstalledQueue() is { } queue && queue.DisplayOne();

    /// <summary>Displays until the display list is empty; false when there was nothing to display.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool DisplayAll()
    {
        TBoldQueue? queue = BoldQueue.BoldInstalledQueue();
        bool result = queue is not null && queue.fDisplayList!.Count != 0;
        if (result)
        {
            while (DisplayOne())
            {
            }
        }

        return result;
    }

    /// <summary>Applies every queued edit, last first. An edit whose apply fails stays queued.</summary>
    [Delphi(PortStatus.Ported)]
    public static void ApplyAll()
    {
        TBoldQueue? queue = BoldQueue.BoldInstalledQueue();
        if (queue is null)
        {
            return;
        }

        TList applyList = queue.fApplyList!;
        var tempList = new TList();
        while (applyList.Count > 0)
        {
            if (applyList.Last() is TBoldQueueable queueable)
            {
                queueable.Apply(); // removes it from the list
                if (applyList.Count > 0 && applyList.Last() == queueable)
                {
                    // The apply failed: keep it aside and put it back when the list is empty.
                    tempList.Add(queueable);
                    applyList.Count--;
                }
            }
            else
            {
                applyList.Count--; // skip empty slots
            }
        }

        for (int i = 0; i < tempList.Count; i++)
        {
            applyList.Add(tempList[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public static void ApplyAllMatching(object? anObject)
    {
        TList? applyList = BoldQueue.BoldInstalledQueue()?.fApplyList;
        if (applyList is null)
        {
            return;
        }

        for (int i = applyList.Count - 1; i >= 0; i--)
        {
            if (i < applyList.Count && applyList[i] is TBoldQueueable queueable && queueable.MatchObject == anObject)
            {
                queueable.Apply();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public static void DiscardChangeAll()
    {
        TList? applyList = BoldQueue.BoldInstalledQueue()?.fApplyList;
        if (applyList is null)
        {
            return;
        }

        for (int i = applyList.Count - 1; i >= 0; i--)
        {
            if (i < applyList.Count && applyList[i] is TBoldQueueable queueable)
            {
                queueable.DiscardChange();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public static void DiscardChangeAllMatching(object? anObject)
    {
        TList? applyList = BoldQueue.BoldInstalledQueue()?.fApplyList;
        if (applyList is null)
        {
            return;
        }

        for (int i = applyList.Count - 1; i >= 0; i--)
        {
            if (i < applyList.Count && applyList[i] is TBoldQueueable queueable && queueable.MatchObject == anObject)
            {
                queueable.DiscardChange();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public static void AddToPreDisplayQueue(TNotifyEvent Event, object? Sender, object? Receiver)
    {
        if (!BoldQueue.BoldQueueFinalized())
        {
            BoldQueue.BoldInstalledQueue()!.AddEventToPredisplayQueue(Event, Sender, Receiver);
        }
    }

    [Delphi(PortStatus.Ported)]
    public static void RemoveFromPreDisplayQueue(object Receiver)
    {
        if (!BoldQueue.BoldQueueFinalized())
        {
            BoldQueue.BoldInstalledQueue()!.RemoveFromPreDisplayQueue(Receiver);
        }
    }

    [Delphi(PortStatus.Ported)] public abstract void Apply();
    [Delphi(PortStatus.Ported)] public abstract void DiscardChange();
    [Delphi(PortStatus.Ported)] public object? MatchObject => fMatchObject;
    [Delphi(PortStatus.Ported)] public bool IsInDisplayList => GetElementFlag(BoldQueue.befIsInDisplayList);

    [Delphi(PortStatus.Ported)]
    public TBoldQueueable? PrioritizedQueuable
    {
        get => fPrioritizedQueuable;
        set => fPrioritizedQueuable = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool StronglyDependedOfPrioritized
    {
        get => GetElementFlag(BoldQueue.befStronglyDependedOfPrioritized);
        set => SetElementFlag(BoldQueue.befStronglyDependedOfPrioritized, value);
    }
}

/// <summary>
/// The queue of an environment: the pre-display events, the display list and the apply list. Lists keep empty slots
/// where items were removed, so removal during iteration is safe.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldQueue : TBoldMemoryManagedObject, IDisposable
{
    private bool fIsDisplaying;
    private TBoldQueueDisplayMode fDisplayMode;
    private TBoldEventQueue? fPreDisplayQueue;
    internal TList? fDisplayList;
    internal TList? fApplyList;

    [Delphi(PortStatus.Ported)]
    protected TBoldQueue()
    {
        fPreDisplayQueue = new TBoldEventQueue();
        fDisplayList = new TList();
        fApplyList = new TList();
        DisplayMode = BoldUtils.BoldRunningAsDesignTimePackage ? TBoldQueueDisplayMode.dmDisplayAll : TBoldQueueDisplayMode.dmDisplayOne;
        ActivateDisplayQueue();
    }

    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        fDisplayList = null;
        fApplyList = null;
        SysUtils.FreeAndNil(ref fPreDisplayQueue);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Displays the last queueable in the display list, or the one it must wait for. An exception from Display goes to
    /// BoldQueue.HandleException, as Delphi passes it to Application.HandleException.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected internal bool DisplayOne()
    {
        if (fIsDisplaying)
        {
            return false;
        }

        fIsDisplaying = true;
        try
        {
            TList displayList = fDisplayList!;
            while (displayList.Count > 0 && displayList.Last() is null)
            {
                displayList.Count--;
            }

            bool result = displayList.Count > 0;
            if (result)
            {
                TBoldQueueable? queueable = null;
                try
                {
                    TBoldQueueable? exchangeWith = ((TBoldQueueable)displayList.Last()!).MostPrioritizedQueuable();
                    if (exchangeWith is not null)
                    {
                        int index = displayList.IndexOf(exchangeWith);
                        if (index != -1)
                        {
                            displayList.Exchange(index, displayList.Count - 1);
                        }
                    }

                    queueable = (TBoldQueueable)displayList.Last()!;
                    queueable.Display();
                }
                catch (Exception e)
                {
                    BoldQueue.HandleException(e, queueable);
                }
            }

            return result;
        }
        finally
        {
            fIsDisplaying = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected bool DisplayAll() => TBoldQueueable.DisplayAll();

    [Delphi(PortStatus.Ported)]
    protected internal void AddToApplyList(TBoldQueueable Queueable) => fApplyList!.Add(Queueable);

    /// <summary>Empties the slot of the queueable, searching from the end; the last slot is dropped.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void RemoveFromApplyList(TBoldQueueable Queueable) => RemoveFromEnd(fApplyList!, Queueable);

    [Delphi(PortStatus.Ported)]
    protected internal void AddToDisplayList(TBoldQueueable Queueable)
    {
        fDisplayList!.Add(Queueable);
        EnsureDequeing();
    }

    [Delphi(PortStatus.Ported)]
    protected internal void RemoveFromDisplayList(TBoldQueueable Queueable) => RemoveFromEnd(fDisplayList!, Queueable);

    private static void RemoveFromEnd(TList list, TBoldQueueable queueable)
    {
        int index = list.Count - 1;
        while (index >= 0 && list[index] != queueable)
        {
            index--;
        }

        if (index > -1)
        {
            if (index == list.Count - 1)
            {
                list.Count--;
            }
            else
            {
                list[index] = null;
            }
        }
    }

    /// <summary>Makes sure the queue gets processed; an environment's queue starts its idle handling here.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void EnsureDequeing()
    {
    }

    [Delphi(PortStatus.Ported)]
    public void AddEventToPredisplayQueue(TNotifyEvent Event, object? Sender, object? Receiver)
    {
        fPreDisplayQueue!.Add(Event, Sender, Receiver);
        EnsureDequeing();
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveFromPreDisplayQueue(object Receiver) => fPreDisplayQueue!.RemoveAllForReceiver(Receiver);

    [Delphi(PortStatus.Ported)]
    public void PerformPreDisplayQueue() => fPreDisplayQueue!.DequeueAll();

    [Delphi(PortStatus.Ported)] public abstract void DeActivateDisplayQueue();
    [Delphi(PortStatus.Ported)] public abstract void ActivateDisplayQueue();

    [Delphi(PortStatus.Ported)]
    public TBoldQueueDisplayMode DisplayMode
    {
        get => fDisplayMode;
        set => fDisplayMode = value;
    }

    /// <summary>The number of display list slots in use, empty ones included.</summary>
    public int DisplayListCount => fDisplayList?.Count ?? 0;

    /// <summary>The number of apply list slots in use, empty ones included.</summary>
    public int ApplyListCount => fApplyList?.Count ?? 0;
}

/// <summary>Flags and routines of BoldQueue.</summary>
public static partial class BoldQueue
{
    [Delphi(PortStatus.Ported)] public const int befIsInDisplayList = BoldBase.BoldElementFlag0;
    [Delphi(PortStatus.Ported)] public const int befStronglyDependedOfPrioritized = BoldBase.BoldElementFlag1;
    [Delphi(PortStatus.Ported)] public const int befFollowerSelected = BoldBase.BoldElementFlag2;

    /// <summary>Receives exceptions from Display. Delphi shows them with Application.HandleException; the WinForms layer sets its own handler.</summary>
    public static Action<Exception, object?> HandleException { get; set; } =
        (e, sender) => System.Diagnostics.Trace.TraceError($"{sender?.GetType().Name}: {e}");

    [Delphi(PortStatus.Ported)]
    public static bool BoldQueueFinalized() => BoldEnvironment.BoldEnvironmentsFinalized() || BoldEnvironment.BoldEffectiveEnvironment().QueueFinalized;

    [Delphi(PortStatus.Ported)]
    public static TBoldQueue? BoldInstalledQueue() =>
        !BoldEnvironment.BoldEnvironmentsFinalized() ? BoldEnvironment.BoldEffectiveEnvironment().Queue : null;
}
