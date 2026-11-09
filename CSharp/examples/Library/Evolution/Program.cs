// Tutorial 6, evolving the model: a database made with the Library model, evolved to version 2 of the model
// (LibraryV2.bld) without losing its objects. Version 2 renames Book.Year to Published, adds Book.Isbn, and adds the
// class Reservation with its associations to Member and Book.
//
//   dotnet run --project examples/Library/Evolution [database file]
using Bold;
using LibraryExamples;

string databaseFile = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "library-evolution.db");

TStringList Model(string file)
{
    var lines = new TStringList();
    lines.LoadFromFile(Path.Combine(AppContext.BaseDirectory, file));
    return lines;
}

string List(TBoldElement on, string expression)
{
    using var list = (TBoldList)on.EvaluateExpressionAsNewElement(expression)!;
    return string.Join(", ", list.Cast<TBoldElement>().Select(element => element.AsString));
}

// 1. Version 1: a new database, with books and a member. (Both versions are used by name here: the generated classes are version 1's.)
using (var v1 = new SqliteSystem(databaseFile, newDatabase: true, Model("Library.bld"), useGeneratedCode: false))
{
    foreach ((string title, int year) in new[] { ("The Left Hand of Darkness", 1969), ("Solaris", 1961) })
    {
        TBoldObject book = v1.System.CreateNewObjectByExpressionName("Book");
        book.BoldMemberByExpressionName("title").AsString = title;
        ((TBAInteger)book.BoldMemberByExpressionName("year")).AsInteger = year;
    }

    v1.System.CreateNewObjectByExpressionName("Member").BoldMemberByExpressionName("name").AsString = "Ann";
    v1.System.UpdateDatabase();
    Console.WriteLine($"Version 1: {List(v1.System, "Book.allInstances->collect(title + ' (' + year.asString + ')')")}.");
}

// 2. Version 2 on the same database, closed: its tables are still version 1's.
using var v2 = new SqliteSystem(databaseFile, newDatabase: false, Model("LibraryV2.bld"), useGeneratedCode: false, open: false);

// 3. The evolutor compares the database with the model, and works out what to change: the SQL, and what it means for the objects.
var evolutor = new TBoldDataBaseEvolutor(v2.Database);
evolutor.CalculateScript();
var sql = new TStringList();
var mapping = new TStringList();
evolutor.GenerateScript(sql, mapping);
// The script is SQL statements, each step under a comment that says what it does.
string[] script = sql.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
Console.WriteLine($"The evolution ({script.Count(line => line.EndsWith(';'))} SQL statements):");
foreach (string step in script.Where(line => line.StartsWith("/*")))
{
    Console.WriteLine($"  {step.Trim('/', '*', ' ')}");
}

var warnings = new TStringList();
evolutor.GenerateWarnings(warnings);   // what the evolution loses, such as the values of a member that goes
Console.WriteLine(warnings.Count == 0 ? "  No warnings: no values are lost." : $"  Warnings: {warnings.Text}");

// 4. Run it: in one transaction, the tables change and the values move to their new places.
evolutor.ExecuteScript();

// 5. Version 2 opens on the evolved database: the years are there, under their new name, and the new class works.
v2.SystemHandle.Active = true;
TBoldSystem system = v2.System;
Console.WriteLine($"Version 2: {List(system, "Book.allInstances->collect(title + ' (' + published.asString + ')')")}.");
TBoldObject solaris = system.EvaluateExpressionAsDirectElement("Book.allInstances->select(title = 'Solaris')->first") as TBoldObject
    ?? throw new InvalidOperationException("Solaris is not there");
solaris.BoldMemberByExpressionName("isbn").AsString = "978-0-306-40615-7";
TBoldObject reservation = system.CreateNewObjectByExpressionName("Reservation");
((TBADate)reservation.BoldMemberByExpressionName("made")).AsDate = DateTime.Today;
((TBoldObjectReference)reservation.BoldMemberByExpressionName("book")).BoldObject = solaris;
((TBoldObjectReference)reservation.BoldMemberByExpressionName("member")).BoldObject =
    system.EvaluateExpressionAsDirectElement("Member.allInstances->first") as TBoldObject;
system.UpdateDatabase();
v2.Reopen();
Console.WriteLine($"Saved and read again: {List(v2.System, "Book.allInstances->select(isbn <> '')->collect(title + ', ISBN ' + isbn)")}; " +
    $"{List(v2.System, "Reservation.allInstances")}.");
