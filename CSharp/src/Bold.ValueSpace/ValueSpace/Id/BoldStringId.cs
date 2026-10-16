// Delphi: Source/ValueSpace/Id/BoldStringId.pas
// Port map: CSharp/port/units/BoldStringId.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldStringId", Symbol = "initialization", Note = "Registers the streamer from the Bold.ValueSpace module initializer.")]

namespace Bold;

/// <summary>The id of a stored object whose key is a string.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldStringID : TBoldExternalObjectID
{
    protected internal string fIdValue = "";

    /// <summary>An empty id with class index 0, as Delphi's plain Create.</summary>
    public TBoldStringID()
    {
    }

    public TBoldStringID(int TopSortedIndex, bool Exact)
        : base(TopSortedIndex, Exact)
    {
    }

    [Delphi(PortStatus.Ported)] protected void SetAsString(string NewValue) => fIdValue = NewValue;
    [Delphi(PortStatus.Ported)] protected override string GetAsString() => fIdValue;
    [Delphi(PortStatus.Ported)] protected override uint GetHash() => BoldStringId.HashString(fIdValue);
    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldDefaultStreamNames.BOLDSTRINGIDNAME;

    /// <summary>A copy of the same type with the class index replaced.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectId CloneWithClassId(int TopSortedIndex, bool Exact)
    {
        var result = (TBoldStringID)Activator.CreateInstance(GetType(), TopSortedIndex, Exact)!;
        result.fIdValue = fIdValue;
        return result;
    }

    /// <summary>Equal to a string id (exactly this class) with the same value.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool GetIsEqual(TBoldObjectId? MatchID) =>
        MatchID is not null && MatchID.GetType() == typeof(TBoldStringID) && fIdValue == ((TBoldStringID)MatchID).fIdValue;

    [Delphi(PortStatus.Ported)]
    public new string AsString
    {
        get => GetAsString();
        set => SetAsString(value);
    }
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLStringIdStreamer")]
internal sealed class TBoldXMLStringIdStreamer : TBoldXMLObjectIdStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLStringIdStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldDefaultStreamNames.BOLDSTRINGIDNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLStringIdStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeString(BoldStringId.BoldNodeName_IdValue, ((TBoldStringID)Obj).fIdValue);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLStringIdStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldStringID)Obj).fIdValue = Node.ReadSubNodeString(BoldStringId.BoldNodeName_IdValue);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLStringIdStreamer.CreateObject")]
    public override object CreateObject() => new TBoldStringID();
}

/// <summary>The hash of string ids, the node name and the streamer registration.</summary>
public static partial class BoldStringId
{
    internal const string BoldNodeName_IdValue = "IdValue";

    /// <summary>Rotates the hash left by 3 (within 31 bits) and xors in each character.</summary>
    [Delphi(PortStatus.Ported)]
    internal static uint HashString(string S)
    {
        uint result = 0;
        foreach (char c in S)
        {
            result = (((result << 3) & 2147483647) | (result >> (32 - 3))) ^ c;
        }

        return result;
    }

    internal static void RegisterStreamers() => TBoldXMLStreamerRegistry.MainStreamerRegistry().RegisterStreamer(new TBoldXMLStringIdStreamer());
}
