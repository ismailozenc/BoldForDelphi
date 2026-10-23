// Delphi: Source/Persistence/DB/BoldDBActions.pas
// Port map: CSharp/port/units/BoldDBActions.md
using System.Data.Common;

namespace Bold.Tests;

[NonParallelizable]
public class BoldDBActionsTests
{
    [Test]
    [Covers(typeof(TBoldGenerateSchemaAction), "*")]
    public void Generate_schema_creates_the_tables_of_an_inactive_handle()
    {
        using var values = new ValuesSample();
        using var db = new SqliteHandle(values.Model, createSchema: false) { KeepFile = false };
        using var components = new System.ComponentModel.Container();
        var action = new TBoldGenerateSchemaAction(components);
        Assert.That(action.Caption, Is.EqualTo(PersistenceConsts.sGenerateSchema));
        Assert.That(action.HandlesTarget(null), Is.False, "no handle");

        var noAdapter = new TBoldPersistenceHandleDB();
        action.BoldPersistenceHandleDB = noAdapter;
        Assert.That(action.HandlesTarget(null), Is.False, "no database adapter");
        noAdapter.Dispose();
        Assert.That(action.BoldPersistenceHandleDB, Is.Null, "a disposed handle is forgotten");

        action.BoldPersistenceHandleDB = db.Handle;
        action.IgnoreUnknownTables = true;
        Assert.That(action.IgnoreUnknownTables, Is.True);
        Assert.That(action.HandlesTarget(null), Is.True);
        Assert.That(action.Execute(), Is.True);
        Assert.That(Tables(db.Connection), Does.Contain("Thing").IgnoreCase);

        db.OpenController();
        Assert.That(action.HandlesTarget(null), Is.False, "an active handle");
        db.Handle.Active = false;
    }

    private static List<string> Tables(TADOConnection connection)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = "select name from sqlite_master where type = 'table'";
        using DbDataReader reader = command.ExecuteReader();
        var result = new List<string>();
        while (reader.Read())
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }
}
