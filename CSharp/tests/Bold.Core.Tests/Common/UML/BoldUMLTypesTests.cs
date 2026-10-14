// Delphi: Source/Common/UML/BoldUMLTypes.pas
// Port map: CSharp/port/units/BoldUMLTypes.md
namespace Bold.Tests;

public class BoldUMLTypesTests
{
    [Test]
    [Covers(typeof(TBoldParameterDirectionKind))]
    [Covers(typeof(TAggregationKind))]
    [Covers(typeof(TVisibilityKind))]
    [Covers(typeof(TChangeableKind))]
    [Covers(typeof(TScopeKind))]
    [Covers(typeof(TOrderingKind))]
    [Covers(typeof(TPseudostateKind))]
    [Covers(typeof(TCallConcurrencyKind))]
    [Covers(typeof(TMessageDirectionKind))]
    public void Enums_have_the_Delphi_members_in_order()
    {
        PortAssert.EnumsMatchDelphi("Source/Common/UML/BoldUMLTypes.pas", typeof(TBoldParameterDirectionKind), typeof(TAggregationKind),
            typeof(TVisibilityKind), typeof(TChangeableKind), typeof(TScopeKind), typeof(TOrderingKind), typeof(TPseudostateKind),
            typeof(TCallConcurrencyKind), typeof(TMessageDirectionKind));
    }

    [TestCase("1", "1", 1, 1, false)]
    [TestCase("0..1", "0..1", 0, 1, false)]
    [TestCase("*", "n", 0, -1, true)]
    [TestCase("n", "n", 0, -1, true)]
    [TestCase("1..*", "1..n", 1, -1, true)]
    [TestCase(" 2 .. 5 ", "2..5", 2, 5, false)]
    [Covers(typeof(TBoldUMLRange), "*")]
    public void Range_parses_and_formats(string text, string formatted, int lower, int upper, bool unlimited)
    {
        var r = new TBoldUMLRange { AsString = text };
        Assert.That(r.AsString, Is.EqualTo(formatted));
        Assert.That(r.Lower, Is.EqualTo(lower));
        Assert.That(r.Upper, Is.EqualTo(upper));
        Assert.That(r.UpperUnlimited, Is.EqualTo(unlimited));
        Assert.That(r.InRange(lower), Is.True);
        Assert.That(r.InRange(lower - 1), Is.False);
        Assert.That(r.InRange(1000), Is.EqualTo(unlimited));
    }

    [Test]
    [Covers(typeof(TBoldUMLMultiplicity), "*")]
    public void Multiplicity_holds_several_ranges()
    {
        var m = new TBoldUMLMultiplicity { AsString = "0..1,3..*" };
        Assert.That(m.Count, Is.EqualTo(2));
        Assert.That(m.Range(1).Lower, Is.EqualTo(3));
        Assert.That(m.InRange(2), Is.False);
        Assert.That(m.InRange(4), Is.True);
        Assert.That(m.FormatAsString("*"), Is.EqualTo("0..1,3..*"));
        Assert.That(m.AsString, Is.EqualTo("0..1,3..n"));
        Assert.Throws<EConvertError>(() => m.AsString = "x..2");
    }
}
