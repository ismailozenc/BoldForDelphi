// Delphi: Source/Common/Handles/BoldHandle.pas
// Port map: CSharp/port/units/BoldHandle.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldHandle", Symbol = "initialization", Note = "The handle list is created by its static field initializer.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldHandle", Symbol = "finalization", Note = "Freed the handle list; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>The base of the handles: a subscribable component that hands out an object. Every handle is kept in <see cref="BoldHandle.BoldHandleList"/>.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldHandle : TBoldSubscribableComponent
{
    /// <summary>Adds the handle to <paramref name="Owner"/> (when given) and to the global handle list.</summary>
    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    protected TBoldHandle(System.ComponentModel.IContainer? Owner = null)
    {
        Owner?.Add(this);
        BoldHandle.BoldHandleList?.Add(this);
    }

    /// <summary>Tells subscribers the handle is going and removes it from the global handle list.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldHandle.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FreePublisher();
            BoldHandle.BoldHandleList?.Remove(this);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    protected abstract object? GetHandledObject();

    [Delphi(PortStatus.Ported)]
    public object? HandledObject => GetHandledObject();
}

/// <summary>The global list of live handles.</summary>
public static partial class BoldHandle
{
    [Delphi(PortStatus.Ported)]
    public static TBoldObjectArray? BoldHandleList = new(10, TBoldContainerOptions.Of());
}
