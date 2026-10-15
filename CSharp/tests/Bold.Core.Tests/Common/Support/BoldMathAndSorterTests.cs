// Delphi: Source/Common/Support/BoldMath.pas, BoldSorter.pas
// Port map: CSharp/port/units/BoldMath.md, CSharp/port/units/BoldSorter.md
namespace Bold.Tests;

public class BoldMathAndSorterTests
{
    [Test]
    [Covers(typeof(BoldMath), "*")]
    public void Floor_and_extremes()
    {
        Assert.That(BoldMath.Floor(2.7), Is.EqualTo(2));
        Assert.That(BoldMath.Floor(-2.2), Is.EqualTo(-3));
        Assert.That(BoldMath.Floor(-2.0), Is.EqualTo(-2));
        Assert.That(BoldMath.MaxIntValue(3, 9, -1), Is.EqualTo(9));
        Assert.That(BoldMath.MinIntValue(3, 9, -1), Is.EqualTo(-1));
        Assert.That(BoldMath.MaxValue(1.5, -2), Is.EqualTo(1.5));
        Assert.That(BoldMath.MinValue(1.5, -2), Is.EqualTo(-2));
    }

    [Test]
    [Covers(typeof(BoldSorter), "*")]
    [Covers(typeof(TBoldSortCompare))]
    [Covers(typeof(TBoldSortExchange))]
    public void BoldSort_sorts_through_callbacks()
    {
        var rnd = new Random(7);
        int[] data = Enumerable.Range(0, 200).Select(_ => rnd.Next(50)).ToArray();
        BoldSorter.BoldSort(0, data.Length - 1, (a, b) => data[a].CompareTo(data[b]), (a, b) => (data[a], data[b]) = (data[b], data[a]));
        Assert.That(data, Is.Ordered);
        int[] two = [2, 1];
        BoldSorter.BoldSort(0, 1, (a, b) => two[a].CompareTo(two[b]), (a, b) => (two[a], two[b]) = (two[b], two[a]));
        Assert.That(two, Is.EqualTo(new[] { 1, 2 }));
    }
}
