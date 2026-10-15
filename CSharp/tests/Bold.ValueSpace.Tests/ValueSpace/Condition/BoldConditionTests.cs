// Delphi: Source/ValueSpace/Condition/BoldCondition.pas
// Port map: CSharp/port/units/BoldCondition.md
using System.Xml;

namespace Bold.Tests;

public class BoldConditionTests
{
    private static T RoundTrip<T>(T condition, TBoldXMLStreamManager? manager = null) where T : TBoldCondition
    {
        manager ??= new TBoldXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry());
        var doc = new XmlDocument();
        manager.NewRootNode(doc, "Root").WriteObject("", condition);
        var reread = new XmlDocument();
        reread.LoadXml(doc.OuterXml);
        return (T)manager.GetRootNode(reread, "Root").ReadObject("")!;
    }

    [Test]
    [Covers(typeof(TBoldCondition), "*")]
    [Covers(typeof(TBoldConditionWithClass), "*")]
    [Covers(typeof(TBoldXMLConditionStreamer), "*")]
    [Covers(typeof(TBoldXMLConditionWithClassStreamer), "*")]
    [Covers("BoldCondition", "initialization")]
    public void Class_condition_round_trips()
    {
        var c = new TBoldConditionWithClass { TopSortedIndex = 4, MaxAnswers = 10, Offset = 20, AvailableAnswers = 30 };
        Assert.That(new TBoldConditionWithClass().Time, Is.EqualTo(BoldDefs.BOLDMAXTIMESTAMP), "now by default");
        Assert.That(new TBoldConditionWithClass().MaxAnswers, Is.EqualTo(-1));
        Assert.That(new TBoldConditionWithClass().Offset, Is.EqualTo(-1));
        c.Time = 7;
        TBoldConditionWithClass r = RoundTrip(c);
        Assert.That((r.TopSortedIndex, r.Time, r.MaxAnswers, r.Offset, r.AvailableAnswers), Is.EqualTo((4, 7, 10, 20, 30)));
        Assert.That(r.GetStreamName(), Is.EqualTo("ClassCondition"));
    }

    [Test]
    [Covers(typeof(TBoldSQLCondition), "*")]
    [Covers("BoldCondition", "TBoldXMLSQLConditionStreamer", "TBoldXMLSQLConditionStreamer.GetStreamName",
        "TBoldXMLSQLConditionStreamer.WriteObject", "TBoldXMLSQLConditionStreamer.ReadObject", "TBoldXMLSQLConditionStreamer.CreateObject")]
    public void SQL_condition_round_trips_with_parameters()
    {
        var c = new TBoldSQLCondition { WhereFragment = "a = :p1", OrderBy = "b", TopSortedIndex = 2 };
        Assert.That(c.JoinInheritedTables, Is.True);
        c.JoinInheritedTables = false;
        c.Params = new TParams();
        c.Params.CreateParam(TFieldType.ftInteger, "p1", TParamType.ptInput).AsInteger = 42;
        c.Params.CreateParam(TFieldType.ftString, "p2", TParamType.ptInput).AsString = "x <y>";
        c.Params.CreateParam(TFieldType.ftDateTime, "p3", TParamType.ptInput).AsDateTime = new DateTime(2020, 1, 2, 3, 4, 5);
        c.Params.CreateParam(TFieldType.ftFloat, "p4", TParamType.ptInput).AsFloat = 1.25;
        c.Params.CreateParam(TFieldType.ftCurrency, "p5", TParamType.ptOutput).AsCurrency = 3.5m;
        c.Params.CreateParam(TFieldType.ftBoolean, "p6", TParamType.ptInput).AsBoolean = true;
        c.Params.CreateParam(TFieldType.ftLargeint, "p7", TParamType.ptInput).AsLargeInt = 1L << 40;
        c.Params.CreateParam(TFieldType.ftString, "nullParam", TParamType.ptInput);
        TBoldSQLCondition r = RoundTrip(c);
        Assert.That((r.WhereFragment, r.OrderBy, r.JoinInheritedTables, r.TopSortedIndex), Is.EqualTo(("a = :p1", "b", false, 2)));
        Assert.That(r.Params!.Count, Is.EqualTo(8));
        Assert.That(r.Params.ParamByName("P1").AsInteger, Is.EqualTo(42));
        Assert.That(r.Params.ParamByName("p2").AsString, Is.EqualTo("x <y>"));
        Assert.That(r.Params.ParamByName("p3").AsDateTime, Is.EqualTo(new DateTime(2020, 1, 2, 3, 4, 5)));
        Assert.That(r.Params.ParamByName("p4").AsFloat, Is.EqualTo(1.25));
        Assert.That(r.Params.ParamByName("p5").AsCurrency, Is.EqualTo(3.5m));
        Assert.That(r.Params.ParamByName("p5").ParamType, Is.EqualTo(TParamType.ptOutput));
        Assert.That(r.Params.ParamByName("p6").AsBoolean, Is.True);
        Assert.That(r.Params.ParamByName("p7").AsLargeInt, Is.EqualTo(1L << 40));
        Assert.That(r.Params.ParamByName("nullParam").IsNull, Is.True);
        Assert.That(r.Params.ParamByName("nullParam").DataType, Is.EqualTo(TFieldType.ftString));

        c.Params = null;
        Assert.That(RoundTrip(c).Params, Is.Null);
        Assert.That(c.GetStreamName(), Is.EqualTo("SQLCondition"));
    }

    [Test]
    [Covers(typeof(TBoldTimestampCondition), "*")]
    [Covers(typeof(TBoldChangePointCondition), "*")]
    [Covers("BoldCondition", "TBoldXMLTimestampConditionStreamer", "TBoldXMLTimestampConditionStreamer.GetStreamName",
        "TBoldXMLTimestampConditionStreamer.WriteObject", "TBoldXMLTimestampConditionStreamer.ReadObject", "TBoldXMLTimestampConditionStreamer.CreateObject",
        "TBoldXMLChangePointConditionStreamer", "TBoldXMLChangePointConditionStreamer.GetStreamName", "TBoldXMLChangePointConditionStreamer.WriteObject",
        "TBoldXMLChangePointConditionStreamer.ReadObject", "TBoldXMLChangePointConditionStreamer.CreateObject")]
    public void Timestamp_and_change_point_conditions_round_trip()
    {
        TBoldTimestampCondition t = RoundTrip(new TBoldTimestampCondition { Timestamp = 12, TopSortedIndex = 1 });
        Assert.That((t.Timestamp, t.TopSortedIndex, t.GetStreamName()), Is.EqualTo((12, 1, "TimestampCondition")));

        var ids = new TBoldObjectIdList();
        ids.Add(new TBoldInternalObjectId(5, 0, true));
        var members = new TBoldMemberIdList();
        members.Add(new TBoldMemberID(3));
        // Object ids stream their class name, so this needs a stream manager with a model.
        using var model = new TMoldModel(null, "M");
        var manager = new TBoldDefaultXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry(), model);
        TBoldChangePointCondition cp = RoundTrip(new TBoldChangePointCondition { IdList = ids, StartTime = 1, EndTime = 9, MemberIdList = members }, manager);
        Assert.That((cp.StartTime, cp.EndTime, cp.GetStreamName()), Is.EqualTo((1, 9, "ChangePointCondition")));
        Assert.That(cp.IdList![0].AsString, Is.EqualTo("5"));
        Assert.That(cp.MemberIdList![0].MemberIndex, Is.EqualTo(3));
    }
}
