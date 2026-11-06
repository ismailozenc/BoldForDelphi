// Delphi: Source/Common/Support/BoldIsoDateTime.pas (and BoldSupportConst.pas, its messages)
// Port map: CSharp/port/units/BoldIsoDateTime.md
namespace Bold.Tests;

public class BoldIsoDateTimeTests
{
    [Test]
    [Covers(typeof(BoldSupportConst), "*")]
    public void Messages_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldSupportConst), "Source/Common/Support/BoldSupportConst.pas");
    }

    [Test]
    [Covers(typeof(BoldIsoDateTime), nameof(BoldIsoDateTime.ParseISODate), "MatchPattern")]
    public void Dates_are_read_and_checked()
    {
        Assert.That(BoldIsoDateTime.ParseISODate("2026-11-06"), Is.EqualTo(new DateTime(2026, 11, 6)));
        Assert.That(BoldIsoDateTime.ParseISODate("2024-02-29"), Is.EqualTo(new DateTime(2024, 2, 29)));
        Assert.That(() => BoldIsoDateTime.ParseISODate("2026-1-06"), Throws.InstanceOf<EBold>().With.Message.EqualTo("ParseISODate: Invalid date format 2026-1-06. Should be YYYY-MM-DD"));
        Assert.That(() => BoldIsoDateTime.ParseISODate("2026-13-01"), Throws.InstanceOf<EBold>().With.Message.Contains("month > 12"));
        Assert.That(() => BoldIsoDateTime.ParseISODate("2026-00-01"), Throws.InstanceOf<EBold>().With.Message.Contains("month < 1"));
        Assert.That(() => BoldIsoDateTime.ParseISODate("2026-04-00"), Throws.InstanceOf<EBold>().With.Message.Contains("date < 1"));
        Assert.That(() => BoldIsoDateTime.ParseISODate("2026-04-31"), Throws.InstanceOf<EBold>().With.Message.EqualTo("ParseISODate: Invalid date format 2026-04-31. there is only 30 days in month 4"));
        Assert.That(() => BoldIsoDateTime.ParseISODate("2026-02-29"), Throws.InstanceOf<EConvertError>(), "not a leap year");
    }

    [Test]
    [Covers(typeof(BoldIsoDateTime), nameof(BoldIsoDateTime.ParseISOTime))]
    public void Times_are_read_and_checked()
    {
        Assert.That(BoldIsoDateTime.ParseISOTime("13:45:30").TimeOfDay, Is.EqualTo(new TimeSpan(13, 45, 30)));
        Assert.That(BoldIsoDateTime.ParseISOTime("13:45").TimeOfDay, Is.EqualTo(new TimeSpan(13, 45, 0)), "the seconds may be left out");
        Assert.That(BoldIsoDateTime.ParseISOTime("13:45").Date, Is.EqualTo(SysUtils.ZeroDateTime), "on Delphi's day zero");
        Assert.That(() => BoldIsoDateTime.ParseISOTime("1:45"), Throws.InstanceOf<EBold>().With.Message.Contains("Should be HH:MM[:SS]"));
        Assert.That(() => BoldIsoDateTime.ParseISOTime("24:00"), Throws.InstanceOf<EBold>().With.Message.Contains("h > 23"));
        Assert.That(() => BoldIsoDateTime.ParseISOTime("12:60"), Throws.InstanceOf<EBold>().With.Message.Contains("m > 59"));
        Assert.That(() => BoldIsoDateTime.ParseISOTime("12:00:60"), Throws.InstanceOf<EBold>().With.Message.Contains("s > 59"));
    }

    [Test]
    [Covers(typeof(BoldIsoDateTime), nameof(BoldIsoDateTime.ParseISODateTime))]
    public void Dates_with_times_are_read()
    {
        Assert.That(BoldIsoDateTime.ParseISODateTime("2026-11-06 13:45:30"), Is.EqualTo(new DateTime(2026, 11, 6, 13, 45, 30)));
        Assert.That(BoldIsoDateTime.ParseISODateTime("2026-11-06 13:45"), Is.EqualTo(new DateTime(2026, 11, 6, 13, 45, 0)));
        Assert.That(() => BoldIsoDateTime.ParseISODateTime("2026-11-06T13:45"), Throws.InstanceOf<EBold>().With.Message.Contains("YYYY-MM-DD HH:MM[:SS]"));
    }
}
