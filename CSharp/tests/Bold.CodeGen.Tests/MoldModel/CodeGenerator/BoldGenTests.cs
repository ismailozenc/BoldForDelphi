// Delphi: Source/MoldModel/CodeGenerator/BoldGen.pas
// Port map: CSharp/port/units/BoldGen.md
using System.Text.RegularExpressions;

namespace Bold.Tests;

public class BoldGenTests
{
    private string fFolder = "";

    [SetUp]
    public void SetUp() => fFolder = Path.Combine(Path.GetTempPath(), $"bold-gen-{Guid.NewGuid():N}") + Path.DirectorySeparatorChar;

    [TearDown]
    public void TearDown()
    {
        BoldFileHandler.BoldCloseAllFilehandlers();
        if (Directory.Exists(fFolder))
        {
            Directory.Delete(fFolder, true);
        }
    }

    /// <summary>The text without the generation date, with the const setter parameters of later templates removed, and with LF line ends.</summary>
    private static string Comparable(string text)
    {
        text = text.Replace("\r", "", StringComparison.Ordinal).TrimEnd();
        text = string.Join("\n", text.Split('\n').Where(l => !l.StartsWith("(* Generated ", StringComparison.Ordinal)));
        return Regex.Replace(text, @"\(const (NewValue|value):", "($1:");
    }

    private void GenerateDelphi(string fixture)
    {
        (TMoldModel model, TBoldTypeNameDictionary types) = GeneratedCode.Load(fixture);
        Directory.CreateDirectory(fFolder);
        TBoldDelphiTemplateManager.InstallTemplates();
        using var generator = new TBoldGenerator(types) { MoldModel = model, BaseFilePath = fFolder, UseTypedLists = true };
        generator.GenerateBusinessObjectCode();
        generator.EnsureMethodImplementations();
    }

    /// <summary>
    /// The Delphi templates reproduce what Delphi Bold generated for the Building and OrgChart examples (master's
    /// files in tests/Fixtures/Generated), apart from the date line and the const setter parameters of later templates.
    /// </summary>
    [TestCase("BuildingsAndOwners.Building.bld", "Building", "BuildingClasses")]
    [TestCase("OrgChartClasses.bld", "OrgChart", "OrgChartClasses")]
    [Covers(typeof(TBoldGenerator), "*")]
    [Covers(typeof(TBoldDelphiTemplateManager), "*")]
    [Covers(typeof(TBoldGeneratorTemplatesDelphiDM))]
    [Covers(typeof(TBoldGeneratorTemplateManager), "*")]
    [Covers(typeof(BoldGeneratorTemplates), "*")]
    [Covers(typeof(BoldGen), "*")]
    [Covers("BoldGen", "TBoldGeneratorClass", "TBoldCodeGenInitializerClass", "TBoldFunctionContent", "TBoldEnsureFlag", "TMoldClassFilterFunction")]
    [Covers("BoldGeneratorTemplates", "TBoldGeneratorTemplateManagerClass")]
    public void Delphi_templates_reproduce_the_code_Delphi_Bold_generated(string fixture, string folder, string unit)
    {
        GenerateDelphi(fixture);
        foreach (string file in new[] { unit + ".pas", unit + "_Interface.inc" })
        {
            string expected = File.ReadAllText(RepoPaths.Full($"CSharp/tests/Fixtures/Generated/Delphi/{folder}/{file}"), TStrings.DefaultEncoding);
            string actual = File.ReadAllText(fFolder + file, TStrings.DefaultEncoding);
            Assert.That(Comparable(actual), Is.EqualTo(Comparable(expected)), file);
        }
    }

    [Test]
    [Covers(typeof(TBoldGenerator), "EnsureMethodImplementations", "EnsureMethod", "FindInCurrentFile", "InitializeMethodIndex")]
    public void Method_stubs_are_added_once_and_user_code_is_kept()
    {
        GenerateDelphi("BuildingsAndOwners.Building.bld");
        string person = File.ReadAllText(fFolder + "Person.inc");
        Assert.That(person, Does.Contain("procedure TPerson.BorrowFrom(Lender: TPerson; Amount: Integer);"));
        Assert.That(person, Does.Contain("procedure TPerson.CompleteCreate;"));
        File.WriteAllText(fFolder + "Person.inc", person.Replace("procedure TPerson.BorrowFrom(Lender: TPerson; Amount: Integer);\r\nbegin", "procedure TPerson.BorrowFrom(Lender: TPerson; Amount: Integer);\r\nbegin\r\n  // mine", StringComparison.Ordinal));

        GenerateDelphi("BuildingsAndOwners.Building.bld");
        string again = File.ReadAllText(fFolder + "Person.inc");
        Assert.That(Regex.Matches(again, "TPerson.BorrowFrom").Count, Is.EqualTo(1), "an existing method is not added again");
        Assert.That(again, Does.Contain("// mine"), "user code is kept");
        Assert.That(File.ReadAllText(fFolder + "ResidentialBuilding.inc"), Does.Contain("  inherited;"), "an override of a model method calls the inherited one");
    }

    [Test]
    [Covers(typeof(TBoldCPPTemplateManager), "*")]
    [Covers(typeof(TBoldGeneratorTemplateCPPDM))]
    public void Cpp_templates_generate_a_unit_and_header()
    {
        (TMoldModel model, TBoldTypeNameDictionary types) = GeneratedCode.Load("BuildingsAndOwners.Building.bld");
        Directory.CreateDirectory(fFolder);
        TBoldCPPTemplateManager.InstallCPPTemplates();
        using (var generator = new TBoldGenerator(types) { MoldModel = model, BaseFilePath = fFolder, UseTypedLists = true })
        {
            generator.GenerateBusinessObjectCode();
            generator.EnsureMethodImplementations();
        }

        string header = File.ReadAllText(fFolder + "BuildingClasses.hpp");
        Assert.That(header, Does.Contain("class DELPHICLASS TPerson;").Or.Contain("TPerson"));
        Assert.That(File.ReadAllText(fFolder + "BuildingClasses.cpp"), Does.Contain("TPerson::_GetFirstName"));
        // The class's FileName tagged value (Person.inc) names its method file.
        Assert.That(File.ReadAllText(fFolder + "Person.inc"), Does.Contain("void __fastcall TPerson::BorrowFrom(TPerson* Lender, Integer Amount)"));
    }

    [Test]
    [Covers(typeof(TBoldCodeGenInitializer), "*")]
    public void The_initializer_moves_implicit_link_classes_to_their_ends_component()
    {
        (TMoldModel model, _) = GeneratedCode.Load("BuildingsAndOwners.Building.bld");
        model.Classes.ItemsByName("Person")!.SetBoldTV(BoldDefaultTaggedValues.TAG_UNITNAME, "People");
        model.Classes.ItemsByName("Building")!.SetBoldTV(BoldDefaultTaggedValues.TAG_UNITNAME, "People");
        TMoldClass ownership = model.Classes.ItemsByName("Ownership")!;
        Assert.That(ownership.Component.Name, Is.EqualTo("BuildingClasses"), "the implicit link class starts in the main component");
        TBoldDelphiTemplateManager.InstallTemplates();
        var initializer = new TBoldCodeGenInitializer(model);
        initializer.Execute();
        Assert.That(ownership.Component.Name, Is.EqualTo("People"), "both ends are in People, which depends on the main component");
        Assert.That(model.FindComponent("People")!.DependentOf(model.MainComponent), Is.True);
        Assert.That(initializer.ValidateFileNames(), Is.True);
    }
}
