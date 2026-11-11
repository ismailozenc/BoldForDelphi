using Bold.PortTool.CSharp;
using Bold.PortTool.Delphi;
using Bold.PortTool.Port;

namespace Bold.PortBrowser;

/// <summary>The two code bases the browser shows.</summary>
public enum Side
{
    Delphi,
    CSharp,
}

public enum NodeKind
{
    Root,
    Folder,
    Project,

    /// <summary>A Delphi unit (.pas) or a C# file (.cs).</summary>
    File,
    Type,
    Member,
}

/// <summary>A node of one of the browser's trees: a folder, a file, a type or a member of the Delphi source or of the C# port.</summary>
public sealed class BrowserNode
{
    private readonly List<BrowserNode> _children = [];

    internal BrowserNode(Side side, NodeKind kind, string name, BrowserNode? parent)
    {
        Side = side;
        Kind = kind;
        Name = name;
        Parent = parent;
        parent?._children.Add(this);
    }

    public Side Side { get; }

    public NodeKind Kind { get; }

    public string Name { get; }

    public BrowserNode? Parent { get; }

    public IReadOnlyList<BrowserNode> Children => _children;

    /// <summary>The file of the node, from the repository's root with forward slashes; null for folders.</summary>
    public string? File { get; internal set; }

    /// <summary>The line of the declaration; 1 for a file.</summary>
    public int Line { get; internal set; } = 1;

    /// <summary>What the port did with it; null where that is not said of the node itself (a folder).</summary>
    public Indicator? Status { get; internal set; }

    /// <summary>C# code with no Delphi counterpart.</summary>
    public bool IsNew { get; internal set; }

    /// <summary>What kind of thing it is, as the details say it: "class", "method", "unit".</summary>
    public string KindText { get; internal set; } = "";

    public UnitStatus? Unit { get; internal set; }

    public DelphiSymbol? Symbol { get; internal set; }

    public CsFile? CsFile { get; internal set; }

    public CsDeclaration? Declaration { get; internal set; }

    public bool IsCode => Kind is NodeKind.File or NodeKind.Type or NodeKind.Member;

    /// <summary>The names from the root to the node, with slashes: where it is in its tree.</summary>
    public string Path => Parent is null || Parent.Kind == NodeKind.Root ? Name : $"{Parent.Path}/{Name}";

    /// <summary>The node and those below it.</summary>
    public IEnumerable<BrowserNode> SelfAndDescendants()
    {
        var pending = new Stack<BrowserNode>();
        pending.Push(this);
        while (pending.Count > 0)
        {
            BrowserNode node = pending.Pop();
            yield return node;
            for (int i = node._children.Count - 1; i >= 0; i--)
            {
                pending.Push(node._children[i]);
            }
        }
    }

    internal void SortChildren(Comparison<BrowserNode> comparison) => _children.Sort(comparison);

    public override string ToString() => $"{Side}: {Path}";
}

/// <summary>What the details panel says of a node.</summary>
public sealed class NodeDetails
{
    /// <summary>One line that says what happened to it: "Ported and verified by 3 tests", "Not ported: COM".</summary>
    public string Summary { get; init; } = "";

    /// <summary>Label and value pairs: status, project, milestone, notes.</summary>
    public List<KeyValuePair<string, string>> Facts { get; } = [];

    /// <summary>The tests that cover it.</summary>
    public List<TestRef> Tests { get; } = [];
}

/// <summary>How many symbols of a part of the Delphi source have each status.</summary>
public sealed class Totals
{
    public int Units { get; internal set; }

    public int Symbols { get; internal set; }

    public Dictionary<Indicator, int> ByStatus { get; } = [];

    public int Count(Indicator indicator) => ByStatus.GetValueOrDefault(indicator);

    public override string ToString()
    {
        if (Symbols == 0)
        {
            return $"{Units} units";
        }

        int Percent(Indicator indicator) => (int)Math.Round(100.0 * Count(indicator) / Symbols);
        return $"{Units} units, {Symbols} symbols: {Percent(Indicator.Verified)}% verified, {Percent(Indicator.Ported)}% ported, " +
               $"{Percent(Indicator.Replaced)}% replaced, {Count(Indicator.Dropped)} dropped, {Count(Indicator.NotStarted) + Count(Indicator.Partial) + Count(Indicator.Stub)} open";
    }
}
