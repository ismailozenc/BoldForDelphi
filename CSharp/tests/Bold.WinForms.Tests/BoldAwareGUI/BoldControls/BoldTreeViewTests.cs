// Delphi: Source/BoldAwareGUI/BoldControls/BoldTreeView.pas, BoldTreeViewConfig.pas, BoldXCVTreeView.pas, BoldDragDropTarget.pas
// Port map: CSharp/port/units/BoldTreeView.md
using System.Drawing;
using System.Windows.Forms;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>The tree views (with their generic configuration and cut, copy and paste) and the drop target, on the Building model.</summary>
[NonParallelizable]
public class BoldTreeViewTests
{
    private sealed class TestXCVTreeView : TBoldXCVTreeView
    {
        public void Send(int msg)
        {
            Message m = Message.Create(Handle, msg, IntPtr.Zero, IntPtr.Zero);
            WndProc(ref m);
        }
    }

    private sealed class TestDropTarget : TBoldDropTarget
    {
        public void Drop() => OnDragDrop(new DragEventArgs(null, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy));
    }

    private static string[] Texts(TreeNodeCollection nodes) => [.. nodes.OfType<TBoldTreeNode>().Select(n => n.Text)];

    /// <summary>Houses at the top (by address), their residents below.</summary>
    private static void ConfigureHouses(TBoldCustomTreeView tree)
    {
        TBoldGenericListPart top = tree.BoldProperties.Parts.Add();
        top.ElementExpression = "Residential_Building.allInstances->orderBy(address)";
        top.InterpretAsList = true;
        top.ControllerExpression = "'House'";
        TBoldNodeDescription house = tree.BoldProperties.NodeDescriptions.Add();
        house.Name = "House";
        house.TextController.Expression = "address";
        TBoldGenericListPart residents = house.ListController.Parts.Add();
        residents.ElementExpression = "residents";
        residents.InterpretAsList = true;
        residents.ControllerExpression = "'Person'";
        residents.Name = "Residents";
        TBoldNodeDescription person = tree.BoldProperties.NodeDescriptions.Add();
        person.Name = "Person";
        person.TextController.Expression = "firstName";
    }

    [Test]
    [Covers(typeof(TBoldCustomTreeView), "*")]
    [Covers(typeof(TBoldTreeView), "*")]
    [Covers(typeof(TBoldTreeNode), "*")]
    public void A_tree_view_shows_its_parts_and_loads_children_when_a_node_expands()
    {
        using var s = new GuiSample();
        TResidential_Building elm = s.NewHouse("Elm Street");
        s.NewHouse("Oak Road");
        TPerson ann = s.NewPerson("Ann");
        TPerson bob = s.NewPerson("Bob");
        ann.Home = elm;
        bob.Home = elm;
        var tree = new TBoldTreeView();
        using var form = new TestForm(tree);
        ConfigureHouses(tree);
        tree.BoldHandle = s.Reference(s.System);
        GuiSample.Display();
        Assert.That(Texts(tree.Nodes), Is.EqualTo(new[] { "Elm Street", "Oak Road" }));

        var elmNode = (TBoldTreeNode)tree.Nodes[0];
        Assert.That(elmNode.Follower!.Element, Is.SameAs(elm));
        Assert.That(elmNode.NodeDescription!.Name, Is.EqualTo("House"));
        Assert.That(elmNode.HasChildren, Is.True, "a node that may have children shows its expand button");
        Assert.That(Texts(elmNode.Nodes), Is.Empty, "the children load when the node expands");

        elmNode.Expand();
        GuiSample.Display();
        Assert.That(Texts(elmNode.Nodes), Is.EqualTo(new[] { "Ann", "Bob" }));
        var annNode = (TBoldTreeNode)elmNode.Nodes[0];
        Assert.That(annNode.NodeDescription!.Name, Is.EqualTo("Person"));
        Assert.That(annNode.Level, Is.EqualTo(1));

        var oakNode = (TBoldTreeNode)tree.Nodes[1];
        oakNode.Expand();
        GuiSample.Display();
        Assert.That(oakNode.Nodes.Count, Is.EqualTo(0), "no residents: the expand button goes");

        ann.FirstName = "Anna";
        s.NewHouse("Ash Lane");
        GuiSample.Display();
        Assert.That(Texts(elmNode.Nodes), Is.EqualTo(new[] { "Anna", "Bob" }));
        Assert.That(Texts(tree.Nodes), Is.EqualTo(new[] { "Ash Lane", "Elm Street", "Oak Road" }));

        tree.Selected = (TBoldTreeNode)elmNode.Nodes[1];
        Assert.That(tree.CurrentElement, Is.SameAs(bob));
        Assert.That(tree.CurrentFollower!.Element, Is.SameAs(bob));
        Assert.That(tree.GetElementAt(-100, -100), Is.Null);

        bob.Home = null;
        GuiSample.Display();
        Assert.That(Texts(elmNode.Nodes), Is.EqualTo(new[] { "Anna" }), "a resident who moves out goes");
    }

    [Test]
    [Covers(typeof(TBoldCustomTreeView), nameof(TBoldCustomTreeView.FillListWithSelectedObjects), nameof(TBoldCustomTreeView.MultiSelect))]
    public void Selected_nodes_give_their_objects()
    {
        using var s = new GuiSample();
        s.NewHouse("Elm Street");
        s.NewHouse("Oak Road");
        var tree = new TBoldTreeView { MultiSelect = true };
        using var form = new TestForm(tree);
        ConfigureHouses(tree);
        tree.BoldHandle = s.Reference(s.System);
        GuiSample.Display();
        ((TBoldTreeNode)tree.Nodes[0]).SetSelected(true);
        ((TBoldTreeNode)tree.Nodes[1]).SetSelected(true);
        var selected = new TBoldObjectList();
        tree.FillListWithSelectedObjects(selected);
        Assert.That(selected.Count, Is.EqualTo(2));
        Assert.That(tree.FindListPartByNames("House", "Residents")!.ElementExpression, Is.EqualTo("residents"));
        Assert.That(tree.FindListPartByNames("Nothing", "Residents"), Is.Null);
    }

    [Test]
    [Covers(typeof(BoldTreeViewConfig), "*")]
    public void The_generic_configuration_has_a_node_per_class_and_per_role()
    {
        using var s = new GuiSample();
        var tree = new TBoldTreeView();
        BoldTreeViewConfig.BoldGenericTreeView(s.SystemHandle.StaticSystemTypeInfo!, tree);
        Assert.That(tree.BoldProperties.Parts.Count, Is.EqualTo(1));
        Assert.That(tree.BoldProperties.NodeDescriptions.FindByName("AttributeNode"), Is.Not.Null);
        TBoldNodeDescription person = tree.BoldProperties.NodeDescriptions.FindByName("Person")!;
        Assert.That(person.TextController.Expression, Is.EqualTo("self.asString + ': ' + oclType.asString"));
        var parts = Enumerable.Range(0, person.ListController.Parts.Count).Select(i => person.ListController.Parts[i]).ToList();
        Assert.That(parts.Select(p => p.ElementExpression), Does.Contain("'FirstName: ' + firstName.asString"));
        Assert.That(parts.Select(p => p.ElementExpression), Does.Contain("home"));
        Assert.That(tree.BoldProperties.NodeDescriptions.FindByName("Person.home")!.TextController.Expression, Is.EqualTo("'Home:'"));
        tree.Dispose();
    }

    [Test]
    [Covers(typeof(TBoldXCVTreeView), "*")]
    public void Cut_copy_and_paste_raise_their_events()
    {
        using var s = new GuiSample();
        var tree = new TestXCVTreeView();
        using var form = new TestForm(tree);
        var raised = new List<string>();
        tree.OnCut = _ => raised.Add("cut");
        tree.OnCopy = _ => raised.Add("copy");
        tree.OnPaste = _ => raised.Add("paste");
        tree.Send(0x0300);
        tree.Send(0x0301);
        tree.Send(0x0302);
        Assert.That(raised, Is.EqualTo(new[] { "cut", "copy", "paste" }));
    }

    [Test]
    [Covers(typeof(TBoldDropTarget), "*")]
    public void A_drop_target_shows_its_object_s_icon_and_takes_a_dropped_object_of_its_type()
    {
        using var s = new GuiSample();
        TPerson ann = s.NewPerson("Ann");
        TPerson bob = s.NewPerson("Bob");
        using var images = new ImageList();
        images.Images.Add(new Bitmap(16, 16));
        images.Images.Add(new Bitmap(16, 16));
        var target = new TestDropTarget { ImageList = images, EmptyImageIndex = 0, IsDropTarget = true };
        using var form = new TestForm(target);
        TBoldNodeDescription person = target.Representations.NodeDescriptions.Add();
        person.Name = "Person";
        person.IconController.Expression = "1";
        TBoldReferenceHandle handle = s.Reference(ann, "Person");
        target.BoldHandle = handle;
        GuiSample.Display();
        Assert.That(target.Element, Is.SameAs(ann));
        Assert.That(target.Image, Is.Not.Null);

        BoldGUI.BoldGUIHandler().DraggedObjects.Clear();
        BoldGUI.BoldGUIHandler().DraggedObjects.Add(bob);
        Assert.That(target.DraggedObject, Is.SameAs(bob));
        target.Drop();
        Assert.That(handle.Value, Is.SameAs(bob));

        BoldGUI.BoldGUIHandler().DraggedObjects.Add(ann);
        Assert.That(target.DraggedObject, Is.Null, "only one object is dropped");
        target.Drop();
        Assert.That(handle.Value, Is.SameAs(bob));
        target.DoEndDrag();
        Assert.That(BoldGUI.BoldGUIHandler().DraggedObjects.Count, Is.EqualTo(0));

        target.DoStartDrag();
        Assert.That(BoldGUI.BoldGUIHandler().DraggedObjects[0], Is.SameAs(bob));
        target.DoEndDrag();

        images.Dispose();
        Assert.That(target.ImageList, Is.Null, "a disposed image list is let go");
    }
}
