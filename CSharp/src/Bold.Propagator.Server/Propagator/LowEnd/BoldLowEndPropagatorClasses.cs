// Delphi: Source/Propagator/LowEnd/BoldLowEndPropagatorClasses.pas
// Port map: CSharp/port/units/BoldLowEndPropagatorClasses.md

namespace Bold;

/// <summary>
/// The low-end propagator: every client's events go to all other clients, without subscriptions or leases. Clients
/// get the slots of clients that left; client ids are slot numbers from 1.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldInternalPropagator : IDisposable
{
    private List<IBoldListener?>? ClientList;

    [Delphi(PortStatus.Ported)]
    public TBoldInternalPropagator()
    {
        ClientList = [];
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldInternalPropagator.Destroy")]
    public void Dispose()
    {
        ClientList = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    internal int RegisterClient(IBoldListener ListenerInterface, out int Value)
    {
        lock (ClientList!)
        {
            int free = ClientList.IndexOf(null);
            if (free >= 0)
            {
                ClientList[free] = ListenerInterface;
                Value = free + 1;
            }
            else
            {
                ClientList.Add(ListenerInterface);
                Value = ClientList.Count;
            }
        }

        RefreshDisplay();
        return Windows.S_OK;
    }

    [Delphi(PortStatus.Ported)]
    internal int UnregisterClient(int ClientID)
    {
        try
        {
            lock (ClientList!)
            {
                ClientList[ClientID - 1] = null;
            }

            RefreshDisplay();
            return Windows.S_OK;
        }
        catch (ArgumentOutOfRangeException)
        {
            return Windows.S_FALSE;
        }
    }

    /// <summary>Sends the events to every registered client except the sender.</summary>
    [Delphi(PortStatus.Ported)]
    public void SendEvents(int BoldClientID, string[]? Events)
    {
        IBoldListener?[] clients;
        lock (ClientList!)
        {
            clients = [.. ClientList];
        }

        for (int i = 0; i < clients.Length; i++)
        {
            if (clients[i] is { } listener && BoldClientID != i + 1)
            {
                listener.ReceiveEvents(Events);
            }
        }
    }

    /// <summary>Tells <see cref="OnRefreshDisplay"/> the number of clients and which slots are taken.</summary>
    [Delphi(PortStatus.Replaced, Note = "Raises OnRefreshDisplay; Delphi updated the low-end propagator's form.")]
    public void RefreshDisplay()
    {
        bool[] slots;
        lock (ClientList!)
        {
            slots = [.. ClientList.Select(c => c is not null)];
        }

        OnRefreshDisplay?.Invoke(slots.Count(s => s), slots);
    }

    /// <summary>Called with the number of clients and which client slots are taken.</summary>
    public Action<int, bool[]>? OnRefreshDisplay { get; set; }

    [Delphi(PortStatus.Ported)]
    public IReadOnlyList<IBoldListener?> PublicClientList
    {
        get
        {
            lock (ClientList!)
            {
                return [.. ClientList];
            }
        }
    }

    private static TBoldInternalPropagator? G_InternalPropagator;

    /// <summary>The program's low-end propagator.</summary>
    [Delphi(PortStatus.Ported, Unit = "BoldLowEndPropagatorClasses", Symbol = "initialization", Note = "Created the first time; Delphi also registered the COM class.")]
    public static TBoldInternalPropagator InternalPropagator => LazyInitializer.EnsureInitialized(ref G_InternalPropagator);

    [Delphi(PortStatus.Ported, Unit = "BoldLowEndPropagatorClasses", Symbol = "finalization")]
    public static void FreeInternalPropagator() => Interlocked.Exchange(ref G_InternalPropagator, null)?.Dispose();
}

/// <summary>
/// A client's connection to the low-end propagator. Leases and subscriptions are accepted and ignored, as in Delphi.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A plain C# object implementing the propagator interfaces, where Delphi had a COM object.")]
public class TBoldClientPropagator : IBoldClientHandler, IBoldEventPropagator
{
    [Delphi(PortStatus.Ported)]
    public int RegisterClient(int LeaseDuration, int PollingInterval, IBoldListener BoldClientListener, string ClientIDString, out int BoldClientID) =>
        TBoldInternalPropagator.InternalPropagator.RegisterClient(BoldClientListener, out BoldClientID);

    [Delphi(PortStatus.Ported)]
    public int ExtendLease(int BoldClientID, int LeaseDuration, out bool ExtensionOK)
    {
        ExtensionOK = false;
        return Windows.S_OK;
    }

    [Delphi(PortStatus.Ported)]
    public int UnRegisterClient(int BoldClientID) => TBoldInternalPropagator.InternalPropagator.UnregisterClient(BoldClientID);

    [Delphi(PortStatus.Ported, Note = "Without the form's event log.")]
    public int SendEvents(int BoldClientID, string[]? Events)
    {
        try
        {
            if (Events is not null)
            {
                TBoldInternalPropagator.InternalPropagator.SendEvents(BoldClientID, Events);
            }

            return Windows.S_OK;
        }
        catch (Exception)
        {
            return Windows.S_FALSE;
        }
    }

    [Delphi(PortStatus.Ported)]
    public int CancelSubscriptions(int BoldClientID, string[]? Subscriptions) => Windows.S_OK;

    [Delphi(PortStatus.Ported, Note = "Without the form's event log.")]
    public int AddSubscriptions(int BoldClientID, string[]? Subscriptions) => Windows.S_OK;
}
