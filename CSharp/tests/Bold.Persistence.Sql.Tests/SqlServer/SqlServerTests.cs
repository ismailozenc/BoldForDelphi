// Delphi: Source/Persistence/DB/BoldPersistenceHandleDB.pas, Source/PMapper/SQL/BoldSQLDatabaseConfig.pas
// Port map: CSharp/port/units/BoldPersistenceHandleDB.md
// The SQL Server tests (decision D4: SQL Server in production). They run in the CI job "SQL Server", which starts a
// server and sets BOLD_SQLSERVER to its connection string; elsewhere they are ignored. Offline builds have no
// SqlClient package, so the file compiles only in normal builds.
#if !BOLD_OFFLINE
using Microsoft.Data.SqlClient;

namespace Bold.Tests;

[Category("SqlServer")]
public class SqlServerTests
{
    /// <summary>A new database on the server, with the schema of a model; dropped on dispose.</summary>
    private sealed class SqlServerHandle : IDisposable
    {
        private readonly string fServer;
        private readonly string fDatabase = $"bold_{Guid.NewGuid():N}";
        public readonly TADOConnection Connection;
        public readonly TBoldDatabaseAdapterADO Adapter = new();
        public readonly TBoldPersistenceHandleDB Handle = new();
        private readonly TBoldAbstractModelForSqlTests fModel;

        public SqlServerHandle(string server, TMoldModel model)
        {
            fServer = server;
            Execute(fServer, $"CREATE DATABASE [{fDatabase}]");
            var connection = new SqlConnection(new SqlConnectionStringBuilder(server) { InitialCatalog = fDatabase }.ConnectionString);
            connection.Open();
            Connection = new TADOConnection(connection);
            Adapter.Connection = Connection;
            Adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLServer);
            fModel = new TBoldAbstractModelForSqlTests(model);
            Handle.DatabaseAdapter = Adapter;
            Handle.BoldModel = fModel;
            Handle.CreateDataBaseSchema();
        }

        private static void Execute(string connectionString, string sql)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using SqlCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

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
            SqlConnection.ClearAllPools();
            Execute(fServer, $"ALTER DATABASE [{fDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{fDatabase}]");
        }
    }

    private static string Server => Environment.GetEnvironmentVariable("BOLD_SQLSERVER") is { Length: > 0 } s
        ? s
        : throw new IgnoreException("BOLD_SQLSERVER is not set; the SQL Server job sets it.");

    private static T Member<T>(TBoldObject o, string name) where T : TBoldMember => (T)o.BoldMemberByExpressionName(name);

    [Test]
    public void Objects_round_trip_through_SQL_Server_and_OCL_runs_as_SQL()
    {
        string server = Server;
        SqlServerHandle? db = null;
        using var s = new ObjectSpaceSample(controller: m =>
        {
            db = new SqlServerHandle(server, m);
            return db.OpenController();
        });
        using SqlServerHandle database = db!;
        TBoldObject ann = s.NewPerson(), bob = s.NewPerson();
        TBoldObject house = s.NewBuilding();
        ann.BoldMemberByExpressionName("firstName").AsString = "Ann";
        Member<TBAInteger>(ann, "age").AsInteger = 42;
        Member<TBACurrency>(ann, "salary").AsCurrency = 1234.5m;
        Member<TBADate>(ann, "born").AsDate = new DateTime(1990, 12, 24);
        bob.BoldMemberByExpressionName("firstName").AsString = "Bob";
        Member<TBAInteger>(bob, "age").AsInteger = 17;
        house.BoldMemberByExpressionName("address").AsString = "1 Main St";
        Member<TBoldObjectReference>(ann, "home").BoldObject = house;
        Member<TBoldObjectList>(ann, "friends").Add(bob);
        s.System.UpdateDatabase();

        TBoldSystem second = s.NewSystem(database.Handle.PersistenceController);
        TBoldObjectList people = second.Classes(s.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex)!;
        TBoldObject ann2 = Enumerable.Range(0, people.Count).Select(i => people[i]!).Single(p => p.BoldMemberByExpressionName("firstName").AsString == "Ann");
        Assert.That(Member<TBAInteger>(ann2, "age").AsInteger, Is.EqualTo(42));
        Assert.That(Member<TBACurrency>(ann2, "salary").AsCurrency, Is.EqualTo(1234.5m));
        Assert.That(Member<TBADate>(ann2, "born").AsDate, Is.EqualTo(new DateTime(1990, 12, 24)));
        Assert.That(Member<TBoldObjectReference>(ann2, "home").BoldObject?.BoldMemberByExpressionName("address").AsString, Is.EqualTo("1 Main St"));
        Assert.That(Member<TBoldObjectList>(ann2, "friends").Count, Is.EqualTo(1));

        TBoldSystem third = s.NewSystem(database.Handle.PersistenceController);
        var result = new TBoldIndirectElement();
        third.EvaluateExpression("Person.allInstances->select(p | p.age > 26)", result, true);
        var adults = (TBoldObjectList)result.Value!;
        Assert.That(adults.Count, Is.EqualTo(1));
        Assert.That(adults[0]!.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Ann"));
    }

    [Test]
    public void The_Building_model_schema_creates_on_SQL_Server()
    {
        string server = Server;
        using TMoldModel model = TMoldBLDRW.ModelFromFile(FixturePaths.Model("BuildingClasses.Delphi-Compound-Building.bld"));
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        model.TypeNameDictionary = types;
        using var db = new SqlServerHandle(server, model);
        var tables = new TStringList();
        db.Connection.GetTableNames(tables);
        Assert.That(tables.IndexOf("Building"), Is.Not.EqualTo(-1));
        Assert.That(tables.IndexOf("Person"), Is.Not.EqualTo(-1));
    }
}
#endif
