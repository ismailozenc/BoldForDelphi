// Delphi: Source/Propagator/Enterprise/BoldPriorityListEnlister.pas
// Port map: CSharp/port/units/BoldPriorityListEnlister.md

namespace Bold;

/// <summary>Puts client queues in the priority list of queues to send.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractPriorityListEnlister
{
    /// <summary>The queue is to be sent after its client's polling interval.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void EnlistQueue(TBoldClientQueue ClientQueue);

    /// <summary>The queue is to be sent at once.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void FlushQueue(TBoldClientQueue ClientQueue);
}

/// <summary>Enlists client queues in a priority list, by the time they are to be sent.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldPriorityListEnlister : TBoldAbstractPriorityListEnlister, IDisposable
{
    private TBoldPriorityQueue? fPriorityList;

    [Delphi(PortStatus.Ported)]
    public TBoldPriorityListEnlister()
    {
        fPriorityList = new TBoldPriorityQueue();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldPriorityListEnlister.Destroy")]
    public void Dispose()
    {
        if (fPriorityList is null)
        {
            return;
        }

        fPriorityList.OnHeadChanged = null;
        while (fPriorityList.Head is not null)
        {
            fPriorityList.RemoveAndFreeHead();
        }

        SysUtils.FreeAndNil(ref fPriorityList);
        GC.SuppressFinalize(this);
    }

    /// <summary>The propagator's client handler (virtual so tests can give another).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldClientHandler getClientHandler() => TBoldPropagatorServer.Instance().AdvancedPropagator!.ClientHandler!;

    [Delphi(PortStatus.Ported)]
    protected TBoldClientHandler ClientHandler => getClientHandler();

    /// <summary>Enlists the queue of a registered client, to be sent its polling interval from now.</summary>
    [Delphi(PortStatus.Ported)]
    public override void EnlistQueue(TBoldClientQueue ClientQueue)
    {
        if (ClientQueue is not null &&
            ClientHandler.HasInfoForClient(ClientQueue.BoldClientID, out string _, out int _, out int PollingInterval, out TTimeStamp _, out bool Initialized) &&
            Initialized)
        {
            TTimeStamp CurrentTime = SysUtils.DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(PollingInterval));
            fPriorityList!.Add(new TBoldClientQueueInfo(CurrentTime, ClientQueue));
        }
    }

    /// <summary>Enlists the queue of a registered client to be sent at once; the queue of a client that is gone is cleared.</summary>
    [Delphi(PortStatus.Ported)]
    public override void FlushQueue(TBoldClientQueue ClientQueue)
    {
        if (ClientQueue is not null)
        {
            if (ClientHandler.IsRegistered(ClientQueue.BoldClientID))
            {
                fPriorityList!.Add(new TBoldClientQueueInfo(default, ClientQueue));
            }
            else
            {
                ClientQueue.Clear();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldPriorityQueue PriorityList => fPriorityList!;
}

/// <summary>A client queue in the priority list, with the time it is to be sent.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientQueueInfo : TBoldPriorityQueueItem, IDisposable
{
    private TBoldClientQueue? fClientQueue;

    [Delphi(PortStatus.Ported)]
    public TBoldClientQueueInfo(TTimeStamp TimeOut, TBoldClientQueue ClientQueue)
    {
        this.TimeOut = TimeOut;
        fClientQueue = ClientQueue;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientQueueInfo.Destroy")]
    public void Dispose()
    {
        fClientQueue = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private string[]? getQueueAsVarArray() => fClientQueue!.AsVarArray();

    [Delphi(PortStatus.Ported)]
    private TBoldClientID getClientId() => fClientQueue!.BoldClientID;

    [Delphi(PortStatus.Ported)]
    private int getCount() => fClientQueue!.Count;

    /// <summary>The queue to send first is the one whose time comes first.</summary>
    [Delphi(PortStatus.Ported, Note = "Compares the whole time stamps; Delphi compared dates and times apart, wrongly across midnight.")]
    public override bool HasHigherPriorityThan(TBoldPriorityQueueItem Item) =>
        BoldUtils.TimeStampComp(TimeOut, ((TBoldClientQueueInfo)Item).TimeOut) < 0;

    [Delphi(PortStatus.Ported)]
    public TTimeStamp TimeOut { get; set; }

    [Delphi(PortStatus.Ported, Note = "A string array where Delphi had a variant.")]
    public string[]? QueueAsVarArray => getQueueAsVarArray();

    [Delphi(PortStatus.Ported)]
    public TBoldClientID ClientID => getClientId();

    [Delphi(PortStatus.Ported)]
    public int Count => getCount();
}
