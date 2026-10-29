// Delphi: Source/BoldAwareGUI/BoldControls/BoldTreeView.pas
// Port map: CSharp/port/units/BoldTreeView.md

using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Bold.BoldControlPackDefs;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldTreeView", Symbol = "TBoldTreeNode.GetTreeView", Note = "The TreeView property reads the node's tree view.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldTreeView", Symbol = "TBoldCustomTreeView.GetSelected", Note = "The Selected property reads the tree view's selected node.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldTreeView", Symbol = "TBoldCustomTreeView.SetSelected", Note = "The Selected property sets the tree view's selected node.")]

namespace Bold;

/// <summary>
/// A node of a Bold tree view: it shows a node follower (an element through its node description's controllers). A
/// node that may have children it has not loaded shows the expand button (HasChildren).
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms TreeNode.")]
public class TBoldTreeNode : TreeNode
{
    private TBoldFollower? FFollower;
    private TBoldNodeDescription? FNodeDescription;

    /// <summary>The child that stands in for children not loaded yet (so the node shows its expand button).</summary>
    internal sealed class TBoldPlaceholderNode : TreeNode
    {
    }

    /// <summary>The node description whose node controller is the follower's (remembered).</summary>
    [Delphi(PortStatus.Ported, Note = "Null when no description has the controller (Delphi read past the end).")]
    private TBoldNodeDescription? GetNodeDescription()
    {
        // Check if cache is accurate.
        if (FNodeDescription is not null && FNodeDescription.NodeFollowerController == Follower?.Controller)
        {
            return FNodeDescription;
        }

        FNodeDescription = null;
        if (TreeView is { } tree && Follower is not null)
        {
            TBoldNodeDescriptions descriptions = tree.BoldProperties.NodeDescriptions;
            for (int I = 0; I < descriptions.Count; I++)
            {
                if (descriptions[I].NodeFollowerController == Follower.Controller)
                {
                    FNodeDescription = descriptions[I];
                    break;
                }
            }
        }

        return FNodeDescription;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetHideNodeIfNoVisibleChildren() => ((TBoldNodeFollowerController)Follower!.Controller!).HideNodeWithNoChildren;

    /// <summary>Selects or unselects the node in Bold (the node and its icon follower), and shows its icon again.</summary>
    [Delphi(PortStatus.Ported)]
    internal void SetSelected(bool Value)
    {
        if (Follower is null)
        {
            return;
        }

        Follower.Selected = Value;
        if (Follower.SubFollowers(1) is { } icon)
        {
            icon.Selected = Value;
        }

        UpdateIcon();
    }

    [Delphi(PortStatus.Ported)]
    internal void UpdateIcon() => Follower?.SubFollowers(1)?.MarkValueOutOfDate();

    /// <summary>The node leaves its tree view: the tree view forgets it.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldTreeNode.Destroy", Note = "Runs when the tree view removes the node.")]
    internal void Destroying(TBoldCustomTreeView Tree)
    {
        if (Tree.fLastSelectedNode == this)
        {
            Tree.fLastSelectedNode = null;
        }

        if (Tree.fSelectAnchor == this)
        {
            Tree.fSelectAnchor = null;
        }

        if (Tree.fPopupNode == this)
        {
            Tree.fPopupNode = null;
        }
    }

    /// <summary>The element is already shown by a node above (the tree would repeat itself below it).</summary>
    [Delphi(PortStatus.Ported)]
    public bool ExistsInParent() => Follower?.ExistInOwner() ?? false;

    /// <summary>The node shows its expand button: it has children, or stands for children not loaded yet.</summary>
    public bool HasChildren
    {
        get => Nodes.Count > 0;
        set
        {
            if (value && Nodes.Count == 0)
            {
                Nodes.Add(new TBoldPlaceholderNode());
            }
            else if (!value)
            {
                RemovePlaceholder();
            }
        }
    }

    internal void RemovePlaceholder()
    {
        for (int i = Nodes.Count - 1; i >= 0; i--)
        {
            if (Nodes[i] is TBoldPlaceholderNode)
            {
                Nodes.RemoveAt(i);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFollower? Follower
    {
        get => FFollower;
        set => FFollower = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldNodeDescription? NodeDescription => GetNodeDescription();

    [Delphi(PortStatus.Ported)]
    public bool HideNodeIfNoVisibleChildren => GetHideNodeIfNoVisibleChildren();

    /// <summary>The Bold tree view of the node, or null.</summary>
    [Delphi(PortStatus.Ported, Note = "A TBoldCustomTreeView, where Delphi cast to TBoldTreeView.")]
    public new TBoldCustomTreeView? TreeView => base.TreeView as TBoldCustomTreeView;
}

/// <summary>
/// A tree view that shows a handle's value as a tree: the tree controller's parts give the top nodes, and each node's
/// description gives its text, icon and children (loaded when the node is shown or expanded, as NodeExpansion says).
/// Several nodes can be selected (MultiSelect); a dragged node drags its element (or the selected objects).
/// </summary>
[Delphi(PortStatus.Ported, Note = "A WinForms TreeView.")]
public class TBoldCustomTreeView : TreeView, IBoldValidateableComponent
{
    private const int WM_CHAR = 0x0102;
    private const int WM_CONTEXTMENU = 0x007B;
    private const int WM_NCDESTROY = 0x0082;
    private const int TVM_GETEDITCONTROL = 0x1100 + 15;

    private TBoldElementHandleFollower? fHandleFollower;
    private TBoldTreeFollowerController? FTreeController;
    private int FMaxLevels;
    private int FAutoExpandLevels;
    private bool FSelectInserted;
    private int FSelectedIndexDelta;
    private int fSelectedImageIndex;
    private int fStateImageSelected;
    private int fStateImageUnselected;
    private TBoldNodeExpansionMethod FNodeExpansion;
    private TBoldFollower? FDragFollower;
    private TBoldFollower? FEditFollower;
    private int FUpdateCount;
    private bool fMultiSelect;
    internal TBoldTreeNode? fSelectAnchor;
    internal TBoldTreeNode? fLastSelectedNode;
    internal TBoldTreeNode? fPopupNode;
    private Keys fMouseDownShiftState;
    private TBoldTreeNode? fMouseDownNode;
    private Point fMouseDownPos;
    private TBoldTreeNode? fSelectedNodePreUpdate;
    private TBoldElement? fSelectedElementPreUpdate;
    private TBoldNodeDescription? fSelectedNodeDescriptionPreUpdate;
    private bool fNodesHaveBeenDeleted;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>The label editor's window: its characters go through the tree view's KeyPress, as Delphi's tree view passed them.</summary>
    private sealed class TBoldLabelEditWindow : NativeWindow
    {
        private readonly TBoldCustomTreeView fTree;

        public TBoldLabelEditWindow(TBoldCustomTreeView tree, IntPtr handle)
        {
            fTree = tree;
            AssignHandle(handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CHAR)
            {
                var e = new KeyPressEventArgs((char)(long)m.WParam);
                fTree.EditorKeyPress(e);
                if (e.Handled)
                {
                    return;
                }
            }

            base.WndProc(ref m);
            if (m.Msg == WM_NCDESTROY)
            {
                ReleaseHandle();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldCustomTreeView()
    {
        FTreeController = (TBoldTreeFollowerController)Activator.CreateInstance(TreeFollowerControllerClass(), this)!;
        FTreeController.OnAfterInsertItem = InsertNode;
        FTreeController.OnBeforeDeleteItem = DeleteNode;
        FTreeController.OnIconChanged = DisplayIcon;
        FTreeController.OnTextChanged = DisplayText;
        FTreeController.OnGetContextType = GetContextType;
        FTreeController.AfterMakeUptoDate = AfterMakeUptoDate;
        FTreeController.BeforeMakeUptoDate = BeforeMakeUptoDate;
        fHandleFollower = new TBoldElementHandleFollower(this, FTreeController);
        FMaxLevels = -1;
        FAutoExpandLevels = -1;
        FNodeExpansion = TBoldNodeExpansionMethod.neDemand;
        fSelectedImageIndex = -1;
        fStateImageSelected = -1;
        fStateImageUnselected = -1;
        LabelEdit = true;
        AllowDrop = true;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SysUtils.FreeAndNil(ref fHandleFollower);
            SysUtils.FreeAndNil(ref FTreeController);
        }

        base.Dispose(disposing);
    }

    protected override void OnParentChanged(EventArgs e)
    {
        base.OnParentChanged(e);
        if (fHandleFollower is not null)
        {
            object match = (object?)FindForm() ?? this;
            fHandleFollower.MatchObject = match;
            fHandleFollower.Follower.MatchObject = match;
        }
    }

    /// <summary>The tree's nodes in their order from the top (Delphi's Items), without the stand-ins for unloaded children.</summary>
    internal List<TBoldTreeNode> AllNodes()
    {
        var result = new List<TBoldTreeNode>();
        void Walk(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                if (node is TBoldTreeNode boldNode)
                {
                    result.Add(boldNode);
                    Walk(node.Nodes);
                }
            }
        }

        Walk(Nodes);
        return result;
    }

    /// <summary>All the node's parents are expanded (Delphi's TTreeNode.IsVisible).</summary>
    private static bool IsShown(TreeNode node)
    {
        for (TreeNode? parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            if (!parent.IsExpanded)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The node (with its children) leaves the tree: its followers forget it, and the tree view too.</summary>
    private void RemoveNode(TBoldTreeNode node)
    {
        void Forget(TBoldTreeNode n)
        {
            foreach (TreeNode child in n.Nodes)
            {
                if (child is TBoldTreeNode boldChild)
                {
                    Forget(boldChild);
                }
            }

            if (n.Follower?.ControlData == n)
            {
                n.Follower.ControlData = null;
            }

            n.Destroying(this);
        }

        Forget(node);
        node.Remove();
    }

    /// <summary>The node of the follower, when it is in this tree.</summary>
    private TBoldTreeNode? NodeOf(TBoldFollower? follower) =>
        follower?.ControlData is TBoldTreeNode node && node.TreeView == this ? node : null;

    [Delphi(PortStatus.Ported)]
    private void SetBoldHandle(TBoldElementHandle? Value) => fHandleFollower!.BoldHandle = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldElementHandle? GetBoldHandle() => fHandleFollower!.BoldHandle;

    [Delphi(PortStatus.Ported)]
    private TBoldFollower GetRootFollower() => fHandleFollower!.Follower;

    [Delphi(PortStatus.Ported)]
    private void SetTreeController(TBoldTreeFollowerController Value) => FTreeController!.Assign(Value);

    /// <summary>The deepest level shown (-1: no limit); the nodes are shown again for the new limit.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested DoList is a local function.")]
    private void SetMaxLevels(int Value)
    {
        void DoList(int Level, TBoldFollower Follower)
        {
            for (int I = 0; I < Follower.SubFollowerCount; I++)
            {
                TBoldFollower sub = Follower.SubFollowers(I)!;
                //Update this node
                if (((TBoldNodeFollowerController)sub.Controller!).HideNodeWithNoChildren)
                {
                    DoInsertHiddenNode(sub);
                }
                else if (NodeOf(sub) is { } node)
                {
                    SetNodeState(node);
                }

                //Recurse through subfollowers
                if (sub.Active && sub.SubFollowerCount >= BoldNodeListIndex && sub.SubFollowers(BoldNodeListIndex) is { Active: true } list)
                {
                    DoList(Level + 1, list);
                }
            }
        }

        RootFollower.EnsureDisplayable();
        if (Value < -1)
        {
            Value = -1;
        }

        if (Value != FMaxLevels)
        {
            FMaxLevels = Value;
            BeginUpdate();
            try
            {
                DoList(0, RootFollower);
            }
            finally
            {
                EndUpdate();
            }
        }
    }

    /// <summary>The levels expanded automatically (-1: none chosen); the nodes are expanded or collapsed to it.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetAutoExpandLevels(int Value)
    {
        RootFollower.EnsureDisplayable();
        if (Value < -1)
        {
            Value = -1;
        }

        FAutoExpandLevels = Value;
        if (FAutoExpandLevels != -1)
        {
            int I = 0;
            BeginUpdate();
            try
            {
                // Expanding loads children, which come after the node: the nodes are taken again each time.
                List<TBoldTreeNode> nodes = AllNodes();
                while (I < nodes.Count)
                {
                    TBoldTreeNode Node = nodes[I];
                    if (Node.Level <= FAutoExpandLevels)
                    {
                        Node.Expand();
                    }
                    else
                    {
                        Node.Collapse();
                    }

                    I++;
                    nodes = AllNodes();
                }
            }
            finally
            {
                EndUpdate();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetSelectedIndexDelta(int Value)
    {
        if (Value != FSelectedIndexDelta)
        {
            FSelectedIndexDelta = Value;
            BeginUpdate();
            try
            {
                foreach (TBoldTreeNode Node in AllNodes())
                {
                    Node.SelectedImageIndex = Node.ImageIndex + FSelectedIndexDelta;
                }
            }
            finally
            {
                EndUpdate();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldFollower? GetCurrentFollower() => Selected?.Follower;

    [Delphi(PortStatus.Ported)]
    private TBoldElement? GetCurrentElement() => Selected?.Follower?.Element;

    /// <summary>The tree's window is made: the root follower becomes active (and the nodes come).</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.CreateWnd", Note = "OnHandleCreated.")]
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (fHandleFollower is not null)
        {
            RootFollower.Active = true;
        }
    }

    /// <summary>The tree's window goes: the root follower becomes inactive, and the nodes go.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.DestroyWnd", Note = "OnHandleDestroyed.")]
    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (fHandleFollower is not null && !RecreatingHandle)
        {
            RootFollower.Active = false;
            foreach (TBoldTreeNode node in AllNodes())
            {
                if (node.Follower?.ControlData == node)
                {
                    node.Follower.ControlData = null;
                }

                node.Destroying(this);
            }

            Nodes.Clear();
        }

        base.OnHandleDestroyed(e);
    }

    /// <summary>Delphi's own update count (it did not hold the tree's drawing).</summary>
    [Delphi(PortStatus.Ported, Note = "Hides TreeView.BeginUpdate, which holds the drawing.")]
    protected new void BeginUpdate()
    {
        //  Items.BeginUpdate;
        FUpdateCount++;
    }

    [Delphi(PortStatus.Ported, Note = "Hides TreeView.EndUpdate.")]
    protected new void EndUpdate()
    {
        //  Items.EndUpdate;
        FUpdateCount--;
    }

    /// <summary>The node's text follower changed: the node shows the text, with the text controller's color and font.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DisplayText(TBoldFollower Follower)
    {
        if (NodeOf(Follower.OwningFollower) is { } Node)
        {
            Node.Text = ((TBoldStringFollowerController)Follower.Controller!).GetCurrentAsString(Follower);
            _CustomDrawItem(Node);
        }
    }

    /// <summary>The node's icon follower changed: the image (and selected and state images) as the icon and selection say.</summary>
    [Delphi(PortStatus.Ported, Note = "Always sets the image indexes (WinForms has no OnGetImageIndex).")]
    protected virtual void DisplayIcon(TBoldFollower Follower)
    {
        if (NodeOf(Follower.OwningFollower) is { } Node)
        {
            int ImageIndex = ((TBoldIntegerFollowerController)Follower.Controller!).GetCurrentAsInteger(Follower);
            int SelectedIndex;
            if (SelectedIndexDelta != 0)
            {
                SelectedIndex = ImageIndex + SelectedIndexDelta;
            }
            else if (SelectedImageIndex != -1)
            {
                SelectedIndex = SelectedImageIndex;
            }
            else
            {
                SelectedIndex = ImageIndex;
            }

            // Make a multiselected node appear selected even thjough the treeview does not know this
            if (Follower.Selected && !Node.IsSelected)
            {
                ImageIndex = SelectedIndex;
            }

            Node.SelectedImageIndex = SelectedIndex;
            Node.ImageIndex = ImageIndex;
            if (StateImageList is not null && StateImageSelected != -1)
            {
                Node.StateImageIndex = Follower.Selected || Node.IsSelected ? StateImageSelected : StateImageUnselected;
            }
        }
    }

    /// <summary>A new node of the tree (overridden for nodes of a descendant class).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldTreeNode CreateNode() => new();

    /// <summary>The tree is up to date: the selection noted before is let go.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void AfterMakeUptoDate(TBoldFollower Follower)
    {
        // for some reason, the pointer to this event seems to get lost after a while.
        // the variables below might contain space pointers outside of an update operation
        // if this method is not executed.
        fSelectedNodePreUpdate = null;
        fSelectedElementPreUpdate = null;
        fSelectedNodeDescriptionPreUpdate = null;
    }

    /// <summary>The tree is about to change: the selected node is noted (to select it again when it comes back).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void BeforeMakeUptoDate(TBoldFollower Follower)
    {
        fSelectedNodePreUpdate = Selected;
        fSelectedElementPreUpdate = null;
        fSelectedNodeDescriptionPreUpdate = null;
        fNodesHaveBeenDeleted = false;
    }

    /// <summary>A node for the follower, before the next sibling that has one (or last under its parent, made as needed).</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldTreeNode DoInsertVisibleNode(TBoldFollower Follower)
    {
        TBoldTreeNode? ParentNode = null;
        TBoldTreeNode? PrevSiblingNode = null;
        TBoldFollower owning = Follower.OwningFollower!;
        int I = Follower.index;
        while (PrevSiblingNode is null && I < owning.SubFollowerCount)
        {
            PrevSiblingNode = NodeOf(owning.SubFollowers(I));
            I++;
        }

        TBoldTreeNode Result = CreateNode();
        if (PrevSiblingNode is not null)
        {
            TreeNodeCollection siblings = PrevSiblingNode.Parent?.Nodes ?? Nodes;
            siblings.Insert(PrevSiblingNode.Index, Result);
        }
        else
        {
            if (owning.OwningFollower is { } parentFollower)
            {
                ParentNode = NodeOf(parentFollower) ?? DoInsertVisibleNode(parentFollower);
            }

            ParentNode?.RemovePlaceholder();
            (ParentNode?.Nodes ?? Nodes).Add(Result);
        }

        Result.Follower = Follower;
        Follower.ControlData = Result;
        SetNodeState(Result);
        return Result;
    }

    /// <summary>
    /// A follower whose node hides when it has no children: its children are loaded (they make its node); beyond the
    /// levels shown, or repeating a parent, it is not shown.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested GetLevel is a local function.")]
    protected void DoInsertHiddenNode(TBoldFollower Follower)
    {
        int GetLevel()
        {
            int Result = 0;
            for (TBoldFollower? aFollower = Follower.OwningFollower; aFollower is not null; aFollower = aFollower.OwningFollower)
            {
                if (aFollower.Controller is TBoldNodeFollowerController)
                {
                    Result++;
                }
            }

            return Result;
        }

        int Level = GetLevel();
        if ((MaxLevels == -1 || Level < MaxLevels - 1) && !Follower.ExistInOwner())
        {
            BeginUpdate();
            try
            {
                Follower.Active = true;
                ((TBoldNodeFollowerController)Follower.Controller!).SetActiveRange(Follower, BoldNodeListIndex, BoldNodeListIndex);
                Follower.EnsureDisplayable();
                //If EnsureDisplayable creates childnodes they will create a node to this follower too.
                if (Level <= AutoExpandLevels && NodeOf(Follower) is { } node)
                {
                    node.Expand(); //CHECKME needed?
                }
            }
            finally
            {
                EndUpdate();
            }
        }
        else
        {
            //Dont show it if we can't show any children!
            Follower.Active = false;
            if (NodeOf(Follower) is { } node)
            {
                RemoveNode(node);
            }

            Follower.ControlData = null;
        }
    }

    /// <summary>
    /// What the node loads: everything when expanded automatically or shown (as NodeExpansion says); else only its
    /// icon and text, with an expand button when it may have children; beyond the levels shown, no children.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Expanding raises BeforeExpand (Delphi raised OnExpanding first as well).")]
    protected void SetNodeState(TBoldTreeNode Node)
    {
        TBoldFollower Follower = Node.Follower!;
        var Controller = (TBoldNodeFollowerController)Follower.Controller!;
        if (Controller.HideNodeWithNoChildren)
        {
            //Nodes with HideNodeWithNoChildren is always fully shown
            Controller.SetActiveRange(Follower, BoldNodeListIndex, BoldNodeTextIndex);
        }
        else if ((MaxLevels == -1 || Node.Level < MaxLevels) && !Node.ExistsInParent())
        {
            if (NodeExpansion == TBoldNodeExpansionMethod.neAll ||
                Node.Level <= AutoExpandLevels ||
                (NodeExpansion == TBoldNodeExpansionMethod.neVisible && IsShown(Node)))
            {
                BeginUpdate();
                try
                {
                    Controller.SetActiveRange(Follower, BoldNodeListIndex, BoldNodeTextIndex);
                    Follower.EnsureDisplayable();
                    if (Node.Level <= AutoExpandLevels)
                    {
                        Node.Expand();
                    }
                }
                finally
                {
                    EndUpdate();
                }
            }
            else if (Follower.SubFollowerCount == 0 || Follower.SubFollowers(BoldNodeListIndex) is not { Active: true })
            {
                //Only set node in "ExpandOnDemand" mode if it's not expanded already!
                Node.HasChildren = ((TBoldGenericListController)Controller[BoldNodeListIndex]).CanHaveSubFollowers();
                Controller.SetActiveRange(Follower, BoldNodeIconIndex, BoldNodeTextIndex);
                Follower.EnsureDisplayable();
            }
        }
        else
        {
            //Don't allow expansion
            foreach (TreeNode child in Node.Nodes.Cast<TreeNode>().ToList())
            {
                if (child is TBoldTreeNode boldChild)
                {
                    RemoveNode(boldChild);
                }
                else
                {
                    child.Remove();
                }
            }

            Controller.SetActiveRange(Follower, BoldNodeIconIndex, BoldNodeTextIndex);
            Follower.EnsureDisplayable();
        }
    }

    /// <summary>A node follower came: its node (or, for a hidden one, its children); selected when SelectInserted or when it was selected before.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void InsertNode(TBoldFollower Follower)
    {
        if (!((TBoldNodeFollowerController)Follower.Controller!).HideNodeWithNoChildren)
        {
            TBoldTreeNode NewNode = DoInsertVisibleNode(Follower);
            if (!fNodesHaveBeenDeleted && FSelectInserted && FUpdateCount < 1)
            {
                Selected = NewNode;
            }

            if (fSelectedNodePreUpdate is not null &&
                Follower.Element == fSelectedElementPreUpdate &&
                NewNode.NodeDescription == fSelectedNodeDescriptionPreUpdate)
            {
                Selected = NewNode;
            }
        }
        else
        {
            DoInsertHiddenNode(Follower);
        }
    }

    /// <summary>A node follower goes: its node goes (and a hiding parent left without children goes too).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DeleteNode(TBoldFollower Follower)
    {
        if (NodeOf(Follower) is { } node)
        {
            var ParentNode = node.Parent as TBoldTreeNode;
            if (fSelectedNodePreUpdate == node)
            {
                fSelectedElementPreUpdate = Follower.Element;
                fSelectedNodeDescriptionPreUpdate = fLastSelectedNode?.NodeDescription;
            }

            fNodesHaveBeenDeleted = true;
            RemoveNode(node);
            Follower.ControlData = null;
            if (ParentNode is not null && ParentNode.HideNodeIfNoVisibleChildren && !ParentNode.HasChildren)
            {
                ((TBoldNodeFollowerController)ParentNode.Follower!.Controller!).SetActiveRange(ParentNode.Follower, BoldNodeListIndex, BoldNodeListIndex);
                DeleteNode(ParentNode.Follower);
            }
        }
        else
        {
            Follower.ControlData = null;
        }
    }

    /// <summary>A node's label may be edited when its text controller may change the element; typed characters are then checked.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.CanEdit", Note = "OnBeforeLabelEdit; the BeforeLabelEdit event is raised first.")]
    protected override void OnBeforeLabelEdit(NodeLabelEditEventArgs e)
    {
        FEditFollower = null;
        base.OnBeforeLabelEdit(e);
        bool Result = !e.CancelEdit;
        if (e.Node is TBoldTreeNode { Follower: { } follower } && follower.SubFollowers(BoldNodeTextIndex) is { } text)
        {
            Result = Result && text.AssertedController.MayModify(text);
            if (Result)
            {
                FEditFollower = text;
            }
        }

        e.CancelEdit = !Result;
        if (Result && IsHandleCreated)
        {
            IntPtr editor = SendMessage(Handle, TVM_GETEDITCONTROL, IntPtr.Zero, IntPtr.Zero);
            if (editor != IntPtr.Zero)
            {
                _ = new TBoldLabelEditWindow(this, editor);
            }
        }
    }

    /// <summary>Drops characters the node's text does not take (with a beep) while its label is edited.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.KeyPress", Note = "The label editor's characters come here too.")]
    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (FEditFollower is not null && e.KeyChar > ' ' &&
            !((TBoldStringFollowerController)FEditFollower.Controller!).ValidateCharacter(e.KeyChar, FEditFollower))
        {
            System.Media.SystemSounds.Beep.Play();
            e.Handled = true;
        }
    }

    internal void EditorKeyPress(KeyPressEventArgs e) => OnKeyPress(e);

    /// <summary>The label was edited: the node's text and element take it (applied at once); a cancelled edit drops the change.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.Edit", Note = "OnAfterLabelEdit; the AfterLabelEdit event is raised first (Delphi's OnEdited).")]
    protected override void OnAfterLabelEdit(NodeLabelEditEventArgs e)
    {
        base.OnAfterLabelEdit(e);
        if (e.Label is not null && !e.CancelEdit)
        {
            string S = e.Label;
            if (e.Node is { } Node)
            {
                Node.Text = S;
                e.CancelEdit = true; // the node has its text: the tree does not set it again
                if (FEditFollower is not null)
                {
                    ((TBoldStringFollowerController)FEditFollower.Controller!).MayHaveChanged(S, FEditFollower);
                    FEditFollower.Apply();
                }
            }
        }
        else
        {
            FEditFollower?.DiscardChange();
        }

        FEditFollower = null;
    }

    private static bool OnItem(TreeViewHitTestInfo hit) =>
        hit.Location is TreeViewHitTestLocations.Label or TreeViewHitTestLocations.Image or TreeViewHitTestLocations.StateImage;

    /// <summary>A click (not a drag) on a node selects it as the keys of the click said.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.MouseUp")]
    protected override void OnMouseUp(MouseEventArgs e)
    {
        TreeViewHitTestInfo HitTests = HitTest(e.X, e.Y);
        // skip the MouseUp if this is a drag...
        if (Math.Abs(e.X - fMouseDownPos.X) + Math.Abs(e.Y - fMouseDownPos.Y) < SystemInformation.DragSize.Width &&
            e.Button == MouseButtons.Left && OnItem(HitTests) && fMouseDownNode is not null)
        {
            UpdateMultiSelect(fMouseDownNode, fMouseDownShiftState, TBoldMouseDirection.DirMouseUp);
        }

        base.OnMouseUp(e);
    }

    /// <summary>A press on a node: the node to drag; a right click selects only it; a left click selects as the keys say.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.MouseDown")]
    protected override void OnMouseDown(MouseEventArgs e)
    {
        Keys Shift = ModifierKeys;
        TreeViewHitTestInfo HitTests = HitTest(e.X, e.Y);
        FDragFollower = e.Button == MouseButtons.Left && OnItem(HitTests) ? GetFollowerAt(e.X, e.Y) : null;
        fMouseDownNode = GetNodeAt(e.X, e.Y) as TBoldTreeNode;
        fMouseDownPos = e.Location;
        if (e.Button == MouseButtons.Right && Shift == Keys.None && OnItem(HitTests) && fMouseDownNode is not null)
        {
            ClearAllSelections();
            Selected = fMouseDownNode;
            fMouseDownNode.SetSelected(true);
        }

        if (e.Button == MouseButtons.Left && OnItem(HitTests) && fMouseDownNode is not null)
        {
            UpdateMultiSelect(fMouseDownNode, Shift, TBoldMouseDirection.DirMouseDown);
        }

        // for some reason, the shiftstate is not preserved until mouse up
        fMouseDownShiftState = Shift;
        base.OnMouseDown(e);
    }

    /// <summary>A node is dragged: its element (or, selecting several, the selected objects) are the dragged objects.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.DoStartDrag", Note = "OnItemDrag; the ItemDrag event is raised first. The drag runs here, then DoEndDrag.")]
    protected override void OnItemDrag(ItemDragEventArgs e)
    {
        base.OnItemDrag(e);
        if (MultiSelect)
        {
            FillListWithSelectedObjects(BoldGUI.BoldGUIHandler().DraggedObjects);
        }
        else
        {
            if (FDragFollower is null)
            {
                Point P = PointToClient(MousePosition);
                FDragFollower = GetFollowerAt(P.X, P.Y);
            }

            FDragFollower?.AssertedController.StartDrag(FDragFollower);
        }

        try
        {
            if (BoldGUI.BoldGUIHandler().DraggedObjects.Count > 0)
            {
                DoDragDrop(new TBoldDragObject { Source = this }, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
            }
        }
        finally
        {
            DoEndDrag();
        }
    }

    /// <summary>The drag ended: the dragged objects are let go.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DoEndDrag()
    {
        if (MultiSelect)
        {
            BoldGUI.BoldGUIHandler().DraggedObjects.Clear();
        }
        else
        {
            FDragFollower?.AssertedController.EndDrag();
            FDragFollower = null;
        }
    }

    /// <summary>Accepts what the controller of the node under the mouse accepts on its element.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.DragOver", Note = "OnDragOver; the DragOver event is raised first.")]
    protected override void OnDragOver(DragEventArgs drgevent)
    {
        base.OnDragOver(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            Point p = PointToClient(new Point(drgevent.X, drgevent.Y));
            TBoldFollower? Follower = GetFollowerAt(p.X, p.Y);
            TBoldDragSupport.Accept(drgevent, Follower is not null && Follower.AssertedController.DropMode != TBoldDropMode.bdpNone &&
                Follower.AssertedController.DragOver(Follower, Follower.Element, 0));
        }
    }

    /// <summary>Drops on the element of the node under the mouse.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.DragDrop", Note = "OnDragDrop; the DragDrop event is raised first.")]
    protected override void OnDragDrop(DragEventArgs drgevent)
    {
        base.OnDragDrop(drgevent);
        if (TBoldDragSupport.IsBoldDrag(drgevent))
        {
            Point p = PointToClient(new Point(drgevent.X, drgevent.Y));
            if (GetFollowerAt(p.X, p.Y) is { } Follower)
            {
                Follower.AssertedController.DragDrop(Follower, Follower.Element, 0);
            }
            else if (BoldGUI.BoldGUIHandler().ActivateTargetFormOnDrop)
            {
                BoldGUI.BoldGUIHandler().TryToFocusHostingForm(this);
            }
        }
    }

    /// <summary>The popup menu: the tree view's own.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual ContextMenuStrip? GetPopupMenu() => ContextMenuStrip;

    /// <summary>
    /// A node may expand when it is within the levels shown and repeats no parent; its children are loaded then (and,
    /// with visible expansion, its children's expand buttons).
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.CanExpand", Note = "OnBeforeExpand; the BeforeExpand event is raised first.")]
    protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
    {
        base.OnBeforeExpand(e);
        if (e.Cancel || e.Node is not TBoldTreeNode Node)
        {
            return;
        }

        bool Result = Node.Follower is not null && (MaxLevels == -1 || Node.Level < MaxLevels) && !Node.ExistsInParent();
        if (Result)
        {
            BeginUpdate();
            try
            {
                TBoldFollower follower = Node.Follower!;
                if (follower.SubFollowerCount == 0 || follower.SubFollowers(BoldNodeListIndex) is not { Active: true })
                {
                    Node.HasChildren = false;
                    ((TBoldNodeFollowerController)follower.Controller!).SetActiveRange(follower, BoldNodeListIndex, BoldNodeTextIndex);
                    follower.EnsureDisplayable();
                }

                if (FNodeExpansion == TBoldNodeExpansionMethod.neVisible && follower.SubFollowerCount > BoldNodeListIndex &&
                    follower.SubFollowers(BoldNodeListIndex) is { } list)
                {
                    for (int I = 0; I < list.SubFollowerCount; I++)
                    {
                        if (NodeOf(list.SubFollowers(I)) is { } ChildNode &&
                            (MaxLevels == -1 || ChildNode.Level < MaxLevels) &&
                            !ChildNode.ExistsInParent() &&
                            (ChildNode.Follower!.SubFollowerCount == 0 || ChildNode.Follower.SubFollowers(BoldNodeListIndex) is not { Active: true }))
                        {
                            ChildNode.HasChildren = false;
                            ((TBoldNodeFollowerController)ChildNode.Follower.Controller!).SetActiveRange(ChildNode.Follower, BoldNodeListIndex, BoldNodeTextIndex);
                            ChildNode.Follower.EnsureDisplayable();
                        }
                    }
                }
            }
            finally
            {
                EndUpdate();
            }
        }

        e.Cancel = !Result;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.Expand", Note = "OnAfterExpand.")]
    protected override void OnAfterExpand(TreeViewEventArgs e) => base.OnAfterExpand(e);

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.CanCollapse", Note = "OnBeforeCollapse.")]
    protected override void OnBeforeCollapse(TreeViewCancelEventArgs e) => base.OnBeforeCollapse(e);

    /// <summary>A collapsed node's descendants are no longer selected.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.Collapse", Note = "OnAfterCollapse.")]
    protected override void OnAfterCollapse(TreeViewEventArgs e)
    {
        base.OnAfterCollapse(e);
        if (e.Node is { } node)
        {
            SetSelectionInSubtree(node, false);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.CanChange", Note = "OnBeforeSelect.")]
    protected override void OnBeforeSelect(TreeViewCancelEventArgs e) => base.OnBeforeSelect(e);

    /// <summary>The selection moved: the icons of the node left and the node selected show again.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.Change", Note = "OnAfterSelect.")]
    protected override void OnAfterSelect(TreeViewEventArgs e)
    {
        base.OnAfterSelect(e);
        fLastSelectedNode?.UpdateIcon();
        if (e.Node is TBoldTreeNode node)
        {
            node.UpdateIcon();
            fLastSelectedNode = node;
        }
    }

    /// <summary>The follower of the node at the point, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldFollower? GetFollowerAt(int X, int Y) => (GetNodeAt(X, Y) as TBoldTreeNode)?.Follower;

    /// <summary>The element of the node at the point, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldElement? GetElementAt(int X, int Y) => GetFollowerAt(X, Y)?.Element;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetContextType() => BoldHandle is { } handle ? handle.StaticBoldType ?? handle.StaticSystemTypeInfo : null;

    /// <summary>Checks the tree's context and each node description's icon, text and list part expressions in their contexts.</summary>
    [Delphi(PortStatus.Ported)]
    private bool ValidateComponent(TBoldComponentValidator ComponentValidator, string NamePrefix)
    {
        // We want to evaluate everything. Thus suboptimized expressions.
        bool Result = true;
        TBoldElementTypeInfo? Context = GetContextType();
        ComponentValidator.ValidateExpressionInContext("", Context, NamePrefix + Name);
        if (Context is not null)
        {
            for (int i = 0; i < BoldProperties.NodeDescriptions.Count; i++)
            {
                TBoldNodeDescription description = BoldProperties.NodeDescriptions[i];
                Context = description.NodeFollowerController.ContextType;
                string BaseName = SysUtils.Format("%s%s.Node[%d:%s]", NamePrefix, Name, i, description.Name); // do not localize
                Result = ComponentValidator.ValidateExpressionInContext("", Context, BaseName);
                if (Context is not null)
                {
                    Result = ComponentValidator.ValidateExpressionInContext(description.IconController.Expression, Context, BaseName + ".IconController") && Result; // do not localize
                    Result = ComponentValidator.ValidateExpressionInContext(description.TextController.Expression, Context, BaseName + ".TextController") && Result; // do not localize
                    for (int j = 0; j < description.ListController.Parts.Count; j++)
                    {
                        Result = ComponentValidator.ValidateExpressionInContext(
                            description.ListController.Parts[j].ControllerExpression,
                            Context,
                            BaseName + SysUtils.Format(".ListPart[%d]", j)) && Result; // do not localize
                    }
                }
            }
        }

        return Result;
    }

    bool IBoldValidateableComponent.ValidateComponent(TBoldComponentValidator ComponentValidator, string NamePrefix) =>
        ValidateComponent(ComponentValidator, NamePrefix);

    /// <summary>The class of the tree controller (overridden for a descendant controller).</summary>
    [Delphi(PortStatus.Ported, Note = "A Type: TBoldTreeFollowerController or a descendant, made with the tree view as owner.")]
    protected virtual Type TreeFollowerControllerClass() => typeof(TBoldTreeFollowerController);

    [Delphi(PortStatus.Ported)]
    private void SetMultiSelect(bool NewValue) => fMultiSelect = NewValue;

    /// <summary>
    /// Selects for a click (down or up) on the node: alone; Shift: the nodes from the anchor; Ctrl: toggled (only when
    /// selecting several).
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "The nested RestoreLastSelectedNode is a local function.")]
    private void UpdateMultiSelect(TBoldTreeNode Node, Keys Shift, TBoldMouseDirection MouseDirection)
    {
        void RestoreLastSelectedNode()
        {
            // restore the selectedstate of the previous node...
            if (fLastSelectedNode is not null && fLastSelectedNode != Node && fLastSelectedNode.Follower is not null)
            {
                fLastSelectedNode.SetSelected(fLastSelectedNode.Follower.Selected);
            }
        }

        bool shift = (Shift & Keys.Shift) != 0, ctrl = (Shift & Keys.Control) != 0;
        bool selected = Node.Follower?.Selected ?? false;
        if (!(shift || ctrl) || !MultiSelect)
        {
            fSelectAnchor = Node;
            if (MouseDirection == TBoldMouseDirection.DirMouseUp || !selected)
            {
                ClearAllSelections();
            }

            if (MouseDirection == TBoldMouseDirection.DirMouseDown && selected)
            {
                // a MouseDown occured on a node that was already selected,
                // make sure that the previous node is not unselected until
                // mouseup if this is a drag.
                RestoreLastSelectedNode();
            }

            Node.SetSelected(true);
            if (MouseDirection == TBoldMouseDirection.DirMouseDown)
            {
                fLastSelectedNode = Node;
            }
        }
        else if (shift && MultiSelect)
        {
            fSelectAnchor ??= Node;
            if (MouseDirection == TBoldMouseDirection.DirMouseDown)
            {
                List<TBoldTreeNode> nodes = AllNodes();
                int Start = nodes.IndexOf(fSelectAnchor);
                int Stop = nodes.IndexOf(Node);
                if (Start > Stop)
                {
                    (Start, Stop) = (Stop, Start);
                }

                ClearAllSelections();
                for (int i = Math.Max(Start, 0); i <= Stop; i++)
                {
                    if (IsShown(nodes[i]))
                    {
                        nodes[i].SetSelected(true);
                    }
                }

                fLastSelectedNode = Node;
            }
        }
        else if (ctrl && MultiSelect)
        {
            RestoreLastSelectedNode();
            fSelectAnchor = Node;
            if (MouseDirection == TBoldMouseDirection.DirMouseDown)
            {
                Node.SetSelected(!(Node.Follower?.Selected ?? false));
                if (Node.IsSelected && !(Node.Follower?.Selected ?? false))
                {
                    SelectedNode = null;
                }

                fLastSelectedNode = Node;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ClearAllSelections()
    {
        foreach (TBoldTreeNode node in AllNodes())
        {
            node.SetSelected(false);
        }
    }

    /// <summary>Adds the objects of the selected nodes (Bold's selection or the tree's selected node) to the list.</summary>
    [Delphi(PortStatus.Ported)]
    public void FillListWithSelectedObjects(TBoldObjectList List)
    {
        foreach (TBoldTreeNode node in AllNodes())
        {
            if (node.Follower is { } Follower && (node.IsSelected || Follower.Selected) && Follower.Element is TBoldObject element)
            {
                List.Add(element);
            }
        }
    }

    /// <summary>Selects or unselects all the node's descendants.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SetSelectionInSubtree(TreeNode Node, bool Selected)
    {
        foreach (TreeNode child in Node.Nodes)
        {
            if (child is TBoldTreeNode ChildNode)
            {
                ChildNode.SetSelected(Selected);
                SetSelectionInSubtree(ChildNode, Selected);
            }
        }
    }

    /// <summary>A context menu asked for: the selected node is the popup's node.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.DoContextPopup", Note = "WndProc, on the context menu message.")]
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_CONTEXTMENU)
        {
            fPopupNode = Selected;
        }

        base.WndProc(ref m);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldElement? GetPopupElement() => fPopupNode?.Follower?.Element;

    /// <summary>Moving with the keys selects the new node as the keys say.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldCustomTreeView.KeyUp")]
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (e.KeyCode is Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.Right or Keys.Left && Selected is { } node)
        {
            UpdateMultiSelect(node, e.Modifiers, TBoldMouseDirection.DirMouseDown);
        }
    }

    /// <summary>The list part with the name in the node description with the name, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldGenericListPart? FindListPartByNames(string NodeDescName, string ListPartName) =>
        BoldProperties.NodeDescriptions.FindByName(NodeDescName)?.ListController.FindPartByName(ListPartName);

    /// <summary>The node takes the text controller's color and font (not while it is selected).</summary>
    [Delphi(PortStatus.Ported, Note = "Runs when the node's text is shown, where Delphi set the colors and font as each node drew.")]
    private void _CustomDrawItem(TBoldTreeNode Node)
    {
        if (Node.Follower is { } Follower)
        {
            TBoldStringFollowerController TextController = ((TBoldNodeFollowerController)Follower.Controller!).TextFollowerController;
            if (Follower.SubFollowers(BoldNodeTextIndex) is { } textFollower)
            {
                Color aColor = BackColor;
                TextController.SetColor(ref aColor, BackColor, textFollower);
                Node.BackColor = aColor == BackColor ? Color.Empty : aColor;
                TFont font = BoldWinForms.ToTFont(Font, ForeColor);
                TFont effective = BoldWinForms.ToTFont(Font, ForeColor);
                TextController.SetFont(effective, font, textFollower);
                Node.ForeColor = effective.Color == ForeColor ? Color.Empty : effective.Color;
                Node.NodeFont = effective.Name == font.Name && effective.Size == font.Size && effective.Style == font.Style ? null : BoldWinForms.ToFont(effective);
            }
        }
    }

    /// <summary>The deepest level shown; -1: no limit.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(-1)]
    public int AutoExpandLevels
    {
        get => FAutoExpandLevels;
        set => SetAutoExpandLevels(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DefaultValue(null)]
    public TBoldElementHandle? BoldHandle
    {
        get => GetBoldHandle();
        set => SetBoldHandle(value);
    }

    [Delphi(PortStatus.Ported)]
    [Category("Bold"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public TBoldTreeFollowerController BoldProperties
    {
        get => FTreeController!;
        set => SetTreeController(value);
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(-1)]
    public int MaxLevels
    {
        get => FMaxLevels;
        set => SetMaxLevels(value);
    }

    /// <summary>When nodes load their children: all at once, when shown, or when expanded.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(TBoldNodeExpansionMethod.neDemand)]
    public TBoldNodeExpansionMethod NodeExpansion
    {
        get => FNodeExpansion;
        set => FNodeExpansion = value;
    }

    /// <summary>A node added becomes the selected one.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool SelectInserted
    {
        get => FSelectInserted;
        set => FSelectInserted = value;
    }

    /// <summary>A selected node's image is its image plus this.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(0)]
    public int SelectedIndexDelta
    {
        get => FSelectedIndexDelta;
        set => SetSelectedIndexDelta(value);
    }

    /// <summary>The image of a selected node (when SelectedIndexDelta is 0); -1: its own.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(-1)]
    public new int SelectedImageIndex
    {
        get => fSelectedImageIndex;
        set => fSelectedImageIndex = value;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(-1)]
    public int StateImageSelected
    {
        get => fStateImageSelected;
        set => fStateImageSelected = value;
    }

    [Delphi(PortStatus.Ported)]
    [DefaultValue(-1)]
    public int StateImageUnselected
    {
        get => fStateImageUnselected;
        set => fStateImageUnselected = value;
    }

    /// <summary>Several nodes can be selected (with Shift and Ctrl), in Bold's selection.</summary>
    [Delphi(PortStatus.Ported)]
    [DefaultValue(false)]
    public bool MultiSelect
    {
        get => fMultiSelect;
        set => SetMultiSelect(value);
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower? CurrentFollower => GetCurrentFollower();

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldElement? CurrentElement => GetCurrentElement();

    /// <summary>The element of the node the last context menu was for.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldElement? PopupElement => GetPopupElement();

    /// <summary>The selected node.</summary>
    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldTreeNode? Selected
    {
        get => SelectedNode as TBoldTreeNode;
        set => SelectedNode = value;
    }

    [Delphi(PortStatus.Ported)]
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TBoldFollower RootFollower => GetRootFollower();
}

/// <summary>A Bold tree view; its properties are public in TBoldCustomTreeView.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTreeView : TBoldCustomTreeView
{
}
