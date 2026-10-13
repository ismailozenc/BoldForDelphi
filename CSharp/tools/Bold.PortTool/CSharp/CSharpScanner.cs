namespace Bold.PortTool.CSharp;

/// <summary>
/// Finds the declarations of a C# file and the attributes on them. Bodies, initializers and expressions are skipped,
/// so only types and their members are seen. It relies on ordinary formatting and is lenient: what it cannot read
/// is skipped and reported as a warning.
/// </summary>
public sealed class CSharpScanner
{
    private static readonly HashSet<string> ModifierWords =
    [
        "public", "private", "protected", "internal", "static", "readonly", "const", "volatile", "virtual", "override",
        "abstract", "sealed", "new", "partial", "extern", "unsafe", "async", "required", "file", "fixed",
    ];

    private readonly List<CsToken> _tokens;
    private readonly CsFile _file;
    private readonly List<string> _types = [];
    private int _position;
    private string _namespace = "";

    private CSharpScanner(List<CsToken> tokens, CsFile file)
    {
        _tokens = tokens;
        _file = file;
    }

    public static CsFile ScanFile(string path, string relativePath) => Scan(File.ReadAllText(path), relativePath);

    public static CsFile Scan(string text, string relativePath)
    {
        var file = new CsFile { Path = relativePath };
        var scanner = new CSharpScanner(CSharpLexer.Tokenize(text), file);
        scanner.ParseBlock(top: true, typeName: null);
        return file;
    }

    private CsToken Peek(int offset = 0) => _position + offset < _tokens.Count ? _tokens[_position + offset] : _tokens[^1];

    private CsToken Next() => _position < _tokens.Count - 1 ? _tokens[_position++] : _tokens[^1];

    private bool AtEnd => Peek().Kind == CsTokenKind.End;

    private bool Accept(string text)
    {
        if (Peek().Is(text))
        {
            _position++;
            return true;
        }

        return false;
    }

    private string? Owner => _types.Count == 0 ? null : string.Join(".", _types);

    private void Warn(string message) => _file.Warnings.Add($"{_file.Path}:{Peek().Line}: {message} (at '{Peek().Text}')");

    /// <summary>Reads declarations up to the "}" that closes this block (or the end of the file at the top).</summary>
    private void ParseBlock(bool top, string? typeName)
    {
        var pending = new List<CsAttribute>();
        while (!AtEnd)
        {
            var token = Peek();
            if (token.Is("}"))
            {
                Next();
                if (!top)
                {
                    return;
                }

                continue;
            }

            if (token.Is(";"))
            {
                Next();
                continue;
            }

            if (token.Is("["))
            {
                var (target, attributes) = ParseAttributeSection();
                if (target is "assembly" or "module")
                {
                    _file.AssemblyAttributes.AddRange(attributes);
                }
                else
                {
                    pending.AddRange(attributes);
                }

                continue;
            }

            if (typeName is null)
            {
                if (token.Is("using") || (token.Is("global") && Peek(1).Is("using")) || (token.Is("extern") && Peek(1).Is("alias")))
                {
                    int usingLine = token.Line;
                    bool global = Accept("global");
                    if (Accept("using") && Peek().IsIdentifier && Peek(1).Is("="))
                    {
                        Add(CsDeclarationKind.Alias, Next().Text, usingLine, global ? ["global"] : [], []);
                    }

                    SkipPastSemicolon();
                    pending.Clear();
                    continue;
                }

                if (token.Is("namespace"))
                {
                    Next();
                    string name = ReadQualifiedName();
                    if (Accept("{"))
                    {
                        string saved = _namespace;
                        _namespace = saved.Length == 0 ? name : $"{saved}.{name}";
                        ParseBlock(top: false, typeName: null);
                        _namespace = saved;
                    }
                    else
                    {
                        Accept(";");
                        _namespace = name;
                    }

                    pending.Clear();
                    continue;
                }
            }

            int line = token.Line;
            var modifiers = ReadModifiers();
            var keyword = Peek();
            if (keyword.Is("class") || keyword.Is("struct") || keyword.Is("interface") || keyword.Is("enum") || keyword.Is("record"))
            {
                ParseType(modifiers, pending, line);
            }
            else if (keyword.Is("delegate") && !Peek(1).Is("*"))
            {
                ParseDelegate(modifiers, pending, line);
            }
            else if (typeName is null)
            {
                Warn("unexpected code outside a type");
                SkipStatement();
            }
            else if (keyword.Is("event"))
            {
                ParseEvent(modifiers, pending, line);
            }
            else
            {
                ParseMember(modifiers, pending, line, typeName);
            }

            pending = [];
        }
    }

    private HashSet<string> ReadModifiers()
    {
        var modifiers = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            var token = Peek();
            if (token.IsIdentifier && ModifierWords.Contains(token.Text) && !Peek(1).Is("(") && !Peek(1).Is("=") && !Peek(1).Is(";"))
            {
                modifiers.Add(Next().Text);
            }
            else if (token.Is("ref") && (Peek(1).Is("struct") || Peek(1).Is("partial") || Peek(1).Is("readonly")))
            {
                modifiers.Add(Next().Text);
            }
            else
            {
                return modifiers;
            }
        }
    }

    private CsDeclaration Add(CsDeclarationKind kind, string name, int line, HashSet<string> modifiers, List<CsAttribute> attributes, int parameters = 0)
    {
        var declaration = new CsDeclaration
        {
            Kind = kind,
            Name = name,
            Owner = Owner,
            Namespace = _namespace,
            ParameterCount = parameters,
            File = _file.Path,
            Line = line,
            Attributes = attributes,
            Modifiers = modifiers,
        };
        _file.Declarations.Add(declaration);
        return declaration;
    }

    private void ParseType(HashSet<string> modifiers, List<CsAttribute> attributes, int line)
    {
        var keyword = Next();
        var kind = keyword.Text switch
        {
            "class" => CsDeclarationKind.Class,
            "struct" => CsDeclarationKind.Struct,
            "interface" => CsDeclarationKind.Interface,
            "enum" => CsDeclarationKind.Enum,
            _ => CsDeclarationKind.Record,
        };
        if (kind == CsDeclarationKind.Record && (Peek().Is("class") || Peek().Is("struct")))
        {
            Next();
        }

        string name = Next().Text;
        if (Peek().Is("<"))
        {
            SkipAngles();
        }

        if (Peek().Is("("))
        {
            SkipBalanced("(", ")"); // primary constructor
        }

        // Base list and constraints, up to the body or the closing ";".
        int depth = 0;
        while (!AtEnd && !(depth == 0 && (Peek().Is("{") || Peek().Is(";"))))
        {
            var token = Next();
            if (token.Is("(") || token.Is("["))
            {
                depth++;
            }
            else if (token.Is(")") || token.Is("]"))
            {
                depth--;
            }
        }

        Add(kind, name, line, modifiers, attributes);
        if (Accept(";"))
        {
            return;
        }

        if (kind == CsDeclarationKind.Enum)
        {
            SkipBalanced("{", "}");
            return;
        }

        if (Accept("{"))
        {
            _types.Add(name);
            ParseBlock(top: false, typeName: name);
            _types.RemoveAt(_types.Count - 1);
        }
    }

    private void ParseDelegate(HashSet<string> modifiers, List<CsAttribute> attributes, int line)
    {
        Next(); // delegate
        SkipType();
        string name = Next().Text;
        if (Peek().Is("<"))
        {
            SkipAngles();
        }

        int parameters = Peek().Is("(") ? CountParameters("(", ")") : 0;
        SkipPastSemicolon();
        Add(CsDeclarationKind.Delegate, name, line, modifiers, attributes, parameters);
    }

    private void ParseEvent(HashSet<string> modifiers, List<CsAttribute> attributes, int line)
    {
        Next(); // event
        SkipType();
        while (!AtEnd)
        {
            string name = ReadMemberName();
            Add(CsDeclarationKind.Event, name, line, modifiers, attributes);
            if (Peek().Is("{"))
            {
                SkipBalanced("{", "}");
                return;
            }

            if (Accept("="))
            {
                SkipInitializer();
            }

            if (!Accept(","))
            {
                Accept(";");
                return;
            }
        }
    }

    private void ParseMember(HashSet<string> modifiers, List<CsAttribute> attributes, int line, string typeName)
    {
        var token = Peek();
        if (token.Is("~"))
        {
            Next();
            string name = Next().Text;
            SkipBalanced("(", ")");
            SkipBody();
            Add(CsDeclarationKind.Finalizer, "~" + name, line, modifiers, attributes);
            return;
        }

        if (token.IsIdentifier && token.Text == typeName && Peek(1).Is("("))
        {
            Next();
            int parameters = CountParameters("(", ")");
            if (Accept(":"))
            {
                Next(); // base or this
                if (Peek().Is("("))
                {
                    SkipBalanced("(", ")");
                }
            }

            SkipBody();
            var kind = modifiers.Contains("static") ? CsDeclarationKind.StaticConstructor : CsDeclarationKind.Constructor;
            Add(kind, typeName, line, modifiers, attributes, parameters);
            return;
        }

        if ((token.Is("implicit") || token.Is("explicit")) && Peek(1).Is("operator"))
        {
            string conversion = Next().Text;
            Next(); // operator
            SkipType();
            int parameters = CountParameters("(", ")");
            SkipBody();
            Add(CsDeclarationKind.Operator, conversion == "implicit" ? "op_Implicit" : "op_Explicit", line, modifiers, attributes, parameters);
            return;
        }

        if (!SkipType())
        {
            Warn("could not read a member declaration");
            SkipStatement();
            return;
        }

        if (Accept("operator"))
        {
            string symbol = "";
            while (!AtEnd && !Peek().Is("("))
            {
                symbol += Next().Text;
            }

            int parameters = CountParameters("(", ")");
            SkipBody();
            Add(CsDeclarationKind.Operator, "operator" + symbol, line, modifiers, attributes, parameters);
            return;
        }

        if (Peek().Is("this") && Peek(1).Is("["))
        {
            Next();
            int parameters = CountParameters("[", "]");
            SkipAccessorBody();
            Add(CsDeclarationKind.Indexer, "this", line, modifiers, attributes, parameters);
            return;
        }

        if (!Peek().IsIdentifier)
        {
            Warn("expected a member name");
            SkipStatement();
            return;
        }

        string memberName = ReadMemberName();
        if (memberName.EndsWith(".this", StringComparison.Ordinal) && Peek().Is("["))
        {
            int parameters = CountParameters("[", "]");
            SkipAccessorBody();
            Add(CsDeclarationKind.Indexer, "this", line, modifiers, attributes, parameters);
            return;
        }

        string shortName = memberName[(memberName.LastIndexOf('.') + 1)..];
        if (Peek().Is("("))
        {
            int parameters = CountParameters("(", ")");
            while (Peek().Is("where"))
            {
                while (!AtEnd && !Peek().Is("{") && !Peek().Is("=>") && !Peek().Is(";"))
                {
                    Next();
                }
            }

            SkipBody();
            Add(CsDeclarationKind.Method, shortName, line, modifiers, attributes, parameters);
            return;
        }

        if (Peek().Is("{"))
        {
            SkipBalanced("{", "}");
            if (Accept("="))
            {
                SkipPastSemicolon();
            }

            Add(CsDeclarationKind.Property, shortName, line, modifiers, attributes);
            return;
        }

        if (Peek().Is("=>"))
        {
            SkipPastSemicolon();
            Add(CsDeclarationKind.Property, shortName, line, modifiers, attributes);
            return;
        }

        var fieldKind = modifiers.Contains("const") ? CsDeclarationKind.Const : CsDeclarationKind.Field;
        Add(fieldKind, shortName, line, modifiers, attributes);
        while (!AtEnd)
        {
            if (Accept("="))
            {
                SkipInitializer();
            }

            if (Peek().Is(",") && Peek(1).IsIdentifier)
            {
                Next();
                var next = Next();
                Add(fieldKind, next.Text, next.Line, modifiers, attributes);
                continue;
            }

            if (!Accept(";"))
            {
                Warn("expected ';' after a field");
                SkipStatement();
            }

            return;
        }
    }

    /// <summary>Reads "Name", "IFoo.Name" or "Name&lt;T&gt;"; generic arguments are dropped.</summary>
    private string ReadMemberName()
    {
        string name = Next().Text;
        while (true)
        {
            if (Peek().Is("<"))
            {
                SkipAngles();
                continue;
            }

            if (Peek().Is(".") && Peek(1).IsIdentifier)
            {
                Next();
                name += "." + Next().Text;
                continue;
            }

            return name;
        }
    }

    private string ReadQualifiedName()
    {
        string name = Next().Text;
        while ((Peek().Is(".") || Peek().Is("::")) && Peek(1).IsIdentifier)
        {
            Next();
            name += "." + Next().Text;
        }

        return name;
    }

    /// <summary>Skips a type: names with generic arguments, tuples, arrays, nullable and pointer suffixes.</summary>
    private bool SkipType()
    {
        Accept("ref");
        Accept("readonly");
        Accept("scoped");
        if (Peek().Is("("))
        {
            SkipBalanced("(", ")");
        }
        else if (Peek().IsIdentifier && !Peek().Is("this") && !Peek().Is("operator"))
        {
            Next();
            if (Peek().Is("::"))
            {
                Next();
                Next();
            }

            if (Peek().Is("<"))
            {
                SkipAngles();
            }

            // A member always has a type before its name (constructors are handled before this), so a dotted
            // chain here is all type: "A.B Name". An explicit interface member "void IFoo.Bar()" has "void" first.
            while (Peek().Is(".") && Peek(1).IsIdentifier && !Peek(1).Is("this"))
            {
                Next();
                Next();
                if (Peek().Is("<"))
                {
                    SkipAngles();
                }
            }
        }
        else
        {
            return false;
        }

        while (true)
        {
            if (Peek().Is("?") || Peek().Is("*"))
            {
                Next();
            }
            else if (Peek().Is("[") && (Peek(1).Is("]") || Peek(1).Is(",")))
            {
                SkipBalanced("[", "]");
            }
            else
            {
                return true;
            }
        }
    }

    private void SkipAngles()
    {
        int depth = 0;
        while (!AtEnd)
        {
            var token = Next();
            if (token.Is("<"))
            {
                depth++;
            }
            else if (token.Is(">"))
            {
                depth--;
                if (depth == 0)
                {
                    return;
                }
            }
            else if (token.Is(";") || token.Is("{"))
            {
                _position--;
                return;
            }
        }
    }

    private void SkipBalanced(string open, string close)
    {
        int depth = 0;
        while (!AtEnd)
        {
            var token = Next();
            if (token.Is(open))
            {
                depth++;
            }
            else if (token.Is(close))
            {
                depth--;
                if (depth <= 0)
                {
                    return;
                }
            }
        }
    }

    /// <summary>Counts the parameters of "(...)" or "[...]" by top-level commas.</summary>
    private int CountParameters(string open, string close)
    {
        if (!Accept(open))
        {
            return 0;
        }

        if (Accept(close))
        {
            return 0;
        }

        int count = 1, depth = 0;
        while (!AtEnd)
        {
            var token = Next();
            if (token.Is("(") || token.Is("[") || token.Is("{") || token.Is("<"))
            {
                depth++;
            }
            else if (depth == 0 && token.Is(close))
            {
                return count;
            }
            else if (token.Is(")") || token.Is("]") || token.Is("}") || token.Is(">"))
            {
                depth--;
            }
            else if (depth == 0 && token.Is(","))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>A method body, an expression body, or ";".</summary>
    private void SkipBody()
    {
        if (Peek().Is("{"))
        {
            SkipBalanced("{", "}");
        }
        else if (Peek().Is("=>"))
        {
            SkipPastSemicolon();
        }
        else
        {
            Accept(";");
        }
    }

    private void SkipAccessorBody()
    {
        if (Peek().Is("{"))
        {
            SkipBalanced("{", "}");
        }
        else if (Peek().Is("=>"))
        {
            SkipPastSemicolon();
        }
    }

    /// <summary>Skips to the ";" that ends the statement, minding brackets, and consumes it.</summary>
    private void SkipPastSemicolon()
    {
        int depth = 0;
        while (!AtEnd)
        {
            var token = Next();
            if (token.Is("(") || token.Is("[") || token.Is("{"))
            {
                depth++;
            }
            else if (token.Is(")") || token.Is("]") || token.Is("}"))
            {
                depth--;
            }
            else if (depth <= 0 && token.Is(";"))
            {
                return;
            }
        }
    }

    /// <summary>Skips a field initializer up to ";" or the "," that starts the next declarator.</summary>
    private void SkipInitializer()
    {
        int depth = 0;
        while (!AtEnd)
        {
            var token = Peek();
            if (depth <= 0 && token.Is(";"))
            {
                return;
            }

            if (depth <= 0 && token.Is(",") && Peek(1).IsIdentifier && (Peek(2).Is("=") || Peek(2).Is(",") || Peek(2).Is(";")))
            {
                return;
            }

            Next();
            if (token.Is("(") || token.Is("[") || token.Is("{"))
            {
                depth++;
            }
            else if (token.Is(")") || token.Is("]") || token.Is("}"))
            {
                depth--;
            }
        }
    }

    /// <summary>Skips something that is not a declaration: up to ";" or past a "{...}" block.</summary>
    private void SkipStatement()
    {
        int depth = 0;
        while (!AtEnd)
        {
            var token = Next();
            if (token.Is("(") || token.Is("["))
            {
                depth++;
            }
            else if (token.Is(")") || token.Is("]"))
            {
                depth--;
            }
            else if (depth <= 0 && token.Is(";"))
            {
                return;
            }
            else if (depth <= 0 && token.Is("{"))
            {
                _position--;
                SkipBalanced("{", "}");
                return;
            }
            else if (depth <= 0 && token.Is("}"))
            {
                _position--;
                return;
            }
        }
    }

    private (string? Target, List<CsAttribute> Attributes) ParseAttributeSection()
    {
        Next(); // [
        string? target = null;
        if (Peek().IsIdentifier && Peek(1).Is(":"))
        {
            target = Next().Text;
            Next();
        }

        var attributes = new List<CsAttribute>();
        while (!AtEnd && !Peek().Is("]"))
        {
            int line = Peek().Line;
            string name = ReadQualifiedName();
            if (Peek().Is("<"))
            {
                SkipAngles();
            }

            var arguments = Peek().Is("(") ? ParseArguments() : [];
            string simple = name[(name.LastIndexOf('.') + 1)..];
            if (simple.EndsWith("Attribute", StringComparison.Ordinal) && simple.Length > "Attribute".Length)
            {
                simple = simple[..^"Attribute".Length];
            }

            attributes.Add(new CsAttribute { Name = simple, Target = target, Arguments = arguments, File = _file.Path, Line = line });
            if (!Accept(","))
            {
                break;
            }
        }

        if (!Accept("]"))
        {
            Warn("expected ']' after an attribute");
            SkipBalanced("[", "]");
        }

        return (target, attributes);
    }

    private List<CsArgument> ParseArguments()
    {
        Next(); // (
        var arguments = new List<CsArgument>();
        while (!AtEnd && !Peek().Is(")"))
        {
            string? name = null;
            if (Peek().IsIdentifier && (Peek(1).Is("=") || Peek(1).Is(":")))
            {
                name = Next().Text;
                Next();
            }

            var tokens = new List<CsToken>();
            int depth = 0;
            while (!AtEnd)
            {
                var token = Peek();
                if (depth == 0 && (token.Is(",") || token.Is(")")))
                {
                    break;
                }

                if (token.Is("(") || token.Is("[") || token.Is("{"))
                {
                    depth++;
                }
                else if (token.Is(")") || token.Is("]") || token.Is("}"))
                {
                    depth--;
                }

                tokens.Add(Next());
            }

            arguments.Add(new CsArgument { Name = name, Tokens = tokens });
            Accept(",");
        }

        Accept(")");
        return arguments;
    }
}
