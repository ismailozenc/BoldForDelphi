// The propagator server program (CSharp/src/Bold.Propagator.Server/Program.cs), started as a user starts it.
using System.Diagnostics;

namespace Bold.Tests;

/// <summary>The propagator server program starts, serves its hub to client handles, and is stopped.</summary>
[NonParallelizable]
public class PropagatorServerProgramTests
{
    /// <summary>A lock manager admin handle connected to the hub, or null while the hub is not there yet.</summary>
    private static TBoldSignalRLockManagerAdminHandle? TryConnect(string hubUrl)
    {
        var admin = new TBoldSignalRLockManagerAdminHandle { Url = hubUrl, TimeOut = 5000 };
        try
        {
            admin.Connected = true;
            return admin;
        }
        catch (Exception)
        {
            admin.Dispose();
            return null;
        }
    }

    [Test]
    public void The_propagator_server_program_serves_its_hub()
    {
        string program = Programs.Require("src/Bold.Propagator.Server", "Bold.Propagator.Server");
        string url = $"http://127.0.0.1:{Programs.FreePort()}";
        Process process = Programs.Start(program, "--urls", url);
        TBoldSignalRLockManagerAdminHandle? admin = null;
        try
        {
            string hubUrl = url + BoldPropagatorHubContract.DefaultPath;
            Programs.WaitUntil(() => (admin = TryConnect(hubUrl)) is not null, TimeSpan.FromSeconds(60), process);
            using var lockManager = new TBoldSignalRLockManagerHandle { Url = hubUrl, TimeOut = 5000 };
            lockManager.Connected = true;
            Assert.That(lockManager.LockManager!.EnsureLocks(1, null, null), Is.True, "the lock manager answers");
            Assert.That(admin!.LockManagerAdmin!.ListAllClients(out _), Is.EqualTo(Windows.S_OK), "the lock manager's administration answers");
        }
        finally
        {
            admin?.Dispose();
            Programs.Stop(process);
        }
    }
}
