using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bold.Tests;

/// <summary>A remote persistence server in the test process, on a free port: a persistence handle at /persistence, and the propagator hub when asked.</summary>
internal sealed class RemoteServerSample : IDisposable
{
    private WebApplication? fApp;

    public RemoteServerSample(TBoldPersistenceHandle PersistenceHandle, TMoldModel Model, bool withPropagatorHub = false)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        if (withPropagatorHub)
        {
            builder.Services.AddBoldPropagatorHub();
        }

        WebApplication app = builder.Build();
        app.MapBoldPersistence(PersistenceHandle, Model);
        if (withPropagatorHub)
        {
            app.MapBoldPropagator();
        }

        // Not on the test thread's synchronization context, which only the test pumps.
        Task.Run(() => app.StartAsync()).GetAwaiter().GetResult();
        fApp = app;
        BaseUrl = app.Urls.First();
    }

    public string BaseUrl { get; }

    public string PersistenceUrl => BaseUrl + BoldRemotePersistenceHosting.DefaultPath;

    public string PropagatorUrl => BaseUrl + BoldPropagatorHubContract.DefaultPath;

    /// <summary>A controller for the server, as a client application has.</summary>
    public TBoldHttpPersistenceControllerProxy NewController(TMoldModel Model) => new(Model, PersistenceUrl);

    public void Stop()
    {
        if (fApp is { } app)
        {
            fApp = null;
            Task.Run(async () =>
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }).GetAwaiter().GetResult();
        }
    }

    public void Dispose() => Stop();
}
