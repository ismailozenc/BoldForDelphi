// The setup of tutorials 1 and 2 in one class, for the examples that follow them: a model, a SQLite database and a
// system on them. Linked into the examples that use it (see their project files).
using System.ComponentModel;
using Bold;
using Microsoft.Data.Sqlite;

namespace LibraryExamples;

/// <summary>A Bold system on a SQLite database: the model component, the database's persistence handle and the system handle.</summary>
public sealed class SqliteSystem : IDisposable
{
    public readonly Container Components = new();
    public readonly TBoldAbstractModel Model;
    public readonly TADOConnection Connection;
    public readonly TBoldPersistenceHandleDB Database;
    public readonly TBoldSystemHandle SystemHandle;

    /// <summary>
    /// A system on <paramref name="databaseFile"/> for the model of <paramref name="modelLines"/> (by default Library.bld
    /// from the LibraryClasses assembly, with its classes). <paramref name="newDatabase"/> starts from a new database;
    /// <paramref name="open"/> false leaves the system closed (SystemHandle.Active opens it). <paramref name="addTypeNames"/>
    /// adds the type names of custom attribute types, before the model is read.
    /// </summary>
    public SqliteSystem(string databaseFile, bool newDatabase = true, TStringList? modelLines = null, bool useGeneratedCode = true,
        string name = "Library", bool open = true, Action<TBoldTypeNameDictionary>? addTypeNames = null)
    {
        if (newDatabase)
        {
            File.Delete(databaseFile);
        }

        Model = new TBoldAbstractModel(Components) { Name = name };
        if (addTypeNames is not null)
        {
            var typeNames = new TBoldTypeNameHandle(Components);
            addTypeNames(typeNames.Dictionary);
            Model.TypeNameHandle = typeNames;
        }

        Model.ReadModel(modelLines ?? LibraryClasses.LibraryModel.Lines());
        Connection = new TADOConnection(new SqliteConnection($"Data Source={databaseFile}"));
        var adapter = new TBoldDatabaseAdapterADO(Components) { Connection = Connection };
        adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
        Database = new TBoldPersistenceHandleDB(Components) { BoldModel = Model, DatabaseAdapter = adapter };
        if (newDatabase)
        {
            Database.CreateDataBaseSchema();
        }

        var typeInfo = new TBoldSystemTypeInfoHandle(Components) { BoldModel = Model, UseGeneratedCode = useGeneratedCode };
        SystemHandle = new TBoldSystemHandle(Components) { SystemTypeInfoHandle = typeInfo, PersistenceHandle = Database };
        SystemHandle.Active = open;
    }

    public TBoldSystem System => SystemHandle.System!;

    /// <summary>Closes the system, unsaved changes and all, and opens a new one on the same database.</summary>
    public void Reopen()
    {
        System.Discard();
        SystemHandle.Active = false;
        SystemHandle.Active = true;
    }

    public void Dispose()
    {
        if (SystemHandle.Active)
        {
            System.Discard();
            SystemHandle.Active = false;
        }

        Components.Dispose();
        Connection.Close();
    }
}
