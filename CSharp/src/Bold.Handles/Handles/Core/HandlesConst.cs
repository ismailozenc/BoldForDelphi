// Delphi: Source/Handles/Core/HandlesConst.pas
// Port map: CSharp/port/units/HandlesConst.md

namespace Bold;

/// <summary>Messages of the handles and the handle actions.</summary>
public static partial class HandlesConst
{
    [Delphi(PortStatus.Ported)] public const string sIllegalTypeSelected = "%s.GetStaticBoldType: Only lists and attributes are allowed as types (expr: %s)";
    [Delphi(PortStatus.Ported)] public const string sNoSystemHandle = "%s.%s: %s has no SystemHandle";
    [Delphi(PortStatus.Ported)] public const string sCircularReference = "%s.%s: %s can not be linked to %s. Circular reference";
    [Delphi(PortStatus.Ported)] public const string sNameNotUnique = "Invalid Name: %s Not Unique";
    // BoldAbstractListHandle
    [Delphi(PortStatus.Ported)] public const string sNoPreviousElement = "%s: No previous element";
    [Delphi(PortStatus.Ported)] public const string sNoNextElement = "%s: No next element";
    [Delphi(PortStatus.Ported)] public const string sNoCurrentElement = "%s.RemoveCurrentElement: No current element";
    [Delphi(PortStatus.Ported)] public const string sCannotRemoveCurrentFromImmutable = "%s: Can not remove current Element from an immutable list (in %s)";
    [Delphi(PortStatus.Ported)] public const string sCurrentElementNotBoldObject = "%s.CurrentBoldObject: Current element is not a TBoldObject";
    // BoldSQLHandle
    [Delphi(PortStatus.Ported)] public const string sSystemHandleNotActive = "%s.ExecuteSQL: Systemhandle is not active";
    [Delphi(PortStatus.Ported)] public const string sCannotCreateListDueToInvalidType = "%s.EnsureList: Unable to create list (%s), can't find valid type";
    // BoldHandlePropEditor
    [Delphi(PortStatus.Ported)] public const string sComnponentNotOCLDefinition = "%s.GetContextType: Component is not a TBoldOclDefinition";
    // BoldManipulators
    [Delphi(PortStatus.Ported)] public const string sCannotSetMultiLinkFromValue = "Can't set Multilink from value";
    [Delphi(PortStatus.Ported)] public const string sCannotSetObjectFromValue = "Can't set Object from value";
    [Delphi(PortStatus.Ported)] public const string sElementLacksTypeInfo = "%s.DefaultTagForElement: Element lacks type information";
    [Delphi(PortStatus.Ported)] public const string sUnknownMapping = "Unknown mapping: %s";
    // BoldSystemHandle
    [Delphi(PortStatus.Ported)] public const string sCannotUpdateDatebaseWithoutSystem = "%s.UpdateDatabase (%s) cannot be invoked without a system";
    [Delphi(PortStatus.Ported)] public const string sUnableToActivateSystemWithoutTypeInfoHandle = "Unable to activate a system without a SystemTypeInfoHandle (%s)";
    [Delphi(PortStatus.Ported)] public const string sUnableToFindTypeInfoHandle = "Unable to find a SystemTypeInfo. Possible misstakes: Forgot to connect the SystemTypeInfoHandle to a Model, CreationOrder of the forms/dataModules";
    [Delphi(PortStatus.Ported)] public const string sUnableToActivateSystem = "Unable to activate system. Initialization errors in StaticSystemTypeInfo:%s%s";
    [Delphi(PortStatus.Ported)] public const string sPersistenceHandleChangedOnRunningSystem = "PersistenceHandle was changed on a running system.";
    [Delphi(PortStatus.Ported)] public const string sModelChangedOnRunningSystem = "The model changed in a running system";
    [Delphi(PortStatus.Ported)] public const string sRegionDefinitionsRemovedFromRunningSystem = "RegionDefinitions were removed from a running system.";
    [Delphi(PortStatus.Ported)] public const string sPanicShutDown = "%s%sSystem Panic shut down. %d objects discarded";
    // BoldVariableHandle
    [Delphi(PortStatus.Ported)] public const string sOnlyListsAndAttributeTypesAllowed = "%s.GetStaticBoldType: Only lists and attributes are allowed as types (expr: %s)";
    [Delphi(PortStatus.Ported)] public const string sValueTypeNameInvalid = "%s.Getvalue: The ValueTypeName of %s does not seem to valid (%s)";
    // BoldXMLProducers
    [Delphi(PortStatus.Ported)] public const string sManipulatorNotAssigned = "%s.%s: BoldManipulator property not assigned";
    // BoldLockingHandles
    [Delphi(PortStatus.Ported)] public const string sCannotActivateWithoutListener = "%s.Activate: Cannot activate Locking without a listener. Set the Listener property of the %0:s";
    [Delphi(PortStatus.Ported)] public const string sCannotActivateWithoutLockManager = "%s.Activate: Cannot activate Locking without a LockManager. Set the LockManager property of the %0:s";
    // BoldHandles
    [Delphi(PortStatus.Ported)] public const string sNotAllowedOnActiveHandle = "%s: Not allowed to change the systemTypeInfoHandle on an active system";
    // BoldActions
    [Delphi(PortStatus.Ported)] public const string sUpdateDB = "Update DB";
    [Delphi(PortStatus.Ported)] public const string sOpenSystem = "Open system";
    [Delphi(PortStatus.Ported)] public const string sCloseSystem = "Close system";
    [Delphi(PortStatus.Ported)] public const string sThereAreDirtyObjects = "There are dirty objects. Do you want to save them before shutting down";
    [Delphi(PortStatus.Ported)] public const string sClosingWithDirtyObjects = "Closing system with dirty objects";
    // BoldRootedHandles
    [Delphi(PortStatus.Ported)] public const string sInternalRootHandle_CircRef = "%s: Cannot set handle - circular reference";
    // BoldListActions
    [Delphi(PortStatus.Ported)] public const string sNext = "Next";
    [Delphi(PortStatus.Ported)] public const string sPrev = "Prev";
    [Delphi(PortStatus.Ported)] public const string sFirst = "First";
    [Delphi(PortStatus.Ported)] public const string sLast = "Last";
    [Delphi(PortStatus.Ported)] public const string sUnknownDeleteMode = "Unknown delete mode";
    [Delphi(PortStatus.Ported)] public const string sDelete = "Delete";
    [Delphi(PortStatus.Ported)] public const string sAddNew = "Add New";
    [Delphi(PortStatus.Ported)] public const string sMoveUp = "Move up";
    [Delphi(PortStatus.Ported)] public const string sMoveDown = "Move Down";
    // BoldUndoActions
    [Delphi(PortStatus.Ported)] public const string sSetCheckPoint = "Set Check-point";
    [Delphi(PortStatus.Ported)] public const string sUndo = "Undo";
    [Delphi(PortStatus.Ported)] public const string sRedo = "Redo";
    // BoldCursorHandle
    [Delphi(PortStatus.Ported)] public const string sIndexOutOfBounds = "%s.SetCurrentIndex: Index out of bounds. Valid range from -1 to %d. Attempted to set %d";
    // BoldOCLVariables
    [Delphi(PortStatus.Ported)] public const string sIllegalCharsInName = "Invalid variable name, only alphanum characters and underscore valid";
    // BoldExpressionHandle
    [Delphi(PortStatus.Ported)] public const string sDeriveAndSubscribeFailed = "%s.DeriveAndSubscribe (%s): Failed with message: %s";
    // BoldOCLRepository
    [Delphi(PortStatus.Ported)] public const string sRepositoryHasNoSystemHandle = "*** OclRepository %s%s has no SystemHandle";
    [Delphi(PortStatus.Ported)] public const string sSystemHandleHasNoTypeInfo = "*** SystemHandle of OclRepository %s%s has no TypeInfo";
}
