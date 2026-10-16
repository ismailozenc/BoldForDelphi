namespace Bold.Tests;

public class DBTests
{
    [Test]
    public void Memory_data_sets_navigate_edit_and_locate()
    {
        var ds = new TMemoryDataSet();
        ds.FieldDefs.Add("Id", TFieldType.ftInteger);
        ds.FieldDefs.Add("Name", TFieldType.ftString, 20);
        ds.AddRecord(1, "Ann");
        ds.AddRecord(2, "Bob");
        ds.Open();
        Assert.That((ds.RecordCount, ds.FieldCount, ds.Eof), Is.EqualTo((2, 2, false)));
        var names = new List<string>();
        for (ds.First(); !ds.Eof; ds.Next())
        {
            names.Add(ds.FieldByName("name").AsString);
        }

        Assert.That(names, Is.EqualTo(new[] { "Ann", "Bob" }));
        Assert.That(ds.Locate("Name", "bob", DelphiSet<TLocateOption>.Of(TLocateOption.loCaseInsensitive)), Is.True);
        Assert.That(ds.FieldByName("Id").AsInteger, Is.EqualTo(2));
        Assert.That(ds.Locate("Id;Name", new object?[] { 1, "Ann" }, default), Is.True);
        ds.Edit();
        ds["Name"] = "Anna";
        ds.Post();
        Assert.That(ds.FieldByName("Name").AsString, Is.EqualTo("Anna"));
        ds.Append();
        ds.FieldByName("Id").AsInteger = 3;
        ds.Post();
        Assert.That((ds.RecordCount, ds.FieldByName("Name").IsNull), Is.EqualTo((3, true)));
        ds.Delete();
        Assert.That(ds.RecordCount, Is.EqualTo(2));
        Assert.That(ds.MoveBy(-5), Is.EqualTo(-1));
        Assert.Throws<EDatabaseError>(() => ds.FieldByName("Missing"));
        ds.Close();
        Assert.Throws<EDatabaseError>(ds.First);
    }

    [Test]
    public void Collections_add_move_and_notify()
    {
        var c = new TCollection(typeof(TCollectionItem));
        TCollectionItem a = c.Add(), b = c.Add();
        Assert.That((c.Count, a.Index, b.ID), Is.EqualTo((2, 0, 1)));
        b.Index = 0;
        Assert.That(c[0], Is.SameAs(b));
        a.Collection = null;
        Assert.That(c.Count, Is.EqualTo(1));
        Assert.That(c.FindItemID(1), Is.SameAs(b));
        c.Clear();
        Assert.That(c.Count, Is.Zero);
    }
}
