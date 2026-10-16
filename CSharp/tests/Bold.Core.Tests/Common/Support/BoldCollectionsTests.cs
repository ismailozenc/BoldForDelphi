// Delphi: Source/Common/Support/BoldCollections.pas, Source/Common/Support/BoldGUIDUtils.pas
// Port map: CSharp/port/units/BoldCollections.md
namespace Bold.Tests;

public class BoldCollectionsTests
{
    private sealed class Item(TCollection? Collection) : TBoldUniquelyNamedCollectionItemWithNameStorage(Collection)
    {
        public string Name { get => UniqueName; set => UniqueName = value; }
    }

    [Test]
    [Covers(typeof(TBoldCollectionWithUniquelyNamedItems), "*")]
    [Covers(typeof(TBoldUniquelyNamedCollectionItem), "*")]
    [Covers(typeof(TBoldUniquelyNamedCollectionItemWithNameStorage), "*")]
    [Covers(typeof(TBoldUniqueNameItemIndex), "*")]
    public void Items_have_unique_names_found_through_the_index()
    {
        var owner = new TPersistent();
        var collection = new TBoldCollectionWithUniquelyNamedItems(owner, typeof(Item));
        var a = (Item)collection.Add();
        a.Name = "First";
        var b = (Item)collection.Add();
        b.Name = "Second";
        Assert.That(collection.ItemByName("second"), Is.SameAs(b), "names are found ignoring case");
        Assert.Throws<EBold>(() => b.Name = "FIRST");
        b.Name = "Renamed";
        Assert.That((collection.ItemByName("Renamed"), collection.ItemByName("Second")), Is.EqualTo((b, (Item?)null)));
        Assert.That(b.DisplayName, Is.EqualTo("Renamed"));
        Assert.That(b.GetNamePath(), Does.EndWith(".Renamed"));
        collection.Delete(0);
        Assert.That(collection.ItemByName("First"), Is.Null, "the index is rebuilt after changes");
    }

    [Test]
    [Covers("BoldGUIDUtils", "BoldCreateGUIDAsString", "BoldCreateGUIDWithBracketsAsString")]
    public void GUIDs_are_upper_case_and_braced()
    {
        string guid = BoldGUIDUtils.BoldCreateGUIDWithBracketsAsString();
        Assert.That(guid, Does.Match("^\\{[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}\\}$"));
        Assert.That(BoldGUIDUtils.BoldCreateGUIDAsString(true), Does.Match("^[0-9A-F-]{36}$"));
    }
}
