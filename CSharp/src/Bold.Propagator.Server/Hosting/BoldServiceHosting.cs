using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
#if BOLD_SERVICE_HOSTING
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;
#endif

namespace Bold;

/// <summary>
/// Running a Bold server (the propagator server, the remote persistence server) as a Windows service or a systemd
/// service, as well as from a console or in a container. Builds without the hosting packages (offline builds) run
/// from a console only.
/// </summary>
public static class BoldServiceHosting
{
    /// <summary>True when the program was started by the Windows service manager.</summary>
    public static bool IsWindowsService
    {
        get
        {
#if BOLD_SERVICE_HOSTING
            return WindowsServiceHelpers.IsWindowsService();
#else
            return false;
#endif
        }
    }

    /// <summary>True when the program was started by systemd.</summary>
    public static bool IsSystemdService
    {
        get
        {
#if BOLD_SERVICE_HOSTING
            return SystemdHelpers.IsSystemdService();
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// The options of a server's web application. Under a service manager the content root (where appsettings.json is
    /// read and relative paths in the settings start) is the program's folder; a Windows service also gets the
    /// program's folder as its current folder, since it would start in the system folder.
    /// </summary>
    public static WebApplicationOptions Options(string[] Args)
    {
        if (IsWindowsService)
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
        }

        return IsWindowsService || IsSystemdService
            ? new WebApplicationOptions { Args = Args, ContentRootPath = AppContext.BaseDirectory }
            : new WebApplicationOptions { Args = Args };
    }

    /// <summary>
    /// Lets the host run as a Windows service named <paramref name="ServiceName"/> (stopped and started by the service
    /// manager, logging to the event log) or as a systemd service (telling systemd when it is ready); a host started
    /// otherwise is not affected.
    /// </summary>
    public static IServiceCollection AddBoldServiceHosting(this IServiceCollection Services, string ServiceName)
    {
#if BOLD_SERVICE_HOSTING
        Services.AddWindowsService(options => options.ServiceName = ServiceName);
        Services.AddSystemd();
#else
        _ = ServiceName;
#endif
        return Services;
    }
}
