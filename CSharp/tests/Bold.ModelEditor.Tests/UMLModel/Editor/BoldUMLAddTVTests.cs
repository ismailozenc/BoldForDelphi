// Delphi: Source/UMLModel/Editor/BoldUMLAddTV.pas
// Port map: CSharp/port/units/BoldUMLAddTV.md

namespace Bold.Tests;

[NonParallelizable]
public class BoldUMLAddTVTests
{
    [TearDown]
    public void CloseForms() => EditorForms.CloseAll();

    [Test]
    [Covers(typeof(TfrmBoldUMLAddTV), nameof(TfrmBoldUMLAddTV.ShowAddTVDialog), "FormClose", "FormShow")]
    public void The_add_dialog_gives_the_element_a_tagged_value()
    {
        TBoldUMLObject customer = EditorForms.Customer();
        using (Modal.WhenShown<TfrmBoldUMLAddTV>(dialog =>
        {
            Assert.That(dialog.edTool.Text, Is.EqualTo("Bold"), "the suggested tool");
            dialog.edName.Text = "Hint";
            dialog.edValue.Text = "the buyer";
            dialog.btOK.PerformClick();
        }))
        {
            TfrmBoldUMLAddTV.ShowAddTVDialog("Bold", customer);
        }

        Assert.That(customer.GetTaggedValue("Bold.Hint"), Is.EqualTo("the buyer"));
        Assert.That(TfrmBoldUMLAddTV.TagOf(BoldDefs.DEFAULTNAME, "persistence"), Is.EqualTo("persistence"), "the default tool has no prefix");
        Assert.That(TfrmBoldUMLAddTV.TagOf("Doc", "Author"), Is.EqualTo("Doc.Author"));

        using (Modal.WhenShown<TfrmBoldUMLAddTV>(dialog =>
        {
            dialog.edName.Text = "Ignored";
            dialog.btCancel.PerformClick();
        }))
        {
            TfrmBoldUMLAddTV.ShowAddTVDialog("Bold", customer);
        }

        Assert.That(customer.FindTaggedValue("Bold.Ignored"), Is.Null, "cancelled");
    }
}
