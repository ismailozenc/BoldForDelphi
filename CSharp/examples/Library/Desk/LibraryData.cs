// The desk's data: the system on the Library model, on a SQLite database of its own or on the library's server.
using Bold;
using Library.Network;
using LibraryClasses;
using LibraryExamples;

namespace Library.Desk;

/// <summary>The desk's system: on a database of the desk's own, or on the library's server with the other desks.</summary>
public sealed class LibraryData : IDisposable
{
    private readonly SqliteSystem? fDatabase;
    private readonly LibraryClient? fClient;

    private LibraryData(SqliteSystem? database, LibraryClient? client, string where)
    {
        fDatabase = database;
        fClient = client;
        Where = where;

        // A new library gets a few books and members to start with.
        if (System.ClassByExpressionName("Author").Count == 0)
        {
            AddSampleLibrary();
        }

        // Undo: the desk sets a check point before each lending and returning, and the Undo button goes back to it.
        System.UndoHandlerInterface.Enabled = true;
    }

    /// <summary>The desk on a SQLite database file of its own, made when the file is not there.</summary>
    public static LibraryData OnDatabase(string databaseFile) =>
        new(new SqliteSystem(databaseFile, newDatabase: !File.Exists(databaseFile)), null, Path.GetFileName(databaseFile));

    /// <summary>The desk on the library's server (Bold.Remote.Server, or the Network example's --serve).</summary>
    public static LibraryData OnServer(string url) =>
        new(null, new LibraryClient($"Desk {Environment.ProcessId}", url), url);

    /// <summary>The system handle the form's handles hang from.</summary>
    public TBoldSystemHandle SystemHandle => fDatabase?.SystemHandle ?? fClient!.SystemHandle;

    public TBoldSystem System => SystemHandle.System!;

    /// <summary>The database file or the server, for the window's title.</summary>
    public string Where { get; }

    private void AddSampleLibrary()
    {
        var leGuin = new TAuthor(System) { Name = "Ursula K. Le Guin" };
        _ = new TBook(System) { Title = "A Wizard of Earthsea", Year = 1968, Copies = 2, WrittenBy = leGuin };
        _ = new TBook(System) { Title = "The Dispossessed", Year = 1974, WrittenBy = leGuin };
        var lem = new TAuthor(System) { Name = "Stanislaw Lem" };
        var solaris = new TBook(System) { Title = "Solaris", Year = 1961, WrittenBy = lem };
        _ = new TBook(System) { Title = "The Cyberiad", Year = 1965, WrittenBy = lem };
        var ann = new TMember(System) { Name = "Ann" };
        _ = new TMember(System) { Name = "Bob" };
        ann.Borrow(solaris);
        System.UpdateDatabase();
    }

    public void Dispose()
    {
        fDatabase?.Dispose();
        fClient?.Dispose();
    }
}
