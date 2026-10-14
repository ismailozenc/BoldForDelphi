namespace Bold.Tests;

/// <summary>Delphi SysUtils semantics the port depends on.</summary>
public class SysUtilsTests
{
    [TestCase("%d items", new object[] { 3 }, "3 items")]
    [TestCase("%5d|%-5d|", new object[] { 42, 42 }, "   42|42   |")]
    [TestCase("%.3d", new object[] { 7 }, "007")]
    [TestCase("%s.%s", new object[] { "a", "b" }, "a.b")]
    [TestCase("%1:s %0:s", new object[] { "a", "b" }, "b a")]
    [TestCase("%x", new object[] { 255 }, "FF")]
    [TestCase("%.2f", new object[] { 3.14159 }, "3.14")]
    [TestCase("%n", new object[] { 1234567.891 }, "1,234,567.89")]
    [TestCase("100%%", new object[0], "100%")]
    [TestCase("%*d", new object[] { 4, 1 }, "   1")]
    public void Format_follows_Delphi(string format, object[] args, string expected)
    {
        Assert.That(SysUtils.Format(format, args), Is.EqualTo(expected));
    }

    [Test]
    public void Strings_are_one_based()
    {
        Assert.That(SysUtils.Pos("lo", "hello"), Is.EqualTo(4));
        Assert.That(SysUtils.Pos("x", "hello"), Is.EqualTo(0));
        Assert.That(SysUtils.PosEx("l", "hello", 4), Is.EqualTo(4));
        Assert.That(SysUtils.Copy("hello", 2, 3), Is.EqualTo("ell"));
        Assert.That(SysUtils.Copy("hello", 4, 100), Is.EqualTo("lo"));
        Assert.That(SysUtils.Copy("hello", 0, 2), Is.EqualTo("he"));
        Assert.That(SysUtils.Copy("hello", 9, 2), Is.EqualTo(""));
        Assert.That(SysUtils.LastDelimiter("\\/", "a/b\\c"), Is.EqualTo(4));
    }

    [Test]
    public void Conversions()
    {
        Assert.That(SysUtils.StrToInt("$1F"), Is.EqualTo(31));
        Assert.That(SysUtils.StrToIntDef("x", -1), Is.EqualTo(-1));
        Assert.Throws<EConvertError>(() => SysUtils.StrToInt("1.5"));
        Assert.That(SysUtils.IntToHex(255, 4), Is.EqualTo("00FF"));
        Assert.That(SysUtils.QuotedStr("it's"), Is.EqualTo("'it''s'"));
        Assert.That(SysUtils.AnsiDequotedStr("'it''s'", '\''), Is.EqualTo("it's"));
        Assert.That(SysUtils.CompareText("abc", "ABD"), Is.LessThan(0));
        Assert.That(SysUtils.SameText("Abc", "aBC"), Is.True);
        Assert.That(SysUtils.StringReplace("aAa", "a", "b", DelphiSet<TReplaceFlag>.Of(TReplaceFlag.rfReplaceAll, TReplaceFlag.rfIgnoreCase)), Is.EqualTo("bbb"));
        Assert.That(SysUtils.StringReplace("aAa", "a", "b", DelphiSet<TReplaceFlag>.Empty), Is.EqualTo("bAa"));
        Assert.That(SysUtils.ChangeFileExt("dir/model.bld", ".xml"), Is.EqualTo("dir/model.xml"));
        Assert.That(SysUtils.ExtractFileExt("model.bld"), Is.EqualTo(".bld"));
    }

    [Test]
    public void Dates_format_and_parse_with_the_format_settings()
    {
        var d = new DateTime(2024, 3, 7, 14, 5, 9, 12);
        Assert.That(SysUtils.FormatDateTime("yyyy-mm-dd hh:nn:ss.zzz", d), Is.EqualTo("2024-03-07 14:05:09.012"));
        Assert.That(SysUtils.FormatDateTime("d/m/yy h:mm", d), Is.EqualTo("7/3/24 14:05"), "m after h means minutes");
        Assert.That(SysUtils.FormatDateTime("dddd d mmmm", d), Is.EqualTo("Thursday 7 March"));
        Assert.That(SysUtils.FormatDateTime("h:nn am/pm", d), Is.EqualTo("2:05 pm"));
        Assert.That(SysUtils.FormatDateTime("'Year' yyyy", d), Is.EqualTo("Year 2024"));
        Assert.That(SysUtils.DateToStr(d), Is.EqualTo("3/7/2024"));
        Assert.That(SysUtils.StrToDate("3/7/2024"), Is.EqualTo(new DateTime(2024, 3, 7)));
        Assert.That(SysUtils.StrToDateTime("3/7/2024 2:05:09 PM"), Is.EqualTo(new DateTime(2024, 3, 7, 14, 5, 9)));
        Assert.That(SysUtils.StrToTime("14:05").TimeOfDay, Is.EqualTo(new TimeSpan(14, 5, 0)));
        Assert.Throws<EConvertError>(() => SysUtils.StrToDate("2/30/2024"));
        Assert.That(SysUtils.EncodeTime(0, 0, 0, 0).ToOADate(), Is.EqualTo(0.0), "Delphi's day zero is the OLE day zero");
        TTimeStamp ts = SysUtils.DateTimeToTimeStamp(d);
        Assert.That(ts.Date, Is.EqualTo(738952), "days since 0001-01-01 plus one, as Delphi");
        Assert.That(SysUtils.TimeStampToDateTime(ts), Is.EqualTo(d));
    }
}
