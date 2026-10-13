# Source/ObjectSpace/BORepresentation

**What this folder is for:** The BORepresentation folder inside Source/ObjectSpace. See the file table below for what it holds.

**Drop list:** not on the drop list as a whole folder. Individual files may be; see the table.

**Contents:** 0 subfolder(s), 14 file(s), plus 13 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldAttributes.pas` | Unit | Method FreeContent: Releases the content from the attribute. Declares TBAString, TBANumeric, TBAInteger, TBAByte and 32 more. |
| `BoldDerivedValueSet.pas` | Unit | Declares TBADerivedValueSetValueList. |
| `BoldDomainElement.pas` | Unit | Method CanCommit: If the element allows the current system-transaction to commit. Declares TBoldDomainElement, TBoldDomainElement_Proxy, TBoldDomainElementClass, TBoldDomainElementCollection and 2 more. |
| `BoldElementList.pas` | Unit | Declares TBoldElementList, TBoldElementListFactory, TBoldIndexableElementList, TBoldElementListController. |
| `BoldExternalObjectSpaceEventHandler.pas` | Unit | Method ObjectDeleted: Default handler for messages with deleted objects Declares TBoldExternalObjectSpaceEventHandler. |
| `BoldLinks.pas` | Unit | Method AddToOtherEnd: Adds self to other end Declares TBoldDirectSingleLinkController, TBoldIndirectSingleLinkController, TBoldMultiLinkController, TBoldDirectMultiLinkController and 8 more. |
| `BoldMLAttributes.pas` | Unit | Method StartModify: Handles aggregated members Declares TBAMLValueSetValue, TBAMLValueSetValueList, TBAMLValueSet, TBALanguage and 4 more. |
| `BoldObjectListControllers.pas` | Unit | Declares TBoldObjectListController, TBoldClassListController, TBoldClassList_Proxy, TBoldObjectList_Proxy. |
| `BoldObjectSpaceLists.pas` | Unit | Declares TBoldObjectAttributeIndexList, TBoldObjectLocatorList, TBoldMembersHashIndex, TBoldMembersKey. |
| `BoldOptimisticLockingSupport.pas` | Unit | Declares TBoldOptimisticLockHandler. |
| `BoldSystem.pas` | Unit | Raised when accessing the value of an attribute that is NULL Declares TBoldObjectLocator, TBoldSystemLocatorList, TBoldSystem, TBoldObject and 36 more. |
| `BoldSystemOldValuehandler.pas` | Unit | Declares TBoldOldValueHandler. |
| `BoldSystemPersistenceHandler.pas` | Unit | Method UpdateDatabaseWithList: Save list of objects to the database. Declares TBoldSystemPersistenceHandler, TBoldTimeMappingCache. |
| `BoldTypeList.pas` | Unit | Declares TBoldTypeListFactory, TBoldTypeList, TBoldTypeListController. |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
