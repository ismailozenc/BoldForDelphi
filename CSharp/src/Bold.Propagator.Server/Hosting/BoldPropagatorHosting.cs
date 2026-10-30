using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bold;

/// <summary>
/// Hosting the propagator server on ASP.NET Core: its own host (Program.cs) or an application's, such as the remote
/// persistence server's (decision D8).
/// </summary>
public static class BoldPropagatorHosting
{
    /// <summary>Adds the propagator server, started and stopped with the host (<see cref="TBoldPropagatorSvc"/>), and its hub.</summary>
    public static IServiceCollection AddBoldPropagator(this IServiceCollection Services)
    {
        Services.AddHostedService<TBoldPropagatorSvc>();
        return Services.AddBoldPropagatorHub();
    }

    /// <summary>Adds only the hub, for a host that starts the propagator server itself (<see cref="TBoldPropagatorServer.Initialize"/>).</summary>
    public static IServiceCollection AddBoldPropagatorHub(this IServiceCollection Services)
    {
        Services.AddSignalR();
        return Services;
    }

    /// <summary>Maps the propagator hub at <paramref name="Pattern"/> (/propagator by default).</summary>
    public static HubEndpointConventionBuilder MapBoldPropagator(this IEndpointRouteBuilder Endpoints, string Pattern = BoldPropagatorHubContract.DefaultPath) =>
        Endpoints.MapHub<TBoldPropagatorHub>(Pattern);
}
