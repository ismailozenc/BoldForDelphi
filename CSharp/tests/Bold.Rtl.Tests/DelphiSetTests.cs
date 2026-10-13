namespace Bold.Tests;

public class DelphiSetTests
{
    private enum TColor { clRed, clGreen, clBlue, clWhite }

    [Test]
    public void Empty_set_has_no_elements()
    {
        var set = DelphiSet<TColor>.Empty;
        Assert.Multiple(() =>
        {
            Assert.That(set.IsEmpty, Is.True);
            Assert.That(set.Count, Is.Zero);
            Assert.That(set.ToString(), Is.EqualTo("[]"));
            Assert.That(set, Is.EqualTo(default(DelphiSet<TColor>)));
        });
    }

    [Test]
    public void Set_literal_holds_its_elements_once()
    {
        var set = DelphiSet<TColor>.Of(TColor.clBlue, TColor.clRed, TColor.clBlue);
        Assert.Multiple(() =>
        {
            Assert.That(set.Count, Is.EqualTo(2));
            Assert.That(set.Contains(TColor.clRed), Is.True);
            Assert.That(set.Contains(TColor.clBlue), Is.True);
            Assert.That(set.Contains(TColor.clGreen), Is.False);
        });
    }

    [Test]
    public void Elements_enumerate_in_ordinal_order()
    {
        var set = DelphiSet<TColor>.Of(TColor.clWhite, TColor.clRed, TColor.clBlue);
        Assert.That(set.ToList(), Is.EqualTo(new[] { TColor.clRed, TColor.clBlue, TColor.clWhite }));
        Assert.That(set.ToString(), Is.EqualTo("[clRed, clBlue, clWhite]"));
    }

    [Test]
    public void Range_includes_both_ends_and_is_empty_when_reversed()
    {
        Assert.That(DelphiSet<byte>.Range(3, 6).ToList(), Is.EqualTo(new byte[] { 3, 4, 5, 6 }));
        Assert.That(DelphiSet<byte>.Range(6, 3).IsEmpty, Is.True);
        Assert.That(DelphiSet<byte>.Range(0, 255).Count, Is.EqualTo(256));
    }

    [Test]
    public void Include_and_exclude_return_new_sets()
    {
        var set = DelphiSet<TColor>.Of(TColor.clRed);
        var included = set.Include(TColor.clGreen);
        var excluded = included.Exclude(TColor.clRed);
        Assert.Multiple(() =>
        {
            Assert.That(set.Count, Is.EqualTo(1), "the original is unchanged");
            Assert.That(included, Is.EqualTo(DelphiSet<TColor>.Of(TColor.clRed, TColor.clGreen)));
            Assert.That(excluded, Is.EqualTo(DelphiSet<TColor>.Of(TColor.clGreen)));
            Assert.That(set + TColor.clBlue, Is.EqualTo(set.Include(TColor.clBlue)));
            Assert.That(included - TColor.clGreen, Is.EqualTo(set));
        });
    }

    [Test]
    public void Union_difference_and_intersection_work_as_in_Delphi()
    {
        var a = DelphiSet<int>.Of(1, 2, 3);
        var b = DelphiSet<int>.Of(3, 4);
        Assert.Multiple(() =>
        {
            Assert.That(a + b, Is.EqualTo(DelphiSet<int>.Of(1, 2, 3, 4)));
            Assert.That(a - b, Is.EqualTo(DelphiSet<int>.Of(1, 2)));
            Assert.That(a * b, Is.EqualTo(DelphiSet<int>.Of(3)));
        });
    }

    [Test]
    public void Subset_and_superset_compare_like_Delphi_less_or_equal_and_greater_or_equal()
    {
        var small = DelphiSet<char>.Of('a', 'b');
        var large = DelphiSet<char>.Range('a', 'z');
        Assert.Multiple(() =>
        {
            Assert.That(small <= large, Is.True);
            Assert.That(large >= small, Is.True);
            Assert.That(large <= small, Is.False);
            Assert.That(small <= DelphiSet<char>.Of('b', 'a'), Is.True, "a set is a subset of an equal set");
            Assert.That(DelphiSet<char>.Empty <= small, Is.True);
        });
    }

    [Test]
    public void Equal_sets_are_equal_and_hash_alike()
    {
        var a = DelphiSet<TColor>.Of(TColor.clRed, TColor.clBlue);
        var b = DelphiSet<TColor>.Of(TColor.clBlue, TColor.clRed);
        Assert.Multiple(() =>
        {
            Assert.That(a == b, Is.True);
            Assert.That(a != b, Is.False);
            Assert.That(a.GetHashCode(), Is.EqualTo(b.GetHashCode()));
            Assert.That(a.Equals((object)b), Is.True);
        });
    }

    [Test]
    public void Elements_in_every_64_bit_word_are_kept_apart()
    {
        byte[] elements = [0, 63, 64, 127, 128, 191, 192, 255];
        var set = DelphiSet<byte>.Of(elements);
        Assert.That(set.ToList(), Is.EqualTo(elements));
        Assert.That(set.Exclude(64).Contains(63), Is.True);
    }

    [Test]
    public void Boolean_sets_hold_false_and_true()
    {
        var set = DelphiSet<bool>.Of(true);
        Assert.That(set.Contains(true), Is.True);
        Assert.That(set.Contains(false), Is.False);
        Assert.That((set + false).ToList(), Is.EqualTo(new[] { false, true }));
    }

    [TestCase(-1)]
    [TestCase(256)]
    public void Ordinals_outside_0_to_255_are_rejected(int element)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DelphiSet<int>.Of(element));
    }

    [Test]
    public void Element_types_without_ordinals_are_rejected()
    {
        var error = Assert.Throws<TypeInitializationException>(() => DelphiSet<double>.Of(1.0));
        Assert.That(error!.InnerException, Is.TypeOf<NotSupportedException>());
    }
}
