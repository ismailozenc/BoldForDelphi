// The code generator as a program (CSharp/tools/Bold.CodeGen.Tool, bold-codegen).
using System.Diagnostics;

namespace Bold.Tests;

/// <summary>bold-codegen writes the classes of a model file, with the type names of custom attribute types when given.</summary>
[NonParallelizable]
public class BoldCodeGenToolTests
{
    private string fFolder = null!;

    [SetUp]
    public void SetUp() => fFolder = Path.Combine(Path.GetTempPath(), $"codegen-{Guid.NewGuid():N}");

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(fFolder))
        {
            Directory.Delete(fFolder, true);
        }
    }

    private static (int ExitCode, string Output) Run(params string[] arguments)
    {
        string program = Programs.Require("tools/Bold.CodeGen.Tool", "bold-codegen");
        Process process = Programs.Start(program, arguments);
        try
        {
            Assert.That(process.WaitForExit(120_000), Is.True, "bold-codegen ends");
            process.WaitForExit(); // the output is read to its end
            return (process.ExitCode, Programs.OutputOf(process));
        }
        finally
        {
            Programs.Stop(process);
        }
    }

    [Test]
    public void The_tool_writes_the_typed_classes_of_custom_attribute_types()
    {
        string example = RepoPaths.Full("CSharp/examples/CustomAttributes");
        (int exitCode, string output) = Run(Path.Combine(example, "Shapes.bld"), "--typenames", Path.Combine(example, "Shapes.typenames"), "--out", fFolder, "--quiet");
        Assert.That(exitCode, Is.Zero, output);
        string shape = File.ReadAllText(Path.Combine(fFolder, "Shape.g.cs"));
        Assert.That(shape, Does.Contain("public TBAColor M_Color => (TBAColor)BoldMembers(1);"));
        Assert.That(shape, Does.Contain("public TColorEnum Color").And.Contain("get => M_Color.AsColor;"), "the value set's enumeration, by the type names");
        Assert.That(shape, Does.Contain("public TBAPoint M_Position => (TBAPoint)BoldMembers(2);"));
        Assert.That(shape, Does.Contain("public TBAShortString M_Label"));
        Assert.That(File.Exists(Path.Combine(fFolder, "BusinessClassesRoot.g.cs")), Is.True);
    }

    [Test]
    public void The_tool_says_what_is_wrong_with_its_arguments()
    {
        (int exitCode, string output) = Run("--help");
        Assert.That((exitCode, output.Contains("--typenames")), Is.EqualTo((0, true)));
        (exitCode, output) = Run(Path.Combine(fFolder, "missing.bld"));
        Assert.That((exitCode, output.Contains("No such file")), Is.EqualTo((2, true)));
        (exitCode, output) = Run(RepoPaths.Full("CSharp/examples/CustomAttributes/Shapes.bld"), "--typenames", Path.Combine(fFolder, "missing.typenames"));
        Assert.That((exitCode, output.Contains("No such file")), Is.EqualTo((2, true)));
    }
}
