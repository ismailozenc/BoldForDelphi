// Delphi: Source/UMLModel/Plugins/BoldUMLModelEditPlugIn.pas, BoldUMLPlugins.pas, BoldUMLPluginCallBacks.pas
// Port map: CSharp/port/units/BoldUMLModelEditPlugIn.md
using System.ComponentModel;
using System.Drawing;
using static Bold.BoldDefaultTaggedValues;
using static Bold.BoldUMLTaggedValues;

namespace Bold.Tests;

public class BoldUMLModelEditPlugInTests
{
    private static string TempFile(string name) => Path.Combine(Path.GetTempPath(), $"bold-editor-{Guid.NewGuid():N}-{name}");

    [Test]
    [Covers(typeof(BoldUMLPlugInRegistry), "*")]
    [Covers(typeof(TUMLPlugIn), "Create", nameof(TUMLPlugIn.MenuItemName), nameof(TUMLPlugIn.PlugInType), nameof(TUMLPlugIn.Options), nameof(TUMLPlugIn.Instance),
        nameof(TUMLPlugIn.ImageResourceName), nameof(TUMLPlugIn.ImageMaskColor), nameof(TUMLPlugIn.PluginClassName))]
    [Covers(typeof(BoldUMLPlugins), "*")]
    [Covers(typeof(TUMLPlugIn))]
    [Covers(typeof(TPlugInType))]
    [Covers(typeof(TBoldUMLPluginOption))]
    [Covers("BoldUMLModelEditPlugIn", "TBoldUMLPluginOptions")]
    [Covers(typeof(TUMLLinkExporter), "GetMenuItemName", "GetPlugInType", "GetImageResourceName", "GetImageMaskColor", "GetOptions")]
    [Covers(typeof(TUMLLinkImporter), "GetMenuItemName", "GetPlugInType", "GetImageResourceName", "GetImageMaskColor")]
    public void The_built_in_plug_ins_are_in_the_list()
    {
        List<TUMLPlugIn> plugins = BoldUMLPlugInRegistry.PlugIns;
        Assert.That(plugins.Select(p => p.MenuItemName), Is.SupersetOf(new[]
        {
            "Export via Link", "Import via Link", "Generate Code", "Generate Persistence Interfaces", "Generate COM/IDL GUIDs",
            "Generate Database", "Validate database structure", "Validate data integrity in database", "Evolve database",
        }));
        Assert.That(plugins.Select(p => p.MenuItemName), Is.Unique);
        Assert.That(plugins.Select(p => p.MenuItemName), Does.Not.Contain("Update model to latest version"), "Delphi did not install the updater either");

        TUMLPlugIn export = plugins.Single(p => p.MenuItemName == "Export via Link");
        Assert.That(export.PlugInType, Is.EqualTo(TPlugInType.ptFile));
        Assert.That(export.Options, Is.EqualTo(TBoldUMLPluginOptions.Of(TBoldUMLPluginOption.poRequireUnBoldified)));
        Assert.That(export.ImageResourceName, Is.EqualTo("UMLPluginExportImage"));
        Assert.That(export.ImageMaskColor, Is.EqualTo(Color.Teal));
        Assert.That(export.PluginClassName, Is.EqualTo(nameof(TUMLLinkExporter)));
        Assert.That(export.Instance, Is.SameAs(typeof(TUMLLinkExporter).Assembly));
        Assert.That(export.ToString(), Is.EqualTo("Export via Link"));
        TUMLPlugIn import = plugins.Single(p => p.MenuItemName == "Import via Link");
        Assert.That((import.PlugInType, import.ImageResourceName, import.Options.IsEmpty), Is.EqualTo((TPlugInType.ptFile, "UMLPluginImportImage", true)));
    }

    [Test]
    [Covers(typeof(TUMLPlugInFunction), "*")]
    [Covers(typeof(TUMLModelUpdaterPlugIn), "*")]
    public void A_plug_in_function_puts_itself_in_the_list_and_takes_itself_out()
    {
        int Count() => BoldUMLPlugInRegistry.PlugIns.Count(p => p.MenuItemName == "Update model to latest version");
        int before = Count();
        using (var updater = new TUMLModelUpdaterPlugIn(true))
        {
            Assert.That(Count(), Is.EqualTo(before + 1));
            Assert.That(updater.PlugIn!.PlugInType, Is.EqualTo(TPlugInType.ptTool));
            Assert.That(updater.ImageResourceName, Is.EqualTo("UMLPluginModelUpdater"));
            Assert.That(updater.ImageMaskColor, Is.EqualTo(Color.Teal));
            Assert.That(updater.Options.IsEmpty, Is.True);
            Assert.That(updater.Instance, Is.SameAs(typeof(TUMLModelUpdaterPlugIn).Assembly));

            using var components = new Container();
            TBoldModel model = EditorSamples.ModelComponent(components);
            TBoldUMLObject attribute = model.EnsuredUMLModel.Graph.AllInstances("Attribute").First();
            attribute.SetBoldTV(TV_PERSISTENT_OLD, "False");
            updater.PlugIn.GuardedExecute(new TestPlugInContext(model));
            Assert.That(attribute.GetTaggedValue(TAG_PERSISTENCE), Is.EqualTo(TV_PERSISTENCE_TRANSIENT));
        }

        Assert.That(Count(), Is.EqualTo(before));
        using var notInstalled = new TUMLModelUpdaterPlugIn(false);
        Assert.That(notInstalled.PlugIn, Is.Null);
        Assert.That(Count(), Is.EqualTo(before));
    }

    [Test]
    [Covers(typeof(TUMLPlugIn), nameof(TUMLPlugIn.GuardedExecute), nameof(TUMLPlugIn.UnGuardedExecute), "OnExecute")]
    [Covers(typeof(IUMLModelPlugInContext), "*")]
    [Covers(typeof(TUMLPluginExecuteEvent))]
    public void A_plug_in_does_not_run_while_another_runs()
    {
        using var components = new Container();
        var model = new TBoldModel(components);
        IUMLModelPlugInContext context = new TestPlugInContext(model);
        int runs = 0;
        bool fail = false;
        var plugIn = new TUMLPlugIn("Test", c =>
        {
            runs++;
            Assert.That(c.IsExecutingPlugin, Is.True);
            if (fail)
            {
                throw new EBold("failed");
            }
        }, TPlugInType.ptTool, null, "", Color.Teal, TBoldUMLPluginOptions.Of(), "Test");
        plugIn.GuardedExecute(context);
        Assert.That((runs, context.IsExecutingPlugin), Is.EqualTo((1, false)));

        context.IsExecutingPlugin = true;
        using (var answers = new DialogAnswers(Controls.mrOk))
        {
            plugIn.GuardedExecute(context);
            Assert.That(answers.Messages, Is.EqualTo(new[] { "Already executing a plugin" }));
        }

        Assert.That(runs, Is.EqualTo(1));
        context.IsExecutingPlugin = false;
        fail = true;
        Assert.That(() => plugIn.GuardedExecute(context), Throws.InstanceOf<EBold>());
        Assert.That(context.IsExecutingPlugin, Is.False, "a plug-in that fails is done too");
        Assert.That(context.GetCurrentModelHandle(), Is.SameAs(model));
        Assert.That(context.GetCurrentElement(), Is.Null);
    }

    [Test]
    [Covers(typeof(TUMLLinkImporterExporter), "*")]
    [Covers(typeof(TUMLLinkImporter), "*")]
    [Covers(typeof(TUMLLinkExporter), "*")]
    [Covers(typeof(TUMLLinkExporter), nameof(TUMLLinkExporter.Execute))]
    public void The_model_is_read_and_written_through_its_link()
    {
        using var components = new Container();
        var model = new TBoldModel(components);
        var context = new TestPlugInContext(model);
        var importer = new TUMLLinkImporter(false);
        var exporter = new TUMLLinkExporter(false);
        using (new DialogAnswers(Controls.mrYes))
        {
            Assert.That(() => importer.Execute(context), Throws.InstanceOf<EBold>().With.Message.Contains("There is no link component"));
        }

        string source = TempFile("Banking.bld"), target = TempFile("Out.bld");
        File.Copy(FixturePaths.Model("BankingClasses.Delphi-Simple-ObjectSpace-Transaction.bld"), source);
        try
        {
            var link = new TBoldUMLBldLink(components) { FileName = source, BoldModel = model };
            using (new DialogAnswers(Controls.mrNo))
            {
                importer.Execute(context);
            }

            Assert.That(model.EnsuredUMLModel.Classes.Select(c => c.name), Does.Not.Contain("Account"), "the user said no");
            using (new DialogAnswers(Controls.mrYes))
            {
                importer.Execute(context);
                Assert.That(model.MoldModel.Classes.ItemsByName("Account"), Is.Not.Null);
                link.FileName = target;
                exporter.Execute(context);
            }

            TMoldModel written = TMoldBLDRW.ModelFromFile(target);
            Assert.That(written.Classes.ItemsByName("Account"), Is.Not.Null);
        }
        finally
        {
            File.Delete(source);
            File.Delete(target);
        }
    }

    [Test]
    [Covers(typeof(TBoldUMLModelValidatorCallBack), "*")]
    [Covers(typeof(TUMLOCLValidatorCallBack), "*")]
    public void The_validation_form_validates_through_call_backs()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        TBoldUMLModelValidatorCallBack.Validate(model);
        Assert.That(EditorSamples.Violations(model.EnsuredUMLModel, TSeverity.sError), Is.Empty);
        TUMLOCLValidatorCallBack.Validate(model);
        Assert.That(EditorSamples.Violations(model.EnsuredUMLModel, TSeverity.sError), Is.Empty);

        TBoldUMLObject person = model.EnsuredUMLModel.Classes.Single(c => c.name == "Person");
        person.SetBoldTV(TAG_DEFAULTSTRINGREPRESENTATION, "noSuchMember");
        TUMLOCLValidatorCallBack.Validate(model);
        Assert.That(EditorSamples.Violations(model.EnsuredUMLModel, TSeverity.sError).Single(), Does.StartWith("Invalid DefaultStringRepresentation: "));
        EditorSamples.NewElement(model.EnsuredUMLModel, "Class", "Person");
        TBoldUMLModelValidatorCallBack.Validate(model);
        Assert.That(EditorSamples.Violations(model.EnsuredUMLModel, TSeverity.sError), Does.Contain("Duplicate class name \"Person\""));
    }
}
