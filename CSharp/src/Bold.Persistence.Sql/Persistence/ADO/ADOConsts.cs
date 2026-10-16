// Delphi: Source/Persistence/ADO/ADOConsts.pas
// Port map: CSharp/port/units/ADOConsts.md

namespace Bold;

/// <summary>Messages of the ADO adapter.</summary>
public static partial class ADOConsts
{
    [Delphi(PortStatus.Ported)] public const string sAdapterNotConnected = "%s.GetDatabaseInterface: The adapter is not connected to an ADO connection";
    [Delphi(PortStatus.Ported)] public const string sCreatedNewAdapter = "Created a new DatabaseAdapterADO";
    [Delphi(PortStatus.Ported)] public const string sCanOnlyTransferToADOAdapter = "The persistencehandle is connected to a %s, properties can only be transfered to a TBoldDatabaseAdapterADO";
    [Delphi(PortStatus.Ported)] public const string sCreatedNewDB = "Created a new ADODatabase";
    [Delphi(PortStatus.Ported)] public const string sCouldNotTransferConnectionString = "Connection string settings could not be transferred to the new ADO connection: ";
    [Delphi(PortStatus.Ported)] public const string sTransferManually = "Please transfer these manually!";
}
