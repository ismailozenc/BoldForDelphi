# Source/Handles/Core

**What this folder is for:** The Core folder inside Source/Handles. See the file table below for what it holds.

**Drop list:** not on the drop list as a whole folder. Individual files may be; see the table.

**Contents:** 0 subfolder(s), 18 file(s), plus 17 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldAbstractListHandle.pas` | Unit | Method First: Sets CurrentIndex to 0. Declares TBoldAbstractListHandle. |
| `BoldCursorHandle.pas` | Unit | Method Create: Creates an instance of TBoldCursorHandle Declares TBoldCursorHandle. |
| `BoldDerivedHandle.pas` | Unit | Declares TBoldDerivedHandle. |
| `BoldExpressionHandle.pas` | Unit | Method RefersToComponent: Determine if there is a relationship to the component passed as parameter. Declares TBoldExpressionHandle. |
| `BoldFilteredHandle.pas` | Unit | Method FilterList: Filters the list passed as argument using Filter. Declares TBoldFilter, TBoldFilteredHandle. |
| `BoldHandles.pas` | Unit | Method Create: Creates an instance of TBoldAbstractSystemHandle Declares TBoldAbstractSystemHandle, TBoldElementHandle, TBoldSystemTypeInfoHandle, TBoldNonSystemHandle. |
| `BoldListHandle.pas` | Unit | Method GetMutableList: Overrides TBoldAbstractListHandle.GetMutableList Declares TBoldListHandle. |
| `BoldOclRepository.pas` | Unit | Declares TBoldOclDefinitions, TBoldOclDefinition, TBoldOclRepository. |
| `BoldOclVariables.pas` | Unit | Method GetUniqueName: Mainly intended for internal use. Declares TBoldHandleBasedExternalVariable, TBoldOclVariables, TBoldVariableTupleList, TBoldVariableTuple. |
| `BoldPlaceableSubscriber.pas` | Unit | Method Create: Constructor Declares TBoldPlaceableSubscriber. |
| `BoldReferenceHandle.pas` | Unit | Method Create: Creates an instance of TBoldReferenceHandle Declares TBoldReferenceHandle. |
| `BoldRootedHandles.pas` | Unit | Method Create: Call Create to instantiate an expression-handle at runtime. Declares TBoldRootedHandle. |
| `BoldSQLHandle.pas` | Unit | Method ClearList: Clear the list comprising the result. Declares TBoldSQLHandle. |
| `BoldSortedHandle.pas` | Unit | Method Compare: Called when the list is sorted. Declares TBoldComparer, TBoldSortedHandle. |
| `BoldSystemHandle.pas` | Unit | Method InstallOclDefinitionLookUp: Used by a repository to provide a Lookup-function for the OCL evaluator Declares TBoldSystemHandle. |
| `BoldVariableDefinition.pas` | Unit | Declares TBoldVariableDefinition. |
| `BoldVariableHandle.pas` | Unit | Declares TBoldVariableHandle. |
| `HandlesConst.pas` | Unit | No public classes; a unit of procedures and constants. |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
