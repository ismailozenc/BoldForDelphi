# Source/Propagator/Enterprise

**What this folder is for:** The Enterprise folder inside Source/Propagator. See the file table below for what it holds.

**Drop list:** not on the drop list as a whole folder. Individual files may be; see the table.

**Contents:** 0 subfolder(s), 50 file(s).

## Files

| File | Kind | What it does |
|---|---|---|
| `BoldAbstractOutputQueueHandler.pas` | Unit | Declares TBoldAbstractOutputQueueHandler. |
| `BoldAdvancedPropagator.pas` | Unit | Declares TBoldAdvancedPropagator. |
| `BoldAdvancedPropagatorCOM.pas` | Unit | Declares TBoldAdvancedPropagatorCOM, TBoldPropagatorFactory. *(drop list: C-14)* |
| `BoldClientHandler.pas` | Unit | Declares TBoldClientHandler, TBoldClientInfoList, TBoldClientInfo. |
| `BoldClientHandlerCOM.pas` | Unit | Declares TBoldClientHandlerCOM, TBoldClientHandlerCOMFactory, TBoldClientHandlerThreadedCOMFactory. *(drop list: C-14)* |
| `BoldClientHandlerExportHandle.pas` | Unit | Declares TBoldClientHandlerExportHandle. |
| `BoldClientNotifierHandler.pas` | Unit | Declares TBoldClientNotifierHandler, TBoldClientNotifier, TBoldClientNotifierPool, TBoldClientNotifierData. |
| `BoldClientQueue.pas` | Unit | Declares TBoldClientQueue. |
| `BoldComServiceRegister.pas` | Unit | Declares TBoldComServiceRegister. *(drop list: C-14)* |
| `BoldEnqueuer.pas` | Unit | Declares TBoldEnqueuer, TBoldExternalEvent, TBoldExternalEventThreadSafeObjectQueue. |
| `BoldEnqueuerCOM.pas` | Unit | Declares TBoldEnqueuerCOM, TBoldEnqueuerThreadedCOMFactory, TBoldEnqueuerCOMFactory. *(drop list: C-14)* |
| `BoldEnqueuerExportHandle.pas` | Unit | Declares TBoldEnqueuerExportHandle. |
| `BoldIndexList.pas` | Unit | Declares TBoldIndexList, TBoldAbstractMultiIndexedList, TBoldIndexListClass. |
| `BoldIndexedList.pas` | Unit | Declares TBoldClientIDList, TBoldEventNameList, TBoldExternalEventHashTable, TBoldExternalEventIndex. |
| `BoldListNodes.pas` | Unit | Declares TBoldMultiIndexedListNode, TBoldAbstractLinkNode, TBoldIndexNode, TBoldEventNameIndexNode. |
| `BoldLockList.pas` | Unit | Declares TBoldLockNameIndexNode, TBoldLockNameList, TBoldLockList, TBoldLockNode and 4 more. |
| `BoldLockManager.pas` | Unit | Declares TBoldLockManager. |
| `BoldLockManagerAdmin.pas` | Unit | Declares TBoldLockManagerAdmin. |
| `BoldLockManagerAdminCOM.pas` | Unit | Declares TBoldLockManagerAdminCOM, TBoldLockManagerAdminComFactory. *(drop list: C-14)* |
| `BoldLockManagerAdminExportHandle.pas` | Unit | Declares TBoldLockManagerAdminComExportHandle. |
| `BoldLockManagerCOM.pas` | Unit | Declares TBoldLockManagerCOM, TBoldLockManagerCOMFactory. *(drop list: C-14)* |
| `BoldLockManagerExportHandle.pas` | Unit | Declares TBoldLockManagerComExportHandle. |
| `BoldOutputQueueHandler.pas` | Unit | Declares TBoldOutputQueueHandler. |
| `BoldPriorityListEnlister.pas` | Unit | Declares TBoldAbstractPriorityListEnlister, TBoldPriorityListEnlister, TBoldClientQueueInfo. |
| `BoldPropagator.cfg` | cfg | File of type .cfg. |
| `BoldPropagator.dof` | dof | File of type .dof. |
| `BoldPropagator.dpr` | dpr | Delphi program source (application entry point). |
| `BoldPropagator.ini` | ini | Configuration file. |
| `BoldPropagator.rc` | rc | Compiled or binary artifact (rc). |
| `BoldPropagator.res` | res | Compiled or binary artifact (res). |
| `BoldPropagatorApplication.pas` | Unit | No public classes; a unit of procedures and constants. |
| `BoldPropagatorCleanup.pas` | Unit | Declares TBoldCleanUpSubscriber, TBoldCleanUpThread, TBoldCleanupSubscriber. |
| `BoldPropagatorDll.dpr` | dpr | Delphi program source (application entry point). |
| `BoldPropagatorDll.ini` | ini | Configuration file. |
| `BoldPropagatorDll.res` | res | Compiled or binary artifact (res). |
| `BoldPropagatorMainForm.ddp` | ddp | Form layout (design-time). |
| `BoldPropagatorMainForm.dfm` | dfm | Form layout for PropagatorMainForm (TPropagatorMainForm). |
| `BoldPropagatorMainForm.pas` | Unit | Declares TPropagatorMainForm, TClientGuiInfo, TClientGUIInfoList. *(drop list: O-1)* |
| `BoldPropagatorServer.pas` | Unit | Declares TBoldPropagatorServer, TBoldPropagatorServerClass. |
| `BoldPropagatorService.dpr` | dpr | Delphi program source (application entry point). |
| `BoldPropagatorService.ini` | ini | Configuration file. |
| `BoldPropagatorService.res` | res | Compiled or binary artifact (res). |
| `BoldPropagatorSubscriptions.pas` | Unit | Declares TBoldSubscriptionList, TBoldSubscriptionNode, TBoldSortedIntegerList. |
| `BoldPropagatorUIManager.pas` | Unit | Declares TUIManager. *(drop list: O-1)* |
| `BoldServerHandlesDataMod.dfm` | dfm | Form layout for dmServerHandles (TdmServerHandles). |
| `BoldServerHandlesDataMod.pas` | Unit | Declares TdmServerHandles. *(drop list: O-2)* |
| `BoldServicePropagatorUnit.ddp` | ddp | Form layout (design-time). |
| `BoldServicePropagatorUnit.dfm` | dfm | Form layout for BoldPropagatorSvc (TBoldPropagatorSvc). |
| `BoldServicePropagatorUnit.pas` | Unit | Declares TBoldPropagatorSvc. |
| `BoldSubscriptionHandler.pas` | Unit | Declares TBoldSubscriptionHandler, TBoldDequeuer. |

---

Descriptions are generated from the source: unit summaries come from the `.UnitDoc` files, and class names from the declarations. The folder purpose line is a summary written for this folder, not a line-by-line review.

<!-- BEGIN C# PORT: generated by Bold.PortTool, do not edit this block -->
## C# port

Where the units of this folder go in the C# port. Each unit page lists the unit's types, methods and constants with their port status and links to the C# code and its tests.

| Unit | C# | Milestone |
|---|---|---|
| [BoldAbstractOutputQueueHandler](../../../CSharp/port/units/BoldAbstractOutputQueueHandler.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldAbstractOutputQueueHandler.cs (not started) | M9 Multi-user |
| [BoldAdvancedPropagator](../../../CSharp/port/units/BoldAdvancedPropagator.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldAdvancedPropagator.cs (not started) | M9 Multi-user |
| [BoldAdvancedPropagatorCOM](../../../CSharp/port/units/BoldAdvancedPropagatorCOM.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldClientHandler](../../../CSharp/port/units/BoldClientHandler.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldClientHandler.cs (not started) | M9 Multi-user |
| [BoldClientHandlerCOM](../../../CSharp/port/units/BoldClientHandlerCOM.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldClientHandlerExportHandle](../../../CSharp/port/units/BoldClientHandlerExportHandle.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldClientNotifierHandler](../../../CSharp/port/units/BoldClientNotifierHandler.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldClientNotifierHandler.cs (not started) | M9 Multi-user |
| [BoldClientQueue](../../../CSharp/port/units/BoldClientQueue.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldClientQueue.cs (not started) | M9 Multi-user |
| [BoldComServiceRegister](../../../CSharp/port/units/BoldComServiceRegister.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldEnqueuer](../../../CSharp/port/units/BoldEnqueuer.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldEnqueuer.cs (not started) | M9 Multi-user |
| [BoldEnqueuerCOM](../../../CSharp/port/units/BoldEnqueuerCOM.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldEnqueuerExportHandle](../../../CSharp/port/units/BoldEnqueuerExportHandle.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldIndexList](../../../CSharp/port/units/BoldIndexList.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldIndexList.cs (not started) | M9 Multi-user |
| [BoldIndexedList](../../../CSharp/port/units/BoldIndexedList.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldIndexedList.cs (not started) | M9 Multi-user |
| [BoldListNodes](../../../CSharp/port/units/BoldListNodes.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldListNodes.cs (not started) | M9 Multi-user |
| [BoldLockList](../../../CSharp/port/units/BoldLockList.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldLockList.cs (not started) | M9 Multi-user |
| [BoldLockManager](../../../CSharp/port/units/BoldLockManager.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldLockManager.cs (not started) | M9 Multi-user |
| [BoldLockManagerAdmin](../../../CSharp/port/units/BoldLockManagerAdmin.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldLockManagerAdmin.cs (not started) | M9 Multi-user |
| [BoldLockManagerAdminCOM](../../../CSharp/port/units/BoldLockManagerAdminCOM.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldLockManagerAdminExportHandle](../../../CSharp/port/units/BoldLockManagerAdminExportHandle.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldLockManagerCOM](../../../CSharp/port/units/BoldLockManagerCOM.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldLockManagerExportHandle](../../../CSharp/port/units/BoldLockManagerExportHandle.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldOutputQueueHandler](../../../CSharp/port/units/BoldOutputQueueHandler.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldOutputQueueHandler.cs (not started) | M9 Multi-user |
| [BoldPriorityListEnlister](../../../CSharp/port/units/BoldPriorityListEnlister.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldPriorityListEnlister.cs (not started) | M9 Multi-user |
| [BoldPropagatorApplication](../../../CSharp/port/units/BoldPropagatorApplication.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldPropagatorApplication.cs (not started) | M9 Multi-user |
| [BoldPropagatorCleanup](../../../CSharp/port/units/BoldPropagatorCleanup.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldPropagatorCleanup.cs (not started) | M9 Multi-user |
| [BoldPropagatorMainForm](../../../CSharp/port/units/BoldPropagatorMainForm.md) | not ported: propagator user interface; the server is a Worker Service |  |
| [BoldPropagatorServer](../../../CSharp/port/units/BoldPropagatorServer.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldPropagatorServer.cs (not started) | M9 Multi-user |
| [BoldPropagatorSubscriptions](../../../CSharp/port/units/BoldPropagatorSubscriptions.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldPropagatorSubscriptions.cs (not started) | M9 Multi-user |
| [BoldPropagatorUIManager](../../../CSharp/port/units/BoldPropagatorUIManager.md) | not ported: propagator user interface |  |
| [BoldServerHandlesDataMod](../../../CSharp/port/units/BoldServerHandlesDataMod.md) | not ported: COM; ASP.NET Core and C# interfaces replace it |  |
| [BoldServicePropagatorUnit](../../../CSharp/port/units/BoldServicePropagatorUnit.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldServicePropagatorUnit.cs (not started) | M9 Multi-user |
| [BoldSubscriptionHandler](../../../CSharp/port/units/BoldSubscriptionHandler.md) | Bold.Propagator.Server: Propagator/Enterprise/BoldSubscriptionHandler.cs (not started) | M9 Multi-user |
<!-- END C# PORT -->
