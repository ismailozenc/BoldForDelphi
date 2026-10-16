namespace Bold.Tests;

/// <summary>A model component holding a copy of a mold model (through its .bld text).</summary>
internal sealed class TBoldAbstractModelForSqlTests : TBoldAbstractModel
{
    public TBoldAbstractModelForSqlTests(TMoldModel model)
    {
        var strings = new TStringList();
        TMoldBLDRW.ModelToStrings(model, strings);
        SetFromModel(TMoldBLDRW.StringsToModel(strings));
    }
}

/// <summary>A database handle on an in-memory SQLite database, with the schema of a model created.</summary>
internal sealed class SqliteHandle : IDisposable
{
    private readonly string fFile = Path.Combine(Path.GetTempPath(), $"bold-{Guid.NewGuid():N}.db");
    public readonly TADOConnection Connection;
    public readonly TBoldDatabaseAdapterADO Adapter = new();
    public readonly TBoldPersistenceHandleDB Handle = new();
    private readonly TBoldAbstractModelForSqlTests fModel;

    public SqliteHandle(TMoldModel model, bool createSchema = true)
    {
        Connection = TestDatabase.NewADOConnection(fFile);
        Adapter.Connection = Connection;
        Adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeGenericANSISQL92);
        fModel = new TBoldAbstractModelForSqlTests(model);
        Handle.DatabaseAdapter = Adapter;
        Handle.BoldModel = fModel;
        if (createSchema)
        {
            Handle.CreateDataBaseSchema();
        }
    }

    /// <summary>The handle's controller, opened.</summary>
    public TBoldPersistenceController OpenController()
    {
        Handle.Active = true;
        return Handle.PersistenceController;
    }

    public void Dispose()
    {
        Handle.Active = false;
        Handle.Dispose();
        Adapter.Dispose();
        fModel.Dispose();
        Connection.Close();
        Connection.Connection.Dispose();
        if (File.Exists(fFile))
        {
            File.Delete(fFile);
        }
    }
}
