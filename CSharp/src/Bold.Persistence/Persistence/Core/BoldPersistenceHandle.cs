// Delphi: Source/Persistence/Core/BoldPersistenceHandle.pas
// Port map: CSharp/port/units/BoldPersistenceHandle.md

using static Bold.BoldSubscription;
using static Bold.PersistenceConsts;

namespace Bold;

/// <summary>A handle that creates and owns a persistence controller, and passes its persistence events on.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldPersistenceHandle : TBoldHandle
{
    private bool fActive;
    private TBoldPersistenceController? fPersistenceController;
    private TBoldPassthroughSubscriber? fPersistenceSubscriber;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    protected TBoldPersistenceHandle(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
        fActive = false;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPersistenceHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            SysUtils.FreeAndNil(ref fPersistenceController);
            fPersistenceSubscriber?.Dispose();
            fPersistenceSubscriber = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)] private bool GetActive() => fActive;

    /// <summary>The controller, created on first use.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldPersistenceController GetPersistenceController() => fPersistenceController ??= CreatePersistenceController();

    [Delphi(PortStatus.Ported)] private bool GetHasPersistenceController() => fPersistenceController is not null;

    /// <summary>Raises when the handle is active.</summary>
    [Delphi(PortStatus.Ported)]
    protected void CheckInactive(string Action)
    {
        if (Active)
        {
            throw new EBold(SysUtils.Format(sNotAllowedOnActiveHandle, Action));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldPersistenceController CreatePersistenceController();

    [Delphi(PortStatus.Ported)]
    protected override object? GetHandledObject() => PersistenceController;

    [Delphi(PortStatus.Ported)]
    protected void ReceiveExtendedEvent(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent, object?[] Args) =>
        SendExtendedEvent(Originator, OriginalEvent, Args);

    /// <summary>Sets the flag; deactivating sends beDeactivating.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void SetActive(bool Value)
    {
        fActive = Value;
        if (!Active)
        {
            SendEvent(this, beDeactivating);
        }
    }

    [Delphi(PortStatus.Ported)] protected bool HasPersistenceController => GetHasPersistenceController();

    /// <summary>Passes the controller's persistence events to the subscriber, through this handle.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void AddPersistenceSubscription(TBoldSubscriber Subscriber)
    {
        fPersistenceSubscriber = new TBoldPassthroughSubscriber(ReceiveExtendedEvent);
        PersistenceController.SubscribeToPeristenceEvents(fPersistenceSubscriber);
        AddSubscription(Subscriber, bpeEndFetch, bpeEndFetch);
        AddSubscription(Subscriber, bpeEndUpdate, bpeEndUpdate);
        AddSubscription(Subscriber, bpeEndFetchID, bpeEndUpdate);
        AddSubscription(Subscriber, bpeFetchObject, bpeFetchObject);
        AddSubscription(Subscriber, bpeFetchMember, bpeFetchMember);
        AddSubscription(Subscriber, bpeUpdateObject, bpeUpdateObject);
        AddSubscription(Subscriber, bpeCreateObject, bpeCreateObject);
        AddSubscription(Subscriber, bpeDeleteObject, bpeDeleteObject);
        AddSubscription(Subscriber, bpeFetchId, bpeFetchId);
        AddSubscription(Subscriber, bpeStartFetch, bpeStartFetch);
        AddSubscription(Subscriber, bpeStartUpdate, bpeStartUpdate);
        AddSubscription(Subscriber, bpeStartFetchID, bpeStartUpdate);
    }

    /// <summary>Deactivates and frees the controller; the next use creates a new one.</summary>
    [Delphi(PortStatus.Ported)]
    public void ReleasePersistenceController()
    {
        if (Active)
        {
            Active = false;
        }

        SysUtils.FreeAndNil(ref fPersistenceController);
    }

    [Delphi(PortStatus.Ported)]
    public bool Active
    {
        get => GetActive();
        set => SetActive(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldPersistenceController PersistenceController => GetPersistenceController();
}
