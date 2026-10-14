// Delphi: Source/Common/Support/BoldUtils.pas
// Port map: CSharp/port/units/BoldUtils.md

using System.Diagnostics;

[assembly: Delphi(PortStatus.Dropped, Unit = "BoldUtils", Symbol = "TBoldNotificationEvent", Note = "VCL component free notification; WinForms controls use their own events.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldUtils", Symbol = "TBoldPassthroughNotifier", Note = "VCL component free notification; WinForms controls use their own events.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldUtils", Symbol = "TBoldPassthroughNotifier.CreateWithEvent", Note = "VCL component free notification.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldUtils", Symbol = "TBoldPassthroughNotifier.Notification", Note = "VCL component free notification.")]

namespace Bold;

/// <summary>String, name, multiplicity, date and file helpers used across Bold.</summary>
public static partial class BoldUtils
{
    [Delphi(PortStatus.Ported)] public const string BoldProductNameShort = "BfD";
    [Delphi(PortStatus.Ported)] public const string BoldProductVersion = "4.0";

    /// <summary>True while Bold runs inside a designer. Always false in the C# port unless a designer host sets it.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldRunningAsDesignTimePackage = false;

    [Delphi(PortStatus.Ported, Note = "Uses Directory.Exists.")]
    public static bool DirectoryExists(string Name) => Directory.Exists(Name);

    [Delphi(PortStatus.Ported, Note = "Uses Directory.CreateDirectory, which creates every missing parent.")]
    public static bool ForceDirectories(string Dir)
    {
        if (string.IsNullOrEmpty(Dir))
        {
            throw new Exception(BoldCommonConst.sCannotCreateDirectory);
        }

        try
        {
            Directory.CreateDirectory(Dir);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>True when <paramref name="S"/> starts with <paramref name="Prefix"/>, case-sensitively.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldIsPrefix(string S, string Prefix) => (S ?? "").StartsWith(Prefix ?? "", StringComparison.Ordinal);

    /// <summary>True when the first <paramref name="Len"/> characters of both strings are equal.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes strings where Delphi takes PChar.")]
    public static bool BoldStrEqual(string P1, string P2, int Len) => string.CompareOrdinal(P1, 0, P2, 0, Len) == 0;

    /// <summary>True when the first <paramref name="Len"/> characters of both strings are equal, ignoring case.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes strings where Delphi takes PChar.")]
    public static bool BoldStrAnsiEqual(string P1, string P2, int Len) =>
        string.Compare(P1, 0, P2, 0, Len, StringComparison.OrdinalIgnoreCase) == 0;

    /// <summary>Case-insensitive equality where only the letters a to z fold case.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes strings where Delphi takes PChar.")]
    public static bool BoldStrCaseIndpendentEqual(string P1, string P2, int Len)
    {
        for (int i = 0; i < Len; i++)
        {
            char ch1 = P1[i];
            char ch2 = P2[i];
            if (ch1 != ch2)
            {
                char l1 = (char)(ch1 | 32);
                if (!(l1 >= 'a' && l1 <= 'z' && l1 == (char)(ch2 | 32)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Equal strings, ignoring case.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldAnsiEqual(string S1, string S2)
    {
        S1 ??= "";
        S2 ??= "";
        return S1.Length == S2.Length && BoldStrAnsiEqual(S1, S2, S1.Length);
    }

    [Delphi(PortStatus.Ported, Note = "Takes a string where Delphi takes PChar.")]
    public static bool BoldStrStringEqual(string S1, string P2, int Len) =>
        Len == (S1 ?? "").Length && BoldStrEqual(S1 ?? "", P2, Len);

    /// <summary>1-based position of <paramref name="Substr"/> in <paramref name="S"/> ignoring case, 0 when absent.</summary>
    [Delphi(PortStatus.Ported)]
    public static int BoldCaseIndependentPos(string Substr, string S)
    {
        int substrLen = SysUtils.Length(Substr);
        if (substrLen > SysUtils.Length(S))
        {
            return 0;
        }

        int result = SysUtils.Pos(Substr, S);
        if (result == 0 || result > substrLen)
        {
            result = SysUtils.Pos(SysUtils.UpperCase(Substr), SysUtils.UpperCase(S));
        }

        return result;
    }

    /// <summary>Appends <paramref name="ch"/> to a non-empty string that does not already end with it.</summary>
    [Delphi(PortStatus.Ported)]
    public static void EnsureTrailing(ref string Str, char ch)
    {
        if (Str.Length > 0 && Str[^1] != ch)
        {
            Str += ch;
        }
    }

    [Delphi(PortStatus.Ported)]
    public static string BooleanToString(bool BoolValue) => BoolValue ? "True" : "False";

    /// <summary>True for "Y", "T" and "TRUE" in any case.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool StringToBoolean(string StrValue)
    {
        string u = SysUtils.UpperCase(StrValue);
        return u == "Y" || u == "T" || u == "TRUE";
    }

    [Delphi(PortStatus.Ported, Note = "Only the key path; the port does not read the Windows registry for settings.")]
    public static string BoldRootRegistryKey() =>
        SysUtils.Format("Software\\BoldSoft\\%s\\%s", BoldProductNameShort, BoldProductVersion);

    [Delphi(PortStatus.Ported, Note = "The path of the running process.")]
    public static string GetModuleFileNameAsString(bool IncludePath)
    {
        string moduleName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? "";
        return IncludePath ? moduleName : SysUtils.ExtractFileName(moduleName);
    }

    /// <summary>Adds the names of the enumeration <paramref name="aTypeInfo"/> in value order.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the enum Type where Delphi takes PTypeInfo.")]
    public static void EnumToStrings(Type aTypeInfo, TStrings Strings)
    {
        foreach (object value in Enum.GetValues(aTypeInfo).Cast<object>().OrderBy(v => Convert.ToInt64(v)))
        {
            Strings.Add(Enum.GetName(aTypeInfo, value)!);
        }
    }

    /// <summary>Names are equal ignoring case.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldNamesEqual(string name1, string name2) => SysUtils.AnsiCompareText(name1, name2) == 0;

    /// <summary>The strings joined by <paramref name="Separator"/>, wrapped in pre and post strings when not empty.</summary>
    [Delphi(PortStatus.Ported)]
    public static string BoldSeparateStringList(TStringList strings, string Separator, string PreString, string PostString)
    {
        if (strings.Count == 0)
        {
            return "";
        }

        return PreString + string.Join(Separator, strings) + PostString;
    }

    /// <summary>
    /// Appends <paramref name="aString"/> to the last line (or a new line), with line breaks turned to spaces, and wraps
    /// lines longer than 80 characters at a space, comma or equals sign.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldAppendToStrings(TStrings strings, string aString, bool ForceNewLine)
    {
        strings.BeginUpdate();
        try
        {
            string tempStr = (aString ?? "").Replace(BoldDefs.BOLDLF, ' ').Replace(BoldDefs.BOLDCR, ' ');
            int strCount = strings.Count - 1;
            if (strCount == -1 || ForceNewLine)
            {
                strings.Add("");
                strCount++;
            }

            strings[strCount] = strings[strCount] + tempStr;
            while (strings[strCount].Length > 80)
            {
                int splitterPos = 80;
                string line = strings[strCount];
                while (!(line[splitterPos - 1] is ' ' or ',' or '=') && splitterPos > 1)
                {
                    splitterPos--;
                }

                strings.Add(SysUtils.Copy(line, splitterPos + 1, 65536));
                strings[strCount] = SysUtils.Copy(line, 1, splitterPos);
                strCount++;
            }
        }
        finally
        {
            strings.EndUpdate();
        }
    }

    /// <summary>S1 and S2 joined by <paramref name="Separator"/>; an empty side is left out with its separator.</summary>
    [Delphi(PortStatus.Ported)]
    public static string BoldSeparatedAppend(string S1, string S2, string Separator = ",")
    {
        if (string.IsNullOrEmpty(S1))
        {
            return S2 ?? "";
        }

        if (string.IsNullOrEmpty(S2))
        {
            return S1;
        }

        return S1 + Separator + S2;
    }

    /// <summary>Removes leading and trailing control characters and spaces (everything up to ' ').</summary>
    [Delphi(PortStatus.Ported)]
    public static string BoldTrim(string S)
    {
        S ??= "";
        int l = S.Length;
        int i = 0;
        while (i < l && S[i] <= ' ')
        {
            i++;
        }

        if (i == l)
        {
            return "";
        }

        while (S[l - 1] <= ' ')
        {
            l--;
        }

        return i > 0 || l < S.Length ? S.Substring(i, l - i) : S;
    }

    /// <summary>Puts a space before each capital letter that follows a non-capital: "FirstName" becomes "First Name".</summary>
    [Delphi(PortStatus.Ported)]
    public static string CapitalisedToSpaced(string Capitalised)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < Capitalised.Length; i++)
        {
            char c = Capitalised[i];
            if (i > 0 && c >= 'A' && c <= 'Z' && !(Capitalised[i - 1] >= 'A' && Capitalised[i - 1] <= 'Z'))
            {
                result.Append(' ');
            }

            result.Append(c);
        }

        return result.ToString();
    }

    /// <summary>Removes the spaces.</summary>
    [Delphi(PortStatus.Ported)]
    public static string SpacedToCapitalised(string Spaced) => (Spaced ?? "").Replace(" ", "");

    /// <summary>The lower bound of a UML multiplicity such as "0..1", "1" or "1..*"; -1 when not a number.</summary>
    [Delphi(PortStatus.Ported)]
    public static int GetLowerLimitForMultiplicity(string Multiplicity)
    {
        int p = SysUtils.Pos("..", Multiplicity);
        return p == 0
            ? SysUtils.StrToIntDef(Multiplicity, 0)
            : SysUtils.StrToIntDef(SysUtils.Copy(Multiplicity, 1, p - 1), -1);
    }

    /// <summary>The upper bound of a UML multiplicity; MaxInt for "*" or "n"; 1 when the multiplicity is empty.</summary>
    [Delphi(PortStatus.Ported)]
    public static int GetUpperLimitForMultiplicity(string Multiplicity)
    {
        int result;
        if (string.IsNullOrEmpty(Multiplicity) || BoldTrim(Multiplicity) == "")
        {
            result = 1;
        }
        else
        {
            int p = SysUtils.Pos("..", Multiplicity);
            result = p == 0
                ? SysUtils.StrToIntDef(Multiplicity, SysUtils.MaxInt)
                : SysUtils.StrToIntDef(SysUtils.Copy(Multiplicity, p + 2, SysUtils.MaxInt), SysUtils.MaxInt);
        }

        return result < 0 ? SysUtils.MaxInt : result;
    }

    /// <summary>The strings as an array, or null (Delphi's Unassigned) when the list is empty.</summary>
    [Delphi(PortStatus.Ported, Note = "Returns string[] where Delphi returns a variant array, and null for Unassigned.")]
    public static object? StringListToVarArray(TStringList List) => List.Count == 0 ? null : List.ToStringArray();

    /// <summary>True when the name is empty or names this computer.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool IsLocalMachine(string Machinename)
    {
        string machName = BoldTrim(Machinename);
        return machName == "" || SysUtils.AnsiCompareText(GetComputerNameStr(), machName) == 0;
    }

    [Delphi(PortStatus.Ported, Note = "Uses Environment.MachineName.")]
    public static string GetComputerNameStr() => Environment.MachineName;

    /// <summary>-1, 0 or 1 as <paramref name="Time1"/> is before, equal to or after <paramref name="Time2"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public static int TimeStampComp(TTimeStamp Time1, TTimeStamp Time2)
    {
        double cTime1 = SysUtils.TimeStampToMSecs(Time1);
        double cTime2 = SysUtils.TimeStampToMSecs(Time2);
        return cTime1 == cTime2 ? 0 : cTime1 > cTime2 ? 1 : -1;
    }

    /// <summary>Parses a date with <paramref name="DateFormat"/> as the short date format.</summary>
    [Delphi(PortStatus.Ported)]
    public static TDateTime StrToDateFmt(string S, string DateFormat, char DateSeparatorChar = '/')
    {
        TFormatSettings fs = SysUtils.FormatSettings;
        string previousShortDateFormat = fs.ShortDateFormat;
        char previousDateSeparator = fs.DateSeparator;
        fs.ShortDateFormat = DateFormat;
        fs.DateSeparator = DateSeparatorChar;
        try
        {
            return SysUtils.StrToDateTime(S);
        }
        finally
        {
            fs.ShortDateFormat = previousShortDateFormat;
            fs.DateSeparator = previousDateSeparator;
        }
    }

    /// <summary>Formats a date with <paramref name="DateFormat"/> as the short date format.</summary>
    [Delphi(PortStatus.Ported)]
    public static string DateToStrFmt(TDateTime aDate, string DateFormat, char DateSeparatorChar = '/')
    {
        TFormatSettings fs = SysUtils.FormatSettings;
        string previousShortDateFormat = fs.ShortDateFormat;
        char previousDateSeparator = fs.DateSeparator;
        fs.ShortDateFormat = DateFormat;
        fs.DateSeparator = DateSeparatorChar;
        try
        {
            return SysUtils.DateToStr(aDate);
        }
        finally
        {
            fs.ShortDateFormat = previousShortDateFormat;
            fs.DateSeparator = previousDateSeparator;
        }
    }

    /// <summary>Adds each element of a one-dimensional array to <paramref name="Strings"/>; returns how many.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes a .NET array where Delphi takes a variant array.")]
    public static int BoldVariantToStrings(object? V, TStrings Strings)
    {
        int result = 0;
        if (V is Array a && a.Rank == 1)
        {
            foreach (object? item in a)
            {
                Strings.Add(Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture) ?? "");
                result++;
            }
        }

        return result;
    }

    /// <summary>How many times <paramref name="c"/> occurs in <paramref name="s"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public static int CharCount(char c, string s) => (s ?? "").Count(ch => ch == c);

    /// <summary>
    /// Parses <paramref name="value"/> with the first matching format. A format is the letters y, m and d for digits and
    /// any other character literally, such as "yyyy-mm-dd". Two-digit years use the century window.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Fixed: Delphi put a two-digit year past the century window in the next century (90 became 2190); here it is the previous century (1990).")]
    public static bool BoldParseFormattedDateList(string value, TStrings formats, ref TDateTime Date)
    {
        for (int i = 0; i < formats.Count; i++)
        {
            if (InternalTryToParse(value, formats[i], ref Date))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly int[] DaysPerMonth = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private static bool InternalTryToParse(string value, string format, ref TDateTime date)
    {
        int currentYear = DateTime.Now.Year;
        if (value.Length != format.Length)
        {
            return false;
        }

        string y = "", m = "", d = "";
        for (int i = 0; i < format.Length; i++)
        {
            switch (format[i])
            {
                case 'y': y += value[i]; break;
                case 'm': m += value[i]; break;
                case 'd': d += value[i]; break;
                default:
                    if (value[i] != format[i])
                    {
                        return false;
                    }

                    break;
            }

            if (format[i] is 'y' or 'm' or 'd' && !(value[i] >= '0' && value[i] <= '9'))
            {
                return false;
            }
        }

        int year;
        if (y.Length == 0)
        {
            year = currentYear;
        }
        else
        {
            year = SysUtils.StrToInt(y);
            if (y.Length == 2)
            {
                if (year < (currentYear + SysUtils.FormatSettings.TwoDigitYearCenturyWindow) % 100)
                {
                    year += currentYear / 100 * 100;
                }
                else
                {
                    year += (currentYear / 100 - 1) * 100;
                }
            }
        }

        // Delphi raises EConvertError here when the format has no m or d; the port does too.
        int month = SysUtils.StrToInt(m);
        int day = SysUtils.StrToInt(d);
        if (month < 1 || month > 12)
        {
            return false;
        }

        if (day < 1 || day > DaysPerMonth[month - 1])
        {
            return false;
        }

        if (month == 2 && !DateTime.IsLeapYear(year) && day > 28)
        {
            return false;
        }

        date = SysUtils.EncodeDate(year, month, day);
        return true;
    }

    /// <summary>As <see cref="BoldParseFormattedDateList"/> with the formats given as an array.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldParseFormattedDate(string value, string[] formats, ref TDateTime Date) =>
        BoldParseFormattedDateList(value, new TStringList(formats), ref Date);
}
