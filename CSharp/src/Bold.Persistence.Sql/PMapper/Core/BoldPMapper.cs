// Delphi: Source/PMapper/Core/BoldPMapper.pas
// Port map: CSharp/port/units/BoldPMapper.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldPMapper", Symbol = "TBoldPersistenceMapperClass", Note = "A System.Type of the mapper class.")]

namespace Bold;

/// <summary>The base of persistence mappers.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPersistenceMapper : TBoldSubscribableObject
{
    /// <summary>Creates the storage; nothing here.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void CreatePersistentStorage()
    {
    }
}
