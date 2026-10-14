// Delphi: Source/Common/Support/BoldIndex.pas, Source/Common/Support/BoldHashIndexes.pas
// Port map: CSharp/port/units/BoldIndex.md, CSharp/port/units/BoldHashIndexes.md
namespace Bold.Tests;

public class BoldIndexTests
{
    internal sealed class Named(string name, uint id = 0)
    {
        public string Name = name;
        public uint Id = id;
        public Guid Guid = Guid.NewGuid();
        public object Key = new();
    }

    private sealed class NameIndex : TBoldStringHashIndex
    {
        protected override string ItemAsKeyString(object? Item) => ((Named)Item!).Name;
    }

    private sealed class CaseNameIndex : TBoldCaseSensitiveStringHashIndex
    {
        protected override string ItemAsKeyString(object? Item) => ((Named)Item!).Name;
    }

    private sealed class IdIndex : TBoldCardinalHashIndex
    {
        protected override uint ItemAsKeyCardinal(object? Item) => ((Named)Item!).Id;
        public object? ById(uint id) => FindByCardinal(id);
    }

    private sealed class GuidIndex : TBoldGUIDHashIndex
    {
        protected override Guid ItemASKeyGUID(object? O) => ((Named)O!).Guid;
        public object? ByGuid(Guid g) => FindByGUID(g);
    }

    private sealed class ObjIndex : TBoldObjectHashIndex
    {
        protected override object? ItemAsKeyObject(object? Item) => ((Named)Item!).Key;
        public object? ByObject(object o) => FindByObject(o);
    }

    private sealed class ClassIndex : TBoldClassHashIndex
    {
        protected override Type ItemAsKeyClass(object? O) => O!.GetType();
        public object? ByClass(Type t) => FindByClass(t);
    }

    [Test]
    [Covers(typeof(TBoldHashIndex), "Add", "Find", "FindAll", "Remove", "Resize", "Clear", "GetCount", "GetAny")]
    [Covers(typeof(TBoldIndex), "Count", "Any")]
    [Covers(typeof(TBoldStringHashIndex), "*")]
    public void String_index_finds_ignoring_case_and_survives_resizing()
    {
        var index = new NameIndex();
        var items = Enumerable.Range(0, 1000).Select(i => new Named("Item" + i)).ToList();
        foreach (Named n in items)
        {
            index.Add(n);
        }

        Assert.That(index.Count, Is.EqualTo(1000));
        Assert.That(index.FindByString("ITEM500"), Is.SameAs(items[500]));
        Assert.That(index.FindByString("missing"), Is.Null);
        index.Add(new Named("item500"));
        var all = new TList();
        index.FindAllByString("Item500", all);
        Assert.That(all.Count, Is.EqualTo(2));
        index.Remove(items[500]);
        Assert.That(index.Count, Is.EqualTo(1000));
        Assert.That(((Named)index.FindByString("ITEM500")!).Name, Is.EqualTo("item500"));
        Assert.That(index.Any, Is.Not.Null);
        index.Clear();
        Assert.That(index.Count, Is.EqualTo(0));
        Assert.That(index.Any, Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldCaseSensitiveStringHashIndex), "*")]
    public void Case_sensitive_index_tells_case_apart()
    {
        var index = new CaseNameIndex();
        var a = new Named("Abc");
        index.Add(a);
        Assert.That(index.FindByString("Abc"), Is.SameAs(a));
        Assert.That(index.FindByString("abc"), Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldHashIndex), "IsCorrectlyIndexed", "RemoveChanged")]
    [Covers(typeof(TBoldIndex), "ItemChanged")]
    public void Changed_items_are_reindexed()
    {
        var index = new CaseNameIndex();
        var a = new Named("Old");
        index.Add(a);
        a.Name = "New";
        Assert.That(index.IsCorrectlyIndexed(a), Is.False);
        index.ItemChanged(a);
        Assert.That(index.FindByString("New"), Is.SameAs(a));
        Assert.That(index.FindByString("Old"), Is.Null);
        Assert.That(index.Count, Is.EqualTo(1));
    }

    [Test]
    [Covers(typeof(TBoldHashIndexTraverser), "*")]
    [Covers(typeof(TBoldHashIndex), "CreateTraverser", "GetSupportsTraverser")]
    [Covers(typeof(TBoldIndex), "SupportsTraverser")]
    public void Traverser_visits_every_item_once()
    {
        var index = new IdIndex();
        for (uint i = 0; i < 100; i++)
        {
            index.Add(new Named("n", i));
        }

        Assert.That(index.SupportsTraverser, Is.True);
        var seen = new HashSet<uint>();
        using (TBoldIndexTraverser t = index.CreateTraverser())
        {
            while (!t.EndOfList)
            {
                Assert.That(seen.Add(((Named)t.Item!).Id), Is.True);
                t.Next();
            }
        }

        Assert.That(seen, Has.Count.EqualTo(100));
    }

    [Test]
    [Covers(typeof(TBoldCardinalHashIndex), "*")]
    [Covers(typeof(TBoldGUIDHashIndex), "*")]
    [Covers(typeof(TBoldObjectHashIndex), "*")]
    [Covers(typeof(TBoldClassHashIndex), "*")]
    public void Key_type_indexes_find_by_their_key()
    {
        var n = new Named("x", 42);
        var ids = new IdIndex();
        var guids = new GuidIndex();
        var objs = new ObjIndex();
        var classes = new ClassIndex();
        ids.Add(n);
        guids.Add(n);
        objs.Add(n);
        classes.Add(n);
        Assert.That(ids.ById(42), Is.SameAs(n));
        Assert.That(ids.ById(43), Is.Null);
        Assert.That(guids.ByGuid(n.Guid), Is.SameAs(n));
        Assert.That(guids.ByGuid(Guid.NewGuid()), Is.Null);
        Assert.That(objs.ByObject(n.Key), Is.SameAs(n));
        Assert.That(objs.ByObject(new object()), Is.Null);
        Assert.That(classes.ByClass(typeof(Named)), Is.SameAs(n));
        Assert.That(classes.ByClass(typeof(string)), Is.Null);
    }

    [Test]
    [Covers(typeof(TBoldStringKey), "*")]
    [Covers(typeof(TBoldStringCompareMode))]
    public void String_hash_matches_the_Delphi_formula()
    {
        // Delphi: Result := ((Result shl 3) and MaxInt) or (Result shr 29) xor Ord(c), for "AB".
        uint expected = 0;
        foreach (char c in "AB")
        {
            expected = ((expected << 3) & 2147483647) | ((expected >> 29) ^ c);
        }

        Assert.That(TBoldStringKey.HashString("AB", TBoldStringCompareMode.bscCaseDependent), Is.EqualTo(expected));
        Assert.That(TBoldStringKey.HashString("ab", TBoldStringCompareMode.bscCaseIndependent),
            Is.EqualTo(TBoldStringKey.HashString("AB", TBoldStringCompareMode.bscCaseIndependent)));
        Assert.That(TBoldStringKey.HashString("äb", TBoldStringCompareMode.bscLocaleCaseIndependent),
            Is.EqualTo(TBoldStringKey.HashString("ÄB", TBoldStringCompareMode.bscLocaleCaseIndependent)));
        PortAssert.EnumsMatchDelphi("Source/Common/Support/BoldHashIndexes.pas", typeof(TBoldStringCompareMode));
    }

    [Test]
    [Covers(typeof(TBoldIntegerIndex), "*")]
    public void Integer_index_keeps_order_and_nil()
    {
        var index = new TBoldIntegerIndex();
        object a = new(), b = new();
        index.Add(a);
        index.Add(null);
        index.Add(b);
        Assert.That(index.SupportsNilItems, Is.True);
        Assert.That(index.Find(2), Is.SameAs(b));
        Assert.That(index.IndexOf(b), Is.EqualTo(2));
        index.Exchange(0, 2);
        Assert.That(index[0], Is.SameAs(b));
        index.Move(0, 1);
        Assert.That(index[1], Is.SameAs(b));
        index.RemoveByIndex(0);
        Assert.That(index.Count, Is.EqualTo(2));
    }
}
