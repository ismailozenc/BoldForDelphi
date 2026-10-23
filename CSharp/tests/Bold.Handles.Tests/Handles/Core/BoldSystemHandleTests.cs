// Delphi: Source/Handles/Core/BoldSystemHandle.pas
// Port map: CSharp/port/units/BoldSystemHandle.md
namespace Bold.Tests;

public class BoldSystemHandleTests
{
    [Test]
    [Covers(typeof(TBoldSystemHandle), "*")]
    public void Activating_runs_a_system_and_deactivating_destroys_it()
    {
        using var s = new HandlesSample(activate: false);
        Assert.That(s.SystemHandle.StaticSystemTypeInfo, Is.Not.Null, "the type info is made first; making it is a type change");
        Func<int> changes = HandlesSample.CountIdentityChanges(s.SystemHandle);
        Assert.That(s.SystemHandle.Active, Is.False);
        Assert.That(s.SystemHandle.Value, Is.Null);
        Assert.That(() => s.SystemHandle.UpdateDatabase(), Throws.InstanceOf<EBold>(), "no system to update");

        TNotifyEvent preUpdate = _ => { };
        TBoldOptimisticLockingFailedEvent lockingFailed = (_, _, _) => { };
        s.SystemHandle.OnPreUpdate = preUpdate;
        s.SystemHandle.OnOptimisticLockingFailed = lockingFailed;
        s.SystemHandle.Active = true;
        Assert.That(changes(), Is.EqualTo(1));
        Assert.That(s.SystemHandle.System, Is.Not.Null);
        Assert.That(s.SystemHandle.Value, Is.SameAs(s.SystemHandle.System));
        Assert.That(s.SystemHandle.StaticBoldType, Is.SameAs(s.TypeInfoHandle.StaticSystemTypeInfo));
        Assert.That(s.SystemHandle.Persistent, Is.False);
        Assert.That(s.SystemHandle.RegionFactory, Is.Not.Null, "the model's regions get a factory");
        Assert.That(s.System.OnPreUpdate, Is.SameAs(preUpdate));
        Assert.That(s.System.OnOptimisticLockingFailed, Is.SameAs(lockingFailed));

        s.NewPerson("Ann");
        s.SystemHandle.Active = false;
        Assert.That(s.SystemHandle.System, Is.Null);
        Assert.That(s.SystemHandle.RegionFactory, Is.Null);
        Assert.That(changes(), Is.EqualTo(2));
    }

    [Test]
    public void Activation_needs_a_runnable_type_info()
    {
        using var s = new HandlesSample(activate: false);
        s.SystemHandle.SystemTypeInfoHandle = null;
        Assert.That(() => s.SystemHandle.Active = true, Throws.InstanceOf<EBold>().With.Message.Contains("SystemTypeInfoHandle"));
        var empty = new TBoldSystemTypeInfoHandle(s.Components);
        s.SystemHandle.SystemTypeInfoHandle = empty;
        Assert.That(() => s.SystemHandle.Active = true, Throws.InstanceOf<EBold>().With.Message.EqualTo(HandlesConst.sUnableToFindTypeInfoHandle));
    }

    [Test]
    public void While_loading_Active_and_AutoActivate_wait_for_Loaded()
    {
        using var s = new HandlesSample(activate: false);
        s.SystemHandle.BeginInit();
        s.SystemHandle.Active = true;
        Assert.That(s.SystemHandle.Active, Is.False, "deferred while loading");
        s.SystemHandle.EndInit();
        Assert.That(s.SystemHandle.Active, Is.True, "activated at Loaded");

        var second = new TBoldSystemHandle(s.Components) { SystemTypeInfoHandle = s.TypeInfoHandle };
        second.BeginInit();
        second.AutoActivate = true;
        Assert.That(second.Active, Is.False);
        second.EndInit();
        Assert.That(second.Active, Is.True, "AutoActivate activates at Loaded");
        Assert.That(second.AutoActivate, Is.True);
        second.Active = false;
    }

    [Test]
    public void A_model_change_shuts_a_running_system_down()
    {
        using var s = new HandlesSample();
        Func<int> changes = HandlesSample.CountIdentityChanges(s.SystemHandle);
        s.ChangeModel();
        Assert.That(s.SystemHandle.Active, Is.False, "as in Delphi, the panic shut down leaves the system inactive");
        Assert.That(changes(), Is.GreaterThan(0));
        s.SystemHandle.Active = true;
        Assert.That(s.System.BoldSystemTypeInfo, Is.SameAs(s.TypeInfoHandle.StaticSystemTypeInfo), "a new system on the new type info");
    }

    [Test]
    public void A_model_change_with_unsaved_objects_shuts_the_system_down_and_says_how_many_went()
    {
        using var storage = new HandlesSample();
        using var s = new HandlesSample(activate: false);
        s.SystemHandle.PersistenceHandle = new TBoldPersistenceHandleSystem(s.Components) { SystemHandle = storage.SystemHandle };
        s.SystemHandle.Active = true;
        s.NewPerson("Ann");
        s.NewPerson("Bob");
        Assert.That(() => s.ChangeModel(),
            Throws.InstanceOf<EBold>().With.Message.Contains("2 objects discarded"));
        Assert.That(s.SystemHandle.Active, Is.False);
    }

    [Test]
    public void The_persistence_handle_is_activated_with_the_system_and_forgotten_when_it_goes()
    {
        using var s = new HandlesSample(activate: false);
        using var storage = new HandlesSample();
        var persistence = new TBoldPersistenceHandleSystem(s.Components) { SystemHandle = storage.SystemHandle };
        s.SystemHandle.PersistenceHandle = persistence;
        Assert.That(s.SystemHandle.RefersToComponent(persistence), Is.True);
        s.SystemHandle.Active = true;
        Assert.That(persistence.Active, Is.True);
        Assert.That(s.SystemHandle.Persistent, Is.True);
        s.NewPerson("Ann");
        Assert.That(s.System.BoldDirty, Is.True, "a persistent system's new objects are unsaved");
        s.SystemHandle.UpdateDatabase();
        Assert.That(storage.System.EvaluateExpressionAsNewElement("Person.allInstances->size")!.AsString, Is.EqualTo("1"), "saved in the storage system");
        s.SystemHandle.Active = false;
        Assert.That(persistence.Active, Is.False);

        persistence.Dispose();
        Assert.That(s.SystemHandle.PersistenceHandle, Is.Null);
    }

    [Test]
    public void The_OCL_definition_lookup_reaches_the_running_system()
    {
        using var s = new HandlesSample();
        s.SystemHandle.InstallOclDefinitionLookUp(name => name == "people" ? "Person.allInstances->size" : "");
        s.NewPerson("Ann");
        Assert.That(s.System.EvaluateExpressionAsNewElement("%people")!.AsString, Is.EqualTo("1"), "a definition is a whole expression");
    }
}
