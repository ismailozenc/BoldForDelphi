// Bold's programs, which come with the extension in its tools folder: where they are and how they are started.
// Nothing here needs Visual Studio, so it can be used and tested without it.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Bold.VisualStudio;

/// <summary>Finds and starts the programs in the extension's tools folder.</summary>
public static class BoldTools
{
    /// <summary>The extension of a Bold model file.</summary>
    public const string ModelExtension = ".bld";

    /// <summary>
    /// The folder with Bold's programs: "tools" next to the extension, or the folder the environment variable BOLD_TOOLS
    /// names (for trying the extension on programs built elsewhere).
    /// </summary>
    public static string Folder
    {
        get
        {
            string? given = Environment.GetEnvironmentVariable("BOLD_TOOLS");
            return !string.IsNullOrEmpty(given)
                ? given!
                : Path.Combine(Path.GetDirectoryName(typeof(BoldTools).Assembly.Location) ?? ".", "tools");
        }
    }

    public static string ModelEditor => Path.Combine(Folder, "model-editor", "Bold.ModelEditor.exe");

    public static string PropagatorAnalyzer => Path.Combine(Folder, "propagator-analyzer", "Bold.PropagatorAnalyzer.exe");

    public static string PortBrowser => Path.Combine(Folder, "port-browser", "Bold.PortBrowser.exe");

    /// <summary>bold-codegen, as the library "dotnet" runs.</summary>
    public static string CodeGenerator => Path.Combine(Folder, "codegen", "bold-codegen.dll");

    public static string UserGuide => Path.Combine(Folder, "docs", "USER_GUIDE.md");

    public static string Tutorials => Path.Combine(Folder, "docs", "TUTORIALS.md");

    /// <summary>Whether the file is a Bold model, by its extension.</summary>
    public static bool IsModel(string? file) =>
        !string.IsNullOrEmpty(file) && string.Equals(Path.GetExtension(file), ModelExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>Starts the model editor, on the model when one is given.</summary>
    public static Process OpenModelEditor(string? model = null) =>
        Start(ModelEditor, model is null ? Array.Empty<string>() : new[] { model }, model is null ? null : Path.GetDirectoryName(model));

    public static Process OpenPropagatorAnalyzer() => Start(PropagatorAnalyzer, Array.Empty<string>(), null);

    /// <summary>Whether the file is Delphi or C# source, which the port browser shows with its counterpart.</summary>
    public static bool IsSource(string? file)
    {
        string extension = string.IsNullOrEmpty(file) ? "" : Path.GetExtension(file);
        return string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".pas", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The port browser's arguments: the repository (a folder in it will do; the browser finds it from the file when
    /// none is given), and the file and line to start at.
    /// </summary>
    public static IReadOnlyList<string> PortBrowserArguments(string? repository, string? file, int line)
    {
        var arguments = new List<string>();
        if (!string.IsNullOrEmpty(repository))
        {
            arguments.Add(repository!);
        }

        if (!string.IsNullOrEmpty(file))
        {
            arguments.Add("--open");
            arguments.Add(file!);
            if (line > 0)
            {
                arguments.Add("--line");
                arguments.Add(line.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        return arguments;
    }

    /// <summary>Starts the port browser, at the file and the line when they are given.</summary>
    public static Process OpenPortBrowser(string? repository, string? file = null, int line = 0) =>
        Start(PortBrowser, PortBrowserArguments(repository, file, line), null);

    /// <summary>The folder bold-codegen writes a model's classes to: the model's own.</summary>
    public static string ClassesFolder(string model) => Path.GetDirectoryName(Path.GetFullPath(model)) ?? ".";

    /// <summary>
    /// The arguments of "dotnet" that generate the classes of the model into its folder. A file named as the model with
    /// the extension .typenames, next to it, gives the type names of the model's custom attribute types.
    /// </summary>
    public static IReadOnlyList<string> GenerateClassesArguments(string model)
    {
        string full = Path.GetFullPath(model);
        var arguments = new List<string> { CodeGenerator, full, "--out", ClassesFolder(full) };
        string typeNames = Path.ChangeExtension(full, ".typenames");
        if (File.Exists(typeNames))
        {
            arguments.Add("--typenames");
            arguments.Add(typeNames);
        }

        return arguments;
    }

    /// <summary>Runs bold-codegen on the model and waits for it; what it wrote goes to <paramref name="output"/>. Returns its exit code.</summary>
    public static int GenerateClasses(string model, Action<string> output)
    {
        Require(CodeGenerator);
        var info = new ProcessStartInfo("dotnet", Quote(GenerateClassesArguments(model)))
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = ClassesFolder(model),
        };
        using var process = new Process { StartInfo = info };
        process.OutputDataReceived += (_, e) => Tell(e.Data);
        process.ErrorDataReceived += (_, e) => Tell(e.Data);
        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            throw new InvalidOperationException("The .NET SDK or runtime is not installed (dotnet could not be started): " + e.Message, e);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        return process.ExitCode;

        void Tell(string? line)
        {
            if (line is not null)
            {
                output(line);
            }
        }
    }

    private static Process Start(string program, IReadOnlyList<string> arguments, string? workingFolder)
    {
        Require(program);
        var info = new ProcessStartInfo(program, Quote(arguments)) { UseShellExecute = false };
        if (!string.IsNullOrEmpty(workingFolder) && Directory.Exists(workingFolder))
        {
            info.WorkingDirectory = workingFolder;
        }

        return Process.Start(info) ?? throw new InvalidOperationException($"{Path.GetFileName(program)} did not start.");
    }

    private static void Require(string file)
    {
        if (!File.Exists(file))
        {
            throw new FileNotFoundException(
                $"{Path.GetFileName(file)} is not in the extension's tools folder ({Folder}). Install the extension again with CSharp/vs/build.ps1 -Install.", file);
        }
    }

    /// <summary>The arguments as one command line, each quoted as the Windows C runtime reads it.</summary>
    public static string Quote(IEnumerable<string> arguments)
    {
        var line = new StringBuilder();
        foreach (string argument in arguments)
        {
            if (line.Length > 0)
            {
                line.Append(' ');
            }

            if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
            {
                line.Append(argument);
                continue;
            }

            line.Append('"');
            int backslashes = 0;
            foreach (char c in argument)
            {
                if (c == '\\')
                {
                    backslashes++;
                }
                else if (c == '"')
                {
                    line.Append('\\', backslashes * 2 + 1).Append('"');
                    backslashes = 0;
                }
                else
                {
                    line.Append('\\', backslashes).Append(c);
                    backslashes = 0;
                }
            }

            line.Append('\\', backslashes * 2).Append('"');
        }

        return line.ToString();
    }
}
