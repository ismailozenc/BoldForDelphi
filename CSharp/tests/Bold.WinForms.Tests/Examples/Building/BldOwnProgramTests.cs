// The Buildings and Owners example as a program (CSharp/examples/Building/BldOwn/BldOwn.cs), started as a user starts it.
using System.Diagnostics;

namespace Bold.Tests;

/// <summary>The example program starts on a database file, shows its form and ends when the form closes.</summary>
[NonParallelizable]
public class BldOwnProgramTests
{
    [Test]
    public void The_example_program_starts_and_ends_with_its_form()
    {
        string program = Programs.Require("examples/Building/BldOwn", "BldOwn");
        string file = Path.Combine(Path.GetTempPath(), $"bldown-program-{Guid.NewGuid():N}.db");
        Process process = Programs.Start(program, file);
        try
        {
            string title = "";
            Programs.WaitUntil(() =>
            {
                process.Refresh();
                title = process.MainWindowTitle;
                return title == "Buildings and owners";
            }, TimeSpan.FromSeconds(60), process);
            Assert.That(process.CloseMainWindow(), Is.True);
            Assert.That(process.WaitForExit(30_000), Is.True, "the program ends when its form closes");
            Assert.That(process.ExitCode, Is.Zero, Programs.OutputOf(process));
        }
        finally
        {
            Programs.Stop(process);
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}
