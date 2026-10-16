// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceHandleSQL.pas
// Port map: CSharp/port/units/BoldExternalPersistenceHandleSQL.md
// The handle of the SQL external persistence: the classes stored in another database, reached through its adapter,
// chained to the handle of the internal database.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldExternalPersistenceHandleSQL", Symbol = "TBoldExternalPersistenceHandleSQL.Destroy", Note = "Freed the class list; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>A partially external persistence handle over another SQL database.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalPersistenceHandleSQL : TBoldAbstractpartiallyExternalPH
{
    private TBoldAbstractDatabaseAdapter? FDatabaseAdapter;
    private readonly TStringList FClassesToHandle = new();

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldExternalPersistenceHandleSQL(System.ComponentModel.IContainer? Owner = null) : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldPersistenceController CreatePersistenceController()
    {
        var Controller = new TBoldExternalPersistenceControllerSQL(BoldModel!.MoldModel, FDatabaseAdapter!, BoldModel.TypeNameDictionary, OnStartUpdates, OnEndUpdates,
            OnFailUpdates, ClassesToHandle);
        ChainPersistenceController(Controller);
        return Controller;
    }

    [Delphi(PortStatus.Ported)]
    private TStrings GetClassesToHandle() => FClassesToHandle;

    [Delphi(PortStatus.Ported)]
    private void SetClassesToHandle(TStrings? Value)
    {
        if (Value is not null)
        {
            FClassesToHandle.Assign(Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldExternalPersistenceControllerSQL GetPersistenceController() => (TBoldExternalPersistenceControllerSQL)base.PersistenceController;

    [Delphi(PortStatus.Ported)] public new TBoldExternalPersistenceControllerSQL PersistenceController => GetPersistenceController();
    [Delphi(PortStatus.Ported)] public TStrings ClassesToHandle { get => GetClassesToHandle(); set => SetClassesToHandle(value); }
    [Delphi(PortStatus.Ported)] public TBoldAbstractDatabaseAdapter? DatabaseAdapter { get => FDatabaseAdapter; set => FDatabaseAdapter = value; }

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
