// The package smoke test: Bold used from its packages, as an application outside this repository uses it. It runs the
// user guide's first application on the Library model (untyped, then through the classes bold-codegen generated), on a
// SQLite database that it reopens between the steps, and checks a custom attribute type the attribute generator wrote
// and the Delphi type names the packages bring (TBoldRepresentation). Exits with 0 when all is well.
using System.ComponentModel;
using Bold;
using LibraryClasses;
#if BOLD_OFFLINE
using SqliteConnection = Bold.Tests.Sqlite.SqliteConnection;
#else
using Microsoft.Data.Sqlite;
#endif

string folder = Path.Combine(Path.GetTempPath(), $"bold-smoke-{Guid.NewGuid():N}");
Directory.CreateDirectory(folder);
string databaseFile = Path.Combine(folder, "library.db");
TBoldRepresentation asText = BoldDefs.brDefault; // a Delphi type name from Bold.Core, which comes through other packages
int failures = 0;

void Check(bool condition, string what)
{
    Console.WriteLine($"{(condition ? "ok  " : "FAIL")} {what}");
    failures += condition ? 0 : 1;
}

try
{
    // 1. A new database, and objects reached by name.
    using (var library = new Library(databaseFile, useGeneratedCode: false, createSchema: true))
    {
        TBoldSystem system = library.System;
        Check(library.TypeInfo.StaticSystemTypeInfo!.SystemIsRunnable, "the model makes a runnable system");
        TBoldObject author = system.CreateNewObjectByExpressionName("Author");
        author.BoldMemberByExpressionName("name").AsString = "Ursula K. Le Guin";
        foreach ((string title, int year) in new[] { ("The Dispossessed", 1974), ("A Wizard of Earthsea", 1968) })
        {
            TBoldObject book = system.CreateNewObjectByExpressionName("Book");
            book.BoldMemberByExpressionName("title").AsString = title;
            ((TBAInteger)book.BoldMemberByExpressionName("year")).AsInteger = year;
            ((TBoldObjectReference)book.BoldMemberByExpressionName("writtenBy")).BoldObject = author;
        }

        Check(((TBoldObjectList)author.BoldMemberByExpressionName("books")).Count == 2, "linking a book to its author adds it to the author's books");
        Check(system.EvaluateExpressionAsString("Book.allInstances->select(year < 1970)->size", asText) == "1", "OCL counts the books before 1970");
        system.UpdateDatabase();
        Check(system.DirtyObjects.Count == 0, "the objects are saved");
    }

    // 2. The same database, through the generated classes.
    using (var library = new Library(databaseFile, useGeneratedCode: true, createSchema: false))
    {
        TBoldSystem system = library.System;
        var authors = (TAuthorList)system.ClassByExpressionName("Author");
        Check(authors.Count == 1, "the author is read back");
        TAuthor author = authors[0]!;
        Check(author.Name == "Ursula K. Le Guin" && author.Books.Count == 2, "with her books");
        var book = new TBook(system) { Title = "The Left Hand of Darkness", Year = 1969, WrittenBy = author };
        Check(author.Books.Includes(book), "a typed link shows at the other end");

        system.StartTransaction();
        book.Title = "Changed";
        system.RollbackTransaction();
        Check(book.Title == "The Left Hand of Darkness", "a rolled back transaction leaves the object as it was");

        var early = (TBoldList)system.EvaluateExpressionAsNewElement("Book.allInstances->select(year < 1970)->orderby(year)->collect(title)")!;
        Check(early.Count == 2 && early[0]!.AsString == "A Wizard of Earthsea", "OCL selects, orders and collects");
        system.UpdateDatabase();
    }

    // 3. Once more, to see the third book stored.
    using (var library = new Library(databaseFile, useGeneratedCode: true, createSchema: false))
    {
        Check(library.System.EvaluateExpressionAsString("Book.allInstances->size", asText) == "3", "the new book is stored");
    }

    // 4. The attribute generator (a package of its own) wrote the value set below when this program was built.
    Check(BoldMemberTypeDictionary.BoldMemberTypes().DescriptorByDelphiName("TBAGenre") is not null && (int)TGenreEnum.gPoetry == 1,
        "the attribute generator wrote and registered a value set");
}
catch (Exception e)
{
    Console.WriteLine($"FAIL {e}");
    failures++;
}
finally
{
    try
    {
        Directory.Delete(folder, true);
    }
    catch (IOException)
    {
    }
}

Console.WriteLine(failures == 0 ? "The packages work." : $"{failures} checks failed.");
return failures == 0 ? 0 : 1;

/// <summary>The Library model's system on a SQLite database file, as the user guide sets it up.</summary>
internal sealed class Library : IDisposable
{
    private readonly Container components = new();
    private readonly TADOConnection connection;

    public Library(string databaseFile, bool useGeneratedCode, bool createSchema)
    {
        var model = new TBoldAbstractModel(components) { Name = "Library" };
        var lines = new TStringList();
        lines.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Library.bld"));
        model.ReadModel(lines);

        connection = new TADOConnection(new SqliteConnection($"Data Source={databaseFile};Pooling=False"));
        var adapter = new TBoldDatabaseAdapterADO(components) { Connection = connection };
        adapter.SQLDatabaseConfig.InitializeDbEngineSettings(TBoldDataBaseEngine.dbeSQLite);
        var database = new TBoldPersistenceHandleDB(components) { BoldModel = model, DatabaseAdapter = adapter };
        if (createSchema)
        {
            database.CreateDataBaseSchema();
        }

        TypeInfo = new TBoldSystemTypeInfoHandle(components) { BoldModel = model, UseGeneratedCode = useGeneratedCode };
        SystemHandle = new TBoldSystemHandle(components) { SystemTypeInfoHandle = TypeInfo, PersistenceHandle = database, Active = true };
    }

    public TBoldSystemTypeInfoHandle TypeInfo { get; }

    public TBoldSystemHandle SystemHandle { get; }

    public TBoldSystem System => SystemHandle.System!;

    public void Dispose()
    {
        if (SystemHandle.Active)
        {
            System.Discard(); // what was not saved is dropped
            SystemHandle.Active = false;
        }

        components.Dispose();
        connection.Close();
        connection.Connection.Dispose();
    }
}

/// <summary>A value set written by the attribute generator (Bold.SourceGenerators).</summary>
[BoldValueSet("Genre", ValuePrefix = "g")]
[BoldValueSetValue("Novel")]
[BoldValueSetValue("Poetry")]
public partial class TBAGenre : TBAValueSet
{
}
