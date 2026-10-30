// Delphi: Source/Propagator/Enterprise/BoldClientHandler.pas
// Port map: CSharp/port/units/BoldClientHandler.md
using static Bold.BoldPropagatorConstants;
using static Bold.PropagatorConsts;
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldClientHandler", Symbol = "IGlobalInterfaceTable", Note = "COM's global interface table; the client info keeps the listener reference.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldClientHandler", Symbol = "IGlobalInterfaceTable.RegisterInterfaceInGlobal", Note = "COM's global interface table.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldClientHandler", Symbol = "IGlobalInterfaceTable.RevokeInterfaceFromGlobal", Note = "COM's global interface table.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldClientHandler", Symbol = "IGlobalInterfaceTable.GetInterfaceFromGlobal", Note = "COM's global interface table.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldClientHandler", Symbol = "GlobalInterfaceTable", Note = "COM's global interface table.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldRemoveClientReasonType { brcrUnregister, brcrConnectionLost, brcrLeaseExpired, brcrAdministrator }

[Delphi(PortStatus.Ported)]
public enum TBoldClientReceiveStatus { crsOK, crsNotReceiving, crsRecovered }

/// <summary>
/// The propagator's clients: registers them for a lease, extends and ends leases, and keeps each client's listener
/// and statistics. Every method is safe to call from any thread. It publishes BOLD_PROPAGATOR_CLIENT_ events (with the
/// client id as argument) for the cleanup, the lock manager and the dequeuer.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientHandler : TBoldPublisher, IDisposable
{
    private TBoldClientInfoList? fClientInfoList;
    private int fNOConnectedClients;
    private bool fEnabled;
    private DateTime fLockTime;
    private TBoldLoggableCriticalSection? FClientHandlerLock;
    private TCriticalSection? fLockTimeLock;
    private int fTotalLostEvents;
    private int fTotalClients;
    private int fPeakClients;

    [Delphi(PortStatus.Ported)]
    public TBoldClientHandler()
    {
        fClientInfoList = new TBoldClientInfoList();
        FClientHandlerLock = new TBoldLoggableCriticalSection("CH");
        fLockTimeLock = new TCriticalSection();
        fNOConnectedClients = 0;
        fLockTime = default;
        AcquireLock();
        try
        {
            fEnabled = true;
        }
        finally
        {
            ReleaseLock();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientHandler.Destroy")]
    public new void Dispose()
    {
        if (FClientHandlerLock is null)
        {
            return;
        }

        try
        {
            NotifySubscribersAndClearSubscriptions(this);
            AcquireLock();
            try
            {
                fEnabled = false;
                SysUtils.FreeAndNil(ref fClientInfoList);
            }
            finally
            {
                ReleaseLock();
            }

            FClientHandlerLock = null;
            fLockTimeLock = null;
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "Destroy", E.Message);
        }

        base.Dispose();
    }

    /// <summary>The client's info, when the handler is enabled and has it.</summary>
    [Delphi(PortStatus.Ported)]
    private bool InfoForClient(TBoldClientID ClientID, out TBoldClientInfo? BoldClientInfo)
    {
        BoldClientInfo = null;
        bool result = Enabled;
        try
        {
            if (result)
            {
                BoldClientInfo = fClientInfoList!.GetExistingClientInfo(ClientID);
                result = BoldClientInfo is not null;
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogErrorAndID, GetType().Name, "InfoForClient", ClientID, E.Message);
        }

        return result;
    }

    /// <summary>Registers a client: gives it a free client id and its registration time. S_OK, or S_FALSE when it failed.</summary>
    [Delphi(PortStatus.Ported)]
    public int RegisterClient(int LeaseDuration, int PollingInterval, IBoldListener BoldClientListener, string ClientIDString,
        out int BoldClientID, out TTimeStamp RegistrationTime)
    {
        BoldClientID = BoldDefs.NOTVALIDCLIENTID;
        RegistrationTime = default;
        int result = Windows.S_FALSE;
        if (!Enabled)
        {
            return result;
        }

        AcquireLock();
        try
        {
            try
            {
                AddClient(LeaseDuration, PollingInterval, BoldClientListener, ClientIDString, out BoldClientID);
                RegistrationTime = fClientInfoList![BoldClientID]!.RegistrationTime;
                SendExtendedEvent(this, BOLD_PROPAGATOR_CLIENT_REGISTERED, [BoldClientID]);
                NotifyLeaseChanged();
                BoldThreadSafeLog.BoldLog(sLoggingID, ClientIDString, BoldClientID);
                fTotalClients++;
                if (NOConnectedClients > fPeakClients)
                {
                    fPeakClients = NOConnectedClients;
                }

                result = Windows.S_OK;
            }
            catch (Exception E)
            {
                BoldThreadSafeLog.BoldLogError(sLogErrorAndID, GetType().Name, "RegisterClient", BoldClientID, E.Message);
            }
        }
        finally
        {
            ReleaseLock();
        }

        return result;
    }

    /// <summary>Extends the client's lease by <paramref name="LeaseDuration"/> milliseconds from now.</summary>
    [Delphi(PortStatus.Ported)]
    public int ExtendLease(int BoldClientID, int LeaseDuration, out bool ExtensionOK)
    {
        int result = Windows.S_FALSE;
        ExtensionOK = false;
        if (!Enabled)
        {
            return result;
        }

        try
        {
            AcquireLock();
            try
            {
                if (InfoForClient(BoldClientID, out TBoldClientInfo? ClientInfo) && ClientInfo!.Initialized)
                {
                    ClientInfo.ReInitializeListenerInterface();
                    ClientInfo.ExtendLease(LeaseDuration);
                    ExtensionOK = true;
                    result = Windows.S_OK;
                    NotifyLeaseChanged();
                }
                else
                {
                    BoldThreadSafeLog.BoldLog(sExtendLeaseFailed, BoldClientID);
                }
            }
            finally
            {
                ReleaseLock();
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogErrorAndID, GetType().Name, "ExtendLease", BoldClientID, E.Message);
        }

        return result;
    }

    /// <summary>Unregisters the client registered at <paramref name="RegistrationTime"/> (a later client with the same id is left alone).</summary>
    [Delphi(PortStatus.Ported)]
    public int UnRegisterClient(int BoldClientID, TTimeStamp RegistrationTime)
    {
        int result = Windows.S_FALSE;
        if (!Enabled)
        {
            return result;
        }

        try
        {
            AcquireLock();
            try
            {
                if (InfoForClient(BoldClientID, out TBoldClientInfo? ClientInfo) && ClientInfo!.Initialized &&
                    BoldUtils.TimeStampComp(ClientInfo.RegistrationTime, RegistrationTime) == 0)
                {
                    string IdString = ClientInfo.ClientIdentifierString;
                    if (InternalRemoveClient(BoldClientID))
                    {
                        result = Windows.S_OK;
                        SendExtendedEvent(this, BOLD_PROPAGATOR_CLIENT_UNREGISTERED, [BoldClientID]);
                        NotifyLeaseChanged();
                        BoldThreadSafeLog.BoldLog(sLogOff, IdString, BoldClientID, ClientInfo.fSuccessfullyReceivedCount,
                            ClientInfo.fSuccessfullyReceivedEventsCount, IntervalToStr(ClientInfo.fLongestIntervalBetweenEvents),
                            SysUtils.DateTimeToStr(SysUtils.TimeStampToDateTime(ClientInfo.RegistrationTime)),
                            IntervalToStr(DateTime.Now - SysUtils.TimeStampToDateTime(ClientInfo.RegistrationTime)),
                            ClientInfo.ClientStatusString);
                    }
                }
            }
            finally
            {
                ReleaseLock();
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogErrorAndID, GetType().Name, "UnRegisterClient", BoldClientID, E.Message);
        }

        return result;
    }

    /// <summary>Uninitializes the client's info, frees its id and has its queue removed.</summary>
    [Delphi(PortStatus.Ported)]
    private bool InternalRemoveClient(TBoldClientID BoldClientId)
    {
        bool result = false;
        if (InfoForClient(BoldClientId, out TBoldClientInfo? ClientInfo) && ClientInfo!.Initialized)
        {
            result = true;
            ClientInfo.UnInitialize();
            fClientInfoList!.ReturnFreeClientID(BoldClientId);
            NOConnectedClients--;
            SendExtendedEvent(this, BOLD_PROPAGATOR_CLIENT_REMOVED, [BoldClientId]);
            EnqueueRemoveClientQueueEvent(BoldClientId);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private void AddClient(int LeaseDuration, int PollingInterval, IBoldListener BoldClientListener, string ClientIDString, out int ClientId)
    {
        ClientId = BoldDefs.NOTVALIDCLIENTID;
        try
        {
            TBoldClientID NewClientID = fClientInfoList!.GetFreeClientID();
            TBoldClientInfo? NewClientInfo = fClientInfoList[NewClientID];
            if (NewClientInfo is null)
            {
                NewClientInfo = new TBoldClientInfo(NewClientID);
                fClientInfoList[NewClientID] = NewClientInfo;
            }

            NewClientInfo.Initialize(LeaseDuration, PollingInterval, BoldClientListener, ClientIDString);
            ClientId = NewClientID;
            NOConnectedClients++;
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "AddClient", E.Message);
        }
    }

    /// <summary>Removes the client registered at <paramref name="RegistrationTime"/> if its lease has run out.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveExpiredLease(TBoldClientID ClientID, TTimeStamp RegistrationTime)
    {
        if (!Enabled)
        {
            return;
        }

        AcquireLock();
        try
        {
            InfoForClient(ClientID, out TBoldClientInfo? ClientInfo);
            if (ClientInfo is not null && ClientInfo.Initialized &&
                BoldUtils.TimeStampComp(ClientInfo.RegistrationTime, RegistrationTime) == 0 && ClientInfo.LeaseIsExpired())
            {
                string IdString = ClientInfo.ClientIdentifierString;
                InternalRemoveClient(ClientID);
                SendExtendedEvent(this, BOLD_PROPAGATOR_CLIENT_LEASE_EXPIRED, [ClientID]);
                BoldThreadSafeLog.BoldLog(sLeaseExpired, IdString, ClientID, ClientInfo.fSuccessfullyReceivedCount,
                    ClientInfo.fSuccessfullyReceivedEventsCount, IntervalToStr(ClientInfo.fLongestIntervalBetweenEvents),
                    IntervalToStr(DateTime.Now - ClientInfo.fLastSuccessfullReceive),
                    SysUtils.DateTimeToStr(SysUtils.TimeStampToDateTime(ClientInfo.RegistrationTime)),
                    IntervalToStr(DateTime.Now - SysUtils.TimeStampToDateTime(ClientInfo.RegistrationTime)),
                    ClientInfo.ClientStatusString);
                NotifyLeaseChanged();
            }
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>Adds "id=name" for each registered client.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetRegisteredClientIDs(TStringList? ClientIds)
    {
        AcquireLock();
        try
        {
            if (ClientIds is not null)
            {
                for (int i = 0; i < fClientInfoList!.Count; i++)
                {
                    if (fClientInfoList[i] is { Initialized: true } info)
                    {
                        ClientIds.Add(SysUtils.Format("%d=%s", info.ClientID, info.ClientIdentifierString));
                    }
                }
            }
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>Removes the client registered at <paramref name="RegistrationTime"/>, as one whose connection is lost.</summary>
    [Delphi(PortStatus.Ported)]
    public bool DisconnectClient(TBoldClientID BoldClientId, TTimeStamp RegistrationTime)
    {
        bool result = false;
        if (!Enabled)
        {
            return result;
        }

        try
        {
            AcquireLock();
            try
            {
                if (InfoForClient(BoldClientId, out TBoldClientInfo? ClientInfo) && ClientInfo!.Initialized &&
                    BoldUtils.TimeStampComp(ClientInfo.RegistrationTime, RegistrationTime) == 0)
                {
                    string IdString = ClientInfo.ClientIdentifierString;
                    if (InternalRemoveClient(BoldClientId))
                    {
                        result = true;
                        SendExtendedEvent(this, BOLD_PROPAGATOR_CLIENT_CONNECTION_LOST, [BoldClientId]);
                        NotifyLeaseChanged();
                        if (ClientInfo.fSuccessfullyReceivedCount == 0)
                        {
                            BoldThreadSafeLog.BoldLog(sClientDisconnected, IdString, BoldClientId,
                                SysUtils.DateTimeToStr(SysUtils.TimeStampToDateTime(ClientInfo.RegistrationTime)),
                                IntervalToStr(DateTime.Now - SysUtils.TimeStampToDateTime(ClientInfo.RegistrationTime)),
                                ClientInfo.ClientStatusString);
                        }
                        else
                        {
                            BoldThreadSafeLog.BoldLog(sClientDisconnected_Long, IdString, BoldClientId, ClientInfo.fSuccessfullyReceivedCount,
                                ClientInfo.fSuccessfullyReceivedEventsCount, IntervalToStr(ClientInfo.fLongestIntervalBetweenEvents),
                                IntervalToStr(DateTime.Now - ClientInfo.fLastSuccessfullReceive),
                                SysUtils.DateTimeToStr(SysUtils.TimeStampToDateTime(ClientInfo.RegistrationTime)),
                                IntervalToStr(DateTime.Now - SysUtils.TimeStampToDateTime(ClientInfo.RegistrationTime)),
                                ClientInfo.ClientStatusString);
                        }
                    }
                }
            }
            finally
            {
                ReleaseLock();
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogErrorAndID, GetType().Name, "DisconnectClient", BoldClientId, E.Message);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool GetRegistrationTime(TBoldClientID ClientID, out TTimeStamp RegistrationTime)
    {
        RegistrationTime = default;
        bool result = false;
        try
        {
            AcquireLock();
            try
            {
                result = InfoForClient(ClientID, out TBoldClientInfo? ClientInfo) && ClientInfo!.Initialized;
                if (result)
                {
                    RegistrationTime = ClientInfo!.RegistrationTime;
                }
            }
            finally
            {
                ReleaseLock();
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogErrorAndID, GetType().Name, "GetRegistrationTime", ClientID, E.Message);
        }

        return result;
    }

    /// <summary>
    /// Adds the totals (TotalClients, PeakClients, TotalLostEvents) and then, for each registered client, a comma text
    /// of ID, IDString, RegistrationTime, LeaseTimeout, Packages, Events, LastSend, LongestInterval, LostEvents, Status.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void GetRegisteredClientInfos(TStringList ClientInfo)
    {
        if (!Enabled)
        {
            return;
        }

        AcquireLock();
        try
        {
            var temp = new TStringList();
            temp.Add("TotalClients=" + SysUtils.IntToStr(fTotalClients));
            temp.Add("PeakClients=" + SysUtils.IntToStr(fPeakClients));
            temp.Add("TotalLostEvents=" + SysUtils.IntToStr(fTotalLostEvents));
            ClientInfo.Add(temp.CommaText);
            for (int i = 0; i < fClientInfoList!.Count; i++)
            {
                if (fClientInfoList[i] is { Initialized: true } ClientInfoItem)
                {
                    temp.Clear();
                    temp.Add("ID=" + SysUtils.IntToStr(ClientInfoItem.ClientID));
                    temp.Add("IDString=" + ClientInfoItem.ClientIdentifierString);
                    temp.Add("RegistrationTime=" + SysUtils.FormatDateTime("yyyy-mm-dd hh:nn:ss", SysUtils.TimeStampToDateTime(ClientInfoItem.RegistrationTime)));
                    temp.Add("LeaseTimeout=" + SysUtils.FormatDateTime("yyyy-mm-dd hh:nn:ss", SysUtils.TimeStampToDateTime(ClientInfoItem.LeaseTimeOut)));
                    temp.Add("Packages=" + SysUtils.IntToStr(ClientInfoItem.fSuccessfullyReceivedCount));
                    temp.Add("Events=" + SysUtils.IntToStr(ClientInfoItem.fSuccessfullyReceivedEventsCount));
                    temp.Add("LastSend=" + SysUtils.FormatDateTime("yyyy-mm-dd hh:nn:ss", ClientInfoItem.fLastSuccessfullReceive));
                    temp.Add("LongestInterval=" + SysUtils.FormatDateTime("yyyy-mm-dd hh:nn:ss", DelphiDateTimeZero + ClientInfoItem.fLongestIntervalBetweenEvents));
                    temp.Add("LostEvents=" + SysUtils.IntToStr(ClientInfoItem.fLostEvents));
                    temp.Add("Status=" + ClientInfoItem.ClientStatusString);
                    ClientInfo.Add(temp.CommaText);
                }
            }
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>Disabling drops every client.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetEnabled(bool Value)
    {
        AcquireLock();
        try
        {
            if (Value != fEnabled)
            {
                fEnabled = Value;
                if (!fEnabled)
                {
                    fClientInfoList?.Clear();
                }
            }
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>The registered client whose lease runs out first; false when there is none.</summary>
    [Delphi(PortStatus.Ported, Note = "Returns true when there is such a client; Delphi never set its result to true.")]
    public bool IsThereAClientTimingOutSoon(out TBoldClientID ClientId, out TTimeStamp RegistrationTime, out TTimeStamp LeaseTimeOut)
    {
        ClientId = BoldDefs.NOTVALIDCLIENTID;
        RegistrationTime = default;
        LeaseTimeOut = default;
        bool result = false;
        if (!Enabled)
        {
            return result;
        }

        try
        {
            AcquireLock();
            try
            {
                TBoldClientInfo? CurrentClientInfo = GetFirstLeaseTimeOutClient();
                if (CurrentClientInfo is not null)
                {
                    ClientId = CurrentClientInfo.ClientID;
                    RegistrationTime = CurrentClientInfo.RegistrationTime;
                    LeaseTimeOut = CurrentClientInfo.LeaseTimeOut;
                    result = true;
                }
            }
            finally
            {
                ReleaseLock();
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "IsThereAClientTimingOutSoon", E.Message);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private void SetNOConnectedClients(int Value)
    {
        AcquireLock();
        try
        {
            fNOConnectedClients = Value;
        }
        finally
        {
            ReleaseLock();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientHandler.HasInfoForClient(TBoldClientId,string,integer,integer,TTimeStamp,Boolean)")]
    public bool HasInfoForClient(TBoldClientID ClientId, out string ClientIdentifierString, out int LeaseDuration, out int PollingInterval,
        out TTimeStamp LeaseTimeOut, out bool Initialized)
    {
        ClientIdentifierString = "";
        LeaseDuration = 0;
        PollingInterval = 0;
        LeaseTimeOut = default;
        Initialized = false;
        if (!Enabled)
        {
            return false;
        }

        AcquireLock();
        try
        {
            bool result = InfoForClient(ClientId, out TBoldClientInfo? ClientInfo);
            if (result)
            {
                ClientIdentifierString = ClientInfo!.ClientIdentifierString;
                LeaseDuration = ClientInfo.LeaseDuration;
                PollingInterval = ClientInfo.PollingInterval;
                LeaseTimeOut = ClientInfo.LeaseTimeOut;
                Initialized = ClientInfo.Initialized;
            }

            return result;
        }
        finally
        {
            ReleaseLock();
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientHandler.HasInfoForClient(TBoldClientId,string,TTimeStamp,Boolean,TBoldClientReceiveStatus)")]
    public bool HasInfoForClient(TBoldClientID ClientId, out string ClientIdentifierString, out TTimeStamp RegistrationTime, out bool Initialized,
        out TBoldClientReceiveStatus Status)
    {
        ClientIdentifierString = "";
        RegistrationTime = default;
        Initialized = false;
        Status = TBoldClientReceiveStatus.crsOK;
        if (!Enabled)
        {
            return false;
        }

        AcquireLock();
        try
        {
            bool result = InfoForClient(ClientId, out TBoldClientInfo? ClientInfo);
            if (result)
            {
                ClientIdentifierString = ClientInfo!.ClientIdentifierString;
                RegistrationTime = ClientInfo.RegistrationTime;
                Initialized = ClientInfo.Initialized;
                Status = ClientInfo.fClientStatus;
            }

            return result;
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>Tells the subscribers (the cleanup thread) which client's lease runs out first.</summary>
    [Delphi(PortStatus.Ported)]
    private void NotifyLeaseChanged()
    {
        TBoldClientInfo? TimeOutClient = GetFirstLeaseTimeOutClient();
        if (TimeOutClient is not null)
        {
            double RegistrationTime = SysUtils.TimeStampToMSecs(TimeOutClient.RegistrationTime);
            double LeaseTimeOut = SysUtils.TimeStampToMSecs(TimeOutClient.LeaseTimeOut);
            SendExtendedEvent(this, BOLD_PROPAGATOR_CLIENT_LEASE_CHANGED, [TimeOutClient.ClientID, RegistrationTime, LeaseTimeOut]);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldClientInfo? GetFirstLeaseTimeOutClient()
    {
        TBoldClientInfo? result = null;
        if (!Enabled)
        {
            return result;
        }

        try
        {
            if (NOConnectedClients != 0)
            {
                for (int i = 0; i < fClientInfoList!.Count; i++)
                {
                    if (fClientInfoList[i] is { Initialized: true } aTemp)
                    {
                        result ??= aTemp;
                        if (BoldUtils.TimeStampComp(result.LeaseTimeOut, aTemp.LeaseTimeOut) == 1)
                        {
                            result = aTemp;
                        }
                    }
                }
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogError, GetType().Name, "GetFirstLeaseTimeOutClient", E.Message);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public bool IsRegistered(TBoldClientID ClientId)
    {
        if (!Enabled)
        {
            return false;
        }

        AcquireLock();
        try
        {
            TBoldClientInfoList list = fClientInfoList!;
            return ClientId >= 0 && ClientId < list.fList!.Count && list.fList[ClientId] is TBoldClientInfo { Initialized: true };
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>Asks the dequeuer to drop the client's queue and subscriptions (virtual so tests can do without a propagator).</summary>
    [Delphi(PortStatus.Ported)]
    protected virtual void EnqueueRemoveClientQueueEvent(TBoldClientID ClientId)
    {
        AcquireLock();
        try
        {
            if (fEnabled)
            {
                TBoldPropagatorServer.Instance().AdvancedPropagator!.Dequeuer!.EnqueueRemoveClientQueueEvent(ClientId);
            }
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>The listener of the client registered at <paramref name="RegistrationTime"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public bool GetListener(TBoldClientID ClientId, TTimeStamp RegistrationTime, out IBoldListener? obj)
    {
        obj = null;
        if (!Enabled)
        {
            return false;
        }

        AcquireLock();
        try
        {
            bool result = InfoForClient(ClientId, out TBoldClientInfo? ClientInfo) &&
                BoldUtils.TimeStampComp(ClientInfo!.RegistrationTime, RegistrationTime) == 0 && ClientInfo.Initialized;
            if (result)
            {
                obj = ClientInfo!.fBoldListener;
            }

            return result;
        }
        finally
        {
            ReleaseLock();
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetEnabled()
    {
        AcquireLock();
        try
        {
            return fEnabled;
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>Adds the events to the client's statistics; a client that was not receiving has recovered.</summary>
    [Delphi(PortStatus.Ported)]
    public void MarkHasReceivedEvents(TBoldClientID ClientID, int EventCount)
    {
        if (!Enabled)
        {
            return;
        }

        AcquireLock();
        try
        {
            if (fClientInfoList![ClientID] is { } ClientInfo)
            {
                ClientInfo.fSuccessfullyReceivedEventsCount += EventCount;
                ClientInfo.fSuccessfullyReceivedCount++;
                DateTime now = DateTime.Now;
                if (now - ClientInfo.fLastSuccessfullReceive > ClientInfo.fLongestIntervalBetweenEvents)
                {
                    ClientInfo.fLongestIntervalBetweenEvents = now - ClientInfo.fLastSuccessfullReceive;
                }

                ClientInfo.fLastSuccessfullReceive = now;
                if (ClientInfo.fClientStatus == TBoldClientReceiveStatus.crsNotReceiving)
                {
                    ClientInfo.fClientStatus = TBoldClientReceiveStatus.crsRecovered;
                    BoldThreadSafeLog.BoldLogError(sClientHasRecovered, ClientInfo.ClientIdentifierString, ClientID, EventCount);
                }
            }
        }
        finally
        {
            ReleaseLock();
        }
    }

    /// <summary>Counts the events as lost and marks the client as not receiving.</summary>
    [Delphi(PortStatus.Ported)]
    public void MarkFailedToReceiveEvents(TBoldClientID ClientId, int EventCount)
    {
        if (!Enabled)
        {
            return;
        }

        AcquireLock();
        try
        {
            if (fClientInfoList![ClientId] is { } ClientInfo)
            {
                ClientInfo.fLostEvents += EventCount;
                ClientInfo.fClientStatus = TBoldClientReceiveStatus.crsNotReceiving;
                fTotalLostEvents += EventCount;
            }
        }
        finally
        {
            ReleaseLock();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void AcquireLock()
    {
        (FClientHandlerLock ?? throw new ObjectDisposedException(GetType().Name)).Acquire();
        fLockTimeLock!.Acquire();
        try
        {
            fLockTime = DateTime.Now;
        }
        finally
        {
            fLockTimeLock.Release();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ReleaseLock()
    {
        TCriticalSection? lockTimeLock = fLockTimeLock;
        TBoldLoggableCriticalSection? clientHandlerLock = FClientHandlerLock;
        if (lockTimeLock is not null)
        {
            lockTimeLock.Acquire();
            try
            {
                fLockTime = default;
            }
            finally
            {
                lockTimeLock.Release();
            }
        }

        clientHandlerLock?.Release();
    }

    /// <summary>How long the handler's lock has been held; zero when it is free.</summary>
    [Delphi(PortStatus.Ported, Note = "A TimeSpan where Delphi gave a TDateTime difference.")]
    public TimeSpan LockTime()
    {
        fLockTimeLock!.Acquire();
        try
        {
            return fLockTime == default ? TimeSpan.Zero : DateTime.Now - fLockTime;
        }
        finally
        {
            fLockTimeLock.Release();
        }
    }

    /// <summary>Asks the client's listener to disconnect, with a message, and to stay away for <paramref name="RemainDisconnected"/> ms.</summary>
    [Delphi(PortStatus.Ported)]
    public void SendDisconnectRequest(TBoldClientID BoldClientId, string msg, int RemainDisconnected)
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            AcquireLock();
            try
            {
                if (InfoForClient(BoldClientId, out TBoldClientInfo? ClientInfo) &&
                    GetListener(BoldClientId, ClientInfo!.RegistrationTime, out IBoldListener? Listener) &&
                    Listener is IBoldListenerAdmin ListenerAdmin)
                {
                    ListenerAdmin.DisconnectClient(msg, RemainDisconnected);
                }
            }
            finally
            {
                ReleaseLock();
            }
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(sLogErrorAndID, GetType().Name, "SendDisconnectRequest", BoldClientId, E.Message);
        }
    }

    [Delphi(PortStatus.Ported)]
    public int NOConnectedClients
    {
        get => fNOConnectedClients;
        set => SetNOConnectedClients(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool Enabled
    {
        get => GetEnabled();
        set => SetEnabled(value);
    }

    /// <summary>Delphi's TDateTime 0, from which a TDateTime difference counts.</summary>
    private static readonly DateTime DelphiDateTimeZero = new(1899, 12, 30);

    /// <summary>A time interval in Delphi's TimeToStr form, as Delphi formatted TDateTime differences.</summary>
    internal static string IntervalToStr(TimeSpan Interval) =>
        SysUtils.TimeToStr(DateTime.MinValue + (Interval < TimeSpan.Zero ? Interval.Negate() : Interval));
}

/// <summary>What the propagator knows of a client: its lease, listener and statistics.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientInfo : IDisposable
{
    private readonly TBoldClientID fClientID;
    private string fClientIdentifierString = "";
    private int fLeaseDuration;
    private int fPollingInterval;
    private TTimeStamp fLeaseTimeOut;
    private TTimeStamp fRegistrationTime;
    internal IBoldListener? fBoldListener;
    internal int fSuccessfullyReceivedEventsCount;
    internal int fSuccessfullyReceivedCount;
    internal DateTime fLastSuccessfullReceive;
    internal TimeSpan fLongestIntervalBetweenEvents;
    internal int fLostEvents;
    internal TBoldClientReceiveStatus fClientStatus;
    internal bool fInitialized;

    [Delphi(PortStatus.Ported)]
    public TBoldClientInfo(TBoldClientID ClientID)
    {
        fClientID = ClientID;
        fClientStatus = TBoldClientReceiveStatus.crsOK;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientInfo.Destroy")]
    public void Dispose()
    {
        UnInitialize();
        GC.SuppressFinalize(this);
    }

    /// <summary>Starts the client's lease and statistics, and registers it now.</summary>
    [Delphi(PortStatus.Ported)]
    public void Initialize(int LeaseTime, int PollingInterval, IBoldListener ClientCallBack, string ClientIDString)
    {
        try
        {
            ExtendLease(LeaseTime);
            fPollingInterval = PollingInterval;
            fClientIdentifierString = ClientIDString;
            fBoldListener = ClientCallBack;
            fRegistrationTime = SysUtils.DateTimeToTimeStamp(DateTime.Now);
            fSuccessfullyReceivedEventsCount = 0;
            fSuccessfullyReceivedCount = 0;
            fLastSuccessfullReceive = DateTime.Now;
            fLongestIntervalBetweenEvents = TimeSpan.Zero;
            fLostEvents = 0;
            fClientStatus = TBoldClientReceiveStatus.crsOK;
            fInitialized = true;
        }
        catch (Exception E)
        {
            BoldThreadSafeLog.BoldLogError(PropagatorConsts.sLogErrorAndID, GetType().Name, "Initialize", ClientIDString, E.Message);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void UnInitialize()
    {
        if (Initialized)
        {
            fLeaseDuration = 0;
            fPollingInterval = 0;
            fBoldListener = null;
            fInitialized = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    internal bool LeaseIsExpired()
    {
        TTimeStamp CurrentTime = SysUtils.DateTimeToTimeStamp(DateTime.Now);
        return BoldUtils.TimeStampComp(LeaseTimeOut, CurrentTime) <= 0;
    }

    [Delphi(PortStatus.Replaced, Note = "Nothing to do: Delphi registered the listener in COM's global interface table again.")]
    internal void ReInitializeListenerInterface()
    {
    }

    [Delphi(PortStatus.Ported)]
    private string GetClientStatusString() => fClientStatus switch
    {
        TBoldClientReceiveStatus.crsOK => PropagatorConsts.sOK,
        TBoldClientReceiveStatus.crsNotReceiving => PropagatorConsts.sNotReceiving,
        TBoldClientReceiveStatus.crsRecovered => PropagatorConsts.sRecovered,
        _ => PropagatorConsts.sUnknown,
    };

    /// <summary>The lease now runs <paramref name="LeaseDuration"/> milliseconds from now.</summary>
    [Delphi(PortStatus.Ported, Note = "Leases of a day or more work; Delphi's TTimeStamp arithmetic took the duration as a time of day.")]
    public void ExtendLease(int LeaseDuration)
    {
        fLeaseDuration = LeaseDuration;
        fLeaseTimeOut = SysUtils.DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(LeaseDuration));
    }

    [Delphi(PortStatus.Ported)] public TBoldClientID ClientID => fClientID;
    [Delphi(PortStatus.Ported)] public int LeaseDuration => fLeaseDuration;
    [Delphi(PortStatus.Ported)] public int PollingInterval => fPollingInterval;
    [Delphi(PortStatus.Ported)] public TTimeStamp LeaseTimeOut => fLeaseTimeOut;
    [Delphi(PortStatus.Ported)] public string ClientIdentifierString => fClientIdentifierString;
    [Delphi(PortStatus.Ported)] public bool Initialized => fInitialized;
    [Delphi(PortStatus.Ported)] public TTimeStamp RegistrationTime => fRegistrationTime;
    [Delphi(PortStatus.Ported)] public string ClientStatusString => GetClientStatusString();
}

/// <summary>The client infos by client id, and the ids free for new clients (handed out oldest freed first).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClientInfoList : IDisposable
{
    internal TBoldObjectArray? fList;
    private TList? fFreeClientIDs;

    [Delphi(PortStatus.Ported)]
    public TBoldClientInfoList()
    {
        fList = new TBoldObjectArray(BoldPropagatorConstants.MINIMAL_CLIENT_INFO_LIST_GROWTH,
            TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner, TBoldContainerOption.bcoThreadSafe));
        fFreeClientIDs = new TList();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldClientInfoList.Destroy")]
    public void Dispose()
    {
        fFreeClientIDs = null;
        SysUtils.FreeAndNil(ref fList);
        GC.SuppressFinalize(this);
    }

    /// <summary>The info at the client id (null when none), growing the list to hold the id.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldClientInfo? GetItem(int Index)
    {
        try
        {
            EnsureListCapacity(Index);
            return (TBoldClientInfo?)fList![Index];
        }
        catch (Exception e) when (e is EBold or ArgumentOutOfRangeException or InvalidCastException)
        {
            return null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetItem(int Index, TBoldClientInfo? AObject)
    {
        EnsureListCapacity(Index);
        fList![Index] = AObject;
    }

    [Delphi(PortStatus.Ported)]
    private int getCount() => fList!.Count;

    /// <summary>Grows the list to hold the index (by at least MINIMAL_CLIENT_INFO_LIST_GROWTH); the new ids are free.</summary>
    [Delphi(PortStatus.Ported)]
    private void EnsureListCapacity(int Index)
    {
        if (Index >= fList!.Count)
        {
            int cnt = Index - fList.Count + 1;
            if (cnt < BoldPropagatorConstants.MINIMAL_CLIENT_INFO_LIST_GROWTH)
            {
                cnt = BoldPropagatorConstants.MINIMAL_CLIENT_INFO_LIST_GROWTH;
            }

            for (int i = 0; i < cnt; i++)
            {
                ReturnFreeClientID(fList.Add(null));
            }
        }
    }

    /// <summary>A free client id, keeping at least MINIMAL_FREE_CLIENTID_COUNT free.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal TBoldClientID GetFreeClientID()
    {
        EnsureListCapacity(fList!.Count + BoldPropagatorConstants.MINIMAL_FREE_CLIENTID_COUNT - fFreeClientIDs!.Count);
        var result = (TBoldClientID)fFreeClientIDs.First()!;
        fFreeClientIDs.Delete(0);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected internal void ReturnFreeClientID(TBoldClientID ClientID) => fFreeClientIDs!.Add(ClientID);

    [Delphi(PortStatus.Ported)]
    public int Add(TBoldClientInfo AObject) => fList!.Add(AObject);

    [Delphi(PortStatus.Ported)]
    public void Clear() => fList!.Clear();

    [Delphi(PortStatus.Ported)]
    public int Remove(TBoldClientInfo AObject) => fList!.Remove(AObject);

    [Delphi(PortStatus.Ported)]
    public int IndexOf(TBoldClientInfo AObject) => fList!.IndexOf(AObject);

    [Delphi(PortStatus.Ported)]
    public void Insert(int Index, TBoldClientInfo AObject) => fList!.Insert(Index, AObject);

    [Delphi(PortStatus.Ported)]
    public TBoldClientInfo? Last() => (TBoldClientInfo?)fList![fList.Count - 1];

    [Delphi(PortStatus.Ported)]
    public void Delete(int Index) => fList!.Delete(Index);

    /// <summary>The info at the client id, without growing the list; null when none.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldClientInfo? GetExistingClientInfo(int Index) =>
        Index >= 0 && Index < fList!.Count ? (TBoldClientInfo?)fList[Index] : null;

    [Delphi(PortStatus.Ported)]
    public TBoldClientInfo? this[int Index]
    {
        get => GetItem(Index);
        set => SetItem(Index, value);
    }

    [Delphi(PortStatus.Ported)]
    public int Count => getCount();
}
