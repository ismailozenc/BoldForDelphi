// Delphi: Source/ObjectSpace/BORepresentation/BoldAttributes.pas
// Port map: CSharp/port/units/BoldAttributes.md
// The unit's constants and registration, strings and numbers. Blobs, moments and value sets are in the other
// BoldAttributes.*.cs files.

using System.Globalization;
using System.Runtime.CompilerServices;
using static Bold.BoldCoreConsts;
using static Bold.BoldDefs;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldAttributes", Symbol = "finalization", Note = "Freed the boolean values and removed the descriptors; the process owns them until it ends.")]

namespace Bold;

/// <summary>The unit's constants, and the registration of its attribute classes as member types.</summary>
public static partial class BoldAttributes
{
    [Delphi(PortStatus.Ported)] public const int bqBaseAttributes = bqMaxSystem + 1;
    [Delphi(PortStatus.Ported)] public const int bqMaySetContentType = bqBaseAttributes + 0;
    [Delphi(PortStatus.Ported)] public const int bqMaxAttributes = bqBaseAttributes + 0;

    internal const string DEFAULTNOW = "<NOW>";
    internal const string Meth_GetStringRepresentation = "GetStringRepresentation";
    internal const string Meth_SetStringRepresentation = "SetStringRepresentation";
    internal const string Meth_AssignContentValue = "AssignContentValue";
    internal const string Meth_AssignValue = "AssignValue";

    /// <summary>The values of booleans, created on first use.</summary>
    internal static TBAValueSetValueList? _BooleanValues;

    /// <summary>Registers the attribute classes as member types, when the assembly loads.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization", Note = "A module initializer, so the classes are registered before any model is used.")]
#pragma warning disable CA2255 // The registration must run before any system type info is built, as Delphi's initialization section does.
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialization()
    {
        TBoldMemberTypeList List = BoldMemberTypeDictionary.BoldMemberTypes();
        List.AddMemberTypeDescriptor(typeof(TBoldAttribute), TBoldAbstractionLevel.alAbstract);
        List.AddMemberTypeDescriptor(typeof(TBAString), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBATrimmedString), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBANumeric), TBoldAbstractionLevel.alAbstract);
        List.AddMemberTypeDescriptor(typeof(TBAInteger), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAWord), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBASmallInt), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAByte), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAShortInt), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAFloat), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBACurrency), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBABlob), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBATypedBlob), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAMoment), TBoldAbstractionLevel.alAbstract);
        List.AddMemberTypeDescriptor(typeof(TBADateTime), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBADate), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBATime), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAValueSet), TBoldAbstractionLevel.alAbstract);
        List.AddMemberTypeDescriptor(typeof(TBABoolean), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBAConstraint), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBABlobImageJPEG), TBoldAbstractionLevel.alConcrete);
        List.AddMemberTypeDescriptor(typeof(TBABlobImageBMP), TBoldAbstractionLevel.alConcrete);
    }

    /// <summary>Converts a variant (object) to an integer as Delphi's variant conversion does.</summary>
    internal static int VariantToInteger(object? Value) =>
        Value is bool b ? (b ? -1 : 0) : Convert.ToInt32(Value, CultureInfo.InvariantCulture);

    internal static double VariantToFloat(object? Value) => Convert.ToDouble(Value, CultureInfo.InvariantCulture);
}

/// <summary>A string attribute; the model's length (when not -1) limits it.</summary>
[Delphi(PortStatus.Ported)]
public class TBAString : TBoldAttribute
{
    internal string fValue = "";

    public TBAString()
    {
    }

    [Delphi(PortStatus.Ported)]
    private void SetDataValue(string NewValue)
    {
        BoldClearLastFailure();
        if (!CanSetValue(NewValue, null))
        {
            BoldRaiseLastFailure(this, "SetDataValue", "");
        }

        if (IsNull || fValue != NewValue)
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
    internal void SetContent(string NewValue)
    {
        if (BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid || ContentIsNull || fValue != NewValue)
        {
            PreChange();
            fValue = NewValue;
            SetToNonNull();
            Changed(beValueChanged, [NewValue]);
        }
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
            SetContent("");
        }
        else if (Source is IBoldStringContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(s.asString);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, BoldAttributes.Meth_AssignContentValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void FreeContent()
    {
        base.FreeContent();
        fValue = "";
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, BoldAttributes.Meth_GetStringRepresentation, "");
        }

        if (Representation != brDefault)
        {
            base.GetStringRepresentation(Representation);
        }

        return IsNull ? "" : fValue;
    }

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, BoldAttributes.Meth_SetStringRepresentation, sStringValidationFailed);
        }

        EnsureValidString(Value, Representation);
        if (Representation == brDefault)
        {
            SetDataValue(Value);
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool MaySetValue(string NewValue, TBoldSubscriber? Subscriber) => true;

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBAString_Proxy);

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBAString str)
        {
            if (str.IsNull)
            {
                SetToNull();
            }
            else
            {
                AsString = Source.AsString;
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldStringContent s)
        {
            SetDataValue(s.asString);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, BoldAttributes.Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBAString other)
        {
            if (EitherIsNull(this, other))
            {
                return NullSmallest(BoldElement);
            }

            return CompType switch
            {
                TBoldCompareType.ctDefault => SysUtils.AnsiCompareText(AsString, other.AsString),
                TBoldCompareType.ctAsString => SysUtils.CompareStr(AsString, other.AsString),
                TBoldCompareType.ctAsText => SysUtils.CompareText(AsString, other.AsString),
                TBoldCompareType.ctAsAnsiString => SysUtils.AnsiCompareStr(AsString, other.AsString),
                TBoldCompareType.ctAsAnsiText => SysUtils.AnsiCompareText(AsString, other.AsString),
                _ => base.CompareToAs(CompType, BoldElement),
            };
        }

        return base.CompareToAs(CompType, BoldElement);
    }

    /// <summary>True when the model's length allows the string; otherwise sets the failure reason.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation)
    {
        if (BoldAttributeRTInfo is not null)
        {
            bool result = BoldAttributeRTInfo.Length == -1 || Value.Length <= BoldAttributeRTInfo.Length;
            if (!result)
            {
                SetBoldLastFailureReason(new TBoldFailureReason(sStringTooLong, this));
            }

            return result;
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSetValue(string NewValue, TBoldSubscriber? Subscriber) =>
        MaySetValue(NewValue, Subscriber) && SendQuery(bqMaySetValue, [NewValue], Subscriber);

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldStringContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldStringContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public override void SetEmptyValue() => AsString = "";
}

/// <summary>Gives a string attribute as IBoldStringContent.</summary>
[Delphi(PortStatus.Ported)]
public class TBAString_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldAttribute_Proxy(aProxedElement, aMode), IBoldStringContent
{
    [Delphi(PortStatus.Ported)] private TBAString GetProxedString() => (TBAString)ProxedElement;
    [Delphi(PortStatus.Ported)] public void SetContentAsString(string NewValue) => ProxedString.SetContent(NewValue);
    [Delphi(PortStatus.Ported)] public string GetContentAsString() => ProxedString.fValue;
    [Delphi(PortStatus.Ported)] protected TBAString ProxedString => GetProxedString();
}

/// <summary>A string attribute that trims what is set as a string.</summary>
[Delphi(PortStatus.Ported)]
public class TBATrimmedString : TBAString
{
    public TBATrimmedString()
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value) =>
        base.SetStringRepresentation(Representation, SysUtils.Trim(Value));
}

/// <summary>A number: integer, float or currency.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBANumeric : TBoldAttribute
{
    protected TBANumeric()
    {
    }

    [Delphi(PortStatus.Ported)] protected abstract double GetAsFloat();
    [Delphi(PortStatus.Ported)] protected abstract void SetAsInteger(int Value);

    [Delphi(PortStatus.Ported)]
    public override void SetEmptyValue() => AsInteger = 0;

    [Delphi(PortStatus.Ported)] public double AsFloat => GetAsFloat();

    [Delphi(PortStatus.Ported)]
    public int AsInteger
    {
        set => SetAsInteger(value);
    }
}

/// <summary>An integer attribute; subclasses narrow the range.</summary>
[Delphi(PortStatus.Ported)]
public class TBAInteger : TBANumeric
{
    internal int fValue;

    public TBAInteger()
    {
    }

    /// <summary>Sets the value; a value out of range raises EBoldInternal with the failure reason.</summary>
    [Delphi(PortStatus.Ported)]
    internal void SetDataValue(int NewValue)
    {
        BoldClearLastFailure();
        if (!CanSetValue(NewValue, null))
        {
            BoldRaiseLastFailure(this, "SetDataValue", "");
        }

        if (!CheckRange(NewValue))
        {
            throw new EBoldInternal(SysUtils.Format("%s: %s", DisplayName, GetBoldLastFailureReason()!.Reason));
        }

        if (IsNull || fValue != NewValue)
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
    internal void SetContent(int NewValue)
    {
        if (BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid || ContentIsNull || fValue != NewValue)
        {
            PreChange();
            fValue = NewValue;
            SetToNonNull();
            Changed(beValueChanged, [NewValue]);
        }
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
            SetContent(0);
        }
        else if (Source is IBoldIntegerContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(s.asInteger);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, BoldAttributes.Meth_AssignContentValue));
        }
    }

    /// <summary>True when the value is within the bounds; otherwise sets the failure reason.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool CheckRangeWithBounds(int Value, int Min, int Max)
    {
        if (Value < Min || Value > Max)
        {
            SetBoldLastFailureReason(new TBoldFailureReason(sRangeError, [GetType().Name, Min, Max, Value], this));
            return false;
        }

        return true;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual int GetAsInteger()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsInteger", "");
        }

        EnsureNotNull();
        return fValue;
    }

    [Delphi(PortStatus.Ported)]
    protected override double GetAsFloat()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsFloat", "");
        }

        EnsureNotNull();
        return fValue;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, BoldAttributes.Meth_GetStringRepresentation, "");
        }

        if (Representation != brDefault)
        {
            base.GetStringRepresentation(Representation);
        }

        return IsNull ? "" : SysUtils.IntToStr(GetAsInteger());
    }

    [Delphi(PortStatus.Ported)]
    protected override void SetAsInteger(int Value) => SetDataValue(Value);

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, BoldAttributes.Meth_SetStringRepresentation, sStringValidationFailed);
        }

        if (Representation == brDefault)
        {
            if (Value.Length == 0)
            {
                SetToNull();
            }
            else
            {
                SetDataValue(SysUtils.StrToInt(Value));
            }
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool MaySetValue(int NewValue, TBoldSubscriber? Subscriber) => CheckRange(NewValue);

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBAInteger_Proxy);

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBAInteger i)
        {
            if (i.IsNull)
            {
                SetToNull();
            }
            else
            {
                AsInteger = i.AsInteger;
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldIntegerContent s)
        {
            SetDataValue(s.asInteger);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, BoldAttributes.Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSetValue(int Value, TBoldSubscriber? Subscriber) =>
        MaySetValue(Value, Subscriber) && SendQuery(bqMaySetValue, [Value], Subscriber);

    [Delphi(PortStatus.Ported)]
    public virtual bool CheckRange(int Value) => CheckRangeWithBounds(Value, int.MinValue, int.MaxValue);

    /// <summary>Compares with integers by value and with other numbers by float value.</summary>
    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBAInteger other)
        {
            if (EitherIsNull(this, other))
            {
                return NullSmallest(BoldElement);
            }

            return CompType == TBoldCompareType.ctDefault ? AsInteger.CompareTo(other.AsInteger) : base.CompareToAs(CompType, BoldElement);
        }

        if (BoldElement is TBANumeric numeric)
        {
            if (EitherIsNull(this, numeric))
            {
                return NullSmallest(BoldElement);
            }

            if (CompType == TBoldCompareType.ctDefault)
            {
                double compareValue = numeric.AsFloat;
                return AsInteger == compareValue ? 0 : AsInteger > compareValue ? 1 : -1;
            }

            return base.CompareToAs(CompType, BoldElement);
        }

        return base.CompareToAs(CompType, BoldElement);
    }

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldIntegerContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldIntegerContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    [Delphi(PortStatus.Ported)]
    public override void SetAsVariant(object? Value) => AsInteger = BoldAttributes.VariantToInteger(Value);

    [Delphi(PortStatus.Ported)]
    public override object? GetAsVariant() => AsInteger;

    /// <summary>An empty string when null is allowed, else a number in range.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation) =>
        Value.Length == 0 ? CanSetToNull(null) : CheckRange(SysUtils.StrToInt(SysUtils.Trim(Value)));

    [Delphi(PortStatus.Ported)]
    public override bool ValidateCharacter(char C, TBoldRepresentation Representation) => C is >= '0' and <= '9' or '-' or '+';

    [Delphi(PortStatus.Ported)]
    public new int AsInteger
    {
        get => GetAsInteger();
        set => SetAsInteger(value);
    }
}

/// <summary>An integer attribute in the SmallInt range.</summary>
[Delphi(PortStatus.Ported)]
public class TBASmallInt : TBAInteger
{
    public TBASmallInt()
    {
    }

    [Delphi(PortStatus.Ported)] private short GetAsSmallInt() => (short)AsInteger;
    [Delphi(PortStatus.Ported)] private void SetAsSmallInt(short Value) => SetDataValue(Value);
    [Delphi(PortStatus.Ported)] public override bool CheckRange(int Value) => CheckRangeWithBounds(Value, short.MinValue, short.MaxValue);

    [Delphi(PortStatus.Ported)]
    public short AsSmallInt
    {
        get => GetAsSmallInt();
        set => SetAsSmallInt(value);
    }
}

/// <summary>An integer attribute in the ShortInt range.</summary>
[Delphi(PortStatus.Ported)]
public class TBAShortInt : TBASmallInt
{
    public TBAShortInt()
    {
    }

    [Delphi(PortStatus.Ported)] private sbyte GetAsShortInt() => (sbyte)AsInteger;
    [Delphi(PortStatus.Ported)] private void SetAsShortInt(sbyte Value) => SetDataValue(Value);
    [Delphi(PortStatus.Ported)] public override bool CheckRange(int Value) => CheckRangeWithBounds(Value, sbyte.MinValue, sbyte.MaxValue);

    [Delphi(PortStatus.Ported)]
    public sbyte AsShortInt
    {
        get => GetAsShortInt();
        set => SetAsShortInt(value);
    }
}

/// <summary>An integer attribute in the Word range.</summary>
[Delphi(PortStatus.Ported)]
public class TBAWord : TBAInteger
{
    public TBAWord()
    {
    }

    [Delphi(PortStatus.Ported)] private ushort GetAsWord() => (ushort)AsInteger;
    [Delphi(PortStatus.Ported)] private void SetAsWord(ushort Value) => SetDataValue(Value);
    [Delphi(PortStatus.Ported)] public override bool CheckRange(int Value) => CheckRangeWithBounds(Value, ushort.MinValue, ushort.MaxValue);

    [Delphi(PortStatus.Ported)]
    public ushort AsWord
    {
        get => GetAsWord();
        set => SetAsWord(value);
    }
}

/// <summary>An integer attribute in the Byte range.</summary>
[Delphi(PortStatus.Ported)]
public class TBAByte : TBAWord
{
    public TBAByte()
    {
    }

    [Delphi(PortStatus.Ported)] private byte GetAsByte() => (byte)AsInteger;
    [Delphi(PortStatus.Ported)] private void SetAsByte(byte Value) => SetDataValue(Value);
    [Delphi(PortStatus.Ported)] public override bool CheckRange(int Value) => CheckRangeWithBounds(Value, byte.MinValue, byte.MaxValue);

    [Delphi(PortStatus.Ported)]
    public byte AsByte
    {
        get => GetAsByte();
        set => SetAsByte(value);
    }
}

/// <summary>A floating point attribute.</summary>
[Delphi(PortStatus.Ported)]
public class TBAFloat : TBANumeric
{
    internal double fValue;

    public TBAFloat()
    {
    }

    [Delphi(PortStatus.Ported)]
    internal void SetContent(double NewValue)
    {
        if (ContentIsNull || fValue != NewValue)
        {
            PreChange();
            fValue = NewValue;
            SetToNonNull();
            Changed(beValueChanged, [NewValue]);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDataValue(double NewValue)
    {
        BoldClearLastFailure();
        if (!CanSetValue(NewValue, null))
        {
            BoldRaiseLastFailure(this, "SetDataValue", "");
        }

        if (IsNull || fValue != NewValue)
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

    /// <summary>Takes float, currency or integer contents.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is null && CanSetToNull(null))
        {
            SetContentToNull();
        }
        else if (Source is null)
        {
            SetContent(0);
        }
        else if (Source is IBoldFloatContent FloatContent)
        {
            if (FloatContent.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(FloatContent.asFloat);
            }
        }
        else if (Source is IBoldCurrencyContent CurrencyContent)
        {
            if (CurrencyContent.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent((double)CurrencyContent.asCurrency);
            }
        }
        else if (Source is IBoldIntegerContent IntegerContent)
        {
            if (IntegerContent.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(IntegerContent.asInteger);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, BoldAttributes.Meth_AssignContentValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override double GetAsFloat()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsFloat", "");
        }

        EnsureNotNull();
        return fValue;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, BoldAttributes.Meth_GetStringRepresentation, "");
        }

        if (Representation != brDefault)
        {
            base.GetStringRepresentation(Representation);
        }

        return IsNull ? "" : SysUtils.FloatToStr(GetAsFloat());
    }

    [Delphi(PortStatus.Ported)] protected virtual void SetAsFloat(double Value) => SetDataValue(Value);
    [Delphi(PortStatus.Ported)] protected override void SetAsInteger(int Value) => SetAsFloat(Value);

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, BoldAttributes.Meth_SetStringRepresentation, sStringValidationFailed);
        }

        if (Representation == brDefault)
        {
            if (Value.Length == 0)
            {
                SetToNull();
            }
            else
            {
                SetDataValue(SysUtils.StrToFloat(Value));
            }
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool MaySetValue(double NewValue, TBoldSubscriber? Subscriber) => true;

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBAFloat_Proxy);

    [Delphi(PortStatus.Ported)]
    public override void SetAsVariant(object? Value) => AsFloat = BoldAttributes.VariantToFloat(Value);

    [Delphi(PortStatus.Ported)]
    public override object? GetAsVariant() => AsFloat;

    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation)
    {
        if (Value.Length == 0)
        {
            return CanSetToNull(null);
        }

        bool result = SysUtils.TryStrToFloat(Value, out _);
        if (!result)
        {
            FormatFailure(Value, "float");
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override bool ValidateCharacter(char C, TBoldRepresentation Representation) =>
        C is >= '0' and <= '9' or '-' or '+' or 'e' or 'E' || C == SysUtils.FormatSettings.DecimalSeparator;

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBANumeric numeric)
        {
            if (numeric.IsNull)
            {
                SetToNull();
            }
            else
            {
                AsFloat = numeric.AsFloat;
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldFloatContent s)
        {
            SetDataValue(s.asFloat);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, BoldAttributes.Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBANumeric numeric)
        {
            if (EitherIsNull(this, numeric))
            {
                return NullSmallest(BoldElement);
            }

            double CompareValue = numeric.AsFloat;
            if (CompType == TBoldCompareType.ctDefault)
            {
                return AsFloat == CompareValue ? 0 : AsFloat > CompareValue ? 1 : -1;
            }

            return base.CompareToAs(CompType, BoldElement);
        }

        return base.CompareToAs(CompType, BoldElement);
    }

    [Delphi(PortStatus.Ported)]
    public new int AsInteger
    {
        set => SetAsInteger(value);
    }

    [Delphi(PortStatus.Ported)]
    public new double AsFloat
    {
        get => GetAsFloat();
        set => SetAsFloat(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSetValue(double NewValue, TBoldSubscriber? Subscriber) =>
        MaySetValue(NewValue, Subscriber) && SendQuery(bqMaySetValue, [NewValue], Subscriber);

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldFloatContent))
        {
            return RetrieveProxyInterface(typeof(IBoldFloatContent), Mode, out Obj, "IBoldFloatContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }
}

/// <summary>A currency attribute: a decimal with four decimals (Delphi's Currency).</summary>
[Delphi(PortStatus.Ported)]
public class TBACurrency : TBANumeric
{
    internal decimal fValue;

    public TBACurrency()
    {
    }

    [Delphi(PortStatus.Ported)]
    internal void SetContent(decimal NewValue)
    {
        if (ContentIsNull || fValue != NewValue)
        {
            PreChange();
            fValue = NewValue;
            SetToNonNull();
            Changed(beValueChanged, [NewValue]);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDataValue(decimal NewValue)
    {
        BoldClearLastFailure();
        if (!CanSetValue(NewValue, null))
        {
            BoldRaiseLastFailure(this, "SetDataValue", "");
        }

        if (IsNull || fValue != NewValue)
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

    /// <summary>Takes currency, float or integer contents.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is null && CanSetToNull(null))
        {
            SetContentToNull();
        }
        else if (Source is null)
        {
            SetContent(0);
        }
        else if (Source is IBoldCurrencyContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(s.asCurrency);
            }
        }
        else if (Source is IBoldFloatContent FloatContent)
        {
            if (FloatContent.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(SysUtils.DoubleToCurr(FloatContent.asFloat));
            }
        }
        else if (Source is IBoldIntegerContent IntegerContent)
        {
            if (IntegerContent.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(IntegerContent.asInteger);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, BoldAttributes.Meth_AssignContentValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected decimal GetAsCurrency()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsCurrency", "");
        }

        EnsureNotNull();
        return fValue;
    }

    [Delphi(PortStatus.Ported)]
    protected override double GetAsFloat()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsFloat", "");
        }

        EnsureNotNull();
        return (double)fValue;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, BoldAttributes.Meth_GetStringRepresentation, "");
        }

        if (Representation != brDefault)
        {
            base.GetStringRepresentation(Representation);
        }

        return IsNull ? "" : SysUtils.CurrToStr(GetAsCurrency());
    }

    [Delphi(PortStatus.Ported)] protected void SetAsCurrency(decimal Value) => SetDataValue(Value);
    [Delphi(PortStatus.Ported)] protected virtual void SetAsFloat(double Value) => SetDataValue(SysUtils.DoubleToCurr(Value));
    [Delphi(PortStatus.Ported)] protected override void SetAsInteger(int Value) => SetAsFloat(Value);

    /// <summary>Sets from a string; a trailing decimal separator gets a zero.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, BoldAttributes.Meth_SetStringRepresentation, sStringValidationFailed);
        }

        if (Representation == brDefault)
        {
            if (Value.Length == 0)
            {
                SetToNull();
            }
            else
            {
                if (Value[^1] == SysUtils.FormatSettings.DecimalSeparator)
                {
                    Value += "0";
                }

                SetDataValue(SysUtils.StrToCurr(Value));
            }
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool MaySetValue(decimal NewValue, TBoldSubscriber? Subscriber) => true;

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBACurrency_Proxy);

    [Delphi(PortStatus.Ported)]
    public override void SetAsVariant(object? Value) => AsCurrency = Convert.ToDecimal(Value, CultureInfo.InvariantCulture);

    [Delphi(PortStatus.Ported)]
    public override object? GetAsVariant() => AsCurrency;

    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation)
    {
        if (Value.Length == 0)
        {
            return CanSetToNull(null);
        }

        bool result = SysUtils.TryStrToCurr(Value, out _);
        if (!result)
        {
            FormatFailure(Value, "currency");
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public override bool ValidateCharacter(char C, TBoldRepresentation Representation) =>
        C is >= '0' and <= '9' or '-' or '+' or 'e' or 'E' || C == SysUtils.FormatSettings.DecimalSeparator;

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBANumeric numeric)
        {
            if (numeric.IsNull)
            {
                SetToNull();
            }
            else if (Source is TBACurrency currency)
            {
                AsCurrency = currency.AsCurrency;
            }
            else
            {
                AsCurrency = SysUtils.DoubleToCurr(numeric.AsFloat);
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldCurrencyContent s)
        {
            SetDataValue(s.asCurrency);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, BoldAttributes.Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBANumeric numeric)
        {
            if (EitherIsNull(this, numeric))
            {
                return NullSmallest(BoldElement);
            }

            decimal CompareValue = BoldElement is TBACurrency currency ? currency.AsCurrency : SysUtils.DoubleToCurr(numeric.AsFloat);
            if (CompType == TBoldCompareType.ctDefault)
            {
                return AsCurrency == CompareValue ? 0 : AsCurrency > CompareValue ? 1 : -1;
            }

            return base.CompareToAs(CompType, BoldElement);
        }

        return base.CompareToAs(CompType, BoldElement);
    }

    [Delphi(PortStatus.Ported)]
    public decimal AsCurrency
    {
        get => GetAsCurrency();
        set => SetAsCurrency(value);
    }

    [Delphi(PortStatus.Ported)]
    public new double AsFloat
    {
        get => GetAsFloat();
        set => SetAsFloat(value);
    }

    [Delphi(PortStatus.Ported)]
    public new int AsInteger
    {
        set => SetAsInteger(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSetValue(decimal NewValue, TBoldSubscriber? Subscriber) =>
        MaySetValue(NewValue, Subscriber) && SendQuery(bqMaySetValue, [NewValue], Subscriber);

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldCurrencyContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldCurrencyContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }
}

/// <summary>Gives an integer attribute as IBoldIntegerContent.</summary>
[Delphi(PortStatus.Ported)]
internal class TBAInteger_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldAttribute_Proxy(aProxedElement, aMode), IBoldIntegerContent
{
    [Delphi(PortStatus.Ported)] private TBAInteger GetProxedInteger() => (TBAInteger)ProxedElement;
    [Delphi(PortStatus.Ported)] public int GetContentAsInteger() => ProxedInteger.fValue;
    [Delphi(PortStatus.Ported)] public void SetContentAsInteger(int NewValue) => ProxedInteger.SetContent(NewValue);
    [Delphi(PortStatus.Ported)] protected TBAInteger ProxedInteger => GetProxedInteger();
}

/// <summary>Gives a float attribute as IBoldFloatContent.</summary>
[Delphi(PortStatus.Ported)]
internal class TBAFloat_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldAttribute_Proxy(aProxedElement, aMode), IBoldFloatContent
{
    [Delphi(PortStatus.Ported)] private TBAFloat GetProxedFloat() => (TBAFloat)ProxedElement;
    [Delphi(PortStatus.Ported)] public double GetContentAsFloat() => ProxedFloat.fValue;
    [Delphi(PortStatus.Ported)] public void SetContentAsFloat(double NewValue) => ProxedFloat.SetContent(NewValue);
    [Delphi(PortStatus.Ported)] protected TBAFloat ProxedFloat => GetProxedFloat();
}

/// <summary>Gives a currency attribute as IBoldCurrencyContent.</summary>
[Delphi(PortStatus.Ported)]
internal class TBACurrency_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldAttribute_Proxy(aProxedElement, aMode), IBoldCurrencyContent
{
    [Delphi(PortStatus.Ported)] private TBACurrency GetProxedCurrency() => (TBACurrency)ProxedElement;
    [Delphi(PortStatus.Ported)] public decimal GetContentAsCurrency() => ProxedCurrency.fValue;
    [Delphi(PortStatus.Ported)] public void SetContentAsCurrency(decimal NewValue) => ProxedCurrency.SetContent(NewValue);
    [Delphi(PortStatus.Ported)] protected TBACurrency ProxedCurrency => GetProxedCurrency();
}
