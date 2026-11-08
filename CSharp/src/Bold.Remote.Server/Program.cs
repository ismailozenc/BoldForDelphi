// The remote persistence server: the persistence configured in the "Bold" section of appsettings.json (see
// TBoldRemotePersistenceSetup) at /persistence, and, when Bold:Propagator is true, the propagator with its hub at
// /propagator on the same host (decision D8); /health answers load balancers and orchestrators. The address comes from
// the usual ASP.NET Core settings, such as --urls. It runs from a console, in a container, or as a Windows or systemd
// service (BoldServiceHosting); a relative Bold:Model is found in the content root.
using Bold;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(BoldServiceHosting.Options(args));
builder.Services.AddBoldServiceHosting("BoldRemotePersistence");
using TBoldRemotePersistenceSetup setup = TBoldRemotePersistenceSetup.FromConfiguration(builder.Configuration.GetSection("Bold"),
    builder.Environment.ContentRootPath);
if (setup.WithPropagator)
{
    builder.Services.AddBoldPropagator();
}

builder.Services.AddHealthChecks();
WebApplication app = builder.Build();
app.MapBoldPersistence(setup.PersistenceHandle, setup.Model.MoldModel);
if (setup.WithPropagator)
{
    app.MapBoldPropagator();
    app.Lifetime.ApplicationStarted.Register(setup.ConnectToPropagator);
}

app.MapHealthChecks("/health");
await app.RunAsync();
