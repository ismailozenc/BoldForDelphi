// Delphi: Source/Common/Core/BoldStreams.pas
// Port map: CSharp/port/units/BoldStreams.md

namespace Bold;

/// <summary>Something that can be streamed under a stream name, such as an attribute value.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldStreamable
{
    [Delphi(PortStatus.Ported)]
    string GetStreamName();

    [Delphi(PortStatus.Ported)]
    string StreamName => GetStreamName();
}
