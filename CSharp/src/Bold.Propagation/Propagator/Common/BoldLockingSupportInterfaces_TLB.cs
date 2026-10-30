// Delphi: Source/Propagator/Common/BoldLockingSupportInterfaces_TLB.pas
// Port map: CSharp/port/units/BoldLockingSupportInterfaces_TLB.md

namespace Bold;

/// <summary>
/// The lock manager: a client asks for exclusive and shared locks (lock names), gives them back, and checks that it
/// still holds them. Lock lists are string arrays where COM passed variant arrays; null is an empty list.
/// </summary>
[Delphi(PortStatus.Ported, Note = "A C# interface where Delphi has a COM interface.")]
public interface IBoldLockManager
{
    /// <summary>
    /// Gives the client the locks when no other client holds them in a conflicting way; otherwise gives none, and
    /// returns the conflicting locks and the clients ("id=name") that hold them.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    bool GetLocks(int ClientId, int TimeOut, string[]? RequestedExclusiveLocks, string[]? RequestedSharedLocks,
        out string[]? HeldLocks, out string[]? ClientsHoldingRequestedLocks);

    [Delphi(PortStatus.Ported)]
    int ReleaseLocks(int ClientId, string[]? Locks);

    /// <summary>True when the client holds all the locks, each of the kind asked for.</summary>
    [Delphi(PortStatus.Ported)]
    bool EnsureLocks(int ClientId, string[]? RequestedExclusiveLocks, string[]? RequestedSharedLocks);
}

/// <summary>Administration of the lock manager: its clients, their locks, and suspending it.</summary>
[Delphi(PortStatus.Ported, Note = "A C# interface where Delphi has a COM interface.")]
public interface IBoldLockManagerAdmin
{
    [Delphi(PortStatus.Ported)]
    int ListAllClients(out string[]? Clients);

    [Delphi(PortStatus.Ported)]
    int RemoveLocksForClient(int ClientId, string[]? Locks);

    [Delphi(PortStatus.Ported)]
    int KillClient(int ClientId);

    [Delphi(PortStatus.Ported)]
    int LocksForClients(string[]? ClientIds, out string[]? Locks, out string[]? LockDurations);

    [Delphi(PortStatus.Ported)]
    int ListLockingClients(out string[]? Clients);

    [Delphi(PortStatus.Ported)]
    [Delphi(PortStatus.Replaced, Symbol = "IBoldLockManagerAdmin.Get_LockManagerSuspended", Note = "The property's getter.")]
    [Delphi(PortStatus.Replaced, Symbol = "IBoldLockManagerAdmin.Set_LockManagerSuspended", Note = "The property's setter.")]
    bool LockManagerSuspended { get; set; }
}

/// <summary>The version and ids of the Delphi type library, kept for reference.</summary>
public static class BoldLockingSupportInterfaces_TLB
{
    [Delphi(PortStatus.Ported)] public const int BoldLockingSupportInterfacesMajorVersion = 1;
    [Delphi(PortStatus.Ported)] public const int BoldLockingSupportInterfacesMinorVersion = 0;
    [Delphi(PortStatus.Ported)] public static readonly Guid LIBID_BoldLockingSupportInterfaces = new("0EE38CD0-5848-4A2F-96E6-BFE2007AC6BD");
    [Delphi(PortStatus.Ported)] public static readonly Guid IID_IBoldLockManager = new("105D857F-DD36-48F8-9554-ABCC212053ED");
    [Delphi(PortStatus.Ported)] public static readonly Guid IID_IBoldLockManagerAdmin = new("89074E8A-9A98-4D2A-A113-65F495611C6C");
}
