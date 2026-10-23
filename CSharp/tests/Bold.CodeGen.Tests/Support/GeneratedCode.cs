using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bold.Tests;

/// <summary>Generates C# code for a model into a folder, and compiles it with Roslyn against the Bold assemblies.</summary>
internal static class GeneratedCode
{
    /// <summary>The model of a fixture file, with the default type mappings.</summary>
    public static (TMoldModel model, TBoldTypeNameDictionary types) Load(string fixture)
    {
        TMoldModel model = TMoldBLDRW.ModelFromFile(FixturePaths.Model(fixture));
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        model.TypeNameDictionary = types;
        return (model, types);
    }

    /// <summary>Generates the model's C# code (classes, component files and method stubs) into the folder.</summary>
    public static void Generate(TMoldModel model, TBoldTypeNameDictionary types, string folder)
    {
        Directory.CreateDirectory(folder);
        using var generator = new TBoldCSharpGenerator(types) { MoldModel = model, BaseFilePath = folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar };
        generator.Generate();
    }

    private static IEnumerable<MetadataReference> References()
    {
        string trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        foreach (string path in trusted.Split(Path.PathSeparator))
        {
            yield return MetadataReference.CreateFromFile(path);
        }

        foreach (Type t in new[] { typeof(TBoldObject), typeof(TBoldElement), typeof(TMoldModel), typeof(BoldDefs), typeof(SysUtils), typeof(DelphiAttribute), typeof(IBoldValueSpace), typeof(TBoldPersistenceController) })
        {
            yield return MetadataReference.CreateFromFile(t.Assembly.Location);
        }
    }

    /// <summary>Compiles the files (and any extra source); returns the errors and the assembly image.</summary>
    public static (string[] errors, byte[] image) Compile(string assemblyName, IEnumerable<string> files, params string[] extraSources)
    {
        var options = new CSharpParseOptions(LanguageVersion.Latest);
        var trees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), options, f))
            .Concat(extraSources.Select(s => CSharpSyntaxTree.ParseText(s, options))).ToList();
        CSharpCompilation compilation = CSharpCompilation.Create(assemblyName, trees, References().DistinctBy(r => r.Display),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        string[] errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToArray();
        return (errors, stream.ToArray());
    }

    /// <summary>Loads a compiled image into the default context, so its types derive from the loaded Bold types.</summary>
    public static Assembly LoadImage(byte[] image) => AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(image));
}
