// Delphi: Source/Common/Support/BoldBase64.pas, BoldExternalizedReferences.pas, BoldXMLStreaming.pas
// Port map: CSharp/port/units/BoldBase64.md
using System.Xml;

namespace Bold.Tests;

public class BoldStreamingSupportTests
{
    [Test]
    [Covers(typeof(BoldBase64), "*")]
    public void Base64_codes_have_the_Delphi_values() =>
        PortAssert.ConstantsMatchDelphi(typeof(BoldBase64), "Source/Common/Support/BoldBase64.pas");

    [TestCase("")]
    [TestCase("a")]
    [TestCase("ab")]
    [TestCase("abc")]
    [TestCase("this needs to be Base64-encoded")]
    [TestCase("\u0000ÿ\u0080binary\u0001")]
    [Covers(typeof(TBase64), "*")]
    public void Base64_matches_the_standard_and_round_trips(string data)
    {
        var b = new TBase64();
        string encoded = "", decoded = "";
        Assert.That(b.EncodeData(data, ref encoded), Is.EqualTo(BoldBase64.BASE64_OK));
        Assert.That(encoded, Is.EqualTo(Convert.ToBase64String(System.Text.Encoding.Latin1.GetBytes(data))));
        Assert.That(b.DecodeData(encoded, ref decoded), Is.EqualTo(BoldBase64.BASE64_OK));
        Assert.That(decoded, Is.EqualTo(data));
    }

    [Test]
    [Covers(typeof(TBase64), nameof(TBase64.DecodeData), nameof(TBase64.FilterdecodeInput))]
    public void Base64_reports_bad_input()
    {
        var b = new TBase64();
        string o = "";
        Assert.That(b.DecodeData("YWJj\r\nZA==", ref o), Is.EqualTo(BoldBase64.BASE64_OK), "line breaks are filtered");
        Assert.That(o, Is.EqualTo("abcd"));
        Assert.That(b.DecodeData("YWJ", ref o), Is.EqualTo(BoldBase64.BASE64_LENGTH));
        Assert.That(b.DecodeData("YW=jYWJj", ref o), Is.EqualTo(BoldBase64.BASE64_DATALEFT));
        Assert.That(b.DecodeData("YW=j", ref o), Is.EqualTo(BoldBase64.BASE64_PADDING));
        b.FilterdecodeInput = false;
        Assert.That(b.DecodeData("YW*j", ref o), Is.EqualTo(BoldBase64.BASE64_INVALID));
    }

    private sealed class Owned : IDisposable
    {
        public bool Disposed;
        public void Dispose() => Disposed = true;
    }

    [Test]
    [Covers(typeof(TBoldExternalizedReferenceList), "*")]
    public void Externalized_references_replace_and_free()
    {
        var list = new TBoldExternalizedReferenceList { ManageReferencedObject = true };
        object referee = new();
        var first = new Owned();
        var second = new Owned();
        list.SetReferencedObjects(referee, first);
        Assert.That(list.ReferencedObjects(referee), Is.SameAs(first));
        list.SetReferencedObjects(referee, second);
        Assert.That(first.Disposed, Is.True, "the list manages referenced objects");
        Assert.That(list.Count, Is.EqualTo(1));
        list.SetReferencedObjects(referee, null);
        Assert.That(list.Count, Is.EqualTo(0));
        Assert.That(list.ReferencedObjects(new object()), Is.Null);
        list.Dispose();
    }

    private sealed class Point : TBoldNonRefCountedObject, IBoldStreamable
    {
        public int X;
        public string GetStreamName() => "Point";
    }

    private sealed class PointStreamer : TBoldXMLObjectStreamer
    {
        protected override string GetStreamName() => "Point";
        public override object CreateObject() => new Point();
        public override void WriteObject(TBoldInterfacedObject Obj, TBoldXMLNode Node) => Node.WriteSubNodeInteger("X", ((Point)Obj).X);
        public override void ReadObject(object Obj, TBoldXMLNode Node) => ((Point)Obj).X = Node.ReadSubNodeInteger("X");
    }

    [Test]
    [Covers(typeof(TBoldXMLNode), "*")]
    [Covers(typeof(TBoldXMLStreamManager), "*")]
    [Covers(typeof(TBoldXMLStreamerRegistry), "*")]
    [Covers(typeof(TBoldXMLStreamerList), "*")]
    [Covers(typeof(TBoldXMLStreamer), "*")]
    [Covers(typeof(TBoldXMLObjectStreamer), "*")]
    [Covers(typeof(TBoldXMLStreamStateManager), "*")]
    [Covers(typeof(BoldXMLStreaming), "*")]
    public void XML_nodes_write_and_read_typed_values()
    {
        PortAssert.ConstantsMatchDelphi(typeof(BoldXMLStreaming), "Source/Common/Support/BoldXMLStreaming.pas");
        var registry = new TBoldXMLStreamerRegistry(TBoldXMLStreamerRegistry.MainStreamerRegistry());
        registry.RegisterStreamer(new PointStreamer());
        var manager = new TBoldXMLStreamManager(registry);
        var doc = new XmlDocument();
        TBoldXMLNode root = manager.NewRootNode(doc, "Root");
        var when = new DateTime(2024, 3, 7, 14, 5, 9, 12);
        root.WriteSubNodeString("S", "hello");
        root.WriteSubNodeInteger("I", 42);
        root.WriteSubNodeBoolean("B", true);
        root.WriteSubNodeFloat("F", 3.25);
        root.WriteSubNodeDateTime("DT", when);
        root.WriteSubNodeData("Bin", "a\u0001b");
        root.WriteSubNodeData("Text", "plain\ttext");
        root.WriteSubNodeObject("P", "Point", new Point { X = 7 });
        root.WriteSubNodeObject("Nothing", "Point", null);
        root.NewSubNode("C").WriteCurrency(12.5m);
        Assert.That(doc.OuterXml, Does.Contain("<F>3.25</F>"));
        Assert.That(doc.OuterXml, Does.Contain("dt=\"binary.base64\""));

        var reread = new XmlDocument { PreserveWhitespace = true };
        reread.LoadXml(doc.OuterXml);
        TBoldXMLNode r = manager.GetRootNode(reread, "Root");
        Assert.That(r.Accessor, Is.EqualTo("Root"));
        Assert.That(r.ReadSubNodeString("S"), Is.EqualTo("hello"));
        Assert.That(r.ReadSubNodeInteger("I"), Is.EqualTo(42));
        Assert.That(r.ReadSubNodeBoolean("B"), Is.True);
        Assert.That(r.ReadSubNodeFloat("F"), Is.EqualTo(3.25));
        Assert.That(r.ReadSubNodeDateTime("DT"), Is.EqualTo(when));
        Assert.That(r.ReadSubNodeData("Bin"), Is.EqualTo("a\u0001b"));
        Assert.That(r.ReadSubNodeData("Text"), Is.EqualTo("plain\ttext"));
        Assert.That(((Point)r.ReadSubNodeObject("P", "Point")!).X, Is.EqualTo(7));
        Assert.That(r.ReadSubNodeObject("Nothing", "Point"), Is.Null);
        Assert.That(r.GetSubNode("C")!.ReadCurrency(), Is.EqualTo(12.5m));
        Assert.That(r.GetSubNode("Missing"), Is.Null);
        Assert.That(r.GetSubNode("Nothing")!.IsNull, Is.True);
        Assert.That(r.GetSubNode("Nothing")!.IsEmpty(), Is.True);

        r.AddStateObject("ctx", this);
        Assert.That(r.GetStateObject("ctx"), Is.SameAs(this));
        r.RemoveStateObject("ctx");
        Assert.That(r.GetStateObject("ctx"), Is.Null);

        Assert.Throws<EBold>(() => manager.GetRootNode(reread, "Other"));
        Assert.Throws<EBold>(() => manager.NewRootNode(reread, "Root"));
        Assert.Throws<EBold>(() => manager.GetRootNode(new XmlDocument(), ""));
        Assert.Throws<EBoldInternal>(() => r.GetSubNode("P")!.ReadObject("Unknown"), "no streamer for the type");
    }
}
