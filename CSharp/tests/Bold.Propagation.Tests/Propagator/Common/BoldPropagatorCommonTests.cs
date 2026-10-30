// Delphi: Source/Propagator/Common (BoldPropagatorConstants, PropagatorConsts, BoldPropagatorGUIDs, BoldPropagatorInterfaces_TLB,
// BoldLockingSupportInterfaces_TLB, BoldObjectMarshaler), Source/ConcurrencyControl/Common/BoldLockingDefs.pas
// Port map: CSharp/port/units/BoldPropagatorConstants.md
namespace Bold.Tests;

public class BoldPropagatorCommonTests
{
    private static readonly string[] WindowsMessages =
    [
        "BM_PRIORITY_CHANGED", "BM_THRD_DOWORK", "BM_THRD_UNREGISTER", "BM_THRD_REGISTER", "BM_UNREGISTER_CLIENT", "BM_QUEUE_NOT_EMPTY",
        "BM_UNMARSHAL_INTERFACE", "BM_EXTEND_LEASE", "BM_PROPAGATOR_CLIENT_REGISTERED", "BM_PROPAGATOR_CLIENT_UNREGISTERED",
        "BM_PROPAGATOR_CLIENT_DISCONNECTED", "BM_PROPAGATOR_CLIENT_LEASE_EXTENDED", "BM_PROPAGATOR_CLIENT_LEASE_CHANGED", "BM_REMOVE_CLIENT_QUEUE",
        "BM_UPDATE_CLIENT_INFO", "BM_REQUEST_CLIENT_INFO", "BM_UPDATE_GLOBAL_INFO", "BM_REQUEST_GLOBAL_INFO", "BM_PROPAGATOR_DISPLAY_UPDATE_REQUEST",
        "BM_PROPAGATOR_DISPLAY_STARTDEQUEUE", "BM_PROPAGATOR_DISPLAY_DONEDEQUEUE",
    ];

    [Test]
    [Covers(typeof(BoldPropagatorConstants), "*")]
    [Covers(typeof(TBoldClientInfoRecord))]
    public void The_propagator_constants_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldPropagatorConstants), "Source/Propagator/Common/BoldPropagatorConstants.pas", WindowsMessages);
        // The thread messages are WM_USER plus their Delphi offsets.
        Assert.That(BoldPropagatorConstants.BM_PRIORITY_CHANGED, Is.EqualTo(Windows.WM_USER + 1));
        Assert.That(BoldPropagatorConstants.BM_QUEUE_NOT_EMPTY, Is.EqualTo(Windows.WM_USER + 12));
        Assert.That(BoldPropagatorConstants.BM_PROPAGATOR_CLIENT_LEASE_CHANGED, Is.EqualTo(Windows.WM_USER + 20));
        Assert.That(BoldPropagatorConstants.BM_PROPAGATOR_DISPLAY_DONEDEQUEUE, Is.EqualTo(Windows.WM_USER + 28));
        var record = new TBoldClientInfoRecord { ClientId = 3 };
        Assert.That(record.ClientId, Is.EqualTo(3));
    }

    [Test]
    [Covers(typeof(PropagatorConsts), "*")]
    [Covers(typeof(BoldLockingDefs), "*")]
    public void The_messages_and_locking_names_have_the_Delphi_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(PropagatorConsts), "Source/Propagator/Common/PropagatorConsts.pas");
        PortAssert.ConstantsMatchDelphi(typeof(BoldLockingDefs), "Source/ConcurrencyControl/Common/BoldLockingDefs.pas");
    }

    [Test]
    [Covers(typeof(BoldPropagatorGUIDs), "*")]
    [Covers(typeof(BoldPropagatorInterfaces_TLB), "*")]
    [Covers(typeof(BoldLockingSupportInterfaces_TLB), "*")]
    public void The_type_library_ids_and_versions_are_kept()
    {
        Assert.That(BoldPropagatorGUIDs.CLSID_BOLDENQUEUER, Is.EqualTo(new Guid("7A091AB8-936C-4EB3-BCFC-AF889AAABC5E")));
        Assert.That(BoldPropagatorGUIDs.CLSID_BOLDCLIENTHANDLER, Is.EqualTo(new Guid("1E1641DF-69A2-4E04-8E24-069477976AF8")));
        Assert.That(BoldPropagatorGUIDs.BoldPropagatorConnection_CLSID, Is.EqualTo(new Guid("11C6E940-CEC6-45BA-873D-27854A82A023")));
        Assert.That((BoldPropagatorInterfaces_TLB.BoldPropagatorInterfacesMajorVersion, BoldPropagatorInterfaces_TLB.BoldPropagatorInterfacesMinorVersion),
            Is.EqualTo((1, 0)));
        Assert.That(BoldPropagatorInterfaces_TLB.IID_IBoldListener, Is.EqualTo(new Guid("0326BF5B-F5AF-4BED-B5E1-F84D2549415A")));
        Assert.That(new[]
        {
            BoldPropagatorInterfaces_TLB.LIBID_BoldPropagatorInterfaces, BoldPropagatorInterfaces_TLB.IID_IBoldEventPropagator,
            BoldPropagatorInterfaces_TLB.IID_IBoldClientHandler, BoldPropagatorInterfaces_TLB.CLASS_BoldPropagator,
            BoldPropagatorInterfaces_TLB.IID_IBoldListenerAdmin, BoldPropagatorInterfaces_TLB.CLASS_BoldListener,
            BoldLockingSupportInterfaces_TLB.LIBID_BoldLockingSupportInterfaces, BoldLockingSupportInterfaces_TLB.IID_IBoldLockManager,
            BoldLockingSupportInterfaces_TLB.IID_IBoldLockManagerAdmin,
        }, Is.Unique);
        Assert.That((BoldLockingSupportInterfaces_TLB.BoldLockingSupportInterfacesMajorVersion, BoldLockingSupportInterfaces_TLB.BoldLockingSupportInterfacesMinorVersion),
            Is.EqualTo((1, 0)));
    }

    [Test]
    [Covers(typeof(TBoldObjectMarshaler), "*")]
    public void The_object_marshaler_hands_over_the_reference()
    {
        var listener = new RecordingListener();
        using (var marshaler = new TBoldObjectMarshaler(typeof(IBoldListener), listener))
        {
            Assert.That(marshaler.UnMarshalObject(out object? obj), Is.True);
            Assert.That(obj, Is.SameAs(listener));
        }

        using var wrong = new TBoldObjectMarshaler(typeof(IBoldClientHandler), listener);
        Assert.That(wrong.UnMarshalObject(out object? none), Is.False, "the object is not of the interface");
        Assert.That(none, Is.Null);
    }
}
