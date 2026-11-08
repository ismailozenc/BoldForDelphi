// The propagator and lock manager server: the propagator server (configured by Bold.Propagator.Server.ini next to the
// program, see TBoldPropagatorServer) with its SignalR hub at /propagator, and /health for load balancers and
// orchestrators. The address comes from the usual ASP.NET Core settings, for example --urls http://0.0.0.0:5050 or
// ASPNETCORE_URLS. It runs from a console, in a container, or as a Windows or systemd service (BoldServiceHosting).
using Bold;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(BoldServiceHosting.Options(args));
builder.Services.AddBoldServiceHosting("BoldPropagator");
builder.Services.AddBoldPropagator();
builder.Services.AddHealthChecks();
WebApplication app = builder.Build();
app.MapBoldPropagator();
app.MapHealthChecks("/health");
await app.RunAsync();
