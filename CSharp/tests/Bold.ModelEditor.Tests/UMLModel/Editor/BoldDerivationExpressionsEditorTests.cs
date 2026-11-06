// Delphi: Source/UMLModel/Editor/BoldDerivationExpressionsEditor.pas
// Port map: CSharp/port/units/BoldDerivationExpressionsEditor.md

namespace Bold.Tests;

[NonParallelizable]
public class BoldDerivationExpressionsEditorTests
{
    [TearDown]
    public void CloseForms() => EditorForms.CloseAll();

    [Test]
    [Covers(typeof(TfrmBoldDerivationExpressionsEditor), nameof(TfrmBoldDerivationExpressionsEditor.ShowDerivationExpressionsEditor), "FormClose")]
    public void The_derivation_expressions_editor_writes_its_text_when_it_closes()
    {
        TBoldUMLObject customer = EditorForms.Customer();
        TBoldUMLObject expressions = customer.EnsuredTaggedValue("Bold.DerivationExpressions");
        expressions["value"] = "a=b";
        using (Modal.WhenShown<TfrmBoldDerivationExpressionsEditor>(editor =>
        {
            Assert.That(editor.BoldMemo1.Text, Is.EqualTo("a=b"));
            editor.BoldMemo1.Text = "fullName=name";
            editor.btClose.PerformClick();
        }))
        {
            TfrmBoldDerivationExpressionsEditor.ShowDerivationExpressionsEditor(expressions);
        }

        Assert.That(customer.GetTaggedValue("Bold.DerivationExpressions"), Is.EqualTo("fullName=name"));
    }
}
