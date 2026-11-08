using System.Collections.Immutable;
using Bold.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bold.Tests;

/// <summary>The attribute generator run on sources, as a build runs it: what it reports, what it writes, and whether that compiles.</summary>
internal sealed class GeneratorRun
{
    private GeneratorRun(ImmutableArray<Diagnostic> generatorDiagnostics, IReadOnlyDictionary<string, string> sources, ImmutableArray<Diagnostic> compilationDiagnostics)
    {
        GeneratorDiagnostics = generatorDiagnostics;
        Sources = sources;
        CompilationDiagnostics = compilationDiagnostics;
    }

    /// <summary>What the generator reported.</summary>
    public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

    /// <summary>What the generator wrote, by file name.</summary>
    public IReadOnlyDictionary<string, string> Sources { get; }

    /// <summary>The errors of compiling the sources with what the generator wrote.</summary>
    public ImmutableArray<Diagnostic> CompilationDiagnostics { get; }

    /// <summary>The ids of what the generator reported.</summary>
    public string[] Ids => [.. GeneratorDiagnostics.Select(d => d.Id)];

    /// <summary>The source the generator wrote for the class.</summary>
    public string Source(string className) => Sources.Single(s => s.Key.EndsWith("." + className + ".BoldAttribute.g.cs", StringComparison.Ordinal)
        || s.Key == className + ".BoldAttribute.g.cs").Value;

    /// <summary>Runs the generator on the source (with "using Bold;" in front), and compiles the result.</summary>
    public static GeneratorRun Of(string source, bool withPersistence = true)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText("using Bold;\n" + source, new CSharpParseOptions(LanguageVersion.Preview), "Source.cs");
        CSharpCompilation compilation = CSharpCompilation.Create("Generated", [tree], References(withPersistence),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BoldAttributeGenerator()).WithUpdatedParseOptions((CSharpParseOptions)tree.Options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> diagnostics);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var sources = result.Results.SelectMany(r => r.GeneratedSources).ToDictionary(s => s.HintName, s => s.SourceText.ToString());
        ImmutableArray<Diagnostic> errors = [.. output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];
        return new GeneratorRun([.. diagnostics.Concat(result.Diagnostics).Distinct()], sources, errors);
    }

    private static IEnumerable<MetadataReference> References(bool withPersistence)
    {
        // the framework's assemblies; Bold's are added as asked
        string trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        string own = Path.GetFullPath(AppContext.BaseDirectory);
        foreach (string path in trusted.Split(Path.PathSeparator).Where(p => !Path.GetFullPath(p).StartsWith(own, StringComparison.OrdinalIgnoreCase)))
        {
            yield return MetadataReference.CreateFromFile(path);
        }

        var types = new List<Type> { typeof(TBoldObject), typeof(TBoldElement), typeof(TMoldModel), typeof(BoldDefs), typeof(SysUtils), typeof(IBoldValueSpace), typeof(DelphiAttribute) };
        if (withPersistence)
        {
            types.Add(typeof(TBoldMemberDefaultMapper));
            types.Add(typeof(TBoldPersistenceController));
        }

        foreach (string location in types.Select(t => t.Assembly.Location).Distinct())
        {
            yield return MetadataReference.CreateFromFile(location);
        }
    }
}
