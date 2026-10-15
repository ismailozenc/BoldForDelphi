// Delphi: Source/ObjectSpace/BORepresentation/BoldAttributes.pas
// Port map: CSharp/port/units/BoldAttributes.md
// Value sets: the values and their list, the value set attribute, booleans and constraints, and their proxies.

using static Bold.BoldAttributes;
using static Bold.BoldCoreConsts;
using static Bold.BoldDefs;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>One value of a value set: an integer and its string representations (1-based by representation).</summary>
[Delphi(PortStatus.Ported)]
public class TBAValueSetValue : TBoldElement
{
    private int FAsInteger;
    private readonly TStringList FStringRepresentations = new();

    /// <summary>A value with no representations, for subclasses that keep their own (Delphi's inherited TBoldElement.Create).</summary>
    protected TBAValueSetValue()
    {
    }

    /// <summary>Creates the value with StringValues + 1 empty representations and adds it to the list.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBAValueSetValue.InternalCreate")]
    public TBAValueSetValue(int StringValues, TBAValueSetValueList List)
    {
        for (int I = 0; I <= StringValues; I++)
        {
            FStringRepresentations.Add("");
        }

        List.AddValue(this);
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBAValueSetValue.Destroy", Note = "Freed the string list; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => FStringRepresentations[Representation];

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        EnsureValidString(Value, Representation);
        FStringRepresentations[Representation] = Value;
    }

    [Delphi(PortStatus.Ported)] protected int GetAsInteger() => FAsInteger;
    [Delphi(PortStatus.Ported)] protected void SetAsInteger(int Value) => FAsInteger = Value;
    [Delphi(PortStatus.Ported)] protected void AddString(string Value) => FStringRepresentations.Add(Value);
    [Delphi(PortStatus.Ported)] protected virtual int GetStringRepresentationCount() => FStringRepresentations.Count;
    [Delphi(PortStatus.Ported)] protected override TBoldElementTypeInfo? GetBoldType() => null;

    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        if (Mutable)
        {
            AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(beItemAdded, beItemDeleted, beItemReplaced, beOrderChanged, beValueInvalid), RequestedEvent);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void GetAsList(TBoldIndirectElement ResultList) => ResultList.SetReferenceValue(this);

    [Delphi(PortStatus.Ported)]
    public int AsInteger
    {
        get => GetAsInteger();
        set => SetAsInteger(value);
    }

    [Delphi(PortStatus.Ported)] public int StringRepresentationCount => GetStringRepresentationCount();
}

/// <summary>The values of a value set type, in order; it owns them.</summary>
[Delphi(PortStatus.Ported)]
public class TBAValueSetValueList : TBoldMemoryManagedObject
{
    internal readonly List<TBAValueSetValue> FValueList = [];

    [Delphi(PortStatus.Ported)]
    public TBAValueSetValueList()
    {
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBAValueSetValueList.Destroy", Note = "Freed the values; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    private int GetCount() => FValueList.Count;

    [Delphi(PortStatus.Ported)]
    private TBAValueSetValue GetValueSetValue(int index)
    {
        System.Diagnostics.Debug.Assert(index >= 0 && index < Count, "TBAValueSetValueList.GetValueSetValue: IndexOutOfRange");
        return FValueList[index];
    }

    [Delphi(PortStatus.Ported)]
    public TBAValueSetValue? GetFirstValue() => FValueList.Count == 0 ? null : FValueList[0];

    [Delphi(PortStatus.Ported)]
    public void AddValue(TBAValueSetValue Value) => FValueList.Add(Value);

    /// <summary>The last value with the integer, or null.</summary>
    [Delphi(PortStatus.Ported)]
    public TBAValueSetValue? FindByInteger(int Value)
    {
        TBAValueSetValue? result = null;
        foreach (TBAValueSetValue v in FValueList)
        {
            if (v.AsInteger == Value)
            {
                result = v;
            }
        }

        return result;
    }

    /// <summary>The first value whose representation equals the string, ignoring ASCII case.</summary>
    [Delphi(PortStatus.Ported)]
    public TBAValueSetValue? FindByString(TBoldRepresentation Representation, string Value) =>
        FValueList.FirstOrDefault(v => SysUtils.CompareText(v.GetStringRepresentation(Representation), Value) == 0);

    [Delphi(PortStatus.Ported)]
    public TBAValueSetValue? FindByText(TBoldRepresentation Representation, string Value) =>
        FValueList.FirstOrDefault(v => SysUtils.AnsiCompareText(v.GetStringRepresentation(Representation), Value) == 0);

    [Delphi(PortStatus.Ported)]
    public void ToStrings(TBoldRepresentation Representation, TStrings theStrings)
    {
        foreach (TBAValueSetValue v in FValueList)
        {
            theStrings.AddObject(v.GetStringRepresentation(Representation), v);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ToStringsWithNil(TBoldRepresentation Representation, TStrings theStrings, string nilstring)
    {
        theStrings.AddObject(nilstring, null);
        ToStrings(Representation, theStrings);
    }

    /// <summary>Adds a value with the integer and the strings as representations 1, 2, ...</summary>
    [Delphi(PortStatus.Ported)]
    public void Add(int intvalue, params string[] arrString)
    {
        int ArrSize = arrString.Length;
        var value = new TBAValueSetValue(ArrSize, this) { AsInteger = intvalue };
        for (int RepresentationIdx = 0; RepresentationIdx < ArrSize; RepresentationIdx++)
        {
            value.SetStringRepresentation(RepresentationIdx + 1, arrString[RepresentationIdx]);
        }
    }

    [Delphi(PortStatus.Ported)] public int Count => GetCount();
    [Delphi(PortStatus.Ported, Symbol = "TBAValueSetValueList.ValueSetValues")] public TBAValueSetValue this[int index] => GetValueSetValue(index);
}

/// <summary>An attribute whose value is one of a fixed list of values (an enumeration); subclasses give the values.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBAValueSet : TBoldAttribute
{
    internal TBAValueSetValue? FValue;

    protected TBAValueSet()
    {
    }

    /// <summary>Raises when non-null without a value (bad data or a failed read).</summary>
    [Delphi(PortStatus.Ported)]
    private void CheckIllegalValue()
    {
        if (!IsNull && FValue is null)
        {
            throw new EBold(BoldPersistenceState == TBoldValuePersistenceState.bvpsCurrent ? sBadDataInDB : sCannotRead);
        }
    }

    [Delphi(PortStatus.Ported)]
    private int GetAsInteger()
    {
        BoldClearLastFailure();
        CheckIllegalValue();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsInteger", "");
        }

        EnsureNotNull();
        return FValue!.AsInteger;
    }

    [Delphi(PortStatus.Ported)]
    private void SetAsInteger(int Value) => SetDataValue(Values.FindByInteger(Value));

    [Delphi(PortStatus.Ported)]
    internal void SetContent(TBAValueSetValue? NewValue)
    {
        if (ContentIsNull || FValue != NewValue)
        {
            PreChange();
            FValue = NewValue;
            SetToNonNull();
            Changed(beValueChanged, [NewValue]);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetContentAsInteger(int NewValue) => SetContent(Values.FindByInteger(NewValue));

    /// <summary>Sets the value; null (no such value) raises EBold.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetDataValue(TBAValueSetValue? NewValue)
    {
        BoldClearLastFailure();
        if (!CanSetValue(NewValue, null))
        {
            BoldRaiseLastFailure(this, "SetDataValue", "");
        }

        if (NewValue is null)
        {
            throw new EBold(sInvalidValue);
        }

        if (IsNull || FValue != NewValue)
        {
            if (!StartModify())
            {
                BoldRaiseLastFailure(this, "SetDataValue", "");
            }

            try
            {
                SetContent(NewValue);
                EndModify();
            }
            catch
            {
                FailModify();
                throw;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual int GetContentAsInteger() => FValue!.AsInteger;

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is null && CanSetToNull(null))
        {
            SetContentToNull();
        }
        else if (Source is null)
        {
            SetContent(Values.GetFirstValue());
        }
        else if (Source is IBoldIntegerContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(Values.FindByInteger(s.asInteger));
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignContentValue));
        }
    }

    /// <summary>Starts at the first value, or null when there are no values.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        if (Values is null)
        {
            throw new EBold(SysUtils.Format(sValuesNotInitialized, GetType().Name));
        }

        if (Values.FValueList.Count != 0)
        {
            FValue = Values.GetFirstValue();
        }
        else
        {
            SetToNull();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected abstract TBAValueSetValueList GetValues();

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        CheckIllegalValue();
        return IsNull ? "" : FValue!.GetStringRepresentation(Representation);
    }

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, Meth_SetStringRepresentation, sStringValidationFailed);
        }

        TBAValueSetValue? ValueSetValue = Values.FindByString(Representation, Value);
        System.Diagnostics.Debug.Assert(ValueSetValue is not null, sCannotFindValueSetValue);
        SetDataValue(ValueSetValue);
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool MaySetValue(TBAValueSetValue? NewValue, TBoldSubscriber? Subscriber) => true;

    [Delphi(PortStatus.Ported)]
    protected int ContentAsInteger
    {
        get => GetContentAsInteger();
        set => SetContentAsInteger(value);
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBAValueSet_Proxy);

    [Delphi(PortStatus.Ported)]
    public override void SetAsVariant(object? Value) => AsInteger = VariantToInteger(Value);

    [Delphi(PortStatus.Ported)]
    public override object? GetAsVariant() => AsInteger;

    /// <summary>True when a value has the string; otherwise the failure reason lists the allowed values.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation)
    {
        bool result = Values.FindByString(Representation, Value) is not null;
        if (!result)
        {
            var strList = new TStringList();
            Values.ToStrings(Representation, strList);
            string Str = BoldUtils.BoldSeparateStringList(strList, ", ", "(", ")");
            SetBoldLastFailureReason(new TBoldFailureReason(sUnknownStringValue, [Value, Str], this));
        }

        return result;
    }

    /// <summary>True when the character occurs in any value's representation.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ValidateCharacter(char C, TBoldRepresentation Representation)
    {
        string Str = "";
        for (int i = 0; i < Values.Count; i++)
        {
            Str += Values[i].GetStringRepresentation(Representation);
        }

        return Str.Contains(C);
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBAValueSet valueSet)
        {
            if (valueSet.IsNull)
            {
                SetToNull();
            }
            else
            {
                AsInteger = valueSet.AsInteger;
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    /// <summary>True when the string equals the value's name expanded as an OCL enum literal.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool CompareToEnumLiteral(string str) =>
        SysUtils.SameText(str, BoldNameExpander.BoldExpandName(AsString, "", TExpansionType.xtExpression, -1, TBoldNationalCharConversion.nccTrue));

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldIntegerContent s)
        {
            SetDataValue(Values.FindByInteger(s.asInteger));
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBAValueSet other)
        {
            if (EitherIsNull(this, other))
            {
                return NullSmallest(BoldElement);
            }

            return CompType == TBoldCompareType.ctDefault ? AsInteger.CompareTo(other.AsInteger) : base.CompareToAs(CompType, BoldElement);
        }

        return base.CompareToAs(CompType, BoldElement);
    }

    [Delphi(PortStatus.Ported)] public TBAValueSetValueList Values => GetValues();

    [Delphi(PortStatus.Ported)]
    public int AsInteger
    {
        get => GetAsInteger();
        set => SetAsInteger(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSetValue(TBAValueSetValue? NewValue, TBoldSubscriber? Subscriber) =>
        MaySetValue(NewValue, Subscriber) && SendQuery(bqMaySetValue, [NewValue], Subscriber);

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldIntegerContent) || IId == typeof(IBoldStringContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldString/IntegerContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    /// <summary>The first value, else null when allowed; raises when neither is possible.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetEmptyValue()
    {
        if (Values.Count > 0)
        {
            AsInteger = Values[0].AsInteger;
        }
        else if (BoldAttributeRTInfo is null || BoldAttributeRTInfo.AllowNull)
        {
            SetToNull();
        }
        else
        {
            throw new EBold(SysUtils.Format(sNoLegalValuesAvailable, GetType().Name, ""));
        }
    }
}

/// <summary>A boolean: a value set of False (N, F, False) and True (Y, T, True).</summary>
[Delphi(PortStatus.Ported)]
public class TBABoolean : TBAValueSet
{
    public TBABoolean()
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is null && CanSetToNull(null))
        {
            SetContentToNull();
        }
        else if (Source is null)
        {
            ContentAsInteger = 0;
        }
        else if (Source is IBoldBooleanContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                ContentAsInteger = s.asBoolean ? 1 : 0;
            }
        }
        else
        {
            base.AssignContentValue(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected bool GetAsBoolean()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsBoolean", "");
        }

        EnsureNotNull();
        return GetContentAsInteger() != 0;
    }

    [Delphi(PortStatus.Ported)]
    protected void SetAsBoolean(bool Value) => AsInteger = Value ? 1 : 0;

    [Delphi(PortStatus.Ported)]
    protected override TBAValueSetValueList GetValues()
    {
        if (_BooleanValues is null)
        {
            var values = new TBAValueSetValueList();
            values.Add(0, "N", "F", "False");
            values.Add(1, "Y", "T", "True");
            _BooleanValues = values;
        }

        return _BooleanValues;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBABoolean_Proxy);

    [Delphi(PortStatus.Ported)]
    public override void SetAsVariant(object? Value) => AsBoolean = Value is bool b ? b : VariantToInteger(Value) != 0;

    [Delphi(PortStatus.Ported)]
    public override object? GetAsVariant() => AsBoolean;

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldBooleanContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldBooleanContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public bool AsBoolean
    {
        get => GetAsBoolean();
        set => SetAsBoolean(value);
    }
}

/// <summary>
/// A boolean that is the value of a model constraint on an element, evaluated with OCL when read. Representations 10 to
/// 14 give the constraint's name, message, expression, the element's display name and the element's string.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBAConstraint : TBABoolean
{
    private TBoldConstraintRTInfo? fConstraint;
    private TBoldElement? fOwningElement;
    private TBoldPassthroughSubscriber? fElementSubscriber;

    public TBAConstraint()
    {
    }

    [Delphi(PortStatus.Ported)]
    private void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (RequestedEvent == beDestroying)
        {
            fOwningElement = null;
            fElementSubscriber!.CancelAllSubscriptions();
        }
    }

    /// <summary>Evaluates the constraint into this boolean; false without a constraint or a boolean result.</summary>
    [Delphi(PortStatus.Ported)]
    private void CalculateConstraint()
    {
        if (Constraint is not null && OwningElement is not null)
        {
            using var ie = new TBoldIndirectElement();
            OwningElement.EvaluateExpression(Constraint.ConstraintExpression, ie);
            if (ie.Value is TBABoolean)
            {
                Assign(ie.Value);
            }
            else
            {
                AsBoolean = false;
            }
        }
        else
        {
            AsBoolean = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override int GetContentAsInteger()
    {
        CalculateConstraint();
        return base.GetContentAsInteger();
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) =>
        Representation switch
        {
            10 => Constraint?.ModelName ?? "",
            11 => Constraint?.ConstraintMessage ?? "",
            12 => Constraint?.ConstraintExpression ?? "",
            13 => OwningElement is TBoldDomainElement domainElement ? domainElement.DisplayName : "",
            14 => OwningElement?.AsString ?? "",
            _ => base.GetStringRepresentation(Representation),
        };

    [Delphi(PortStatus.Ported)]
    protected internal override void InitializeMember(TBoldDomainElement? AOwningElement, TBoldElementTypeInfo? ElementTypeInfo)
    {
        base.InitializeMember(AOwningElement, ElementTypeInfo);
        fElementSubscriber = new TBoldPassthroughSubscriber(Receive);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBAConstraint.Destroy")]
    public override void Dispose()
    {
        fElementSubscriber?.Dispose();
        fElementSubscriber = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        base.Assign(Source);
        if (Source is TBAConstraint constraint)
        {
            Initialize(constraint.Constraint, constraint.OwningElement);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void DefaultSubscribe(TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        if (OwningElement is not null && Constraint is not null)
        {
            OwningElement.SubscribeToExpression(Constraint.ConstraintExpression, Subscriber, RequestedEvent == breReSubscribe);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void SubscribeToStringRepresentation(TBoldRepresentation Representation, TBoldSubscriber? Subscriber, TBoldEvent RequestedEvent = breReEvaluate)
    {
        switch (Representation)
        {
            case >= 10 and <= 13:
                break;
            case 14:
                OwningElement?.SubscribeToStringRepresentation(brDefault, Subscriber, RequestedEvent);
                break;
            default:
                base.SubscribeToStringRepresentation(Representation, Subscriber, RequestedEvent);
                break;
        }
    }

    /// <summary>Sets the constraint and its element, checking that the expression is a boolean in the element's context.</summary>
    [Delphi(PortStatus.Ported)]
    public void Initialize(TBoldConstraintRTInfo? Constraint, TBoldElement? OwningElement)
    {
        fOwningElement = OwningElement;
        fOwningElement?.AddSmallSubscription(fElementSubscriber, TBoldSmallEventSet.Of(beDestroying, beObjectDeleted), beDestroying);
        fConstraint = Constraint;
        if (Constraint is not null && OwningElement is not null)
        {
            TBoldElementTypeInfo? ResType;
            try
            {
                ResType = OwningElement.Evaluator!.ExpressionType(Constraint.ConstraintExpression, OwningElement.BoldType, true);
            }
            catch (Exception e)
            {
                throw new EBold(SysUtils.Format(sInvalidConstraint, Constraint.ConstraintExpression, BoldDefs.BOLDCRLF, e.Message));
            }

            if (ResType is null)
            {
                throw new EBold(SysUtils.Format(sUnknownConstraintType, Constraint.ConstraintExpression, OwningElement.BoldType!.AsString));
            }

            if (!ResType.ConformsTo(((TBoldSystemTypeInfo)BoldType!.SystemTypeInfo!).AttributeTypeInfoByDelphiName(nameof(TBABoolean))))
            {
                throw new EBold(SysUtils.Format(sConstraintNotBoolean, Constraint.ConstraintExpression, OwningElement.BoldType!.AsString));
            }
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldConstraintRTInfo? Constraint => fConstraint;
    [Delphi(PortStatus.Ported)] public new TBoldElement? OwningElement => fOwningElement;
}

/// <summary>Gives a value set as integer and string contents.</summary>
[Delphi(PortStatus.Ported)]
internal class TBAValueSet_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldAttribute_Proxy(aProxedElement, aMode), IBoldIntegerContent, IBoldStringContent
{
    [Delphi(PortStatus.Ported)] private TBAValueSet GetProxedvalueSet() => (TBAValueSet)ProxedElement;
    [Delphi(PortStatus.Ported)] public int GetContentAsInteger() => ProxedValueSet.GetContentAsInteger();
    [Delphi(PortStatus.Ported)] public void SetContentAsInteger(int NewValue) => ProxedValueSet.SetContent(ProxedValueSet.Values.FindByInteger(NewValue));

    /// <summary>Sets the value with the default representation; an unknown string is ignored.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetContentAsString(string NewValue)
    {
        TBAValueSetValue? ValueSetValue = ProxedValueSet.Values.FindByString(brDefault, NewValue);
        if (ValueSetValue is not null)
        {
            ProxedValueSet.SetContent(ValueSetValue);
        }
    }

    [Delphi(PortStatus.Ported)] public string GetContentAsString() => ProxedValueSet.FValue!.GetStringRepresentation(brDefault);
    [Delphi(PortStatus.Ported)] protected TBAValueSet ProxedValueSet => GetProxedvalueSet();
}

/// <summary>Gives a boolean as boolean contents (and integer and string).</summary>
[Delphi(PortStatus.Ported)]
internal class TBABoolean_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBAValueSet_Proxy(aProxedElement, aMode), IBoldBooleanContent
{
    [Delphi(PortStatus.Ported)] public bool GetContentAsBoolean() => GetContentAsInteger() != 0;
    [Delphi(PortStatus.Ported)] public void SetContentAsBoolean(bool NewValue) => SetContentAsInteger(NewValue ? 1 : 0);
}
