// Delphi: Source/Common/UML/BoldUMLTypes.pas
// Port map: CSharp/port/units/BoldUMLTypes.md

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldParameterDirectionKind { pdIn, pdOut, pdInout, pdReturn }

[Delphi(PortStatus.Ported)]
public enum TAggregationKind { akNone, akAggregate, akComposite }

[Delphi(PortStatus.Ported)]
public enum TVisibilityKind { vkPrivate, vkProtected, vkPublic }

[Delphi(PortStatus.Ported)]
public enum TChangeableKind { ckChangeable, ckFrozen, ckAddOnly }

[Delphi(PortStatus.Ported)]
public enum TScopeKind { skInstance, skClassifier }

[Delphi(PortStatus.Ported)]
public enum TOrderingKind { okUnordered, okOrdered }

[Delphi(PortStatus.Ported)]
public enum TPseudostateKind { pkInitial, pkDeepHistory, pkShallowHistory, pkJoin, pkFork, pkBranch, pkJunction, pkFinal }

[Delphi(PortStatus.Ported)]
public enum TCallConcurrencyKind { cckSequential, cckGuarded, cckConcurrent }

[Delphi(PortStatus.Ported)]
public enum TMessageDirectionKind { mdkActivation, mdkReturn }

/// <summary>One range of a UML multiplicity, such as "1", "0..1" or "1..*". An unlimited upper bound is -1.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLRange : TBoldMemoryManagedObject
{
    private int fUpper;
    private bool fUpperUnlimited;
    private int fLower;

    [Delphi(PortStatus.Ported)]
    private string GetAsString() => FormatAsString();

    /// <summary>Parses "n", "*", "3", "0..1" or "1..*"; a non-numeric upper bound means unlimited.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetAsString(string Value)
    {
        int dotDotPos = SysUtils.Pos("..", Value);
        if (dotDotPos == 0)
        {
            string lowerString = SysUtils.Trim(Value);
            if (lowerString is "n" or "*")
            {
                Lower = 0;
                UpperUnlimited = true;
            }
            else
            {
                Lower = SysUtils.StrToInt(lowerString);
                Upper = Lower;
            }
        }
        else
        {
            string lowerString = SysUtils.Trim(SysUtils.Copy(Value, 1, dotDotPos - 1));
            string upperString = SysUtils.Trim(SysUtils.Copy(Value, dotDotPos + 2, SysUtils.MaxInt));
            Lower = SysUtils.StrToInt(lowerString);
            Upper = SysUtils.StrToIntDef(upperString, -1);
            if (Upper < 0)
            {
                UpperUnlimited = true;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetUpper(int Value)
    {
        fUpper = Value;
        fUpperUnlimited = false;
    }

    [Delphi(PortStatus.Ported)]
    private void SetUpperUnlimited(bool Value)
    {
        fUpperUnlimited = Value;
        if (Value)
        {
            fUpper = -1;
        }
    }

    /// <summary>The range as text, with <paramref name="UnlimitedString"/> for an unlimited upper bound.</summary>
    [Delphi(PortStatus.Ported)]
    public string FormatAsString(string UnlimitedString = "n")
    {
        if (UpperUnlimited)
        {
            return Lower == 0 ? UnlimitedString : SysUtils.Format("%d..%s", Lower, UnlimitedString);
        }

        return Upper == Lower ? SysUtils.IntToStr(Lower) : SysUtils.Format("%d..%d", Lower, Upper);
    }

    [Delphi(PortStatus.Ported)]
    public bool InRange(int value) => value >= Lower && (UpperUnlimited || value <= Upper);

    [Delphi(PortStatus.Ported)]
    public string AsString
    {
        get => GetAsString();
        set => SetAsString(value);
    }

    [Delphi(PortStatus.Ported)]
    public int Lower
    {
        get => fLower;
        set => fLower = value;
    }

    [Delphi(PortStatus.Ported)]
    public int Upper
    {
        get => fUpper;
        set => SetUpper(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool UpperUnlimited
    {
        get => fUpperUnlimited;
        set => SetUpperUnlimited(value);
    }
}

/// <summary>A UML multiplicity: comma-separated ranges such as "0..1,3..*".</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMultiplicity : TBoldObjectArray
{
    [Delphi(PortStatus.Ported)]
    public TBoldUMLMultiplicity()
        : base(1, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner))
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldUMLRange GetRange(int index) => (TBoldUMLRange)this[index]!;

    [Delphi(PortStatus.Ported)]
    private string GetAsString() => FormatAsString();

    [Delphi(PortStatus.Ported)]
    private void SetAsString(string Value)
    {
        Clear();
        string s = Value ?? "";
        while (s.Length > 0)
        {
            int commaPos = SysUtils.Pos(",", s);
            int endPos = commaPos == 0 ? s.Length : commaPos - 1;
            AddRange().AsString = SysUtils.Copy(s, 1, endPos);
            s = commaPos == 0 ? "" : SysUtils.Copy(s, commaPos + 1, SysUtils.MaxInt);
        }
    }

    [Delphi(PortStatus.Ported)]
    public string FormatAsString(string UnlimitedString = "n")
    {
        string result = "";
        for (int i = 0; i < Count; i++)
        {
            if (result.Length > 0)
            {
                result += ",";
            }

            result += Range(i).FormatAsString(UnlimitedString);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool InRange(int value)
    {
        for (int i = 0; i < Count; i++)
        {
            if (Range(i).InRange(value))
            {
                return true;
            }
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    public string AsString
    {
        get => GetAsString();
        set => SetAsString(value);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldUMLRange AddRange()
    {
        var result = new TBoldUMLRange();
        Add(result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldUMLRange Range(int index) => GetRange(index);
}
