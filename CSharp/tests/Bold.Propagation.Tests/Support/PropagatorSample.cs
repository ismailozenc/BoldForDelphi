namespace Bold.Tests;

/// <summary>A listener that records the events the propagator sends it, and can be made to fail.</summary>
internal sealed class RecordingListener : IBoldListener, IBoldListenerAdmin
{
    private readonly List<string> fEvents = [];
    public bool Fail { get; set; }
    public readonly List<(string Message, int RemainDisconnected)> Disconnects = [];

    public int ReceiveEvents(string[]? Events)
    {
        if (Fail)
        {
            throw new InvalidOperationException("the client is gone");
        }

        lock (fEvents)
        {
            fEvents.AddRange(Events ?? []);
            Monitor.PulseAll(fEvents);
        }

        return Windows.S_OK;
    }

    public int Ping() => 1;

    public void DisconnectClient(string aMessage, int RemainDisconnected) => Disconnects.Add((aMessage, RemainDisconnected));

    public string[] Events
    {
        get
        {
            lock (fEvents)
            {
                return [.. fEvents];
            }
        }
    }

    public void Clear()
    {
        lock (fEvents)
        {
            fEvents.Clear();
        }
    }

    /// <summary>Waits until the listener has <paramref name="count"/> events (five seconds at most); true when it has.</summary>
    public bool WaitFor(int count, int milliseconds = 5000)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        lock (fEvents)
        {
            while (fEvents.Count < count)
            {
                long left = milliseconds - watch.ElapsedMilliseconds;
                if (left <= 0)
                {
                    return false;
                }

                Monitor.Wait(fEvents, (int)left);
            }

            return true;
        }
    }
}

/// <summary>The propagator server of the process, started for a test (small thread pool, short polling) and freed after it.</summary>
internal sealed class PropagatorSample : IDisposable
{
    public readonly TBoldPropagatorServer Server;

    public PropagatorSample(int poolSize = 3)
    {
        TBoldPropagatorServer.FreeSingleton();
        string ini = Path.Combine(Path.GetTempPath(), $"propagator-{Guid.NewGuid():N}.ini");
        File.WriteAllText(ini, $"[CONFIG PARAMS]\r\nTHREADPOOLSIZE={poolSize}\r\n[COM SERVER]\r\nSERVERNAME=TestPropagator\r\n");
        Server = TBoldPropagatorServer.Instance();
        Server.ConfigFileName = ini;
        Server.Initialize();
        File.Delete(ini);
    }

    public TBoldAdvancedPropagator Propagator => Server.AdvancedPropagator!;

    public TBoldClientHandler ClientHandler => Propagator.ClientHandler!;

    /// <summary>Registers a client through a new connection, polling every <paramref name="pollingInterval"/> ms.</summary>
    public (TBoldPropagatorConnection Connection, int Id, RecordingListener Listener) Register(string name, int pollingInterval = 10,
        int leaseDuration = 60000)
    {
        var connection = new TBoldPropagatorConnection(Server);
        var listener = new RecordingListener();
        Assert.That(connection.RegisterClient(leaseDuration, pollingInterval, listener, name, out int id), Is.EqualTo(Windows.S_OK));
        return (connection, id, listener);
    }

    /// <summary>Waits until the in-queue is empty and the dequeuer has handled it.</summary>
    public void WaitForDequeuer()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while ((Propagator.Dequeuer!.InQueueCount > 0 || Propagator.Dequeuer.MessageCount > 0) && watch.ElapsedMilliseconds < 5000)
        {
            Thread.Sleep(5);
        }

        Thread.Sleep(20);
    }

    public void Dispose() => TBoldPropagatorServer.FreeSingleton();
}
