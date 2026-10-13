using System.Text;
using System.Text.RegularExpressions;

namespace Bold.Tests;

/// <summary>The test models in CSharp/tests/Fixtures/Models match their manifest, so later model tests can trust both.</summary>
public partial class FixtureModelsTests
{
    [GeneratedRegex(@"^\s*\(Class\s*$", RegexOptions.Multiline)]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"^\s*\(Association\s*$", RegexOptions.Multiline)]
    private static partial Regex AssociationRegex();

    [Test]
    public void Every_model_file_is_in_the_manifest_and_every_entry_has_a_file()
    {
        var files = FixturePaths.Models.Select(Path.GetFileName).ToList();
        var listed = FixturePaths.Manifest.Select(m => m.File).ToList();
        Assert.That(files, Is.EquivalentTo(listed));
        Assert.That(files, Has.Count.GreaterThanOrEqualTo(39), "master holds at least 39 distinct models");
    }

    [TestCaseSource(nameof(Entries))]
    public void Model_text_matches_its_manifest_entry(FixtureModelInfo entry)
    {
        string text = Encoding.Latin1.GetString(File.ReadAllBytes(FixturePaths.Model(entry.File))).Replace("\r\n", "\n");
        Assert.Multiple(() =>
        {
            Assert.That(text, Does.StartWith($"VERSION {entry.Version}\n"));
            Assert.That(text, Does.Contain($"(Model\n\t\"{entry.Model}\""));
            Assert.That(ClassRegex().Matches(text), Has.Count.EqualTo(entry.Classes));
            Assert.That(AssociationRegex().Matches(text), Has.Count.EqualTo(entry.Associations));
            Assert.That(entry.Sources, Is.Not.Empty);
        });
    }

    [Test]
    public void The_UML_metamodel_is_among_the_models()
    {
        var uml = FixturePaths.Manifest.Single(m => m.Model == "BoldUMLModel");
        Assert.That(uml.Classes, Is.EqualTo(122));
    }

    private static IEnumerable<FixtureModelInfo> Entries() => FixturePaths.Manifest;
}
