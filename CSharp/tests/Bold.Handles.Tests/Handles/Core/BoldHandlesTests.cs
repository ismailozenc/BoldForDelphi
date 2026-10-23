// Delphi: Source/Handles/Core/BoldHandles.pas
// Port map: CSharp/port/units/BoldHandles.md
namespace Bold.Tests;

[NonParallelizable]
public class BoldHandlesTests
{
    [Test]
    [Covers(typeof(TBoldSystemTypeInfoHandle), "*")]
    [Covers(typeof(TBoldElementHandle), "*")]
    public void The_type_info_handle_builds_the_type_info_from_its_model_and_drops_it_when_the_model_changes()
    {
        using var s = new HandlesSample(activate: false);
        TBoldSystemTypeInfo? typeInfo = s.TypeInfoHandle.StaticSystemTypeInfo;
        Assert.That(typeInfo, Is.Not.Null);
        Assert.That(typeInfo!.SystemIsRunnable, Is.True, typeInfo.InitializationLog.Text);
        Assert.That(typeInfo.UseGeneratedCode, Is.True);
        Assert.That(s.TypeInfoHandle.Value, Is.SameAs(typeInfo));
        Assert.That(s.TypeInfoHandle.StaticBoldType, Is.SameAs(typeInfo));
        Assert.That(s.TypeInfoHandle.BoldType, Is.SameAs(typeInfo.BoldType), "the dynamic type is the value's type");
        Assert.That(s.TypeInfoHandle.RefersToComponent(s.Model), Is.True);
        Assert.That(s.TypeInfoHandle.RefersToComponent(s.SystemHandle), Is.False);
        Assert.That(s.TypeInfoHandle.RegionDefinitions, Is.Not.Null);

        Func<int> changes = HandlesSample.CountIdentityChanges(s.TypeInfoHandle);
        s.ChangeModel();
        Assert.That(changes(), Is.GreaterThan(0), "a model change is an identity change");
        Assert.That(s.TypeInfoHandle.StaticSystemTypeInfo, Is.Not.SameAs(typeInfo), "a new type info for the new model");

        s.TypeInfoHandle.UseGeneratedCode = false;
        s.TypeInfoHandle.CheckCodeCheckSum = false;
        s.TypeInfoHandle.BoldModel = null;
        Assert.That(s.TypeInfoHandle.StaticSystemTypeInfo, Is.Null);
        s.TypeInfoHandle.BoldModel = s.Model;
        Assert.That(s.TypeInfoHandle.StaticSystemTypeInfo!.UseGeneratedCode, Is.False);
        Assert.That(s.TypeInfoHandle.StrictType, Is.False);
        s.TypeInfoHandle.StrictType = true;
        Assert.That(s.TypeInfoHandle.StrictType, Is.True);
    }

    [Test]
    public void The_type_info_handle_installs_the_OCL_definition_lookup()
    {
        using var s = new HandlesSample(activate: false);
        s.TypeInfoHandle.InstallOclDefinitionLookUp(name => name == "rich" ? "Person.allInstances->select(assets > 1000)" : "");
        TBoldSystemTypeInfo typeInfo = s.TypeInfoHandle.StaticSystemTypeInfo!;
        Assert.That(typeInfo.Evaluator!.ExpressionType("%rich", typeInfo, true)!.ExpressionName, Is.EqualTo("Collection(Person)"));
    }

    [Test]
    public void Disposing_the_model_leaves_the_type_info_handle_without_a_model()
    {
        using var s = new HandlesSample(activate: false);
        s.Model.Dispose();
        Assert.That(s.TypeInfoHandle.BoldModel, Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldAbstractSystemHandle), "*")]
    public void One_system_handle_is_the_default_and_non_system_handles_fall_back_on_it()
    {
        using var s = new HandlesSample();
        var expression = new TBoldExpressionHandle(s.Components);
        Assert.That(TBoldAbstractSystemHandle.DefaultBoldSystemHandle(), Is.Null);
        Assert.That(expression.StaticSystemTypeInfo, Is.Null, "no static system handle and no default");

        s.SystemHandle.IsDefault = true;
        Assert.That(TBoldAbstractSystemHandle.DefaultBoldSystemHandle(), Is.SameAs(s.SystemHandle));
        Assert.That(TBoldAbstractSystemHandle.DefaultBoldSystemTypeInfo(), Is.SameAs(s.TypeInfoHandle.StaticSystemTypeInfo));
        Assert.That(s.System.IsDefault, Is.True, "the running system follows");
        Assert.That(expression.StaticSystemTypeInfo, Is.SameAs(s.TypeInfoHandle.StaticSystemTypeInfo), "a non-system handle falls back on the default");

        s.SystemHandle.IsDefault = false;
        Assert.That(TBoldAbstractSystemHandle.DefaultBoldSystemHandle(), Is.Null, "switching IsDefault off clears the default");
        Assert.That(s.System.IsDefault, Is.False);

        s.SystemHandle.IsDefault = true;
        Assert.That(s.SystemHandle.RefersToComponent(s.TypeInfoHandle), Is.True);
        s.System.Discard();
        s.SystemHandle.Active = false;
        s.SystemHandle.Dispose();
        Assert.That(TBoldAbstractSystemHandle.DefaultBoldSystemHandle(), Is.Null, "a disposed default is no longer the default");
    }

    [Test]
    [Covers(typeof(TBoldNonSystemHandle), "*")]
    public void A_non_system_handle_follows_its_static_system_handle()
    {
        using var s = new HandlesSample(activate: false);
        var reference = new TBoldReferenceHandle(s.Components) { StaticValueTypeName = "Person" };
        Func<int> changes = HandlesSample.CountIdentityChanges(reference);
        reference.StaticSystemHandle = s.SystemHandle;
        Assert.That(changes(), Is.EqualTo(1));
        Assert.That(reference.StaticBoldType!.ExpressionName, Is.EqualTo("Person"));

        s.SystemHandle.SystemTypeInfoHandle = null;
        Assert.That(changes(), Is.GreaterThan(1), "the system handle's type change reaches the handle");
        Assert.That(reference.StaticSystemTypeInfo, Is.Null);

        s.SystemHandle.Dispose();
        Assert.That(reference.StaticSystemHandle, Is.Null, "a disposed system handle is forgotten");
    }

    [Test]
    public void The_type_info_handle_cannot_change_under_an_active_system()
    {
        using var s = new HandlesSample();
        var other = new TBoldSystemTypeInfoHandle(s.Components) { BoldModel = s.Model };
        Assert.That(() => s.SystemHandle.SystemTypeInfoHandle = other, Throws.InstanceOf<EBold>());
    }

    [Test]
    public void Disposing_the_type_info_handle_stops_the_system()
    {
        using var s = new HandlesSample();
        s.TypeInfoHandle.Dispose();
        Assert.That(s.SystemHandle.Active, Is.False);
        Assert.That(s.SystemHandle.SystemTypeInfoHandle, Is.Null);
    }
}
