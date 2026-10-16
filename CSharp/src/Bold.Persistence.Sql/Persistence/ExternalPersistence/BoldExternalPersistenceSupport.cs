// Delphi: Source/Persistence/ExternalPersistence/BoldExternalPersistenceSupport.pas
// Port map: CSharp/port/units/BoldExternalPersistenceSupport.md
// Adapters that give typed access to an object's contents in a value space, registered by class expression name.

[assembly: Delphi(PortStatus.Ported, Unit = "BoldExternalPersistenceSupport", Symbol = "TBoldObjectPersistenceAdapterClass", Note = "A System.Type of the adapter class.")]

namespace Bold;

/// <summary>An object's contents seen as a persistent object.</summary>
[Delphi(PortStatus.Ported)]
public interface IPersistentBoldObject
{
    [Delphi(PortStatus.Ported)] IBoldObjectContents GetAsObjectContents();
    [Delphi(PortStatus.Ported)] IBoldObjectContents AsObjectContents => GetAsObjectContents();
}

/// <summary>Adapts an object's contents in a value space; subclasses add typed accessors for a class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectPersistenceAdapter : TBoldRefCountedObject, IPersistentBoldObject
{
    private static readonly Dictionary<string, Type> G_RegisteredPersistenceAdapters = new(StringComparer.OrdinalIgnoreCase);
    private readonly IBoldObjectContents fAdaptedInterface;
    private readonly IBoldValueSpace fValueSpace;
    private readonly TMoldClass fMoldClass;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectPersistenceAdapter(IBoldObjectContents AdaptedInterface, IBoldValueSpace ValueSpace, TMoldClass MoldClass)
    {
        fAdaptedInterface = AdaptedInterface;
        fValueSpace = ValueSpace;
        fMoldClass = MoldClass;
    }

    [Delphi(PortStatus.Ported)]
    public IBoldObjectContents GetAsObjectContents() => AdaptedInterface;

    /// <summary>Registers the adapter class (a TBoldObjectPersistenceAdapter subclass) for the class.</summary>
    [Delphi(PortStatus.Ported, Note = "A class procedure in Delphi; the adapter class is a System.Type.")]
    public static void RegisterPersistenceInterface(Type Adapter, string ExpressionName)
    {
        lock (G_RegisteredPersistenceAdapters)
        {
            G_RegisteredPersistenceAdapters.TryAdd(ExpressionName, Adapter);
        }
    }

    /// <summary>The adapter class registered for the class, or null.</summary>
    [Delphi(PortStatus.Ported, Note = "A class function in Delphi; the adapter class is a System.Type.")]
    public static Type? FindRegisteredPersistenceInterface(string ExpressionName)
    {
        lock (G_RegisteredPersistenceAdapters)
        {
            return G_RegisteredPersistenceAdapters.GetValueOrDefault(ExpressionName);
        }
    }

    [Delphi(PortStatus.Ported)] public IBoldValueSpace ValueSpace => fValueSpace;
    [Delphi(PortStatus.Ported)] public IBoldObjectContents AdaptedInterface => fAdaptedInterface;
    [Delphi(PortStatus.Ported)] public TMoldClass MoldClass => fMoldClass;
}
