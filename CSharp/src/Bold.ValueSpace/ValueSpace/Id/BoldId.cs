// Delphi: Source/ValueSpace/Id/BoldId.pas
// Port map: CSharp/port/units/BoldId.md

using System.Xml;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldId", Symbol = "TBoldObjectIdClass", Note = "Global using alias of System.Type, in BoldId.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldId", Symbol = "initialization", Note = "Registers the id streamers from the Bold.ValueSpace module initializer.")]

namespace Bold;

/// <summary>An id: of an object, a member, or a list of them. Ids are streamable.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldID : TBoldNonRefCountedObject, IBoldStreamable
{
    [Delphi(PortStatus.Ported)]
    public abstract string GetStreamName();
}

/// <summary>The index of a member in its class's member list.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberID : TBoldID
{
    internal int fMemberIndex;

    [Delphi(PortStatus.Ported)]
    public TBoldMemberID(int MemberIndex)
    {
        fMemberIndex = MemberIndex;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldDefaultStreamNames.BOLDMEMBERIDNAME;

    [Delphi(PortStatus.Ported)]
    public int MemberIndex => fMemberIndex;
}

/// <summary>A member id that is part of another member id.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubMemberID : TBoldMemberID, IDisposable
{
    private readonly bool fOwnsPartof;
    private TBoldMemberID? fPartOf;

    [Delphi(PortStatus.Ported)]
    public TBoldSubMemberID(TBoldMemberID partOf, bool OwnspartOf, int IndexInMemberList)
        : base(IndexInMemberList)
    {
        fPartOf = partOf;
        fOwnsPartof = OwnspartOf;
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        if (fOwnsPartof)
        {
            (fPartOf as IDisposable)?.Dispose();
            fPartOf = null;
        }

        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// The id of an object. It carries the topological index of the object's class and whether that class is exact (the
/// object may belong to a subclass when it is not).
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldObjectId : TBoldID
{
    private int FClassId;

    /// <summary>An id with class index 0, not exact; Delphi's plain Create, used when reading ids from a stream.</summary>
    protected TBoldObjectId()
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectId.CreateWithClassID")]
    protected TBoldObjectId(int TopSortedIndex, bool Exact)
    {
        SetClassIdData(TopSortedIndex, Exact);
    }

    [Delphi(PortStatus.Ported)]
    private int getTopSortedIndex() => FClassId & BoldId.TOPSORTEDINDEXTMASK;

    [Delphi(PortStatus.Ported)]
    private bool GetTopSortedIndexExact() => (FClassId & BoldId.CLASSIDEXACTMASK) == BoldId.CLASSIDEXACTMASK;

    [Delphi(PortStatus.Ported)]
    internal void SetClassIdData(int TopSortedIndex, bool Exact) =>
        FClassId = Exact ? TopSortedIndex | BoldId.CLASSIDEXACTMASK : TopSortedIndex;

    [Delphi(PortStatus.Ported)] protected abstract string GetAsString();
    [Delphi(PortStatus.Ported)] protected abstract bool GetIsStorable();
    [Delphi(PortStatus.Ported)] protected abstract uint GetHash();
    [Delphi(PortStatus.Ported)] public abstract bool GetIsEqual(TBoldObjectId? MatchID);

    /// <summary>The timestamp of a versioned id; BOLDMAXTIMESTAMP (now) for others.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldTimeStampType GetTimeStamp() => BoldDefs.BOLDMAXTIMESTAMP;

    [Delphi(PortStatus.Ported)]
    public abstract TBoldObjectId CloneWithClassId(int TopSortedIndex, bool Exact);

    [Delphi(PortStatus.Ported)]
    public TBoldObjectId Clone() => CloneWithClassId(TopSortedIndex, TopSortedIndexExact);

    [Delphi(PortStatus.Ported)] public int TopSortedIndex => getTopSortedIndex();
    [Delphi(PortStatus.Ported)] public bool TopSortedIndexExact => GetTopSortedIndexExact();
    [Delphi(PortStatus.Ported)] public string AsString => GetAsString();
    [Delphi(PortStatus.Ported)] public bool IsStorable => GetIsStorable();
    [Delphi(PortStatus.Ported)] public bool IsEqual(TBoldObjectId? MatchId) => GetIsEqual(MatchId);
    [Delphi(PortStatus.Ported)] public uint Hash => GetHash();
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType TimeStamp => GetTimeStamp();

    public override string ToString() => AsString;
}

/// <summary>The id of an object that is not stored yet: a number unique in the process.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldInternalObjectId : TBoldObjectId
{
    private static int InternalIdCounter;
    internal int fInternalIdentifier;

    /// <summary>An unnumbered id, as Delphi's plain Create; a streamer fills it in.</summary>
    public TBoldInternalObjectId()
    {
    }

    /// <summary>A new id with the next internal number.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldInternalObjectId.CreateWithClassID")]
    public TBoldInternalObjectId(int TopSortedIndex, bool Exact)
        : base(TopSortedIndex, Exact)
    {
        fInternalIdentifier = Interlocked.Increment(ref InternalIdCounter) - 1;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldInternalObjectId.CreateWithClassIDandInternalId")]
    public TBoldInternalObjectId(int InternalIdentifier, int TopSortedIndex, bool Exact)
        : this(TopSortedIndex, Exact)
    {
        fInternalIdentifier = InternalIdentifier;
    }

    [Delphi(PortStatus.Ported)] protected override string GetAsString() => SysUtils.IntToStr(fInternalIdentifier);
    [Delphi(PortStatus.Ported)] protected override uint GetHash() => unchecked((uint)fInternalIdentifier);
    [Delphi(PortStatus.Ported)] protected override bool GetIsStorable() => false;

    [Delphi(PortStatus.Ported)]
    public override bool GetIsEqual(TBoldObjectId? MatchID) =>
        MatchID is not null && MatchID.GetType() == GetType() && fInternalIdentifier == ((TBoldInternalObjectId)MatchID).fInternalIdentifier;

    [Delphi(PortStatus.Ported)]
    public override string GetStreamName() => BoldDefaultStreamNames.BOLDINTERNALIDNAME;

    /// <summary>A copy with the same internal number. Creating it still takes a number from the counter, as in Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldObjectId CloneWithClassId(int TopSortedIndex, bool Exact) =>
        new TBoldInternalObjectId(TopSortedIndex, Exact) { fInternalIdentifier = fInternalIdentifier };
}

/// <summary>The id of a stored object.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldExternalObjectID : TBoldObjectId
{
    protected TBoldExternalObjectID()
    {
    }

    protected TBoldExternalObjectID(int TopSortedIndex, bool Exact)
        : base(TopSortedIndex, Exact)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetIsStorable() => true;
}

[Delphi(PortStatus.Ported)]
public class TBoldIdList : TBoldIndexableList
{
}

/// <summary>Object ids keyed by id equality.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectIdHashIndex : TBoldHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldObjectId ItemAsBoldObjectId(object? Item) => (TBoldObjectId)Item!;

    [Delphi(PortStatus.Ported)]
    protected override uint HashItem(object? Item) => ItemAsBoldObjectId(Item).Hash;

    [Delphi(PortStatus.Ported)]
    protected override bool Match(object Key, object? Item) => ((TBoldObjectId)Key).IsEqual(ItemAsBoldObjectId(Item));

    [Delphi(PortStatus.Ported)]
    protected override uint Hash(object Key) => ((TBoldObjectId)Key).Hash;

    [Delphi(PortStatus.Ported)]
    protected internal object? FindById(TBoldObjectId boldObjectId) => Find(boldObjectId);
}

/// <summary>
/// An ordered list of object ids. Adding stores a clone; the list owns its ids. Lookup by id equality uses a linear search
/// below 10 ids and a hash index above.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectIdList : TBoldIdList, IBoldStreamable
{
    private static int IX_ObjectID = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldObjectIdList()
    {
        SetIndexCapacity(1);
        OwnsEntries = true;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectIdHashIndex GetObjectIDIndex()
    {
        if (UnorderedIndexCount == 0)
        {
            BoldIndexableList.SetIndexVariable(ref IX_ObjectID, AddIndex(new TBoldObjectIdHashIndex()));
        }

        return (TBoldObjectIdHashIndex)Indexes(IX_ObjectID)!;
    }

    [Delphi(PortStatus.Ported)]
    protected int GetCount() => Count;

    [Delphi(PortStatus.Ported)]
    protected TBoldObjectId? GetIDByID(TBoldObjectId ObjectID)
    {
        if (Count < 10)
        {
            for (int i = 0; i < Count; i++)
            {
                if (ObjectID.IsEqual((TBoldObjectId?)base[i]))
                {
                    return (TBoldObjectId?)base[i];
                }
            }

            return null;
        }

        return (TBoldObjectId?)ObjectIDIndex.FindById(ObjectID);
    }

    [Delphi(PortStatus.Ported)]
    protected int GetIndexByID(TBoldObjectId ObjectID)
    {
        TBoldObjectId? internalId = GetIDByID(ObjectID);
        return internalId is not null ? IndexOf(internalId) : -1;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldObjectId GetObjectId(int index) => (TBoldObjectId)base[index]!;

    [Delphi(PortStatus.Ported)]
    protected bool GetIdInList(TBoldObjectId? ObjectID) => ObjectID is not null && GetIDByID(ObjectID) is not null;

    [Delphi(PortStatus.Ported)]
    public string GetStreamName() => BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME;

    [Delphi(PortStatus.Ported)]
    protected TBoldObjectIdHashIndex ObjectIDIndex => GetObjectIDIndex();

    /// <summary>Adds a clone of the id (null adds an empty slot).</summary>
    [Delphi(PortStatus.Ported)]
    public void Add(TBoldObjectId? ObjectID) => base.Add(ObjectID?.Clone());

    [Delphi(PortStatus.Ported)]
    public void AddIfNotInList(TBoldObjectId ObjectID)
    {
        if (!IdInList(ObjectID))
        {
            Add(ObjectID);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddList(TBoldObjectIdList ObjectIdList)
    {
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            Add(ObjectIdList[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Insert(int Index, TBoldObjectId? ObjectID) => base.Insert(Index, ObjectID?.Clone());

    /// <summary>Same count and every id of <paramref name="List"/> in this list.</summary>
    [Delphi(PortStatus.Ported)]
    public bool ContainsSameIDs(TBoldObjectIdList List)
    {
        bool result = List.Count == Count;
        for (int i = 0; result && i < List.Count; i++)
        {
            result = IdInList(List[i]);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldObjectIdList Clone()
    {
        var result = new TBoldObjectIdList();
        for (int i = 0; i < Count; i++)
        {
            result.Add(this[i]);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public string CommaSeparatedIdList() => string.Join(", ", Enumerable.Range(0, Count).Select(i => this[i].AsString));

    /// <summary>Removes the id object itself (not an equal id).</summary>
    [Delphi(PortStatus.Ported)]
    public void remove(TBoldObjectId Id)
    {
        int p = IndexOf(Id);
        if (p != -1)
        {
            RemoveByIndex(p);
        }
    }

    /// <summary>Replaces the id equal to <paramref name="OldId"/> with a clone of <paramref name="NewId"/>, in place.</summary>
    [Delphi(PortStatus.Ported)]
    public void ReplaceID(TBoldObjectId OldId, TBoldObjectId NewId)
    {
        if (OldId == NewId)
        {
            return;
        }

        TBoldObjectId? tempId = IDByID(OldId);
        int oldIndex = IndexOf(tempId);
        RemoveByIndex(oldIndex);
        Insert(oldIndex, NewId);
    }

    /// <summary>Replaces each inexact id with its translation.</summary>
    [Delphi(PortStatus.Ported)]
    public void ExactifyIds(TBoldIDTranslationList TranslationList)
    {
        for (int i = 0; i < Count; i++)
        {
            if (!this[i].TopSortedIndexExact)
            {
                ReplaceID(this[i], TranslationList.TranslateToNewId(this[i])!);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ApplyTranslationList(TBoldIDTranslationList TranslationList)
    {
        for (int i = Count - 1; i >= 0; i--)
        {
            TBoldObjectId? anId = TranslationList.TranslateToNewId(this[i]);
            if (anId is not null)
            {
                ReplaceID(this[i], anId);
            }
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectId? IDByID(TBoldObjectId ObjectID) => GetIDByID(ObjectID);
    [Delphi(PortStatus.Ported)] public int IndexByID(TBoldObjectId ObjectID) => GetIndexByID(ObjectID);

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectIdList.ObjectIds")]
    public new TBoldObjectId this[int index] => GetObjectId(index);

    [Delphi(PortStatus.Ported)] public bool IdInList(TBoldObjectId? Objectid) => GetIdInList(Objectid);
}

/// <summary>A list of member ids; it owns them.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberIdList : TBoldIdList, IBoldStreamable
{
    [Delphi(PortStatus.Ported)]
    public string GetStreamName() => BoldDefaultStreamNames.BOLDMEMBERIDLISTNAME;

    [Delphi(PortStatus.Ported)]
    protected TBoldMemberID GetMemberIds(int Index) => (TBoldMemberID)base[Index]!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberIdList.MemberIds")]
    public new TBoldMemberID this[int Index] => GetMemberIds(Index);
}

/// <summary>
/// Pairs of old and new ids: internal ids that became stored ids when created, or inexact ids made exact when fetched.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldIDTranslationList : TBoldNonRefCountedObject, IBoldStreamable, IDisposable
{
    internal TBoldObjectIdList fOldIds;
    internal TBoldObjectIdList fNewIds;

    [Delphi(PortStatus.Ported)]
    public TBoldIDTranslationList()
    {
        fOldIds = new TBoldObjectIdList();
        fNewIds = new TBoldObjectIdList();
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        fOldIds.Dispose();
        fNewIds.Dispose();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)] private TBoldObjectId GetOldId(int index) => fOldIds[index];
    [Delphi(PortStatus.Ported)] private TBoldObjectId GetNewId(int index) => fNewIds[index];
    [Delphi(PortStatus.Ported)] private int GetCount() => fOldIds.Count;

    /// <summary>The old id of <paramref name="NewID"/>, or <paramref name="NewID"/> itself (one step only).</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldObjectId? GetTranslateToOldId(TBoldObjectId? NewID)
    {
        if (NewID is null)
        {
            return null;
        }

        int pos = fNewIds.IndexByID(NewID);
        return pos != -1 ? GetOldId(pos) : NewID;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectId? GetTranslateToNewId(TBoldObjectId? OldID)
    {
        if (OldID is null)
        {
            return null;
        }

        int pos = fOldIds.IndexByID(OldID);
        return pos != -1 ? GetNewId(pos) : OldID;
    }

    [Delphi(PortStatus.Ported)]
    public string GetStreamName() => BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME;

    [Delphi(PortStatus.Ported)]
    public void AddTranslation(TBoldObjectId? OldId, TBoldObjectId? NewId)
    {
        fOldIds.Add(OldId);
        fNewIds.Add(NewId);
    }

    [Delphi(PortStatus.Ported)] public int Count => GetCount();
    [Delphi(PortStatus.Ported)] public TBoldObjectId? TranslateToOldId(TBoldObjectId? NewID) => GetTranslateToOldId(NewID);
    [Delphi(PortStatus.Ported)] public TBoldObjectId? TranslateToNewId(TBoldObjectId? OldID) => GetTranslateToNewId(OldID);
    [Delphi(PortStatus.Ported)] public TBoldObjectId OldIds(int Index) => GetOldId(Index);
    [Delphi(PortStatus.Ported)] public TBoldObjectId NewIds(int Index) => GetNewId(Index);
}

/// <summary>An operation failed for some objects; the exception carries a copy of their ids.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldOperationFailedForIdList : EBold
{
    private readonly TBoldObjectIdList fIdList;

    [Delphi(PortStatus.Ported)]
    public EBoldOperationFailedForIdList(string msg, object?[] args, TBoldObjectIdList IdList)
        : base(SysUtils.Format(msg, args))
    {
        fIdList = IdList.Clone();
    }

    [Delphi(PortStatus.Replaced, Symbol = "EBoldOperationFailedForIdList.Destroy", Note = "The garbage collector frees the id list.")]
    [Delphi(PortStatus.Ported)]
    public TBoldObjectIdList IdList => fIdList;
}

/// <summary>Streams the class part of an object id: the class name and, when inexact, Exact=0.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldXMLObjectIdStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported)]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        TMoldModel model = ((TBoldDefaultXMLStreamManager)Node.Manager).Model;
        var objectId = (TBoldObjectId)Obj;
        Node.NewSubNode("ClassName").WriteString(model.Classes[objectId.TopSortedIndex].Name);
        if (!objectId.TopSortedIndexExact)
        {
            Node.NewSubNode("Exact").WriteBoolean(objectId.TopSortedIndexExact);
        }
    }

    /// <summary>Reads ClassName and Exact, or the older classid/name and classid/exact form.</summary>
    [Delphi(PortStatus.Ported)]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        TMoldModel model = ((TBoldDefaultXMLStreamManager)Node.Manager).Model;
        int topSortedIndex;
        bool exact;
        TBoldXMLNode? subNode = Node.GetSubNode("ClassName");
        if (subNode is not null)
        {
            topSortedIndex = model.Classes.ItemsByName(subNode.ReadString())!.TopSortedIndex;
            subNode = Node.GetSubNode("Exact");
            exact = subNode is null || subNode.ReadBoolean();
        }
        else
        {
            TBoldXMLNode classIdNode = Node.GetSubNode("classid")!;
            topSortedIndex = model.Classes.ItemsByName(classIdNode.GetSubNode("name")!.ReadString())!.TopSortedIndex;
            exact = classIdNode.GetSubNode("exact")!.ReadBoolean();
        }

        ((TBoldObjectId)Obj).SetClassIdData(topSortedIndex, exact);
    }
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLInternalObjectIdStreamer")]
internal sealed class TBoldXMLInternalObjectIdStreamer : TBoldXMLObjectIdStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLInternalObjectIdStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldDefaultStreamNames.BOLDINTERNALIDNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLInternalObjectIdStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        base.WriteObject(Obj, Node);
        Node.NewSubNode("identifier").WriteInteger(((TBoldInternalObjectId)Obj).fInternalIdentifier);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLInternalObjectIdStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        base.ReadObject(Obj, Node);
        ((TBoldInternalObjectId)Obj).fInternalIdentifier = Node.GetSubNode("identifier")!.ReadInteger();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLInternalObjectIdStreamer.CreateObject")]
    public override object CreateObject() => new TBoldInternalObjectId();
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLObjectIdListStreamer")]
internal sealed class TBoldXMLObjectIdListStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLObjectIdListStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLObjectIdListStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        var idList = (TBoldObjectIdList)Obj;
        for (int i = 0; i < idList.Count; i++)
        {
            Node.NewSubNode("id").WriteObject("", idList[i]);
        }
    }

    /// <summary>Reads every "id" element below the node, as MSXML getElementsByTagName does.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLObjectIdListStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        var idList = (TBoldObjectIdList)Obj;
        foreach (XmlElement element in Node.XMLDomElement.GetElementsByTagName("id"))
        {
            idList.Add((TBoldObjectId)Node.MakeNodeForElement(element).ReadObject("")!);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLObjectIdListStreamer.CreateObject")]
    public override object CreateObject() => new TBoldObjectIdList();
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLTranslationListStreamer")]
internal sealed class TBoldXMLTranslationListStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLTranslationListStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldDefaultStreamNames.BOLDIDTRANSLATIONLISTNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLTranslationListStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        var list = (TBoldIDTranslationList)Obj;
        Node.WriteSubNodeObject("OldIds", BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, list.fOldIds);
        Node.WriteSubNodeObject("NewIds", BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME, list.fNewIds);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLTranslationListStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        var list = (TBoldIDTranslationList)Obj;
        list.fOldIds = (TBoldObjectIdList)Node.ReadSubNodeObject("OldIds", BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME)!;
        list.fNewIds = (TBoldObjectIdList)Node.ReadSubNodeObject("NewIds", BoldDefaultStreamNames.BOLDOBJECTIDLISTNAME)!;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLTranslationListStreamer.CreateObject")]
    public override object CreateObject() => new TBoldIDTranslationList();
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdStreamer")]
internal sealed class TBoldXMLMemberIdStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldDefaultStreamNames.BOLDMEMBERIDNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node) =>
        Node.WriteSubNodeInteger("MemberIndex", ((TBoldMemberID)Obj).MemberIndex);

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node) =>
        ((TBoldMemberID)Obj).fMemberIndex = Node.ReadSubNodeInteger("MemberIndex");

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdStreamer.CreateObject")]
    public override object CreateObject() => new TBoldMemberID(0);
}

[Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdListStreamer")]
internal sealed class TBoldXMLMemberIdListStreamer : TBoldXMLObjectStreamer
{
    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdListStreamer.GetStreamName")]
    protected override string GetStreamName() => BoldDefaultStreamNames.BOLDMEMBERIDLISTNAME;

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdListStreamer.WriteObject")]
    public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node)
    {
        var list = (TBoldMemberIdList)Obj;
        for (int i = 0; i < list.Count; i++)
        {
            Node.NewSubNode("id").WriteObject("", list[i]);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdListStreamer.ReadObject")]
    public override void ReadObject(object Obj, TBoldXMLNode Node)
    {
        var list = (TBoldMemberIdList)Obj;
        foreach (XmlElement element in Node.XMLDomElement.GetElementsByTagName("id"))
        {
            list.Add((TBoldMemberID)Node.MakeNodeForElement(element).ReadObject("")!);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMLMemberIdListStreamer.CreateObject")]
    public override object CreateObject() => new TBoldMemberIdList();
}

/// <summary>Class id bits of BoldId.</summary>
public static partial class BoldId
{
    internal const int TOPSORTEDINDEXTMASK = 0x0FFFFFFF;
    internal const int CLASSIDEXACTMASK = 0x10000000;

    internal static void RegisterStreamers()
    {
        TBoldXMLStreamerRegistry main = TBoldXMLStreamerRegistry.MainStreamerRegistry();
        main.RegisterStreamer(new TBoldXMLInternalObjectIdStreamer());
        main.RegisterStreamer(new TBoldXMLObjectIdListStreamer());
        main.RegisterStreamer(new TBoldXMLMemberIdStreamer());
        main.RegisterStreamer(new TBoldXMLMemberIdListStreamer());
        main.RegisterStreamer(new TBoldXMLTranslationListStreamer());
    }
}
