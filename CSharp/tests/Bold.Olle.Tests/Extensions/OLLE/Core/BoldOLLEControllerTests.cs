// Delphi: Source/Extensions/OLLE/Core/BoldOLLEController.pas
// Port map: CSharp/port/units/BoldOLLEController.md
using Bold.Olle;

namespace Bold.Tests;

/// <summary>The OLLE system of an application: in the application's database, under table names of its own, or in memory.</summary>
[NonParallelizable]
public class BoldOLLEControllerTests
{
    private OlleStorage fStorage = null!;

    [SetUp]
    public void SetUp() => fStorage = new OlleStorage("S");

    [TearDown]
    public void TearDown() => fStorage.Dispose();

    private bool HasTable(string name) =>
        Convert.ToInt32(TestDatabase.Scalar(fStorage.Connection, $"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = '{name}'")) == 1;

    [Test]
    [Covers(typeof(TBoldOLLEController), ".ctor", nameof(TBoldOLLEController.GenerateDatabase), nameof(TBoldOLLEController.OLLESystem), "GetOLLESystem",
        nameof(TBoldOLLEController.Persistent), "GetPersistent")]
    [Covers(typeof(TBoldPHandleMimic), "GetDataBaseInterface", "GetSQLDatabaseConfig")]
    public void The_OLLE_system_keeps_its_objects_in_the_application_database()
    {
        Assert.That(HasTable("OLLE_TYPE") && HasTable("OLLE_OBJECT") && HasTable("DistributableObjectInfo") && HasTable("PSInfo"), Is.True,
            "the OLLE tables, with OLLE system tables");
        Assert.That(HasTable("BOLD_OBJECT"), Is.True, "beside the application's");
        TBoldOLLEController controller = fStorage.OLLEHandle.OLLEController;
        Assert.That(controller.Persistent, Is.True);
        TBoldSystem olle = controller.OLLESystem;
        Assert.That(controller.OLLESystem, Is.SameAs(olle));
        Assert.That(olle.PersistenceController, Is.Not.Null);
        _ = new TForeignPSInfo(olle) { GlobalID = "B" };
        olle.UpdateDatabase();

        using var second = new TBoldOLLEController(fStorage.DatabaseHandle);
        TBoldObjectList psInfos = second.OLLESystem.ClassByExpressionName("ForeignPSInfo");
        Assert.That(((TForeignPSInfo)psInfos[0]!).GlobalID, Is.EqualTo("B"), "another OLLE system on the database reads it");
    }

    [Test]
    [Covers(typeof(TBoldOLLEController), "SetPersistent")]
    [Covers(typeof(OlleConsts), nameof(OlleConsts.sCannotChangePersistenceWhenActive))]
    public void A_transient_OLLE_system_keeps_its_objects_in_memory()
    {
        using var controller = new TBoldOLLEController(fStorage.DatabaseHandle) { Persistent = false };
        Assert.That(controller.Persistent, Is.False);
        TBoldSystem olle = controller.OLLESystem;
        Assert.That(olle.PersistenceController, Is.Null);
        var b = new TForeignPSInfo(olle) { GlobalID = "B" };
        Assert.That(olle.ClassByExpressionName("ForeignPSInfo")[0], Is.SameAs(b));
        Assert.That(() => controller.Persistent = true,
            Throws.InstanceOf<EBold>().With.Message.EqualTo("TBoldOLLEController: Can not change Persistent-property when the OLLE system is active"));
    }

    [Test]
    [Covers(typeof(TBoldPHandleMimic), "Dispose")]
    public void Disposing_the_controller_closes_its_system()
    {
        var controller = new TBoldOLLEController(fStorage.DatabaseHandle);
        TBoldSystem olle = controller.OLLESystem;
        _ = new TForeignPSInfo(olle) { GlobalID = "B" };
        controller.Dispose();
        Assert.That(() => fStorage.NewPerson("Ann", "Archer"), Throws.Nothing, "the application goes on with its database");
    }
}
