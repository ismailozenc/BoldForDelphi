// Tutorial 5, several desks over the network: one server with the library's database behind remote persistence and the
// propagator, and two desks without a database of their own. Each desk sees what the other saves, and a pessimistic
// lock keeps them from changing the same book at once.
//
//   dotnet run --project examples/Library/Network [database file]
//   dotnet run --project examples/Library/Network -- --serve [url]      the server alone, for WinForms desks (Desk --server url)
using Bold;
using Library.Network;
using LibraryClasses;

if (args is ["--serve", ..])
{
    string url = args.Length > 1 ? args[1] : "http://localhost:5000";
    using var library = new LibraryServer(Path.Combine(Path.GetTempPath(), "library-server.db"), url, newDatabase: false);
    Console.WriteLine($"The library's server listens at {library.Url}; start desks with: dotnet run --project examples/Library/Desk -- --server {library.Url}");
    Console.WriteLine("Press Enter to stop it.");
    Console.ReadLine();
    return;
}

string databaseFile = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "library-network.db");

// 1. The server. In production it is its own program (Bold.Remote.Server) or your ASP.NET Core application.
using var server = new LibraryServer(databaseFile);
Console.WriteLine($"The server listens at {server.Url}.");

// 2. A console program has no message loop: the console environment queues what the propagator brings, and runs it
// when the program waits in it (WinForms applications need none of this).
using var environment = TBoldConsoleEnvironmentConfiguration.Install();
bool WaitFor(Func<bool> condition) => environment.WaitUntil(condition, TimeSpan.FromSeconds(10));

// 3. Two desks.
using var front = new LibraryClient("Front desk", server.Url);
using var back = new LibraryClient("Back office", server.Url);
Console.WriteLine($"Two desks are connected: {front.Name} and {back.Name}.");

// 4. The back office adds a book. The front desk has read the list of books before, so the propagator tells it when that
// list changes, and it reads the list again.
Console.WriteLine($"{front.Name} has {front.System.ClassByExpressionName("Book").Count} books.");
_ = new TBook(back.System) { Title = "Solaris", Year = 1961 };
back.System.UpdateDatabase();
WaitFor(() => front.Book("Solaris") is not null);
Console.WriteLine($"{back.Name} saves Solaris; {front.Name} has {front.System.ClassByExpressionName("Book").Count} book: {front.Book("Solaris")?.Title}.");

// 5. The front desk reads how many copies are available, which reads the book's loans. When the back office lends it,
// the front desk is told that those loans changed.
TBook solarisInFront = front.Book("Solaris")!;
Console.WriteLine($"{front.Name}: {solarisInFront.Available} copy available.");
var ann = new TMember(back.System) { Name = "Ann" };
ann.Borrow(back.Book("Solaris"));
back.System.UpdateDatabase();
WaitFor(() => solarisInFront.Available == 0);
Console.WriteLine($"{back.Name} lends Solaris to Ann; {front.Name}: {solarisInFront.Available} copies available.");

// 6. Locks: the front desk changes the book's copies, which takes the lock of its region (Book: title, year, copies).
solarisInFront.Copies = 2;
Console.WriteLine($"{front.Name} changes the copies to 2, and holds the lock of the book's region.");
TBook solarisInBack = back.Book("Solaris")!;
try
{
    solarisInBack.Title = "Solaris (1961)";
    Console.WriteLine($"{back.Name} changes the title too.");
}
catch (EBoldGetLocksFailed locked)
{
    // ClientIds are "id=name" of the desks that hold the locks.
    string holders = string.Join(", ", locked.ClientIds.ToArray().Select(holder => holder[(holder.IndexOf('=') + 1)..]));
    Console.WriteLine($"{back.Name} cannot change the title: {holders} holds the lock.");
}

// 7. Saving releases the lock; once the back office has the front desk's change, it can change the book.
front.System.UpdateDatabase();
WaitFor(() => solarisInBack.Copies == 2);
solarisInBack.Title = "Solaris (1961)";
back.System.UpdateDatabase();
WaitFor(() => solarisInFront.Title == "Solaris (1961)");
Console.WriteLine($"{front.Name} saves; {back.Name} sees {solarisInBack.Copies} copies, then changes the title, which {front.Name} sees: {solarisInFront.Title}.");
