// Delphi: Source/UMLModel/Editor/BoldUMLModelEdit.pas
// Port map: CSharp/port/units/BoldUMLModelEdit.md
using System.ComponentModel;
using System.Drawing;

namespace Bold.Tests;

/// <summary>The model editor: a form for each model component, with the plug-ins, that comes back as it was left.</summary>
[NonParallelizable]
public class BoldUMLModelEditTests
{
    [Test]
    [Covers(typeof(TModelEdit), ".ctor", nameof(TModelEdit.ShowEditFormForBoldModel), nameof(TModelEdit.FindFormForBoldModel), "CreateFormForBoldModel",
        "Notification", "EditForms", nameof(TModelEdit.PlugInList), nameof(TModelEdit.Dispose))]
    public void The_editor_has_a_form_for_each_model_component()
    {
        using var components = new Container();
        TBoldModel building = EditorSamples.ModelComponent(components);
        var shop = new TBoldModel(components);
        var editor = new TModelEdit();
        TBoldModelEditFrm form = editor.ShowEditFormForBoldModel(building);
        Assert.That((form.Visible, form.ModelHandle), Is.EqualTo((true, building)));
        Assert.That(editor.ShowEditFormForBoldModel(building), Is.SameAs(form), "one form for a model component");
        Assert.That(editor.FindFormForBoldModel(building), Is.SameAs(form));
        Assert.That(editor.FindFormForBoldModel(shop), Is.Null);
        Assert.That(editor.PlugInList.Select(p => p.MenuItemName), Does.Contain("Validate OCL in model"));
        Assert.That(form.PlugInList, Is.EquivalentTo(editor.PlugInList));

        TBoldModelEditFrm other = editor.ShowEditFormForBoldModel(shop);
        Assert.That(other, Is.Not.SameAs(form));
        other.Dispose();
        Assert.That(editor.FindFormForBoldModel(shop), Is.Null, "a form that went is no longer the editor's");

        editor.Dispose();
        Assert.That(form.IsDisposed, Is.True, "the editor closes its forms");
    }

    [Test]
    [Covers(typeof(TModelEdit), nameof(TModelEdit.UMLModelEditor), nameof(TModelEdit.UMLModelEditorAssigned))]
    public void There_is_one_model_editor()
    {
        Assert.That(TModelEdit.UMLModelEditor(), Is.SameAs(TModelEdit.UMLModelEditor()));
        Assert.That(TModelEdit.UMLModelEditorAssigned(), Is.True);
    }

    [Test]
    [Covers(typeof(TModelEdit), nameof(TModelEdit.SaveFormsettingsToRegistry), "LoadFormsettingsFromRegistry")]
    public void The_form_comes_back_as_it_was_left()
    {
        using var components = new Container();
        TBoldModel model = EditorSamples.ModelComponent(components);
        using var editor = new TModelEdit();
        TBoldModelEditFrm form = editor.ShowEditFormForBoldModel(model);
        form.Size = new Size(820, 610);
        form.BoldTreeView1.Width = 260;
        form.SetSplitDistance("sbClass", 280);
        form.Dispose(); // keeps its settings
        TBoldModelEditFrm again = editor.ShowEditFormForBoldModel(model);
        Assert.That(again, Is.Not.SameAs(form));
        Assert.That((again.Width, again.Height, again.BoldTreeView1.Width), Is.EqualTo((820, 610, 260)));
        Assert.That(again.SplitDistance("sbClass"), Is.EqualTo(280));
    }
}
