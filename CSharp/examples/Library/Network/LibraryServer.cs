// The library's server: an ASP.NET Core host with the database behind remote persistence (/persistence) and the
// propagator (/propagator), as Bold.Remote.Server runs them. The desks have no database of their own.
using Bold;
using LibraryClasses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Library.Network;

/// <summary>The server: the persistence of a SQLite database, and the propagator that tells the desks what changed.</summary>
public sealed class LibraryServer : IDisposable
{
    private readonly TBoldRemotePersistenceSetup fSetup;
    private readonly WebApplication fApp;

    /// <summary>
    /// Starts a server at <paramref name="url"/> (port 0: a free port) on the database, which it creates when it has no
    /// tables yet; <paramref name="newDatabase"/> starts from a new database.
    /// </summary>
    public LibraryServer(string databaseFile, string url = "http://127.0.0.1:0", bool newDatabase = true)
    {
        // The persistence: the model, the database and, in front of it, a snooper that tells the propagator what is saved.
        var model = new TBoldAbstractModel { Name = "Library" };
        model.ReadModel(LibraryModel.Lines());
        if (newDatabase)
        {
            File.Delete(databaseFile);
        }

        fSetup = new TBoldRemotePersistenceSetup(model, new SqliteConnection($"Data Source={databaseFile}"), TBoldDataBaseEngine.dbeSQLite,
            WithPropagator: true);
        if (!fSetup.HasSchema())
        {
            fSetup.DatabaseHandle.CreateDataBaseSchema();
        }

        fSetup.Open();

        // The host: the propagator server and its hub, and the persistence endpoint.
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(url);
        builder.Logging.ClearProviders();
        builder.Services.AddBoldPropagator();
        fApp = builder.Build();
        fApp.MapBoldPersistence(fSetup.PersistenceHandle, fSetup.Model.MoldModel);
        fApp.MapBoldPropagator();
        fApp.Lifetime.ApplicationStarted.Register(fSetup.ConnectToPropagator);
        Task.Run(() => fApp.StartAsync()).GetAwaiter().GetResult();
        Url = fApp.Urls.First();
    }

    /// <summary>Where the server listens, such as http://127.0.0.1:5000.</summary>
    public string Url { get; }

    public void Dispose()
    {
        Task.Run(async () =>
        {
            await fApp.StopAsync();
            await fApp.DisposeAsync();
        }).GetAwaiter().GetResult();
        fSetup.Dispose();
    }
}
