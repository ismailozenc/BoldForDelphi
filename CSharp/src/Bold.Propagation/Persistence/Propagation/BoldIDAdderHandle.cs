// Delphi: Source/Persistence/Propagation/BoldIDAdderHandle.pas
// Port map: CSharp/port/units/BoldIDAdderHandle.md
using static Bold.BoldSubscription;
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldIDAdderHandle", Symbol = "TBoldIDAdderHandle.DefineProperties", Note = "Read obsolete properties from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldIDAdderHandle", Symbol = "TBoldIDAdderHandle.ReadObsoleteAutoStartProperty", Note = "Read an obsolete property from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldIDAdderHandle", Symbol = "TBoldIDAdderHandle.ReadObsoleteLeaseDurationProperty", Note = "Read an obsolete property from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldIDAdderHandle", Symbol = "TBoldIDAdderHandle.ReadObsoleteAutoExtendLeaseProperty", Note = "Read an obsolete property from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldIDAdderHandle", Symbol = "TBoldIDAdderHandle.ReadObsoletePollingIntervalProperty", Note = "Read an obsolete property from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldIDAdderHandle", Symbol = "TBoldIDAdderHandle.ReadObsoleteMachineNameProperty", Note = "Read an obsolete property from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldIDAdderHandle", Symbol = "TBoldIDAdderHandle.ReadObsoleteOnRegistrationFailedEventHandler", Note = "Read an obsolete event from Delphi form files.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldIDAdderHandle", Symbol = "TBoldIDAdderHandle.ReadObsoleteProperty", Note = "Warned in the designer about an obsolete property.")]

namespace Bold;

/// <summary>A persistence handle that puts an ID adder in the persistence chain, with the listener of <see cref="BoldListener"/>; activating it activates the listener.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIDAdderHandle : TBoldPersistenceHandlePassthrough
{
    private TBoldListenerHandle? fBoldListenerHandle;
    private TBoldPassthroughSubscriber? fPTSubscriber;

    [Delphi(PortStatus.Ported)]
    public TBoldIDAdderHandle(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
        fPTSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldIDAdderHandle.Destroy")]
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
        var Temp = new TBoldIDAdder();
        ChainPersistenceController(Temp);
        if (fBoldListenerHandle is not null)
        {
            Temp.Listener = fBoldListenerHandle.ListenerThread;
        }

        return Temp;
    }

    [Delphi(PortStatus.Ported)]
    protected override void SetActive(bool Value)
    {
        base.SetActive(Value);
        BoldListener?.SetActive(Value);
    }

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator == fBoldListenerHandle && RequestedEvent == beDestroying)
        {
            BoldListener = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldListenerHandle(TBoldListenerHandle? Value)
    {
        if (fBoldListenerHandle != Value)
        {
            Subscribe(false);
            fBoldListenerHandle = Value;
            Subscribe(true);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void Subscribe(bool DoSubscribe)
    {
        if (DoSubscribe)
        {
            if (!IsDesigning)
            {
                fBoldListenerHandle?.AddSmallSubscription(fPTSubscriber, TBoldSmallEventSet.Of(beDestroying), beDestroying);
            }
        }
        else
        {
            fPTSubscriber?.CancelAllSubscriptions();
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldListenerHandle? BoldListener
    {
        get => fBoldListenerHandle;
        set => SetBoldListenerHandle(value);
    }
}
