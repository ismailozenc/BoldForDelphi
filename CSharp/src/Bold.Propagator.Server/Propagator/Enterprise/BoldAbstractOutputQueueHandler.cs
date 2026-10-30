// Delphi: Source/Propagator/Enterprise/BoldAbstractOutputQueueHandler.pas
// Port map: CSharp/port/units/BoldAbstractOutputQueueHandler.md

namespace Bold;

/// <summary>Queues the events to send to each client.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractOutputQueueHandler
{
    [Delphi(PortStatus.Ported)]
    public abstract void SendEvent(TBoldClientID ClientID, string EventName);

    [Delphi(PortStatus.Ported)]
    public abstract void ClearQueueForClient(TBoldClientID ClientID);

    [Delphi(PortStatus.Ported)]
    public abstract int QueueCountForClient(TBoldClientID ClientID);

    /// <summary>Queues the event and has the client's queue sent at once.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void SendEventAndFlushQueue(TBoldClientID ClientID, string EventName);
}
