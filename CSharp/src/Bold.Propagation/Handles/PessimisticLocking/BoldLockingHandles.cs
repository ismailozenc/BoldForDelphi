// Delphi: Source/Handles/PessimisticLocking/BoldLockingHandles.pas
// Port map: CSharp/port/units/BoldLockingHandles.md

using static Bold.BoldDefs;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>
/// Pessimistic locking for a system handle's system: while the system is active the handle keeps a lock handler,
/// whose lock holder gets the locks from the lock manager of <see cref="LockManager"/> for the client of
/// <see cref="Listener"/>.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockingHandle : TBoldSubscribableComponent
{
    private bool fActive;
    private TBoldPassthroughSubscriber? fSubscriber;
    private TBoldAbstractSystemHandle? FSystemHandle;
    private TBoldPessimisticLockHandler? fLockHandler;
    private TBoldLockHolder? fLockHolder;
    private TBoldListenerHandle? FListener;
    private TBoldAbstractLockManagerHandle? FLockManager;

    [Delphi(PortStatus.Ported)]
    public TBoldLockingHandle(System.ComponentModel.IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        fSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldLockingHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fSubscriber);
            Deactivate();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetSystemHandle(TBoldAbstractSystemHandle? Value)
    {
        FSystemHandle = Value;
        if (FSystemHandle is not null)
        {
            FSystemHandle.AddSubscription(fSubscriber, beValueIdentityChanged, beValueIdentityChanged);
            FSystemHandle.AddSubscription(fSubscriber, beDestroying, beDestroying);
        }

        AdjustActive();
    }

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == FSystemHandle)
        {
            switch (OriginalEvent)
            {
                case beValueIdentityChanged:
                    AdjustActive();
                    break;
                case beDestroying:
                    Deactivate();
                    SystemHandle = null;
                    break;
            }
        }
        else if (Originator == LockManager)
        {
            switch (OriginalEvent)
            {
                case beConnected:
                    if (Active)
                    {
                        LockHolder.LockManager = LockManager!.LockManager;
                    }

                    break;
                case beDisconnected:
                    if (Active)
                    {
                        LockHolder.LockManager = null;
                    }

                    break;
                case beDestroying:
                    if (Active)
                    {
                        LockHolder.LockManager = null;
                    }

                    FLockManager = null;
                    break;
            }
        }
        else if (Originator == Listener && OriginalEvent == beDestroying)
        {
            Listener = null;
        }
    }

    /// <summary>Creates the lock handler for the active system; raises without a system, listener or lock manager.</summary>
    [Delphi(PortStatus.Ported)]
    private void Activate()
    {
        if (SystemHandle is null)
        {
            throw new EBoldInternal(SysUtils.Format("%s.Activate: Cannot activate Locking without a SystemHandle. Set the Systemhandle property of the %0:s", GetType().Name));
        }

        if (SystemHandle.System is null)
        {
            throw new EBoldInternal(SysUtils.Format("%s.Activate: Cannot activate Locking. The system is not active.", GetType().Name));
        }

        if (Listener is null)
        {
            throw new EBold(SysUtils.Format(HandlesConst.sCannotActivateWithoutListener, GetType().Name));
        }

        if (LockManager is null)
        {
            throw new EBold(SysUtils.Format(HandlesConst.sCannotActivateWithoutLockManager, GetType().Name));
        }

        if (!Active)
        {
            fLockHandler = new TBoldPessimisticLockHandler(FSystemHandle!.System!, LockHolder)
            {
                OnActivityStart = OnActivityStart,
                OnActivityEnd = OnActivityEnd,
                OnProgress = OnProgress,
            };
            fActive = true;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void Deactivate()
    {
        if (Active)
        {
            SysUtils.FreeAndNil(ref fLockHolder);
            SysUtils.FreeAndNil(ref fLockHandler);
            fActive = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetListener(TBoldListenerHandle? Value)
    {
        FListener = Value;
        Listener?.AddSubscription(fSubscriber, beDestroying, 0);
    }

    /// <summary>Active while the system handle is active; the lock holder follows the lock manager handle's lock manager.</summary>
    [Delphi(PortStatus.Ported)]
    private void AdjustActive()
    {
        if (SystemHandle is { Active: true })
        {
            Activate();
        }
        else
        {
            Deactivate();
        }

        if (Active && LockManager is not null && LockManager.LockManager != LockHolder.LockManager)
        {
            LockHolder.LockManager = LockManager.LockManager;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetLockManager(TBoldAbstractLockManagerHandle? Value)
    {
        FLockManager = Value;
        if (FLockManager is not null)
        {
            FLockManager.AddSubscription(fSubscriber, beConnected, beConnected);
            FLockManager.AddSubscription(fSubscriber, beDestroying, beDestroying);
        }

        AdjustActive();
    }

    [Delphi(PortStatus.Ported)]
    private TBoldLockHolder GetLockHolder() =>
        fLockHolder ??= new TBoldLockHolder(FListener!.ListenerThread, FListener.Dequeuer, LockManager!.LockManager);

    [Delphi(PortStatus.Ported)] public TBoldPessimisticLockHandler? LockHandler => fLockHandler;
    [Delphi(PortStatus.Ported)] public TBoldLockHolder LockHolder => GetLockHolder();
    [Delphi(PortStatus.Ported)] public bool Active => fActive;

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractSystemHandle? SystemHandle
    {
        get => FSystemHandle;
        set => SetSystemHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldListenerHandle? Listener
    {
        get => FListener;
        set => SetListener(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractLockManagerHandle? LockManager
    {
        get => FLockManager;
        set => SetLockManager(value);
    }

    [Delphi(PortStatus.Ported)] public TNotifyEvent? OnActivityStart { get; set; }
    [Delphi(PortStatus.Ported)] public TNotifyEvent? OnActivityEnd { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldLockManagerProgressEvent? OnProgress { get; set; }
}
