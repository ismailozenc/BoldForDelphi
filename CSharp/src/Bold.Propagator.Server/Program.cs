// The propagator and lock manager server: the propagator server (configured by Bold.Propagator.Server.ini next to the
// program, see TBoldPropagatorServer) with its SignalR hub at /propagator. The address comes from the usual ASP.NET
// Core settings, for example --urls http://0.0.0.0:5050 or ASPNETCORE_URLS.
using Bold;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddBoldPropagator();
WebApplication app = builder.Build();
app.MapBoldPropagator();
await app.RunAsync();
