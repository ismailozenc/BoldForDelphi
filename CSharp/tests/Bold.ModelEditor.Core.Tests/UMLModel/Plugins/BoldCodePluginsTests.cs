// Delphi: Source/UMLModel/Plugins/BoldCodePlugins.pas
// Port map: CSharp/port/units/BoldCodePlugins.md
using System.ComponentModel;
using System.Drawing;
using static Bold.BoldDefaultTaggedValues;

namespace Bold.Tests;

public class BoldCodePluginsTests
{
    private static string NewFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"bold-editor-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return folder;
    }

    [Test]
    [Covers(typeof(TUMLCodeGenerator), "*")]
    [Covers(typeof(TUMLAbstractCodeGenerator), "*")]
    public void Code_is_generated_next_to_the_model_s_file()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        string folder = NewFolder();
        try
        {
            model.FileName = Path.Combine(folder, "Building.bld");
            var generator = new TUMLCodeGenerator(false);
            Assert.That(generator.MenuItemName, Is.EqualTo("Generate Code"));
            Assert.That(generator.PlugInType, Is.EqualTo(TPlugInType.ptTool));
            Assert.That(generator.ImageResourceName, Is.EqualTo("UMLPluginGenCodeImage"));
            Assert.That(generator.ImageMaskColor, Is.EqualTo(Color.Teal));
            Assert.That(generator.Options, Is.EqualTo(TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified)));
            generator.Execute(new TestPlugInContext(model));
            Assert.That(File.Exists(Path.Combine(folder, "Person.g.cs")), Is.True);
            Assert.That(File.Exists(Path.Combine(folder, "Building.g.cs")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(folder, "Person.g.cs")), Does.Contain("public partial class TPerson : TBusinessClassesRoot"));

            EditorSamples.NewElement(model.EnsuredUMLModel, "Class", "Person");
            Assert.That(() => generator.Execute(new TestPlugInContext(model)), Throws.InstanceOf<EBold>().With.Message.EqualTo("Errors in model, can not generate code"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Test]
    [Covers(typeof(TUMLPersistenceInterfaceGenerator), "*")]
    public void The_persistence_interfaces_are_generated_with_Delphi_s_templates()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        string folder = NewFolder();
        try
        {
            model.FileName = Path.Combine(folder, "Building.bld");
            var generator = new TUMLPersistenceInterfaceGenerator(false);
            Assert.That(generator.MenuItemName, Is.EqualTo("Generate Persistence Interfaces"));
            Assert.That(generator.ImageResourceName, Is.EqualTo("UMLPluginGenPersistenceInterfaceImage"));
            generator.Execute(new TestPlugInContext(model));
            Assert.That(Directory.GetFiles(folder, "*_PersistenceInterfaces.pas"), Is.Not.Empty);

            new TUMLCodeGenerator(false).Execute(new TestPlugInContext(model));
            Assert.That(File.Exists(Path.Combine(folder, "Person.g.cs")), Is.True, "the C# templates come back for the code");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Test]
    [Covers(typeof(TUMLGUIDGenerator), "*")]
    [Covers(typeof(BoldCodePlugins), "*")]
    public void GUIDs_are_given_to_the_model_and_its_classes()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        TBoldUMLObject uml = model.EnsuredUMLModel;
        var generator = new TUMLGUIDGenerator(false);
        Assert.That((generator.MenuItemName, generator.ImageResourceName), Is.EqualTo(("Generate COM/IDL GUIDs", "UMLPluginGenGUIDs")));
        Assert.That(generator.Options, Is.EqualTo(TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireBoldified)));
        TBoldUMLObject person = uml.Classes.Single(c => c.name == "Person");
        person.SetBoldTV(TAG_GUID, "{KEEP}");
        using (new DialogAnswers(Controls.mrYes))
        {
            generator.Execute(new TestPlugInContext(model));
        }

        Assert.That(person.GetBoldTV(TAG_GUID), Is.EqualTo("{KEEP}"), "existing GUIDs are kept when the user says so");
        Assert.That(uml.GetBoldTV(TAG_GUID), Does.Match(@"^[0-9A-F-]{36}$"));
        Assert.That(uml.Classes.All(c => c.GetBoldTV(TAG_LISTGUID) != ""), Is.True);
        Assert.That(uml.Classes.Select(c => c.GetBoldTV(TAG_LISTGUID)), Is.Unique);
        using (new DialogAnswers(Controls.mrNo))
        {
            generator.Execute(new TestPlugInContext(model));
        }

        Assert.That(person.GetBoldTV(TAG_GUID), Is.Not.EqualTo("{KEEP}"));
        Assert.That(BoldUMLPlugInRegistry.PlugIns.Count(p => p.PluginClassName == nameof(TUMLGUIDGenerator)), Is.EqualTo(1));
    }
}
