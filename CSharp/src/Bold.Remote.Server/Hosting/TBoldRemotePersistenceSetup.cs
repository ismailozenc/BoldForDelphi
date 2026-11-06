using System.ComponentModel;
using System.Data.Common;
using Microsoft.Extensions.Configuration;

namespace Bold;

/// <summary>
/// The persistence a remote persistence server offers, read from its configuration (section "Bold"):
/// <list type="bullet">
/// <item><c>Model</c>: the model's .bld file.</item>
/// <item><c>Provider</c>: the ADO.NET provider's invariant name, Microsoft.Data.SqlClient (the default) or Microsoft.Data.Sqlite.</item>
/// <item><c>ConnectionString</c>: the database's connection string.</item>
/// <item><c>Engine</c>: the SQL dialect, a <see cref="TBoldDataBaseEngine"/> such as dbeSQLServer (the default) or dbeSQLite.</item>
/// <item><c>CreateSchema</c>: true to create the database's tables at start (for a new database).</item>
/// <item><c>Propagator</c>: true to run the propagator here as well (its hub at /propagator) and tell it of the changes saved through this server.</item>
/// </list>
/// </summary>
public sealed class TBoldRemotePersistenceSetup : IDisposable
{
    private readonly Container fComponents = new();
    private TBoldInProcessPropagatorHandle? fPropagatorHandle;
    private TBoldInProcessLockManagerHandle? fLockManagerHandle;

    /// <summary>The persistence for <paramref name="Model"/> on <paramref name="Connection"/>; with a snooper when <paramref name="WithPropagator"/>.</summary>
    public TBoldRemotePersistenceSetup(TBoldAbstractModel Model, DbConnection Connection, TBoldDataBaseEngine Engine, bool WithPropagator)
    {
        this.Model = Model;
        fComponents.Add(Model);
        this.Connection = new TADOConnection(Connection);
        var adapter = new TBoldDatabaseAdapterADO(fComponents) { Connection = this.Connection };
        adapter.SQLDatabaseConfig.InitializeDbEngineSettings(Engine);
        DatabaseHandle = new TBoldPersistenceHandleDB(fComponents) { BoldModel = Model, DatabaseAdapter = adapter };
        if (WithPropagator)
        {
            fPropagatorHandle = new TBoldInProcessPropagatorHandle(fComponents);
            fLockManagerHandle = new TBoldInProcessLockManagerHandle(fComponents);
            PersistenceHandle = new TBoldSnooperHandle(fComponents)
            {
                BoldModel = Model,
                NextPersistenceHandle = DatabaseHandle,
                PropagatorHandle = fPropagatorHandle,
                LockManagerHandle = fLockManagerHandle,
            };
        }
        else
        {
            PersistenceHandle = DatabaseHandle;
        }
    }

    public TBoldAbstractModel Model { get; }

    public TADOConnection Connection { get; }

    public TBoldPersistenceHandleDB DatabaseHandle { get; }

    /// <summary>What the requests run on: the database handle, or the snooper in front of it.</summary>
    public TBoldPersistenceHandle PersistenceHandle { get; }

    public bool WithPropagator => fPropagatorHandle is not null;

    /// <summary>Opens the persistence handle (and the database); after creating the schema, if that is to be done.</summary>
    public void Open() => PersistenceHandle.Active = true;

    /// <summary>Connects the snooper to the propagator in this process, once it runs.</summary>
    public void ConnectToPropagator()
    {
        if (fPropagatorHandle is not null)
        {
            fPropagatorHandle.Connected = true;
            fLockManagerHandle!.Connected = true;
        }
    }

    /// <summary>The setup the configuration section describes.</summary>
    public static TBoldRemotePersistenceSetup FromConfiguration(IConfiguration Section)
    {
        string modelFile = Section["Model"] ?? throw new EBold("Bold:Model, the model's .bld file, is not configured.");
        var model = new TBoldAbstractModel { Name = Path.GetFileNameWithoutExtension(modelFile) };
        var lines = new TStringList();
        lines.LoadFromFile(modelFile);
        model.ReadModel(lines);

        string provider = Section["Provider"] ?? "Microsoft.Data.SqlClient";
        RegisterKnownProviders();
        DbConnection connection = DbProviderFactories.GetFactory(provider).CreateConnection()
            ?? throw new EBold($"The provider {provider} makes no connections.");
        connection.ConnectionString = Section["ConnectionString"] ?? throw new EBold("Bold:ConnectionString is not configured.");

        TBoldDataBaseEngine engine = Enum.Parse<TBoldDataBaseEngine>(Section["Engine"] ?? nameof(TBoldDataBaseEngine.dbeSQLServer), true);
        var setup = new TBoldRemotePersistenceSetup(model, connection, engine, Section.GetValue("Propagator", false));
        if (Section.GetValue("CreateSchema", false))
        {
            setup.DatabaseHandle.CreateDataBaseSchema();
        }

        setup.Open();
        return setup;
    }

    /// <summary>Registers the providers the server is published with, by name, so that they load only when used.</summary>
    public static void RegisterKnownProviders()
    {
        DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", "Microsoft.Data.SqlClient.SqlClientFactory, Microsoft.Data.SqlClient");
        DbProviderFactories.RegisterFactory("Microsoft.Data.Sqlite", "Microsoft.Data.Sqlite.SqliteFactory, Microsoft.Data.Sqlite");
    }

    public void Dispose()
    {
        fComponents.Dispose();
        Connection.Close();
        Connection.Connection.Dispose();
    }
}
