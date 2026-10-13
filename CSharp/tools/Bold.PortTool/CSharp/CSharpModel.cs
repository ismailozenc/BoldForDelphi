namespace Bold.PortTool.CSharp;

public enum CsDeclarationKind
{
    Class,
    Struct,
    Interface,
    Enum,
    Record,
    Delegate,
    Method,
    Constructor,
    StaticConstructor,
    Finalizer,
    Property,
    Indexer,
    Field,
    Const,
    Event,
    Operator,

    /// <summary>A using alias, such as "global using TBoldTimeStampType = int;" (how Delphi type aliases are ported).</summary>
    Alias,
}

/// <summary>One argument of an attribute, kept as tokens and read on demand.</summary>
public sealed class CsArgument
{
    /// <summary>The name of a named argument ("Symbol = ..."), otherwise null.</summary>
    public string? Name { get; init; }

    public required List<CsToken> Tokens { get; init; }

    public string Text => string.Join("", Tokens.Select(t => t.Kind switch
    {
        CsTokenKind.String => $"\"{t.Text}\"",
        CsTokenKind.InterpolatedString => $"$\"{t.Text}\"",
        _ => t.Text,
    }));

    /// <summary>A string literal, string literals joined with "+", or nameof(a.b) (which gives "b").</summary>
    public string? AsString()
    {
        if (Tokens.Count >= 3 && Tokens[0].Is("nameof") && Tokens[1].Is("(") && Tokens[^1].Is(")"))
        {
            var last = Tokens.Take(Tokens.Count - 1).LastOrDefault(t => t.IsIdentifier);
            return last?.Text;
        }

        if (Tokens.Count == 0 || Tokens.Count % 2 == 0)
        {
            return null;
        }

        var parts = new List<string>();
        for (int k = 0; k < Tokens.Count; k++)
        {
            if (k % 2 == 0)
            {
                if (Tokens[k].Kind != CsTokenKind.String)
                {
                    return null;
                }

                parts.Add(Tokens[k].Text);
            }
            else if (!Tokens[k].Is("+"))
            {
                return null;
            }
        }

        return string.Concat(parts);
    }

    /// <summary>typeof(A.B&lt;T&gt;) gives "A.B"; "global::" and generic arguments are dropped.</summary>
    public string? AsTypeOf()
    {
        if (Tokens.Count < 4 || !Tokens[0].Is("typeof") || !Tokens[1].Is("(") || !Tokens[^1].Is(")"))
        {
            return null;
        }

        var parts = new List<string>();
        int angle = 0;
        for (int k = 2; k < Tokens.Count - 1; k++)
        {
            var t = Tokens[k];
            if (t.Is("<"))
            {
                angle++;
            }
            else if (t.Is(">"))
            {
                angle--;
            }
            else if (angle == 0 && t.IsIdentifier && t.Text != "global")
            {
                parts.Add(t.Text);
            }
        }

        return parts.Count == 0 ? null : string.Join(".", parts);
    }

    /// <summary>Enum.Member (also Bold.Enum.Member) gives "Member" when the enum name matches.</summary>
    public string? AsEnumMember(string enumName)
    {
        var identifiers = Tokens.Where(t => t.IsIdentifier).ToList();
        if (identifiers.Count < 2 || Tokens.Any(t => !t.IsIdentifier && !t.Is(".") && !t.Is("::")))
        {
            return null;
        }

        return identifiers[^2].Text == enumName ? identifiers[^1].Text : null;
    }
}

public sealed class CsAttribute
{
    /// <summary>Simple name without namespace or "Attribute" suffix, such as "Delphi" or "Covers".</summary>
    public required string Name { get; init; }

    /// <summary>The attribute target, such as "assembly", or null.</summary>
    public string? Target { get; init; }

    public required List<CsArgument> Arguments { get; init; }

    public required string File { get; init; }

    public required int Line { get; init; }

    public IEnumerable<CsArgument> Positional => Arguments.Where(a => a.Name is null);

    public CsArgument? Named(string name) => Arguments.FirstOrDefault(a => a.Name == name);
}

/// <summary>A C# declaration: a type or a member, with its attributes.</summary>
public sealed class CsDeclaration
{
    public required CsDeclarationKind Kind { get; init; }

    public required string Name { get; init; }

    /// <summary>Containing types, outermost first, joined with "."; null for top-level types.</summary>
    public string? Owner { get; init; }

    public required string Namespace { get; init; }

    public int ParameterCount { get; init; }

    /// <summary>Repository-relative path.</summary>
    public required string File { get; init; }

    public required int Line { get; init; }

    public required List<CsAttribute> Attributes { get; init; }

    public required HashSet<string> Modifiers { get; init; }

    public bool IsType => Kind is CsDeclarationKind.Class or CsDeclarationKind.Struct or CsDeclarationKind.Interface or
        CsDeclarationKind.Enum or CsDeclarationKind.Record or CsDeclarationKind.Delegate;

    /// <summary>Owner.Name, or Name for top-level types.</summary>
    public string FullName => Owner is null ? Name : $"{Owner}.{Name}";

    /// <summary>The innermost containing type.</summary>
    public string? OwnerName => Owner?[(Owner.LastIndexOf('.') + 1)..];

    public bool IsPrivate => Modifiers.Contains("private") && !Modifiers.Contains("protected");

    public bool IsTestMethod => Kind == CsDeclarationKind.Method &&
                                Attributes.Any(a => a.Name is "Test" or "TestCase" or "TestCaseSource" or "Theory");

    public string KindText => Kind switch
    {
        CsDeclarationKind.StaticConstructor => "static constructor",
        _ => Kind.ToString().ToLowerInvariant(),
    };

    public override string ToString() => $"{FullName} ({KindText}, {File}:{Line})";
}

public sealed class CsFile
{
    /// <summary>Repository-relative path.</summary>
    public required string Path { get; init; }

    public List<CsDeclaration> Declarations { get; } = [];

    /// <summary>[assembly: ...] attributes.</summary>
    public List<CsAttribute> AssemblyAttributes { get; } = [];

    public List<string> Warnings { get; } = [];

    /// <summary>The file name up to the first dot: BoldDefs.Aliases.cs and BoldDefs.cs both give BoldDefs.</summary>
    public string UnitName
    {
        get
        {
            string name = System.IO.Path.GetFileName(Path);
            int dot = name.IndexOf('.');
            return dot < 0 ? name : name[..dot];
        }
    }
}
