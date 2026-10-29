// Delphi: Source/PMapper/DbEvolutor/BoldDbEvolutorForm.pas
// Port map: CSharp/port/units/BoldDbEvolutorForm.md
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>Evolving a SQLite database to a changed model through the evolution form.</summary>
[NonParallelizable]
public class BoldDbEvolutorFormTests
{
    [TearDown]
    public void TearDown() => TfrmBoldDbEvolutor.ShowModalForm = form => form.ShowDialog();

    [Test]
    [Covers(typeof(TfrmBoldDbEvolutor), nameof(TfrmBoldDbEvolutor.EvolveDB), nameof(TfrmBoldDbEvolutor.SQLScript), nameof(TfrmBoldDbEvolutor.MappingInfoScript),
        nameof(TfrmBoldDbEvolutor.Warnings), "GetSQLScript", "GetMappingInfoScript", "GetWarnings", "FormClose")]
    public void The_form_shows_the_changes_and_executes_them_when_told()
    {
        using var values = new ValuesSample();
        string file;
        using (var first = new SqliteHandle(values.Model) { KeepFile = true })
        {
            file = first.File;
        }

        try
        {
            using (var same = new SqliteHandle(values.Model, createSchema: false, file: file))
            {
                TfrmBoldDbEvolutor.EvolveDB(same.Handle, false);
                Assert.That(values.Environment.Dialogs, Has.Member(BoldPMConsts.sNoUpdateNeeded));
            }

            TMoldClass thing = values.Model.Classes.ItemsByName("Thing")!;
            new TMoldAttribute(thing, "extra").SetStdTV(BoldUMLTaggedValues.TAG_PERSISTENCE, BoldUMLTaggedValues.TV_PERSISTENCE_PERSISTENT);
            using var next = new SqliteHandle(values.Model, createSchema: false, file: file);
            string script = "", actions = "", memo = "";
            TfrmBoldDbEvolutor.ShowModalForm = form =>
            {
                script = form.SQLScript.Text;
                memo = form.mmoSQLScript.Text;
                actions = form.mmoActions.Text;
                Assert.That(form.MappingInfoScript.Count, Is.GreaterThan(0));
                Assert.That(form.tsWarnings.Text, Is.EqualTo(SysUtils.Format(BoldPMConsts.sWarnings, form.Warnings.Count)));
                return DialogResult.Cancel;
            };
            TfrmBoldDbEvolutor.EvolveDB(next.Handle, false);
            Assert.That(script, Does.Contain("extra"));
            Assert.That(memo.Trim(), Is.EqualTo(script.Trim()), "the memo shows the script");
            Assert.That(actions, Does.Contain(BoldPMConsts.sDetectingChanges));
            Assert.That(TestDatabase.Scalar(next.Connection, "SELECT COUNT(*) FROM pragma_table_info('Thing') WHERE name = 'extra'"), Is.EqualTo(0), "cancelled");

            TfrmBoldDbEvolutor.ShowModalForm = _ => DialogResult.OK;
            TfrmBoldDbEvolutor.EvolveDB(next.Handle, false);
            Assert.That(values.Environment.Dialogs, Has.Member(BoldPMConsts.sUpdateSuccessful));
            Assert.That(TestDatabase.Scalar(next.Connection, "SELECT COUNT(*) FROM pragma_table_info('Thing') WHERE name = 'extra'"), Is.EqualTo(1));
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Test]
    [Covers(typeof(TfrmBoldDbEvolutor), "UpdateWarningCount", "FormCreate", "FormDestroy", "Dispose")]
    [Covers("BoldDbEvolutorForm", "TfrmBoldDbEvolutor.Create", "TfrmBoldDbEvolutor.SetProgress", "TfrmBoldDbEvolutor.SetLogHeader", "TfrmBoldDbEvolutor.SetProgressMax",
        "TfrmBoldDbEvolutor.Clear", "TfrmBoldDbEvolutor.Hide", "TfrmBoldDbEvolutor.Log", "TfrmBoldDbEvolutor.ProgressStep", "TfrmBoldDbEvolutor.Show",
        "TfrmBoldDbEvolutor.Sync", "TfrmBoldDbEvolutor.StartLog", "TfrmBoldDbEvolutor.EndLog", "TfrmBoldDbEvolutor.ProcessInterruption")]
    public void The_form_shows_the_log_and_the_warnings()
    {
        var form = new TfrmBoldDbEvolutor();
        try
        {
            TBoldLogHandler log = BoldLogHandler.BoldLog();
            log.StartLog("Evolution");
            Assert.That(form.Visible, Is.True, "the form shows when a session starts");
            form.Location = new System.Drawing.Point(-2000, -2000);
            log.LogHeader = "ignored";
            log.Log("an action");
            log.ProgressMax = 5;
            log.Progress = 2;
            log.ProgressStep();
            Assert.That(form.ProgressBar1.Maximum, Is.EqualTo(5));
            Assert.That(form.ProgressBar1.Value, Is.EqualTo(3));
            log.Clear();
            log.Sync();
            Assert.That(log.ProcessInterruption(), Is.False);
            log.EndLog();
            Assert.That(form.mmoActions.Text, Does.Contain("Starting Evolution").And.Contain("an action").And.Contain("Done Evolution"));
            Assert.That(form.mmoActions.Text, Does.Not.Contain("ignored"));
            log.Hide();
            Assert.That(form.Visible, Is.False);
            log.Show();
            Assert.That(form.Visible, Is.True);

            form.Warnings.Add("a warning");
            Assert.That(form.tsWarnings.Text, Is.EqualTo(SysUtils.Format(BoldPMConsts.sWarnings, 1)));
            Assert.That(form.mmoWarnings.Text, Does.Contain("a warning"));
            Assert.That(form.PageControl1.SelectedTab, Is.SameAs(form.tsActions));
        }
        finally
        {
            form.Dispose();
        }

        BoldLogHandler.BoldLog().Log("after");
    }
}
