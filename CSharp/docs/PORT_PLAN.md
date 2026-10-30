<!-- A copy of the "Port plan" tab of the Bold C# Port Plan doc (rev 60, 2026-10-30). The doc is the original: update this copy when the doc changes. -->

# Bold C# Port Plan

|  |  |  |
| --- | --- | --- |
|  |  |  |
|  |  |  |

Oct 13, 2026 · @ismail özenç

M0 is done: the `CSharp` branch builds and passes its tests in CI, `BoldDefs` is ported and verified against the Delphi source, and every Delphi unit and C# file point at each other. All eight decisions are made, and OLLE is back in the branch. Next is M1, the first testable point: loading and round-tripping all 76 test models, which needs 24 Delphi units.

## M0 result

The skeleton, the port tool and the first unit are on the `CSharp` branch at `8998e7e`, and [CI passed on Windows](https://github.com/ismailozenc/BoldForDelphi/actions/runs/38488149321).

| Part | Result |
| --- | --- |
| Solution | `CSharp/Bold.slnx`: 18 source projects on .NET 10 (WinForms parts on `net10.0-windows`), 3 NUnit test projects, `Bold.TestSupport` and `Bold.PortTool` |
| First unit | `BoldDefs`: 105 of 105 symbols ported and verified; the tests read their expected values from `BoldDefs.pas` itself |
| Port status | A `[Delphi(PortStatus…)]` attribute on every ported element, shown as verified only when a covering test passes; `PORT_STATUS.md` and a page per unit list all 24,187 symbols of the 481 Delphi units |
| Links both ways | Each unit page links every symbol to its Delphi line and body, its C# code and its tests; each Delphi `unit` line names its C# file, and each `FOLDER_INFO.md` lists where its units went; CI fails when a link breaks |
| Port map | `CSharp/port/port-map.json`: 381 units to port, each with its project and milestone, and 100 not ported, each with its reason |
| Test models | 76 distinct models from 77 sources on master, 22 of them read from binary forms, in `CSharp/tests/Fixtures/Models` |
| CI | GitHub Actions on Windows: build, 137 tests, link check, and the port status on the job page |

## What the rescan found

Only one removed part holds a feature we need and do not plan to rebuild: OLLE. The other rows are test data and reference code we use from master.

| Removed part | What it did | What we do |
| --- | --- | --- |
| `Source/Extensions/OLLE/Core` (6 units, about 2,260 lines) | Lends objects between Bold databases: global ids, holding and returning objects, repairing broken links. Used by the CheckInOut and ObjectLending examples. No COM. | **Restored to the branch (e930283).** It was removed by mistake in the COM step, listed as "OLE". Port it after SQL persistence (M5). |
| Example models (`examples/` and a few in Source and Tools: 77 model texts, 76 distinct) | Models for link classes, derived attributes, transactions, versioning, table mapping, evolution and more. | Copied into `CSharp/tests/Fixtures/Models` in M0 by the port tool, with where each one came from. They are test data, not code. |
| `Source/Samples/SystemComparer` | Compares two object spaces object by object. | Port it into `Bold.TestSupport` for save-and-reload tests (M3). |
| `Source/Persistence/SOAP` (operation streaming) | Lists every remote persistence operation and its arguments. | Read it on master when we design the ASP.NET Core API (M10). |
| `Source/Samples/FormSaver`, `Source/Samples/SystemDebugger` | Saves or cancels the objects one form changed; a debug window for the object space. | Read them on master when we build `Bold.WinForms` (M8). |
| `Source/Persistence/IBX`, `Source/Persistence/DOA` | InterBase/Firebird and Oracle adapters, with engine quirks such as database creation. | Read them on master only if you want Firebird or Oracle (decision D4). |

Everything else we removed is not needed or has a planned replacement: CLX, COM and DCOM, Rose (XMI and the model editor replace it), ModelMaker (later), ECO III, the dead drivers, three tools, the other samples, the connection classes and the code generator patch. Every folder is listed in Master rescan.

## Is the branch ready

Yes: every part we port is in `CSharpPortSource`, OLLE included since its restore; the tests and test models are on the CSharp branch.

| What the port needs | In the branch | Where, and what it means |
| --- | --- | --- |
| Load a model without the UML object space | Yes | `MoldModel/Bld/BoldBld.pas` reads and writes `.bld` straight into the Mold model, so models load before the object space exists. |
| Runtime type info | Yes | `ObjectSpace/RTModel/BoldSystemRT.pas` builds it from the Mold model. |
| Object space | Yes | `ObjectSpace/BORepresentation`: `BoldSystem` (7,179 lines), `BoldAttributes` (3,561), `BoldLinks` (2,588). |
| OCL | Yes | Grammar in `ObjectSpace/Ocl/BoldOcl.ycc`, operations in `BoldOclSymbolImplementations`, the OCL 1.1 spec in `Doc/`. We write a parser from the grammar; the generated parser tables are not ported. |
| O/R mapping and SQL | Yes | `PMapper/*`, `Persistence/DB`, `Persistence/ADO`. Dialects exist for SQL Server, InterBase, Oracle, ANSI SQL-92, DBISAM, Advantage, Paradox and Informix; SQLite is not one, so we add it. |
| Persistence without a database | Yes | `Persistence/File` (XML file) and `Persistence/System` give save-and-reload tests before SQL. |
| Handles, GUI and actions | Yes | `Handles/*`, `BoldAwareGUI/*`, `Handles/Actions`; VCL becomes WinForms. |
| Multi-user | Logic yes, transport no | `Persistence/Propagation`, `Propagator/Enterprise`, `ConcurrencyControl`; the COM transport is replaced. |
| Code generation | Yes | `MoldModel/CodeGenerator`; we write new C# templates. |
| Model editor, validation, XMI 1.x | Yes | `UMLModel/Editor`, the validators in `UMLModel/Core`, `UMLModel/ModelLinks/XMI`. |
| Documentation | Yes | `Doc/`: Starting BfD parts 1 to 3, the OCL 1.1 spec, the MDA paper. |
| Tests | Started | Bold never had any. M0 added NUnit tests for BoldDefs, DelphiSet and the port tool; each milestone adds its own. |
| Test models | Yes | Copied from `examples/` on master into the CSharp branch in M0. |
| OLLE | Yes | Restored to Source/Extensions/OLLE/Core after its removal by mistake. |

Windows pieces get .NET replacements: MSXML becomes `System.Xml`, VCL becomes WinForms, COM becomes ASP.NET Core and C# interfaces, Windows threads and the registry become tasks, channels and `appsettings.json`, and TDataSet becomes ADO.NET. Eight kept units still point at removed units; all are COM, design-time code or the old GettingStarted sample, so none is on the port path.

## Solution and projects

One solution, `CSharp/Bold.slnx`, on .NET 10 LTS: 17 projects in layers that only reference downward, two small support projects, the test projects and the port tool. The five engine projects come first and hold about 63,000 of the 154,000 Delphi lines we rebuild. `port-map.json` assigns every unit; this table sums it up.

&#91;embedded content: Bold C# solution · projects in layers, arrows to the referenced project\]

The tests mirror these layers: one test project per project, plus `Bold.TestSupport` and the model fixtures in `tests/Fixtures`. `Bold.SourceGenerators` comes later and stands apart, with its own copy of the model reader.

| Project | Type | Built from (branch folders) | Delphi size | Milestone |
| --- | --- | --- | --- | --- |
| `Bold.Core` | Class library | `Common/Core`, `Support`, `Subscription`, `Queue`, `Logging`, `Environment`, `TaggedValues`, `UML`, and the `BoldHandle` base class from `Common/Handles` (non-GUI units) | 49 units, 13,186 lines | M0 to M3; some helpers when their users come |
| `Bold.Model` | Class library | `MoldModel/Core`, `Bld`, `TypeNameDictionary`, `Handles`, and `BoldDefaultStreamNames` | 9 units, 5,666 lines | M1; the model handles in M3 |
| `Bold.ValueSpace` | Class library | `ValueSpace/*`, `FreestandingValueSpace` | 13 units, 5,161 lines | M2 |
| `Bold.Persistence` | Class library | `Persistence/Core`, `Persistence/File`, the passthrough controller, and the shared message units `BoldPMConsts` and `BoldCoreConsts` | 13 units, 2,961 lines | M3 |
| `Bold.ObjectSpace` | Class library | `ObjectSpace/Core`, `RTModel`, `BORepresentation`, `Undo`, `Unloader`, lock regions, the `Persistence/System` controller; OCL from `ObjectSpace/Ocl` | 38 units, 36,130 lines (OCL 11,436) | M2 to M4 |
| `Bold.Persistence.Sql` | Class library | `PMapper/*`, `Persistence/DB`, `ADO`, `ObjectUpgrading`, `ExternalPersistence`, `Unassigned/AutoInc` | 58 units, 26,409 lines | M5 |
| `Bold.CodeGen` | Class library and `dotnet` tool | `MoldModel/CodeGenerator`, with new C# templates | 4 units, 2,451 lines | M6 |
| `Bold.Handles` | Class library | `Handles/Core`, `Manipulators`, `UnLoader`, `XML`, `BoldPersistenceHandleSystem`, the component validator | 23 units, 4,820 lines | M7 |
| `Bold.WinForms` | Class library (`net10.0-windows`) | `BoldAwareGUI/*`, `Handles/Actions`, log forms, GUI helpers | 68 units, 27,472 lines | M8 |
| `Bold.Propagation` | Class library | `Persistence/Propagation`, `UDPPropagator`, lock handler and holder, the external event handler, `ConcurrencyControl`, `Propagator/Common` as C# interfaces | 23 units, 4,133 lines | M9 |
| `Bold.Propagator.Server` | Worker Service | `Propagator/Enterprise` and `LowEnd`, logic only, and `BoldThread` | 22 units, 5,895 lines | M9 |
| `Bold.Remote.Server`, `Bold.Remote.Client` | ASP.NET Core app, class library | New; master's SOAP units as reference | New code | M10 |
| `Bold.Xmi` | Class library | `UMLModel/ModelLinks/XMI`, the model link base class, Boldify and flattening from `UMLModel/Core`, new XMI 2.x | 13 units, 4,938 lines | M11 |
| `Bold.ModelEditor` | WinForms app | `UMLModel/Editor`, `Plugins`, the validators, the `.bld` model link | 22 units, 7,697 lines | M12 |
| `Bold.Olle` | Class library | `Extensions/OLLE/Core` | 6 units, 2,259 lines | After M5 |
| `Bold.SourceGenerators` | Roslyn generator (`netstandard2.0`) | Scanner from `ObjectSpace/IDE/AttributeWizard` (DECIDE-3) | 19 units, 4,478 lines | After M12 |
| `Bold.Porting`, `Bold.Rtl` | Class libraries | New: the port status attributes, and Delphi semantics such as `DelphiSet<T>` | New code | M0 |
| `Bold.TestSupport` and the test projects | NUnit 4 | Readers that take expected values from the Delphi source, the test models, later the SystemComparer port | New code | From M0 |
| `Bold.PortTool` | Console tool | New: links both ways, status pages, link checks, test models | New code | M0 |

Delphi lets units use each other in circles; C# projects cannot. These moves keep every reference pointing down; we checked them against every uses clause in the branch, and `port-map.json` notes each one:

- `BoldDefaultStreamNames` moves into `Bold.Model`, and `BoldPMConsts` and `BoldCoreConsts` into `Bold.Persistence`: the lowest projects that use them.
- `BoldHandle`, the base class of every handle, moves into `Bold.Core`, so persistence handles can derive from it.
- The passthrough persistence controller moves from `Persistence/Propagation` into `Bold.Persistence`.
- `BoldPersistenceHandleSystem` and the component validator (`BoldComponentValidator`) move into `Bold.Handles`.
- `BoldExternalObjectSpaceEventHandler`, `BoldLockHandler` and `BoldLockHolder` move into `Bold.Propagation`, since they need the propagator handle or the listener thread.
- `BoldThread` moves into `Bold.Propagator.Server`, its only user.
- `BoldObjectUpgraderHandle` takes the runtime type info directly instead of a type info handle, so `Bold.Persistence.Sql` does not need `Bold.Handles`.
- Dialogs and log windows that lower code opens (`BoldQueryUserDlg`, `BoldLogForm`), and the VCL environment hook, become callback interfaces in `Bold.Core`; `Bold.WinForms` supplies the windows.
- `BoldDBActions` and the log forms move into `Bold.WinForms`.
- The XMI importer is rewritten against the Mold model, so `Bold.Xmi` needs no UML model units; it takes the model link base class and the Boldify code from `UMLModel`, and the editor takes the validators.

OCL stays inside `Bold.ObjectSpace`, because `BoldSystem` and `BoldSystemRT` create the OCL evaluator and OCL uses the object space. Not ported: 100 units, about 35,900 lines. They are IDE and design-time code (54 units), COM (29), `MSXML_TLB` (`System.Xml` instead), the generated `BoldUMLModel.pas` and three units that only serve the UML object space (D6), the two `TBoldModel` component units, which the `Bold.Model` loader replaces, and ten small units such as the propagator windows and unused unassigned code. `port-map.json` gives the reason for each.

## Milestones

Each milestone ends when its tests pass in CI. M1 is the first testable point and needs 24 Delphi units; M3 is the first that behaves like Bold, with objects in memory.

| Milestone | What gets ported (from the branch) | Tests that close it |
| --- | --- | --- |
| **M0 Skeleton** | `CSharp/` with `Bold.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props` and `.editorconfig`; `Bold.Core` and `Bold.Core.Tests`; a GitHub Actions build; the port tool, which links both code bases, reports the status of every symbol and copies the test models from master. Done, with BoldDefs ported. | Done: CI builds, runs 137 tests and checks every link; the 76 test models are in `tests/Fixtures/Models`. |
| **M1 Load models (first testable point)** | 24 units, about 11,100 lines: `BoldDefs`, `BoldUtils`, the tagged value units, `BoldUMLTypes`, `BoldNameExpander`, `BoldMeta`, `BoldMetaSupport`, `BoldMoldConsts`, `BoldTypeNameDictionary`, `BoldBld`. Bold's container units become .NET collections. | Done: 1,635 of 1,642 symbols ported, 7 partial; 68 of the 76 fixtures write back byte for byte, and the other 8 (not written by BoldBld) match in meaning. Every fixture loads; `.bld` to model to `.bld` gives the same text; class, attribute and role counts match; superclasses and association ends resolve; a bad file fails with its line number; the 122-class UML metamodel loads. |
| **M2 Runtime model and subscriptions** | `BoldSubscription`, `BoldDeriver`, the event queue, ids, value interfaces, free-standing values, the type info part of `BoldElements`, `BoldSystemRT`, `BoldMemberTypeDictionary`. | Done: all 1,965 symbols ported and 1,901 covered by tests; the 7 that waited on classes from M3 and M4 (TBoldObject, TBoldObjectList, TBoldTypeList, TBoldOCL) were finished with them. Runtime type info builds for all 76 fixtures. BoldOclError was pulled forward from M4. Subscribe, publish, unsubscribe and deferred events; runtime type info for every fixture (inheritance, member indexes, roles, link classes); value spaces copy and compare. |
| **M3 Object space in memory** | `BoldElements`, `BoldDomainElement`, `BoldSystem`, `BoldAttributes`, `BoldLinks`, object lists, undo, lock regions, optimistic locking support, XML file and in-memory persistence: about 90 units with M1 and M2. The OCL evaluator is an interface that throws until M4. | Done: 2,628 of 2,630 symbols ported and 1,223 covered by tests. The two partial ones stream SQL condition parameters to XML as text, where Delphi wrote `TParam`'s binary buffer. The VCL progress notifier (`TBoldPersistenceProgressNotifier`) was ported with the WinForms controls (M8). The M1–M5 audit added closing tests: every fixture fills, saves to XML, reloads and compares equal; delete rules; derivation and constraints; undo of links and objects; lock regions. Create, change and delete objects; both ends of a link stay in step; delete rules hold; undo and redo; a transaction rolls back; the Building model saves, reloads and compares equal. |
| **M4 OCL** | A parser written from `BoldOcl.ycc`, the type checker, the evaluator, the operation library, derived attributes and links, reverse derivation, constraints. | Done: all 1,289 symbols ported and 558 covered by tests. OCL also runs as SQL (M5). The audit's catalogue evaluates every operation of the OCL library on a small town and checks the value Delphi Bold gives; it found two Delphi bugs (includesAll compared values by identity; a time literal always held 0), fixed with a note on each. An expression catalogue (parse, type, value) on fixture models; derived values update when inputs change; constraints report broken rules. |
| **M5 SQL persistence** | `PMapper/*` and `Persistence/DB` on ADO.NET; SQL Server and a new SQLite dialect; schema generation; OCL to SQL; database evolution; the validator; object upgrading; external persistence; AutoInc. | Done: all 2,652 symbols of the 61 units ported and 979 covered by tests. 74 of the 76 fixtures save and reload on SQLite (GettingStarted has model errors; the UML metamodel has a role named `constraint`, an SQL reserved word). The Building schema has snapshots for SQL Server, ANSI and a new `dbeSQLite` dialect. OCL queries run as SQL. The ModelEvolution example evolves from version 1 to 2. A separate CI job runs the tests on SQL Server 2022. `BoldPersistenceHandleADO` is dropped, since it is deprecated in Delphi Bold. The audit's round trips set every stored attribute and link every role before they compare, and two systems race to save one object under each optimistic locking mode. SQLite in-memory round trips for every fixture; schema snapshots per dialect; OCL queries run as SQL; evolution from ModelEvolution v1 to v2; SQL Server tests in a separate CI job. |
| **M6 Typed classes** | `Bold.CodeGen` with C# templates, and a generated-code registry that replaces `BoldGeneratedCodeDictionary`. | Done: all 258 symbols of the 7 units ported and 230 covered by tests. The Delphi templates reproduce the code Delphi Bold generated for the Building and OrgChart examples; C# code generated for every fixture compiles; typed access works on the Building model (links, link objects, typed lists, methods, CompleteCreate overrides, OCL); members derived in code derive and reverse derive; link classes land in their own file in the right namespace (DECIDE-5); generating again keeps user code. bold-codegen generated the Building example's classes. COM code generation is dropped with the COM object space. |
| **M7 Handles** | System, expression, list, reference, variable, cursor, derived, sorted, filtered and SQL handles; OCL variables and repository; XML producers. | Done: all 582 symbols of the 24 units ported and 568 covered by tests. Handles follow object and list changes (list, cursor, expression, filtered, sorted, derived and reference handles); OCL variables resolve to handle values and re-evaluate when those change. Six Delphi bugs are fixed, each with a note on its marker. |
| **M8 WinForms** | Control packs (followers and renderers), the controls, actions and form generation; our first example app, built on the Building model. | Done: all 2,668 symbols of the 69 units ported and 1,976 covered by tests. The GUI logic that needs no WinForms (27 units: followers, renderers, actions, properties controllers) is in a new project, Bold.AwareGui, tested on every platform. The controls, the auto forms, the forms of the persistence layer and the editors are in Bold.WinForms, tested on Windows in CI on forms shown off screen (65 tests). The grid is a DataGridView, the Bold data set runs on the RTL's TDataSet emulation, auto forms are built at run time as in Delphi, and TBoldActionLink links actions to buttons and menu items. The example app runs: Buildings and Owners (examples/Building/BldOwn, on SQLite), which a test drives through its buttons and menus, from creating the database to showing a person in its own form. Three Delphi bugs are fixed, each with a note on its marker. |
| **M9 Multi-user** | Listener, snooper and ID adder; the propagator server as a Worker Service; the lock manager, lock handler and lock holder. | Done: all 1,205 symbols of the 51 units ported and 1,145 covered by tests. The propagator server (client handler, enqueuer, subscriptions, client notifier, cleanup, lock manager and its administration) runs on ASP.NET Core rather than a bare Worker Service, and a SignalR hub replaces DCOM (D8); the client handles speak SignalR's protocol over a WebSocket, so the client libraries need no package. Both closing scenarios run in the process and through the hub, and the UDP broadcaster keeps two object spaces in step by broadcasts. The listener's lease and disconnects, reconnecting, the external event handler and the RTL's threads have tests of their own. The propagator forms and the COM wrappers are not ported. Delphi bugs found on the way are fixed, each with a note on its marker. Two object spaces stay in step through the propagator; lock conflicts are reported. |
| **M10 Remote persistence** | `Bold.Remote.Server` (ASP.NET Core) and `Bold.Remote.Client` (HttpClient), with the operations taken from master's SOAP streaming. | Round trips through an in-process server. |
| **M11 XMI** | The XMI 1.x importer ported to the Mold model; new XMI 2.x import and export; Boldify (flatten packages, apply defaults). | Models exported from UML tools import; XMI round trips. |
| **M12 Model editor** | A WinForms editor over `Bold.Model` with undo, the validators, and the code and database plugins. This completes the Rose replacement. | Fixture models can be edited, validated and saved. |

Later: OLLE (after M5; restored to the branch), the attribute source generator (DECIDE-3) and the ModelMaker link.

## How we port

We rebuild behaviour in plain C#, test first, from reading the Delphi code; we do not translate it line by line.

- **Behaviour first:** read the unit and its `.UnitDoc`, write tests for what it does, then write the C#.
- **Names:** keep Delphi's file, type, member and parameter names, prefixes included, so a search finds both sides: `TBoldSystem`, `TMoldClass` and `EBold` keep their names. Every type is in namespace `Bold`, and unit-level routines and constants go in a static class named after the unit, such as `BoldDefs`.
- **Collections:** Bold's indexed lists become `List<T>` and `Dictionary<,>`, keeping their lookup rules, such as case-insensitive names and order.
- **Memory:** no `Free`; subscriptions, handles and systems are `IDisposable`, so a forgotten subscription cannot leak.
- **Threads:** the object space stays single-threaded, as in Delphi; deferred events run on the UI thread through `SynchronizationContext`; server parts use `Channel<T>` and `PriorityQueue<,>`.
- **Errors:** `EBold` exceptions keep their names and their Delphi parents (`EBold` derives from `Exception`); resource strings become constants for now.
- **Tracking:** every ported element carries a `[Delphi(PortStatus…)]` attribute and every test a `[Covers]` attribute. The port tool writes `PORT_STATUS.md` and a page per Delphi unit that links each symbol to its Delphi line, its C# code and its tests, and shows it as verified only when a covering test passes. Each Delphi `unit` line and each `FOLDER_INFO.md` links back, and CI fails when a link breaks. `CSharp/PORTING.md` has the rules.
- **Reference:** the Delphi source stays in the branch as the specification until the port ends.

## Decisions

All eight are decided. D1, D2, D3 and D7 are carried out; the others apply from the milestone shown.

| ID | Question | Decision | Applies |
| --- | --- | --- | --- |
| D1 | Where the C# code lives | A `CSharp` branch made from `CSharpPortSource`, with the port in `CSharp/` next to the Delphi source it is built from | Done in M0 |
| D2 | Target framework | .NET 10 LTS, supported until 14 November 2028; WinForms parts on `net10.0-windows`; stay on 10 when .NET 11 (STS) ships | Done in M0 |
| D3 | Test framework | NUnit 4, the most feature-rich of the .NET test frameworks: parameterized tests, constraint asserts, setup at every level; one test project per project | Done in M0 |
| D4 | Databases | SQL Server for production and SQLite for tests; Firebird, PostgreSQL or Oracle only if needed | M5 |
| D5 | Database schema | The same tables and columns as Delphi Bold (the `BOLD_` system tables such as `BOLD_ID`, `BOLD_TYPE`, `BOLD_TABLES`, `BOLD_TIMESTAMP`, `BOLD_XFILES` and `BOLD_CLOCKLOG`), so existing Bold databases open | M5 |
| D6 | Model in memory | The Mold model only, with no UML metamodel object space; the editor and XMI work on it too | M1 |
| D7 | OLLE | Restored to `Source/Extensions/OLLE/Core`, with the "OLE" wording fixed; ported after M5 | Restored; port after M5 |
| D8 | Multi-user transport | SignalR on the same ASP.NET Core host as remote persistence | M9 |

## Sources

- Code facts: `CSharpPortSource` at `e930283`, `CSharp` at `46f8075` and `master` at `7cb43aa` in `ismailozenc/BoldForDelphi`.
- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy): .NET 10 is LTS and supported until 14 November 2028; .NET 8 and 9 end on 10 November 2026.
- [dotnet new sln defaults to .slnx in the .NET 10 SDK](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/dotnet-new-sln-slnx-default)

  |  |  |  |
  | --- | --- | --- |
  |  |  |  |
  |  |  |  |
