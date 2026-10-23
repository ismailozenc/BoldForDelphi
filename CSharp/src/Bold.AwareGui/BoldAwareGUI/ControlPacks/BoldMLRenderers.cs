// Delphi: Source/BoldAwareGUI/ControlPacks/BoldMLRenderers.pas
// Port map: CSharp/port/units/BoldMLRenderers.md

using System.ComponentModel;
using static Bold.BoldGuiResourceStrings;
using static Bold.BoldSubscription;

namespace Bold;

/// <summary>Gives the string of an element in a language.</summary>
[Delphi(PortStatus.Ported)]
public delegate string TBoldLanguageGetAsString(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, string Language);

/// <summary>Sets an element from a string in a language.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldLanguageSetAsString(TBoldElement? Element, string Value, TBoldRepresentation Representation, TBoldExpression Expression, string Language);

/// <summary>Subscribes to what the string of an element in a language depends on.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldLanguageSubscribe(TBoldElement? Element, TBoldRepresentation Representation, TBoldExpression Expression, TBoldSubscriber? Subscriber, string Language);

/// <summary>Shows multi-language strings and value sets in the renderer's language.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAsMLStringRenderer : TBoldAsStringRenderer
{
    private string fLanguage = "";
    private TBoldLanguageGetAsString? fOnLanguageGetAsString;
    private TBoldLanguageSetAsString? fOnLanguageSetAsString;
    private TBoldLanguageSubscribe? fOnLanguageSubscribe;

    /// <summary>Adds the renderer to <paramref name="Owner"/> when given.</summary>
    public TBoldAsMLStringRenderer(IContainer? Owner = null)
        : base(Owner)
    {
    }

    /// <summary>Sets the language and tells the controllers.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetLanguage(string newValue)
    {
        if (newValue != fLanguage)
        {
            fLanguage = newValue;
            SendEvent(this, beValueChanged);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void SetAsString(TBoldElement? Element, string Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        if (OnLanguageSetAsString is not null)
        {
            OnLanguageSetAsString(Element, Value, Representation, Expression, Language);
        }
        else
        {
            DefaultSetAsString(Element, Value, Representation, Expression, VariableList);
        }
    }

    /// <summary>The string: OnLanguageSubscribe and OnLanguageGetAsString replace the defaults when set.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override string GetAsStringAndSubscribe(TBoldElement? Element, TBoldStringFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        if (OnLanguageSubscribe is not null && Subscriber is not null)
        {
            if (Element is not null)
            {
                OnLanguageSubscribe(Element, FollowerController.Representation, FollowerController.Expression, Subscriber, Language);
            }

            Subscriber = null;
        }

        if (OnLanguageGetAsString is null)
        {
            return DefaultGetAsStringAndSubscribe(Element, FollowerController, Subscriber);
        }

        string Result = OnLanguageGetAsString(Element, FollowerController.Representation, FollowerController.Expression, Language);
        if (OnLanguageSubscribe is null && Subscriber is not null)
        {
            DefaultGetAsStringAndSubscribe(Element, FollowerController, Subscriber);
        }

        return Result;
    }

    /// <summary>The string of the expression's value in the language (for multi-language values), subscribing to it.</summary>
    [Delphi(PortStatus.Ported, Note = "The nested language branches are the private helper StringInLanguage.")]
    public override string DefaultGetAsStringAndSubscribe(TBoldElement? Element, TBoldStringFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        if (Element is null)
        {
            return "";
        }

        using var E = new TBoldIndirectElement();
        Element.EvaluateAndSubscribeToExpression(FollowerController.Expression, Subscriber, E, false, false, FollowerController.GetVariableListAndSubscribe(Subscriber));
        return E.Value is null ? "" : StringInLanguage(E.Value, FollowerController, Subscriber);
    }

    private string StringInLanguage(TBoldElement Value, TBoldStringFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        string Result;
        if (Value is TBAMLString mlString)
        {
            Result = mlString.AsStringByLanguage(Language).StringRepresentation(FollowerController.Representation);
            if (Subscriber is not null)
            {
                mlString.SubscribeToLanguage(Language, Subscriber, breReEvaluate);
                Publisher.AddSubscription(Subscriber, beValueChanged, breReEvaluate);
            }
        }
        else if (Value is TBAMLValueSet mlValueSet)
        {
            Result = mlValueSet.StringRepresentationByLanguage(FollowerController.Representation, Language);
            if (Subscriber is not null)
            {
                mlValueSet.SubscribeToLanguage(FollowerController.Representation, Language, Subscriber, breReEvaluate);
                Publisher.AddSubscription(Subscriber, beValueChanged, breReEvaluate);
            }
        }
        else
        {
            Result = Value.StringRepresentation(FollowerController.Representation);
            if (Subscriber is not null)
            {
                Value.SubscribeToStringRepresentation(FollowerController.Representation, Subscriber, breReEvaluate);
            }
        }

        return Result;
    }

    /// <summary>Sets the expression's value from the string in the language; raises without a value.</summary>
    [Delphi(PortStatus.Ported)]
    public override void DefaultSetAsString(TBoldElement? Element, string Value, TBoldRepresentation Representation, TBoldExpression Expression, TBoldExternalVariableList? VariableList)
    {
        TBoldElement? ValueElement = GetExpressionAsDirectElement(Element, Expression, VariableList)
            ?? throw new EBold(SysUtils.Format(sCannotSetStringValue, GetType().Name));
        switch (ValueElement)
        {
            case TBAMLValueSet MLValueSet:
                MLValueSet.SetStringRepresentationByLanguage(Representation, Language, Value);
                break;
            case TBAMLString mlString:
                mlString.AsStringByLanguage(Language).SetStringRepresentation(Representation, Value);
                break;
            default:
                ValueElement.SetStringRepresentation(Representation, Value);
                break;
        }
    }

    /// <summary>The string in the language and whether the subscriber may change the value.</summary>
    [Delphi(PortStatus.Ported)]
    public override void DefaultMakeUptodateAndSetMayModifyAndSubscribe(TBoldElement? Element, TBoldRendererData RendererData, TBoldStringFollowerController FollowerController, TBoldSubscriber? Subscriber)
    {
        string S = "";
        if (Element is not null)
        {
            using var E = new TBoldIndirectElement();
            Element.EvaluateAndSubscribeToExpression(FollowerController.Expression, Subscriber, E, false);
            if (E.Value is not null)
            {
                S = StringInLanguage(E.Value, FollowerController, Subscriber);
                RendererData.MayModify = E.Value.ObserverMayModify(Subscriber);
            }
        }

        var data = (TBoldStringRendererData)RendererData;
        data.OldStringValue = S;
        data.CurrentStringValue = S;
    }

    [Delphi(PortStatus.Ported)]
    public string Language
    {
        get => fLanguage;
        set => SetLanguage(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldLanguageGetAsString? OnLanguageGetAsString
    {
        get => fOnLanguageGetAsString;
        set => fOnLanguageGetAsString = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldLanguageSetAsString? OnLanguageSetAsString
    {
        get => fOnLanguageSetAsString;
        set => fOnLanguageSetAsString = value;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldLanguageSubscribe? OnLanguageSubscribe
    {
        get => fOnLanguageSubscribe;
        set => fOnLanguageSubscribe = value;
    }
}
