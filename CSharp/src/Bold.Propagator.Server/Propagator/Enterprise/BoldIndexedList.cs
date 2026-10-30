// Delphi: Source/Propagator/Enterprise/BoldIndexedList.pas
// Port map: CSharp/port/units/BoldIndexedList.md

namespace Bold;

/// <summary>Sizes of the client id index.</summary>
public static class BoldIndexedList
{
    [Delphi(PortStatus.Ported)] public const int Initial_Client_Count = 1000;
    [Delphi(PortStatus.Ported)] public const int ClientIdArrayInc = 100;
}

/// <summary>An index by client id: an array with an index node per client id, grown as ids come.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientIDList : TBoldIndexList, IDisposable
{
    private TBoldObjectArray? fClients;

    [Delphi(PortStatus.Ported)]
    public TBoldClientIDList(int IndexOrder)
        : base(IndexOrder)
    {
        fClients = new TBoldObjectArray(BoldIndexedList.Initial_Client_Count,
            TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner, TBoldContainerOption.bcoThreadSafe));
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientIDList.Destroy")]
    public void Dispose()
    {
        if (fClients is null)
        {
            return;
        }

        DeleteNodes();
        SysUtils.FreeAndNil(ref fClients);
        GC.SuppressFinalize(this);
    }

    /// <summary>The index node of the client id, created if missing.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexNode? getItem(object Key)
    {
        int aKey = (int)Key;
        if (Clients(aKey) is null)
        {
            fClients![aKey] = new TBoldIndexNode();
        }

        return (TBoldIndexNode?)Clients(aKey);
    }

    [Delphi(PortStatus.Ported)]
    protected override void setItem(object Key, TBoldIndexNode Value)
    {
        int aKey = (int)Key;
        if (Clients(aKey) is null)
        {
            setClient(aKey, Value);
        }
        else
        {
            ((TBoldIndexNode)Clients(aKey)!).Next = Value.Next;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override int getCount() => fClients!.Count;

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexNode InsertINode(object Key)
    {
        int aKey = (int)Key;
        if (Clients(aKey) is null)
        {
            setClient(aKey, new TBoldIndexNode());
        }

        return (TBoldIndexNode)Clients(aKey)!;
    }

    /// <summary>The slot of the client id, growing the array to hold it.</summary>
    [Delphi(PortStatus.Ported)]
    protected object? getClient(int Index)
    {
        if (Index >= fClients!.Count)
        {
            int cnt = Index - fClients.Count + 1;
            int increment = BoldIndexedList.ClientIdArrayInc;
            while (cnt > increment)
            {
                increment += BoldIndexedList.ClientIdArrayInc;
            }

            for (int i = 0; i <= increment; i++)
            {
                fClients.Add(null);
            }
        }

        return fClients[Index];
    }

    [Delphi(PortStatus.Ported, Note = "Writes the slot; Delphi's setter called itself through the property (it was never reached).")]
    protected void setClient(int Index, object? Value)
    {
        getClient(Index);
        fClients![Index] = Value;
    }

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    protected object? Clients(int Index) => getClient(Index);

    [Delphi(PortStatus.Ported)]
    public override void DeleteNodes()
    {
        for (int i = 0; i < Count; i++)
        {
            if (fClients![i] is TBoldIndexNode iNode)
            {
                DeleteINode(iNode);
            }
        }
    }
}

/// <summary>The hash index of the event name index nodes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalEventIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldEventNameIndexNode)Item!).EventName;
}

/// <summary>The event name index nodes, found by event name (ignoring case).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldExternalEventHashTable : TBoldUnOrderedIndexableList
{
    private static int IX_EventName = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldExternalEventHashTable()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_EventName, AddIndex(new TBoldExternalEventIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldEventNameIndexNode? GetItemByEventName(string EventName) =>
        (TBoldEventNameIndexNode?)((TBoldExternalEventIndex)Indexes(IX_EventName)!).FindByString(EventName);

    [Delphi(PortStatus.Ported)]
    public void Add(TBoldEventNameIndexNode Item) => base.Add(Item);

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldEventNameIndexNode? ItemsByEventName(string ExpressionName) => GetItemByEventName(ExpressionName);
}

/// <summary>An index by event name: an index node per event name, created when the first node with the name comes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldEventNameList : TBoldIndexList, IDisposable
{
    private TBoldExternalEventHashTable? fEvents;

    [Delphi(PortStatus.Ported)]
    public TBoldEventNameList(int IndexOrder)
        : base(IndexOrder)
    {
        fEvents = new TBoldExternalEventHashTable();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldEventNameList.Destroy")]
    public void Dispose()
    {
        if (fEvents is null)
        {
            return;
        }

        DeleteNodes();
        SysUtils.FreeAndNil(ref fEvents);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexNode? getItem(object Key) => fEvents!.ItemsByEventName((string)Key);

    [Delphi(PortStatus.Ported)]
    protected override void setItem(object Key, TBoldIndexNode Value) => fEvents!.Add((TBoldEventNameIndexNode)Value);

    [Delphi(PortStatus.Ported)]
    protected override int getCount() => fEvents!.Count;

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexNode InsertINode(object Key)
    {
        string aKey = (string)Key;
        if (fEvents!.ItemsByEventName(aKey) is null)
        {
            fEvents.Add(new TBoldEventNameIndexNode { EventName = aKey, Next = null });
        }

        return fEvents.ItemsByEventName(aKey)!;
    }

    [Delphi(PortStatus.Ported)]
    public override void DeleteINodeByKey(object Key)
    {
        base.DeleteINodeByKey(Key);
        if (fEvents!.ItemsByEventName((string)Key) is { } node)
        {
            fEvents.Remove(node);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void DeleteNodes()
    {
        using TBoldIndexableListTraverser? Traverser = fEvents!.CreateTraverser();
        while (Traverser is not null && !Traverser.EndOfList)
        {
            if (Traverser.Item is TBoldIndexNode iNode)
            {
                DeleteINode(iNode);
            }

            Traverser.Next();
        }
    }
}
