using System.Collections.Concurrent;
using Bold.PortTool.Delphi;

namespace Bold.Tests;

/// <summary>Reads Delphi units from the repository with the port tool's parser, so tests take expected values from the Delphi code.</summary>
public static class DelphiSource
{
    private static readonly ConcurrentDictionary<string, DelphiUnit> Units = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The parsed unit at a repository-relative path such as "Source/Common/Core/BoldDefs.pas".</summary>
    public static DelphiUnit Unit(string relativePath) => Units.GetOrAdd(relativePath, path =>
        DelphiParser.ParseFile(RepoPaths.Full(path), full => Path.GetRelativePath(RepoPaths.Root, full).Replace('\\', '/'))
        ?? throw new InvalidOperationException($"{path} is not a Delphi unit"));

    /// <summary>The values of the unit's constants and resource strings; null where the expression cannot be evaluated.</summary>
    public static Dictionary<string, object?> Constants(string relativePath) => DelphiConstants.Evaluate(Unit(relativePath));

    /// <summary>A type, routine or constant declared at unit level.</summary>
    public static DelphiSymbol Symbol(string relativePath, string name) =>
        Unit(relativePath).Symbols.FirstOrDefault(s => s.Owner is null && string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"{relativePath} declares no {name}");
}
