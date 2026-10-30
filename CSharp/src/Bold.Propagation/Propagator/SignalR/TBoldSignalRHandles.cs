using static Bold.BoldDefs;

namespace Bold;

/// <summary>
/// A propagator handle for a propagator server reached through its SignalR hub (Bold.Propagator.Server), at
/// <see cref="Url"/>: what Delphi's COM connection handle was for a propagator on another machine. When the
/// connection closes, <see cref="DoPropagatorCallFailed"/> is called on the thread that connected; the handle stays
/// connected until it is disconnected (or reconnected, see BoldPropagatorUtils.ReConnectToPropagator).
/// </summary>
public class TBoldSignalRPropagatorHandle : TBoldAbstractPropagatorHandle
{
    private TBoldHubConnection? fHub;
    private TBoldSignalRPropagatorConnection? fConnection;
    private SynchronizationContext? fContext;

    public TBoldSignalRPropagatorHandle(System.ComponentModel.IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

    /// <summary>The hub's address, as http://server:port/propagator.</summary>
    public string Url { get; set; } = "";

    /// <summary>How long connecting and each call wait for the hub, in milliseconds.</summary>
    public int TimeOut { get; set; } = 30000;

    /// <summary>The connection while connected.</summary>
    public TBoldSignalRPropagatorConnection? Connection => fConnection;

    /// <summary>Called when a call to the propagator failed or the connection closed; without it the failure is logged.</summary>
    public TBoldNotifyEventWithErrorMessage? OnPropagatorCallFailed { get; set; }

    protected override IBoldClientHandler? GetClientHandler() => fConnection;

    protected override IBoldEventPropagator? GetEventPropagator() => fConnection;

    protected override bool GetConnected() => fConnection is not null;

    protected override void SetConnected(bool Value)
    {
        if (Value == Connected)
        {
            return;
        }

        if (Value)
        {
            fHub = TBoldSignalRHub.Connect(Url, TimeOut, HubClosed);
            fConnection = new TBoldSignalRPropagatorConnection(fHub);
            fContext = SynchronizationContext.Current;
            SendEvent(this, beConnected);
        }
        else
        {
            SendEvent(this, beDisconnecting);
            TBoldSignalRHub.Close(ref fHub);
            fConnection = null;
            SendEvent(this, beDisconnected);
        }
    }

    private void HubClosed(TBoldHubConnection Hub, Exception? Error)
    {
        string message = $"The connection to the propagator at {Url} closed{(Error is null ? "" : ": " + Error.Message)}";
        void Report()
        {
            if (fHub == Hub)
            {
                DoPropagatorCallFailed(this, message);
            }
        }

        if (fContext is null)
        {
            Report();
        }
        else
        {
            fContext.Post(_ => Report(), null);
        }
    }

    public override void DoPropagatorCallFailed(object? Sender, string ErrorMessage)
    {
        if (OnPropagatorCallFailed is not null)
        {
            OnPropagatorCallFailed(Sender!, ErrorMessage);
        }
        else
        {
            BoldThreadSafeLog.BoldLogError("%s: %s", GetType().Name, ErrorMessage);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && Connected)
        {
            Connected = false;
        }

        base.Dispose(disposing);
    }
}

/// <summary>A lock manager handle for the lock manager of a propagator server reached through its SignalR hub.</summary>
public class TBoldSignalRLockManagerHandle : TBoldAbstractLockManagerHandle
{
    private TBoldHubConnection? fHub;
    private TBoldSignalRPropagatorConnection? fConnection;

    public TBoldSignalRLockManagerHandle(System.ComponentModel.IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

    /// <summary>The hub's address, as http://server:port/propagator.</summary>
    public string Url { get; set; } = "";

    /// <summary>How long connecting and each call wait for the hub, in milliseconds.</summary>
    public int TimeOut { get; set; } = 30000;

    public bool Connected
    {
        get => fConnection is not null;
        set
        {
            if (value == Connected)
            {
                return;
            }

            if (value)
            {
                fHub = TBoldSignalRHub.Connect(Url, TimeOut, null);
                fConnection = new TBoldSignalRPropagatorConnection(fHub);
                SendEvent(this, beConnected);
            }
            else
            {
                TBoldSignalRHub.Close(ref fHub);
                fConnection = null;
                SendEvent(this, beDisconnected);
            }
        }
    }

    protected override IBoldLockManager? GetLockManager() => fConnection;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connected = false;
        }

        base.Dispose(disposing);
    }
}

/// <summary>A lock manager administration handle for a propagator server reached through its SignalR hub.</summary>
public class TBoldSignalRLockManagerAdminHandle : TBoldAbstractLockManagerAdminHandle
{
    private TBoldHubConnection? fHub;
    private TBoldSignalRPropagatorConnection? fConnection;

    public TBoldSignalRLockManagerAdminHandle(System.ComponentModel.IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

    /// <summary>The hub's address, as http://server:port/propagator.</summary>
    public string Url { get; set; } = "";

    /// <summary>How long connecting and each call wait for the hub, in milliseconds.</summary>
    public int TimeOut { get; set; } = 30000;

    public bool Connected
    {
        get => fConnection is not null;
        set
        {
            if (value == Connected)
            {
                return;
            }

            if (value)
            {
                fHub = TBoldSignalRHub.Connect(Url, TimeOut, null);
                fConnection = new TBoldSignalRPropagatorConnection(fHub);
            }
            else
            {
                TBoldSignalRHub.Close(ref fHub);
                fConnection = null;
            }
        }
    }

    protected override IBoldLockManagerAdmin? GetLockManagerAdmin() => fConnection;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connected = false;
        }

        base.Dispose(disposing);
    }
}

/// <summary>Connecting and closing the handles' hub connections.</summary>
internal static class TBoldSignalRHub
{
    public static TBoldHubConnection Connect(string Url, int TimeOut, Action<TBoldHubConnection, Exception?>? OnClosed)
    {
        if (string.IsNullOrEmpty(Url))
        {
            throw new EBold("The propagator hub's Url is not set.");
        }

        var hub = new TBoldHubConnection(Url) { InvokeTimeOut = TimeOut };
        try
        {
            hub.Start(TimeOut);
        }
        catch
        {
            hub.Dispose();
            throw;
        }

        if (OnClosed is not null)
        {
            hub.OnClosed = error => OnClosed(hub, error);
        }

        return hub;
    }

    public static void Close(ref TBoldHubConnection? Hub)
    {
        TBoldHubConnection? hub = Hub;
        Hub = null;
        hub?.Dispose();
    }
}
