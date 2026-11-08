# Bold for C#

Bold turns a UML model into a running object system: you describe the classes, attributes and associations, and Bold
gives you the objects, their storage in SQL Server or SQLite, OCL queries and derived members that stay current, undo,
multi-user synchronization and Bold-aware WinForms controls. It is the C# port of Bold for Delphi, on .NET 10, with
Delphi Bold's class names.

| Package | What |
|---|---|
| `Bold.ObjectSpace` | The object space: systems, objects, attributes, associations, OCL, undo. Brings `Bold.Core`, `Bold.Model`, `Bold.ValueSpace` and `Bold.Persistence`. |
| `Bold.Persistence.Sql` | Storage in SQL databases through ADO.NET: schema generation, evolution, SQL Server and SQLite dialects. |
| `Bold.Handles` | Non-visual handles: system, list, expression, reference and variable handles that follow the objects. |
| `Bold.AwareGui`, `Bold.WinForms` | Bold-aware GUI logic, and the WinForms controls built on it (Windows). |
| `Bold.Propagation` | Multi-user clients: snooper, listener, ID adder, pessimistic locks. |
| `Bold.Propagator.Server`, `Bold.Remote.Server`, `Bold.Remote.Client` | The propagator server and remote persistence, to host in ASP.NET Core, and the remote persistence client. |
| `Bold.Xmi`, `Bold.CodeGen`, `Bold.Olle` | XMI import and export, code generation, object lending between databases. |
| `Bold.SourceGenerators` | The attribute generator: custom attribute types from attributes on partial classes. |
| `bold-codegen` | A .NET tool that writes typed C# classes from a model. |

A minimal system on a SQLite database:

```csharp
using System.ComponentModel;
using Bold;
using Microsoft.Data.Sqlite;

var components = new Container();
var model = new TBoldAbstractModel(components) { Name = "Library" };
var lines = new TStringList();
lines.LoadFromFile("Library.bld");
model.ReadModel(lines);

var adapter = new TBoldDatabaseAdapterADO(components) { Connection = new TADOConnection(new SqliteConnection("Data Source=library.db")) };
adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
var database = new TBoldPersistenceHandleDB(components) { BoldModel = model, DatabaseAdapter = adapter };
database.CreateDataBaseSchema(); // once, for a new database: it replaces Bold's tables

var typeInfo = new TBoldSystemTypeInfoHandle(components) { BoldModel = model, UseGeneratedCode = false };
var systemHandle = new TBoldSystemHandle(components) { SystemTypeInfoHandle = typeInfo, PersistenceHandle = database, Active = true };
TBoldSystem system = systemHandle.System!;

TBoldObject book = system.CreateNewObjectByExpressionName("Book");
book.BoldMemberByExpressionName("title").AsString = "The Dispossessed";
system.UpdateDatabase();
Console.WriteLine(system.EvaluateExpressionAsString("Book.allInstances->size", BoldDefs.brDefault));
```

`bold-codegen Library.bld` writes typed classes (`TBook` with a `Title` property); with them, leave `UseGeneratedCode` at
its default.

The user guide, the examples and the sources are at https://github.com/ismailozenc/BoldForDelphi/tree/CSharp/CSharp.
