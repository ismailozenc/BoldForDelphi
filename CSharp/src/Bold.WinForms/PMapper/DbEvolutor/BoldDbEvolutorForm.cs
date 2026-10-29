// Delphi: Source/PMapper/DbEvolutor/BoldDbEvolutorForm.pas
// Port map: CSharp/port/units/BoldDbEvolutorForm.md

using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldPMConsts;

namespace Bold;

/// <summary>
/// Shows the evolution of a database: the log of the actions, the warnings, the SQL script and the mapping info
/// script, with a progress bar; Execute runs the script.
/// </summary>
[Delphi(PortStatus.Ported, Note = "The form's layout is that of the Delphi form file. The scripts are string lists that the memos show.")]
public class TfrmBoldDbEvolutor : Form, IBoldLogReceiver
{
    private TStringList? fWarnings;
    private readonly TStringList fSQLScript = new();
    private readonly TStringList fMappingInfoScript = new();
    private string fSessionName = "";
    public readonly TabControl PageControl1;
    public readonly TabPage tsActions;
    public readonly TabPage tsSQLScript;
    public readonly TextBox mmoActions;
    public readonly TextBox mmoSQLScript;
    public readonly Button btnCancel;
    public readonly Button btnExecute;
    public readonly ProgressBar ProgressBar1;
    public readonly TabPage tsMappingInfo;
    public readonly TextBox mmoMappingInfoScript;
    public readonly TabPage tsWarnings;
    public readonly TextBox mmoWarnings;
    public readonly Button Button1;
    public readonly SaveFileDialog SaveDialog1;

    /// <summary>The form, registered with the global log.</summary>
    [Delphi(PortStatus.Ported, Note = "No owner.")]
    public TfrmBoldDbEvolutor()
    {
        SuspendLayout();
        Text = "Db Evolution";
        ClientSize = new Size(632, 446);
        MinimumSize = new Size(322, 200);
        StartPosition = FormStartPosition.CenterScreen;

        static TextBox Memo(string name) => new() { Name = name, Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false };
        static TabPage Page(string name, string caption, Control content)
        {
            var page = new TabPage { Name = name, Text = caption };
            page.Controls.Add(content);
            return page;
        }

        mmoActions = Memo("mmoActions");
        mmoWarnings = Memo("mmoWarnings");
        mmoSQLScript = Memo("mmoSQLScript");
        mmoMappingInfoScript = Memo("mmoMappingInfoScript");
        tsActions = Page("tsActions", "Actions", mmoActions);
        tsWarnings = Page("tsWarnings", "Warnings", mmoWarnings);
        tsSQLScript = Page("tsSQLScript", "SQL script", mmoSQLScript);
        tsMappingInfo = Page("tsMappingInfo", "Mapping info script", mmoMappingInfoScript);
        PageControl1 = new TabControl { Name = "PageControl1", Bounds = new Rectangle(0, 0, 632, 388), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom };
        PageControl1.TabPages.AddRange([tsActions, tsWarnings, tsSQLScript, tsMappingInfo]);
        ProgressBar1 = new ProgressBar { Name = "ProgressBar1", Bounds = new Rectangle(8, 397, 617, 16), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom, Style = ProgressBarStyle.Continuous, Step = 1 };
        btnCancel = new Button { Name = "btnCancel", Bounds = new Rectangle(545, 421, 81, 25), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Text = "Cancel", DialogResult = DialogResult.Cancel };
        btnExecute = new Button { Name = "btnExecute", Bounds = new Rectangle(457, 421, 81, 25), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Text = "&Execute", DialogResult = DialogResult.OK };
        Button1 = new Button { Name = "Button1", Bounds = new Rectangle(373, 421, 75, 25), Text = "Save scripts" };
        Button1.Click += Button1Click;
        SaveDialog1 = new SaveFileDialog();
        Controls.AddRange([PageControl1, ProgressBar1, btnCancel, btnExecute, Button1]);
        AcceptButton = btnExecute;
        CancelButton = btnCancel;
        ResumeLayout(false);

        fWarnings = new TStringList { OnChange = UpdateWarningCount };
        fSQLScript.OnChange = _ => mmoSQLScript.Text = fSQLScript.Text;
        fMappingInfoScript.OnChange = _ => mmoMappingInfoScript.Text = fMappingInfoScript.Text;
        FormClosed += FormClose;
        FormCreate(this, EventArgs.Empty);
    }

    [Delphi(PortStatus.Ported, Symbol = "TfrmBoldDbEvolutor.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FormDestroy(this, EventArgs.Empty);
            fWarnings = null;
            SaveDialog1.Dispose();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private void Clear()
    {
        //  mmoActions.lines.Clear;
    }

    [Delphi(PortStatus.Ported)]
    private void EndLog() => Log(SysUtils.Format(sDone, SysUtils.FormatDateTime("c", DateTime.Now), fSessionName)); // do not localize

    [Delphi(PortStatus.Ported)]
    private new void Hide() => base.Hide();

    /// <summary>The line goes to the actions.</summary>
    [Delphi(PortStatus.Ported)]
    private void Log(string s, TBoldLogType LogType = TBoldLogType.ltInfo)
    {
        mmoActions.AppendText((mmoActions.TextLength > 0 ? Environment.NewLine : "") + s);
        mmoActions.Refresh();
        mmoSQLScript.Refresh();
    }

    [Delphi(PortStatus.Ported)]
    private void ProgressStep() => ProgressBar1.PerformStep();

    [Delphi(PortStatus.Ported, Note = "Kept within the bar's range, as Delphi's progress bar does.")]
    private void SetProgress(int Value)
    {
        ProgressBar1.Value = Math.Clamp(Value, ProgressBar1.Minimum, ProgressBar1.Maximum);
        ProgressBar1.Refresh();
    }

    [Delphi(PortStatus.Ported)]
    private void SetLogHeader(string Value)
    {
        // intentionally left empty
    }

    [Delphi(PortStatus.Ported)]
    private void SetProgressMax(int Value)
    {
        ProgressBar1.Maximum = Math.Max(Value, ProgressBar1.Minimum);
        ProgressBar1.Refresh();
    }

    [Delphi(PortStatus.Ported)]
    private new void Show() => base.Show();

    /// <summary>The session's start is logged and the form shows.</summary>
    [Delphi(PortStatus.Ported)]
    private void StartLog(string SessionName)
    {
        Log(SysUtils.Format(sStarting, SysUtils.FormatDateTime("c", DateTime.Now), SessionName)); // do not localize
        fSessionName = SessionName;
        if (!Modal)
        {
            Show();
        }
    }

    [Delphi(PortStatus.Ported, Note = "Called by the constructor (Delphi's OnCreate).")]
    private void FormCreate(object? Sender, EventArgs e)
    {
        BoldLogHandler.BoldLog().RegisterLogReceiver(this);
        PageControl1.SelectedTab = tsActions;
    }

    [Delphi(PortStatus.Ported, Note = "Called by Dispose (Delphi's OnDestroy).")]
    private void FormDestroy(object? Sender, EventArgs e) => BoldLogHandler.BoldLog().UnregisterLogReceiver(this);

    /// <summary>A closed form goes (Delphi's caFree).</summary>
    [Delphi(PortStatus.Ported, Note = "The FormClosed event. WinForms disposes a closed modeless form itself; EvolveDB disposes the modal one.")]
    private void FormClose(object? Sender, FormClosedEventArgs e)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TStrings GetMappingInfoScript() => fMappingInfoScript;

    [Delphi(PortStatus.Ported)]
    private TStrings GetSQLScript() => fSQLScript;

    [Delphi(PortStatus.Ported)]
    private TStrings GetWarnings() => fWarnings!;

    /// <summary>The warnings are shown, and counted in the tab's caption.</summary>
    [Delphi(PortStatus.Ported)]
    private void UpdateWarningCount(object? Sender)
    {
        mmoWarnings.Text = fWarnings!.Text;
        tsWarnings.Text = SysUtils.Format(sWarnings, fWarnings.Count);
    }

    [Delphi(PortStatus.Ported)]
    private void ProcessInterruption()
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    private void Sync() => Application.DoEvents();

    /// <summary>The dialogs the evolution shows and the modal form (tests replace them).</summary>
    internal static Func<TfrmBoldDbEvolutor, DialogResult> ShowModalForm { get; set; } = form => form.ShowDialog();

    /// <summary>
    /// Evolves the database to the model: the changes are detected and shown, and when the user executes them the
    /// script runs (a failure logs the statements that ran and shows the log).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public static void EvolveDB(TBoldAbstractPersistenceHandleDB PersistenceHandle, bool GenerateGenericScript)
    {
        var Evolutor = new TBoldDataBaseEvolutor(PersistenceHandle);
        TfrmBoldDbEvolutor? EvolutorForm = new();
        EvolutorForm.Show();
        try
        {
            BoldLogHandler.BoldLog().StartLog(sDetectingChanges);
            Evolutor.GenericScript = GenerateGenericScript;
            try
            {
                Evolutor.CalculateScript();
                Evolutor.GenerateScript(EvolutorForm.SQLScript, EvolutorForm.MappingInfoScript);
                Evolutor.GenerateWarnings(EvolutorForm.Warnings);
            }
            catch (Exception e)
            {
                Dialogs.ShowMessage(SysUtils.Format(sFailedToDetectChanges, e.Message));
                return;
            }

            BoldLogHandler.BoldLog().EndLog();
            if (EvolutorForm.SQLScript.Count == 0)
            {
                Dialogs.ShowMessage(sNoUpdateNeeded);
                EvolutorForm.Close();
                EvolutorForm = null;
            }
            else
            {
                Dialogs.ShowMessage(SysUtils.Format(sInspectChanges, BoldDefs.BOLDCRLF, BoldDefs.BOLDCRLF));
                EvolutorForm.Hide();
                DialogResult ModalResult = ShowModalForm(EvolutorForm);
                TfrmBoldDbEvolutor shown = EvolutorForm;
                EvolutorForm = null;
                if (ModalResult == DialogResult.OK)
                {
                    try
                    {
                        BoldLogHandler.BoldLog().StartLog(sUpdateDatabase);
                        Evolutor.ExecuteScript();
                        BoldLogHandler.BoldLog().EndLog();
                        Dialogs.ShowMessage(sUpdateSuccessful);
                    }
                    catch (Exception e)
                    {
                        Dialogs.ShowMessage(SysUtils.Format(sEvolveFailed, e.Message, BoldDefs.BOLDCRLF, BoldDefs.BOLDCRLF));
                        var ExecutedStatements = new TStringList();
                        TBoldLogHandler log = BoldLogHandler.BoldLog();
                        log.Separator();
                        log.Log(sExecutedStatements);
                        log.Separator();
                        Evolutor.GenerateExecutedStatements(ExecutedStatements);
                        for (int i = 0; i < ExecutedStatements.Count; i++)
                        {
                            log.Log(ExecutedStatements[i]);
                        }

                        log.Separator();
                        log.Show();
                    }
                }

                if (!shown.IsDisposed)
                {
                    shown.Dispose();
                }
            }
        }
        finally
        {
            EvolutorForm?.Close();
        }
    }

    /// <summary>Saves the SQL script, then the mapping info script, to files the user picks.</summary>
    [Delphi(PortStatus.Ported)]
    private void Button1Click(object? Sender, EventArgs e)
    {
        SaveDialog1.FileName = "EvolutionScript.sql";
        if (SaveDialog1.ShowDialog(this) == DialogResult.OK)
        {
            fSQLScript.SaveToFile(SaveDialog1.FileName);
        }

        SaveDialog1.FileName = "MappingInfoScript.sql";
        if (SaveDialog1.ShowDialog(this) == DialogResult.OK)
        {
            fMappingInfoScript.SaveToFile(SaveDialog1.FileName);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TStrings SQLScript => GetSQLScript();

    [Delphi(PortStatus.Ported)]
    public TStrings MappingInfoScript => GetMappingInfoScript();

    [Delphi(PortStatus.Ported)]
    public TStrings Warnings => GetWarnings();

    void IBoldLogReceiver.SetProgress(int Value) => SetProgress(Value);

    void IBoldLogReceiver.SetLogHeader(string Value) => SetLogHeader(Value);

    void IBoldLogReceiver.SetProgressMax(int Value) => SetProgressMax(Value);

    void IBoldLogReceiver.Clear() => Clear();

    void IBoldLogReceiver.Hide() => Hide();

    void IBoldLogReceiver.Log(string s, TBoldLogType LogType) => Log(s, LogType);

    void IBoldLogReceiver.ProgressStep() => ProgressStep();

    void IBoldLogReceiver.Show()
    {
        if (!Modal)
        {
            Show();
        }
    }

    void IBoldLogReceiver.Sync() => Sync();

    void IBoldLogReceiver.StartLog(string SessionName) => StartLog(SessionName);

    void IBoldLogReceiver.EndLog() => EndLog();

    void IBoldLogReceiver.ProcessInterruption() => ProcessInterruption();
}
