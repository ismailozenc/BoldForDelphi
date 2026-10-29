// Delphi: Source/Common/Logging/BoldLogHandlerForm.pas
// Port map: CSharp/port/units/BoldLogHandlerForm.md

using System.Windows.Forms;
using static Bold.BoldCommonConst;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLogHandlerForm", Symbol = "TBoldLogHandlerReceiver.GetLogFormNotifier", Note = "Made the notifier that heard the log form go (FreeNotification); the form's Disposed event does that.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldLogHandlerForm", Symbol = "TBoldLogHandlerReceiver.LogFormNotifier", Note = "See GetLogFormNotifier.")]

namespace Bold;

/// <summary>A log receiver that shows the log in a log form (made on first use).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLogHandlerReceiver : IBoldLogReceiver, IDisposable
{
    private TBoldLogForm? fLogForm;
    private string fSessionName = "";

    [Delphi(PortStatus.Ported)]
    public void Clear() => LogForm.Clear();

    [Delphi(PortStatus.Ported, Symbol = "TBoldLogHandlerReceiver.Destroy")]
    public void Dispose()
    {
        TBoldLogForm? form = fLogForm;
        fLogForm = null;
        form?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Logs the end of the session and resets the progress bar.</summary>
    [Delphi(PortStatus.Ported)]
    public void EndLog()
    {
        Log(SysUtils.Format(sLogDone, SysUtils.FormatDateTime("c", DateTime.Now), fSessionName));
        HideProgressBar();
    }

    /// <summary>The log form, made on first use; when it goes, the next use makes another.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldLogForm GetLogForm()
    {
        if (fLogForm is null)
        {
            fLogForm = new TBoldLogForm();
            fLogForm.Disposed += Notification;
        }

        return fLogForm;
    }

    [Delphi(PortStatus.Ported)]
    public void Hide() => LogForm.Hide();

    [Delphi(PortStatus.Ported)]
    public void Log(string s, TBoldLogType LogType = TBoldLogType.ltInfo)
    {
        LogForm.AddLog(s);
        LogForm.Refresh();
    }

    [Delphi(PortStatus.Ported)]
    public void ProgressStep() => LogForm.ProgressBar1.PerformStep();

    [Delphi(PortStatus.Ported)]
    public void SaveLog() => LogForm.SaveLog();

    [Delphi(PortStatus.Ported, Note = "Kept within the bar's range, as Delphi's progress bar does.")]
    protected void SetProgress(int Value) => LogForm.ProgressBar1.Value = Math.Clamp(Value, LogForm.ProgressBar1.Minimum, LogForm.ProgressBar1.Maximum);

    /// <summary>The header between two lines of equal signs.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SetLogHeader(string Value)
    {
        LogForm.AddLog("==================================");
        LogForm.AddLog("===" + Value);
        LogForm.AddLog("==================================");
    }

    [Delphi(PortStatus.Ported)]
    protected void SetProgressMax(int Value)
    {
        if (Value > 0)
        {
            LogForm.ProgressBar1.Maximum = Value;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Show()
    {
        LogForm.Show();
        LogForm.UpdateView();
    }

    /// <summary>The form's caption names the session, whose start is logged.</summary>
    [Delphi(PortStatus.Ported)]
    public void StartLog(string SessionName)
    {
        LogForm.Text = SysUtils.Format(sLogFormCaption, SessionName);
        fSessionName = SessionName;
        Log(SysUtils.Format(sLogStarting, SysUtils.FormatDateTime("c", DateTime.Now), SessionName)); // do not localize
    }

    [Delphi(PortStatus.Ported)]
    protected void ProcessInterruption() => Application.DoEvents();

    [Delphi(PortStatus.Ported)]
    private void HideProgressBar() => LogForm.ProgressBar1.Value = LogForm.ProgressBar1.Minimum;

    [Delphi(PortStatus.Ported)]
    public void Sync() => Application.DoEvents();

    /// <summary>The log form went: the receiver has none.</summary>
    [Delphi(PortStatus.Ported, Note = "The log form's Disposed event.")]
    protected void Notification(object? AComponent, EventArgs e)
    {
        if (AComponent == fLogForm)
        {
            fLogForm = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldLogForm LogForm => GetLogForm();

    void IBoldLogReceiver.SetProgress(int Value) => SetProgress(Value);

    void IBoldLogReceiver.SetLogHeader(string Value) => SetLogHeader(Value);

    void IBoldLogReceiver.SetProgressMax(int Value) => SetProgressMax(Value);

    void IBoldLogReceiver.ProcessInterruption() => ProcessInterruption();
}

/// <summary>The unit's log receiver, registered with the global log by <see cref="Initialization"/>.</summary>
public static class BoldLogHandlerForm
{
    private static TBoldLogHandlerReceiver? LogHandlerForm;

    /// <summary>The registered receiver, null before <see cref="Initialization"/>.</summary>
    public static TBoldLogHandlerReceiver? Receiver => LogHandlerForm;

    /// <summary>Registers a log form receiver with the global log (once).</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Call it to show the log in a log form: Delphi ran it when the unit was linked into the application. It does not run by itself, as the log form would be made on whatever thread logs first.")]
    public static void Initialization()
    {
        if (LogHandlerForm is null)
        {
            LogHandlerForm = new TBoldLogHandlerReceiver();
            BoldLogHandler.BoldLog().RegisterLogReceiver(LogHandlerForm);
        }
    }

    /// <summary>Unregisters the receiver and lets its form go.</summary>
    [Delphi(PortStatus.Ported, Symbol = "finalization", Note = "Also disposes the receiver's form.")]
    public static void Finalization()
    {
        if (LogHandlerForm is { } receiver)
        {
            LogHandlerForm = null;
            BoldLogHandler.BoldLog().UnregisterLogReceiver(receiver);
            receiver.Dispose();
        }
    }
}
