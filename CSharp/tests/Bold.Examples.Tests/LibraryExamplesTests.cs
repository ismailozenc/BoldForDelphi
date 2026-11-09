// The Library examples (CSharp/examples/Library), run as their tutorials run them: each program runs to its end and
// says what the tutorial says it does.
using System.Diagnostics;

namespace Bold.Tests;

[NonParallelizable]
public class LibraryExamplesTests
{
    private string fFolder = "";

    [SetUp]
    public void SetUp()
    {
        fFolder = Path.Combine(Path.GetTempPath(), $"library-examples-{Guid.NewGuid():N}");
        Directory.CreateDirectory(fFolder);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Directory.Delete(fFolder, recursive: true);
        }
        catch (IOException)
        {
            // a file a provider still holds is left for the temp folder's cleanup
        }
    }

    /// <summary>Runs the example to its end (two minutes at most); it must end with 0. Returns what it wrote, lines without their ends.</summary>
    private static string[] Run(string example, params string[] arguments)
    {
        string program = Programs.Require($"examples/Library/{example}", $"Library.{example}");
        Process process = Programs.Start(program, arguments);
        try
        {
            Assert.That(process.WaitForExit(120_000), Is.True, $"Library.{example} did not end:\n{Programs.OutputOf(process)}");
            process.WaitForExit();   // and its output has been read to the end
            string output = Programs.OutputOf(process);
            Assert.That(process.ExitCode, Is.Zero, output);
            TestContext.Out.WriteLine(output);
            return output.Split('\n').Select(line => line.TrimEnd('\r')).ToArray();
        }
        finally
        {
            Programs.Stop(process);
        }
    }

    private string File(string name) => Path.Combine(fFolder, name);

    [Test]
    public void Tutorial_1_Basics_creates_links_queries_saves_and_reads_back()
    {
        string[] output = Run("Basics", File("basics.db"));
        Assert.That(output, Does.Contain("The model Library: 5 classes, 3 associations."));
        Assert.That(output, Does.Contain("Books: 3"));
        Assert.That(output, Does.Contain("Before 1970: The Left Hand of Darkness, Solaris"));
        Assert.That(output, Does.Contain("Ursula K. Le Guin wrote 2 books (bookCount is derived: books->size)."));
        Assert.That(output, Does.Contain("Solaris: 1 copy, 0 available (copies - loans->size)."));
        Assert.That(output, Does.Contain("Ann has 1 loan, of Solaris."));
        Assert.That(output, Does.Contain("7 new objects to save."));
        Assert.That(output, Does.Contain("Saved; 0 left to save."));
        Assert.That(output, Does.Contain("  Ursula K. Le Guin: The Left Hand of Darkness (1969), The Dispossessed (1974)"));
        Assert.That(output, Does.Contain("  Stanislaw Lem: Solaris (1961)"));
        Assert.That(output, Does.Contain("  Lent: Ann has Solaris"));
    }

    [Test]
    public void Tutorial_2_Typed_uses_the_generated_classes_and_their_methods()
    {
        string[] output = Run("Typed", File("typed.db"));
        string due = (DateTime.Today + TimeSpan.FromDays(14)).ToString("yyyy-MM-dd");
        Assert.That(output, Does.Contain("The Dispossessed is by Ursula K. Le Guin; it has 1 copy (the model's initial value)."));
        Assert.That(output, Does.Contain("  1968 A Wizard of Earthsea"));
        Assert.That(output, Does.Contain("Ursula K. Le Guin: 2 books, 3 copies."));
        Assert.That(output, Does.Contain($"Ann borrows A Wizard of Earthsea, due {due} (TLoan.CompleteCreate sets it)."));
        Assert.That(output, Does.Contain("After Bob: 0 of 2 copies available (derived in OCL: copies - loans->size)."));
        Assert.That(output, Does.Contain("Cy gets none: no copy is left."));
        Assert.That(output, Does.Contain("Overdue: False"));
        Assert.That(output, Does.Contain("Due yesterday, overdue: True"));
        Assert.That(output, Does.Contain("Ann returns it: 1 available, Ann has 0 loans."));
        Assert.That(output, Does.Contain("  Ann: no loans"));
        Assert.That(output, Does.Contain($"  Bob: A Wizard of Earthsea, due {due}"));
    }

    [Test]
    public void Tutorial_3_Rules_derives_subscribes_checks_constraints_rolls_back_and_undoes()
    {
        string[] output = Run("Rules", File("rules.db"));
        Assert.That(output, Does.Contain("Ann borrows it: 2 available."));
        Assert.That(output.Count(line => line.StartsWith("  (told:", StringComparison.Ordinal)), Is.GreaterThanOrEqualTo(4), "the subscriber hears of the changes");
        Assert.That(output, Does.Contain("  Not saved: Member 'Ann': At most three loans."));
        Assert.That(output, Does.Contain("Ann returns A Wizard of Earthsea (3 loans, 2 copies available):"));
        Assert.That(output, Does.Contain("  Not saved: Book '': Has a title."));
        Assert.That(output, Does.Contain("Bob wants A Wizard of Earthsea and Tehanu together; Tehanu is out, so neither: he has 1 loan."));
        Assert.That(output, Does.Contain("Two copies more: 4 available."));
        Assert.That(output, Does.Contain("Undone: 2 available."));
        Assert.That(output, Does.Contain("Redone: 4 available."));
        Assert.That(output.Count(line => line == "  Saved."), Is.EqualTo(2));
    }

    [Test]
    public void Tutorial_5_Network_keeps_two_desks_in_step_and_locks()
    {
        string[] output = Run("Network", File("network.db"));
        Assert.That(output, Does.Contain("Two desks are connected: Front desk and Back office."));
        Assert.That(output, Does.Contain("Back office saves Solaris; Front desk has 1 book: Solaris."));
        Assert.That(output, Does.Contain("Back office lends Solaris to Ann; Front desk: 0 copies available."));
        Assert.That(output, Does.Contain("Back office cannot change the title: Front desk holds the lock."));
        Assert.That(output, Does.Contain("Front desk saves; Back office sees 2 copies, then changes the title, which Front desk sees: Solaris (1961)."));
    }

    [Test]
    public void Tutorial_5_Network_serves_until_told_to_stop()
    {
        string program = Programs.Require("examples/Library/Network", "Library.Network");
        int port = Programs.FreePort();
        var info = new ProcessStartInfo(program)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            WorkingDirectory = Path.GetDirectoryName(program)!,
        };
        info.ArgumentList.Add("--serve");
        info.ArgumentList.Add($"http://127.0.0.1:{port}");
        using var process = Process.Start(info)!;
        try
        {
            string? first = process.StandardOutput.ReadLine();
            Assert.That(first, Does.StartWith($"The library's server listens at http://127.0.0.1:{port}"));
            using var client = new HttpClient();
            HttpResponseMessage answer = client.PostAsync($"http://127.0.0.1:{port}/persistence", new StringContent("")).GetAwaiter().GetResult();
            Assert.That((int)answer.StatusCode, Is.EqualTo(200), "the persistence endpoint answers");
            process.StandardInput.WriteLine();
            Assert.That(process.WaitForExit(30_000), Is.True, "Enter stops it");
            Assert.That(process.ExitCode, Is.Zero);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    [Test]
    public void Tutorial_6_Evolution_evolves_the_database_and_keeps_the_objects()
    {
        string[] output = Run("Evolution", File("evolution.db"));
        Assert.That(output, Does.Contain("Version 1: The Left Hand of Darkness (1969), Solaris (1961)."));
        Assert.That(output, Does.Contain("  Add table Reservation"));
        Assert.That(output, Does.Contain("  Add column Book.Isbn"));
        Assert.That(output, Does.Contain("  Add column Book.Published"));
        Assert.That(output, Does.Contain("  Move data from Book.Year to Book.Published (dbtypes: 2)"));
        Assert.That(output, Does.Contain("  Drop column Book.Year"));
        Assert.That(output, Does.Contain("  No warnings: no values are lost."));
        Assert.That(output, Does.Contain("Version 2: The Left Hand of Darkness (1969), Solaris (1961)."));
        Assert.That(output, Does.Contain("Saved and read again: Solaris, ISBN 978-0-306-40615-7; Ann waits for Solaris."));
    }

    [Test]
    public void Tutorial_7_Xmi_exports_imports_and_runs_a_model()
    {
        string[] output = Run("Xmi", fFolder);
        Assert.That(output, Does.Contain("Library.xmi: 4 UML classes and 3 associations, for a UML tool."));
        Assert.That(output, Does.Contain("Hotel.xmi: the model Hotel, root class BusinessClassesRoot:"));
        Assert.That(output, Does.Contain("  Guest: name: String, Checkout()"));
        Assert.That(output, Does.Contain("  Suite (a Room): lounge: Boolean"));
        Assert.That(output, Does.Contain("  (set in the UML tool: Guest's stereotype person and its tagged value Bold.TableName=GUESTS)"));
        Assert.That(output, Does.Contain("  Stay: Guest.room [0..1] and Room.guests [0..*]"));
        Assert.That(output, Does.Contain("  Furnishing: Room.beds [1..*] and Bed.room [0..1]"));
        Assert.That(output, Does.Contain("Saved and read again: Suite 101, 1 bed, guests Ann."));
        Assert.That(System.IO.File.Exists(File("Library.xmi")) && System.IO.File.Exists(File("Hotel.bld")), Is.True);
    }

    [Test]
    public void Tutorial_8_Attributes_uses_the_librarys_own_attribute_types()
    {
        string[] output = Run("Attributes", File("attributes.db"));
        Assert.That(output, Does.Contain("Solaris: condition New (the default, the first value), shelf L, position 3."));
        Assert.That(output, Does.Contain("The conditions: New,Good,Worn,Damaged; in C#, cNew, cGood, cWorn, cDamaged."));
        Assert.That(output, Does.Contain("Worn: Solaris."));
        Assert.That(output, Does.Contain("An ISBN with a wrong check digit: refused (Copy.isbn.SetStringRepresentation failed: String validation failed)."));
        Assert.That(output, Does.Contain("A shelf mark without a position: refused (\"L\" is not a shelf mark (shelf-position, such as B-12))."));
        Assert.That(output, Does.Contain("  In the table Copy: Solaris | Shelf M | Shelf_1 1"));
        Assert.That(output, Does.Contain("Read again: Solaris, ISBN 978-0-306-40615-7, Worn, shelf M-1."));
        Assert.That(output, Does.Contain("Read again: A Wizard of Earthsea, ISBN 978-1-234-56789-7, Good, shelf L-7."));
    }

    [Test]
    public void Tutorial_9_Branches_lend_a_book_and_check_it_back_in()
    {
        string[] output = Run("Branches", fFolder);
        Assert.That(output, Does.Contain("Central: Solaris (1 copy), The Cyberiad (1 copy)."));
        Assert.That(output, Does.Contain("Harbour: Solaris (1 copy)."));
        Assert.That(output, Does.Contain("  Central changes Solaris: refused (Book.copies.SetDataValue failed: Member is read only)."));
        Assert.That(output, Does.Contain("  Harbour changes its author: refused (Author.name.SetDataValue failed: Member is read only)."));
        Assert.That(output, Does.Contain("Harbour saved: Solaris (3 copies)."));
        Assert.That(output, Does.Contain("Checked in, Central: Solaris (3 copies), The Cyberiad (1 copy)."));
        Assert.That(output, Does.Contain("  Central changes Solaris: done."));
        Assert.That(output, Does.Contain("  Harbour changes Solaris: refused (Book.copies.SetDataValue failed: Member is read only)."));
    }
}
