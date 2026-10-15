// Delphi: Source/ValueSpace/ExternalEvents/BoldObjectSpaceExternalEvents.pas
// Port map: CSharp/port/units/BoldObjectSpaceExternalEvents.md
namespace Bold.Tests;

public class BoldObjectSpaceExternalEventsTests
{
    private static TBoldDefaultID Id(int v) => new() { AsInteger = v };

    [Test]
    [Covers(typeof(TBoldObjectSpaceExternalEvent), "*")]
    [Covers(typeof(TBoldObjectSpaceSubscriptionType))]
    [Covers("BoldObjectSpaceExternalEvents", "TBoldEventType", "TBoldExternalEvent")]
    public void Events_encode_and_decode()
    {
        PortAssert.EnumsMatchDelphi("Source/ValueSpace/ExternalEvents/BoldObjectSpaceExternalEvents.pas", typeof(TBoldObjectSpaceSubscriptionType));
        string Enc(TBoldObjectSpaceSubscriptionType t) => TBoldObjectSpaceExternalEvent.EncodeExternalEvent(t, "Person", "name", "lock1", Id(42));
        Assert.That(Enc(TBoldObjectSpaceSubscriptionType.bsClassChanged), Is.EqualTo("C:Person"));
        Assert.That(Enc(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged), Is.EqualTo("E:42"));
        Assert.That(Enc(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged), Is.EqualTo("I:name:42"));
        Assert.That(Enc(TBoldObjectSpaceSubscriptionType.bsLockLost), Is.EqualTo("L:lock1"));
        Assert.That(Enc(TBoldObjectSpaceSubscriptionType.bsGotLocks), Is.EqualTo("GotLocks"));
        Assert.That(Enc(TBoldObjectSpaceSubscriptionType.bsObjectDeleted), Is.EqualTo("E:42|D"));

        string cls = "", member = "", lockName = "";
        TBoldDefaultID id = Id(0);
        Assert.That(TBoldObjectSpaceExternalEvent.DecodeExternalEvent("C:Person", ref cls, ref member, ref lockName, id),
            Is.EqualTo(TBoldObjectSpaceSubscriptionType.bsClassChanged));
        Assert.That(cls, Is.EqualTo("Person"));
        Assert.That(TBoldObjectSpaceExternalEvent.DecodeExternalEvent("E:42|D", ref cls, ref member, ref lockName, id),
            Is.EqualTo(TBoldObjectSpaceSubscriptionType.bsObjectDeleted));
        Assert.That(id.AsInteger, Is.EqualTo(42));
        Assert.That(TBoldObjectSpaceExternalEvent.DecodeExternalEvent("E:7", ref cls, ref member, ref lockName, id),
            Is.EqualTo(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged));
        Assert.That(id.AsInteger, Is.EqualTo(7));
        Assert.That(TBoldObjectSpaceExternalEvent.DecodeExternalEvent("I:name:9", ref cls, ref member, ref lockName, id),
            Is.EqualTo(TBoldObjectSpaceSubscriptionType.bsNonEmbeddedStateOfObjectChanged));
        Assert.That((member, id.AsInteger), Is.EqualTo(("name", 9)));
        Assert.That(TBoldObjectSpaceExternalEvent.DecodeExternalEvent("L:lock1", ref cls, ref member, ref lockName, id),
            Is.EqualTo(TBoldObjectSpaceSubscriptionType.bsLockLost));
        Assert.That(lockName, Is.EqualTo("lock1"));
        Assert.That(TBoldObjectSpaceExternalEvent.DecodeExternalEvent("GotLocks", ref cls, ref member, ref lockName, null),
            Is.EqualTo(TBoldObjectSpaceSubscriptionType.bsGotLocks));
        Assert.That(TBoldObjectSpaceExternalEvent.DecodeExternalEvent("E:3", ref cls, ref member, ref lockName, null),
            Is.EqualTo(TBoldObjectSpaceSubscriptionType.bsEmbeddedStateOfObjectChanged), "no id object: nothing read");

        var e = Assert.Throws<EBold>(() => TBoldObjectSpaceExternalEvent.DecodeExternalEvent("X:1", ref cls, ref member, ref lockName, id))!;
        Assert.That(e.Message, Is.EqualTo("Invalid event: X:1"));
        e = Assert.Throws<EBold>(() => TBoldObjectSpaceExternalEvent.DecodeExternalEvent("E:abc", ref cls, ref member, ref lockName, id))!;
        Assert.That(e.Message, Is.EqualTo("Invalid ID, abc is not an valid integer."));
        Assert.Throws<EBold>(() => TBoldObjectSpaceExternalEvent.DecodeExternalEvent("", ref cls, ref member, ref lockName, id));
    }
}
