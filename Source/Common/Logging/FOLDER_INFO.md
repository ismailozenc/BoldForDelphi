# Source/Common/Logging

**What this folder is for:** The Logging folder inside Source/Common. See the file table below for what it holds.

**Drop list:** not on the drop list as a whole folder. Individual files may be; see the table.

**Contents:** 0 subfolder(s), 11 file(s), plus 6 .UnitDoc file(s) documenting units here.

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldLogForm.ddp` | ddp | Form layout (design-time). |
| `BoldLogForm.dfm` | dfm | Form layout for BoldLogForm (TBoldLogForm). |
| `BoldLogForm.pas` | Unit | Declares TBoldLogForm. *(drop list: W-16)* |
| `BoldLogHandler.pas` | Unit | Method Create: Creates an instance of TBoldLogHandler Declares TBoldLogHandler, TBoldLogHandlerClass, TBoldLogReceiverSubscriber. |
| `BoldLogHandlerForm.pas` | Unit | Declares TBoldLogHandlerReceiver. *(drop list: W-16)* |
| `BoldLogHandlerSimple.pas` | Unit | Declares TBoldSimpleLogReceiver. |
| `BoldLogReceiverInterface.pas` | Unit | Method Clear: Clears the log |
| `BoldSmallLogFrame.ddp` | ddp | Form layout (design-time). |
| `BoldSmallLogFrame.dfm` | dfm | Form layout for BoldLogFrame (TBoldLogFrame). |
| `BoldSmallLogFrame.pas` | Unit | Method CreateSmallLogForm: Creates a small form that shows logstatus and progress Declares TBoldLogFrame, TExposedShape. *(drop list: W-16)* |
| `BoldThreadSafeLog.pas` | Unit | Declares TBoldLogger, TFileLogging. |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.
