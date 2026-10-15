// Delphi: Source/ObjectSpace/RTModel/BoldGeneratedCodeDictionary.pas
// Port map: CSharp/port/units/BoldGeneratedCodeDictionary.md
namespace Bold.Tests;

public class BoldGeneratedCodeDictionaryTests
{
    [Test]
    [Covers(typeof(TBoldGeneratedCodeList), "*")]
    [Covers(typeof(TBoldGeneratedCodeDescriptor), "*")]
    [Covers(typeof(TBoldGeneratedClassList), "*")]
    [Covers(typeof(TBoldGeneratedClassDescriptor), "*")]
    [Covers(typeof(BoldGeneratedCodeDictionary), "*")]
    [Covers(typeof(TInstallBusinessClasses))]
    [Covers(typeof(TInstallObjectListClasses))]
    [Covers("BoldGeneratedCodeDictionary", "TGeneratedCodeExpressionNameIndex", "TGeneratedCodeExpressionNameIndex.ItemAsKeyString",
        "TGeneratedClassExpressionNameIndex", "TGeneratedClassExpressionNameIndex.ItemAsKeyString", "initialization", "finalization")]
    public void Generated_code_registers_its_classes_by_model()
    {
        Assert.That(BoldGeneratedCodeDictionary.BoldGeneratedCodesAssigned(), Is.True, "created when the unit loads");
        var codes = new TBoldGeneratedCodeList();
        TBoldGeneratedCodeDescriptor d = codes.AddGeneratedCodeDescriptorWithFunc("TownModel",
            classes => classes.AddObjectEntry("Person", typeof(string)),
            lists => lists.AddEntry(new TBoldGeneratedClassDescriptor("PersonList", typeof(List<string>))),
            "1234");
        Assert.That(codes.DescriptorByExpressionName("townmodel"), Is.SameAs(d), "names ignore case");
        Assert.That(codes.DescriptorByExpressionName("Other"), Is.Null);
        Assert.That(codes.ModelEntries(0), Is.SameAs(d));
        Assert.That((d.ExpressionName, d.CRC), Is.EqualTo(("TownModel", "1234")));
        Assert.That(codes.AddGeneratedCodeDescriptorWithFunc("NoCrc", null, null).CRC, Is.EqualTo(""));

        var classes = new TBoldGeneratedClassList();
        d.InstallBusinessClasses!(classes);
        d.InstallObjectListClasses!(classes);
        Assert.That(classes.EntryByExpressionName("PERSON")!.TheClass, Is.EqualTo(typeof(string)));
        Assert.That(classes.EntryByExpressionName("PersonList")!.ExpressionName, Is.EqualTo("PersonList"));
        Assert.That(classes.EntryByExpressionName("Nobody"), Is.Null);

        TBoldGeneratedCodeList global = BoldGeneratedCodeDictionary.GeneratedCodes();
        Assert.That(BoldGeneratedCodeDictionary.GeneratedCodes(), Is.SameAs(global));
    }
}
