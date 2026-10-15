// Delphi: Source/Persistence/Core/PersistenceConsts.pas
// Port map: CSharp/port/units/PersistenceConsts.md

namespace Bold;

/// <summary>Messages of the persistence layer.</summary>
public static partial class PersistenceConsts
{
    [Delphi(PortStatus.Ported)] public const string sGenerateSchema = "Generate Database Schema";
    [Delphi(PortStatus.Ported)] public const string sFetchingObjects = "Fetching objects";
    [Delphi(PortStatus.Ported)] public const string sRetrievingIDs = "Retrieving object IDs";
    [Delphi(PortStatus.Ported)] public const string sUpdatingDB = "Updating database";
    [Delphi(PortStatus.Ported)] public const string sPMTimeForTimeStampNotSupported = "PMTimeForTimestamp not supported by %s";
    [Delphi(PortStatus.Ported)] public const string sPMTimeStampForTimeNotSupported = "PMTimestampForTime not supported by %s";
    [Delphi(PortStatus.Ported)] public const string sNotAllowedOnActiveHandle = "%s Not allowed on active PersistenceHandle";
    [Delphi(PortStatus.Ported)] public const string sOptimisticLockingFailedForNObjects = "Optimistic locking failed for %d objects";
    [Delphi(PortStatus.Ported)] public const string sCannotGenerateWhenHandleIsActive = "Can not generate database schema when the PersistenceHandle is Active";
    [Delphi(PortStatus.Ported)] public const string sModelComponentMissing = "%s.CreateDataBaseSchema: Can not create database schema for %s without a Model-component";
    [Delphi(PortStatus.Ported)] public const string sCreateSchema = "Create DatabaseSchema";
    [Delphi(PortStatus.Ported)] public const string sSchemaGenerationAborted = "Generation of Database Schema Aborted (%s)";
    [Delphi(PortStatus.Ported)] public const string sCannotGetPControllerWithoutModel = "%s.GetPersistenceController: Can not get a PersistenceController without a Model";
    [Delphi(PortStatus.Ported)] public const string sCannotActivate_UpgraderMismatch = "%s.SetActive: Cannot activate, there is an UpgraderHandle but the Model does not have UseModelVersion true";
    [Delphi(PortStatus.Ported)] public const string sClockStringFormatError = "%s.SetClockLogGranularity: string is not properly formatted. Should be <hrs>:<mins>:<secs>.<msecs>";
    [Delphi(PortStatus.Ported)] public const string sCannotSetWhenHandleIsActive = "%s.%s: Cannot set this property when the handle (%s) is active";
    [Delphi(PortStatus.Ported)] public const string sSQLDatabaseConfigMissing = "%s: Unable to %s. There is no SQLDatabaseConfig available";
    [Delphi(PortStatus.Ported)] public const string sNotActive = "%s.%s: Not Active";
    [Delphi(PortStatus.Ported)] public const string sNoDatabaseAdapterAvailable = "%s: Unable to %s. There is no DatabaseAdapter available";
    [Delphi(PortStatus.Ported)] public const string sCannotActivateWithoutDBAdapter = "%s.SetActive: Can not set persistence handle to active since it is not connected to a database adapter";
    [Delphi(PortStatus.Ported)] public const string sObjectNotInFile = "%s.PMFetch: Trying to fetch an object that does not exist in file (%s)";
    [Delphi(PortStatus.Ported)] public const string sSQLNotSpokenHere = "This filehandler does not understand SQL, ignoring condition and orderby...";
    [Delphi(PortStatus.Ported)] public const string sUnknownConditionType = "Unknown conditiontype: %s";
    [Delphi(PortStatus.Ported)] public const string sPreconditionsNotSupported = "%s.PMUpdate: Preconditions (%s) not supported in this component";
    [Delphi(PortStatus.Ported)] public const string sModelRequired = "%s.CreatePersistenceController: Unable to create, model is missing.";
    [Delphi(PortStatus.Ported)] public const string sXMLParseError = "Error reading/parsing XML file";
    [Delphi(PortStatus.Ported)] public const string sEnqueuerNotEnabled = "Call to %s failed. Propagator Enqueuer not enabled";
    [Delphi(PortStatus.Ported)] public const string sClientNotRegistered = "Call to %s failed. Client not registered with propagator";
    [Delphi(PortStatus.Ported)] public const string sInvalidParameter = "Call to %s failed. Invalid parameter";
    [Delphi(PortStatus.Ported)] public const string sClientNotReceivingEvents = "Call to %s OK, but client is currently not receiving events";
    [Delphi(PortStatus.Ported)] public const string sCallFailed = "Call to %s failed. Error: %s";
    [Delphi(PortStatus.Ported)] public const string sCannotAcquireLock = "%s.%s: Cannot acquire Database Lock";
    [Delphi(PortStatus.Ported)] public const string sLockManagerNotAssigned = "%s.GetLockManager: LockManager not assigned";
    [Delphi(PortStatus.Ported)] public const string sUpgradeNotSupported = "%s.Create: The SystemPersistenceMapper does not support object upgrading!";
    [Delphi(PortStatus.Ported)] public const string sObjectIDListNotExact = "%s.UpgradeObjectIdList: ObjectIdlist not Exact";
    [Delphi(PortStatus.Ported)] public const string sObjectIDListNotHomogenous = "%s.UpgradeObjectIdList: ObjectIdlist not homogenous";
    [Delphi(PortStatus.Ported)] public const string sDBMustBeOpened = "%s.UpgradeObjects: The database must be opened first";
    [Delphi(PortStatus.Ported)] public const string sBoldModelNotAssigned = "%s.CreatePersistenceController: BoldModel not Assigned";
    [Delphi(PortStatus.Ported)] public const string sPropagatorHandleNotAssigned = "%s.PropagatorHandle not assigned";
    [Delphi(PortStatus.Ported)] public const string sPropertyMoved = "%s.%s has been moved to component (%s.%s). Old value was \"%s\"";
}
