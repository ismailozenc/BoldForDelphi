// The remote persistence server program (CSharp/src/Bold.Remote.Server/Program.cs), started as a user starts it.
using System.Diagnostics;
using System.Text;
using BuildingClasses;

namespace Bold.Tests;

/// <summary>The remote server program starts on its configuration, stores what a client application saves, and serves the propagator.</summary>
[NonParallelizable]
public class RemoteServerProgramTests
{
    /// <summary>A client application of the server: its model, a persistence handle for the server and a system.</summary>
    private sealed class Client : IDisposable
    {
        public readonly System.ComponentModel.Container Components = new();
        public readonly TBoldSystemHandle SystemHandle;

        public Client(string persistenceUrl)
        {
            var model = new TBoldAbstractModel(Components) { Name = "Building" };
            model.ReadModel(BuildingModel.Lines());
            var handle = new TBoldHttpClientPersistenceHandle(Components) { BoldModel = model, Url = persistenceUrl };
            var typeInfo = new TBoldSystemTypeInfoHandle(Components) { BoldModel = model };
            SystemHandle = new TBoldSystemHandle(Components) { SystemTypeInfoHandle = typeInfo, PersistenceHandle = handle };
            SystemHandle.Active = true;
        }

        public TBoldSystem System => SystemHandle.System!;

        public void Dispose()
        {
            if (SystemHandle.Active)
            {
                System.Discard();
                SystemHandle.Active = false;
            }

            Components.Dispose();
        }
    }

    [Test]
    public void The_remote_server_program_stores_what_a_client_saves()
    {
        string program = Programs.Require("src/Bold.Remote.Server", "Bold.Remote.Server");
        if (!File.Exists(Path.Combine(Path.GetDirectoryName(program)!, "Microsoft.Data.Sqlite.dll")))
        {
            Assert.Ignore("The server was built without its database providers (an offline build).");
        }

        string folder = Path.Combine(Path.GetTempPath(), $"remote-program-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string modelFile = Path.Combine(folder, "Building.bld");
        BuildingModel.Lines().SaveToFile(modelFile);
        string url = $"http://127.0.0.1:{Programs.FreePort()}";
        Process process = Programs.Start(program, "--urls", url, "--Bold:Model", modelFile, "--Bold:Provider", "Microsoft.Data.Sqlite",
            "--Bold:ConnectionString", $"Data Source={Path.Combine(folder, "Bold.db")};Pooling=False", "--Bold:Engine", "dbeSQLite",
            "--Bold:CreateSchema", "true", "--Bold:Propagator", "true");
        try
        {
            string persistenceUrl = url + BoldRemotePersistenceHosting.DefaultPath;
            using (var http = new HttpClient())
            {
                Programs.WaitUntil(() =>
                {
                    try
                    {
                        HttpResponseMessage response = http.PostAsync(persistenceUrl, new StringContent("<Body />", Encoding.UTF8, "application/xml")).GetAwaiter().GetResult();
                        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult().Contains("no operation");
                    }
                    catch (HttpRequestException)
                    {
                        return false;
                    }
                }, TimeSpan.FromSeconds(60), process);
            }

            using (var client = new Client(persistenceUrl))
            {
                _ = new TPerson(client.System) { FirstName = "Ann", LastName = "Archer" };
                client.System.UpdateDatabase();
            }

            using (var client = new Client(persistenceUrl))
            {
                TBoldObjectList persons = client.System.ClassByExpressionName("Person");
                persons.EnsureObjects();
                Assert.That(persons.Count, Is.EqualTo(1));
                Assert.That(((TPerson)persons[0]!).LastName, Is.EqualTo("Archer"), "another client reads what the first saved");
            }

            using var admin = new TBoldSignalRLockManagerAdminHandle { Url = url + BoldPropagatorHubContract.DefaultPath, TimeOut = 5000 };
            admin.Connected = true;
            Assert.That(admin.LockManagerAdmin!.ListAllClients(out _), Is.EqualTo(Windows.S_OK), "the propagator runs on the same host");
        }
        finally
        {
            Programs.Stop(process);
            try
            {
                Directory.Delete(folder, true);
            }
            catch (IOException)
            {
            }
        }
    }
}
