// Delphi: Source/ValueSpace/Id/ValueSpaceConst.pas
// Port map: CSharp/port/units/ValueSpaceConst.md

namespace Bold;

/// <summary>Messages of the value space.</summary>
public static partial class ValueSpaceConst
{
    [Delphi(PortStatus.Ported)] public const string sInvalidEvent = "Invalid event: %s";
    [Delphi(PortStatus.Ported)] public const string sInvalidID = "Invalid ID, %s is not an valid integer.";
    [Delphi(PortStatus.Ported)] public const string sInvalidIndex = "%s.GetClassStreamer: Not a valid index";
    [Delphi(PortStatus.Ported)] public const string sUnrecognizedClassName = "%s.%s: Unrecognized class name %s";
}
