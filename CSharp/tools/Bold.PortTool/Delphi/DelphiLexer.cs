using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bold.PortTool.Delphi;

public enum TokenKind
{
    Identifier,
    Number,
    String,
    Symbol,
    End,
}

/// <summary>A Delphi token with the file and line it came from (include files keep their own file name).</summary>
public sealed record Token(TokenKind Kind, string Text, string File, int Line)
{
    public bool Is(string text) =>
        Kind is TokenKind.Identifier or TokenKind.Symbol && string.Equals(Text, text, StringComparison.OrdinalIgnoreCase);

    public bool IsIdentifier => Kind == TokenKind.Identifier;

    public override string ToString() => $"{Text} ({Path.GetFileName(File)}:{Line})";
}

/// <summary>
/// Turns Delphi source into tokens. Comments are dropped, {$I file} includes are expanded, and
/// {$IFDEF}/{$IFNDEF}/{$IFOPT}/{$ELSE}/{$ENDIF}/{$DEFINE}/{$UNDEF} are evaluated against a define set,
/// so only the code a normal Delphi build of Bold compiles produces tokens.
/// </summary>
public sealed class DelphiLexer
{
    /// <summary>Symbols defined for a normal Delphi build of Bold. Everything else (BOLD_BCB, BOLDCOMCLIENT, DEBUG...) is undefined.</summary>
    public static readonly IReadOnlyCollection<string> DefaultDefines =
        ["BOLD_DELPHI", "MSWINDOWS", "WIN32", "DELPHI6_OR_LATER", "BOLD_DELPH6_OR_LATER"];

    private static readonly Regex DirectiveRegex = new(@"^\$([A-Za-z]+)\s*(.*)$", RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private readonly HashSet<string> _defines;
    private readonly List<Token> _tokens = [];
    private readonly List<string> _warnings = [];
    private readonly Stack<(bool ParentActive, bool Active, bool Taken)> _conditions = new();

    private DelphiLexer(IEnumerable<string> defines)
    {
        _defines = new HashSet<string>(defines, StringComparer.OrdinalIgnoreCase);
    }

    private bool Active => _conditions.Count == 0 || _conditions.Peek().Active;

    /// <summary>Reads Delphi source the way Delphi 7 stores it (single-byte Windows-1252 text).</summary>
    public static string ReadSource(string path) => Encoding.Latin1.GetString(File.ReadAllBytes(path));

    public static (List<Token> Tokens, List<string> Warnings) Tokenize(string path, IEnumerable<string>? defines = null)
    {
        var lexer = new DelphiLexer(defines ?? DefaultDefines);
        lexer.Lex(path, ReadSource(path), 0);
        if (lexer._conditions.Count != 0)
        {
            lexer._warnings.Add($"{path}: {lexer._conditions.Count} conditional block(s) not closed");
        }

        lexer._tokens.Add(new Token(TokenKind.End, "", path, int.MaxValue));
        return (lexer._tokens, lexer._warnings);
    }

    /// <summary>Tokenizes a source text directly; used by tests.</summary>
    public static List<Token> TokenizeText(string text, string fileName = "test.pas", IEnumerable<string>? defines = null)
    {
        var lexer = new DelphiLexer(defines ?? DefaultDefines);
        lexer.Lex(fileName, text, 0);
        lexer._tokens.Add(new Token(TokenKind.End, "", fileName, int.MaxValue));
        return lexer._tokens;
    }

    private void Lex(string path, string text, int depth)
    {
        int i = 0, line = 1, n = text.Length;
        while (i < n)
        {
            char c = text[i];
            if (c == '\n')
            {
                line++;
                i++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '{')
            {
                int end = text.IndexOf('}', i + 1);
                if (end < 0)
                {
                    end = n;
                }

                string body = text.Substring(i + 1, end - i - 1);
                if (body.StartsWith('$'))
                {
                    Directive(body, path, line, depth);
                }

                line += Count(body, '\n');
                i = end + 1;
                continue;
            }

            if (c == '(' && i + 1 < n && text[i + 1] == '*')
            {
                int end = text.IndexOf("*)", i + 2, StringComparison.Ordinal);
                if (end < 0)
                {
                    end = n;
                }

                string body = text.Substring(i + 2, end - i - 2);
                if (body.StartsWith('$'))
                {
                    Directive(body, path, line, depth);
                }

                line += Count(body, '\n');
                i = end + 2;
                continue;
            }

            if (c == '/' && i + 1 < n && text[i + 1] == '/')
            {
                while (i < n && text[i] != '\n')
                {
                    i++;
                }

                continue;
            }

            if (c == '\'' || (c == '#' && i + 1 < n && (char.IsDigit(text[i + 1]) || text[i + 1] == '$')))
            {
                int startLine = line;
                var value = new StringBuilder();
                while (i < n && (text[i] == '\'' || (text[i] == '#' && i + 1 < n && (char.IsDigit(text[i + 1]) || text[i + 1] == '$'))))
                {
                    if (text[i] == '\'')
                    {
                        i++;
                        while (i < n)
                        {
                            if (text[i] == '\'')
                            {
                                if (i + 1 < n && text[i + 1] == '\'')
                                {
                                    value.Append('\'');
                                    i += 2;
                                    continue;
                                }

                                i++;
                                break;
                            }

                            if (text[i] == '\n')
                            {
                                line++;
                            }

                            value.Append(text[i]);
                            i++;
                        }
                    }
                    else
                    {
                        i++;
                        int start = i;
                        if (i < n && text[i] == '$')
                        {
                            i++;
                            while (i < n && Uri.IsHexDigit(text[i]))
                            {
                                i++;
                            }

                            value.Append((char)int.Parse(text.AsSpan(start + 1, i - start - 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            while (i < n && char.IsDigit(text[i]))
                            {
                                i++;
                            }

                            value.Append((char)int.Parse(text.AsSpan(start, i - start), CultureInfo.InvariantCulture));
                        }
                    }
                }

                Emit(TokenKind.String, value.ToString(), path, startLine);
                continue;
            }

            if (char.IsLetter(c) || c == '_' || (c == '&' && i + 1 < n && (char.IsLetter(text[i + 1]) || text[i + 1] == '_')))
            {
                if (c == '&')
                {
                    i++;
                }

                int start = i;
                while (i < n && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
                {
                    i++;
                }

                Emit(TokenKind.Identifier, text[start..i], path, line);
                continue;
            }

            if (char.IsDigit(c) || (c == '$' && i + 1 < n && Uri.IsHexDigit(text[i + 1])))
            {
                int start = i;
                i++;
                while (i < n && (char.IsLetterOrDigit(text[i]) ||
                                 (text[i] == '.' && i + 1 < n && char.IsDigit(text[i + 1])) ||
                                 ((text[i] == '+' || text[i] == '-') && (text[i - 1] == 'e' || text[i - 1] == 'E') && c != '$')))
                {
                    i++;
                }

                Emit(TokenKind.Number, text[start..i], path, line);
                continue;
            }

            if (i + 1 < n)
            {
                string pair = text.Substring(i, 2);
                if (pair is ":=" or ".." or "<=" or ">=" or "<>" or "(." or ".)")
                {
                    Emit(TokenKind.Symbol, pair, path, line);
                    i += 2;
                    continue;
                }
            }

            Emit(TokenKind.Symbol, c.ToString(), path, line);
            i++;
        }
    }

    private void Emit(TokenKind kind, string text, string path, int line)
    {
        if (Active)
        {
            _tokens.Add(new Token(kind, text, path, line));
        }
    }

    private void Directive(string body, string path, int line, int depth)
    {
        var match = DirectiveRegex.Match(body);
        if (!match.Success)
        {
            return;
        }

        string name = match.Groups[1].Value.ToUpperInvariant();
        string argument = match.Groups[2].Value.Trim();
        string firstWord = argument.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
        switch (name)
        {
            case "IFDEF":
                Push(_defines.Contains(firstWord));
                break;
            case "IFNDEF":
                Push(!_defines.Contains(firstWord));
                break;
            case "IFOPT":
            case "IF":
                // Bold uses {$IFOPT C+} only around assertion helpers; treat such blocks as not compiled.
                Push(false);
                break;
            case "ELSE":
            case "ELSEIF":
                if (_conditions.Count > 0)
                {
                    var frame = _conditions.Pop();
                    bool take = name == "ELSE" && !frame.Taken;
                    _conditions.Push((frame.ParentActive, frame.ParentActive && take, frame.Taken || take));
                }
                else
                {
                    _warnings.Add($"{path}:{line}: {{${name}}} without {{$IF...}}");
                }

                break;
            case "ENDIF":
            case "IFEND":
                if (_conditions.Count > 0)
                {
                    _conditions.Pop();
                }
                else
                {
                    _warnings.Add($"{path}:{line}: {{${name}}} without {{$IF...}}");
                }

                break;
            case "DEFINE":
                if (Active)
                {
                    _defines.Add(firstWord);
                }

                break;
            case "UNDEF":
                if (Active)
                {
                    _defines.Remove(firstWord);
                }

                break;
            case "I":
            case "INCLUDE":
                if (Active && argument.Length > 0 && argument != "+" && argument != "-")
                {
                    Include(argument.Trim('\'', '"'), path, line, depth);
                }

                break;
        }
    }

    private void Push(bool condition)
    {
        bool parent = Active;
        _conditions.Push((parent, parent && condition, condition));
    }

    private void Include(string fileName, string path, int line, int depth)
    {
        if (depth > 8)
        {
            _warnings.Add($"{path}:{line}: include nesting too deep for {fileName}");
            return;
        }

        string? resolved = ResolveCaseInsensitive(Path.GetDirectoryName(path) ?? ".", fileName);
        if (resolved is null)
        {
            _warnings.Add($"{path}:{line}: include file {fileName} not found");
            return;
        }

        Lex(resolved, ReadSource(resolved), depth + 1);
    }

    /// <summary>
    /// Finds a file the way Windows does, ignoring case, since the Delphi source was written on Windows, and returns it
    /// with the case it has on disk: links built from it must work on GitHub, where paths are case-sensitive, on every OS.
    /// </summary>
    public static string? ResolveCaseInsensitive(string directory, string fileName)
    {
        if (!Directory.Exists(directory))
        {
            return null;
        }

        string[] files = Directory.GetFiles(directory);
        return files.FirstOrDefault(f => string.Equals(Path.GetFileName(f), fileName, StringComparison.Ordinal))
               ?? files.FirstOrDefault(f => string.Equals(Path.GetFileName(f), fileName, StringComparison.OrdinalIgnoreCase));
    }

    private static int Count(string text, char c)
    {
        int count = 0;
        foreach (char x in text)
        {
            if (x == c)
            {
                count++;
            }
        }

        return count;
    }
}
