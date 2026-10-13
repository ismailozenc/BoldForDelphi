# How we port

We rebuild Bold's behaviour in C#, unit by unit, test first. The Delphi code stays in [`../Source`](../Source) as the specification. Everything ported is linked to its Delphi origin and carries a port status, so you can always find what came from where, and how far it got.

## Same names

The C# code keeps the Delphi names, so a search for a Delphi name finds the C# code and the other way round.

| Delphi | C# |
|---|---|
| `Source/Common/Core/BoldDefs.pas` | `CSharp/src/Bold.Core/Common/Core/BoldDefs.cs`: the project from [port-map.json](port/port-map.json), then the path under `Source` |
| Types `TBoldSystem`, `EBold`, `IBoldValue` | The same names, prefixes included, all in namespace `Bold` |
| Unit-level constants, routines and variables | Members of `public static partial class BoldDefs` (the unit name); use them with `using static Bold.BoldDefs;` where Delphi has `uses BoldDefs` |
| `TBoldTimeStampType = integer` | `global using TBoldTimeStampType = int;` in `BoldDefs.Aliases.cs`; projects that reference Bold.Core see it too |
| `set of TBoldValuePersistenceState` | `DelphiSet<TBoldValuePersistenceState>` (Bold.Rtl), with `+ - * <= >=` as in Delphi |
| `procedure(Sender: TObject) of object` | A delegate with the same parameter names |
| `constructor Create(...)` | A C# constructor |
| `constructor CreateFromStream(...)` | `public static TFoo CreateFromStream(...)` |
| `destructor Destroy` | `Dispose()` (IDisposable) |
| `EBold = class(Exception)` | `public class EBold(string? message = null, Exception? innerException = null) : Exception(message, innerException);` |
| `property Name read GetName write SetName` | A C# property `Name`; `GetName` and `SetName` take the property's status |
| Members, parameters, enum values | The same names and order |

A large unit may be split into partial files named `<Unit>.<Part>.cs` (for example `BoldSystem.TBoldObject.cs`); the unit is the part of the file name before the first dot. Tests follow the same layout: `tests/Bold.Core.Tests/Common/Core/BoldDefsTests.cs`.

## Port status on every element

Every C# element that ports a Delphi element carries `[Delphi(...)]`:

```csharp
[Delphi(PortStatus.Ported)] public const int NO_CLASS = -1;

[Delphi(PortStatus.Ported, Symbol = "TBoldSystem.Destroy")]
public void Dispose() { ... }

[Delphi(PortStatus.Replaced, Note = "List<T> replaces TBoldIndexableList")]
public sealed class TBoldObjectList { ... }
```

| Status | Use it when | Note |
|---|---|---|
| `Stub` | The declaration exists with the Delphi name; the body throws `NotImplementedException` | |
| `Partial` | Part of the behaviour is ported | Say what is missing |
| `Ported` | All of the behaviour is ported | |
| `Replaced` | A .NET feature or a new design does the job | Say what replaces it |
| `Dropped` | Not ported on purpose | Say why |

You never write "verified": the port tool shows an element as ✅ verified only when it is `Ported` or `Replaced` and a test that covers it passed. The tool finds the Delphi unit from the C# file name and the Delphi symbol from the C# name (`TFoo.Bar`, or just `Bar` inside the unit class). Set `Symbol` only when the names differ: for overloads (`Symbol = "TFoo.Add(Integer)"`), for `Dispose`, or for the unit's `initialization`. Set `Unit` when the code is in a file named after another unit. A Delphi element with no C# declaration to carry the attribute (a type alias, a dropped routine) gets an assembly-level marker with both:

```csharp
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefs", Symbol = "TBoldTimeStampType", Note = "Global using alias of int")]
```

Elements of a unit that the port map marks as not ported show as ⚫ dropped without markers. C# code with no Delphi counterpart (helpers, .NET plumbing) needs no marker; the unit page lists it under "C# additions".

## Tests

Tests use NUnit 4. A test says what it checks with `[Covers]`:

```csharp
[Test]
[Covers(typeof(TBoldFoo), nameof(TBoldFoo.Add), nameof(TBoldFoo.Count))]
public void Add_increases_the_count() { ... }

[Covers(typeof(BoldDefs), "*")]            // every marked member of the type
[Covers("BoldDefs", "TBoldTimeStampType")] // Delphi symbols with assembly-level markers
```

`[Covers]` on a test class covers the symbols for every test in the class. Take expected values from the Delphi source where you can, so a wrong value cannot be typed into both the code and the test: `PortAssert.ConstantsMatchDelphi`, `EnumsMatchDelphi`, `ClassParentsMatchDelphi` and `DelegatesMatchDelphi` in Bold.TestSupport compare C# declarations with the Delphi unit, and `DelphiSource.Constants` gives the value of any Delphi constant. Model tests use the files in [`tests/Fixtures/Models`](tests/Fixtures/Models), copied from master by the `fixtures` command.

## Porting a unit

1. Open its page in [`port/units`](port/units). It lists every symbol to port with links to the Delphi declaration and body, and shows the planned C# file.
2. Create the C# file at that path and port the unit, with a `[Delphi]` marker on each element.
3. Write the tests with `[Covers]`.
4. Build, run the tests with a results file, then update the links and pages:

   ```
   dotnet test CSharp/Bold.slnx --logger trx --results-directory CSharp/TestResults
   dotnet run --project CSharp/tools/Bold.PortTool -- all
   dotnet run --project CSharp/tools/Bold.PortTool -- check
   ```

5. Commit the C# code, the tests, and the files the tool changed.

## Port tool commands

Run them from the repository root: `dotnet run --project CSharp/tools/Bold.PortTool -- <command>`.

| Command | What it does |
|---|---|
| `report` | Writes [`port/PORT_STATUS.md`](port/PORT_STATUS.md) and the unit pages, using the test results in `CSharp/TestResults` (`--results <dir>` for another folder). |
| `sync` | Updates the links kept in the sources: the comment after `unit X;` in each Delphi unit, the "C# port" block in each `FOLDER_INFO.md` under `Source`, and the `// Delphi:` header of each C# file. Delphi line numbers do not change. |
| `all` | `sync`, then `report`. |
| `check` | Fails when a marker names a symbol that does not exist, two C# elements claim one Delphi symbol, a `[Covers]` names something missing, a unit is missing from the port map, or `sync` has work to do. CI runs it. |
| `summary` | A short markdown status; CI puts it on the job page. |
| `inventory` | What the Delphi parser found; `--warnings` lists parse problems, `--json <file>` writes everything. |
| `fixtures` | Copies every model on master (`.bld` files, and `TBoldModel` components in text and binary forms) into `tests/Fixtures/Models`, with `models.json` and a README saying where each came from. |

## Building without NuGet access to the test adapter

Where the package feed has NUnit and NUnitLite but not `Microsoft.NET.Test.Sdk` or `NUnit3TestAdapter`, build with `-p:BoldOffline=true`. Each test project then becomes a NUnitLite program: run its dll with `--result=CSharp/TestResults/<project>.xml` and the port tool reads those results like the TRX files of `dotnet test`.
