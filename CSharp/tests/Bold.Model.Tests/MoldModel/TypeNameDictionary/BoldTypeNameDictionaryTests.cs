// Delphi: Source/MoldModel/TypeNameDictionary/BoldTypeNameDictionary.pas
// Port map: CSharp/port/units/BoldTypeNameDictionary.md
namespace Bold.Tests;

public class BoldTypeNameDictionaryTests
{
    [Test]
    [Covers(typeof(TBoldTypeNameDictionary), "*")]
    [Covers(typeof(TBoldTypeNameMapping), "*")]
    [Covers(typeof(BoldTypeNameDictionary), nameof(BoldTypeNameDictionary.TBoldCurrentTypeNameDictionaryClass))]
    public void Default_mappings_expand_the_expression_name()
    {
        var d = new TBoldTypeNameDictionary(null);
        d.AddDefaultMappings();
        Assert.That(d.Count, Is.EqualTo(30));
        TBoldTypeNameMapping integer = d.MappingForModelName("integer")!;
        Assert.That(integer.ExpandedDelphiName, Is.EqualTo("TBAInteger"));
        Assert.That(integer.ExpandedMapperName, Is.EqualTo("TBoldPMInteger"));
        Assert.That(integer.ExpandedAccessor, Is.EqualTo("AsInteger"));
        Assert.That(integer.ExpandedNativeType, Is.EqualTo("Integer"));
        Assert.That(integer.ExpandedContentsName, Is.EqualTo("Integer"));
        Assert.That(integer.ExpandedComType, Is.EqualTo("Integer"));
        Assert.That(integer.ValueInterface, Is.EqualTo("IBoldIntegerContent"));
        Assert.That(d.MappingForModelName("DateTime")!.ExpandedNativeType, Is.EqualTo("TDateTime"));
        Assert.That(d.MappingForModelName("TimeStamp")!.ExpandedDelphiName, Is.EqualTo("TBADateTime"));
        Assert.That(d.ExactMappingForModelName("NoSuchType"), Is.Null);
        Assert.That(d.MappingForModelName("NoSuchType")!.ModelName, Is.EqualTo("<Default>"), "unknown types use the default mapping");
        Assert.That(d.Mapping(0).ModelName, Is.EqualTo("<Default>"));
        Assert.That(BoldTypeNameDictionary.TBoldCurrentTypeNameDictionaryClass, Is.EqualTo(typeof(TBoldTypeNameDictionary)));
    }

    [Test]
    [Covers(typeof(TBoldTypeNameDictionary), nameof(TBoldTypeNameDictionary.SaveToFile), nameof(TBoldTypeNameDictionary.LoadFromFile), nameof(TBoldTypeNameDictionary.AddMapping))]
    [Covers(typeof(TBoldTypeNameMapping), nameof(TBoldTypeNameMapping.AssignTo))]
    public void Mappings_save_and_load_one_per_line()
    {
        var d = new TBoldTypeNameDictionary(null);
        d.AddDefaultMappings();
        TBoldTypeNameMapping extra = d.AddMapping();
        d.Mapping(4).AssignTo(extra);
        extra.ModelName = "Money";
        extra.ComType = "Currency, with comma";
        string path = Path.GetTempFileName();
        try
        {
            d.SaveToFile(path);
            Assert.That(File.ReadAllLines(path)[1], Does.StartWith("ModelName=String,ExpressionName=String,DelphiName=TBA<Name>,ContentsName=<Name>"));
            var loaded = new TBoldTypeNameDictionary(null);
            loaded.LoadFromFile(path);
            Assert.That(loaded.Count, Is.EqualTo(31));
            TBoldTypeNameMapping money = loaded.MappingForModelName("Money")!;
            Assert.That(money.ExpressionName, Is.EqualTo("Integer"));
            Assert.That(money.ComType, Is.EqualTo("Currency"), "the writer does not quote, so a comma ends the value, as in Delphi");

            File.WriteAllText(path, "ModelName=Old,StreamName=Legacy,ContentsName=Ignored\r\n\r\n");
            var old = new TBoldTypeNameDictionary(null);
            old.LoadFromFile(path);
            Assert.That(old.Count, Is.EqualTo(1), "blank lines are skipped");
            Assert.That(old.Mapping(0).ContentsName, Is.EqualTo("Legacy"), "the old StreamName key wins");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
