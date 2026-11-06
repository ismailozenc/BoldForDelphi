// The model editor program (CSharp/src/Bold.ModelEditor/Program.cs).
using System.Diagnostics;

namespace Bold.Tests;

/// <summary>The model editor program starts on a model file, shows it and ends when its form closes.</summary>
[NonParallelizable]
public class ProgramTests
{
    /// <summary>The program, next to the tests or where its project builds it.</summary>
    private static string EditorProgram()
    {
        var testDirectory = new DirectoryInfo(Path.GetDirectoryName(typeof(ProgramTests).Assembly.Location)!);
        string beside = Path.Combine(testDirectory.FullName, "Bold.ModelEditor.exe");
        if (File.Exists(beside))
        {
            return beside;
        }

        // tests/Bold.ModelEditor.Tests/bin/<configuration>/<framework>
        DirectoryInfo csharp = testDirectory.Parent!.Parent!.Parent!.Parent!.Parent!;
        return Path.Combine(csharp.FullName, "src", "Bold.ModelEditor", "bin", testDirectory.Parent!.Name, testDirectory.Name, "Bold.ModelEditor.exe");
    }

    [Test]
    public void The_editor_program_opens_a_model_file()
    {
        string program = EditorProgram();
        Assert.That(File.Exists(program), Is.True, program);
        string model = FixturePaths.Model(EditorSamples.BuildingModel);
        using Process process = Process.Start(new ProcessStartInfo(program, $"\"{model}\"") { UseShellExecute = false })!;
        try
        {
            string expected = "Bold UML Model Editor - " + Path.GetFileName(model);
            string title = "";
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < TimeSpan.FromSeconds(60) && !process.HasExited && title != expected)
            {
                Thread.Sleep(250);
                process.Refresh();
                title = process.MainWindowTitle;
            }

            Assert.That(title, Is.EqualTo(expected), "the editor shows the model of the file");
            Assert.That(process.CloseMainWindow(), Is.True);
            Assert.That(process.WaitForExit(30_000), Is.True, "the program ends when its form closes");
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
}
