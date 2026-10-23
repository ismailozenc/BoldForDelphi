// Delphi: Source/PMapper/Default/BoldPMappersDefault.pas, Source/Persistence/DB/BoldPersistenceHandleDB.pas
// Port map: CSharp/port/units/BoldPersistenceHandleDB.md
// The M5 closing test: every fixture model gets a SQLite schema, and an object of each concrete persistent class
// saves and loads into a new system.
namespace Bold.Tests;

public class FixtureRoundTripTests
{
    public static IEnumerable<FixtureModelInfo> Fixtures => FixturePaths.Manifest;

    [TestCaseSource(nameof(Fixtures))]
    public void Fixture_models_round_trip_through_SQLite(FixtureModelInfo info)
    {
        TMoldModel model = TMoldBLDRW.ModelFromFile(FixturePaths.Model(info.File));
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        model.TypeNameDictionary = types;
        if (model.TVByName(BoldDefaultTaggedValues.BOLDINTERALTVPREFIX + BoldDefaultTaggedValues.TV_MODELERRORS) != "")
        {
            Assert.Ignore("The model has errors, so it has no runtime model (see the runtime model fixture test).");
        }

        if (info.File == "BoldUMLModel.bld")
        {
            // The UML metamodel is used in memory. Its role "constraint" is an SQL reserved word, which Delphi Bold only
            // warns about (in the UML model validator); the database refuses the column, as it does for Delphi.
            Assert.Ignore("The UML metamodel has a role named constraint, an SQL reserved word.");
        }

        using var systemTypeInfo = new TBoldSystemTypeInfo(model, false, false, types);
        using var db = new SqliteHandle(model);
        TBoldPersistenceController controller = db.OpenController();
        var created = new Dictionary<string, int>();
        var system = new TBoldSystem(null, systemTypeInfo, controller);
        // Multi-language attributes read their languages from the default system, which a system handle would set.
        system.MakeDefault();
        try
        {
            for (int i = 0; i < systemTypeInfo.TopSortedClasses.Count; i++)
            {
                TBoldClassTypeInfo c = systemTypeInfo.TopSortedClasses[i];
                if (!c.IsAbstract && c.Persistent && !c.IsLinkClass)
                {
                    system.CreateNewObjectByExpressionName(c.ExpressionName);
                    created[c.ExpressionName] = 1;
                }
            }

            system.UpdateDatabase();
            Assert.That(system.BoldDirty, Is.False);
        }
        finally
        {
            // A failure leaves the system dirty; discard it so disposing does not hide the failure.
            system.Discard();
            system.Dispose();
        }

        using var second = new TBoldSystem(null, systemTypeInfo, controller);
        second.MakeDefault();
        foreach ((string className, _) in created)
        {
            TBoldClassTypeInfo c = systemTypeInfo.ClassTypeInfoByExpressionName(className)!;
            TBoldObjectList list = second.Classes(c.TopSortedIndex)!;
            int expected = created.Keys.Count(k => systemTypeInfo.ClassTypeInfoByExpressionName(k)!.BoldIsA(c));
            Assert.That(list.Count, Is.EqualTo(expected), className);
        }

        second.Discard();
    }
}
