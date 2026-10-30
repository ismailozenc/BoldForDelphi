// Delphi: Source/Propagator/Common/BoldPropagatorGUIDs.pas
// Port map: CSharp/port/units/BoldPropagatorGUIDs.md

namespace Bold;

/// <summary>The class ids of the Delphi propagator's COM objects, kept for reference.</summary>
public static class BoldPropagatorGUIDs
{
    [Delphi(PortStatus.Ported)] public static readonly Guid CLSID_BOLDENQUEUER = new("7A091AB8-936C-4EB3-BCFC-AF889AAABC5E");
    [Delphi(PortStatus.Ported)] public static readonly Guid CLSID_BOLDCLIENTHANDLER = new("1E1641DF-69A2-4E04-8E24-069477976AF8");
    [Delphi(PortStatus.Ported)] public static readonly Guid BoldPropagatorConnection_CLSID = new("11C6E940-CEC6-45BA-873D-27854A82A023");
}
