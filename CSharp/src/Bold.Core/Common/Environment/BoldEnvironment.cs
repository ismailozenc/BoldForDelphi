// Delphi: Source/Common/Environment/BoldEnvironment.pas
// Port map: CSharp/port/units/BoldEnvironment.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldEnvironment", Symbol = "TBoldQueueClass", Note = "Global using alias of System.Type, in BoldEnvironment.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldEnvironment", Symbol = "initialization", Note = "The freestanding configuration is a static field initializer.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEnvironment", Symbol = "finalization", Note = "Freed the configurations at unit finalization; call BoldEnvironment.FinalizeEnvironments to do it explicitly.")]

namespace Bold;

/// <summary>A component that tells the environment when the application goes away.</summary>
[Delphi(PortStatus.Ported, Note = "A System.ComponentModel.Component; its owner, when it is a container, holds it.")]
public class TBoldApplicationSubscriber : System.ComponentModel.Component
{
    private TBoldJustNotifyEvent? fOnApplicationDestroyed;

    /// <summary>Calls OnApplicationDestroyed before the component goes away.</summary>
    [Delphi(PortStatus.Ported, Note = "Runs from Dispose.")]
    public void BeforeDestruction() => fOnApplicationDestroyed?.Invoke();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BeforeDestruction();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldJustNotifyEvent? OnApplicationDestroyed
    {
        get => fOnApplicationDestroyed;
        set => fOnApplicationDestroyed = value;
    }
}

/// <summary>
/// What Bold needs from the host: a queue for deferred display and apply, and GUI services. The base raises EBoldInternal
/// for services an environment does not support.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldEnvironmentConfiguration : IDisposable
{
    private const string UnsupportedFunction = "%s: function unsupported in environment configuration %s";

    private TBoldQueue? fQueue;
    private TBoldApplicationSubscriber? fApplicationSubscriber;
    private bool fQueueFinalized;

    private EBoldInternal Unsupported(string function) => new(SysUtils.Format(UnsupportedFunction, function, Name));

    /// <summary>Frees the queue; afterwards the environment has none.</summary>
    [Delphi(PortStatus.Ported)]
    private void FinalizeQueue()
    {
        if (fQueue is not null)
        {
            fQueue.Dispose();
            fQueue = null;
            if (fApplicationSubscriber is not null)
            {
                fApplicationSubscriber.OnApplicationDestroyed = null;
                fApplicationSubscriber.Dispose();
                fApplicationSubscriber = null;
            }
        }

        fQueueFinalized = true;
    }

    /// <summary>Called when the application goes away: stops the display queue.</summary>
    [Delphi(PortStatus.Ported)]
    private void DeactivateQueue()
    {
        fApplicationSubscriber = null;
        if (fQueue is not null && !QueueFinalized)
        {
            fQueue.DeActivateDisplayQueue();
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetRunningInIDE() => BoldEnvironment.BoldInternalRunningInIDE;

    /// <summary>The queue, created on first use with the environment's queue class.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldQueue? GetQueue()
    {
        if (fQueue is null && !QueueFinalized)
        {
            fQueue = (TBoldQueue)Activator.CreateInstance(GetQueueClass())!;
            fApplicationSubscriber = new TBoldApplicationSubscriber();
            (RootComponent as System.ComponentModel.IContainer)?.Add(fApplicationSubscriber);
            fApplicationSubscriber.OnApplicationDestroyed = DeactivateQueue;
        }

        return fQueue;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldQueueClass GetQueueClass() => throw Unsupported("GetQueueClass");

    [Delphi(PortStatus.Ported)]
    protected void SetQueue(TBoldQueue? NewValue) => fQueue = NewValue;

    [Delphi(PortStatus.Ported)]
    protected abstract string GetName();

    [Delphi(PortStatus.Ported, Note = "Returns object; a container root holds the application subscriber.")]
    protected virtual object? GetRootComponent() => throw Unsupported("GetRootComponentVCL");

    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        if (!QueueFinalized)
        {
            FinalizeQueue();
        }

        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)] public virtual void HandleDesigntimeException(object? Sender) => throw Unsupported("HandleDesigntimeException");
    [Delphi(PortStatus.Ported)] public virtual void UpdateDesigner(object? Sender) => throw Unsupported("UpdateDesigner");
    [Delphi(PortStatus.Ported)] public virtual bool IsFormOrDataModule(object? Sender) => throw Unsupported("IsFormOrDataModule");
    [Delphi(PortStatus.Ported)] public virtual bool AskUser(string Text) => throw Unsupported("AskUser");
    [Delphi(PortStatus.Ported)] public virtual void ProcessMessages() => throw Unsupported("ProcessMessages");
    [Delphi(PortStatus.Ported)] public virtual void BringToFront() => throw Unsupported("BringToFront");
    [Delphi(PortStatus.Ported)] public virtual void FocusMainForm() => throw Unsupported("FocusMainForm");
    [Delphi(PortStatus.Ported)] public virtual void TriggerQueueMechanism() => throw Unsupported("TriggerQueueMechanism");

    [Delphi(PortStatus.Ported)] public object? RootComponent => GetRootComponent();
    [Delphi(PortStatus.Ported)] public string Name => GetName();
    [Delphi(PortStatus.Ported)] public bool RunningInIDE => GetRunningInIDE();
    [Delphi(PortStatus.Ported)] public bool QueueFinalized => fQueueFinalized;
    [Delphi(PortStatus.Ported)] public TBoldQueue? Queue => GetQueue();
}

/// <summary>The environment without a GUI: bringing to front and focusing are ignored, everything else is unsupported.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFreestandingEnvironmentConfiguration : TBoldEnvironmentConfiguration
{
    [Delphi(PortStatus.Ported)]
    protected override string GetName() => "Freestanding";

    [Delphi(PortStatus.Ported)]
    public override void BringToFront()
    {
        // Ignored silently.
    }

    [Delphi(PortStatus.Ported)]
    public override void FocusMainForm()
    {
        // Ignored silently.
    }
}

/// <summary>The installed environments; the custom one wins over the GUI one, which wins over the freestanding one.</summary>
public static partial class BoldEnvironment
{
    [Delphi(PortStatus.Ported)] public static bool BoldInternalRunningInIDE = false;

    [Delphi(PortStatus.Ported, Note = "The WinForms environment in the port.")]
    public static TBoldEnvironmentConfiguration? BoldInternalVCLConfiguration;

    [Delphi(PortStatus.Ported)] public static TBoldEnvironmentConfiguration? BoldInternalCustomConfiguration;

    [Delphi(PortStatus.Ported)]
    public static TBoldEnvironmentConfiguration? BoldInternalFreestandingConfiguration = new TBoldFreestandingEnvironmentConfiguration();

    private static bool G_EnvironmentsFinalized;

    [Delphi(PortStatus.Ported)]
    public static TBoldEnvironmentConfiguration BoldEffectiveEnvironment() =>
        BoldInternalCustomConfiguration ?? BoldInternalVCLConfiguration ?? BoldInternalFreestandingConfiguration
        ?? throw new EBoldInternal("BoldEffectiveEnvironment, no environment available. Unit BoldEnvironment either not initialized, or already finalized");

    [Delphi(PortStatus.Ported)]
    public static bool BoldEnvironmentsFinalized() => G_EnvironmentsFinalized;

    /// <summary>Frees every configuration, as the Delphi unit's finalization does; afterwards there is no queue.</summary>
    public static void FinalizeEnvironments()
    {
        SysUtils.FreeAndNil(ref BoldInternalVCLConfiguration);
        SysUtils.FreeAndNil(ref BoldInternalCustomConfiguration);
        SysUtils.FreeAndNil(ref BoldInternalFreestandingConfiguration);
        G_EnvironmentsFinalized = true;
    }
}
