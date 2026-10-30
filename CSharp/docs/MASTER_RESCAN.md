<!-- A copy of the "Master rescan" tab of the Bold C# Port Plan doc (2026-10-30). The doc is the original: update this copy when the doc changes. -->

# Master rescan, folder by folder

Master has 2,611 files and `CSharpPortSource` has 1,383, of which 115 are new `FOLDER_INFO.md` files. We removed 1,343 files from 164 folders; only the OLLE extension is needed back.

## Folder by folder

73 rows cover the 164 folders with removals; the example folders are grouped by language. Only the OLLE Core row needs code back in the branch; SystemComparer goes into the tests.

| Folder | Files removed | What it did | Needed? | What we do |
| --- | --- | --- | --- | --- |
| Repository root | 19 of 23 | 19 Delphi package files (.dpk) | No | None; layering now comes from the uses graph |
| `Images/Components` | 6 of 129 | Palette bitmaps for removed SOAP/HTTP/ModelMaker components | No | None |
| `Source/BoldQAwareGUI/BoldControls` | 2 (all) | CLX (Qt) GUI controls | No | None; WinForms replaces VCL/CLX |
| `Source/BoldQAwareGUI/ControlPacks` | 3 (all) | CLX (Qt) GUI controls | No | None; WinForms replaces VCL/CLX |
| `Source/BoldQAwareGUI/Core` | 1 (all) | CLX (Qt) GUI controls | No | None; WinForms replaces VCL/CLX |
| `Source/ClientGuiCom/BoldControls` | 46 (all) | VCL controls bound to remote COM handles | No | None; remote access goes through ASP.NET Core |
| `Source/ClientGuiCom/ControlPacks` | 28 (all) | VCL controls bound to remote COM handles | No | None; remote access goes through ASP.NET Core |
| `Source/ClientGuiCom/Core` | 4 (all) | VCL controls bound to remote COM handles | No | None; remote access goes through ASP.NET Core |
| `Source/ClientGuiCom/IDE` | 5 (all) | VCL controls bound to remote COM handles | No | None; remote access goes through ASP.NET Core |
| `Source/ClientHandlesCom/Core` | 30 (all) | Client-side handles for a remote COM object space | No | None |
| `Source/ClientHandlesCom/IDE` | 2 (all) | Client-side handles for a remote COM object space | No | None |
| `Source/Common/Connection` | 4 (all) | Abstract client/server registry (DECIDE-2) | No | None |
| `Source/Common/ConnectionCOM` | 1 of 10 | BoldComServ: returns the COM server object | No | None |
| `Source/Common/ConnectionHandles` | 4 (all) | Handles for the client/server registry (DECIDE-2) | No | None |
| `Source/Common/ConnectionHandlesCOM` | 6 of 10 | XML dispatcher over COM/SOAP | No | None |
| `Source/Common/Environment` | 6 of 12 | CLX environment and VCL/CLX switch | No | None |
| `Source/Common/HTTP` | 4 (all) | WinINet HTTP client for SOAP persistence | No | None; HttpClient in Bold.Remote.Client |
| `Source/Common/IDE` | 2 of 29 | Design-time registration of the web connection | No | None |
| `Source/Common/IDECOM` | 3 of 9 | Design-time editors for the XML dispatcher | No | None |
| `Source/Common/Rose2000` | 2 (all) | Rational Rose 2000 type library | No | None; replaced by XMI + model editor |
| `Source/Common/Rose98` | 2 (all) | Rational Rose 98 type library | No | None; replaced by XMI + model editor |
| `Source/Common/SOAP` | 11 (all) | SOAP type libraries and XML requests | No (protocol behaviour yes) | Use master as reference for the ASP.NET Core API |
| `Source/Common/Support` | 2 of 58 | BoldDirectoryTraverser: file system walker | No | None |
| `Source/Common/SupportWin` | 2 of 5 | BoldWinUtils: task bar height | No | None |
| `Source/Common/Template` | 1 (all) | BoldTemplate: empty class | No | None |
| `Source/Common/UML` | 2 of 4 | BoldUMLDelphiSupport: parses Delphi parameter syntax | No | None |
| `Source/ConcurrencyControl/COM` | 4 (all) | Lock manager handles over COM | No (logic kept) | None; lock manager logic stays in Propagator/Enterprise |
| `Source/ConcurrencyControl/IDECOM` | 2 (all) | Design-time registration | No | None |
| `Source/Extensions/OLLE/Core` | 12 (all) | Object lending between Bold databases (global ids, hold/lend, broken links); no COM | **YES** | Restore Core (removed by mistake as "OLE") |
| `Source/Extensions/OLLE/IDE` | 3 (all) | Design-time editor and registration for the OLLE handle | No | None; only Core comes back |
| `Source/Handles/COM` | 6 (all) | Element handles exported over COM | No | None |
| `Source/Handles/IDECOM` | 2 (all) | Design-time registration | No | None |
| `Source/ObjectSpace/COM` | 2 of 11 | COM component validator | No | None |
| `Source/Persistence/Advantage` | 5 (all) | Advantage database adapter | No | None |
| `Source/Persistence/BDE` | 12 (all) | BDE adapter | No | None |
| `Source/Persistence/COM` | 2 (all) | COM persistence proxy | No | None |
| `Source/Persistence/DBExpress` | 6 (all) | dbExpress adapter | No | None |
| `Source/Persistence/DBISAM` | 6 (all) | DBISAM adapter | No | None |
| `Source/Persistence/DOA` | 6 (all) | Direct Oracle Access adapter | No | Reference on master if Oracle is added |
| `Source/Persistence/HTTP` | 6 (all) | SOAP-over-HTTP persistence | No (transport decided: ASP.NET Core) | Reference on master |
| `Source/Persistence/IBX` | 9 (all) | InterBase Express adapter + create-database action | No | Reference on master if Firebird is added |
| `Source/Persistence/IDECOM` | 6 (all) | Design-time registration | No | None |
| `Source/Persistence/SOAP` | 12 (all) | SOAP persistence: operation streaming, proxy, stub | No (protocol behaviour yes) | Operation list is the reference for the new API |
| `Source/Persistence/SQLDirect` | 5 (all) | SQLDirect adapter | No | None |
| `Source/Samples/Actions` | 4 (all) | Debug actions, OCL edit action (VCL) | No | None |
| `Source/Samples/BoldCheckListBox` | 6 (all) | Check list box control (VCL) | Later | WinForms control later if wanted |
| `Source/Samples/FormSaver` | 2 (all) | Form-level save/cancel of dirty objects | Later | Reference for Bold.WinForms |
| `Source/Samples/IDE` | 3 (all) | Design-time registration | No | None |
| `Source/Samples/Misc` | 3 (all) | Object by id string; lock utilities | Small | Trivial API in Bold.ObjectSpace |
| `Source/Samples/ModelLoader` | 2 (all) | Load a model file by extension | Covered | Bold.Model loader does this |
| `Source/Samples/NewObjectInterceptor` | 2 (all) | Fills new objects with random words | No | None |
| `Source/Samples/SortingGrid` | 2 (all) | Sortable grid (VCL) | No | DataGridView sorts |
| `Source/Samples/SystemComparer` | 2 (all) | Compares two object spaces object by object | YES (tests) | Port into Bold.TestSupport |
| `Source/Samples/SystemDebugger` | 3 (all) | Object space debug window | Later | Optional diagnostics window |
| `Source/Samples/UMLPlugins` | 2 (all) | Model editor name trimmer plugin | No | None |
| `Source/Samples/Unicode` | 4 (all) | Wide string attribute + mapper for non-Unicode Delphi | No | C# strings are Unicode; map strings to nvarchar |
| `Source/UMLModel/ModelLinks/ModelMaker/Link` | 4 (all) | ModelMaker link and plugin | Later | ModelMaker integration later, from master |
| `Source/UMLModel/ModelLinks/ModelMaker/MMPlugin` | 16 (all) | ModelMaker link and plugin | Later | ModelMaker integration later, from master |
| `Source/UMLModel/ModelLinks/ModelMaker/Support` | 2 (all) | ModelMaker link and plugin | Later | ModelMaker integration later, from master |
| `Source/UMLModel/ModelLinks/Rose98` | 12 (all) | Rose model link | No (behaviour replaced) | XMI + model editor |
| `Source/UMLModel/Plugins` | 1 of 10 | ECO III generator | No | None |
| `Source/Unassigned` | 4 of 10 | Oracle-BDE interfaces, two .dpk files | No | None |
| `Source/Unassigned/Asta` | 5 (all) | Asta middleware adapter | No | None |
| `Source/Unassigned/BDEUtils` | 4 (all) | BDE alias dialogs | No | None |
| `Source/Unassigned/BDEUtils/UtilsGuiBde` | 8 (all) | BDE alias dialogs | No | None |
| `Source/Unassigned/dbvalidate` | 1 (all) | SQL executor over BDE | No | None |
| `Source/Unassigned/patches` | 1 of 2 | MultiUnitCodeGenPatch (DECIDE-5) | No | Generator test for link-class placement |
| `Tools/BoldClientifier` | 9 (all) | Converts an app into a COM client | No | None |
| `Tools/BoldUpgrader` | 7 (all) | Upgrades Bold 1 source code to Bold 2 | No | None |
| `Tools/DBConvert` | 6 (all) | Copies tables between BDE databases | No | None |
| `examples/CBuilder` (30 folders) | 345 (all) | C++Builder example apps | Models yes | Extract models into test fixtures |
| `examples/Delphi` (61 folders) | 516 (all) | Delphi example apps | Models yes | Extract models into test fixtures; use as feature checklist |
| `examples/html` (3 folders) | 63 (all) | Feature description pages | Reference | Feature checklist; keep on master |

## Kept code that still points at removed units

Eight kept units name a removed unit in their uses lists, and none is on the port path. A scan also flags 11 COM and propagator units that use Delphi's own `ComServ`; an example carried a copy of that unit, so nothing is really missing there.

| Kept units | Point at | Why it is fine |
| --- | --- | --- |
| `BoldComClient`, `BoldComServer`, `BoldComClientHandles`, `BoldComServerHandles` | `BoldClient`, `BoldServer`, `BoldClientHandles`, `BoldServerHandles` (removed under DECIDE-2) | COM units; not ported. |
| `Propagator/Enterprise/BoldServerHandlesDataMod` | `BoldServerHandles` | The COM propagator's data module; not ported. |
| `Doc/Documents/GettingStarted` (`DataMod`, `MainForm`) | `BoldDatabaseAdapterIB`, `BoldUMLRose98Link`, `BoldUMLMMLink` | A historical sample in the docs. |
| `Unassigned/BoldUMLModelEditorDebuggerPlugin` | `BoldSystemDebuggerForm` (a removed sample) | A model editor debug plugin; rebuild it with the editor if wanted. |

## Test models on master

Master holds 55 model texts in readable form, 39 of them distinct. They all use the current `.bld` format (VERSION 19), stored either as `.bld` files or inside text `.dfm` forms. Five more models exist only in binary `.dfm` forms: `BuildingsAndOwners`, `MultilangClasses`, `CustAttrExampleClasses`, `OclVariableClasses` and OLLE's own `DistributableInfo`. The fixture script can read those too if we add a small binary `.dfm` reader. Paths are under `examples/` unless they start with `Source/` or `Doc/`.

| Model | Classes | Associations | Taken from |
| --- | --- | --- | --- |
| `AccountClasses` | 2 | 0 | `Delphi/Compound/bios2/server` |
| `AssociationClassExampleClasses` | 4 | 1 | `CBuilder/Simple/ObjectSpace/AssociationClass` |
| `BankingClasses` | 6 | 4 | `Delphi/Simple/ObjectSpace/Transaction` |
| `BldOwnClasses` | 5 | 2 | `Delphi/Simple/Persistence/Synchronization` |
| `BoldUMLModel` | 122 | 116 | `Source/UMLModel/Core` |
| `BudgetClasses` | 7 | 5 | `Delphi/Compound/XML` |
| `BuildingClasses` | 5 | 2 | `Delphi/Compound/Building` |
| `BusinessClasses` | 1 | 0 | `CBuilder/Simple/ObjectSpace/DataSetAndReporting` |
| `BusinessClasses` | 2 | 0 | `Delphi/Simple/Persistence/ObjectLending` |
| `BusinessClasses` | 3 | 1 | `Delphi/Simple/Persistence/CheckInOut` |
| `BusinessClasses` | 4 | 2 | `Delphi/Simple/GUIControls/SimpleTreeView` |
| `BusinessClasses` | 5 | 3 | `Delphi/Compound/Locking/Common` |
| `BusinessClasses` | 5 | 2 | `Delphi/Compound/ASP/Common` |
| `BusinessClasses` | 9 | 8 | `Delphi/Compound/ExternalPersistence` |
| `ComboBoxClasses` | 4 | 1 | `CBuilder/Simple/GUIControls/ComboBox` |
| `ConstraintExampleClasses` | 5 | 3 | `CBuilder/Simple/ObjectSpace/Constraints` |
| `ConwayClasses` | 3 | 6 | `Delphi/Compound/Conway` |
| `DataSetExampleClasses` | 3 | 1 | `Delphi/Simple/ObjectSpace/DataSetAndReporting` |
| `DataValidationExampleClasses` | 5 | 1 | `CBuilder/Simple/ObjectSpace/DataValidation` |
| `DerivedAttrExampleClasses` | 3 | 0 | `Delphi/Simple/ObjectSpace/DerivedAttributes` |
| `DerivedHandleExampleClasses` | 4 | 2 | `CBuilder/Simple/ObjectSpace/DerivedHandle` |
| `DerivedHandleExampleClasses` | 4 | 4 | `CBuilder/Simple/ObjectSpace/DerivedRelations` |
| `DocumentClasses` | 8 | 7 | `Delphi/Simple/ObjectSpace/Temporal` |
| `DocumentClasses` (variant) | 8 | 7 | `CBuilder/Simple/ObjectSpace/Temporal` |
| `EmptyModel` | 1 | 0 | `Delphi/Repository/DefaultProject` |
| `GettingStarted` | 2 | 0 | `Doc/Documents/GettingStarted` |
| `ImageDemoClasses` | 2 | 0 | `CBuilder/Simple/GUIControls/BoldImage` |
| `ModelEvDemo` | 5 | 3 | `Delphi/Compound/ModelEvolution/Version1` |
| `ModelEvDemo` | 6 | 4 | `Delphi/Compound/ModelEvolution/Version2` |
| `New_Model` | 1 | 0 | `Source/Unassigned` |
| `OptimisticLockingExampleClasses` | 3 | 1 | `Delphi/Simple/ObjectSpace/OptimisticLocking` |
| `OrgChartClasses` | 9 | 6 | `Delphi/Compound/XML2` |
| `ParentChildMappingClasses` | 7 | 0 | `CBuilder/Simple/Persistence/ParentChildMapping` |
| `ProdStructClasses` | 5 | 1 | `Delphi/Compound/ProdStruct` |
| `QueryDemoClasses` | 2 | 1 | `CBuilder/Simple/ObjectSpace/Query` |
| `RendererExampleClasses` | 3 | 0 | `CBuilder/Simple/GUIControls/Renderers` |
| `ReverseDeriveExampleClasses` | 4 | 1 | `CBuilder/Simple/ObjectSpace/ReverseDerivedAttributes` |
| `SQLHandleClasses` | 4 | 2 | `CBuilder/Simple/ObjectSpace/SQLHandle` |
| `TreeViewExampleClasses` | 8 | 2 | `CBuilder/Simple/GUIControls/TreeView` |

## Feature coverage from master's examples

Each feature master's examples show gets tests in the milestone below, using that example's model as the fixture. Paths are under `examples/Delphi/`.

| Feature | Example on master | Milestone |
| --- | --- | --- |
| Buildings and owners (the basic sample) | `Compound/Building` | M3 |
| Link classes (association classes) | `Simple/ObjectSpace/AssociationClass` | M3 |
| Transactions | `Simple/ObjectSpace/Transaction` | M3 |
| Custom attribute types | `Simple/ObjectSpace/CustomAttributes` | M3, M6 |
| Multi-language attributes | `Simple/GUIControls/MultiLang` | M3, M8 |
| Recursive structures | `Compound/ProdStruct` | M3, M5 |
| Many objects (performance) | `Compound/Conway` | M3 |
| Constraints and data validation | `Simple/ObjectSpace/Constraints` | M4 |
| Derived attributes, handles and links | `Simple/ObjectSpace/DerivedAttributes`, `DerivedHandle`, `DerivedRelations` | M4, M7 |
| Reverse derived attributes | `Simple/ObjectSpace/ReverseDerivedAttributes` | M4 |
| OCL variables | `Simple/ObjectSpace/OclVariables` | M4, M7 |
| Optimistic locking | `Simple/ObjectSpace/OptimisticLocking` | M5 |
| Queries run in the database | `Simple/ObjectSpace/Query`, `SQLHandle` | M5, M7 |
| Object versioning (temporal) | `Simple/ObjectSpace/Temporal` | M5 |
| Parent and child table mapping | `Simple/Persistence/ParentandChildMapping` | M5 |
| Model evolution | `Compound/ModelEvolution` (Version1, Version2) | M5 |
| External persistence | `Compound/ExternalPersistence`, `ExternalPersistence2` | M5 |
| Object lending, check-in and check-out | `Simple/Persistence/ObjectLending`, `CheckInOut` | After M5 (OLLE) |
| XML producers | `Compound/XML` | M7 |
| Renderers, combo box, image and tree views | `Simple/GUIControls/*` | M8 |
| Data sets and reporting | `Simple/ObjectSpace/DataSetAndReporting` | M8, as data binding |
| Synchronization between clients | `Simple/Persistence/Synchronization` | M9 |
| Pessimistic locking and the lock manager | `Compound/Locking` | M9 |
| Remote access over COM, SOAP and HTTP | `Compound/Bios`, `bios2`, `ASP`, `HTTPPMapper`, `XML2` | M10 replaces them |
