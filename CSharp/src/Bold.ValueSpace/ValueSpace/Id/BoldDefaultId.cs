// Delphi: Source/ValueSpace/Id/BoldDefaultId.pas
// Port map: CSharp/port/units/BoldDefaultId.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefaultId", Symbol = "TBoldObjectIdClass", Note = "The global System.Type alias from BoldId.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefaultId", Symbol = "initialization", Note = "Registers the id streamers from the Bold.ValueSpace module initializer.")]

namespace Bold;

/// <summary>The id of a stored object: its integer key in the database.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultID : TBoldExternalObjectID
{
    protected internal int fDBValue;

    /// <summary>An id with value 0 and class index 0, as Delphi's plain Create.</summary>
    public TBoldDefaultID()
    {
    }

    public TBoldDefaultID(int TopSortedIndex, bool Exact)
        : base(TopSortedIndex, Exact)
    {
    }

    [Delphi(PortStatus.Ported)] protected void SetAsInteger(int NewValue) => fDBValue = NewValue;
    [Delphi(PortStatus.Ported)] protected int GetAsInteger() => fDBValue;
    [Delphi(PortStatus.Ported)] protected override string GetAsString() => SysUtils.IntToStr(fDBValue);

    /// <summary>The value plus the timestamp, so versions of an object hash apart.</summary>
    [Delphi(PortStatus.Ported)]
    protected override uint GetHash() => unchecked((uint)fDBValue + (uint)TimeStamp);

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldDefaultStreamNames.BOLDDEFAULTIDNAME;

    /// <summary>A copy of the same type with the class index replaced.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectId CloneWithClassId(int TopSortedIndex, bool Exact)
    {
        var result = (TBoldDefaultID)Activator.CreateInstance(GetType(), TopSortedIndex, Exact)!;
        result.fDBValue = fDBValue;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldDefaultID CloneWithTimeStamp(TBoldTimeStampType Time) => CloneWithClassIdAndTimeStamp(TopSortedIndex, TopSortedIndexExact, Time);

    /// <summary>A copy at a point in time: a plain default id for BOLDMAXTIMESTAMP, else a timestamped one.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldDefaultID CloneWithClassIdAndTimeStamp(int TopSortedIndex, bool Exact, TBoldTimeStampType Time)
    {
        TBoldDefaultID result = Time == BoldDefs.BOLDMAXTIMESTAMP
            ? new TBoldDefaultID(TopSortedIndex, Exact)
            : new TBoldTimestampedDefaultId(Time, TopSortedIndex, Exact);
        result.fDBValue = fDBValue;
        return result;
    }

    /// <summary>Equal to a default or timestamped id with the same value and timestamp.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool GetIsEqual(TBoldObjectId? MatchID) =>
        MatchID is not null && (MatchID.GetType() == typeof(TBoldDefaultID) || MatchID.GetType() == typeof(TBoldTimestampedDefaultId))
        && fDBValue == ((TBoldDefaultID)MatchID).fDBValue && MatchID.TimeStamp == TimeStamp;

    [Delphi(PortStatus.Ported)]
    public int AsInteger
    {
        get => GetAsInteger();
        set => SetAsInteger(value);
    }
}

/// <summary>A default id for an object version at a timestamp.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTimestampedDefaultId : TBoldDefaultID
{
    private TBoldTimeStampType fTimeStamp;

    public TBoldTimestampedDefaultId()
    {
    }

    public TBoldTimestampedDefaultId(int TopSortedIndex, bool Exact)
        : base(TopSortedIndex, Exact)
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldTimestampedDefaultId.createWithTimeAndClassId")]
    public TBoldTimestampedDefaultId(TBoldTimeStampType TimeStamp, int TopSortedIndex, bool Exact)
        : base(TopSortedIndex, Exact)
    {
        fTimeStamp = TimeStamp;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldDefaultStreamNames.BOLDTIMESTAMPEDDEFAULTIDNAME;

    [Delphi(PortStatus.Ported)]
    protected override TBoldTimeStampType GetTimeStamp() => fTimeStamp;

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectId CloneWithClassId(int TopSortedIndex, bool Exact)
    {
        var result = (TBoldTimestampedDefaultId)base.CloneWithClassId(TopSortedIndex, Exact);
        result.fTimeStamp = fTimeStamp;
        return result;
    }

    [Delphi(PortStatus.Ported)]
    public new TBoldTimeStampType TimeStamp
    {
        get => GetTimeStamp();
        set => fTimeStamp = value;
    }
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLDefaultIdStreamer")]
internal class TBoldXMLDefaultIdStreamer : TBoldXMLObjectIdStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLDefaultIdStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldDefaultStreamNames.BOLDDEFAULTIDNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLDefaultIdStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.NewSubNode(BoldDefaultId.BoldNodeName_DbValue).WriteInteger(((TBoldDefaultID)Obj).fDBValue);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLDefaultIdStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldDefaultID)Obj).fDBValue = Node.GetSubNode(BoldDefaultId.BoldNodeName_DbValue)!.ReadInteger();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLDefaultIdStreamer.CreateObject")]
    public override object CreateObject() => new TBoldDefaultID();
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLTimestampedDefaultIdStreamer")]
internal sealed class TBoldXMLTimestampedDefaultIdStreamer : TBoldXMLDefaultIdStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLTimestampedDefaultIdStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldDefaultStreamNames.BOLDTIMESTAMPEDDEFAULTIDNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLTimestampedDefaultIdStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeInteger(BoldDefaultId.BoldNodeName_Timestamp, ((TBoldTimestampedDefaultId)Obj).TimeStamp);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLTimestampedDefaultIdStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldTimestampedDefaultId)Obj).TimeStamp = Node.ReadSubNodeInteger(BoldDefaultId.BoldNodeName_Timestamp);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLTimestampedDefaultIdStreamer.CreateObject")]
    public override object CreateObject() => new TBoldTimestampedDefaultId();
}

/// <summary>Node names and streamer registration of BoldDefaultId.</summary>
public static partial class BoldDefaultId
{
    internal const string BoldNodeName_DbValue = "DbValue";
    internal const string BoldNodeName_Timestamp = "Timestamp";

    internal static void RegisterStreamers()
    {
        TBoldXMLStreamerRegistry main = TBoldXMLStreamerRegistry.MainStreamerRegistry();
        main.RegisterStreamer(new TBoldXMLDefaultIdStreamer());
        main.RegisterStreamer(new TBoldXMLTimestampedDefaultIdStreamer());
    }
}
