// Delphi: Source/Propagator/Enterprise/BoldServicePropagatorUnit.pas
// Port map: CSharp/port/units/BoldServicePropagatorUnit.md
using Microsoft.Extensions.Hosting;
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldServicePropagatorUnit", Symbol = "BoldPropagatorSvc", Note = "The host creates the service.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldServicePropagatorUnit", Symbol = "ServiceController", Note = "Windows service control; the host handles it.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldServicePropagatorUnit", Symbol = "TBoldPropagatorSvc.GetServiceController", Note = "Windows service control; the host handles it.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldServicePropagatorUnit", Symbol = "TBoldPropagatorSvc.ServiceAfterInstall", Note = "Registered the COM server as a service.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldServicePropagatorUnit", Symbol = "TBoldPropagatorSvc.ServiceAfterUninstall", Note = "Unregistered the COM server as a service.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldServicePropagatorUnit", Symbol = "TBoldPropagatorSvc.getAppId", Note = "COM application id.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldServicePropagatorUnit", Symbol = "TBoldPropagatorSvc.AppID", Note = "COM application id.")]

namespace Bold;

/// <summary>
/// The propagator as a service of a .NET host (a Worker Service, or the web host of the propagator hub): it starts the
/// propagator server when the host starts and frees it when the host stops. Delphi's was a Windows service.
/// </summary>
[Delphi(PortStatus.Replaced, Note = "An IHostedService where Delphi had a TService; run the host as a Windows service or a systemd unit to have a service.")]
public class TBoldPropagatorSvc : IHostedService
{
    [Delphi(PortStatus.Replaced, Symbol = "TBoldPropagatorSvc.ServiceStart", Note = "Starting the hosted service starts the propagator server.")]
    [Delphi(PortStatus.Replaced, Symbol = "initialization", Note = "The server starts with the hosted service.")]
    public Task StartAsync(CancellationToken cancellationToken)
    {
        TBoldPropagatorServer.Instance().Initialize();
        return Task.CompletedTask;
    }

    [Delphi(PortStatus.Replaced, Symbol = "finalization", Note = "The server is freed when the hosted service stops.")]
    public Task StopAsync(CancellationToken cancellationToken)
    {
        TBoldPropagatorServer.FreeSingleton();
        return Task.CompletedTask;
    }
}
