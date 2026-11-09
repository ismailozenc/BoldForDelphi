// Tutorial 2, typed classes: the classes bold-codegen generated from Library.bld (the LibraryClasses project), with
// typed properties, links and lists, the model's methods written in C#, and a member derived in code.
//
//   dotnet run --project examples/Library/Typed [database file]
using System.ComponentModel;
using Bold;
using LibraryClasses;
using Microsoft.Data.Sqlite;

string databaseFile = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "library-typed.db");
File.Delete(databaseFile);
using var components = new Container();

// The model is embedded in the classes' assembly: the classes and the model they were generated from travel together.
// Using the assembly also registers its classes with Bold (a module initializer in LibraryClasses.g.cs).
var model = new TBoldAbstractModel(components) { Name = "Library" };
model.ReadModel(LibraryModel.Lines());

var connection = new TADOConnection(new SqliteConnection($"Data Source={databaseFile}"));
var adapter = new TBoldDatabaseAdapterADO(components) { Connection = connection };
adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
var database = new TBoldPersistenceHandleDB(components) { BoldModel = model, DatabaseAdapter = adapter };
database.CreateDataBaseSchema();

// UseGeneratedCode is on by default: the objects are TAuthors, TBooks, TMembers and TLoans.
var typeInfo = new TBoldSystemTypeInfoHandle(components) { BoldModel = model };
var systemHandle = new TBoldSystemHandle(components) { SystemTypeInfoHandle = typeInfo, PersistenceHandle = database };
systemHandle.Active = true;
TBoldSystem system = systemHandle.System!;

// 1. Objects are created with new, in a system; attributes and single links are properties.
var leGuin = new TAuthor(system) { Name = "Ursula K. Le Guin" };
var earthsea = new TBook(system) { Title = "A Wizard of Earthsea", Year = 1968, Copies = 2, WrittenBy = leGuin };
var dispossessed = new TBook(system) { Title = "The Dispossessed", Year = 1974 };
leGuin.Books.Add(dispossessed);   // a multi link is a typed list; adding to it sets the book's WrittenBy too
Console.WriteLine($"{dispossessed.Title} is by {dispossessed.WrittenBy?.Name}; it has {dispossessed.Copies} copy (the model's initial value).");

// 2. Lists work with foreach and LINQ; a loop over a typed list names the class.
foreach (TBook book in leGuin.Books)
{
    Console.WriteLine($"  {book.Year} {book.Title}");
}

Console.WriteLine($"{leGuin.Name}: {leGuin.BookCount} books, {leGuin.Books.Cast<TBook>().Sum(book => book.Copies)} copies.");

// 3. Methods: Member.Borrow is declared in the model and written in Member.cs; it lends a copy while one is left.
var ann = new TMember(system) { Name = "Ann" };
var bob = new TMember(system) { Name = "Bob" };
var cy = new TMember(system) { Name = "Cy" };
TLoan annsLoan = ann.Borrow(earthsea)!;
Console.WriteLine($"Ann borrows {annsLoan.Book!.Title}, due {annsLoan.Due:yyyy-MM-dd} (TLoan.CompleteCreate sets it).");
bob.Borrow(earthsea);
Console.WriteLine($"After Bob: {earthsea.Available} of {earthsea.Copies} copies available (derived in OCL: copies - loans->size).");
Console.WriteLine(cy.Borrow(earthsea) is null ? "Cy gets none: no copy is left." : "Cy gets a copy.");

// 4. A member derived in code: Loan.Overdue (Loan.cs) is derived again when the due date it subscribes to changes.
Console.WriteLine($"Overdue: {annsLoan.Overdue}");
annsLoan.Due = DateTime.Today.AddDays(-1);
Console.WriteLine($"Due yesterday, overdue: {annsLoan.Overdue}");

// 5. Returning a book deletes its loan; the derived members follow.
annsLoan.Delete();
Console.WriteLine($"Ann returns it: {earthsea.Available} available, Ann has {ann.LoanCount} loans.");

// 6. Saved and read back, the objects are of the generated classes again; a class's list is a typed list.
system.UpdateDatabase();
systemHandle.Active = false;
systemHandle.Active = true;
system = systemHandle.System!;
var members = (TMemberList)system.ClassByExpressionName("Member");
foreach (TMember member in members)
{
    string loans = string.Join("; ", member.Loans.Cast<TLoan>().Select(loan => $"{loan.Book?.Title}, due {loan.Due:yyyy-MM-dd}"));
    Console.WriteLine($"  {member.Name}: {(loans == "" ? "no loans" : loans)}");
}

systemHandle.Active = false;
connection.Close();
