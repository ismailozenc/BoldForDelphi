// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceHandleDataSet.pas
// Port map: CSharp/port/units/BoldExternalPersistenceHandleDataSet.md
// The handle of the data set external persistence: a data set per class, chained to the handle of the internal database.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldExternalPersistenceHandleDataSet", Symbol = "TBoldExternalPersistenceHandleDataSet.Destroy", Note = "Freed the configuration; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>A partially external persistence handle over a data set per class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceHandleDataSet : TBoldAbstractpartiallyExternalPH
{
    private readonly TBoldExternalPersistenceConfigDataSetItems FConfig = new();
    private int FMaxFetchBlockSize = 250;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldExternalPersistenceHandleDataSet(System.ComponentModel.IContainer? Owner = null) : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldPersistenceController CreatePersistenceController()
    {
        var Controller = new TBoldExternalPersistenceControllerDataSet(BoldModel!.MoldModel, Config, BoldModel.TypeNameDictionary, OnStartUpdates, OnEndUpdates,
            OnFailUpdates, MaxFetchBlockSize);
        ChainPersistenceController(Controller);
        return Controller;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldExternalPersistenceControllerDataSet GetPersistenceController() => (TBoldExternalPersistenceControllerDataSet)base.PersistenceController;

    [Delphi(PortStatus.Ported)]
    private void SetConfig(TBoldExternalPersistenceConfigDataSetItems? Value)
    {
        if (Value is not null)
        {
            Config.Assign(Value);
        }
    }

    [Delphi(PortStatus.Ported)] public new TBoldExternalPersistenceControllerDataSet PersistenceController => GetPersistenceController();
    [Delphi(PortStatus.Ported)] public TBoldExternalPersistenceConfigDataSetItems Config { get => FConfig; set => SetConfig(value); }
    [Delphi(PortStatus.Ported)] public int MaxFetchBlockSize { get => FMaxFetchBlockSize; set => FMaxFetchBlockSize = value; }

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
