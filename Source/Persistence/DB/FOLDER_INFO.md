# Source/Persistence/DB

**What this folder is for:** The DB folder inside Source/Persistence. See the file table below for what it holds.

**Drop list:** not on the drop list as a whole folder. Individual files may be; see the table.

**Contents:** 0 subfolder(s), 9 file(s), plus 7 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldAbstractDatabaseAdapter.pas` | Unit | Declares TBoldAbstractDatabaseAdapter. |
| `BoldAbstractPersistenceHandleDB.pas` | Unit | Method AddModelEvolutionInfoToDatabase: Adds tables to the database to support ModelEvolution Declares TBoldAbstractPersistenceHandleDB. |
| `BoldDBActions.pas` | Unit | Method AliasCreated: private Declares TBoldGenerateSchemaAction. |
| `BoldDBInterfaces.pas` | Unit | Method SupportsDefaultColumnValues: Indicates whether the underlying database supports default values when creating columns using SQL-statements Declares TBoldDataSetWrapper, TBoldDatabaseWrapper, TBoldFieldWrapper, TBoldFieldWrapperClass and 2 more. |
| `BoldPersistenceControllerDefault.pas` | Unit | Declares TBoldPersistenceControllerDefault. |
| `BoldPersistenceHandleDB.pas` | Unit | Declares TBoldPersistenceHandleDB. |
| `BoldPersistenceHandleDB.rc` | rc | Compiled or binary artifact (rc). |
| `BoldPersistenceHandleDB_deprecated.pas` | Unit | Declares TBoldDBPersistenceHandle. |
| `BoldPersistenceHandleDBreg.pas` | Unit | Declares TBoldPersistenceHandleDBEditor. Registers design-time components or editors (IDE only). |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
