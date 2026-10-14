using System.Globalization;
using System.Text;

namespace Bold;

/// <summary>Raised by conversions such as StrToInt, as Delphi's EConvertError.</summary>
public class EConvertError(string? message = null, Exception? innerException = null) : Exception(message, innerException);

/// <summary>Raised by list operations with a bad index, as Delphi's EListError.</summary>
public class EListError(string? message = null, Exception? innerException = null) : Exception(message, innerException);

/// <summary>Options of <see cref="SysUtils.StringReplace"/>.</summary>
public enum TReplaceFlag { rfReplaceAll, rfIgnoreCase }

/// <summary>
/// Delphi SysUtils routines the ported code relies on, with Delphi semantics: 1-based string positions, Delphi format
/// strings, case-insensitive comparisons that follow Delphi. Use them with <c>using static Bold.SysUtils;</c>.
/// Numbers are formatted with the invariant culture, as Delphi does with '.' as decimal separator.
/// </summary>
public static partial class SysUtils
{
    public const string sLineBreak = "\r\n";
    public const int MaxInt = int.MaxValue;

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>
    /// Delphi FreeAndNil: clears the variable first, then disposes the object when it is IDisposable. Code that runs
    /// during the dispose sees the variable as nil, as in Delphi.
    /// </summary>
    public static void FreeAndNil<T>(ref T? obj)
        where T : class
    {
        T? temp = obj;
        obj = null;
        (temp as IDisposable)?.Dispose();
    }

    // ---- Strings ----

    /// <summary>1-based position of <paramref name="substr"/> in <paramref name="s"/>, 0 when absent.</summary>
    public static int Pos(string substr, string? s) =>
        string.IsNullOrEmpty(substr) || s is null ? 0 : s.IndexOf(substr, StringComparison.Ordinal) + 1;

    /// <summary>Pos from a 1-based offset.</summary>
    public static int PosEx(string substr, string? s, int offset = 1)
    {
        if (string.IsNullOrEmpty(substr) || s is null || offset < 1 || offset > s.Length)
        {
            return 0;
        }

        return s.IndexOf(substr, offset - 1, StringComparison.Ordinal) + 1;
    }

    /// <summary>Delphi Copy: <paramref name="count"/> characters from 1-based <paramref name="index"/>, clipped to the string.</summary>
    public static string Copy(string? s, int index, int count)
    {
        if (s is null || count <= 0)
        {
            return "";
        }

        if (index < 1)
        {
            index = 1;
        }

        if (index > s.Length)
        {
            return "";
        }

        long available = s.Length - index + 1;
        return s.Substring(index - 1, (int)Math.Min(available, count));
    }

    public static int Length(string? s) => s?.Length ?? 0;

    public static string UpperCase(string? s) => UpperAscii(s ?? "");

    public static string LowerCase(string? s) => LowerAscii(s ?? "");

    public static string AnsiUpperCase(string? s) => (s ?? "").ToUpperInvariant();

    public static string AnsiLowerCase(string? s) => (s ?? "").ToLowerInvariant();

    private static string UpperAscii(string s)
    {
        var chars = s.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= 'a' and <= 'z')
            {
                chars[i] = (char)(chars[i] - 32);
            }
        }

        return new string(chars);
    }

    private static string LowerAscii(string s)
    {
        var chars = s.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= 'A' and <= 'Z')
            {
                chars[i] = (char)(chars[i] + 32);
            }
        }

        return new string(chars);
    }

    /// <summary>Case-insensitive comparison (ASCII letters), &lt;0, 0 or &gt;0.</summary>
    public static int CompareText(string? s1, string? s2) => string.CompareOrdinal(UpperAscii(s1 ?? ""), UpperAscii(s2 ?? ""));

    public static int AnsiCompareText(string? s1, string? s2) => string.Compare(s1 ?? "", s2 ?? "", StringComparison.OrdinalIgnoreCase);

    public static int CompareStr(string? s1, string? s2) => string.CompareOrdinal(s1 ?? "", s2 ?? "");

    public static int AnsiCompareStr(string? s1, string? s2) => string.CompareOrdinal(s1 ?? "", s2 ?? "");

    public static bool SameText(string? s1, string? s2) => CompareText(s1, s2) == 0;

    public static bool AnsiSameText(string? s1, string? s2) => AnsiCompareText(s1, s2) == 0;

    /// <summary>Removes leading and trailing characters up to and including space, as Delphi Trim.</summary>
    public static string Trim(string? s)
    {
        s ??= "";
        int start = 0, end = s.Length - 1;
        while (start <= end && s[start] <= ' ')
        {
            start++;
        }

        while (end >= start && s[end] <= ' ')
        {
            end--;
        }

        return s.Substring(start, end - start + 1);
    }

    public static string TrimLeft(string? s)
    {
        s ??= "";
        int start = 0;
        while (start < s.Length && s[start] <= ' ')
        {
            start++;
        }

        return s[start..];
    }

    public static string TrimRight(string? s)
    {
        s ??= "";
        int end = s.Length;
        while (end > 0 && s[end - 1] <= ' ')
        {
            end--;
        }

        return s[..end];
    }

    public static string QuotedStr(string? s) => AnsiQuotedStr(s, '\'');

    public static string AnsiQuotedStr(string? s, char quote) =>
        quote + (s ?? "").Replace(quote.ToString(), new string(quote, 2), StringComparison.Ordinal) + quote;

    /// <summary>Removes the quotes added by AnsiQuotedStr; returns the text unchanged when it is not quoted.</summary>
    public static string AnsiDequotedStr(string? s, char quote)
    {
        s ??= "";
        if (s.Length < 2 || s[0] != quote || s[^1] != quote)
        {
            return s;
        }

        return s[1..^1].Replace(new string(quote, 2), quote.ToString(), StringComparison.Ordinal);
    }

    public static string StringReplace(string? s, string oldPattern, string newPattern, DelphiSet<TReplaceFlag> flags)
    {
        s ??= "";
        if (oldPattern.Length == 0)
        {
            return s;
        }

        var comparison = flags.Contains(TReplaceFlag.rfIgnoreCase) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (flags.Contains(TReplaceFlag.rfReplaceAll))
        {
            return s.Replace(oldPattern, newPattern, comparison);
        }

        int index = s.IndexOf(oldPattern, comparison);
        return index < 0 ? s : string.Concat(s.AsSpan(0, index), newPattern, s.AsSpan(index + oldPattern.Length));
    }

    public static string StringOfChar(char c, int count) => count <= 0 ? "" : new string(c, count);

    public static string DupeString(string s, int count) => count <= 0 ? "" : string.Concat(Enumerable.Repeat(s, count));

    public static bool IsDelimiter(string delimiters, string s, int index) =>
        index >= 1 && index <= s.Length && delimiters.Contains(s[index - 1], StringComparison.Ordinal);

    public static int LastDelimiter(string delimiters, string s)
    {
        for (int i = s.Length - 1; i >= 0; i--)
        {
            if (delimiters.Contains(s[i], StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return 0;
    }

    // ---- Numbers ----

    public static string IntToStr(long value) => value.ToString(Invariant);

    public static string IntToHex(long value, int digits) => value.ToString("X" + digits, Invariant);

    public static int StrToInt(string? s)
    {
        if (!TryStrToInt(s, out int value))
        {
            throw new EConvertError($"'{s}' is not a valid integer value");
        }

        return value;
    }

    public static int StrToIntDef(string? s, int defaultValue) => TryStrToInt(s, out int value) ? value : defaultValue;

    public static bool TryStrToInt(string? s, out int value)
    {
        value = 0;
        if (!TryParseDelphiInteger(s, out long big) || big < int.MinValue || big > int.MaxValue)
        {
            return false;
        }

        value = (int)big;
        return true;
    }

    public static long StrToInt64(string? s) =>
        TryParseDelphiInteger(s, out long value) ? value : throw new EConvertError($"'{s}' is not a valid integer value");

    public static long StrToInt64Def(string? s, long defaultValue) => TryParseDelphiInteger(s, out long value) ? value : defaultValue;

    /// <summary>Delphi Val rules: optional sign, decimal or $hex (also 0x), surrounding blanks allowed at the start only.</summary>
    private static bool TryParseDelphiInteger(string? s, out long value)
    {
        value = 0;
        if (s is null)
        {
            return false;
        }

        string t = s.TrimStart(' ');
        if (t.Length == 0)
        {
            return false;
        }

        bool negative = false;
        if (t[0] is '+' or '-')
        {
            negative = t[0] == '-';
            t = t[1..];
        }

        bool ok;
        if (t.StartsWith('$') || t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            string hex = t.StartsWith('$') ? t[1..] : t[2..];
            ok = long.TryParse(hex, NumberStyles.AllowHexSpecifier, Invariant, out value);
        }
        else
        {
            ok = t.Length > 0 && t.All(char.IsAsciiDigit) && long.TryParse(t, NumberStyles.None, Invariant, out value);
        }

        if (ok && negative)
        {
            value = -value;
        }

        return ok;
    }

    public static double StrToFloat(string? s) =>
        TryStrToFloat(s, out double value) ? value : throw new EConvertError($"'{s}' is not a valid floating point value");

    public static double StrToFloatDef(string? s, double defaultValue) => TryStrToFloat(s, out double value) ? value : defaultValue;

    public static bool TryStrToFloat(string? s, out double value) =>
        double.TryParse(Trim(s), NumberStyles.Float, Invariant, out value);

    /// <summary>Delphi FloatToStr: up to 15 significant digits, no trailing zeros.</summary>
    public static string FloatToStr(double value) => FormatGeneral(value, 15);

    public static string BoolToStr(bool value, bool useBoolStrs = false) => useBoolStrs ? (value ? "True" : "False") : (value ? "-1" : "0");

    public static bool StrToBool(string? s) =>
        TryStrToBool(s, out bool value) ? value : throw new EConvertError($"'{s}' is not a valid boolean value");

    public static bool TryStrToBool(string? s, out bool value)
    {
        value = false;
        string t = Trim(s);
        if (SameText(t, "True"))
        {
            value = true;
            return true;
        }

        if (SameText(t, "False"))
        {
            return true;
        }

        if (TryStrToFloat(t, out double number))
        {
            value = number != 0;
            return true;
        }

        return false;
    }

    // ---- Files ----

    public static string ExtractFileName(string? fileName)
    {
        fileName ??= "";
        int i = LastDelimiter("\\/:", fileName);
        return fileName[i..];
    }

    public static string ExtractFilePath(string? fileName)
    {
        fileName ??= "";
        int i = LastDelimiter("\\/:", fileName);
        return fileName[..i];
    }

    public static string ExtractFileExt(string? fileName)
    {
        fileName ??= "";
        int i = LastDelimiter(".\\/:", fileName);
        return i > 0 && fileName[i - 1] == '.' ? fileName[(i - 1)..] : "";
    }

    public static string ChangeFileExt(string? fileName, string extension)
    {
        fileName ??= "";
        int i = LastDelimiter(".\\/:", fileName);
        return i == 0 || fileName[i - 1] != '.' ? fileName + extension : fileName[..(i - 1)] + extension;
    }

    public static string IncludeTrailingPathDelimiter(string? s)
    {
        s ??= "";
        return s.EndsWith(Path.DirectorySeparatorChar) || s.EndsWith('/') || s.EndsWith('\\') ? s : s + Path.DirectorySeparatorChar;
    }

    public static string ExcludeTrailingPathDelimiter(string? s)
    {
        s ??= "";
        return s.Length > 0 && (s[^1] == '/' || s[^1] == '\\') ? s[..^1] : s;
    }

    // ---- Format ----

    /// <summary>
    /// Delphi Format: "%" [index ":"] ["-"] [width] ["." precision] type, with types d u e f g n m p s x and "%%".
    /// Width and precision may be "*", taken from the arguments.
    /// </summary>
    public static string Format(string format, params object?[] args)
    {
        var result = new StringBuilder();
        int argIndex = 0;
        int i = 0;
        while (i < format.Length)
        {
            char c = format[i];
            if (c != '%')
            {
                result.Append(c);
                i++;
                continue;
            }

            i++;
            if (i < format.Length && format[i] == '%')
            {
                result.Append('%');
                i++;
                continue;
            }

            int ReadNumber()
            {
                if (i < format.Length && format[i] == '*')
                {
                    i++;
                    return Convert.ToInt32(Arg(args, argIndex++), Invariant);
                }

                int start = i;
                while (i < format.Length && char.IsAsciiDigit(format[i]))
                {
                    i++;
                }

                return i > start ? int.Parse(format.AsSpan(start, i - start), Invariant) : -1;
            }

            int number = ReadNumber();
            if (i < format.Length && format[i] == ':')
            {
                i++;
                argIndex = number < 0 ? 0 : number;
                number = -1;
            }

            bool left = false;
            if (i < format.Length && format[i] == '-')
            {
                left = true;
                i++;
            }

            int width = number >= 0 ? number : ReadNumber();
            int precision = -1;
            if (i < format.Length && format[i] == '.')
            {
                i++;
                precision = ReadNumber();
                if (precision < 0)
                {
                    precision = 0;
                }
            }

            if (i >= format.Length)
            {
                throw new EConvertError($"Format '{format}' invalid or incompatible with argument");
            }

            char type = char.ToLowerInvariant(format[i]);
            i++;
            object? arg = Arg(args, argIndex++);
            string text = type switch
            {
                'd' => FormatInteger(arg, precision),
                'u' => FormatInteger(arg is IConvertible ? unchecked((ulong)Convert.ToInt64(arg, Invariant)) : arg, precision),
                'x' => FormatHex(arg, precision),
                's' => FormatString(arg, precision),
                'f' => Convert.ToDouble(arg, Invariant).ToString("F" + (precision < 0 ? 2 : precision), Invariant),
                'n' => Convert.ToDouble(arg, Invariant).ToString("N" + (precision < 0 ? 2 : precision), Invariant),
                'm' => Convert.ToDouble(arg, Invariant).ToString("N" + (precision < 0 ? 2 : precision), Invariant),
                'g' => FormatGeneral(Convert.ToDouble(arg, Invariant), precision < 0 ? 15 : precision),
                'e' => FormatScientific(Convert.ToDouble(arg, Invariant), precision < 0 ? 15 : precision),
                'p' => (arg?.GetHashCode() ?? 0).ToString("X8", Invariant),
                _ => throw new EConvertError($"Format '{format}' invalid or incompatible with argument"),
            };
            if (width > text.Length)
            {
                text = left ? text.PadRight(width) : text.PadLeft(width);
            }

            result.Append(text);
        }

        return result.ToString();
    }

    private static object? Arg(object?[] args, int index) =>
        index < args.Length ? args[index] : throw new EConvertError("Format: not enough arguments");

    private static string FormatString(object? arg, int precision)
    {
        string s = arg switch
        {
            null => "",
            string str => str,
            char ch => ch.ToString(),
            bool b => b ? "True" : "False",
            IFormattable f => f.ToString(null, Invariant),
            _ => arg.ToString() ?? "",
        };
        return precision >= 0 && precision < s.Length ? s[..precision] : s;
    }

    private static string FormatInteger(object? arg, int precision)
    {
        string s = Convert.ToString(arg is Enum e ? Convert.ToInt64(e, Invariant) : arg, Invariant) ?? "0";
        bool negative = s.StartsWith('-');
        string digits = negative ? s[1..] : s;
        if (precision > digits.Length)
        {
            digits = digits.PadLeft(precision, '0');
        }

        return negative ? "-" + digits : digits;
    }

    private static string FormatHex(object? arg, int precision)
    {
        long value = Convert.ToInt64(arg, Invariant);
        string s = (arg is int or uint or short or ushort or byte or sbyte ? ((uint)value).ToString("X", Invariant) : value.ToString("X", Invariant));
        return precision > s.Length ? s.PadLeft(precision, '0') : s;
    }

    /// <summary>Delphi general format: shortest of fixed and scientific with the given significant digits.</summary>
    public static string FormatGeneral(double value, int precision)
    {
        if (double.IsNaN(value))
        {
            return "NAN";
        }

        if (double.IsInfinity(value))
        {
            return value > 0 ? "INF" : "-INF";
        }

        if (value == 0)
        {
            return "0";
        }

        int exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
        if (exponent >= precision || exponent < -5)
        {
            return FormatScientificTrimmed(value, precision);
        }

        string s = Math.Round(value, Math.Max(0, Math.Min(15, precision - 1 - exponent))).ToString("0.###############", Invariant);
        return s;
    }

    private static string FormatScientificTrimmed(double value, int precision)
    {
        string mantissaFormat = "0." + new string('#', Math.Max(0, precision - 1));
        string s = value.ToString(mantissaFormat + "E+0", Invariant);
        return s;
    }

    /// <summary>Delphi %e: d.dddE+ddd with <paramref name="precision"/> significant digits.</summary>
    public static string FormatScientific(double value, int precision)
    {
        string s = value.ToString(new string('0', 1) + "." + new string('0', Math.Max(0, precision - 1)) + "E+000", Invariant);
        return s;
    }
}
