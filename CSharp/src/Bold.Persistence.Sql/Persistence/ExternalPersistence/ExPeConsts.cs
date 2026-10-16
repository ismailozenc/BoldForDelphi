// Delphi: Source/Persistence/ExternalPersistence/ExPeConsts.pas
// Port map: CSharp/port/units/ExPeConsts.md

namespace Bold;

/// <summary>Messages of the external persistence handles.</summary>
public static class ExPeConsts
{
    [Delphi(PortStatus.Ported)] public const string sKeyTypeNotAutoHandled = "Keytype not handled automatically: %s.%s";
    //BoldAbstractExternalPersistenceHandle
    [Delphi(PortStatus.Ported)] public const string sValueNotCurrency = "%s.CurrencyValueForObject: The value (%s) is not a currency";
    [Delphi(PortStatus.Ported)] public const string sValueNotDate = "%s.DateValueForObject: The value (%s) is not a date";
    [Delphi(PortStatus.Ported)] public const string sValueNotInteger = "%s.IntValueForObject: The value (%s) is not an integer";
    [Delphi(PortStatus.Ported)] public const string sKeyNotInteger = "%s.KeyIntForObject: The key is not an integer";
    [Delphi(PortStatus.Ported)] public const string sKeyNotString = "%s.KeyStringForObject: The key is not a string";
    [Delphi(PortStatus.Ported)] public const string sValueNotString = "%s.StringValueForObject: The value (%s) is not is not a string";
    //BoldAbstractPartiallyExternalPC
    [Delphi(PortStatus.Ported)] public const string sNotSupportedWithMultipleKeys = "%s.ExternalKeysToInternalSQL: Automatic SQL-generation only supported for classes with 1 (one) external key (class %s has multiple)";
    [Delphi(PortStatus.Ported)] public const string sNotSupportedWithNoKeys = "%s.ExternalKeysToInternalSQL: Automatic SQL-generation only supported for classes with 1 (one) external key (class %s has none!)";
    [Delphi(PortStatus.Ported)] public const string sNoSuchRole = "%s.FindMoldRoleByName: There is no role called %s in class %s";
    //BoldAbstractPartiallyExternalPH
    [Delphi(PortStatus.Ported)] public const string sInvalidClassName = "%s.GetObjectIdByExternalKey: Invalid class name (%s)";
    //BoldExternalPersistenceControllerDataSet
    [Delphi(PortStatus.Ported)] public const string sUnknownDataType = "Unknown data type";
    [Delphi(PortStatus.Ported)] public const string sLinkToUnconfiguredTable = "External link to unconfigured table %s";
    [Delphi(PortStatus.Ported)] public const string sUnknownVarTypeLoadingID = "Unknown vartype when loading an external ID for multilink %s.%s";
    [Delphi(PortStatus.Ported)] public const string sUnknownVarTypeLoadingSingleID = "Unknown vartype when loading an external ID for Singlelink %s.%s";
    [Delphi(PortStatus.Ported)] public const string sUnknownVarTypeLoadingObject = "Unknown vartype when loading an external ID for %s";
    [Delphi(PortStatus.Ported)] public const string sRoleHasNoColumnNames = "Role %s does not have any column names!";
    [Delphi(PortStatus.Ported)] public const string sRoleEndCountMismatch = "Role %s does not have an equal amount of columns on both ends!";
    [Delphi(PortStatus.Ported)] public const string sObjectNoLongerInDB = "Object no longer exists in database";
    //BoldExternalPersistenceControllerEventDriven
    [Delphi(PortStatus.Ported)] public const string sAssignKeyValueRequiresOneKey = "AssignKeyValue only supported automatically for classes with one external key: %s";
    [Delphi(PortStatus.Ported)] public const string sCreateObjectsNotAllowed = "Creating new objects of type %s not supported/allowed";
    [Delphi(PortStatus.Ported)] public const string sDeleteObjectsNotAllowed = "Deleting objects of type %s not supported/allowed";
    [Delphi(PortStatus.Ported)] public const string sReadObjectNotImplementedForClass = "Event ReadObject is not implemented for class %s";
    [Delphi(PortStatus.Ported)] public const string sGetExternalKeyNotImplementedForClass = "Getting external keys for class %s not implemented";
    [Delphi(PortStatus.Ported)] public const string sModifyObjectsNotAllowed = "Modifying objects of type %s not supported/allowed";
    //BoldExternalPersistenceHandlesReg
    [Delphi(PortStatus.Ported)] public const string sEditConfiguration = "Edit configuration";
    [Delphi(PortStatus.Ported)] public const string sCannotCreateNameNow = "Can not create name for eventhandler. Assign an expressionname for the config-item first";
    [Delphi(PortStatus.Ported)] public const string sBoldModelNotAssigned = "BoldModel is not assigned!";
}
