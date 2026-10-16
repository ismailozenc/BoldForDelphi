// Delphi: Source/Unassigned/AutoInc/BoldAttributeAutoInc.pas, BoldPMAutoInc.pas
// Port map: CSharp/port/units/BoldAttributeAutoInc.md
namespace Bold.Tests;

public class BoldAutoIncTests
{
    /// <summary>The sample with Person.number of the model type AutoInc (TBAAutoInc, mapped by TBoldPMAutoInc).</summary>
    private static ObjectSpaceSample Sample(Func<TMoldModel, TBoldPersistenceController?>? controller = null)
    {
        // The registrations run with the assembly's module initializer, which nothing else here may have triggered yet.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(TBAAutoInc).Module.ModuleHandle);
        return new ObjectSpaceSample(
        m =>
        {
            TBoldTypeNameMapping mapping = m.TypeNameDictionary!.AddMapping();
            mapping.ModelName = "AutoInc";
            mapping.ExpressionName = "AutoInc";
            mapping.DelphiName = "TBAAutoInc";
            mapping.ContentsName = BoldDefaultStreamNames.BoldContentName_Integer;
            mapping.MapperName = "TBoldPMAutoInc";
            // SQLite has no identity columns, so the column must take the null the insert leaves.
            new TMoldAttribute(m.Classes.ItemsByName("Person")!, "number") { BoldType = "AutoInc" }.SetBoldTV(BoldDefaultTaggedValues.TAG_ALLOWNULL, "True");
        },
        controller);
    }

    [Test]
    [Covers(typeof(TBAAutoInc), "*")]
    [Covers(typeof(BoldAttributeAutoInc), "Initialization")]
    public void An_auto_increment_attribute_starts_at_minus_one_and_cannot_be_modified()
    {
        using ObjectSpaceSample s = Sample();
        TBoldObject p = s.NewPerson();
        var number = (TBAAutoInc)p.BoldMemberByExpressionName("number");
        Assert.That(number.AsInteger, Is.EqualTo(-1));
        Assert.That(number.CanModify(), Is.False);
        Assert.That(BoldSystem.GetBoldLastFailureReason()?.Reason, Does.Contain("can not be modified"));
        Assert.That(() => number.AsInteger = 5, Throws.InstanceOf<EBold>());
    }

    [Test]
    [Covers(typeof(TBoldPMAutoInc), "*")]
    [Covers(typeof(BoldPMAutoInc), "Initialization")]
    public void The_mapper_declares_an_identity_column_and_does_not_write_it()
    {
        SqliteHandle? db = null;
        using ObjectSpaceSample s = Sample(m =>
        {
            db = new SqliteHandle(m);
            return db.OpenController();
        });
        using SqliteHandle database = db!;
        Assert.That(TestDatabase.Scalar(database.Connection, "SELECT type FROM pragma_table_info('Person') WHERE name = 'number'"), Does.EndWith("IDENTITY(1, 1)"));
        s.NewPerson().BoldMemberByExpressionName("firstName").AsString = "Ann";
        s.System.UpdateDatabase();
        // SQLite has no identity columns: the column the database would fill stays empty.
        Assert.That(TestDatabase.Scalar(database.Connection, "SELECT number FROM Person"), Is.Null);
    }
}
