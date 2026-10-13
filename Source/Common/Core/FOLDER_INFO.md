# Source/Common/Core

**What this folder is for:** The Core folder inside Source/Common. See the file table below for what it holds.

**Drop list:** not on the drop list as a whole folder. Individual files may be; see the table.

**Contents:** 0 subfolder(s), 7 file(s), plus 6 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldAggeregates.pas` | Unit | Method NonDelegatingAddRef: Increases the reference count. Declares TBoldAggregateableObject, TBoldContainedObject, TBoldAggregatedObject. |
| `BoldBase.pas` | Unit | Method QueryInterface: Requestes/queries for an an interface. Declares TBoldInterfacedObject, TBoldRefCountedObject, TBoldNonRefCountedObject, TBoldMemoryManagedObject and 1 more. |
| `BoldCommonConst.pas` | Unit | No public classes; a unit of procedures and constants. |
| `BoldContainers.pas` | Unit | Base class for container related exceptions. Declares TBoldContainer, TBoldArray, TBoldPointerArray, TBoldObjectArray and 2 more. |
| `BoldDefs.pas` | Unit | The mother of all exceptions used inside Bold. |
| `BoldStreams.pas` | Unit | No public classes; a unit of procedures and constants. |
| `BoldThreadSafeQueue.pas` | Unit | Method Dequeue: Dequeue object. Declares TBoldThreadSafeQueue, TBoldThreadSafeQueueEntry, TBoldThreadSafeStringQueue, TBoldThreadSafeObjectQueue and 4 more. |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
