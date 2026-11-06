using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Bold;

/// <summary>
/// Remote persistence on ASP.NET Core: a POST endpoint that runs the requests of
/// <see cref="TBoldHttpPersistenceControllerProxy"/> on a persistence handle's controller, where Delphi Bold had its
/// SOAP server persistence handles.
/// </summary>
public static class BoldRemotePersistenceHosting
{
    /// <summary>Where the endpoint is mapped by default.</summary>
    public const string DefaultPath = "/persistence";

    /// <summary>
    /// Maps the endpoint at <paramref name="Pattern"/>: requests run, one at a time, on the controller of
    /// <paramref name="PersistenceHandle"/> (a database handle, or a snooper in front of one), streamed by <paramref name="Model"/>.
    /// </summary>
    public static IEndpointConventionBuilder MapBoldPersistence(this IEndpointRouteBuilder Endpoints, TBoldPersistenceHandle PersistenceHandle,
        TMoldModel Model, string Pattern = DefaultPath)
    {
        var core = new TBoldRemotePersistenceAdapterCore(Model);
        return Endpoints.MapPost(Pattern, async (HttpContext context) =>
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            string request = await reader.ReadToEndAsync(context.RequestAborted);
            string reply;
            lock (PersistenceHandle)
            {
                // The handle opens (its database too) at the first request, if the host did not open it.
                PersistenceHandle.Active = true;
                reply = core.Get(request, PersistenceHandle.PersistenceController);
            }

            context.Response.ContentType = TBoldHttpPersistenceControllerProxy.ContentType + "; charset=utf-8";
            await context.Response.WriteAsync(reply, Encoding.UTF8, context.RequestAborted);
        });
    }
}
