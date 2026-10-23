namespace Bold.Tests;

/// <summary>A model component holding a copy of a mold model (through its .bld text).</summary>
internal sealed class TBoldAbstractModelForSqlTests : TBoldAbstractModel
{
    public TBoldAbstractModelForSqlTests(TMoldModel model)
    {
        var strings = new TStringList();
        TMoldBLDRW.ModelToStrings(model, strings);
        SetFromModel(TMoldBLDRW.StringsToModel(strings));
        // The model's own type mappings (beyond the defaults), as a type name handle would give them.
        if (model.TypeNameDictionary is { } types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (TypeNameDictionary.ExactMappingForModelName(types.Mapping(i).ModelName) is null)
                {
                    TBoldTypeNameMapping source = types.Mapping(i), copy = TypeNameDictionary.AddMapping();
                    copy.ModelName = source.ModelName;
                    copy.ExpressionName = source.ExpressionName;
                    copy.DelphiName = source.DelphiName;
                    copy.ContentsName = source.ContentsName;
                    copy.MapperName = source.MapperName;
                }
            }
        }
    }
}

/// <summary>A database handle on an in-memory SQLite database, with the schema of a model created.</summary>
internal sealed class SqliteHandle : IDisposable
{
    public readonly string File;

    /// <summary>Whether Dispose leaves the database file, for another handle to open.</summary>
    public bool KeepFile;
    public readonly TADOConnection Connection;
    public readonly TBoldDatabaseAdapterADO Adapter = new();
    public readonly TBoldPersistenceHandleDB Handle = new();
    private readonly TBoldAbstractModelForSqlTests fModel;

    /// <summary>The model component of the handle.</summary>
    public TBoldAbstractModel Model => fModel;

    public SqliteHandle(TMoldModel model, bool createSchema = true, string? file = null)
    {
        File = file ?? Path.Combine(Path.GetTempPath(), $"bold-{Guid.NewGuid():N}.db");
        Connection = TestDatabase.NewADOConnection(File);
        Adapter.Connection = Connection;
        Adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
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
        if (!KeepFile && System.IO.File.Exists(File))
        {
            System.IO.File.Delete(File);
        }
    }
}
