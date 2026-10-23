// Delphi: Source/Persistence/System/BoldPersistenceHandleSystem.pas
// Port map: CSharp/port/units/BoldPersistenceHandleSystem.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldPersistenceHandleSystemTests
{
    [Test]
    [Covers(typeof(TBoldPersistenceHandleSystem), "*")]
    public void A_system_handle_stores_its_objects_in_another_system_handles_system()
    {
        using var storage = new HandlesSample();
        using var client = new HandlesSample(activate: false);
        var persistence = new TBoldPersistenceHandleSystem(client.Components) { SystemHandle = storage.SystemHandle };
        Assert.That(persistence.SystemHandle, Is.SameAs(storage.SystemHandle));
        client.SystemHandle.PersistenceHandle = persistence;
        client.SystemHandle.Active = true;
        Assert.That(persistence.PersistenceControllerSystem.BoldSystem, Is.SameAs(storage.System));

        client.NewPerson("Ann", "Smith");
        client.SystemHandle.UpdateDatabase();
        var stored = (TBoldObjectList)storage.System.EvaluateExpressionAsDirectElement("Person.allInstances")!;
        Assert.That(stored.Count, Is.EqualTo(1));
        Assert.That(((TPerson)stored[0]!).LastName, Is.EqualTo("Smith"), "the stored object has the generated class too");
    }
}
