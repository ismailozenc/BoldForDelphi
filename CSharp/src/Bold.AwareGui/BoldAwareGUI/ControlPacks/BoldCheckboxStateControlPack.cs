// Delphi: Source/BoldAwareGUI/ControlPacks/BoldCheckboxStateControlPack.pas
// Port map: CSharp/port/units/BoldCheckboxStateControlPack.md

using System.ComponentModel;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldCheckboxStateControlPack", Symbol = "initialization", Note = "The default renderer is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldCheckboxStateControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Gives the check box state of an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate TCheckBoxState TBoldGetAsCheckBoxState(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Sets an element from a check box state.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSetAsCheckBoxState(TBoldElement? Element, TCheckBoxState newValue, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Says whether a check box state is valid for an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldValidateCheckBoxState(TBoldElement? Element, TCheckBoxState Value, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Says whether the check box state in the control differs from the element's.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldCheckBoxIsChanged(TBoldCheckBoxRendererData RendererData, TCheckBoxState NewValue, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>The check box state a control shows: as the element gave it, and as the user has it now.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldCheckBoxRendererData : TBoldRendererData
{
    private TCheckBoxState fOldvalue;
    private TCheckBoxState fCurrentValue;

    /// <summary>Renderer data for the follower.</summary>
    public TBoldCheckBoxRendererData(TBoldFollower OwningFollower)
        : base(OwningFollower)
    {
    }

    [Delphi(PortStatus.Ported)]
    public TCheckBoxState Oldvalue
    {
        get => fOldvalue;
        set => fOldvalue = value;
    }

    [Delphi(PortStatus.Ported)]
    public TCheckBoxState CurrentValue
    {
        get => fCurrentValue;
        set => fCurrentValue = value;
    }
}

/// <summary>Shows a boolean element as a check box state (grayed for null) and sets it from one.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsCheckBoxStateRenderer : TBoldSingleRenderer
{
    private static TBoldAsCheckBoxStateRenderer? DefaultAsCheckBoxStateRenderer;

    private TBoldGetAsCheckBoxState? FOnGetAsCheckBoxState;
    private TBoldSetAsCheckBoxState? FOnSetAsCheckBoxState;
    private TBoldValidateCheckBoxState? FOnValidate;
    private TBoldCheckBoxIsChanged? fOnIsChanged;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldAsCheckBoxStateRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type GetRendererDataClass() => typeof(TBoldCheckBoxRendererData);

    /// <summary>The renderer of check box controllers that have none of their own.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldAsCheckBoxStateRenderer DefaultRenderer() => DefaultAsCheckBoxStateRenderer ??= new TBoldAsCheckBoxStateRenderer();

    /// <summary>Checked or unchecked for a boolean value, grayed for null, no value or another value; subscribes to it.</summary>
    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual TCheckBoxState DefaultGetAsCheckBoxStateAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        TCheckBoxState Result = TCheckBoxState.cbGrayed;
        if (Element is not null)
        {
            using var E = new TBoldIndirectElement();
            Element.EvaluateAndSubscribeToExpression(Expression, Subscriber, E, false, false, VariableList);
            if (E.Value is TBABoolean boolean)
            {
                Result = boolean.IsNull ? TCheckBoxState.cbGrayed : boolean.AsBoolean ? TCheckBoxState.cbChecked : TCheckBoxState.cbUnchecked;
            }
        }

        return Result;
    }

    /// <summary>Sets the boolean value: null for grayed; raises when the value is not a boolean.</summary>
    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual void DefaultSetAsCheckBoxState(TBoldElement? Element, TCheckBoxState newValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        if (GetExpressionAsDirectElement(Element, Expression, VariableList) is not TBABoolean boolean)
        {
            throw new EBold(SysUtils.Format(sCannotSetValue, GetType().Name));
        }

        switch (newValue)
        {
            case TCheckBoxState.cbGrayed:
                boolean.SetToNull();
                break;
            case TCheckBoxState.cbChecked:
                boolean.AsBoolean = true;
                break;
            default:
                boolean.AsBoolean = false;
                break;
        }
    }

    [Delphi(PortStatus.Ported, Note = "An instance method; Delphi's is a virtual class method.")]
    public virtual bool DefaultValidateCheckBoxState(TBoldElement? Element, TCheckBoxState Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) => true;

    /// <summary>Puts the element's state in the renderer data, as both the old and the current state.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeUptodateAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        var Controller = (TBoldCheckBoxStateFollowerController)FollowerController;
        TCheckBoxState s = GetAsCheckBoxStateAndSubscribe(Element, Controller.Representation, Controller.Expression, Controller.GetVariableListAndSubscribe(Subscriber), Subscriber);
        var data = (TBoldCheckBoxRendererData)RendererData;
        data.Oldvalue = s;
        data.CurrentValue = s;
    }

    [Delphi(PortStatus.Ported)]
    public bool DefaultIsChanged(TBoldCheckBoxRendererData RendererData, TCheckBoxState NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        NewValue != RendererData.Oldvalue;

    /// <summary>The state: OnSubscribe places the subscriptions when set, and OnGetAsCheckBoxState gives the state when set.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual TCheckBoxState GetAsCheckBoxStateAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (OnSubscribe is not null && Subscriber is not null)
        {
            if (Element is not null)
            {
                OnSubscribe(Element, Representation, Expression, Subscriber);
            }

            Subscriber = null;
        }

        return OnGetAsCheckBoxState is not null
            ? OnGetAsCheckBoxState(Element, Representation, Expression)
            : DefaultGetAsCheckBoxStateAndSubscribe(Element, Representation, Expression, VariableList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void SetAsCheckBoxState(TBoldElement? Element, TCheckBoxState Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        if (OnSetAsCheckBoxState is not null)
        {
            OnSetAsCheckBoxState(Element, Value, Representation, Expression);
        }
        else
        {
            DefaultSetAsCheckBoxState(Element, Value, Representation, Expression, VariableList);
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual bool ValidateCheckBoxState(TBoldElement? Element, TCheckBoxState Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        OnValidateCheckBoxState is not null
            ? OnValidateCheckBoxState(Element, Value, Representation, Expression)
            : DefaultValidateCheckBoxState(Element, Value, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public bool IsChanged(TBoldCheckBoxRendererData RendererData, TCheckBoxState NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        fOnIsChanged is not null
            ? fOnIsChanged(RendererData, NewValue, Representation, Expression)
            : DefaultIsChanged(RendererData, NewValue, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public TBoldGetAsCheckBoxState? OnGetAsCheckBoxState
    {
        get => FOnGetAsCheckBoxState;
        set => FOnGetAsCheckBoxState = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSetAsCheckBoxState? OnSetAsCheckBoxState
    {
        get => FOnSetAsCheckBoxState;
        set => FOnSetAsCheckBoxState = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldValidateCheckBoxState? OnValidateCheckBoxState
    {
        get => FOnValidate;
        set => FOnValidate = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldCheckBoxIsChanged? OnIsChanged
    {
        get => fOnIsChanged;
        set => fOnIsChanged = value;
    }
}

/// <summary>The controller of a check box (or another control showing a boolean as a check box state).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldCheckBoxStateFollowerController : TBoldSingleFollowerController
{
    /// <summary>A controller for the component (the control) that owns it.</summary>
    public TBoldCheckBoxStateFollowerController(object? aOwningComponent)
        : base(aOwningComponent)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAsCheckBoxStateRenderer GetEffectiveAsCheckBoxStateRenderer() => Renderer ?? TBoldAsCheckBoxStateRenderer.DefaultRenderer();

    [Delphi(PortStatus.Ported)] private TBoldAsCheckBoxStateRenderer? GetRenderer() => (TBoldAsCheckBoxStateRenderer?)UntypedRenderer;
    [Delphi(PortStatus.Ported)] private void SetRenderer(TBoldAsCheckBoxStateRenderer? Value) => UntypedRenderer = Value;

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => EffectiveAsCheckBoxStateRenderer;

    [Delphi(PortStatus.Ported)] protected TBoldAsCheckBoxStateRenderer EffectiveAsCheckBoxStateRenderer => GetEffectiveAsCheckBoxStateRenderer();

    [Delphi(PortStatus.Ported)]
    public TCheckBoxState GetCurrentAsCheckBoxState(TBoldFollower Follower) => ((TBoldCheckBoxRendererData)Follower.RendererData).CurrentValue;

    /// <summary>Writes the control's state back.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeClean(TBoldFollower Follower)
    {
        ReleaseChangedValue(Follower); // note, must do first, since set can change element
        SetAsCheckBoxState(GetCurrentAsCheckBoxState(Follower), Follower);
    }

    /// <summary>The control's state is now the value: the follower becomes dirty when it differs from the element's.</summary>
    [Delphi(PortStatus.Ported)]
    public void MayHaveChanged(TCheckBoxState NewValue, TBoldFollower Follower)
    {
        if (bfsDisplayable.Contains(Follower.State))
        {
            var data = (TBoldCheckBoxRendererData)Follower.RendererData;
            data.CurrentValue = NewValue;
            Follower.ControlledValueChanged(EffectiveAsCheckBoxStateRenderer.IsChanged(data, NewValue, Representation, Expression, VariableList));
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SetAsCheckBoxState(TCheckBoxState Value, TBoldFollower Follower) =>
        EffectiveAsCheckBoxStateRenderer.SetAsCheckBoxState(Follower.Element, Value, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public bool ValidateCheckBoxState(TCheckBoxState Value, TBoldFollower Follower) =>
        EffectiveAsCheckBoxStateRenderer.ValidateCheckBoxState(Follower.Element, Value, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public TBoldAsCheckBoxStateRenderer? Renderer
    {
        get => GetRenderer();
        set => SetRenderer(value);
    }
}
