// Delphi: Source/ConcurrencyControl/Common/BoldLockingDefs.pas
// Port map: CSharp/port/units/BoldLockingDefs.md

namespace Bold;

/// <summary>Names and settings of pessimistic locking.</summary>
public static class BoldLockingDefs
{
    [Delphi(PortStatus.Ported)] public const string LOCK_MANAGER_OBJECT_NAME = "BoldLockManager";
    [Delphi(PortStatus.Ported)] public const string LOCK_MANAGER_ADMIN_OBJECT_NAME = "BoldLockManagerAdmin";
    [Delphi(PortStatus.Ported)] public const bool LOCKS_CAN_TIMEOUT = false;

    /// <summary>The lock a client holds exclusively while it updates the database (see TBoldLockHolder.LockDatabase).</summary>
    [Delphi(PortStatus.Ported)] public const string BOLD_DBLOCK_NAME = "DBLock";
}
