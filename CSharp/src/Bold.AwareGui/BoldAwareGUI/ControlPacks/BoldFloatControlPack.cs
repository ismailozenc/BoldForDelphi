// Delphi: Source/BoldAwareGUI/ControlPacks/BoldFloatControlPack.pas
// Port map: CSharp/port/units/BoldFloatControlPack.md

using System.ComponentModel;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldFloatControlPack", Symbol = "initialization", Note = "The default renderer is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldFloatControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Gives the floating point value of an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate double TBoldGetAsFloatEvent(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Sets an element from a floating point value.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSetAsFloatEvent(TBoldElement? Element, double Value, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Says whether the floating point value in the control differs from the element's.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldFloatIsChangedEvent(TBoldFloatRendererData RenderData, double NewValue, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>The floating point value a control shows: as the element gave it, and as the user has it now.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFloatRendererData : TBoldRendererData
{
    private double FOldFloatValue;
    private double FCurrentFloatValue;

    /// <summary>Renderer data for the follower.</summary>
    public TBoldFloatRendererData(TBoldFollower OwningFollower)
        : base(OwningFollower)
    {
    }

    [Delphi(PortStatus.Ported)]
    public double CurrentFloatValue
    {
        get => FCurrentFloatValue;
        set => FCurrentFloatValue = value;
    }

    [Delphi(PortStatus.Ported)]
    public double OldFloatValue
    {
        get => FOldFloatValue;
        set => FOldFloatValue = value;
    }
}

/// <summary>Shows elements as floating point values (for track bars, spin edits, date pickers) and sets them from floating point values.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsFloatRenderer : TBoldSingleRenderer
{
    private static TBoldAsFloatRenderer? G_DefaultAsFloatRenderer;

    private TBoldGetAsFloatEvent? FOnGetAsFloat;
    private TBoldSetAsFloatEvent? FOnSetAsFloat;
    private TBoldFloatIsChangedEvent? FOnIsChanged;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldAsFloatRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type GetRendererDataClass() => typeof(TBoldFloatRendererData);

    /// <summary>The floating point value of the expression's value, subscribing to it; 0 without an element.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual double DefaultGetAsFloatAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        double Result = 0;
        if (Element is not null)
        {
            using var IndirectElement = new TBoldIndirectElement();
            Element.EvaluateAndSubscribeToExpression(Expression, Subscriber, IndirectElement, false, false, VariableList);
            if (IndirectElement.Value is not null)
            {
                Result = IndirectElement.Value switch
                {
                    TBANumeric numeric => numeric.AsFloat,
                    TBADate date => date.AsDate.ToOADate(),
                    TBATime time => time.AsTime.ToOADate(),
                    TBADateTime dateTime => dateTime.AsDateTime.ToOADate(),
                    _ => throw new EBold(SysUtils.Format(sCannotGetFloatValueFromElement, IndirectElement.Value.GetType().Name)),
                };
            }
        }

        return Result;
    }

    /// <summary>Sets the expression's value from the floating point value; raises when it is not a floating point attribute.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DefaultSetAsFloat(TBoldElement? Element, double Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        if (ValueElement is TBAFloat floatAttribute)
        {
            floatAttribute.AsFloat = Value;
        }
        else if (ValueElement is TBADateTime dateTime)
        {
            dateTime.AsDateTime = DateTime.FromOADate(Value);
        }
        else
        {
            throw new EBold(SysUtils.Format(sCannotSetFloatValueOnElement, ValueElement?.GetType().Name ?? "nil"));
        }
    }

    /// <summary>The floating point value: OnSubscribe places the subscriptions when set, and OnGetAsFloat gives the value when set.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual double GetAsFloatAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (OnSubscribe is not null && Subscriber is not null)
        {
            if (Element is not null)
            {
                OnSubscribe(Element, Representation, Expression, Subscriber);
            }

            Subscriber = null;
        }

        return OnGetAsFloat is not null
            ? OnGetAsFloat(Element, Representation, Expression)
            : DefaultGetAsFloatAndSubscribe(Element, Representation, Expression, VariableList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetAsFloat(TBoldElement? Element, double Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        if (FOnSetAsFloat is not null)
        {
            OnSetAsFloat!(Element, Value, Representation, Expression);
        }
        else
        {
            DefaultSetAsFloat(Element, Value, Representation, Expression, VariableList);
        }
    }

    /// <summary>The renderer of floating point controllers that have none of their own.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldAsFloatRenderer DefaultRenderer() => G_DefaultAsFloatRenderer ??= new TBoldAsFloatRenderer();

    [Delphi(PortStatus.Ported)]
    public bool DefaultIsChanged(TBoldFloatRendererData RendererData, double NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        NewValue != RendererData.OldFloatValue;

    /// <summary>True when the expression's value is a floating point attribute the subscriber may change (evaluating the expression once).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool DefaultMayModify(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        // Note! We don't call inherited DefaultMayModify to prevent evaluation of expression two times!
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        return (ValueElement is TBAFloat || ValueElement is TBADateTime) && ValueElement.ObserverMayModify(Subscriber);
    }

    /// <summary>Puts the element's floating point value in the renderer data, as both the old and the current value.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeUptodateAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        var Controller = (TBoldFloatFollowerController)FollowerController;
        double Value = GetAsFloatAndSubscribe(Element, Controller.Representation, Controller.Expression, Controller.GetVariableListAndSubscribe(Subscriber), Subscriber);
        var data = (TBoldFloatRendererData)RendererData;
        data.OldFloatValue = Value;
        data.CurrentFloatValue = Value;
    }

    [Delphi(PortStatus.Ported)]
    public bool IsChanged(TBoldFloatRendererData RendererData, double NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        FOnIsChanged is not null
            ? FOnIsChanged(RendererData, NewValue, Representation, Expression)
            : DefaultIsChanged(RendererData, NewValue, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public TBoldGetAsFloatEvent? OnGetAsFloat
    {
        get => FOnGetAsFloat;
        set => FOnGetAsFloat = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSetAsFloatEvent? OnSetAsFloat
    {
        get => FOnSetAsFloat;
        set => FOnSetAsFloat = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldFloatIsChangedEvent? OnIsChanged
    {
        get => FOnIsChanged;
        set => FOnIsChanged = value;
    }
}

/// <summary>The controller of a control that shows an element as a floating point value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFloatFollowerController : TBoldSingleFollowerController
{
    /// <summary>A controller for the component (the control) that owns it.</summary>
    public TBoldFloatFollowerController(object? aOwningComponent)
        : base(aOwningComponent)
    {
    }

    [Delphi(PortStatus.Ported)] private TBoldAsFloatRenderer? GetRenderer() => (TBoldAsFloatRenderer?)UntypedRenderer;

    /// <summary>Sets the renderer and says the controller changed.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetRenderer(TBoldAsFloatRenderer? Value)
    {
        if (Value != Renderer)
        {
            if (OwningComponent is System.ComponentModel.IComponent owner)
            {
                Value?.FreeNotification(owner);
            }

            UntypedRenderer = Value;
            Changed();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAsFloatRenderer GetEffectiveAsFloatRenderer() => Renderer ?? TBoldAsFloatRenderer.DefaultRenderer();

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => EffectiveAsFloatRenderer;

    [Delphi(PortStatus.Ported)] protected TBoldAsFloatRenderer EffectiveAsFloatRenderer => GetEffectiveAsFloatRenderer();

    [Delphi(PortStatus.Ported)]
    public double GetCurrentAsFloat(TBoldFollower Follower) => ((TBoldFloatRendererData)Follower.RendererData).CurrentFloatValue;

    /// <summary>Writes the control's floating point value back.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeClean(TBoldFollower Follower)
    {
        ReleaseChangedValue(Follower);
        SetAsFloat(GetCurrentAsFloat(Follower), Follower);
    }

    /// <summary>The control's floating point value is now the value: the follower becomes dirty when it differs from the element's.</summary>
    [Delphi(PortStatus.Ported)]
    public void MayHaveChanged(double NewValue, TBoldFollower Follower)
    {
        if (bfsDisplayable.Contains(Follower.State))
        {
            var data = (TBoldFloatRendererData)Follower.RendererData;
            data.CurrentFloatValue = NewValue;
            Follower.ControlledValueChanged(EffectiveAsFloatRenderer.IsChanged(data, NewValue, Representation, Expression, VariableList));
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SetAsFloat(double Value, TBoldFollower Follower) =>
        EffectiveAsFloatRenderer.SetAsFloat(Follower.Element, Value, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public TBoldAsFloatRenderer? Renderer
    {
        get => GetRenderer();
        set => SetRenderer(value);
    }
}
