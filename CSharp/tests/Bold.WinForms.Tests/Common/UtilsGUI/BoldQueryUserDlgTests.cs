// Delphi: Source/Common/UtilsGUI/BoldQueryUserDlg.pas
// Port map: CSharp/port/units/BoldQueryUserDlg.md
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>The question with yes to all, yes, no and cancel.</summary>
[NonParallelizable]
public class BoldQueryUserDlgTests
{
    /// <summary>Asks, and presses the button the choice gives once the dialog shows.</summary>
    private static (TBoldQueryResult Result, string Caption, string Question) Ask(string title, string query, Func<TfrmBoldQueryUser, Button> choice)
    {
        string caption = "", question = "";
        using var timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += (_, _) =>
        {
            if (Application.OpenForms.OfType<TfrmBoldQueryUser>().FirstOrDefault() is { Visible: true } dialog)
            {
                timer.Stop();
                caption = dialog.Text;
                question = dialog.lblQuestion.Text;
                choice(dialog).PerformClick();
            }
        };
        timer.Start();
        TBoldQueryResult result = BoldQueryUserDlg.QueryUser(title, query);
        return (result, caption, question);
    }

    [Test]
    [Covers(typeof(TfrmBoldQueryUser), "*")]
    [Covers(typeof(BoldQueryUserDlg), "*")]
    [Covers("BoldQueryUserDlg", "TBoldQueryResult")]
    public void The_dialog_gives_the_button_pressed()
    {
        var yes = Ask("Delete", "Delete the table?", d => d.cmdYes);
        Assert.That(yes.Result, Is.EqualTo(TBoldQueryResult.qrYes));
        Assert.That(yes.Caption, Is.EqualTo("Delete"));
        Assert.That(yes.Question, Is.EqualTo("Delete the table?"));

        var defaults = Ask("", "", d => d.cmdYesAll);
        Assert.That(defaults.Result, Is.EqualTo(TBoldQueryResult.qrYesAll));
        Assert.That(defaults.Caption, Is.EqualTo("User Query"));
        Assert.That(defaults.Question, Is.EqualTo(BoldCommonConst.sDefaultQuery));

        Assert.That(Ask("", "?", d => d.cmdNo).Result, Is.EqualTo(TBoldQueryResult.qrNo));
        Assert.That(Ask("", "?", d => d.cmdNoAll).Result, Is.EqualTo(TBoldQueryResult.qrNoAll));
        Assert.That(BoldPSDescriptionsSQL.QueryUserHandler, Is.Not.Null, "Bold.WinForms asks the persistence layer's questions");
    }
}
