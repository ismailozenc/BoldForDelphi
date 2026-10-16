// Delphi: Source/Persistence/Core/BoldPersistenceHandlePassthrough.pas
// Port map: CSharp/port/units/BoldPersistenceHandlePassthrough.md

using static Bold.BoldSubscription;

namespace Bold;

/// <summary>A persistence handle chained to a next handle: activation passes on, and its controller can be chained to the next one's.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldPersistenceHandlePassthrough : TBoldPersistenceHandle
{
    private TBoldPersistenceHandle? fNextPersistenceHandle;
    private TBoldPassthroughSubscriber? fNextPHandleSubscriber;

    [Delphi(PortStatus.Ported)]
    protected TBoldPersistenceHandlePassthrough(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
        fNextPHandleSubscriber = new TBoldPassthroughSubscriber(NextPHandleDeleted);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPersistenceHandlePassthrough.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            fNextPHandleSubscriber?.Dispose();
            fNextPHandleSubscriber = null;
            fNextPersistenceHandle = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetNextPersistenceHandle(TBoldPersistenceHandle? NextPHandle)
    {
        if (fNextPersistenceHandle != NextPHandle)
        {
            fNextPHandleSubscriber!.CancelAllSubscriptions();
            fNextPersistenceHandle = NextPHandle;
            fNextPersistenceHandle?.AddSmallSubscription(fNextPHandleSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
            InitNextPHandle(NextPHandle);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void NextPHandleDeleted(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == fNextPersistenceHandle && RequestedEvent == beDestroying)
        {
            NextPersistenceHandle = null;
        }
    }

    /// <summary>Makes the next handle's controller the next controller of the pass-through controller.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ChainPersistenceController(TBoldPersistenceControllerPassthrough PersistenceController)
    {
        if (NextPersistenceHandle is not null)
        {
            PersistenceController.NextPersistenceController = NextPersistenceHandle.PersistenceController;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void SetActive(bool Value)
    {
        base.SetActive(Value);
        if (NextPersistenceHandle is not null)
        {
            NextPersistenceHandle.Active = Value;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void InitNextPHandle(TBoldPersistenceHandle? NextPHandle)
    {
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPersistenceHandle? NextPersistenceHandle
    {
        get => fNextPersistenceHandle;
        set => SetNextPersistenceHandle(value);
    }
}
