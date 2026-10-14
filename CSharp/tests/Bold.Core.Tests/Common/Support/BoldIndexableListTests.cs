// Delphi: Source/Common/Support/BoldIndexableList.pas, BoldNamedValueList.pas, BoldSharedStrings.pas
// Port map: CSharp/port/units/BoldIndexableList.md
namespace Bold.Tests;

public class BoldIndexableListTests
{
    private sealed class Item(string name) : IDisposable
    {
        public string Name = name;
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    private sealed class NameIndex : TBoldCaseSensitiveStringHashIndex
    {
        protected override string ItemAsKeyString(object? Item) => ((Item)Item!).Name;
    }

    private sealed class NamedList : TBoldIndexableList
    {
        private readonly int ix;
        public NamedList() => ix = AddIndex(new NameIndex());
        public Item? ByName(string n) => (Item?)((NameIndex)Indexes(ix)!).FindByString(n);
        public Item At(int i) => (Item)this[i]!;
        public void Put(int i, Item v) => this[i] = v;
    }

    [Test]
    [Covers(typeof(TBoldIndexableList), "*")]
    [Covers(typeof(TBoldUnOrderedIndexableList), "Add", "Remove", "Count", "ItemChanged", "AddIndex", "Indexes", "OwnsEntries", "Clear")]
    public void List_keeps_order_and_name_index_in_step()
    {
        var list = new NamedList();
        var a = new Item("a");
        var b = new Item("b");
        var c = new Item("c");
        list.Add(a);
        list.Add(b);
        list.Insert(0, c);
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list.At(0), Is.SameAs(c));
        Assert.That(list.ByName("b"), Is.SameAs(b));
        list.Move(0, 2);
        Assert.That(list.IndexOf(c), Is.EqualTo(2));
        list.Sort((x, y) => string.CompareOrdinal(((Item)x!).Name, ((Item)y!).Name));
        Assert.That(list.At(0).Name + list.At(1).Name + list.At(2).Name, Is.EqualTo("abc"));
        b.Name = "bb";
        list.ItemChanged(b);
        Assert.That(list.ByName("bb"), Is.SameAs(b));
        list.RemoveByIndex(0);
        Assert.That(a.Disposed, Is.True, "the list owns its entries");
        Assert.That(list.ByName("a"), Is.Null);
        var d = new Item("d");
        list.Put(0, d);
        Assert.That(b.Disposed, Is.True);
        Assert.That(list.ByName("d"), Is.SameAs(d));
        list.Clear();
        Assert.That(c.Disposed, Is.True);
        Assert.That(list.Count, Is.EqualTo(0));
    }

    [Test]
    [Covers(typeof(TBoldUnOrderedIndexableList), "CreateTraverser", "Any", "Dispose")]
    [Covers(typeof(TBoldIndexableListTraverser), "*")]
    public void Unordered_list_traverses_and_rejects_nil()
    {
        var list = new TBoldSharedStringCache();
        Assert.Throws<EBold>(() => list.Add(null));
        list.Add(new TBoldSharedStringHolder("x"));
        list.Add(new TBoldSharedStringHolder("y"));
        Assert.That(list.Any, Is.Not.Null);
        int n = 0;
        using (TBoldIndexableListTraverser t = list.CreateTraverser()!)
        {
            while (!t.EndOfList)
            {
                Assert.That(t.CurrentIndex, Is.EqualTo(n));
                n++;
                t.Next();
            }
        }

        Assert.That(n, Is.EqualTo(2));
        list.Dispose();
    }

    [Test]
    [Covers(typeof(BoldIndexableList), "SetIndexVariable")]
    public void SetIndexVariable_sets_once()
    {
        int ix = -1;
        BoldIndexableList.SetIndexVariable(ref ix, 3);
        Assert.That(ix, Is.EqualTo(3));
        BoldIndexableList.SetIndexVariable(ref ix, 3);
        Assert.That(ix, Is.EqualTo(3));
    }

    [Test]
    [Covers(typeof(TBoldNamedValueList), "*")]
    [Covers(typeof(TBoldNamedValueListEntry), "*")]
    public void Named_value_list_reads_and_writes_comma_text()
    {
        var list = new TBoldNamedValueList { CommaText = "a=1,\"b=two words\",c=" };
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list.ValueByName("b"), Is.EqualTo("two words"));
        Assert.That(list.ValueByName("missing"), Is.EqualTo(""));
        list.SetValueByname("a", "11");
        list.SetValueByname("d", "4");
        object o = new();
        list.SetObjectByName("c", o);
        Assert.That(list.ObjectByName("c"), Is.SameAs(o));
        Assert.That(list[0].Value, Is.EqualTo("11"));
        Assert.That(list.ItemByName("d")!.Name, Is.EqualTo("d"));
        list.RemoveName("b");
        Assert.That(list.CommaText, Is.EqualTo("a=11,c=,d=4"));
        var strings = new TStringList();
        list.AddToStrings(strings);
        Assert.That(strings.Objects(1), Is.SameAs(o));
    }

    [Test]
    [Covers(typeof(TBoldSharedStringManager), "GetSharedString", "GarbageCollect", "InfoString")]
    [Covers(typeof(TBoldSharedStringCache), "*")]
    [Covers(typeof(BoldSharedStrings), "BoldSharedStringManager")]
    public void Shared_strings_return_one_instance_per_value()
    {
        var m = new TBoldSharedStringManager();
        string first = m.GetSharedString(new string("Name".ToCharArray()));
        string second = m.GetSharedString(new string("Name".ToCharArray()));
        Assert.That(ReferenceEquals(first, second), Is.True);
        Assert.That(m.GetSharedString(""), Is.EqualTo(""));
        m.GarbageCollect(true);
        Assert.That(ReferenceEquals(m.GetSharedString(new string("Name".ToCharArray())), first), Is.True);
        Assert.That(m.InfoString, Does.StartWith("Number of shared strings: 1"));
        Assert.That(BoldSharedStrings.BoldSharedStringManager(), Is.SameAs(BoldSharedStrings.BoldSharedStringManager()));
    }
}
