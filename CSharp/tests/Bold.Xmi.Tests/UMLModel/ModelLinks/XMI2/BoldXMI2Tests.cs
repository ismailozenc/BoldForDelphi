// XMI 2: new in the port (Delphi Bold read and wrote XMI 1 only).
namespace Bold.Tests;

public class BoldXMI2Tests
{
    public static IEnumerable<TestCaseData> Fixtures => BoldUMLXMILinkTests.Fixtures;

    [TestCaseSource(nameof(Fixtures))]
    public void A_model_written_as_XMI_2_reads_back_as_the_same_model(string file)
    {
        TMoldModel original = XmiSamples.LoadFixture(file);
        var link = new TBoldUMLXMILink { FileName = XmiSamples.NewFile("model.xmi"), XMIVersion = TBoldXMIVersion.xmi21 };
        Assert.That(link.ExportModel(original), Is.True);
        TMoldModel back = link.ImportMoldModel();
        TMoldModel expected = TBoldUMLModelLink.UMLModelToMoldModel(TBoldUMLModelLink.MoldModelToUMLModel(original));
        Assert.That(BoldUMLXMILinkTests.Comparable(back), Is.EqualTo(BoldUMLXMILinkTests.Comparable(expected)));
    }

    [Test]
    public void The_XMI_2_file_is_UML_2()
    {
        TMoldModel original = XmiSamples.LoadFixture("AssociationClassExampleClasses.Delphi-Simple-ObjectSpace-AssociationClass.bld");
        var link = new TBoldUMLXMILink { FileName = XmiSamples.NewFile("model.xmi"), XMIVersion = TBoldXMIVersion.xmi21 };
        link.ExportModel(original);
        var doc = new System.Xml.XmlDocument();
        doc.Load(link.FileName);
        var ns = new System.Xml.XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("xmi", TBoldXMI2Exporter.XMINamespace);
        ns.AddNamespace("uml", TBoldXMI2Exporter.UMLNamespace);
        Assert.That(doc.DocumentElement!.LocalName, Is.EqualTo("XMI"));
        Assert.That(doc.DocumentElement.GetAttribute("version", TBoldXMI2Exporter.XMINamespace), Is.EqualTo("2.1"));
        var job = (System.Xml.XmlElement)doc.SelectSingleNode("//packagedElement[@xmi:type='uml:AssociationClass']", ns)!;
        Assert.That(job.GetAttribute("name"), Is.EqualTo("Job"));
        Assert.That(job.SelectNodes("ownedAttribute")!.Count, Is.EqualTo(2));
        Assert.That(job.SelectNodes("ownedEnd")!.Count, Is.EqualTo(2));
        Assert.That(job.GetAttribute("memberEnd").Split(' '), Has.Length.EqualTo(2));
        var end = (System.Xml.XmlElement)job.SelectSingleNode("ownedEnd")!;
        Assert.That(end.SelectSingleNode("upperValue/@value")!.Value, Is.EqualTo("*"));
        Assert.That(job.SelectSingleNode("xmi:Extension[@extender='Bold']/taggedValue[@tag='Bold.TableName']", ns), Is.Not.Null);
        Assert.That(doc.SelectNodes("//packagedElement[@xmi:type='uml:Association']", ns)!.Cast<System.Xml.XmlElement>().Any(a => a.GetAttribute("name") == "Job"), Is.False);
    }

    [Test]
    public void An_Enterprise_Architect_file_is_read()
    {
        var link = new TBoldUMLXMILink { FileName = XmiSamples.Write(Xmi2Samples.EnterpriseArchitect, "Hotel.xmi") };
        TMoldModel m = link.ImportMoldModel();
        Assert.That(m.Name, Is.EqualTo("EA_Model"));
        TMoldClass guest = m.Classes.ItemsByName("Guest")!;
        Assert.That(guest.TVByName("Bold.TableName"), Is.EqualTo("GUESTS"), "a tag in Enterprise Architect's extension");
        Assert.That(guest.Stereotype, Is.EqualTo("person"));
        TMoldAttribute name = guest.Attributes.ItemsByName("name")!;
        Assert.That(name.BoldType, Is.EqualTo("String"), "a type of Enterprise Architect's library");
        Assert.That(name.Visibility, Is.EqualTo(TVisibilityKind.vkPrivate));
        TMoldMethod checkout = guest.Methods.ItemsByName("Checkout")!;
        Assert.That(checkout.ReturnType, Is.EqualTo("Integer"));
        Assert.That(((TMoldParameter)checkout.Parameters[0]!).ParameterType, Is.EqualTo("Boolean"));
        Assert.That(m.Classes.ItemsByName("Suite")!.SuperClass!.Name, Is.EqualTo("Room"));

        TMoldAssociation stay = m.Associations.ItemsByName("Stay")!;
        TMoldRole guests = stay.Roles.ItemsByName("guests")!;
        Assert.That(guests.Multiplicity, Is.EqualTo("0..*"));
        Assert.That(guests.Navigable, Is.True, "navigability not said: navigable");
        Assert.That(guests.MoldClass!.Name, Is.EqualTo("Room"));
        Assert.That(stay.Roles.ItemsByName("room")!.Multiplicity, Is.EqualTo("0..1"));

        TMoldAssociation furnishing = m.Associations.ItemsByName("Furnishing")!;
        TMoldRole beds = furnishing.Roles.ItemsByName("beds")!;
        Assert.That(beds.Multiplicity, Is.EqualTo("1..*"));
        Assert.That(beds.Ordered, Is.True);
        Assert.That(beds.Aggregation, Is.EqualTo(TAggregationKind.akNone), "UML 2 marks the part's end composite");
        Assert.That(furnishing.Roles.ItemsByName("room")!.Aggregation, Is.EqualTo(TAggregationKind.akComposite), "UML 1.3 and Bold the whole's");
        Assert.That(furnishing.Roles.ItemsByName("room")!.Multiplicity, Is.EqualTo("0..1"), "no bounds: Boldify's default for a navigable end");
    }

    [Test]
    public void A_Papyrus_file_is_read()
    {
        var link = new TBoldUMLXMILink { FileName = XmiSamples.Write(Xmi2Samples.Papyrus, "Zoo.uml") };
        TMoldModel m = link.ImportMoldModel();
        Assert.That(m.Name, Is.EqualTo("Zoo"));
        TMoldClass animal = m.Classes.ItemsByName("Animal")!;
        Assert.That(animal.IsAbstract, Is.True);
        Assert.That(animal.Stereotype, Is.EqualTo("Persistent"), "an applied stereotype");
        Assert.That(animal.TVByName("TableName"), Is.EqualTo("ANIMALS"), "its value");
        Assert.That(animal.Constraints.Values("hasLegs"), Is.EqualTo("legs >= 0"));
        TMoldAttribute legs = animal.Attributes.ItemsByName("legs")!;
        Assert.That(legs.BoldType, Is.EqualTo("Integer"), "a library type by href");
        Assert.That(legs.InitialValue, Is.EqualTo("4"));
        Assert.That(animal.Attributes.ItemsByName("keeper"), Is.Null, "an association's end is not an attribute");

        TMoldAssociation cares = m.Associations.ItemsByName("Cares")!;
        TMoldRole keeper = cares.Roles.ItemsByName("keeper")!;
        Assert.That(keeper.Navigable, Is.True, "owned by its class");
        Assert.That(keeper.Multiplicity, Is.EqualTo("0..1"));
        TMoldRole animals = cares.Roles.ItemsByName("animals")!;
        Assert.That(animals.Navigable, Is.False, "owned by the association");
        Assert.That(animals.Multiplicity, Is.EqualTo("0..*"));
    }
}
