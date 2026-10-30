// Delphi: Source/ConcurrencyControl/Common/BoldAbstractLockManagerAdminHandle.pas
// Port map: CSharp/port/units/BoldAbstractLockManagerAdminHandle.md

namespace Bold;

/// <summary>A connection to a lock manager's administration.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractLockManagerAdminHandle : TBoldSubscribableComponent
{
    [Delphi(PortStatus.Ported)]
    protected abstract IBoldLockManagerAdmin? GetLockManagerAdmin();

    [Delphi(PortStatus.Ported)]
    public IBoldLockManagerAdmin? LockManagerAdmin => GetLockManagerAdmin();
}
