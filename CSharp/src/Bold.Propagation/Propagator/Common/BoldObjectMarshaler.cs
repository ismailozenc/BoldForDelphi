// Delphi: Source/Propagator/Common/BoldObjectMarshaler.pas
// Port map: CSharp/port/units/BoldObjectMarshaler.md

namespace Bold;

/// <summary>
/// Hands an interface reference from one thread to another. Delphi marshaled the COM interface into a stream for the
/// other apartment; .NET objects can be used from any thread, so the marshaler only keeps the reference.
/// </summary>
[Delphi(PortStatus.Replaced, Note = "Keeps the reference: .NET objects need no marshaling between threads.")]
public class TBoldObjectMarshaler : IDisposable
{
    protected object? FStream;
    protected Type FMarshalIID;

    [Delphi(PortStatus.Replaced, Note = "Takes the interface type where Delphi took its GUID.")]
    public TBoldObjectMarshaler(Type IID, object obj)
    {
        FMarshalIID = IID;
        MarshalObject(obj);
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldObjectMarshaler.Destroy", Note = "Lets go of the reference.")]
    public void Dispose()
    {
        FStream = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>Keeps the object when it implements the interface; false (and logged) otherwise.</summary>
    [Delphi(PortStatus.Replaced, Note = "Keeps the reference.")]
    protected bool MarshalObject(object obj)
    {
        if (FMarshalIID.IsInstanceOfType(obj))
        {
            FStream = obj;
            return true;
        }

        BoldThreadSafeLog.BoldLogError("%s.MarshalObject: %s", GetType().Name, $"the object is not a {FMarshalIID.Name}");
        return false;
    }

    /// <summary>The kept reference.</summary>
    [Delphi(PortStatus.Replaced, Note = "Gives the kept reference.")]
    public bool UnMarshalObject(out object? Obj)
    {
        Obj = FStream;
        return Obj is not null;
    }
}
