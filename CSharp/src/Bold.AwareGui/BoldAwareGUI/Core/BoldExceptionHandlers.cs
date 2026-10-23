// Delphi: Source/BoldAwareGUI/Core/BoldExceptionHandlers.pas
// Port map: CSharp/port/units/BoldExceptionHandlers.md

using System.ComponentModel;

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldExceptionHandlers", Symbol = "finalization", Note = "Freed the list of exception handlers; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldExceptionHandlers", Symbol = "initialization", Note = "Empty in Delphi.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldExceptionHandlers", Symbol = "GetBoldExceptionHandlers", Note = "Made the list of handlers on first use; here a static list field of TBoldExceptionHandler, made with the class.")]

namespace Bold;

/// <summary>Handles an exception raised when a control writes its value back; Discard says whether to drop the change.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldApplyExceptionEvent(Exception E, object? Component, TBoldElement? Elem, ref bool Discard);

/// <summary>Handles an exception raised when a control shows its value.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldDisplayExceptionEvent(Exception E, object? Component, TBoldElement? Elem);

/// <summary>
/// Catches the exceptions Bold controls raise when they show or write back values, for the controls of the form (or
/// container) it belongs to.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldExceptionHandler : Component
{
    private static readonly List<TBoldExceptionHandler> G_BoldExceptionHandlers = [];

    private TBoldApplyExceptionEvent? fOnApplyException;
    private TBoldDisplayExceptionEvent? fOnDisplayException;
    private object? fOwner;

    /// <summary>Adds the handler to <paramref name="AOwner"/> (its owner) and to the list of handlers.</summary>
    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner; Owner can also be set to a form.")]
    public TBoldExceptionHandler(IContainer? AOwner = null)
    {
        AOwner?.Add(this);
        fOwner = AOwner;
        lock (G_BoldExceptionHandlers)
        {
            G_BoldExceptionHandlers.Add(this);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldExceptionHandler.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (G_BoldExceptionHandlers)
            {
                G_BoldExceptionHandlers.Remove(this);
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>The handler for the component: the one owned by the component's topmost owner or by its topmost parent control.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested OwningComponent and ParentControl are private helpers; owners and parent controls come from the GUI handler.")]
    public static TBoldExceptionHandler? FindExceptionHandler(object? Component)
    {
        object? owning = OwningComponent(Component);
        object? parent = ParentControl(Component);
        lock (G_BoldExceptionHandlers)
        {
            foreach (TBoldExceptionHandler handler in G_BoldExceptionHandlers)
            {
                object? ExceptionHandlerOwner = handler.Owner;
                if (ExceptionHandlerOwner is not null && (ExceptionHandlerOwner == owning || ExceptionHandlerOwner == parent))
                {
                    return handler;
                }
            }
        }

        return null;
    }

    private static object? OwningComponent(object? Component)
    {
        object? result = Component;
        while (result is not null && BoldGUI.BoldGUIHandler().ComponentOwner(result) is { } owner)
        {
            result = owner;
        }

        return result;
    }

    private static object? ParentControl(object? Component) =>
        Component is null ? null : BoldGUI.BoldGUIHandler().TopmostParentControl(Component);

    /// <summary>Lets OnApplyException handle the exception; HandledByUser says whether there was one.</summary>
    [Delphi(PortStatus.Ported)]
    public void HandleApplyException(Exception E, object? Component, TBoldElement? Elem, ref bool Discard, ref bool HandledByUser)
    {
        // Note: Discard must be set by the caller, as there might be no matching exception handler to set it!
        HandledByUser = fOnApplyException is not null;
        if (HandledByUser)
        {
            OnApplyException!(E, Component, Elem, ref Discard);
        }
    }

    /// <summary>Lets OnDisplayException handle the exception; HandledByUser says whether there was one.</summary>
    [Delphi(PortStatus.Ported)]
    public void HandleDisplayException(Exception E, object? Component, TBoldElement? Elem, ref bool HandledByUser)
    {
        HandledByUser = fOnDisplayException is not null;
        if (HandledByUser)
        {
            OnDisplayException!(E, Component, Elem);
        }
    }

    /// <summary>The owner (Delphi's TComponent.Owner): the container given to the constructor, or a form.</summary>
    public object? Owner
    {
        get => fOwner;
        set => fOwner = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldApplyExceptionEvent? OnApplyException
    {
        get => fOnApplyException;
        set => fOnApplyException = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldDisplayExceptionEvent? OnDisplayException
    {
        get => fOnDisplayException;
        set => fOnDisplayException = value;
    }
}
