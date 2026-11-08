using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Bold.Tests;

/// <summary>
/// The programs of the port (the model editor, the servers, the tools, the examples) as a user starts them: their
/// executables where their projects build them, started as processes.
/// </summary>
public static class Programs
{
    /// <summary>
    /// The executable of a program: the project's apphost (Name.exe on Windows, Name elsewhere) in the project's bin
    /// folder, in the configuration the tests were built in; null when it was not built.
    /// </summary>
    /// <param name="projectFolder">The project's folder from CSharp, as "src/Bold.Propagator.Server".</param>
    /// <param name="name">The program's name, as "Bold.Propagator.Server" or "bold-codegen".</param>
    public static string? Find(string projectFolder, string name)
    {
        // tests/<project>/bin/<configuration>/<framework>
        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        string bin = Path.Combine(RepoPaths.Full("CSharp/" + projectFolder), "bin", configuration);
        if (!Directory.Exists(bin))
        {
            return null;
        }

        string file = OperatingSystem.IsWindows() ? name + ".exe" : name;
        return Directory.EnumerateDirectories(bin)
            .Select(framework => Path.Combine(framework, file))
            .Where(File.Exists)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    /// <summary>The program's executable; the test is inconclusive when it was not built (a Windows program elsewhere, say).</summary>
    public static string Require(string projectFolder, string name) =>
        Find(projectFolder, name) ?? throw new InconclusiveException($"{name} is not built in {projectFolder}");

    /// <summary>Starts the program with the arguments, its output read so that it never blocks on a full pipe.</summary>
    public static Process Start(string executable, params string[] arguments)
    {
        var info = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(executable)!,
        };
        foreach (string argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        var process = new Process { StartInfo = info };
        var output = new System.Text.StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            lock (output)
            {
                output.AppendLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            lock (output)
            {
                output.AppendLine(e.Data);
            }
        };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        Outputs.AddOrUpdate(process, output);
        return process;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Process, System.Text.StringBuilder> Outputs = [];

    /// <summary>What the program wrote so far, on its output and error.</summary>
    public static string OutputOf(Process process)
    {
        if (!Outputs.TryGetValue(process, out System.Text.StringBuilder? output))
        {
            return "";
        }

        lock (output)
        {
            return output.ToString();
        }
    }

    /// <summary>Ends the program (and what it started) if it still runs.</summary>
    public static void Stop(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(10_000);
            }
        }
        catch (InvalidOperationException)
        {
            // it ended meanwhile
        }

        process.Dispose();
    }

    /// <summary>A TCP port of this machine that is free now.</summary>
    public static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>Tries the action until it succeeds (the program is ready), for at most the time given.</summary>
    public static void WaitUntil(Func<bool> ready, TimeSpan timeout, Process process)
    {
        var watch = Stopwatch.StartNew();
        while (!ready())
        {
            if (process.HasExited)
            {
                Assert.Fail($"The program ended with {process.ExitCode}:\n{OutputOf(process)}");
            }

            if (watch.Elapsed > timeout)
            {
                Assert.Fail($"The program was not ready after {timeout.TotalSeconds} seconds:\n{OutputOf(process)}");
            }

            Thread.Sleep(200);
        }
    }
}
