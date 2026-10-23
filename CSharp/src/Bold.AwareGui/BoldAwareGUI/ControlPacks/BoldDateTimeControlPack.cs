// Delphi: Source/BoldAwareGUI/ControlPacks/BoldDateTimeControlPack.pas
// Port map: CSharp/port/units/BoldDateTimeControlPack.md

using System.ComponentModel;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldDateTimeControlPack", Symbol = "initialization", Note = "The default renderer is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDateTimeControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Gives the date and time value of an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate TDateTime TBoldGetAsDateTimeEvent(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Sets an element from a date and time value.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSetAsDateTimeEvent(TBoldElement? Element, TDateTime Value, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Says whether the date and time value in the control differs from the element's.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldDateTimeIsChangedEvent(TBoldDateTimeRendererData RenderData, TDateTime NewValue, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>The date and time value a control shows: as the element gave it, and as the user has it now.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDateTimeRendererData : TBoldRendererData
{
    private TDateTime FOldDateTimeValue;
    private TDateTime FCurrentDateTimeValue;

    /// <summary>Renderer data for the follower.</summary>
    public TBoldDateTimeRendererData(TBoldFollower OwningFollower)
        : base(OwningFollower)
    {
    }

    [Delphi(PortStatus.Ported)]
    public TDateTime CurrentDateTimeValue
    {
        get => FCurrentDateTimeValue;
        set => FCurrentDateTimeValue = value;
    }

    [Delphi(PortStatus.Ported)]
    public TDateTime OldDateTimeValue
    {
        get => FOldDateTimeValue;
        set => FOldDateTimeValue = value;
    }
}

/// <summary>Shows elements as date and time values (for track bars, spin edits, date pickers) and sets them from date and time values.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsDateTimeRenderer : TBoldSingleRenderer
{
    private static TBoldAsDateTimeRenderer? DefaultAsDateTimeRenderer;

    private TBoldGetAsDateTimeEvent? FOnGetAsDateTime;
    private TBoldSetAsDateTimeEvent? FOnSetAsDateTime;
    private TBoldDateTimeIsChangedEvent? FOnIsChanged;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldAsDateTimeRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type GetRendererDataClass() => typeof(TBoldDateTimeRendererData);

    /// <summary>The date and time value of the expression's value, subscribing to it; 0 without an element.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TDateTime DefaultGetAsDateTimeAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        TDateTime Result = default;
        if (Element is not null)
        {
            using var IndirectElement = new TBoldIndirectElement();
            Element.EvaluateAndSubscribeToExpression(Expression, Subscriber, IndirectElement, false, false, VariableList);
            if (IndirectElement.Value is not null)
            {
                Result = IndirectElement.Value switch
                {
                    TBADateTime dateTime => dateTime.AsDateTime,
                    TBADate date => date.AsDate,
                    TBATime time => time.AsTime,
                    _ => throw new EBold(SysUtils.Format(sCannotGetDateTimeValueFromElement, IndirectElement.Value.GetType().Name)),
                };
            }
        }

        return Result;
    }

    /// <summary>Sets the expression's value from the date and time value; raises when it is not a date and time attribute.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DefaultSetAsDateTime(TBoldElement? Element, TDateTime Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        switch (ValueElement)
        {
            case TBADateTime dateTime:
                dateTime.AsDateTime = Value;
                break;
            case TBADate date:
                date.AsDate = Value;
                break;
            case TBATime time:
                time.AsTime = Value;
                break;
            default:
                throw new EBold(SysUtils.Format(sCannotSetDateTimeValueOnElement, ValueElement?.GetType().Name ?? "nil"));
        }
    }

    /// <summary>The date and time value: OnSubscribe places the subscriptions when set, and OnGetAsDateTime gives the value when set.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TDateTime GetAsDateTimeAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (OnSubscribe is not null && Subscriber is not null)
        {
            if (Element is not null)
            {
                OnSubscribe(Element, Representation, Expression, Subscriber);
            }

            Subscriber = null;
        }

        return OnGetAsDateTime is not null
            ? OnGetAsDateTime(Element, Representation, Expression)
            : DefaultGetAsDateTimeAndSubscribe(Element, Representation, Expression, VariableList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetAsDateTime(TBoldElement? Element, TDateTime Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        if (FOnSetAsDateTime is not null)
        {
            OnSetAsDateTime!(Element, Value, Representation, Expression);
        }
        else
        {
            DefaultSetAsDateTime(Element, Value, Representation, Expression, VariableList);
        }
    }

    /// <summary>The renderer of date and time controllers that have none of their own.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldAsDateTimeRenderer DefaultRenderer() => DefaultAsDateTimeRenderer ??= new TBoldAsDateTimeRenderer();

    [Delphi(PortStatus.Ported)]
    public bool DefaultIsChanged(TBoldDateTimeRendererData RendererData, TDateTime NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        NewValue != RendererData.OldDateTimeValue;

    /// <summary>True when the expression's value is a date and time attribute the subscriber may change (evaluating the expression once).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool DefaultMayModify(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        // Note! We don't call inherited DefaultMayModify to prevent evaluation of expression two times!
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        return ValueElement is TBAMoment && ValueElement.ObserverMayModify(Subscriber);
    }

    /// <summary>Puts the element's date and time value in the renderer data, as both the old and the current value.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeUptodateAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        var Controller = (TBoldDateTimeFollowerController)FollowerController;
        TDateTime Value = GetAsDateTimeAndSubscribe(Element, Controller.Representation, Controller.Expression, Controller.GetVariableListAndSubscribe(Subscriber), Subscriber);
        var data = (TBoldDateTimeRendererData)RendererData;
        data.OldDateTimeValue = Value;
        data.CurrentDateTimeValue = Value;
    }

    [Delphi(PortStatus.Ported)]
    public bool IsChanged(TBoldDateTimeRendererData RendererData, TDateTime NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        FOnIsChanged is not null
            ? FOnIsChanged(RendererData, NewValue, Representation, Expression)
            : DefaultIsChanged(RendererData, NewValue, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public TBoldGetAsDateTimeEvent? OnGetAsDateTime
    {
        get => FOnGetAsDateTime;
        set => FOnGetAsDateTime = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSetAsDateTimeEvent? OnSetAsDateTime
    {
        get => FOnSetAsDateTime;
        set => FOnSetAsDateTime = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldDateTimeIsChangedEvent? OnIsChanged
    {
        get => FOnIsChanged;
        set => FOnIsChanged = value;
    }
}

/// <summary>The controller of a control that shows an element as a date and time value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDateTimeFollowerController : TBoldSingleFollowerController
{
    /// <summary>A controller for the component (the control) that owns it.</summary>
    public TBoldDateTimeFollowerController(object? aOwningComponent)
        : base(aOwningComponent)
    {
    }

    [Delphi(PortStatus.Ported)] private TBoldAsDateTimeRenderer? GetRenderer() => (TBoldAsDateTimeRenderer?)UntypedRenderer;

    /// <summary>Sets the renderer and says the controller changed.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetRenderer(TBoldAsDateTimeRenderer? Value)
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
    private TBoldAsDateTimeRenderer GetEffectiveAsDateTimeRenderer() => Renderer ?? TBoldAsDateTimeRenderer.DefaultRenderer();

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => EffectiveAsDateTimeRenderer;

    [Delphi(PortStatus.Ported)] protected TBoldAsDateTimeRenderer EffectiveAsDateTimeRenderer => GetEffectiveAsDateTimeRenderer();

    [Delphi(PortStatus.Ported)]
    public TDateTime GetCurrentAsDateTime(TBoldFollower Follower) => ((TBoldDateTimeRendererData)Follower.RendererData).CurrentDateTimeValue;

    /// <summary>Writes the control's date and time value back.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeClean(TBoldFollower Follower)
    {
        ReleaseChangedValue(Follower);
        SetAsDateTime(GetCurrentAsDateTime(Follower), Follower);
    }

    /// <summary>The control's date and time value is now the value: the follower becomes dirty when it differs from the element's.</summary>
    [Delphi(PortStatus.Ported)]
    public void MayHaveChanged(TDateTime NewValue, TBoldFollower Follower)
    {
        if (bfsDisplayable.Contains(Follower.State))
        {
            var data = (TBoldDateTimeRendererData)Follower.RendererData;
            data.CurrentDateTimeValue = NewValue;
            Follower.ControlledValueChanged(EffectiveAsDateTimeRenderer.IsChanged(data, NewValue, Representation, Expression, VariableList));
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SetAsDateTime(TDateTime Value, TBoldFollower Follower) =>
        EffectiveAsDateTimeRenderer.SetAsDateTime(Follower.Element, Value, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public TBoldAsDateTimeRenderer? Renderer
    {
        get => GetRenderer();
        set => SetRenderer(value);
    }
}
