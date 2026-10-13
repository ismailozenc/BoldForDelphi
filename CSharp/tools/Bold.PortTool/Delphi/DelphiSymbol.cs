namespace Bold.PortTool.Delphi;

public enum SymbolKind
{
    Class,
    Interface,
    Record,
    Object,
    Enum,
    Set,
    Alias,
    ClassRef,
    Procedural,
    Array,
    Pointer,
    Constructor,
    Destructor,
    Method,
    ClassMethod,
    Property,
    Routine,
    Const,
    ResourceString,
    Var,
    Initialization,
    Finalization,
}

/// <summary>One Delphi element that needs a port status: a type, a method, a property, a routine, a constant or a unit section.</summary>
public sealed class DelphiSymbol
{
    public required string Unit { get; init; }

    public required string Name { get; init; }

    /// <summary>The declaring type for members, otherwise null.</summary>
    public string? Owner { get; init; }

    public required SymbolKind Kind { get; init; }

    /// <summary>Parameter types, comma separated, for routines and methods.</summary>
    public string Signature { get; set; } = "";

    /// <summary>Repository-relative path of the file that declares it (an include file when the code is included).</summary>
    public required string File { get; init; }

    public required int Line { get; init; }

    /// <summary>True when declared in the interface section; false for implementation-only types and routines.</summary>
    public bool InInterface { get; init; }

    public string Visibility { get; init; } = "";

    /// <summary>Where the body is, for methods and routines.</summary>
    public string? ImplFile { get; set; }

    public int? ImplLine { get; set; }

    /// <summary>The name used in [Delphi] markers: Owner.Name or Name, with (Signature) added when the name is overloaded.</summary>
    public string Key { get; set; } = "";

    /// <summary>For properties: the field or method named after "read".</summary>
    public string? Reader { get; set; }

    /// <summary>For properties: the field or method named after "write".</summary>
    public string? Writer { get; set; }

    /// <summary>For methods declared "abstract": there is no body to port, only the declaration.</summary>
    public bool IsAbstract { get; set; }

    /// <summary>For array properties marked "default"; a C# indexer ports it.</summary>
    public bool IsDefaultProperty { get; set; }

    /// <summary>For getter and setter methods: the property that uses them. Unmarked accessors take the property's status.</summary>
    public DelphiSymbol? AccessorOf { get; set; }

    /// <summary>For classes, interfaces and objects: the parent type ("TObject" when none is given).</summary>
    public string? Parent { get; set; }

    /// <summary>For classes: the interfaces listed after the parent.</summary>
    public List<string> Interfaces { get; set; } = [];

    /// <summary>For enums: the member names with their ordinal values.</summary>
    public List<KeyValuePair<string, long>>? EnumMembers { get; set; }

    /// <summary>For aliases, sets, arrays, pointers and typed constants: the type as written.</summary>
    public string? TypeText { get; set; }

    /// <summary>Parameter names of routines, methods and event types, in order.</summary>
    public List<string> ParameterNames { get; set; } = [];

    /// <summary>The result type of functions and function event types; null for procedures.</summary>
    public string? ReturnType { get; set; }

    /// <summary>For constants and resource strings: the tokens of the value expression.</summary>
    public List<Token>? ValueTokens { get; set; }

    public string BaseKey => Owner is null ? Name : $"{Owner}.{Name}";

    public bool IsCallable => Kind is SymbolKind.Constructor or SymbolKind.Destructor or SymbolKind.Method or SymbolKind.ClassMethod or SymbolKind.Routine;

    public bool IsType => Kind is SymbolKind.Class or SymbolKind.Interface or SymbolKind.Record or SymbolKind.Object or SymbolKind.Enum or
        SymbolKind.Set or SymbolKind.Alias or SymbolKind.ClassRef or SymbolKind.Procedural or SymbolKind.Array or SymbolKind.Pointer;

    public string KindText => Kind switch
    {
        SymbolKind.ClassRef => "class reference",
        SymbolKind.Procedural => "event type",
        SymbolKind.ClassMethod => "class method",
        SymbolKind.ResourceString => "resourcestring",
        SymbolKind.Initialization => "initialization",
        SymbolKind.Finalization => "finalization",
        _ => Kind.ToString().ToLowerInvariant(),
    };

    public int ParameterCount => string.IsNullOrEmpty(Signature) ? 0 : Signature.Split(',').Length;

    public override string ToString() => $"{Unit}.{Key} ({KindText}, {File}:{Line})";
}

public sealed class DelphiUnit
{
    public required string Name { get; init; }

    /// <summary>Repository-relative path of the .pas file, with forward slashes.</summary>
    public required string File { get; init; }

    /// <summary>Number of lines in the .pas file (include files not counted).</summary>
    public int Lines { get; set; }

    /// <summary>The line with "unit Name;", where the link to the C# port goes.</summary>
    public int UnitLine { get; set; } = 1;

    public List<string> UsesInterface { get; } = [];

    public List<string> UsesImplementation { get; } = [];

    public List<DelphiSymbol> Symbols { get; } = [];

    public List<string> Warnings { get; } = [];

    public string Folder => System.IO.Path.GetDirectoryName(File)?.Replace('\\', '/') ?? "";
}
