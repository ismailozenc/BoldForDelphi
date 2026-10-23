// Delphi: Source/Common/Support/BoldStringList.pas
// Port map: CSharp/port/units/BoldStringList.md
namespace Bold.Tests;

public class BoldStringListTests
{
    [Test]
    [Covers(typeof(TBoldStringList), "*")]
    [Covers("BoldStringList", "BoldAnsiLCompareShortest")]
    public void A_sorted_string_list_finds_name_value_pairs_by_binary_search()
    {
        var list = new TBoldStringList();
        list.AddMultipleStrings(["b=2", "a=1"]);
        Assert.That(list.FastValues("a"), Is.EqualTo("1"), "an unsorted list looks values up the usual way");
        list.SetFastValues("c", "3");
        Assert.That(list.Count, Is.EqualTo(3));

        list.Sorted = true;
        Assert.That(list.ToStringArray(), Is.EqualTo(new[] { "a=1", "b=2", "c=3" }));
        Assert.That(list.FastValues("b"), Is.EqualTo("2"));
        Assert.That(list.FastValues("B"), Is.EqualTo("2"), "names compare without case");
        Assert.That(list.FastValues("x"), Is.EqualTo(""));
        list.SetFastValues("b", "22");
        Assert.That(list.FastValues("b"), Is.EqualTo("22"));
        Assert.That(list.Sorted, Is.True, "still sorted");
        list.SetFastValues("bb", "4");
        Assert.That(list.IndexOf("bb=4"), Is.EqualTo(2), "a new pair goes in its sorted place");

        Assert.That(list.GetIndexOfName("c", out int index), Is.True);
        Assert.That(index, Is.EqualTo(3));
        Assert.That(list.GetIndexOfName("aa", out index), Is.False);
        Assert.That(index, Is.EqualTo(1), "where it would go");
        Assert.That(list.GetIndexOfprefix("b", out index), Is.True);
        Assert.That(index, Is.EqualTo(1), "the first entry with the prefix");
        Assert.That(list.ValueByIndex(0), Is.EqualTo("1"));

        list.RemoveValue("bb");
        list.RemoveValue("nothing");
        Assert.That(list.Count, Is.EqualTo(3));

        list.AssignMultipleStrings(["x=9"]);
        Assert.That(list.Count, Is.EqualTo(1));
        list.Sorted = false;
        list.Put(0, "noequals");
        Assert.That(list.ValueByIndex(0), Is.EqualTo(""));
        Assert.That(list[0], Is.SameAs(BoldSharedStrings.BoldSharedStringManager().GetSharedString("noequals")), "strings are shared");
    }

    [Test]
    public void With_duplicates_accepted_the_search_finds_the_last_match_place()
    {
        var list = new TBoldStringList { Duplicates = TDuplicates.dupAccept, Sorted = true };
        list.AddMultipleStrings(["a=1", "a=2", "b=3"]);
        Assert.That(list.GetIndexOfName("a", out int index), Is.True);
        Assert.That(index, Is.EqualTo(0));
    }
}
