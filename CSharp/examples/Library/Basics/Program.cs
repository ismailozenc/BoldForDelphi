// Tutorial 1, a first system: the Library model read from Library.bld, a new SQLite database, objects created and
// linked by name, OCL queries and derived members, saving, and the objects read back in a new system.
//
//   dotnet run --project examples/Library/Basics [database file]
using System.ComponentModel;
using Bold;
using Microsoft.Data.Sqlite;

string databaseFile = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "library-basics.db");
using var components = new Container();

// 1. The model: classes, attributes and associations, read from the .bld file the model editor writes.
var model = new TBoldAbstractModel(components) { Name = "Library" };
var lines = new TStringList();
lines.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Library.bld"));
model.ReadModel(lines);
Console.WriteLine($"The model {model.MoldModel.Name}: {model.MoldModel.Classes.Count} classes, {model.MoldModel.Associations.Count} associations.");

// 2. The database: an ADO.NET connection, the SQL dialect, and a persistence handle that maps the model onto tables.
File.Delete(databaseFile);   // each run starts with a new database
var connection = new TADOConnection(new SqliteConnection($"Data Source={databaseFile}"));
var adapter = new TBoldDatabaseAdapterADO(components) { Connection = connection };
adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
var database = new TBoldPersistenceHandleDB(components) { BoldModel = model, DatabaseAdapter = adapter };
database.CreateDataBaseSchema();   // the tables of the model and Bold's own; on a database that has them, it asks first
Console.WriteLine($"A new database in {databaseFile}.");

// 3. The system: the object space, on the model and the database. Without generated classes, objects are TBoldObjects.
var typeInfo = new TBoldSystemTypeInfoHandle(components) { BoldModel = model, UseGeneratedCode = false };
var systemHandle = new TBoldSystemHandle(components) { SystemTypeInfoHandle = typeInfo, PersistenceHandle = database };
systemHandle.Active = true;
TBoldSystem system = systemHandle.System!;

// 4. Objects, their attributes and their links, reached by the names the model gives them (in OCL's lower camel case).
TBoldObject NewAuthor(string name)
{
    TBoldObject author = system.CreateNewObjectByExpressionName("Author");
    author.BoldMemberByExpressionName("name").AsString = name;
    return author;
}

TBoldObject NewBook(string title, int year, TBoldObject author)
{
    TBoldObject book = system.CreateNewObjectByExpressionName("Book");
    book.BoldMemberByExpressionName("title").AsString = title;
    ((TBAInteger)book.BoldMemberByExpressionName("year")).AsInteger = year;
    // A single link is an object reference; setting it also adds the book to the author's books.
    ((TBoldObjectReference)book.BoldMemberByExpressionName("writtenBy")).BoldObject = author;
    return book;
}

TBoldObject leGuin = NewAuthor("Ursula K. Le Guin");
TBoldObject lem = NewAuthor("Stanislaw Lem");
NewBook("The Left Hand of Darkness", 1969, leGuin);
NewBook("The Dispossessed", 1974, leGuin);
TBoldObject solaris = NewBook("Solaris", 1961, lem);

// A multi link is an object list: adding a loan to it sets the loan's other end.
TBoldObject ann = system.CreateNewObjectByExpressionName("Member");
ann.BoldMemberByExpressionName("name").AsString = "Ann";
TBoldObject loan = system.CreateNewObjectByExpressionName("Loan");
((TBoldObjectList)ann.BoldMemberByExpressionName("loans")).Add(loan);
((TBoldObjectReference)loan.BoldMemberByExpressionName("book")).BoldObject = solaris;

// 5. OCL: queries on the system or on an object; derived members are OCL in the model, and follow what they read.
string Ocl(TBoldElement on, string expression) => on.EvaluateExpressionAsString(expression, BoldDefs.brDefault);

// A collection comes back as a new list, which the caller frees; its elements are values or objects.
string List(TBoldElement on, string expression)
{
    using var list = (TBoldList)on.EvaluateExpressionAsNewElement(expression)!;
    return string.Join(", ", list.Cast<TBoldElement>().Select(element => element.AsString));
}

Console.WriteLine($"Books: {Ocl(system, "Book.allInstances->size")}");
Console.WriteLine($"Before 1970: {List(system, "Book.allInstances->select(year < 1970)->collect(title)")}");
Console.WriteLine($"{Ocl(leGuin, "name")} wrote {Ocl(leGuin, "bookCount")} books (bookCount is derived: books->size).");
Console.WriteLine($"Solaris: {Ocl(solaris, "copies")} copy, {Ocl(solaris, "available")} available (copies - loans->size).");
Console.WriteLine($"Ann has {Ocl(ann, "loanCount")} loan, of {List(ann, "loans.book.title")}.");

// 6. Saving: what changed goes to the database, in one transaction.
Console.WriteLine($"{system.DirtyObjects.Count} new objects to save.");
system.UpdateDatabase();
Console.WriteLine($"Saved; {system.DirtyObjects.Count} left to save.");

// 7. A new system on the same database: objects are fetched when a list or a link first needs them.
systemHandle.Active = false;
systemHandle.Active = true;
system = systemHandle.System!;
Console.WriteLine("In a new system:");
foreach (TBoldObject author in system.ClassByExpressionName("Author"))
{
    Console.WriteLine($"  {Ocl(author, "name")}: {List(author, "books->orderby(year)->collect(title + ' (' + year.asString + ')')")}");
}

Console.WriteLine($"  Lent: {List(system, "Loan.allInstances->collect(member.name + ' has ' + book.title)")}");

systemHandle.Active = false;
connection.Close();
