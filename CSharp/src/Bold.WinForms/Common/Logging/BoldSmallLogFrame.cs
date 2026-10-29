// Delphi: Source/Common/Logging/BoldSmallLogFrame.pas
// Port map: CSharp/port/units/BoldSmallLogFrame.md

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldCommonConst;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSmallLogFrame", Symbol = "TExposedShape", Note = "Opened the shape's OnClick; a WinForms control's Click event is public.")]

namespace Bold;

/// <summary>
/// A strip that shows the running log session: its name, header and last line, a progress bar with the time left, a
/// stop button, and a light that turns yellow on warnings and red on errors (a click shows the full log). It hides a
/// second after the session ends without warnings.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A UserControl (Delphi's frame); the layout is that of the Delphi frame file.")]
public class TBoldLogFrame : UserControl, IBoldLogReceiver
{
    private DateTime fStartTime;
    private string fSessionName = "";
    private TBoldLogType fHighestSeverity;
    private readonly ToolTip fHints = new();
    public readonly ProgressBar pgLog;
    public readonly Panel WarningPanel;
    public readonly TBoldShape WarningIndicator;
    public readonly Panel InfoPanel;
    public readonly Label lblLogText;
    public readonly Label lblLogMainHeader;
    public readonly System.Windows.Forms.Timer Timer1;
    public readonly Panel Panel1;
    public readonly Button btnStop;
    public readonly Label lblTimeLeft;
    public readonly Label lblTotTime;
    public readonly Label lblLogHeader;

    /// <summary>The frame, registered with the global log.</summary>
    [Delphi(PortStatus.Ported, Note = "No owner.")]
    public TBoldLogFrame()
    {
        SuspendLayout();
        Size = new Size(443, 67);
        MinimumSize = new Size(175, 67);
        MaximumSize = new Size(0, 67);
        Dock = DockStyle.Bottom;
        Visible = false;

        pgLog = new ProgressBar { Name = "pgLog", Dock = DockStyle.Bottom, Height = 17, Step = 1 };
        WarningIndicator = new TBoldShape { Name = "WarningIndicator", Bounds = new Rectangle(0, 2, 25, 25), BrushColor = Color.Yellow };
        fHints.SetToolTip(WarningIndicator, "Click to see full Log");
        WarningPanel = new Panel { Name = "WarningPanel", Dock = DockStyle.Left, Width = 25, Visible = false };
        WarningPanel.Controls.Add(WarningIndicator);
        lblLogText = new Label { Name = "lblLogText", Location = new Point(2, 32), AutoSize = true, Text = "lblLogText" };
        lblLogMainHeader = new Label { Name = "lblLogMainHeader", Location = new Point(2, 0), AutoSize = true, Text = "lblLogMainHeader" };
        lblLogHeader = new Label { Name = "lblLogHeader", Location = new Point(2, 16), AutoSize = true, Text = "lblLogHeader" };
        InfoPanel = new Panel { Name = "InfoPanel", Dock = DockStyle.Fill };
        InfoPanel.Controls.AddRange([lblLogText, lblLogMainHeader, lblLogHeader]);
        lblTimeLeft = new Label { Name = "lblTimeLeft", Bounds = new Rectangle(0, 16, 110, 13), TextAlign = ContentAlignment.TopRight, Anchor = AnchorStyles.Top | AnchorStyles.Right, Text = "Time left: 00:00:00" };
        lblTotTime = new Label { Name = "lblTotTime", Bounds = new Rectangle(0, 0, 110, 13), TextAlign = ContentAlignment.TopRight, Anchor = AnchorStyles.Top | AnchorStyles.Right, Text = "Tot time: 00:00:00" };
        btnStop = new Button { Name = "btnStop", Bounds = new Rectangle(118, 2, 81, 25), Cursor = Cursors.Hand, Text = "Stop" };
        btnStop.Click += btnStopClick;
        Panel1 = new Panel { Name = "Panel1", Dock = DockStyle.Right, Width = 200 };
        Panel1.Controls.AddRange([lblTimeLeft, lblTotTime, btnStop]);
        Timer1 = new System.Windows.Forms.Timer { Enabled = false, Interval = 1000 };
        Timer1.Tick += Timer1Timer;
        Controls.AddRange([InfoPanel, Panel1, WarningPanel, pgLog]);
        ResumeLayout(false);

        lblLogText.Text = "";
        lblLogMainHeader.Text = "";
        lblLogHeader.Text = "";
        lblTimeLeft.Text = "";
        BoldLogHandler.BoldLog().RegisterLogReceiver(this);
        WarningIndicator.Click += WarningIndicatorClick;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldLogFrame.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            BoldLogHandler.BoldLog().UnregisterLogReceiver(this);
            Timer1.Dispose();
            fHints.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>The time left and the total time, from the progress so far.</summary>
    [Delphi(PortStatus.Ported)]
    private void CalculateTimeLeft()
    {
        if (pgLog.Maximum != 0 && pgLog.Value != 0)
        {
            TimeSpan UsedTime = DateTime.Now - fStartTime;
            double DonePart = (double)pgLog.Value / pgLog.Maximum;
            double PartLeft = 1 - DonePart;
            TimeSpan TimeLeft = PartLeft * UsedTime / DonePart;
            lblTimeLeft.Text = SysUtils.Format(sLogTimeLeft, Clock(TimeLeft)); // do not localize
            lblTotTime.Text = SysUtils.Format(sLogTotalTime, Clock(TimeLeft + UsedTime)); // do not localize
            Refresh();
        }
    }

    /// <summary>A time span as hh:mm:ss (whole days dropped, as Delphi's FormatDateTime of a TDateTime).</summary>
    private static string Clock(TimeSpan span) => SysUtils.FormatDateTime("hh:mm:ss", DateTime.MinValue.AddTicks(Math.Max(0, span.Ticks) % TimeSpan.TicksPerDay));

    [Delphi(PortStatus.Ported)]
    private void Clear()
    {
        lblLogMainHeader.Text = "";
        lblLogHeader.Text = "";
        lblLogText.Text = "";
        pgLog.Value = pgLog.Minimum;
        lblTimeLeft.Text = "";
        WarningPanel.Visible = false;
    }

    /// <summary>The session ended: done (with a word on warnings or errors), and the frame hides a second later unless there were any.</summary>
    [Delphi(PortStatus.Ported)]
    private void EndLog()
    {
        lblLogText.Text = sLogSmallDone;
        pgLog.Value = pgLog.Maximum;
        if (!WarningPanel.Visible)
        {
            Timer1.Enabled = true;
        }

        btnStop.Enabled = false;

        if (fHighestSeverity == TBoldLogType.ltWarning)
        {
            lblLogText.Text = SysUtils.Format(sLogSmallWarnings, sLogSmallDone);
        }
        else if (fHighestSeverity == TBoldLogType.ltError)
        {
            lblLogText.Text = SysUtils.Format(sLogSmallErrors, sLogSmallDone);
        }
    }

    [Delphi(PortStatus.Ported)]
    private new void Hide() => Visible = false;

    /// <summary>Shows the line (not details or separators); a warning lights yellow, an error red.</summary>
    [Delphi(PortStatus.Ported)]
    private void Log(string s, TBoldLogType LogType = TBoldLogType.ltInfo)
    {
        if (LogType is TBoldLogType.ltInfo or TBoldLogType.ltWarning or TBoldLogType.ltError)
        {
            lblLogText.Text = s.Trim();
            Refresh();
        }

        if (LogType == TBoldLogType.ltWarning)
        {
            WarningIndicator.BrushColor = Color.Yellow;
            WarningPanel.Visible = true;
        }

        if (LogType == TBoldLogType.ltError)
        {
            WarningIndicator.BrushColor = Color.Red;
            WarningPanel.Visible = true;
        }

        if (LogType is TBoldLogType.ltWarning or TBoldLogType.ltError && LogType > fHighestSeverity)
        {
            fHighestSeverity = LogType;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ProgressStep()
    {
        pgLog.PerformStep();
        CalculateTimeLeft();
    }

    [Delphi(PortStatus.Ported, Note = "Kept within the bar's range, as Delphi's progress bar does.")]
    private void SetProgress(int Value)
    {
        pgLog.Value = Math.Clamp(Value, pgLog.Minimum, pgLog.Maximum);
        CalculateTimeLeft();
    }

    [Delphi(PortStatus.Ported)]
    private void SetLogHeader(string Value)
    {
        lblLogHeader.Text = Value;
        Refresh();
    }

    [Delphi(PortStatus.Ported)]
    private void SetProgressMax(int Value) => pgLog.Maximum = Math.Max(Value, pgLog.Minimum);

    [Delphi(PortStatus.Ported)]
    private new void Show() => Visible = true;

    /// <summary>A new session: cleared, named, shown, and the clock started.</summary>
    [Delphi(PortStatus.Ported)]
    private void StartLog(string SessionName)
    {
        Clear();
        lblLogMainHeader.Text = SessionName;
        fSessionName = SessionName;
        Refresh();
        Show();
        fStartTime = DateTime.Now;
        Timer1.Enabled = false;
        btnStop.Enabled = true;
        fHighestSeverity = TBoldLogType.ltInfo;
    }

    [Delphi(PortStatus.Ported)]
    private void WarningIndicatorClick(object? Sender, EventArgs e) => BoldLogHandler.BoldLog().Show();

    [Delphi(PortStatus.Ported)]
    private void Timer1Timer(object? Sender, EventArgs e)
    {
        Clear();
        Hide();
    }

    /// <summary>Asks the running process to stop.</summary>
    [Delphi(PortStatus.Ported)]
    private void btnStopClick(object? Sender, EventArgs e)
    {
        BoldLogHandler.BoldLog().InterruptProcess();
        btnStop.Enabled = false;
    }

    [Delphi(PortStatus.Ported)]
    private void ProcessInterruption() => Application.DoEvents();

    [Delphi(PortStatus.Ported)]
    private void Sync() => Application.DoEvents();

    /// <summary>A form (centered, without border icons) holding a log frame, shown.</summary>
    [Delphi(PortStatus.Ported)]
    public static Form CreateSmallLogForm(string Caption)
    {
        var result = new Form { ControlBox = false, StartPosition = FormStartPosition.CenterScreen };
        var Frame = new TBoldLogFrame { Parent = result };
        result.ClientSize = Frame.Size;
        result.Text = Caption;
        result.Show();
        return result;
    }

    /// <summary>The session's name.</summary>
    [Browsable(false)]
    public string SessionName => fSessionName;

    void IBoldLogReceiver.SetProgress(int Value) => SetProgress(Value);

    void IBoldLogReceiver.SetLogHeader(string Value) => SetLogHeader(Value);

    void IBoldLogReceiver.SetProgressMax(int Value) => SetProgressMax(Value);

    void IBoldLogReceiver.Clear() => Clear();

    void IBoldLogReceiver.Hide() => Hide();

    void IBoldLogReceiver.Log(string s, TBoldLogType LogType) => Log(s, LogType);

    void IBoldLogReceiver.ProgressStep() => ProgressStep();

    void IBoldLogReceiver.Show() => Show();

    void IBoldLogReceiver.Sync() => Sync();

    void IBoldLogReceiver.StartLog(string SessionName) => StartLog(SessionName);

    void IBoldLogReceiver.EndLog() => EndLog();

    void IBoldLogReceiver.ProcessInterruption() => ProcessInterruption();
}

/// <summary>A filled circle (Delphi's TShape with stCircle) in the brush color.</summary>
public class TBoldShape : Control
{
    private Color fBrushColor = Color.White;

    public TBoldShape()
    {
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
    }

    /// <summary>The fill color.</summary>
    [Category("Appearance")]
    public Color BrushColor
    {
        get => fBrushColor;
        set
        {
            fBrushColor = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int size = Math.Min(ClientSize.Width, ClientSize.Height) - 1;
        if (size > 0)
        {
            var bounds = new Rectangle((ClientSize.Width - size) / 2, (ClientSize.Height - size) / 2, size, size);
            using var brush = new SolidBrush(fBrushColor);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.FillEllipse(brush, bounds);
            e.Graphics.DrawEllipse(Pens.Black, bounds);
        }
    }
}
