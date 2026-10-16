// Delphi: Source/ValueSpace/Id/BoldGlobalId.pas
// Port map: CSharp/port/units/BoldGlobalId.md

[assembly: Delphi(PortStatus.Ported, Unit = "BoldGlobalId", Symbol = "initialization", Note = "Registers the streamer from the Bold.ValueSpace module initializer.")]

namespace Bold;

/// <summary>A global (GUID) id of an object known outside the database, with its class name; not storable.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldGlobalId : TBoldObjectId
{
    private string fId = "";
    private string fClassExpressionName = "";

    /// <summary>An empty id with class index 0, as Delphi's plain Create.</summary>
    public TBoldGlobalId()
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldGlobalId.CreateWithInfo")]
    public TBoldGlobalId(string GlobalId, int TopSortedIndex, bool Exact, string ClassExpressionName)
        : base(TopSortedIndex, Exact)
    {
        fId = GlobalId;
        fClassExpressionName = ClassExpressionName;
    }

    [Delphi(PortStatus.Ported)] protected override string GetAsString() => fId;
    [Delphi(PortStatus.Ported)] protected override uint GetHash() => TBoldStringKey.HashString(fId, TBoldStringCompareMode.bscCaseDependent);
    [Delphi(PortStatus.Ported)] protected override bool GetIsStorable() => false;
    [Delphi(PortStatus.Ported)] public override string GetStreamName() => BoldGlobalId.BOLDGLOBALIDNAME;

    [Delphi(PortStatus.Ported)]
    public override TBoldObjectId CloneWithClassId(int TopSortedIndex, bool Exact) => new TBoldGlobalId(fId, TopSortedIndex, Exact, ClassExpressionName);

    [Delphi(PortStatus.Ported)]
    public override bool GetIsEqual(TBoldObjectId? MatchID) => MatchID is not null && MatchID.GetType() == GetType() && ((TBoldGlobalId)MatchID).fId == fId;

    [Delphi(PortStatus.Ported)] public string ClassExpressionName => fClassExpressionName;
    [Delphi(PortStatus.Ported)] public new string AsString => GetAsString();

    internal void SetFromStream(string Id, string ClassExpressionName)
    {
        fId = Id;
        fClassExpressionName = ClassExpressionName;
    }
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLGlobalIdStreamer")]
internal sealed class TBoldXMLGlobalIdStreamer : TBoldXMLObjectIdStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLGlobalIdStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldGlobalId.BOLDGLOBALIDNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLGlobalIdStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.WriteSubNodeString(BoldGlobalId.BoldNodeName_Id, ((TBoldGlobalId)Obj).AsString);
    }

    /// <summary>Reads the id; the class name comes from the stream manager's model.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLGlobalIdStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        var GlobalId = (TBoldGlobalId)Obj;
        GlobalId.SetFromStream(Node.ReadSubNodeString(BoldGlobalId.BoldNodeName_Id),
            ((TBoldDefaultXMLStreamManager)Node.Manager).Model.Classes[GlobalId.TopSortedIndex].Name);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLGlobalIdStreamer.CreateObject")]
    public override object CreateObject() => new TBoldGlobalId();
}

/// <summary>The stream name, node name and streamer registration of global ids.</summary>
public static partial class BoldGlobalId
{
    [Delphi(PortStatus.Ported)] public const string BOLDGLOBALIDNAME = "BoldGlobalId";
    internal const string BoldNodeName_Id = "Id";

    internal static void RegisterStreamers() => TBoldXMLStreamerRegistry.MainStreamerRegistry().RegisterStreamer(new TBoldXMLGlobalIdStreamer());
}
