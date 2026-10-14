// Delphi: Source/Common/Queue/BoldEventQueue.pas
// Port map: CSharp/port/units/BoldEventQueue.md

namespace Bold;

/// <summary>A queued notification: the event to call with its sender, and the receiver it is queued for.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldEventQueueItem : TBoldMemoryManagedObject
{
    private readonly TNotifyEvent fEvent;
    private readonly object? fSender;
    private readonly object? fReceiver;

    [Delphi(PortStatus.Ported)]
    public TBoldEventQueueItem(TNotifyEvent Event, object? Sender, object? Receiver)
    {
        fEvent = Event;
        fSender = Sender;
        fReceiver = Receiver;
    }

    [Delphi(PortStatus.Ported)] public TNotifyEvent Event => fEvent;
    [Delphi(PortStatus.Ported)] public object? Sender => fSender;
    [Delphi(PortStatus.Ported)] public object? Receiver => fReceiver;

    [Delphi(PortStatus.Ported)]
    public void SendEvent() => Event(Sender);
}

/// <summary>
/// Notifications held until the outermost notification ends. The queue has no order (Delphi takes any item); items can be
/// removed by receiver, for a receiver that goes away first.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldEventQueue : TBoldMemoryManagedObject, IDisposable
{
    private TBoldEventQueueItemReceiverIndex? fItemIndex;

    [Delphi(PortStatus.Ported)]
    public TBoldEventQueue()
    {
        fItemIndex = new TBoldEventQueueItemReceiverIndex();
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fItemIndex);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public void Add(TNotifyEvent Event, object? Sender, object? Receiver) =>
        fItemIndex!.Add(new TBoldEventQueueItem(Event, Sender, Receiver));

    /// <summary>Removes one item and sends its event.</summary>
    [Delphi(PortStatus.Ported)]
    public void DequeueOne()
    {
        var item = (TBoldEventQueueItem)fItemIndex!.Any!;
        fItemIndex.Remove(item);
        item.SendEvent();
    }

    /// <summary>Sends as many events as were queued when it started; events queued meanwhile wait for the next call.</summary>
    [Delphi(PortStatus.Ported)]
    public void DequeueAll()
    {
        int internalCount = fItemIndex!.Count;
        for (int i = 0; i < internalCount; i++)
        {
            DequeueOne();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveAllForReceiver(object Receiver)
    {
        var matchingItems = new TList();
        fItemIndex!.FindAllByObject(Receiver, matchingItems);
        for (int i = 0; i < matchingItems.Count; i++)
        {
            fItemIndex.Remove(matchingItems[i]);
        }
    }

    /// <summary>The number of queued items.</summary>
    public int Count => fItemIndex?.Count ?? 0;
}

/// <summary>Queue items keyed by receiver.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldEventQueueItemReceiverIndex : TBoldObjectHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override object? ItemAsKeyObject(object? Item) => ((TBoldEventQueueItem)Item!).Receiver;
}
