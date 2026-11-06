// Delphi: Source/UMLModel/Plugins/BoldModelOCLValidatorPlugIn.pas
// Port map: CSharp/port/units/BoldModelOCLValidatorPlugIn.md
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using static Bold.BoldDefaultTaggedValues;

namespace Bold.Tests;

[NonParallelizable]
public class BoldModelOCLValidatorPlugInTests
{
    [TearDown]
    public void CloseForms() => EditorForms.CloseAll();

    [Test]
    [Covers(typeof(TUMLOCLValidator), nameof(TUMLOCLValidator.Initialize), nameof(TUMLOCLValidator.Execute), "GetMenuItemName", "GetImageResourceName",
        "GetImageMaskColor", "GetOptions", "GetPlugInType")]
    public void The_validator_shows_the_invalid_OCL_of_the_model()
    {
        TUMLOCLValidator.Initialize();
        TUMLPlugIn plugIn = BoldUMLPlugInRegistry.PlugIns.Single(p => p.PluginClassName == nameof(TUMLOCLValidator));
        Assert.That((plugIn.MenuItemName, plugIn.PlugInType, plugIn.ImageResourceName, plugIn.ImageMaskColor),
            Is.EqualTo(("Validate OCL in model", TPlugInType.ptTool, nameof(TUMLOCLValidator), Color.Teal)));
        Assert.That(plugIn.Options.Contains(TBoldUMLPluginOption.poRequireBoldified), Is.True);
        Assert.That(TUMLPlugInImages.ImageOf(plugIn), Is.Not.Null, "the plug-in's bitmap");

        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        model.EnsuredUMLModel.Classes.Single(c => c.name == "Person").SetBoldTV(TAG_DEFAULTSTRINGREPRESENTATION, "42");
        TfrmValidation.G_ValidationFormDefaultOwner = null;
        plugIn.UnGuardedExecute(new TestPlugInContext(model));
        TfrmValidation form = Application.OpenForms.OfType<TfrmValidation>().Single();
        Assert.That(form.Visible, Is.True);
        Assert.That(form.Violations.Select(v => v.Description), Does.Contain("Invalid DefaultStringRepresentation:  Type mismatch: Expected String got Integer"));
    }
}
