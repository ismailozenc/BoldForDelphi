# Source/Persistence/ExternalPersistence

**What this folder is for:** The ExternalPersistence folder inside Source/Persistence. See the file table below for what it holds.

**Drop list:** not on the drop list as a whole folder. Individual files may be; see the table.

**Contents:** 0 subfolder(s), 18 file(s), plus 15 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldAbstractExternalPersistenceController.pas` | Unit | Method EndUpdates: Called after any updates are made to the internal and external persistence store Declares TBoldAbstractExternalPersistenceController. |
| `BoldAbstractExternalPersistenceHandle.pas` | Unit | Method SetActive: Changes the active state of the handle and calls OnActivate/OnDeactivate Declares TBoldAbstractExternalPersistenceHandle. |
| `BoldAbstractPartiallyExternalPC.pas` | Unit | Method ExternalKeysToInternalSQL: Helper function to generate SQL fragments for external keys Declares TBoldAbstractPartiallyExternalPC. |
| `BoldAbstractPartiallyExternalPH.pas` | Unit | Method GetObjectIdByExternalKeyInteger: Helper function to get the internal ID of a known external object Declares TBoldAbstractpartiallyExternalPH. |
| `BoldExternalPersistenceConfigItemDataSet.pas` | Unit | Method Create: Creates an instance of TBoldExternalPersistenceConfigDataSetItem Declares TBoldExternalPersistenceConfigDataSetItem, TBoldExternalPersistenceConfigDataSetItems. |
| `BoldExternalPersistenceControllerConfig.pas` | Unit | Declares TBoldExternalPersistenceConfigItem, TBoldExternalPersistenceConfigItems. |
| `BoldExternalPersistenceControllerDataSet.pas` | Unit | Method AssignKeyToObject: Assignes an external object Id to it's respective members Declares TBoldExternalPersistenceControllerDataSet. |
| `BoldExternalPersistenceControllerEventDriven.pas` | Unit | Method AssignKeyToObject: Assignes an external object Id to it's respective members Declares TBoldExternalPersistenceControllerEventDriven. |
| `BoldExternalPersistenceControllerSQL.pas` | Unit | Method CreateObjects: Creates objects in the external database Declares TBoldExternalPersistenceControllerSQL, TFetchObjectList, TAbstractFetchObject, TFetchObjectClass and 5 more. |
| `BoldExternalPersistenceHandleDataSet.pas` | Unit | Method Create: Creates an instance of TBoldExternalPersistenceHandleDataSet Declares TBoldExternalPersistenceHandleDataSet. |
| `BoldExternalPersistenceHandleEventDriven.pas` | Unit | Method Create: Creates an instance of TBoldExternalPersistenceHandleEventDriven Declares TBoldExternalPersistenceHandleEventDriven. |
| `BoldExternalPersistenceHandleSQL.pas` | Unit | Method SetActive: Changes the active state of the handle Declares TBoldExternalPersistenceHandleSQL. |
| `BoldExternalPersistenceHandleSQLPropEditor.dfm` | dfm | Form layout for BoldExternalPersistenceHandleSQLPropEditorForm (TBoldExternalPersistenceHandleSQLPropEditorForm). |
| `BoldExternalPersistenceHandleSQLPropEditor.pas` | Unit | Declares TBoldExternalPersistenceHandleSQLPropEditorForm. *(drop list: I-8)* |
| `BoldExternalPersistenceHandles.rc` | rc | Compiled or binary artifact (rc). |
| `BoldExternalPersistenceHandlesReg.pas` | Unit | Declares TBoldExternalPersistenceHandleEditor, TBoldExternalPersistenceHandleEventProperty, TBoldExternalPersistenceHandleSQLPropEdit. Registers design-time components or editors (IDE only). *(drop list: I-8)* |
| `BoldExternalPersistenceSupport.pas` | Unit | Declares TBoldObjectPersistenceAdapter, TBoldObjectPersistenceAdapterClass. |
| `ExPeConsts.pas` | Unit | No public classes; a unit of procedures and constants. |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
