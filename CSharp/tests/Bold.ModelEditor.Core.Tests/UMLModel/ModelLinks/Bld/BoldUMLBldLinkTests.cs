// Delphi: Source/UMLModel/ModelLinks/Bld/BoldUMLBldLink.pas
// Port map: CSharp/port/units/BoldUMLBldLink.md
using System.ComponentModel;

namespace Bold.Tests;

public class BoldUMLBldLinkTests
{
    private static string TempFile(string name) =>
        Path.Combine(Path.GetTempPath(), $"bold-editor-{Guid.NewGuid():N}-{name}");

    private static string[] ClassNames(TBoldUMLObject model) => [.. model.Classes.Select(c => c.name).Order()];

    [Test]
    [Covers(typeof(TBoldUMLBldLink), "*")]
    public void The_bld_link_writes_a_UML_model_and_reads_it_back()
    {
        TBoldUMLBldLink.EnsureRegistered();
        TBoldUMLBldLink.EnsureRegistered();
        TBoldUMLModelLinkList links = BoldUMLModelLinkUnit.BoldUMLModelLinkList();
        Assert.That(links.LinkClasses(-1, ".bld")!.LinkType, Is.EqualTo(typeof(TBoldUMLBldLink)));
        Assert.That(links.FileFilter.Split("*.bld|").Length, Is.EqualTo(2), "registered once");
        Assert.That(links.FileFilter, Does.Contain("Bold Proprietary Format (*.bld)|*.bld"));

        TBoldUMLObject uml = EditorSamples.UMLModel();
        string file = TempFile("Building.bld");
        try
        {
            var link = new TBoldUMLBldLink { FileName = file };
            Assert.That(link.CanExport, Is.True);
            Assert.That(link.DisplayName, Is.EqualTo(file));
            Assert.That(link.ExportModel(uml), Is.True);
            Assert.That(File.ReadAllText(file), Does.StartWith("VERSION"));

            TBoldUMLObject read = new TBoldUMLModelGraph().Create("Model");
            EditorSamples.NewElement(read, "Class", "Leftover");
            Assert.That(new TBoldUMLBldLink { FileName = file }.ImportModel(read), Is.True);
            Assert.That(ClassNames(read), Is.EqualTo(ClassNames(uml)), "the model is cleared first");
            Assert.That(read.name, Is.EqualTo(uml.name));
        }
        finally
        {
            File.Delete(file);
        }

        Assert.That(new TBoldUMLBldLink { FileName = file }.ImportModel(new TBoldUMLModelGraph().Create("Model")), Is.False, "a file that is not there");
        Assert.That(() => new TBoldUMLBldLink().ImportModel((TBoldUMLObject)null!), Throws.InstanceOf<EBoldImport>());
    }

    [Test]
    [Covers(typeof(TBoldUMLBldLink), nameof(TBoldUMLBldLink.ImportModel))]
    public void A_model_component_reads_its_model_through_the_link()
    {
        using var components = new Container();
        string file = TempFile("Banking.bld");
        File.Copy(FixturePaths.Model("BankingClasses.Delphi-Simple-ObjectSpace-Transaction.bld"), file);
        try
        {
            var model = new TBoldModel(components);
            var link = new TBoldUMLBldLink(components) { FileName = file, BoldModel = model };
            link.ImportModel();
            Assert.That(model.MoldModel.Classes.ItemsByName("Account"), Is.Not.Null);
            Assert.That(model.EnsuredUMLModel.Classes.Any(c => c.name == "Account"), Is.True);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
