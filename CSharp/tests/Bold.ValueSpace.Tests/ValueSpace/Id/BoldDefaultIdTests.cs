// Delphi: Source/ValueSpace/Id/BoldDefaultId.pas
// Port map: CSharp/port/units/BoldDefaultId.md
namespace Bold.Tests;

public class BoldDefaultIdTests
{
    [Test]
    [Covers(typeof(TBoldDefaultID), "*")]
    public void Default_ids_compare_by_value_and_timestamp()
    {
        var a = new TBoldDefaultID(2, true) { AsInteger = 12 };
        var b = new TBoldDefaultID(4, false) { AsInteger = 12 };
        Assert.That(a.IsEqual(b), Is.True, "the class does not take part in equality");
        Assert.That(a.Hash, Is.EqualTo(b.Hash));
        Assert.That(a.IsStorable, Is.True);
        Assert.That(a.AsString, Is.EqualTo("12"));
        Assert.That(a.GetStreamName(), Is.EqualTo(BoldDefaultStreamNames.BOLDDEFAULTIDNAME));
        Assert.That(a.IsEqual(new TBoldInternalObjectId(12, 2, true)), Is.False);

        var clone = (TBoldDefaultID)a.CloneWithClassId(7, false);
        Assert.That(clone.GetType(), Is.EqualTo(typeof(TBoldDefaultID)));
        Assert.That(clone.AsInteger, Is.EqualTo(12));
        Assert.That(clone.TopSortedIndex, Is.EqualTo(7));
        Assert.That(clone.TopSortedIndexExact, Is.False);

        Assert.That(a.CloneWithTimeStamp(BoldDefs.BOLDMAXTIMESTAMP).GetType(), Is.EqualTo(typeof(TBoldDefaultID)), "now is a plain id");
        TBoldDefaultID versioned = a.CloneWithClassIdAndTimeStamp(3, true, 50);
        Assert.That(versioned, Is.InstanceOf<TBoldTimestampedDefaultId>());
        Assert.That(versioned.TimeStamp, Is.EqualTo(50));
        Assert.That(versioned.AsInteger, Is.EqualTo(12));
        Assert.That(versioned.IsEqual(a), Is.False, "versions differ by timestamp");
    }

    [Test]
    [Covers(typeof(TBoldTimestampedDefaultId), "*")]
    public void Timestamped_ids_carry_their_timestamp()
    {
        var a = new TBoldTimestampedDefaultId(9, 1, true) { AsInteger = 3 };
        Assert.That(a.TimeStamp, Is.EqualTo(9));
        Assert.That(a.GetStreamName(), Is.EqualTo(BoldDefaultStreamNames.BOLDTIMESTAMPEDDEFAULTIDNAME));
        var clone = (TBoldTimestampedDefaultId)a.Clone();
        Assert.That(clone.TimeStamp, Is.EqualTo(9));
        Assert.That(clone.IsEqual(a), Is.True);
        clone.TimeStamp = 10;
        Assert.That(clone.IsEqual(a), Is.False);
        Assert.That(new TBoldTimestampedDefaultId().TimeStamp, Is.EqualTo(0));
        Assert.That(new TBoldTimestampedDefaultId(2, true).TopSortedIndex, Is.EqualTo(2));
        Assert.That(new TBoldTimestampedDefaultId(2, true) { AsInteger = 3, TimeStamp = BoldDefs.BOLDMAXTIMESTAMP }
            .IsEqual(new TBoldDefaultID { AsInteger = 3 }), Is.True, "a timestamped id at now equals the plain id");
    }
}
