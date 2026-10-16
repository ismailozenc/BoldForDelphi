// Delphi: Source/ObjectSpace/BORepresentation/BoldMLAttributes.pas
// Port map: CSharp/port/units/BoldMLAttributes.md
namespace Bold.Tests;

public class BoldMLAttributesTests
{
    private static void AddLanguages(TMoldModel model)
    {
        var language = new TMoldClass(model, "LanguageClass") { SuperClass = model.RootClass };
        _ = new TMoldAttribute(language, "LanguageName");
        _ = new TMoldAttribute(language, "LanguageNumber") { BoldType = "Integer" };
        _ = new TMoldAttribute(model.Classes.ItemsByName("Person")!, "title") { BoldType = "MLString" };
    }

    private static void NewLanguage(TBoldSystem system, string name, int number)
    {
        TBoldObject o = system.CreateNewObjectByExpressionName("LanguageClass");
        o.BoldMemberByExpressionName("LanguageName").AsString = name;
        ((TBAInteger)o.BoldMemberByExpressionName("LanguageNumber")).AsInteger = number;
    }

    [Test]
    [Covers(typeof(BoldMLAttributes), "*")]
    [Covers(typeof(TBAMLString), "*")]
    [Covers(typeof(TBAMLString_Proxy), "*")]
    [Covers(typeof(TBAMLSubString), "*")]
    [Covers(typeof(TBALanguage), "*")]
    [Covers(typeof(TBADerivedValueSetValueList), "*")]
    [Covers("BoldMLAttributes", "TBAStringClass")]
    public void Multi_language_strings_keep_a_value_per_language()
    {
        using var s = new ObjectSpaceSample(AddLanguages);
        s.System.MakeDefault();
        NewLanguage(s.System, "en", 0);
        NewLanguage(s.System, "sv", 1);

        TBAValueSetValueList languages = BoldMLAttributes.BoldLanguageList();
        Assert.That(languages.Count, Is.EqualTo(2));
        Assert.That(languages.FindByInteger(1)!.AsString, Is.EqualTo("sv"));
        Assert.That(BoldMLAttributes.BoldPrimaryLanguage(null).AsString, Is.EqualTo("en"), "language number 0 is the start");

        TBoldObject p = s.NewPerson();
        var title = (TBAMLString)p.BoldMemberByExpressionName("title");
        title.AsString = "Doctor";
        title.AsStringByLanguage("sv").AsString = "Doktor";
        Assert.That(title.AsString, Is.EqualTo("Doctor"));
        BoldMLAttributes.BoldSetPrimaryLanguageByName(s.System, "sv");
        Assert.That(title.AsString, Is.EqualTo("Doktor"));
        Assert.Catch<EBold>(() => BoldMLAttributes.BoldSetPrimaryLanguageByName(s.System, "xx"), "unknown language");

        // Fallbacks: the secondary language, then any language.
        BoldMLAttributes.BoldSetSecondaryLanguageByName(s.System, "en");
        title.AsStringByLanguage("sv").AsString = "";
        Assert.That(title.AsString, Is.EqualTo("< en: Doctor >"));
        BoldMLAttributes.BoldSetSecondaryLanguageByName(s.System, "sv");
        Assert.That(title.AsString, Is.EqualTo("<< en: Doctor >>"));

        // brMLString holds all languages, and is what is stored, as a blob.
        string all = title.GetStringRepresentation(BoldMLAttributes.brMLString);
        Assert.That(all, Is.EqualTo("enÿDoctorÿsvÿÿ"));
        var blob = (IBoldBlobContent)title.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents);
        Assert.That(blob.asBlob, Is.EqualTo(all));
        Assert.That(title.AsIBoldValue(TBoldDomainElementProxyMode.bdepContents).ContentName, Is.EqualTo(BoldDefaultStreamNames.BoldContentName_Blob));

        TBoldObject q = s.NewPerson();
        var title2 = (TBAMLString)q.BoldMemberByExpressionName("title");
        title2.Assign(title);
        Assert.That(title2.AsStringByLanguage("en").AsString, Is.EqualTo("Doctor"));
        Assert.That(title2.AsStringByLanguage("en"), Is.InstanceOf<TBAMLSubString>());
        title2.SetStringRepresentation(BoldMLAttributes.brMLString, "enÿMrÿsvÿHerrÿ");
        Assert.That(title2.AsStringByLanguage("sv").AsString, Is.EqualTo("Herr"));
        Assert.That(BoldMLAttributes.BoldMLLanguageClassName, Is.EqualTo("LanguageClass"));

        var derived = new TBADerivedValueSetValueList(s.System, "LanguageClass", "LanguageNumber", "LanguageName");
        Assert.That(derived.FindByString(1, "sv")!.AsInteger, Is.EqualTo(1));
        Assert.Catch<EBold>(() => _ = new TBADerivedValueSetValueList(s.System, "NoSuchClass", "LanguageNumber"));
        Assert.Catch<EBold>(() => _ = new TBADerivedValueSetValueList(s.System, "LanguageClass", "LanguageName"), "not an integer");
    }
}
