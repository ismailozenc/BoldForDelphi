// Delphi: Source/Common/Core/BoldContainers.pas
// Port map: CSharp/port/units/BoldContainers.md
namespace Bold.Tests;

public class BoldContainersTests
{
    private sealed class Disposable : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private static TBoldContainerOptions Owner => TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner);

    [Test]
    [Covers(typeof(TBoldIntegerArray), "*")]
    [Covers(typeof(TBoldArray), "Delete", "Exchange", "Move", "Sort", "Clear", "GetCount", "SetCount")]
    [Covers(typeof(TBoldContainer), "Count")]
    public void Integer_array_adds_moves_and_sorts()
    {
        var a = new TBoldIntegerArray(0, TBoldContainerOptions.Empty);
        foreach (int i in new[] { 5, 3, 9, 1 })
        {
            a.Add(i);
        }

        Assert.That(a.Count, Is.EqualTo(4));
        Assert.That(a.IndexOf(9), Is.EqualTo(2));
        a.Exchange(0, 3);
        Assert.That(a[0], Is.EqualTo(1));
        a.Move(0, 3);
        Assert.That(a[3], Is.EqualTo(1));
        a.Sort((x, y) => (int)x! - (int)y!);
        Assert.That(new[] { a[0], a[1], a[2], a[3] }, Is.EqualTo(new[] { 1, 3, 5, 9 }));
        a.Delete(1);
        Assert.That(a.Count, Is.EqualTo(3));
        a.Count = 5;
        Assert.That(a[4], Is.EqualTo(0), "new slots are EmptyItem");
        a.Clear();
        Assert.That(a.Count, Is.EqualTo(0));
    }

    [Test]
    [Covers(typeof(TBoldArray), "DeleteRange")]
    public void DeleteRange_removes_both_ends()
    {
        var a = new TBoldIntegerArray(0, TBoldContainerOptions.Empty);
        for (int i = 0; i < 6; i++)
        {
            a.Add(i);
        }

        a.DeleteRange(1, 3);
        Assert.That(new[] { a[0], a[1], a[2] }, Is.EqualTo(new[] { 0, 4, 5 }));
    }

    [Test]
    [Covers(typeof(TBoldObjectArray), "*")]
    public void Owning_object_array_disposes_removed_items()
    {
        var a = new TBoldObjectArray(0, Owner);
        var d1 = new Disposable();
        var d2 = new Disposable();
        a.Add(d1);
        a.Add(d2);
        a.Delete(0);
        Assert.That(d1.Disposed, Is.True);
        a.Dispose();
        Assert.That(d2.Disposed, Is.True);
    }

    [Test]
    [Covers(typeof(TBoldPointerArray), "*")]
    public void Pointer_array_keeps_nil_and_bad_indexes_raise()
    {
        var a = new TBoldPointerArray(0, TBoldContainerOptions.Empty);
        object o = new();
        a.Add(null);
        a.Add(o);
        Assert.That(a.IndexOf(o), Is.EqualTo(1));
        Assert.That(a.RemoveWithNil(o), Is.EqualTo(1));
        Assert.That(a.Count, Is.EqualTo(2), "RemoveWithNil keeps the slot");
        Assert.That(a[1], Is.Null);
        Assert.Throws<EBoldContainerError>(() => _ = a[5]);
    }
}
