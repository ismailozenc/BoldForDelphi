// Delphi: Source/Handles/Core/BoldDerivedHandle.pas
// Port map: CSharp/port/units/BoldDerivedHandle.md
using BuildingClasses;

namespace Bold.Tests;

public class BoldDerivedHandleTests
{
    [Test]
    [Covers(typeof(TBoldDerivedHandle), "*")]
    [Covers("BoldDerivedHandle", "TBoldHandleDeriveAndSubscribe", "TBoldGetStaticBoldType")]
    public void An_event_derives_the_value_and_its_subscriptions_keep_it_current()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann", "Smith");
        TBoldReferenceHandle root = s.Reference(ann, "Person");
        int derivations = 0;
        var initials = new TBoldDerivedHandle(s.Components) { RootHandle = root, ValueTypeName = "String" };
        initials.OnDeriveAndSubscribe = (sender, rootValue, result, subscriber) =>
        {
            derivations++;
            var person = (TPerson)rootValue!;
            person.M_FirstName.DefaultSubscribe(subscriber);
            person.M_LastName.DefaultSubscribe(subscriber);
            result.SetOwnedValue(new TBAString { AsString = person.FirstName[..1] + person.LastName[..1] });
        };
        Assert.That(initials.ValueTypeName, Is.EqualTo("String"));
        Assert.That(initials.StaticBoldType!.ExpressionName, Is.EqualTo("String"));
        Assert.That(initials.Value!.AsString, Is.EqualTo("AS"));
        Assert.That(initials.Value!.AsString, Is.EqualTo("AS"));
        Assert.That(derivations, Is.EqualTo(1), "derived once while current");
        ann.LastName = "Brown";
        Assert.That(initials.Value!.AsString, Is.EqualTo("AB"));
        Assert.That(derivations, Is.EqualTo(2));
        Assert.That(initials.OnDeriveAndSubscribe, Is.Not.Null);

        initials.ValueTypeName = "Person";
        Assert.That(() => initials.StaticBoldType, Throws.InstanceOf<EBold>());
        var unrooted = new TBoldDerivedHandle(s.Components) { ValueTypeName = "Integer" };
        Assert.That(unrooted.StaticBoldType, Is.Null, "no system handle and no root");
        unrooted.StaticSystemHandle = s.SystemHandle;
        Assert.That(unrooted.StaticBoldType!.ExpressionName, Is.EqualTo("Integer"));
    }
}
