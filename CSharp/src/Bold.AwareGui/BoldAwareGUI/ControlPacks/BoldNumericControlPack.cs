// Delphi: Source/BoldAwareGUI/ControlPacks/BoldNumericControlPack.pas
// Port map: CSharp/port/units/BoldNumericControlPack.md

using System.ComponentModel;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldNumericControlPack", Symbol = "initialization", Note = "The default renderer is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldNumericControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Gives the integer value of an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate int TBoldGetAsIntegerEvent(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Sets an element from a integer value.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSetAsIntegerEvent(TBoldElement? Element, int Value, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Says whether the integer value in the control differs from the element's.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldIntegerIsChangedEvent(TBoldIntegerRendererData RenderData, int NewValue, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>The integer value a control shows: as the element gave it, and as the user has it now.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIntegerRendererData : TBoldRendererData
{
    private int FOldIntegerValue;
    private int FCurrentIntegerValue;

    /// <summary>Renderer data for the follower.</summary>
    public TBoldIntegerRendererData(TBoldFollower OwningFollower)
        : base(OwningFollower)
    {
    }

    [Delphi(PortStatus.Ported)]
    public int CurrentIntegerValue
    {
        get => FCurrentIntegerValue;
        set => FCurrentIntegerValue = value;
    }

    [Delphi(PortStatus.Ported)]
    public int OldIntegerValue
    {
        get => FOldIntegerValue;
        set => FOldIntegerValue = value;
    }
}

/// <summary>Shows elements as integer values (for track bars, spin edits, date pickers) and sets them from integer values.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsIntegerRenderer : TBoldSingleRenderer
{
    private static TBoldAsIntegerRenderer? DefaultAsIntegerRenderer;

    private TBoldGetAsIntegerEvent? FOnGetAsInteger;
    private TBoldSetAsIntegerEvent? FOnSetAsInteger;
    private TBoldIntegerIsChangedEvent? FOnIsChanged;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldAsIntegerRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type GetRendererDataClass() => typeof(TBoldIntegerRendererData);

    /// <summary>The integer value of the expression's value, subscribing to it; 0 without an element.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual int DefaultGetAsIntegerAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        int Result = 0;
        if (Element is not null)
        {
            using var IndirectElement = new TBoldIndirectElement();
            Element.EvaluateAndSubscribeToExpression(Expression, Subscriber, IndirectElement, false, false, VariableList);
            if (IndirectElement.Value is not null)
            {
                Result = IndirectElement.Value switch
                {
                    TBAInteger integer => integer.AsInteger,
                    TBANumeric numeric => (int)Math.Round(numeric.AsFloat),
                    _ => throw new EBold(SysUtils.Format(SCantGetIntegerValue, IndirectElement.Value.GetType().Name)),
                };
            }
        }

        return Result;
    }

    /// <summary>Sets the expression's value from the integer value; raises when it is not a integer attribute.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void DefaultSetAsInteger(TBoldElement? Element, int Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        if (ValueElement is not TBANumeric numeric)
        {
            throw new EBold(SysUtils.Format(SCantSetIntegerValue, ValueElement?.GetType().Name ?? "nil"));
        }

        numeric.AsInteger = Value;
    }

    /// <summary>The integer value: OnSubscribe places the subscriptions when set, and OnGetAsInteger gives the value when set.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual int GetAsIntegerAndSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        if (OnSubscribe is not null && Subscriber is not null)
        {
            if (Element is not null)
            {
                OnSubscribe(Element, Representation, Expression, Subscriber);
            }

            Subscriber = null;
        }

        return OnGetAsInteger is not null
            ? OnGetAsInteger(Element, Representation, Expression)
            : DefaultGetAsIntegerAndSubscribe(Element, Representation, Expression, VariableList, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetAsInteger(TBoldElement? Element, int Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        if (FOnSetAsInteger is not null)
        {
            OnSetAsInteger!(Element, Value, Representation, Expression);
        }
        else
        {
            DefaultSetAsInteger(Element, Value, Representation, Expression, VariableList);
        }
    }

    /// <summary>The renderer of integer controllers that have none of their own.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldAsIntegerRenderer DefaultRenderer() => DefaultAsIntegerRenderer ??= new TBoldAsIntegerRenderer();

    [Delphi(PortStatus.Ported)]
    public bool DefaultIsChanged(TBoldIntegerRendererData RendererData, int NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        NewValue != RendererData.OldIntegerValue;

    /// <summary>True when the expression's value is a integer attribute the subscriber may change (evaluating the expression once).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool DefaultMayModify(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList, TBoldSubscriber? Subscriber)
    {
        // Note! We don't call inherited DefaultMayModify to prevent evaluation of expression two times!
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        return ValueElement is TBANumeric && ValueElement.ObserverMayModify(Subscriber);
    }

    /// <summary>Puts the element's integer value in the renderer data, as both the old and the current value.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeUptodateAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        var Controller = (TBoldIntegerFollowerController)FollowerController;
        int Value = GetAsIntegerAndSubscribe(Element, Controller.Representation, Controller.Expression, Controller.GetVariableListAndSubscribe(Subscriber), Subscriber);
        var data = (TBoldIntegerRendererData)RendererData;
        data.OldIntegerValue = Value;
        data.CurrentIntegerValue = Value;
    }

    [Delphi(PortStatus.Ported)]
    public bool IsChanged(TBoldIntegerRendererData RendererData, int NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        FOnIsChanged is not null
            ? FOnIsChanged(RendererData, NewValue, Representation, Expression)
            : DefaultIsChanged(RendererData, NewValue, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public TBoldGetAsIntegerEvent? OnGetAsInteger
    {
        get => FOnGetAsInteger;
        set => FOnGetAsInteger = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSetAsIntegerEvent? OnSetAsInteger
    {
        get => FOnSetAsInteger;
        set => FOnSetAsInteger = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldIntegerIsChangedEvent? OnIsChanged
    {
        get => FOnIsChanged;
        set => FOnIsChanged = value;
    }
}

/// <summary>The controller of a control that shows an element as a integer value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldIntegerFollowerController : TBoldSingleFollowerController
{
    /// <summary>A controller for the component (the control) that owns it.</summary>
    public TBoldIntegerFollowerController(object? aOwningComponent)
        : base(aOwningComponent)
    {
    }

    [Delphi(PortStatus.Ported)] private TBoldAsIntegerRenderer? GetRenderer() => (TBoldAsIntegerRenderer?)UntypedRenderer;

    [Delphi(PortStatus.Ported)] private void SetRenderer(TBoldAsIntegerRenderer? Value) => UntypedRenderer = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldAsIntegerRenderer GetEffectiveAsIntegerRenderer() => Renderer ?? TBoldAsIntegerRenderer.DefaultRenderer();

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => EffectiveAsIntegerRenderer;

    [Delphi(PortStatus.Ported)] protected TBoldAsIntegerRenderer EffectiveAsIntegerRenderer => GetEffectiveAsIntegerRenderer();

    [Delphi(PortStatus.Ported)]
    public int GetCurrentAsInteger(TBoldFollower Follower) => ((TBoldIntegerRendererData)Follower.RendererData).CurrentIntegerValue;

    /// <summary>Writes the control's integer value back.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeClean(TBoldFollower Follower)
    {
        ReleaseChangedValue(Follower);
        SetAsInteger(GetCurrentAsInteger(Follower), Follower);
    }

    /// <summary>The control's integer value is now the value: the follower becomes dirty when it differs from the element's.</summary>
    [Delphi(PortStatus.Ported)]
    public void MayHaveChanged(int NewValue, TBoldFollower Follower)
    {
        if (bfsDisplayable.Contains(Follower.State))
        {
            var data = (TBoldIntegerRendererData)Follower.RendererData;
            data.CurrentIntegerValue = NewValue;
            Follower.ControlledValueChanged(EffectiveAsIntegerRenderer.IsChanged(data, NewValue, Representation, Expression, GetVariableListAndSubscribe(Follower.Subscriber)));
        }
    }

    [Delphi(PortStatus.Ported)]
    public void SetAsInteger(int Value, TBoldFollower Follower) =>
        EffectiveAsIntegerRenderer.SetAsInteger(Follower.Element, Value, Representation, Expression, GetVariableListAndSubscribe(Follower.Subscriber));

    [Delphi(PortStatus.Ported)]
    public TBoldAsIntegerRenderer? Renderer
    {
        get => GetRenderer();
        set => SetRenderer(value);
    }
}
