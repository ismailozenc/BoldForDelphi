// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceHandleEventDriven.pas
// Port map: CSharp/port/units/BoldExternalPersistenceHandleEventDriven.md
// The handle of the event-driven external persistence: a configuration of events per class, chained to the handle
// of the internal database.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldExternalPersistenceHandleEventDriven", Symbol = "TBoldExternalPersistenceHandleEventDriven.Destroy", Note = "Freed the configuration; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>A partially external persistence handle whose external storage is reached through events.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceHandleEventDriven : TBoldAbstractpartiallyExternalPH
{
    private readonly TBoldExternalPersistenceConfigItems FConfig = new();
    private int fMaxFetchBlockSize = 250;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldExternalPersistenceHandleEventDriven(System.ComponentModel.IContainer? Owner = null) : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldPersistenceController CreatePersistenceController()
    {
        var Controller = new TBoldExternalPersistenceControllerEventDriven(BoldModel!.MoldModel, Config, BoldModel.TypeNameDictionary, OnStartUpdates, OnEndUpdates,
            OnFailUpdates, MaxFetchBlockSize);
        ChainPersistenceController(Controller);
        return Controller;
    }

    [Delphi(PortStatus.Ported)]
    private void SetConfig(TBoldExternalPersistenceConfigItems? Value)
    {
        if (Value is not null)
        {
            Config.Assign(Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldExternalPersistenceControllerEventDriven GetPersistenceController() => (TBoldExternalPersistenceControllerEventDriven)base.PersistenceController;

    [Delphi(PortStatus.Ported)] public new TBoldExternalPersistenceControllerEventDriven PersistenceController => GetPersistenceController();
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceConfigItems Config { get => FConfig; set => SetConfig(value); }
    [Delphi(PortStatus.Ported)] public int MaxFetchBlockSize { get => fMaxFetchBlockSize; set => fMaxFetchBlockSize = value; }

    /// <summary>Published in Delphi.</summary>
    public new TNotifyEvent? OnStartUpdates { get => base.OnStartUpdates; set => base.OnStartUpdates = value; }

    /// <summary>Published in Delphi.</summary>
    public new TNotifyEvent? OnEndUpdates { get => base.OnEndUpdates; set => base.OnEndUpdates = value; }

    /// <summary>Published in Delphi.</summary>
    public new TNotifyEvent? OnFailUpdates { get => base.OnFailUpdates; set => base.OnFailUpdates = value; }

    /// <summary>Published in Delphi.</summary>
    public new TNotifyEvent? OnActivate { get => base.OnActivate; set => base.OnActivate = value; }

    /// <summary>Published in Delphi.</summary>
    public new TNotifyEvent? OnDeActivate { get => base.OnDeActivate; set => base.OnDeActivate = value; }
}
