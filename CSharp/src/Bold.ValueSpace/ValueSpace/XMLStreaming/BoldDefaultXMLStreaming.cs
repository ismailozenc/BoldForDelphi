// Delphi: Source/ValueSpace/XMLStreaming/BoldDefaultXMLStreaming.pas
// Port map: CSharp/port/units/BoldDefaultXMLStreaming.md

using System.Xml;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldDefaultXMLStreaming", Symbol = "initialization", Note = "Registers the value streamers from the Bold.ValueSpace module initializer.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDefaultXMLStreaming", Symbol = "finalization", Note = "Freed the main registry; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>The registry of the default streaming; it falls back to the main registry.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultXMLStreamerRegistry : TBoldXMLStreamerRegistry
{
    private static TBoldDefaultXMLStreamerRegistry? G_MainRegistry;

    public TBoldDefaultXMLStreamerRegistry(TBoldXMLStreamerRegistry? Parent)
        : base(Parent)
    {
    }

    [Delphi(PortStatus.Ported)]
    public static new TBoldDefaultXMLStreamerRegistry MainStreamerRegistry() =>
        G_MainRegistry ??= new TBoldDefaultXMLStreamerRegistry(TBoldXMLStreamerRegistry.MainStreamerRegistry());
}

/// <summary>A streamer for a model element, found by its expression name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXMLModelElementStreamer
{
    internal string fExpressionName = "";

    [Delphi(PortStatus.Ported)] public string ExpressionName => fExpressionName;
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLModelElementStreamerList : TBoldIndexableList
{
    private static int IX_ExpressionNameIndex = -1;

    [Delphi(PortStatus.Ported, Symbol = "TBoldExpressionNameIndex")]
    private sealed class TBoldExpressionNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldExpressionNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldXMLModelElementStreamer)Item!).ExpressionName;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldXMLModelElementStreamerList()
    {
        BoldIndexableList.SetIndexVariable(ref IX_ExpressionNameIndex, AddIndex(new TBoldExpressionNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldXMLModelElementStreamer? GetStreamerByName(string Name) =>
        (TBoldXMLModelElementStreamer?)((TBoldStringHashIndex)Indexes(IX_ExpressionNameIndex)!).FindByString(Name);

    [Delphi(PortStatus.Ported)]
    public TBoldXMLModelElementStreamer? StreamerByName(string Name) => GetStreamerByName(Name);

    internal object? Items(int index) => this[index];
}

/// <summary>Streams one member of a class: persistent members whose value is in the states to stream.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXMLMemberStreamer : TBoldXMLModelElementStreamer
{
    private readonly string fTypeStreamName;
    private readonly int fIndex;
    private readonly bool fPersistent;
    private readonly TBoldXMLClassStreamer fOwner;
    private readonly TBoldMemberID fMemberId;

    [Delphi(PortStatus.Ported)]
    public TBoldXMLMemberStreamer(TMoldMember MoldMember, TBoldXMLClassStreamer Owner)
    {
        fOwner = Owner;
        fExpressionName = MoldMember.ExpandedExpressionName;
        fTypeStreamName = MoldMember.TypeStreamName;
        fPersistent = MoldMember.EffectivePersistent;
        fIndex = MoldMember.MoldClass!.AllBoldMembers.IndexOf(MoldMember);
        fMemberId = new TBoldMemberID(fIndex);
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldXMLMemberStreamer.Destroy", Note = "The garbage collector frees the member id.")]
    [Delphi(PortStatus.Ported)]
    public TBoldMemberID MemberId => fMemberId;

    /// <summary>Writes the value as a sub-element named by the member, when the member is persistent and the value is in a state to stream.</summary>
    [Delphi(PortStatus.Ported)]
    public void WriteValue(TBoldXMLNode Node, IBoldValue? Value)
    {
        if (Value is not null && Persistent && fOwner.fOwner.PersistenceStatesToBeStreamed.Contains(Value.BoldPersistenceState))
        {
            Node.NewSubNode(ExpressionName).WriteInterface(TypeStreamName, (IBoldStreamable)Value);
        }
    }

    /// <summary>Reads the value when its state is one to overwrite.</summary>
    [Delphi(PortStatus.Ported)]
    public void ReadValue(TBoldXMLNode Node, IBoldValue? Value)
    {
        if (Value is not null && fOwner.fOwner.PersistenceStatesToOverwrite.Contains(Value.BoldPersistenceState))
        {
            Node.ReadInterface(TypeStreamName, (IBoldStreamable)Value);
        }
    }

    [Delphi(PortStatus.Ported)] public string TypeStreamName => fTypeStreamName;
    [Delphi(PortStatus.Ported)] public bool Persistent => fPersistent;
    [Delphi(PortStatus.Ported)] public int Index => fIndex;
}

/// <summary>Streams the objects of one class: id, states, timestamp, global id and members.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXMLClassStreamer : TBoldXMLModelElementStreamer
{
    private readonly TBoldXMLModelElementStreamerList fMemberStreamers;
    internal readonly TBoldDefaultXMLStreamManager fOwner;

    [Delphi(PortStatus.Ported)]
    public TBoldXMLClassStreamer(TMoldClass MoldClass, TBoldDefaultXMLStreamManager Owner)
    {
        fOwner = Owner;
        fExpressionName = MoldClass.ExpandedExpressionName;
        fMemberStreamers = new TBoldXMLModelElementStreamerList();
        for (int i = 0; i < MoldClass.AllBoldMembers.Count; i++)
        {
            fMemberStreamers.Add(new TBoldXMLMemberStreamer(MoldClass.AllBoldMembers[i], this));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLClassStreamer.Destroy", Note = "The garbage collector frees the member streamers.")]
    [Delphi(PortStatus.Ported)]
    private TBoldXMLMemberStreamer? GetMemberStreamer(int Index) =>
        Index < fMemberStreamers.Count ? (TBoldXMLMemberStreamer?)fMemberStreamers.Items(Index) : null;

    [Delphi(PortStatus.Ported)]
    private TBoldXMLMemberStreamer GetMemberStreamerByName(string Name) =>
        (TBoldXMLMemberStreamer?)fMemberStreamers.StreamerByName(Name)
        ?? throw new EBold(SysUtils.Format(ValueSpaceConst.sUnrecognizedClassName, GetType().Name, "GetMemberStreamerByName", Name));

    [Delphi(PortStatus.Ported)]
    public void WriteObject(TBoldXMLNode Node, IBoldObjectContents? ObjectContents, TBoldObjectId ObjectId, TBoldMemberIdList? MemberIdList)
    {
        if (ObjectContents is null)
        {
            return;
        }

        TBoldXMLNode objNode = Node.NewSubNode(ExpressionName);
        objNode.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_id).WriteObject("", ObjectId);
        objNode.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_persistencestate).WriteInteger((int)ObjectContents.BoldPersistenceState);
        objNode.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_existencestate).WriteInteger((int)ObjectContents.BoldExistenceState);
        objNode.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_timestamp).WriteInteger(ObjectContents.TimeStamp);
        if (ObjectContents.GlobalId != "")
        {
            objNode.WriteSubNodeString(BoldDefaultXMLStreaming.BoldNodeName_globalid, ObjectContents.GlobalId);
        }

        TBoldXMLNode membersNode = objNode.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_members);
        if (MemberIdList is not null)
        {
            for (int i = 0; i < MemberIdList.Count; i++)
            {
                MemberStreamers(MemberIdList[i].MemberIndex)?.WriteValue(membersNode, ObjectContents.ValueByMemberId(MemberIdList[i]));
            }
        }
        else
        {
            for (int i = 0; i < ObjectContents.MemberCount; i++)
            {
                MemberStreamers(i)?.WriteValue(membersNode, ObjectContents.ValueByIndex(i));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ReadObject(TBoldXMLNode Node, IBoldValueSpace ValueSpace)
    {
        var anId = (TBoldObjectId)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_id)!.ReadObject("")!;
        IBoldObjectContents objectContents = ValueSpace.EnsuredObjectContentsByObjectId(anId);
        if (!fOwner.IgnorePersistenceState)
        {
            objectContents.BoldPersistenceState = (TBoldValuePersistenceState)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_persistencestate)!.ReadInteger();
        }

        objectContents.BoldExistenceState = (TBoldExistenceState)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_existencestate)!.ReadInteger();
        TBoldXMLNode? subNode = Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_timestamp);
        if (subNode is not null)
        {
            objectContents.TimeStamp = subNode.ReadInteger();
        }

        subNode = Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_globalid);
        if (subNode is not null)
        {
            objectContents.GlobalId = subNode.ReadString();
        }

        TBoldXMLNode membersNode = Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_members)!;
        foreach (XmlNode child in membersNode.XMLDomElement.ChildNodes)
        {
            if (child is not XmlElement element)
            {
                continue;
            }

            var memberNode = new TBoldXMLNode(Node.Manager, element, null);
            TBoldXMLMemberStreamer memberStreamer = MemberStreamerByName(memberNode.Accessor);
            objectContents.EnsureMember(memberStreamer.MemberId, memberStreamer.TypeStreamName);
            memberStreamer.ReadValue(memberNode, objectContents.ValueByIndex(memberStreamer.Index));
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldXMLMemberStreamer? MemberStreamers(int Index) => GetMemberStreamer(Index);
    [Delphi(PortStatus.Ported)] public TBoldXMLMemberStreamer MemberStreamerByName(string Name) => GetMemberStreamerByName(Name);
}

/// <summary>
/// Streams a value space for a model: one element per object, named by its class's expression name. By default it writes
/// modified values, overwrites invalid ones when reading, and ignores persistence states in the stream.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultXMLStreamManager : TBoldXMLStreamManager
{
    private readonly TMoldModel fModel;
    private readonly TBoldXMLModelElementStreamerList fClassStreamers;
    private bool fIgnorePersistenceState;
    private TBoldValuePersistenceStateSet fPersistenceStatesToBeStreamed;
    private TBoldValuePersistenceStateSet fPersistenceStatesToOverwrite;

    [Delphi(PortStatus.Ported)]
    public TBoldDefaultXMLStreamManager(TBoldXMLStreamerRegistry Registry, TMoldModel Model)
        : base(Registry)
    {
        fModel = Model;
        fClassStreamers = new TBoldXMLModelElementStreamerList();
        fIgnorePersistenceState = true;
        fPersistenceStatesToBeStreamed = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsModified);
        fPersistenceStatesToOverwrite = TBoldValuePersistenceStateSet.Of(TBoldValuePersistenceState.bvpsInvalid);
        Model.EnsureTopSorted();
        for (int i = 0; i < Model.Classes.Count; i++)
        {
            fClassStreamers.Add(new TBoldXMLClassStreamer(Model.Classes[i], this));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldDefaultXMLStreamManager.Destroy", Note = "The garbage collector frees the class streamers.")]
    [Delphi(PortStatus.Ported)]
    private TBoldXMLClassStreamer GetClassStreamer(int TopSortedIndex) =>
        TopSortedIndex < fClassStreamers.Count
            ? (TBoldXMLClassStreamer)fClassStreamers.Items(TopSortedIndex)!
            : throw new EBold(SysUtils.Format(ValueSpaceConst.sInvalidIndex, GetType().Name));

    [Delphi(PortStatus.Ported)]
    private TBoldXMLClassStreamer GetClassStreamerByName(string Name) =>
        (TBoldXMLClassStreamer?)fClassStreamers.StreamerByName(Name)
        ?? throw new EBold(SysUtils.Format(ValueSpaceConst.sUnrecognizedClassName, GetType().Name, "GetClassStreamerByName", Name));

    [Delphi(PortStatus.Ported)]
    public void WriteValueSpace(IBoldValueSpace? ValueSpace, TBoldObjectIdList IdList, TBoldMemberIdList? MemberIdList, TBoldXMLNode Node)
    {
        if (ValueSpace is null)
        {
            return;
        }

        for (int i = 0; i < IdList.Count; i++)
        {
            TBoldObjectId anId = IdList[i];
            ClassStreamers(anId.TopSortedIndex).WriteObject(Node, ValueSpace.ObjectContentsByObjectId(anId), anId, MemberIdList);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ReadValueSpace(IBoldValueSpace? ValueSpace, TBoldXMLNode Node)
    {
        if (ValueSpace is null)
        {
            return;
        }

        foreach (XmlNode child in Node.XMLDomElement.ChildNodes)
        {
            if (child is XmlElement element)
            {
                var subNode = new TBoldXMLNode(Node.Manager, element, null);
                ClassStreamerByName(subNode.Accessor).ReadObject(subNode, ValueSpace);
            }
        }
    }

    [Delphi(PortStatus.Ported)] public TMoldModel Model => fModel;
    [Delphi(PortStatus.Ported)] public TBoldXMLClassStreamer ClassStreamers(int TopSortedIndex) => GetClassStreamer(TopSortedIndex);
    [Delphi(PortStatus.Ported)] public TBoldXMLClassStreamer ClassStreamerByName(string Name) => GetClassStreamerByName(Name);

    [Delphi(PortStatus.Ported)]
    public bool IgnorePersistenceState { get => fIgnorePersistenceState; set => fIgnorePersistenceState = value; }

    [Delphi(PortStatus.Ported)]
    public TBoldValuePersistenceStateSet PersistenceStatesToOverwrite { get => fPersistenceStatesToOverwrite; set => fPersistenceStatesToOverwrite = value; }

    [Delphi(PortStatus.Ported)]
    public TBoldValuePersistenceStateSet PersistenceStatesToBeStreamed { get => fPersistenceStatesToBeStreamed; set => fPersistenceStatesToBeStreamed = value; }
}

/// <summary>Streams a value with its persistence state (read only when the manager does not ignore states).</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXMLValueStreamer : TBoldXMLInterfaceStreamer
{
    [Delphi(PortStatus.Ported)]
    public override void WriteInterface(IBoldStreamable Item, TBoldXMLNode Node)
    {
        base.WriteInterface(Item, Node);
        Node.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_persistencestate).WriteInteger((int)((IBoldValue)Item).BoldPersistenceState);
    }

    [Delphi(PortStatus.Ported)]
    public override void ReadInterface(IBoldStreamable Item, TBoldXMLNode Node)
    {
        base.ReadInterface(Item, Node);
        if (!((TBoldDefaultXMLStreamManager)Node.Manager).IgnorePersistenceState)
        {
            ((IBoldValue)Item).BoldPersistenceState = (TBoldValuePersistenceState)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_persistencestate)!.ReadInteger();
        }
    }
}

/// <summary>Streams a nullable value: its content element is marked null or holds the content.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXMLNullableValueStreamer : TBoldXMLValueStreamer
{
    [Delphi(PortStatus.Ported)]
    protected virtual void WriteContent(IBoldValue Item, TBoldXMLNode Node)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void ReadContent(IBoldValue Item, TBoldXMLNode Node)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteInterface(IBoldStreamable Item, TBoldXMLNode Node)
    {
        base.WriteInterface(Item, Node);
        TBoldXMLNode contentNode = Node.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_content);
        if (!((IBoldNullableValue)Item).IsNull)
        {
            WriteContent((IBoldValue)Item, contentNode);
        }
        else
        {
            contentNode.SetToNull();
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void ReadInterface(IBoldStreamable Item, TBoldXMLNode Node)
    {
        base.ReadInterface(Item, Node);
        TBoldXMLNode contentNode = Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_content)!;
        if (contentNode.IsNull)
        {
            ((IBoldNullableValue)Item).SetContentToNull();
        }
        else
        {
            ReadContent((IBoldValue)Item, contentNode);
        }
    }
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLStringContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_String;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldStringContent)Item).asString = Node.ReadString();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteString(((IBoldStringContent)Item).asString);
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLIntegerContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Integer;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldIntegerContent)Item).asInteger = Node.ReadInteger();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteInteger(((IBoldIntegerContent)Item).asInteger);
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLFloatContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Float;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldFloatContent)Item).asFloat = Node.ReadFloat();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteFloat(((IBoldFloatContent)Item).asFloat);
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLCurrencyContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Currency;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldCurrencyContent)Item).asCurrency = Node.ReadCurrency();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteCurrency(((IBoldCurrencyContent)Item).asCurrency);
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLBooleanContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Boolean;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldBooleanContent)Item).asBoolean = Node.ReadBoolean();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteBoolean(((IBoldBooleanContent)Item).asBoolean);
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLDateContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Date;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldDateContent)Item).asDate = Node.ReadDate();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteDate(((IBoldDateContent)Item).asDate);
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLTimeContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Time;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldTimeContent)Item).asTime = Node.ReadTime();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteTime(((IBoldTimeContent)Item).asTime);
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLDateTimeContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_DateTime;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldDateTimeContent)Item).asDateTime = Node.ReadDateTime();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteDateTime(((IBoldDateTimeContent)Item).asDateTime);
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLBlobContentStreamer : TBoldXMLNullableValueStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_Blob;
    [Delphi(PortStatus.Ported)] protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node) => ((IBoldBlobContent)Item).asBlob = Node.ReadData();
    [Delphi(PortStatus.Ported)] protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node) => Node.WriteData(((IBoldBlobContent)Item).asBlob);
}

/// <summary>A blob with a ContentType sub-element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXMLTypedBlobStreamer : TBoldXMLBlobContentStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_TypedBlob;

    [Delphi(PortStatus.Ported)]
    protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node)
    {
        base.ReadContent(Item, Node);
        ((IBoldTypedBlob)Item).ContentTypeContent = Node.ReadSubNodeString(BoldDefaultXMLStreaming.BoldNodeName_ContentType);
    }

    [Delphi(PortStatus.Ported)]
    protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node)
    {
        base.WriteContent(Item, Node);
        Node.WriteSubNodeString(BoldDefaultXMLStreaming.BoldNodeName_ContentType, ((IBoldTypedBlob)Item).ContentTypeContent);
    }
}

/// <summary>Streams a role value: its content element holds the ids.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXMLRoleStreamer : TBoldXMLValueStreamer
{
    [Delphi(PortStatus.Ported)]
    protected virtual void WriteContent(IBoldValue Item, TBoldXMLNode Node)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void ReadContent(IBoldValue Item, TBoldXMLNode Node)
    {
    }

    [Delphi(PortStatus.Ported)]
    public override void WriteInterface(IBoldStreamable Item, TBoldXMLNode Node)
    {
        base.WriteInterface(Item, Node);
        WriteContent((IBoldValue)Item, Node.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_content));
    }

    [Delphi(PortStatus.Ported)]
    public override void ReadInterface(IBoldStreamable Item, TBoldXMLNode Node)
    {
        base.ReadInterface(Item, Node);
        ReadContent((IBoldValue)Item, Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_content)!);
    }
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLIdRefStreamer : TBoldXMLRoleStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdRef;

    [Delphi(PortStatus.Ported)]
    protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node)
    {
        var idRef = (IBoldObjectIdRef)Item;
        idRef.SetFromId((TBoldObjectId?)Node.ReadSubNodeObject(BoldDefaultXMLStreaming.BoldNodeName_id, ""));
        TBoldXMLNode? subNode = Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_OrderNo);
        if (subNode is not null)
        {
            idRef.OrderNo = subNode.ReadInteger();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node)
    {
        var idRef = (IBoldObjectIdRef)Item;
        Node.WriteSubNodeObject(BoldDefaultXMLStreaming.BoldNodeName_id, "", idRef.Id);
        Node.WriteSubNodeInteger(BoldDefaultXMLStreaming.BoldNodeName_OrderNo, idRef.OrderNo);
    }
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLIdRefPairStreamer : TBoldXMLRoleStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair;

    [Delphi(PortStatus.Ported)]
    protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node)
    {
        var id1 = (TBoldObjectId?)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_id1)!.ReadObject("");
        var id2 = (TBoldObjectId?)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_id2)!.ReadObject("");
        ((IBoldObjectIdRefPair)Item).SetFromIds(id1, id2);
    }

    [Delphi(PortStatus.Ported)]
    protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node)
    {
        var pair = (IBoldObjectIdRefPair)Item;
        Node.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_id1).WriteObject("", pair.Id1);
        Node.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_id2).WriteObject("", pair.Id2);
    }
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLIdListRefStreamer : TBoldXMLRoleStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdListRef;

    [Delphi(PortStatus.Ported)]
    protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node)
    {
        var idList = (TBoldObjectIdList?)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_idlist)!.ReadObject(BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME);
        ((IBoldObjectIdListRef)Item).SetFromIdList(idList);
    }

    [Delphi(PortStatus.Ported)]
    protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node)
    {
        var listRef = (IBoldObjectIdListRef)Item;
        var idList = new TBoldObjectIdList();
        for (int i = 0; i < listRef.Count; i++)
        {
            idList.Add(listRef.IdList(i));
        }

        Node.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_idlist).WriteObject(BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, idList);
    }
}

[Delphi(PortStatus.Ported)]
public class TBoldXMLIdListRefPairStreamer : TBoldXMLRoleStreamer
{
    [Delphi(PortStatus.Ported)] protected override string GetStreamName() => BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair;

    [Delphi(PortStatus.Ported)]
    protected override void ReadContent(IBoldValue Item, TBoldXMLNode Node)
    {
        var list1 = (TBoldObjectIdList?)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_idlist1)!.ReadObject(BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME);
        var list2 = (TBoldObjectIdList?)Node.GetSubNode(BoldDefaultXMLStreaming.BoldNodeName_idlist2)!.ReadObject(BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME);
        ((IBoldObjectIdListRefPair)Item).SetFromIdLists(list1, list2);
    }

    [Delphi(PortStatus.Ported)]
    protected override void WriteContent(IBoldValue Item, TBoldXMLNode Node)
    {
        var pair = (IBoldObjectIdListRefPair)Item;
        var idList = new TBoldObjectIdList();
        for (int i = 0; i < pair.Count; i++)
        {
            idList.Add(pair.IdList1(i));
        }

        Node.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_idlist1).WriteObject(BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, idList);
        idList = new TBoldObjectIdList();
        for (int i = 0; i < pair.Count; i++)
        {
            idList.Add(pair.IdList2(i));
        }

        Node.NewSubNode(BoldDefaultXMLStreaming.BoldNodeName_idlist2).WriteObject(BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, idList);
    }
}

/// <summary>Node names and streamer registration of BoldDefaultXMLStreaming.</summary>
public static partial class BoldDefaultXMLStreaming
{
    internal const string BoldNodeName_persistencestate = "persistencestate";
    internal const string BoldNodeName_existencestate = "existencestate";
    internal const string BoldNodeName_timestamp = "timestamp";
    internal const string BoldNodeName_globalid = "globalid";
    internal const string BoldNodeName_members = "members";
    internal const string BoldNodeName_content = "content";
    internal const string BoldNodeName_ContentType = "ContentType";
    internal const string BoldNodeName_id = "id";
    internal const string BoldNodeName_id1 = "id1";
    internal const string BoldNodeName_id2 = "id2";
    internal const string BoldNodeName_idlist = "idlist";
    internal const string BoldNodeName_idlist1 = "idlist1";
    internal const string BoldNodeName_idlist2 = "idlist2";
    internal const string BoldNodeName_OrderNo = "OrderNo";

    internal static void RegisterStreamers()
    {
        TBoldXMLStreamerRegistry main = TBoldXMLStreamerRegistry.MainStreamerRegistry();
        main.RegisterStreamer(new TBoldXMLStringContentStreamer());
        main.RegisterStreamer(new TBoldXMLIntegerContentStreamer());
        main.RegisterStreamer(new TBoldXMLFloatContentStreamer());
        main.RegisterStreamer(new TBoldXMLCurrencyContentStreamer());
        main.RegisterStreamer(new TBoldXMLBooleanContentStreamer());
        main.RegisterStreamer(new TBoldXMLDateContentStreamer());
        main.RegisterStreamer(new TBoldXMLTimeContentStreamer());
        main.RegisterStreamer(new TBoldXMLDateTimeContentStreamer());
        main.RegisterStreamer(new TBoldXMLBlobContentStreamer());
        main.RegisterStreamer(new TBoldXMLTypedBlobStreamer());
        main.RegisterStreamer(new TBoldXMLIdRefStreamer());
        main.RegisterStreamer(new TBoldXMLIdRefPairStreamer());
        main.RegisterStreamer(new TBoldXMLIdListRefStreamer());
        main.RegisterStreamer(new TBoldXMLIdListRefPairStreamer());
    }
}
