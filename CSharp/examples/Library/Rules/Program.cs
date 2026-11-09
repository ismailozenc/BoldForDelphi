// Tutorial 3, business rules: derived members that stay current and a subscription that hears of their changes, the
// model's constraints checked before saving, transactions, and undo and redo.
//
//   dotnet run --project examples/Library/Rules [database file]
using Bold;
using LibraryClasses;
using LibraryExamples;

using var library = new SqliteSystem(args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "library-rules.db"));
TBoldSystem system = library.System;
var leGuin = new TAuthor(system) { Name = "Ursula K. Le Guin" };
TBook NewBook(string title, int year, int copies = 1) => new(system) { Title = title, Year = year, Copies = copies, WrittenBy = leGuin };
TBook earthsea = NewBook("A Wizard of Earthsea", 1968, copies: 3);
TBook[] more = [NewBook("The Tombs of Atuan", 1970), NewBook("The Farthest Shore", 1972), NewBook("Tehanu", 1990)];
var ann = new TMember(system) { Name = "Ann" };
var bob = new TMember(system) { Name = "Bob" };
system.UpdateDatabase();

// 1. Derived members stay current: Available (copies - loans->size) follows the loans. A subscriber hears when it changes.
using var subscriber = new TBoldPassthroughSubscriber((_, _, _) => Console.WriteLine($"  (told: the available copies of {earthsea.Title} changed)"));
earthsea.M_Available.DefaultSubscribe(subscriber);
Console.WriteLine($"{earthsea.Title}: {earthsea.Available} available.");
ann.Borrow(earthsea);
Console.WriteLine($"Ann borrows it: {earthsea.Available} available.");
bob.Borrow(earthsea);
Console.WriteLine($"Bob borrows it: {earthsea.Available} available.");

// 2. Constraints are OCL in the model that should hold; Bold evaluates them, the application decides what to do. This
// one saves only when no object the save changes, or links to, breaks one.
List<string> Broken(TBoldObject boldObject)
{
    // "constraints" holds each constraint of the object, as a Boolean that tells whether it holds.
    using var constraints = (TBoldList)boldObject.EvaluateExpressionAsNewElement("constraints->select(c | not c)")!;
    return [.. constraints.Cast<TBAConstraint>().Select(constraint => constraint.Constraint!.ModelName)];
}

bool Save()
{
    var touched = new HashSet<TBoldObject>();
    foreach (TBoldObject changed in system.DirtyObjects.Cast<TBoldObject>().Where(o => !o.BoldObjectIsDeleted))
    {
        touched.Add(changed);
        for (int i = 0; i < changed.BoldMemberCount; i++)
        {
            if (changed.BoldMembers(i) is TBoldObjectReference { BoldObject: { } linked })
            {
                touched.Add(linked);   // a new loan changes its member's and its book's loans
            }
        }
    }

    List<string> broken = [.. touched.SelectMany(o => Broken(o).Select(rule => $"{o.BoldClassTypeInfo.ModelName} '{o.AsString}': {rule}"))];
    if (broken.Count > 0)
    {
        Console.WriteLine($"  Not saved: {string.Join("; ", broken)}.");
        return false;
    }

    system.UpdateDatabase();
    Console.WriteLine("  Saved.");
    return true;
}

foreach (TBook book in more)
{
    ann.Borrow(book);
}

Console.WriteLine($"Ann borrows three more books ({ann.LoanCount} loans):");
Save();
ann.Loans.Cast<TLoan>().First(loan => loan.Book == earthsea).Delete();
Console.WriteLine($"Ann returns {earthsea.Title} ({ann.LoanCount} loans, {earthsea.Available} copies available):");
Save();
earthsea.Title = "";
Console.WriteLine("A book loses its title:");
Save();
earthsea.Title = "A Wizard of Earthsea";

// 3. Transactions: changes in memory that are kept or undone together.
system.StartTransaction();
try
{
    bob.Borrow(earthsea);
    if (bob.Borrow(more[2]) is null)
    {
        throw new InvalidOperationException($"{more[2].Title} is out");
    }

    system.CommitTransaction();
}
catch (InvalidOperationException e)
{
    system.RollbackTransaction();
    Console.WriteLine($"Bob wants {earthsea.Title} and {more[2].Title} together; {e.Message}, so neither: he has {bob.LoanCount} loan.");
}

// 4. Undo and redo: switched on for the system, with a check point before each step the user may want to undo.
IBoldUndoHandler undo = system.UndoHandlerInterface;
undo.Enabled = true;
undo.SetCheckPoint();
earthsea.Copies = 5;
Console.WriteLine($"Two copies more: {earthsea.Available} available.");
undo.UndoLatest();
Console.WriteLine($"Undone: {earthsea.Available} available.");
undo.RedoLatest();
Console.WriteLine($"Redone: {earthsea.Available} available.");
Save();
