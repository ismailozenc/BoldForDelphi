<!-- A copy of the drop list (v16, 2026-10-30), which is also published as a page. Keep the two, and the plan, in step. -->

# Bold for Delphi → C# port: Drop List

Status: v16, 2026-10-30
Source: `ismailozenc/BoldForDelphi`, branch `master`, shallow clone at `/home/claude/ismailozenc/boldfordelphi`
Line counts are `.pas` lines only. Test every entry marked **verify** with a repo-wide grep before deleting it from the port.

## Done so far

| Change | Branch | Commit |
|---|---|---|
| Standalone tools `Tools/BoldClientifier`, `Tools/BoldUpgrader`, `Tools/DBConvert` (T-1 to T-3) | `remove-dropped` | `0066730` |
| Delphi package files (27 `.dpk`) | `remove-dropped` | `cd0f2d3` |
| Dead database drivers and the Oracle-BDE unit (D-1 to D-11) | `remove-dead-db-drivers` | `edfdd0c` |
| Examples that depend on a dropped feature (41 folders) | `remove-unrelated-examples` | `0a78bec` |
| Non-Windows code: CLX GUI and CLX environment (W-1, W-2), `BoldEnvironmentAllowBothUseVCL`, Linux `IFDEF` branches | `remove-cross-platform` | `4413db8` |
| COM and DCOM distribution: 84 units and their files (C-1 to C-18, except the units listed as still needed in section 5) | `remove-com-dcom` | ca94f93 |
| OLLE restored: `Source/Extensions/OLLE/Core` (6 units) came back; it was removed in the COM step by mistake | `CSharpPortSource` | e930283 |
| Windows-only units with no remaining users: `Common/SupportWin/BoldWinUtils` (W-4), `Common/Support/BoldDirectoryTraverser` (W-13), `Common/Template/BoldTemplate` (W-14), `Samples/Unicode` (W-17) | `remove-windows-api` | 4645ae1 |
| ModelMaker link (M-1) and ECO III generator (M-2), plus `BoldUMLDelphiSupport` (W-10), whose only user was the ModelMaker importer | `remove-modelmaker-ecoiii` | fffa5ee |
| Rational Rose code: `Common/Rose98`, `Common/Rose2000`, `UMLModel/ModelLinks/Rose98` (R-1 to R-3), and the Rose link component in `BoldUMLModelDataModule` | `remove-rose` | e39c68b |
| All examples: the whole `examples/` folder (CBuilder, Delphi, html) | `remove-examples` | d8054f2 |
| Samples (`Source/Samples`, DECIDE-4) and `Unassigned/patches/MultiUnitCodeGenPatch.pas` (DECIDE-5) | `CSharpPortSource` | 5f44b47 |
| Connection base classes `Common/Connection` and `Common/ConnectionHandles` (DECIDE-2) | `CSharpPortSource` | 9a1c8b1 |
| SOAP persistence (`Persistence/SOAP`, `Common/SOAP`), the HTTP persistence transport (`Persistence/HTTP`, `Common/HTTP`, which was SOAP over HTTP), SOAP-only XML helpers, the ASP and HTTPPMapper examples, the XML dispatcher COM and IDE remnants, and their component bitmaps | `remove-soap` | 2144ea1 |
| Links to the C# port, not a removal: a comment on every `unit` line naming its C# file, and a "C# port" block in every `FOLDER_INFO.md` (no Delphi line numbers change) | `CSharp` | d6d63d6 |
| Units that stay in the branch but are not ported (100, each with its reason) are listed in `CSharp/port/port-map.json` | `CSharp` | 8998e7e |
| `Persistence/ADO/BoldPersistenceHandleADO` is not ported (O-5): it is deprecated in Delphi Bold. Listed in `port-map.json` | `CSharp` | 74272b9 |
| Symbols dropped inside ported units (M6 to M8), each with a note on its `[Delphi]` marker: the COM code generation of `BoldGen` and the template managers (it goes with the COM object space); `GetPalette` of `TBoldImage` and the bitmap and JPEG view adapters (Windows GDI palettes; GDI+ draws without them); `TBoldExposedCustomGrid` in `BoldAFPDefault` and `ModeType` in `BoldOclPropEditor` (used only by code that is commented out in Delphi) | `CSharp` | 9c905ee, 3a9fb15, c0c594e, b251618 |
| The first example of our own: `CSharp/examples/Building/BldOwn`, Delphi Bold's Buildings and Owners application on WinForms and SQLite, with a test that runs it (M8) | `CSharp` | 2ed07ed |
| M9 multi-user: the Enterprise propagator, the lock manager, the listener, the snooper and the UDP broadcaster are ported. DCOM is replaced by a SignalR hub on ASP.NET Core (`TBoldPropagatorHub`, D8) with client handles that need no package. Not ported: the propagator forms and data module (O-1, O-2) and the COM wrappers and export handles (section 5), each listed in `port-map.json`. Dropped inside ported units, each with a note: the type libraries' dispinterfaces and coclasses, `TBoldListenerCOMFactory`, the service's Windows service control, the form callbacks of `TBoldAdvancedPropagator`, and the obsolete form-file properties of the snooper and ID adder handles | `CSharp` | abf966b, 0769b77, d9ea7c9 |

Removed folders keep their history in git. Restore any of them from the commit before the removal.

## How to read this

| Decision | Meaning |
|---|---|
| **DROP** | Do not port. Behaviour is not needed, or the platform has no C# equivalent. |
| **DROP-verify** | Probably not needed, but has at least one reference we haven't traced. Confirm first. |
| **REPLACE** | Not ported as Pascal. The behaviour is needed and gets a C# equivalent. |
| **KEEP** | Port it. Listed here only where it could look like a drop candidate. |
| **DECIDE** | Open question. Needs your call. |

## 0. Standing decisions (from conversation)

- Drop all non-Windows features (CLX, Linux, Kylix).
- Drop dead database drivers. Keep only what ADO.NET can support.
- Drop Rational Rose COM (Rose98 and Rose2000).
- Drop all COM and DCOM distribution.
- Standalone tools in `Tools/`: keep only `PropagatorAnalyzer`.
- Built-in tools (`Source/` tooling): keep all, except `ObjectSpace/IDE`, which needs a C# equivalent (REPLACE).
- Delphi IDE integration: keep only the parts that translate or are needed, as logic rather than IDE code.
- Samples and unassigned code: drop, unless a sample is needed as a reference for the port.
- All of `examples/` (CBuilder, Delphi, html): drop. You asked for examples to be removed entirely. We will write our own examples once the main functionality runs.

---

## 1. Standalone tools (`Tools/`)

All three dropped tools are removed. See "Done so far".

**Kept from `Tools/`:** `Tools/PropagatorAnalyzer` (265 lines). Reads thread lock/unlock/ID logs and reports lock-order dependencies. Useful for deadlock analysis.

---

## 2. Windows-API-only code

| ID | Unit / folder | Lines | What it does | Why it should not be converted | Decision |
|---|---|---|---|---|---|
| W-3 | `Common/Environment/BoldEnvironmentIDE.pas` | 14 | IDE environment hook. | Delphi IDE. | **DROP** |
| W-5 | `Common/Support/BoldRegistry.pas` | 173 | Wraps the Windows registry. Used by About, license, Getting Started, the UML model editor, and the OCL property editor. | Registry is Windows-only and a poor place for settings. Settings are replaced by `appsettings.json` (see R-table). | **Ported instead** (M8): `TBoldRegistry` over the current user's key, Windows only. The OCL editor keeps its settings with it, as in Delphi. |
| W-6 | `Common/UtilsGUI/BoldCursorGuard.pas` | 40 | Sets the wait cursor and restores it on scope exit. | Replaced by `using` with `Cursor.Current`. | **Ported instead** (M8): `IBoldCursorGuard` is an `IDisposable` that sets `Cursor.Current`. |
| W-7 | `Common/Support/BoldMath.pas` | 65 | Math helper functions. | `System.Math`. | **Ported instead** (M3), for the persistence mappers and the log handler. |
| W-8 | `Common/Support/BoldRev.pas` | 78 | Build version constants. | Replaced by assembly version info. | **Nothing to port** (M6): the unit is empty on the branch, its version functions are commented out. Version information comes from the assemblies. |
| W-9 | `Common/Support/BoldCommonBitmaps.pas` | 84 | Creates bitmap resources. | Windows bitmap handling. Icons and images become embedded resources. | **Ported** (M8) on embedded resources: the bitmaps of the grid, the navigator and the edit button come from the repository's `Images`. |
| W-10 | `Common/UML/BoldUMLDelphiSupport.pas` | 87 | Delphi-specific naming and type support for UML. | Delphi naming rules. | **DROP** |
| W-11 | `Common/Support/BoldMemoryManager.pas` | 481 | Custom memory manager. | The GC replaces it. Verified for `BoldFreeStandingValues` (its member array is a managed array in the port, M2); `BoldSystem` (M3) is the last user to check. | **DROP-verify** |
| W-12 | `Common/Support/BoldSharedStrings.pas` | 209 | String interning to reduce memory. | The GC and .NET string handling replace it. | **Ported in part** (M1): the interface stays; the counts are estimates, since the GC frees the strings. |
| W-15 | `Common/UtilsGUI/BoldQueryUserDlg.pas` | 83 | Modal query dialog. | Replaced by standard WinForms dialogs. | **Ported instead** (M8) as a WinForms dialog; the persistence layer asks through a callback that Bold.WinForms fills. |
| W-16 | `Common/Logging/BoldLogForm.pas`, `BoldLogHandlerForm.pas`, `BoldSmallLogFrame.pas` | 584 | VCL log viewer forms and frames. | VCL UI. The logic is kept (see REPLACE-table, logging). | **Ported instead** (M8) as WinForms forms, because the XMI link and the UML editor use them. |

**Outcome in the port.** Every unit of this table that kept code used is now ported (see the Decision column), so none is waiting on a caller any more; `BoldMemoryManager` (W-11) is replaced by the garbage collector. The list below is the reasoning at the time:

- **W-5 `BoldRegistry`:** used by `BoldUMLModelEdit`, `BoldOclPropEditor` and the IDE About/Getting Started code. Port those to `appsettings.json` first.
- **W-6 `BoldCursorGuard`:** used by `BoldUMLModelEditForm`. Port to `using` with `Cursor.Current`.
- **W-7 `BoldMath`:** used by the OCL, PMapper, and grid units. Port to `System.Math`.
- **W-8 `BoldRev`:** used by the code generator templates. Port to assembly version info.
- **W-9 `BoldCommonBitmaps`:** used by `BoldGrid`, `BoldEdit` and `BoldNavigator`. Port to embedded resources.
- **W-11 `BoldMemoryManager`** (verify): `BoldBase`, `BoldIndex`, `BoldCommonConst` and `BoldFreeStandingValues` are ported without it (M1, M2). Only `BoldSystem` (M3) remains. Remove after that is ported.
- **W-12 `BoldSharedStrings`:** used by `BoldMeta`, `BoldORed`, `BoldStringList`, and others. Port to .NET strings.
- **W-15 `BoldQueryUserDlg`:** used by the PMapper SQL and validator. Port to WinForms dialogs.
- **W-16 log forms:** `BoldLogForm` is imported by the XMI importer and exporter. `BoldSmallLogFrame` is used by the UML editor form. Remove those `uses` first.
- **W-3 `BoldEnvironmentIDE`:** still listed, no code uses it. It is an IDE hook, not Windows API, so it is outside this step.

---

## 3. Rational Rose (Rose98 and Rose2000)

Removed. See "Done so far". The replacement is still needed: see REPLACE in section 11 and item 1 in section 15.

---

## 4. ModelMaker and ECO III

**Status: removed** on branch `remove-modelmaker-ecoiii`. Both the ModelMaker link (M-1) and the ECO III generator (M-2) are gone.

**ModelMaker integration is planned for later.** See section 15.

---

## 5. COM and DCOM distribution

**Status: removed** on branch `remove-com-dcom` (commit `ca94f93`). 84 COM-only units and their files are gone: the object space COM layer, the COM client GUI and handles, the COM connection layers, the Enterprise propagator COM wrappers, the COM handles and concurrency control, and the SOAP and MSXML type libraries where nothing else needed them.

**Correction:** C-18 (`Extensions/OLLE`) was listed here as "OLE" by mistake. OLLE is the Object Lending Library Extension, which lends objects between Bold databases, and it has no COM code. Its six core units were restored on `CSharpPortSource` (commit `e930283`), so 78 COM units remain removed. Only the OLLE design-time editor stays removed.

**SOAP removed** on branch `remove-soap` (commit `2144ea1`). The SOAP persistence group is gone, and so is the HTTP persistence transport (`Persistence/HTTP`, `Common/HTTP`). That transport was SOAP over HTTP, so it could not stay on its own. `BoldPersistenceOperationXMLStreaming` went with it. It was kept as shared logic under DECIDE-1, but its only users were SOAP and HTTP, so it was removed.

**Still in the tree, because kept code uses them.** Each one stays until its caller is ported. Replacements are shown for each group.

| Group | Units | Still used by | Replacement |
|---|---|---|---|
| XML parsing (MSXML) | `MSXML_TLB` | `Common/Support/BoldXMLStreaming`, `Handles/XML/BoldXMLProducers`, `ObjectSpace/Ocl/BoldOclLightWeightNodes`, and others | `System.Xml` and `System.Xml.Linq` |
| Propagator and locking interfaces (TLB) | `BoldPropagatorInterfaces_TLB`, `BoldLockingSupportInterfaces_TLB` | `Persistence/Propagation` (listener, snooper), `ConcurrencyControl/Common`, `ObjectSpace/PessimisticLocking`, `Propagator/Common` | C# interfaces. **Ported in M9.** |
| COM client, server, and connection | `BoldComClient`, `BoldComServer`, `BoldComClientHandles`, `BoldComServerHandles`, `BoldComConnection`, `BoldComEventQueue`, `BoldComObj`, `BoldComThreads`, `BoldApartmentThread`, `BoldThreadedComObjectFactory`, `BoldComConst`, `BoldComUtils`, `BoldComAdapter`, `BoldAwareGUI` control packs, `BoldComConst`, the Enterprise propagator server, `Persistence/Propagation/BoldListenerThread`, and the code generator templates | HTTP client and server, plus the new propagation transport. **Propagation done in M9:** the propagator server and the listener thread are ported and talk through the SignalR hub; the remote persistence transport comes with M10. |
| COM object space | `BoldComObjectSpace`, `BoldComObjectSpaceAdapters`, `BoldComObjectSpace_TLB` | `MoldModel/CodeGenerator` templates (`.dfm`) | Remove with the generator's COM templates |
| COM propagator and lock manager | `BoldListenerCOM` (**kept and ported in M9**: the listener object itself, without its COM factory), `BoldPropagatorHandleCOM`, `BoldAdvancedPropagatorCOM`, `BoldClientHandlerCOM`, `BoldEnqueuerCOM`, `BoldLockManagerCOM`, `BoldLockManagerAdminCOM`, `BoldComServiceRegister`, `BoldClientHandlerExportHandle`, `BoldEnqueuerExportHandle`, `BoldLockManagerExportHandle`, `BoldLockManagerAdminExportHandle` | Enterprise propagator server (`BoldPropagatorServer`), the IDE registration in `Propagator/IDECOM` | Propagator server rewritten on the new transport. **Done in M9:** `TBoldPropagatorHub` (SignalR) and `TBoldPropagatorConnection` on the server, `TBoldSignalRPropagatorHandle` and the lock manager handles on the clients. |

**Note:** The abstract client and server base classes in `Common/Connection` and `Common/ConnectionHandles` were removed on `CSharpPortSource` (DECIDE-2). The COM-blocked Enterprise propagator server (`BoldServerHandlesDataMod.pas`) still refers to `BoldServerHandles`, so the Delphi propagator will not build; the C# port replaces it (M9) and does not port the data module (O-2).

---

## 6. Dead database drivers

All ten drivers and the Oracle-BDE unit are removed. See "Done so far".

**Kept, with changes:**
- `Persistence/ADO`: kept as an ADO.NET layer (see REPLACE-table).
- `Persistence/DB`: its `TDataSet` abstraction is replaced by ADO.NET `DbDataAdapter` and `DbCommand` (see REPLACE-table).

---

## 7. Delphi IDE integration

Rule: drop IDE code (Open Tools API, property editors, component editors, registration units). Keep logic only where it translates. The only translating logic is the attribute wizard, which is in `ObjectSpace/IDE` and handled in section 9.

| ID | Unit / folder | Lines | What it does | Why it should not be converted | Decision |
|---|---|---|---|---|---|
| I-1 | `Common/IDE` (18 units) | 3,327 | Bold expert, IDE menus, OTA support, OTA file handler, About and license forms, Getting Started expert and form, `BoldReg.pas`, property and component editors, `BoldDefsDT.pas` (help file constants), `BoldTextStream.pas` (no references), web connection registration. | Open Tools API and VCL design-time. None of it has a C# equivalent that needs porting. | **DROP** |
| I-2 | `Common/IDECOM` (6 files) | 742 | COM design-time editors and registration, XML dispatcher editors. | COM and IDE. | **DROP** |
| I-3 | `BoldAwareGUI/IDE` (7 files) | 1,617 | Property editors for Bold controls. | Design-time VCL. | **DROP** |
| I-4 | `ClientGuiCom/IDE` (4 files) | 502 | COM design-time editors. | COM and IDE. | **DROP** |
| I-5 | `Handles/IDE` (5 files) | 378 | Handle property editor and registration for locking, manipulators, and XML. | Design-time. | **DROP** |
| I-6 | `MoldModel/IDE` (2 files) | 138 | Model and type-name registration. | Design-time. The type-name dictionary itself is kept (`MoldModel/TypeNameDictionary`). | **DROP** |
| I-7 | `Persistence/IDE` (6 files), `Persistence/IDEUDP` (1 file) | 229 | Persistence handle registration and the UDP broadcaster registration. | Design-time. The handles they register are kept. | **DROP** |
| I-8 | `UMLModel/Ide` (2 files) | 200 | Model editor and handle registration. | Design-time. | **DROP** |
| I-9 | `Persistence/ExternalPersistence/BoldExternalPersistenceHandleSQLPropEditor.pas`, `BoldExternalPersistenceHandlesReg.pas` | 306 | SQL property editor and registration for external persistence. | Design-time. | **DROP** |
| I-10 | `ObjectSpace/IDE/BoldComponentValidatorIDE.pas` | 259 | Adds IDE menu items for component validation. | IDE menus. The validator itself is in `ObjectSpace/UtilsGUI/BoldComponentValidator.pas` and is kept. | **DROP** |
| I-11 | `ObjectSpace/IDE/BoldWSimpleMenuWizard.pas`, `BoldWProjectWizard.pas`, `BoldOTACodeGen.pas`, `BoldExternalObjectSpaceEventHandlerReg.pas` | ~600 | Wizard menus, project creation via OTA, OTA unit generation, event-handler registration. | Open Tools API. The generation they do is covered by the code generator (kept). | **DROP** |
| I-12 | `Samples/IDE` (2 files), `Samples/BoldCheckListBox/*Reg.pas` | ~150 | Sample IDE registration and editors. | Samples and IDE. | **DROP** |

---

## 8. Samples and unassigned code

| ID | Unit / folder | Lines | Notes | Decision |
|---|---|---|---|---|
| S-1 | `Samples/` (all folders, 22 files) | 4,260 | Example components and apps. Reference only. Worth keeping as a reference: `Actions`, `NewObjectInterceptor`, `SystemDebugger`, `SystemComparer`, `ModelLoader`, `FormSaver`. | **DROP** (removed from the port on `CSharpPortSource`, commit `5f44b47`; reference copy on `master`) |
| S-2 | `Unassigned/AutoInc` | 195 | No references. Not in a package. | **DROP** |
| S-3 | `Unassigned/UMLPlugins/BoldAppEventsRestorer.pas` | 208 | Restores VCL application events. | VCL. |
| S-4 | `Unassigned/patches/OCLEditorPatch.pas` | 81 | Patches the OCL editor. | Not in any package. |
| S-5 | `Unassigned/BoldUMLModelEditorDebuggerPlugin.pas` | — | Debugger plugin for the model editor. | Not in any package. |
| S-6 | `Unassigned/patches/MultiUnitCodeGenPatch.pas` | 96 | Multi-unit output for the code generator. | **DROP** (removed on `CSharpPortSource`, commit `5f44b47`) |

---

## 9. Other drops and reviews

| ID | Unit / folder | Lines | What it does | Why it should not be converted | Decision |
|---|---|---|---|---|---|
| O-1 | `Propagator/Enterprise/BoldPropagatorMainForm.pas`, `BoldPropagatorUIManager.pas`, `BoldLowEndPropagatorMainForm.pas` | 1,067 | VCL forms for the propagator server and low-end propagator. | VCL UI. The propagator logic is kept. | **DROP** (UI only; done, listed in `port-map.json`) |
| O-2 | `Propagator/Enterprise/BoldServerHandlesDataMod.pas` | 94 | VCL data module for the propagator. | VCL. | **DROP** (done, listed in `port-map.json`) |
| O-3 | `Propagator/Common/BoldObjectMarshaler.pas` | 84 | Marshals objects for the COM propagator. Referenced only by `Bold.dpk` and `BoldPropagator.dpr`. | Marshalling is a COM concern. | **KEEP** (decided in M9): the port's listener thread hands the propagator to its own thread through it, where Delphi's listener thread marshaled the interface with COM calls of its own. Ported as a holder of the object, since nothing crosses an apartment in .NET. |
| O-4 | `Samples/` and `Unassigned/` folders not listed above | — | See section 8. | **DROP** |
| O-5 | `Persistence/ADO/BoldPersistenceHandleADO.pas` | — | An ADO persistence handle built on `BoldPersistenceHandleDB_deprecated`. | Deprecated in Delphi Bold itself. `TBoldPersistenceHandleDB` with `TBoldDatabaseAdapterADO` does the same, and is ported (M5). | **DROP** (done) |

---

## 10. Things that look droppable but are kept

These are on the keep list, so they're not lost by accident. Each one is a feature a reader might assume is dead.

| Unit / folder | Lines | Why it's kept |
|---|---|---|
| `Persistence/ExternalPersistence` (16 files) | 4,493 | Maps Bold classes onto an existing SQL schema. Large feature. Keep the logic, switch to ADO.NET. |
| `Persistence/File` (2 files) | 412 | File persistence, including XML. Portable. |
| `Persistence/System` (2 files) | 624 | Core persistence controller. |
| `Persistence/Propagation/BoldSnooper.pas`, `BoldSnooperHandle.pas`, `BoldIDAdder*.pas` | ~500 | DB change detection and ID allocation for propagation. Keep, with a new transport. |
| `Persistence/UDPPropagator` (2 files) | 347 | UDP broadcast propagation. Cross-platform, so it translates directly. |
| `Propagator/LowEnd/BoldLowEndPropagatorClasses.pas` | 203 | In-process propagator. The non-COM alternative. Keep. |
| `Propagator/Enterprise/BoldLockManager.pas`, `BoldLockList.pas`, `BoldClientNotifierHandler.pas`, `BoldSubscriptionHandler.pas` | ~1,300 | Core locking and subscription logic. Only the COM wrappers are dropped. |
| `ObjectSpace/Unloader/BoldUnloader.pas` (183), `Handles/UnLoader/BoldUnloaderHandle.pas` (215) | ~400 | Unloads unused objects to bound memory. Needed for production. |
| `Persistence/ObjectUpgrading` (4 files) | 748 | Upgrades stored objects to a new model version on load. |
| `PMapper/DbEvolutor` (3 files) | 2,142 | Schema migration. |
| `MoldModel/CodeGenerator` (4 files) | 2,450 | Code generator and its templates. |
| `MoldModel/Bld/BoldBld.pas` | 1,150 | Reads and writes the `.bld` model format. |
| `MoldModel/UtilsGUI/BoldTypeNameEditor.pas` | 265 | Type-name editor. Built-in tool. |
| `ObjectSpace/UtilsGUI/BoldComponentValidator.pas` | 122 | Validates components at run time. |
| `ObjectSpace/UtilsGUI/BoldOclPropEditor.pas` | 765 | OCL editor. Built-in tool. |
| `ObjectSpace/UtilsGUI/BoldOCLGraphicRTDebug.pas` | 131 | Run-time OCL debugger graph. Built-in tool. |
| `ObjectSpace/UtilsGUI/BoldTypeNameSelector.pas` | 280 | Type-name selector. Built-in tool. |
| `UMLModel/Editor/*` (10 files) | 4,714 | Model editor. Built-in tool. Port the logic, rebuild the forms in WinForms. |
| `UMLModel/ModelLinks/XMI` | ~1,100 | XMI import and export. Replaces Rose. |
| `UMLModel/Plugins` (except ECO III) | ~1,150 | UML plugin framework and plugins, including `BoldCodePlugins` (GUID generator) and `BoldDbPlugins` (generic DB plugin). Both are shipped in `BoldUml.dpk` but not referenced by code. |
| `Common/Support/BoldFileHandler.pas` | 380 | File handling. Used by file persistence. |
| `Common/Support/BoldExternalizedReferences.pas` | 143 | Referenced directly by `ObjectSpace/BORepresentation/BoldSystem.pas`. Core. |
| `Common/Support/BoldTemplateExpander.pas` | 522 | Template expansion used by the code generator. |
| `Common/Queue/*` (3 files) | 642 | Event queue. Core. Replaced in form only (see REPLACE-table). |
| `Common/Logging/BoldLogHandler.pas`, `BoldThreadSafeLog.pas`, `BoldLogHandlerSimple.pas` | ~730 | Logging logic. Keep. Forms are dropped (W-16). |

---

## 11. REPLACE: needed behaviour, C# equivalent

These are **not** drops. The behaviour is needed and gets a C# equivalent.

| Unit / folder | Lines | Behaviour | C# equivalent |
|---|---|---|---|
| `ObjectSpace/IDE/AttributeWizard` (about 19 units) | ~4,000 | Scans Delphi source and generates Bold-aware classes with attributes. **You asked for this to be replaced, not dropped.** | Roslyn source generator, or T4 templates. `BoldWScanner` becomes a Roslyn scanner. `BoldWAdmTemplates` is kept as template source. |
| `BoldAwareGUI` (VCL controls and control packs) | 25,122 | Bold-aware VCL controls: grid, tree view, combo box, edit, and more. | WinForms controls and a data-binding layer. **Ported in M8:** the followers, controllers and renderers in `Bold.AwareGui` (no WinForms, tested on every platform) and the controls in `Bold.WinForms`. The grid is a `DataGridView`; the Bold data set runs on the RTL's `TDataSet` emulation. |
| `BoldAwareGUI/FormGen` (`BoldAFP*`) | 2,482 | Automatic form generation from the model. | Generated WinForms forms. **Ported in M8** as in Delphi: the auto forms are built at run time from the element's type, and a form of one's own can be plugged in (`TBoldPlaceableAFP`). |
| `Persistence/DB` (8 files) | 2,173 | `TDataSet`-based persistence base layer. | ADO.NET `DbDataAdapter` and `DbCommand`. **Ported in M5.** |
| `Persistence/ADO` (5 files) | 1,228 | ADO persistence. | ADO.NET (SQL Server and other providers). **Ported in M5**, except `BoldPersistenceHandleADO` (O-5). |
| `Persistence/HTTP` and `Common/HTTP` | ~620 | Persistence over HTTP. **Removed on `remove-soap`**, because the transport was SOAP over HTTP. | **Required, not built:** ASP.NET Core server and `HttpClient` transport. See DECIDE-1. |
| `Common/SupportWin/BoldWinINet.pas` | 195 | WinINet HTTP calls. | `HttpClient`. |
| `Common/MsXml` (use, not the TLB) | — | XML parsing for XMI. | `System.Xml` and `System.Xml.Linq`. |
| `Common/Support` collections: `BoldIndex`, `BoldHashIndexes`, `BoldIndexableList`, `BoldNamedValueList`, `BoldStringList`, `BoldCollections`, `BoldContainers` | ~2,900 | Delphi-style indexed collections. Used in 19+ files. **Behaviour must be kept, not just the names.** | `Dictionary<,>`, `SortedDictionary<,>`, `List<T>`, `HashSet<T>`. |
| `Common/Support/BoldPriorityQueue.pas` | 224 | Priority queue. Used by propagator. | `PriorityQueue<TElement,TPriority>` (.NET 6+). |
| `Common/Support/BoldSorter.pas` | 41 | Sort comparison helper. | `IComparer<T>`. |
| `Common/Support/BoldGuard.pas` | 219 | Guarded resource release. Used in 69 files. | `IDisposable` and `using`. |
| `Common/Support/BoldLoggableCriticalSection.pas` | 48 | Logged lock. | `lock` plus the logger. |
| `Common/SupportWin/BoldThread.pas` | 207 | Thread that notifies on completion. | `Thread` or `Task` with a completion event. **Ported in M9** (in `Bold.Propagation`) on the RTL's `TThread`, whose Windows message queue is a managed queue. |
| `Common/Core/BoldThreadSafeQueue.pas` | 457 | Thread-safe queue. | `System.Threading.Channels.Channel<T>`. **Ported in M9** as Bold's own queue, which keeps its "not empty" notification. |
| `Common/Queue/*` | 642 | Event and dequeue dispatch. | `BlockingCollection<T>` or `Channel<T>`. |
| `Common/Support/BoldBase64.pas` | 330 | Base64 encoding. | `Convert.ToBase64String`. |
| `Common/Support/BoldIsoDateTime.pas` | 80 | ISO date parsing. | `DateTime` with the `O` format. |
| `Common/Support/BoldPerformanceCounter.pas` | 241 | Performance counter. | `Stopwatch` and `EventCounter`. |
| `Common/Support/BoldRegistry.pas` (settings use only) | — | Settings storage. | `appsettings.json` and `ConfigurationBuilder`. **Not needed:** the unit was ported (W-5). |
| `Common/Support/BoldStreams.pas` | 18 | Stream helpers. | `System.IO.Stream`. |
| `Common/Environment/BoldEnvironmentVCL.pas` | 202 | VCL app-event queue. | WinForms application-event handling. **Ported in M8:** the display queue runs on `Application.Idle`. |
| `Common/Logging` (logic) | ~730 | Logging. | `ILogger` (Microsoft.Extensions.Logging) or Serilog. **Ported as Bold's own log handler** (M3); the log forms are WinForms (M8). An `ILogger` receiver can be added later. |
| `Propagator/Enterprise/BoldServicePropagatorUnit.pas`, `BoldPropagatorApplication.pas` | ~100 | Windows service host. | .NET Worker Service (`BackgroundService`). **Ported in M9:** an `IHostedService` that starts the propagator server with the ASP.NET Core host. |
| `Handles/Actions` (5 files) | 859 | Handle actions, based on VCL `TAction`. | `ICommand` pattern. **Ported in M8** on the RTL's emulation of Delphi's actions; `TBoldActionLink` links them to WinForms buttons and menu items, as Delphi's `Action` property did. |
| `UMLModel/Editor` (10 files) | 4,714 | Model editor forms. | WinForms, same logic. |
| `ObjectSpace/UtilsGUI/BoldOclPropEditor.pas` | 765 | OCL editor. | WinForms form. **Ported in M8**, with the type name selector and the run-time OCL debugger. |
| `Propagator/Common` constants and interfaces | ~180 | Propagator interfaces. | C# interfaces. **Ported in M9.** |
| **UML model creation (replaces Rose)** | — | Rose was the only way to create UML models back then, and Bold imported them from Rose. The port must let users create UML models, and import them. **Not built yet.** | Model editor in the port (`UMLModel/Editor`, WinForms) that saves to `.bld` (kept in `UMLModel/ModelLinks/Bld`). Import and export through XMI (kept in `UMLModel/ModelLinks/XMI`). Support XMI 2.x, which current tools write; the kept link is written for XMI 1.0. Eclipse Papyrus can create test models. |

---

## 12. Decisions needed

| ID | Question | My recommendation |
|---|---|---|
| DECIDE-1 | Which remote persistence transport? | **Decided: build an ASP.NET Core transport.** SOAP is dropped. The old HTTP transport was SOAP over HTTP, so it was removed with SOAP (`remove-soap`). The new transport is a REPLACE item in section 11 and is not built yet. Choose XML or JSON when it is built. |
| DECIDE-2 | Do we need the abstract client and server base classes (`Common/Connection`, `Common/ConnectionHandles`)? | **Decided: dropped** (`CSharpPortSource`, commit `9a1c8b1`). Only the COM layer used them. If propagation later needs a client registry, design it then. The COM-blocked Enterprise propagator server still refers to them until it is replaced. |
| DECIDE-3 | Keep `ObjectSpace/IDE/AttributeWizard` as a C# scanner and generator (REPLACE), or drop the wizard entirely and rely on the code generator? | **Decided: A, replace.** Rebuild the scanner as a Roslyn source generator that reads attributes straight from the C# classes at build time. It is scoped after the core port runs. The model-based generator stays as is. |
| DECIDE-4 | Do the `Samples/` apps stay as a reference copy outside the port? | **Decided: removed from the port** (`CSharpPortSource`, commit `5f44b47`). The reference copy stays on `master`. |
| DECIDE-5 | Keep `MultiUnitCodeGenPatch.pas` (multi-unit output)? | **Decided: dropped** (`CSharpPortSource`, commit `5f44b47`). It fixed Delphi unit placement for link classes. C# output uses one file per class. Test link-class placement when the C# generator is built. |

---

## 13. Verify list

Confirm these before deleting from the port:

1. `BoldMemoryManager` is still used by `BoldSystem` in the Delphi source. `BoldBase`, `BoldIndex`, `BoldCommonConst` and `BoldFreeStandingValues` are ported without it. Keep it until `BoldSystem` is ported (M3).
2. `BoldDirectoryTraverser`: done. It had no remaining references and was removed.
3. `BoldTemplate`: done. It had no references and was removed.
4. `BoldObjectMarshaler`: decided in M9, it is kept (O-3); the port's listener thread uses it.
5. Read the `.UnitDoc` for each dropped unit before deleting it. There are 393 of them, and they're the best record of intended behaviour.

## 14. Method and limits

- Built from folder structure, line counts, token search (COM, Win32, IDE, CLX, and DB-driver names), reference counts across the repo, and package membership (`*.dpk`, `*.dproj`).
- Read in full: `Tools/` headers and key units; `BoldQAwareGUI/Core/BoldQGUI.pas`; the first lines of every other unit listed.
- Not read in full: most IDE and COM units. Their classification rests on names, headers, and references. Read the `.UnitDoc` before deleting any entry marked DROP-verify.
- Nothing was compiled or run. The repo is a snapshot of `master`. The `develop` and `AttracsBold` branches were not checked.

## 15. Follow-ups from the removals

- **ModelMaker integration (planned, not started).** The ModelMaker link was removed. When it's needed, rebuild it as a separate plugin on top of the XMI link, rather than restoring the old COM link. The old code is in git history before branch `remove-modelmaker-ecoiii`.
- **Replacements for the COM-kept units are required, not built.** See the table in section 5. The HTTP client, the XML parsing, and the propagator interfaces are used by kept features, so they can't be removed until their replacements exist.
- **Code generator COM templates.** `MoldModel/CodeGenerator` still has `.dfm` templates that emit COM code. Remove them with the COM object space.
- **Replacement for Rose is required, not built.** Create UML models in the port (model editor), and import/export XMI 2.x. Until this is built, the port cannot create or import UML models. The UML data module's model came from Rose's `uml.mdl` (the removed Rose link component), so the editor needs its UML metamodel from another source, such as `.bld` or XMI.
- **Rose-specific XMI handling is kept.** `UMLModel/ModelLinks/XMI/BoldUMLXMIImporter.pas` reads the `RationalRose$` prefix that Rose writes into XMI. It's a parsing detail, not a Rose dependency, so it stays.
- **`Doc/Documents/GettingStarted`** (`MainForm.pas`, `DataMod.pas`) uses `BoldUMLRose98Link`. It's a historical sample, left as is.
- **Examples (removed).** The whole `examples/` folder is gone (`remove-examples`, `d8054f2`). Write our own examples once the main functionality runs. The html docs and the IB/BDE examples went with it. The first one is written (M8): `CSharp/examples/Building/BldOwn`, the Buildings and Owners application on WinForms and SQLite, from the master copy of the Delphi example. Their models live on as test data: the port tool copied every model on master (76 distinct from 77 sources, 22 of them in binary forms) into `CSharp/tests/Fixtures/Models` on the `CSharp` branch.
- **Project and option files still list CLX/Qt packages.** `Source/Propagator/Enterprise/BoldPropagator.dof` lists `visualclx` and `Rave50CLX`. The `.bpr` and `.dproj` files reference VCLX (VCL extensions, which are Windows, so kept). Remove the CLX entries from the `.dof` file if you want a clean list.
- **`Source/resources.ini`** still lists the removed IB and SQLDirect resource files as comments. It is inert, but it can be cleaned up with the package removal.
- **`Doc/`** (`history.txt`, the getting-started documents) mentions the removed units. These are historical records, so I left them.
- **Remote persistence is required, not built.** Until the ASP.NET Core transport exists, the port cannot run persistence across a network. Build it as a REPLACE item, with the server and `HttpClient` client in one change.
- **SOAP follow-ups.** The `examples/html/` docs went with the examples. `Source/resources.ini` no longer lists `BoldSOAP.rc`. `Bold.dproj` and `BoldPropagator.dof` still reference the `Soap` and `soaprtl` RTL names. These are Delphi RTL names, so they stay.
- **Constants kept on purpose.** `BoldSOAPTypeAttributeName` and `BoldSOAPNullAttributeName` in `BoldXMLStreaming.pas` are generic XML Schema-instance names (`xsi:type`, `xsi:null`), used by kept XML streaming. They are a rename candidate, not a SOAP dependency.
- **`Source/Persistence/DB` and `Source/Persistence/ADO`** are kept as the basis for the ADO.NET layer. Nothing in the kept tree depended on the removed drivers, . The Oracle-BDE unit was also removed.
- **Delphi bugs fixed in the M5 port.** Each is noted on its `[Delphi]` marker. The batch upgrader qualifies `BOLD_TYPE` and the version column (they were ambiguous). The evolutor's move between tables sets the target from the source (they were swapped). The external persistence controllers raise failed updates (they were swallowed), quote text keys with single quotes (double quotes are identifiers), include the first of several keys, and write only external members. The SQL external controller inserts the columns of the members it writes. The data validator's duplicate-link check ends (it never moved to the next row). The SQL Server preset stores blobs as `VARBINARY(MAX)` (Delphi kept `BLOB`, which SQL Server does not have). A new `dbeSQLite` preset was added for SQLite (decision D4).
- **Delphi bugs fixed in the M6 to M8 port.** Each is noted on its `[Delphi]` marker. M6: the file handler finds an open handler by path and name (it compared the name alone, so a second handler overwrote the first's writes); `AnsiCompareStr` and `AnsiCompareText` compare by locale rules. M7: switching `IsDefault` off clears the default system handle; the region definition clearing event is recognised; the panic shut down reads the dirty count before the system closes; the circular reference message has its four arguments; the manipulator names the right class and takes mappers; the type info handle keeps its type info until the system has shut down. M8: the run-time OCL debugger stops following fixes that lead back to themselves; constraints of a member without run-time information are empty (the auto form of an attribute list failed on them); the Bold data set remembers the element type it opens on (the first scroll reopened it at the first record).
- **Undo in the Buildings and Owners example.** A system's undo handler is off until enabled, so the example turns it on when the system opens; its Undo and Redo buttons did nothing without that.
