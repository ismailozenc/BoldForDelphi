// Delphi: Source/BoldAwareGUI/Core/BoldGuiResourceStrings.pas
// Port map: CSharp/port/units/BoldGuiResourceStrings.md

namespace Bold;

/// <summary>Messages and captions of the Bold-aware GUI.</summary>
public static partial class BoldGuiResourceStrings
{
    [Delphi(PortStatus.Ported)] public const string SCantGetIntegerValue = "Can't get integer value from element (%s)";
    [Delphi(PortStatus.Ported)] public const string SCantSetIntegerValue = "Can't set integer value om element (%s)";
    [Delphi(PortStatus.Ported)] public const string SBoldInvalidName = "'%s' is not a valid name";
    [Delphi(PortStatus.Ported)] public const string SBoldInactiveFollowerNoRenderData = "Inactive Follower has no renderer data";
    [Delphi(PortStatus.Ported)] public const string SRepresentationDefault = "Default";
    [Delphi(PortStatus.Ported)] public const string SRepresentationShort = "Short";
    [Delphi(PortStatus.Ported)] public const string SRepresentationLong = "Long";
    [Delphi(PortStatus.Ported)] public const string SCannotDragOverMultipleObjects = "Can't DragOver multiple objects";
    [Delphi(PortStatus.Ported)] public const string SLinkAlreadyAssigned = "Link already assigned";
    [Delphi(PortStatus.Ported)] public const string SCannotChangeStateWithModifiedValue = "Can't Change State with Modified Value";
    [Delphi(PortStatus.Ported)] public const string SValueReadOnly = "Can't change value. Value is read only";
    // BoldCheckBox
    [Delphi(PortStatus.Ported)] public const string sStateNotModifiable = "%s.State: Not modifiable";
    // BoldComboBox
    [Delphi(PortStatus.Ported)] public const string sHandleMustBeReferenceHandle = "The BoldHandle property must be a TBoldReferenceHandle when BoldSelectChangeAction is bdscSetReference";
    [Delphi(PortStatus.Ported)] public const string sChangeActionCannotBeSetReference = "The BoldSelectChangeAction property can not be bdscSetReference when BoldHandle is not a TBoldReferenceHandle";
    // BoldDataSet
    [Delphi(PortStatus.Ported)] public const string sNoRecordsFound = "No records found!";
    [Delphi(PortStatus.Ported)] public const string sCannotInitializeFieldsWithoutContext = "Cannot initialize fields - dataset %s has no context";
    [Delphi(PortStatus.Ported)] public const string sInvalidFieldExpression = "Invalid field expression %s";
    [Delphi(PortStatus.Ported)] public const string sInvalidFieldName = "Invalid fieldname %s";
    // BoldEdit, BoldMemo, BoldRichEdit
    [Delphi(PortStatus.Ported)] public const string sTextNotModifiable = "%s.Text: Not modifiable";
    // BoldGrid
    [Delphi(PortStatus.Ported)] public const string sCannotCreateColumnOutsideCollection = "%s.Create: Cannot create TBoldGridColumn outside a TBoldGridColumns";
    [Delphi(PortStatus.Ported)] public const string sCaptionClassName = "Class name";
    [Delphi(PortStatus.Ported)] public const string sCaptionType = "Type";
    [Delphi(PortStatus.Ported)] public const string sCaptionAsString = "AsString";
    [Delphi(PortStatus.Ported)] public const string sClosePopup = "&Close Popup";
    // BoldGridRTColEditor
    [Delphi(PortStatus.Ported)] public const string sNewColumn = "<new column>";
    [Delphi(PortStatus.Ported)] public const string sExecuteGridNotAssigned = "%s.Execute: BoldCustomGrid not assigned";
    // BoldImage
    [Delphi(PortStatus.Ported)] public const string sUnknownFileFormat = "%s.LoadFromFile: File format unknown: File: '%s'";
    // BoldImageBitmap
    [Delphi(PortStatus.Ported)] public const string sBitMapImage = "Bitmap image";
    // BoldImageJPeg
    [Delphi(PortStatus.Ported)] public const string sJpegImage = "JPEG image";
    // Navigator hints
    [Delphi(PortStatus.Ported)] public const string SNavHintFirst = "First";
    [Delphi(PortStatus.Ported)] public const string SNavHintPrior = "Prior";
    [Delphi(PortStatus.Ported)] public const string SNavHintNext = "Next";
    [Delphi(PortStatus.Ported)] public const string SNavHintLast = "Last";
    [Delphi(PortStatus.Ported)] public const string SNavHintNew = "New";
    [Delphi(PortStatus.Ported)] public const string SNavHintDelete = "Delete";
    [Delphi(PortStatus.Ported)] public const string SNavHintMoveUp = "Move up";
    [Delphi(PortStatus.Ported)] public const string SNavHintMoveDown = "Move down";
    [Delphi(PortStatus.Ported)] public const string sUnknownDeleteMode = "%s.MapMinus: Unknown delete mode";
    [Delphi(PortStatus.Ported)] public const string sDeleteQuestion = "Delete \"%1:s\"?";
    [Delphi(PortStatus.Ported)] public const string sUnlinkQuestion = "Unlink \"%1:s\" from \"%2:s\"?";
    [Delphi(PortStatus.Ported)] public const string sRemoveQuestion = "Remove \"%1:s\" from the list?";
    // BoldPropertiesController
    [Delphi(PortStatus.Ported)] public const string sCannotSetIntegerProperty = "Could not set the integer %s property to value %s. (%s)";
    [Delphi(PortStatus.Ported)] public const string sCannotSetProperty = "Could not set the %s property to value %s. (%s)";
    // BoldStringsPropertyController
    [Delphi(PortStatus.Ported)] public const string sStringsControlledByOtherMeans = "Strings property controlled by \"%s\" changed by other means";
    // BoldCheckBoxStageControlPack
    [Delphi(PortStatus.Ported)] public const string sCannotSetValue = "%s: Can't set value";
    // BoldControlPack
    [Delphi(PortStatus.Ported)] public const string sClassHasNoSubfollowers = "%s: This class has no subfollowers";
    [Delphi(PortStatus.Ported)] public const string sNotImplemented = "%s.%s not implemented";
    [Delphi(PortStatus.Ported)] public const string sCannotModifyValue = "%s.DefaultHoldsChangedValue: Can't Modify Value";
    [Delphi(PortStatus.Ported)] public const string sReplaceNotImplemented = "%s.DefaultDragDrop: Replace not implemented yet";
    [Delphi(PortStatus.Ported)] public const string sUnknownRequestedEvent = "%s.Receive: Unknown RequestedEvent (%d)";
    [Delphi(PortStatus.Ported)] public const string sFollowerStateError = "%s.%s: Follower state error";
    [Delphi(PortStatus.Ported)] public const string sCannotDisplayInThisOrder = "%s.Display: Can not display because there is an owning follower that must be displayed before";
    [Delphi(PortStatus.Ported)] public const string sDisplayError = "%s%soccured when displaying component %s";
    [Delphi(PortStatus.Ported)] public const string sControllerNotAssigned = "%s.GetAssertedController: Controller not assigned";
    // BoldDateTimeControlPack
    [Delphi(PortStatus.Ported)] public const string sCannotGetDateTimeValueFromElement = "Can't get datetime value from element (%s)";
    [Delphi(PortStatus.Ported)] public const string sCannotSetDateTimeValueOnElement = "Can't set datetime value on element (%s)";
    // BoldFloatControlPack
    [Delphi(PortStatus.Ported)] public const string sCannotGetFloatValueFromElement = "Can't get float value from element (%s)";
    [Delphi(PortStatus.Ported)] public const string sCannotSetFloatValueOnElement = "Can't set float value on element (%s)";
    // BoldGenericListControlPack
    [Delphi(PortStatus.Ported)] public const string sCouldNotGetController = "%s%soccured when getting controller for component %s";
    // BoldListListControlPack
    [Delphi(PortStatus.Ported)] public const string sDraggedObjectsNotCleared = "%s.DefaultStartDrag: TBoldGUIHandler.DraggedObjects not cleared";
    // BoldMLRenderers
    [Delphi(PortStatus.Ported)] public const string sCannotSetStringValue = "%s.DefaultSetAsString: Can't set string value";
    // BoldNodeControlPack
    [Delphi(PortStatus.Ported)] public const string sInvalidNodeName = "%s.SetDefaultNodeDescriptionName: '%s' is not a valid name";
    // BoldStringControlPack
    [Delphi(PortStatus.Ported)] public const string sStringValidationFailedExtended = "String validation failed for %s: %2:s";
    [Delphi(PortStatus.Ported)] public const string sStringValidationFailed = "String validation failed";
    [Delphi(PortStatus.Ported)] public const string sUnknownReason = "Unknown reason";
    // B oldViewerControlPack
    [Delphi(PortStatus.Ported)] public const string sViewerNotAvailable = "Viewer not available (%s)";
    // BoldComboBoxPropertyEditors
    [Delphi(PortStatus.Ported)] public const string sComponentNotComboBox = "%s.GetContextType: Incoming component is not a BoldComboBox";
    // BoldControlPackPropertyEditors
    [Delphi(PortStatus.Ported)] public const string sInvalidPropertyValue = "Invalid property value";
    [Delphi(PortStatus.Ported)] public const string sEditNodeDescriptions = "&Edit Node Descriptions...";
    // BoldPropertiesControllerPropertyEditors
    [Delphi(PortStatus.Ported)] public const string sEditDrivenProperties = "Edit driven properties...";
    // BoldGridPropertyEditors
    [Delphi(PortStatus.Ported)] public const string sEditColumns = "&Edit Columns";
    [Delphi(PortStatus.Ported)] public const string sCreateDefaultColumns = "Create Default Columns";
    [Delphi(PortStatus.Ported)] public const string sClearAllColumns = "Clear all Columns";
    // BoldDataSetPropertyEditors
    [Delphi(PortStatus.Ported)] public const string sFieldsEditor = "Fields Editor...";
    [Delphi(PortStatus.Ported)] public const string sCreateDefaultFields = "Create default fields";
    [Delphi(PortStatus.Ported)] public const string sClearAllFields = "Clear all fields";
    // BoldAFPDefault
    [Delphi(PortStatus.Ported)] public const string sClearLinkFirst = "Clear the link first";
    [Delphi(PortStatus.Ported)] public const string sCreatingNewNotSupported = "Creating new objects like this only supported for relations to non-subclassed classes";
    [Delphi(PortStatus.Ported)] public const string sAreYouSureToDelete = "Are you sure you want to delete this object??";
    [Delphi(PortStatus.Ported)] public const string sAreYouSureToUnlink = "Are you sure you want to unlink this object??";
    [Delphi(PortStatus.Ported)] public const string sNavigate = "Navigate";
    [Delphi(PortStatus.Ported)] public const string sCreateNew = "Create new...";
    [Delphi(PortStatus.Ported)] public const string sAddExisting = "Add existing...";
    [Delphi(PortStatus.Ported)] public const string sUnlink = "Unlink";
    [Delphi(PortStatus.Ported)] public const string sDelete = "Delete";
    [Delphi(PortStatus.Ported)] public const string sHistoricObject = "Historic object - Timestamp %d";
    [Delphi(PortStatus.Ported)] public const string sClose = "Close";
    [Delphi(PortStatus.Ported)] public const string sApply = "&Apply";
    [Delphi(PortStatus.Ported)] public const string sCancel = "&Cancel";
    [Delphi(PortStatus.Ported)] public const string sOK = "&OK";
    [Delphi(PortStatus.Ported)] public const string sIllegalApplyPolicy = "Illegal applypolicy";
    [Delphi(PortStatus.Ported)] public const string sIncludeChangesInNonEmbeddedLinks = "Include changes in non embedded links";
    [Delphi(PortStatus.Ported)] public const string sObjectNotVersioned = "  This object is not versioned, no history can be shown";
    [Delphi(PortStatus.Ported)] public const string sToDisableTab_1 = "  To disable this tab, set the variable ";
    [Delphi(PortStatus.Ported)] public const string sToDisableTab_2 = "  \"BoldShowHistoryTabInAutoForms\" to false";
}
