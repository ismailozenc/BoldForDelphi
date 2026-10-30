using static Bold.BoldDefs;

namespace Bold;

/// <summary>
/// A propagator handle for a propagator server in the same process (<see cref="TBoldPropagatorServer.Instance"/>,
/// started with Initialize): an application and its propagator in one process, or tests. Each connection gets its own
/// <see cref="TBoldPropagatorConnection"/>.
/// </summary>
public class TBoldInProcessPropagatorHandle : TBoldAbstractPropagatorHandle
{
    private TBoldPropagatorConnection? fConnection;

    public TBoldInProcessPropagatorHandle(System.ComponentModel.IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

    /// <summary>The connection while connected.</summary>
    public TBoldPropagatorConnection? Connection => fConnection;

    /// <summary>Called when a call to the propagator failed; without it the failure is logged.</summary>
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
            TBoldPropagatorServer server = TBoldPropagatorServer.Instance();
            if (!server.Initialized)
            {
                throw new EBold($"{GetType().Name}: the propagator server of this process is not initialized.");
            }

            fConnection = new TBoldPropagatorConnection(server);
            SendEvent(this, beConnected);
        }
        else
        {
            SendEvent(this, beDisconnecting);
            fConnection = null;
            SendEvent(this, beDisconnected);
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

/// <summary>A lock manager handle for the lock manager of the propagator server in the same process.</summary>
public class TBoldInProcessLockManagerHandle : TBoldAbstractLockManagerHandle
{
    private TBoldPropagatorConnection? fConnection;

    public TBoldInProcessLockManagerHandle(System.ComponentModel.IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

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
                fConnection = new TBoldPropagatorConnection(TBoldPropagatorServer.Instance());
                SendEvent(this, beConnected);
            }
            else
            {
                fConnection = null;
                SendEvent(this, beDisconnected);
            }
        }
    }

    protected override IBoldLockManager? GetLockManager() => fConnection;
}

/// <summary>A lock manager administration handle for the propagator server in the same process.</summary>
public class TBoldInProcessLockManagerAdminHandle : TBoldAbstractLockManagerAdminHandle
{
    private readonly TBoldPropagatorConnection fConnection = new();

    public TBoldInProcessLockManagerAdminHandle(System.ComponentModel.IContainer? Owner = null)
    {
        Owner?.Add(this);
    }

    protected override IBoldLockManagerAdmin? GetLockManagerAdmin() => fConnection;
}
