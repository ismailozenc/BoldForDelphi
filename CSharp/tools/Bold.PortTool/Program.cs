using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Bold.PortTool.Fixtures;
using Bold.PortTool.Port;

namespace Bold.PortTool;

public static class Program
{
    private const string Usage = """
        Bold.PortTool: links the Delphi source and the C# port both ways and reports the port status of every symbol.

        Usage: dotnet run --project CSharp/tools/Bold.PortTool -- <command> [options]

        Commands:
          report     Write CSharp/port/PORT_STATUS.md and one page per unit in CSharp/port/units.
          sync       Update the links kept in the sources: the comment after "unit X;" in each Delphi unit,
                     the "C# port" block in each FOLDER_INFO.md, and the "// Delphi:" header of each C# file.
          check      Exit with code 1 when a marker, a test link or a source link is broken or out of date.
          summary    Print a short markdown status; --out <file> appends it to a file instead (for the CI job page).
          inventory  Print what the Delphi parser found; --json <file> writes it all, --warnings lists parse warnings.
          fixtures   Copy the test models from a branch (default master) into CSharp/tests/Fixtures/Models.
          all        sync, then report.

        Options:
          --results <dir>  Test results to read: .trx files (dotnet test --logger trx) or NUnit .xml files.
                           Default: CSharp/TestResults.
          --ref <branch>   For fixtures: the branch or commit to copy from (default master).
          --root <dir>     The repository root (default: found from the current folder).
        """;

    public static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        var options = ParseOptions(args.Skip(1).ToArray());
        if (options is null)
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        RepoLayout layout;
        try
        {
            layout = RepoLayout.Find(options.GetValueOrDefault("--root"));
        }
        catch (InvalidOperationException e)
        {
            Console.Error.WriteLine(e.Message);
            return 2;
        }

        string? results = options.GetValueOrDefault("--results");
        return args[0] switch
        {
            "report" => Report(layout, results),
            "sync" => Sync(layout, results),
            "check" => Check(layout, results),
            "summary" => Summary(layout, results, options.GetValueOrDefault("--out")),
            "inventory" => Inventory(layout, options),
            "fixtures" => Fixtures(layout, options.GetValueOrDefault("--ref") ?? "master"),
            "all" => Sync(layout, results) == 0 ? Report(layout, results) : 1,
            _ => UnknownCommand(args[0]),
        };
    }

    private static Dictionary<string, string?>? ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (int k = 0; k < args.Length; k++)
        {
            string name = args[k];
            if (name is "--warnings")
            {
                options[name] = "true";
            }
            else if (name is "--results" or "--ref" or "--root" or "--json" or "--out" && k + 1 < args.Length)
            {
                options[name] = args[++k];
            }
            else
            {
                Console.Error.WriteLine($"Unknown option {name}");
                return null;
            }
        }

        return options;
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Unknown command {command}");
        Console.Error.WriteLine(Usage);
        return 2;
    }

    private static void PrintProblems(PortAnalysis analysis)
    {
        foreach (var problem in analysis.Problems)
        {
            (problem.Severity == Severity.Error ? Console.Error : Console.Out).WriteLine(problem);
        }
    }

    private static int Report(RepoLayout layout, string? results)
    {
        var analysis = PortAnalysis.Run(layout, results);
        var written = new ReportWriter(analysis).WriteAll();
        Console.WriteLine($"Report: {analysis.Units.Count} units, {analysis.Units.Sum(u => u.Symbols.Count)} symbols, " +
                          $"{analysis.Markers.Count} markers, {analysis.Results.Count} test results; {written.Count} files updated.");
        PrintProblems(analysis);
        return 0;
    }

    private static int Sync(RepoLayout layout, string? results)
    {
        var analysis = PortAnalysis.Run(layout, results);
        var changed = new HeaderSync(analysis).Run(write: true);
        foreach (string file in changed)
        {
            Console.WriteLine($"updated {file}");
        }

        Console.WriteLine($"Sync: {changed.Count} files updated.");
        return 0;
    }

    private static int Check(RepoLayout layout, string? results)
    {
        var analysis = PortAnalysis.Run(layout, results);
        foreach (string file in new HeaderSync(analysis).Run(write: false))
        {
            analysis.Problems.Add(new Problem(Severity.Error, file, "port links are out of date; run the sync command"));
        }

        PrintProblems(analysis);
        int errors = analysis.Problems.Count(p => p.Severity == Severity.Error);
        int warnings = analysis.Problems.Count - errors;
        Console.WriteLine($"Check: {analysis.Units.Count} units, {analysis.Markers.Count} markers, {analysis.TestMethodCount} test methods; " +
                          $"{errors} errors, {warnings} warnings.");
        return errors == 0 ? 0 : 1;
    }

    private static int Summary(RepoLayout layout, string? results, string? output)
    {
        var analysis = PortAnalysis.Run(layout, results);
        string summary = new ReportWriter(analysis).Summary();
        if (output is null)
        {
            Console.Write(summary);
        }
        else
        {
            File.AppendAllText(output, summary, new UTF8Encoding(false));
        }

        return 0;
    }

    private static int Inventory(RepoLayout layout, Dictionary<string, string?> options)
    {
        var inventory = DelphiInventory.Load(layout);
        var symbols = inventory.Units.SelectMany(u => u.Symbols).ToList();
        Console.WriteLine($"{inventory.Units.Count} units, {inventory.Units.Sum(u => u.Lines)} lines, {symbols.Count} symbols:");
        foreach (var kind in symbols.GroupBy(s => s.KindText).OrderByDescending(g => g.Count()))
        {
            Console.WriteLine($"  {kind.Key,-16} {kind.Count(),6}");
        }

        var warnings = inventory.Warnings.Concat(inventory.Units.SelectMany(u => u.Warnings)).ToList();
        Console.WriteLine($"{warnings.Count} parse warnings{(options.ContainsKey("--warnings") ? ":" : " (list them with --warnings)")}");
        if (options.ContainsKey("--warnings"))
        {
            foreach (string warning in warnings)
            {
                Console.WriteLine($"  {warning}");
            }
        }

        if (options.GetValueOrDefault("--json") is { } json)
        {
            var data = inventory.Units.Select(u => new
            {
                unit = u.Name,
                file = u.File,
                lines = u.Lines,
                usesInterface = u.UsesInterface,
                usesImplementation = u.UsesImplementation,
                symbols = u.Symbols.Select(s => new
                {
                    key = s.Key,
                    kind = s.KindText,
                    line = s.Line,
                    file = s.File == u.File ? null : s.File,
                    implLine = s.ImplLine,
                    visibility = string.IsNullOrEmpty(s.Visibility) ? null : s.Visibility,
                    inInterface = s.InInterface,
                    accessorOf = s.AccessorOf?.Key,
                    isAbstract = s.IsAbstract ? true : (bool?)null,
                }),
            });
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            };
            File.WriteAllText(json, JsonSerializer.Serialize(data, jsonOptions));
            Console.WriteLine($"wrote {json}");
        }

        return 0;
    }

    private static int Fixtures(RepoLayout layout, string gitRef)
    {
        var extractor = new FixtureExtractor(layout, gitRef);
        List<ModelSource> sources;
        try
        {
            sources = extractor.FindSources();
        }
        catch (InvalidOperationException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }

        var models = extractor.Build(sources, extractor.RepositoryUrl());
        var written = extractor.Write(models);
        foreach (string warning in extractor.Warnings)
        {
            Console.Error.WriteLine($"warning: {warning}");
        }

        foreach (string file in written)
        {
            Console.WriteLine($"updated {file}");
        }

        Console.WriteLine($"Fixtures: {sources.Count} model sources on {gitRef}, {models.Count} distinct models, {written.Count} files updated.");
        return 0;
    }
}
