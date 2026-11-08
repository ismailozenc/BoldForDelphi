// Delphi: Source/Extensions/OLLE/Core/BoldOLLEdmmain.pas
// Port map: CSharp/port/units/BoldOLLEDmmain.md

namespace Bold.Tests;

/// <summary>The OLLE data module: the OLLE model and a system on it.</summary>
public class BoldOLLEdmmainTests
{
    [Test]
    [Covers(typeof(TdmOLL))]
    [Covers("BoldOLLEDmmain", "initialization")]
    public void The_data_module_runs_a_system_on_the_OLLE_model()
    {
        using var dm = new TdmOLL();
        Assert.That(dm.BoldModel1.MoldModel.Name, Is.EqualTo("DistributableInfo"));
        Assert.That(Enumerable.Range(0, dm.BoldModel1.MoldModel.Classes.Count).Select(i => dm.BoldModel1.MoldModel.Classes[i]!.Name), Is.SupersetOf(new[]
        {
            "DistributableObjectInfo", "OwnObjectInfo", "ForeignObjectInfo", "HeldObjectInfo", "CheckedOutObjectInfo", "CheckingInObjectInfo", "PSInfo",
            "ForeignPSInfo", "OwnPSInfo", "Mapping",
        }));
        Assert.That(dm.BoldSystemTypeInfoHandle1.StaticSystemTypeInfo!.SystemIsRunnable, Is.True);
        Assert.That((dm.BoldObjectInfoSystem.SystemTypeInfoHandle, dm.BoldObjectInfoSystem.IsDefault), Is.EqualTo((dm.BoldSystemTypeInfoHandle1, false)),
            "the OLLE system is not the application's default system");
        dm.BoldObjectInfoSystem.Active = true;
        Assert.That(dm.BoldObjectInfoSystem.System!.PersistenceController, Is.Null, "transient until the controller gives it persistence");
        Assert.That(dm.Components.Components, Has.Count.EqualTo(3));
    }
}
