// Delphi: Source/Propagator/Enterprise/BoldPropagatorSubscriptions.pas
// Port map: CSharp/port/units/BoldPropagatorSubscriptions.md

namespace Bold;

/// <summary>A sorted list of distinct integers (client ids).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSortedIntegerList : TList
{
    /// <summary>Inserts the value in order; returns its position, or -1 when it is in the list already.</summary>
    [Delphi(PortStatus.Ported)]
    public int Add(int Value)
    {
        int result = -1;
        int Low = 0;
        int High = Count;
        while (High - Low > 1)
        {
            int i = (High + Low) / 2;
            int Item = (int)this[i]!;
            if (Value == Item)
            {
                return result;
            }

            if (Value < Item)
            {
                High = i;
            }
            else
            {
                Low = i;
            }
        }

        if (Count > 0)
        {
            if ((int)this[Low]! < Value)
            {
                Insert(Low + 1, Value);
                result = Low + 1;
            }
            else if ((int)this[Low]! > Value)
            {
                Insert(Low, Value);
                result = Low;
            }
        }
        else
        {
            result = base.Add(Value);
        }

        return result;
    }
}

/// <summary>A subscription: a client's node, in the chain of its client id and in the chain of its event name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubscriptionNode : TBoldAbstractLinkNode
{
    private readonly TBoldClientID fClientID;
    private readonly TBoldMultiIndexedListNode?[] fNextNodes = new TBoldMultiIndexedListNode?[2];
    private readonly TBoldMultiIndexedListNode?[] fPreviousNodes = new TBoldMultiIndexedListNode?[2];

    [Delphi(PortStatus.Ported)]
    public TBoldSubscriptionNode(TBoldClientID ClientID)
    {
        fClientID = ClientID;
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

    [Delphi(PortStatus.Ported)]
    public TBoldClientID ClientID => fClientID;
}

/// <summary>
/// The propagator's subscriptions: which clients subscribe to which events, indexed both by client id and by event
/// name.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubscriptionList : TBoldAbstractMultiIndexedList, IDisposable
{
    private TBoldClientIDList? fClientIDList;
    private TBoldEventNameList? fEventNameList;
    private readonly int ClientIDIndexOrder;
    private readonly int EventNameIndexOrder;

    [Delphi(PortStatus.Ported)]
    public TBoldSubscriptionList()
    {
        ClientIDIndexOrder = AddIndex(typeof(TBoldClientIDList));
        EventNameIndexOrder = AddIndex(typeof(TBoldEventNameList));
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldSubscriptionList.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fClientIDList);
        SysUtils.FreeAndNil(ref fEventNameList);
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldIndexNode getClient(TBoldClientID Index) => fClientIDList![Index]!;

    [Delphi(PortStatus.Ported)]
    private TBoldEventNameIndexNode? getEvent(string Index) => (TBoldEventNameIndexNode?)fEventNameList![Index];

    [Delphi(PortStatus.Ported)]
    protected override TBoldIndexList? getIndexList(int Index) => Index switch
    {
        0 => fClientIDList,
        1 => fEventNameList,
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
            fEventNameList = (TBoldEventNameList)Value;
        }
    }

    /// <summary>Subscribes the client to the event (a client subscribed twice gets two nodes, as in Delphi).</summary>
    [Delphi(PortStatus.Ported)]
    public void AddSubscription(TBoldClientID ClientID, string EventName) =>
        AddNode([ClientID, EventName], new TBoldSubscriptionNode(ClientID));

    /// <summary>Removes all the client's subscriptions.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveClient(TBoldClientID ClientId) => RemoveKey(ClientIDIndexOrder, ClientId);

    /// <summary>Removes every client's subscription to the event.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveEvent(string EventName) => RemoveKey(EventNameIndexOrder, EventName);

    /// <summary>Adds the names of the events the client subscribes to.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetEventsByClientID(TBoldClientID ClientID, TStringList? events)
    {
        if (events is null)
        {
            return;
        }

        var aNode = (TBoldSubscriptionNode?)getClient(ClientID).Next;
        while (aNode is not null)
        {
            TBoldMultiIndexedListNode? iNode = aNode.Previous(EventNameIndexOrder);
            while (iNode is TBoldSubscriptionNode subscription)
            {
                iNode = subscription.Previous(EventNameIndexOrder);
            }

            if (iNode is TBoldEventNameIndexNode eventNode)
            {
                events.Add(eventNode.EventName);
            }

            aNode = (TBoldSubscriptionNode?)aNode.Next(ClientIDIndexOrder);
        }
    }

    [Delphi(PortStatus.Ported)]
    public int GetEventCountByClientID(TBoldClientID ClientID)
    {
        int result = 0;
        var aNode = (TBoldSubscriptionNode?)getClient(ClientID).Next;
        while (aNode is not null)
        {
            TBoldMultiIndexedListNode? iNode = aNode.Previous(EventNameIndexOrder);
            while (iNode is TBoldSubscriptionNode subscription)
            {
                iNode = subscription.Previous(EventNameIndexOrder);
            }

            if (iNode is not null)
            {
                result++;
            }

            aNode = (TBoldSubscriptionNode?)aNode.Next(ClientIDIndexOrder);
        }

        return result;
    }

    /// <summary>Adds the ids of the clients subscribed to the event (once per subscription).</summary>
    [Delphi(PortStatus.Ported)]
    public void GetClientsSubscribedToEvent(string EventName, TList? ClientIds)
    {
        if (ClientIds is not null && getEvent(EventName) is { } eventNode)
        {
            var aNode = (TBoldSubscriptionNode?)eventNode.Next;
            while (aNode is not null)
            {
                ClientIds.Add(aNode.ClientID);
                aNode = (TBoldSubscriptionNode?)aNode.Next(EventNameIndexOrder);
            }
        }
    }

    /// <summary>Adds the ids of the clients subscribed to the event, except <paramref name="BoldClientID"/>, each once.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetAllSubscribedClientsExcept(string EventName, TBoldClientID BoldClientID, TBoldSortedIntegerList ClientIds)
    {
        if (ClientIds is null)
        {
            throw new EBold(SysUtils.Format(PropagatorConsts.sClientIDsNotAssigned, GetType().Name, "GetAllSubscribedClientsExcept"));
        }

        if (getEvent(EventName) is { } aEvent)
        {
            var aNode = (TBoldSubscriptionNode?)aEvent.Next;
            while (aNode is not null)
            {
                if (aNode.ClientID != BoldClientID)
                {
                    ClientIds.Add(aNode.ClientID);
                }

                aNode = (TBoldSubscriptionNode?)aNode.Next(EventNameIndexOrder);
            }
        }
    }

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldIndexNode Clients(TBoldClientID Index) => getClient(Index);

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldEventNameIndexNode? Events(string Index) => getEvent(Index);
}
