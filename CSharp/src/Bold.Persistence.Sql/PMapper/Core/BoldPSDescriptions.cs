// Delphi: Source/PMapper/Core/BoldPSDescriptions.pas
// Port map: CSharp/port/units/BoldPSDescriptions.md

namespace Bold;

/// <summary>An element of a description of persistent storage (tables, columns, indexes), owned by another.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPSDescriptionElement
{
    private readonly TBoldPSDescriptionElement? fOwner;

    [Delphi(PortStatus.Ported)]
    public TBoldPSDescriptionElement(TBoldPSDescriptionElement? aOwner)
    {
        fOwner = aOwner;
    }

    [Delphi(PortStatus.Ported)] public TBoldPSDescriptionElement? Owner => fOwner;
}

/// <summary>The description of a whole storage, which can create it.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldPSSystemDescription(TBoldPSDescriptionElement? aOwner) : TBoldPSDescriptionElement(aOwner)
{
    [Delphi(PortStatus.Ported)]
    public abstract void CreatePersistentStorage(TBoldPSParams PSParams);
}
