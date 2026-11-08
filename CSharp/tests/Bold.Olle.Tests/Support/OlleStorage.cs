using System.ComponentModel;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>
/// A persistent storage for the OLLE tests: an application on the Building model, with its own SQLite database file
/// that holds the application's tables and the OLLE tables, a running system, and the OLLE handle on its persistence.
/// </summary>
internal sealed class OlleStorage : IDisposable
{
    public readonly Container Components = new();

    /// <summary>The storage's id, by which the other storages know it (Delphi's TBoldPSId).</summary>
    public readonly string Name;
    public readonly string DatabaseFile;
    public readonly TBoldAbstractModel Model;
    public readonly TADOConnection Connection;
    public readonly TBoldDatabaseAdapterADO Adapter;
    public readonly TBoldPersistenceHandleDB DatabaseHandle;
    public readonly TBoldSystemTypeInfoHandle TypeInfoHandle;
    public readonly TBoldSystemHandle SystemHandle;
    public readonly TBoldOLLEHandle OLLEHandle;

    /// <summary>A storage on a new database file: the application's tables and the OLLE tables are made, the system opened.</summary>
    public OlleStorage(string name)
    {
        Name = name;
        DatabaseFile = Path.Combine(Path.GetTempPath(), $"olle-{name}-{Guid.NewGuid():N}.db");
        Model = new TBoldAbstractModel(Components) { Name = name + ".Model" };
        Model.ReadModel(BuildingModel.Lines());
        Connection = TestDatabase.NewADOConnection(DatabaseFile);
        Adapter = new TBoldDatabaseAdapterADO(Components) { Connection = Connection };
        Adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
        DatabaseHandle = new TBoldPersistenceHandleDB(Components) { BoldModel = Model, DatabaseAdapter = Adapter };
        DatabaseHandle.CreateDataBaseSchema();
        OLLEHandle = new TBoldOLLEHandle(Components) { Name = name + ".OLLE", ApplicationPersistenceHandle = DatabaseHandle };
        OLLEHandle.OLLEController.GenerateDatabase();
        TypeInfoHandle = new TBoldSystemTypeInfoHandle(Components) { BoldModel = Model };
        SystemHandle = new TBoldSystemHandle(Components) { Name = name + ".System", SystemTypeInfoHandle = TypeInfoHandle, PersistenceHandle = DatabaseHandle };
        SystemHandle.Active = true;
    }

    public TBoldSystem System => SystemHandle.System!;

    public TBoldSystem OLLESystem => OLLEHandle.OLLEController.OLLESystem;

    public TBoldOwnObjectHandler Own => OLLEHandle.OwnObjectHandler;

    public TBoldForeignObjectHandler Foreign => OLLEHandle.ForeignObjectHandler;

    /// <summary>A new person, saved.</summary>
    public TPerson NewPerson(string firstName, string lastName)
    {
        var person = new TPerson(System) { FirstName = firstName, LastName = lastName };
        System.UpdateDatabase();
        return person;
    }

    /// <summary>The persons in the database, read again: the system is closed and opened, so that it has what other storages wrote.</summary>
    public TPerson[] Persons()
    {
        Reopen();
        TBoldObjectList persons = System.ClassByExpressionName("Person");
        persons.EnsureObjects();
        return [.. Enumerable.Range(0, persons.Count).Select(i => (TPerson)persons[i]!)];
    }

    /// <summary>The person of the first name, read again from the database.</summary>
    public TPerson Person(string firstName) => Persons().Single(p => p.FirstName == firstName);

    /// <summary>Closes and opens the system: what is loaded is read from the database again.</summary>
    public void Reopen()
    {
        System.Discard();
        SystemHandle.Active = false;
        SystemHandle.Active = true;
    }

    /// <summary>The local id of the object.</summary>
    public static TBoldObjectId Id(TBoldObject anObject) => anObject.BoldObjectLocator!.BoldObjectID;

    /// <summary>A list of the ids of the objects.</summary>
    public static TBoldObjectIdList Ids(params TBoldObject[] objects)
    {
        var result = new TBoldObjectIdList();
        foreach (TBoldObject anObject in objects)
        {
            result.Add(Id(anObject));
        }

        return result;
    }

    /// <summary>The ids as strings, to compare lists.</summary>
    public static string[] Strings(TBoldObjectIdList ids) => [.. Enumerable.Range(0, ids.Count).Select(i => ids[i].AsString)];

    /// <summary>The read-only flag of the object in the database (READ_ONLY of BOLD_OBJECT).</summary>
    public bool IsReadOnlyInDatabase(TBoldObjectId id) =>
        Convert.ToInt32(TestDatabase.Scalar(Connection, "SELECT READ_ONLY FROM BOLD_OBJECT WHERE BOLD_ID = " + id.AsString)) == 1;

    /// <summary>The global id of the object in the database (EXTERNAL_ID of BOLD_XFILES).</summary>
    public string GlobalIdInDatabase(TBoldObjectId id) =>
        (string)TestDatabase.Scalar(Connection, "SELECT EXTERNAL_ID FROM BOLD_XFILES WHERE BOLD_ID = " + id.AsString)!;

    /// <summary>The number of rows of a table of the database.</summary>
    public int Count(string table) => Convert.ToInt32(TestDatabase.Scalar(Connection, "SELECT COUNT(*) FROM " + table));

    /// <summary>The OLLE objects of the class (by expression name).</summary>
    public TBoldObject[] OLLEObjects(string expressionName)
    {
        TBoldObjectList objects = OLLESystem.ClassByExpressionName(expressionName);
        return [.. Enumerable.Range(0, objects.Count).Select(i => objects[i]!)];
    }

    /// <summary>The OLLE objects of the class (by expression name), as the class.</summary>
    public T[] OLLEObjects<T>(string expressionName)
        where T : TBoldObject => [.. OLLEObjects(expressionName).Cast<T>()];

    public void Dispose()
    {
        if (SystemHandle.Active)
        {
            System.Discard();
            SystemHandle.Active = false;
        }

        Components.Dispose();
        Connection.Close();
        Connection.Connection.Dispose();
        try
        {
            File.Delete(DatabaseFile);
        }
        catch (IOException)
        {
            // a file a provider still holds is left for the temp folder's cleanup
        }
    }
}
