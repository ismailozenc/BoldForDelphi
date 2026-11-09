// The library's own attribute types. The attributes on each partial class say what the type is, and the Bold attribute
// generator (Bold.SourceGenerators) writes the rest of the class when the project builds; what is the library's own
// rule is written here.
using Bold;

namespace Library.Attributes;

/// <summary>The condition a copy is in: a value set. The first representation of a value is its name; New is the default.</summary>
[BoldValueSet("Condition", ValuePrefix = "c")]
[BoldValueSetValue("New")]
[BoldValueSetValue("Good")]
[BoldValueSetValue("Worn")]
[BoldValueSetValue("Damaged")]
public partial class TBACondition : TBAValueSet
{
}

/// <summary>Where a copy stands: a shelf and a position on it, written "B-12"; the generated mapper stores it in two columns.</summary>
[BoldAttributeType("ShelfMark", GenerateMapper = true)]
[BoldAttributeField("Shelf", typeof(string))]
[BoldAttributeField("Position", typeof(int))]
public partial class TBAShelfMark : TBoldAttribute
{
    public override string GetStringRepresentation(TBoldRepresentation Representation) => IsNull ? "" : $"{AsShelf}-{AsPosition}";

    public override void SetStringRepresentation(TBoldRepresentation Representation, string Value)
    {
        string[] parts = Value.Split('-', 2, StringSplitOptions.TrimEntries);
        if (Value.Trim().Length == 0)
        {
            SetToNull();
        }
        else if (parts.Length == 2 && parts[0].Length > 0 && int.TryParse(parts[1], out int position))
        {
            AsShelf = parts[0];
            AsPosition = position;
        }
        else
        {
            throw new EBold($"\"{Value}\" is not a shelf mark (shelf-position, such as B-12)");
        }
    }
}

/// <summary>An ISBN-13: a string attribute that takes only numbers whose check digit is right.</summary>
[BoldAttributeType("Isbn")]
public partial class TBAIsbn : TBAString
{
    public override bool ValidateString(string Value, TBoldRepresentation Representation) =>
        (Value.Length == 0 || IsIsbn13(Value)) && base.ValidateString(Value, Representation);

    /// <summary>Thirteen digits (hyphens and spaces aside) whose weighted sum, 1 and 3 in turn, is a multiple of ten.</summary>
    public static bool IsIsbn13(string isbn)
    {
        string digits = isbn.Replace("-", "").Replace(" ", "");
        if (digits.Length != 13 || !digits.All(char.IsAsciiDigit))
        {
            return false;
        }

        int sum = digits.Select((digit, i) => (digit - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        return sum % 10 == 0;
    }
}
