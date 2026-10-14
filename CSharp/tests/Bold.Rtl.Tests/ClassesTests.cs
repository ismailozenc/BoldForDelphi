namespace Bold.Tests;

/// <summary>Delphi Classes semantics (TStrings, TStringList, TList) the port depends on.</summary>
public class ClassesTests
{
    [Test]
    public void CommaText_quotes_like_Delphi()
    {
        var s = new TStringList { CommaText = "a,\"b c\",d e,'x'" };
        Assert.That(s.ToStringArray(), Is.EqualTo(new[] { "a", "b c", "d", "e", "'x'" }));
        s.Clear();
        s.Add("plain");
        s.Add("with space");
        s.Add("with\"quote");
        Assert.That(s.CommaText, Is.EqualTo("plain,\"with space\",\"with\"\"quote\""));
    }

    [Test]
    public void Names_and_values()
    {
        var s = new TStringList();
        s.Add("Name=Bold");
        s.Add("Version=4");
        Assert.That(s.Names(1), Is.EqualTo("Version"));
        Assert.That(s.Values("NAME"), Is.EqualTo("Bold"));
        s.SetValues("Version", "5");
        s.SetValues("New", "x");
        Assert.That(s.Text, Is.EqualTo("Name=Bold\r\nVersion=5\r\nNew=x\r\n"));
        Assert.That(s.IndexOfName("new"), Is.EqualTo(2));
    }

    [Test]
    public void Sorted_list_finds_and_handles_duplicates()
    {
        var s = new TStringList { Sorted = true, Duplicates = TDuplicates.dupIgnore };
        s.Add("b");
        s.Add("A");
        s.Add("c");
        s.Add("B");
        Assert.That(s.ToStringArray(), Is.EqualTo(new[] { "A", "b", "c" }));
        Assert.That(s.Find("C", out int index), Is.True);
        Assert.That(index, Is.EqualTo(2));
        s.Duplicates = TDuplicates.dupError;
        Assert.Throws<EListError>(() => s.Add("a"));
    }

    [Test]
    public void List_operations()
    {
        var l = new TList();
        object a = new(), b = new(), c = new();
        l.Add(a);
        l.Add(null);
        l.Add(b);
        l.Insert(0, c);
        Assert.That(l.Count, Is.EqualTo(4));
        Assert.That(l.IndexOf(b), Is.EqualTo(3));
        l.Pack();
        Assert.That(l.Count, Is.EqualTo(3));
        l.Move(0, 2);
        Assert.That(l.Last(), Is.SameAs(c));
        Assert.That(l.Extract(a), Is.SameAs(a));
        Assert.That(l.Remove(a), Is.EqualTo(-1));
        Assert.Throws<EListError>(() => _ = l[10]);
    }
}
