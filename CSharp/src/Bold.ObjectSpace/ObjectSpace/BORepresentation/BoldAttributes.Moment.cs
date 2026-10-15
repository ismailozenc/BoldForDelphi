// Delphi: Source/ObjectSpace/BORepresentation/BoldAttributes.pas
// Port map: CSharp/port/units/BoldAttributes.md
// Moments: date-time, date and time attributes and their proxies. TDateTime is System.DateTime; Delphi's Int and
// Frac of a TDateTime are SysUtils.DateOf and SysUtils.TimeOf (the time on day zero, 1899-12-30).

using System.Globalization;
using static Bold.BoldAttributes;
using static Bold.BoldCoreConsts;
using static Bold.BoldDefs;
using static Bold.BoldSubscription;
using static Bold.BoldSystem;

namespace Bold;

/// <summary>A point in time; subclasses expose it as date-time, date or time.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBAMoment : TBoldAttribute
{
    internal TDateTime fValue = SysUtils.ZeroDateTime;

    protected TBAMoment()
    {
    }

    [Delphi(PortStatus.Ported)]
    private void SetDataValue(TDateTime NewValue)
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
    internal void SetContent(TDateTime NewValue)
    {
        if (ContentIsNull || fValue != NewValue)
        {
            PreChange();
            fValue = NewValue;
            SetToNonNull();
            Changed(beValueChanged, [NewValue]);
        }
    }

    [Delphi(PortStatus.Ported)] private ushort GetDays() => (ushort)AsDate.Day;
    [Delphi(PortStatus.Ported)] private ushort GetHours() => (ushort)AsTime.Hour;
    [Delphi(PortStatus.Ported)] private ushort GetMinutes() => (ushort)AsTime.Minute;
    [Delphi(PortStatus.Ported)] private ushort GetMonths() => (ushort)AsDate.Month;
    [Delphi(PortStatus.Ported)] private ushort GetSeconds() => (ushort)AsTime.Second;
    [Delphi(PortStatus.Ported)] private ushort GetYears() => (ushort)AsDate.Year;

    [Delphi(PortStatus.Ported)]
    protected TDateTime GetAsDate()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsDate", "");
        }

        EnsureNotNull();
        return SysUtils.DateOf(fValue);
    }

    [Delphi(PortStatus.Ported)]
    protected TDateTime GetAsDateTime()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsDateTime", "");
        }

        EnsureNotNull();
        return fValue;
    }

    [Delphi(PortStatus.Ported)]
    protected TDateTime GetAsTime()
    {
        BoldClearLastFailure();
        if (!CanRead(null))
        {
            BoldRaiseLastFailure(this, "GetAsTime", "");
        }

        EnsureNotNull();
        return SysUtils.TimeOf(fValue);
    }

    /// <summary>Sets the date part, keeping the time unless null.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SetAsDate(TDateTime Value) =>
        SetAsDateTime(IsNull ? SysUtils.DateOf(Value) : SysUtils.DateOf(Value) + AsTime.TimeOfDay);

    [Delphi(PortStatus.Ported)]
    protected void SetAsDateTime(TDateTime Value) => SetDataValue(Value);

    /// <summary>Sets the time part, keeping the date unless null.</summary>
    [Delphi(PortStatus.Ported)]
    protected void SetAsTime(TDateTime Value) =>
        SetAsDateTime(IsNull ? SysUtils.TimeOf(Value) : AsDate + Value.TimeOfDay);

    [Delphi(PortStatus.Ported)]
    protected virtual bool MaySetValue(TDateTime NewValue, TBoldSubscriber? Subscriber) => true;

    [Delphi(PortStatus.Ported)]
    protected TDateTime AsDateTime
    {
        get => GetAsDateTime();
        set => SetAsDateTime(value);
    }

    [Delphi(PortStatus.Ported)]
    protected TDateTime AsDate
    {
        get => GetAsDate();
        set => SetAsDate(value);
    }

    [Delphi(PortStatus.Ported)]
    protected TDateTime AsTime
    {
        get => GetAsTime();
        set => SetAsTime(value);
    }

    [Delphi(PortStatus.Ported)] protected ushort Seconds => GetSeconds();
    [Delphi(PortStatus.Ported)] protected ushort Minutes => GetMinutes();
    [Delphi(PortStatus.Ported)] protected ushort Hours => GetHours();
    [Delphi(PortStatus.Ported)] protected ushort Days => GetDays();
    [Delphi(PortStatus.Ported)] protected ushort Months => GetMonths();
    [Delphi(PortStatus.Ported)] protected ushort Years => GetYears();

    [Delphi(PortStatus.Ported)]
    public override object? GetAsVariant() => GetAsDateTime();

    [Delphi(PortStatus.Ported, Note = "Takes a DateTime, or a number as a Delphi date (OLE automation date).")]
    public override void SetAsVariant(object? Value) =>
        SetAsDateTime(Value switch
        {
            DateTime d => d,
            double or float or int or long or decimal => DateTime.FromOADate(Convert.ToDouble(Value, CultureInfo.InvariantCulture)),
            _ => Convert.ToDateTime(Value, CultureInfo.CurrentCulture),
        });

    [Delphi(PortStatus.Ported)]
    public override void Assign(TBoldElement? Source)
    {
        if (Source is TBAMoment moment)
        {
            if (moment.IsNull)
            {
                SetToNull();
            }
            else
            {
                SetAsDateTime(moment.GetAsDateTime());
            }
        }
        else
        {
            base.Assign(Source);
        }
    }

    [Delphi(PortStatus.Ported)]
    public bool CanSetValue(TDateTime NewValue, TBoldSubscriber? Subscriber) =>
        MaySetValue(NewValue, Subscriber) && SendQuery(bqMaySetValue, [NewValue], Subscriber);

    /// <summary>Compares as date-time, date only (ctAsDate) or time only (ctAsTime).</summary>
    [Delphi(PortStatus.Ported)]
    public override int CompareToAs(TBoldCompareType CompType, TBoldElement? BoldElement)
    {
        if (BoldElement is TBAMoment other)
        {
            if (EitherIsNull(this, other))
            {
                return NullSmallest(BoldElement);
            }

            (TDateTime MyValue, TDateTime CompareValue) = CompType switch
            {
                TBoldCompareType.ctAsDate => (GetAsDate(), other.GetAsDate()),
                TBoldCompareType.ctAsTime => (GetAsTime(), other.GetAsTime()),
                _ => (GetAsDateTime(), other.GetAsDateTime()),
            };

            if (CompType is TBoldCompareType.ctDefault or TBoldCompareType.ctAsTime or TBoldCompareType.ctAsDate)
            {
                return MyValue == CompareValue ? 0 : MyValue > CompareValue ? 1 : -1;
            }

            return base.CompareToAs(CompType, BoldElement);
        }

        return base.CompareToAs(CompType, BoldElement);
    }

    [Delphi(PortStatus.Ported)]
    public override void SetEmptyValue() => SetAsDateTime(SysUtils.ZeroDateTime);
}

/// <summary>A date and time attribute.</summary>
[Delphi(PortStatus.Ported)]
public class TBADateTime : TBAMoment
{
    public TBADateTime()
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
            SetContent(SysUtils.ZeroDateTime);
        }
        else if (Source is IBoldDateTimeContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(s.asDateTime);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignContentValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        if (Representation != brDefault)
        {
            base.GetStringRepresentation(Representation);
        }

        return IsNull ? "" : SysUtils.DateTimeToStr(GetAsDateTime());
    }

    /// <summary>Empty sets null, &lt;NOW&gt; the current time, anything else is parsed.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, Meth_SetStringRepresentation, sStringValidationFailed);
        }

        if (Representation == brDefault)
        {
            if (Value.Length == 0)
            {
                SetToNull();
            }
            else if (SysUtils.UpperCase(Value) == DEFAULTNOW)
            {
                SetAsDateTime(SysUtils.Now());
            }
            else
            {
                SetAsDateTime(SysUtils.StrToDateTime(Value));
            }
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBADateTime_Proxy);

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldDateTimeContent s)
        {
            SetAsDateTime(s.asDateTime);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignValue));
        }
    }

    public new TDateTime AsDateTime
    {
        get => GetAsDateTime();
        set => SetAsDateTime(value);
    }

    public new TDateTime AsDate
    {
        get => GetAsDate();
        set => SetAsDate(value);
    }

    public new TDateTime AsTime
    {
        get => GetAsTime();
        set => SetAsTime(value);
    }

    public new ushort Seconds => base.Seconds;
    public new ushort Minutes => base.Minutes;
    public new ushort Hours => base.Hours;
    public new ushort Days => base.Days;
    public new ushort Months => base.Months;
    public new ushort Years => base.Years;

    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation)
    {
        if (Value.Length == 0)
        {
            return CanSetToNull(null);
        }

        if (SysUtils.UpperCase(Value) == DEFAULTNOW || SysUtils.TryStrToDateTime(Value, out _))
        {
            return true;
        }

        FormatFailure(Value, "datetime");
        return false;
    }

    [Delphi(PortStatus.Ported)]
    public override bool ValidateCharacter(char C, TBoldRepresentation Representation) =>
        C is >= '0' and <= '9' or ' ' || C == SysUtils.FormatSettings.TimeSeparator || C == SysUtils.FormatSettings.DateSeparator;

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldDateTimeContent) || IId == typeof(IBoldDateContent) || IId == typeof(IBoldTimeContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBold[Date][Time]Content");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }
}

/// <summary>A date attribute.</summary>
[Delphi(PortStatus.Ported)]
public class TBADate : TBAMoment
{
    public TBADate()
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
            SetContent(SysUtils.ZeroDateTime);
        }
        else if (Source is IBoldDateContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(s.asDate);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignContentValue));
        }
    }

    /// <summary>Empty sets null, &lt;NOW&gt; the current time; a date that does not parse is ignored, as in Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, Meth_SetStringRepresentation, sStringValidationFailed);
        }

        if (Representation == brDefault)
        {
            if (Value.Length == 0)
            {
                SetToNull();
            }
            else if (SysUtils.UpperCase(Value) == DEFAULTNOW)
            {
                SetAsDateTime(SysUtils.Now());
            }
            else
            {
                try
                {
                    SetAsDateTime(SysUtils.StrToDate(Value));
                }
                catch
                {
                    // Delphi swallows the exception here.
                }
            }
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        if (Representation != brDefault)
        {
            base.GetStringRepresentation(Representation);
        }

        return IsNull ? "" : SysUtils.DateToStr(GetAsDateTime());
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBADate_Proxy);

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldDateContent s)
        {
            SetAsDateTime(s.asDate);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation)
    {
        if (Value.Length == 0)
        {
            return CanSetToNull(null);
        }

        if (SysUtils.UpperCase(Value) == DEFAULTNOW || SysUtils.TryStrToDate(Value, out _))
        {
            return true;
        }

        FormatFailure(Value, "date");
        return false;
    }

    [Delphi(PortStatus.Ported)]
    public override bool ValidateCharacter(char C, TBoldRepresentation Representation) =>
        C is >= '0' and <= '9' || C == SysUtils.FormatSettings.DateSeparator;

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldDateContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldDateContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    public new TDateTime AsDate
    {
        get => GetAsDate();
        set => SetAsDate(value);
    }

    public new ushort Days => base.Days;
    public new ushort Months => base.Months;
    public new ushort Years => base.Years;
}

/// <summary>A time attribute.</summary>
[Delphi(PortStatus.Ported)]
public class TBATime : TBAMoment
{
    public TBATime()
    {
    }

    [Delphi(PortStatus.Ported)]
    private uint GetAsSeconds() => (uint)(Seconds + Minutes * 60 + Hours * 3600);

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignContentValue(IBoldValue Source)
    {
        if (Source is null && CanSetToNull(null))
        {
            SetContentToNull();
        }
        else if (Source is null)
        {
            SetContent(SysUtils.ZeroDateTime);
        }
        else if (Source is IBoldTimeContent s)
        {
            if (s.IsNull)
            {
                SetContentToNull();
            }
            else
            {
                SetContent(s.asTime);
            }
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignContentValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        if (!ValidateString(Value, Representation))
        {
            BoldRaiseLastFailure(this, Meth_SetStringRepresentation, sStringValidationFailed);
        }

        if (Representation == brDefault)
        {
            if (Value.Length == 0)
            {
                SetToNull();
            }
            else if (SysUtils.UpperCase(Value) == DEFAULTNOW)
            {
                SetAsDateTime(SysUtils.Now());
            }
            else
            {
                SetAsDateTime(SysUtils.StrToTime(Value));
            }
        }
        else
        {
            base.SetStringRepresentation(Representation, Value);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation)
    {
        if (Representation != brDefault)
        {
            base.GetStringRepresentation(Representation);
        }

        return IsNull ? "" : SysUtils.TimeToStr(GetAsDateTime());
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMember_ProxyClass ProxyClass() => typeof(TBATime_Proxy);

    [Delphi(PortStatus.Ported)]
    protected internal override void AssignValue(IBoldValue Source)
    {
        if (Source is IBoldTimeContent s)
        {
            SetAsDateTime(s.asTime);
        }
        else
        {
            throw new EBold(SysUtils.Format(sUnknownTypeOfSource, GetType().Name, Meth_AssignValue));
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool ValidateString(string Value, TBoldRepresentation Representation)
    {
        if (Value.Length == 0)
        {
            return CanSetToNull(null);
        }

        if (SysUtils.UpperCase(Value) == DEFAULTNOW)
        {
            return true;
        }

        try
        {
            SysUtils.StrToTime(Value);
            return true;
        }
        catch
        {
            FormatFailure(Value, "time");
            return false;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override bool ValidateCharacter(char C, TBoldRepresentation Representation) =>
        C is >= '0' and <= '9' || C == SysUtils.FormatSettings.TimeSeparator;

    [Delphi(PortStatus.Ported)]
    public override bool ProxyInterface(Type IId, TBoldDomainElementProxyMode Mode, out object? Obj)
    {
        if (IId == typeof(IBoldTimeContent))
        {
            return RetrieveProxyInterface(IId, Mode, out Obj, "IBoldTimeContent");
        }

        return base.ProxyInterface(IId, Mode, out Obj);
    }

    public new TDateTime AsTime
    {
        get => GetAsTime();
        set => SetAsTime(value);
    }

    [Delphi(PortStatus.Ported)] public uint AsSeconds => GetAsSeconds();
    public new ushort Seconds => base.Seconds;
    public new ushort Minutes => base.Minutes;
    public new ushort Hours => base.Hours;
}

/// <summary>Gives a moment's value as date-time, date and time contents.</summary>
[Delphi(PortStatus.Ported)]
internal class TBAMoment_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBoldAttribute_Proxy(aProxedElement, aMode)
{
    [Delphi(PortStatus.Ported)] protected TBAMoment GetProxedMoment() => (TBAMoment)ProxedElement;
    [Delphi(PortStatus.Ported)] public TDateTime GetContentAsDateTime() => ProxedMoment.fValue;
    [Delphi(PortStatus.Ported)] public void SetContentAsDateTime(TDateTime NewValue) => ProxedMoment.SetContent(NewValue);
    [Delphi(PortStatus.Ported)] public TDateTime GetContentAsDate() => SysUtils.DateOf(ProxedMoment.fValue);
    [Delphi(PortStatus.Ported)] public void SetContentAsDate(TDateTime NewValue) => ProxedMoment.SetContent(SysUtils.DateOf(NewValue) + GetContentAsTime().TimeOfDay);
    [Delphi(PortStatus.Ported)] public TDateTime GetContentAsTime() => SysUtils.TimeOf(ProxedMoment.fValue);
    [Delphi(PortStatus.Ported)] public void SetContentAsTime(TDateTime NewValue) => ProxedMoment.SetContent(GetContentAsDate() + NewValue.TimeOfDay);
    [Delphi(PortStatus.Ported)] protected TBAMoment ProxedMoment => GetProxedMoment();
}

/// <summary>Gives a date-time attribute as date-time, date and time contents.</summary>
[Delphi(PortStatus.Ported)]
internal class TBADateTime_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBAMoment_Proxy(aProxedElement, aMode), IBoldDateTimeContent, IBoldTimeContent, IBoldDateContent;

/// <summary>Gives a date attribute as date contents.</summary>
[Delphi(PortStatus.Ported)]
internal class TBADate_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBAMoment_Proxy(aProxedElement, aMode), IBoldDateContent;

/// <summary>Gives a time attribute as time contents.</summary>
[Delphi(PortStatus.Ported)]
internal class TBATime_Proxy(TBoldDomainElement aProxedElement, TBoldDomainElementProxyMode aMode)
    : TBAMoment_Proxy(aProxedElement, aMode), IBoldTimeContent;
