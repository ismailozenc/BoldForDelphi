// Delphi: Source/Extensions/OLLE/Core/OlleConsts.pas
// Port map: CSharp/port/units/OlleConsts.md

namespace Bold;

/// <summary>The messages of OLLE.</summary>
public static class OlleConsts
{
    [Delphi(PortStatus.Ported)] public const string sCannotChangePersistenceWhenActive = "%s: Can not change Persistent-property when the OLLE system is active";
    [Delphi(PortStatus.Ported)] public const string sObjectNotForeign = "%s.EnsureForeignInfo: Object is not a foreign object";
    [Delphi(PortStatus.Ported)] public const string sWrongOwner = "%s.EnsureForeignInfo: Wrong owner";
    [Delphi(PortStatus.Ported)] public const string sObjectsAlreadyBeingCheckedIn = "%s.StartCheckIn: Already checking in objects for this persistent storage";
    [Delphi(PortStatus.Ported)] public const string sObjectNotOwned = "%s.%s: Object is not an owned object";
    [Delphi(PortStatus.Ported)] public const string sCannotFailIndividualObjects = "%s.VerifyAssociations: Failing inidividual objects not implemented";
    [Delphi(PortStatus.Ported)] public const string sUnresolvedLink = "Operation failed: Unresolved link";
    [Delphi(PortStatus.Ported)] public const string sMissingObjectsNotImplemented = "Missing Objects not implemented";
    [Delphi(PortStatus.Ported, Note = "Delphi's message began with \"%.\", which made formatting it fail; it begins with \"%s.\" here.")] public const string sMemberNotSingleLink = "%s.ResolveBrokenLink: Member is not a singlelink";
    [Delphi(PortStatus.Ported)] public const string sSynchInProgress = "%s.GetSynch: There is already an ongoing synch that must either be acknowledged or failed.";
    [Delphi(PortStatus.Ported)] public const string sGenerateOLLEDB = "Generate OLLE database";
    [Delphi(PortStatus.Ported)] public const string sNonexistingAction = "Nonexisting action";
}
