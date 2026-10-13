using Bold.PortTool.Port;

namespace Bold.Tests;

/// <summary>
/// The links between the Delphi source and the C# port hold for the whole repository: every unit parses, every unit is in the
/// port map, and every [Delphi] and [Covers] attribute names something that exists. The CI check command tests the same.
/// </summary>
public class RepositoryLinksTests
{
    private static readonly Lazy<PortAnalysis> Analysis = new(() => PortAnalysis.Run(new RepoLayout(RepoPaths.Root), resultsDirectory: null));

    [Test]
    public void Every_Delphi_unit_parses_without_warnings()
    {
        var analysis = Analysis.Value;
        Assert.That(analysis.Delphi.Units, Has.Count.GreaterThan(400));
        Assert.That(analysis.Delphi.Warnings.Concat(analysis.Delphi.Units.SelectMany(u => u.Warnings)), Is.Empty);
    }

    [Test]
    public void Every_method_with_a_body_is_linked_to_it()
    {
        var unlinked = Analysis.Value.Delphi.Units
            .SelectMany(u => u.Symbols.Where(s => s.IsCallable && s.ImplLine is null && !s.IsAbstract &&
                                                  !u.Symbols.Any(t => t.Kind == PortTool.Delphi.SymbolKind.Interface && t.Name == s.Owner)))
            .Select(s => s.ToString())
            .ToList();
        Assert.That(unlinked, Is.Empty);
    }

    [Test]
    public void Every_marker_and_test_link_resolves()
    {
        var errors = Analysis.Value.Problems.Where(p => p.Severity == Severity.Error).Select(p => p.ToString()).ToList();
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void The_pilot_unit_is_fully_ported()
    {
        var unit = Analysis.Value.Units.Single(u => u.Unit.Name == "BoldDefs");
        Assert.That(unit.Symbols.Where(s => s.Status is null).Select(s => s.Symbol.Key), Is.Empty, "BoldDefs symbols without a [Delphi] marker");
    }
}
