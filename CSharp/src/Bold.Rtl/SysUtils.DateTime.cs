using System.Globalization;
using System.Text;

namespace Bold;

/// <summary>Delphi's TTimeStamp: milliseconds since midnight and days since 0001-01-01 plus one.</summary>
public struct TTimeStamp
{
    public int Time;
    public int Date;
}

/// <summary>
/// The parts of Delphi's TFormatSettings the port uses. Defaults match a Delphi program running with English (US)
/// settings and '/' as date separator.
/// </summary>
public sealed class TFormatSettings
{
    public char DateSeparator = '/';
    public char TimeSeparator = ':';
    public char DecimalSeparator = '.';
    public char ThousandSeparator = ',';
    public string ShortDateFormat = "M/d/yyyy";
    public string LongDateFormat = "dddd, MMMM d, yyyy";
    public string ShortTimeFormat = "h:mm AMPM";
    public string LongTimeFormat = "h:mm:ss AMPM";
    public string TimeAMString = "AM";
    public string TimePMString = "PM";
    public int TwoDigitYearCenturyWindow = 50;

    public string[] ShortMonthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    public string[] LongMonthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    public string[] ShortDayNames = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    public string[] LongDayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public TFormatSettings Clone() => (TFormatSettings)MemberwiseClone();
}

/// <summary>
/// Delphi date and time routines. TDateTime is <see cref="DateTime"/> (alias in SysUtils.Aliases.cs); Delphi's
/// floating point value is <see cref="DateTime.ToOADate"/>, which uses the same epoch (1899-12-30).
/// </summary>
public static partial class SysUtils
{
    public const int MSecsPerDay = 24 * 60 * 60 * 1000;
    public const int DateDelta = 693594;

    /// <summary>The global format settings, as Delphi's FormatSettings variable. Shared by the whole process.</summary>
    public static TFormatSettings FormatSettings = new();

    public static DateTime Now() => DateTime.Now;

    public static DateTime Date() => DateTime.Today;

    public static DateTime Time() => DateTime.FromOADate(0) + DateTime.Now.TimeOfDay;

    /// <summary>Delphi Int(TDateTime) / DateOf: the date part, midnight.</summary>
    public static DateTime DateOf(DateTime Value) => Value.Date;

    /// <summary>Delphi Frac(TDateTime) / TimeOf: the time part on day zero (1899-12-30).</summary>
    public static DateTime TimeOf(DateTime Value) => DateTime.FromOADate(0) + Value.TimeOfDay;

    /// <summary>Delphi TDateTime 0: 1899-12-30 00:00.</summary>
    public static DateTime ZeroDateTime => DateTime.FromOADate(0);

    public static DateTime EncodeDate(int Year, int Month, int Day)
    {
        if (Year < 1 || Year > 9999 || Month < 1 || Month > 12 || Day < 1 || Day > DateTime.DaysInMonth(Year, Month))
        {
            throw new EConvertError("Invalid argument to date encode");
        }

        return new DateTime(Year, Month, Day);
    }

    public static bool TryEncodeDate(int Year, int Month, int Day, out DateTime Date)
    {
        Date = default;
        if (Year < 1 || Year > 9999 || Month < 1 || Month > 12 || Day < 1 || Day > DateTime.DaysInMonth(Year, Month))
        {
            return false;
        }

        Date = new DateTime(Year, Month, Day);
        return true;
    }

    /// <summary>A time of day on Delphi's day zero (1899-12-30), as Delphi's EncodeTime.</summary>
    public static DateTime EncodeTime(int Hour, int Min, int Sec, int MSec)
    {
        if (Hour < 0 || Hour > 23 || Min < 0 || Min > 59 || Sec < 0 || Sec > 59 || MSec < 0 || MSec > 999)
        {
            throw new EConvertError("Invalid argument to time encode");
        }

        return DateTime.FromOADate(0).Add(new TimeSpan(0, Hour, Min, Sec, MSec));
    }

    public static void DecodeDate(DateTime Date, out int Year, out int Month, out int Day)
    {
        Year = Date.Year;
        Month = Date.Month;
        Day = Date.Day;
    }

    public static void DecodeTime(DateTime Time, out int Hour, out int Min, out int Sec, out int MSec)
    {
        Hour = Time.Hour;
        Min = Time.Minute;
        Sec = Time.Second;
        MSec = Time.Millisecond;
    }

    public static bool IsLeapYear(int Year) => DateTime.IsLeapYear(Year);

    public static TTimeStamp DateTimeToTimeStamp(DateTime DateTime) => new()
    {
        Time = (int)DateTime.TimeOfDay.TotalMilliseconds,
        Date = DateTime.Date.Subtract(System.DateTime.MinValue).Days + 1,
    };

    public static DateTime TimeStampToDateTime(TTimeStamp TimeStamp) =>
        System.DateTime.MinValue.AddDays(TimeStamp.Date - 1).AddMilliseconds(TimeStamp.Time);

    public static double TimeStampToMSecs(TTimeStamp TimeStamp) => (double)TimeStamp.Date * MSecsPerDay + TimeStamp.Time;

    public static TTimeStamp MSecsToTimeStamp(double MSecs) => new()
    {
        Date = (int)Math.Truncate(MSecs / MSecsPerDay),
        Time = (int)(MSecs - Math.Truncate(MSecs / MSecsPerDay) * MSecsPerDay),
    };

    public static string DateToStr(DateTime Date) => FormatDateTime("ddddd", Date);

    public static string TimeToStr(DateTime Time) => FormatDateTime("tt", Time);

    public static string DateTimeToStr(DateTime DateTime) => FormatDateTime("c", DateTime);

    /// <summary>
    /// Delphi FormatDateTime. Tokens: c d dd ddd dddd ddddd dddddd m mm mmm mmmm yy yyyy h hh n nn s ss z zzz t tt
    /// am/pm a/p ampm / : and quoted text. "m" right after "h" or "hh" means minutes, as in Delphi.
    /// </summary>
    public static string FormatDateTime(string Format, DateTime DateTime)
    {
        var sb = new StringBuilder();
        AppendFormatted(sb, Format, DateTime, FormatSettings, 0);
        return sb.ToString();
    }

    private static void AppendFormatted(StringBuilder sb, string format, DateTime dt, TFormatSettings fs, int depth)
    {
        if (depth > 2)
        {
            return;
        }

        bool useAmPm = HasAmPm(format);
        bool lastWasHour = false;
        int i = 0;
        while (i < format.Length)
        {
            char c = format[i];
            char lc = char.ToLowerInvariant(c);
            int run = 1;
            while (i + run < format.Length && char.ToLowerInvariant(format[i + run]) == lc && lc is 'd' or 'm' or 'y' or 'h' or 'n' or 's' or 'z' or 't')
            {
                run++;
            }

            switch (lc)
            {
                case '\'':
                case '"':
                {
                    int end = format.IndexOf(c, i + 1);
                    if (end < 0)
                    {
                        end = format.Length;
                    }

                    sb.Append(format, i + 1, end - i - 1);
                    i = end + 1;
                    continue;
                }
                case 'c':
                    AppendFormatted(sb, fs.ShortDateFormat, dt, fs, depth + 1);
                    if (dt.TimeOfDay != TimeSpan.Zero)
                    {
                        sb.Append(' ');
                        AppendFormatted(sb, fs.LongTimeFormat, dt, fs, depth + 1);
                    }

                    i++;
                    lastWasHour = false;
                    continue;
                case 'd':
                    switch (run)
                    {
                        case 1: sb.Append(dt.Day); break;
                        case 2: sb.Append(dt.Day.ToString("00", CultureInfo.InvariantCulture)); break;
                        case 3: sb.Append(fs.ShortDayNames[(int)dt.DayOfWeek]); break;
                        case 4: sb.Append(fs.LongDayNames[(int)dt.DayOfWeek]); break;
                        case 5: AppendFormatted(sb, fs.ShortDateFormat, dt, fs, depth + 1); break;
                        default: AppendFormatted(sb, fs.LongDateFormat, dt, fs, depth + 1); break;
                    }

                    lastWasHour = false;
                    break;
                case 'm':
                    if (lastWasHour && run <= 2)
                    {
                        sb.Append(run == 1 ? dt.Minute.ToString(CultureInfo.InvariantCulture) : dt.Minute.ToString("00", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        switch (run)
                        {
                            case 1: sb.Append(dt.Month); break;
                            case 2: sb.Append(dt.Month.ToString("00", CultureInfo.InvariantCulture)); break;
                            case 3: sb.Append(fs.ShortMonthNames[dt.Month - 1]); break;
                            default: sb.Append(fs.LongMonthNames[dt.Month - 1]); break;
                        }
                    }

                    lastWasHour = false;
                    break;
                case 'y':
                    sb.Append(run <= 2 ? (dt.Year % 100).ToString("00", CultureInfo.InvariantCulture) : dt.Year.ToString("0000", CultureInfo.InvariantCulture));
                    lastWasHour = false;
                    break;
                case 'h':
                {
                    int h = dt.Hour;
                    if (useAmPm)
                    {
                        h %= 12;
                        if (h == 0)
                        {
                            h = 12;
                        }
                    }

                    sb.Append(run == 1 ? h.ToString(CultureInfo.InvariantCulture) : h.ToString("00", CultureInfo.InvariantCulture));
                    lastWasHour = true;
                    break;
                }
                case 'n':
                    sb.Append(run == 1 ? dt.Minute.ToString(CultureInfo.InvariantCulture) : dt.Minute.ToString("00", CultureInfo.InvariantCulture));
                    lastWasHour = false;
                    break;
                case 's':
                    sb.Append(run == 1 ? dt.Second.ToString(CultureInfo.InvariantCulture) : dt.Second.ToString("00", CultureInfo.InvariantCulture));
                    lastWasHour = false;
                    break;
                case 'z':
                    sb.Append(run < 3 ? dt.Millisecond.ToString(CultureInfo.InvariantCulture) : dt.Millisecond.ToString("000", CultureInfo.InvariantCulture));
                    lastWasHour = false;
                    break;
                case 't':
                    AppendFormatted(sb, run == 1 ? fs.ShortTimeFormat : fs.LongTimeFormat, dt, fs, depth + 1);
                    lastWasHour = false;
                    break;
                case 'a':
                    if (string.Compare(format, i, "ampm", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        sb.Append(dt.Hour < 12 ? fs.TimeAMString : fs.TimePMString);
                        i += 4;
                        continue;
                    }

                    if (string.Compare(format, i, "am/pm", 0, 5, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string s = dt.Hour < 12 ? "am" : "pm";
                        sb.Append(char.IsUpper(c) ? s.ToUpperInvariant() : s);
                        i += 5;
                        continue;
                    }

                    if (string.Compare(format, i, "a/p", 0, 3, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string s = dt.Hour < 12 ? "a" : "p";
                        sb.Append(char.IsUpper(c) ? s.ToUpperInvariant() : s);
                        i += 3;
                        continue;
                    }

                    sb.Append(c);
                    break;
                case '/':
                    sb.Append(fs.DateSeparator);
                    break;
                case ':':
                    sb.Append(fs.TimeSeparator);
                    break;
                default:
                    sb.Append(c);
                    break;
            }

            i += run;
        }
    }

    private static bool HasAmPm(string format)
    {
        string f = format.ToLowerInvariant();
        return f.Contains("ampm") || f.Contains("am/pm") || f.Contains("a/p");
    }

    /// <summary>Delphi StrToDate: numbers separated by DateSeparator, in the order of ShortDateFormat.</summary>
    public static DateTime StrToDate(string S) =>
        TryStrToDate(S, out DateTime d) ? d : throw new EConvertError($"'{S}' is not a valid date");

    public static bool TryStrToDate(string S, out DateTime Value)
    {
        int pos = 0;
        return TryScanDate(S ?? "", ref pos, out Value) && SkipBlanks(S ?? "", pos) == (S ?? "").Length;
    }

    /// <summary>Delphi StrToDateTime: a date, a time, or a date followed by a time.</summary>
    public static DateTime StrToDateTime(string S) =>
        TryStrToDateTime(S, out DateTime d) ? d : throw new EConvertError($"'{S}' is not a valid date and time");

    public static bool TryStrToDateTime(string S, out DateTime Value)
    {
        S ??= "";
        int pos = 0;
        Value = default;
        if (TryScanDate(S, ref pos, out DateTime date))
        {
            pos = SkipBlanks(S, pos);
            if (pos == S.Length)
            {
                Value = date;
                return true;
            }

            if (TryScanTime(S, ref pos, out TimeSpan t) && SkipBlanks(S, pos) == S.Length)
            {
                Value = date + t;
                return true;
            }

            return false;
        }

        pos = 0;
        if (TryScanTime(S, ref pos, out TimeSpan time) && SkipBlanks(S, pos) == S.Length)
        {
            Value = DateTime.FromOADate(0) + time;
            return true;
        }

        return false;
    }

    public static DateTime StrToTime(string S)
    {
        int pos = 0;
        if (TryScanTime(S ?? "", ref pos, out TimeSpan t) && SkipBlanks(S ?? "", pos) == (S ?? "").Length)
        {
            return DateTime.FromOADate(0) + t;
        }

        throw new EConvertError($"'{S}' is not a valid time");
    }

    private static int SkipBlanks(string s, int pos)
    {
        while (pos < s.Length && s[pos] == ' ')
        {
            pos++;
        }

        return pos;
    }

    private static bool ScanNumber(string s, ref int pos, out int value, out int digits)
    {
        value = 0;
        digits = 0;
        pos = SkipBlanks(s, pos);
        while (pos < s.Length && s[pos] >= '0' && s[pos] <= '9' && digits < 9)
        {
            value = value * 10 + (s[pos] - '0');
            pos++;
            digits++;
        }

        return digits > 0;
    }

    private static bool ScanChar(string s, ref int pos, char c)
    {
        pos = SkipBlanks(s, pos);
        if (pos < s.Length && s[pos] == c)
        {
            pos++;
            return true;
        }

        return false;
    }

    private static string DateOrder(string shortDateFormat)
    {
        var order = new StringBuilder();
        foreach (char ch in shortDateFormat.ToUpperInvariant())
        {
            if ((ch == 'D' || ch == 'M' || ch == 'Y') && order.ToString().IndexOf(ch) < 0)
            {
                order.Append(ch);
            }
        }

        return order.Length == 3 ? order.ToString() : "MDY";
    }

    private static bool TryScanDate(string s, ref int pos, out DateTime date)
    {
        date = default;
        TFormatSettings fs = FormatSettings;
        string order = DateOrder(fs.ShortDateFormat);
        int start = pos;
        if (!ScanNumber(s, ref pos, out int n1, out int l1))
        {
            return false;
        }

        if (!ScanChar(s, ref pos, fs.DateSeparator))
        {
            pos = start;
            return false;
        }

        if (!ScanNumber(s, ref pos, out int n2, out int l2))
        {
            pos = start;
            return false;
        }

        int y, m, d, yLen;
        int save = pos;
        if (ScanChar(s, ref pos, fs.DateSeparator) && ScanNumber(s, ref pos, out int n3, out int l3))
        {
            int[] values = [n1, n2, n3];
            int[] lens = [l1, l2, l3];
            y = values[order.IndexOf('Y')];
            yLen = lens[order.IndexOf('Y')];
            m = values[order.IndexOf('M')];
            d = values[order.IndexOf('D')];
        }
        else
        {
            pos = save;
            y = DateTime.Today.Year;
            yLen = 4;
            if (order.IndexOf('M') < order.IndexOf('D'))
            {
                m = n1;
                d = n2;
            }
            else
            {
                d = n1;
                m = n2;
            }
        }

        if (yLen <= 2)
        {
            int current = DateTime.Today.Year;
            y += current / 100 * 100;
            if (fs.TwoDigitYearCenturyWindow > 0 && y < current - fs.TwoDigitYearCenturyWindow)
            {
                y += 100;
            }
        }

        if (!TryEncodeDate(y, m, d, out date))
        {
            pos = start;
            return false;
        }

        return true;
    }

    private static bool TryScanTime(string s, ref int pos, out TimeSpan time)
    {
        time = default;
        TFormatSettings fs = FormatSettings;
        int start = pos;
        if (!ScanNumber(s, ref pos, out int h, out _))
        {
            return false;
        }

        int min = 0, sec = 0, ms = 0;
        if (ScanChar(s, ref pos, fs.TimeSeparator))
        {
            if (!ScanNumber(s, ref pos, out min, out _))
            {
                pos = start;
                return false;
            }

            if (ScanChar(s, ref pos, fs.TimeSeparator))
            {
                if (!ScanNumber(s, ref pos, out sec, out _))
                {
                    pos = start;
                    return false;
                }

                if (ScanChar(s, ref pos, fs.DecimalSeparator) && !ScanNumber(s, ref pos, out ms, out _))
                {
                    pos = start;
                    return false;
                }
            }
        }

        int p = SkipBlanks(s, pos);
        string rest = s.Substring(p);
        if (rest.StartsWith(fs.TimeAMString, StringComparison.OrdinalIgnoreCase) || rest.StartsWith("AM", StringComparison.OrdinalIgnoreCase))
        {
            if (h == 12)
            {
                h = 0;
            }

            pos = p + (rest.StartsWith(fs.TimeAMString, StringComparison.OrdinalIgnoreCase) ? fs.TimeAMString.Length : 2);
        }
        else if (rest.StartsWith(fs.TimePMString, StringComparison.OrdinalIgnoreCase) || rest.StartsWith("PM", StringComparison.OrdinalIgnoreCase))
        {
            if (h < 12)
            {
                h += 12;
            }

            pos = p + (rest.StartsWith(fs.TimePMString, StringComparison.OrdinalIgnoreCase) ? fs.TimePMString.Length : 2);
        }

        if (h > 23 || min > 59 || sec > 59 || ms > 999)
        {
            pos = start;
            return false;
        }

        time = new TimeSpan(0, h, min, sec, ms);
        return true;
    }
}
