// Delphi: Source/Propagator/Common/BoldAbstractPropagatorHandle.pas
// Port map: CSharp/port/units/BoldAbstractPropagatorHandle.md

namespace Bold;

/// <summary>
/// A connection to a propagator: its client handler and event propagator. Subclasses connect in process or over the
/// network. Others subscribe to it for beConnected, beDisconnecting, beDisconnected and beDestroying.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractPropagatorHandle : TBoldSubscribableComponent
{
    [Delphi(PortStatus.Ported)]
    protected abstract IBoldClientHandler? GetClientHandler();

    [Delphi(PortStatus.Ported)]
    protected abstract IBoldEventPropagator? GetEventPropagator();

    [Delphi(PortStatus.Ported)]
    protected abstract bool GetConnected();

    [Delphi(PortStatus.Ported)]
    protected abstract void SetConnected(bool Value);

    /// <summary>Called when a call to the propagator failed.</summary>
    [Delphi(PortStatus.Ported)]
    public abstract void DoPropagatorCallFailed(object? Sender, string ErrorMessage);

    [Delphi(PortStatus.Ported)]
    public IBoldClientHandler? ClientHandler => GetClientHandler();

    [Delphi(PortStatus.Ported)]
    public IBoldEventPropagator? EventPropagator => GetEventPropagator();

    [Delphi(PortStatus.Ported)]
    public bool Connected
    {
        get => GetConnected();
        set => SetConnected(value);
    }
}
