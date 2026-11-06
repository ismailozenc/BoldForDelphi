// Delphi: Source/UMLModel/Editor/BoldUMLTaggedValuesEditor.pas
// Port map: CSharp/port/units/BoldUMLTaggedValuesEditor.md
using System.Windows.Forms;

namespace Bold.Tests;

[NonParallelizable]
public class BoldUMLTaggedValuesEditorTests
{
    [TearDown]
    public void CloseForms() => EditorForms.CloseAll();

    [Test]
    [Covers(typeof(TfrmBoldUMLTaggedValuesEditor), nameof(TfrmBoldUMLTaggedValuesEditor.ShowTaggedValuesEditor), "CreateTabs", "tcToolsChange",
        nameof(TfrmBoldUMLTaggedValuesEditor.RefreshGUI), "btAddTVClick", "btDeleteTVClick", nameof(TfrmBoldUMLTaggedValuesEditor.GetToolName),
        nameof(TfrmBoldUMLTaggedValuesEditor.GetTagName), nameof(TfrmBoldUMLTaggedValuesEditor.bdhAllToolsDeriveAndSubscribe),
        nameof(TfrmBoldUMLTaggedValuesEditor.bdhSelectedTVsDeriveAndSubscribe), nameof(TfrmBoldUMLTaggedValuesEditor.BoldAsStringRenderer1GetAsString))]
    public void The_tagged_values_editor_shows_a_tab_for_each_tool()
    {
        TBoldUMLObject customer = EditorForms.Customer();
        customer.SetTaggedValue("Doc.Author", "me");
        TfrmBoldUMLTaggedValuesEditor editor = TfrmBoldUMLTaggedValuesEditor.ShowTaggedValuesEditor(customer);
        string[] tools = [.. editor.tcTools.TabPages.Cast<TabPage>().Select(p => p.Text)];
        Assert.That(tools, Is.SupersetOf(new[] { "Bold", "Doc" }));
        Assert.That(editor.Text, Is.EqualTo("Tagged values for the class Customer."));
        editor.tcTools.SelectedIndex = Array.IndexOf(tools, "Doc");
        Assert.That(editor.lbTagName.Items.Cast<object>().Select(i => i.ToString()), Is.EqualTo(new[] { "Author" }));
        Assert.That(editor.bmValue.Text, Is.EqualTo("me"));

        editor.Deletetaggedvalue1.PerformClick();
        Assert.That(customer.FindTaggedValue("Doc.Author"), Is.Null);
        using (Modal.WhenShown<TfrmBoldUMLAddTV>(dialog =>
        {
            dialog.edTool.Text = "Doc";
            dialog.edName.Text = "Reviewer";
            dialog.edValue.Text = "you";
            dialog.btOK.PerformClick();
        }))
        {
            editor.Addtaggedvalue1.PerformClick();
        }

        Assert.That(customer.GetTaggedValue("Doc.Reviewer"), Is.EqualTo("you"));
        Assert.That(editor.tcTools.TabPages.Cast<TabPage>().Select(p => p.Text), Does.Contain("Doc"));
    }
}
