namespace Bold.Tests;

/// <summary>Finds files in the repository from a running test, wherever the test binaries are.</summary>
public static class RepoPaths
{
    private static readonly Lazy<string> RootPath = new(FindRoot);

    /// <summary>The repository root: the folder with both Source (the Delphi code) and CSharp.</summary>
    public static string Root => RootPath.Value;

    /// <summary>The full path of a repository-relative path such as "Source/Common/Core/BoldDefs.pas".</summary>
    public static string Full(string relativePath) => Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "Source")) && Directory.Exists(Path.Combine(directory.FullName, "CSharp")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"No repository root above {AppContext.BaseDirectory}: the tests need the Delphi source in Source.");
    }
}
