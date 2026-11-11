namespace Bold.PortBrowser;

/// <summary>
/// The command line: Bold.PortBrowser [repository] [--open file] [--line n] [--symbol Name] [--results folder] [--close].
/// </summary>
public sealed class Options
{
    /// <summary>The repository (a folder with Source and CSharp), or a folder inside it.</summary>
    public string? Repository { get; private set; }

    /// <summary>A .pas or .cs file to start at.</summary>
    public string? Open { get; private set; }

    /// <summary>The line of <see cref="Open"/> to start at.</summary>
    public int Line { get; private set; }

    /// <summary>A name to start at: "TBoldSystem" or "TBoldSystem.UpdateDatabase".</summary>
    public string? Symbol { get; private set; }

    /// <summary>The folder with test results; CSharp/TestResults of the repository when not given.</summary>
    public string? Results { get; private set; }

    /// <summary>For the tests: the window closes itself once it has shown where it was told to start, and the exit code says whether both sides showed code.</summary>
    public bool Close { get; private set; }

    public static Options Parse(string[] args)
    {
        var options = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string? Value() => i + 1 < args.Length ? args[++i] : null;
            switch (args[i])
            {
                case "--open":
                    options.Open = Value();
                    break;
                case "--line":
                    options.Line = int.TryParse(Value(), out int line) ? line : 0;
                    break;
                case "--symbol":
                    options.Symbol = Value();
                    break;
                case "--results":
                    options.Results = Value();
                    break;
                case "--close":
                    options.Close = true;
                    break;
                default:
                    if (!args[i].StartsWith("--", StringComparison.Ordinal))
                    {
                        options.Repository ??= args[i];
                    }

                    break;
            }
        }

        return options;
    }
}
