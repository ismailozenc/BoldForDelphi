// Delphi: Source/MoldModel/UtilsGUI/BoldTypeNameEditor.pas
// Port map: CSharp/port/units/BoldTypeNameEditor.md

namespace Bold.Tests;

/// <summary>Editing the mappings of a type name dictionary.</summary>
[NonParallelizable]
public class BoldTypeNameEditorTests
{
    [Test]
    [Covers(typeof(TBoldTypeNameEditorForm), nameof(TBoldTypeNameEditorForm.LoadFromDictionary), nameof(TBoldTypeNameEditorForm.SaveToDictionary),
        "FormCreate", "btnAddClick", "btnDeleteClick", "btnDownClick", "btnUpClick", "MoveRow", "Copyrow1Click", "MergeDefaultsmappings1Click")]
    public void The_editor_loads_edits_and_saves_the_mappings()
    {
        using var form = new TBoldTypeNameEditorForm();
        BoldAFPDefaultTests.ShowOffScreen(form);
        Assert.That(form.Cells(0, 0), Is.EqualTo("ModelName"));
        Assert.That(form.Cells(12, 0), Is.EqualTo("VI-NativeType"));

        var dictionary = new TBoldTypeNameDictionary(null);
        TBoldTypeNameMapping first = dictionary.AddMapping();
        first.ModelName = "Text";
        first.ExpressionName = "Text";
        first.DelphiName = "TBAString";
        TBoldTypeNameMapping second = dictionary.AddMapping();
        second.ModelName = "Number";
        second.ExpressionName = "Number";
        form.LoadFromDictionary(dictionary);
        Assert.That(form.StringGrid1.RowCount, Is.EqualTo(2));
        Assert.That(form.Cells(2, 1), Is.EqualTo("TBAString"));

        form.btnAdd.PerformClick();
        Assert.That(form.StringGrid1.RowCount, Is.EqualTo(3));
        Assert.That(form.Cells(2, 3), Is.EqualTo("TXX<Name>"));
        Assert.That(form.Cells(4, 3), Is.EqualTo("TBoldPM<Name>"));
        form.SetCells(0, 3, "Money");

        form.btnUp.PerformClick();
        Assert.That(form.Cells(0, 2), Is.EqualTo("Money"), "moved up");
        Assert.That(form.Cells(0, 3), Is.EqualTo("Number"));
        form.btnDown.PerformClick();
        Assert.That(form.Cells(0, 3), Is.EqualTo("Money"), "moved down");

        form.StringGrid1.CurrentCell = form.StringGrid1[0, 0];
        form.Copyrow1.PerformClick();
        Assert.That(form.StringGrid1.RowCount, Is.EqualTo(4));
        Assert.That(form.Cells(2, 4), Is.EqualTo("TBAString"), "the names but the model and expression names are copied");
        Assert.That(form.Cells(0, 4), Is.Empty);

        form.btnDelete.PerformClick();
        Assert.That(form.StringGrid1.RowCount, Is.EqualTo(3));

        var saved = new TBoldTypeNameDictionary(null);
        form.SaveToDictionary(saved);
        Assert.That(saved.Count, Is.EqualTo(3));
        Assert.That(saved.Mapping(2).ModelName, Is.EqualTo("Money"));
        Assert.That(saved.Mapping(2).MapperName, Is.EqualTo("TBoldPM<Name>"));

        form.MergeDefaultsmappings1.PerformClick();
        var merged = new TBoldTypeNameDictionary(null);
        form.SaveToDictionary(merged);
        var defaults = new TBoldTypeNameDictionary(null);
        defaults.AddDefaultMappings();
        Assert.That(merged.Count, Is.GreaterThanOrEqualTo(defaults.Count + 3 - 1));
        Assert.That(merged.ExactMappingForModelName("String"), Is.Not.Null, "the defaults are merged in");
        Assert.That(merged.ExactMappingForModelName("Text")!.DelphiName, Is.EqualTo("TBAString"), "the own mappings stay");
    }
}
