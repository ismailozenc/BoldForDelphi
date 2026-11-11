// Bold for Visual Studio (CSharp/vs): the part of the extension that needs no Visual Studio. BoldTools.cs is compiled
// into these tests, and starts the real bold-codegen as the extension's "Generate Bold Classes" command does. The
// extension's ids are checked against each other, since Visual Studio only reports a mismatch as a menu that is missing.
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Bold.VisualStudio;

namespace Bold.Tests;

[NonParallelizable]
public class VisualStudioExtensionTests
{
    private static string Extension(string file) => RepoPaths.Full("CSharp/vs/Bold.VisualStudio/" + file);

    [Test]
    public void Generating_classes_runs_bold_codegen_on_the_model_into_its_folder()
    {
        string codegen = Programs.Require("tools/Bold.CodeGen.Tool", "bold-codegen");
        string folder = Path.Combine(Path.GetTempPath(), $"bold vs {Guid.NewGuid():N}");   // a space, as in "Visual Studio 2022"
        string? before = Environment.GetEnvironmentVariable("BOLD_TOOLS");
        try
        {
            // The extension's tools folder, with bold-codegen as build.ps1 publishes it.
            string tools = Path.Combine(folder, "tools");
            Directory.CreateDirectory(Path.Combine(tools, "codegen"));
            foreach (string file in Directory.EnumerateFiles(Path.GetDirectoryName(codegen)!))
            {
                File.Copy(file, Path.Combine(tools, "codegen", Path.GetFileName(file)));
            }

            Environment.SetEnvironmentVariable("BOLD_TOOLS", tools);
            string model = Path.Combine(folder, "My Models", "Library.bld");
            Directory.CreateDirectory(Path.GetDirectoryName(model)!);
            File.Copy(RepoPaths.Full("CSharp/examples/Library/Library.bld"), model);

            Assert.That(BoldTools.IsModel(model), Is.True);
            Assert.That(BoldTools.ClassesFolder(model), Is.EqualTo(Path.GetDirectoryName(model)));
            var output = new List<string>();
            int exitCode = BoldTools.GenerateClasses(model, line => { lock (output) { output.Add(line); } });
            Assert.That(exitCode, Is.Zero, string.Join("\n", output));
            foreach (string written in new[] { "Book.g.cs", "Author.g.cs", "Member.g.cs", "Loan.g.cs", "LibraryClasses.g.cs", "Member.cs" })
            {
                Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(model)!, written)), Is.True, written);
            }

            // A model that cannot be read ends with an exit code, and says why.
            string broken = Path.Combine(folder, "Broken.bld");
            File.WriteAllText(broken, "this is no model");
            output.Clear();
            Assert.That(BoldTools.GenerateClasses(broken, line => { lock (output) { output.Add(line); } }), Is.Not.Zero);
            Assert.That(output, Is.Not.Empty);
        }
        finally
        {
            Environment.SetEnvironmentVariable("BOLD_TOOLS", before);
            Directory.Delete(folder, recursive: true);
        }
    }

    [Test]
    public void A_typenames_file_next_to_the_model_is_given_to_the_generator()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"bold-vs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            string model = Path.Combine(folder, "Shapes.bld");
            File.WriteAllText(model, "");
            Assert.That(BoldTools.GenerateClassesArguments(model), Does.Not.Contain("--typenames"));
            File.WriteAllText(Path.Combine(folder, "Shapes.typenames"), "");
            Assert.That(BoldTools.GenerateClassesArguments(model).Skip(1),
                Is.EqualTo(new[] { model, "--out", folder, "--typenames", Path.Combine(folder, "Shapes.typenames") }));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [TestCase(new[] { "plain" }, "plain")]
    [TestCase(new[] { @"C:\My Models\Library.bld", "--out", @"C:\My Models\" }, @"""C:\My Models\Library.bld"" --out ""C:\My Models\\""")]
    [TestCase(new[] { "say \"hi\"", "" }, @"""say \""hi\"""" """"")]
    public void Arguments_are_quoted_as_a_Windows_program_reads_them(string[] arguments, string line) =>
        Assert.That(BoldTools.Quote(arguments), Is.EqualTo(line));

    [Test]
    public void The_port_browser_is_started_at_the_file_and_line()
    {
        Assert.That(BoldTools.PortBrowserArguments(@"C:\Bold", null, 0), Is.EqualTo(new[] { @"C:\Bold" }));
        Assert.That(BoldTools.PortBrowserArguments(null, @"C:\Bold\CSharp\src\Bold.Core\Common\Core\BoldDefs.cs", 42),
            Is.EqualTo(new[] { "--open", @"C:\Bold\CSharp\src\Bold.Core\Common\Core\BoldDefs.cs", "--line", "42" }));
        Assert.That(BoldTools.PortBrowserArguments(null, "BoldDefs.pas", 0), Is.EqualTo(new[] { "--open", "BoldDefs.pas" }));
        Assert.That(BoldTools.PortBrowserArguments(null, null, 7), Is.Empty);
        Assert.That(BoldTools.IsSource("A.CS") && BoldTools.IsSource(@"x\BoldDefs.pas"), Is.True);
        Assert.That(BoldTools.IsSource("Library.bld") || BoldTools.IsSource(null), Is.False);
    }

    [Test]
    public void A_program_that_is_not_there_is_named()
    {
        string? before = Environment.GetEnvironmentVariable("BOLD_TOOLS");
        try
        {
            Environment.SetEnvironmentVariable("BOLD_TOOLS", Path.Combine(Path.GetTempPath(), "no-bold-tools-here"));
            Assert.That(() => BoldTools.OpenModelEditor(), Throws.InstanceOf<FileNotFoundException>().With.Message.Contains("Bold.ModelEditor.exe"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("BOLD_TOOLS", before);
        }
    }

    [Test]
    public void The_ids_of_the_menus_are_those_of_the_code()
    {
        XNamespace vsct = "http://schemas.microsoft.com/VisualStudio/2005-10-18/CommandTable";
        XDocument menus = XDocument.Load(Extension("BoldPackage.vsct"));
        string code = File.ReadAllText(Extension("PackageIds.cs"));
        string Constant(string name) => Regex.Match(code, $@"const \w+ {name} = ""?(0x[0-9A-Fa-f]+|[0-9a-f-]+)""?;").Groups[1].Value;
        string Guid(string symbol) => menus.Descendants(vsct + "GuidSymbol").Single(s => (string)s.Attribute("name")! == symbol).Attribute("value")!.Value.Trim('{', '}');
        string Id(string symbol) => menus.Descendants(vsct + "IDSymbol").Single(s => (string)s.Attribute("name")! == symbol).Attribute("value")!.Value;

        Assert.That(Guid("guidBoldPackage"), Is.EqualTo(Constant("Package")));
        Assert.That(Guid("guidBoldCmdSet"), Is.EqualTo(Constant("CommandSet")));
        Assert.That(Guid("guidBldSelected"), Is.EqualTo(Constant("BldSelected")));
        Assert.That(Guid("guidSourceSelected"), Is.EqualTo(Constant("SourceSelected")));
        var commands = new Dictionary<string, string>
        {
            ["cmdModelEditor"] = "ModelEditor", ["cmdGenerateClasses"] = "GenerateClasses", ["cmdPropagatorAnalyzer"] = "PropagatorAnalyzer",
            ["cmdUserGuide"] = "UserGuide", ["cmdTutorials"] = "Tutorials", ["cmdOpenModel"] = "OpenModel",
            ["cmdGenerateClassesOfModel"] = "GenerateClassesOfModel", ["cmdPortBrowser"] = "PortBrowser",
            ["cmdShowCounterpart"] = "ShowCounterpart", ["cmdShowCounterpartOfFile"] = "ShowCounterpartOfFile",
        };
        foreach ((string symbol, string constant) in commands)
        {
            Assert.That(Id(symbol), Is.EqualTo(Constant(constant)), symbol);
        }

        // Every button of the menus has a handler in the package, and the other way round.
        string package = File.ReadAllText(Extension("BoldPackage.cs"));
        string[] buttons = [.. menus.Descendants(vsct + "Button").Select(b => (string)b.Attribute("id")!).Order()];
        Assert.That(buttons, Is.EqualTo(commands.Keys.Order()));
        foreach (string constant in commands.Values)
        {
            Assert.That(package, Does.Contain($"Add(commands, PackageIds.{constant},"), constant);
        }

        // The installer removes the extension by the id of its manifest.
        XNamespace manifest = "http://schemas.microsoft.com/developer/vsx-schema/2011";
        string identity = XDocument.Load(Extension("source.extension.vsixmanifest")).Descendants(manifest + "Identity").Single().Attribute("Id")!.Value;
        Assert.That(File.ReadAllText(RepoPaths.Full("CSharp/vs/build.ps1")), Does.Contain($"'{identity}'"));
    }
}
