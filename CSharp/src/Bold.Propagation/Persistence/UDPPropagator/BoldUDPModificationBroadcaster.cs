// Delphi: Source/Persistence/UDPPropagator/BoldUDPModificationBroadcaster.pas
// Port map: CSharp/port/units/BoldUDPModificationBroadcaster.md

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bold;

/// <summary>Called when the broadcaster cannot start listening.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TActivationErrorEvent(object? Sender, Exception E);

/// <summary>
/// A notification propagator that broadcasts the client's events over UDP and listens for the others' (on
/// <see cref="Port"/>, 4098 by default). Received events are handled on the thread that created the broadcaster,
/// through its synchronization context, as Indy handled them on the main thread.
/// </summary>
[Delphi(PortStatus.Ported, Note = "System.Net.Sockets where Delphi used Indy.")]
public class TBoldUDPModificationBroadcaster : TBoldAbstractNotificationPropagator
{
    private const string SIdentification = "#BOLDUDP#\r\n";

    private int fPort = 4098;
    private UdpClient? fUDPServer;
    private CancellationTokenSource? fListening;
    private readonly SynchronizationContext? fContext;

    [Delphi(PortStatus.Ported)]
    public TBoldUDPModificationBroadcaster(System.ComponentModel.IContainer? AOwner = null)
        : base(AOwner)
    {
        fContext = SynchronizationContext.Current;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUDPModificationBroadcaster.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopListening();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private int GetPort() => fPort;

    /// <summary>Changing the port deactivates the broadcaster.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetPort(int Value)
    {
        if (Active)
        {
            SetActive(false);
        }

        fPort = Value;
    }

    /// <summary>Sends the queued events, one datagram each, to the broadcast address.</summary>
    [Delphi(PortStatus.Ported, Note = "Does not let the application idle first, as Delphi did.")]
    protected override void OnSendQueueNotEmpty(TBoldThreadSafeQueue Sender)
    {
        using var client = new UdpClient { EnableBroadcast = true };
        while (!SendQueue.Empty)
        {
            byte[] datagram = Encoding.UTF8.GetBytes(SIdentification + SendQueue.Dequeue());
            client.Send(datagram, datagram.Length, BroadcastAddress, fPort);
        }
    }

    /// <summary>Activating starts listening on the port; a failure goes to OnActivationError, or is raised without one.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetActive(bool Value)
    {
        base.SetActive(Value);
        try
        {
            if (Value)
            {
                StartListening();
            }
            else
            {
                StopListening();
            }
        }
        catch (Exception E)
        {
            if (OnActivationError is not null)
            {
                OnActivationError(this, E);
            }
            else
            {
                throw;
            }
        }
    }

    /// <summary>A datagram with the broadcaster's signature is an event.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the datagram's bytes where Indy gave a stream and the socket.")]
    protected void InternalUDPRead(byte[] AData)
    {
        string S = Encoding.UTF8.GetString(AData);
        if (S.Length > 0 && S.StartsWith(SIdentification, StringComparison.OrdinalIgnoreCase))
        {
            ReceiveEvent(S[SIdentification.Length..]);
        }
    }

    private void StartListening()
    {
        if (fUDPServer is not null)
        {
            return;
        }

        var server = new UdpClient(AddressFamily.InterNetwork);
        server.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        server.Client.Bind(new IPEndPoint(IPAddress.Any, fPort));
        fUDPServer = server;
        fListening = new CancellationTokenSource();
        CancellationToken token = fListening.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult received;
                try
                {
                    received = await server.ReceiveAsync(token).ConfigureAwait(false);
                }
                catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException or SocketException)
                {
                    break;
                }

                byte[] data = received.Buffer;
                if (fContext is null)
                {
                    InternalUDPRead(data);
                }
                else
                {
                    fContext.Post(_ => InternalUDPRead(data), null);
                }
            }
        }, token);
    }

    private void StopListening()
    {
        fListening?.Cancel();
        fUDPServer?.Dispose();
        fUDPServer = null;
        fListening = null;
    }

    [Delphi(PortStatus.Ported)]
    public int Port
    {
        get => GetPort();
        set => SetPort(value);
    }

    [Delphi(PortStatus.Ported)] public TActivationErrorEvent? OnActivationError { get; set; }

    /// <summary>Where events are sent: the limited broadcast address unless set (another address can be a subnet's broadcast).</summary>
    public string BroadcastAddress { get; set; } = "255.255.255.255";

    /// <summary>The event handler that gets the received events.</summary>
    public new TBoldExternalObjectSpaceEventHandler? Dequeuer
    {
        get => base.Dequeuer;
        set => base.Dequeuer = value;
    }

    /// <summary>Called with each received event.</summary>
    public new TReceivePropagatorEvent? OnReceiveEvent
    {
        get => base.OnReceiveEvent;
        set => base.OnReceiveEvent = value;
    }

    /// <summary>The system handle whose model the snooper uses.</summary>
    public new TBoldSystemHandle? SystemHandle
    {
        get => base.SystemHandle;
        set => base.SystemHandle = value;
    }

    /// <summary>Activates the broadcaster (Delphi published the inherited Active).</summary>
    public new bool Active
    {
        get => base.Active;
        set => SetActive(value);
    }
}
