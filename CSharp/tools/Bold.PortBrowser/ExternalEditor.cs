using System.Diagnostics;

namespace Bold.PortBrowser;

/// <summary>Opens a file outside the browser: in Visual Studio at a line, or in the program Windows has for it.</summary>
public static class ExternalEditor
{
    /// <summary>devenv.exe of the newest Visual Studio, or null when there is none.</summary>
    public static string? VisualStudio()
    {
        string vswhere = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft Visual Studio", "Installer", "vswhere.exe");
        if (!File.Exists(vswhere))
        {
            return null;
        }

        try
        {
            var info = new ProcessStartInfo(vswhere, "-latest -prerelease -products * -property productPath")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            using Process? process = Process.Start(info);
            string? path = process?.StandardOutput.ReadLine();
            process?.WaitForExit(5000);
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path) ? path : null;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Opens the file in Visual Studio (in the one that is running, when one is) and goes to the line; without Visual
    /// Studio, in the program Windows opens such files with. Returns what went wrong, or null.
    /// </summary>
    public static string? Open(string file, int line)
    {
        try
        {
            string? devenv = VisualStudio();
            if (devenv is not null)
            {
                string arguments = $"/Edit \"{file}\"" + (line > 1 ? $" /Command \"Edit.GoTo {line}\"" : "");
                Process.Start(new ProcessStartInfo(devenv, arguments) { UseShellExecute = false })?.Dispose();
            }
            else
            {
                Process.Start(new ProcessStartInfo(file) { UseShellExecute = true })?.Dispose();
            }

            return null;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            return e.Message;
        }
    }

    /// <summary>Opens a file or a folder with the program Windows has for it. Returns what went wrong, or null.</summary>
    public static string? Shell(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true })?.Dispose();
            return null;
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or IOException or InvalidOperationException)
        {
            return e.Message;
        }
    }
}
