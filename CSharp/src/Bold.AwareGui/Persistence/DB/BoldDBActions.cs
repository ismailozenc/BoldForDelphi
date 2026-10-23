// Delphi: Source/Persistence/DB/BoldDBActions.pas
// Port map: CSharp/port/units/BoldDBActions.md

using System.ComponentModel;
using static Bold.PersistenceConsts;

namespace Bold;

/// <summary>Creates the database schema of a persistence handle; it handles targets when the handle has a database adapter and is not active.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGenerateSchemaAction : TAction, IBoldNotifiable
{
    private TBoldPersistenceHandleDB? FBoldPersistenceHandleDB;
    private bool fIgnoreUnknownTables;

    [Delphi(PortStatus.Ported)]
    public TBoldGenerateSchemaAction(IContainer? AOwner = null)
        : base(AOwner)
    {
        Caption = sGenerateSchema;
    }

    [Delphi(PortStatus.Ported)]
    public override void ExecuteTarget(object? Target)
    {
        base.ExecuteTarget(Target);
        if (HandlesTarget(null))
        {
            BoldPersistenceHandleDB!.CreateDataBaseSchema(IgnoreUnknownTables);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool HandlesTarget(object? Target) =>
        BoldPersistenceHandleDB is not null && BoldPersistenceHandleDB.DatabaseAdapter is not null && !BoldPersistenceHandleDB.Active;

    /// <summary>Forgets the persistence handle when it is disposed.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void Notification(IComponent AComponent, TOperation Operation)
    {
        if (AComponent == BoldPersistenceHandleDB && Operation == TOperation.opRemove)
        {
            FBoldPersistenceHandleDB = null;
        }
    }

    void IBoldNotifiable.Notification(IComponent AComponent, TOperation Operation) => Notification(AComponent, Operation);

    [Delphi(PortStatus.Ported)]
    private void SetBoldPersistenceHandleDB(TBoldPersistenceHandleDB? Value)
    {
        FBoldPersistenceHandleDB = Value;
        FBoldPersistenceHandleDB?.FreeNotification(this);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPersistenceHandleDB? BoldPersistenceHandleDB
    {
        get => FBoldPersistenceHandleDB;
        set => SetBoldPersistenceHandleDB(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool IgnoreUnknownTables
    {
        get => fIgnoreUnknownTables;
        set => fIgnoreUnknownTables = value;
    }
}
