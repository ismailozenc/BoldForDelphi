namespace Bold.PortTool.Port;

/// <summary>Where things are in the repository. All paths in reports are repository-relative with forward slashes.</summary>
public sealed class RepoLayout
{
    /// <summary>Top folders that hold Delphi units to port.</summary>
    public static readonly string[] DelphiRoots = ["Source", "Tools"];

    public RepoLayout(string root)
    {
        Root = Path.GetFullPath(root);
    }

    public string Root { get; }

    public string CSharpDir => Path.Combine(Root, "CSharp");

    public string PortDir => Path.Combine(CSharpDir, "port");

    public string PortMapFile => Path.Combine(PortDir, "port-map.json");

    public string UnitsDir => Path.Combine(PortDir, "units");

    public string StatusFile => Path.Combine(PortDir, "PORT_STATUS.md");

    public string FixtureModelsDir => Path.Combine(CSharpDir, "tests", "Fixtures", "Models");

    public string DefaultResultsDir => Path.Combine(CSharpDir, "TestResults");

    /// <summary>Repository-relative path with forward slashes.</summary>
    public string Relative(string path) => Path.GetRelativePath(Root, Path.GetFullPath(path)).Replace('\\', '/');

    public string Full(string relative) => Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>The repository-relative path of a unit's port page.</summary>
    public static string UnitPage(string unit) => $"CSharp/port/units/{unit}.md";

    /// <summary>Finds the repository root: the nearest folder upwards that has both Source and CSharp.</summary>
    public static RepoLayout Find(string? start = null)
    {
        var directory = new DirectoryInfo(start ?? Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Source")) && Directory.Exists(Path.Combine(directory.FullName, "CSharp")))
            {
                return new RepoLayout(directory.FullName);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Run the port tool inside the Bold repository (a folder with Source and CSharp).");
    }

    /// <summary>C# files of the port: src and tests, without build output.</summary>
    public IEnumerable<string> CSharpSourceFiles()
    {
        foreach (string top in new[] { "src", "tests" })
        {
            string dir = Path.Combine(CSharpDir, top);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Relative(file);
                if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return file;
            }
        }
    }

    /// <summary>The project a C# file belongs to: the folder under CSharp/src or CSharp/tests.</summary>
    public static string? ProjectOf(string relativePath)
    {
        string[] parts = relativePath.Split('/');
        return parts.Length > 3 && parts[0] == "CSharp" && parts[1] is "src" or "tests" ? parts[2] : null;
    }
}

/// <summary>Markdown helpers for links that work on GitHub from any generated file.</summary>
public static class Md
{
    /// <summary>A relative link target from the file <paramref name="fromFile"/> to <paramref name="toPath"/> (both repository-relative).</summary>
    public static string Href(string fromFile, string toPath, int? line = null)
    {
        string fromDir = Path.GetDirectoryName(fromFile.Replace('/', Path.DirectorySeparatorChar)) ?? "";
        string relative = Path.GetRelativePath(string.IsNullOrEmpty(fromDir) ? "." : fromDir, toPath.Replace('/', Path.DirectorySeparatorChar))
            .Replace('\\', '/');
        relative = string.Join('/', relative.Split('/').Select(Uri.EscapeDataString));
        return line is null ? relative : $"{relative}#L{line}";
    }

    public static string Link(string text, string fromFile, string toPath, int? line = null) => $"[{Escape(text)}]({Href(fromFile, toPath, line)})";

    /// <summary>A link whose text is shown as code, for symbol and file names.</summary>
    public static string CodeLink(string text, string fromFile, string toPath, int? line = null) => $"[{Code(text)}]({Href(fromFile, toPath, line)})";

    /// <summary>Escapes characters that would break a table cell or emphasis.</summary>
    public static string Escape(string text) => text
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("*", "\\*", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\r", "", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);

    /// <summary>Inline code; backticks inside are not expected in symbol names.</summary>
    public static string Code(string text) => $"`{text.Replace("`", "'", StringComparison.Ordinal).Replace("|", "\\|", StringComparison.Ordinal)}`";
}
