// Delphi: Source/Propagator/Enterprise/BoldPropagatorApplication.pas
// Port map: CSharp/port/units/BoldPropagatorApplication.md

namespace Bold;

/// <summary>Starts and stops the propagator server of a program (Delphi did it in the unit's initialization and finalization).</summary>
public static class BoldPropagatorApplication
{
    /// <summary>Starts the propagator server; a failure is reported and rethrown.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "Called by the program; Delphi showed the error and terminated the application.")]
    public static void Initialization()
    {
        try
        {
            TBoldPropagatorServer.Instance().Initialize();
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError("%s", E.Message);
            throw;
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "finalization", Note = "Called by the program.")]
    public static void Finalization() => TBoldPropagatorServer.FreeSingleton();
}
