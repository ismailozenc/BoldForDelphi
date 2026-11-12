using System.Drawing;
using System.Windows.Forms;
using Bold.PortTool.Port;

namespace Bold.PortBrowser;

/// <summary>
/// One of the two trees: the nodes of a side, made as they are opened (the trees have tens of thousands of nodes), with
/// the port status as each node's picture, and a filter that leaves the nodes of one status and the folders above them.
/// </summary>
public sealed class SideTree : TreeView
{
    private const string Placeholder = "…";
    private readonly Dictionary<BrowserNode, TreeNode> _made = [];
    private HashSet<BrowserNode>? _shown;
    private BrowserNode? _root;
    private bool _selecting;

    public SideTree(Side side)
    {
        Side = side;
        Dock = DockStyle.Fill;
        HideSelection = false;
        ShowNodeToolTips = true;
        BorderStyle = BorderStyle.None;
        ImageList = StatusImages.List;
        BeforeExpand += (_, e) => Fill(e.Node!);
        AfterSelect += (_, e) =>
        {
            if (!_selecting && e.Node?.Tag is BrowserNode node)
            {
                NodeChosen?.Invoke(this, node);
            }
        };
    }

    public Side Side { get; }

    /// <summary>The user selected a node (not the browser, following the other side).</summary>
    public event EventHandler<BrowserNode>? NodeChosen;

    public BrowserNode? Selected => SelectedNode?.Tag as BrowserNode;

    /// <summary>Shows the tree below the root: all of it, or only the nodes for which <paramref name="show"/> holds, with the folders above them.</summary>
    public void Show(BrowserNode root, Func<BrowserNode, bool>? show)
    {
        _root = root;
        _shown = null;
        if (show is not null)
        {
            _shown = [];
            foreach (BrowserNode node in root.SelfAndDescendants().Where(show))
            {
                for (BrowserNode? up = node; up is not null && _shown.Add(up); up = up.Parent)
                {
                }
            }
        }

        BeginUpdate();
        try
        {
            _made.Clear();
            Nodes.Clear();
            foreach (BrowserNode child in Children(root))
            {
                Nodes.Add(Make(child));
            }

            // The first level open: Source, or src and tests.
            foreach (TreeNode top in Nodes)
            {
                top.Expand();
            }
        }
        finally
        {
            EndUpdate();
        }
    }

    private IEnumerable<BrowserNode> Children(BrowserNode node) => _shown is null ? node.Children : node.Children.Where(_shown.Contains);

    private TreeNode Make(BrowserNode node)
    {
        string image = StatusImages.KeyOf(node);
        var made = new TreeNode(node.Name)
        {
            Tag = node,
            ImageKey = image,
            SelectedImageKey = image,
            ToolTipText = node.Status is Indicator status ? $"{node.KindText}: {PortIndex.StatusText(status)}" : node.IsNew ? $"{node.KindText}: new in C#" : node.KindText,
        };
        if (node.Status == Indicator.Dropped)
        {
            made.ForeColor = SystemColors.GrayText;
        }

        if (Children(node).Any())
        {
            made.Nodes.Add(Placeholder);
        }

        _made[node] = made;
        return made;
    }

    private void Fill(TreeNode treeNode)
    {
        if (treeNode.Nodes.Count == 1 && treeNode.Nodes[0].Tag is null && treeNode.Tag is BrowserNode node)
        {
            BeginUpdate();
            try
            {
                treeNode.Nodes.Clear();
                treeNode.Nodes.AddRange([.. Children(node).Select(Make)]);
            }
            finally
            {
                EndUpdate();
            }
        }
    }

    /// <summary>Selects the node, opening the folders above it; a node the filter hides is not selected. Tells nobody.</summary>
    public bool Select(BrowserNode? node)
    {
        _selecting = true;
        try
        {
            if (node is null || _root is null || (_shown is not null && !_shown.Contains(node)))
            {
                SelectedNode = null;
                return false;
            }

            var path = new Stack<BrowserNode>();
            for (BrowserNode? up = node; up is not null && up != _root; up = up.Parent)
            {
                path.Push(up);
            }

            TreeNode? treeNode = null;
            foreach (BrowserNode step in path)
            {
                if (treeNode is not null)
                {
                    Fill(treeNode);
                }

                if (!_made.TryGetValue(step, out treeNode))
                {
                    SelectedNode = null;
                    return false;
                }
            }

            SelectedNode = treeNode;
            treeNode?.EnsureVisible();
            return treeNode is not null;
        }
        finally
        {
            _selecting = false;
        }
    }
}

/// <summary>The pictures of the trees: a coloured dot for each port status, a folder, a plain file, and a star for what is new in C#.</summary>
public static class StatusImages
{
    private static readonly Lazy<ImageList> Made = new(Make);

    public static ImageList List => Made.Value;

    public static Color ColorOf(Indicator indicator) => indicator switch
    {
        Indicator.Verified => Color.FromArgb(0, 150, 60),
        Indicator.Ported => Color.FromArgb(120, 200, 80),
        Indicator.Replaced => Color.FromArgb(40, 110, 220),
        Indicator.Partial => Color.FromArgb(235, 200, 0),
        Indicator.Stub => Color.FromArgb(240, 140, 0),
        Indicator.Failing => Color.FromArgb(215, 30, 30),
        Indicator.NotStarted => Color.FromArgb(200, 200, 200),
        _ => Color.FromArgb(60, 60, 60),
    };

    public static string KeyOf(BrowserNode node) =>
        node.Status is Indicator status ? status.ToString() :
        node.IsNew ? "new" :
        node.Kind is NodeKind.Folder or NodeKind.Project or NodeKind.Root ? "folder" : "plain";

    private static ImageList Make()
    {
        var list = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
        foreach (Indicator indicator in Enum.GetValues<Indicator>())
        {
            list.Images.Add(indicator.ToString(), Draw(g =>
            {
                using var brush = new SolidBrush(ColorOf(indicator));
                g.FillEllipse(brush, 3, 3, 10, 10);
                if (indicator == Indicator.Verified)
                {
                    using var pen = new Pen(Color.White, 1.6F);
                    g.DrawLines(pen, [new PointF(5.5F, 8.2F), new PointF(7.4F, 10.2F), new PointF(10.6F, 6.2F)]);
                }
                else if (indicator == Indicator.Dropped)
                {
                    using var pen = new Pen(Color.White, 1.6F);
                    g.DrawLine(pen, 5.5F, 8F, 10.5F, 8F);
                }
            }));
        }

        list.Images.Add("folder", Draw(g =>
        {
            using var brush = new SolidBrush(Color.FromArgb(240, 200, 90));
            g.FillRectangle(brush, 2, 5, 12, 8);
            g.FillRectangle(brush, 2, 3, 5, 3);
        }));
        list.Images.Add("plain", Draw(g =>
        {
            using var pen = new Pen(Color.FromArgb(150, 150, 150));
            g.DrawRectangle(pen, 4, 2, 8, 11);
        }));
        list.Images.Add("new", Draw(g =>
        {
            using var brush = new SolidBrush(Color.FromArgb(150, 60, 200));
            g.FillPolygon(brush, [new PointF(8, 2), new PointF(9.8F, 6.2F), new PointF(14, 6.6F), new PointF(10.8F, 9.6F), new PointF(11.8F, 14), new PointF(8, 11.6F), new PointF(4.2F, 14), new PointF(5.2F, 9.6F), new PointF(2, 6.6F), new PointF(6.2F, 6.2F)]);
        }));
        return list;
    }

    private static Bitmap Draw(Action<Graphics> draw)
    {
        var bitmap = new Bitmap(16, 16);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        draw(graphics);
        return bitmap;
    }
}
