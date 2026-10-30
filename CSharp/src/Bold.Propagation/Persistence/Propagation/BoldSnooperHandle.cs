// Delphi: Source/Persistence/Propagation/BoldSnooperHandle.pas
// Port map: CSharp/port/units/BoldSnooperHandle.md
using static Bold.BoldSubscription;
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldSnooperHandle", Symbol = "TBoldSnooperHandle.DefineProperties", Note = "Read an obsolete property from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldSnooperHandle", Symbol = "TBoldSnooperHandle.ReadObsoleteMachineNameProperty", Note = "Read an obsolete property from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldSnooperHandle", Symbol = "TBoldSnooperHandle.ReadObsoleteProperty", Note = "Warned in the designer about an obsolete property.")]

namespace Bold;

/// <summary>
/// A persistence handle that puts a snooper in the persistence chain: the client's changes go to the propagator of
/// <see cref="PropagatorHandle"/>, and updates can be made to need the database lock of <see cref="LockManagerHandle"/>.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSnooperHandle : TBoldPersistenceHandlePassthroughWithModel
{
    private TBoldPassthroughSubscriber? fPTSubscriber;
    private TBoldAbstractLockManagerHandle? fLockManagerHandle;
    private bool FCheckDatabaseLock;
    private TBoldAbstractPropagatorHandle? fPropagatorHandle;

    [Delphi(PortStatus.Ported)]
    public TBoldSnooperHandle(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
        fPTSubscriber = new TBoldPassthroughSubscriber(_Receive);
        FCheckDatabaseLock = false;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldSnooperHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fPTSubscriber);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldPersistenceController CreatePersistenceController()
    {
        if (BoldModel is null)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sBoldModelNotAssigned, GetType().Name));
        }

        var Snooper = new TBoldSnooper(BoldModel.MoldModel!, this);
        ChainPersistenceController(Snooper);
        if (PropagatorHandle is not null)
        {
            Snooper.OnPropagatorFailure = PropagatorHandle.DoPropagatorCallFailed;
        }
        else
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sPropagatorHandleNotAssigned, GetType().Name));
        }

        return Snooper;
    }

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == fLockManagerHandle && RequestedEvent == beDestroying)
        {
            LockManagerHandle = null;
        }

        if (Originator == fPropagatorHandle && RequestedEvent == beDestroying)
        {
            PropagatorHandle = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetLockManagerHandle(TBoldAbstractLockManagerHandle? Value)
    {
        if (fLockManagerHandle != Value)
        {
            Subscribe(false);
            fLockManagerHandle = Value;
            Subscribe(true);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetPropagatorHandle(TBoldAbstractPropagatorHandle? Value)
    {
        if (Value != fPropagatorHandle)
        {
            Subscribe(false);
            fPropagatorHandle = Value;
            if (IsLoaded && HasPersistenceController)
            {
                if (fPropagatorHandle is not null)
                {
                    Snooper.OnPropagatorFailure = fPropagatorHandle.DoPropagatorCallFailed;
                }
                else if (!IsDestroying)
                {
                    Snooper.OnPropagatorFailure = null;
                }
            }

            Subscribe(true);
        }
    }

    /// <summary>True when active with a propagator to send to.</summary>
    [Delphi(PortStatus.Ported)]
    private bool GetConnected()
    {
        bool result = Active;
        if (result && IsLoaded)
        {
            result = Snooper.Propagator is not null;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetIsLoaded() => !IsDesigning && !IsLoading;

    [Delphi(PortStatus.Ported)]
    private TBoldSnooper GetSnooper() => (TBoldSnooper)PersistenceController;

    [Delphi(PortStatus.Ported)]
    private void Subscribe(bool DoSubscribe)
    {
        if (DoSubscribe)
        {
            if (!IsDesigning)
            {
                fLockManagerHandle?.AddSmallSubscription(fPTSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
                fPropagatorHandle?.AddSmallSubscription(fPTSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
            }
        }
        else
        {
            fPTSubscriber?.CancelAllSubscriptions();
        }
    }

    [Delphi(PortStatus.Ported)] protected bool IsLoaded => GetIsLoaded();
    [Delphi(PortStatus.Ported)] protected TBoldSnooper Snooper => GetSnooper();
    [Delphi(PortStatus.Ported)] public bool Connected => GetConnected();

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractLockManagerHandle? LockManagerHandle
    {
        get => fLockManagerHandle;
        set => SetLockManagerHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool CheckDatabaseLock
    {
        get => FCheckDatabaseLock;
        set => FCheckDatabaseLock = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractPropagatorHandle? PropagatorHandle
    {
        get => fPropagatorHandle;
        set => SetPropagatorHandle(value);
    }
}
