// Delphi: Source/PMapper/SQL/BoldPSDescriptionsSQL.pas, Source/PMapper/SQL/BoldSQLDatabaseConfig.pas
// Port map: CSharp/port/units/BoldPSDescriptionsSQL.md
// The M5 schema snapshots: the CREATE statements of the Building model for each dialect, compared with the files in
// CSharp/tests/Fixtures/Schemas. A missing snapshot is written and the test fails, so a new one is reviewed first.
namespace Bold.Tests;

public class SchemaSnapshotTests
{
    private const string Model = "BuildingClasses.Delphi-Compound-Building.bld";

    /// <summary>The tables of the model's default mapping on the dialect, with their secondary indexes.</summary>
    internal static string Schema(string modelFile, TBoldDataBaseEngine engine)
    {
        using TMoldModel model = TMoldBLDRW.ModelFromFile(FixturePaths.Model(modelFile));
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        model.TypeNameDictionary = types;
        var config = new TBoldSQLDataBaseConfig();
        config.InitializeDbEngineSettings(engine);
        var mapper = new TBoldSystemDefaultMapper(model, types, config, null);
        TBoldSQLSystemDescription description = mapper.PSSystemDescription;
        var lines = new List<string>();
        for (int i = 0; i < description.SQLTablesList.Count; i++)
        {
            var table = (TBoldSQLTableDescription)description.SQLTablesList[i]!;
            lines.Add(table.SQLForCreateTable(null).Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd() + config.SqlScriptTerminator);
            for (int j = 0; j < table.IndexList.Count; j++)
            {
                if (!table.IndexList[j].IndexOptions.Contains(TIndexOption.ixPrimary))
                {
                    lines.Add(table.IndexList[j].SQLForSecondaryKey() + config.SqlScriptTerminator);
                }
            }
        }

        return string.Join("\n", lines) + "\n";
    }

    [TestCase(TBoldDataBaseEngine.dbeSQLServer, "SQLServer")]
    [TestCase(TBoldDataBaseEngine.dbeSQLite, "SQLite")]
    [TestCase(TBoldDataBaseEngine.dbeGenericANSISQL92, "ANSI")]
    [Covers(typeof(TBoldSQLDataBaseConfig), "InitializeDbEngineSettings")]
    [Covers(typeof(TBoldSQLTableDescription), "SQLForCreateTable")]
    public void The_schema_of_the_Building_model_matches_its_snapshot(TBoldDataBaseEngine engine, string dialect)
    {
        string actual = Schema(Model, engine);
        string file = RepoPaths.Full($"CSharp/tests/Fixtures/Schemas/Building.{dialect}.sql");
        if (!File.Exists(file))
        {
            File.WriteAllText(file, actual);
            Assert.Fail($"The snapshot {file} was missing and has been written; review it and run again.");
        }

        Assert.That(actual, Is.EqualTo(File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal)));
    }

    [Test]
    public void The_SQLite_schema_creates_on_SQLite()
    {
        string schema = Schema(Model, TBoldDataBaseEngine.dbeSQLite);
        string file = Path.Combine(Path.GetTempPath(), $"bold-schema-{Guid.NewGuid():N}.db");
        TADOConnection connection = TestDatabase.NewADOConnection(file);
        try
        {
            foreach (string statement in schema.Split(";\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                TestDatabase.Exec(connection, statement.TrimEnd(';'));
            }
        }
        finally
        {
            connection.Close();
            connection.Connection.Dispose();
            File.Delete(file);
        }
    }
}
