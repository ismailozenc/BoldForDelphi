// Delphi: Source/PMapper/DbEvolutor/BoldDbEvolutor.pas, BoldDbEvolutorScript.pas
// Port map: CSharp/port/units/BoldDbEvolutor.md
namespace Bold.Tests;

public class BoldDbEvolutorTests
{
    /// <summary>The sample's next version: Person gains nickname and loses weight, and there is a new class Garage.</summary>
    private static void NextVersion(TMoldModel m)
    {
        TMoldClass person = m.Classes.ItemsByName("Person")!;
        _ = new TMoldAttribute(person, "nickname");
        person.Attributes.ItemsByName("weight")!.Dispose();
        var garage = new TMoldClass(m, "Garage") { SuperClass = m.RootClass };
        _ = new TMoldAttribute(garage, "spaces") { BoldType = "Integer" };
    }

    /// <summary>A database file of the sample with Ann (42) and a building stored.</summary>
    private static string StoreFirstVersion()
    {
        SqliteHandle? db = null;
        using var s = new ObjectSpaceSample(controller: m =>
        {
            db = new SqliteHandle(m) { KeepFile = true };
            return db.OpenController();
        });
        using SqliteHandle database = db!;
        TBoldObject ann = s.NewPerson();
        ann.BoldMemberByExpressionName("firstName").AsString = "Ann";
        ((TBAInteger)ann.BoldMemberByExpressionName("age")).AsInteger = 42;
        s.NewBuilding().BoldMemberByExpressionName("address").AsString = "1 Main St";
        s.System.UpdateDatabase();
        return database.File;
    }

    [Test]
    [Covers(typeof(TBoldDataBaseEvolutor), "*")]
    [Covers(typeof(TBoldDataBaseEvolutorScript), "*")]
    [Covers(typeof(TBoldAddTable), "*")]
    [Covers(typeof(TBoldAddColumn), "*")]
    [Covers(typeof(TBoldDropColumn), "*")]
    public void The_evolutor_adds_and_drops_tables_and_columns_and_keeps_the_data()
    {
        string file = StoreFirstVersion();
        var next = new ObjectSpaceSample(NextVersion);
        using var db = new SqliteHandle(next.Model, createSchema: false, file: file);
        try
        {
            var evolutor = new TBoldDataBaseEvolutor(db.Handle);
            evolutor.CalculateScript();
            var dbScript = new TStringList();
            var mappingScript = new TStringList();
            evolutor.GenerateScript(dbScript, mappingScript);
            string script = dbScript.Text;
            Assert.That(script, Does.Contain("CREATE TABLE Garage"));
            Assert.That(script, Does.Contain("ALTER TABLE Person ADD nickname"));
            Assert.That(script, Does.Match("(?i)ALTER TABLE Person DROP.*weight"));
            Assert.That(mappingScript.Text, Does.Contain("Garage"));
            var warnings = new TStringList();
            evolutor.GenerateWarnings(warnings);
            Assert.That(warnings.Text, Does.Contain("weight"));

            evolutor.ExecuteScript();
            var executed = new TStringList();
            evolutor.GenerateExecutedStatements(executed);
            Assert.That(executed.Text, Does.Contain("CREATE TABLE Garage"));
            Assert.That(TestDatabase.Scalar(db.Connection, "SELECT COUNT(*) FROM pragma_table_info('Person') WHERE name = 'weight'"), Is.EqualTo(0));

            TBoldSystem system = next.NewSystem(db.OpenController());
            TBoldObjectList people = system.Classes(next.SystemTypeInfo.ClassTypeInfoByExpressionName("Person")!.TopSortedIndex)!;
            Assert.That(people.Count, Is.EqualTo(1));
            Assert.That(((TBAInteger)people[0]!.BoldMemberByExpressionName("age")).AsInteger, Is.EqualTo(42));
            people[0]!.BoldMemberByExpressionName("nickname").AsString = "Annie";
            TBoldObject garage = system.CreateNewObjectByExpressionName("Garage");
            ((TBAInteger)garage.BoldMemberByExpressionName("spaces")).AsInteger = 2;
            system.UpdateDatabase();
            Assert.That(TestDatabase.Scalar(db.Connection, "SELECT nickname FROM Person"), Is.EqualTo("Annie"));
            Assert.That(TestDatabase.Scalar(db.Connection, "SELECT spaces FROM Garage"), Is.EqualTo(2));
        }
        finally
        {
            next.Dispose();
        }
    }

    [Test]
    [Covers(typeof(TBoldMoveData), "*")]
    [Covers(typeof(TBoldCopyInstances), "*")]
    [Covers(typeof(TBoldDeleteInstances), "*")]
    [Covers(typeof(TBoldDropTable), "*")]
    [Covers(typeof(TBoldDropIndex), "*")]
    public void The_script_moves_copies_and_deletes_data_in_one_transaction()
    {
        var config = new TBoldSQLDataBaseConfig();
        config.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeGenericANSISQL92);
        var script = new TBoldDataBaseEvolutorScript();
        script.MoveData("Old", "New", "a", "a2", "BOLD_ID", 3);
        script.MoveData("Old", "New", "b", "b", "BOLD_ID", 3);
        script.MoveData("Old", "New", "a", "a2", "BOLD_ID", 4);
        script.MoveData("Old", "New", "b", "b", "BOLD_ID", 4);
        script.MoveData("T", "T", "x", "x", "BOLD_ID", 5);
        script.MoveData("T", "T", "y", "y2", "BOLD_ID", 5);
        script.CopyInstances("Gadget", "Root", "Gadget", "BOLD_ID", 7);
        script.MoveData("Root", "Gadget", "price", "price", "BOLD_ID", 7);
        script.DeleteInstances("Thing", "Things", 9);
        script.DropIndex("IX_Old", "Old");
        script.DropTable("Old");
        script.OptimizeScript();
        var sql = new TStringList();
        script.GenerateScript(sql, config);
        string text = sql.Text;
        Assert.That(text, Does.Contain("UPDATE New Target SET a2 = (SELECT a FROM Old Source WHERE Source.BOLD_ID = Target.BOLD_ID), " +
                                       "b = (SELECT b FROM Old Source WHERE Source.BOLD_ID = Target.BOLD_ID) WHERE BOLD_TYPE IN (4, 3)")
            .Or.Contain("WHERE BOLD_TYPE IN (3, 4)"));
        // A column that stays where it is is not moved.
        Assert.That(text, Does.Contain("UPDATE T SET y2 = y WHERE BOLD_TYPE IN (5)"));
        Assert.That(text, Does.Contain("INSERT INTO Gadget (BOLD_TYPE, BOLD_ID, price) SELECT Root.BOLD_TYPE, Root.BOLD_ID, Root.price FROM Root WHERE BOLD_TYPE = 7"));
        Assert.That(text, Does.Contain("DELETE FROM Things WHERE BOLD_TYPE = 9"));
        Assert.That(text, Does.Contain(config.GetDropIndexQuery("Old", "IX_Old")));
        Assert.That(text, Does.Contain(config.GetDropTableQuery("Old")));
        Assert.That(text.IndexOf(config.SqlScriptStartTransaction, StringComparison.Ordinal), Is.LessThan(text.IndexOf("INSERT INTO Gadget", StringComparison.Ordinal)));
    }
}
