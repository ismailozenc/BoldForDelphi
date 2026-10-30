// Delphi: Source/Propagator/Enterprise/BoldOutputQueueHandler.pas
// Port map: CSharp/port/units/BoldOutputQueueHandler.md

namespace Bold;

/// <summary>Sizes of the client queue array.</summary>
public static class BoldOutputQueueHandler
{
    [Delphi(PortStatus.Ported)] public const int INITIAL_ARRAY_SIZE = 100;
    [Delphi(PortStatus.Ported)] public const int ArrayIncrement = 100;
}

/// <summary>
/// A queue of events per client. When a client's queue gets its first event, the queue is enlisted in the priority
/// list, to be sent after the client's polling interval.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldOutputQueueHandler : TBoldAbstractOutputQueueHandler, IDisposable
{
    private TBoldObjectArray? fOutputQueues;

    [Delphi(PortStatus.Ported)]
    public TBoldOutputQueueHandler()
    {
        fOutputQueues = new TBoldObjectArray(BoldOutputQueueHandler.INITIAL_ARRAY_SIZE,
            TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner, TBoldContainerOption.bcoThreadSafe));
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldOutputQueueHandler.Destroy")]
    public void Dispose()
    {
        SysUtils.FreeAndNil(ref fOutputQueues);
        GC.SuppressFinalize(this);
    }

    /// <summary>The client's queue, created the first time.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldClientQueue getOutputQueue(TBoldClientID Index)
    {
        lock (fOutputQueues!)
        {
            if (Index >= fOutputQueues.Count)
            {
                int count = Index - fOutputQueues.Count + 1;
                if (count < BoldOutputQueueHandler.ArrayIncrement)
                {
                    count = BoldOutputQueueHandler.ArrayIncrement;
                }

                for (int i = 0; i <= count; i++)
                {
                    fOutputQueues.Add(null);
                }
            }

            if (fOutputQueues[Index] is not TBoldClientQueue ClientQueue)
            {
                ClientQueue = new TBoldClientQueue(SysUtils.Format("CliQ[%d]", Index))
                {
                    OnQueueNotEmpty = OnQueueNotEmpty,
                    BoldClientID = Index,
                };
                fOutputQueues[Index] = ClientQueue;
            }

            return ClientQueue;
        }
    }

    /// <summary>The propagator's priority list enlister (virtual so tests can give another).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldAbstractPriorityListEnlister? getPriorityListEnlister() =>
        TBoldPropagatorServer.Instance().AdvancedPropagator?.PriorityListEnlister;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractPriorityListEnlister? PriorityListEnlister => getPriorityListEnlister();

    [Delphi(PortStatus.Ported)]
    public override void ClearQueueForClient(TBoldClientID ClientID) => OutputQueues(ClientID).Clear();

    [Delphi(PortStatus.Ported)]
    public override int QueueCountForClient(TBoldClientID ClientID) => OutputQueues(ClientID).Count;

    [Delphi(PortStatus.Ported)]
    public override void SendEvent(TBoldClientID ClientID, string EventName)
    {
        try
        {
            OutputQueues(ClientID).Enqueue(EventName);
        }
        catch (Exception)
        {
            BoldThreadSafeLog.BoldLogError("%s.SendEvent: Client = %d", GetType().Name, ClientID);
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void SendEventAndFlushQueue(TBoldClientID ClientID, string EventName)
    {
        OutputQueues(ClientID).Enqueue(EventName);
        PriorityListEnlister?.FlushQueue(OutputQueues(ClientID));
    }

    /// <summary>A client queue got its first event: it is enlisted to be sent.</summary>
    [Delphi(PortStatus.Ported)]
    public void OnQueueNotEmpty(TBoldThreadSafeQueue Queue)
    {
        try
        {
            PriorityListEnlister?.EnlistQueue((TBoldClientQueue)Queue);
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError("%s.OnQueueNotEmpty: %s", GetType().Name, E.Message);
        }
    }

    [Delphi(PortStatus.Ported, Note = "A method: C# has no indexed properties with a name.")]
    public TBoldClientQueue OutputQueues(TBoldClientID Index) => getOutputQueue(Index);
}
