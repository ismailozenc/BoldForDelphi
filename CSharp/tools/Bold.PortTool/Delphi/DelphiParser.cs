namespace Bold.PortTool.Delphi;

/// <summary>
/// Reads the declarations of a Delphi unit: types, methods, properties, routines, constants, variables and the
/// initialization and finalization sections. Method and routine bodies are skipped, but their lines are recorded.
/// It is a declaration reader, not a compiler: it is lenient and reports what it could not read as warnings.
/// </summary>
public sealed class DelphiParser
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "begin", "end", "const", "var", "threadvar", "type", "procedure", "function", "constructor", "destructor",
        "implementation", "initialization", "finalization", "resourcestring", "property", "class", "private",
        "protected", "public", "published", "automated", "strict", "uses", "label", "exports", "operator", "case",
    };

    private static readonly HashSet<string> RoutineDirectives = new(StringComparer.OrdinalIgnoreCase)
    {
        "virtual", "dynamic", "abstract", "override", "overload", "reintroduce", "static", "stdcall", "cdecl",
        "safecall", "register", "pascal", "message", "deprecated", "platform", "inline", "final", "assembler",
        "export", "far", "near", "dispid", "varargs", "experimental", "forward", "external", "library",
    };

    private static readonly HashSet<string> HintDirectives = new(StringComparer.OrdinalIgnoreCase)
    {
        "deprecated", "platform", "library", "experimental",
    };

    private readonly List<Token> _tokens;
    private readonly DelphiUnit _unit;
    private readonly Func<string, string> _relativePath;
    private int _position;
    private List<string> _parameterNames = [];

    private DelphiParser(List<Token> tokens, DelphiUnit unit, Func<string, string> relativePath)
    {
        _tokens = tokens;
        _unit = unit;
        _relativePath = relativePath;
    }

    /// <summary>Parses one unit file. Returns null for programs, libraries and packages.</summary>
    public static DelphiUnit? ParseFile(string path, Func<string, string> relativePath)
    {
        var (tokens, warnings) = DelphiLexer.Tokenize(path);
        var unit = Parse(tokens, relativePath(path), relativePath);
        if (unit is not null)
        {
            unit.Warnings.InsertRange(0, warnings);
            unit.Lines = CountLines(File.ReadAllBytes(path));
        }

        return unit;
    }

    /// <summary>Parses tokens of a unit; used by tests with <see cref="DelphiLexer.TokenizeText"/>.</summary>
    public static DelphiUnit? Parse(List<Token> tokens, string file, Func<string, string>? relativePath = null)
    {
        if (tokens.Count == 0 || !tokens[0].Is("unit"))
        {
            return null;
        }

        relativePath ??= p => p;
        string name = tokens.Count > 1 ? tokens[1].Text : Path.GetFileNameWithoutExtension(file);
        var unit = new DelphiUnit { Name = name, File = file, UnitLine = tokens[0].Line };
        var parser = new DelphiParser(tokens, unit, relativePath);
        parser.ParseUnit();
        AssignKeys(unit);
        LinkAccessors(unit);
        return unit;
    }

    private static int CountLines(byte[] bytes)
    {
        if (bytes.Length == 0)
        {
            return 0;
        }

        int lines = 0;
        foreach (byte b in bytes)
        {
            if (b == (byte)'\n')
            {
                lines++;
            }
        }

        return bytes[^1] == (byte)'\n' ? lines : lines + 1;
    }

    /// <summary>Marks getter and setter methods that a property of the same class names after "read" or "write".</summary>
    private static void LinkAccessors(DelphiUnit unit)
    {
        var methods = unit.Symbols
            .Where(s => s.Owner is not null && s.Kind is SymbolKind.Method or SymbolKind.ClassMethod)
            .ToLookup(s => $"{s.Owner}.{s.Name}", StringComparer.OrdinalIgnoreCase);
        foreach (var property in unit.Symbols.Where(s => s.Kind == SymbolKind.Property))
        {
            foreach (string? accessor in new[] { property.Reader, property.Writer })
            {
                if (accessor is null)
                {
                    continue;
                }

                foreach (var method in methods[$"{property.Owner}.{accessor}"])
                {
                    method.AccessorOf ??= property;
                }
            }
        }
    }

    private Token Peek(int offset = 0) => _position + offset < _tokens.Count ? _tokens[_position + offset] : _tokens[^1];

    private Token Next() => _position < _tokens.Count - 1 ? _tokens[_position++] : _tokens[^1];

    private bool AtEnd => Peek().Kind == TokenKind.End;

    private bool Accept(string text)
    {
        if (Peek().Is(text))
        {
            _position++;
            return true;
        }

        return false;
    }

    private void Warn(string message) => _unit.Warnings.Add($"{_unit.File}:{Peek().Line}: {message} (at '{Peek().Text}')");

    private void ParseUnit()
    {
        Next(); // unit
        Next(); // name
        while (Peek().Is("."))
        {
            Next();
            Next();
        }

        SkipPast(";");
        bool inInterface = true;
        Accept("interface");
        while (!AtEnd)
        {
            var token = Peek();
            if (token.Is("implementation"))
            {
                Next();
                inInterface = false;
            }
            else if (token.Is("end") && Peek(1).Is("."))
            {
                break;
            }
            else if (token.Is("initialization") || token.Is("finalization") || (!inInterface && token.Is("begin")))
            {
                ParseUnitSection();
            }
            else if (token.Is("uses"))
            {
                Next();
                ParseUses(inInterface ? _unit.UsesInterface : _unit.UsesImplementation);
            }
            else if (token.Is("exports"))
            {
                SkipPast(";");
            }
            else if (token.Is("type"))
            {
                Next();
                ParseTypeSection(inInterface);
            }
            else if (token.Is("const"))
            {
                Next();
                ParseConstSection(SymbolKind.Const, inInterface);
            }
            else if (token.Is("resourcestring"))
            {
                Next();
                ParseConstSection(SymbolKind.ResourceString, inInterface);
            }
            else if (token.Is("var") || token.Is("threadvar"))
            {
                Next();
                ParseVarSection(inInterface);
            }
            else if (IsRoutineStart())
            {
                if (inInterface)
                {
                    ParseRoutineDeclaration();
                }
                else
                {
                    ParseRoutineImplementation();
                }
            }
            else
            {
                Warn("unexpected token at unit level");
                Next();
            }
        }
    }

    /// <summary>Reads "A, B.C, D in 'D.pas';" into unit names.</summary>
    private void ParseUses(List<string> into)
    {
        while (!AtEnd && !Peek().Is(";"))
        {
            if (Peek().IsIdentifier)
            {
                string name = Next().Text;
                while (Peek().Is(".") && Peek(1).IsIdentifier)
                {
                    Next();
                    name += "." + Next().Text;
                }

                into.Add(name);
                if (Accept("in"))
                {
                    Next(); // 'file name'
                }
            }
            else
            {
                Next();
            }

            Accept(",");
        }

        Accept(";");
    }

    private bool IsRoutineStart()
    {
        var token = Peek();
        if (token.Is("procedure") || token.Is("function") || token.Is("constructor") || token.Is("destructor") || token.Is("operator"))
        {
            return true;
        }

        return token.Is("class") && (Peek(1).Is("procedure") || Peek(1).Is("function") || Peek(1).Is("constructor") ||
                                     Peek(1).Is("destructor") || Peek(1).Is("operator"));
    }

    private void ParseUnitSection()
    {
        var token = Next();
        bool isFinalization = token.Is("finalization");
        Add(isFinalization ? "finalization" : "initialization", null,
            isFinalization ? SymbolKind.Finalization : SymbolKind.Initialization, token, inInterface: false);
        if (token.Is("begin"))
        {
            // Old-style unit initialization: "begin ... end."
            SkipStatements(stopAtFinalization: false);
            return;
        }

        SkipStatements(stopAtFinalization: !isFinalization);
    }

    /// <summary>Skips statements up to "finalization" or the final "end." of the unit.</summary>
    private void SkipStatements(bool stopAtFinalization)
    {
        int depth = 0;
        while (!AtEnd)
        {
            var token = Peek();
            if (depth == 0 && ((stopAtFinalization && token.Is("finalization")) || (token.Is("end") && Peek(1).Is("."))))
            {
                return;
            }

            Next();
            if (token.Is("asm"))
            {
                SkipAsm();
            }
            else if (token.Is("begin") || token.Is("case") || token.Is("try"))
            {
                depth++;
            }
            else if (token.Is("end"))
            {
                depth--;
            }
        }
    }

    private void ParseTypeSection(bool inInterface)
    {
        while (Peek().IsIdentifier && !Reserved.Contains(Peek().Text) && Peek(1).Is("="))
        {
            ParseTypeDeclaration(inInterface, record: true);
        }
    }

    private void ParseTypeDeclaration(bool inInterface, bool record)
    {
        var nameToken = Next();
        Next(); // =
        Accept("type");
        Accept("packed");
        var token = Peek();
        string name = nameToken.Text;

        if (token.Is("class"))
        {
            Next();
            if (Accept(";"))
            {
                return; // forward declaration
            }

            if (Peek().Is("of"))
            {
                SkipPast(";");
                AddType(name, SymbolKind.ClassRef, nameToken, inInterface, record);
                return;
            }

            while (Peek().Is("abstract") || Peek().Is("sealed"))
            {
                Next();
            }

            var heritage = Peek().Is("(") ? ReadHeritage() : [];
            var classSymbol = AddType(name, SymbolKind.Class, nameToken, inInterface, record);
            if (classSymbol is not null)
            {
                classSymbol.Parent = heritage.FirstOrDefault() ?? "TObject";
                classSymbol.Interfaces = heritage.Skip(1).ToList();
            }

            if (Accept(";"))
            {
                return; // class with no body, such as "EBold = class(Exception);"
            }

            ParseMembers(name, isRecord: false, inInterface, record);
            ExpectEnd();
            return;
        }

        if (token.Is("interface") || token.Is("dispinterface"))
        {
            Next();
            if (Accept(";"))
            {
                return; // forward declaration
            }

            var interfaceHeritage = Peek().Is("(") ? ReadHeritage() : [];
            if (Peek().Is("["))
            {
                SkipBalanced("[", "]");
            }

            var interfaceSymbol = AddType(name, SymbolKind.Interface, nameToken, inInterface, record);
            if (interfaceSymbol is not null)
            {
                interfaceSymbol.Parent = interfaceHeritage.FirstOrDefault() ?? "IInterface";
            }

            ParseMembers(name, isRecord: false, inInterface, record);
            ExpectEnd();
            return;
        }

        if (token.Is("object"))
        {
            Next();
            var objectHeritage = Peek().Is("(") ? ReadHeritage() : [];
            var objectSymbol = AddType(name, SymbolKind.Object, nameToken, inInterface, record);
            if (objectSymbol is not null)
            {
                objectSymbol.Parent = objectHeritage.FirstOrDefault();
            }

            ParseMembers(name, isRecord: false, inInterface, record);
            ExpectEnd();
            return;
        }

        if (token.Is("record"))
        {
            Next();
            AddType(name, SymbolKind.Record, nameToken, inInterface, record);
            ParseMembers(name, isRecord: true, inInterface, record);
            ExpectEnd();
            return;
        }

        if (token.Is("("))
        {
            var members = ReadEnumMembers();
            SkipTypeRest();
            var enumSymbol = AddType(name, SymbolKind.Enum, nameToken, inInterface, record);
            if (enumSymbol is not null)
            {
                enumSymbol.EnumMembers = members;
            }

            return;
        }

        if (token.Is("procedure") || token.Is("function") || token.Is("reference"))
        {
            var (signature, parameterNames, returnType) = ReadProceduralType();
            var procedural = AddType(name, SymbolKind.Procedural, nameToken, inInterface, record);
            if (procedural is not null)
            {
                procedural.Signature = signature;
                procedural.ParameterNames = parameterNames;
                procedural.ReturnType = returnType;
            }

            return;
        }

        var kind = token.Is("set") ? SymbolKind.Set : token.Is("array") ? SymbolKind.Array : token.Is("^") ? SymbolKind.Pointer : SymbolKind.Alias;
        string typeText = CaptureTypeRest();
        var typeSymbol = AddType(name, kind, nameToken, inInterface, record);
        if (typeSymbol is not null)
        {
            typeSymbol.TypeText = typeText;
        }

    }

    private DelphiSymbol? AddType(string name, SymbolKind kind, Token at, bool inInterface, bool record) =>
        record ? Add(name, null, kind, at, inInterface) : null;

    /// <summary>Reads "(TParent, IFoo, IBar)" into type names.</summary>
    private List<string> ReadHeritage()
    {
        var names = new List<string>();
        Next(); // (
        while (!AtEnd && !Peek().Is(")"))
        {
            if (Peek().IsIdentifier)
            {
                string name = Next().Text;
                while (Peek().Is(".") && Peek(1).IsIdentifier)
                {
                    Next();
                    name += "." + Next().Text;
                }

                names.Add(name);
            }
            else
            {
                Next();
            }
        }

        Accept(")");
        return names;
    }

    /// <summary>Reads "(a, b = 5, c)" into names with ordinal values.</summary>
    private List<KeyValuePair<string, long>> ReadEnumMembers()
    {
        var members = new List<KeyValuePair<string, long>>();
        long next = 0;
        Next(); // (
        while (!AtEnd && !Peek().Is(")"))
        {
            if (!Peek().IsIdentifier)
            {
                Next();
                continue;
            }

            string name = Next().Text;
            long value = next;
            if (Accept("="))
            {
                var tokens = new List<Token>();
                int depth = 0;
                while (!AtEnd && !(depth == 0 && (Peek().Is(",") || Peek().Is(")"))))
                {
                    var token = Next();
                    depth += token.Is("(") ? 1 : token.Is(")") ? -1 : 0;
                    tokens.Add(token);
                }

                if (DelphiConstants.EvaluateTokens(tokens, _ => null) is long explicitValue)
                {
                    value = explicitValue;
                }
            }

            members.Add(new KeyValuePair<string, long>(name, value));
            next = value + 1;
            Accept(",");
        }

        Accept(")");
        return members;
    }

    /// <summary>Reads the rest of a type declaration up to its ";" and returns it as text.</summary>
    private string CaptureTypeRest()
    {
        int start = _position;
        SkipTypeRest();
        int end = _position - 1; // the ";"
        return string.Join(" ", _tokens.Skip(start).Take(Math.Max(0, end - start)).Select(t => t.Kind == TokenKind.String ? $"'{t.Text}'" : t.Text));
    }

    /// <summary>Reads a procedural type: parameters, result, "of object" and calling convention directives.</summary>
    private (string Signature, List<string> ParameterNames, string? ReturnType) ReadProceduralType()
    {
        Next(); // procedure / function / reference
        if (Peek().Is("to"))
        {
            Next();
            Next();
        }

        _parameterNames = [];
        string signature = Peek().Is("(") ? ParseParameters() : "";
        var parameterNames = _parameterNames;
        string? returnType = Accept(":") ? ReadTypeReference() : null;

        if (Accept("of"))
        {
            Accept("object");
        }

        SkipTypeRest();
        while (Peek().IsIdentifier && RoutineDirectives.Contains(Peek().Text) && !Peek(1).Is("=") && !Peek(1).Is(":"))
        {
            Next();
            Accept(";");
        }

        return (signature, parameterNames, returnType);
    }

    /// <summary>Skips the rest of a type expression up to and including its ";", minding nested records and brackets.</summary>
    private void SkipTypeRest()
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
            else if (token.Is("record") || token.Is("class") && !Peek().Is("of"))
            {
                SkipRecordBody();
            }
            else if (token.Is(";") && depth <= 0)
            {
                return;
            }
        }
    }

    private void SkipRecordBody()
    {
        int depth = 1;
        while (!AtEnd && depth > 0)
        {
            var token = Next();
            if (token.Is("record"))
            {
                depth++;
            }
            else if (token.Is("end"))
            {
                depth--;
            }
        }
    }

    private void ParseMembers(string owner, bool isRecord, bool inInterface, bool record)
    {
        string visibility = "public";
        while (!AtEnd && !Peek().Is("end"))
        {
            var token = Peek();
            if (token.Is("private") || token.Is("protected") || token.Is("public") || token.Is("published") || token.Is("automated"))
            {
                visibility = Next().Text.ToLowerInvariant();
                continue;
            }

            if (token.Is("strict"))
            {
                Next();
                continue;
            }

            if (token.Is("class"))
            {
                if (Peek(1).Is("procedure") || Peek(1).Is("function") || Peek(1).Is("constructor") || Peek(1).Is("destructor") || Peek(1).Is("operator"))
                {
                    Next();
                    ParseMethodHeader(owner, isClassMethod: true, visibility, inInterface, record);
                    continue;
                }

                if (Peek(1).Is("property"))
                {
                    Next();
                    ParseProperty(owner, visibility, inInterface, record);
                    continue;
                }

                Next();
                Accept("var");
                continue;
            }

            if (token.Is("procedure") || token.Is("function") || token.Is("constructor") || token.Is("destructor") || token.Is("operator"))
            {
                ParseMethodHeader(owner, isClassMethod: false, visibility, inInterface, record);
                continue;
            }

            if (token.Is("property"))
            {
                ParseProperty(owner, visibility, inInterface, record);
                continue;
            }

            if (token.Is("case") && isRecord)
            {
                SkipVariantPart();
                continue;
            }

            if (token.Is("const") || token.Is("type") || token.Is("var"))
            {
                Next();
                continue;
            }

            if (token.IsIdentifier)
            {
                SkipField();
                continue;
            }

            Warn($"unexpected token in {owner}");
            Next();
        }
    }

    /// <summary>A record variant part: "case Tag: Type of 1: (fields); 2: (fields)" up to the record's "end".</summary>
    private void SkipVariantPart()
    {
        Next(); // case
        while (!AtEnd && !Peek().Is("of"))
        {
            Next();
        }

        Next(); // of
        while (!AtEnd && !Peek().Is("end"))
        {
            if (Peek().Is("("))
            {
                SkipBalanced("(", ")");
            }
            else
            {
                Next();
            }
        }
    }

    private void SkipField()
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
            else if (token.Is("record"))
            {
                SkipRecordBody();
            }
            else if (token.Is(";") && depth <= 0)
            {
                return;
            }
            else if (token.Is("end") && depth <= 0)
            {
                // a last field without ";" before the record's end
                _position--;
                return;
            }
        }
    }

    private void ParseMethodHeader(string owner, bool isClassMethod, string visibility, bool inInterface, bool record)
    {
        var keyword = Next();
        var nameToken = Next();
        if (Peek().Is(".") || Peek().Is("="))
        {
            // Method resolution clause, such as "procedure IFoo.Bar = MyBar;".
            SkipPast(";");
            return;
        }

        _parameterNames = [];
        string signature = Peek().Is("(") ? ParseParameters() : "";
        var parameterNames = _parameterNames;
        string? returnType = Accept(":") ? ReadTypeReference() : null;
        Accept(";");
        bool isAbstract = ParseDirectives().Contains("abstract");
        var kind = keyword.Is("constructor") ? SymbolKind.Constructor
            : keyword.Is("destructor") ? SymbolKind.Destructor
            : isClassMethod ? SymbolKind.ClassMethod
            : SymbolKind.Method;
        if (record)
        {
            var method = Add(nameToken.Text, owner, kind, nameToken, inInterface, signature, visibility);
            method.IsAbstract = isAbstract;
            method.ParameterNames = parameterNames;
            method.ReturnType = returnType;
        }
    }

    private HashSet<string> ParseDirectives()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (Peek().IsIdentifier && RoutineDirectives.Contains(Peek().Text) && !Peek(1).Is(":") && !Peek(1).Is(",") && !Peek(1).Is("="))
        {
            var directive = Next();
            seen.Add(directive.Text);
            if (directive.Is("message") || directive.Is("dispid") || directive.Is("external") || directive.Is("deprecated") || directive.Is("experimental"))
            {
                while (!AtEnd && !Peek().Is(";"))
                {
                    Next();
                }
            }

            Accept(";");
        }

        return seen;
    }

    /// <summary>Reads "(a, b: Integer; const c: string = '')" into "Integer,Integer,string"; the names go to _parameterNames.</summary>
    private string ParseParameters()
    {
        Next(); // (
        var types = new List<string>();
        _parameterNames = [];
        while (!AtEnd && !Peek().Is(")"))
        {
            if (Peek().Is("const") || Peek().Is("var") || Peek().Is("out") || Peek().Is("constref"))
            {
                Next();
            }

            int names = 1;
            _parameterNames.Add(Next().Text); // first parameter name
            while (Accept(","))
            {
                _parameterNames.Add(Next().Text);
                names++;
            }

            string type = "";
            if (Accept(":"))
            {
                type = ReadTypeReference();
            }

            if (Accept("="))
            {
                SkipDefaultValue();
            }

            for (int k = 0; k < names; k++)
            {
                types.Add(type);
            }

            if (!Accept(";") && !Peek().Is(")"))
            {
                Warn("unexpected token in a parameter list");
                Next();
            }
        }

        Accept(")");
        return string.Join(",", types);
    }

    private void SkipDefaultValue()
    {
        int depth = 0;
        while (!AtEnd)
        {
            var token = Peek();
            if (depth == 0 && (token.Is(";") || token.Is(")")))
            {
                return;
            }

            Next();
            if (token.Is("(") || token.Is("["))
            {
                depth++;
            }
            else if (token.Is(")") || token.Is("]"))
            {
                depth--;
            }
        }
    }

    private string ReadTypeReference()
    {
        if (Peek().Is("array"))
        {
            Next();
            if (Peek().Is("["))
            {
                SkipBalanced("[", "]");
            }

            Accept("of");
            return "array of " + ReadTypeReference();
        }

        var text = Next().Text;
        while (Peek().Is("."))
        {
            Next();
            text += "." + Next().Text;
        }

        return text;
    }

    private void SkipTypeReference() => ReadTypeReference();

    /// <summary>
    /// Reads a property with its read and write accessors. A property without a type ("property DragMode;") only
    /// changes the visibility of an inherited property, so it is not recorded.
    /// </summary>
    private void ParseProperty(string owner, string visibility, bool inInterface, bool record)
    {
        Next(); // property
        var nameToken = Next();
        string signature = "";
        if (Peek().Is("["))
        {
            signature = ParseIndexParameters();
        }

        bool redeclaration = !Peek().Is(":");
        string? reader = null, writer = null;
        if (Accept(":"))
        {
            SkipTypeReference();
        }

        int depth = 0;
        while (!AtEnd && !(depth == 0 && Peek().Is(";")))
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
            else if (depth == 0 && (token.Is("read") || token.Is("write")) && Peek().IsIdentifier)
            {
                string accessor = Next().Text;
                while (Peek().Is(".") && Peek(1).IsIdentifier)
                {
                    Next();
                    accessor += "." + Next().Text;
                }

                if (token.Is("read"))
                {
                    reader = accessor;
                }
                else
                {
                    writer = accessor;
                }
            }
        }

        Accept(";");
        bool isDefault = false;
        if (Peek().Is("default") && Peek(1).Is(";"))
        {
            Next();
            Next();
            isDefault = true;
        }

        if (record && !redeclaration)
        {
            var symbol = Add(nameToken.Text, owner, SymbolKind.Property, nameToken, inInterface, signature, visibility);
            symbol.Reader = reader;
            symbol.Writer = writer;
            symbol.IsDefaultProperty = isDefault;
        }
    }

    private string ParseIndexParameters()
    {
        Next(); // [
        var types = new List<string>();
        while (!AtEnd && !Peek().Is("]"))
        {
            Accept("const");
            Accept("var");
            int names = 1;
            Next();
            while (Accept(","))
            {
                Next();
                names++;
            }

            string type = Accept(":") ? ReadTypeReference() : "";
            for (int k = 0; k < names; k++)
            {
                types.Add(type);
            }

            if (!Accept(";") && !Peek().Is("]"))
            {
                Next();
            }
        }

        Accept("]");
        return string.Join(",", types);
    }

    private void ParseConstSection(SymbolKind kind, bool inInterface)
    {
        while (Peek().IsIdentifier && !Reserved.Contains(Peek().Text) && (Peek(1).Is("=") || Peek(1).Is(":")))
        {
            var nameToken = Next();
            string? typeText = null;
            if (Accept(":"))
            {
                int start = _position;
                int depth = 0;
                while (!AtEnd && !(depth == 0 && Peek().Is("=")))
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
                    else if (token.Is("record"))
                    {
                        SkipRecordBody();
                    }
                }

                typeText = string.Join(" ", _tokens.Skip(start).Take(_position - start).Select(t => t.Text));
            }

            Accept("=");
            var value = new List<Token>();
            int valueDepth = 0;
            while (!AtEnd && !(valueDepth <= 0 && Peek().Is(";")))
            {
                var token = Next();
                if (token.Is("(") || token.Is("["))
                {
                    valueDepth++;
                }
                else if (token.Is(")") || token.Is("]"))
                {
                    valueDepth--;
                }

                value.Add(token);
            }

            Accept(";");
            // "X = 1 deprecated;" and "X = 'a' deprecated 'use Y';": the hint is not part of the value.
            int hint = value.FindIndex(t => t.IsIdentifier && HintDirectives.Contains(t.Text));
            if (hint > 0)
            {
                value.RemoveRange(hint, value.Count - hint);
            }

            if (inInterface)
            {
                var symbol = Add(nameToken.Text, null, kind, nameToken, inInterface);
                symbol.TypeText = typeText;
                symbol.ValueTokens = value;
            }
        }
    }

    private void ParseVarSection(bool inInterface)
    {
        while (Peek().IsIdentifier && !Reserved.Contains(Peek().Text) && (Peek(1).Is(":") || Peek(1).Is(",")))
        {
            var names = new List<Token> { Next() };
            while (Accept(","))
            {
                names.Add(Next());
            }

            SkipField();
            if (inInterface)
            {
                foreach (var nameToken in names)
                {
                    Add(nameToken.Text, null, SymbolKind.Var, nameToken, inInterface);
                }
            }
        }
    }

    private void ParseRoutineDeclaration()
    {
        Accept("class");
        Next(); // procedure / function
        var nameToken = Next();
        _parameterNames = [];
        string signature = Peek().Is("(") ? ParseParameters() : "";
        var parameterNames = _parameterNames;
        string? returnType = Accept(":") ? ReadTypeReference() : null;
        Accept(";");
        ParseDirectives();
        var routine = Add(nameToken.Text, null, SymbolKind.Routine, nameToken, inInterface: true, signature);
        routine.ParameterNames = parameterNames;
        routine.ReturnType = returnType;
    }

    private void ParseRoutineImplementation()
    {
        bool isClassMethod = Accept("class");
        var keyword = Next();
        var first = Next();
        string name = first.Text;
        string? owner = null;
        while (Accept("."))
        {
            owner = owner is null ? name : owner + "." + name;
            name = Next().Text;
        }

        _parameterNames = [];
        string? signature = Peek().Is("(") ? ParseParameters() : null;
        var parameterNames = _parameterNames;
        string? returnType = Accept(":") ? ReadTypeReference() : null;
        Accept(";");
        var directives = ParseDirectives();
        bool hasBody = !directives.Contains("forward") && !directives.Contains("external");
        if (hasBody)
        {
            SkipRoutineBody();
        }

        var symbol = LinkImplementation(owner, name, signature, keyword, isClassMethod, first, hasBody, directives.Contains("forward"));
        if (symbol is not null && symbol.ParameterNames.Count == 0 && symbol.ReturnType is null)
        {
            symbol.ParameterNames = parameterNames;
            symbol.ReturnType = returnType;
        }
    }

    private DelphiSymbol? LinkImplementation(string? owner, string name, string? signature, Token keyword, bool isClassMethod, Token at, bool hasBody, bool isForward)
    {
        var candidates = _unit.Symbols
            .Where(s => s.IsCallable &&
                        string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(s.Owner, owner, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (candidates.Count > 1 && signature is not null)
        {
            candidates = candidates.Where(s => SameSignature(s.Signature, signature)).ToList();
        }

        var match = candidates.FirstOrDefault(s => s.ImplLine is null) ?? candidates.FirstOrDefault();
        if (match is not null)
        {
            if (hasBody)
            {
                match.ImplFile = _relativePath(at.File);
                match.ImplLine = at.Line;
            }

            return match;
        }

        if (isForward)
        {
            // A forward declaration in the implementation section: record it now, the body links later.
            return Add(name, owner, SymbolKind.Routine, at, inInterface: false, signature ?? "");
        }

        var kind = owner is null ? SymbolKind.Routine
            : keyword.Is("constructor") ? SymbolKind.Constructor
            : keyword.Is("destructor") ? SymbolKind.Destructor
            : isClassMethod ? SymbolKind.ClassMethod
            : SymbolKind.Method;
        var symbol = Add(name, owner, kind, at, inInterface: false, signature ?? "");
        if (hasBody)
        {
            symbol.ImplFile = _relativePath(at.File);
            symbol.ImplLine = at.Line;
        }

        return symbol;
    }

    private static bool SameSignature(string a, string b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string signature) => signature.Replace(" ", "", StringComparison.Ordinal);

    /// <summary>Skips local declarations, nested routines and the begin..end (or asm..end) block of a routine.</summary>
    private void SkipRoutineBody()
    {
        while (!AtEnd)
        {
            var token = Peek();
            if (token.Is("var") || token.Is("threadvar"))
            {
                Next();
                while (Peek().IsIdentifier && !Reserved.Contains(Peek().Text) && (Peek(1).Is(":") || Peek(1).Is(",")))
                {
                    SkipField();
                }

                continue;
            }

            if (token.Is("const") || token.Is("resourcestring"))
            {
                Next();
                ParseConstSection(SymbolKind.Const, inInterface: false);
                continue;
            }

            if (token.Is("type"))
            {
                Next();
                while (Peek().IsIdentifier && !Reserved.Contains(Peek().Text) && Peek(1).Is("="))
                {
                    ParseTypeDeclaration(inInterface: false, record: false);
                }

                continue;
            }

            if (token.Is("label"))
            {
                SkipPast(";");
                continue;
            }

            if (token.Is("procedure") || token.Is("function"))
            {
                Next();
                Next();
                if (Peek().Is("("))
                {
                    ParseParameters();
                }

                if (Accept(":"))
                {
                    SkipTypeReference();
                }

                Accept(";");
                var directives = ParseDirectives();
                if (!directives.Contains("forward") && !directives.Contains("external"))
                {
                    SkipRoutineBody();
                }

                continue;
            }

            if (token.Is("asm"))
            {
                Next();
                SkipAsm();
                Accept(";");
                return;
            }

            if (token.Is("begin"))
            {
                SkipBlock();
                Accept(";");
                return;
            }

            Warn("unexpected token before a routine body");
            Next();
        }
    }

    private void SkipAsm()
    {
        while (!AtEnd && !Peek().Is("end"))
        {
            Next();
        }

        Next(); // end
    }

    private void SkipBlock()
    {
        int depth = 0;
        do
        {
            var token = Next();
            if (token.Is("asm"))
            {
                SkipAsm();
            }
            else if (token.Is("begin") || token.Is("case") || token.Is("try"))
            {
                depth++;
            }
            else if (token.Is("end"))
            {
                depth--;
            }
        }
        while (depth > 0 && !AtEnd);
    }

    private void ExpectEnd()
    {
        if (!Accept("end"))
        {
            Warn("expected 'end'");
        }

        while (Peek().IsIdentifier && HintDirectives.Contains(Peek().Text))
        {
            Next();
            if (Peek().Kind == TokenKind.String)
            {
                Next();
            }
        }

        Accept(";");
    }

    private void SkipPast(string text)
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
            else if (depth <= 0 && token.Is(text))
            {
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
                if (depth == 0)
                {
                    return;
                }
            }
        }
    }

    private DelphiSymbol Add(string name, string? owner, SymbolKind kind, Token at, bool inInterface, string signature = "", string visibility = "")
    {
        var symbol = new DelphiSymbol
        {
            Unit = _unit.Name,
            Name = name,
            Owner = owner,
            Kind = kind,
            Signature = signature,
            File = _relativePath(at.File),
            Line = at.Line,
            InInterface = inInterface,
            Visibility = visibility,
        };
        _unit.Symbols.Add(symbol);
        return symbol;
    }

    /// <summary>Gives every symbol its marker key, adding the parameter types where a name is overloaded, and drops duplicates.</summary>
    private static void AssignKeys(DelphiUnit unit)
    {
        foreach (var group in unit.Symbols.Where(s => s.IsCallable).GroupBy(s => s.BaseKey, StringComparer.OrdinalIgnoreCase))
        {
            bool overloaded = group.Count() > 1;
            foreach (var symbol in group)
            {
                symbol.Key = overloaded ? $"{symbol.BaseKey}({symbol.Signature})" : symbol.BaseKey;
            }
        }

        foreach (var symbol in unit.Symbols.Where(s => !s.IsCallable))
        {
            symbol.Key = symbol.BaseKey;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = unit.Symbols.Where(s => !seen.Add(s.Key)).ToList();
        foreach (var duplicate in duplicates)
        {
            unit.Symbols.Remove(duplicate);
            unit.Warnings.Add($"{duplicate.File}:{duplicate.Line}: duplicate declaration of {duplicate.Key} ignored");
        }
    }
}
