// Delphi: Source/Propagator/Enterprise/BoldAdvancedPropagator.pas
// Port map: CSharp/port/units/BoldAdvancedPropagator.md
using static Bold.BoldPropagatorConstants;
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldAdvancedPropagator", Symbol = "TBoldAdvancedPropagator.StartDequeue", Note = "Showed dequeuing in the propagator's form, which is not ported; TBoldDequeuer.OnStartDequeue is there for a user interface.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldAdvancedPropagator", Symbol = "TBoldAdvancedPropagator.DoneDequeue", Note = "Showed dequeuing in the propagator's form, which is not ported; TBoldDequeuer.OnDoneDequeue is there for a user interface.")]

namespace Bold;

/// <summary>
/// The propagator: the client handler, the enqueuer and its in-queue, the dequeuer thread with the subscriptions and
/// client queues, the priority list of queues to send, the notifier threads and the lease cleanup.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldAdvancedPropagator : IDisposable
{
    private TBoldClientHandler? fClientHandler;
    private TBoldEnqueuer? fEnqueuer;
    private TBoldDequeuer? fDequeuer;
    private TBoldClientNotifierHandler? fClientNotifierHandler;
    private TBoldPriorityListEnlister? fPriorityListEnlister;
    private TBoldCleanupSubscriber? fCleanUpSubscriber;
    private bool fEnableLogging;
    private int fMaxLogFileSize;
    private int fClientNotifierPoolSize;
    private string fLogFileName = "";
    private string fErrorLogFileName = "";
    private string fThreadLogfileName = "";
    private bool fDisconnectClientsOnSendFailure = DEFAULT_DISCONNECT_CLIENTS_ON_SENDFAILURE;
    private bool fInitialized;

    [Delphi(PortStatus.Ported)]
    public TBoldAdvancedPropagator()
    {
        fInitialized = false;
        fEnableLogging = DEFAULT_ENABLELOGGING;
        fMaxLogFileSize = DEFAULT_LOGFILESIZE;
        fClientNotifierPoolSize = DEFAULT_THREADPOOLSIZE;
    }

    /// <summary>Stops the threads and frees the parts, then ends the log.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldAdvancedPropagator.Destroy")]
    public void Dispose()
    {
        if (fInitialized)
        {
            fClientHandler!.Enabled = false;
            fDequeuer!.OnStartDequeue = null;
            fDequeuer.OnDoneDequeue = null;
            fDequeuer.OnClientInfoChanged = null;
            fDequeuer.OnGlobalInfoChanged = null;
            fEnqueuer!.Enabled = false;
            SysUtils.FreeAndNil(ref fCleanUpSubscriber);
            fClientNotifierHandler!.Quit(true);
            SysUtils.FreeAndNil(ref fClientNotifierHandler);
            fDequeuer.Quit(true);
            SysUtils.FreeAndNil(ref fDequeuer);
            SysUtils.FreeAndNil(ref fEnqueuer);
            SysUtils.FreeAndNil(ref fPriorityListEnlister);
            SysUtils.FreeAndNil(ref fClientHandler);
            fInitialized = false;
        }

        BoldThreadSafeLog.BoldLogError(PropagatorConsts.sDestroying, GetType().Name);
        BoldThreadSafeLog.BoldDoneLog();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    public void SetConfigurationParams(int cClientNotifierPoolSize, bool cEnableLogging, string cLogFileName, string cErrorLogFileName,
        string cThreadLogFileName, int cMaxLogFileSize, bool cDisconnectClientsOnSendFailure)
    {
        fClientNotifierPoolSize = cClientNotifierPoolSize;
        fEnableLogging = cEnableLogging;
        fMaxLogFileSize = cMaxLogFileSize;
        fLogFileName = cLogFileName;
        fErrorLogFileName = cErrorLogFileName;
        fThreadLogfileName = cThreadLogFileName;
        fDisconnectClientsOnSendFailure = cDisconnectClientsOnSendFailure;
    }

    /// <summary>Starts the log (when enabled), creates the parts and starts the threads.</summary>
    [Delphi(PortStatus.Ported, Note = "Without the propagator's user interface manager, which is not ported.")]
    public void Initialize()
    {
        if (fEnableLogging)
        {
            BoldThreadSafeLog.BoldInitLog(fLogFileName, fErrorLogFileName, fThreadLogfileName, fMaxLogFileSize);
        }

        BoldThreadSafeLog.BoldLogError(PropagatorConsts.sInitializing, GetType().Name);
        fClientHandler = new TBoldClientHandler();
        fEnqueuer = new TBoldEnqueuer(fClientHandler);
        fDequeuer = new TBoldDequeuer(fEnqueuer.InQueue);
        fPriorityListEnlister = new TBoldPriorityListEnlister();
        fClientNotifierHandler = new TBoldClientNotifierHandler(fClientNotifierPoolSize, fClientHandler, fPriorityListEnlister.PriorityList,
            fDisconnectClientsOnSendFailure);
        fCleanUpSubscriber = new TBoldCleanupSubscriber(fClientHandler);
        fClientNotifierHandler.Resume();
        fClientNotifierHandler.WaitUntilReady(TIMEOUT);
        fDequeuer.Resume();
        fDequeuer.WaitUntilReady(TIMEOUT);
        fInitialized = true;
    }

    /// <summary>The client handler (virtual so tests can give another).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual TBoldClientHandler? getClientHandler() => fClientHandler;

    [Delphi(PortStatus.Ported)] public TBoldClientHandler? ClientHandler => getClientHandler();
    [Delphi(PortStatus.Ported)] public TBoldEnqueuer? Enqueuer => fEnqueuer;
    [Delphi(PortStatus.Ported)] public TBoldDequeuer? Dequeuer => fDequeuer;
    [Delphi(PortStatus.Ported)] public TBoldClientNotifierHandler? ClientNotifierHandler => fClientNotifierHandler;
    [Delphi(PortStatus.Ported)] public TBoldPriorityListEnlister? PriorityListEnlister => fPriorityListEnlister;
    [Delphi(PortStatus.Ported)] public TBoldCleanupSubscriber? CleanUpSubscriber => fCleanUpSubscriber;
    [Delphi(PortStatus.Ported)] public bool Initialized => fInitialized;
    [Delphi(PortStatus.Ported)] public bool DisconnectClientsOnSendFailure => fDisconnectClientsOnSendFailure;
}
