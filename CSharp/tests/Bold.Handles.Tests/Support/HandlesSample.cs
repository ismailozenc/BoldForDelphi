using System.ComponentModel;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>
/// The Building example's model in a model component, a type info handle and a running transient system handle, all
/// in one container, as a form would hold them.
/// </summary>
internal sealed class HandlesSample : IDisposable
{
    public readonly Container Components = new();
    public readonly TBoldAbstractModel Model;
    public readonly TBoldSystemTypeInfoHandle TypeInfoHandle;
    public readonly TBoldSystemHandle SystemHandle;

    public HandlesSample(bool activate = true)
    {
        Model = new TBoldAbstractModel(Components) { Name = "Model" };
        Model.ReadModel(BuildingModelLines());
        TypeInfoHandle = new TBoldSystemTypeInfoHandle(Components) { Name = "TypeInfo", BoldModel = Model };
        SystemHandle = new TBoldSystemHandle(Components) { Name = "SystemHandle", SystemTypeInfoHandle = TypeInfoHandle };
        if (activate)
        {
            SystemHandle.Active = true;
        }
    }

    /// <summary>The lines of the Building model (Building.bld, embedded in the example's classes).</summary>
    public static TStringList BuildingModelLines()
    {
        using Stream stream = typeof(BuildingModel).Assembly.GetManifestResourceStream("BuildingClasses.Building.bld")!;
        using var reader = new StreamReader(stream);
        return new TStringList { Text = reader.ReadToEnd() };
    }

    public TBoldSystem System => SystemHandle.System!;

    /// <summary>Reads the model again and says it changed, as the UML model component does when its model is set.</summary>
    public void ChangeModel()
    {
        Model.ReadModel(BuildingModelLines());
        Model.SendEvent(Model, BoldAbstractModel.beModelChanged);
    }

    public TPerson NewPerson(string firstName, string lastName = "")
    {
        var person = new TPerson(System) { FirstName = firstName, LastName = lastName };
        return person;
    }

    public TResidential_Building NewHouse(string address) => new(System) { Address = address };

    /// <summary>A reference handle on the value, in the container.</summary>
    public TBoldReferenceHandle Reference(TBoldElement? value, string typeName = "")
    {
        var handle = new TBoldReferenceHandle(Components) { StaticSystemHandle = SystemHandle, StaticValueTypeName = typeName };
        handle.Value = value;
        return handle;
    }

    /// <summary>Counts the beValueIdentityChanged events the component sends.</summary>
    public static Func<int> CountIdentityChanges(TBoldSubscribableComponent component)
    {
        int count = 0;
        var subscriber = new TBoldPassthroughSubscriber((_, _, _) => count++);
        component.AddSmallSubscription(subscriber, TBoldSmallEventSet.Of(BoldSubscription.beValueIdentityChanged), BoldSubscription.beValueIdentityChanged);
        return () => count;
    }

    public void Dispose()
    {
        if (SystemHandle.Active)
        {
            System.Discard();
            SystemHandle.Active = false;
        }

        Components.Dispose();
    }
}
