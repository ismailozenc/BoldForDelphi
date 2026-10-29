// Delphi: Source/Common/Logging/BoldLogForm.pas, BoldLogHandlerForm.pas, BoldSmallLogFrame.pas
// Port map: CSharp/port/units/BoldLogForm.md
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>The log window, its log receiver and the small log frame, on the global log.</summary>
[NonParallelizable]
public class BoldLogFormsTests
{
    private static readonly MethodInfo OnClickMethod = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private sealed class TestReceiver : TBoldLogHandlerReceiver
    {
        public TBoldLogForm Form => LogForm;
    }

    private static void OffScreen(Form form)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-2000, -2000);
        form.ShowInTaskbar = false;
    }

    private static TBoldLogHandler BoldLog => BoldLogHandler.BoldLog();

    [Test]
    [Covers(typeof(TBoldLogForm), nameof(TBoldLogForm.AddLog), nameof(TBoldLogForm.Clear), nameof(TBoldLogForm.UpdateView), nameof(TBoldLogForm.LogLines),
        "mnuShowAllClick", "ShowAllLines", "mnuClearLogClick", "mnuAddSeparatorClick", "mnuCopyClick", "cmdCloseClick", "FormCreate", "FormDestroy")]
    public void The_log_form_shows_the_last_lines_or_all_and_its_menu_clears_and_copies()
    {
        var form = new TBoldLogForm();
        OffScreen(form);
        for (int i = 1; i <= 60; i++)
        {
            form.AddLog("line " + i);
        }

        Assert.That(form.LogLines.Count, Is.EqualTo(60));
        Assert.That(form.Log.Lines, Is.Empty, "a hidden form shows nothing");
        form.Show();
        form.Clear();
        Assert.That(form.Log.Lines.Length, Is.EqualTo(50), "the last 50 lines");
        Assert.That(form.Log.Lines[0], Is.EqualTo("line 11"));

        form.mnuShowAll.PerformClick();
        Assert.That(form.mnuShowAll.Checked, Is.True);
        Assert.That(form.Log.Lines.Length, Is.EqualTo(60));
        form.AddLog("line 61");
        Assert.That(form.Log.Lines[^1], Is.EqualTo("line 61"));

        form.Log.Select(0, 4);
        form.ActiveControl = form.Log;
        form.mnuCopy.PerformClick();
        Assert.That(Clipboard.GetText(), Is.EqualTo("line"));

        form.mnuAddSeparator.PerformClick();
        form.mnuClearLog.PerformClick();
        Assert.That(form.LogLines.Count, Is.EqualTo(0));

        form.mnuCloseLogForm.PerformClick();
        Assert.That(form.IsDisposed, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldLogHandlerReceiver), nameof(TBoldLogHandlerReceiver.StartLog), nameof(TBoldLogHandlerReceiver.EndLog), nameof(TBoldLogHandlerReceiver.Log),
        nameof(TBoldLogHandlerReceiver.Show), nameof(TBoldLogHandlerReceiver.Hide), nameof(TBoldLogHandlerReceiver.Clear), nameof(TBoldLogHandlerReceiver.ProgressStep),
        nameof(TBoldLogHandlerReceiver.Sync), nameof(TBoldLogHandlerReceiver.Dispose), "HideProgressBar", "GetLogForm", "LogForm", "Notification")]
    [Covers("BoldLogHandlerForm", "TBoldLogHandlerReceiver.SetProgress", "TBoldLogHandlerReceiver.SetLogHeader", "TBoldLogHandlerReceiver.SetProgressMax",
        "TBoldLogHandlerReceiver.ProcessInterruption")]
    public void The_log_receiver_shows_the_global_log_in_a_log_form()
    {
        var receiver = new TestReceiver();
        BoldLog.RegisterLogReceiver(receiver);
        try
        {
            OffScreen(receiver.Form);
            BoldLog.StartLog("Session");
            Assert.That(receiver.Form.Text, Is.EqualTo(SysUtils.Format(BoldCommonConst.sLogFormCaption, "Session")));
            BoldLog.LogHeader = "Header";
            BoldLog.Log("a line");
            BoldLog.ProgressMax = 10;
            BoldLog.Progress = 3;
            BoldLog.ProgressStep();
            Assert.That(receiver.Form.ProgressBar1.Maximum, Is.EqualTo(10));
            Assert.That(receiver.Form.ProgressBar1.Value, Is.EqualTo(4));
            BoldLog.Progress = 99;
            Assert.That(receiver.Form.ProgressBar1.Value, Is.EqualTo(10), "kept within the bar");
            Assert.That(receiver.Form.LogLines.Text, Does.Contain("===Header").And.Contain("a line").And.Contain("Starting Session"));

            BoldLog.Show();
            Assert.That(receiver.Form.Visible, Is.True);
            Assert.That(receiver.Form.Log.Lines, Has.Some.Contains("a line"));
            BoldLog.Sync();
            Assert.That(BoldLog.ProcessInterruption(), Is.False);
            BoldLog.Clear();
            BoldLog.EndLog();
            Assert.That(receiver.Form.ProgressBar1.Value, Is.EqualTo(0));
            Assert.That(receiver.Form.LogLines.Text, Does.Contain("Done Session"));
            BoldLog.Hide();
            Assert.That(receiver.Form.Visible, Is.False);

            TBoldLogForm first = receiver.Form;
            first.Dispose();
            Assert.That(receiver.Form, Is.Not.SameAs(first), "a form that went is made again");
        }
        finally
        {
            BoldLog.UnregisterLogReceiver(receiver);
            receiver.Dispose();
        }
    }

    [Test]
    [Covers(typeof(BoldLogHandlerForm), "*")]
    public void The_unit_registers_one_log_form_receiver()
    {
        BoldLogHandlerForm.Initialization();
        TBoldLogHandlerReceiver receiver = BoldLogHandlerForm.Receiver!;
        BoldLogHandlerForm.Initialization();
        Assert.That(BoldLogHandlerForm.Receiver, Is.SameAs(receiver));
        BoldLogHandlerForm.Finalization();
        Assert.That(BoldLogHandlerForm.Receiver, Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldLogFrame), "*")]
    public void The_small_log_frame_shows_the_session_its_progress_and_a_light_for_warnings()
    {
        var frame = new TBoldLogFrame();
        using var form = new TestForm(frame);
        frame.Dock = DockStyle.Bottom;
        Assert.That(frame.Visible, Is.False);

        BoldLog.StartLog("Session");
        Assert.That(frame.Visible, Is.True);
        Assert.That(frame.lblLogMainHeader.Text, Is.EqualTo("Session"));
        Assert.That(frame.SessionName, Is.EqualTo("Session"));
        Assert.That(frame.btnStop.Enabled, Is.True);
        BoldLog.LogHeader = "Header";
        Assert.That(frame.lblLogHeader.Text, Is.EqualTo("Header"));
        BoldLog.Log("  info  ");
        Assert.That(frame.lblLogText.Text, Is.EqualTo("info"));
        BoldLog.Log("detail", TBoldLogType.ltDetail);
        Assert.That(frame.lblLogText.Text, Is.EqualTo("info"), "details are not shown");

        BoldLog.ProgressMax = 10;
        BoldLog.Progress = 5;
        BoldLog.ProgressStep();
        Assert.That(frame.pgLog.Value, Is.EqualTo(6));
        Assert.That(frame.lblTimeLeft.Text, Does.StartWith("Time left: "));
        Assert.That(frame.lblTotTime.Text, Does.StartWith("Tot time: "));

        BoldLog.Log("careful", TBoldLogType.ltWarning);
        Assert.That(frame.WarningPanel.Visible, Is.True);
        Assert.That(frame.WarningIndicator.BrushColor, Is.EqualTo(Color.Yellow));
        frame.WarningIndicator.Refresh();
        OnClickMethod.Invoke(frame.WarningIndicator, [EventArgs.Empty]);

        frame.btnStop.PerformClick();
        Assert.That(frame.btnStop.Enabled, Is.False);
        Assert.That(BoldLog.ProcessInterruption(), Is.True, "stop asks the process to stop");
        BoldLog.Sync();

        BoldLog.EndLog();
        Assert.That(frame.lblLogText.Text, Is.EqualTo(SysUtils.Format(BoldCommonConst.sLogSmallWarnings, BoldCommonConst.sLogSmallDone)));
        Assert.That(frame.pgLog.Value, Is.EqualTo(frame.pgLog.Maximum));
        Assert.That(frame.Timer1.Enabled, Is.False, "with warnings the frame stays");

        BoldLog.StartLog("Again");
        Assert.That(frame.WarningPanel.Visible, Is.False);
        BoldLog.Log("bad", TBoldLogType.ltError);
        Assert.That(frame.WarningIndicator.BrushColor, Is.EqualTo(Color.Red));
        BoldLog.EndLog();
        Assert.That(frame.lblLogText.Text, Is.EqualTo(SysUtils.Format(BoldCommonConst.sLogSmallErrors, BoldCommonConst.sLogSmallDone)));

        BoldLog.StartLog("Quiet");
        BoldLog.EndLog();
        Assert.That(frame.Timer1.Enabled, Is.True);
        DateTime until = DateTime.Now.AddSeconds(5);
        while (frame.Visible && DateTime.Now < until)
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }

        Assert.That(frame.Visible, Is.False, "the frame hides a second after a quiet session");
        Assert.That(frame.lblLogMainHeader.Text, Is.Empty);

        BoldLog.Hide();
        BoldLog.Show();
        Assert.That(frame.Visible, Is.True);
        BoldLog.Clear();
        frame.Dispose();
        BoldLog.Log("after");

        Form small = TBoldLogFrame.CreateSmallLogForm("Working");
        try
        {
            Assert.That(small.Text, Is.EqualTo("Working"));
            Assert.That(small.ControlBox, Is.False);
            Assert.That(small.Controls.OfType<TBoldLogFrame>().Single().Size, Is.EqualTo(small.ClientSize));
        }
        finally
        {
            small.Dispose();
        }
    }
}
