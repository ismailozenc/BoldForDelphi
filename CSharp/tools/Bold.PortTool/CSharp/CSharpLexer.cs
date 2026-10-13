using System.Globalization;
using System.Text;

namespace Bold.PortTool.CSharp;

public enum CsTokenKind
{
    Identifier,
    Number,
    String,

    /// <summary>An interpolated string ($"..."); its text is only the literal parts, so it is not a constant value.</summary>
    InterpolatedString,
    Char,
    Symbol,
    End,
}

/// <summary>A C# token. String and char tokens hold the decoded value.</summary>
public sealed record CsToken(CsTokenKind Kind, string Text, int Line)
{
    public bool Is(string text) => Kind is CsTokenKind.Identifier or CsTokenKind.Symbol && Text == text;

    public bool IsIdentifier => Kind == CsTokenKind.Identifier;

    public override string ToString() => $"{Text} (line {Line})";
}

/// <summary>
/// A small C# tokenizer: enough to find declarations and attributes in code that follows the port's conventions.
/// Comments and preprocessor lines are dropped; regular, verbatim, interpolated and raw strings are read whole.
/// "&lt;" and "&gt;" are always single tokens, so generic argument lists can be skipped by counting them.
/// </summary>
public static class CSharpLexer
{
    private static readonly string[] Operators =
    [
        "??=", "...", "=>", "::", "??", "?.", "==", "!=", "&&", "||", "++", "--", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "->", "..",
    ];

    public static List<CsToken> Tokenize(string text)
    {
        var tokens = new List<CsToken>();
        int i = 0, line = 1, n = text.Length;
        bool lineStart = true;
        while (i < n)
        {
            char c = text[i];
            if (c == '\n')
            {
                line++;
                i++;
                lineStart = true;
                continue;
            }

            if (char.IsWhiteSpace(c) || c == '﻿')
            {
                i++;
                continue;
            }

            if (c == '#' && lineStart)
            {
                while (i < n && text[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            lineStart = false;
            if (c == '/' && i + 1 < n && text[i + 1] == '/')
            {
                while (i < n && text[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (c == '/' && i + 1 < n && text[i + 1] == '*')
            {
                int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? n : end + 2;
                line += CountNewLines(text, i, end);
                i = end;
                continue;
            }

            if (IsStringStart(text, i))
            {
                int start = i;
                bool interpolated = text[i] == '$' || (text[i] == '@' && text[i + 1] == '$');
                string value = ScanString(text, ref i);
                tokens.Add(new CsToken(interpolated ? CsTokenKind.InterpolatedString : CsTokenKind.String, value, line));
                line += CountNewLines(text, start, i);
                continue;
            }

            if (c == '\'')
            {
                int start = i;
                string value = ScanChar(text, ref i);
                tokens.Add(new CsToken(CsTokenKind.Char, value, line));
                line += CountNewLines(text, start, i);
                continue;
            }

            if (char.IsLetter(c) || c == '_' || (c == '@' && i + 1 < n && (char.IsLetter(text[i + 1]) || text[i + 1] == '_')))
            {
                if (c == '@')
                {
                    i++;
                }

                int start = i;
                while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                tokens.Add(new CsToken(CsTokenKind.Identifier, text[start..i], line));
                continue;
            }

            if (char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(text[i + 1])))
            {
                int start = i;
                bool hex = c == '0' && i + 1 < n && (text[i + 1] == 'x' || text[i + 1] == 'X');
                i++;
                while (i < n)
                {
                    char d = text[i];
                    if (char.IsLetterOrDigit(d) || d == '_')
                    {
                        i++;
                    }
                    else if (d == '.' && i + 1 < n && char.IsDigit(text[i + 1]))
                    {
                        i++;
                    }
                    else if ((d == '+' || d == '-') && !hex && (text[i - 1] == 'e' || text[i - 1] == 'E'))
                    {
                        i++;
                    }
                    else
                    {
                        break;
                    }
                }

                tokens.Add(new CsToken(CsTokenKind.Number, text[start..i], line));
                continue;
            }

            string? op = null;
            foreach (string candidate in Operators)
            {
                if (string.CompareOrdinal(text, i, candidate, 0, candidate.Length) == 0)
                {
                    op = candidate;
                    break;
                }
            }

            op ??= c.ToString();
            tokens.Add(new CsToken(CsTokenKind.Symbol, op, line));
            i += op.Length;
        }

        tokens.Add(new CsToken(CsTokenKind.End, "", line));
        return tokens;
    }

    private static bool IsStringStart(string text, int i)
    {
        int n = text.Length;
        int j = i;
        while (j < n && (text[j] == '$' || text[j] == '@'))
        {
            j++;
        }

        if (j >= n || text[j] != '"')
        {
            return false;
        }

        // "@" alone before an identifier is a verbatim identifier, handled elsewhere; here a quote follows.
        return true;
    }

    /// <summary>Reads any string literal starting at <paramref name="i"/> and returns its value (holes of interpolated strings are skipped).</summary>
    private static string ScanString(string text, ref int i)
    {
        int n = text.Length;
        int dollars = 0;
        bool verbatim = false;
        while (text[i] == '$' || text[i] == '@')
        {
            if (text[i] == '$')
            {
                dollars++;
            }
            else
            {
                verbatim = true;
            }

            i++;
        }

        var value = new StringBuilder();
        if (string.CompareOrdinal(text, i, "\"\"\"", 0, 3) == 0)
        {
            int quotes = 0;
            while (i < n && text[i] == '"')
            {
                quotes++;
                i++;
            }

            int contentStart = i;
            while (i < n)
            {
                if (text[i] == '"')
                {
                    int run = 0;
                    int j = i;
                    while (j < n && text[j] == '"')
                    {
                        run++;
                        j++;
                    }

                    if (run >= quotes)
                    {
                        value.Append(text, contentStart, i - contentStart);
                        i = j;
                        break;
                    }

                    i = j;
                    continue;
                }

                i++;
            }
        }
        else
        {
            i++; // opening quote
            while (i < n)
            {
                char c = text[i];
                if (c == '"')
                {
                    if (verbatim && i + 1 < n && text[i + 1] == '"')
                    {
                        value.Append('"');
                        i += 2;
                        continue;
                    }

                    i++;
                    break;
                }

                if (dollars > 0 && c == '{')
                {
                    if (i + 1 < n && text[i + 1] == '{')
                    {
                        value.Append('{');
                        i += 2;
                        continue;
                    }

                    SkipHole(text, ref i);
                    continue;
                }

                if (!verbatim && c == '\\' && i + 1 < n)
                {
                    value.Append(Escape(text, ref i));
                    continue;
                }

                if (!verbatim && c == '\n')
                {
                    break; // unterminated
                }

                value.Append(c);
                i++;
            }
        }

        if (i + 1 < n && text[i] == 'u' && text[i + 1] == '8')
        {
            i += 2;
        }

        return value.ToString();
    }

    /// <summary>Skips an interpolation hole "{...}", including strings nested in it.</summary>
    private static void SkipHole(string text, ref int i)
    {
        int n = text.Length;
        int depth = 0;
        while (i < n)
        {
            if (IsStringStart(text, i))
            {
                ScanString(text, ref i);
                continue;
            }

            if (text[i] == '\'')
            {
                ScanChar(text, ref i);
                continue;
            }

            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    i++;
                    return;
                }
            }

            i++;
        }
    }

    private static string ScanChar(string text, ref int i)
    {
        int n = text.Length;
        i++; // opening quote
        string value = "";
        if (i < n && text[i] == '\\')
        {
            value = Escape(text, ref i);
        }
        else if (i < n)
        {
            value = text[i].ToString();
            i++;
        }

        while (i < n && text[i] != '\'' && text[i] != '\n')
        {
            i++;
        }

        if (i < n && text[i] == '\'')
        {
            i++;
        }

        return value;
    }

    private static string Escape(string text, ref int i)
    {
        int n = text.Length;
        char e = i + 1 < n ? text[i + 1] : '\\';
        i += 2;
        switch (e)
        {
            case 'n': return "\n";
            case 'r': return "\r";
            case 't': return "\t";
            case '0': return "\0";
            case 'a': return "\a";
            case 'b': return "\b";
            case 'f': return "\f";
            case 'v': return "\v";
            case 'e': return "\u001B";
            case 'u':
            case 'x':
            case 'U':
            {
                int max = e == 'U' ? 8 : 4;
                int start = i;
                while (i < n && i - start < max && Uri.IsHexDigit(text[i]))
                {
                    i++;
                }

                if (i == start)
                {
                    return e.ToString();
                }

                int code = int.Parse(text.AsSpan(start, i - start), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return char.ConvertFromUtf32(code);
            }

            default:
                return e.ToString();
        }
    }

    private static int CountNewLines(string text, int start, int end)
    {
        int count = 0;
        for (int k = start; k < end && k < text.Length; k++)
        {
            if (text[k] == '\n')
            {
                count++;
            }
        }

        return count;
    }
}
