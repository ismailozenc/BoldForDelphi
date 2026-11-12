using System.Text;
using Bold.PortTool.CSharp;
using Bold.PortTool.Delphi;
using Bold.PortTool.Port;

namespace Bold.PortBrowser;

/// <summary>
/// The Delphi source and the C# port as two trees, linked both ways. It is built on the port tool's analysis (the
/// [Delphi] markers in the C# code, the port map and the test results), so it says what the status pages say, of the
/// sources as they are now.
/// </summary>
public sealed class PortIndex
{
    private readonly Dictionary<DelphiSymbol, BrowserNode> _symbolNodes = [];
    private readonly Dictionary<CsDeclaration, BrowserNode> _declarationNodes = [];
    private readonly Dictionary<string, BrowserNode> _fileNodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<BrowserNode>> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<(int Line, BrowserNode Node)>> _byLine = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<UnitStatus>> _unitsOfCSharpFile = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<CsDeclaration> _additions = [];

    private PortIndex(PortAnalysis analysis)
    {
        Analysis = analysis;
        DelphiRoot = new BrowserNode(Side.Delphi, NodeKind.Root, "Delphi", null);
        CSharpRoot = new BrowserNode(Side.CSharp, NodeKind.Root, "C#", null);
        foreach (UnitStatus unit in analysis.Units)
        {
            foreach (string file in unit.CSharpFiles)
            {
                if (!_unitsOfCSharpFile.TryGetValue(file, out var units))
                {
                    _unitsOfCSharpFile[file] = units = [];
                }

                units.Add(unit);
            }

            foreach (CsDeclaration addition in unit.Additions)
            {
                _additions.Add(addition);
            }
        }

        BuildDelphi();
        BuildCSharp();
        foreach (var lines in _byLine.Values)
        {
            lines.Sort((a, b) => a.Line.CompareTo(b.Line));
        }
    }

    public PortAnalysis Analysis { get; }

    public BrowserNode DelphiRoot { get; }

    public BrowserNode CSharpRoot { get; }

    public string Root => Analysis.Layout.Root;

    /// <summary>Whether test results were found: without them nothing shows as verified.</summary>
    public bool HasTestResults => !Analysis.Results.IsEmpty;

    public BrowserNode RootOf(Side side) => side == Side.Delphi ? DelphiRoot : CSharpRoot;

    /// <summary>Reads both code bases of the repository in <paramref name="root"/> (a folder with Source and CSharp) and links them.</summary>
    /// <param name="root">The repository, or a folder inside it.</param>
    /// <param name="resultsDirectory">The folder with test results; CSharp/TestResults when null.</param>
    public static PortIndex Load(string root, string? resultsDirectory = null) =>
        new(PortAnalysis.Run(RepoLayout.Find(root), resultsDirectory));

    /// <summary>The repository above a folder, or null when the folder is not in one.</summary>
    public static string? FindRepository(string? start)
    {
        try
        {
            return string.IsNullOrEmpty(start) ? null : RepoLayout.Find(start).Root;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    // ---- building the trees ----

    private void BuildDelphi()
    {
        var folders = new Dictionary<string, BrowserNode>(StringComparer.OrdinalIgnoreCase);
        foreach (UnitStatus unit in Analysis.Units.OrderBy(u => u.Unit.File, StringComparer.OrdinalIgnoreCase))
        {
            BrowserNode folder = Folder(folders, DelphiRoot, unit.Unit.Folder);
            var unitNode = new BrowserNode(Side.Delphi, NodeKind.File, System.IO.Path.GetFileName(unit.Unit.File), folder)
            {
                File = unit.Unit.File,
                Line = Math.Max(1, unit.Unit.UnitLine),
                Status = unit.Overall,
                Unit = unit,
                KindText = "unit",
            };
            _fileNodes[unit.Unit.File] = unitNode;
            AddName(unit.Unit.Name, unitNode);
            AddLine(unit.Unit.File, 1, unitNode);

            // Types first, so that their members find them; a member whose type is not in the unit hangs from the unit.
            var types = new Dictionary<string, BrowserNode>(StringComparer.OrdinalIgnoreCase);
            foreach (SymbolStatus status in unit.Symbols.Where(s => s.Symbol.Owner is null).OrderBy(s => s.Symbol.Line))
            {
                BrowserNode node = SymbolNode(status, unit, unitNode, status.Symbol.IsType ? NodeKind.Type : NodeKind.Member);
                if (status.Symbol.IsType)
                {
                    types.TryAdd(status.Symbol.Name, node);
                }
            }

            foreach (SymbolStatus status in unit.Symbols.Where(s => s.Symbol.Owner is not null).OrderBy(s => s.Symbol.Line))
            {
                BrowserNode parent = types.GetValueOrDefault(status.Symbol.Owner!, unitNode);
                SymbolNode(status, unit, parent, NodeKind.Member);
            }
        }

        SortFolders(DelphiRoot);
    }

    private BrowserNode SymbolNode(SymbolStatus status, UnitStatus unit, BrowserNode parent, NodeKind kind)
    {
        DelphiSymbol symbol = status.Symbol;
        var node = new BrowserNode(Side.Delphi, kind, symbol.IsCallable && symbol.Key.Length > 0 ? KeyName(symbol) : symbol.Name, parent)
        {
            File = symbol.File,
            Line = symbol.Line,
            Status = status.Indicator,
            Unit = unit,
            Symbol = symbol,
            KindText = symbol.KindText,
        };
        _symbolNodes[symbol] = node;
        AddName(symbol.Name, node);
        AddLine(symbol.File, symbol.Line, node);
        if (symbol.ImplLine is int body && symbol.ImplFile is not null)
        {
            AddLine(symbol.ImplFile, body, node);
        }

        return node;
    }

    /// <summary>A routine's name with its parameter list as the port tool keys it, so that overloads differ: "Create(,)".</summary>
    private static string KeyName(DelphiSymbol symbol)
    {
        int dot = symbol.Owner is null ? -1 : symbol.Key.IndexOf('.');
        return dot >= 0 ? symbol.Key[(dot + 1)..] : symbol.Key;
    }

    private void BuildCSharp()
    {
        var folders = new Dictionary<string, BrowserNode>(StringComparer.OrdinalIgnoreCase);
        foreach (CsFile file in Analysis.CSharp.Files.OrderBy(f => f.Path, StringComparer.OrdinalIgnoreCase))
        {
            // CSharp/src/<project>/<folders>/<file>: the tree starts below CSharp.
            string path = file.Path.StartsWith("CSharp/", StringComparison.Ordinal) ? file.Path["CSharp/".Length..] : file.Path;
            string folderPath = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
            BrowserNode folder = Folder(folders, CSharpRoot, folderPath);
            List<UnitStatus>? units = _unitsOfCSharpFile.GetValueOrDefault(file.Path);
            var fileNode = new BrowserNode(Side.CSharp, NodeKind.File, System.IO.Path.GetFileName(file.Path), folder)
            {
                File = file.Path,
                CsFile = file,
                Unit = units?.FirstOrDefault(),
                Status = units?.FirstOrDefault()?.Overall,
                IsNew = units is null && file.Path.StartsWith("CSharp/src/", StringComparison.Ordinal),
                KindText = "file",
            };
            _fileNodes[file.Path] = fileNode;
            AddLine(file.Path, 1, fileNode);

            var byFullName = new Dictionary<string, BrowserNode>(StringComparer.Ordinal);
            foreach (CsDeclaration declaration in file.Declarations.OrderBy(d => d.Line))
            {
                BrowserNode parent = declaration.Owner is not null && byFullName.TryGetValue(declaration.Owner, out BrowserNode? owner) ? owner : fileNode;
                IReadOnlyList<Marker> markers = Analysis.MarkersOn(declaration);
                DelphiSymbol? target = markers.Select(m => m.Target).FirstOrDefault(t => t is not null);
                var node = new BrowserNode(Side.CSharp, declaration.IsType ? NodeKind.Type : NodeKind.Member, DeclarationName(declaration), parent)
                {
                    File = file.Path,
                    Line = declaration.Line,
                    CsFile = file,
                    Declaration = declaration,
                    Unit = fileNode.Unit,
                    Status = target is not null ? Analysis.StatusOf(target).Indicator : null,
                    IsNew = _additions.Contains(declaration),
                    KindText = declaration.KindText,
                };
                _declarationNodes[declaration] = node;
                if (declaration.IsType)
                {
                    byFullName.TryAdd(declaration.FullName, node);
                }

                AddName(declaration.Name, node);
                AddLine(file.Path, declaration.Line, node);
            }
        }

        SortFolders(CSharpRoot);
    }

    private static string DeclarationName(CsDeclaration declaration) => declaration.Kind switch
    {
        CsDeclarationKind.Method or CsDeclarationKind.Constructor =>
            $"{declaration.Name}({new string(',', Math.Max(0, declaration.ParameterCount - 1))})",
        _ => declaration.Name,
    };

    private static BrowserNode Folder(Dictionary<string, BrowserNode> folders, BrowserNode root, string path)
    {
        if (path.Length == 0)
        {
            return root;
        }

        if (folders.TryGetValue(path, out BrowserNode? known))
        {
            return known;
        }

        int slash = path.LastIndexOf('/');
        BrowserNode parent = slash < 0 ? root : Folder(folders, root, path[..slash]);
        string name = slash < 0 ? path : path[(slash + 1)..];
        // Below CSharp/src and CSharp/tests come the projects.
        bool project = root.Side == Side.CSharp && parent.Parent == root;
        var folder = new BrowserNode(root.Side, project ? NodeKind.Project : NodeKind.Folder, name, parent) { KindText = project ? "project" : "folder" };
        folders[path] = folder;
        return folder;
    }

    /// <summary>Folders before files, each by name; what is in a file stays in the order of its lines.</summary>
    private static void SortFolders(BrowserNode root)
    {
        foreach (BrowserNode node in root.SelfAndDescendants().Where(n => n.Kind is NodeKind.Root or NodeKind.Folder or NodeKind.Project).ToList())
        {
            node.SortChildren((a, b) =>
            {
                int byKind = (a.Kind == NodeKind.File).CompareTo(b.Kind == NodeKind.File);
                return byKind != 0 ? byKind : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
        }
    }

    private void AddName(string name, BrowserNode node)
    {
        if (!_byName.TryGetValue(name, out var nodes))
        {
            _byName[name] = nodes = [];
        }

        nodes.Add(node);
    }

    private void AddLine(string file, int line, BrowserNode node)
    {
        if (!_byLine.TryGetValue(file, out var lines))
        {
            _byLine[file] = lines = [];
        }

        lines.Add((line, node));
    }

    // ---- finding ----

    /// <summary>The node of a file: a Delphi unit or a C# file, by its path from the repository's root or its full path.</summary>
    public BrowserNode? FindFile(string path)
    {
        string relative = System.IO.Path.IsPathRooted(path) ? Analysis.Layout.Relative(path) : path.Replace('\\', '/');
        return _fileNodes.GetValueOrDefault(relative);
    }

    /// <summary>The node declared at the line of the file, or the last one declared before it; the file's node when the line is above them all.</summary>
    public BrowserNode? NodeAt(string file, int line)
    {
        BrowserNode? fileNode = FindFile(file);
        if (fileNode?.File is null || !_byLine.TryGetValue(fileNode.File, out var lines))
        {
            return fileNode;
        }

        BrowserNode found = fileNode;
        foreach ((int declaredAt, BrowserNode node) in lines)
        {
            if (declaredAt > line)
            {
                break;
            }

            found = node;
        }

        return found;
    }

    /// <summary>
    /// The node a name means, seen from a node (where the name was read): "TBoldSystem", or "TBoldSystem.UpdateDatabase".
    /// Names are the same in Delphi and in C#, and Delphi's are not case sensitive. The same side is preferred, then the
    /// same file, then a type before a member.
    /// </summary>
    public BrowserNode? FindByName(string name, BrowserNode? from = null, Side? side = null)
    {
        Side preferred = side ?? from?.Side ?? Side.Delphi;
        string member = name;
        string? owner = null;
        int dot = name.LastIndexOf('.');
        if (dot > 0 && dot < name.Length - 1)
        {
            owner = name[..dot];
            member = name[(dot + 1)..];
        }

        if (!_byName.TryGetValue(member, out var candidates))
        {
            return null;
        }

        IEnumerable<BrowserNode> matching = candidates;
        if (owner is not null)
        {
            string ownerName = owner[(owner.LastIndexOf('.') + 1)..];
            matching = candidates.Where(c => c.Parent is not null && string.Equals(BareName(c.Parent.Name), ownerName, StringComparison.OrdinalIgnoreCase));
        }

        return matching
            .OrderByDescending(c => c.Side == preferred)
            .ThenByDescending(c => from?.File is not null && string.Equals(c.File, from.File, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(c => c.Kind == NodeKind.File)
            .ThenByDescending(c => c.Kind == NodeKind.Type)
            .ThenByDescending(c => c.Side == Side.CSharp && c.File?.StartsWith("CSharp/src/", StringComparison.Ordinal) == true)
            .FirstOrDefault();
    }

    private static string BareName(string name)
    {
        int paren = name.IndexOf('(');
        return paren < 0 ? name : name[..paren];
    }

    /// <summary>The files, types and members whose names contain the text, best first: the name itself, then names that start with it.</summary>
    public IReadOnlyList<BrowserNode> Search(string text, int limit = 200)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return [];
        }

        return DelphiRoot.SelfAndDescendants().Concat(CSharpRoot.SelfAndDescendants())
            .Where(n => n.IsCode && n.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => Rank(n, text))
            .ThenBy(n => n.Kind)
            .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(n => n.Side)
            .Take(limit)
            .ToList();
    }

    private static int Rank(BrowserNode node, string text)
    {
        string name = BareName(node.Name);
        if (node.Kind == NodeKind.File)
        {
            name = System.IO.Path.GetFileNameWithoutExtension(name);
        }

        return string.Equals(name, text, StringComparison.OrdinalIgnoreCase) ? 0 :
            name.StartsWith(text, StringComparison.OrdinalIgnoreCase) ? 1 : 2;
    }

    // ---- counterparts ----

    /// <summary>
    /// The counterparts of a node on the other side: the C# files of a Delphi unit, the C# declarations that carry a
    /// Delphi symbol's markers, the Delphi unit of a C# file, the Delphi symbols a C# declaration ports. Empty when there
    /// is none; <see cref="Describe"/> then says why.
    /// </summary>
    public IReadOnlyList<BrowserNode> CounterpartsOf(BrowserNode node)
    {
        var found = new List<BrowserNode>();
        void Add(BrowserNode? counterpart)
        {
            if (counterpart is not null && !found.Contains(counterpart))
            {
                found.Add(counterpart);
            }
        }

        if (node.Side == Side.Delphi)
        {
            if (node.Symbol is not null)
            {
                SymbolStatus status = Analysis.StatusOf(node.Symbol);
                foreach (Marker marker in status.Markers)
                {
                    CsDeclaration? home = Analysis.HomeOf(marker);
                    Add(home is not null ? _declarationNodes.GetValueOrDefault(home) : _fileNodes.GetValueOrDefault(marker.File));
                }

                // A property's accessor has no code of its own in C#: it is the property.
                if (found.Count == 0 && node.Symbol.AccessorOf is not null && _symbolNodes.TryGetValue(node.Symbol.AccessorOf, out BrowserNode? property))
                {
                    foreach (BrowserNode counterpart in CounterpartsOf(property))
                    {
                        Add(counterpart);
                    }
                }
            }
            else if (node.Unit is not null && node.Kind == NodeKind.File)
            {
                foreach (string file in node.Unit.CSharpFiles)
                {
                    Add(_fileNodes.GetValueOrDefault(file));
                }
            }
        }
        else if (node.Declaration is not null)
        {
            foreach (Marker marker in Analysis.MarkersOn(node.Declaration))
            {
                if (marker.Target is not null)
                {
                    Add(_symbolNodes.GetValueOrDefault(marker.Target));
                }
            }

            // A member without a marker of its own belongs to what its type ports.
            if (found.Count == 0 && node.Parent is { Declaration: not null } parent)
            {
                foreach (BrowserNode counterpart in CounterpartsOf(parent))
                {
                    Add(counterpart);
                }
            }

            if (found.Count == 0)
            {
                AddUnitsOf(node.File);
            }
        }
        else if (node.Kind == NodeKind.File)
        {
            AddUnitsOf(node.File);
        }

        return found;

        void AddUnitsOf(string? file)
        {
            if (file is not null && _unitsOfCSharpFile.TryGetValue(file, out var units))
            {
                foreach (UnitStatus unit in units)
                {
                    Add(_fileNodes.GetValueOrDefault(unit.Unit.File));
                }
            }
        }
    }

    // ---- what happened to it, and why ----

    public static string StatusText(Indicator indicator) => indicator switch
    {
        Indicator.Verified => "Verified",
        Indicator.Failing => "Failing",
        Indicator.Ported => "Ported",
        Indicator.Replaced => "Replaced",
        Indicator.Partial => "Partial",
        Indicator.Stub => "Stub",
        Indicator.NotStarted => "Not started",
        _ => "Dropped",
    };

    public static string StatusMeaning(Indicator indicator) => indicator switch
    {
        Indicator.Verified => "ported or replaced, and every test that covers it passed",
        Indicator.Failing => "a test that covers it failed",
        Indicator.Ported => "ported with the same behaviour; no passing test covers it yet",
        Indicator.Replaced => "a .NET feature or a new design does the job",
        Indicator.Partial => "part of the behaviour is ported",
        Indicator.Stub => "declared with the Delphi name; throws NotImplementedException",
        Indicator.NotStarted => "no C# yet",
        _ => "not ported on purpose",
    };

    /// <summary>What the port did with the node and why: the summary line, the facts and the tests of the details panel.</summary>
    public NodeDetails Describe(BrowserNode node)
    {
        if (node.Side == Side.Delphi && node.Symbol is not null)
        {
            return DescribeSymbol(node, Analysis.StatusOf(node.Symbol));
        }

        if (node.Side == Side.Delphi && node.Unit is not null && node.Kind == NodeKind.File)
        {
            return DescribeUnit(node, node.Unit);
        }

        if (node.Side == Side.CSharp && node.IsCode)
        {
            return DescribeCSharp(node);
        }

        var details = new NodeDetails { Summary = $"{node.KindText} {node.Path}".Trim() };
        if (node.Side == Side.Delphi && node.Kind != NodeKind.Root)
        {
            details.Facts.Add(new("In this folder", TotalsOf(node).ToString()));
        }

        return details;
    }

    private NodeDetails DescribeUnit(BrowserNode node, UnitStatus unit)
    {
        PortMapUnit? map = unit.Map;
        string summary = map?.Drop is not null ? $"Not ported: {map.Drop}"
            : map?.Replace is not null ? $"Replaced: {map.Replace}"
            : unit.CSharpFiles.Count == 0 ? "No C# file yet"
            : $"{StatusText(unit.Overall)}: {unit.Done} of {unit.Symbols.Count} symbols done, {unit.Verified} verified";
        var details = new NodeDetails { Summary = summary };
        details.Facts.Add(new("Delphi unit", $"{unit.Unit.File} ({unit.Unit.Lines} lines)"));
        details.Facts.Add(new("Status", $"{StatusText(unit.Overall)}: {StatusMeaning(unit.Overall)}"));
        if (unit.CSharpFiles.Count > 0)
        {
            details.Facts.Add(new("C#", string.Join("; ", unit.CSharpFiles)));
        }

        if (map?.Project is not null)
        {
            details.Facts.Add(new("Project", map.Project));
        }

        if (map?.Milestone is not null)
        {
            details.Facts.Add(new("Milestone", Analysis.Map.MilestoneTitle(map.Milestone)));
        }

        if (map?.Drop is not null)
        {
            details.Facts.Add(new("Why not ported", map.Drop));
        }

        if (map?.Replace is not null)
        {
            details.Facts.Add(new("Replaced by", map.Replace));
        }

        if (map?.Note is not null)
        {
            details.Facts.Add(new("Note", map.Note));
        }

        if (map?.AlsoIn is not null)
        {
            details.Facts.Add(new("Also in", map.AlsoIn));
        }

        details.Facts.Add(new("Symbols", TotalsOf(node).ToString()));
        if (unit.Additions.Count > 0)
        {
            details.Facts.Add(new("New in C#", $"{unit.Additions.Count} declarations with no Delphi counterpart"));
        }

        if (unit.Unit.UsesInterface.Count + unit.Unit.UsesImplementation.Count > 0)
        {
            details.Facts.Add(new("Uses", string.Join(", ", unit.Unit.UsesInterface.Concat(unit.Unit.UsesImplementation))));
        }

        details.Facts.Add(new("Status page", RepoLayout.UnitPage(unit.Unit.Name)));
        details.Tests.AddRange(unit.Symbols.SelectMany(s => s.Tests).Distinct());
        return details;
    }

    private NodeDetails DescribeSymbol(BrowserNode node, SymbolStatus status)
    {
        DelphiSymbol symbol = status.Symbol;
        UnitStatus? unit = node.Unit;
        string why = status.Via switch
        {
            "unit dropped" => $"the unit is not ported: {unit?.Map?.Drop}",
            "unit replaced" => $"the unit is replaced: {unit?.Map?.Replace}",
            { } via => $"through its {via}",
            null => StatusMeaning(status.Indicator),
        };
        var details = new NodeDetails { Summary = $"{StatusText(status.Indicator)}: {why}" };
        details.Facts.Add(new("Delphi", $"{symbol.KindText} {symbol.BaseKey}, {symbol.File} line {symbol.Line}" +
            (symbol.ImplLine is int body ? $", body at line {body}" : "")));
        if (symbol.Signature.Length > 0)
        {
            details.Facts.Add(new("Declaration", symbol.Signature));
        }

        details.Facts.Add(new("Status", $"{StatusText(status.Indicator)}: {StatusMeaning(status.Indicator)}"));
        foreach (Marker marker in status.Markers)
        {
            details.Facts.Add(new("C#", $"{marker.CSharpName}, {marker.File} line {marker.Line}"));
            if (!string.IsNullOrEmpty(marker.Note))
            {
                details.Facts.Add(new("Note", marker.Note!));
            }
        }

        if (status.Markers.Count == 0 && !string.IsNullOrEmpty(status.Note))
        {
            details.Facts.Add(new("Note", status.Note!));
        }

        if (symbol.Visibility.Length > 0)
        {
            details.Facts.Add(new("Visibility", symbol.Visibility));
        }

        if (symbol.Parent is not null)
        {
            details.Facts.Add(new("Inherits", string.Join(", ", new[] { symbol.Parent }.Concat(symbol.Interfaces))));
        }

        if (unit?.Map?.Project is not null)
        {
            details.Facts.Add(new("Project", unit.Map.Project));
        }

        if (unit?.Map?.Milestone is not null)
        {
            details.Facts.Add(new("Milestone", Analysis.Map.MilestoneTitle(unit.Map.Milestone)));
        }

        details.Facts.Add(new("Tests", status.Tests.Count == 0 ? "none cover it" : $"{status.Tests.Count} cover it ({status.Outcome})"));
        details.Tests.AddRange(status.Tests);
        return details;
    }

    private NodeDetails DescribeCSharp(BrowserNode node)
    {
        IReadOnlyList<BrowserNode> counterparts = CounterpartsOf(node);
        IReadOnlyList<Marker> markers = node.Declaration is null ? [] : Analysis.MarkersOn(node.Declaration);
        string summary;
        if (markers.Count > 0)
        {
            Marker marker = markers[0];
            summary = marker.Target is not null
                ? $"{StatusText(Analysis.StatusOf(marker.Target).Indicator)}: ports {marker.Target.KindText} {marker.Target.BaseKey} of {marker.Target.Unit}"
                : $"{marker.Status}: of unit {marker.Unit}";
        }
        else if (node.IsNew)
        {
            summary = "New in C#: no Delphi counterpart";
        }
        else if (counterparts.Count > 0)
        {
            summary = $"Part of the port of {string.Join(", ", counterparts.Select(c => c.Name))}";
        }
        else
        {
            summary = node.File?.StartsWith("CSharp/tests/", StringComparison.Ordinal) == true ? "A test of the port" : "New in C#: no Delphi counterpart";
        }

        var details = new NodeDetails { Summary = summary };
        details.Facts.Add(new("C#", node.Declaration is null ? node.File ?? "" : $"{node.Declaration.KindText} {node.Declaration.FullName}, {node.File} line {node.Line}"));
        string? project = node.File is null ? null : RepoLayout.ProjectOf(node.File);
        if (project is not null)
        {
            details.Facts.Add(new("Project", project));
        }

        foreach (Marker marker in markers)
        {
            details.Facts.Add(new("Marker", $"{marker.Status}" + (marker.Target is null ? $" (unit {marker.Unit})" : $": {marker.Target.BaseKey} in {marker.Target.File} line {marker.Target.Line}")));
            if (!string.IsNullOrEmpty(marker.Note))
            {
                details.Facts.Add(new("Note", marker.Note!));
            }

            if (marker.Target is not null)
            {
                SymbolStatus status = Analysis.StatusOf(marker.Target);
                details.Tests.AddRange(status.Tests.Where(t => !details.Tests.Contains(t)));
            }
        }

        if (node.Kind == NodeKind.File && node.Unit is not null)
        {
            details.Facts.Add(new("Delphi unit", node.Unit.Unit.File));
            details.Facts.Add(new("Status", $"{StatusText(node.Unit.Overall)}: {node.Unit.Done} of {node.Unit.Symbols.Count} symbols done, {node.Unit.Verified} verified"));
            if (node.Unit.Map?.Milestone is not null)
            {
                details.Facts.Add(new("Milestone", Analysis.Map.MilestoneTitle(node.Unit.Map.Milestone)));
            }
        }

        if (details.Tests.Count > 0)
        {
            details.Facts.Add(new("Tests", $"{details.Tests.Count} cover it"));
        }

        return details;
    }

    /// <summary>
    /// What to show in the other code pane when a node has no counterpart: why there is no code. Null when it has one.
    /// </summary>
    public string? WhyNoCounterpart(BrowserNode node)
    {
        if (CounterpartsOf(node).Count > 0)
        {
            return null;
        }

        var text = new StringBuilder();
        NodeDetails details = Describe(node);
        if (node.Side == Side.Delphi)
        {
            text.AppendLine($"There is no C# code for {node.Name}.").AppendLine().AppendLine(details.Summary);
            foreach ((string label, string value) in details.Facts.Where(f => f.Key is "Why not ported" or "Replaced by" or "Note"))
            {
                text.AppendLine().AppendLine($"{label}: {value}");
            }

            if (node.Symbol is not null && node.Unit?.CSharpFiles.Count > 0)
            {
                text.AppendLine().AppendLine($"The unit's C# code is in {string.Join(", ", node.Unit.CSharpFiles)}.");
            }
        }
        else
        {
            text.AppendLine($"There is no Delphi code for {node.Name}.").AppendLine().AppendLine(details.Summary);
            if (node.File?.StartsWith("CSharp/tests/", StringComparison.Ordinal) == true)
            {
                text.AppendLine().AppendLine("Delphi Bold had no tests; the tests are new with the port.");
            }
        }

        return text.ToString();
    }

    /// <summary>The symbols of the Delphi units in or below a node, by status.</summary>
    public Totals TotalsOf(BrowserNode node)
    {
        var totals = new Totals();
        if (node.Side != Side.Delphi)
        {
            return totals;
        }

        if (node.Symbol is not null)
        {
            totals.Symbols = 1;
            totals.ByStatus[Analysis.StatusOf(node.Symbol).Indicator] = 1;
            return totals;
        }

        foreach (BrowserNode unitNode in node.SelfAndDescendants().Where(n => n.Kind == NodeKind.File && n.Unit is not null))
        {
            totals.Units++;
            foreach (SymbolStatus status in unitNode.Unit!.Symbols)
            {
                totals.Symbols++;
                totals.ByStatus[status.Indicator] = totals.ByStatus.GetValueOrDefault(status.Indicator) + 1;
            }
        }

        return totals;
    }

    /// <summary>Whether a node, or something below it, has the status (for the trees' filter); with <paramref name="newOnly"/>, is new in C#.</summary>
    public static bool Matches(BrowserNode node, Indicator? status, bool newOnly) =>
        node.SelfAndDescendants().Any(n => newOnly ? n.IsNew : n.Status == status);

    // ---- the text ----

    /// <summary>The text of a file of the repository: Delphi sources are Latin-1, C# sources UTF-8.</summary>
    public string ReadText(string file)
    {
        string full = Analysis.Layout.Full(file);
        return file.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? System.IO.File.ReadAllText(full) : DelphiLexer.ReadSource(full);
    }

    public string FullPath(string file) => Analysis.Layout.Full(file);
}
