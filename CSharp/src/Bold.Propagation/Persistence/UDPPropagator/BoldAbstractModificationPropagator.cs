// Delphi: Source/Persistence/UDPPropagator/BoldAbstractModificationPropagator.pas
// Port map: CSharp/port/units/BoldAbstractModificationPropagator.md

namespace Bold;

/// <summary>Called with each event a notification propagator receives.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TReceivePropagatorEvent(object? Sender, string Event);

/// <summary>The snooper of a notification propagator: it hands the events of the client's updates to the propagator to send.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldNotificationPropagatorPersistenceControllerPassthrough : TBoldAbstractSnooper
{
    private readonly TBoldClientID fClientId;

    [Delphi(PortStatus.Ported)]
    public TBoldNotificationPropagatorPersistenceControllerPassthrough(TMoldModel MoldModel, TBoldClientID ClientId)
        : base(MoldModel)
    {
        fClientId = ClientId;
    }

    /// <summary>Updates as the propagator's client.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values,
        TBoldUpdatePrecondition? Precondition, TBoldIDTranslationList? TranslationList, ref TBoldTimeStampType TimeStamp, TBoldClientID BoldClientID) =>
        base.PMUpdate(ObjectIdList, ValueSpace, Old_Values, Precondition, TranslationList, ref TimeStamp, ClientId);

    /// <summary>Hands the events to the propagator's send queue.</summary>
    [Delphi(PortStatus.Ported, Note = "Then forgets them; Delphi kept them and sent them again with the next update.")]
    public override void TransmitEvents(TBoldClientID ClientID)
    {
        try
        {
            for (int i = 0; i < Events.Count; i++)
            {
                BoldNotificationPropagator!.EnqueEvent(Events[i]);
            }
        }
        finally
        {
            ClearEvents();
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldAbstractNotificationPropagator? BoldNotificationPropagator { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldClientID ClientId => fClientId;
}

/// <summary>
/// A persistence handle that sends the events of the client's updates to the other clients itself (subclasses say
/// how) and hands the events it receives to its dequeuer, an external object space event handler. Changed classes
/// are invalidated at once.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractNotificationPropagator : TBoldPersistenceHandlePassthrough
{
    private TBoldThreadSafeStringQueue? FSendQueue;
    private TBoldThreadSafeStringQueue? FReceiveQueue;
    private TBoldThreadSafeStringQueue? FRefreshQueue;
    private readonly TBoldClientID fClientId;

    [Delphi(PortStatus.Ported)]
    protected TBoldAbstractNotificationPropagator(System.ComponentModel.IContainer? AOwner = null)
        : base(AOwner)
    {
        FReceiveQueue = new TBoldThreadSafeStringQueue("NotificationPropagator/Receive") { OnQueueNotEmpty = OnReceiveQueueNotEmpty };
        FSendQueue = new TBoldThreadSafeStringQueue("NotificationPropagator/Send") { OnQueueNotEmpty = OnSendQueueNotEmpty };
        FRefreshQueue = new TBoldThreadSafeStringQueue("NotificationPropagator/Refresh");
        fClientId = 0;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractNotificationPropagator.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Dequeuer is not null)
            {
                Dequeuer.Queue = null;
            }

            SysUtils.FreeAndNil(ref FSendQueue);
            SysUtils.FreeAndNil(ref FReceiveQueue);
            SysUtils.FreeAndNil(ref FRefreshQueue);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldNotificationPropagatorPersistenceControllerPassthrough GetController() =>
        (TBoldNotificationPropagatorPersistenceControllerPassthrough)PersistenceController;

    /// <summary>Puts an event in the send queue.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void EnqueEvent(string Event) => FSendQueue!.Enqueue(Event);

    /// <summary>Queues a received event for the dequeuer (a class change also for invalidation) and calls OnReceiveEvent.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void ReceiveEvent(string Event)
    {
        if (Dequeuer is not null)
        {
            if (Dequeuer.Queue != FReceiveQueue)
            {
                Dequeuer.Queue = FReceiveQueue;
            }

            FReceiveQueue!.Enqueue(Event);
            string AClassName = "", AMemberName = "", ALockName = "";
            if (TBoldObjectSpaceExternalEvent.DecodeExternalEvent(Event, ref AClassName, ref AMemberName, ref ALockName, null) ==
                TBoldObjectSpaceSubscriptionType.bsClassChanged)
            {
                FRefreshQueue!.Enqueue(AClassName);
            }
        }

        OnReceiveEvent?.Invoke(this, Event);
    }

    /// <summary>Invalidates the changed classes and has the dequeuer dequeue.</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void OnReceiveQueueNotEmpty(TBoldThreadSafeQueue Sender)
    {
        while (!FRefreshQueue!.Empty)
        {
            string className = FRefreshQueue.Dequeue();
            if (Dequeuer?.BoldSystemHandle?.System is { } system)
            {
                system.ClassByExpressionName(className).Invalidate();
            }
        }

        Dequeuer?.QueueNotEmpty();
    }

    /// <summary>Sends what is in the send queue; subclasses say how.</summary>
    [Delphi(PortStatus.Ported)]
    protected abstract void OnSendQueueNotEmpty(TBoldThreadSafeQueue Sender);

    [Delphi(PortStatus.Ported)] protected TBoldExternalObjectSpaceEventHandler? Dequeuer { get; set; }
    [Delphi(PortStatus.Ported)] protected TReceivePropagatorEvent? OnReceiveEvent { get; set; }
    [Delphi(PortStatus.Ported)] protected TBoldThreadSafeStringQueue SendQueue => FSendQueue!;
    [Delphi(PortStatus.Ported)] protected TBoldNotificationPropagatorPersistenceControllerPassthrough Controller => GetController();

    [Delphi(PortStatus.Ported)]
    protected override TBoldPersistenceController CreatePersistenceController()
    {
        var TempController = new TBoldNotificationPropagatorPersistenceControllerPassthrough(SystemHandle!.SystemTypeInfoHandle!.BoldModel!.MoldModel!, ClientId);
        ChainPersistenceController(TempController);
        TempController.BoldNotificationPropagator = this;
        return TempController;
    }

    [Delphi(PortStatus.Ported)] protected TBoldSystemHandle? SystemHandle { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldClientID ClientId => fClientId;
}

/// <summary>Constants of BoldAbstractModificationPropagator.</summary>
public static class BoldAbstractModificationPropagator
{
    /// <summary>The signature sent with every propagated event.</summary>
    [Delphi(PortStatus.Ported)] public const string SPropagatorIdentification = "#BOLDEVENT#\r\n";
}
