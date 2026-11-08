// Custom attribute types, written the way Delphi's attribute wizard made them: the attributes on each class say what
// the wizard's forms asked, and the Bold attribute generator writes the rest of the class when the project builds.
// What the wizard left to "your code here" is written here by hand.

using Bold;

namespace CustomAttributes;

/// <summary>A color: a value set of three values, the second with a German representation as well.</summary>
[BoldValueSet("Color", ValuePrefix = "c")]
[BoldValueSetValue("Red")]
[BoldValueSetValue("Green", "Green", "Grün")]
[BoldValueSetValue("Blue")]
public partial class TBAColor : TBAValueSet
{
}

/// <summary>A point on a plane: a new attribute with two fields, stored in two columns by its generated mapper.</summary>
[BoldAttributeType("Point", GenerateMapper = true)]
[BoldAttributeField("X", typeof(int))]
[BoldAttributeField("Y", typeof(int))]
public partial class TBAPoint : TBoldAttribute
{
    /// <summary>The point as "(x, y)"; empty when null.</summary>
    public override string GetStringRepresentation(TBoldRepresentation Representation) => IsNull ? "" : $"({AsX}, {AsY})";

    /// <summary>Reads "(x, y)" (or "x, y"); empty makes the point null.</summary>
    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        string text = Value.Trim().TrimStart('(').TrimEnd(')');
        if (text.Length == 0)
        {
            SetToNull();
            return;
        }

        string[] parts = text.Split(',');
        if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), out int x) || !int.TryParse(parts[1].Trim(), out int y))
        {
            throw new EBold($"{GetType().Name}: \"{Value}\" is not a point");
        }

        AsX = x;
        AsY = y;
    }

    /// <summary>A point is moved by setting both coordinates.</summary>
    public void MoveTo(int x, int y)
    {
        AsX = x;
        AsY = y;
    }
}

/// <summary>A string of at most ten characters: a subclass of the string attribute.</summary>
[BoldAttributeType("ShortString")]
public partial class TBAShortString : TBAString
{
    /// <summary>The longest a short string can be.</summary>
    public const int MaxLength = 10;

    /// <summary>A short string is at most <see cref="MaxLength"/> characters.</summary>
    public override bool ValidateString(string Value, TBoldRepresentation Representation) =>
        Value.Length <= MaxLength && base.ValidateString(Value, Representation);
}
