<!-- Written from the programs in CSharp/examples/Library and what they print. Each tutorial is run by a test (tests/Bold.Examples.Tests, and tests/Bold.WinForms.Tests for the desk), so update this page when a program changes. -->

# Bold for C# Tutorials

Nine tutorials on one model, a lending library. Each is a small program in [`examples/Library`](../examples/Library) that you can run and change, and each shows one thing Bold does. They follow the [user guide](USER_GUIDE.md), which explains the ideas; here you see them work.

| # | Tutorial | Program | Runs on |
| --- | --- | --- | --- |
| 1 | [A first system](#1-a-first-system) | `Basics` | Windows, Linux |
| 2 | [Typed classes](#2-typed-classes) | `Typed` | Windows, Linux |
| 3 | [Business rules](#3-business-rules) | `Rules` | Windows, Linux |
| 4 | [A WinForms desk](#4-a-winforms-desk) | `Desk` | Windows |
| 5 | [Several desks over the network](#5-several-desks-over-the-network) | `Network` | Windows, Linux |
| 6 | [Evolving the model](#6-evolving-the-model) | `Evolution` | Windows, Linux |
| 7 | [Models from UML tools](#7-models-from-uml-tools) | `Xmi` | Windows, Linux |
| 8 | [Custom attribute types](#8-custom-attribute-types) | `Attributes` | Windows, Linux |
| 9 | [Lending objects between databases](#9-lending-objects-between-databases) | `Branches` | Windows, Linux |

## Before you start

You need the .NET 10 SDK and a clone of the repository on the `CSharp` branch. Run every command from the `CSharp` folder:

```
dotnet run --project examples/Library/Basics
```

The console programs keep their databases in the temp folder and start from a new one on each run. A path as the argument puts the database elsewhere. The first run restores `Microsoft.Data.Sqlite` from nuget.org.

In an application of your own, reference the Bold packages in place of the projects: `Bold.Persistence.Sql` and `Bold.Handles` for most of these, and the ones each tutorial names. The user guide's Getting started section says how to make the packages.

## The model

All nine programs share [`Library.bld`](../examples/Library/Library.bld): authors write books, and members borrow them through loans.

| Class | Attributes | Derived in OCL | Rules |
| --- | --- | --- | --- |
| `Author` | `Name` | `BookCount = books->size` | |
| `Book` | `Title`, `Year`, `Copies` (1 to start with) | `Available = copies - loans->size` | "Has a title", "Not lent beyond its copies" |
| `Member` | `Name` | `LoanCount = loans->size` | "At most three loans"; the method `Borrow(Book)` |
| `Loan` | `Due` | `Overdue`, derived in code | `CompleteCreate` sets `Due` |

Three associations link them: an author's `books` and a book's `writtenBy`, a book's `loans` and a loan's `book`, a member's `loans` and a loan's `member`. To look at the model or change it, open it in the model editor, on Windows: `dotnet run --project src/Bold.ModelEditor -- examples/Library/Library.bld`.

## 1. A first system

**What it shows:** a model read from a `.bld` file, a new SQLite database, objects created and linked by name, OCL queries, saving, and the objects read back.

```
dotnet run --project examples/Library/Basics
```

A Bold application is four components in a chain. The model component reads the model, the persistence handle maps it onto a database, the type info handle builds the runtime types, and the system handle runs the object space on all three:

```csharp
using var components = new Container();
var model = new TBoldAbstractModel(components) { Name = "Library" };
var lines = new TStringList();
lines.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Library.bld"));
model.ReadModel(lines);

var connection = new TADOConnection(new SqliteConnection($"Data Source={databaseFile}"));
var adapter = new TBoldDatabaseAdapterADO(components) { Connection = connection };
adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
var database = new TBoldPersistenceHandleDB(components) { BoldModel = model, DatabaseAdapter = adapter };
database.CreateDataBaseSchema();

var typeInfo = new TBoldSystemTypeInfoHandle(components) { BoldModel = model, UseGeneratedCode = false };
var systemHandle = new TBoldSystemHandle(components) { SystemTypeInfoHandle = typeInfo, PersistenceHandle = database };
systemHandle.Active = true;
TBoldSystem system = systemHandle.System!;
```

`CreateDataBaseSchema` makes the model's tables and Bold's own. With `UseGeneratedCode = false` every object is a `TBoldObject`, and its members are reached by the names the model gives them, in OCL's lower camel case:

```csharp
TBoldObject book = system.CreateNewObjectByExpressionName("Book");
book.BoldMemberByExpressionName("title").AsString = "Solaris";
((TBAInteger)book.BoldMemberByExpressionName("year")).AsInteger = 1961;
((TBoldObjectReference)book.BoldMemberByExpressionName("writtenBy")).BoldObject = author;
```

Setting the book's `writtenBy` also adds the book to the author's `books`. Bold keeps both ends of an association in step.

OCL asks questions of the system or of one object. A derived member such as `bookCount` is OCL in the model, and it follows what it reads:

```csharp
on.EvaluateExpressionAsString("Book.allInstances->size", BoldDefs.brDefault);
using var list = (TBoldList)on.EvaluateExpressionAsNewElement("Book.allInstances->select(year < 1970)->collect(title)")!;
```

`system.UpdateDatabase()` saves what changed, in one transaction. Closing and opening the system handle gives a new system on the same database, which fetches objects when a list or a link first needs them.

**It prints:**

```
The model Library: 5 classes, 3 associations.
Books: 3
Before 1970: The Left Hand of Darkness, Solaris
Ursula K. Le Guin wrote 2 books (bookCount is derived: books->size).
Solaris: 1 copy, 0 available (copies - loans->size).
Ann has 1 loan, of Solaris.
7 new objects to save.
Saved; 0 left to save.
In a new system:
  Ursula K. Le Guin: The Left Hand of Darkness (1969), The Dispossessed (1974)
  Stanislaw Lem: Solaris (1961)
  Lent: Ann has Solaris
```

The model has five classes because Bold adds a root class, `LibraryRoot`, above the four.

## 2. Typed classes

**What it shows:** the classes `bold-codegen` generates from the model, with typed properties, links and lists, the model's methods written in C#, and a member derived in code.

```
dotnet run --project examples/Library/Typed
```

[`examples/Library/LibraryClasses`](../examples/Library/LibraryClasses) holds the generated classes. To generate them again after a model change:

```
dotnet run --project tools/Bold.CodeGen.Tool -- examples/Library/Library.bld --out examples/Library/LibraryClasses
```

The tool writes each `<Class>.g.cs` again on every run and only ever adds to `<Class>.cs`, which is yours. The project embeds `Library.bld`, so the classes and their model travel together, and `LibraryModel.Lines()` reads it.

With generated code (the default), objects are created with `new` in a system, and attributes and links are properties:

```csharp
var leGuin = new TAuthor(system) { Name = "Ursula K. Le Guin" };
var earthsea = new TBook(system) { Title = "A Wizard of Earthsea", Year = 1968, Copies = 2, WrittenBy = leGuin };
foreach (TBook book in leGuin.Books) { ... }
int copies = leGuin.Books.Cast<TBook>().Sum(book => book.Copies);
```

A method the model declares becomes a partial method to write. `Member.Borrow` is in [`Member.cs`](../examples/Library/LibraryClasses/Member.cs):

```csharp
public partial TLoan? Borrow(TBook? Book)
{
    if (Book is null || Book.Available <= 0)
    {
        return null;
    }

    return new TLoan(BoldSystem) { Book = Book, Member = this };
}
```

[`Loan.cs`](../examples/Library/LibraryClasses/Loan.cs) has the other two kinds of hand-written code. `CompleteCreate` runs when a loan is created and sets its due date. `_Overdue_DeriveAndSubscribe` derives `Overdue` in code and subscribes to the due date, so Bold derives it again when the date changes.

**It prints** (the dates follow the day you run it):

```
The Dispossessed is by Ursula K. Le Guin; it has 1 copy (the model's initial value).
  1968 A Wizard of Earthsea
  1974 The Dispossessed
Ursula K. Le Guin: 2 books, 3 copies.
Ann borrows A Wizard of Earthsea, due 2026-11-25 (TLoan.CompleteCreate sets it).
After Bob: 0 of 2 copies available (derived in OCL: copies - loans->size).
Cy gets none: no copy is left.
Overdue: False
Due yesterday, overdue: True
Ann returns it: 1 available, Ann has 0 loans.
  Ann: no loans
  Bob: A Wizard of Earthsea, due 2026-11-25
  Cy: no loans
```

From here on the programs use [`Shared/SqliteSystem.cs`](../examples/Library/Shared/SqliteSystem.cs), which is the setup of tutorials 1 and 2 in one class.

## 3. Business rules

**What it shows:** derived members that stay current, a subscription that hears of their changes, constraints checked before saving, transactions, and undo and redo.

```
dotnet run --project examples/Library/Rules
```

**Subscriptions.** Each member has an `M_` property for the member object itself. A subscriber on it is told when the value changes, whatever caused the change:

```csharp
using var subscriber = new TBoldPassthroughSubscriber((_, _, _) => Console.WriteLine("  (told: ...)"));
earthsea.M_Available.DefaultSubscribe(subscriber);
```

**Constraints.** A constraint is OCL in the model that should hold. Bold evaluates it, and the application decides what to do about a broken one. The OCL member `constraints` holds each constraint of an object as a Boolean:

```csharp
using var constraints = (TBoldList)boldObject.EvaluateExpressionAsNewElement("constraints->select(c | not c)")!;
```

The program's `Save` saves only when no object the save touches breaks a constraint.

**Transactions.** Changes in memory are kept or undone together:

```csharp
system.StartTransaction();
try { ...; system.CommitTransaction(); }
catch (InvalidOperationException) { system.RollbackTransaction(); }
```

**Undo.** The undo handler is off until you switch it on. Set a check point before each step the user may want to undo:

```csharp
IBoldUndoHandler undo = system.UndoHandlerInterface;
undo.Enabled = true;
undo.SetCheckPoint();
earthsea.Copies = 5;
undo.UndoLatest();
undo.RedoLatest();
```

**It prints:**

```
A Wizard of Earthsea: 3 available.
  (told: the available copies of A Wizard of Earthsea changed)
Ann borrows it: 2 available.
  (told: the available copies of A Wizard of Earthsea changed)
Bob borrows it: 1 available.
Ann borrows three more books (4 loans):
  Not saved: Member 'Ann': At most three loans.
  (told: the available copies of A Wizard of Earthsea changed)
Ann returns A Wizard of Earthsea (3 loans, 2 copies available):
  Saved.
A book loses its title:
  Not saved: Book '': Has a title.
  (told: the available copies of A Wizard of Earthsea changed)
Bob wants A Wizard of Earthsea and Tehanu together; Tehanu is out, so neither: he has 1 loan.
Two copies more: 4 available.
  (told: the available copies of A Wizard of Earthsea changed)
Undone: 2 available.
  (told: the available copies of A Wizard of Earthsea changed)
Redone: 4 available.
  Saved.
```

## 4. A WinForms desk

**What it shows:** grids on list handles that follow each other, navigators, Bold's actions on buttons, and an auto form. Windows only.

```
dotnet run --project examples/Library/Desk
dotnet run --project examples/Library/Desk -- --server http://localhost:5000
```

The first command works on `library-desk.db` in the current folder, made with a few books and members when it is not there. The second works on the server of tutorial 5.

**Handles.** A list handle hands out a list that stays current, and has a current element. A handle rooted in another follows that one's current element, so the books are those of the current author:

```csharp
Authors = new TBoldListHandle(components) { RootHandle = system, Expression = "Author.allInstances" };
Books = new TBoldListHandle(components) { RootHandle = Authors, Expression = "books" };
Members = new TBoldListHandle(components) { RootHandle = system, Expression = "Member.allInstances" };
Loans = new TBoldListHandle(components) { RootHandle = Members, Expression = "loans" };
```

**Grids.** A grid shows a list handle, and a column is an OCL expression on the row's object. A new grid has its fixed column and one empty column, which the first expression takes:

```csharp
var grid = new TBoldGrid { BoldAutoColumns = false, AddNewAtEnd = false, Dock = DockStyle.Fill };
grid.BoldHandle = handle;
TBoldGridColumn column = grid.Columns[1];        // then grid.AddColumn() for the next ones
column.BoldProperties.Expression = "available";
column.Title.Caption = "Available";
```

`MemberGrid.BoldShowConstraints = true` adds a column that marks a member who breaks a constraint. A `TBoldNavigator` on a handle moves through its list, and adds and deletes. A double click on a row opens the object's auto form, which Bold builds from the model.

**Actions.** Undo, redo and saving are Bold's actions, linked to buttons. They enable their buttons when there is something to do:

```csharp
TBoldActionLink.Link(UndoButton, new TBoldUndoAction(components) { BoldSystemHandle = system });
TBoldActionLink.Link(SaveButton, new TBoldUpdateDBAction(components) { Caption = "Save", BoldSystemHandle = system });
```

Lending and returning are the desk's own code. Each sets an undo check point first, then calls `Member.Borrow` or deletes the loan. Closing the window with unsaved changes asks whether to save them.

**Try it:** select an author and watch the books change. Select a book and a member and click **Lend the book to the member**: the loan appears, and the book's Available column drops. Click **Undo**, then **Redo**. Lend Ann books until she has four loans, and her row is marked: the model allows three.

## 5. Several desks over the network

**What it shows:** one server with the database behind remote persistence and the propagator, and two clients without a database that see each other's changes and meet a pessimistic lock.

```
dotnet run --project examples/Library/Network
dotnet run --project examples/Library/Network -- --serve
```

The second command runs the server alone on `http://localhost:5000`, for WinForms desks started with `--server`.

**The server** ([`LibraryServer.cs`](../examples/Library/Network/LibraryServer.cs)) is an ASP.NET Core host. `TBoldRemotePersistenceSetup` puts a snooper in front of the database, which tells the propagator what is saved:

```csharp
builder.Services.AddBoldPropagator();
app.MapBoldPersistence(setup.PersistenceHandle, setup.Model.MoldModel);   // POST /persistence
app.MapBoldPropagator();                                                  // the hub at /propagator
```

In production, run `Bold.Remote.Server` as it is, or map the same two endpoints in your own application.

**A client** ([`LibraryClient.cs`](../examples/Library/Network/LibraryClient.cs)) has the server where tutorial 1 had the database. A listener hears from the propagator what others saved, and an event handler makes the system read those objects again:

```csharp
var server = new TBoldHttpClientPersistenceHandle(components) { BoldModel = model, Url = serverUrl + "/persistence" };
var propagator = new TBoldSignalRPropagatorHandle(components) { Url = serverUrl + "/propagator" };
var events = new TBoldExternalObjectSpaceEventHandler(components) { BoldSystemHandle = SystemHandle, PropagatorHandle = propagator };
Listener = new TBoldListenerHandle(components) { PropagatorHandle = propagator, Dequeuer = events, ClientIdentifierString = name };
SystemHandle.PersistenceHandle = new TBoldIDAdderHandle(components) { NextPersistenceHandle = server, BoldListener = Listener };
```

**Locks.** A `TBoldLockingHandle` with a `TBoldSignalRLockManagerHandle` takes a lock before a member of a lock region changes. The model's `Bold.RegionDefinitions` make a book's title, year and copies one region. A second client that changes the same book gets `EBoldGetLocksFailed`, which names the holder. Saving releases the lock.

**A console program has no message loop**, so `TBoldConsoleEnvironmentConfiguration.Install()` queues what the propagator brings and runs it while the program waits in `WaitUntil`. A WinForms application needs none of this.

**It prints** (the port differs on each run):

```
The server listens at http://127.0.0.1:45697.
Two desks are connected: Front desk and Back office.
Front desk has 0 books.
Back office saves Solaris; Front desk has 1 book: Solaris.
Front desk: 1 copy available.
Back office lends Solaris to Ann; Front desk: 0 copies available.
Front desk changes the copies to 2, and holds the lock of the book's region.
Back office cannot change the title: Front desk holds the lock.
Front desk saves; Back office sees 2 copies, then changes the title, which Front desk sees: Solaris (1961).
```

## 6. Evolving the model

**What it shows:** a database made with version 1 of the model, evolved to version 2 without losing its objects.

```
dotnet run --project examples/Library/Evolution
```

[`LibraryV2.bld`](../examples/Library/Evolution/LibraryV2.bld) renames `Book.Year` to `Published`, adds `Book.Isbn`, and adds the class `Reservation` with associations to `Member` and `Book`. The rename is known to Bold because the new attribute carries the tagged value `Bold.FormerNames=year`. Without it, the evolutor would drop one column and add another, and the years would be lost.

The evolutor compares the database with the model, shows what it will do, and runs it in one transaction:

```csharp
var evolutor = new TBoldDataBaseEvolutor(v2.Database);   // version 2's persistence handle, its system still closed
evolutor.CalculateScript();
evolutor.GenerateScript(sql, mapping);     // the SQL, each step under a comment
evolutor.GenerateWarnings(warnings);       // what the evolution loses, if anything
evolutor.ExecuteScript();
```

Read the warnings before you run the script on a database you care about.

**It prints:**

```
Version 1: The Left Hand of Darkness (1969), Solaris (1961).
The evolution (17 SQL statements):
  Add table Reservation
  Add column Book.Isbn
  Add column Book.Published
  Move data from Book.Year to Book.Published (dbtypes: 2)
  Drop column Book.Year
  No warnings: no values are lost.
Version 2: The Left Hand of Darkness (1969), Solaris (1961).
Saved and read again: Solaris, ISBN 978-0-306-40615-7; Ann waits for Solaris.
```

## 7. Models from UML tools

**What it shows:** the Library model written as XMI 2.1 for a UML tool, and a model in Enterprise Architect's XMI read, kept as `.bld`, and run.

```
dotnet run --project examples/Library/Xmi
```

Reference `Bold.Xmi`. One link class reads and writes:

```csharp
TMoldModel library = TMoldBLDRW.ModelFromFile("Library.bld");
var export = new TBoldUMLXMILink { FileName = "Library.xmi", XMIVersion = TBoldXMIVersion.xmi21 };
export.ExportModel(library);

var import = new TBoldUMLXMILink { FileName = "Hotel.xmi" };
TMoldModel hotel = import.ImportMoldModel();
TMoldBLDRW.ModelToFile(hotel, "Hotel.bld");
```

The export leaves out what Bold adds to a model, so the file has the four classes you drew. The import reads XMI 1.x and 2.x, flattens the packages and "boldifies" the model: a root class above the others, names for unnamed association ends, and Bold's tagged values. Stereotypes and tagged values set in the UML tool come through, such as `Bold.TableName`.

Once the model is a `.bld` file, the model editor opens it, `bold-codegen` generates its classes, and a system runs on it.

**It prints:**

```
Library.xmi: 4 UML classes and 3 associations, for a UML tool.
Hotel.xmi: the model Hotel, root class BusinessClassesRoot:
  Guest: name: String, Checkout()
  Room: number: Integer, rate: Integer
  Suite (a Room): lounge: Boolean
  Bed: size: String
  (set in the UML tool: Guest's stereotype person and its tagged value Bold.TableName=GUESTS)
  Stay: Guest.room [0..1] and Room.guests [0..*]
  Furnishing: Room.beds [1..*] and Bed.room [0..1]
Written as Hotel.bld.
Saved and read again: Suite 101, 1 bed, guests Ann.
```

## 8. Custom attribute types

**What it shows:** attribute types of the library's own, written by the attribute generator: a value set, a two-field attribute stored in two columns, and an ISBN that checks itself.

```
dotnet run --project examples/Library/Attributes
```

Reference `Bold.SourceGenerators` as an analyzer. In this repository that is a project reference with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`; the package does it for you.

Attributes on a partial class say what the type is, and the generator writes the rest when the project builds ([`CatalogueTypes.cs`](../examples/Library/Attributes/CatalogueTypes.cs)):

```csharp
[BoldValueSet("Condition", ValuePrefix = "c")]
[BoldValueSetValue("New")]
[BoldValueSetValue("Good")]
[BoldValueSetValue("Worn")]
[BoldValueSetValue("Damaged")]
public partial class TBACondition : TBAValueSet { }            // TConditionEnum, AsCondition, the values

[BoldAttributeType("ShelfMark", GenerateMapper = true)]
[BoldAttributeField("Shelf", typeof(string))]
[BoldAttributeField("Position", typeof(int))]
public partial class TBAShelfMark : TBoldAttribute { ... }     // AsShelf, AsPosition, a mapper on two columns

[BoldAttributeType("Isbn")]
public partial class TBAIsbn : TBAString { ... }               // a string with a rule of its own
```

What is your own rule you write in the partial class: `TBAShelfMark` reads and writes "L-3", and `TBAIsbn.ValidateString` takes only a correct check digit.

The model ([`Catalogue.bld`](../examples/Library/Attributes/Catalogue.bld)) names the types. Tell Bold the classes behind those names before the model is read:

```csharp
var typeNames = new TBoldTypeNameHandle(components);
TBAIsbn.AddTypeNameMapping(typeNames.Dictionary);
TBACondition.AddTypeNameMapping(typeNames.Dictionary);
TBAShelfMark.AddTypeNameMapping(typeNames.Dictionary);
model.TypeNameHandle = typeNames;
```

A value set's values are an enumeration in C# (`TConditionEnum.cWorn`), names in strings (`"Worn"`), and `#Worn` in OCL.

**It prints:**

```
Solaris: condition New (the default, the first value), shelf L, position 3.
The conditions: New,Good,Worn,Damaged; in C#, cNew, cGood, cWorn, cDamaged.
Worn: Solaris.
An ISBN with a wrong check digit: refused (Copy.isbn.SetStringRepresentation failed: String validation failed).
A shelf mark without a position: refused ("L" is not a shelf mark (shelf-position, such as B-12)).
  In the table Copy: A Wizard of Earthsea | Shelf L | Shelf_1 7
  In the table Copy: Solaris | Shelf M | Shelf_1 1
Read again: Solaris, ISBN 978-0-306-40615-7, Worn, shelf M-1.
Read again: A Wizard of Earthsea, ISBN 978-1-234-56789-7, Good, shelf L-7.
```

For typed classes on a model with custom types, give `bold-codegen` their names with `--typenames <file>`.

## 9. Lending objects between databases

**What it shows:** two branch libraries with databases of their own and no server between them. OLLE lends a book's record from one to the other, which changes it and checks it back in.

```
dotnet run --project examples/Library/Branches
```

Reference `Bold.Olle`. Each branch adds an OLLE handle to its database and makes OLLE's own tables once:

```csharp
Olle = new TBoldOLLEHandle(components) { ApplicationPersistenceHandle = database };
Olle.OLLEController.GenerateDatabase();
```

The objects travel as a value space. Here it stays in memory; between real branches you send it as a file or over the network. The owner lends, and the other side takes the objects in. The second list names the ones the receiver holds, which it may change; the rest are copies it can only read:

```csharp
using var lent = new TBoldFreeStandingValueSpace();
central.Olle.OwnObjectHandler.Get(ids, held, "Harbour", lent);
harbour.Olle.ForeignObjectHandler.Put(lent, ids, held, "Central");
```

While Harbour holds the book, Central cannot change it. Checking in takes three steps, so that neither side loses a change if the other fails:

```csharp
harbour.Olle.ForeignObjectHandler.StartCheckIn(ids, release, changes, "Central");
central.Olle.OwnObjectHandler.CheckIn(changes, ids, release, "Harbour", ref version);
harbour.Olle.ForeignObjectHandler.AcknowledgeCheckIn("Central", version);
```

The model must use global ids, timestamps and read-only flags, which are Bold's defaults.

**It prints:**

```
Central: Solaris (1 copy), The Cyberiad (1 copy).
Harbour: Solaris (1 copy).
  Central changes Solaris: refused (Book.copies.SetDataValue failed: Member is read only).
  Harbour changes its author: refused (Author.name.SetDataValue failed: Member is read only).
Harbour saved: Solaris (3 copies).
Checked in, Central: Solaris (3 copies), The Cyberiad (1 copy).
  Central changes Solaris: done.
  Harbour changes Solaris: refused (Book.copies.SetDataValue failed: Member is read only).
```

## Where to go next

- The [user guide](USER_GUIDE.md) covers each topic in full, with deployment and troubleshooting.
- [`examples/Building/BldOwn`](../examples/Building/BldOwn) is Delphi Bold's Buildings and Owners application on WinForms.
- [`examples/CustomAttributes`](../examples/CustomAttributes) has the three kinds of attribute type on a model of shapes.
- If something here does not behave as written, run the tests that cover it: `dotnet test tests/Bold.Examples.Tests`, and `dotnet test tests/Bold.WinForms.Tests` on Windows for the desk.
