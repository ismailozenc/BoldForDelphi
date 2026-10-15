// Delphi: Source/FreestandingValueSpace/Core/FreeStandingValuesConst.pas
// Port map: CSharp/port/units/FreeStandingValuesConst.md

namespace Bold;

/// <summary>Messages of the free-standing value space.</summary>
public static partial class FreeStandingValuesConst
{
    [Delphi(PortStatus.Ported)] public const string sNoClassregisteredForName = "%s.CreateInstance: No freestanding class registered for name %s";
    [Delphi(PortStatus.Ported)] public const string sAbstractError = "%s.AssignContentValue: Abstract error";
    [Delphi(PortStatus.Ported)] public const string sUnknownTypeOfSource = "%s.AssignContentValue: unknown type of source";
}
