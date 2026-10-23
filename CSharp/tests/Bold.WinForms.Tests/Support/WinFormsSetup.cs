using System.Windows.Forms;

// WinForms controls live on a single-threaded apartment thread.
[assembly: Apartment(ApartmentState.STA)]

namespace Bold.Tests;

/// <summary>
/// Installs Bold's WinForms services before any test (so that a test's GUI environment replaces them, not the other way
/// round), and makes display errors fail the test rather than show a dialog.
/// </summary>
[SetUpFixture]
public class WinFormsSetup
{
    [OneTimeSetUp]
    public void InstallWinForms()
    {
        try
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        }
        catch (InvalidOperationException)
        {
            // A control was made on this thread already: the mode stays.
        }

        BoldWinForms.Initialize();
        BoldQueue.HandleException = (e, _) => global::System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(e);
    }
}

/// <summary>A form that shows a control during a test (off the visible screen, without taking a taskbar button).</summary>
internal sealed class TestForm : Form
{
    public TestForm(Control control)
    {
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        Location = new System.Drawing.Point(-2000, -2000);
        Size = new System.Drawing.Size(640, 400);
        control.Dock = DockStyle.Fill;
        Controls.Add(control);
        Show();
        Application.DoEvents();
    }
}
