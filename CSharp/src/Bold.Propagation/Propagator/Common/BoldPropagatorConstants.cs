// Delphi: Source/Propagator/Common/BoldPropagatorConstants.pas
// Port map: CSharp/port/units/BoldPropagatorConstants.md
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPropagatorConstants", Symbol = "PBoldClientInfoRecord", Note = "A pointer type; C# passes the struct.")]

namespace Bold;

/// <summary>A client's id with its registration time and when its lease runs out.</summary>
[Delphi(PortStatus.Ported)]
public struct TBoldClientInfoRecord
{
    public TBoldClientID ClientId;
    public TTimeStamp RegistrationTime;
    public TTimeStamp LeaseTimeOut;
}

/// <summary>The propagator's thread messages, result codes, events, defaults and object names.</summary>
public static class BoldPropagatorConstants
{
    [Delphi(PortStatus.Ported)] public const int BM_PRIORITY_CHANGED = Windows.WM_USER + 1;
    [Delphi(PortStatus.Ported)] public const int BM_THRD_DOWORK = Windows.WM_USER + 2;
    [Delphi(PortStatus.Ported)] public const int BM_THRD_UNREGISTER = Windows.WM_USER + 3;
    [Delphi(PortStatus.Ported)] public const int BM_THRD_REGISTER = Windows.WM_USER + 4;
    [Delphi(PortStatus.Ported)] public const int BM_UNREGISTER_CLIENT = Windows.WM_USER + 10;
    [Delphi(PortStatus.Ported)] public const int BM_QUEUE_NOT_EMPTY = Windows.WM_USER + 12;
    [Delphi(PortStatus.Ported)] public const int BM_UNMARSHAL_INTERFACE = Windows.WM_USER + 13;
    [Delphi(PortStatus.Ported)] public const int BM_EXTEND_LEASE = Windows.WM_USER + 14;

    [Delphi(PortStatus.Ported)] public const int BM_PROPAGATOR_CLIENT_REGISTERED = Windows.WM_USER + 15;
    [Delphi(PortStatus.Ported)] public const int BM_PROPAGATOR_CLIENT_UNREGISTERED = Windows.WM_USER + 16;
    [Delphi(PortStatus.Ported)] public const int BM_PROPAGATOR_CLIENT_DISCONNECTED = Windows.WM_USER + 17;
    [Delphi(PortStatus.Ported)] public const int BM_PROPAGATOR_CLIENT_LEASE_EXTENDED = Windows.WM_USER + 18;
    [Delphi(PortStatus.Ported)] public const int BM_PROPAGATOR_CLIENT_LEASE_CHANGED = Windows.WM_USER + 20;
    [Delphi(PortStatus.Ported)] public const int BM_REMOVE_CLIENT_QUEUE = Windows.WM_USER + 21;

    [Delphi(PortStatus.Ported)] public const int BM_UPDATE_CLIENT_INFO = Windows.WM_USER + 22;
    [Delphi(PortStatus.Ported)] public const int BM_REQUEST_CLIENT_INFO = Windows.WM_USER + 23;
    [Delphi(PortStatus.Ported)] public const int BM_UPDATE_GLOBAL_INFO = Windows.WM_USER + 24;
    [Delphi(PortStatus.Ported)] public const int BM_REQUEST_GLOBAL_INFO = Windows.WM_USER + 25;

    [Delphi(PortStatus.Ported)] public const int BM_PROPAGATOR_DISPLAY_UPDATE_REQUEST = Windows.WM_USER + 26;
    [Delphi(PortStatus.Ported)] public const int BM_PROPAGATOR_DISPLAY_STARTDEQUEUE = Windows.WM_USER + 27;
    [Delphi(PortStatus.Ported)] public const int BM_PROPAGATOR_DISPLAY_DONEDEQUEUE = Windows.WM_USER + 28;

    [Delphi(PortStatus.Ported)] public const int E_ENQUEUER_NOT_ENABLED = 101;
    [Delphi(PortStatus.Ported)] public const int E_CLIENT_NOT_REGISTERED = 102;
    [Delphi(PortStatus.Ported)] public const int E_INVALID_PARAMETER = 100;
    [Delphi(PortStatus.Ported)] public const int W_CLIENT_NOT_RECEIVING = 103;

    [Delphi(PortStatus.Ported)] public const int BOLD_START_CLIENTID = 1;

    // Events that can be subscribed to.
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_UNREGISTERED = 45;
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_LIST_NOT_EMPTY = 46;
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_LIST_EMPTY = 47;
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_LEASE_CHANGED = 48;
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_REGISTERED = 49;
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_LEASE_EXPIRED = 50;
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_LEASE_EXTENDED = 51;
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_REMOVED = 52;
    [Delphi(PortStatus.Ported)] public const int BOLD_PROPAGATOR_CLIENT_CONNECTION_LOST = 53;

    // Requested events.
    [Delphi(PortStatus.Ported)] public const int breUpdateDisplay = 150;
    [Delphi(PortStatus.Ported)] public const int breClearClientQueue = 151;
    [Delphi(PortStatus.Ported)] public const int breReleaseClientLocks = 152;
    [Delphi(PortStatus.Ported)] public const int breClientRemoved = 153;

    [Delphi(PortStatus.Ported)] public const int MINIMAL_CLIENT_INFO_LIST_GROWTH = 100;
    [Delphi(PortStatus.Ported)] public const int MINIMAL_FREE_CLIENTID_COUNT = 100;
    [Delphi(PortStatus.Ported)] public const int DEFAULT_LEASE_DURATION = 1000 * 60 * 5;
    [Delphi(PortStatus.Ported)] public const int DEFAULT_EXTEND_LEASE_AFTER = 80;
    [Delphi(PortStatus.Ported)] public const int DEFAULT_POLLING_INTERVAL = 1000 * 5;
    [Delphi(PortStatus.Ported)] public const int TIMEOUT = 1000;
    [Delphi(PortStatus.Ported)] public const int DEFAULT_THREADPOOLSIZE = 15;
    [Delphi(PortStatus.Ported)] public const int DEFAULT_LOGFILESIZE = 0;
    [Delphi(PortStatus.Ported)] public const bool DEFAULT_ENABLELOGGING = false;
    [Delphi(PortStatus.Ported)] public const bool DEFAULT_DISCONNECT_CLIENTS_ON_SENDFAILURE = true;

    // Object names of the propagator's interfaces (COM export handles in Delphi).
    [Delphi(PortStatus.Ported)] public const string CLIENT_HANDLER_OBJECT_NAME = "BoldPropagatorClientHandler";
    [Delphi(PortStatus.Ported)] public const string ENQUEUER_OBJECT_NAME = "BoldPropagatorEventPropagator";
}
