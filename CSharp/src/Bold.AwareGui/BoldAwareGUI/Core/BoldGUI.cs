// Delphi: Source/BoldAwareGUI/Core/BoldGUI.pas
// Port map: CSharp/port/units/BoldGUI.md

using System.ComponentModel;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldGUI", Symbol = "initialization", Note = "The GUI toolkit creates the popup menu (Bold.WinForms sets BoldPopupMenu); the handler is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldGUI", Symbol = "finalization", Note = "Freed the handler and the popup menu; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>
/// GUI services the controls share: the objects being dragged, the element and control of the current popup menu,
/// and finding (and focusing) the form a component is on. The forms belong to the GUI toolkit: this handler knows
/// none, and Bold.WinForms installs a handler that knows its forms (<see cref="BoldGUI.BoldGUIHandlerClass"/>).
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldGUIHandler : TBoldMemoryManagedObject, IDisposable
{
    private TBoldElement? FPopupElement;
    private object? FPopupControl;
    private TBoldObjectList? FDraggedObjects;
    private bool fActivateTargetFormOnDrop;

    [Delphi(PortStatus.Ported)]
    public TBoldGUIHandler()
    {
        FDraggedObjects = new TBoldObjectList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldGUIHandler.Destroy")]
    public virtual void Dispose()
    {
        SysUtils.FreeAndNil(ref FDraggedObjects);
        GC.SuppressFinalize(this);
    }

    /// <summary>Fills the popup menu before it shows; nothing yet, as in Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    public void DoPopUp(object? sender)
    {
        // FIXME fill in items in popup menu, etc, etc
    }

    /// <summary>The form the component is on, or null; this handler knows no forms.</summary>
    [Delphi(PortStatus.Ported, Note = "Gives null here; the handler of a GUI toolkit (Bold.WinForms) overrides it to find the form.")]
    public virtual object? FindHostingForm(object? Component) => null;

    /// <summary>Focuses the visible form the component is on; false when there is none.</summary>
    [Delphi(PortStatus.Ported, Note = "Always false here; the handler of a GUI toolkit (Bold.WinForms) overrides it.")]
    public virtual bool TryToFocusHostingForm(object? Component) => false;

    /// <summary>
    /// True when the dragged objects can be dropped on the element: an object list, or a single link when one object
    /// is dragged, that is mutable and accepts every dragged object.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public bool DraggedObjectsAssignable(TBoldElement? element, TBoldDropMode dropMode)
    {
        TBoldElementTypeInfo? AcceptableBoldType;
        if (element is TBoldObjectReference)
        {
            AcceptableBoldType = element.BoldType;
        }
        else if (element is TBoldList)
        {
            AcceptableBoldType = ((TBoldListTypeInfo)element.BoldType!).ListElementTypeInfo;
        }
        else
        {
            return false;
        }

        if (!element.Mutable)
        {
            return false;
        }

        if (element is TBoldMember member && !member.CanModify())
        {
            return false;
        }

        for (int DraggedIndex = 0; DraggedIndex < DraggedObjects.Count; DraggedIndex++)
        {
            TBoldObject dragged = DraggedObjects[DraggedIndex]!;
            if (!dragged.BoldType!.ConformsTo(AcceptableBoldType))
            {
                return false;
            }

            if (element is TBoldList list && !list.CanInsert(-1, dragged, null))
            {
                return false;
            }

            if (element is TBoldObjectReference reference && !reference.CanSetLocator(dragged.BoldObjectLocator, null))
            {
                return false;
            }
        }

        return element is TBoldObjectList || (element is TBoldObjectReference && DraggedObjects.Count <= 1);
    }

    /// <summary>The component that owns the component (Delphi's Owner): here the container it is in.</summary>
    public virtual object? ComponentOwner(object Component) => (Component as IComponent)?.Site?.Container;

    /// <summary>The topmost parent of a control; null for a component that is not a control (this handler knows none).</summary>
    public virtual object? TopmostParentControl(object Component) => null;

    [Delphi(PortStatus.Ported)] public TBoldObjectList DraggedObjects => FDraggedObjects!;

    [Delphi(PortStatus.Ported)]
    public TBoldElement? PopupElement
    {
        get => FPopupElement;
        set => FPopupElement = value;
    }

    [Delphi(PortStatus.Ported, Note = "Any object; Delphi's is a TControl.")]
    public object? PopupControl
    {
        get => FPopupControl;
        set => FPopupControl = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool ActivateTargetFormOnDrop
    {
        get => fActivateTargetFormOnDrop;
        set => fActivateTargetFormOnDrop = value;
    }
}

/// <summary>Unit-level routines and variables of BoldGUI.</summary>
public static partial class BoldGUI
{
    private static TBoldGUIHandler? G_BoldGUIHandler;
    private static Type fBoldGUIHandlerClass = typeof(TBoldGUIHandler);

    /// <summary>The GUI handler, created on first use from <see cref="BoldGUIHandlerClass"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldGUIHandler BoldGUIHandler() =>
        G_BoldGUIHandler ??= (TBoldGUIHandler)Activator.CreateInstance(fBoldGUIHandlerClass)!;

    /// <summary>The class of the GUI handler; setting it (Bold.WinForms does) replaces the handler on its next use.</summary>
    public static Type BoldGUIHandlerClass
    {
        get => fBoldGUIHandlerClass;
        set
        {
            if (value != fBoldGUIHandlerClass)
            {
                fBoldGUIHandlerClass = value;
                G_BoldGUIHandler?.Dispose();
                G_BoldGUIHandler = null;
            }
        }
    }

    /// <summary>The popup menu of Bold controls; the GUI toolkit sets it (Bold.WinForms to a ContextMenuStrip).</summary>
    [Delphi(PortStatus.Ported, Note = "Any object; Delphi's is a TPopupMenu made in the initialization section.")]
    public static object? BoldPopupMenu;
}
