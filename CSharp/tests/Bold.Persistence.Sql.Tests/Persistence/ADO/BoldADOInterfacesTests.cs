// Delphi: Source/Persistence/ADO/BoldADOInterfaces.pas, Source/Persistence/DB/BoldDBInterfaces.pas
// Port map: CSharp/port/units/BoldADOInterfaces.md
namespace Bold.Tests;

public class BoldADOInterfacesTests
{
    private static TBoldADOConnection NewDatabase(out TADOConnection connection, string emptyMarker = "")
    {
        connection = TestDatabase.NewADOConnection();
        var config = new TBoldSQLDataBaseConfig { EmptyStringMarker = emptyMarker };
        TestDatabase.Exec(connection, "CREATE TABLE Person (Id INTEGER, Name VARCHAR(20), Weight DOUBLE PRECISION, Photo BLOB)");
        return new TBoldADOConnection(connection, config);
    }

    [Test]
    [Covers(typeof(TBoldADOQuery), "ExecSQL", "Open", "AssignSQLText", "AssignSQL", "ParamByName", "GetRowsAffected", "GetSQLText", "GetRecordCount", "ClearParams")]
    [Covers(typeof(TBoldADOParameter), "SetAsInteger", "SetAsString", "SetAsFloat", "SetAsBlob", "GetAsString", "GetName")]
    [Covers(typeof(TBoldDataSetWrapper), "FieldByName", "First", "Next", "GetEof", "FindField", "GetFieldCount", "GetFields")]
    [Covers(typeof(TBoldFieldWrapper), "GetAsInteger", "GetAsString", "GetAsFloat", "GetAsBlob", "GetIsNull", "GetAsVariant")]
    [Covers("BoldDBInterfaces", "BoldLogSQL", "BoldSQLLogCount")]
    public void Queries_write_and_read_rows_with_parameters()
    {
        TBoldADOConnection db = NewDatabase(out TADOConnection connection);
        IBoldExecQuery insert = db.GetExecQuery();
        insert.AssignSQLText("INSERT INTO Person (Id, Name, Weight, Photo) VALUES (:Id, :Name, :Weight, :Photo)");
        Assert.That(insert.SQLText, Does.Contain(":Name"));
        int logged = BoldDBInterfaces.BoldSQLLogCount;
        foreach ((int id, string name) in new[] { (1, "Ann"), (2, "Bob") })
        {
            insert.ParamByName("Id").AsInteger = id;
            insert.ParamByName("Name").AsString = name;
            insert.ParamByName("Weight").AsFloat = id * 10.5;
            insert.ParamByName("Photo").SetAsBlob("\u0001ÿ" + name);
            insert.ExecSQL();
            Assert.That(insert.RowsAffected, Is.EqualTo(1));
        }

        Assert.That(BoldDBInterfaces.BoldSQLLogCount, Is.EqualTo(logged + 2), "every statement is counted");
        IBoldExecQuery? release = insert;
        db.ReleaseExecQuery(ref release);
        Assert.That(release, Is.Null);

        IBoldQuery query = db.GetQuery();
        query.AssignSQLText("SELECT Id, Name, Weight, Photo FROM Person WHERE Id >= :Min ORDER BY Id");
        query.ParamByName("Min").AsInteger = 1;
        query.Open();
        Assert.That((query.RecordCount, query.FieldCount), Is.EqualTo((2, 4)));
        var names = new List<string>();
        for (query.First(); !query.Eof; query.Next())
        {
            names.Add(query.FieldByName("Name").AsString);
            Assert.That(query.FieldByName("Weight").AsFloat, Is.EqualTo(query.FieldByName("Id").AsInteger * 10.5));
            Assert.That(query.FieldByName("Photo").AsBlob, Is.EqualTo("\u0001ÿ" + names[^1]), "blobs are strings of byte characters");
        }

        Assert.That(names, Is.EqualTo(new[] { "Ann", "Bob" }));
        Assert.That(query.FindField("Missing"), Is.Null);
        Assert.That(query.Fields(1).FieldName, Is.EqualTo("Name"));
        query.Close();
        IBoldQuery? q = query;
        db.ReleaseQuery(ref q);
        Assert.That(db.GetQuery().SQLText, Is.Empty, "the released query is reused, cleared");
    }

    [Test]
    [Covers(typeof(TBoldDbParameter), "SetAsString", "GetAsString")]
    [Covers(typeof(TBoldFieldWrapper), "SetAsString")]
    public void Empty_strings_are_written_as_the_marker()
    {
        TBoldADOConnection db = NewDatabase(out TADOConnection connection, emptyMarker: "<e>");
        IBoldExecQuery insert = db.GetExecQuery();
        insert.AssignSQLText("INSERT INTO Person (Id, Name) VALUES (1, :Name)");
        insert.ParamByName("Name").AsString = "";
        Assert.That(insert.ParamByName("Name").AsString, Is.Empty, "the marker reads as empty");
        insert.ExecSQL();
        Assert.That(TestDatabase.Scalar(connection, "SELECT Name FROM Person"), Is.EqualTo("<e>"));
        IBoldQuery query = db.GetQuery();
        query.AssignSQLText("SELECT Name FROM Person");
        query.Open();
        Assert.That(query.FieldByName("Name").AsString, Is.Empty);
    }

    [Test]
    [Covers(typeof(TBoldADOConnection), "StartTransaction", "Commit", "RollBack", "GetInTransaction", "AllTableNames", "GetConnected", "GetIsSQLBased", "SupportsTableCreation", "ReleaseCachedObjects", "GetTable", "ReleaseTable")]
    [Covers(typeof(TBoldDatabaseWrapper), "TableExists", "GetSQLDatabaseConfig")]
    [Covers(typeof(TBoldADOTable), "GetExists", "SetTableName", "GetTableName", "CreateTable")]
    public void Transactions_and_table_names()
    {
        TBoldADOConnection db = NewDatabase(out TADOConnection connection);
        IBoldDataBase database = db;
        Assert.That(database.Connected && database.IsSqlBased && !database.SupportsTableCreation(), Is.True);
        database.StartTransaction();
        Assert.That(database.InTransaction, Is.True);
        TestDatabase.Exec(connection, "INSERT INTO Person (Id) VALUES (1)");
        database.RollBack();
        Assert.That(TestDatabase.Scalar(connection, "SELECT COUNT(*) FROM Person"), Is.EqualTo(0L));
        database.StartTransaction();
        TestDatabase.Exec(connection, "INSERT INTO Person (Id) VALUES (2)");
        database.Commit();
        Assert.That(TestDatabase.Scalar(connection, "SELECT COUNT(*) FROM Person"), Is.EqualTo(1L));

        TestDatabase.Exec(connection, "CREATE TABLE BOLD_TYPE (Id INTEGER)");
        var names = new TStringList();
        database.AllTableNames("BOLD*", false, names);
        Assert.That(names.Text.Trim(), Is.EqualTo("BOLD_TYPE"));
        Assert.That(database.TableExists("person") && !database.TableExists("Nothing"), Is.True);
        Assert.That(database.SQLDatabaseConfig, Is.Not.Null);

        IBoldTable table = database.GetTable();
        table.Tablename = "Person";
        Assert.That(table.Exists, Is.True);
        Assert.Throws<EBold>(table.CreateTable);
        IBoldTable? t = table;
        database.ReleaseTable(ref t);
        database.ReleaseCachedObjects();
    }

    [Test]
    [Covers(typeof(TBoldDatabaseAdapterADO), "*")]
    [Covers(typeof(TBoldAbstractDatabaseAdapter), "*")]
    [Covers("BoldAbstractDatabaseAdapter", "beDatabaseAdapterChanged")]
    [Covers("ADOConsts", "sAdapterNotConnected")]
    public void The_adapter_makes_the_database_for_its_connection()
    {
        var adapter = new TBoldDatabaseAdapterADO();
        Assert.Throws<EBold>(() => _ = adapter.DatabaseInterface, "no connection");
        adapter.DatabaseEngine = TBoldDataBaseEngine.dbeSQLServer;
        Assert.That(adapter.SQLDatabaseConfig.ColumnTypeForDateTime, Is.EqualTo("DATETIME"), "the engine preset is applied");
        int changes = 0;
        var subscriber = new TBoldPassthroughSubscriber((_, e, _) => changes += e == BoldAbstractDatabaseAdapter.beDatabaseAdapterChanged ? 1 : 0);
        adapter.AddSubscription(subscriber, BoldAbstractDatabaseAdapter.beDatabaseAdapterChanged, BoldAbstractDatabaseAdapter.beDatabaseAdapterChanged);
        adapter.Connection = TestDatabase.NewADOConnection();
        Assert.That(changes, Is.EqualTo(1));
        IBoldDataBase database = adapter.DatabaseInterface;
        Assert.That(adapter.DatabaseInterface, Is.SameAs(database));
        Assert.That(database.SQLDatabaseConfig.ColumnTypeForDateTime, Is.EqualTo("DATETIME"), "the database has a copy of the configuration");
        adapter.Dispose();
    }
}
