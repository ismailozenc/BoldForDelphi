using System.Text.Json;

namespace Bold.Tests;

/// <summary>The test models in CSharp/tests/Fixtures/Models, copied from master by the port tool (see the README there).</summary>
public static class FixturePaths
{
    public static string ModelsDirectory => RepoPaths.Full("CSharp/tests/Fixtures/Models");

    /// <summary>Every .bld file, sorted by name.</summary>
    public static IReadOnlyList<string> Models =>
        Directory.EnumerateFiles(ModelsDirectory, "*.bld").Order(StringComparer.OrdinalIgnoreCase).ToList();

    public static string Model(string fileName) => Path.Combine(ModelsDirectory, fileName);

    /// <summary>models.json: for each file, the model name, version, class and association counts, and where it came from.</summary>
    public static IReadOnlyList<FixtureModelInfo> Manifest
    {
        get
        {
            using var stream = File.OpenRead(Path.Combine(ModelsDirectory, "models.json"));
            using var document = JsonDocument.Parse(stream);
            return document.RootElement.GetProperty("models").EnumerateArray()
                .Select(m => new FixtureModelInfo(
                    m.GetProperty("file").GetString()!,
                    m.GetProperty("model").GetString()!,
                    m.GetProperty("version").GetInt32(),
                    m.GetProperty("classes").GetInt32(),
                    m.GetProperty("associations").GetInt32(),
                    m.GetProperty("sources").EnumerateArray().Select(s => s.GetProperty("path").GetString()!).ToList()))
                .ToList();
        }
    }
}

/// <summary>One entry of models.json. Counts are taken from the text, so a model loader can be checked against them.</summary>
public sealed record FixtureModelInfo(string File, string Model, int Version, int Classes, int Associations, IReadOnlyList<string> Sources)
{
    public override string ToString() => File;
}
