// Delphi: Source/MoldModel/Bld/BoldBld.pas
// Port map: CSharp/port/units/BoldBld.md
namespace Bold.Tests;

/// <summary>The .bld reader and writer against the 76 fixture models that Delphi Bold read and wrote.</summary>
public class BoldBldTests
{
    public static IEnumerable<FixtureModelInfo> Fixtures => FixturePaths.Manifest;

    private static string[] Lines(TStrings s) => s.ToStringArray();

    [TestCaseSource(nameof(Fixtures))]
    [Covers(typeof(TMoldBLDRW), nameof(TMoldBLDRW.ModelFromFile), nameof(TMoldBLDRW.StringsToModel))]
    [Covers("BoldBld", "TReader", "TReader.Create", "TReader.GetToken", "TReader.GetCharacter", "TReader.Skip", "TReader.NextCharacter",
        "TReader.GetCharacterNotEof", "TReader.Eat", "TReader.EatKeyword", "TReader.EatStartBlock", "TReader.TryKeyword",
        "TReader.GetQuotedString", "TReader.GetKeyword", "TReader.Getinteger", "TReader.GetBoolean", "TReader.GetClassReference",
        "TReader.FormatVersion", "TReader.LineNumber", "TReader.Line", "TReader.EOS", "TToken", "BLDTokenKind",
        "TElement", "TElement.Read", "TElement.CreateAndRead", "TElementList", "TElementList.Read", "TModel", "TModel.Read",
        "TPClass", "TPClass.Read", "TAttribute", "TAttribute.Read", "TMethod", "TMethod.Read", "TRole", "TRole.Read",
        "TQualifier", "TQualifier.Read", "TAssociation", "TAssociation.Read", "TypeTableByName", "TElementClassRecord")]
    public void Fixture_loads_with_the_manifest_counts(FixtureModelInfo info)
    {
        using TMoldModel model = TMoldBLDRW.ModelFromFile(FixturePaths.Model(info.File));
        Assert.That(model.Name, Is.EqualTo(info.Model));
        Assert.That(model.Classes.Count, Is.EqualTo(info.Classes), "classes");
        Assert.That(model.Associations.Count, Is.EqualTo(info.Associations), "associations");
        for (int i = 0; i < model.Associations.Count; i++)
        {
            Assert.That(model.Associations[i].Roles.Count, Is.EqualTo(2), model.Associations[i].Name);
        }
    }

    /// <summary>
    /// Files that the BoldBld writer did not write last: older tools stored default tagged values (which the reader
    /// drops, as Delphi's does), or the model component saved classes in sorted order. They still round-trip by meaning.
    /// </summary>
    private static readonly Dictionary<string, string> NotWrittenByBoldBld = new()
    {
        ["AccountClasses.server.Account.bld"] = "stores default tagged values",
        ["BoldUMLModel.bld"] = "classes saved in sorted order by the model component",
        ["BudgetClasses.XML.Budget.bld"] = "stores default tagged values",
        ["BuildingClasses.Delphi-Compound-Building.bld"] = "stores default tagged values",
        ["DataValidationExampleClasses.bld"] = "written without the model's DelphiName tagged value",
        ["ModelEvDemo.CBuilder-Compound-ModelEvolution-Version1.bld"] = "stores default tagged values",
        ["ModelEvDemo.CBuilder-Compound-ModelEvolution-Version2.bld"] = "stores default tagged values",
        ["ProdStructClasses.Delphi-Compound-ProdStruct.ProdStruct.bld"] = "stores default tagged values",
    };

    private static string[] Write(TMoldModel model)
    {
        var written = new TStringList();
        TMoldBLDRW.ModelToStrings(model, written);
        return Lines(written);
    }

    [TestCaseSource(nameof(Fixtures))]
    [Covers(typeof(TMoldBLDRW), nameof(TMoldBLDRW.ModelToStrings))]
    [Covers("BoldBld", "TWriter", "TWriter.Create", "TWriter.DoIndent", "TWriter.PutStartBlock", "TWriter.PutEndBlock", "TWriter.Put",
        "TWriter.PutQuotedString", "TWriter.Putinteger", "TWriter.PutBoolean", "TWriter.PutClassReference",
        "TElement.Write", "TElementList.Write", "TModel.Write", "TPClass.Write", "TAttribute.Write", "TMethod.Write", "TRole.Write",
        "TQualifier.Write", "TAssociation.Write", "TypeTableByMoldElement")]
    public void Fixture_writes_back_as_Delphi_wrote_it(FixtureModelInfo info)
    {
        var original = new TStringList();
        original.LoadFromFile(FixturePaths.Model(info.File));
        using TMoldModel model = TMoldBLDRW.StringsToModel(original);
        string[] written = Write(model);
        if (NotWrittenByBoldBld.ContainsKey(info.File))
        {
            Assert.That(written, Is.Not.EqualTo(Lines(original)), "listed as differing; remove it from the list if it now matches");
        }
        else
        {
            Assert.That(written, Is.EqualTo(Lines(original)));
        }
    }

    [TestCaseSource(nameof(Fixtures))]
    [Covers(typeof(TMoldBLDRW), nameof(TMoldBLDRW.ModelToStrings), nameof(TMoldBLDRW.StringsToModel))]
    [Covers(typeof(TMoldModel), nameof(TMoldModel.EnsureTopSorted))]
    public void Fixture_round_trips_by_meaning(FixtureModelInfo info)
    {
        using TMoldModel original = TMoldBLDRW.ModelFromFile(FixturePaths.Model(info.File));
        int classCount = original.Classes.Count;
        original.EnsureTopSorted();
        Assert.That(original.Classes.Count, Is.EqualTo(classCount), "every class is reachable from the root class");
        string[] first = Write(original);
        using TMoldModel reread = TMoldBLDRW.StringsToModel(new TStringList(first));
        Assert.That(ModelDump.Of(reread), Is.EqualTo(ModelDump.Of(original)));

        // A class named as a superclass before its own block is created early, so the class order of a re-read model
        // can differ; sorted, the text is the same.
        reread.EnsureTopSorted();
        Assert.That(Write(reread), Is.EqualTo(first), "writing what was read gives the same text");
    }

    [Test]
    [Covers(typeof(TMoldBLDRW), nameof(TMoldBLDRW.ModelToFile), nameof(TMoldBLDRW.ModelFromFile))]
    public void File_round_trip_keeps_national_characters()
    {
        string path = Path.Combine(Path.GetTempPath(), "bold-" + Guid.NewGuid().ToString("N") + ".bld");
        try
        {
            using (var model = new TMoldModel(null, "Möbel"))
            {
                var c = new TMoldClass(model, "Stuhl");
                c.SuperClass = model.RootClass;
                var a = new TMoldAttribute(c, "Größe") { InitialValue = "line1\r\nline2 \"quoted\" back\\slash" };
                TMoldBLDRW.ModelToFile(model, path);
            }

            Assert.That(File.ReadAllBytes(path), Does.Contain((byte)0xF6), "ö is one Windows-1252 byte");
            using TMoldModel loaded = TMoldBLDRW.ModelFromFile(path);
            Assert.That(loaded.Name, Is.EqualTo("Möbel"));
            TMoldAttribute attr = loaded.Classes.ItemsByName("Stuhl")!.Attributes.ItemsByName("Größe")!;
            Assert.That(attr.InitialValue, Is.EqualTo("line1\r\nline2 \"quoted\" back\\slash"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    [Covers(typeof(TMoldBLDRW), nameof(TMoldBLDRW.StringsToModel))]
    public void No_lines_give_an_empty_model()
    {
        using TMoldModel model = TMoldBLDRW.StringsToModel(new TStringList());
        Assert.That(model.Name, Is.EqualTo("New_Model"));
        Assert.That(model.Classes.Count, Is.EqualTo(1), "the root class");
    }

    [TestCase("VERSION 19\r\n(Model\r\n\t\"M\"\r\n\t\"MRoot\"\r\n\t\"\"\r\n\t\"\"\r\n\t\"\"\r\n\t(Klasses", 8, "'Classes' expected")]
    [TestCase("VERSION 19\r\n(Model\r\n\t\"M\"\r\n\t#", 4, "Bad character 35")]
    [TestCase("VERSION 19\r\n(Model\r\n\t\"M", 3, "Unexpected EOF")]
    [TestCase("VERSION 19\r\n(Thing \"x\")", 2, null)]
    [Covers(typeof(EBoldBLDParseError))]
    [Covers(typeof(BoldMoldConsts), nameof(BoldMoldConsts.sErrorOnPos), nameof(BoldMoldConsts.sAKeywordExpected), nameof(BoldMoldConsts.sBadCharacter), nameof(BoldMoldConsts.sUnexpectedEOF))]
    public void Bad_input_reports_the_line(string text, int line, string? message)
    {
        var s = new TStringList { Text = text };
        if (message is null)
        {
            Assert.Throws<EBoldInternal>(() => TMoldBLDRW.StringsToModel(s), "an unknown block kind is an internal error, as in Delphi");
            return;
        }

        var e = Assert.Throws<EBoldBLDParseError>(() => TMoldBLDRW.StringsToModel(s))!;
        Assert.That(e.Message, Does.Contain(message));
        Assert.That(e.Message, Does.Contain($"Line: {line} "));
    }

    [Test]
    [Covers(typeof(BoldBld), nameof(BoldBld.BooleanToMultiplicityString))]
    public void Old_boolean_multiplicity()
    {
        Assert.That(BoldBld.BooleanToMultiplicityString(true), Is.EqualTo("*"));
        Assert.That(BoldBld.BooleanToMultiplicityString(false), Is.EqualTo("1"));
    }
}
