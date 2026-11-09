// The Library examples' WinForms desk (CSharp/examples/Library/Desk, tutorial 4), in the test process and as a program.
using System.Diagnostics;
using System.Windows.Forms;
using Library.Desk;
using LibraryClasses;

namespace Bold.Tests;

/// <summary>The desk on a new SQLite database: the sample library in its grids, lending, undo and redo, returning and saving.</summary>
[NonParallelizable]
public class LibraryDeskTests
{
    [Test]
    public void The_desk_shows_the_library_lends_undoes_returns_and_saves()
    {
        string file = Path.Combine(Path.GetTempPath(), $"library-desk-{Guid.NewGuid():N}.db");
        using var environment = new GuiTestEnvironment();
        try
        {
            using (LibraryData data = LibraryData.OnDatabase(file))
            using (var form = new DeskForm(data))
            {
                BoldAFPDefaultTests.ShowOffScreen(form);
                Assert.That(form.Text, Is.EqualTo($"Library desk: {Path.GetFileName(file)}"));
                Assert.That(form.ShowsTheSampleLibrary(), Is.True, "the sample library a new database starts with");
                Assert.That(DeskForm.Cell(form.AuthorGrid, "bookCount", 1), Is.EqualTo("2"));
                Assert.That(DeskForm.Cell(form.BookGrid, "available", 1), Is.EqualTo("2"));
                Assert.That(DeskForm.Cell(form.MemberGrid, "loanCount", 1), Is.EqualTo("1"));

                // The current member (Ann) borrows the current book (the current author's first, A Wizard of Earthsea).
                var ann = (TMember)form.Members.CurrentBoldObject!;
                var earthsea = (TBook)form.Books.CurrentBoldObject!;
                Assert.That((ann.Name, earthsea.Title), Is.EqualTo(("Ann", "A Wizard of Earthsea")));
                form.LendButton.PerformClick();
                GuiSample.Display();
                Assert.That(ann.LoanCount, Is.EqualTo(2));
                Assert.That(DeskForm.Cell(form.LoanGrid, "book.title", 2), Is.EqualTo("A Wizard of Earthsea"));
                Assert.That(DeskForm.Cell(form.BookGrid, "available", 1), Is.EqualTo("1"), "the grid follows the derived member");

                // Undo and redo the lending: the actions enable their buttons when the application is idle.
                Application.RaiseIdle(EventArgs.Empty);
                Assert.That(form.UndoButton.Enabled, Is.True);
                form.UndoButton.PerformClick();
                Assert.That(ann.LoanCount, Is.EqualTo(1));
                Application.RaiseIdle(EventArgs.Empty);
                Assert.That(form.RedoButton.Enabled, Is.True);
                form.RedoButton.PerformClick();
                Assert.That(ann.LoanCount, Is.EqualTo(2));

                // The book's auto form, as a double click opens it.
                Assert.That(BoldAFPDefaultTests.FormFor(earthsea), Is.Not.Null);
                BoldAFPDefaultTests.CloseAutoForms();

                // Ann returns Solaris, and the desk saves.
                form.Loans.CurrentIndex = 0;
                form.ReturnButton.PerformClick();
                GuiSample.Display();
                Assert.That(ann.LoanCount, Is.EqualTo(1));
                Application.RaiseIdle(EventArgs.Empty);
                Assert.That(form.SaveButton.Enabled, Is.True);
                form.SaveButton.PerformClick();
                Assert.That(data.System.BoldDirty, Is.False);
                form.Close();
                Assert.That(environment.Dialogs, Is.Empty, "nothing to ask when all is saved");
            }

            // Opened again, the database has the change.
            using LibraryData again = LibraryData.OnDatabase(file);
            TMember annAgain = again.System.ClassByExpressionName("Member").Cast<TMember>().Single(member => member.Name == "Ann");
            Assert.That(annAgain.Loans.Cast<TLoan>().Select(loan => loan.Book!.Title), Is.EqualTo(new[] { "A Wizard of Earthsea" }));
        }
        finally
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // a file a provider still holds is left for the temp folder's cleanup
            }
        }
    }

    [Test]
    public void Closing_with_unsaved_changes_asks_whether_to_save_them()
    {
        string file = Path.Combine(Path.GetTempPath(), $"library-desk-{Guid.NewGuid():N}.db");
        using var environment = new GuiTestEnvironment();
        try
        {
            using LibraryData data = LibraryData.OnDatabase(file);
            using var form = new DeskForm(data);
            BoldAFPDefaultTests.ShowOffScreen(form);
            form.LendButton.PerformClick();
            environment.DialogAnswer = Bold.Controls.mrCancel;
            form.Close();
            Assert.That(environment.Dialogs, Is.EqualTo(new[] { "Save the changes?" }));
            Assert.That(form.IsDisposed, Is.False, "Cancel keeps the window");
            environment.DialogAnswer = Bold.Controls.mrYes;
            form.Close();
            Assert.That(form.IsDisposed, Is.True);
            Assert.That(data.System.BoldDirty, Is.False, "Yes saves");
        }
        finally
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // a file a provider still holds is left for the temp folder's cleanup
            }
        }
    }
}

/// <summary>The desk as a program, started as a user starts it: it shows the sample library and closes (its --close switch).</summary>
[NonParallelizable]
public class LibraryDeskProgramTests
{
    [Test]
    public void The_desk_program_shows_the_sample_library()
    {
        string program = Programs.Require("examples/Library/Desk", "Library.Desk");
        string file = Path.Combine(Path.GetTempPath(), $"library-desk-program-{Guid.NewGuid():N}.db");
        Process process = Programs.Start(program, file, "--close");
        try
        {
            Assert.That(process.WaitForExit(120_000), Is.True, Programs.OutputOf(process));
            Assert.That(process.ExitCode, Is.Zero, "the window showed the sample library:\n" + Programs.OutputOf(process));
        }
        finally
        {
            Programs.Stop(process);
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // a file a provider still holds is left for the temp folder's cleanup
            }
        }
    }
}
