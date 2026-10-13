// Placeholder until M9: the propagator and lock manager server will run here as a Worker Service.
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
using var host = builder.Build();
await host.RunAsync();
