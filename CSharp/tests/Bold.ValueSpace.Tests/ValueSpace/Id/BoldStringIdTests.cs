// Delphi: Source/ValueSpace/Id/BoldStringId.pas, Source/ValueSpace/Id/BoldGlobalId.pas
// Port map: CSharp/port/units/BoldStringId.md
using System.Xml;

namespace Bold.Tests;

public class BoldStringIdTests
{
    [Test]
    [Covers(typeof(TBoldStringID), "*")]
    [Covers("BoldStringId", "HashString", "TBoldXMLStringIdStreamer", "TBoldXMLStringIdStreamer.WriteObject", "TBoldXMLStringIdStreamer.ReadObject",
        "TBoldXMLStringIdStreamer.CreateObject", "TBoldXMLStringIdStreamer.GetStreamName", "initialization")]
    public void String_ids_compare_by_value_and_stream()
    {
        var a = new TBoldStringID(2, true) { AsString = "ABC" };
        var b = new TBoldStringID(5, false) { AsString = "ABC" };
        Assert.That(a.IsEqual(b) && a.Hash == b.Hash && a.IsStorable, Is.True);
        Assert.That(a.IsEqual(new TBoldStringID { AsString = "abc" }), Is.False, "case matters");
        // h = ((h shl 3) and $7FFFFFFF or (h shr 29)) xor ord(c), for 'A', 'B', 'C'.
        Assert.That(a.Hash, Is.EqualTo((uint)((((('A' << 3) | 0) ^ 'B') << 3 ^ 'C') & 0x7FFFFFFF) | 0));
        var clone = (TBoldStringID)a.CloneWithClassId(7, false);
        Assert.That((clone.AsString, clone.TopSortedIndex, clone.TopSortedIndexExact), Is.EqualTo(("ABC", 7, false)));

        var model = new TMoldModel(null, "M");
        _ = new TMoldClass(model, "A") { SuperClass = model.RootClass };
        _ = new TMoldClass(model, "B") { SuperClass = model.RootClass };
        var manager = new TBoldDefaultXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry(), model);
        var doc = new XmlDocument();
        manager.NewRootNode(doc, "Root").WriteObject("", a);
        var r = (TBoldStringID)manager.GetRootNode(doc, "Root").ReadObject("")!;
        Assert.That((r.AsString, r.TopSortedIndex), Is.EqualTo(("ABC", 2)));
    }

    [Test]
    [Covers(typeof(TBoldGlobalId), "*")]
    [Covers("BoldGlobalId", "BOLDGLOBALIDNAME", "TBoldXMLGlobalIdStreamer", "TBoldXMLGlobalIdStreamer.WriteObject", "TBoldXMLGlobalIdStreamer.ReadObject",
        "TBoldXMLGlobalIdStreamer.CreateObject", "TBoldXMLGlobalIdStreamer.GetStreamName", "initialization")]
    public void Global_ids_are_not_storable_and_take_the_class_name_from_the_model()
    {
        var id = new TBoldGlobalId("{G}", 1, true, "Person");
        Assert.That((id.AsString, id.ClassExpressionName, id.IsStorable), Is.EqualTo(("{G}", "Person", false)));
        Assert.That(id.IsEqual(id.CloneWithClassId(1, true)) && !id.IsEqual(new TBoldGlobalId("{H}", 1, true, "Person")), Is.True);
        Assert.That(id.GetStreamName(), Is.EqualTo(BoldGlobalId.BOLDGLOBALIDNAME));

        var model = new TMoldModel(null, "M");
        _ = new TMoldClass(model, "Person") { SuperClass = model.RootClass };
        var manager = new TBoldDefaultXMLStreamManager(TBoldXMLStreamerRegistry.MainStreamerRegistry(), model);
        var doc = new XmlDocument();
        manager.NewRootNode(doc, "Root").WriteObject("", id);
        var r = (TBoldGlobalId)manager.GetRootNode(doc, "Root").ReadObject("")!;
        Assert.That((r.AsString, r.ClassExpressionName), Is.EqualTo(("{G}", "Person")));
    }
}
