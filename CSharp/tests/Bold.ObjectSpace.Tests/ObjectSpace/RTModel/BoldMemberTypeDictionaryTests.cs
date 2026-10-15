// Delphi: Source/ObjectSpace/RTModel/BoldMemberTypeDictionary.pas
// Port map: CSharp/port/units/BoldMemberTypeDictionary.md
namespace Bold.Tests;

public class BoldMemberTypeDictionaryTests
{
    private sealed class TBAString;

    private sealed class TBAInteger;

    [Test]
    [Covers(typeof(TBoldMemberTypeList), "*")]
    [Covers(typeof(TBoldMemberTypeDescriptor), "*")]
    [Covers(typeof(BoldMemberTypeDictionary), "*")]
    [Covers("BoldMemberTypeDictionary", "TMemberNameIndex", "TMemberNameIndex.ItemAsKeyString", "TMemberClassIndex",
        "TMemberClassIndex.ItemAsKeyClass", "initialization", "finalization")]
    public void Member_types_are_found_by_class_and_class_name()
    {
        var list = new TBoldMemberTypeList();
        list.AddMemberTypeDescriptor(typeof(TBAString), TBoldAbstractionLevel.alConcrete);
        list.AddMemberTypeDescriptor(typeof(TBAInteger), TBoldAbstractionLevel.alAbstract);
        Assert.That(list.DescriptorByClass(typeof(TBAInteger))!.AbstractionLevel, Is.EqualTo(TBoldAbstractionLevel.alAbstract));
        Assert.That(list.DescriptorByDelphiName("tbastring")!.MemberClass, Is.EqualTo(typeof(TBAString)), "names ignore case");
        Assert.That(list.Descriptors(1).MemberClass, Is.EqualTo(typeof(TBAInteger)));
        list.RemoveDescriptorByClass(typeof(TBAString));
        Assert.That(list.DescriptorByClass(typeof(TBAString)), Is.Null);
        Assert.That(list.DescriptorByDelphiName("TBAString"), Is.Null);
        Assert.That(list.Count, Is.EqualTo(1));

        TBoldMemberTypeList global = BoldMemberTypeDictionary.BoldMemberTypes();
        Assert.That(BoldMemberTypeDictionary.BoldMemberTypesAssigned(), Is.True, "created on first use");
        Assert.That(BoldMemberTypeDictionary.BoldMemberTypes(), Is.SameAs(global));
    }
}
