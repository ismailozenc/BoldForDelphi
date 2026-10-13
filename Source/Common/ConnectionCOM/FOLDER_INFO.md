# Source/Common/ConnectionCOM

**What this folder is for:** The ConnectionCOM folder inside Source/Common. See the file table below for what it holds.

**Drop list:** this folder (or items in it) is on the drop list: C-5. See DROP_LIST.md.

**Contents:** 0 subfolder(s), 6 file(s), plus 3 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldComClient.pas` | Unit | Declares TBoldComClientSubscriber, TBoldComClientInterface, TBoldComClientConnection, TBoldComClientEventQueue and 5 more. *(drop list: C-5)* |
| `BoldComConnection.dpr` | dpr | Delphi program source (application entry point). *(drop list: C-5)* |
| `BoldComConnection.pas` | Unit | Method OnServerEvent: Receives events from the server. *(drop list: C-5)* |
| `BoldComConnection.res` | res | Compiled or binary artifact (res). *(drop list: C-5)* |
| `BoldComConnection.tlb` | tlb | Compiled or binary artifact (tlb). *(drop list: C-5)* |
| `BoldComServer.pas` | Unit | Method CreateObject: Requests for a new instance of ClassName. Declares TBoldComServerSubscriber, TBoldComServerEventQueue, TBoldComServerInterface, TBoldComServerProvider and 8 more. *(drop list: C-5)* |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
