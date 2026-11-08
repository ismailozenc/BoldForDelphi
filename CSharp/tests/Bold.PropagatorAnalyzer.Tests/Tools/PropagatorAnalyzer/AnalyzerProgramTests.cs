// The propagator analyzer as a program (CSharp/src/Bold.PropagatorAnalyzer/Program.cs), started as a user starts it.
using System.Diagnostics;

namespace Bold.Tests;

/// <summary>The analyzer program starts on a thread log, shows its form and ends when the form closes.</summary>
[NonParallelizable]
public class AnalyzerProgramTests
{
    [Test]
    public void The_analyzer_program_starts_on_a_thread_log()
    {
        string program = Programs.Require("src/Bold.PropagatorAnalyzer", "Bold.PropagatorAnalyzer");
        string log = Path.Combine(Path.GetTempPath(), $"propagator-{Guid.NewGuid():N}.thread");
        File.WriteAllLines(log, ["10:00:00 L=A:TID=1", "10:00:01 L=B:TID=1"]);
        Process process = Programs.Start(program, log);
        try
        {
            Programs.WaitUntil(() =>
            {
                process.Refresh();
                return process.MainWindowTitle == "Propagator analyzer";
            }, TimeSpan.FromSeconds(60), process);
            Assert.That(process.CloseMainWindow(), Is.True);
            Assert.That(process.WaitForExit(30_000), Is.True, "the program ends when its form closes");
            Assert.That(process.ExitCode, Is.Zero, Programs.OutputOf(process));
        }
        finally
        {
            Programs.Stop(process);
            File.Delete(log);
        }
    }
}
