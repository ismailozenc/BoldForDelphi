// Delphi: Source/Common/Logging/BoldLogForm.pas
// Port map: CSharp/port/units/BoldLogForm.md

using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldCommonConst;

namespace Bold;

/// <summary>
/// The log window: the log's lines (the last 50, or all), a progress bar, and a menu to clear, save and copy the log
/// and add a separator.
/// </summary>
[Delphi(PortStatus.Ported, Note = "The form's layout is that of the Delphi form file; the menu has no images.")]
public class TBoldLogForm : Form
{
    private TStringList? fLogLines;
    public readonly RichTextBox Log;
    public readonly MenuStrip MainMenu1;
    public readonly ToolStripMenuItem File1;
    public readonly ToolStripMenuItem SaveLog1;
    public readonly ToolStripSeparator N1;
    public readonly ToolStripMenuItem Edit1;
    public readonly ToolStripMenuItem mnuCopy;
    public readonly ToolStripMenuItem mnuCloseLogForm;
    public readonly Panel Panel1;
    public readonly ProgressBar ProgressBar1;
    public readonly ToolStripMenuItem mnuShowAll;
    public readonly ToolStripMenuItem mnuClearLog;
    public readonly ToolStripMenuItem mnuAddSeparator;

    public TBoldLogForm()
    {
        SuspendLayout();
        Text = "Bold";
        Size = new Size(469, 347);
        StartPosition = FormStartPosition.CenterScreen;

        Log = new RichTextBox { Name = "Log", Dock = DockStyle.Fill, ReadOnly = true, ScrollBars = RichTextBoxScrollBars.Both, WordWrap = false };
        Panel1 = new Panel { Name = "Panel1", Dock = DockStyle.Bottom, Height = 20 };
        ProgressBar1 = new ProgressBar { Name = "ProgressBar1", Dock = DockStyle.Fill, Step = 1 };
        Panel1.Controls.Add(ProgressBar1);

        mnuClearLog = new ToolStripMenuItem("Clear Log", null, mnuClearLogClick) { Name = "mnuClearLog" };
        SaveLog1 = new ToolStripMenuItem("&Save Log", null, cmdSaveClick) { Name = "SaveLog1" };
        N1 = new ToolStripSeparator { Name = "N1" };
        mnuCloseLogForm = new ToolStripMenuItem("Close log form", null, cmdCloseClick) { Name = "mnuCloseLogForm" };
        File1 = new ToolStripMenuItem("&File") { Name = "File1" };
        File1.DropDownItems.AddRange([mnuClearLog, SaveLog1, N1, mnuCloseLogForm]);
        mnuAddSeparator = new ToolStripMenuItem("Add Separator", null, mnuAddSeparatorClick) { Name = "mnuAddSeparator" };
        mnuCopy = new ToolStripMenuItem("&Copy", null, mnuCopyClick) { Name = "mnuCopy", ShortcutKeys = Keys.Control | Keys.C };
        mnuShowAll = new ToolStripMenuItem("Show all log", null, mnuShowAllClick) { Name = "mnuShowAll" };
        Edit1 = new ToolStripMenuItem("&Edit") { Name = "Edit1" };
        Edit1.DropDownItems.AddRange([mnuAddSeparator, mnuCopy, mnuShowAll]);
        MainMenu1 = new MenuStrip { Name = "MainMenu1" };
        MainMenu1.Items.AddRange([File1, Edit1]);

        Controls.AddRange([Log, Panel1, MainMenu1]);
        MainMenuStrip = MainMenu1;
        ResumeLayout(false);
        PerformLayout();
        FormCreate(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FormDestroy(this, EventArgs.Empty);
        }

        base.Dispose(disposing);
    }

    /// <summary>Adds the line and shows it; when showing fails, the user may clear the view and go on.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddLog(string s)
    {
        fLogLines!.Add(s);
        try
        {
            UpdateView();
        }
        catch (Exception E)
        {
            if (Dialogs.MessageDlg(SysUtils.Format(sClearAndContinue, E.Message), TMsgDlgType.mtWarning, DelphiSet<TMsgDlgBtn>.Of(TMsgDlgBtn.mbYes, TMsgDlgBtn.mbNo), 0) == global::Bold.Controls.mrYes)
            {
                Log.Clear();
            }
            else
            {
                throw;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Clear() => UpdateView();

    [Delphi(PortStatus.Ported)]
    private void cmdSaveClick(object? Sender, EventArgs e) => SaveLog();

    [Delphi(PortStatus.Ported)]
    private void cmdCloseClick(object? Sender, EventArgs e) => Close();

    /// <summary>Copies the selection of the active control.</summary>
    [Delphi(PortStatus.Ported, Note = "Copies from the active text control, where Delphi sent it WM_COPY.")]
    private void mnuCopyClick(object? Sender, EventArgs e) => (ActiveControl as TextBoxBase)?.Copy();

    /// <summary>Shows the log's lines when the form is visible: all of them, or the last 50.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpdateView()
    {
        if (Visible)
        {
            var lines = new List<string>();
            if (ShowAllLines())
            {
                for (int i = 0; i < fLogLines!.Count; i++)
                {
                    lines.Add(fLogLines[i]);
                }
            }
            else
            {
                for (int i = Math.Max(0, fLogLines!.Count - 50); i < fLogLines.Count; i++)
                {
                    lines.Add(fLogLines[i]);
                }
            }

            Log.Lines = [.. lines];
        }
    }

    [Delphi(PortStatus.Ported)]
    private void mnuShowAllClick(object? Sender, EventArgs e)
    {
        mnuShowAll.Checked = !mnuShowAll.Checked;
        UpdateView();
    }

    [Delphi(PortStatus.Ported)]
    private bool ShowAllLines() => mnuShowAll.Checked;

    [Delphi(PortStatus.Ported, Note = "Called by the constructor (Delphi's OnCreate).")]
    private void FormCreate(object? Sender, EventArgs e) => fLogLines = new TStringList();

    [Delphi(PortStatus.Ported, Note = "Called by Dispose (Delphi's OnDestroy).")]
    private void FormDestroy(object? Sender, EventArgs e) => fLogLines = null;

    /// <summary>Saves the log's lines to a file the user picks.</summary>
    [Delphi(PortStatus.Ported)]
    public void SaveLog()
    {
        using var dialog = new SaveFileDialog
        {
            DefaultExt = "log", // do not localize
            Filter = SysUtils.Format("%s (*.log)|*.log|%s (*.txt)|*.txt|%s (*.*)|*.*", sLogFiles, sTextFiles, sAllFiles), // do not localize
            OverwritePrompt = true,
            CheckPathExists = true,
            Title = sSaveLogAs,
        };
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            LogLines.SaveToFile(dialog.FileName);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void mnuAddSeparatorClick(object? Sender, EventArgs e) => BoldLogHandler.BoldLog().Separator();

    [Delphi(PortStatus.Ported)]
    private void mnuClearLogClick(object? Sender, EventArgs e) => fLogLines!.Clear();

    /// <summary>All the lines logged.</summary>
    [Delphi(PortStatus.Ported)]
    public TStringList LogLines => fLogLines!;
}
