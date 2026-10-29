// Delphi: Source/Common/UtilsGUI/BoldQueryUserDlg.pas
// Port map: CSharp/port/units/BoldQueryUserDlg.md

using System.Drawing;
using System.Windows.Forms;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldQueryUserDlg", Symbol = "TBoldQueryResult", Note = "Declared in Bold.Persistence.Sql (BoldPSDescriptionsSQL.cs), so that the persistence layer can ask without WinForms.")]

namespace Bold;

/// <summary>A question with the answers yes to all, yes, no and cancel (no to all).</summary>
[Delphi(PortStatus.Ported, Note = "The form's layout is that of the Delphi form file.")]
public class TfrmBoldQueryUser : Form
{
    private TBoldQueryResult fQueryResult;
    public readonly Button cmdYesAll;
    public readonly Button cmdYes;
    public readonly Button cmdNo;
    public readonly Button cmdNoAll;
    public readonly Label lblQuestion;

    public TfrmBoldQueryUser()
    {
        SuspendLayout();
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        Text = "User Query";
        ClientSize = new Size(411, 136);
        StartPosition = FormStartPosition.CenterScreen;
        lblQuestion = new Label { Name = "lblQuestion", Bounds = new Rectangle(24, 12, 365, 77), AutoSize = false, Text = "lblQuestion" };
        cmdYes = NewButton("cmdYes", 32, "&Yes", cmdYesClick);
        cmdYesAll = NewButton("cmdYesAll", 120, "Yes to &all", cmdYesAllClick);
        cmdNo = NewButton("cmdNo", 208, "&No", cmdNoClick);
        cmdNoAll = NewButton("cmdNoAll", 296, "Cancel", cmdNoAllClick);
        Controls.AddRange([lblQuestion, cmdYes, cmdYesAll, cmdNo, cmdNoAll]);
        ResumeLayout(false);
    }

    private static Button NewButton(string name, int left, string caption, EventHandler click)
    {
        var result = new Button { Name = name, Bounds = new Rectangle(left, 100, 81, 25), Text = caption, DialogResult = DialogResult.OK };
        result.Click += click;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    private void cmdYesAllClick(object? Sender, EventArgs e) => fQueryResult = TBoldQueryResult.qrYesAll;

    [Delphi(PortStatus.Ported)]
    private void cmdYesClick(object? Sender, EventArgs e) => fQueryResult = TBoldQueryResult.qrYes;

    [Delphi(PortStatus.Ported)]
    private void cmdNoClick(object? Sender, EventArgs e) => fQueryResult = TBoldQueryResult.qrNo;

    [Delphi(PortStatus.Ported)]
    private void cmdNoAllClick(object? Sender, EventArgs e) => fQueryResult = TBoldQueryResult.qrNoAll;

    /// <summary>The answer (yes to all until a button is pressed).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldQueryResult QueryResult => fQueryResult;
}

/// <summary>Unit-level routines of BoldQueryUserDlg.</summary>
public static class BoldQueryUserDlg
{
    /// <summary>Asks the question (a default one when empty) in a modal dialog with the title, and gives the answer.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldQueryResult QueryUser(string Title, string Query)
    {
        using var form = new TfrmBoldQueryUser();
        if (Title != "")
        {
            form.Text = Title;
        }

        form.lblQuestion.Text = Query != "" ? Query : BoldCommonConst.sDefaultQuery;
        form.ShowDialog();
        return form.QueryResult;
    }
}
