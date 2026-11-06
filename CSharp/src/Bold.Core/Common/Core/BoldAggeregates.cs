// Delphi: Source/Common/Core/BoldAggeregates.pas
// Port map: CSharp/port/units/BoldAggeregates.md
// COM-style aggregation: an object whose interface queries and reference counts are those of the object that
// contains it (its controller). C# interfaces are not counted and queried by GUID; a query here takes the interface
// type and gives the object that implements it, and the counts are kept for code that reads them.

namespace Bold;

/// <summary>
/// An object that can be aggregated: with a controller, its interface queries and reference counts are the
/// controller's; without one, its own.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldAggregateableObject : TBoldRefCountedObject
{
    /// <summary>The controller, not counted (Delphi kept it as a pointer).</summary>
    protected object? FController;

    [Delphi(PortStatus.Ported)]
    public TBoldAggregateableObject(object? Controller)
    {
        FController = Controller;
    }

    [Delphi(PortStatus.Ported)]
    protected object? GetController() => FController;

    [Delphi(PortStatus.Ported)]
    public object? Controller => GetController();

    /// <summary>The object that implements the interface type: the controller's answer, or this object's own.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the interface type and gives the implementing object, or null, where Delphi took a GUID and returned an HResult.")]
    public virtual object? QueryInterface(Type IID) =>
        FController is not null ? BoldAggeregates.QueryInterface(FController, IID) : NonDelegatingQueryInterface(IID);

    [Delphi(PortStatus.Ported, Note = "Counts on the controller when it is counted; a controller that is not counted gives -1.")]
    protected internal override int _AddRef() =>
        FController is null ? NonDelegatingAddRef() : FController is TBoldRefCountedObject counted ? counted._AddRef() : -1;

    [Delphi(PortStatus.Ported, Note = "Counts on the controller when it is counted; a controller that is not counted gives -1.")]
    protected internal override int _Release() =>
        FController is null ? NonDelegatingRelease() : FController is TBoldRefCountedObject counted ? counted._Release() : -1;

    /// <summary>This object, when it implements the interface type.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives the object or null, where Delphi returned S_OK or E_NOINTERFACE.")]
    protected virtual object? NonDelegatingQueryInterface(Type IID) => IID.IsInstanceOfType(this) ? this : null;

    [Delphi(PortStatus.Ported)]
    protected virtual int NonDelegatingAddRef() => base._AddRef();

    /// <summary>Counts down; at zero the object is destroyed, as Delphi's was (see TBoldRefCountedObject._Release).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual int NonDelegatingRelease() => base._Release();
}

/// <summary>An object contained in another: its reference counts are its controller's.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldContainedObject : TBoldInterfacedObject
{
    /// <summary>The controller, not counted (Delphi kept it as a pointer).</summary>
    protected object? FController;

    [Delphi(PortStatus.Ported)]
    public TBoldContainedObject(object? Controller)
    {
        FController = Controller;
    }

    [Delphi(PortStatus.Ported)]
    protected object? GetController() => FController;

    [Delphi(PortStatus.Ported)]
    public object? Controller => GetController();

    [Delphi(PortStatus.Ported, Note = "Counts on the controller when it is counted; otherwise gives -1.")]
    protected internal virtual int _AddRef() => FController is TBoldRefCountedObject counted ? counted._AddRef() : -1;

    [Delphi(PortStatus.Ported, Note = "Counts on the controller when it is counted; otherwise gives -1.")]
    protected internal virtual int _Release() => FController is TBoldRefCountedObject counted ? counted._Release() : -1;
}

/// <summary>A contained object whose interface queries are its controller's too.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAggregatedObject : TBoldContainedObject
{
    public TBoldAggregatedObject(object? Controller)
        : base(Controller)
    {
    }

    /// <summary>The object that implements the interface type, as the controller answers.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the interface type and gives the implementing object, or null, where Delphi took a GUID and returned an HResult.")]
    public virtual object? QueryInterface(Type IID) => FController is null ? null : BoldAggeregates.QueryInterface(FController, IID);
}

/// <summary>Interface queries on any object.</summary>
public static class BoldAggeregates
{
    /// <summary>
    /// The object that implements the interface type for an object: an aggregateable or aggregated object answers for
    /// itself, any other object when it implements the type.
    /// </summary>
    public static object? QueryInterface(object Obj, Type IID) => Obj switch
    {
        TBoldAggregateableObject aggregateable => aggregateable.QueryInterface(IID),
        TBoldAggregatedObject aggregated => aggregated.QueryInterface(IID),
        _ => IID.IsInstanceOfType(Obj) ? Obj : null,
    };
}
