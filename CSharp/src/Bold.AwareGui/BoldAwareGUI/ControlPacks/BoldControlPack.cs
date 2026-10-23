// Delphi: Source/BoldAwareGUI/ControlPacks/BoldControlPack.pas
// Port map: CSharp/port/units/BoldControlPack.md

using System.ComponentModel;
using System.Drawing;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;
using static Bold.BoldSubscription;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldControlPack", Symbol = "TBoldRendererDataClass", Note = "A System.Type of a TBoldRendererData subclass, whose constructor takes the owning follower.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldControlPack", Symbol = "TBoldClientableList", Note = "TBoldObjectArray; the COM client's interface array is not ported.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldControlPack", Symbol = "initialization", Note = "The default renderer is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Bold.WinForms")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Bold.AwareGui.Tests")]

namespace Bold;

/// <summary>Called with a follower (before and after it is brought up to date).</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldFollowerEvent(TBoldFollower Follower);

/// <summary>Gives the context type of a controller's expression.</summary>
[Delphi(PortStatus.Ported)]
public delegate TBoldElementTypeInfo? TBoldGetContextTypeEvent();

/// <summary>Called for a subfollower of a follower.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSubFollowerEvent(int index, TBoldFollower OwningFollower);

/// <summary>Says whether the value of an element may be changed through a renderer.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldMayModify(TBoldElement Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldSubscriber? Subscriber);

/// <summary>Called when a control starts holding a changed value of an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldHoldsChangedValue(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldSubscriber? Subscriber);

/// <summary>Called when a control no longer holds a changed value of an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldReleaseChangedValue(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldSubscriber? Subscriber);

/// <summary>Places the subscriptions a renderer's value depends on.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldSubscriber? Subscriber);

/// <summary>Called when dragging starts from a control.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldStartDrag(TBoldElement? Element, TBoldDragMode DragMode, TBoldRendererData RendererData);

/// <summary>Called when dragging ends.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldEndDrag(TBoldDragMode DragMode, bool InternalDrag);

/// <summary>Says whether the dragged objects may be dropped on the element.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldDragOver(TBoldElement? Element, TBoldDropMode DropMode, bool InternalDrag, TBoldRendererData RendererData, int dropindex);

/// <summary>Drops the dragged objects on the element.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldDragDrop(TBoldElement? Element, TBoldDropMode DropMode, int dropindex);

/// <summary>The subscriber of a follower: it passes what it receives to the follower.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFollowerSubscriber : TBoldSubscriber
{
    private readonly TBoldFollower fFollower;

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerSubscriber(TBoldFollower follower)
    {
        fFollower = follower;
    }

    [Delphi(PortStatus.Ported)]
    protected override void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent) =>
        Follower.Receive(Originator, OriginalEvent, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    protected override string GetContextString() => Follower.AssertedController.GetNamePath();

    [Delphi(PortStatus.Ported)] public TBoldFollower Follower => fFollower;
}

/// <summary>
/// What a follower shows, as its renderer made it from the element (a string, a check state, a list of subfollowers);
/// each renderer has its own kind of renderer data.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldRendererData : TBoldMemoryManagedObject, IDisposable
{
    private readonly TBoldFollower fOwningFollower;
    private bool fMayModify;

    [Delphi(PortStatus.Ported)]
    public TBoldRendererData(TBoldFollower OwningFollower)
    {
        fOwningFollower = OwningFollower;
    }

    /// <summary>Frees what the renderer data holds (subfollowers).</summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);

    [Delphi(PortStatus.Ported)]
    protected internal virtual void EnsureSubfollowersDisplayable()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual int GetSubFollowerCount() => 0;

    [Delphi(PortStatus.Ported)]
    protected internal virtual TBoldFollower? GetSubFollower(int index) => throw new EBold(SysUtils.Format(sClassHasNoSubfollowers, GetType().Name));

    [Delphi(PortStatus.Ported)]
    protected internal virtual int GetCurrentSubFollowerIndex() => -1;

    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetCurrentSubFollowerIndex(int index)
    {
        // just ignore
    }

    [Delphi(PortStatus.Ported)] public TBoldFollower OwningFollower => fOwningFollower;

    [Delphi(PortStatus.Ported)]
    public bool MayModify
    {
        get => fMayModify;
        set => fMayModify = value;
    }
}

/// <summary>
/// Turns elements into what a control shows (and back): a renderer decides whether an element may be changed,
/// subscribes to what its value depends on and handles dragging and dropping. Events can replace each default.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldRenderer : TBoldSubscribableComponentViaBoldElem
{
    private TBoldStartDrag? FOnStartDrag;
    private TBoldEndDrag? FOnEndDrag;
    private TBoldDragOver? FOnDragOver;
    private TBoldDragDrop? FOnDragDrop;
    private TBoldMayModify? FOnMayModify;
    private TBoldHoldsChangedValue? FOnHoldsChangedValue;
    private TBoldReleaseChangedValue? FOnReleaseChangedValue;
    private TBoldSubscribe? FOnSubscribe;
    private TStringList? FRepresentations;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldRenderer(IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldRenderer.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FRepresentations = null;
        }

        base.Dispose(disposing);
    }

    /// <summary>The representations the renderer offers, the defaults unless set.</summary>
    [Delphi(PortStatus.Ported)]
    private TStrings GetRepresentations() => FRepresentations ??= GetDefaultRepresentationStringList();

    [Delphi(PortStatus.Ported)]
    private void SetRepresentations(TStrings Value)
    {
        FRepresentations ??= new TStringList();
        FRepresentations.Assign(Value);
    }

    /// <summary>True when the representations differ from the defaults (the designer writes them only then).</summary>
    [Delphi(PortStatus.Ported)]
    private bool StoreRepresentations()
    {
        if (FRepresentations is null || FRepresentations.Count == 0)
        {
            return false;
        }

        return !GetDefaultRepresentationStringList().ToStringArray().SequenceEqual(FRepresentations.ToStringArray());
    }

    private bool ShouldSerializeRepresentations() => StoreRepresentations();

    [Delphi(PortStatus.Ported)]
    private void SetOnSubscribe(TBoldSubscribe? Value)
    {
        if (FOnSubscribe != Value)
        {
            FOnSubscribe = Value;
            Changed();
        }
    }

    /// <summary>The element the expression gives on the element, or null.</summary>
    [Delphi(PortStatus.Ported)]
    protected static TBoldElement? GetExpressionAsDirectElement(TBoldElement? Element, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        Element?.EvaluateExpressionAsDirectElement(Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    protected virtual Type GetRendererDataClass() => typeof(TBoldRendererData);

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetSupportsMulti() => false;

    /// <summary>Starts dragging the selection: the element's object (or a link's object) goes in the dragged objects.</summary>
    [Delphi(PortStatus.Ported, Note = "Fills in the class name of the message, which Delphi left as %s.")]
    protected virtual void DefaultStartDrag(TBoldElement? Element, TBoldDragMode DragMode, TBoldRendererData RendererData)
    {
        if (BoldGUI.BoldGUIHandler().DraggedObjects.Count != 0)
        {
            throw new EBold(SysUtils.Format(sDraggedObjectsNotCleared, GetType().Name));
        }

        if (DragMode == TBoldDragMode.bdgSelection)
        {
            TBoldObject? Obj = Element switch
            {
                TBoldObject boldObject => boldObject,
                TBoldObjectReference reference => reference.BoldObject,
                _ => null,
            };
            if (Obj is not null)
            {
                BoldGUI.BoldGUIHandler().DraggedObjects.Add(Obj);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void DefaultEndDrag(TBoldDragMode DragMode, bool InternalDrag) => BoldGUI.BoldGUIHandler().DraggedObjects.Clear();

    /// <summary>True when the element may change and accepts the dragged objects.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual bool DefaultDragOver(TBoldElement? Element, TBoldDropMode DropMode, bool InternalDrag, TBoldRendererData RendererData, int dropindex) =>
        Element is not null && Element.ObserverMayModify(this) &&
        BoldGUI.BoldGUIHandler().DraggedObjects.Count > 0 &&
        BoldGUI.BoldGUIHandler().DraggedObjectsAssignable(Element, DropMode);

    /// <summary>
    /// Drops the dragged objects: a single link gets the object; a list gets them appended or inserted at the index
    /// (objects already in the list move there).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DefaultDragDrop(TBoldElement? Element, TBoldDropMode DropMode, int dropindex)
    {
        TBoldObjectList dragged = BoldGUI.BoldGUIHandler().DraggedObjects;
        if (Element is TBoldObjectReference TheLink)
        {
            TBoldObject? BoldObject = dragged.Count switch
            {
                0 => null,
                1 => dragged[0],
                _ => throw new EBold(SCannotDragOverMultipleObjects),
            };
            switch (DropMode)
            {
                case TBoldDropMode.bdpInsert:
                case TBoldDropMode.bdpAppend:
                    if (TheLink.BoldObject is not null && BoldObject is not null)
                    {
                        throw new EBold(SLinkAlreadyAssigned);
                    }

                    TheLink.BoldObject = BoldObject;
                    break;
                case TBoldDropMode.bdpReplace:
                    TheLink.BoldObject = BoldObject;
                    break;
            }
        }
        else if (Element is TBoldObjectList TheList)
        {
            switch (DropMode)
            {
                case TBoldDropMode.bdpAppend:
                    for (int i = 0; i < dragged.Count; i++)
                    {
                        if (TheList.IndexOf(dragged[i]) == -1)
                        {
                            TheList.Add(dragged[i]);
                        }
                    }

                    break;
                case TBoldDropMode.bdpReplace:
                    throw new EBoldFeatureNotImplementedYet(SysUtils.Format(sReplaceNotImplemented, GetType().Name));
                case TBoldDropMode.bdpInsert:
                    if (dropindex < 0)
                    {
                        dropindex = 0;
                    }

                    for (int I = 0; I < dragged.Count; I++)
                    {
                        int prevIndex = TheList.IndexOf(dragged[I]);
                        if (prevIndex == -1)
                        {
                            if (dropindex < TheList.Count)
                            {
                                TheList.Insert(dropindex, dragged[I]);
                            }
                            else
                            {
                                TheList.Add(dragged[I]);
                            }

                            dropindex++;
                        }
                        else
                        {
                            TheList.Move(prevIndex, dropindex);
                            if (prevIndex > dropindex)
                            {
                                dropindex++;
                            }
                        }
                    }

                    break;
            }
        }
    }

    /// <summary>Draws the follower's value; nothing here.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void DrawOnCanvas(TBoldFollower Follower, TCanvas Canvas, Rectangle Rect, TAlignment Aligment, Point Margins)
    {
    }

    /// <summary>True when an event replaces how the value is found (OnSubscribe), which makes the renderer read-only without OnMayModify.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual bool HasEventOverrides() => FOnSubscribe is not null;

    /// <summary>Default, Short and Long.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TStringList GetDefaultRepresentationStringList()
    {
        var Result = new TStringList();
        Result.Add(SysUtils.IntToStr(BoldDefs.brDefault) + "=" + SRepresentationDefault);
        Result.Add(SysUtils.IntToStr(BoldDefs.brShort) + "=" + SRepresentationShort);
        Result.Add(SysUtils.IntToStr(BoldDefs.brLong) + "=" + SRepresentationLong);
        return Result;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldStartDrag? OnStartDrag
    {
        get => FOnStartDrag;
        set => FOnStartDrag = value;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldEndDrag? OnEndDrag
    {
        get => FOnEndDrag;
        set => FOnEndDrag = value;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldDragOver? OnDragOver
    {
        get => FOnDragOver;
        set => FOnDragOver = value;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldDragDrop? OnDragDrop
    {
        get => FOnDragDrop;
        set => FOnDragDrop = value;
    }

    /// <summary>Tells subscribers (controllers) that the renderer changed.</summary>
    [Delphi(PortStatus.Ported)]
    public void Changed() => SendEvent(this, beValueChanged);

    /// <summary>True when the value the expression gives may be changed by the subscriber.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool DefaultMayModify(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        return ValueElement is not null && ValueElement.ObserverMayModify(Subscriber);
    }

    /// <summary>Registers the subscriber as holding a changed value of the expression's value.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void DefaultHoldsChangedValue(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        if (ValueElement is null)
        {
            throw new EBold(SysUtils.Format(sCannotModifyValue, GetType().Name));
        }

        ValueElement.RegisterModifiedValueHolder(Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void DefaultReleaseChangedValue(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber) =>
        GetExpressionAsDirectElement(Element, Expression, VariableList)?.UnRegisterModifiedValueHolder(Subscriber);

    /// <summary>OnMayModify's verdict; read-only when an event replaces how the value is found; else the default.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool MayModify(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (FOnMayModify is not null && Element is not null)
        {
            return OnMayModify!(Element, Representation, Expression, Subscriber);
        }

        if (HasEventOverrides())
        {
            // this forces read-only renderers that have an OnSubscribe event but no OnMayModify;
            // OnMayModify is mandatory for a writeable renderer.
            return false;
        }

        return DefaultMayModify(Element, Representation, Expression, VariableList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void HoldsChangedValue(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (FOnHoldsChangedValue is not null)
        {
            OnHoldsChangedValue!(Element, Representation, Expression, Subscriber);
        }
        else
        {
            DefaultHoldsChangedValue(Element, Representation, Expression, VariableList, Subscriber);
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void ReleaseChangedValue(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (FOnReleaseChangedValue is not null)
        {
            OnReleaseChangedValue!(Element, Representation, Expression, Subscriber);
        }
        else
        {
            DefaultReleaseChangedValue(Element, Representation, Expression, VariableList, Subscriber);
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void StartDrag(TBoldElement? Element, TBoldDragMode DragMode, TBoldRendererData RendererData)
    {
        if (FOnStartDrag is not null)
        {
            OnStartDrag!(Element, DragMode, RendererData);
        }
        else
        {
            DefaultStartDrag(Element, DragMode, RendererData);
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void EndDrag(TBoldDragMode DragMode, bool InternalDrag)
    {
        if (FOnEndDrag is not null)
        {
            OnEndDrag!(DragMode, InternalDrag);
        }
        else
        {
            DefaultEndDrag(DragMode, InternalDrag);
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool DragOver(TBoldElement? Element, TBoldDropMode DropMode, bool InternalDrag, TBoldRendererData RendererData, int dropindex) =>
        FOnDragOver is not null
            ? FOnDragOver(Element, DropMode, InternalDrag, RendererData, dropindex)
            : DefaultDragOver(Element, DropMode, InternalDrag, RendererData, dropindex);

    [Delphi(PortStatus.Ported)]
    public virtual void DragDrop(TBoldElement? Element, TBoldDropMode DropMode, int dropindex)
    {
        if (FOnDragDrop is not null)
        {
            OnDragDrop!(Element, DropMode, dropindex);
        }
        else
        {
            DefaultDragDrop(Element, DropMode, dropindex);
        }
    }

    /// <summary>Subscribes to what the expression's value depends on (or lets OnSubscribe do it).</summary>
    [Delphi(PortStatus.Ported)]
    public void SubscribeToElement(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldSubscriber? Subscriber, TBoldExternalVariableList? VariableList = null)
    {
        if (FOnSubscribe is not null)
        {
            FOnSubscribe(Element, Representation, Expression, Subscriber);
        }
        else
        {
            Element?.SubscribeToExpression(Expression, Subscriber, false, false, VariableList);
        }
    }

    [Delphi(PortStatus.Ported)] public Type RendererDataClass => GetRendererDataClass();
    [Delphi(PortStatus.Ported)] public bool SupportsMulti => GetSupportsMulti();

    [Delphi(PortStatus.Ported)]
    public TStrings Representations
    {
        get => GetRepresentations();
        set => SetRepresentations(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldMayModify? OnMayModify
    {
        get => FOnMayModify;
        set => FOnMayModify = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldHoldsChangedValue? OnHoldsChangedValue
    {
        get => FOnHoldsChangedValue;
        set => FOnHoldsChangedValue = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldReleaseChangedValue? OnReleaseChangedValue
    {
        get => FOnReleaseChangedValue;
        set => FOnReleaseChangedValue = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSubscribe? OnSubscribe
    {
        get => FOnSubscribe;
        set => SetOnSubscribe(value);
    }
}

/// <summary>
/// Follows one element for a control: it keeps what the control shows up to date (Display, through the display queue)
/// and writes changes back (Apply), with its controller's renderer. List controls have a follower per row, the
/// subfollowers of the list's follower.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldFollower : TBoldQueueable
{
    private int fIndex;
    private readonly TBoldFollower? fOwningFollower;
    private TBoldFollowerState fState;
    private TBoldElement? fElement;
    private TBoldRendererData? fRendererData;
    private TBoldFollowerController? fController;
    private object? fControlData;
    private TBoldSubscriber? fSubscriber;

    /// <summary>A follower for the controller, matched by the object (the control) in the queue.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldFollower(object? MatchObject, TBoldFollowerController Controller)
        : base(MatchObject)
    {
        System.Diagnostics.Debug.Assert(Controller is not null);
        fController = Controller;
        fSubscriber = new TBoldFollowerSubscriber(this);
        fIndex = -1;
    }

    /// <summary>A subfollower of the follower (a row of a list), following the element; it displays after its owner.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldFollower.CreateSubFollower")]
    public TBoldFollower(TBoldFollower OwningFollower, TBoldFollowerController Controller, TBoldElement? Element)
        : base(OwningFollower.MatchObject)
    {
        fOwningFollower = OwningFollower;
        PrioritizedQueuable = OwningFollower;
        System.Diagnostics.Debug.Assert(Controller is not null);
        fController = Controller;
        fSubscriber = new TBoldFollowerSubscriber(this);
        fElement = Element;
        fIndex = -1;
        MarkSubscriptionOutOfDate();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldFollower.Destroy")]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fRendererData);
        SysUtils.FreeAndNil(ref fSubscriber);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private bool GetActive() => fState is not (TBoldFollowerState.bfsInactiveValidElement or TBoldFollowerState.bfsInactiveInvalidElement);

    /// <summary>Activating brings the follower up to date and subscribes; deactivating drops its subscriptions and renderer data.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetActive(bool Value)
    {
        if (Value != GetActive())
        {
            if (Value)
            {
                System.Diagnostics.Debug.Assert(State != TBoldFollowerState.bfsInactiveInvalidElement);
                SetState(TBoldFollowerState.bfsActivating);
                MakeUptoDateAndSubscribe(); // CHECKME This could cause errors if an owning follower is in bfsOutOfDate
                MarkClean();
            }
            else if (State != TBoldFollowerState.bfsDirty)
            {
                SetState(TBoldFollowerState.bfsInactiveValidElement);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetElementValid() => State != TBoldFollowerState.bfsInactiveInvalidElement;

    [Delphi(PortStatus.Ported)]
    private void SetElementValid(bool Value)
    {
        System.Diagnostics.Debug.Assert(!Active || Value);
        if (Value != ElementValid)
        {
            SetState(Value ? TBoldFollowerState.bfsInactiveValidElement : TBoldFollowerState.bfsInactiveInvalidElement);
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldFollower? GetSubFollower(int index) => RendererData.GetSubFollower(index);

    [Delphi(PortStatus.Ported)]
    private int GetSubFollowerCount() => Active ? RendererData.GetSubFollowerCount() : 0;

    [Delphi(PortStatus.Ported)] private int GetCurrentIndex() => RendererData.GetCurrentSubFollowerIndex();

    [Delphi(PortStatus.Ported)]
    private void SetCurrentIndex(int index)
    {
        if (index != CurrentIndex)
        {
            RendererData.SetCurrentSubFollowerIndex(index);
            MarkValueOutOfDate();
        }
    }

    /// <summary>Sets the element (and makes the follower resubscribe, even for the same nil element, as other settings may have changed).</summary>
    [Delphi(PortStatus.Ported)]
    private void SetElement(TBoldElement? theElement)
    {
        // if the element is nil and the new element is nil as well we still need to mark the follower out of date,
        // since other properties of the controller might have changed (especially the nil string representation)
        if (theElement is null || theElement != fElement)
        {
            fElement = theElement;
            ElementValid = true;
            MarkSubscriptionOutOfDate();
        }
    }

    /// <summary>Moves the follower to a state: leaving takes it off the display or apply list, entering puts it on one.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetState(TBoldFollowerState Value)
    {
        // action when leaving state
        switch (State)
        {
            case TBoldFollowerState.bfsValueOutOfDate:
            case TBoldFollowerState.bfsSubscriptionOutOfDate:
                RemoveFromDisplayList();
                break;
            case TBoldFollowerState.bfsDirty:
                RemoveFromApplyList();
                break;
        }

        fState = Value;

        // action when entering state
        switch (State)
        {
            case TBoldFollowerState.bfsValueOutOfDate:
                AddToDisplayList();
                break;
            case TBoldFollowerState.bfsSubscriptionOutOfDate:
                AddToDisplayList();
                Subscriber.CancelAllSubscriptions();
                break;
            case TBoldFollowerState.bfsDirty:
                AssertedController.HoldsChangedValue(this);
                AddToApplyList();
                break;
            case TBoldFollowerState.bfsInactiveValidElement:
            case TBoldFollowerState.bfsInactiveInvalidElement:
                Subscriber.CancelAllSubscriptions();
                SysUtils.FreeAndNil(ref fRendererData);
                break;
            case TBoldFollowerState.bfsEmpty:
            case TBoldFollowerState.bfsCurrent:
            case TBoldFollowerState.bfsActivating:
                break;
            default:
                throw new EBoldInternal(SysUtils.Format("%s: Unknown FollowerState", GetType().Name));
        }
    }

    /// <summary>The renderer data, made by the controller's renderer on first use; an inactive follower has none.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldRendererData GetRendererData()
    {
        if (!Active)
        {
            throw new EBold(SBoldInactiveFollowerNoRenderData);
        }

        return fRendererData ??= (TBoldRendererData)Activator.CreateInstance(AssertedController.RendererDataClass, this)!;
    }

    /// <summary>Adds the out of date followers of the controller in this subtree (not below out of date ones) to the list.</summary>
    [Delphi(PortStatus.Ported)]
    private void CollectMatchingDownwards(TBoldObjectArray Followers, TBoldFollowerController MatchController)
    {
        if (Controller == MatchController && Element is not null && bfsOutOfDate.Contains(State) && MostPrioritizedQueuable() is null)
        {
            Followers.Add(this);
        }

        if (!bfsOutOfDate.Contains(State))
        {
            for (int I = 0; I < SubFollowerCount; I++)
            {
                SubFollowers(I)!.CollectMatchingDownwards(Followers, MatchController);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldFollowerController GetAssertedController() =>
        fController ?? throw new EBold(SysUtils.Format(sControllerNotAssigned, GetType().Name));

    /// <summary>Queues the follower for display, unless it lost its controller.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void AddToDisplayList()
    {
        if (Controller is not null)
        {
            base.AddToDisplayList();
        }
    }

    /// <summary>
    /// Reacts to what the subscriber got: a value change makes the value out of date, a resubscribe request the
    /// subscriptions, and a controller change drops renderer data that no longer fits.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected internal void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (OriginalEvent == beDestroying && Originator == Element)
        {
            fElement = null; // CHECKME Is this necessary? /frha
        }

        switch (RequestedEvent)
        {
            case breReEvaluate:
                MarkValueOutOfDate();
                break;
            case breReSubscribe:
                MarkSubscriptionOutOfDate();
                break;
            case TBoldFollowerController.breControllerChanged:
                if (OriginalEvent == beDestroying)
                {
                    if (fRendererData is not null)
                    {
                        AssertedController.CleanRendererData(fRendererData);
                    }

                    SysUtils.FreeAndNil(ref fRendererData);
                    fController = null;
                }

                if (fRendererData is not null && !AssertedController.RendererDataClass.IsInstanceOfType(fRendererData))
                {
                    SysUtils.FreeAndNil(ref fRendererData);
                }

                MarkSubscriptionOutOfDate();
                break;
            default:
                throw new EBoldInternal(SysUtils.Format(sUnknownRequestedEvent, GetType().Name, RequestedEvent));
        }
    }

    /// <summary>Displays: moves the value from the object to the renderer data, and resubscribes when needed.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void MakeUptoDateAndSubscribe()
    {
        bool resubscribe = bfdNeedResubscribe.Contains(State);
        if (resubscribe)
        {
            Subscriber.CancelAllSubscriptions();
            AssertedController.AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beValueChanged, beDestroying), TBoldFollowerController.breControllerChanged);
        }

        AssertedController.MakeUptodateAndSubscribe(this, resubscribe);
    }

    /// <summary>Displays several followers of one controller at once (fetching their objects together).</summary>
    [Delphi(PortStatus.Ported)]
    protected internal static void MultiMakeUptodateAndSubscribe(TBoldObjectArray Followers)
    {
        System.Diagnostics.Debug.Assert(Followers.Count > 0);
        TBoldFollowerController Controller = ((TBoldFollower)Followers[0]!).AssertedController;
        for (int I = 0; I < Followers.Count; I++)
        {
            var follower = (TBoldFollower)Followers[I]!;
            if (bfdNeedResubscribe.Contains(follower.State))
            {
                follower.Subscriber.CancelAllSubscriptions(); // CHECKME ever needed?
                Controller.AddSmallSubscription(follower.Subscriber, TBoldSmallEventSet.Of(beValueChanged, beDestroying), TBoldFollowerController.breControllerChanged);
            }
        }

        Controller.MultiMakeUptodateAndSubscribe(Followers);
    }

    /// <summary>Applies: moves the value from the renderer data to the object.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void MakeClean()
    {
        AssertedController.MakeClean(this);
        if (State == TBoldFollowerState.bfsDirty)
        {
            SetState(TBoldFollowerState.bfsSubscriptionOutOfDate);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal void MarkDirty()
    {
        switch (State)
        {
            case TBoldFollowerState.bfsCurrent:
                SetState(TBoldFollowerState.bfsDirty);
                break;
            case TBoldFollowerState.bfsDirty:
                break;
            default:
                throw new EBoldInternal(SysUtils.Format(sFollowerStateError, GetType().Name, "MarkDirty"));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal void MarkClean() => SetState(TBoldFollowerState.bfsCurrent);

    /// <summary>Collects the out of date followers of the controller in the whole follower tree.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void CollectMatching(TBoldObjectArray Followers, TBoldFollowerController MatchController)
    {
        if (fOwningFollower is not null)
        {
            fOwningFollower.CollectMatching(Followers, MatchController);
        }
        else
        {
            CollectMatchingDownwards(Followers, MatchController);
        }
    }

    /// <summary>Brings the follower (or all the out of date followers of its controller at once) up to date.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested DisplaySelf and DisplayMulti are private helpers. An error is raised as EBold with the controller in its message and the error inside, where Delphi changed the error's message.")]
    public override void Display()
    {
        try
        {
            if (AssertedController.SupportsMulti)
            {
                DisplayMulti();
            }
            else
            {
                DisplaySelf();
            }
        }
        catch (Exception E)
        {
            if (Controller is not null && Controller.HandleDisplayException(E, Element))
            {
                // don't re-raise
            }
            else if (Controller is not null)
            {
                throw new EBold(SysUtils.Format(sDisplayError, E.Message, BoldDefs.BOLDCRLF, Controller.GetNamePath()), E);
            }
            else
            {
                throw;
            }
        }
    }

    private void DisplaySelf()
    {
        try
        {
            if (MostPrioritizedQueuable() is not null)
            {
                throw new EBold(SysUtils.Format(sCannotDisplayInThisOrder, GetType().Name));
            }

            MakeUptoDateAndSubscribe();
        }
        finally
        {
            MarkClean();
        }
    }

    private void DisplayMulti()
    {
        var Followers = new TBoldObjectArray(10, TBoldContainerOptions.Of());
        CollectMatching(Followers, Controller!);
        if (Followers.Count > 1)
        {
            try
            {
                MultiMakeUptodateAndSubscribe(Followers);
            }
            finally
            {
                for (int i = 0; i < Followers.Count; i++)
                {
                    ((TBoldFollower)Followers[i]!).MarkClean();
                }
            }
        }
        else
        {
            DisplaySelf();
        }
    }

    /// <summary>Writes a changed value back; an error is offered to the exception handler and the change dropped.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Apply()
    {
        if (State != TBoldFollowerState.bfsDirty)
        {
            return;
        }

        try
        {
            AssertedController.ReleaseChangedValue(this);
            MakeClean();
        }
        catch (Exception E)
        {
            bool Discard = true;
            bool Handled = Controller is not null && Controller.HandleApplyException(E, Element, ref Discard);
            if (Discard)
            {
                DiscardChange();
            }

            if (!Handled)
            {
                throw;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void MarkValueOutOfDate()
    {
        switch (State)
        {
            case TBoldFollowerState.bfsEmpty:
            case TBoldFollowerState.bfsCurrent:
            case TBoldFollowerState.bfsDirty:
                SetState(TBoldFollowerState.bfsValueOutOfDate);
                break;
            case TBoldFollowerState.bfsInactiveValidElement:
            case TBoldFollowerState.bfsValueOutOfDate:
            case TBoldFollowerState.bfsSubscriptionOutOfDate:
            case TBoldFollowerState.bfsActivating: // FIXME bfsActivating is a temporary fix for the delayed fetch problem
                break;
            default:
                throw new EBoldInternal(SysUtils.Format(sFollowerStateError, GetType().Name, "MarkOutOfDate"));
        }
    }

    [Delphi(PortStatus.Ported)]
    public void MarkSubscriptionOutOfDate()
    {
        switch (State)
        {
            case TBoldFollowerState.bfsEmpty:
            case TBoldFollowerState.bfsCurrent:
            case TBoldFollowerState.bfsDirty:
            case TBoldFollowerState.bfsValueOutOfDate:
                SetState(TBoldFollowerState.bfsSubscriptionOutOfDate);
                break;
            case TBoldFollowerState.bfsSubscriptionOutOfDate:
            case TBoldFollowerState.bfsActivating:
                break;
            case TBoldFollowerState.bfsInactiveValidElement:
            case TBoldFollowerState.bfsInactiveInvalidElement:
                // these should not happen, but it is safe to ignore them; a bug in the grid seems to cause these
                // when the grid is not displayed right after creation (if it is on an invisible page control)
                SetState(State);
                break;
            default:
                throw new EBoldInternal(SysUtils.Format(sFollowerStateError, GetType().Name, "MarkSubscriptionOutOfDate"));
        }
    }

    /// <summary>True when the follower or one of its owners follows the element with the controller.</summary>
    [Delphi(PortStatus.Ported)]
    public bool CheckIfInHierarchy(TBoldElement? aElement, TBoldFollowerController aController)
    {
        for (TBoldFollower? Follower = this; Follower is not null; Follower = Follower.OwningFollower)
        {
            if (Follower.Element is not null && Follower.Element == aElement && Follower.Controller == aController)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The control's value changed (or changed back): the follower becomes dirty, and applies at once with bapChange.</summary>
    [Delphi(PortStatus.Ported)]
    public void ControlledValueChanged(bool IsChanged)
    {
        if (IsChanged)
        {
            if (State != TBoldFollowerState.bfsDirty)
            {
                AssertedController.HoldsChangedValue(this);
                MarkDirty();
            }

            if (AssertedController.ApplyPolicy == TBoldApplyPolicy.bapChange)
            {
                Apply();
            }
        }
        else if (State == TBoldFollowerState.bfsDirty && AssertedController.CleanOnEqual)
        {
            DiscardChange();
        }
    }

    /// <summary>Drops a changed value; the follower shows the object's value again.</summary>
    [Delphi(PortStatus.Ported)]
    public override void DiscardChange()
    {
        if (State == TBoldFollowerState.bfsDirty)
        {
            AssertedController.ReleaseChangedValue(this);
            MarkValueOutOfDate();
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool Displayable() => bfsDisplayable.Contains(State);

    /// <summary>Makes the follower (and its subfollowers) displayable now; only within Display, or when no owner is out of date.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureDisplayable()
    {
        if (!Displayable())
        {
            Active = true;
            MakeUptoDateAndSubscribe();
            MarkClean();
        }

        RendererData.EnsureSubfollowersDisplayable();
    }

    /// <summary>True when an owning follower follows the same element with the same controller (a recursion).</summary>
    [Delphi(PortStatus.Ported)]
    public bool ExistInOwner()
    {
        if (Element is null)
        {
            return false;
        }

        TBoldElement aElement = Element;
        for (TBoldFollower? Follower = OwningFollower; Follower is not null; Follower = Follower.OwningFollower)
        {
            if (Follower.Element is not null && Follower.Element == aElement && Follower.Controller == Controller)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>True, unless the follower holds a changed value, which raises.</summary>
    [Delphi(PortStatus.Ported)]
    public bool MayChange()
    {
        if (State == TBoldFollowerState.bfsDirty)
        {
            throw new EBold(SCannotChangeStateWithModifiedValue);
        }

        return true;
    }

    /// <summary>Makes sure the elements of all followers of the controller are fetched at once; errors are ignored.</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureMulti()
    {
        var Followers = new TBoldObjectArray(10, TBoldContainerOptions.Of());
        CollectMatching(Followers, Controller!);
        if (Followers.Count > 1)
        {
            try
            {
                Controller!.MultiMakeEnsure(Followers);
            }
            catch (Exception)
            {
                // silence any exceptions
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool Active
    {
        get => GetActive();
        set => SetActive(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool ElementValid
    {
        get => GetElementValid();
        set => SetElementValid(value);
    }

    [Delphi(PortStatus.Ported)] public TBoldFollowerController? Controller => fController;
    [Delphi(PortStatus.Ported)] public TBoldFollowerController AssertedController => GetAssertedController();

    [Delphi(PortStatus.Ported)]
    public object? ControlData
    {
        get => fControlData;
        set => fControlData = value;
    }

    [Delphi(PortStatus.Ported)]
    public int CurrentIndex
    {
        get => GetCurrentIndex();
        set => SetCurrentIndex(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldElement? Element
    {
        get => fElement;
        set => SetElement(value);
    }

    [Delphi(PortStatus.Ported)]
    public int index
    {
        get => fIndex;
        set => fIndex = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldFollower? OwningFollower => fOwningFollower;
    [Delphi(PortStatus.Ported)] public TBoldRendererData RendererData => GetRendererData();
    [Delphi(PortStatus.Ported)] public TBoldFollowerState State => fState;

    [Delphi(PortStatus.Ported)]
    public bool Selected
    {
        get => GetElementFlag(BoldQueue.befFollowerSelected);
        set => SetElementFlag(BoldQueue.befFollowerSelected, value);
    }

    [Delphi(PortStatus.Ported)] public int SubFollowerCount => GetSubFollowerCount();

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldFollower? SubFollowers(int index) => GetSubFollower(index);

    [Delphi(PortStatus.Ported)] public TBoldSubscriber Subscriber => fSubscriber!;
}

/// <summary>
/// The settings of how a control follows elements: the expression and representation, the renderer, when changes are
/// applied, dragging and dropping. A control owns one controller (a grid one per column) and asks it to bring its
/// followers up to date.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldFollowerController : TBoldSubscribablePersistent
{
    private const int breVariablesRemoved = 42;
    private const int breVariablesChanged = 43;
    private const int breRendererRemoved = 44;
    private const int breRendererChanged = 45;
    internal const int breControllerChanged = 46;

    private TBoldRenderer? fUntypedRenderer;
    private bool fInternalDrag;
    private readonly object? fOwningComponent;
    private TBoldApplyPolicy fApplyPolicy;
    private TBoldDragMode fDragMode;
    private TBoldDropMode fDropMode;
    private bool fCleanOnEqual;
    private TBoldPopup fPopup;
    private TBoldRepresentation fRepresentation;
    private TBoldExpression fExpression = "";
    private TBoldFollowerEvent? fAfterMakeUptoDate;
    private TBoldFollowerEvent? fBeforeMakeUptoDate;
    private TBoldPassthroughSubscriber? fComponentSubscriber;
    private TBoldGetContextTypeEvent? FOnGetContextType;
    private TBoldOclVariables? fVariables;

    /// <summary>A controller for the component (the control) that owns it.</summary>
    [Delphi(PortStatus.Ported, Note = "The owning component is any object (a control); Delphi's is a TComponent.")]
    public TBoldFollowerController(object? aOwningComponent)
    {
        fComponentSubscriber = new TBoldPassthroughSubscriber(_Receive);
        fOwningComponent = aOwningComponent;
        fApplyPolicy = TBoldApplyPolicy.bapExit;
        fDragMode = TBoldDragMode.bdgNone;
        fDropMode = TBoldDropMode.bdpNone;
        fPopup = new TBoldPopup();
        fRepresentation = BoldDefs.brDefault;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldFollowerController.Destroy")]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fComponentSubscriber);
        base.Dispose();
    }

    /// <summary>A removed renderer or variables are forgotten; any change of them is a change of this controller.</summary>
    [Delphi(PortStatus.Ported)]
    private void _Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == breRendererRemoved)
        {
            UntypedRenderer = null;
        }

        if (RequestedEvent == breVariablesRemoved)
        {
            fVariables = null;
        }

        Changed();
    }

    [Delphi(PortStatus.Ported)] private Type GetRendererDataClass() => EffectiveRenderer.RendererDataClass;

    [Delphi(PortStatus.Ported)]
    private void SetRepresentation(TBoldRepresentation Value)
    {
        if (Value != Representation)
        {
            fRepresentation = Value;
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetExpression(string Value)
    {
        if (Value != Expression)
        {
            fExpression = Value;
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetUntypedRenderer(TBoldRenderer? NewRender)
    {
        if (NewRender != UntypedRenderer)
        {
            fUntypedRenderer = NewRender;
            Resubscribe();
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetVariables(TBoldOclVariables? Value)
    {
        if (Value != fVariables)
        {
            fVariables = Value;
            Resubscribe();
            Changed();
        }
    }

    /// <summary>Subscribes to the removal and changes of the variables and the renderer.</summary>
    [Delphi(PortStatus.Ported)]
    private void Resubscribe()
    {
        fComponentSubscriber!.CancelAllSubscriptions();
        if (Variables is not null)
        {
            Variables.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(beDestroying), breVariablesRemoved);
            Variables.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(beValueChanged), breVariablesChanged);
        }

        if (UntypedRenderer is not null)
        {
            UntypedRenderer.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(beDestroying), breRendererRemoved);
            UntypedRenderer.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(beValueChanged), breRendererChanged);
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldExternalVariableList? GetVariableList() => Variables?.VariableList;
    [Delphi(PortStatus.Ported)] private bool GetSupportsMulti() => EffectiveRenderer.SupportsMulti;

    /// <summary>Offers an apply error to the exception handler of the owning component; true when it handled it.</summary>
    [Delphi(PortStatus.Ported)]
    internal bool HandleApplyException(Exception E, TBoldElement? Elem, ref bool Discard)
    {
        TBoldExceptionHandler? ExceptionHandler = TBoldExceptionHandler.FindExceptionHandler(fOwningComponent);
        bool Result = ExceptionHandler is not null;
        if (Result)
        {
            ExceptionHandler!.HandleApplyException(E, fOwningComponent, Elem, ref Discard, ref Result);
        }

        return Result;
    }

    /// <summary>Offers a display error to the exception handler of the owning component; true when it handled it.</summary>
    [Delphi(PortStatus.Ported)]
    internal bool HandleDisplayException(Exception E, TBoldElement? Elem)
    {
        TBoldExceptionHandler? ExceptionHandler = TBoldExceptionHandler.FindExceptionHandler(fOwningComponent);
        bool Result = ExceptionHandler is not null;
        if (Result)
        {
            ExceptionHandler!.HandleDisplayException(E, fOwningComponent, Elem, ref Result);
        }

        return Result;
    }

    [Delphi(PortStatus.Ported, Note = "Gives the owning component, which may be any object.")]
    protected new object? GetOwner() => OwningComponent;

    /// <summary>The name of the controller in messages: the owning component's name and the controller's class.</summary>
    public override string GetNamePath()
    {
        string ownerName = OwningComponent switch
        {
            null => "",
            TBoldSubscribableComponent component => component.Name,
            IComponent component => component.Site?.Name ?? OwningComponent.GetType().GetProperty("Name")?.GetValue(OwningComponent) as string ?? "",
            _ => OwningComponent.GetType().GetProperty("Name")?.GetValue(OwningComponent) as string ?? "",
        };
        return ownerName != "" ? ownerName + "." + GetType().Name : GetType().Name;
    }

    /// <summary>The renderer to use: the default renderer here.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldRenderer GetEffectiveRenderer() => BoldControlPack.DefaultRenderer();

    [Delphi(PortStatus.Ported)]
    protected virtual TBoldElementTypeInfo? GetContextType() => FOnGetContextType?.Invoke();

    /// <summary>Brings the follower up to date: here only whether the value may be changed.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual void DoMakeUptodateAndSubscribe(TBoldFollower Follower, bool Subscribe)
    {
        Follower.RendererData.MayModify = EffectiveRenderer.MayModify(Follower.Element, Representation, Expression,
            GetVariableListAndSubscribe(Follower.Subscriber), Follower.Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void DoMultiMakeUptodateAndSubscribe(TBoldObjectArray Followers) =>
        throw new EBoldInternal("DoMultiMakeUptodateAndSubscribe: called when Multi not supported");

    /// <summary>Tells subscribers (the followers) that the controller changed.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void Changed() => SendEvent(this, beValueChanged);

    /// <summary>Copies the settings of another controller of the same class.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DoAssign(TPersistent Source)
    {
        var source = (TBoldFollowerController)Source;
        DragMode = source.DragMode;
        DropMode = source.DropMode;
        InternalDrag = source.InternalDrag;
        Representation = source.Representation;
        Expression = source.Expression;
        ApplyPolicy = source.ApplyPolicy;
        CleanOnEqual = source.CleanOnEqual;
        Popup.Assign(source.Popup);
        if (source.UntypedRenderer is not null)
        {
            UntypedRenderer = source.UntypedRenderer;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void CleanRendererData(TBoldRendererData RendererData)
    {
        // do nothing
    }

    /// <summary>Evaluates the expression on all the followers' objects at once, so their values are fetched together.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested AddObject is a private helper.")]
    protected internal void MultiMakeEnsure(TBoldObjectArray Followers)
    {
        using var ObjectList = new TBoldObjectList();
        TBoldClassTypeInfo? BoldType = null;
        for (int i = 0; i < Followers.Count; i++)
        {
            var Follower = (TBoldFollower)Followers[i]!;
            if (Follower.Element is TBoldObject boldObject)
            {
                AddObject(ObjectList, boldObject, ref BoldType);
            }
        }

        if (BoldType is not null)
        {
            ObjectList.EnsureObjects();
            if (ObjectList.Count > 1 && Expression != "")
            {
                TBoldListTypeInfo? ListType = ((TBoldSystemTypeInfo)BoldType.SystemTypeInfo!).ListTypeInfoByElement(BoldType);
                var RealObjectList = (TBoldObjectList)TBoldMemberFactory.CreateMemberFromBoldType(ListType);
                RealObjectList.AddList(ObjectList);
                using var ie = new TBoldIndirectElement();
                RealObjectList.EvaluateExpression(Expression, ie);
            }
        }
    }

    private static void AddObject(TBoldObjectList ObjectList, TBoldObject Obj, ref TBoldClassTypeInfo? BoldType)
    {
        ObjectList.Add(Obj);
        BoldType = BoldType is null ? Obj.BoldClassTypeInfo : BoldType.LeastCommonSuperClass(Obj.BoldClassTypeInfo);
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetSupportsMultiEnsure() => false;

    [Delphi(PortStatus.Ported)] protected TBoldRenderer EffectiveRenderer => GetEffectiveRenderer();
    [Delphi(PortStatus.Ported)] protected internal Type RendererDataClass => GetRendererDataClass();
    [Delphi(PortStatus.Ported)] protected internal object? OwningComponent => fOwningComponent;

    [Delphi(PortStatus.Ported)]
    protected bool InternalDrag
    {
        get => fInternalDrag;
        set => fInternalDrag = value;
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldRepresentation Representation
    {
        get => fRepresentation;
        set => SetRepresentation(value);
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldExpression Expression
    {
        get => fExpression;
        set => SetExpression(value);
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldRenderer? UntypedRenderer
    {
        get => fUntypedRenderer;
        set => SetUntypedRenderer(value);
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldOclVariables? Variables
    {
        get => fVariables;
        set => SetVariables(value);
    }

    /// <summary>Copies the settings of another controller of the same class; other sources raise.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is not null && Source.GetType() == GetType())
        {
            DoAssign(Source);
        }
        else
        {
            base.Assign(Source);
        }
    }

    /// <summary>Applies the follower's value; controllers of editable values override it.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void MakeClean(TBoldFollower Follower) =>
        throw new EBoldInternal(SysUtils.Format(sNotImplemented, GetType().Name, "MakeClean")); // do not localize

    [Delphi(PortStatus.Ported)]
    public bool MayModify(TBoldFollower Follower) => Follower.RendererData.MayModify;

    [Delphi(PortStatus.Ported)]
    public void HoldsChangedValue(TBoldFollower Follower) =>
        EffectiveRenderer.HoldsChangedValue(Follower.Element, Representation, Expression, VariableList, Follower.Subscriber);

    [Delphi(PortStatus.Ported)]
    public void ReleaseChangedValue(TBoldFollower Follower) =>
        EffectiveRenderer.ReleaseChangedValue(Follower.Element, Representation, Expression, VariableList, Follower.Subscriber);

    /// <summary>The variables' list, after subscribing the subscriber to their handles.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldExternalVariableList? GetVariableListAndSubscribe(TBoldSubscriber? Subscriber)
    {
        TBoldExternalVariableList? result = GetVariableList();
        if (Subscriber is not null && Variables is not null)
        {
            Variables.SubscribeToHandles(Subscriber);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void StartDrag(TBoldFollower Follower) => EffectiveRenderer.StartDrag(Follower.Element, DragMode, Follower.RendererData);

    [Delphi(PortStatus.Ported)]
    public void EndDrag() => EffectiveRenderer.EndDrag(DragMode, InternalDrag);

    /// <summary>Brings the follower up to date, between the BeforeMakeUptoDate and AfterMakeUptoDate events.</summary>
    [Delphi(PortStatus.Ported)]
    public void MakeUptodateAndSubscribe(TBoldFollower Follower, bool Subscribe)
    {
        BeforeMakeUptoDate?.Invoke(Follower);
        try
        {
            DoMakeUptodateAndSubscribe(Follower, Subscribe);
        }
        finally
        {
            AfterMakeUptoDate?.Invoke(Follower);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SubscribeToElement(TBoldElement? Element, TBoldSubscriber? Subscriber) =>
        EffectiveRenderer.SubscribeToElement(Element, Representation, Expression, Subscriber, VariableList);

    [Delphi(PortStatus.Ported)]
    public void MultiMakeUptodateAndSubscribe(TBoldObjectArray Followers)
    {
        if (BeforeMakeUptoDate is not null)
        {
            for (int i = 0; i < Followers.Count; i++)
            {
                BeforeMakeUptoDate((TBoldFollower)Followers[i]!);
            }
        }

        try
        {
            DoMultiMakeUptodateAndSubscribe(Followers);
        }
        finally
        {
            if (AfterMakeUptoDate is not null)
            {
                for (int i = 0; i < Followers.Count; i++)
                {
                    AfterMakeUptoDate((TBoldFollower)Followers[i]!);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool DragOver(TBoldFollower Follower, TBoldElement? ReceivingElement, int dropindex) =>
        EffectiveRenderer.DragOver(ReceivingElement, DropMode, InternalDrag, Follower.RendererData, dropindex);

    /// <summary>Drops on the element (activating the target form first when asked); an error is offered to the exception handler.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void DragDrop(TBoldFollower Follower, TBoldElement? ReceivingElement, int dropindex)
    {
        if (BoldGUI.BoldGUIHandler().ActivateTargetFormOnDrop)
        {
            bool Success = false;
            if (OwningComponent is not null)
            {
                Success = BoldGUI.BoldGUIHandler().TryToFocusHostingForm(OwningComponent);
            }

            if (!Success && Follower.MatchObject is IComponent)
            {
                BoldGUI.BoldGUIHandler().TryToFocusHostingForm(Follower.MatchObject);
            }
        }

        try
        {
            EffectiveRenderer.DragDrop(ReceivingElement, DropMode, dropindex);
        }
        catch (Exception E)
        {
            bool Discard = true;
            bool Handled = HandleApplyException(E, ReceivingElement, ref Discard);
            if (Discard)
            {
                Follower.DiscardChange();
            }

            if (!Handled)
            {
                throw;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void DrawOnCanvas(TBoldFollower Follower, TCanvas Canvas, Rectangle Rect, TAlignment Alignment, Point Margins) =>
        EffectiveRenderer.DrawOnCanvas(Follower, Canvas, Rect, Alignment, Margins);

    [Delphi(PortStatus.Ported)]
    public TBoldApplyPolicy ApplyPolicy
    {
        get => fApplyPolicy;
        set => fApplyPolicy = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool CleanOnEqual
    {
        get => fCleanOnEqual;
        set => fCleanOnEqual = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPopup Popup
    {
        get => fPopup;
        set => fPopup = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerEvent? AfterMakeUptoDate
    {
        get => fAfterMakeUptoDate;
        set => fAfterMakeUptoDate = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFollowerEvent? BeforeMakeUptoDate
    {
        get => fBeforeMakeUptoDate;
        set => fBeforeMakeUptoDate = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldGetContextTypeEvent? OnGetContextType
    {
        get => FOnGetContextType;
        set => FOnGetContextType = value;
    }

    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ContextType => GetContextType();
    [Delphi(PortStatus.Ported)] public TBoldExternalVariableList? VariableList => GetVariableList();
    [Delphi(PortStatus.Ported)] public bool SupportsMulti => GetSupportsMulti();
    [Delphi(PortStatus.Ported)] public bool SupportsMultiEnsure => GetSupportsMultiEnsure();

    [Delphi(PortStatus.Ported)]
    public TBoldDragMode DragMode
    {
        get => fDragMode;
        set => fDragMode = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldDropMode DropMode
    {
        get => fDropMode;
        set => fDropMode = value;
    }
}

/// <summary>A renderer for a single value (not a list).</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSingleRenderer : TBoldRenderer
{
    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    protected TBoldSingleRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    /// <summary>Moves the element's value to the renderer data and subscribes to what it depends on.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void MakeUptodateAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldFollowerController FollowerController, TBoldSubscriber? Subscriber);
}

/// <summary>A controller of a single value, which its single renderer brings up to date.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSingleFollowerController : TBoldFollowerController
{
    /// <summary>A controller for the component (the control) that owns it.</summary>
    public TBoldSingleFollowerController(object? aOwningComponent)
        : base(aOwningComponent)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void DoMakeUptodateAndSubscribe(TBoldFollower Follower, bool Subscribe)
    {
        base.DoMakeUptodateAndSubscribe(Follower, Subscribe);
        ((TBoldSingleRenderer)EffectiveRenderer).MakeUptodateAndSubscribe(Follower.Element, Follower.RendererData, this, Subscribe ? Follower.Subscriber : null);
    }

    /// <summary>The expression and representation, variables and apply settings, published here.</summary>
    public new TBoldRepresentation Representation
    {
        get => base.Representation;
        set => base.Representation = value;
    }

    public new TBoldExpression Expression
    {
        get => base.Expression;
        set => base.Expression = value;
    }

    public new TBoldOclVariables? Variables
    {
        get => base.Variables;
        set => base.Variables = value;
    }

    public new Type RendererDataClass => base.RendererDataClass;
}

/// <summary>The settings of a Bold popup menu: whether it is on, and its insert, delete and move items.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPopup : TPersistent
{
    private bool FEnable; // FIXME notify owner of change here
    private bool FInsertNew;
    private TBoldPopupDeleteType FDelete;
    private bool FMove;

    /// <summary>The popup menu for the control and element (remembered in the GUI handler).</summary>
    [Delphi(PortStatus.Ported, Note = "Takes and gives any object: the control and menu belong to the GUI toolkit (BoldGUI.BoldPopupMenu).")]
    public object? GetMenu(object? CONTROL, TBoldElement? Element)
    {
        object? Result = null;
        BoldGUI.BoldGUIHandler().PopupElement = Element;
        BoldGUI.BoldGUIHandler().PopupControl = CONTROL;
        // fixme build actual menu
        if (!Enable)
        {
            Result = BoldGUI.BoldPopupMenu;
        }

        return Result;
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldPopup popup)
        {
            Enable = popup.Enable;
            InsertNew = popup.InsertNew;
            Delete = popup.Delete;
            Move = popup.Move;
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool Enable
    {
        get => FEnable;
        set => FEnable = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool InsertNew
    {
        get => FInsertNew;
        set => FInsertNew = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPopupDeleteType Delete
    {
        get => FDelete;
        set => FDelete = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool Move
    {
        get => FMove;
        set => FMove = value;
    }
}

/// <summary>Unit-level routines of BoldControlPack.</summary>
public static partial class BoldControlPack
{
    private static TBoldRenderer? G_DefaultRenderer;

    /// <summary>True when the element is of the type.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldTestType(object? element, Type TypeOrInterface) => TypeOrInterface.IsInstanceOfType(element);

    /// <summary>The renderer of controllers that have none of their own.</summary>
    internal static TBoldRenderer DefaultRenderer() => G_DefaultRenderer ??= new TBoldRenderer();
}
