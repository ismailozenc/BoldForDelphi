// Delphi: Source/ObjectSpace/UtilsGUI/BoldTypeNameSelector.pas
// Port map: CSharp/port/units/BoldTypeNameSelector.md
using System.Windows.Forms;

namespace Bold.Tests;

/// <summary>Picking a type name of the Building model.</summary>
[NonParallelizable]
public class BoldTypeNameSelectorTests
{
    private static string[] Texts(TreeNodeCollection nodes) => [.. nodes.Cast<TreeNode>().Select(n => n.Text)];

    [Test]
    [Covers(typeof(TfrmBoldTypeNameSelector), "*")]
    [Covers(typeof(TNodeType))]
    public void The_selector_shows_the_approved_types_and_gives_the_one_picked()
    {
        using var s = new GuiSample();
        TBoldSystemTypeInfo types = s.SystemHandle.StaticSystemTypeInfo!;
        string[] roots = [];
        string[] classes = [];
        bool okBeforePick = true;
        string selectedBefore = "";
        using var form = new TfrmBoldTypeNameSelector();
        using (Modal.WhenShown<TfrmBoldTypeNameSelector>(f =>
        {
            TreeNode root = f.tvMetaTypes.Nodes[0];
            roots = Texts(root.Nodes);
            classes = Texts(root.Nodes[0].Nodes);
            selectedBefore = f.tvMetaTypes.SelectedNode?.Text ?? "";
            f.tvMetaTypes.SelectedNode = root.Nodes[0];
            okBeforePick = f.btOK.Enabled;
            f.tvMetaTypes.SelectedNode = root.Nodes[0].Nodes.Cast<TreeNode>().Single(n => n.Text == "Building");
            Assert.That(f.SystemTypeInfo, Is.SameAs(types));
            f.mnSort.PerformClick();
            Modal.Raise(f.tvMetaTypes, "OnDoubleClick", EventArgs.Empty);
        }))
        {
            string name = "Person";
            int result = form.Select(ref name, types, TBoldValueTypes.Of(TBoldValueTypeSet.bvtClass, TBoldValueTypeSet.bvtList, TBoldValueTypeSet.bvtAttr, TBoldValueTypeSet.bvtSystem, TBoldValueTypeSet.bvtType));
            Assert.That(result, Is.EqualTo(Controls.mrOk));
            Assert.That(name, Is.EqualTo("Building"));
        }

        Assert.That(roots, Is.EqualTo(new[] { "Classes", "Lists", "Attributes", types.ExpressionName, types.TypeTypeInfo!.ExpressionName }));
        Assert.That(classes, Is.Ordered.Using((IComparer<string>)StringComparer.CurrentCultureIgnoreCase), "the classes are sorted");
        Assert.That(classes, Does.Contain("Person").And.Contain("Residential_Building"));
        Assert.That(selectedBefore, Is.EqualTo("Person"), "the current name's node is selected");
        Assert.That(okBeforePick, Is.False, "a group is no type");

        using var second = new TfrmBoldTypeNameSelector();
        using (Modal.WhenShown<TfrmBoldTypeNameSelector>(f =>
        {
            Assert.That(Texts(f.tvMetaTypes.Nodes[0].Nodes), Is.EqualTo(new[] { "Classes" }));
            f.btCancel.PerformClick();
        }))
        {
            string name = "Person";
            Assert.That(second.Select(ref name, types, TBoldValueTypes.Of(TBoldValueTypeSet.bvtClass)), Is.EqualTo(Controls.mrCancel));
            Assert.That(name, Is.EqualTo("Person"));
        }
    }
}
