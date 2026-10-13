using System.Collections.Concurrent;
using Bold.PortTool.CSharp;
using Bold.PortTool.Delphi;

namespace Bold.PortTool.Port;

/// <summary>Every Delphi unit under Source and Tools, parsed.</summary>
public sealed class DelphiInventory
{
    private readonly Dictionary<string, DelphiUnit> _byName = new(StringComparer.OrdinalIgnoreCase);

    private DelphiInventory(List<DelphiUnit> units, List<string> warnings)
    {
        Units = units;
        Warnings = warnings;
        foreach (var unit in units)
        {
            if (!_byName.TryAdd(unit.Name, unit))
            {
                warnings.Add($"{unit.File}: unit name {unit.Name} is also used by {_byName[unit.Name].File}; only the first is linked");
            }
        }
    }

    public List<DelphiUnit> Units { get; }

    public List<string> Warnings { get; }

    public DelphiUnit? Find(string unit) => _byName.GetValueOrDefault(unit);

    public static DelphiInventory Load(RepoLayout layout)
    {
        var files = RepoLayout.DelphiRoots
            .Select(root => Path.Combine(layout.Root, root))
            .Where(Directory.Exists)
            .SelectMany(dir => Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".pas", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var units = new ConcurrentBag<DelphiUnit>();
        var warnings = new ConcurrentBag<string>();
        Parallel.ForEach(files, file =>
        {
            try
            {
                var unit = DelphiParser.ParseFile(file, layout.Relative);
                if (unit is null)
                {
                    warnings.Add($"{layout.Relative(file)}: not a unit, skipped");
                }
                else
                {
                    units.Add(unit);
                }
            }
            catch (Exception e)
            {
                warnings.Add($"{layout.Relative(file)}: could not be read: {e.Message}");
            }
        });
        return new DelphiInventory(
            units.OrderBy(u => u.File, StringComparer.Ordinal).ToList(),
            warnings.OrderBy(w => w, StringComparer.Ordinal).ToList());
    }
}

/// <summary>Every C# file of the port (src and tests), scanned for declarations and attributes.</summary>
public sealed class CSharpInventory
{
    private CSharpInventory(List<CsFile> files)
    {
        Files = files;
    }

    public List<CsFile> Files { get; }

    public IEnumerable<CsDeclaration> Declarations => Files.SelectMany(f => f.Declarations);

    public static CSharpInventory Load(RepoLayout layout)
    {
        var files = layout.CSharpSourceFiles()
            .AsParallel()
            .Select(file => CSharpScanner.ScanFile(file, layout.Relative(file)))
            .OrderBy(f => f.Path, StringComparer.Ordinal)
            .ToList();
        return new CSharpInventory(files);
    }
}
