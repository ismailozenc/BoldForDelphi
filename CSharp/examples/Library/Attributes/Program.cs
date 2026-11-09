// Tutorial 8, custom attribute types: a catalogue of copies whose members are of the library's own attribute types
// (CatalogueTypes.cs), written by the Bold attribute generator: a value set, a two-field attribute stored in two
// columns, and a string that checks itself.
//
//   dotnet run --project examples/Library/Attributes [database file]
using System.Data.Common;
using Bold;
using Library.Attributes;
using LibraryExamples;

string databaseFile = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "library-attributes.db");
var lines = new TStringList();
lines.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Catalogue.bld"));

// 1. The model names the types (Isbn, Condition, ShelfMark); type names tell Bold the classes behind those names. They
// are added before the model is read.
using var catalogue = new SqliteSystem(databaseFile, modelLines: lines, useGeneratedCode: false, name: "Catalogue", addTypeNames: names =>
{
    TBAIsbn.AddTypeNameMapping(names);
    TBACondition.AddTypeNameMapping(names);
    TBAShelfMark.AddTypeNameMapping(names);
});
TBoldSystem system = catalogue.System;

// 2. Each member is of its attribute class, with the accessors the generator wrote (AsCondition, AsShelf, AsPosition).
TBoldObject NewCopy(string title, string isbn, string shelfMark)
{
    TBoldObject copy = system.CreateNewObjectByExpressionName("Copy");
    copy.BoldMemberByExpressionName("title").AsString = title;
    copy.BoldMemberByExpressionName("isbn").AsString = isbn;
    copy.BoldMemberByExpressionName("shelf").AsString = shelfMark;   // SetStringRepresentation reads "shelf-position"
    return copy;
}

TBoldObject solaris = NewCopy("Solaris", "978-0-306-40615-7", "L-3");
TBoldObject earthsea = NewCopy("A Wizard of Earthsea", "978-1-234-56789-7", "L-7");
var condition = (TBACondition)solaris.BoldMemberByExpressionName("condition");
var shelf = (TBAShelfMark)solaris.BoldMemberByExpressionName("shelf");
Console.WriteLine($"Solaris: condition {condition.AsString} (the default, the first value), shelf {shelf.AsShelf}, position {shelf.AsPosition}.");

// 3. A value set's values are an enumeration in C#, names in strings, and #names in OCL.
condition.AsCondition = TConditionEnum.cWorn;
((TBACondition)earthsea.BoldMemberByExpressionName("condition")).AsString = "Good";
var values = new TStringList();
condition.Values.ToStrings(BoldDefs.brDefault, values);
Console.WriteLine($"The conditions: {values.CommaText}; in C#, {string.Join(", ", Enum.GetNames<TConditionEnum>())}.");
using (var worn = (TBoldList)system.EvaluateExpressionAsNewElement("Copy.allInstances->select(condition = #Worn)")!)
{
    Console.WriteLine($"Worn: {string.Join(", ", worn.Cast<TBoldObject>().Select(copy => copy.AsString))}.");
}

// 4. An attribute type's rules hold wherever the value comes from: an ISBN with a wrong check digit, a shelf mark without a position.
Console.WriteLine($"An ISBN with a wrong check digit: {Try(() => solaris.BoldMemberByExpressionName("isbn").AsString = "978-0-306-40615-8")}");
Console.WriteLine($"A shelf mark without a position: {Try(() => shelf.AsString = "L")}");
shelf.AsString = "M-1";

// 5. Saved, the shelf mark takes two columns, which the generated mapper reads and writes.
system.UpdateDatabase();
using (DbCommand command = catalogue.Connection.CreateCommand())
{
    command.CommandText = "SELECT Title, Shelf, Shelf_1 FROM Copy ORDER BY Title";
    using DbDataReader reader = command.ExecuteReader();
    while (reader.Read())
    {
        Console.WriteLine($"  In the table Copy: {reader.GetString(0)} | Shelf {reader.GetValue(1)} | Shelf_1 {reader.GetValue(2)}");
    }
}

catalogue.Reopen();
foreach (TBoldObject copy in catalogue.System.ClassByExpressionName("Copy"))
{
    Console.WriteLine($"Read again: {copy.AsString}, ISBN {copy.BoldMemberByExpressionName("isbn").AsString}, " +
        $"{copy.BoldMemberByExpressionName("condition").AsString}, shelf {copy.BoldMemberByExpressionName("shelf").AsString}.");
}

static string Try(Action change)
{
    try
    {
        change();
        return "taken.";
    }
    catch (EBold e)
    {
        return $"refused ({e.Message}).";
    }
}
