// Delphi: Source/UMLModel/Editor/BoldUMLUsesEditorForm.pas
// Port map: CSharp/port/units/BoldUMLUsesEditorForm.md

namespace Bold.Tests;

[NonParallelizable]
public class BoldUMLUsesEditorFormTests
{
    [TearDown]
    public void CloseForms() => EditorForms.CloseAll();

    [Test]
    [Covers(typeof(TfrmUsesEditor), ".ctor", "btnOKClick", "btnCancelClick", "FormClose")]
    public void The_uses_editor_writes_the_units_back()
    {
        TBoldUMLObject model = EditorForms.Customer().model!;
        model.SetTaggedValue("Bold.InterfaceUses", "Dialogs");
        var editor = new TfrmUsesEditor("Interface Uses Editor", model, "Bold.InterfaceUses");
        editor.Show();
        Assert.That((editor.Text, editor.BoldMemo1.Text), Is.EqualTo(("Interface Uses Editor", "Dialogs")));
        editor.BoldMemo1.Text = "Dialogs,SysUtils";
        editor.btnOK.PerformClick();
        Assert.That(model.GetTaggedValue("Bold.InterfaceUses"), Is.EqualTo("Dialogs,SysUtils"));
        Assert.That(editor.IsDisposed, Is.True, "OK closes the editor");

        var cancelled = new TfrmUsesEditor("Interface Uses Editor", model, "Bold.InterfaceUses");
        cancelled.Show();
        cancelled.BoldMemo1.Text = "Forms";
        cancelled.btnCancel.PerformClick();
        Assert.That(model.GetTaggedValue("Bold.InterfaceUses"), Is.EqualTo("Dialogs,SysUtils"), "Cancel changes nothing");
    }
}
