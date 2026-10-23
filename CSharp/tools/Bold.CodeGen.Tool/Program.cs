// bold-codegen: generates typed classes from a Bold model file (.bld).
using Bold;

const string Usage = """
    bold-codegen <model.bld> [--out <folder>] [--language csharp|delphi|cpp] [--untyped-lists] [--quiet]

    Generates the classes of the model into the folder (default: the model's folder). C#: <Class>.g.cs for each
    class and <Component>.g.cs for each component (always written again), and <Class>.cs with the stubs of the
    methods the model declares (only ever added to). Delphi and C++: the units and include files of Delphi Bold.
    """;

string? model = null, folder = null, language = "csharp";
bool typedLists = true, quiet = false;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--out" when i + 1 < args.Length:
            folder = args[++i];
            break;
        case "--language" when i + 1 < args.Length:
            language = args[++i].ToLowerInvariant();
            break;
        case "--untyped-lists":
            typedLists = false;
            break;
        case "--quiet":
            quiet = true;
            break;
        case "-h" or "--help":
            Console.WriteLine(Usage);
            return 0;
        default:
            if (args[i].StartsWith('-') || model is not null)
            {
                Console.Error.WriteLine($"Unknown argument {args[i]}");
                Console.Error.WriteLine(Usage);
                return 2;
            }

            model = args[i];
            break;
    }
}

if (model is null || !File.Exists(model))
{
    Console.Error.WriteLine(model is null ? "No model file given." : $"No such file: {model}");
    Console.Error.WriteLine(Usage);
    return 2;
}

folder = Path.GetFullPath(folder ?? Path.GetDirectoryName(Path.GetFullPath(model))!);
Directory.CreateDirectory(folder);
TMoldModel moldModel = TMoldBLDRW.ModelFromFile(model);
var types = new TBoldTypeNameDictionary(null);
types.AddDefaultMappings();
moldModel.TypeNameDictionary = types;
if (!quiet)
{
    BoldLogHandler.BoldLog().RegisterLogReceiver(new ConsoleLog());
}

string basePath = folder + Path.DirectorySeparatorChar;
switch (language)
{
    case "csharp" or "c#":
        using (var generator = new TBoldCSharpGenerator(types) { MoldModel = moldModel, BaseFilePath = basePath, UseTypedLists = typedLists })
        {
            generator.Generate();
        }

        break;
    case "delphi" or "cpp":
        if (language == "delphi")
        {
            TBoldDelphiTemplateManager.InstallTemplates();
        }
        else
        {
            TBoldCPPTemplateManager.InstallCPPTemplates();
        }

        using (var generator = new TBoldGenerator(types) { MoldModel = moldModel, BaseFilePath = basePath, UseTypedLists = typedLists })
        {
            generator.GenerateBusinessObjectCode();
            generator.EnsureMethodImplementations();
        }

        break;
    default:
        Console.Error.WriteLine($"Unknown language {language}");
        return 2;
}

return 0;

/// <summary>Writes the generator's log to the console.</summary>
internal sealed class ConsoleLog : IBoldLogReceiver
{
    public void SetProgress(int Value) { }
    public void SetLogHeader(string Value) { }
    public void SetProgressMax(int Value) { }
    public void Clear() { }
    public void Hide() { }
    public void Log(string s, TBoldLogType LogType) => (LogType is TBoldLogType.ltError or TBoldLogType.ltWarning ? Console.Error : Console.Out).WriteLine(s);
    public void ProgressStep() { }
    public void Show() { }
    public void StartLog(string SessionName) { }
    public void EndLog() { }
    public void Sync() { }
    public void ProcessInterruption() { }
}
