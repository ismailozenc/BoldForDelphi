// New in the C# port: the C# templates and generator (Bold.CodeGen/MoldModel/CodeGenerator/BoldGeneratorTemplatesCSharp.cs).
// The M6 closing tests: generated code compiles for every fixture, typed access works on the Building model, and link
// classes land in the right file (DECIDE-5).
using System.Reflection;
using System.Text.RegularExpressions;
using BuildingClasses;

namespace Bold.Tests;

public class BoldGeneratorTemplatesCSharpTests
{
    private string fFolder = "";

    [SetUp]
    public void SetUp() => fFolder = Path.Combine(Path.GetTempPath(), $"bold-cs-{Guid.NewGuid():N}") + Path.DirectorySeparatorChar;

    [TearDown]
    public void TearDown()
    {
        BoldFileHandler.BoldCloseAllFilehandlers();
        if (Directory.Exists(fFolder))
        {
            Directory.Delete(fFolder, true);
        }
    }

    public static IEnumerable<FixtureModelInfo> Fixtures => FixturePaths.Manifest;

    [TestCaseSource(nameof(Fixtures))]
    public void Generated_code_compiles_for_every_fixture(FixtureModelInfo info)
    {
        (TMoldModel model, TBoldTypeNameDictionary types) = GeneratedCode.Load(info.File);
        if (model.TVByName(BoldDefaultTaggedValues.BOLDINTERALTVPREFIX + BoldDefaultTaggedValues.TV_MODELERRORS) != "")
        {
            Assert.Ignore("The model has errors, so it has no runtime model (see the runtime model fixture test).");
        }

        GeneratedCode.Generate(model, types, fFolder);
        string[] files = Directory.GetFiles(fFolder, "*.cs");
        Assert.That(files.Count(f => f.EndsWith(".g.cs", StringComparison.Ordinal)), Is.EqualTo(model.Classes.Count + model.Components.Count), "a file for each class and each component");
        (string[] errors, _) = GeneratedCode.Compile("Fixture", files);
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Typed_access_works_on_the_Building_model()
    {
        using TMoldModel model = BuildingModel.Load();
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        model.TypeNameDictionary = types;
        using var systemTypeInfo = new TBoldSystemTypeInfo(model, true, true, types);
        Assert.That(systemTypeInfo.InitializationLog.ToStringArray(), Is.Empty, "the generated classes are registered, with the model's CRC");
        Assert.That(systemTypeInfo.ClassTypeInfoByExpressionName("Person")!.ObjectClass, Is.EqualTo(typeof(TPerson)));

        using var system = new TBoldSystem(null, systemTypeInfo, null);
        try
        {
            var ann = (TPerson)system.CreateNewObjectByExpressionName("Person");
            var bob = new TPerson(system);
            Assert.That(ann.Assets, Is.InRange(5000m, 15000m), "CompleteCreate, written in Person.cs, gave her money");
            Assert.That(ann.FirstName, Is.Not.Empty);
            ann.FirstName = "Ann";
            ann.Assets = 1000;
            bob.Assets = 500;
            Assert.That(ann.M_FirstName.AsString, Is.EqualTo("Ann"), "the property reads and writes the member");

            var flat = new TResidential_Building(system) { TotalRent = 300 };
            Assert.That(flat.Address, Does.Contain(" "), "the street address of CompleteCreate");
            ann.Home = flat;
            bob.Home = flat;
            Assert.That(flat.Residents.Count, Is.EqualTo(2));
            Assert.That(flat.Residents[0], Is.SameAs(ann), "the typed list gives a TPerson");
            Assert.That(flat.Residents.Includes(bob), Is.True);

            var owner = new TPerson(system) { Assets = 0 };
            owner.OwnedBuildings.Add(flat);
            Assert.That(flat.Owners[0], Is.SameAs(owner), "both ends of the link");
            TOwnership ownership = owner.Ownership[0]!;
            Assert.That((ownership.Owners, ownership.OwnedBuildings), Is.EqualTo((owner, (TBuilding)flat)), "the link object of the link class");

            ann.BorrowFrom(bob, 100);
            Assert.That((ann.Assets, bob.Assets), Is.EqualTo((1100m, 400m)), "a method written in Person.cs");

            var evicted = new List<string>();
            TResidential_Building.Evicted += evicted.Add;
            try
            {
                flat.ChargeRent();
                flat.ChargeRent();
                flat.ChargeRent();
            }
            finally
            {
                TResidential_Building.Evicted -= evicted.Add;
            }

            Assert.That(owner.Assets, Is.EqualTo(900m), "the owner received the rent three times");
            Assert.That(bob.Home, Is.Null, "Bob ran out of money and was evicted");
            Assert.That(evicted, Has.Count.EqualTo(1).And.Some.StartsWith(bob.FirstName));

            TPerson added = flat.Residents.AddNew();
            Assert.That(added.Home, Is.SameAs(flat), "AddNew gives a typed object");
            Assert.That(system.EvaluateExpressionAsString("Person.allInstances->select(assets > 1000)->size", BoldDefs.brDefault), Is.Not.EqualTo(""),
                "OCL works on the typed objects");
            Assert.That(owner.EvaluateExpressionAsString("ownedBuildings->first.oclType.typename", BoldDefs.brDefault), Is.EqualTo("Residential_Building"));
        }
        finally
        {
            system.Discard();
        }
    }

    [Test]
    public void Link_classes_land_in_their_own_file_in_the_right_namespace()
    {
        (TMoldModel model, TBoldTypeNameDictionary types) = GeneratedCode.Load("BuildingsAndOwners.Building.bld");
        model.Classes.ItemsByName("Person")!.SetBoldTV(BoldDefaultTaggedValues.TAG_UNITNAME, "People");
        model.Classes.ItemsByName("Building")!.SetBoldTV(BoldDefaultTaggedValues.TAG_UNITNAME, "People");
        GeneratedCode.Generate(model, types, fFolder);

        string ownership = File.ReadAllText(fFolder + "Ownership.g.cs");
        Assert.That(ownership, Does.Contain("namespace People;"), "the implicit link class moved to the component of its ends");
        Assert.That(Regex.Matches(ownership, @"public partial class (\w+)").Select(m => m.Groups[1].Value), Is.EqualTo(new[] { "TOwnership", "TOwnershipList" }),
            "the file holds the link class and its list, and nothing else");
        Assert.That(File.ReadAllText(fFolder + "People.g.cs"), Does.Contain("typeof(TOwnership)"), "its component registers it");
        Assert.That(File.ReadAllText(fFolder + "BuildingClasses.g.cs"), Does.Not.Contain("TOwnership"));
        Assert.That(File.ReadAllText(fFolder + "Residential_Building.g.cs"), Does.Contain("namespace People;"), "a subclass is in its superclass's component");
        (string[] errors, _) = GeneratedCode.Compile("Components", Directory.GetFiles(fFolder, "*.cs"));
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void Generating_again_keeps_user_code_and_adds_new_methods()
    {
        (TMoldModel model, TBoldTypeNameDictionary types) = GeneratedCode.Load("BuildingsAndOwners.Building.bld");
        GeneratedCode.Generate(model, types, fFolder);
        string person = File.ReadAllText(fFolder + "Person.cs");
        File.WriteAllText(fFolder + "Person.cs", person.Replace("public partial void BorrowFrom(TPerson? Lender, int Amount)\r\n    {", "public partial void BorrowFrom(TPerson? Lender, int Amount)\r\n    {\r\n        Assets += Amount; // mine", StringComparison.Ordinal));

        (model, types) = GeneratedCode.Load("BuildingsAndOwners.Building.bld");
        TMoldClass personClass = model.Classes.ItemsByName("Person")!;
        _ = new TMoldMethod(personClass, "Greet") { Signature = "Other: TPerson", ReturnType = "String", Visibility = TVisibilityKind.vkPublic };
        GeneratedCode.Generate(model, types, fFolder);

        string again = File.ReadAllText(fFolder + "Person.cs");
        Assert.That(again, Does.Contain("// mine"), "user code is kept");
        Assert.That(Regex.Matches(again, @"\bBorrowFrom\(").Count, Is.EqualTo(1), "an existing method is not added again");
        Assert.That(again, Does.Contain("public partial string Greet(TPerson? Other)"), "the new method's stub is added");
        Assert.That(again.TrimEnd(), Does.EndWith("}"), "inside the class");
        (string[] errors, _) = GeneratedCode.Compile("Again", Directory.GetFiles(fFolder, "*.cs"));
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void The_Building_example_is_generated_from_its_model()
    {
        string example = RepoPaths.Full("CSharp/examples/Building/BuildingClasses");
        Directory.CreateDirectory(fFolder);
        foreach (string file in Directory.GetFiles(example, "*.cs").Concat([Path.Combine(example, "Building.bld")]))
        {
            File.Copy(file, fFolder + Path.GetFileName(file));
        }

        TMoldModel model = TMoldBLDRW.ModelFromFile(fFolder + "Building.bld");
        var types = new TBoldTypeNameDictionary(null);
        types.AddDefaultMappings();
        model.TypeNameDictionary = types;
        GeneratedCode.Generate(model, types, fFolder);
        foreach (string file in Directory.GetFiles(example, "*.cs"))
        {
            Assert.That(File.ReadAllText(fFolder + Path.GetFileName(file)).Replace("\r", "", StringComparison.Ordinal), Is.EqualTo(File.ReadAllText(file).Replace("\r", "", StringComparison.Ordinal)),
                Path.GetFileName(file) + ": generate it again with bold-codegen (CSharp/tools/Bold.CodeGen.Tool)");
        }
    }

    [Test]
    public void Members_derived_in_code_derive_and_reverse_derive_through_the_generated_class()
    {
        (TMoldModel model, TBoldTypeNameDictionary types) = GeneratedCode.Load("ReverseDeriveExampleClasses.Delphi-Simple-ObjectSpace-ReverseDerivedAttributes.bld");
        GeneratedCode.Generate(model, types, fFolder);
        string person = File.ReadAllText(fFolder + "Person.cs");
        person = person.Replace("        // Calculate the value, subscribe to what it depends on, and set it:\r\n        // M_fullName.AsString = ...;", """
                    M_fullName.AsString = (firstName + " " + family?.familyName).Trim();
                    M_firstName.DefaultSubscribe(Subscriber);
                    M_family.DefaultSubscribe(Subscriber);
                    family?.M_familyName.DefaultSubscribe(Subscriber);
            """.Replace("\n", "\r\n", StringComparison.Ordinal), StringComparison.Ordinal);
        person = person.Replace("protected virtual partial void _fullName_ReverseDerive(object? DerivedObject)\r\n    {", """
            protected virtual partial void _fullName_ReverseDerive(object? DerivedObject)
                {
                    string[] parts = M_fullName.AsString.Split(' ', 2);
                    firstName = parts[0];
                    if (family is not null && parts.Length > 1)
                    {
                        family.familyName = parts[1];
                    }
            """.Replace("\n", "\r\n", StringComparison.Ordinal), StringComparison.Ordinal);
        File.WriteAllText(fFolder + "Person.cs", person);
        (string[] errors, byte[] image) = GeneratedCode.Compile("ReverseDeriveExample", Directory.GetFiles(fFolder, "*.cs"));
        Assert.That(errors, Is.Empty);
        Assembly assembly = GeneratedCode.LoadImage(image);
        BoldGeneratedCodeRegistry.Register(assembly);

        (model, types) = GeneratedCode.Load("ReverseDeriveExampleClasses.Delphi-Simple-ObjectSpace-ReverseDerivedAttributes.bld");
        using var systemTypeInfo = new TBoldSystemTypeInfo(model, true, true, types);
        Assert.That(systemTypeInfo.InitializationLog.ToStringArray(), Is.Empty);
        using var system = new TBoldSystem(null, systemTypeInfo, null);
        try
        {
            TBoldObject ann = system.CreateNewObjectByExpressionName("Person");
            TBoldObject lees = system.CreateNewObjectByExpressionName("Family");
            Assert.That(ann.GetType().Assembly, Is.SameAs(assembly), "the object is of the generated class");
            ann.BoldMemberByExpressionName("firstName").AsString = "Ann";
            lees.BoldMemberByExpressionName("familyName").AsString = "Lee";
            ((TBoldObjectReference)ann.BoldMemberByExpressionName("family")).BoldObject = lees;
            TBoldMember fullName = ann.BoldMemberByExpressionName("fullName");
            Assert.That(fullName.AsString, Is.EqualTo("Ann Lee"), "derived by the method written in Person.cs");
            lees.BoldMemberByExpressionName("familyName").AsString = "Smith";
            Assert.That(fullName.AsString, Is.EqualTo("Ann Smith"), "and derived again when an input changes");
            fullName.AsString = "Bea Jones";
            Assert.That(ann.BoldMemberByExpressionName("firstName").AsString, Is.EqualTo("Bea"), "reverse derived by Person.cs");
            Assert.That(lees.BoldMemberByExpressionName("familyName").AsString, Is.EqualTo("Jones"));
        }
        finally
        {
            system.Discard();
        }
    }
}
