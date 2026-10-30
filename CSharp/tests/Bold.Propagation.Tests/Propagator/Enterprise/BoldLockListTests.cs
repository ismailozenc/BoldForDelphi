// Delphi: Source/Propagator/Enterprise/BoldLockList.pas
// Port map: CSharp/port/units/BoldLockList.md
namespace Bold.Tests;

[Covers(typeof(TBoldServerLockList), "*")]
[Covers(typeof(TBoldLockNode), "*")]
[Covers(typeof(TBoldLockNameIndexNode), "*")]
[Covers(typeof(TBoldLockNameList), "*")]
[Covers(typeof(TBoldLockNameHashList), "*")]
[Covers(typeof(TBoldLockIndex), "*")]
[Covers(typeof(TBoldClientIdHashList), "*")]
[Covers(typeof(TBoldClientIdIndex), "*")]
[Covers(typeof(TBoldLockType))]
public class BoldLockListTests
{
    private static readonly TTimeStamp Now = SysUtils.DateTimeToTimeStamp(DateTime.Now);

    [Test]
    public void Locks_are_kept_by_client_and_by_name_with_counts_of_shared_and_exclusive()
    {
        PortAssert.EnumsMatchDelphi("Source/Propagator/Enterprise/BoldLockList.pas", typeof(TBoldLockType));
        using var locks = new TBoldServerLockList();
        Assert.That((locks.ClientIdIndexOrder, locks.LockNameIndexOrder), Is.EqualTo((0, 1)));
        locks.AddLock(1, 1000, Now, "X", TBoldLockType.bltShared);
        locks.AddLock(2, 1000, Now, "X", TBoldLockType.bltShared);
        locks.AddLock(1, 1000, Now, "Y", TBoldLockType.bltExclusive);
        TBoldLockNameIndexNode x = locks.Locks("X")!;
        Assert.That((x.LockName, x.NoSharedLocks, x.ExclusiveLock), Is.EqualTo(("X", 2, false)));
        Assert.That(locks.Locks("y")!.ExclusiveLock, Is.True, "lock names ignore case");
        Assert.That(locks.Locks("Z"), Is.Null);

        var names = new TStringList();
        locks.GetLocksByClientId(1, names);
        Assert.That(names.ToArray(), Is.EquivalentTo(new[] { "X", "Y" }));
        var clients = new TList();
        locks.GetClientIDsbyLock("X", clients);
        Assert.That(clients.Select(c => (int)c!), Is.EquivalentTo(new[] { 1, 2 }));
        Assert.That(locks.Clients(1).Next, Is.Not.Null);

        // Asking again for a held lock changes nothing; asking for it exclusive upgrades it.
        TBoldLockNode node = locks.AddLock(2, 1000, Now, "X", TBoldLockType.bltShared);
        Assert.That(locks.Items(2, "X"), Is.SameAs(node));
        Assert.That(x.NoSharedLocks, Is.EqualTo(2));
        locks.AddLock(2, 1000, Now, "X", TBoldLockType.bltExclusive);
        Assert.That((node.LockType, x.NoSharedLocks, x.ExclusiveLock), Is.EqualTo((TBoldLockType.bltExclusive, 1, true)));
        Assert.That(x.Clients.ItemsbyClientID(2), Is.SameAs(node));

        locks.RemoveLockForClient(2, "X");
        Assert.That((x.NoSharedLocks, x.ExclusiveLock), Is.EqualTo((1, false)));
        Assert.That(locks.Items(2, "X"), Is.Null);
        locks.RemoveClient(1);
        Assert.That(locks.Items(1, "Y"), Is.Null);
        Assert.That(locks.Locks("Y")!.ExclusiveLock, Is.False);
        locks.AddLock(3, 1000, Now, "X", TBoldLockType.bltShared);
        locks.RemoveLock("X");
        Assert.That(locks.Items(3, "X"), Is.Null);
        Assert.DoesNotThrow(() => locks.RemoveLockForClient(3, "Nothing"));
    }

    [Test]
    public void A_lock_that_can_time_out_does_so_after_its_timeout()
    {
        using var locks = new TBoldServerLockList();
        TBoldLockNode node = locks.AddLock(1, 50, SysUtils.DateTimeToTimeStamp(DateTime.Now.AddMilliseconds(-100)), "X", TBoldLockType.bltShared);
        Assert.That((node.ClientId, node.TimeOut), Is.EqualTo((1, 50)));
        Assert.That(node.CanTimeOut, Is.EqualTo(BoldLockingDefs.LOCKS_CAN_TIMEOUT));
        Assert.That(node.HasTimedOut, Is.False, "locks do not time out by default");
        node.EnsureLock(false);
        Assert.That(node.CanTimeOut, Is.True);
        Assert.That(node.HasTimedOut, Is.True);
        Assert.That(node.GetLockDuration(SysUtils.DateTimeToTimeStamp(DateTime.Now)), Is.GreaterThanOrEqualTo(100));
        node.EnsureLock(true);
        Assert.That(node.HasTimedOut, Is.False);
        node.Remove();
        Assert.DoesNotThrow(() => node.Remove(), "a lock removed twice");
    }
}
