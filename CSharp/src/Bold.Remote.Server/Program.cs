// The remote persistence server: the persistence configured in the "Bold" section of appsettings.json (see
// TBoldRemotePersistenceSetup) at /persistence, and, when Bold:Propagator is true, the propagator with its hub at
// /propagator on the same host (decision D8). The address comes from the usual ASP.NET Core settings, such as --urls.
using Bold;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
using TBoldRemotePersistenceSetup setup = TBoldRemotePersistenceSetup.FromConfiguration(builder.Configuration.GetSection("Bold"));
if (setup.WithPropagator)
{
    builder.Services.AddBoldPropagator();
}

WebApplication app = builder.Build();
app.MapBoldPersistence(setup.PersistenceHandle, setup.Model.MoldModel);
if (setup.WithPropagator)
{
    app.MapBoldPropagator();
    app.Lifetime.ApplicationStarted.Register(setup.ConnectToPropagator);
}

await app.RunAsync();
