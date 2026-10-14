using Bold.Porting;
using Bold.PortTool.CSharp;
using Bold.PortTool.Delphi;

namespace Bold.PortTool.Port;

public enum Severity
{
    Error,
    Warning,
}

public sealed record Problem(Severity Severity, string Location, string Message)
{
    public override string ToString() => $"{(Severity == Severity.Error ? "error" : "warning")}: {Location}: {Message}";
}

/// <summary>One [Delphi] attribute found in the C# code.</summary>
public sealed class Marker
{
    public required PortStatus Status { get; init; }

    public required string Unit { get; init; }

    /// <summary>The Symbol argument, when given; otherwise the symbol is inferred from the declaration.</summary>
    public string? Symbol { get; init; }

    public string? Note { get; init; }

    /// <summary>The declaration that carries the attribute; null for [assembly: Delphi(...)].</summary>
    public CsDeclaration? Declaration { get; init; }

    public required string File { get; init; }

    public required int Line { get; init; }

    /// <summary>The Delphi symbol this marker names, once resolved.</summary>
    public DelphiSymbol? Target { get; set; }

    public string Location => $"{File}:{Line}";

    /// <summary>What a reader sees in the C# column of a report.</summary>
    public string CSharpName => Declaration is null ? $"assembly marker ({Symbol})" : Declaration.FullName;
}

/// <summary>The test indicator shown for a Delphi symbol.</summary>
public enum Indicator
{
    Verified,
    Failing,
    Ported,
    Replaced,
    Partial,
    Stub,
    NotStarted,
    Dropped,
}

/// <summary>A test method that covers a symbol.</summary>
public sealed record TestRef(string ClassName, string Method, string File, int Line)
{
    public string ShortName => $"{ClassName[(ClassName.LastIndexOf('.') + 1)..]}.{Method}";
}

public sealed class SymbolStatus
{
    public required DelphiSymbol Symbol { get; init; }

    public List<Marker> Markers { get; } = [];

    /// <summary>The marker's status, the property's status for unmarked accessors, Dropped for dropped units, or null (not started).</summary>
    public PortStatus? Status { get; set; }

    /// <summary>Set when the status comes from elsewhere, such as "property TFoo.Bar".</summary>
    public string? Via { get; set; }

    public string? Note { get; set; }

    public List<TestRef> Tests { get; } = [];

    public TestOutcome Outcome { get; set; }

    public Indicator Indicator { get; set; }

    public Marker? Marker => Markers.FirstOrDefault();
}

public sealed class UnitStatus
{
    public required DelphiUnit Unit { get; init; }

    public required PortMapUnit? Map { get; init; }

    public required List<SymbolStatus> Symbols { get; init; }

    /// <summary>C# files that port this unit: named after it or holding its markers.</summary>
    public required List<string> CSharpFiles { get; init; }

    /// <summary>Declarations in this unit's C# files that have no Delphi counterpart.</summary>
    public required List<CsDeclaration> Additions { get; init; }

    public bool IsDropped => Map?.IsDropped == true;

    public int Count(Indicator indicator) => Symbols.Count(s => s.Indicator == indicator);

    /// <summary>Ported, replaced or dropped symbols, verified or not.</summary>
    public int Done => Symbols.Count(s => s.Indicator is Indicator.Verified or Indicator.Ported or Indicator.Replaced or Indicator.Dropped);

    public int Verified => Count(Indicator.Verified);

    public int Started => Symbols.Count(s => s.Indicator != Indicator.NotStarted && s.Indicator != Indicator.Dropped);

    /// <summary>One indicator for the unit as a whole.</summary>
    public Indicator Overall
    {
        get
        {
            if (IsDropped)
            {
                return Indicator.Dropped;
            }

            if (Map?.IsReplaced == true && Symbols.All(s => s.Indicator is Indicator.Replaced or Indicator.Verified))
            {
                return Indicator.Replaced;
            }

            if (Symbols.Any(s => s.Indicator == Indicator.Failing))
            {
                return Indicator.Failing;
            }

            if (Symbols.Count > 0 && Symbols.All(s => s.Indicator is Indicator.Verified or Indicator.Dropped))
            {
                return Indicator.Verified;
            }

            if (Symbols.Count > 0 && Done == Symbols.Count)
            {
                return Indicator.Ported;
            }

            return Started > 0 || Done > 0 ? Indicator.Partial : Indicator.NotStarted;
        }
    }
}

/// <summary>
/// Joins the Delphi inventory, the [Delphi] and [Covers] attributes, the port map and the test results into a status
/// for every Delphi symbol, and collects the problems that break a link.
/// </summary>
public sealed class PortAnalysis
{
    private readonly Dictionary<CsDeclaration, List<Marker>> _markersByDeclaration = [];
    private readonly Dictionary<DelphiSymbol, SymbolStatus> _status = [];

    private PortAnalysis(RepoLayout layout, DelphiInventory delphi, CSharpInventory csharp, PortMap map, TestResults results)
    {
        Layout = layout;
        Delphi = delphi;
        CSharp = csharp;
        Map = map;
        Results = results;
    }

    public RepoLayout Layout { get; }

    public DelphiInventory Delphi { get; }

    public CSharpInventory CSharp { get; }

    public PortMap Map { get; }

    public TestResults Results { get; }

    public List<Marker> Markers { get; } = [];

    public List<Problem> Problems { get; } = [];

    public List<UnitStatus> Units { get; } = [];

    public int TestMethodCount { get; private set; }

    public IEnumerable<string> ParserWarnings =>
        Delphi.Warnings.Concat(Delphi.Units.SelectMany(u => u.Warnings)).Concat(CSharp.Files.SelectMany(f => f.Warnings));

    public SymbolStatus StatusOf(DelphiSymbol symbol) => _status[symbol];

    /// <summary>The test method declaration for "Namespace.Class.Method", to point at it from a failure.</summary>
    public CsDeclaration? FindTest(string testKey) =>
        CSharp.Declarations.FirstOrDefault(d => d.IsTestMethod && $"{d.Namespace}.{d.Owner}.{d.Name}" == testKey);

    public IReadOnlyList<Marker> MarkersOn(CsDeclaration declaration) =>
        _markersByDeclaration.TryGetValue(declaration, out var list) ? list : [];

    /// <summary>
    /// The C# declaration a reader should be sent to for a marker: the declaration that carries it, or for an assembly-level
    /// marker the "global using" alias of the same name in a file of the unit.
    /// </summary>
    public CsDeclaration? HomeOf(Marker marker) =>
        marker.Declaration ?? (marker.Target is null ? null : CSharp.Declarations.FirstOrDefault(d =>
            d.Kind == CsDeclarationKind.Alias && string.Equals(d.Name, marker.Target.Name, StringComparison.OrdinalIgnoreCase) &&
            CSharp.Files.Any(f => f.Path == d.File && string.Equals(f.UnitName, marker.Target.Unit, StringComparison.OrdinalIgnoreCase))));

    public static PortAnalysis Run(RepoLayout layout, string? resultsDirectory)
    {
        var delphi = DelphiInventory.Load(layout);
        var csharp = CSharpInventory.Load(layout);
        PortMap map;
        var problems = new List<Problem>();
        if (File.Exists(layout.PortMapFile))
        {
            map = PortMap.Load(layout.PortMapFile);
        }
        else
        {
            map = new PortMap();
            problems.Add(new Problem(Severity.Error, layout.Relative(layout.PortMapFile), "the port map is missing"));
        }

        var analysis = new PortAnalysis(layout, delphi, csharp, map, TestResults.Load(layout, resultsDirectory));
        analysis.Problems.AddRange(problems);
        analysis.CheckMap();
        foreach (var unit in delphi.Units)
        {
            foreach (var symbol in unit.Symbols)
            {
                analysis._status[symbol] = new SymbolStatus { Symbol = symbol };
            }
        }

        analysis.CollectMarkers();
        analysis.ResolveMarkers();
        analysis.CollectCoverage();
        analysis.ComputeStatus();
        analysis.BuildUnits();
        return analysis;
    }

    private void Error(string location, string message) => Problems.Add(new Problem(Severity.Error, location, message));

    private void Warning(string location, string message) => Problems.Add(new Problem(Severity.Warning, location, message));

    private void CheckMap()
    {
        string mapFile = Layout.Relative(Layout.PortMapFile);
        foreach (var unit in Delphi.Units)
        {
            var entry = Map.Find(unit.Name);
            if (entry is null)
            {
                Error(unit.File, $"unit {unit.Name} has no entry in {mapFile}");
            }
            else if (!string.Equals(entry.File, unit.File, StringComparison.Ordinal))
            {
                Error(mapFile, $"{unit.Name} is listed as {entry.File} but the unit is {unit.File}");
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Map.Units)
        {
            if (!seen.Add(entry.Unit))
            {
                Error(mapFile, $"{entry.Unit} is listed twice");
            }

            if (Delphi.Find(entry.Unit) is null)
            {
                Error(mapFile, $"{entry.Unit} is listed but there is no such unit in the Delphi source");
            }

            if (entry.Milestone is not null && entry.Milestone != "Later" && !Map.Milestones.ContainsKey(entry.Milestone))
            {
                Warning(mapFile, $"{entry.Unit} names milestone {entry.Milestone}, which is not in the milestones table");
            }

            if (entry.IsDropped && entry.Project is not null)
            {
                Warning(mapFile, $"{entry.Unit} is dropped but also names project {entry.Project}");
            }
        }
    }

    private void CollectMarkers()
    {
        foreach (var file in CSharp.Files)
        {
            foreach (var attribute in file.AssemblyAttributes.Where(a => a.Name == "Delphi"))
            {
                var marker = ReadMarker(attribute, null, file);
                if (marker is null)
                {
                    continue;
                }

                if (attribute.Named("Unit") is null || marker.Symbol is null)
                {
                    Error(marker.Location, "an assembly-level [Delphi] marker needs both Unit and Symbol");
                    continue;
                }

                Markers.Add(marker);
            }

            foreach (var declaration in file.Declarations)
            {
                foreach (var attribute in declaration.Attributes.Where(a => a.Name == "Delphi"))
                {
                    var marker = ReadMarker(attribute, declaration, file);
                    if (marker is null)
                    {
                        continue;
                    }

                    Markers.Add(marker);
                    if (!_markersByDeclaration.TryGetValue(declaration, out var list))
                    {
                        _markersByDeclaration[declaration] = list = [];
                    }

                    list.Add(marker);
                }
            }
        }
    }

    private Marker? ReadMarker(CsAttribute attribute, CsDeclaration? declaration, CsFile file)
    {
        string location = $"{attribute.File}:{attribute.Line}";
        var first = attribute.Positional.FirstOrDefault();
        string? statusName = first?.AsEnumMember(nameof(PortStatus));
        if (statusName is null || !Enum.TryParse<PortStatus>(statusName, out var status))
        {
            Error(location, $"cannot read the port status of [Delphi({first?.Text})]; write it as PortStatus.Ported (or Stub, Partial, Replaced, Dropped)");
            return null;
        }

        string? unit = ReadString(attribute, "Unit", location);
        string? symbol = ReadString(attribute, "Symbol", location);
        string? note = ReadString(attribute, "Note", location);
        if (status is PortStatus.Partial or PortStatus.Replaced or PortStatus.Dropped && string.IsNullOrWhiteSpace(note))
        {
            Warning(location, $"{status} needs a Note that says {(status == PortStatus.Partial ? "what is missing" : status == PortStatus.Replaced ? "what replaces it" : "why")}");
        }

        return new Marker
        {
            Status = status,
            Unit = unit ?? file.UnitName,
            Symbol = symbol,
            Note = note,
            Declaration = declaration,
            File = attribute.File,
            Line = attribute.Line,
        };
    }

    private string? ReadString(CsAttribute attribute, string name, string location)
    {
        var argument = attribute.Named(name);
        if (argument is null)
        {
            return null;
        }

        string? value = argument.AsString();
        if (value is null)
        {
            Error(location, $"{name} must be a string literal, not {argument.Text}");
        }

        return value;
    }

    private void ResolveMarkers()
    {
        foreach (var marker in Markers)
        {
            var unit = Delphi.Find(marker.Unit);
            if (unit is null)
            {
                Error(marker.Location, marker.Declaration is null || marker.Symbol is not null
                    ? $"[Delphi] names unit {marker.Unit}, which is not in the Delphi source"
                    : $"{marker.CSharpName}: the unit is taken from the file name, {marker.Unit}, which is not a Delphi unit; set Unit = \"...\"");
                continue;
            }

            marker.Target = marker.Symbol is not null
                ? ResolveExplicit(marker.Symbol, unit, marker.Location)
                : ResolveInferred(marker, unit);
            if (marker.Target is null)
            {
                continue;
            }

            var map = Map.Find(unit.Name);
            if (map?.IsDropped == true && marker.Status != PortStatus.Dropped)
            {
                Warning(marker.Location, $"{unit.Name} is dropped in the port map ({map.Drop}) but {marker.Target.Key} is marked {marker.Status}");
            }

            string? project = RepoLayout.ProjectOf(marker.File);
            if (map?.Project is not null && project is not null && marker.File.StartsWith("CSharp/src/", StringComparison.Ordinal) &&
                !string.Equals(project, map.Project, StringComparison.Ordinal))
            {
                Warning(marker.Location, $"{unit.Name} goes to {map.Project} in the port map, but this marker is in {project}");
            }
        }

        foreach (var group in Markers.Where(m => m.Target is not null).GroupBy(m => m.Target!))
        {
            var list = group.ToList();
            if (list.Count > 1)
            {
                Error(list[1].Location, $"{group.Key.Unit}.{group.Key.Key} is already claimed by {list[0].CSharpName} ({list[0].Location}); each Delphi symbol has one C# home");
            }

            _status[group.Key].Markers.AddRange(list);
        }
    }

    /// <summary>Finds "Type.Member", "Name" or "Type.Member(T1,T2)" in a unit.</summary>
    private DelphiSymbol? ResolveExplicit(string text, DelphiUnit unit, string location)
    {
        var exact = unit.Symbols.Where(s => string.Equals(s.Key, text, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count == 1)
        {
            return exact[0];
        }

        string normalized = text.Replace(" ", "", StringComparison.Ordinal);
        exact = unit.Symbols.Where(s => string.Equals(s.Key.Replace(" ", "", StringComparison.Ordinal), normalized, StringComparison.OrdinalIgnoreCase)).ToList();
        if (exact.Count == 1)
        {
            return exact[0];
        }

        var byBase = unit.Symbols.Where(s => string.Equals(s.BaseKey, text, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byBase.Count == 1)
        {
            return byBase[0];
        }

        if (byBase.Count > 1)
        {
            Error(location, $"{unit.Name}.{text} is overloaded; name one: {string.Join(", ", byBase.Select(s => s.Key))}");
            return null;
        }

        Error(location, $"{unit.Name} has no symbol {text}{Suggest(unit, text)}");
        return null;
    }

    private DelphiSymbol? ResolveInferred(Marker marker, DelphiUnit unit)
    {
        var declaration = marker.Declaration!;
        string location = marker.Location;
        if (declaration.IsType)
        {
            if (declaration.Kind == CsDeclarationKind.Class && string.Equals(declaration.Name, unit.Name, StringComparison.OrdinalIgnoreCase))
            {
                Error(location, $"{declaration.Name} holds the unit-level members of {unit.Name}; mark its members, not the class");
                return null;
            }

            var types = unit.Symbols.Where(s => s.IsType && string.Equals(s.Name, declaration.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            return Pick(types, declaration, unit, declaration.Name, location);
        }

        string? owner = declaration.OwnerName;
        bool unitLevel = owner is not null && string.Equals(owner, unit.Name, StringComparison.OrdinalIgnoreCase);
        string? delphiOwner = unitLevel ? null : owner;
        List<DelphiSymbol> candidates;
        string described;
        switch (declaration.Kind)
        {
            case CsDeclarationKind.Indexer:
                candidates = unit.Symbols.Where(s => s.Kind == SymbolKind.Property && s.IsDefaultProperty && SameOwner(s, delphiOwner)).ToList();
                described = $"the default array property of {owner}";
                break;
            case CsDeclarationKind.StaticConstructor when unitLevel:
                candidates = unit.Symbols.Where(s => s.Kind == SymbolKind.Initialization).ToList();
                described = "initialization";
                break;
            case CsDeclarationKind.Constructor:
                candidates = Members(unit, delphiOwner, "Create").Where(s => s.Kind is SymbolKind.Constructor).ToList();
                described = $"{owner}.Create";
                break;
            case CsDeclarationKind.Method when declaration.Name == "Dispose" && !Members(unit, delphiOwner, "Dispose").Any():
                candidates = Members(unit, delphiOwner, "Destroy").ToList();
                described = $"{owner}.Destroy";
                break;
            default:
                candidates = Members(unit, delphiOwner, declaration.Name).Where(s => !s.IsType).ToList();
                described = delphiOwner is null ? declaration.Name : $"{delphiOwner}.{declaration.Name}";
                break;
        }

        var target = Pick(candidates, declaration, unit, described, location);
        if (target is not null && declaration.Kind is not (CsDeclarationKind.Constructor or CsDeclarationKind.Indexer or CsDeclarationKind.StaticConstructor) &&
            declaration.Name != "Dispose" && !string.Equals(target.Name, declaration.Name, StringComparison.Ordinal))
        {
            Warning(location, $"C# name {declaration.Name} differs in case from Delphi {target.Name}; keep the Delphi spelling");
        }

        return target;
    }

    private static bool SameOwner(DelphiSymbol symbol, string? owner) => string.Equals(symbol.Owner, owner, StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<DelphiSymbol> Members(DelphiUnit unit, string? owner, string name) =>
        unit.Symbols.Where(s => SameOwner(s, owner) && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

    private DelphiSymbol? Pick(List<DelphiSymbol> candidates, CsDeclaration declaration, DelphiUnit unit, string described, string location)
    {
        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        if (candidates.Count == 0)
        {
            Error(location, $"{declaration.FullName}: {unit.Name} has no {described}{Suggest(unit, described)}; fix the name or set Symbol = \"...\"");
            return null;
        }

        var byCount = candidates.Where(s => !s.IsCallable || s.ParameterCount == declaration.ParameterCount).ToList();
        if (byCount.Count == 1)
        {
            return byCount[0];
        }

        Error(location, $"{declaration.FullName} could port any of {string.Join(", ", candidates.Select(s => s.Key))}; set Symbol = \"...\"");
        return null;
    }

    private static string Suggest(DelphiUnit unit, string text)
    {
        string name = text;
        int paren = name.IndexOf('(');
        if (paren >= 0)
        {
            name = name[..paren];
        }

        name = name[(name.LastIndexOf('.') + 1)..];
        var similar = unit.Symbols.Where(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)).Select(s => s.Key).Take(5).ToList();
        return similar.Count == 0 ? "" : $" (did you mean {string.Join(" or ", similar)}?)";
    }

    private void CollectCoverage()
    {
        var testMethods = CSharp.Declarations.Where(d => d.IsTestMethod).ToList();
        TestMethodCount = testMethods.Count;
        foreach (var declaration in CSharp.Declarations)
        {
            foreach (var cover in declaration.Attributes.Where(a => a.Name == "Covers"))
            {
                string location = $"{cover.File}:{cover.Line}";
                List<CsDeclaration> tests;
                if (declaration.IsTestMethod)
                {
                    tests = [declaration];
                }
                else if (declaration.Kind == CsDeclarationKind.Class)
                {
                    tests = testMethods.Where(t => t.Owner == declaration.FullName && t.Namespace == declaration.Namespace).ToList();
                    if (tests.Count == 0)
                    {
                        Warning(location, $"[Covers] on {declaration.FullName}, which has no test methods");
                        continue;
                    }
                }
                else
                {
                    Warning(location, $"[Covers] belongs on a test method or test class, not on {declaration.FullName}");
                    continue;
                }

                foreach (var symbol in ResolveCover(cover, location))
                {
                    var status = _status[symbol];
                    foreach (var test in tests)
                    {
                        var reference = new TestRef(
                            string.IsNullOrEmpty(test.Namespace) ? test.Owner ?? "" : $"{test.Namespace}.{test.Owner}",
                            test.Name, test.File, test.Line);
                        if (!status.Tests.Contains(reference))
                        {
                            status.Tests.Add(reference);
                        }
                    }
                }
            }
        }
    }

    private List<DelphiSymbol> ResolveCover(CsAttribute cover, string location)
    {
        var targets = new List<DelphiSymbol>();
        var positional = cover.Positional.ToList();
        if (positional.Count == 0)
        {
            Error(location, "[Covers] needs a type or a unit name");
            return targets;
        }

        var names = new List<string>();
        foreach (var argument in positional.Skip(1))
        {
            string? name = argument.AsString();
            if (name is null)
            {
                Error(location, $"cannot read {argument.Text}; use nameof(...) or a string literal");
                continue;
            }

            names.Add(name);
        }

        string? typeName = positional[0].AsTypeOf();
        if (typeName is null)
        {
            string? unitName = positional[0].AsString();
            var unit = unitName is null ? null : Delphi.Find(unitName);
            if (unit is null)
            {
                Error(location, $"[Covers] names unit {positional[0].Text}, which is not in the Delphi source");
                return targets;
            }

            if (names.Count == 0)
            {
                Error(location, $"[Covers(\"{unit.Name}\")] needs at least one Delphi symbol name");
            }

            foreach (string name in names)
            {
                var symbol = ResolveExplicit(name, unit, location);
                if (symbol is not null)
                {
                    targets.Add(symbol);
                }
            }

            return targets;
        }

        var types = CSharp.Declarations.Where(d => d.IsType && MatchesType(d, typeName)).ToList();
        if (types.Count == 0)
        {
            Error(location, $"[Covers] names type {typeName}, which is not declared in the C# port");
            return targets;
        }

        var declarations = new List<CsDeclaration>();
        if (names.Count == 0)
        {
            declarations.AddRange(types);
        }

        foreach (string name in names)
        {
            if (name == "*")
            {
                declarations.AddRange(types);
                declarations.AddRange(CSharp.Declarations.Where(d => types.Any(t => d.Owner == t.FullName || (d.Owner?.StartsWith(t.FullName + ".", StringComparison.Ordinal) ?? false))));
                continue;
            }

            var members = CSharp.Declarations.Where(d => !d.IsType && types.Any(t => d.Owner == t.FullName) &&
                                                          (d.Name == name || (d.Kind == CsDeclarationKind.Constructor && name is "Create" or ".ctor")))
                .ToList();
            members.AddRange(CSharp.Declarations.Where(d => d.IsType && types.Any(t => d.Owner == t.FullName) && d.Name == name));
            if (members.Count == 0)
            {
                Error(location, $"[Covers] names {typeName}.{name}, which is not declared in the C# port");
            }

            declarations.AddRange(members);
        }

        foreach (var declaration in declarations.Distinct())
        {
            var markers = MarkersOn(declaration);
            if (markers.Count == 0 && names.Count > 0 && !names.Contains("*"))
            {
                Warning(location, $"[Covers] names {declaration.FullName}, which has no [Delphi] marker, so it covers nothing");
            }

            targets.AddRange(markers.Where(m => m.Target is not null).Select(m => m.Target!));
        }

        if (names.Count == 0 && targets.Count == 0)
        {
            Warning(location, $"[Covers(typeof({typeName}))] covers nothing: the type has no [Delphi] marker; name its members");
        }

        return targets;
    }

    private static bool MatchesType(CsDeclaration declaration, string typeName)
    {
        string full = declaration.FullName;
        string qualified = string.IsNullOrEmpty(declaration.Namespace) ? full : $"{declaration.Namespace}.{full}";
        return full == typeName || qualified == typeName || qualified.EndsWith("." + typeName, StringComparison.Ordinal);
    }

    private void ComputeStatus()
    {
        foreach (var unit in Delphi.Units)
        {
            var map = Map.Find(unit.Name);
            foreach (var symbol in unit.Symbols)
            {
                var status = _status[symbol];
                var marker = status.Marker;
                if (marker is not null)
                {
                    status.Status = marker.Status;
                    status.Note = marker.Note;
                }
                else if (symbol.AccessorOf is not null && _status[symbol.AccessorOf].Marker is { } propertyMarker)
                {
                    status.Status = propertyMarker.Status;
                    status.Via = $"property {symbol.AccessorOf.Key}";
                }
                else if (map?.IsReplaced == true)
                {
                    status.Status = PortStatus.Replaced;
                    status.Note = map.Replace;
                    status.Via = "unit replaced";
                }
                else if (map?.IsDropped == true)
                {
                    status.Status = PortStatus.Dropped;
                    status.Note = map.Drop;
                    status.Via = "unit dropped";
                }
            }
        }

        // Accessors without tests of their own are checked by the tests of their property.
        foreach (var status in _status.Values.Where(s => s.Via?.StartsWith("property ", StringComparison.Ordinal) == true && s.Tests.Count == 0))
        {
            status.Tests.AddRange(_status[status.Symbol.AccessorOf!].Tests);
        }

        foreach (var status in _status.Values)
        {
            var outcomes = status.Tests.Select(t => Results.Outcome(t.ClassName, t.Method)).ToList();
            status.Outcome = outcomes.Contains(TestOutcome.Failed) ? TestOutcome.Failed
                : outcomes.Contains(TestOutcome.Passed) ? TestOutcome.Passed
                : TestOutcome.NotRun;
            status.Indicator = status.Status switch
            {
                _ when status.Outcome == TestOutcome.Failed => Indicator.Failing,
                null => Indicator.NotStarted,
                PortStatus.Dropped => Indicator.Dropped,
                PortStatus.Stub => Indicator.Stub,
                PortStatus.Partial => Indicator.Partial,
                PortStatus.Ported or PortStatus.Replaced when status.Outcome == TestOutcome.Passed => Indicator.Verified,
                PortStatus.Ported => Indicator.Ported,
                _ => Indicator.Replaced,
            };
        }
    }

    private void BuildUnits()
    {
        var filesByUnit = CSharp.Files
            .GroupBy(f => f.UnitName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
        foreach (var unit in Delphi.Units)
        {
            var map = Map.Find(unit.Name);
            var files = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var marker in Markers.Where(m => m.Target?.Unit == unit.Name))
            {
                files.Add(marker.File);
            }

            var own = filesByUnit.GetValueOrDefault(unit.Name) ?? [];
            foreach (var file in own.Where(f => f.Path.StartsWith("CSharp/src/", StringComparison.Ordinal)))
            {
                files.Add(file.Path);
            }

            var additions = own
                .Where(f => f.Path.StartsWith("CSharp/src/", StringComparison.Ordinal))
                .SelectMany(f => f.Declarations)
                .Where(d => !d.IsPrivate && MarkersOn(d).Count == 0 && d.Kind != CsDeclarationKind.Alias &&
                            !(d.Kind == CsDeclarationKind.Class && string.Equals(d.Name, unit.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            Units.Add(new UnitStatus
            {
                Unit = unit,
                Map = map,
                Symbols = unit.Symbols.Select(s => _status[s]).ToList(),
                CSharpFiles = files.ToList(),
                Additions = additions,
            });
        }
    }
}
