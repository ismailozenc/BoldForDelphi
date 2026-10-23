// Delphi: Source/BoldAwareGUI/ControlPacks/BoldStringControlPack.pas
// Port map: CSharp/port/units/BoldStringControlPack.md

using System.ComponentModel;
using System.Drawing;
using static Bold.BoldControlPackDefs;
using static Bold.BoldGuiResourceStrings;
using static Bold.BoldSubscription;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldStringControlPack", Symbol = "initialization", Note = "The default renderer is created on first use.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldStringControlPack", Symbol = "finalization", Note = "Freed the default renderer; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Gives the string of an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate string TBoldGetAsString(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Sets an element from a string.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSetAsString(TBoldElement? Element, string NewValue, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Says whether a string (or a character) is valid for an element.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldValidateString(TBoldElement? Element, string Value, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Sets the font an element is shown with.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSetFont(TBoldElement? Element, TFont AFont, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Sets the color an element is shown with.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldSetColor(TBoldElement? Element, ref Color AColor, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>Says whether the string in the control differs from the element's.</summary>
[Delphi(PortStatus.Ported)]
public delegate bool TBoldStringIsChanged(TBoldStringRendererData RendererData, string NewValue, TBoldRepresentation Representation, TBoldExpression Expression);

/// <summary>The string a control shows: as the element gave it, and as the user has it now.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldStringRendererData : TBoldRendererData
{
    private string fOldStringValue = "";
    private string fCurrentStringValue = "";
    private int fMaxStringLength;

    [Delphi(PortStatus.Ported)]
    public TBoldStringRendererData(TBoldFollower OwningFollower)
        : base(OwningFollower)
    {
        fMaxStringLength = -1;
    }

    [Delphi(PortStatus.Ported)]
    public string OldStringValue
    {
        get => fOldStringValue;
        set => fOldStringValue = value;
    }

    [Delphi(PortStatus.Ported)]
    public string CurrentStringValue
    {
        get => fCurrentStringValue;
        set => fCurrentStringValue = value;
    }

    /// <summary>The longest string the value takes (an attribute's length), or -1.</summary>
    [Delphi(PortStatus.Ported)]
    public int MaxStringLength
    {
        get => fMaxStringLength;
        set => fMaxStringLength = value;
    }
}

/// <summary>
/// Shows elements as strings (the expression's value in the controller's representation) and sets them from strings,
/// with validation, fonts and colors; events can replace each step.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsStringRenderer : TBoldSingleRenderer
{
    private static TBoldAsStringRenderer? G_DefaultAsStringRenderer;

    private TBoldGetAsString? FOnGetAsString;
    private TBoldSetAsString? FOnSetAsString;
    private TBoldValidateString? FOnValidateCharacter;
    private TBoldValidateString? FOnValidateString;
    private TBoldSetFont? fOnSetFont;
    private TBoldSetColor? fOnSetColor;
    private TBoldStringIsChanged? fOnIsChanged;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldAsStringRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    /// <summary>"(name)" (or "(class)"), shown by a renderer in a designer.</summary>
    [Delphi(PortStatus.Ported)]
    private string DefaultDisplayString() => Name != "" ? "(" + Name + ")" : "(" + GetType().Name + ")";

    [Delphi(PortStatus.Ported)]
    protected override bool GetSupportsMulti() => false;

    [Delphi(PortStatus.Ported)]
    protected override Type GetRendererDataClass() => typeof(TBoldStringRendererData);

    /// <summary>The string of the element: OnSubscribe places the subscriptions when set, and OnGetAsString gives the string when set.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal virtual string GetAsStringAndSubscribe(TBoldElement? Element, TBoldStringFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        if (OnSubscribe is not null && Subscriber is not null)
        {
            if (Element is not null)
            {
                OnSubscribe(Element, FollowerController.Representation, FollowerController.Expression, Subscriber);
            }

            Subscriber = null;
        }

        return OnGetAsString is not null
            ? OnGetAsString(Element, FollowerController.Representation, FollowerController.Expression)
            : DefaultGetAsStringAndSubscribe(Element, FollowerController, Subscriber);
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void SetAsString(TBoldElement? Element, string Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        if (FOnSetAsString is not null)
        {
            OnSetAsString!(Element, Value, Representation, Expression);
        }
        else
        {
            DefaultSetAsString(Element, Value, Representation, Expression, VariableList);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void DrawOnCanvas(TBoldFollower Follower, TCanvas Canvas, Rectangle Rect, TAlignment Alignment, Point Margins) =>
        DrawStringOnCanvas(Canvas, Rect, Alignment, Margins, ((TBoldStringRendererData)Follower.RendererData).CurrentStringValue);

    /// <summary>The renderer of string controllers that have none of their own.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldAsStringRenderer DefaultRenderer() => G_DefaultAsStringRenderer ??= new TBoldAsStringRenderer();

    /// <summary>Draws the string in the rectangle, aligned, inside the margins.</summary>
    [Delphi(PortStatus.Ported)]
    public static void DrawStringOnCanvas(TCanvas Canvas, Rectangle Rect, TAlignment Alignment, Point Margins, string S)
    {
        int Left = Alignment switch
        {
            TAlignment.taLeftJustify => Margins.X + Rect.Left,
            TAlignment.taRightJustify => Rect.Right - Rect.Left - Canvas.TextWidth(S) + Rect.Left - 1 - Margins.X,
            _ => Rect.Left + (Rect.Right - Rect.Left - Canvas.TextWidth(S)) / 2,
        };
        Canvas.TextRect(Rect, Left, Rect.Top + Margins.Y, S);
    }

    /// <summary>
    /// The string of the expression's value in the representation (the nil representation for no value or an empty
    /// link), subscribing to it; in a designer, a renderer other than the default shows its name.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public virtual string DefaultGetAsStringAndSubscribe(TBoldElement? Element, TBoldStringFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        if (IsDesigning && this != DefaultRenderer())
        {
            return DefaultDisplayString();
        }

        if (Element is null)
        {
            return FollowerController.NilStringRepresentation;
        }

        using var E = new TBoldIndirectElement();
        Element.EvaluateAndSubscribeToExpression(FollowerController.Expression, Subscriber, E, false, false, FollowerController.GetVariableListAndSubscribe(Subscriber));
        string Result;
        if (E.Value is TBoldObjectReference { BoldObject: null })
        {
            Result = FollowerController.NilStringRepresentation;
            if (Subscriber is not null)
            {
                E.Value.SubscribeToStringRepresentation(FollowerController.Representation, Subscriber, breReEvaluate);
            }
        }
        else if (E.Value is not null)
        {
            Result = E.Value.StringRepresentation(FollowerController.Representation);
            if (Subscriber is not null)
            {
                E.Value.SubscribeToStringRepresentation(FollowerController.Representation, Subscriber, breReEvaluate);
            }
        }
        else
        {
            Result = FollowerController.NilStringRepresentation;
        }

        return Result;
    }

    /// <summary>Sets the string representation of the expression's value; raises without a value.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void DefaultSetAsString(TBoldElement? Element, string Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList);
        if (ValueElement is null)
        {
            throw new EBold(SysUtils.Format(sCannotSetStringValue, GetType().Name));
        }

        ValueElement.SetStringRepresentation(Representation, Value);
    }

    [Delphi(PortStatus.Ported, Note = "Takes a char where Delphi takes an AnsiChar.")]
    public virtual bool DefaultValidateCharacter(TBoldElement? Element, char C, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        GetExpressionAsDirectElement(Element, Expression, VariableList)?.ValidateCharacter(C, Representation) ?? false;

    [Delphi(PortStatus.Ported)]
    public virtual bool DefaultValidateString(TBoldElement? Element, string Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        GetExpressionAsDirectElement(Element, Expression, VariableList)?.ValidateString(Value, Representation) ?? false;

    /// <summary>True when the string differs from the one the element gave.</summary>
    [Delphi(PortStatus.Ported)]
    public bool DefaultIsChanged(TBoldStringRendererData RendererData, string NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        NewValue != RendererData.OldStringValue;

    [Delphi(PortStatus.Ported, Note = "Takes a char where Delphi takes an AnsiChar.")]
    public virtual bool ValidateCharacter(TBoldElement? Element, char C, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        FOnValidateCharacter is not null
            ? OnValidateCharacter!(Element, C.ToString(), Representation, Expression)
            : DefaultValidateCharacter(Element, C, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public virtual bool ValidateString(TBoldElement? Element, string Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        FOnValidateString is not null
            ? OnValidateString!(Element, Value, Representation, Expression)
            : DefaultValidateString(Element, Value, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public bool IsChanged(TBoldStringRendererData RendererData, string NewValue, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList) =>
        fOnIsChanged is not null
            ? fOnIsChanged(RendererData, NewValue, Representation, Expression)
            : DefaultIsChanged(RendererData, NewValue, Representation, Expression, VariableList);

    /// <summary>The control's font, then OnSetFont's changes for the element.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetFont(TBoldElement? Element, TFont EffectiveFont, TFont Font, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        EffectiveFont.Assign(Font);
        fOnSetFont?.Invoke(Element, EffectiveFont, Representation, Expression);
    }

    /// <summary>The control's color, then OnSetColor's change for the element.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetColor(TBoldElement? Element, ref Color EffectiveColor, Color Color, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        EffectiveColor = Color;
        fOnSetColor?.Invoke(Element, ref EffectiveColor, Representation, Expression);
    }

    /// <summary>Puts the element's string in the renderer data, as both the old and the current string.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeUptodateAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        string S = GetAsStringAndSubscribe(Element, (TBoldStringFollowerController)FollowerController, Subscriber);
        var data = (TBoldStringRendererData)RendererData;
        data.OldStringValue = S;
        data.CurrentStringValue = S;
    }

    /// <summary>Brings several followers up to date at once; only the COM client did this, so it does nothing.</summary>
    [Delphi(PortStatus.Ported)]
    public void MultiMakeUpToDateAndSubscribe(TBoldObjectArray Elements, TBoldObjectArray Subscribers, TBoldObjectArray RendererData, TBoldFollowerController FollowerController)
    {
    }

    /// <summary>
    /// The default way to bring a follower up to date: the string of the expression's value, whether the user may
    /// change it, and its longest length, subscribing to it.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public virtual void DefaultMakeUptodateAndSetMayModifyAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldStringFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        var data = (TBoldStringRendererData)RendererData;
        string S;
        if (IsDesigning && this != DefaultRenderer())
        {
            S = DefaultDisplayString();
            RendererData.MayModify = false;
        }
        else if (Element is not null)
        {
            using var E = new TBoldIndirectElement();
            Element.EvaluateAndSubscribeToExpression(FollowerController.Expression, Subscriber, E, false, false, FollowerController.GetVariableListAndSubscribe(Subscriber));
            if (E.Value is TBoldObjectReference { BoldObject: null })
            {
                S = FollowerController.NilStringRepresentation;
                if (Subscriber is not null)
                {
                    E.Value.SubscribeToStringRepresentation(FollowerController.Representation, Subscriber, breReEvaluate);
                }

                RendererData.MayModify = E.Value.ObserverMayModifyAsString(FollowerController.Representation, Subscriber);
                data.MaxStringLength = -1;
            }
            else if (E.Value is not null)
            {
                S = E.Value.StringRepresentation(FollowerController.Representation);
                if (Subscriber is not null)
                {
                    E.Value.SubscribeToStringRepresentation(FollowerController.Representation, Subscriber, breReEvaluate);
                }

                RendererData.MayModify = E.Value.ObserverMayModifyAsString(FollowerController.Representation, Subscriber);
                data.MaxStringLength = E.Value is TBoldAttribute { BoldAttributeRTInfo: { } rtInfo } ? rtInfo.Length : -1;
            }
            else
            {
                S = FollowerController.NilStringRepresentation;
            }
        }
        else
        {
            S = FollowerController.NilStringRepresentation;
        }

        data.OldStringValue = S;
        data.CurrentStringValue = S;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldGetAsString? OnGetAsString
    {
        get => FOnGetAsString;
        set => FOnGetAsString = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSetAsString? OnSetAsString
    {
        get => FOnSetAsString;
        set => FOnSetAsString = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldValidateString? OnValidateCharacter
    {
        get => FOnValidateCharacter;
        set => FOnValidateCharacter = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldValidateString? OnValidateString
    {
        get => FOnValidateString;
        set => FOnValidateString = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSetFont? OnSetFont
    {
        get => fOnSetFont;
        set => fOnSetFont = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSetColor? OnSetColor
    {
        get => fOnSetColor;
        set => fOnSetColor = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldStringIsChanged? OnIsChanged
    {
        get => fOnIsChanged;
        set => fOnIsChanged = value;
    }
}

/// <summary>The controller of a control that shows an element as a string (an edit box, a label, a grid cell).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldStringFollowerController : TBoldSingleFollowerController
{
    private string FNilStringRepresentation = "";

    /// <summary>A controller for the component (the control) that owns it.</summary>
    public TBoldStringFollowerController(object? aOwningComponent)
        : base(aOwningComponent)
    {
    }

    [Delphi(PortStatus.Ported)] private TBoldAsStringRenderer? GetRenderer() => (TBoldAsStringRenderer?)UntypedRenderer;
    [Delphi(PortStatus.Ported)] private void SetRenderer(TBoldAsStringRenderer? Value) => UntypedRenderer = Value;

    [Delphi(PortStatus.Ported)]
    private TBoldAsStringRenderer GetEffectiveAsStringRenderer() => Renderer ?? TBoldAsStringRenderer.DefaultRenderer();

    [Delphi(PortStatus.Ported)]
    private void SetNilStringRepresentation(string Value)
    {
        if (FNilStringRepresentation != Value)
        {
            FNilStringRepresentation = Value;
            Changed();
        }
    }

    /// <summary>The default renderer can fetch several values at once, unless the expression concatenates.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool GetSupportsMultiEnsure() =>
        EffectiveRenderer == TBoldAsStringRenderer.DefaultRenderer() && SysUtils.Pos("+", Expression) == 0;

    [Delphi(PortStatus.Ported)]
    protected override TBoldRenderer GetEffectiveRenderer() => EffectiveAsStringRenderer;

    [Delphi(PortStatus.Ported)] protected TBoldAsStringRenderer EffectiveAsStringRenderer => GetEffectiveAsStringRenderer();

    /// <summary>Hands the elements, subscribers and renderer data of the followers to the renderer at once.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void DoMultiMakeUptodateAndSubscribe(TBoldObjectArray Followers)
    {
        System.Diagnostics.Debug.Assert(SupportsMulti);
        var Renderer = (TBoldAsStringRenderer)EffectiveRenderer;
        var Elements = new TBoldObjectArray(Followers.Count, TBoldContainerOptions.Of());
        var Subscribers = new TBoldObjectArray(Followers.Count, TBoldContainerOptions.Of());
        var RendererData = new TBoldObjectArray(Followers.Count, TBoldContainerOptions.Of());
        for (int I = 0; I < Followers.Count; I++)
        {
            var TempFollower = (TBoldFollower)Followers[I]!;
            Elements.Add(TempFollower.Element);
            Subscribers.Add(bfdNeedResubscribe.Contains(TempFollower.State) ? TempFollower.Subscriber : null);
            RendererData.Add(TempFollower.RendererData);
        }

        Renderer.MultiMakeUpToDateAndSubscribe(Elements, Subscribers, RendererData, this);
    }

    /// <summary>Writes the control's string back when it is valid; otherwise raises the last failure reason.</summary>
    [Delphi(PortStatus.Ported)]
    public override void MakeClean(TBoldFollower Follower)
    {
        if (ValidateString(GetCurrentAsString(Follower), Follower))
        {
            ReleaseChangedValue(Follower); // note, must do first, since set can change element
            SetAsString(GetCurrentAsString(Follower), Follower);
        }
        else
        {
            var el = Follower.Element as TBoldDomainElement;
            TBoldFailureReason? FailureReason = BoldSystem.GetBoldLastFailureReason();
            if (FailureReason is not null)
            {
                FailureReason.MessageFormatStr = sStringValidationFailedExtended;
            }

            BoldSystem.BoldRaiseLastFailure(el, "", sUnknownReason);
        }
    }

    [Delphi(PortStatus.Ported)]
    public string GetCurrentAsString(TBoldFollower Follower) => ((TBoldStringRendererData)Follower.RendererData).CurrentStringValue;

    [Delphi(PortStatus.Ported)]
    public void SetAsString(string Value, TBoldFollower Follower) =>
        EffectiveAsStringRenderer.SetAsString(Follower.Element, Value, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported, Note = "Takes a char where Delphi takes an AnsiChar.")]
    public bool ValidateCharacter(char C, TBoldFollower Follower) =>
        EffectiveAsStringRenderer.ValidateCharacter(Follower.Element, C, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public bool ValidateString(string Value, TBoldFollower Follower) =>
        EffectiveAsStringRenderer.ValidateString(Follower.Element, Value, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public void SetFont(TFont EffectiveFont, TFont Font, TBoldFollower Follower) =>
        EffectiveAsStringRenderer.SetFont(Follower.Element, EffectiveFont, Font, Representation, Expression, VariableList);

    [Delphi(PortStatus.Ported)]
    public void SetColor(ref Color EffectiveColor, Color COLOR, TBoldFollower Follower) =>
        EffectiveAsStringRenderer.SetColor(Follower.Element, ref EffectiveColor, COLOR, Representation, Expression, VariableList);

    /// <summary>The control's string is now the value: the follower becomes dirty when it differs from the element's.</summary>
    [Delphi(PortStatus.Ported)]
    public void MayHaveChanged(string NewValue, TBoldFollower Follower)
    {
        if (bfsDisplayable.Contains(Follower.State))
        {
            var data = (TBoldStringRendererData)Follower.RendererData;
            data.CurrentStringValue = NewValue;
            Follower.ControlledValueChanged(EffectiveAsStringRenderer.IsChanged(data, NewValue, Representation, Expression, VariableList));
        }
    }

    /// <summary>Brings the follower up to date through the renderer's events when it has any, else the default way.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void DoMakeUptodateAndSubscribe(TBoldFollower Follower, bool Subscribe)
    {
        TBoldSubscriber? Subscriber = Subscribe ? Follower.Subscriber : null;
        var renderer = (TBoldAsStringRenderer)EffectiveRenderer;
        if (renderer.OnGetAsString is not null || renderer.OnSubscribe is not null || renderer.OnMayModify is not null)
        {
            renderer.MakeUptodateAndSubscribe(Follower.Element, Follower.RendererData, this, Subscriber);
            Follower.RendererData.MayModify = renderer.MayModify(Follower.Element, Representation, Expression, GetVariableListAndSubscribe(Follower.Subscriber), Follower.Subscriber);
        }
        else
        {
            renderer.DefaultMakeUptodateAndSetMayModifyAndSubscribe(Follower.Element, Follower.RendererData, this, Subscriber);
        }
    }

    /// <summary>The string of the element, without subscribing.</summary>
    [Delphi(PortStatus.Ported)]
    public string GetAsString(TBoldElement? Element) => EffectiveAsStringRenderer.GetAsStringAndSubscribe(Element, this, null);

    [Delphi(PortStatus.Ported)]
    public TBoldAsStringRenderer? Renderer
    {
        get => GetRenderer();
        set => SetRenderer(value);
    }

    [Delphi(PortStatus.Ported)]
    public string NilStringRepresentation
    {
        get => FNilStringRepresentation;
        set => SetNilStringRepresentation(value);
    }
}
