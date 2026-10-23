using System.ComponentModel;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>A display queue that displays only when told to (TBoldQueueable.DisplayAll), as an idle GUI would.</summary>
internal sealed class TestQueue : TBoldQueue
{
    public bool Active;
    public override void ActivateDisplayQueue() => Active = true;
    public override void DeActivateDisplayQueue() => Active = false;
}

/// <summary>
/// An environment installed as the custom one while it lives, with the test queue; an IContainer counts as a form.
/// Dialogs are answered by <see cref="DialogAnswer"/>.
/// </summary>
internal sealed class GuiTestEnvironment : TBoldFreestandingEnvironmentConfiguration
{
    public static GuiTestEnvironment? Current;

    public int DialogAnswer = Controls.mrCancel;
    public readonly List<string> Dialogs = [];

    /// <summary>The exceptions displaying raised (the GUI would show them), in order.</summary>
    public readonly List<Exception> DisplayErrors = [];

    private readonly Action<Exception, object?> fOldHandler = BoldQueue.HandleException;

    public GuiTestEnvironment()
    {
        Current = this;
        BoldQueue.HandleException = (e, _) => DisplayErrors.Add(e);
        BoldEnvironment.BoldInternalCustomConfiguration = this;
        Bold.Dialogs.MessageDlgHandler = (msg, _, _, _) =>
        {
            this.Dialogs.Add(msg);
            return DialogAnswer;
        };
    }

    protected override Type GetQueueClass() => typeof(TestQueue);

    protected override object? GetRootComponent() => null;

    public override bool IsFormOrDataModule(object? Sender) => Sender is IContainer;

    public override void Dispose()
    {
        BoldQueue.HandleException = fOldHandler;
        Current = null;
        Bold.Dialogs.MessageDlgHandler = null;
        BoldEnvironment.BoldInternalCustomConfiguration = null;
        base.Dispose();
    }
}

/// <summary>
/// The Building example's model in a model component, a type info handle and a running transient system handle, all
/// in one container as a form would hold them, with the GUI test environment installed.
/// </summary>
internal sealed class GuiSample : IDisposable
{
    public readonly GuiTestEnvironment Environment = new();
    public readonly Container Components = new();
    public readonly TBoldAbstractModel Model;
    public readonly TBoldSystemTypeInfoHandle TypeInfoHandle;
    public readonly TBoldSystemHandle SystemHandle;

    public GuiSample(bool activate = true)
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

    public TPerson NewPerson(string firstName, string lastName = "") => new(System) { FirstName = firstName, LastName = lastName };

    public TResidential_Building NewHouse(string address) => new(System) { Address = address };

    /// <summary>A reference handle on the value, in the container.</summary>
    public TBoldReferenceHandle Reference(TBoldElement? value, string typeName = "")
    {
        var handle = new TBoldReferenceHandle(Components) { StaticSystemHandle = SystemHandle, StaticValueTypeName = typeName };
        handle.Value = value;
        return handle;
    }

    /// <summary>A list handle on the expression, rooted in the system handle.</summary>
    public TBoldListHandle List(string expression) =>
        new(Components) { RootHandle = SystemHandle, Expression = expression };

    /// <summary>Displays everything queued, as the GUI does when idle; the first exception displaying raised is raised here.</summary>
    public static void Display()
    {
        TBoldQueueable.DisplayAll();
        if (GuiTestEnvironment.Current is { DisplayErrors.Count: > 0 } environment)
        {
            Exception first = environment.DisplayErrors[0];
            environment.DisplayErrors.Clear();
            global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(first);
        }
    }

    /// <summary>Applies every pending edit.</summary>
    public static void ApplyAll() => TBoldQueueable.ApplyAll();

    public void Dispose()
    {
        if (SystemHandle.Active)
        {
            System.Discard();
            SystemHandle.Active = false;
        }

        Components.Dispose();
        Environment.Dispose();
    }
}
