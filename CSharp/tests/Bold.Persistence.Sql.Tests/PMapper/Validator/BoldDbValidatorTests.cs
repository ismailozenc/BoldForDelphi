// Delphi: Source/PMapper/Validator/BoldDbValidator.pas, BoldDbStructureValidator.pas, BoldDbDataValidator.pas
// Port map: CSharp/port/units/BoldDbValidator.md
namespace Bold.Tests;

public class BoldDbValidatorTests
{
    private static string Remedies(TBoldDbValidator v) => string.Join("\n", Enumerable.Range(0, v.remedy.Count).Select(i => v.remedy[i]));

    [Test]
    [Covers(typeof(TBoldDbValidator), "*")]
    [Covers(typeof(TBoldDbStructureValidator), "*")]
    public void The_structure_validator_finds_missing_tables_and_columns()
    {
        using var s = new ObjectSpaceSample();
        using var db = new SqliteHandle(s.Model);
        using var valid = new TBoldDbStructureValidator { PersistenceHandle = db.Handle };
        Assert.That(valid.Execute(), Is.True, () => Remedies(valid));
        Assert.That(db.Handle.Active, Is.False);

        TestDatabase.Exec(db.Connection, "DROP TABLE Building");
        TestDatabase.Exec(db.Connection, "ALTER TABLE Person DROP COLUMN age");
        using var broken = new TBoldDbStructureValidator { PersistenceHandle = db.Handle };
        Assert.That(broken.Execute(), Is.False);
        string remedies = Remedies(broken);
        Assert.That(remedies, Does.Contain("CREATE TABLE Building"));
        Assert.That(remedies, Does.Contain("alter table Person add age INTEGER"));
    }

    [Test]
    [Covers(typeof(TBoldDbDataValidator), "*")]
    public void The_data_validator_finds_broken_links_missing_rows_and_stray_objects()
    {
        SqliteHandle? db = null;
        using var s = new ObjectSpaceSample(controller: m =>
        {
            db = new SqliteHandle(m);
            return db.OpenController();
        });
        using SqliteHandle database = db!;
        TBoldObject ann = s.NewPerson(), bob = s.NewPerson();
        ann.BoldMemberByExpressionName("firstName").AsString = "Ann";
        bob.BoldMemberByExpressionName("firstName").AsString = "Bob";
        TBoldObject house = s.NewBuilding();
        ((TBoldObjectReference)ann.BoldMemberByExpressionName("home")).BoldObject = house;
        ((TBoldObjectList)ann.BoldMemberByExpressionName("friends")).Add(bob);
        s.System.UpdateDatabase();
        database.Handle.Active = false;

        using (var clean = new TBoldDbDataValidator { PersistenceHandle = database.Handle })
        {
            Assert.That(clean.Execute(), Is.True, () => Remedies(clean));
        }

        // The house loses its Building row, and a friendship loses an end.
        TestDatabase.Exec(database.Connection, "DELETE FROM Building");
        TestDatabase.Exec(database.Connection, "UPDATE Friendship SET friends = -1");
        using var validator = new TBoldDbDataValidator { PersistenceHandle = database.Handle };
        Assert.That(validator.Execute(), Is.False);
        string remedies = Remedies(validator);
        Assert.That(remedies, Does.Contain("INSERT INTO Building (BOLD_ID, BOLD_TYPE"));
        Assert.That(remedies, Does.Contain("UPDATE Person SET home = -1 WHERE BOLD_ID IN ("));
        Assert.That(remedies, Does.Contain("DELETE FROM Friendship WHERE BOLD_ID IN ("));
    }
}
