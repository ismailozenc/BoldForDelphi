// Delphi: Source/Common/Queue/BoldAbstractDequeuer.pas
// Port map: CSharp/port/units/BoldAbstractDequeuer.md

namespace Bold;

/// <summary>
/// Handles the messages of a string queue that another thread fills. <see cref="QueueNotEmpty"/> asks the Bold queue
/// to dequeue them before it next displays (on the GUI thread), and <see cref="DequeueAll"/> passes each to
/// <see cref="HandleMessage"/>.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractDequeuer : TBoldSubscribableComponent
{
    private TBoldThreadSafeStringQueue? fQueue;

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractDequeuer.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            TBoldQueueable.RemoveFromPreDisplayQueue(this);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    protected abstract void HandleMessage(string aMsg);

    [Delphi(PortStatus.Ported)]
    protected void DequeueMessages(object? Sender) => DequeueAll();

    /// <summary>
    /// Asks for the queue to be dequeued before the Bold queue next displays. Call it on the thread that runs the Bold
    /// queue (the GUI thread); the listener handle passes the listener's calls to that thread.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void QueueNotEmpty() => TBoldQueueable.AddToPreDisplayQueue(DequeueMessages, null, this);

    /// <summary>Handles the queued messages, oldest first, until the queue is empty.</summary>
    [Delphi(PortStatus.Ported)]
    public void DequeueAll()
    {
        if (Queue is not null)
        {
            string aMsg = Queue.Dequeue();
            while (aMsg != "")
            {
                HandleMessage(aMsg);
                aMsg = Queue.Dequeue();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldThreadSafeStringQueue? Queue
    {
        get => fQueue;
        set => fQueue = value;
    }
}
