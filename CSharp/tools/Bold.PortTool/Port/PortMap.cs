using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bold.PortTool.Port;

/// <summary>
/// CSharp/port/port-map.json: for every Delphi unit, the C# project it goes to and the milestone that ports it,
/// or the reason it is not ported. This file is edited by hand; the tool checks it against the Delphi source.
/// </summary>
public sealed class PortMap
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    [JsonPropertyName("$comment")]
    public string? Comment { get; set; }

    /// <summary>Milestone ids and titles, in order, such as "M1": "Load models".</summary>
    public Dictionary<string, string> Milestones { get; set; } = [];

    public List<PortMapUnit> Units { get; set; } = [];

    /// <summary>A JSON string literal (or null) that keeps characters such as &lt; and &gt; readable.</summary>
    public static string Json(string? value) => JsonSerializer.Serialize(value, WriteOptions);

    public static PortMap Load(string path)
    {
        var map = JsonSerializer.Deserialize<PortMap>(File.ReadAllText(path), ReadOptions)
                  ?? throw new InvalidDataException($"{path} is empty");
        return map;
    }

    public PortMapUnit? Find(string unit) =>
        Units.FirstOrDefault(u => string.Equals(u.Unit, unit, StringComparison.OrdinalIgnoreCase));

    /// <summary>"M1 Load models"; "Later (after M12, or when needed)" for ids that are not numbered.</summary>
    public string MilestoneTitle(string? milestone)
    {
        if (milestone is null)
        {
            return "";
        }

        if (!Milestones.TryGetValue(milestone, out string? title))
        {
            return milestone;
        }

        return milestone.Length > 1 && milestone[0] == 'M' && char.IsDigit(milestone[1]) ? $"{milestone} {title}" : $"{milestone} ({title})";
    }

    /// <summary>Milestone order for sorting: the order in the milestones table, unknown ones last.</summary>
    public int MilestoneOrder(string? milestone)
    {
        if (milestone is null)
        {
            return int.MaxValue;
        }

        int index = Milestones.Keys.ToList().IndexOf(milestone);
        return index < 0 ? int.MaxValue - 1 : index;
    }

    /// <summary>Writes the map with one unit per line, so a change shows as a one-line diff.</summary>
    public void Save(string path)
    {
        var text = new StringBuilder();
        text.Append("{\n");
        text.Append("  \"$comment\": ").Append(Json(Comment ?? "")).Append(",\n");
        text.Append("  \"milestones\": {\n");
        var milestones = Milestones.ToList();
        for (int k = 0; k < milestones.Count; k++)
        {
            text.Append("    ").Append(Json(milestones[k].Key)).Append(": ")
                .Append(Json(milestones[k].Value)).Append(k < milestones.Count - 1 ? ",\n" : "\n");
        }

        text.Append("  },\n");
        text.Append("  \"units\": [\n");
        for (int k = 0; k < Units.Count; k++)
        {
            text.Append("    ").Append(Units[k].ToJsonLine()).Append(k < Units.Count - 1 ? ",\n" : "\n");
        }

        text.Append("  ]\n}\n");
        File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
    }
}

public sealed class PortMapUnit
{
    public string Unit { get; set; } = "";

    /// <summary>The Delphi file, repository-relative.</summary>
    public string File { get; set; } = "";

    /// <summary>The C# project, such as Bold.Core; null when the unit is not ported or not planned yet.</summary>
    public string? Project { get; set; }

    /// <summary>The milestone that ports the unit, such as M1, or "Later".</summary>
    public string? Milestone { get; set; }

    /// <summary>Why the unit is not ported. Set only for units that are not ported.</summary>
    public string? Drop { get; set; }

    /// <summary>Anything a reader should know, such as why the unit moved to another project.</summary>
    public string? Note { get; set; }

    /// <summary>The C# file, relative to CSharp/, when it is not the default (the Delphi path under src/Project).</summary>
    public string? CSharp { get; set; }

    [JsonIgnore]
    public bool IsDropped => Drop is not null;

    /// <summary>The C# file of the unit, repository-relative: CSharp/src/Project/(path under Source).cs.</summary>
    [JsonIgnore]
    public string? CSharpFile
    {
        get
        {
            if (CSharp is not null)
            {
                return "CSharp/" + CSharp;
            }

            if (Project is null || IsDropped)
            {
                return null;
            }

            string path = File.StartsWith("Source/", StringComparison.Ordinal) ? File["Source/".Length..] : File;
            return $"CSharp/src/{Project}/{Path.ChangeExtension(path, ".cs")}";
        }
    }

    public string ToJsonLine()
    {
        var parts = new List<string>
        {
            $"\"unit\": {PortMap.Json(Unit)}",
            $"\"file\": {PortMap.Json(File)}",
            $"\"project\": {PortMap.Json(Project)}",
            $"\"milestone\": {PortMap.Json(Milestone)}",
        };
        if (Drop is not null)
        {
            parts.Add($"\"drop\": {PortMap.Json(Drop)}");
        }

        if (Note is not null)
        {
            parts.Add($"\"note\": {PortMap.Json(Note)}");
        }

        if (CSharp is not null)
        {
            parts.Add($"\"csharp\": {PortMap.Json(CSharp)}");
        }

        return "{ " + string.Join(", ", parts) + " }";
    }
}
