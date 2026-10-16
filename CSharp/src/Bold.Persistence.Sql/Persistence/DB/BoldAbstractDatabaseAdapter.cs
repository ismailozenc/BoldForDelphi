// Delphi: Source/Persistence/DB/BoldAbstractDatabaseAdapter.pas
// Port map: CSharp/port/units/BoldAbstractDatabaseAdapter.md

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAbstractDatabaseAdapter", Symbol = "TBoldAbstractDatabaseAdapter.Destroy", Note = "Freed the configuration; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldAbstractDatabaseAdapter", Symbol = "TBoldAbstractDatabaseAdapter.Notification", Note = "Released the database when the connection component was freed; .NET has no free notification, so set InternalDatabase to null instead.")]

namespace Bold;

/// <summary>The event sent when a database adapter changes its connection.</summary>
public static partial class BoldAbstractDatabaseAdapter
{
    [Delphi(PortStatus.Ported)] public const int beDatabaseAdapterChanged = 100;
}

/// <summary>Connects a persistence handle to a database: holds the SQL configuration and makes the IBoldDataBase.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractDatabaseAdapter : TBoldSubscribableComponent
{
    private readonly TBoldSQLDataBaseConfig FSQLDatabaseConfig;
    private TBoldDataBaseEngine fDatabaseEngine;
    private object? fInternalDatabase;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractDatabaseAdapter(System.ComponentModel.IContainer? aOwner = null)
    {
        FSQLDatabaseConfig = new TBoldSQLDataBaseConfig();
        aOwner?.Add(this);
    }

    [Delphi(PortStatus.Ported)]
    private void SetSQLDatabaseConfig(TBoldSQLDataBaseConfig Value) => FSQLDatabaseConfig.AssignConfig(Value);

    /// <summary>Changes the connection: tells subscribers and releases the database made for the old one.</summary>
    [Delphi(PortStatus.Ported, Note = "The connection is any object (an ADO.NET connection wrapper) where Delphi has a TComponent.")]
    private void SetInternalDatabase(object? Value)
    {
        if (fInternalDatabase != Value)
        {
            Changed();
            ReleaseBoldDatabase();
            fInternalDatabase = Value;
        }
    }

    [Delphi(PortStatus.Ported)] protected abstract void ReleaseBoldDatabase();
    [Delphi(PortStatus.Ported)] protected void Changed() => SendEvent(this, BoldAbstractDatabaseAdapter.beDatabaseAdapterChanged);

    /// <summary>Sets the engine and applies its preset settings.</summary>
    [Delphi(PortStatus.Ported, Note = "Always applies the preset; Delphi skips it while loading a form.")]
    protected virtual void SetDataBaseEngine(TBoldDataBaseEngine Value)
    {
        if (Value != fDatabaseEngine)
        {
            SQLDatabaseConfig.InitializeDbEngineSettings(Value);
            fDatabaseEngine = Value;
            SQLDatabaseConfig.Engine = DatabaseEngine;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldDataBaseEngine DatabaseEngine { get => fDatabaseEngine; set => SetDataBaseEngine(value); }
    [Delphi(PortStatus.Ported)] protected object? InternalDatabase { get => fInternalDatabase; set => SetInternalDatabase(value); }
    [Delphi(PortStatus.Ported)] protected abstract IBoldDataBase GetDataBaseInterface();
    [Delphi(PortStatus.Ported)] public IBoldDataBase DatabaseInterface => GetDataBaseInterface();
    [Delphi(PortStatus.Ported)] public TBoldSQLDataBaseConfig SQLDatabaseConfig { get => FSQLDatabaseConfig; set => SetSQLDatabaseConfig(value); }
}
