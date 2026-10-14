// Delphi: Source/Common/Core/BoldBase.pas
// Port map: CSharp/port/units/BoldBase.md
namespace Bold.Tests;

public class BoldBaseTests
{
    private const string Delphi = "Source/Common/Core/BoldBase.pas";

    private sealed class Flagged : TBoldFlaggedObject
    {
        public void SetFlag(int flag, bool value) => SetElementFlag(flag, value);
        public bool GetFlag(int flag) => GetElementFlag(flag);
        public void SetState(uint mask, int shift, uint value) => SetInternalState(mask, shift, value);
        public uint GetState(uint mask, int shift) => GetInternalState(mask, shift);
    }

    private interface IProbe;

    private sealed class Probe : TBoldRefCountedObject, IProbe
    {
        public int AddRef() => _AddRef();
        public int Release() => _Release();
    }

    [Test]
    [Covers(typeof(BoldBase), "*")]
    public void Constants_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldBase), Delphi);
    }

    [Test]
    [Covers(typeof(TBoldMemoryManagedObject))]
    [Covers(typeof(TBoldInterfacedObject))]
    [Covers(typeof(TBoldRefCountedObject))]
    [Covers(typeof(TBoldNonRefCountedObject))]
    [Covers(typeof(TBoldFlaggedObject))]
    public void Classes_have_the_Delphi_parents()
    {
        PortAssert.ClassParentsMatchDelphi(Delphi,
            typeof(TBoldMemoryManagedObject), typeof(TBoldInterfacedObject), typeof(TBoldRefCountedObject),
            typeof(TBoldNonRefCountedObject), typeof(TBoldFlaggedObject));
    }

    [Test]
    [Covers(typeof(TBoldFlaggedObject), "SetElementFlag", "GetElementFlag")]
    [Covers("BoldBase", "TBoldElementFlag")]
    public void Element_flags_are_independent_bits()
    {
        var o = new Flagged();
        o.SetFlag(BoldBase.BoldElementFlag3, true);
        o.SetFlag(BoldBase.BoldElementFlag23, true);
        Assert.That(o.GetFlag(BoldBase.BoldElementFlag3), Is.True);
        Assert.That(o.GetFlag(BoldBase.BoldElementFlag23), Is.True);
        Assert.That(o.GetFlag(BoldBase.BoldElementFlag4), Is.False);
        o.SetFlag(BoldBase.BoldElementFlag3, false);
        Assert.That(o.GetFlag(BoldBase.BoldElementFlag3), Is.False);
        Assert.That(o.GetFlag(BoldBase.BoldElementFlag23), Is.True);
    }

    [Test]
    [Covers(typeof(TBoldFlaggedObject), "SetInternalState", "GetInternalState")]
    public void Internal_state_is_packed_beside_the_flags()
    {
        var o = new Flagged();
        const uint mask = 0x3u << 24;
        o.SetFlag(BoldBase.BoldElementFlag0, true);
        o.SetState(mask, 24, 2);
        Assert.That(o.GetState(mask, 24), Is.EqualTo(2u));
        o.SetState(mask, 24, 1);
        Assert.That(o.GetState(mask, 24), Is.EqualTo(1u));
        Assert.That(o.GetFlag(BoldBase.BoldElementFlag0), Is.True);
    }

    [Test]
    [Covers(typeof(TBoldRefCountedObject), "RefCount", "_AddRef", "_Release", "BeforeDestruction")]
    public void Destroying_a_referenced_object_raises()
    {
        var p = new Probe();
        Assert.That(p.AddRef(), Is.EqualTo(1));
        Assert.That(p.RefCount, Is.EqualTo(1));
        Assert.Throws<EBold>(() => p.BeforeDestruction());
        Assert.That(p.Release(), Is.EqualTo(0));
        Assert.DoesNotThrow(() => p.BeforeDestruction());
    }

    [Test]
    [Covers(typeof(TBoldInterfacedObject), "SupportsInterface")]
    public void SupportsInterface_tests_the_type()
    {
        var p = new Probe();
        Assert.That(p.SupportsInterface(typeof(IProbe)), Is.True);
        Assert.That(p.SupportsInterface(typeof(IDisposable)), Is.False);
    }
}
