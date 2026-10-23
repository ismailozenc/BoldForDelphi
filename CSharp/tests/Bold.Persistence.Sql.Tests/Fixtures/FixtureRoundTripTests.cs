// Delphi: Source/PMapper/Default/BoldPMappersDefault.pas, Source/Persistence/DB/BoldPersistenceHandleDB.pas
// Port map: CSharp/port/units/BoldPersistenceHandleDB.md
// The M5 closing test: every fixture model gets a SQLite schema, and objects of each concrete persistent class, with
// every stored attribute set and every role linked, save, load into a new system and compare equal.
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
        var system = new TBoldSystem(null, systemTypeInfo, controller);
        TBoldSystem? second = null;
        try
        {
            // Multi-language attributes read their languages from the default system, which a system handle would set.
            system.MakeDefault();
            SystemFiller.Fill(system);
            system.UpdateDatabase();
            Assert.That(system.BoldDirty, Is.False);

            second = new TBoldSystem(null, systemTypeInfo, controller);
            second.MakeDefault();
            // Members stored externally are left to an external persistence controller; the database does not keep them.
            SystemFiller.AssertSame(system, second, m =>
                model.Classes.ItemsByName(m.ClassTypeInfo.ModelName)!.AllBoldMembers.ItemsByName(m.ModelName)?.Storage == TBoldStorage.bsExternal);
        }
        finally
        {
            // A failure leaves a system dirty; discard it so disposing does not hide the failure.
            foreach (TBoldSystem? s in new[] { system, second })
            {
                s?.Discard();
                s?.Dispose();
            }
        }
    }
}
