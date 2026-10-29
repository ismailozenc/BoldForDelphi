// The Buildings and Owners example: CSharp/examples/Building/BldOwn.
using System.Windows.Forms;
using BldOwn;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>The example application runs: on a new SQLite database, through its buttons and menus, as a user would.</summary>
[NonParallelizable]
public class BldOwnTests
{
    [Test]
    public void The_example_app_runs()
    {
        string file = Path.Combine(Path.GetTempPath(), $"bldown-{Guid.NewGuid():N}.db");
        using var environment = new GuiTestEnvironment();
        var reported = new List<Exception>();
        void Report(object? sender, System.Threading.ThreadExceptionEventArgs e) => reported.Add(e.Exception);
        Application.ThreadException += Report;
        try
        {
            Datamod.DataModule1 = new TDataModule1(TestDatabase.NewConnection(file));
            Mainform.allform = new Tallform();
            Tallform form = Mainform.allform;
            BoldAFPDefaultTests.ShowOffScreen(form);
            Assert.That(form.Text, Is.EqualTo("Buildings and owners"));
            Assert.That(form.Button1.Text, Is.EqualTo("Open system"));
            Assert.That(form.Button2.Text, Is.EqualTo("Create DB"));
            Assert.That(form.btnUpdateDB.Text, Is.EqualTo("Update DB"));

            // The first time, the database is created, then the system opened.
            form.Button2.PerformClick();
            Assert.That(TestDatabase.Scalar(Datamod.DataModule1.ADOConnection1, "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table'"), Is.GreaterThan(0));
            form.Button1.PerformClick();
            TBoldSystemHandle systemHandle = Datamod.DataModule1.BoldSystemHandle1;
            Assert.That(systemHandle.Active, Is.True);
            Assert.That(form.Button1.Text, Is.EqualTo("Close system"));
            Assert.That(systemHandle.System!.UndoHandlerInterface.Enabled, Is.True, "undo is on in the opened system");

            // Two persons from the person grid's menu, a residential building from the buildings' menu.
            form.NewPerson.PerformClick();
            form.NewPerson.PerformClick();
            GuiSample.Display();
            Assert.That(form.blhAllPerson.Count, Is.EqualTo(2));
            Assert.That(form.PersonCount.Text, Is.EqualTo("2"));
            var owner = (TPerson)form.blhAllPerson.List![0]!;
            var resident = (TPerson)form.blhAllPerson.List[1]!;
            Assert.That(form.bedFirstName.Text, Is.EqualTo(owner.FirstName), "the current person's details");
            Assert.That(form.PageControl1.SelectedTab, Is.SameAs(form.tabResidentialBuilding));
            form.newBuilding.PerformClick();
            GuiSample.Display();
            var house = (TResidential_Building)form.blhAllResidentialBuilding.CurrentBoldObject!;
            Assert.That(form.bedAddress2.Text, Is.EqualTo(house.Address));

            // An owner and a resident (as dragging the persons to the list boxes does); the resident pays the rent.
            house.Owners.Add(owner);
            house.Residents.Add(resident);
            house.TotalRent = 1000;
            owner.Assets = 100;
            resident.Assets = 5000;
            GuiSample.Display();
            Assert.That(form.blbBuildingOwners2.Items, Has.Count.EqualTo(1));
            Assert.That(form.blbBuildingResidents.Items[0]!.ToString(), Is.EqualTo(resident.LastName));
            form.btnChargeRent.PerformClick();
            Assert.That(resident.Assets, Is.EqualTo(4000m));
            Assert.That(owner.Assets, Is.EqualTo(1100m));

            // Undo and redo back to the checkpoint.
            form.btnCheckpoint.PerformClick();
            owner.FirstName = "Changed";
            form.btnUnDo.PerformClick();
            Assert.That(owner.FirstName, Is.Not.EqualTo("Changed"));
            form.btnRedo.PerformClick();
            Assert.That(owner.FirstName, Is.EqualTo("Changed"));

            // Saving: the button is enabled while there is something to save.
            Application.RaiseIdle(EventArgs.Empty);
            Assert.That(form.btnUpdateDB.Enabled, Is.True);
            form.btnUpdateDB.PerformClick();
            Assert.That(systemHandle.System.BoldDirty, Is.False);
            Application.RaiseIdle(EventArgs.Empty);
            Assert.That(form.btnUpdateDB.Enabled, Is.False);

            // Closing and opening the system again: the objects come from the database.
            form.Button1.PerformClick();
            Assert.That(systemHandle.Active, Is.False);
            Assert.That(form.Button1.Text, Is.EqualTo("Open system"));
            form.Button1.PerformClick();
            GuiSample.Display();
            Assert.That(form.blhAllPerson.Count, Is.EqualTo(2));
            house = (TResidential_Building)form.blhAllResidentialBuilding.CurrentBoldObject!;
            Assert.That(house.TotalRent, Is.EqualTo(1000m));
            Assert.That(house.Residents.Count, Is.EqualTo(1));
            Assert.That(house.Owners[0]!.FirstName, Is.EqualTo("Changed"));

            // Persons only: rich ones, alphabetically.
            Modal.Raise(form.CheckBox2, "OnClick", EventArgs.Empty);
            Assert.That(form.blhAllPerson.BoldComparer, Is.SameAs(Datamod.DataModule1.NameComparer));
            Modal.Raise(form.CheckBox1, "OnClick", EventArgs.Empty);
            GuiSample.Display();
            Assert.That(form.blhAllPerson.Count, Is.EqualTo(0), "nobody has more than 10,000");
            Modal.Raise(form.CheckBox1, "OnClick", EventArgs.Empty);
            Modal.Raise(form.CheckBox2, "OnClick", EventArgs.Empty);
            GuiSample.Display();
            Assert.That(form.blhAllPerson.Count, Is.EqualTo(2));

            // A new person shows in the form plugged in for persons, which goes when the person is deleted.
            form.NewPerson.PerformClick();
            GuiSample.Display();
            var newcomer = (TPerson)form.blhAllPerson.List![2]!;
            form.blhAllPerson.CurrentIndex = 2;
            form.ActiveControl = form.bgrPerson;
            form.Showinownwindow1.PerformClick();
            TPersonAutoForm personForm = Application.OpenForms.OfType<TPersonAutoForm>().Single();
            GuiSample.Display();
            Assert.That(personForm.beFirstName.Text, Is.EqualTo(newcomer.FirstName));
            form.ActiveControl = form.bgrPerson;
            form.DeletePerson.PerformClick();
            Assert.That(newcomer.BoldObjectIsDeleted, Is.True);
            Application.DoEvents();
            Assert.That(personForm.IsDisposed, Is.True, "the person's form is released");

            // Unsaved changes: quitting asks; no keeps the form, yes discards them.
            form.NewPerson.PerformClick();
            environment.DialogAnswer = Bold.Controls.mrNo;
            form.Close();
            Assert.That(environment.Dialogs, Has.Member("You have unsaved changes, quit anyway?"));
            Assert.That(form.IsDisposed, Is.False);
            environment.DialogAnswer = Bold.Controls.mrYes;
            form.Close();
            Assert.That(form.IsDisposed, Is.True);
            Assert.That(systemHandle.System.BoldDirty, Is.False, "discarded");
            Assert.That(reported, Is.Empty, "no exceptions reached the application");
        }
        finally
        {
            Application.ThreadException -= Report;
            Mainform.allform?.Dispose();
            Datamod.DataModule1?.Dispose();
            Mainform.allform = null!;
            Datamod.DataModule1 = null!;
            File.Delete(file);
        }
    }
}
