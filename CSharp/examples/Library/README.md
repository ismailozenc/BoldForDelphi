# The Library examples

Nine small programs on one model, a lending library, each showing one thing Bold does. Each is the subject of a
tutorial in the user guide's Tutorials tab (a copy is in [`docs/TUTORIALS.md`](../../docs/TUTORIALS.md)), and each is
run by a test, so what the tutorials say stays true.

| Folder | Tutorial | What it shows |
|---|---|---|
| [`Basics`](Basics) | 1. A first system | The model read from `Library.bld`, a new SQLite database, objects created and linked by name, OCL queries and derived members, saving, and the objects read back in a new system. |
| [`Typed`](Typed) | 2. Typed classes | The classes `bold-codegen` generated (`LibraryClasses`): typed properties, links and lists, the methods `Member.Borrow` and `Loan.CompleteCreate`, and `Loan.Overdue` derived in code. |
| [`Rules`](Rules) | 3. Business rules | Derived members that stay current and a subscription that hears of their changes, constraints checked before saving, transactions, undo and redo. |
| [`Desk`](Desk) | 4. A WinForms desk | Grids on list handles that follow each other, navigators, Bold's actions, an auto form; on a database of its own or on the server of tutorial 5 (Windows). |
| [`Network`](Network) | 5. Several desks over the network | One ASP.NET Core server with remote persistence and the propagator; two clients without a database of their own that see each other's changes and meet a pessimistic lock. |
| [`Evolution`](Evolution) | 6. Evolving the model | Version 2 of the model (`LibraryV2.bld`) renames an attribute, adds another and adds a class; the evolutor brings the database there without losing its objects. |
| [`Xmi`](Xmi) | 7. Models from UML tools | The Library model written as XMI 2.1, and a model in the XMI of Enterprise Architect (`Hotel.xmi`) read, written as `.bld` and run. |
| [`Attributes`](Attributes) | 8. Custom attribute types | Attribute types of the library's own, written by the attribute generator: a value set, a two-field attribute stored in two columns, and an ISBN that checks itself. |
| [`Branches`](Branches) | 9. Lending objects between databases | Two branch libraries with databases of their own: OLLE lends a book's record from one to the other, which changes it and checks it back in. |

## The model

[`Library.bld`](Library.bld) is the model the examples share: authors write books, members borrow them through loans.

| Class | Attributes | Derived (OCL) | Rules |
|---|---|---|---|
| `Author` | `Name` | `BookCount = books->size` | |
| `Book` | `Title`, `Year`, `Copies` (1 to start with) | `Available = copies - loans->size` | "Has a title", "Not lent beyond its copies" |
| `Member` | `Name` | `LoanCount = loans->size` | "At most three loans"; method `Borrow(Book)` |
| `Loan` | `Due` | `Overdue`, in code | `CompleteCreate` sets `Due` |

The associations are Authorship (an author's `books`, a book's `writtenBy`), Lending (a book's `loans`, a loan's
`book`) and Borrowing (a member's `loans`, a loan's `member`). The model's `Bold.RegionDefinitions` make a book's title,
year and copies one lock region, for tutorial 5.

[`LibraryClasses`](LibraryClasses) holds the classes `bold-codegen` generated from the model (the `*.g.cs` files), the
methods written by hand (`Member.cs`, `Loan.cs`), and the model itself, embedded. [`Shared/SqliteSystem.cs`](Shared/SqliteSystem.cs)
is the setup of tutorials 1 and 2 in one class (a model, a SQLite database and a system), which the later examples
use.

## Run them

From the `CSharp` folder:

```
dotnet run --project examples/Library/Basics
dotnet run --project examples/Library/Desk                     (Windows)
dotnet run --project examples/Library/Network -- --serve       the server alone; then Desk -- --server http://localhost:5000
```

The console examples write their databases to the temp folder (a path as the argument puts them elsewhere), and each
run starts from a new one. `tests/Bold.Examples.Tests` runs them; the desk's tests are in `tests/Bold.WinForms.Tests`.

Outside this repository, reference the packages in place of the projects: `Bold.Persistence.Sql` and `Bold.Handles`
for the console examples, and the ones each project file names.
