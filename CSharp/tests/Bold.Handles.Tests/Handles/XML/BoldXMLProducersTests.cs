// Delphi: Source/Handles/XML/BoldXMLProducers.pas
// Port map: CSharp/port/units/BoldXMLProducers.md
using System.Xml;
using BuildingClasses;

namespace Bold.Tests;

public class BoldXMLProducersTests
{
    [Test]
    [Covers(typeof(TBoldXMLProducer), "*")]
    [Covers(typeof(TBoldAbstractXMLProducer), "*")]
    [Covers(typeof(BoldXMLProducers), "*")]
    [Covers("BoldXMLProducers", "TBoldXMLProducerOption", "TBoldXMLProducerOptions", "TBoldProduceEvent")]
    public void A_producer_writes_objects_as_XML_with_their_id_strings()
    {
        using var s = new HandlesSample();
        TPerson ann = s.NewPerson("Ann", "Smith");
        ann.IsMarried = true;
        var manipulator = new TBoldManipulator(s.Components) { BoldSystemHandle = s.SystemHandle };
        var upper = (TBoldManipulatorMapper)manipulator.Mappers.Add();
        upper.MappingName = "upper";
        upper.OnGet = e => e!.AsString.ToUpperInvariant();
        var producer = new TBoldXMLProducer(s.Components) { BoldManipulator = manipulator };
        Assert.That(producer.BoldManipulator, Is.SameAs(manipulator));
        producer.OnProduce = (parameters, doc) =>
        {
            XmlElement root = doc.CreateElement("people");
            doc.AppendChild(root);
            XmlElement person = producer.AddDomElementForBoldElement(root, ann, "", "", DelphiSet<TBoldXMLProducerOption>.Of(TBoldXMLProducerOption.xpoIncludeIdString));
            producer.AddDomElementsForBoldObjectAttributes(person, ann, []);
            producer.AddDomElementsForBoldObjectAttributes(person, ann, ["upper.lastName"], DelphiSet<TBoldXMLProducerOption>.Of(TBoldXMLProducerOption.xpoIncludeValue));
            var attributes = new TBoldStringList();
            attributes.Add("kind=note");
            producer.AddDomElement(root, "note", parameters.FastValues("note"), attributes);
        };
        Assert.That(producer.OnProduce, Is.Not.Null);

        var parameters = new TBoldStringList();
        parameters.Add("note=hello");
        string xml = producer.getDocumentAsString(parameters);
        string id = manipulator.IdStringForElement(ann);
        Assert.That(xml, Does.StartWith("<?xml version=\"1.0\"?><people>"));
        Assert.That(xml, Does.Contain($"<Person BoldID=\"{id}\">"));
        Assert.That(xml, Does.Contain($"<firstName BoldID=\"{id}:firstName\">Ann</firstName>"));
        Assert.That(xml, Does.Contain($"<isMarried BoldID=\"{id}:isMarried\">Y</isMarried>"));
        Assert.That(xml, Does.Contain("<upper>SMITH</upper>"), "a mapping names the element and converts the value");
        Assert.That(xml, Does.Contain("<note kind=\"note\">hello</note>"));
        Assert.That(xml, Does.Not.Contain("<home"), "only attributes");

        manipulator.Dispose();
        Assert.That(producer.BoldManipulator, Is.Null, "a disposed manipulator is forgotten");
        Assert.That(() => producer.getDocumentAsString(parameters), Throws.InstanceOf<EBold>().With.Message.Contains("BoldManipulator property not assigned"));
    }
}
