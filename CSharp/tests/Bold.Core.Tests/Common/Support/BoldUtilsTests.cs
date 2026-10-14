// Delphi: Source/Common/Support/BoldUtils.pas
// Port map: CSharp/port/units/BoldUtils.md
namespace Bold.Tests;

public class BoldUtilsTests
{
    [Test]
    [Covers(typeof(BoldUtils), nameof(BoldUtils.BoldProductNameShort), nameof(BoldUtils.BoldProductVersion))]
    public void Constants_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldUtils), "Source/Common/Support/BoldUtils.pas");
    }

    [TestCase("", 1, 0)]
    [TestCase("0..1", 1, 0)]
    [TestCase("1", 1, 1)]
    [TestCase("1..*", int.MaxValue, 1)]
    [TestCase("0..n", int.MaxValue, 0)]
    [TestCase("*", int.MaxValue, 0)]
    [TestCase("2..5", 5, 2)]
    [Covers(typeof(BoldUtils), nameof(BoldUtils.GetUpperLimitForMultiplicity), nameof(BoldUtils.GetLowerLimitForMultiplicity))]
    public void Multiplicity_limits(string multiplicity, int upper, int lower)
    {
        Assert.That(BoldUtils.GetUpperLimitForMultiplicity(multiplicity), Is.EqualTo(upper));
        Assert.That(BoldUtils.GetLowerLimitForMultiplicity(multiplicity), Is.EqualTo(lower));
    }

    [Test]
    [Covers(typeof(BoldUtils), nameof(BoldUtils.BoldTrim), nameof(BoldUtils.BoldSeparatedAppend), nameof(BoldUtils.CharCount),
        nameof(BoldUtils.CapitalisedToSpaced), nameof(BoldUtils.SpacedToCapitalised), nameof(BoldUtils.BoldCaseIndependentPos),
        nameof(BoldUtils.BoldIsPrefix), nameof(BoldUtils.BoldAnsiEqual), nameof(BoldUtils.BoldNamesEqual), nameof(BoldUtils.EnsureTrailing),
        nameof(BoldUtils.BoldStrEqual), nameof(BoldUtils.BoldStrAnsiEqual), nameof(BoldUtils.BoldStrStringEqual), nameof(BoldUtils.BoldStrCaseIndpendentEqual))]
    public void String_helpers()
    {
        Assert.That(BoldUtils.BoldTrim("\t a b \r\n"), Is.EqualTo("a b"));
        Assert.That(BoldUtils.BoldTrim(" \t"), Is.EqualTo(""));
        Assert.That(BoldUtils.BoldSeparatedAppend("a", "b"), Is.EqualTo("a,b"));
        Assert.That(BoldUtils.BoldSeparatedAppend("", "b", ";"), Is.EqualTo("b"));
        Assert.That(BoldUtils.BoldSeparatedAppend("a", ""), Is.EqualTo("a"));
        Assert.That(BoldUtils.CharCount('a', "banana"), Is.EqualTo(3));
        Assert.That(BoldUtils.CapitalisedToSpaced("FirstNameOCL"), Is.EqualTo("First Name OCL"));
        Assert.That(BoldUtils.SpacedToCapitalised("First Name"), Is.EqualTo("FirstName"));
        Assert.That(BoldUtils.BoldCaseIndependentPos("NAME", "firstname"), Is.EqualTo(6));
        Assert.That(BoldUtils.BoldCaseIndependentPos("x", "firstname"), Is.EqualTo(0));
        Assert.That(BoldUtils.BoldIsPrefix("Bold.Core", "Bold"), Is.True);
        Assert.That(BoldUtils.BoldIsPrefix("Bold", "bold"), Is.False);
        Assert.That(BoldUtils.BoldAnsiEqual("Ärger", "ärger"), Is.True);
        Assert.That(BoldUtils.BoldNamesEqual("Person", "PERSON"), Is.True);
        Assert.That(BoldUtils.BoldStrEqual("abcd", "abxx", 2), Is.True);
        Assert.That(BoldUtils.BoldStrAnsiEqual("ABcd", "abxx", 2), Is.True);
        Assert.That(BoldUtils.BoldStrStringEqual("ab", "abc", 2), Is.True);
        Assert.That(BoldUtils.BoldStrCaseIndpendentEqual("Ab1", "aB1", 3), Is.True);
        Assert.That(BoldUtils.BoldStrCaseIndpendentEqual("[", "{", 1), Is.False, "only letters fold");
        string s = "path";
        BoldUtils.EnsureTrailing(ref s, '\\');
        BoldUtils.EnsureTrailing(ref s, '\\');
        Assert.That(s, Is.EqualTo("path\\"));
    }

    [Test]
    [Covers(typeof(BoldUtils), nameof(BoldUtils.BooleanToString), nameof(BoldUtils.StringToBoolean), nameof(BoldUtils.BoldRootRegistryKey),
        nameof(BoldUtils.IsLocalMachine), nameof(BoldUtils.GetComputerNameStr), nameof(BoldUtils.EnumToStrings),
        nameof(BoldUtils.StringListToVarArray), nameof(BoldUtils.BoldVariantToStrings), nameof(BoldUtils.BoldSeparateStringList))]
    public void Conversions()
    {
        Assert.That(BoldUtils.BooleanToString(true), Is.EqualTo("True"));
        Assert.That(BoldUtils.StringToBoolean("t"), Is.True);
        Assert.That(BoldUtils.StringToBoolean("yes"), Is.False);
        Assert.That(BoldUtils.BoldRootRegistryKey(), Is.EqualTo("Software\\BoldSoft\\BfD\\4.0"));
        Assert.That(BoldUtils.IsLocalMachine(" "), Is.True);
        Assert.That(BoldUtils.IsLocalMachine(Environment.MachineName.ToLowerInvariant()), Is.True);
        var names = new TStringList();
        BoldUtils.EnumToStrings(typeof(TBoldStringCompareMode), names);
        Assert.That(names.CommaText, Is.EqualTo("bscCaseDependent,bscCaseIndependent,bscLocaleCaseIndependent"));
        Assert.That(BoldUtils.StringListToVarArray(new TStringList()), Is.Null);
        object? array = BoldUtils.StringListToVarArray(names);
        var back = new TStringList();
        Assert.That(BoldUtils.BoldVariantToStrings(array, back), Is.EqualTo(3));
        Assert.That(BoldUtils.BoldSeparateStringList(back, "+", "(", ")"), Is.EqualTo("(bscCaseDependent+bscCaseIndependent+bscLocaleCaseIndependent)"));
        Assert.That(BoldUtils.BoldSeparateStringList(new TStringList(), "+", "(", ")"), Is.EqualTo(""));
    }

    [Test]
    [Covers(typeof(BoldUtils), nameof(BoldUtils.BoldAppendToStrings))]
    public void Appended_text_wraps_at_80_characters()
    {
        var s = new TStringList();
        BoldUtils.BoldAppendToStrings(s, "first\r\nline", false);
        Assert.That(s[0], Is.EqualTo("first  line"));
        string longText = string.Join(",", Enumerable.Repeat("abcdefghij", 12));
        BoldUtils.BoldAppendToStrings(s, longText, true);
        Assert.That(s.Count, Is.GreaterThan(2));
        Assert.That(s.ToStringArray().All(l => l.Length <= 80), Is.True);
        Assert.That(string.Concat(s.ToStringArray().Skip(1)), Is.EqualTo(longText));
    }

    [Test]
    [Covers(typeof(BoldUtils), nameof(BoldUtils.BoldParseFormattedDate), nameof(BoldUtils.BoldParseFormattedDateList),
        nameof(BoldUtils.StrToDateFmt), nameof(BoldUtils.DateToStrFmt), nameof(BoldUtils.TimeStampComp))]
    public void Dates()
    {
        DateTime d = default;
        Assert.That(BoldUtils.BoldParseFormattedDate("2024-02-29", ["dd.mm.yyyy", "yyyy-mm-dd"], ref d), Is.True);
        Assert.That(d, Is.EqualTo(new DateTime(2024, 2, 29)));
        Assert.That(BoldUtils.BoldParseFormattedDate("2023-02-29", ["yyyy-mm-dd"], ref d), Is.False);
        Assert.That(BoldUtils.BoldParseFormattedDate("2023-13-01", ["yyyy-mm-dd"], ref d), Is.False);
        Assert.That(BoldUtils.BoldParseFormattedDate("05.06.07", ["dd.mm.yy"], ref d), Is.True);
        Assert.That(d.Year, Is.EqualTo(2007));
        Assert.That(BoldUtils.DateToStrFmt(new DateTime(2024, 3, 7), "yyyy/mm/dd", '-'), Is.EqualTo("2024-03-07"));
        Assert.That(BoldUtils.StrToDateFmt("07.03.2024", "dd/mm/yyyy", '.'), Is.EqualTo(new DateTime(2024, 3, 7)));
        TTimeStamp t1 = SysUtils.DateTimeToTimeStamp(new DateTime(2024, 1, 1, 10, 0, 0));
        TTimeStamp t2 = SysUtils.DateTimeToTimeStamp(new DateTime(2024, 1, 1, 11, 0, 0));
        Assert.That(BoldUtils.TimeStampComp(t1, t2), Is.EqualTo(-1));
        Assert.That(BoldUtils.TimeStampComp(t2, t1), Is.EqualTo(1));
        Assert.That(BoldUtils.TimeStampComp(t1, t1), Is.EqualTo(0));
    }

    [Test]
    [Covers(typeof(BoldUtils), nameof(BoldUtils.ForceDirectories), nameof(BoldUtils.DirectoryExists), nameof(BoldUtils.GetModuleFileNameAsString))]
    public void Files()
    {
        string dir = Path.Combine(Path.GetTempPath(), "bold-" + Guid.NewGuid().ToString("N"), "a", "b");
        Assert.That(BoldUtils.ForceDirectories(dir), Is.True);
        Assert.That(BoldUtils.DirectoryExists(dir), Is.True);
        Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(dir))!, true);
        Assert.Throws<Exception>(() => BoldUtils.ForceDirectories(""));
        Assert.That(BoldUtils.GetModuleFileNameAsString(false), Is.Not.Empty);
        Assert.That(BoldUtils.GetModuleFileNameAsString(true), Does.EndWith(BoldUtils.GetModuleFileNameAsString(false)));
    }
}
