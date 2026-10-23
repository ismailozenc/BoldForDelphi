// Delphi: Source/Common/Environment/BoldEnvironmentVCL.pas
// Port map: CSharp/port/units/BoldEnvironmentVCL.md

using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldEnvironmentVCL", Symbol = "initialization", Note = "BoldWinForms.Initialize, which runs when Bold.WinForms is first used, installs the configuration.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEnvironmentVCL", Symbol = "TBoldAppEventQueue.AppEvents", Note = "Delphi made a TApplicationEvents component for OnIdle; the queue handles the static Application.Idle event.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldEnvironmentVCL", Symbol = "TBoldAppEventQueue.GetAppEvents", Note = "See AppEvents.")]

namespace Bold;

/// <summary>
/// The display queue of a WinForms application: it displays when the application is idle, a little at a time, and wakes
/// the message loop again while there is more to display.
/// </summary>
[Delphi(PortStatus.Ported, Note = "Application.Idle stands in for TApplicationEvents.OnIdle.")]
public class TBoldAppEventQueue : TBoldQueue
{
    /// <summary>How long one idle turn displays before it lets the message loop run (milliseconds).</summary>
    private const int IdleBudget = 50;

    private bool fBoldQueueActive;
    private bool fHooked;
    private SynchronizationContext? fContext;
    private int fWakePosted;

    /// <summary>A queue, active from the start.</summary>
    public TBoldAppEventQueue()
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldAppEventQueue.Destroy")]
    public override void Dispose()
    {
        Unhook();
        base.Dispose();
    }

    private void Hook()
    {
        if (!fHooked)
        {
            Application.Idle += ApplicationEventsOnIdle;
            fHooked = true;
        }

        CaptureContext();
    }

    private void Unhook()
    {
        if (fHooked)
        {
            Application.Idle -= ApplicationEventsOnIdle;
            fHooked = false;
        }
    }

    /// <summary>Remembers the UI thread's synchronization context, to wake its message loop.</summary>
    private void CaptureContext()
    {
        if (fContext is null && SynchronizationContext.Current is WindowsFormsSynchronizationContext context)
        {
            fContext = context;
        }
    }

    /// <summary>Posts an empty message to the UI thread, so the message loop goes idle (and displays) again.</summary>
    internal void Wake()
    {
        if (fContext is not null && Interlocked.Exchange(ref fWakePosted, 1) == 0)
        {
            fContext.Post(_ => Interlocked.Exchange(ref fWakePosted, 0), null);
        }
    }

    /// <summary>
    /// Runs the pre-display queue, then displays (one at a time, or all, as the display mode says) for a little while;
    /// when more is left it wakes the message loop for another turn.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's OnIdle had a Done parameter that kept the idle events coming; here the queue posts a message to get another one.")]
    private void ApplicationEventsOnIdle(object? Sender, EventArgs e)
    {
        try
        {
            PerformPreDisplayQueue();
            if (fBoldQueueActive)
            {
                var watch = Stopwatch.StartNew();
                bool more;
                switch (DisplayMode)
                {
                    case TBoldQueueDisplayMode.dmDisplayOne:
                        do
                        {
                            more = DisplayOne();
                        }
                        while (more && watch.ElapsedMilliseconds < IdleBudget);
                        break;
                    case TBoldQueueDisplayMode.dmDisplayAll:
                        DisplayAll();
                        more = false;
                        break;
                    default:
                        throw new EBoldInternal(SysUtils.Format(BoldCommonConst.sUnknownDisplayMode, GetType().Name));
                }

                if (more)
                {
                    Wake();
                }
            }
        }
        catch (Exception E)
        {
            Application.OnThreadException(E);
        }
    }

    /// <summary>Starts displaying when the application is idle.</summary>
    [Delphi(PortStatus.Ported)]
    public override void ActivateDisplayQueue()
    {
        fBoldQueueActive = true;
        Hook();
    }

    /// <summary>Stops displaying.</summary>
    [Delphi(PortStatus.Ported)]
    public override void DeActivateDisplayQueue()
    {
        fBoldQueueActive = false;
        Unhook();
    }

    /// <summary>Something was queued: make sure an idle turn comes.</summary>
    protected override void EnsureDequeing()
    {
        CaptureContext();
        Wake();
    }
}

/// <summary>
/// The environment of a WinForms application: the display queue runs when the application is idle, and questions,
/// errors, focus and the designer go through WinForms.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldVCLEnvironmentConfiguration : TBoldEnvironmentConfiguration
{
    private static readonly Container fApplicationComponents = new();
    private static bool fApplicationExitHooked;

    [Delphi(PortStatus.Ported)]
    public override bool AskUser(string Text) =>
        MessageBox.Show(Text, Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    [Delphi(PortStatus.Ported)]
    public override void BringToFront()
    {
        if (MainForm() is { } form)
        {
            form.BringToFront();
            form.Activate();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void FocusMainForm() => MainForm()?.Focus();

    /// <summary>The application's first open form, Delphi's Application.MainForm.</summary>
    private static Form? MainForm() => Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null;

    [Delphi(PortStatus.Ported, Note = "WinForms where Delphi says VCL.")]
    protected override string GetName() => "WinForms"; // do not localize

    [Delphi(PortStatus.Ported)]
    protected override TBoldQueueClass GetQueueClass() => typeof(TBoldAppEventQueue);

    /// <summary>A container the application disposes when it exits, Delphi's Application component.</summary>
    [Delphi(PortStatus.Ported, Note = "A container disposed on Application.ApplicationExit stands in for the Application component.")]
    protected override object? GetRootComponent()
    {
        if (!fApplicationExitHooked)
        {
            Application.ApplicationExit += (_, _) => fApplicationComponents.Dispose();
            fApplicationExitHooked = true;
        }

        return fApplicationComponents;
    }

    /// <summary>Shows that the component failed at design time.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi showed the exception being handled; .NET has no current exception, so this shows which component failed.")]
    public override void HandleDesigntimeException(object? Sender) =>
        MessageBox.Show(SysUtils.Format("%s failed; see the output for the error.", (Sender as IComponent)?.Site?.Name ?? Sender?.GetType().Name ?? ""), // do not localize
            Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);

    /// <summary>Forms, user controls and component containers count as forms (Delphi's forms and data modules).</summary>
    [Delphi(PortStatus.Ported, Note = "A Form, a UserControl or a component container, for Delphi's forms and data modules.")]
    public override bool IsFormOrDataModule(object? Sender) => Sender is Form or UserControl or IContainer;

    [Delphi(PortStatus.Ported)]
    public override void ProcessMessages() => Application.DoEvents();

    /// <summary>Wakes the message loop so the queue displays.</summary>
    [Delphi(PortStatus.Ported, Note = "Posts an empty message to the UI thread where Delphi posted WM_PAINT.")]
    public override void TriggerQueueMechanism() => (Queue as TBoldAppEventQueue)?.Wake();

    /// <summary>Tells the designer that the component changed (in a designer only).</summary>
    [Delphi(PortStatus.Ported, Note = "Through the designer's IComponentChangeService, where Delphi told the form's designer.")]
    public override void UpdateDesigner(object? Sender)
    {
        if (!RunningInIDE)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sNotRunningInIDE, "UpdateDesigner")); // do not localize
        }

        if (Sender is IComponent component && component.Site?.GetService(typeof(IComponentChangeService)) is IComponentChangeService changes)
        {
            changes.OnComponentChanged(component, null, null, null);
        }
    }
}
