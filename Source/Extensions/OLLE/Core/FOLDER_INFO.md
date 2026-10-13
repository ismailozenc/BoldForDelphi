# Source/Extensions/OLLE/Core

**What this folder is for:** Object Lending Library Extension (OLLE): lends objects between Bold databases, with global ids, holding and returning objects, and repairing broken links.

**Drop list:** not dropped. It was removed by mistake in the COM step and restored. It has no COM code; the C# port comes after SQL persistence (M5).

**Contents:** 0 subfolder(s), 9 file(s), plus 3 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldOLLEController.pas` | Unit | Method ConnectToPersistenceHandle: Tells the OLLEController which persistence handle to use Declares TBoldPHandleMimic, TBoldOLLEController. |
| `BoldOLLEDistributableObjectHandlers.pas` | Unit | Method AddToMapping: private Declares TBoldDistributableObjectHandler, TBoldForeignObjectHandler, TBoldOwnObjecthandler, TBoldBrokenLinkResolver and 1 more. |
| `BoldOLLEHandles.pas` | Unit | Method Destroy: destructor Declares TBoldOLLEHandle. |
| `BoldOLLEdmmain.dfm` | dfm | Form layout (design-time). |
| `BoldOLLEdmmain.pas` | Unit | Declares TdmOLL. |
| `DistributableInfo.inc` | inc | Include file, shared compiler directives or code fragments. |
| `DistributableInfo.pas` | Unit | No public classes; a unit of procedures and constants. |
| `DistributableInfo_Interface.inc` | inc | Include file, shared compiler directives or code fragments. |
| `OlleConsts.pas` | Unit | No public classes; a unit of procedures and constants. |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
