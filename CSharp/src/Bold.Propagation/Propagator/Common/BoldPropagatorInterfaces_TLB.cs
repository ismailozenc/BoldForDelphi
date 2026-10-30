// Delphi: Source/Propagator/Common/BoldPropagatorInterfaces_TLB.pas
// Port map: CSharp/port/units/BoldPropagatorInterfaces_TLB.md
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "BoldPropagator", Note = "COM coclass alias of IBoldClientHandler.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "BoldListener", Note = "COM coclass alias of IBoldListener.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "IBoldListenerDisp", Note = "COM automation dispinterface.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "IBoldListenerDisp.ReceiveEvents", Note = "COM automation dispinterface.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "IBoldListenerAdminDisp", Note = "COM automation dispinterface.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "IBoldListenerAdminDisp.Ping", Note = "COM automation dispinterface.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "IBoldListenerAdminDisp.DisconnectClient", Note = "COM automation dispinterface.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "CoBoldPropagator", Note = "COM object creation; a propagator handle connects to the propagator.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "CoBoldPropagator.Create", Note = "COM object creation.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "CoBoldPropagator.CreateRemote", Note = "COM object creation.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "CoBoldListener", Note = "COM object creation; the listener thread creates its listener.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "CoBoldListener.Create", Note = "COM object creation.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldPropagatorInterfaces_TLB", Symbol = "CoBoldListener.CreateRemote", Note = "COM object creation.")]

namespace Bold;

/// <summary>
/// The propagator's event interface: a client sends the events of its updates, and the subscriptions it wants or no
/// longer wants. Each call returns an HRESULT (<see cref="Windows.S_OK"/>, or one of the E_ codes of BoldPropagatorConstants).
/// Event lists are string arrays where COM passed variant arrays; null is an empty list.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A C# interface where Delphi has a COM interface.")]
public interface IBoldEventPropagator
{
    [Delphi(PortStatus.Ported)]
    int SendEvents(int BoldClientID, string[]? Events);

    [Delphi(PortStatus.Ported)]
    int AddSubscriptions(int BoldClientID, string[]? Subscriptions);

    [Delphi(PortStatus.Ported)]
    int CancelSubscriptions(int BoldClientID, string[]? Subscriptions);
}

/// <summary>
/// The propagator's client interface: a client registers its listener for a lease, extends the lease while it runs,
/// and unregisters. Each call returns an HRESULT.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A C# interface where Delphi has a COM interface.")]
public interface IBoldClientHandler
{
    [Delphi(PortStatus.Ported)]
    int RegisterClient(int LeaseDuration, int PollingInterval, IBoldListener BoldClientListener, string ClientIDString, out int BoldClientID);

    [Delphi(PortStatus.Ported)]
    int ExtendLease(int BoldClientID, int LeaseDuration, out bool ExtensionOK);

    [Delphi(PortStatus.Ported)]
    int UnRegisterClient(int BoldClientID);
}

/// <summary>A client's listener, which the propagator calls with the events the client subscribed to.</summary>
[Delphi(PortStatus.Ported, Note = "A C# interface where Delphi has a COM interface.")]
public interface IBoldListener
{
    /// <summary>Takes the events; returns S_OK, or S_FALSE when they could not be taken.</summary>
    [Delphi(PortStatus.Ported, Symbol = "IBoldListener.ReceiveEvents")]
    int ReceiveEvents(string[]? Events);
}

/// <summary>What the propagator can ask a client's listener besides taking events.</summary>
[Delphi(PortStatus.Ported, Note = "A C# interface where Delphi has a COM interface.")]
public interface IBoldListenerAdmin
{
    [Delphi(PortStatus.Ported, Symbol = "IBoldListenerAdmin.Ping")]
    int Ping();

    /// <summary>Tells the client it is disconnected, with a message, and for how many milliseconds not to reconnect.</summary>
    [Delphi(PortStatus.Ported, Symbol = "IBoldListenerAdmin.DisconnectClient")]
    void DisconnectClient(string aMessage, int RemainDisconnected);
}

/// <summary>The version and ids of the Delphi type library, kept for reference.</summary>
public static class BoldPropagatorInterfaces_TLB
{
    [Delphi(PortStatus.Ported)] public const int BoldPropagatorInterfacesMajorVersion = 1;
    [Delphi(PortStatus.Ported)] public const int BoldPropagatorInterfacesMinorVersion = 0;
    [Delphi(PortStatus.Ported)] public static readonly Guid LIBID_BoldPropagatorInterfaces = new("DC6A300A-C8C2-4D26-9A8D-C8ECB164B54B");
    [Delphi(PortStatus.Ported)] public static readonly Guid IID_IBoldEventPropagator = new("BB7ABB77-BCE2-44C8-9510-F760F507A298");
    [Delphi(PortStatus.Ported)] public static readonly Guid IID_IBoldClientHandler = new("A86E36B1-5EA3-4961-8448-45FD822A271E");
    [Delphi(PortStatus.Ported)] public static readonly Guid IID_IBoldListener = new("0326BF5B-F5AF-4BED-B5E1-F84D2549415A");
    [Delphi(PortStatus.Ported)] public static readonly Guid CLASS_BoldPropagator = new("97C5A9AD-B5F4-4350-A51D-028975782BDA");
    [Delphi(PortStatus.Ported)] public static readonly Guid IID_IBoldListenerAdmin = new("7457EA48-E3B5-4E07-8496-87229ACA5E2D");
    [Delphi(PortStatus.Ported)] public static readonly Guid CLASS_BoldListener = new("A1582C23-E7AB-451A-837F-2131B425E73B");
}
