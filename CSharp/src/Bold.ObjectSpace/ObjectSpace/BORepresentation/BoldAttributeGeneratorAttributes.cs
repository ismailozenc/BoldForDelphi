// The attributes the Bold attribute generator (Bold.SourceGenerators) reads, in place of what Delphi's attribute
// wizard asked for in its forms (decision DECIDE-3). Put them on a partial class and reference Bold.SourceGenerators as
// an analyzer; the generator writes the rest of the class when the project builds:
//
//     [BoldValueSet("Color", ValuePrefix = "c")]
//     [BoldValueSetValue("Red")]
//     [BoldValueSetValue("Green", "Green", "grön")]
//     public partial class TBAColor : TBAValueSet { }
//
//     [BoldAttributeType("Point", GenerateMapper = true)]
//     [BoldAttributeField("X", typeof(int))]
//     [BoldAttributeField("Y", typeof(int))]
//     public partial class TBAPoint : TBoldAttribute { }
//
//     [BoldAttributeType("ShortString")]
//     public partial class TBAShortString : TBAString { }

namespace Bold;

/// <summary>
/// A value set attribute type (an enumeration): the generator writes the enumeration T&lt;Name&gt;Enum, the property
/// As&lt;Name&gt;, the list of values and the registration of the type. The values are given with
/// <see cref="BoldValueSetValueAttribute"/>, in their order.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BoldValueSetAttribute(string ExpressionName) : Attribute
{
    /// <summary>The type's expression name (and the model's name for it unless <see cref="ModelName"/> says otherwise).</summary>
    public string ExpressionName { get; } = ExpressionName;

    /// <summary>The name models use for the type; the expression name when not set.</summary>
    public string? ModelName { get; set; }

    /// <summary>What the names of the enumeration's members start with (Delphi's value prefix), such as "c" for cRed.</summary>
    public string ValuePrefix { get; set; } = "";
}

/// <summary>
/// A value of a value set attribute type: its name, and its string representations. Every value gets as many
/// representations as the value with the most; a missing one is the value's name.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class BoldValueSetValueAttribute(string Name, params string[] Representations) : Attribute
{
    /// <summary>The value's name, which names the enumeration member (after the prefix).</summary>
    public string Name { get; } = Name;

    /// <summary>The value's string representations, the default representation first.</summary>
    public string[] Representations { get; } = Representations;
}

/// <summary>
/// An attribute type. On a class that derives from TBoldAttribute and has fields (<see cref="BoldAttributeFieldAttribute"/>),
/// the generator writes a new attribute: the fields with their As&lt;Field&gt; properties, the content interface
/// IBold&lt;Name&gt;Content with its proxy, the free-standing value TBFS&lt;Name&gt;, and, when asked, the persistence
/// mapper TBoldPM&lt;Name&gt;. On a subclass of another attribute type, it writes the registration and the type name
/// mapping, which take the superclass's content and persistence.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class BoldAttributeTypeAttribute(string ExpressionName) : Attribute
{
    /// <summary>The type's expression name (and the model's name for it unless <see cref="ModelName"/> says otherwise).</summary>
    public string ExpressionName { get; } = ExpressionName;

    /// <summary>The name models use for the type; the expression name when not set.</summary>
    public string? ModelName { get; set; }

    /// <summary>The content (stream) name of a new attribute; the expression name when not set.</summary>
    public string? ContentName { get; set; }

    /// <summary>
    /// The persistence mapper of the type: the generated one's name for a new attribute (TBoldPM&lt;Name&gt; when not set),
    /// or a mapper of your own; a subclass takes its superclass's when not set.
    /// </summary>
    public string? MapperName { get; set; }

    /// <summary>Generate the persistence mapper of a new attribute (one column a field); the project must reference Bold.Persistence.Sql.</summary>
    public bool GenerateMapper { get; set; }
}

/// <summary>A field of a new attribute type: its name and type, and whether it can be read and set.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class BoldAttributeFieldAttribute(string Name, Type FieldType) : Attribute
{
    /// <summary>The field's name: the attribute gets the property As&lt;Name&gt; and the content the property &lt;Name&gt;.</summary>
    public string Name { get; } = Name;

    /// <summary>The field's type: int, string, double, decimal, bool or DateTime when the mapper is generated.</summary>
    public Type FieldType { get; } = FieldType;

    /// <summary>The field can be read (As&lt;Name&gt; has a getter).</summary>
    public bool Readable { get; set; } = true;

    /// <summary>The field can be set (As&lt;Name&gt; has a setter).</summary>
    public bool Writable { get; set; } = true;
}
