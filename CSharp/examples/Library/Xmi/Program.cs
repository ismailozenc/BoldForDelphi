// Tutorial 7, models from UML tools: the Library model written as XMI 2.1 (UML 2) for a UML tool, and a model drawn
// in a UML tool (Hotel.xmi, in the XMI of Enterprise Architect) read, kept as .bld, and run.
//
//   dotnet run --project examples/Library/Xmi [output folder]
using System.Xml;
using Bold;
using LibraryExamples;

string folder = args.Length > 0 ? args[0] : Path.Combine(Path.GetTempPath(), "library-xmi");
Directory.CreateDirectory(folder);

string List(TBoldElement on, string expression)
{
    using var list = (TBoldList)on.EvaluateExpressionAsNewElement(expression)!;
    return string.Join(", ", list.Cast<TBoldElement>().Select(element => element.AsString));
}

// 1. Export: the Library model as XMI 2.1, without the parts Bold adds to a model (its root class, its tagged values' defaults).
TMoldModel library = TMoldBLDRW.ModelFromFile(Path.Combine(AppContext.BaseDirectory, "Library.bld"));
var export = new TBoldUMLXMILink { FileName = Path.Combine(folder, "Library.xmi"), XMIVersion = TBoldXMIVersion.xmi21 };
export.ExportModel(library);
var xmi = new XmlDocument();
xmi.Load(export.FileName);
var uml = new XmlNamespaceManager(xmi.NameTable);
uml.AddNamespace("xmi", TBoldXMI2Exporter.XMINamespace);
int Count(string type) => xmi.SelectNodes($"//packagedElement[@xmi:type='uml:{type}']", uml)!.Count;
Console.WriteLine($"Library.xmi: {Count("Class")} UML classes and {Count("Association")} associations, for a UML tool.");

// 2. Import: a model drawn in a UML tool. Bold reads XMI 1.x and 2.x; it flattens the packages and "boldifies" the
// model: a root class above the others, and Bold's tagged values.
var import = new TBoldUMLXMILink { FileName = Path.Combine(AppContext.BaseDirectory, "Hotel.xmi") };
TMoldModel hotel = import.ImportMoldModel();
Console.WriteLine($"Hotel.xmi: the model {hotel.Name}, root class {hotel.RootClass?.Name}:");
for (int i = 0; i < hotel.Classes.Count; i++)
{
    TMoldClass moldClass = hotel.Classes[i]!;
    if (moldClass == hotel.RootClass)
    {
        continue;
    }

    var members = new List<string>();
    for (int a = 0; a < moldClass.Attributes.Count; a++)
    {
        members.Add($"{moldClass.Attributes[a]!.Name}: {moldClass.Attributes[a]!.BoldType}");
    }

    for (int m = 0; m < moldClass.Methods.Count; m++)
    {
        members.Add($"{moldClass.Methods[m]!.Name}()");
    }

    string superClass = moldClass.SuperClass == hotel.RootClass ? "" : $" (a {moldClass.SuperClass!.Name})";
    Console.WriteLine($"  {moldClass.Name}{superClass}: {string.Join(", ", members)}");
}

TMoldClass guestClass = hotel.Classes.ItemsByName("Guest")!;
Console.WriteLine($"  (set in the UML tool: Guest's stereotype {guestClass.Stereotype} and its tagged value Bold.TableName={guestClass.TVByName("Bold.TableName")})");

// An association has two roles; each is a member of its class, which reaches the other end's objects.
string Role(TMoldRole role) => $"{role.MoldClass!.Name}.{role.Name} [{role.Multiplicity}]";
for (int i = 0; i < hotel.Associations.Count; i++)
{
    TMoldAssociation association = hotel.Associations[i]!;
    Console.WriteLine($"  {association.Name}: {Role(association.Roles[0]!)} and {Role(association.Roles[1]!)}");
}

// 3. Kept as .bld, the model editor opens it and bold-codegen generates its classes.
string hotelBld = Path.Combine(folder, "Hotel.bld");
TMoldBLDRW.ModelToFile(hotel, hotelBld);
Console.WriteLine($"Written as {Path.GetFileName(hotelBld)}.");

// 4. And it runs: a system on the imported model, by name, on a new SQLite database.
var lines = new TStringList();
lines.LoadFromFile(hotelBld);
using var hotelSystem = new SqliteSystem(Path.Combine(folder, "hotel.db"), modelLines: lines, useGeneratedCode: false, name: "Hotel");
TBoldSystem system = hotelSystem.System;
TBoldObject suite = system.CreateNewObjectByExpressionName("Suite");
((TBAInteger)suite.BoldMemberByExpressionName("number")).AsInteger = 101;
((TBoldObjectList)suite.BoldMemberByExpressionName("beds")).Add(system.CreateNewObjectByExpressionName("Bed"));
TBoldObject guest = system.CreateNewObjectByExpressionName("Guest");
guest.BoldMemberByExpressionName("name").AsString = "Ann";
((TBoldObjectReference)guest.BoldMemberByExpressionName("room")).BoldObject = suite;
system.UpdateDatabase();
hotelSystem.Reopen();
foreach (TBoldObject room in hotelSystem.System.ClassByExpressionName("Room"))
{
    Console.WriteLine($"Saved and read again: {room.BoldClassTypeInfo.ModelName} {room.EvaluateExpressionAsString("number", BoldDefs.brDefault)}, " +
        $"{room.EvaluateExpressionAsString("beds->size", BoldDefs.brDefault)} bed, guests {List(room, "guests.name")}.");
}
