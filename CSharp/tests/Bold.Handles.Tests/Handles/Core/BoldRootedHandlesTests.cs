// Delphi: Source/Handles/Core/BoldRootedHandles.pas
// Port map: CSharp/port/units/BoldRootedHandles.md
using BuildingClasses;

namespace Bold.Tests;

[NonParallelizable]
public class BoldRootedHandlesTests
{
    [Test]
    [Covers(typeof(TBoldRootedHandle), "*")]
    public void A_rooted_handle_follows_its_root_and_can_be_disabled()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann");
        TBoldReferenceHandle root = s.Reference(ann, "Person");
        var name = new TBoldExpressionHandle(s.Components) { RootHandle = root, Expression = "firstName" };
        Assert.That(name.Enabled, Is.True);
        Assert.That(name.StaticSystemTypeInfo, Is.SameAs(s.TypeInfoHandle.StaticSystemTypeInfo), "the root's type info");
        Func<int> changes = HandlesSample.CountIdentityChanges(name);
        name.Enabled = false;
        Assert.That(changes(), Is.EqualTo(1), "disabling is an identity change");
        Assert.That(name.Value, Is.Null);
        name.Enabled = true;
        Assert.That(name.Value!.AsString, Is.EqualTo("Ann"));

        name.MarkOutOfDate();
        Assert.That(name.Value!.AsString, Is.EqualTo("Ann"));

        root.Dispose();
        Assert.That(name.RootHandle, Is.Null, "a disposed root is forgotten");
        Assert.That(name.Value, Is.Null);

        var typed = new TBoldExpressionHandle(s.Components) { StaticSystemHandle = s.SystemHandle, RootTypeName = "Person", Expression = "firstName" };
        Assert.That(typed.RootTypeName, Is.EqualTo("Person"));
        Assert.That(typed.StaticRootType!.ExpressionName, Is.EqualTo("Person"), "without a root the root type is named");
        Assert.That(typed.StaticBoldType!.ExpressionName, Is.EqualTo("String"));
        Assert.That(new TBoldExpressionHandle(s.Components).StaticRootType, Is.Null);
    }

    [Test]
    public void In_a_designer_errors_at_Loaded_go_to_the_environment()
    {
        using var s = new HandlesSample();
        using var environment = new TestEnvironment();
        var designer = new DesignSite();
        var list = new DesignedListHandle { Site = designer };
        list.RootHandle = s.SystemHandle;
        list.Expression = "Person.allInstances";
        list.BeginInit();
        list.Enabled = true;
        list.FailOnEnable = true;
        list.EndInit();
        Assert.That(environment.DesigntimeExceptions, Has.Count.EqualTo(1));
        Assert.That(environment.DesigntimeExceptions[0], Is.SameAs(list));
        list.Site = null;
    }

    private sealed class DesignedListHandle : TBoldListHandle
    {
        public bool FailOnEnable;

        protected override void SetEnabled(bool Value)
        {
            if (FailOnEnable && !IsLoading)
            {
                throw new EBold("failed");
            }

            base.SetEnabled(Value);
        }
    }

    private sealed class DesignSite : System.ComponentModel.ISite
    {
        public System.ComponentModel.IComponent Component => null!;
        public System.ComponentModel.IContainer? Container => null;
        public bool DesignMode => true;
        public string? Name { get; set; } = "Designed";
        public object? GetService(Type serviceType) => null;
    }
}
