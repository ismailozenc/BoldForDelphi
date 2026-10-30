// Delphi: Source/ConcurrencyControl/Common/BoldAbstractLockManagerHandle.pas
// Port map: CSharp/port/units/BoldAbstractLockManagerHandle.md

namespace Bold;

/// <summary>A connection to a lock manager. Others subscribe to it for beConnected, beDisconnected and beDestroying.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractLockManagerHandle : TBoldSubscribableComponent
{
    [Delphi(PortStatus.Ported)]
    protected abstract IBoldLockManager? GetLockManager();

    [Delphi(PortStatus.Ported)]
    public IBoldLockManager? LockManager => GetLockManager();
}
