using System.Text;

namespace Bold.PortBrowser;

public enum CodeLanguage
{
    Delphi,
    CSharp,
    Plain,
}

public enum CodeColor
{
    Text,
    Keyword,
    Comment,
    String,
    Number,
    Directive,
    LineNumber,
}

/// <summary>A coloured stretch of a text.</summary>
public readonly record struct CodeSpan(int Start, int Length, CodeColor Color);

/// <summary>
/// Colours Delphi and C# code for the browser's code panes: keywords, comments, strings, numbers and compiler
/// directives. It reads characters only, so it colours any text, a file that does not compile included.
/// </summary>
public static class CodeHighlighter
{
    /// <summary>The width of the line numbers <see cref="ToRtf"/> writes before each line, the two spaces after them included.</summary>
    public const int LineNumberWidth = 7;

    private static readonly HashSet<string> DelphiKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "array", "as", "asm", "begin", "case", "class", "const", "constructor", "destructor", "dispinterface", "div", "do", "downto",
        "else", "end", "except", "exports", "file", "finalization", "finally", "for", "function", "goto", "if", "implementation", "in",
        "inherited", "initialization", "inline", "interface", "is", "label", "library", "mod", "nil", "not", "object", "of", "or", "out",
        "packed", "procedure", "program", "property", "raise", "record", "repeat", "resourcestring", "set", "shl", "shr", "string", "then",
        "threadvar", "to", "try", "type", "unit", "until", "uses", "var", "while", "with", "xor", "private", "protected", "public",
        "published", "virtual", "override", "abstract", "overload", "reintroduce", "dynamic", "default", "read", "write", "stored",
        "forward", "external", "stdcall", "cdecl", "safecall", "message", "on", "true", "false", "self", "result", "automated",
    };

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal",
        "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
        "object", "operator", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte",
        "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "var", "partial", "get", "set",
        "init", "required", "record", "where", "when", "yield", "async", "await", "global", "not", "and", "or", "with", "nameof", "file",
    };

    /// <summary>The language of a file, by its extension.</summary>
    public static CodeLanguage LanguageOf(string? file)
    {
        string extension = System.IO.Path.GetExtension(file ?? "");
        return extension.ToLowerInvariant() switch
        {
            ".pas" or ".inc" or ".dpr" or ".dpk" => CodeLanguage.Delphi,
            ".cs" => CodeLanguage.CSharp,
            _ => CodeLanguage.Plain,
        };
    }

    public static bool IsIdentifierPart(char c) => char.IsLetterOrDigit(c) || c == '_';

    /// <summary>The coloured stretches of the text, in order; what is between them has the text's own colour.</summary>
    public static List<CodeSpan> Spans(string text, CodeLanguage language)
    {
        var spans = new List<CodeSpan>();
        if (language == CodeLanguage.Plain)
        {
            return spans;
        }

        bool delphi = language == CodeLanguage.Delphi;
        HashSet<string> keywords = delphi ? DelphiKeywords : CSharpKeywords;
        int i = 0, n = text.Length;
        bool lineStart = true;
        while (i < n)
        {
            char c = text[i];
            char next = i + 1 < n ? text[i + 1] : '\0';
            int start = i;
            if (c == '\n')
            {
                lineStart = true;
                i++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            bool wasLineStart = lineStart;
            lineStart = false;
            if (c == '/' && next == '/')
            {
                i = EndOfLine(text, i);
                spans.Add(new CodeSpan(start, i - start, CodeColor.Comment));
            }
            else if (delphi && (c == '{' || (c == '(' && next == '*')))
            {
                bool brace = c == '{';
                int end = brace ? text.IndexOf('}', i + 1) : text.IndexOf("*)", i + 2, StringComparison.Ordinal);
                i = end < 0 ? n : end + (brace ? 1 : 2);
                bool directive = brace ? start + 1 < n && text[start + 1] == '$' : start + 2 < n && text[start + 2] == '$';
                spans.Add(new CodeSpan(start, i - start, directive ? CodeColor.Directive : CodeColor.Comment));
            }
            else if (!delphi && c == '/' && next == '*')
            {
                int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = end < 0 ? n : end + 2;
                spans.Add(new CodeSpan(start, i - start, CodeColor.Comment));
            }
            else if (!delphi && c == '#' && wasLineStart)
            {
                i = EndOfLine(text, i);
                spans.Add(new CodeSpan(start, i - start, CodeColor.Directive));
            }
            else if (delphi && c == '\'')
            {
                // '' inside a string is a quote; a string ends with its line at the latest.
                i++;
                while (i < n && text[i] != '\n')
                {
                    if (text[i] == '\'')
                    {
                        if (i + 1 < n && text[i + 1] == '\'')
                        {
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    i++;
                }

                spans.Add(new CodeSpan(start, i - start, CodeColor.String));
            }
            else if (delphi && c == '#' && (char.IsDigit(next) || next == '$'))
            {
                i++;
                while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '$'))
                {
                    i++;
                }

                spans.Add(new CodeSpan(start, i - start, CodeColor.String));
            }
            else if (!delphi && (c == '"' || ((c == '@' || c == '$') && (next == '"' || ((next == '@' || next == '$') && i + 2 < n && text[i + 2] == '"')))))
            {
                i = EndOfCSharpString(text, i);
                spans.Add(new CodeSpan(start, i - start, CodeColor.String));
            }
            else if (!delphi && c == '\'')
            {
                i++;
                while (i < n && text[i] != '\'' && text[i] != '\n')
                {
                    i += text[i] == '\\' ? 2 : 1;
                }

                i = Math.Min(n, i + 1);
                spans.Add(new CodeSpan(start, i - start, CodeColor.String));
            }
            else if (char.IsDigit(c) || (delphi && c == '$' && Uri.IsHexDigit(next)))
            {
                i++;
                while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '_' || (text[i] == '.' && i + 1 < n && char.IsDigit(text[i + 1]))))
                {
                    i++;
                }

                spans.Add(new CodeSpan(start, i - start, CodeColor.Number));
            }
            else if (char.IsLetter(c) || c == '_')
            {
                while (i < n && IsIdentifierPart(text[i]))
                {
                    i++;
                }

                // "&begin" in Delphi and "@class" in C# are names, not keywords.
                bool escaped = start > 0 && (text[start - 1] == '&' || text[start - 1] == '@');
                if (!escaped && keywords.Contains(text.Substring(start, i - start)))
                {
                    spans.Add(new CodeSpan(start, i - start, CodeColor.Keyword));
                }
            }
            else
            {
                i++;
            }
        }

        return spans;
    }

    private static int EndOfLine(string text, int from)
    {
        int end = text.IndexOf('\n', from);
        return end < 0 ? text.Length : end;
    }

    /// <summary>The end of a C# string that starts at <paramref name="i"/>: plain, verbatim, interpolated or raw.</summary>
    private static int EndOfCSharpString(string text, int i)
    {
        int n = text.Length;
        bool verbatim = false;
        while (i < n && text[i] != '"')
        {
            verbatim |= text[i] == '@';
            i++;
        }

        int quotes = 0;
        while (i + quotes < n && text[i + quotes] == '"')
        {
            quotes++;
        }

        if (quotes >= 3)
        {
            // A raw string ends with as many quotes as it began with.
            int end = text.IndexOf(new string('"', quotes), i + quotes, StringComparison.Ordinal);
            return end < 0 ? n : end + quotes;
        }

        i++;
        while (i < n)
        {
            char c = text[i];
            if (verbatim)
            {
                if (c == '"')
                {
                    if (i + 1 < n && text[i + 1] == '"')
                    {
                        i += 2;
                        continue;
                    }

                    return i + 1;
                }
            }
            else if (c == '\\')
            {
                i += 2;
                continue;
            }
            else if (c == '"')
            {
                return i + 1;
            }
            else if (c == '\n')
            {
                return i;
            }

            i++;
        }

        return n;
    }

    /// <summary>The text's lines, as the code panes count them: split at line feeds, without carriage returns.</summary>
    public static string[] Lines(string text) => text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

    /// <summary>
    /// The text as RTF for a RichTextBox: coloured, in a fixed-width font, each line with its number before it
    /// (<see cref="LineNumberWidth"/> characters). Line n of the text is line n of the box.
    /// </summary>
    public static string ToRtf(string text, CodeLanguage language, string font = "Consolas", int points = 10, bool lineNumbers = true)
    {
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        List<CodeSpan> spans = Spans(text, language);
        var rtf = new StringBuilder(text.Length * 2);
        rtf.Append(@"{\rtf1\ansi\deff0{\fonttbl{\f0\fmodern ").Append(font).Append(";}}");
        // 1 text, 2 keyword, 3 comment, 4 string, 5 number, 6 directive, 7 line number
        rtf.Append(@"{\colortbl;\red0\green0\blue0;\red0\green0\blue200;\red0\green128\blue0;\red163\green21\blue21;\red9\green134\blue88;\red128\green0\blue128;\red140\green140\blue140;}");
        rtf.Append(@"\f0\fs").Append(points * 2).Append(' ');
        int line = 1, span = 0;
        CodeColor current = CodeColor.Text;
        void Color(CodeColor color)
        {
            if (color != current)
            {
                rtf.Append(@"\cf").Append((int)color + 1).Append(' ');
                current = color;
            }
        }

        void Number()
        {
            if (lineNumbers)
            {
                Color(CodeColor.LineNumber);
                rtf.Append(line.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(LineNumberWidth - 2)).Append("  ");
            }
        }

        Number();
        for (int i = 0; i < text.Length; i++)
        {
            while (span < spans.Count && spans[span].Start + spans[span].Length <= i)
            {
                span++;
            }

            char c = text[i];
            if (c == '\n')
            {
                rtf.Append(@"\par").Append('\n');
                line++;
                Number();
                continue;
            }

            Color(span < spans.Count && spans[span].Start <= i ? spans[span].Color : CodeColor.Text);
            switch (c)
            {
                case '\\' or '{' or '}':
                    rtf.Append('\\').Append(c);
                    break;
                case '\t':
                    rtf.Append("    ");
                    break;
                case < (char)0x20:
                    break;
                case < (char)0x80:
                    rtf.Append(c);
                    break;
                default:
                    rtf.Append(@"\u").Append((short)c).Append('?');
                    break;
            }
        }

        rtf.Append('}');
        return rtf.ToString();
    }

    /// <summary>
    /// The name at a position of a line: the identifier around the column, with the name before a dot when there is
    /// one ("TBoldSystem.UpdateDatabase"); null when the column is on no name.
    /// </summary>
    public static string? NameAt(string line, int column, bool withOwner = false)
    {
        if (column < 0 || column > line.Length)
        {
            return null;
        }

        int start = column, end = column;
        while (start > 0 && IsIdentifierPart(line[start - 1]))
        {
            start--;
        }

        while (end < line.Length && IsIdentifierPart(line[end]))
        {
            end++;
        }

        if (end == start || char.IsDigit(line[start]))
        {
            return null;
        }

        if (withOwner && start > 1 && line[start - 1] == '.' && IsIdentifierPart(line[start - 2]))
        {
            int ownerStart = start - 1;
            while (ownerStart > 0 && IsIdentifierPart(line[ownerStart - 1]))
            {
                ownerStart--;
            }

            if (!char.IsDigit(line[ownerStart]))
            {
                start = ownerStart;
            }
        }

        return line[start..end];
    }
}
