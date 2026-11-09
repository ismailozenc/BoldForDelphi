// Tutorial 9, lending objects between databases: two branch libraries, each with a database of its own and no server
// between them. The central branch owns the catalogue and lends a book's record to the harbour branch with OLLE (object
// lending); the harbour branch changes it and checks it back in.
//
//   dotnet run --project examples/Library/Branches [folder]
using Bold;
using LibraryClasses;
using LibraryExamples;

string folder = args.Length > 0 ? args[0] : Path.GetTempPath();
using var central = new Branch("Central", Path.Combine(folder, "library-central.db"));
using var harbour = new Branch("Harbour", Path.Combine(folder, "library-harbour.db"));

// 1. The central branch owns the catalogue.
var author = new TAuthor(central.System) { Name = "Stanislaw Lem" };
_ = new TBook(central.System) { Title = "Solaris", Year = 1961, WrittenBy = author };
_ = new TBook(central.System) { Title = "The Cyberiad", Year = 1965, WrittenBy = author };
central.System.UpdateDatabase();
Console.WriteLine($"Central: {central.Books()}.");
TBook solaris = central.Book("Solaris");
TAuthor lem = solaris.WrittenBy!;

// 2. Central lends Solaris and its author to Harbour, which holds Solaris: Harbour may change the book; the author is a
// copy it can only read. The objects travel as a value space: in memory here, by file or over the network otherwise.
using (var lent = new TBoldFreeStandingValueSpace())
{
    TBoldObjectIdList ids = Branch.Ids(solaris, lem);
    TBoldObjectIdList held = Branch.Ids(solaris);
    central.Olle.OwnObjectHandler.Get(ids, held, harbour.Name, lent);       // the values, with global ids; Solaris is checked out
    harbour.Olle.ForeignObjectHandler.Put(lent, ids, held, central.Name);   // stored in Harbour's database
}

Console.WriteLine($"Harbour: {harbour.Books()}.");
Console.WriteLine($"  Central changes Solaris: {Try(() => central.Book("Solaris").Copies = 2)}");

// 3. Harbour bought two more copies, and saves that in its own database.
TBook solarisInHarbour = harbour.Book("Solaris");
solarisInHarbour.Copies = 3;
Console.WriteLine($"  Harbour changes its author: {Try(() => solarisInHarbour.WrittenBy!.Name = "S. Lem")}");
harbour.System.UpdateDatabase();
Console.WriteLine($"Harbour saved: {harbour.Books()}.");

// 4. Harbour checks Solaris in: it releases the book and sends its changes, Central takes them, and Harbour is told the
// version Central has now.
using (var changes = new TBoldFreeStandingValueSpace())
{
    TBook book = harbour.Book("Solaris");
    TBoldObjectIdList ids = Branch.Ids(book);
    TBoldObjectIdList release = Branch.Ids(book);
    harbour.Olle.ForeignObjectHandler.StartCheckIn(ids, release, changes, central.Name);
    int version = 0;
    central.Olle.OwnObjectHandler.CheckIn(changes, ids, release, harbour.Name, ref version);
    harbour.Olle.ForeignObjectHandler.AcknowledgeCheckIn(central.Name, version);
}

Console.WriteLine($"Checked in, Central: {central.Books()}.");
Console.WriteLine($"  Central changes Solaris: {Try(() => central.Book("Solaris").Copies = 4)}");
Console.WriteLine($"  Harbour changes Solaris: {Try(() => harbour.Book("Solaris").Copies = 5)}");

static string Try(Action change)
{
    try
    {
        change();
        return "done.";
    }
    catch (EBold e)
    {
        return $"refused ({e.Message}).";
    }
}

/// <summary>A branch library: its own database, with the Library's tables and OLLE's, and a system on it.</summary>
internal sealed class Branch : IDisposable
{
    /// <summary>The branch's id, by which the other branches know it.</summary>
    public readonly string Name;
    public readonly SqliteSystem Library;
    public readonly TBoldOLLEHandle Olle;

    public Branch(string name, string databaseFile)
    {
        Name = name;
        Library = new SqliteSystem(databaseFile, newDatabase: true, name: name, open: false);
        Olle = new TBoldOLLEHandle(Library.Components) { ApplicationPersistenceHandle = Library.Database };
        Olle.OLLEController.GenerateDatabase();   // OLLE's own tables, once for a database
        Library.SystemHandle.Active = true;
    }

    public TBoldSystem System => Library.System;

    /// <summary>The branch's book of this title, read again from its database, where OLLE writes (the objects read before are gone).</summary>
    public TBook Book(string title)
    {
        Library.Reopen();
        return System.ClassByExpressionName("Book").Cast<TBook>().Single(book => book.Title == title);
    }

    /// <summary>The branch's books, read again from its database (the objects read before are gone).</summary>
    public string Books()
    {
        Library.Reopen();
        return string.Join(", ", System.ClassByExpressionName("Book").Cast<TBook>().Select(book => $"{book.Title} ({book.Copies} {(book.Copies == 1 ? "copy" : "copies")})"));
    }

    /// <summary>The ids of the objects in their database.</summary>
    public static TBoldObjectIdList Ids(params TBoldObject[] objects)
    {
        var ids = new TBoldObjectIdList();
        foreach (TBoldObject boldObject in objects)
        {
            ids.Add(boldObject.BoldObjectLocator!.BoldObjectID);
        }

        return ids;
    }

    public void Dispose() => Library.Dispose();
}
