// Delphi: Source/Persistence/DB/BoldPersistenceHandleDB.pas
// Port map: CSharp/port/units/BoldPersistenceHandleDB.md
// The persistence handle on a database adapter, which gives the database interface and SQL configuration.

[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPersistenceHandleDB", Symbol = "TBoldPersistenceHandleDB.GetNewComponentName", Note = "Named new components in the Delphi form designer.")]

namespace Bold;

/// <summary>Unit-level constants of BoldPersistenceHandleDB.</summary>
public static partial class BoldPersistenceHandleDB
{
    internal const int breDatabaseAdapterDestroying = 100;
    internal const int breDatabaseAdapterChanging = 101;
}

/// <summary>A persistence handle whose database and configuration come from a database adapter.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceHandleDB : TBoldAbstractPersistenceHandleDB
{
    private TBoldAbstractDatabaseAdapter? fDatabaseAdapter;
    private readonly TBoldPassthroughSubscriber fComponentSubscriber;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    public TBoldPersistenceHandleDB(System.ComponentModel.IContainer? aOwner = null) : base(aOwner)
    {
        fComponentSubscriber = new TBoldPassthroughSubscriber(_ReceiveComponentEvents);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPersistenceHandleDB.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fComponentSubscriber.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>A destroyed adapter is forgotten.</summary>
    [Delphi(PortStatus.Ported)]
    private void _ReceiveComponentEvents(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == BoldPersistenceHandleDB.breDatabaseAdapterDestroying)
        {
            fDatabaseAdapter = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDatabaseAdapter(TBoldAbstractDatabaseAdapter? Value)
    {
        if (fDatabaseAdapter != Value)
        {
            fDatabaseAdapter = Value;
            PlaceComponentSubscriptions();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void PlaceComponentSubscriptions()
    {
        fComponentSubscriber.CancelAllSubscriptions();
        if (DatabaseAdapter is not null)
        {
            DatabaseAdapter.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldPersistenceHandleDB.breDatabaseAdapterDestroying);
            DatabaseAdapter.AddSubscription(fComponentSubscriber, BoldAbstractDatabaseAdapter.beDatabaseAdapterChanged, BoldPersistenceHandleDB.breDatabaseAdapterChanging);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldSQLDataBaseConfig GetSQLDatabaseConfig() => fDatabaseAdapter?.SQLDatabaseConfig!;

    [Delphi(PortStatus.Ported)]
    protected override IBoldDataBase? GetDataBaseInterface() => fDatabaseAdapter?.DatabaseInterface;

    [Delphi(PortStatus.Ported)]
    protected override void SetActive(bool Value)
    {
        if (Value && DatabaseAdapter is null)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sCannotActivateWithoutDBAdapter, GetType().Name));
        }

        base.SetActive(Value);
    }

    [Delphi(PortStatus.Ported)]
    protected override void AssertSQLDatabaseconfig(string Context)
    {
        if (DatabaseAdapter is null)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sNoDatabaseAdapterAvailable, GetType().Name, Context));
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldAbstractDatabaseAdapter? DatabaseAdapter { get => fDatabaseAdapter; set => SetDatabaseAdapter(value); }
}
