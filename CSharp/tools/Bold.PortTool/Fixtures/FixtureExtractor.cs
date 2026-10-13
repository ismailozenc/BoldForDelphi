using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bold.PortTool.Port;

namespace Bold.PortTool.Fixtures;

/// <summary>A model text found on the reference branch: a .bld file, or the Model property of a TBoldModel in a form.</summary>
public sealed record ModelSource(string Path, string? Component, string Text, byte[]? OriginalBytes);

public sealed class FixtureModel
{
    public required string File { get; set; }

    public required string Model { get; init; }

    public required int Version { get; init; }

    public required int Classes { get; init; }

    public required int Associations { get; init; }

    public required string Sha1 { get; init; }

    public required List<FixtureSource> Sources { get; init; }

    /// <summary>The bytes written to the file.</summary>
    public required byte[] Bytes { get; init; }
}

public sealed record FixtureSource(string Path, string? Component, string? Url);

/// <summary>
/// Copies the test models from a git ref (master) into CSharp/tests/Fixtures/Models: every .bld file, and every
/// TBoldModel stored in a text or binary .dfm. Identical texts are stored once. models.json and README.md record where
/// each model came from.
/// </summary>
public sealed partial class FixtureExtractor(RepoLayout layout, string gitRef)
{
    [GeneratedRegex(@"^VERSION (\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex VersionRegex();

    [GeneratedRegex("\\(Model\\s*\\r?\\n\\s*\"([^\"]*)\"", RegexOptions.CultureInvariant)]
    private static partial Regex ModelNameRegex();

    [GeneratedRegex(@"^\s*\(Class\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"^\s*\(Association\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex AssociationRegex();

    public List<string> Warnings { get; } = [];

    public List<ModelSource> FindSources()
    {
        var sources = new List<ModelSource>();
        foreach (string path in Git("ls-tree", "-r", "--name-only", gitRef).Split('\n', StringSplitOptions.RemoveEmptyEntries).Order(StringComparer.Ordinal))
        {
            bool bld = path.EndsWith(".bld", StringComparison.OrdinalIgnoreCase);
            bool dfm = path.EndsWith(".dfm", StringComparison.OrdinalIgnoreCase);
            if (!bld && !dfm)
            {
                continue;
            }

            byte[] bytes = GitBytes("show", $"{gitRef}:{path}");
            if (bld)
            {
                sources.Add(new ModelSource(path, null, Encoding.Latin1.GetString(bytes), bytes));
                continue;
            }

            if (bytes.AsSpan().IndexOf("TBoldModel"u8) < 0)
            {
                continue;
            }

            DfmObject root;
            try
            {
                root = DfmReader.Read(bytes);
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException or FormatException)
            {
                Warnings.Add($"{path}: could not read the form: {e.Message}");
                continue;
            }

            foreach (var component in root.DescendantsAndSelf().Where(c => c.ClassName.Equals("TBoldModel", StringComparison.OrdinalIgnoreCase)))
            {
                var lines = component.StringList("Model");
                if (lines is null || lines.Count == 0)
                {
                    Warnings.Add($"{path}: {component.Name} has no Model text");
                    continue;
                }

                sources.Add(new ModelSource(path, component.Name, string.Concat(lines.Select(l => l + "\r\n")), null));
            }
        }

        return sources;
    }

    /// <summary>Groups identical model texts and names the files.</summary>
    public List<FixtureModel> Build(List<ModelSource> sources, string? repositoryUrl)
    {
        var models = new List<FixtureModel>();
        foreach (var group in sources.GroupBy(s => Normalize(s.Text)).OrderBy(g => g.First().Path, StringComparer.Ordinal))
        {
            var first = group.OrderBy(s => s.OriginalBytes is null).ThenBy(s => s.Path, StringComparer.Ordinal).First();
            string text = group.Key;
            byte[] bytes = first.OriginalBytes ?? Encoding.Latin1.GetBytes(first.Text);
            var version = VersionRegex().Match(text);
            var name = ModelNameRegex().Match(text);
            models.Add(new FixtureModel
            {
                File = "",
                Model = name.Success ? name.Groups[1].Value : "Model",
                Version = version.Success ? int.Parse(version.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) : 0,
                Classes = ClassRegex().Matches(text).Count,
                Associations = AssociationRegex().Matches(text).Count,
                Sha1 = Convert.ToHexStringLower(SHA1.HashData(Encoding.Latin1.GetBytes(text)))[..10],
                Sources = group.OrderBy(s => s.Path, StringComparer.Ordinal).ThenBy(s => s.Component, StringComparer.Ordinal)
                    .Select(s => new FixtureSource(s.Path, s.Component, repositoryUrl is null ? null : $"{repositoryUrl}/blob/{gitRef}/{s.Path}"))
                    .ToList(),
                Bytes = bytes,
            });
        }

        NameFiles(models);
        return models.OrderBy(m => m.File, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>
    /// "Model.bld" when the model name is unique; otherwise "Model.Folder.bld" with as many trailing folders of the source path
    /// as it takes to tell the variants apart, and the source file name too when two variants come from the same folder.
    /// </summary>
    private static void NameFiles(List<FixtureModel> models)
    {
        static string[] Folders(FixtureModel model) =>
            (Path.GetDirectoryName(model.Sources[0].Path) ?? "").Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);

        static string Suffix(FixtureModel model, int depth)
        {
            string[] folders = Folders(model);
            return string.Join("-", folders.Skip(Math.Max(0, folders.Length - depth)));
        }

        foreach (var group in models.GroupBy(m => m.Model, StringComparer.OrdinalIgnoreCase))
        {
            var list = group.ToList();
            if (list.Count == 1)
            {
                list[0].File = $"{Safe(group.Key)}.bld";
                continue;
            }

            int folderCount = list.Select(m => string.Join("/", Folders(m))).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            int maxDepth = list.Max(m => Folders(m).Length);
            int depth = 1;
            while (depth < maxDepth && list.Select(m => Suffix(m, depth)).Distinct(StringComparer.OrdinalIgnoreCase).Count() < folderCount)
            {
                depth++;
            }

            foreach (var model in list)
            {
                model.File = $"{Safe(group.Key)}.{Safe(Suffix(model, depth))}.bld";
            }

            foreach (var clash in list.GroupBy(m => m.File, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
            {
                foreach (var model in clash)
                {
                    string stem = Path.GetFileNameWithoutExtension(model.Sources[0].Path);
                    string component = model.Sources[0].Component is { } c ? $"-{c}" : "";
                    model.File = $"{Path.GetFileNameWithoutExtension(model.File)}.{Safe(stem + component)}.bld";
                }
            }
        }
    }

    private static string Safe(string text) => new(text.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_').ToArray());

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n', '\r', ' ', '\0');

    /// <summary>Writes the models, models.json and README.md; removes .bld files that are no longer produced.</summary>
    public List<string> Write(List<FixtureModel> models)
    {
        string dir = layout.FixtureModelsDir;
        Directory.CreateDirectory(dir);
        var written = new List<string>();
        var keep = new HashSet<string>(models.Select(m => m.File), StringComparer.OrdinalIgnoreCase);
        foreach (string stale in Directory.EnumerateFiles(dir, "*.bld").Where(f => !keep.Contains(Path.GetFileName(f))))
        {
            File.Delete(stale);
            written.Add(layout.Relative(stale) + " (removed)");
        }

        foreach (var model in models)
        {
            string path = Path.Combine(dir, model.File);
            if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(model.Bytes))
            {
                File.WriteAllBytes(path, model.Bytes);
                written.Add(layout.Relative(path));
            }
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        var manifest = new
        {
            source = gitRef,
            models = models.Select(m => new { m.File, m.Model, m.Version, m.Classes, m.Associations, m.Sha1, m.Sources }),
        };
        string json = JsonSerializer.Serialize(manifest, options).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
        if (ReportWriter.WriteIfChanged(Path.Combine(dir, "models.json"), json))
        {
            written.Add(layout.Relative(Path.Combine(dir, "models.json")));
        }

        if (ReportWriter.WriteIfChanged(Path.Combine(dir, "README.md"), Readme(models)))
        {
            written.Add(layout.Relative(Path.Combine(dir, "README.md")));
        }

        return written;
    }

    private string Readme(List<FixtureModel> models)
    {
        var text = new StringBuilder();
        text.Append("# Test models\n\n");
        text.Append($"Bold models copied from the `{gitRef}` branch by the port tool (`dotnet run --project CSharp/tools/Bold.PortTool -- fixtures`). ");
        text.Append("Do not edit them: they are the inputs Delphi Bold read and wrote, and the C# model loader must read them unchanged. ");
        text.Append($"{models.Count} distinct models from {models.Sum(m => m.Sources.Count)} sources: `.bld` files, and `TBoldModel` components in text and binary `.dfm` forms. ");
        text.Append("Files taken from a `.bld` keep their original bytes; models taken from a form are written as Delphi saves a `.bld` ");
        text.Append("(Windows-1252 text, CRLF line ends). `models.json` lists the same data for tests.\n\n");
        text.Append("| File | Model | Version | Classes | Associations | Taken from |\n|---|---|---:|---:|---:|---|\n");
        foreach (var model in models)
        {
            string sources = string.Join("<br>", model.Sources.Select(s =>
            {
                string label = s.Component is null ? s.Path : $"{s.Path} ({s.Component})";
                return s.Url is null ? Md.Escape(label) : $"[{Md.Escape(label)}]({s.Url})";
            }));
            text.Append($"| [{model.File}]({Uri.EscapeDataString(model.File)}) | {Md.Escape(model.Model)} | {model.Version} | {model.Classes} | {model.Associations} | {sources} |\n");
        }

        return text.ToString();
    }

    /// <summary>The https URL of the origin remote on GitHub, or null.</summary>
    public string? RepositoryUrl()
    {
        try
        {
            string url = Git("remote", "get-url", "origin").Trim();
            if (url.StartsWith("git@github.com:", StringComparison.Ordinal))
            {
                url = "https://github.com/" + url["git@github.com:".Length..];
            }

            if (url.EndsWith(".git", StringComparison.Ordinal))
            {
                url = url[..^4];
            }

            return url.StartsWith("https://github.com/", StringComparison.Ordinal) ? url : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private string Git(params string[] arguments) => Encoding.UTF8.GetString(GitBytes(arguments));

    private byte[] GitBytes(params string[] arguments)
    {
        var info = new ProcessStartInfo("git")
        {
            WorkingDirectory = layout.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info) ?? throw new InvalidOperationException("git could not be started");
        using var output = new MemoryStream();
        var copy = process.StandardOutput.BaseStream.CopyToAsync(output);
        string error = process.StandardError.ReadToEnd();
        copy.Wait();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', arguments)} failed: {error.Trim()}");
        }

        return output.ToArray();
    }
}
