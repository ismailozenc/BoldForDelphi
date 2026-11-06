// Delphi: Source/UMLModel/Editor/BoldUMLConstraintEditor.pas
// Port map: CSharp/port/units/BoldUMLConstraintEditor.md

namespace Bold.Tests;

[NonParallelizable]
public class BoldUMLConstraintEditorTests
{
    [TearDown]
    public void CloseForms() => EditorForms.CloseAll();

    [Test]
    [Covers(typeof(TfrmBoldUMLConstraintEditor), nameof(TfrmBoldUMLConstraintEditor.ShowTheConstraintEditor),
        nameof(TfrmBoldUMLConstraintEditor.EnsureFreeConstraintEditor), "InitElements", "btAddClick", "btRemoveClick", "FormClose")]
    public void The_constraint_editor_adds_and_removes_constraints()
    {
        TBoldUMLObject customer = EditorForms.Customer();
        TfrmBoldUMLConstraintEditor editor = TfrmBoldUMLConstraintEditor.ShowTheConstraintEditor(customer, null);
        Assert.That(editor.Text, Is.EqualTo("Constraints editor - " + customer.qualifiedName));
        editor.btAdd.PerformClick();
        Assert.That(customer.constraint, Has.Count.EqualTo(1));
        TBoldUMLObject constraint = customer.constraint[0];
        Assert.That(editor.BoldGrid1.Rows.Count, Is.EqualTo(1));
        editor.BoldGrid1.Rows[0].Cells[1].Value = "name <> ''";
        Assert.That(constraint.body, Is.EqualTo("name <> ''"));

        editor.BoldGrid1.CurrentCell = editor.BoldGrid1.Rows[0].Cells[0];
        editor.btRemove.PerformClick();
        Assert.That(customer.constraint, Is.Empty);
        Assert.That(TfrmBoldUMLConstraintEditor.ShowTheConstraintEditor(customer, null), Is.SameAs(editor), "one constraint editor");
        editor.Close();
        Assert.That((editor.Visible, editor.IsDisposed), Is.EqualTo((false, false)), "closing hides it");
        TfrmBoldUMLConstraintEditor.EnsureFreeConstraintEditor();
        Assert.That(editor.IsDisposed, Is.True);
    }
}
