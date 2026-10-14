// Delphi: Source/Common/Core/BoldBase.pas
// Port map: CSharp/port/units/BoldBase.md
[assembly: Delphi(PortStatus.Ported, Unit = "BoldBase", Symbol = "TBoldElementFlag", Note = "Global using alias of int, in BoldBase.Aliases.cs.")]

namespace Bold;

/// <summary>
/// Base of Bold's objects. In Delphi it routes allocation through Bold's own memory manager; in .NET the garbage
/// collector allocates, so the class only keeps the hierarchy.
/// </summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Replaced, Symbol = "TBoldMemoryManagedObject.NewInstance", Note = "The .NET runtime allocates objects.")]
[Delphi(PortStatus.Replaced, Symbol = "TBoldMemoryManagedObject.FreeInstance", Note = "The garbage collector frees objects.")]
public class TBoldMemoryManagedObject
{
}

/// <summary>
/// An object that can be queried for interfaces. Delphi's COM-style reference counting is not needed in .NET:
/// a C# class implements interfaces directly and the garbage collector manages lifetime.
/// </summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Replaced, Symbol = "TBoldInterfacedObject.QueryInterface", Note = "Use a C# type test (obj is IFoo foo).")]
[Delphi(PortStatus.Replaced, Symbol = "TBoldInterfacedObject._AddRef", Note = "No reference counting in .NET.")]
[Delphi(PortStatus.Replaced, Symbol = "TBoldInterfacedObject._Release", Note = "No reference counting in .NET.")]
public abstract class TBoldInterfacedObject : TBoldMemoryManagedObject
{
    /// <summary>True when this object implements the interface <paramref name="IID"/>.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the interface type where Delphi takes its GUID.")]
    public virtual bool SupportsInterface(Type IID) => IID.IsInstanceOfType(this);
}

/// <summary>An object whose lifetime Delphi ties to interface references. In .NET the count is kept for code that reads it.</summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Replaced, Symbol = "TBoldRefCountedObject.NewInstance", Note = "The .NET runtime allocates objects.")]
[Delphi(PortStatus.Replaced, Symbol = "TBoldRefCountedObject.AfterConstruction", Note = "Delphi starts the count at 1 during construction; the count starts at 0 here.")]
public class TBoldRefCountedObject : TBoldInterfacedObject
{
    protected int FRefCount;

    [Delphi(PortStatus.Ported)]
    public int RefCount => FRefCount;

    /// <summary>Increments the reference count, as Delphi's _AddRef; the object is not freed at zero, the GC frees it.</summary>
    [Delphi(PortStatus.Partial, Note = "Counts only; reaching zero does not destroy the object, the garbage collector does.")]
    protected internal virtual int _AddRef() => Interlocked.Increment(ref FRefCount);

    [Delphi(PortStatus.Partial, Note = "Counts only; reaching zero does not destroy the object, the garbage collector does.")]
    protected internal virtual int _Release() => Interlocked.Decrement(ref FRefCount);

    /// <summary>Raises EBold when the object is destroyed while references are counted, as Delphi does.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void BeforeDestruction()
    {
        if (RefCount != 0)
        {
            throw new EBold(SysUtils.Format(BoldCommonConst.sRefCountNotNilOnDestruction, GetType().Name));
        }
    }
}

/// <summary>An interfaced object that is not reference counted; in .NET this is every object.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldNonRefCountedObject : TBoldInterfacedObject
{
    [Delphi(PortStatus.Ported)]
    protected internal virtual int _AddRef() => -1;

    [Delphi(PortStatus.Ported)]
    protected internal virtual int _Release() => -1;
}

/// <summary>An object with a 32-bit bank of flags and packed state values, used by the object space for compact state.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFlaggedObject : TBoldMemoryManagedObject
{
    private uint fStateAndFlagBank;

    [Delphi(PortStatus.Ported)]
    protected void SetInternalState(uint Mask, int shift, uint value) =>
        fStateAndFlagBank = (fStateAndFlagBank & ~Mask) | (value << shift);

    [Delphi(PortStatus.Ported)]
    protected uint GetInternalState(uint Mask, int shift) => (fStateAndFlagBank & Mask) >> shift;

    [Delphi(PortStatus.Ported)]
    protected void SetElementFlag(TBoldElementFlag Flag, bool Value)
    {
        if (Value)
        {
            fStateAndFlagBank |= unchecked((uint)Flag);
        }
        else
        {
            fStateAndFlagBank &= ~unchecked((uint)Flag);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected bool GetElementFlag(TBoldElementFlag Flag) => (unchecked((uint)Flag) & fStateAndFlagBank) != 0;
}

/// <summary>Unit-level constants of BoldBase: one bit per element flag.</summary>
public static partial class BoldBase
{
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag0 = 1 << 0;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag1 = 1 << 1;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag2 = 1 << 2;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag3 = 1 << 3;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag4 = 1 << 4;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag5 = 1 << 5;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag6 = 1 << 6;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag7 = 1 << 7;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag8 = 1 << 8;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag9 = 1 << 9;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag10 = 1 << 10;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag11 = 1 << 11;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag12 = 1 << 12;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag13 = 1 << 13;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag14 = 1 << 14;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag15 = 1 << 15;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag16 = 1 << 16;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag17 = 1 << 17;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag18 = 1 << 18;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag19 = 1 << 19;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag20 = 1 << 20;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag21 = 1 << 21;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag22 = 1 << 22;
    [Delphi(PortStatus.Ported)] public const int BoldElementFlag23 = 1 << 23;
}
