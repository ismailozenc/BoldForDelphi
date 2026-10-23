// Delphi: Source/Handles/Actions/BoldActionDefs.pas
// Port map: CSharp/port/units/BoldActionDefs.md

namespace Bold;

/// <summary>The defaults of the database actions: the user, the password and the database path.</summary>
public static class BoldActionDefs
{
    [Delphi(PortStatus.Ported)] public const string DEFAULTUSERNAME = "SYSDBA";
    [Delphi(PortStatus.Ported)] public const string DEFAULTPASSWORD = "masterkey";

    /// <summary>The database path the database actions prefer; empty for none.</summary>
    [Delphi(PortStatus.Ported)] public static string BoldPreferedDbPath = "";
}
