// Delphi: Source/Handles/UnLoader/BoldUnloaderHandle.pas
// Port map: CSharp/port/units/BoldUnloaderHandle.md
namespace Bold.Tests;

public class BoldUnloaderHandleTests
{
    [Test]
    [Covers(typeof(TBoldUnloaderHandle), "*")]
    public void The_unloader_runs_on_the_system_handles_system_while_active()
    {
        using var s = new HandlesSample(activate: false);
        var unloader = new TBoldUnloaderHandle(s.Components);
        Assert.That(unloader.Active, Is.True);
        Assert.That(unloader.TickIntervall, Is.EqualTo(1000));
        Assert.That(unloader.ScanPerTick, Is.EqualTo(1000));
        Assert.That(unloader.MinAgeForUnload, Is.EqualTo(300000));
        unloader.TickIntervall = 50;
        unloader.ScanPerTick = 10;
        unloader.MinAgeForUnload = 0;
        unloader.UnloadDelayedFetch = true;
        TBoldInvalidateMemberEvent mayInvalidate = (TBoldMember member, ref bool invalidate) => invalidate = false;
        TBoldUnloadObjectEvent mayUnload = (TBoldObject boldObject, ref bool unload) => unload = false;
        unloader.OnMayInvalidate = mayInvalidate;
        unloader.OnMayUnload = mayUnload;
        Assert.That(unloader.TickIntervall, Is.EqualTo(50));
        Assert.That(unloader.ScanPerTick, Is.EqualTo(10));
        Assert.That(unloader.MinAgeForUnload, Is.EqualTo(0));
        Assert.That(unloader.UnloadDelayedFetch, Is.True);
        Assert.That(unloader.OnMayInvalidate, Is.SameAs(mayInvalidate));
        Assert.That(unloader.OnMayUnload, Is.SameAs(mayUnload));

        unloader.BoldSystemHandle = s.SystemHandle;
        Assert.That(unloader.BoldSystemHandle, Is.SameAs(s.SystemHandle));
        s.SystemHandle.Active = true;
        unloader.Active = false;
        Assert.That(unloader.Active, Is.False);
        unloader.Active = true;
        s.System.Discard();
        s.SystemHandle.Active = false;
        s.SystemHandle.Dispose();
        Assert.That(unloader.BoldSystemHandle, Is.Null, "a disposed system handle is forgotten");
    }
}
