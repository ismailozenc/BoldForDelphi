// Delphi: Source/Propagator/Enterprise/BoldLockList.pas
// Port map: CSharp/port/units/BoldLockList.md

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldLockType { bltShared, bltExclusive }

/// <summary>The hash index of the lock name index nodes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldLockNameIndexNode)Item!).LockName;
}

/// <summary>The lock name index nodes, found by lock name (ignoring case).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockNameHashList : TBoldUnOrderedIndexableList
{
    private static int IX_LockName = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldLockNameHashList()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_LockName, AddIndex(new TBoldLockIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldLockNameIndexNode? GetItembyLockName(string LockName) =>
        (TBoldLockNameIndexNode?)((TBoldLockIndex)Indexes(IX_LockName)!).FindByString(LockName);

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldLockNameIndexNode Item) => base.Add(Item);

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldLockNameIndexNode? ItemsByLockName(string LockName) => GetItembyLockName(LockName);
}

/// <summary>The hash index of a lock's nodes by client id.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientIdIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => SysUtils.IntToStr(((TBoldLockNode)Item!).ClientId);
}

/// <summary>The nodes of one lock, found by client id; it does not own them.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientIdHashList : TBoldIndexableList
{
    private static int IX_ClientID = -1;
    internal readonly TBoldLockNameIndexNode fOwnerIndexNode;

    [Delphi(PortStatus.Ported)]
    public TBoldClientIdHashList(TBoldLockNameIndexNode OwnerIndexNode)
    {
        OwnsEntries = false;
        SetIndexCapacity(2);
        BoldIndexableList.SetIndexVariable(ref IX_ClientID, AddIndex(new TBoldClientIdIndex()));
        fOwnerIndexNode = OwnerIndexNode;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldLockNode? GetItembyClientId(TBoldClientID ClientId) =>
        (TBoldLockNode?)((TBoldClientIdIndex)Indexes(IX_ClientID)!).FindByString(SysUtils.IntToStr(ClientId));

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldLockNode? ItemsbyClientID(TBoldClientID ClientId) => GetItembyClientId(ClientId);
}

/// <summary>The index node of a lock name: how many shared locks and whether an exclusive lock are handed out.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockNameIndexNode : TBoldIndexNode, IDisposable
{
    private TBoldClientIdHashList? fClients;
    internal int fNoSharedLocks;

    [Delphi(PortStatus.Ported, Symbol = "TBoldLockNameIndexNode.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fClients);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldClientIdHashList getClients() => fClients ??= new TBoldClientIdHashList(this);

    [Delphi(PortStatus.Ported)]
    public string LockName { get; set; } = "";

    [Delphi(PortStatus.Ported)]
    public int NoSharedLocks
    {
        get => fNoSharedLocks;
        set => fNoSharedLocks = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool ExclusiveLock { get; set; }

    [Delphi(PortStatus.Ported)]
    public TBoldClientIdHashList Clients => getClients();
}

/// <summary>The index by lock name; each lock name index node also finds the lock's nodes by client id.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockNameList : TBoldIndexList, IDisposable
{
    private TBoldLockNameHashList? fLocks;

    [Delphi(PortStatus.Ported)]
    public TBoldLockNameList(int IndexOrder)
        : base(IndexOrder)
    {
        fLocks = new TBoldLockNameHashList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldLockNameList.Destroy")]
    public void Dispose()
    {
        if (fLocks is null)
        {
            return;
        }

        DeleteNodes();
        SysUtils.FreeAndNil(ref fLocks);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexNode? getItem(object Key) => fLocks!.ItemsByLockName((string)Key);

    [Delphi(PortStatus.Ported)]
    protected override void setItem(object Key, TBoldIndexNode Value) => fLocks!.Add((TBoldLockNameIndexNode)Value);

    [Delphi(PortStatus.Ported)]
    protected override int getCount() => fLocks!.Count;

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexNode InsertINode(object Key)
    {
        string aKey = (string)Key;
        if (fLocks!.ItemsByLockName(aKey) is null)
        {
            fLocks.Add(new TBoldLockNameIndexNode { LockName = aKey, Next = null, ExclusiveLock = false, NoSharedLocks = 0 });
        }

        return fLocks.ItemsByLockName(aKey)!;
    }

    [Delphi(PortStatus.Ported)]
    public override void DeleteNodes()
    {
        using TBoldIndexableListTraverser? Traverser = fLocks!.CreateTraverser();
        while (Traverser is not null && !Traverser.EndOfList)
        {
            if (Traverser.Item is TBoldIndexNode iNode)
            {
                DeleteINode(iNode);
            }

            Traverser.Next();
        }
    }

    /// <summary>
    /// Links the lock node in under its lock name and in the lock's client list. When the client already has a node
    /// for the lock, that node is removed instead (as in Delphi; TBoldLockList.AddLock never gets there).
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override void InsertNode(object Key, TBoldAbstractLinkNode NewNode)
    {
        var NewLockNode = (TBoldLockNode)NewNode;
        var iNode = (TBoldLockNameIndexNode)(this[Key] ?? InsertINode(Key));
        TBoldLockNode? CurrentNode = iNode.Clients.ItemsbyClientID(NewLockNode.ClientId);
        if (CurrentNode is not null)
        {
            CurrentNode.Remove();
        }
        else
        {
            NewNode.setPrevious(IndexOrder, iNode);
            NewNode.setNext(IndexOrder, iNode.Next);
            iNode.Next?.setPrevious(IndexOrder, NewNode);
            iNode.Next = NewNode;
            NewLockNode.AddToList(iNode.Clients);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void DeleteINodeByKey(object Key)
    {
        base.DeleteINodeByKey(Key);
        if (fLocks!.ItemsByLockName((string)Key) is { } node)
        {
            fLocks.Remove(node);
        }
    }
}

/// <summary>A lock handed to a client: in the chain of the client id and in the chain of the lock name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldLockNode : TBoldAbstractLinkNode
{
    private TBoldLockType fLocktype;
    private TBoldClientIdHashList? fOwnerList;
    private bool fCanTimeOut;
    private readonly TBoldMultiIndexedListNode?[] fNextNodes = new TBoldMultiIndexedListNode?[2];
    private readonly TBoldMultiIndexedListNode?[] fPreviousNodes = new TBoldMultiIndexedListNode?[2];

    [Delphi(PortStatus.Ported)]
    public TBoldLockNode()
    {
        fCanTimeOut = BoldLockingDefs.LOCKS_CAN_TIMEOUT;
    }

    /// <summary>Changes the lock type, keeping the lock name's counts of shared and exclusive locks.</summary>
    [Delphi(PortStatus.Ported)]
    private void setLockType(TBoldLockType Value)
    {
        if (fLocktype != Value)
        {
            fLocktype = Value;
            if (IndexNode(1) is TBoldLockNameIndexNode LockNameIndexNode)
            {
                switch (fLocktype)
                {
                    case TBoldLockType.bltShared:
                        LockNameIndexNode.fNoSharedLocks++;
                        LockNameIndexNode.ExclusiveLock = false;
                        break;
                    case TBoldLockType.bltExclusive:
                        LockNameIndexNode.ExclusiveLock = true;
                        LockNameIndexNode.fNoSharedLocks--;
                        break;
                }
            }
        }
    }

    /// <summary>True when the lock can time out and has been held longer than its timeout.</summary>
    [Delphi(PortStatus.Ported)]
    private bool GetHasTimedOut()
    {
        bool result = fCanTimeOut;
        if (result)
        {
            TTimeStamp CurrentTime = SysUtils.DateTimeToTimeStamp(DateTime.Now);
            double LockAcquisitionDuration = SysUtils.TimeStampToMSecs(CurrentTime) - SysUtils.TimeStampToMSecs(LockAcquisitionTime);
            result = Math.Truncate(LockAcquisitionDuration) >= TimeOut;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMultiIndexedListNode? getNext(int Index) => fNextNodes[Index];

    [Delphi(PortStatus.Ported)]
    protected internal override void setNext(int Index, TBoldMultiIndexedListNode? Value) => fNextNodes[Index] = Value;

    [Delphi(PortStatus.Ported)]
    protected internal override TBoldMultiIndexedListNode? getPrevious(int Index) => fPreviousNodes[Index];

    [Delphi(PortStatus.Ported)]
    protected internal override void setPrevious(int Index, TBoldMultiIndexedListNode? Value) => fPreviousNodes[Index] = Value;

    [Delphi(PortStatus.Ported)]
    protected override int getNumberOfIndices() => 2;

    /// <summary>Unlinks the lock from its chains and from its lock's client list, and gives back its count.</summary>
    [Delphi(PortStatus.Ported, Note = "Does nothing to the counts of a lock in no client list; Delphi failed on it.")]
    public override void Remove()
    {
        if (fOwnerList is not null)
        {
            fOwnerList.Remove(this);
            switch (LockType)
            {
                case TBoldLockType.bltShared:
                    fOwnerList.fOwnerIndexNode.fNoSharedLocks--;
                    break;
                case TBoldLockType.bltExclusive:
                    fOwnerList.fOwnerIndexNode.ExclusiveLock = false;
                    break;
            }

            fOwnerList = null;
        }

        base.Remove();
    }

    /// <summary>Puts the lock in its lock's client list and counts it.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddToList(TBoldClientIdHashList OwnerList)
    {
        fOwnerList = OwnerList;
        fOwnerList.Add(this);
        switch (LockType)
        {
            case TBoldLockType.bltShared:
                fOwnerList.fOwnerIndexNode.fNoSharedLocks++;
                break;
            case TBoldLockType.bltExclusive:
                fOwnerList.fOwnerIndexNode.ExclusiveLock = true;
                break;
        }
    }

    /// <summary>Makes the lock one that cannot time out (or can again).</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureLock(bool Ensure) => fCanTimeOut = !Ensure;

    /// <summary>How long the lock has been held at <paramref name="CurrentTime"/>, in milliseconds.</summary>
    [Delphi(PortStatus.Ported)]
    public double GetLockDuration(TTimeStamp CurrentTime) =>
        SysUtils.TimeStampToMSecs(CurrentTime) - SysUtils.TimeStampToMSecs(LockAcquisitionTime);

    [Delphi(PortStatus.Ported)]
    public TBoldClientID ClientId { get; set; }

    /// <summary>The lock's timeout in milliseconds.</summary>
    [Delphi(PortStatus.Ported)]
    public int TimeOut { get; set; }

    [Delphi(PortStatus.Ported)]
    public TTimeStamp LockAcquisitionTime { get; set; }

    [Delphi(PortStatus.Ported)]
    public TBoldLockType LockType
    {
        get => fLocktype;
        set => setLockType(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool CanTimeOut => fCanTimeOut;

    [Delphi(PortStatus.Ported)]
    public bool HasTimedOut => GetHasTimedOut();
}

/// <summary>
/// The locks handed out by the lock manager, indexed by client id and by lock name. Delphi's TBoldLockList: the lock
/// holder's TBoldLockList (client side) has the name in C#.
/// </summary>
[Delphi(PortStatus.Ported, Symbol = "TBoldLockList", Note = "Named TBoldServerLockList: BoldLockHolder's TBoldLockList, which client code uses, has the Delphi name.")]
public class TBoldServerLockList : TBoldAbstractMultiIndexedList, IDisposable
{
    private TBoldClientIDList? fClientIDList;
    private TBoldLockNameList? fLockNameList;
    private readonly int fClientIDIndexOrder;
    private readonly int fLockNameIndexOrder;

    [Delphi(PortStatus.Ported)]
    public TBoldServerLockList()
    {
        fClientIDIndexOrder = AddIndex(typeof(TBoldClientIDList));
        fLockNameIndexOrder = AddIndex(typeof(TBoldLockNameList));
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldLockList.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fClientIDList);
        SysUtils.FreeAndNil(ref fLockNameList);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldIndexNode getClient(TBoldClientID Index) => fClientIDList![Index]!;

    [Delphi(PortStatus.Ported)]
    private TBoldLockNameIndexNode? getLock(string Index) => (TBoldLockNameIndexNode?)fLockNameList![Index];

    [Delphi(PortStatus.Ported)]
    private TBoldLockNode? getItem(TBoldClientID ClientId, string LockName) => getLock(LockName)?.Clients.ItemsbyClientID(ClientId);

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexList? getIndexList(int Index) => Index switch
    {
        0 => fClientIDList,
        1 => fLockNameList,
        _ => null,
    };

    [Delphi(PortStatus.Ported)]
    protected override void setIndexList(int Index, TBoldIndexList Value)
    {
        if (Index == 0)
        {
            fClientIDList = (TBoldClientIDList)Value;
        }
        else if (Index == 1)
        {
            fLockNameList = (TBoldLockNameList)Value;
        }
    }

    /// <summary>
    /// Hands the lock to the client. A client that holds the lock shared and asks for it exclusive gets it upgraded,
    /// acquired again now.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public TBoldLockNode AddLock(TBoldClientID ClientId, int TimeOut, TTimeStamp CurrentTime, string LockName, TBoldLockType LockType)
    {
        TBoldLockNode? result = getItem(ClientId, LockName);
        if (result is not null)
        {
            if (LockType == TBoldLockType.bltExclusive && result.LockType == TBoldLockType.bltShared)
            {
                result.LockType = TBoldLockType.bltExclusive;
                result.LockAcquisitionTime = CurrentTime;
            }
        }
        else
        {
            result = new TBoldLockNode
            {
                ClientId = ClientId,
                LockAcquisitionTime = CurrentTime,
                TimeOut = TimeOut,
                LockType = LockType,
            };
            AddNode([ClientId, LockName], result);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void RemoveClient(TBoldClientID ClientID) => RemoveKey(ClientIdIndexOrder, ClientID);

    [Delphi(PortStatus.Ported)]
    public void RemoveLock(string LockName) => RemoveKey(LockNameIndexOrder, LockName);

    [Delphi(PortStatus.Ported)]
    public void RemoveLockForClient(TBoldClientID ClientID, string LockName) => Items(ClientID, LockName)?.Remove();

    /// <summary>Adds the names of the client's locks.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetLocksByClientId(TBoldClientID ClientId, TStringList? locks)
    {
        if (locks is null)
        {
            return;
        }

        var aNode = (TBoldLockNode?)getClient(ClientId).Next;
        while (aNode is not null)
        {
            TBoldMultiIndexedListNode? iNode = aNode.Previous(LockNameIndexOrder);
            while (iNode is TBoldLockNode lockNode)
            {
                iNode = lockNode.Previous(LockNameIndexOrder);
            }

            locks.Add(((TBoldLockNameIndexNode)iNode!).LockName);
            aNode = (TBoldLockNode?)aNode.Next(ClientIdIndexOrder);
        }
    }

    /// <summary>Adds the ids of the clients that hold the lock.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetClientIDsbyLock(string LockName, TList? ClientIDs)
    {
        if (ClientIDs is not null && getLock(LockName) is { } LockNode)
        {
            var aNode = (TBoldLockNode?)LockNode.Next;
            while (aNode is not null)
            {
                ClientIDs.Add(aNode.ClientId);
                aNode = (TBoldLockNode?)aNode.Next(LockNameIndexOrder);
            }
        }
    }

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldIndexNode Clients(TBoldClientID Index) => getClient(Index);

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldLockNameIndexNode? Locks(string Index) => getLock(Index);

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldLockNode? Items(TBoldClientID ClientId, string LockName) => getItem(ClientId, LockName);

    [Delphi(PortStatus.Ported)]
    public int ClientIdIndexOrder => fClientIDIndexOrder;

    [Delphi(PortStatus.Ported)]
    public int LockNameIndexOrder => fLockNameIndexOrder;
}
