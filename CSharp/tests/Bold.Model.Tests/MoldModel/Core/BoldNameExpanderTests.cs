// Delphi: Source/MoldModel/Core/BoldNameExpander.pas, BoldMoldConsts.pas, BoldMetaSupport.pas
// Port map: CSharp/port/units/BoldNameExpander.md
namespace Bold.Tests;

public class BoldNameExpanderTests
{
    [Test]
    [Covers(typeof(BoldMoldConsts), "*")]
    public void Mold_messages_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldMoldConsts), "Source/MoldModel/Core/BoldMoldConsts.pas");
    }

    [Test]
    [Covers(typeof(TExpansionType))]
    public void Expansion_types_have_the_Delphi_members()
    {
        PortAssert.EnumsMatchDelphi("Source/MoldModel/Core/BoldNameExpander.pas", typeof(TExpansionType));
    }

    [TestCase("T<Name>", "Person", TExpansionType.xtDelphi, "TPerson")]
    [TestCase("<name>List<NAME>", "Item", TExpansionType.xtDelphi, "ItemListItem")]
    [TestCase("<Name>", "Größe", TExpansionType.xtDelphi, "Gr__e")]
    [TestCase("<Name>", "1st", TExpansionType.xtExpression, "_st")]
    [TestCase("<Name>", "a-b c", TExpansionType.xtSQL, "a_b_c")]
    [TestCase("<Name>", "it's", TExpansionType.xtSQL, "it's")]
    [TestCase("<Nam", "X", TExpansionType.xtDelphi, "_Nam")]
    [Covers(typeof(BoldNameExpander), nameof(BoldNameExpander.BoldExpandName))]
    [Covers(typeof(TBoldAbstractNameExpander), nameof(TBoldAbstractNameExpander.ExpandName), nameof(TBoldAbstractNameExpander.MapCharacters),
        nameof(TBoldAbstractNameExpander.ValidCharacters), nameof(TBoldAbstractNameExpander.MapCharacter), "GetValidCharacters", "GetMapCharacter")]
    [Covers(typeof(TBoldDelphiNameExpander), "*")]
    [Covers(typeof(TBoldExpressionNameExpander), "*")]
    [Covers(typeof(TBoldSQLNameExpander), "*")]
    public void Names_expand_and_map(string template, string name, TExpansionType type, string expected)
    {
        Assert.That(BoldNameExpander.BoldExpandName(template, name, type, -1, TBoldNationalCharConversion.nccDefault), Is.EqualTo(expected));
    }

    [Test]
    [Covers(typeof(BoldNameExpander), nameof(BoldNameExpander.BoldNameExpanderMapAnsiCharacters), nameof(BoldNameExpander.BoldSQLNameExpanderUpperCaseNames),
        nameof(BoldNameExpander.BoldSQLNameExpanderNameLimit), nameof(BoldNameExpander.BoldSQLNameExpanderClass), nameof(BoldNameExpander.BoldDelphiNameExpanderClass),
        nameof(BoldNameExpander.BoldExpressionNameExpanderClass))]
    public void National_characters_and_settings()
    {
        Assert.That(BoldNameExpander.BoldExpandName("<Name>", "Größe", TExpansionType.xtDelphi, -1, TBoldNationalCharConversion.nccTrue), Is.EqualTo("Gro_e"),
            "ö maps to o; ß is not in the table");
        Assert.That(BoldNameExpander.BoldExpandName("<Name>", "Größe", TExpansionType.xtDelphi, -1, TBoldNationalCharConversion.nccFalse), Is.EqualTo("Gr__e"));
        try
        {
            BoldNameExpander.BoldNameExpanderMapAnsiCharacters = true;
            Assert.That(BoldNameExpander.BoldExpandName("<Name>", "Ärger", TExpansionType.xtExpression, -1, TBoldNationalCharConversion.nccDefault), Is.EqualTo("Arger"));
            BoldNameExpander.BoldSQLNameExpanderUpperCaseNames = true;
            Assert.That(BoldNameExpander.BoldExpandName("<Name>_T", "abc", TExpansionType.xtSQL, -1, TBoldNationalCharConversion.nccDefault), Is.EqualTo("ABC_T"));
            BoldNameExpander.BoldSQLNameExpanderNameLimit = 10;
            Assert.That(BoldNameExpander.BoldExpandName("<Name>", "ABCDEFGHIJKLMNOP", TExpansionType.xtSQL, -1, TBoldNationalCharConversion.nccDefault).Length,
                Is.EqualTo(10), "-1 means the SQL name limit");
        }
        finally
        {
            BoldNameExpander.BoldNameExpanderMapAnsiCharacters = false;
            BoldNameExpander.BoldSQLNameExpanderUpperCaseNames = false;
            BoldNameExpander.BoldSQLNameExpanderNameLimit = 255;
        }

        Assert.That(BoldNameExpander.BoldSQLNameExpanderClass, Is.EqualTo(typeof(TBoldSQLNameExpander)));
        Assert.That(BoldNameExpander.BoldDelphiNameExpanderClass, Is.EqualTo(typeof(TBoldDelphiNameExpander)));
        Assert.That(BoldNameExpander.BoldExpressionNameExpanderClass, Is.EqualTo(typeof(TBoldExpressionNameExpander)));
    }

    [Test]
    [Covers(typeof(TBoldAbstractNameExpander), nameof(TBoldAbstractNameExpander.TruncateName))]
    public void Long_names_end_in_a_checksum_of_the_full_name()
    {
        static string DelphiTruncate(string name, int max)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVXYZ01234567890_";
            string result = name.Substring(0, max - 3);
            int sum = 0;
            foreach (char c in name)
            {
                sum = (sum ^ c) & 0xFFFF;
                sum = ((sum << 3) % 65536) + (sum >> 13);
            }

            while (sum > 0 && result.Length < max)
            {
                result += chars[sum % 37];
                sum /= 37;
            }

            return result;
        }

        var e = new TBoldSQLNameExpander();
        string longName = "VERY_LONG_TABLE_NAME_FOR_ORACLE";
        Assert.That(e.TruncateName(longName, 18), Is.EqualTo(DelphiTruncate(longName, 18)));
        Assert.That(e.TruncateName(longName, 18), Does.StartWith("VERY_LONG_TABLE"));
        Assert.That(e.TruncateName(longName, 18), Is.Not.EqualTo(e.TruncateName(longName + "X", 18)));
        Assert.That(e.TruncateName("SHORT", 18), Is.EqualTo("SHORT"));
        Assert.That(e.TruncateName(longName, -1), Is.EqualTo(longName));
    }

    [TestCase("Good_1", true, "")]
    [TestCase("bad-name", false, "Name has invalid characters")]
    [TestCase("TooLongForTheLimit", false, "Name is too long")]
    [TestCase("1st", false, "Name has invalid characters")]
    [TestCase("", false, "Name can not be empty")]
    [Covers(typeof(TBoldAbstractNameExpander), nameof(TBoldAbstractNameExpander.ValidateName), nameof(TBoldAbstractNameExpander.LanguageIsCaseSensitive))]
    public void Delphi_names_validate(string name, bool valid, string reason)
    {
        var e = new TBoldDelphiNameExpander();
        string r = "";
        Assert.That(e.ValidateName(name, ref r, TBoldNationalCharConversion.nccDefault, 12), Is.EqualTo(valid));
        Assert.That(r, Is.EqualTo(reason));
        Assert.That(e.LanguageIsCaseSensitive, Is.False);
        string r2 = "";
        Assert.That(new TBoldExpressionNameExpander().ValidateName(name, ref r2, TBoldNationalCharConversion.nccDefault, 12), Is.EqualTo(valid));
    }

    [Test]
    [Covers(typeof(BoldNameExpander), nameof(BoldNameExpander.BoldExpandPrefix))]
    public void Table_prefix_is_replaced_before_expansion()
    {
        Assert.That(BoldNameExpander.BoldExpandPrefix("<prefix>_<Name>", "Person", "BOLD", -1, TBoldNationalCharConversion.nccDefault), Is.EqualTo("BOLD_Person"));
        Assert.That(BoldNameExpander.BoldExpandPrefix(BoldDefs.OBJECTTABLE_NAME, "", "X", -1, TBoldNationalCharConversion.nccDefault), Is.EqualTo("X_OBJECT"));
    }

    [Test]
    [Covers(typeof(TBoldMetaSupport), "*")]
    [Covers(typeof(BoldMetaSupport), "*")]
    [Covers(typeof(TBoldNamePairRecord))]
    public void Parameter_types_map_to_COM_and_IDL()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldMetaSupport), "Source/MoldModel/Core/BoldMetaSupport.pas", "BoldObjectSpaceInterfacenames");
        string delphi = File.ReadAllText(RepoPaths.Full("Source/MoldModel/Core/BoldMetaSupport.pas"));
        var pairs = System.Text.RegularExpressions.Regex.Matches(delphi, @"\(InterfaceName: '(\w+)'; DelphiName: '(\w+)'\)")
            .Select(m => new TBoldNamePairRecord(m.Groups[1].Value, m.Groups[2].Value)).ToArray();
        Assert.That(BoldMetaSupport.BoldObjectSpaceInterfacenames, Is.EqualTo(pairs));
        using var model = new TMoldModel(null, "M");
        var person = new TMoldClass(model, "Person") { SuperClass = model.RootClass };
        bool flag = false;
        Assert.That(TBoldMetaSupport.ParameterTypeToIDLType("string", model, ref flag), Is.EqualTo("BSTR"));
        Assert.That(flag, Is.False);
        Assert.That(TBoldMetaSupport.ParameterTypeToIDLType("Person", model, ref flag), Is.EqualTo("IPerson"));
        Assert.That(flag, Is.True);
        Assert.That(TBoldMetaSupport.ParameterTypeToIDLType("Pointer", model, ref flag), Is.EqualTo(""));
        Assert.That(TBoldMetaSupport.ParameterTypeToCOMType("String", model, ref flag), Is.EqualTo("WideString"));
        Assert.That(flag, Is.True, "strings are passed const");
        Assert.That(TBoldMetaSupport.ParameterTypeToCOMType("Integer", model, ref flag), Is.EqualTo("Integer"));
        Assert.That(flag, Is.False);
        Assert.That(TBoldMetaSupport.ParameterTypeToCOMType("TPerson", model, ref flag), Is.EqualTo("IPerson"), "by Delphi name");
        Assert.That(TBoldMetaSupport.ParameterTypeToInterfaceType("TBoldObjectList", model), Is.EqualTo("IBoldObjectList"));
        Assert.That(TBoldMetaSupport.ParameterTypeToInterfaceType("IUnknown", model), Is.EqualTo("IUnknown"));
        Assert.That(BoldMetaSupport.BoldObjectSpaceInterfacenames, Has.Length.EqualTo(20));
        Assert.That(person.ExpandedInterfaceName, Is.EqualTo("IPerson"));
    }
}
