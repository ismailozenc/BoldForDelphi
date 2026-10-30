// Delphi: Source/Propagator/Common/PropagatorConsts.pas
// Port map: CSharp/port/units/PropagatorConsts.md

namespace Bold;

/// <summary>The propagator's messages (Delphi resource strings), in Delphi's Format syntax.</summary>
public static class PropagatorConsts
{
    [Delphi(PortStatus.Ported)] public const string sLogError = "%s.%s Error: %s)";
    [Delphi(PortStatus.Ported)] public const string sClientIDsNotAssigned = "%s.%s: ClientIds is not assigned";

    // BoldAdvancedPropagator.
    [Delphi(PortStatus.Ported)] public const string sInitializing = "%s.Initialize: Starting..........................................";
    [Delphi(PortStatus.Ported)] public const string sDestroying = "%s.Destroy: Closing down peacefully..........................................";

    // BoldClientHandler.
    [Delphi(PortStatus.Ported)] public const string sLogErrorAndID = "%s.%s Error: [ID=%d] %s)";
    [Delphi(PortStatus.Ported)] public const string sLoggingID = "Log In: %s [ID=%d]";
    [Delphi(PortStatus.Ported)] public const string sExtendLeaseFailed = "ExtendLease failed: [ID=%d] Client already disconnected ";
    [Delphi(PortStatus.Ported)] public const string sLogOff = "Log Off: %s [ID=%d] [Pkg: %d Ev: %d Int: %s Login: %s (%s ago) Status: %s]";
    [Delphi(PortStatus.Ported)] public const string sLeaseExpired = "Lease Expired: %s [ID=%d] [Pkg: %d Ev: %d Int: %s Last: %s ago Login: %s (%s ago) Status: %s]";
    [Delphi(PortStatus.Ported)] public const string sClientDisconnected = "Disconnected: %s [ID=%d] Login: %s (%s ago) Status: %s";
    [Delphi(PortStatus.Ported)] public const string sClientDisconnected_Long = "Disconnected: %s [ID=%d] [Pkg: %d Ev: %d Int: %s Last: %s ago Login: %s (%s) Status: %s]";
    [Delphi(PortStatus.Ported)] public const string sErrorCode = "  ErrorCode=%d";
    [Delphi(PortStatus.Ported)] public const string sInvalidListener = "%s.ValidateClientInterface: ClientListener not valid for Client %s; Error=%s";
    [Delphi(PortStatus.Ported)] public const string sListenerInterfaceMissing = "%s.ValidateClientInterface Error: [ID=%s] Listener interface missing";
    [Delphi(PortStatus.Ported)] public const string sClientHasRecovered = "!!! Client %s (%d) has recovered and received %d messages";
    [Delphi(PortStatus.Ported)] public const string sOK = "OK";
    [Delphi(PortStatus.Ported)] public const string sNotReceiving = "Not Receiving";
    [Delphi(PortStatus.Ported)] public const string sRecovered = "Recovered";
    [Delphi(PortStatus.Ported)] public const string sUnknown = "<Unknown>";

    // BoldClientNotifierHandler.
    [Delphi(PortStatus.Ported)] public const string sFailedToSendToClient = "%s.SendToClient: could not send events to client";
    [Delphi(PortStatus.Ported)] public const string sDisconnectMsg = " [Disconnected]";
    [Delphi(PortStatus.Ported)] public const string sClientFailure = "%s.Execute: Client %s (ID=%d)%s failed to receive messages (%d msgs): %s. RegistrationTime: %s (%s ago), LeaseTimeout: %s (%s left)";

    // BoldLockManagerAdmin.
    [Delphi(PortStatus.Ported)] public const string sLocksNotAssigned = "%s.LocksForClients: Locks is not assigned";
    [Delphi(PortStatus.Ported)] public const string sLockDurationNotAssigned = "%s.LocksForClients: LockDurations is not assigned";

    // BoldPropagatorCleanup.
    [Delphi(PortStatus.Ported)] public const string sWindowProcError = "Error in WindowProc %s";

    // BoldPropagatorMainForm.
    [Delphi(PortStatus.Ported)] public const string sFormCaption = "%s Console";
    [Delphi(PortStatus.Ported)] public const string sClientID = "ClientID";
    [Delphi(PortStatus.Ported)] public const string sName = "Name";
    [Delphi(PortStatus.Ported)] public const string sRegTime = "RegTime";
    [Delphi(PortStatus.Ported)] public const string sTimeOut = "Timeout";
    [Delphi(PortStatus.Ported)] public const string sSubscriptions = "Subscriptions";
    [Delphi(PortStatus.Ported)] public const string sQueue = "Queue";
    [Delphi(PortStatus.Ported)] public const string sLongestInt = "Longest Int";
    [Delphi(PortStatus.Ported)] public const string sLast = "Last (ago)";
    [Delphi(PortStatus.Ported)] public const string sPkg = "Pkg";
    [Delphi(PortStatus.Ported)] public const string sEv = "Ev";
    [Delphi(PortStatus.Ported)] public const string sStatus = "Status";
    [Delphi(PortStatus.Ported)] public const string sLost = "Lost";
    [Delphi(PortStatus.Ported)] public const string sFile = "File: %s";
    [Delphi(PortStatus.Ported)] public const string sNoINIFile = "INI file: N/A";
    [Delphi(PortStatus.Ported)] public const string sINIFile = "INI file:";
    [Delphi(PortStatus.Ported)] public const string sStarted = "Started: %s";
    [Delphi(PortStatus.Ported)] public const string sUptime = "Uptime: %d days %s";
    [Delphi(PortStatus.Ported)] public const string sHangon = "Hang on...";
    [Delphi(PortStatus.Ported)] public const string sCli = "Cli";
    [Delphi(PortStatus.Ported)] public const string sTot = "Tot";
    [Delphi(PortStatus.Ported)] public const string sSubs = "Subs";
    [Delphi(PortStatus.Ported)] public const string sInQ = "InQ";
    [Delphi(PortStatus.Ported)] public const string sInQPeak = "InQ^";
    [Delphi(PortStatus.Ported)] public const string sOutQ = "OutQ";
    [Delphi(PortStatus.Ported)] public const string sAdded = "Added";
    [Delphi(PortStatus.Ported)] public const string sSent = "Sent";
    [Delphi(PortStatus.Ported)] public const string sBytesPerSubscription = "%0.2f bytes per subscription";
    [Delphi(PortStatus.Ported)] public const string sMemAllocated = "Allocated %6.2f Mb";
    [Delphi(PortStatus.Ported)] public const string sRefresh = "Refresh";
    [Delphi(PortStatus.Ported)] public const string sDL = "DL?";

    // BoldPropagatorServer.
    [Delphi(PortStatus.Ported)] public const string sInvalidCommandLineArgs = "Invalid command line argument %s";
    [Delphi(PortStatus.Ported)] public const string sAppIDNotFound = "AppId not found, run DCOMCNFG to add appid to registry";

    // BoldPropagatorUIManager.
    [Delphi(PortStatus.Ported)] public const string sPropagatorMustBeShutDown = "Propagator must be shut down";

    // BoldSubscriptionHandler.
    [Delphi(PortStatus.Ported)] public const string sOutputQueueHandlerNotAssigned = "%s.getQueueHandler: AbstractOutputQueueHandler is not assigned.";
}
