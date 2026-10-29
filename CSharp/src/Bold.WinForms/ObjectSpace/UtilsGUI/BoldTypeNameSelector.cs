// Delphi: Source/ObjectSpace/UtilsGUI/BoldTypeNameSelector.pas
// Port map: CSharp/port/units/BoldTypeNameSelector.md

using System.Drawing;
using System.Windows.Forms;

namespace Bold;

/// <summary>The kinds of nodes in the type name selector (they pick the node's image).</summary>
[Delphi(PortStatus.Ported)]
public enum TNodeType { ntClass, ntClasses, ntAttributes, ntList, ntAttribute, ntClassList, ntAttributeList, ntAttributeLists, ntClassLists, ntRoot, ntSystem, ntType }

/// <summary>
/// Picks a type name of a system from a tree: its classes, lists of classes and attributes, attributes, the system and
/// the type type (those of the approved value types). The node of the current name is selected.
/// </summary>
[Delphi(PortStatus.Ported, Note = "The form's layout is that of the Delphi form file.")]
public class TfrmBoldTypeNameSelector : Form
{
    private TBoldSystemTypeInfo? fSystemTypeInfo;
    public readonly TreeView tvMetaTypes;
    public readonly Panel pnButtons;
    public readonly Button btCancel;
    public readonly Button btOK;
    public readonly ImageList ilImages;
    public readonly ContextMenuStrip PopupMenu1;
    public readonly ToolStripMenuItem mnSort;

    public TfrmBoldTypeNameSelector()
    {
        SuspendLayout();
        Text = "Bold Type Name Selector";
        ClientSize = new Size(336, 317);
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ilImages = new ImageList { ColorDepth = ColorDepth.Depth32Bit, ImageSize = new Size(16, 16) };
        mnSort = new ToolStripMenuItem("Sort sub nodes", null, mnSortClick) { Name = "mnSort" };
        PopupMenu1 = new ContextMenuStrip { ImageList = ilImages };
        PopupMenu1.Items.Add(mnSort);
        tvMetaTypes = new TreeView
        {
            Name = "tvMetaTypes", Dock = DockStyle.Fill, HotTracking = true, ImageList = ilImages, Indent = 19,
            ContextMenuStrip = PopupMenu1, LabelEdit = false, HideSelection = false,
        };
        tvMetaTypes.AfterSelect += (_, e) => tvMetaTypesChange(tvMetaTypes, e.Node);
        tvMetaTypes.DoubleClick += tvMetaTypesDblClick;
        tvMetaTypes.NodeMouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                tvMetaTypes.SelectedNode = e.Node; // RightClickSelect
            }
        };
        btOK = new Button { Name = "btOK", Bounds = new Rectangle(168, 8, 81, 25), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Text = "OK", Enabled = false };
        btOK.Click += btOKClick;
        btCancel = new Button { Name = "btCancel", Bounds = new Rectangle(256, 8, 81, 25), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Text = "Cancel" };
        btCancel.Click += btCancelClick;
        pnButtons = new Panel { Name = "pnButtons", Dock = DockStyle.Bottom, Height = 40 };
        pnButtons.Controls.AddRange([btOK, btCancel]);
        Controls.AddRange([tvMetaTypes, pnButtons]);
        AcceptButton = btOK;
        CancelButton = btCancel;
        ResumeLayout(false);
        FormCreate(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ilImages.Dispose();
            PopupMenu1.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>Shows the types; gives OK and the selected type's name, or Cancel and the name unchanged.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives a modal result (an integer, Delphi's TModalResult).")]
    public int Select(ref string StringValue, TBoldSystemTypeInfo SystemTypeInfo, TBoldValueTypes ApprovedTypes)
    {
        int Result = global::Bold.Controls.mrCancel;
        fSystemTypeInfo = SystemTypeInfo;
        GenerateNodes(SystemTypeInfo, ApprovedTypes, StringValue);
        if (ShowDialog() == DialogResult.OK)
        {
            StringValue = tvMetaTypes.SelectedNode!.Text;
            Result = global::Bold.Controls.mrOk;
        }

        return Result;
    }

    /// <summary>The images of the node types.</summary>
    [Delphi(PortStatus.Ported, Note = "Called by the constructor (Delphi's OnCreate); the bitmaps are embedded in Bold.WinForms from the repository's Images.")]
    private void FormCreate(object? Sender, EventArgs e)
    {
        foreach (string name in new[]
        {
            "SmallUMLModelRoot.bmp", "SmallUMLClass.bmp", "SmallUMLClasses.bmp", "SmallUMLAttribute2.bmp", "SmallUMLAttributes2.bmp",
            "SmallList.bmp", "SmallUMLClassList.bmp", "SmallUMLAttributeList.bmp", "SmallSystem.bmp", "SmallType.bmp",
        })
        {
            ilImages.Images.Add(BoldWinForms.ImageResource(name, Color.White) ?? new Bitmap(16, 16)); // do not localize
        }
    }

    /// <summary>The tree of the approved types under a root, with the node of the current name selected.</summary>
    [Delphi(PortStatus.Ported)]
    private void GenerateNodes(TBoldSystemTypeInfo SystemTypeInfo, TBoldValueTypes ApprovedTypes, string CurrentStringValue)
    {
        TreeNodeCollection Items = tvMetaTypes.Nodes;
        // add the root node
        TreeNode Root = Items.Add("Types in the system"); // do not localize
        SetImageIndex(Root, TNodeType.ntRoot);

        TreeNode Add(TreeNode parent, string text, object? data, TNodeType nodeType)
        {
            TreeNode node = parent.Nodes.Add(text);
            node.Tag = data;
            SetImageIndex(node, nodeType);
            return node;
        }

        if (ApprovedTypes.Contains(TBoldValueTypeSet.bvtClass) && SystemTypeInfo.TopSortedClasses.Count > 0)
        {
            // add the classes root node
            TreeNode Node2 = Add(Root, "Classes", null, TNodeType.ntClasses); // do not localize
            // add the classes
            for (int Index = 0; Index < SystemTypeInfo.TopSortedClasses.Count; Index++)
            {
                TBoldClassTypeInfo classType = SystemTypeInfo.TopSortedClasses[Index];
                SelectCurrentNode(CurrentStringValue, Add(Node2, classType.ExpressionName, classType, TNodeType.ntClass));
            }

            AlphaSort(Node2);
        }

        if (ApprovedTypes.Contains(TBoldValueTypeSet.bvtList))
        {
            // add the list root node
            TreeNode ListRoot = Add(Root, "Lists", null, TNodeType.ntList); // do not localize
            // add the class list root node
            TreeNode Node2 = Add(ListRoot, "Class lists", null, TNodeType.ntClassLists); // do not localize
            // add the class list nodes
            for (int Index = 0; Index < SystemTypeInfo.TopSortedClasses.Count; Index++)
            {
                TBoldListTypeInfo listType = SystemTypeInfo.ListTypeInfoByElement(SystemTypeInfo.TopSortedClasses[Index])!;
                SelectCurrentNode(CurrentStringValue, Add(Node2, listType.ExpressionName, listType, TNodeType.ntClassList));
            }

            AlphaSort(Node2);

            // add the attribute list root node
            Node2 = Add(ListRoot, "Attribute lists", null, TNodeType.ntAttributeLists); // do not localize
            // add the attribute list nodes
            for (int Index = 0; Index < SystemTypeInfo.AttributeTypes.Count; Index++)
            {
                TBoldListTypeInfo listType = SystemTypeInfo.ListTypeInfoByElement(SystemTypeInfo.AttributeTypes[Index])!;
                SelectCurrentNode(CurrentStringValue, Add(Node2, listType.ExpressionName, listType, TNodeType.ntAttributeList));
            }

            AlphaSort(Node2);
        }

        if (ApprovedTypes.Contains(TBoldValueTypeSet.bvtAttr))
        {
            // add the attributes node
            TreeNode Node2 = Add(Root, "Attributes", null, TNodeType.ntAttributes); // do not localize
            for (int Index = 0; Index < SystemTypeInfo.AttributeTypes.Count; Index++)
            {
                // add the attribute nodes
                TBoldElementTypeInfo attributeType = SystemTypeInfo.AttributeTypes[Index];
                SelectCurrentNode(CurrentStringValue, Add(Node2, attributeType.ExpressionName, attributeType, TNodeType.ntAttribute));
            }

            AlphaSort(Node2);
        }

        if (ApprovedTypes.Contains(TBoldValueTypeSet.bvtSystem))
        {
            SelectCurrentNode(CurrentStringValue, Add(Root, SystemTypeInfo.ExpressionName, SystemTypeInfo, TNodeType.ntSystem));
        }

        if (ApprovedTypes.Contains(TBoldValueTypeSet.bvtType))
        {
            SelectCurrentNode(CurrentStringValue, Add(Root, SystemTypeInfo.TypeTypeInfo!.ExpressionName, SystemTypeInfo.TypeTypeInfo, TNodeType.ntType));
        }

        Root.Expand();
    }

    /// <summary>Sorts the node's children by their text (Delphi's TTreeNode.AlphaSort), keeping the selection.</summary>
    private void AlphaSort(TreeNode node)
    {
        TreeNode? selected = tvMetaTypes.SelectedNode;
        TreeNode[] children = [.. node.Nodes.Cast<TreeNode>().OrderBy(child => child.Text, StringComparer.CurrentCultureIgnoreCase)];
        node.Nodes.Clear();
        node.Nodes.AddRange(children);
        if (selected is not null)
        {
            tvMetaTypes.SelectedNode = selected;
        }
    }

    [Delphi(PortStatus.Ported, Note = "The node is passed by value: setting its images needs no var.")]
    private static void SetImageIndex(TreeNode Node, TNodeType NodeType)
    {
        int index = NodeType switch
        {
            TNodeType.ntRoot => 0,
            TNodeType.ntClass => 1,
            TNodeType.ntClasses => 2,
            TNodeType.ntAttribute => 3,
            TNodeType.ntAttributes => 4,
            TNodeType.ntList => 5,
            TNodeType.ntClassList => 6,
            TNodeType.ntAttributeList => 7,
            TNodeType.ntAttributeLists => 7,
            TNodeType.ntClassLists => 6,
            TNodeType.ntSystem => 8,
            TNodeType.ntType => 9,
            _ => Node.ImageIndex,
        };
        Node.ImageIndex = index;
        Node.SelectedImageIndex = index;
    }

    [Delphi(PortStatus.Ported)]
    private void btOKClick(object? Sender, EventArgs e)
    {
        if (btOK.Enabled)
        {
            DialogResult = DialogResult.OK;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void btCancelClick(object? Sender, EventArgs e) => DialogResult = DialogResult.Cancel;

    /// <summary>Only a node of a type can be taken.</summary>
    [Delphi(PortStatus.Ported, Note = "The tree's AfterSelect event (Delphi's OnChange).")]
    private void tvMetaTypesChange(object? Sender, TreeNode? Node) => btOK.Enabled = Node?.Tag is not null;

    [Delphi(PortStatus.Ported)]
    private void tvMetaTypesDblClick(object? Sender, EventArgs e) => btOKClick(null, e);

    [Delphi(PortStatus.Ported)]
    private void SelectCurrentNode(string CurrentStringValue, TreeNode aNode)
    {
        if (string.Equals(aNode.Text, CurrentStringValue, StringComparison.OrdinalIgnoreCase))
        {
            tvMetaTypes.SelectedNode = aNode;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void mnSortClick(object? Sender, EventArgs e)
    {
        if (tvMetaTypes.SelectedNode is { } selected)
        {
            AlphaSort(selected);
        }
    }

    /// <summary>The system whose types are shown.</summary>
    public TBoldSystemTypeInfo? SystemTypeInfo => fSystemTypeInfo;
}
