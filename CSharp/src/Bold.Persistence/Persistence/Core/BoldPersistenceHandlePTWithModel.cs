// Delphi: Source/Persistence/Core/BoldPersistenceHandlePTWithModel.pas
// Port map: CSharp/port/units/BoldPersistenceHandlePTWithModel.md

using static Bold.BoldAbstractModel;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>A pass-through persistence handle with a model: a change of model releases the controller.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldPersistenceHandlePassthroughWithModel : TBoldPersistenceHandlePassthrough
{
    private const int breModelDestroyed = 42;
    private const int breModelChanged = 43;

    private TBoldAbstractModel? fBoldModel;
    private TBoldPassthroughSubscriber? fModelSubscriber;

    [Delphi(PortStatus.Ported)]
    protected TBoldPersistenceHandlePassthroughWithModel(System.ComponentModel.IContainer? Owner = null)
        : base(Owner)
    {
        fModelSubscriber = new TBoldPassthroughSubscriber(_Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPersistenceHandlePassthroughWithModel.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fModelSubscriber?.Dispose();
            fModelSubscriber = null;
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldModel(TBoldAbstractModel? Value)
    {
        if (Value != fBoldModel)
        {
            fModelSubscriber!.CancelAllSubscriptions();
            if (Value is not null)
            {
                Value.AddSmallSubscription(fModelSubscriber, TBoldSmallEventSet.Of(beDestroying), breModelDestroyed);
                Value.AddSmallSubscription(fModelSubscriber, TBoldSmallEventSet.Of(beModelChanged), breModelChanged);
            }

            fBoldModel = Value;
            ModelChanged();
        }
    }

    /// <summary>Releases the controller and sends beValueIdentityChanged (a type change counts as an identity change).</summary>
    [Delphi(PortStatus.Ported)]
    private void ModelChanged()
    {
        ReleasePersistenceController();
        SendEvent(this, beValueIdentityChanged);
    }

    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        switch (RequestedEvent)
        {
            case breModelChanged:
                ModelChanged();
                break;
            case breModelDestroyed:
                fBoldModel = null;
                fModelSubscriber!.CancelAllSubscriptions();
                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldAbstractModel? BoldModel
    {
        get => fBoldModel;
        set => SetBoldModel(value);
    }
}
