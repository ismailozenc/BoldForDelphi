// Delphi: Source/MoldModel/Core/BoldMoldConsts.pas
// Port map: CSharp/port/units/BoldMoldConsts.md

namespace Bold;

/// <summary>Messages of the Mold model, the .bld reader and the name expander.</summary>
public static partial class BoldMoldConsts
{
    [Delphi(PortStatus.Ported)] public const string sRecursiveAssignment = "Attempt to make recursive assignment";
    [Delphi(PortStatus.Ported)] public const string sClassIsRelation = "Class \"%s\" is already relation class for another association";
    [Delphi(PortStatus.Ported)] public const string sErrorOnPos = "%s Line: %d Position: %d";
    [Delphi(PortStatus.Ported)] public const string sUnexpectedEOF = "BLD Reader: Unexpected EOF";
    [Delphi(PortStatus.Ported)] public const string sBadCharacter = "BLD Reader: Bad character %s";
    [Delphi(PortStatus.Ported)] public const string sSyntaxError = "BLD Reader: Syntax error";
    [Delphi(PortStatus.Ported)] public const string sAKeywordExpected = "BLD Reader: '%s' expected";
    [Delphi(PortStatus.Ported)] public const string sQuotedStringExpected = "BLD Reader: Quoted string expected";
    [Delphi(PortStatus.Ported)] public const string sKeyWordTokenExpected = "BLD Reader: KEYWORD expected";
    [Delphi(PortStatus.Ported)] public const string sIntegerExpected = "BLD Reader: Integer expected";
    [Delphi(PortStatus.Ported)] public const string sBooleanExpected = "BLD Reader: Boolean expected";
    [Delphi(PortStatus.Ported)] public const string sLogGeneratingInPath = "Generating in path: %s";
    [Delphi(PortStatus.Ported)] public const string sLogGeneratingFile = "Generating file: %s";
    [Delphi(PortStatus.Ported)] public const string sLogInitializingVars = "Initializing variables";
    [Delphi(PortStatus.Ported)] public const string sLogExpandingTemplate = "Expanding template";
    [Delphi(PortStatus.Ported)] public const string sLogConsiderNameChange1 = "You should consider naming your model and base unit";
    [Delphi(PortStatus.Ported)] public const string sLogConsiderNameChange2 = "to avoid filename conflicts with other projects";
    [Delphi(PortStatus.Ported)] public const string sLogNoDelphiMappingForType = "No Delphimapping for type %s";
    [Delphi(PortStatus.Ported)] public const string sLogNoCOMMappingForType = "No COM/IDL mapping for type %s, No attribute generated for %s.%s";
    [Delphi(PortStatus.Ported)] public const string sLogNoNativeMappingForType = "No native mapping for type %s used in attribute %s.%s, only Bold attribute generated";
    [Delphi(PortStatus.Ported)] public const string sNoValueTypeMappingForType = "No Valueinterface mapping for type %s used in persistent attribute %s.%s";
    [Delphi(PortStatus.Ported)] public const string sProcessingClassXFileY = "Processing class %s, file %s";
    [Delphi(PortStatus.Ported)] public const string sMoveToComponent_NoSuperClass = "MoveClassTreeToComponent: No SuperClass";
    [Delphi(PortStatus.Ported)] public const string sMoveToComponent_NoMoldModel = "MoveClassTreeToComponent: No MoldModel";
    [Delphi(PortStatus.Ported)] public const string sCollidingFileName = "WARNING! class %s has a file name that collides with another component (%s)!";
    [Delphi(PortStatus.Ported)] public const string sNoTemplateForPersistenceInterfaces = "No template defined for PersistenceInterfaces";
    [Delphi(PortStatus.Ported)] public const string sUnknownRoleType = "%s.GetMulti: Unknown roletype for %s.%s";
    [Delphi(PortStatus.Ported)] public const string sCannotTrimAfterLinkRolesEnsured = "Can not trim removed elements after the linkroles have been ensured...";
    [Delphi(PortStatus.Ported)] public const string sMemberHasNoDispID = "Member has no Dispid";
    [Delphi(PortStatus.Ported)] public const string sEditTypeNames = "Edit type names";
    [Delphi(PortStatus.Ported)] public const string sNameHasInvalidChars = "Name has invalid characters";
    [Delphi(PortStatus.Ported)] public const string sNameTooLong = "Name is too long";
    [Delphi(PortStatus.Ported)] public const string sNameCannotBeEmpty = "Name can not be empty";
    [Delphi(PortStatus.Ported)] public const string sInvalidFirstChar = "Name must begin with an alpha-character or underscore";
}
