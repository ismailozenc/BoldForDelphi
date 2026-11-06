// Delphi: Source/Common/Support/BoldIsoDateTime.pas
// Port map: CSharp/port/units/BoldIsoDateTime.md

using static Bold.BoldSupportConst;

namespace Bold;

/// <summary>Reads dates and times written as ISO 8601: YYYY-MM-DD, HH:MM[:SS] and YYYY-MM-DD HH:MM[:SS].</summary>
public static class BoldIsoDateTime
{
    private static readonly int[] daysPerMonth = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    /// <summary>True when the text has the pattern's length and characters, a digit where the pattern has '#'.</summary>
    [Delphi(PortStatus.Ported)]
    private static bool MatchPattern(string pattern, string s)
    {
        bool result = pattern.Length == s.Length;
        for (int i = 0; result && i < pattern.Length; i++)
        {
            result = pattern[i] == '#' ? s[i] is >= '0' and <= '9' : s[i] == pattern[i];
        }

        return result;
    }

    /// <summary>The date of a text YYYY-MM-DD.</summary>
    [Delphi(PortStatus.Ported)]
    public static TDateTime ParseISODate(string s)
    {
        if (!MatchPattern("####-##-##", s))
        {
            throw new EBold(SysUtils.Format(sInvalidDateFormatFormat, s));
        }

        int y = SysUtils.StrToInt(s.Substring(0, 4));
        int m = SysUtils.StrToInt(s.Substring(5, 2));
        if (m > 12)
        {
            throw new EBold(SysUtils.Format(sInnvalidDateFormatLargeMonth, s));
        }

        if (m < 1)
        {
            throw new EBold(SysUtils.Format(sInvalidDateFormatSmallMonth, s));
        }

        int d = SysUtils.StrToInt(s.Substring(8, 2));
        if (d < 1)
        {
            throw new EBold(SysUtils.Format(sInvalidDateFormatSmallDay, s));
        }

        if (d > daysPerMonth[m - 1])
        {
            throw new EBold(SysUtils.Format(sInvalidDateFormatBadDay, s, daysPerMonth[m - 1], m));
        }

        return SysUtils.EncodeDate(y, m, d);
    }

    /// <summary>The date and time of a text YYYY-MM-DD HH:MM[:SS].</summary>
    [Delphi(PortStatus.Ported)]
    public static TDateTime ParseISODateTime(string s)
    {
        if (!MatchPattern("####-##-## ##:##", s) && !MatchPattern("####-##-## ##:##:##", s))
        {
            throw new EBold(SysUtils.Format(sInvalidDateTimeFormat, s));
        }

        return ParseISODate(s.Substring(0, 10)) + ParseISOTime(s.Substring(11)).TimeOfDay;
    }

    /// <summary>The time of a text HH:MM[:SS], on Delphi's day zero as EncodeTime gives it.</summary>
    [Delphi(PortStatus.Ported, Note = "The seconds may be left out, as the pattern allows; Delphi read them anyway and raised EConvertError.")]
    public static TDateTime ParseISOTime(string str)
    {
        if (!MatchPattern("##:##:##", str) && !MatchPattern("##:##", str))
        {
            throw new EBold(SysUtils.Format(sInvalidTimeFormat, str));
        }

        int h = SysUtils.StrToInt(str.Substring(0, 2));
        if (h > 23)
        {
            throw new EBold(SysUtils.Format(sInvalidTimeFormatLargeHour, str));
        }

        int m = SysUtils.StrToInt(str.Substring(3, 2));
        if (m > 59)
        {
            throw new EBold(SysUtils.Format(sInvalidTimeFormatLargeMinute, str));
        }

        int s = str.Length > 6 ? SysUtils.StrToInt(str.Substring(6, 2)) : 0;
        if (s > 59)
        {
            throw new EBold(SysUtils.Format(sInvalidTimeFormatLargeSecond, str));
        }

        return SysUtils.EncodeTime(h, m, s, 0);
    }
}
