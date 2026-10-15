// Delphi: Source/ValueSpace/Condition/BoldCondition.pas
// Port map: CSharp/port/units/BoldCondition.md

using System.Globalization;
using System.Xml;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldCondition", Symbol = "initialization", Note = "Registers the condition streamers from the Bold.ValueSpace module initializer.")]

namespace Bold;

/// <summary>What to fetch from a persistence layer, with paging: at most MaxAnswers from Offset (-1 = no limit).</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldCondition : TBoldNonRefCountedObject, IBoldStreamable
{
    private int fMaxAnswers;
    private int fOffset;
    private int fAvailableAnswers;

    [Delphi(PortStatus.Ported)]
    protected TBoldCondition()
    {
        fMaxAnswers = -1;
        fOffset = -1;
    }

    [Delphi(PortStatus.Ported)]
    public abstract string GetStreamName();

    [Delphi(PortStatus.Ported)] public int MaxAnswers { get => fMaxAnswers; set => fMaxAnswers = value; }
    [Delphi(PortStatus.Ported)] public int Offset { get => fOffset; set => fOffset = value; }
    [Delphi(PortStatus.Ported)] public int AvailableAnswers { get => fAvailableAnswers; set => fAvailableAnswers = value; }
}

/// <summary>All objects of a class (by topological index), at a point in time (now by default).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldConditionWithClass : TBoldCondition
{
    private int fTopSortedIndex;
    private TBoldTimeStampType fTime;

    [Delphi(PortStatus.Ported)]
    public TBoldConditionWithClass()
    {
        fTime = BoldDefs.BOLDMAXTIMESTAMP;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldCondition.ClassConditionName;

    [Delphi(PortStatus.Ported)] public int TopSortedIndex { get => fTopSortedIndex; set => fTopSortedIndex = value; }
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType Time { get => fTime; set => fTime = value; }
}

/// <summary>Objects of a class matching an SQL where fragment, ordered, with parameters.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLCondition : TBoldConditionWithClass
{
    private string fWhereFragment = "";
    private string fOrderBy = "";
    private TParams? fParams;
    private bool fJoinInheritedTables;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLCondition()
    {
        fJoinInheritedTables = true;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldCondition.SQLConditionName;

    [Delphi(PortStatus.Ported)] public string WhereFragment { get => fWhereFragment; set => fWhereFragment = value; }
    [Delphi(PortStatus.Ported)] public string OrderBy { get => fOrderBy; set => fOrderBy = value; }
    [Delphi(PortStatus.Ported)] public TParams? Params { get => fParams; set => fParams = value; }
    [Delphi(PortStatus.Ported)] public bool JoinInheritedTables { get => fJoinInheritedTables; set => fJoinInheritedTables = value; }
}

/// <summary>Objects of a class as they were at a timestamp.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTimestampCondition : TBoldConditionWithClass
{
    private TBoldTimeStampType fTimestamp;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldCondition.TimestampConditionName;

    [Delphi(PortStatus.Ported)] public TBoldTimeStampType Timestamp { get => fTimestamp; set => fTimestamp = value; }
}

/// <summary>The timestamps at which the given members of the given objects changed between two times.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldChangePointCondition : TBoldCondition
{
    private TBoldObjectIdList? fIdList;
    private TBoldTimeStampType fStart;
    private TBoldTimeStampType fEnd;
    private TBoldMemberIdList? fMemberIds;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldCondition.ChangePointConditionName;

    [Delphi(PortStatus.Ported)] public TBoldObjectIdList? IdList { get => fIdList; set => fIdList = value; }
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType StartTime { get => fStart; set => fStart = value; }
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType EndTime { get => fEnd; set => fEnd = value; }
    [Delphi(PortStatus.Ported)] public TBoldMemberIdList? MemberIdList { get => fMemberIds; set => fMemberIds = value; }
}

/// <summary>Streams the paging fields shared by all conditions.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXMLConditionStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var Condition = (TBoldCondition)Obj;
        Node.WriteSubNodeInteger(BoldCondition.BoldNodeName_MaxAnswers, Condition.MaxAnswers);
        Node.WriteSubNodeInteger(BoldCondition.BoldNodeName_Offset, Condition.Offset);
        Node.WriteSubNodeInteger(BoldCondition.BoldNodeName_AvailableAnswers, Condition.AvailableAnswers);
    }

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var Condition = (TBoldCondition)Obj;
        Condition.MaxAnswers = Node.ReadSubNodeInteger(BoldCondition.BoldNodeName_MaxAnswers);
        Condition.Offset = Node.ReadSubNodeInteger(BoldCondition.BoldNodeName_Offset);
        Condition.AvailableAnswers = Node.ReadSubNodeInteger(BoldCondition.BoldNodeName_AvailableAnswers);
    }
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLConditionWithClassStreamer : TBoldXMLConditionStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldCondition.ClassConditionName;

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var Condition = (TBoldConditionWithClass)Obj;
        Node.WriteSubNodeInteger(BoldCondition.BoldNodeName_TopSortedIndex, Condition.TopSortedIndex);
        Node.WriteSubNodeInteger(BoldCondition.BoldNodeName_Time, Condition.Time);
    }

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var Condition = (TBoldConditionWithClass)Obj;
        Condition.TopSortedIndex = Node.ReadSubNodeInteger(BoldCondition.BoldNodeName_TopSortedIndex);
        Condition.Time = Node.ReadSubNodeInteger(BoldCondition.BoldNodeName_Time);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldConditionWithClass();
}

/// <summary>Streams an SQL condition with its parameters (name, field type, direction and value).</summary>
[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLSQLConditionStreamer : TBoldXMLConditionWithClassStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldCondition.SQLConditionName;

    /// <summary>Writes the fragments and each parameter; a parameter's value is written as invariant text.</summary>
    [Delphi(PortStatus.Partial, Note = "A parameter value is written as invariant text where Delphi writes TParam's binary buffer.")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var anSQLCond = (TBoldSQLCondition)Obj;
        Node.WriteSubNodeString(BoldCondition.BoldNodeName_WhereFragment, anSQLCond.WhereFragment);
        Node.WriteSubNodeString(BoldCondition.BoldNodeName_OrderBy, anSQLCond.OrderBy);
        Node.WriteSubNodeBoolean(BoldCondition.BoldNodeName_JoinInheritedTables, anSQLCond.JoinInheritedTables);
        TBoldXMLNode ParamsNode = Node.NewSubNode(BoldCondition.BoldNodeName_Params);
        if (anSQLCond.Params is not null)
        {
            foreach (TParam p in anSQLCond.Params)
            {
                TBoldXMLNode SubNode = ParamsNode.NewSubNode(BoldCondition.BoldNodeName_Param);
                SubNode.WriteSubNodeString(BoldCondition.BoldNodeName_Name, p.Name);
                SubNode.WriteSubNodeInteger(BoldCondition.BoldNodeName_DataType, (int)p.DataType);
                SubNode.WriteSubNodeInteger(BoldCondition.BoldNodeName_ParamType, (int)p.ParamType);
                if (!p.IsNull)
                {
                    SubNode.WriteSubNodeData(BoldCondition.BoldNodeName_Data, BoldCondition.ParamValueToText(p));
                }
            }
        }
        else
        {
            ParamsNode.SetToNull();
        }
    }

    [Delphi(PortStatus.Partial, Note = "Reads a parameter value written as invariant text, as WriteObject writes it.")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var anSQLCond = (TBoldSQLCondition)Obj;
        anSQLCond.WhereFragment = Node.ReadSubNodeString(BoldCondition.BoldNodeName_WhereFragment);
        anSQLCond.OrderBy = Node.ReadSubNodeString(BoldCondition.BoldNodeName_OrderBy);
        anSQLCond.JoinInheritedTables = Node.ReadSubNodeBoolean(BoldCondition.BoldNodeName_JoinInheritedTables);
        TBoldXMLNode? ParamsNode = Node.GetSubNode(BoldCondition.BoldNodeName_Params);
        if (ParamsNode is not null && !ParamsNode.IsNull)
        {
            anSQLCond.Params = new TParams();
            foreach (XmlElement element in Node.XMLDomElement.GetElementsByTagName(BoldCondition.BoldNodeName_Param))
            {
                TBoldXMLNode aSubNode = Node.MakeNodeForElement(element);
                TParam aParam = anSQLCond.Params.Add();
                aParam.Name = aSubNode.ReadSubNodeString(BoldCondition.BoldNodeName_Name);
                aParam.DataType = (TFieldType)aSubNode.ReadSubNodeInteger(BoldCondition.BoldNodeName_DataType);
                aParam.ParamType = (TParamType)aSubNode.ReadSubNodeInteger(BoldCondition.BoldNodeName_ParamType);
                TBoldXMLNode? DataNode = aSubNode.GetSubNode(BoldCondition.BoldNodeName_Data);
                if (DataNode is not null)
                {
                    BoldCondition.ParamValueFromText(aParam, DataNode.ReadData());
                }
            }
        }
        else
        {
            anSQLCond.Params = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldSQLCondition();
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLTimestampConditionStreamer : TBoldXMLConditionWithClassStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldCondition.TimestampConditionName;

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeInteger(BoldCondition.BoldNodeName_Timestamp, ((TBoldTimestampCondition)Obj).Timestamp);
    }

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldTimestampCondition)Obj).Timestamp = Node.ReadSubNodeInteger(BoldCondition.BoldNodeName_Timestamp);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldTimestampCondition();
}

[Delphi(PortStatus.Ported)]
internal sealed class TBoldXMLChangePointConditionStreamer : TBoldXMLConditionStreamer
{
    [Delphi(PortStatus.Ported)]
    protected override string GetStreamName() => BoldCondition.ChangePointConditionName;

    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        var aCond = (TBoldChangePointCondition)Obj;
        Node.WriteSubNodeObject(BoldCondition.BoldNodeName_IdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, aCond.IdList);
        Node.WriteSubNodeInteger(BoldCondition.BoldNodeName_StartTime, aCond.StartTime);
        Node.WriteSubNodeInteger(BoldCondition.BoldNodeName_EndTime, aCond.EndTime);
        Node.WriteSubNodeObject(BoldCondition.BoldNodeName_MemberIds, BoldDefaultStreamNames.BOLDMEMBERIDLISTNAME, aCond.MemberIdList);
    }

    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var aCond = (TBoldChangePointCondition)Obj;
        aCond.IdList = (TBoldObjectIdList?)Node.ReadSubNodeObject(BoldCondition.BoldNodeName_IdList, BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME);
        aCond.StartTime = Node.ReadSubNodeInteger(BoldCondition.BoldNodeName_StartTime);
        aCond.EndTime = Node.ReadSubNodeInteger(BoldCondition.BoldNodeName_EndTime);
        aCond.MemberIdList = (TBoldMemberIdList?)Node.ReadSubNodeObject(BoldCondition.BoldNodeName_MemberIds, BoldDefaultStreamNames.BOLDMEMBERIDLISTNAME);
    }

    [Delphi(PortStatus.Ported)]
    public override object CreateObject() => new TBoldChangePointCondition();
}

/// <summary>Stream names, node names and streamer registration of BoldCondition.</summary>
public static partial class BoldCondition
{
    internal const string BoldNodeName_MaxAnswers = "MaxAnswers";
    internal const string BoldNodeName_Offset = "Offset";
    internal const string BoldNodeName_AvailableAnswers = "AvailableAnswers";
    internal const string BoldNodeName_TopSortedIndex = "TopSortedIndex";
    internal const string BoldNodeName_Time = "Time";
    internal const string BoldNodeName_WhereFragment = "WhereFragment";
    internal const string BoldNodeName_OrderBy = "OrderBy";
    internal const string BoldNodeName_JoinInheritedTables = "JoinInheritedTables";
    internal const string BoldNodeName_Param = "Param";
    internal const string BoldNodeName_Params = "Params";
    internal const string BoldNodeName_Name = "Name";
    internal const string BoldNodeName_DataType = "DataType";
    internal const string BoldNodeName_ParamType = "ParamType";
    internal const string BoldNodeName_Data = "Data";
    internal const string BoldNodeName_Timestamp = "Timestamp";
    internal const string BoldNodeName_IdList = "IdList";
    internal const string BoldNodeName_StartTime = "StartTime";
    internal const string BoldNodeName_EndTime = "EndTime";
    internal const string BoldNodeName_MemberIds = "MemberIds";

    internal const string SQLConditionName = "SQLCondition";
    internal const string ClassConditionName = "ClassCondition";
    internal const string TimestampConditionName = "TimestampCondition";
    internal const string OclConditionName = "OclCondition";
    internal const string ChangePointConditionName = "ChangePointCondition";

    /// <summary>A parameter value as invariant text (dates in ISO 8601 round-trip form).</summary>
    internal static string ParamValueToText(TParam p) => p.Value switch
    {
        DateTime d => d.ToString("o", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        var v => v?.ToString() ?? "",
    };

    /// <summary>Sets the parameter's value from text, converted to the parameter's field type.</summary>
    internal static void ParamValueFromText(TParam p, string text)
    {
        p.Value = p.DataType switch
        {
            TFieldType.ftSmallint or TFieldType.ftInteger or TFieldType.ftWord or TFieldType.ftAutoInc =>
                int.Parse(text, CultureInfo.InvariantCulture),
            TFieldType.ftLargeint => long.Parse(text, CultureInfo.InvariantCulture),
            TFieldType.ftFloat => double.Parse(text, CultureInfo.InvariantCulture),
            TFieldType.ftCurrency or TFieldType.ftBCD or TFieldType.ftFMTBcd => decimal.Parse(text, CultureInfo.InvariantCulture),
            TFieldType.ftBoolean => bool.Parse(text),
            TFieldType.ftDate or TFieldType.ftTime or TFieldType.ftDateTime or TFieldType.ftTimeStamp =>
                DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            _ => text,
        };
    }

    internal static void RegisterStreamers()
    {
        TBoldXMLStreamerRegistry main = TBoldXMLStreamerRegistry.MainStreamerRegistry();
        main.RegisterStreamer(new TBoldXMLConditionWithClassStreamer());
        main.RegisterStreamer(new TBoldXMLSQLConditionStreamer());
        main.RegisterStreamer(new TBoldXMLTimestampConditionStreamer());
        main.RegisterStreamer(new TBoldXMLChangePointConditionStreamer());
    }
}
