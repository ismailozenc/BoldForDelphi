// Delphi: Source/Extensions/OLLE/Core/BoldOLLEHandles.pas
// Port map: CSharp/port/units/BoldOLLEHandles.md

namespace Bold.Tests;

/// <summary>The OLLE handle: the controller and both handlers on the application's persistence handle.</summary>
[NonParallelizable]
public class BoldOLLEHandlesTests
{
    [Test]
    [Covers(typeof(TBoldOLLEHandle), nameof(TBoldOLLEHandle.OLLEController), "GetOLLEController", nameof(TBoldOLLEHandle.OwnObjectHandler), "GetOwnObjectHandler",
        nameof(TBoldOLLEHandle.ForeignObjectHandler), "GetForeignObjectHandler", nameof(TBoldOLLEHandle.ApplicationPersistenceHandle), "GetHandledObject")]
    [Covers(typeof(TBoldDistributableObjectHandler), nameof(TBoldDistributableObjectHandler.OllSystem), nameof(TBoldDistributableObjectHandler.PController))]
    public void The_handlers_share_the_OLLE_system_and_the_application_persistence()
    {
        using var storage = new OlleStorage("S");
        TBoldOLLEHandle handle = storage.OLLEHandle;
        Assert.That(handle.ApplicationPersistenceHandle, Is.SameAs(storage.DatabaseHandle));
        Assert.That(handle.HandledObject, Is.SameAs(handle.OLLEController));
        Assert.That(handle.OwnObjectHandler, Is.SameAs(handle.OwnObjectHandler), "made once");
        Assert.That(handle.OwnObjectHandler.OllSystem, Is.SameAs(handle.OLLEController.OLLESystem));
        Assert.That(handle.ForeignObjectHandler.OllSystem, Is.SameAs(handle.OLLEController.OLLESystem));
        Assert.That(handle.OwnObjectHandler.PController, Is.SameAs(storage.DatabaseHandle.PersistenceControllerDefault));
        Assert.That(handle.ForeignObjectHandler.PController, Is.SameAs(storage.DatabaseHandle.PersistenceControllerDefault));
    }

    [Test]
    [Covers(typeof(TBoldOLLEHandle), nameof(TBoldOLLEHandle.Persistent), "GetPersistent", "SetPersistent", "Loaded", "Dispose")]
    public void The_handle_sets_whether_the_OLLE_system_is_persistent()
    {
        using var storage = new OlleStorage("S");
        using var handle = new TBoldOLLEHandle { ApplicationPersistenceHandle = storage.DatabaseHandle };
        handle.BeginInit();
        handle.EndInit();
        Assert.That(handle.Persistent, Is.True);
        handle.Persistent = false;
        Assert.That((handle.Persistent, handle.OLLEController.Persistent), Is.EqualTo((false, false)));
        Assert.That(handle.OLLEController.OLLESystem.PersistenceController, Is.Null);
    }
}
