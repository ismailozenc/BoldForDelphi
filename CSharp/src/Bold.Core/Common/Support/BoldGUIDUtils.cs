// Delphi: Source/Common/Support/BoldGUIDUtils.pas
// Port map: CSharp/port/units/BoldGUIDUtils.md

namespace Bold;

/// <summary>New GUIDs as strings in registry format ({XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}).</summary>
public static partial class BoldGUIDUtils
{
    /// <summary>A new GUID in upper case with braces, or without them when StripBrackets.</summary>
    [Delphi(PortStatus.Ported, Note = "Guid.NewGuid where Delphi calls CoCreateGuid and StringFromCLSID; same upper-case braced format.")]
    public static string BoldCreateGUIDAsString(bool StripBrackets = false)
    {
        string result = Guid.NewGuid().ToString("B").ToUpperInvariant();
        return StripBrackets ? result[1..^1] : result;
    }

    [Delphi(PortStatus.Ported)]
    public static string BoldCreateGUIDWithBracketsAsString() => BoldCreateGUIDAsString();
}
