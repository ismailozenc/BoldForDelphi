// Delphi: Source/PMapper/SQL/BoldSQLDatabaseConfig.pas
// Port map: CSharp/port/units/BoldSQLDatabaseConfig.md
namespace Bold.Tests;

public class BoldSQLDatabaseConfigTests
{
    [Test]
    [Covers(typeof(TBoldSQLDataBaseConfig), "*")]
    public void Engine_presets_templates_and_copies()
    {
        var config = new TBoldSQLDataBaseConfig();
        Assert.That((config.ColumnTypeForString, config.DefaultStringLength, config.FetchBlockSize, config.SystemTablePrefix),
            Is.EqualTo(("VARCHAR(%d)", 255, 250, "BOLD")));
        Assert.That(config.ReservedWords.Text, Does.Contain("SELECT"));
        int changes = 0;
        config.OnChange = _ => changes++;
        config.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeOracle);
        Assert.That((config.ColumnTypeForString, config.MaxIndexNameLength), Is.EqualTo(("VARCHAR2(%d)", 30)));
        Assert.That(changes, Is.GreaterThan(0));
        Assert.That(config.GetDropColumnQuery("T", "C"), Is.EqualTo("ALTER TABLE T DROP COLUMN C"));
        Assert.That(config.GetDropTableQuery("T"), Is.EqualTo("DROP TABLE T"));
        config.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLServer);
        Assert.That(config.GetDropIndexQuery("T", "I"), Is.EqualTo("DROP INDEX T.I"));

        config.SQLforNotNull = "";
        Assert.That((config.SQLforNotNull, config.EffectiveSQLForNotNull), Is.EqualTo(("<Empty>", "")), "an empty clause is kept as a marker");
        config.SystemTablePrefix = "";
        Assert.That(config.SystemTablePrefix, Is.EqualTo("BOLD"));
        config.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeAdvantage);
        Assert.That(config.CorrectlyQuotedDefaultValue("1"), Is.EqualTo("'1'"));

        var copy = new TBoldSQLDataBaseConfig();
        copy.Assign(config);
        Assert.That((copy.ColumnTypeForString, copy.QuoteNonStringDefaultValues, copy.StoreEmptyStringsAsNULL), Is.EqualTo(("CHAR(%d)", true, true)));
        config.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeParadox);
        Assert.That(config.DBGenerationMode, Is.EqualTo(TBoldDatabaseGenerationMode.dbgTable));
        Assert.Throws<EConvertError>(() => copy.Assign(new TPersistent()));
    }
}
