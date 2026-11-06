// Delphi: Source/UMLModel/ModelLinks/XMI/BoldUMLXMILink.pas, BoldUMLXMIImporter.pas, BoldXMI10Exporter.pas
// Port map: CSharp/port/units/BoldUMLXMILink.md
// Models are written as XMI and read back; XMI files in the forms of Rose (UML 1.3) and of UML 1.4 tools are read.
namespace Bold.Tests;

public class BoldUMLXMILinkTests
{
    /// <summary>A model's dump with the parts an association class changes in XMI left out: it is one XMI element, so
    /// its association takes the class's name and the class keeps one tool id (as in Delphi).</summary>
    internal static List<string> Comparable(TMoldModel model)
    {
        var renames = new Dictionary<string, string>();
        var linkNames = new Dictionary<string, string>();
        for (int i = 0; i < model.Associations.Count; i++)
        {
            if (model.Associations[i].LinkClass is { } linkClass)
            {
                renames["association " + model.Associations[i].Name + " "] = "association " + linkClass.Name + " ";
                renames["association " + model.Associations[i].Name + "."] = "association " + linkClass.Name + ".";
                linkNames[" link=" + model.Associations[i].Name] = " link=" + linkClass.Name;
            }
        }

        return ModelDump.Of(model)
            .Select(l => l.StartsWith("class ", StringComparison.Ordinal) && linkNames.FirstOrDefault(n => l.EndsWith(n.Key, StringComparison.Ordinal)) is { Key: not null } n
                ? l[..^n.Key.Length] + n.Value
                : l)
            .Where(l => !l.Contains("_BoldInternal.toolId"))
            .Select(l => renames.Aggregate(l, (s, r) => s.StartsWith(r.Key, StringComparison.Ordinal) ? r.Value + s[r.Key.Length..] : s))
            .Where(l => !(l.StartsWith("association ", StringComparison.Ordinal) && l.Contains(" TV _Boldify.noName=")))
            // XMI has no "n" for many, nor spaces in a range; both come back as the same multiplicity
            .Select(l => l.Replace("..n ", "..* ").Replace("multiplicity=n ", "multiplicity=0..* ").Replace("multiplicity=* ", "multiplicity=0..* ").Replace(" .. ", ".."))
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    public static IEnumerable<TestCaseData> Fixtures =>
        FixturePaths.Models.Select(f => new TestCaseData(Path.GetFileName(f)).SetArgDisplayNames(Path.GetFileName(f)));

    [TestCaseSource(nameof(Fixtures))]
    [Covers(typeof(TBoldUMLXMILink), "*")]
    [Covers(typeof(TBoldXMI10Exporter), "*")]
    [Covers(typeof(TBoldUMLXMIImporter), "*")]
    [Covers(typeof(TBoldUMLMOFObjectAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFAttributeListAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFReferenceListAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFAttributeAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFReferenceAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFObjectListAdapter), "*")]
    [Covers(typeof(TBoldGenericMOFObjectListAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFMultipleInheritenceTVListAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFMultiplicityAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFMultiplicityRangeAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFMultiplicityRangeBoundAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFExpressionAdapter), "*")]
    [Covers(typeof(TBoldUMLMOFDummyAttrAdapter), "*")]
    [Covers(typeof(BoldUMLModelMOFAdapters), "*")]
    [Covers(typeof(TBoldXMIIObjectList), "*")]
    [Covers(typeof(TBoldXMIObjectItem), "*")]
    [Covers(typeof(TBoldUMLElementArray), "*")]
    [Covers(typeof(TXMIId), "*")]
    [Covers(typeof(BoldUMLXMILinkSupport), "*")]
    [Covers(typeof(BoldUMLXMICommon), "*")]
    [Covers(typeof(IBoldMOFObject), "*")]
    [Covers(typeof(IBoldMOFAttribute), "*")]
    [Covers(typeof(IBoldMOFReference), "*")]
    [Covers(typeof(IBoldMOFObjectList), "*")]
    [Covers(typeof(IBoldMOFAttributeList), "*")]
    [Covers(typeof(IBoldMOFReferenceList), "*")]
    public void A_model_written_as_XMI_reads_back_as_the_same_model(string file)
    {
        TMoldModel original = XmiSamples.LoadFixture(file);
        var link = new TBoldUMLXMILink { FileName = XmiSamples.NewFile("model.xmi") };
        Assert.That(link.ExportModel(original), Is.True);
        TMoldModel back = link.ImportMoldModel();

        // The model as it is after the same trip through the UML model without XMI: unboldified and boldified again.
        TMoldModel expected = TBoldUMLModelLink.UMLModelToMoldModel(TBoldUMLModelLink.MoldModelToUMLModel(original));
        Assert.That(Comparable(back), Is.EqualTo(Comparable(expected)));
    }

    [Test]
    [Covers(typeof(TBoldUMLMOFMergedObjectListAdapter), "*")]
    public void An_association_class_is_one_XMI_element_with_the_class_and_the_association_members()
    {
        TMoldModel original = XmiSamples.LoadFixture("AssociationClassExampleClasses.Delphi-Simple-ObjectSpace-AssociationClass.bld");
        var link = new TBoldUMLXMILink { FileName = XmiSamples.NewFile("model.xmi") };
        link.ExportModel(original);
        var doc = new System.Xml.XmlDocument();
        doc.Load(link.FileName);
        var job = doc.SelectNodes("//Foundation.Core.AssociationClass[@xmi.id]")!.Cast<System.Xml.XmlElement>().Single();
        Assert.That(job["Foundation.Core.ModelElement.name"]!.InnerText, Is.EqualTo("Job"));
        Assert.That(job["Foundation.Core.Classifier.feature"]!.ChildNodes.Cast<System.Xml.XmlElement>().Select(f => f["Foundation.Core.ModelElement.name"]!.InnerText),
            Is.EqualTo(new[] { "title", "salary", "CompleteCreate" }), "the class's features");
        Assert.That(job["Foundation.Core.Association.connection"]!.ChildNodes.Count, Is.EqualTo(2), "the association's ends");
        Assert.That(doc.SelectNodes("//Foundation.Core.Association[@xmi.id]")!.Cast<System.Xml.XmlElement>()
            .Any(a => a["Foundation.Core.ModelElement.name"]!.InnerText == "Job"), Is.False, "the association is not written alone");
        Assert.That(File.ReadLines(link.FileName).Take(2), Is.EqualTo(new[] { "<?xml version = \"1.0\"?>", "<!DOCTYPE XMI SYSTEM \"uml13.dtd\">" }));
    }

    [Test]
    public void Values_are_written_as_UML_1_3_wants_them()
    {
        TMoldModel original = XmiSamples.LoadFixture("BuildingsAndOwners.Building.bld");
        var link = new TBoldUMLXMILink { FileName = XmiSamples.NewFile("model.xmi") };
        link.ExportModel(original);
        var doc = new System.Xml.XmlDocument();
        doc.Load(link.FileName);
        var ends = doc.SelectNodes("//Foundation.Core.AssociationEnd[@xmi.id]")!.Cast<System.Xml.XmlElement>().ToList();
        var owners = ends.Single(e => e["Foundation.Core.ModelElement.name"]!.InnerText == "Owners");
        Assert.That(owners["Foundation.Core.AssociationEnd.isNavigable"]!.GetAttribute("xmi.value"), Is.EqualTo("true"));
        Assert.That(owners["Foundation.Core.AssociationEnd.ordering"]!.GetAttribute("xmi.value"), Is.EqualTo("ordered"));
        var range = owners.SelectSingleNode("Foundation.Core.AssociationEnd.multiplicity/Foundation.Data_Types.Multiplicity/Foundation.Data_Types.Multiplicity.range/Foundation.Data_Types.MultiplicityRange")!;
        Assert.That(range["Foundation.Data_Types.MultiplicityRange.lower"]!.InnerText, Is.EqualTo("0"));
        Assert.That(range["Foundation.Data_Types.MultiplicityRange.upper"]!.InnerText, Is.EqualTo("-1"));
        Assert.That(owners.SelectSingleNode("Foundation.Core.AssociationEnd.type/Foundation.Core.Class")!.Attributes!["xmi.idref"]!.Value, Does.StartWith("x"));
    }

    [Test]
    [Covers(typeof(TPass))]
    [Covers(typeof(TClassAction))]
    [Covers(typeof(TXMIExporter))]
    public void A_Rose_XMI_file_of_UML_1_3_is_read()
    {
        var link = new TBoldUMLXMILink { FileName = XmiSamples.Write(XmiSamples.Rose13, "Rose.xmi"), TranslateRoseTaggedValues = true };
        TMoldModel m = link.ImportMoldModel();
        Assert.That(m.Name, Is.EqualTo("Shop"));

        TMoldClass customer = m.Classes.ItemsByName("Customer")!;
        Assert.That(customer.Stereotype, Is.EqualTo("entity"));
        Assert.That(customer.TVByName("Bold.TableName"), Is.EqualTo("CUSTOMERS"), "Rose's tag is translated");
        Assert.That(customer.TVByName("_BoldInternal.toolId"), Is.EqualTo("UUID-CUSTOMER"), "the uuid is the tool id");
        Assert.That(customer.TVByName(BoldDefaultTaggedValues.BOLDINTERALTVPREFIX + "unflattenedNamespace"), Is.EqualTo("Shop.Sales"), "the model is flattened");
        Assert.That(customer.Constraints.Values("hasName"), Is.EqualTo("name <> ''"));

        TMoldAttribute name = customer.Attributes.ItemsByName("name")!;
        Assert.That(name.BoldType, Is.EqualTo("String"));
        Assert.That(name.InitialValue, Is.EqualTo("'anonymous'"));
        Assert.That(name.Visibility, Is.EqualTo(TVisibilityKind.vkPublic), "package visibility is public");

        TMoldMethod rename = customer.Methods.ItemsByName("Rename")!;
        Assert.That(rename.IsClassMethod, Is.True);
        Assert.That(rename.ReturnType, Is.EqualTo("Integer"));
        Assert.That(rename.Parameters.Count, Is.EqualTo(1));
        Assert.That(((TMoldParameter)rename.Parameters[0]!).ParameterName, Is.EqualTo("NewName"));
        Assert.That(((TMoldParameter)rename.Parameters[0]!).ParameterType, Is.EqualTo("String"));

        Assert.That(m.Classes.ItemsByName("BigOrder")!.SuperClass!.Name, Is.EqualTo("Order"));
        Assert.That(m.Classes.ItemsByName("Order")!.SuperClass!.Name, Is.EqualTo(m.RootClass.Name), "Boldify gives the root class");
        Assert.That(m.Classes.ItemsByName("Lifecycle"), Is.Null, "a state machine is not read");

        TMoldAssociation orders = m.Associations.ItemsByName("customerorders")!;
        Assert.That(orders, Is.Not.Null, "an unnamed association is named by Boldify");
        TMoldRole ordersEnd = orders.Roles.ItemsByName("orders")!;
        Assert.That(ordersEnd.Multiplicity, Is.EqualTo("0..*"));
        Assert.That(ordersEnd.Ordered, Is.True);
        Assert.That(ordersEnd.Aggregation, Is.EqualTo(TAggregationKind.akComposite));
        Assert.That(orders.Roles.ItemsByName("customer")!.Multiplicity, Is.EqualTo("1"));

        TMoldAssociation membership = m.Associations.ItemsByName("Membership")!;
        Assert.That(membership.LinkClass!.Name, Is.EqualTo("Membership"));
        Assert.That(membership.LinkClass.Attributes.ItemsByName("since")!.BoldType, Is.EqualTo("Integer"));
        Assert.That(membership.Roles.ItemsByName("clubs")!.MoldClass!.Name, Is.EqualTo("Customer"), "a role is of the class at the other end");
    }

    [Test]
    public void A_XMI_file_of_UML_1_4_is_read()
    {
        var link = new TBoldUMLXMILink { FileName = XmiSamples.Write(XmiSamples.Uml14, "Library.xmi") };
        TMoldModel m = link.ImportMoldModel();
        Assert.That(m.Name, Is.EqualTo("Library"));
        TMoldClass book = m.Classes.ItemsByName("Book")!;
        Assert.That(book.IsAbstract, Is.True);
        Assert.That(book.Stereotype, Is.EqualTo("entity"), "a reference by id in an attribute");
        Assert.That(book.TVByName("Bold.TableName"), Is.EqualTo("BOOKS"), "a tag definition's data value");
        TMoldAttribute title = book.Attributes.ItemsByName("title")!;
        Assert.That(title.BoldType, Is.EqualTo("String"));
        Assert.That(title.InitialValue, Is.EqualTo("'untitled'"));
        Assert.That(title.Visibility, Is.EqualTo(TVisibilityKind.vkPrivate));
        Assert.That(m.Classes.ItemsByName("Novel")!.SuperClass!.Name, Is.EqualTo("Book"));

        TMoldAssociation writes = m.Associations.ItemsByName("Writes")!;
        Assert.That(writes.LinkClass!.Name, Is.EqualTo("Writes"), "many to many: Boldify makes a link class");
        TMoldRole Role(string name) => Enumerable.Range(0, writes.Roles.Count).Select(i => writes.Roles[i]).Single(r => r.Name == name && r.RoleType == TBoldRoleType.rtRole);
        TMoldRole authors = Role("authors");
        Assert.That(authors.Multiplicity, Is.EqualTo("1..*"));
        Assert.That(authors.Ordered, Is.True);
        Assert.That(authors.MoldClass!.Name, Is.EqualTo("Book"), "participant is the end's type");
        Assert.That(Role("books").MoldClass!.Name, Is.EqualTo("Author"), "a participant given by id");
    }

    [Test]
    public void A_file_that_is_not_XMI_is_not_read_and_the_error_is_kept()
    {
        var link = new TBoldUMLXMILink { FileName = XmiSamples.Write("<XMI><nothing/></XMI>", "empty.xmi") };
        TBoldUMLObject model = new TBoldUMLModelGraph().Create("Model");
        Assert.That(link.ImportModel(model), Is.False);
        Assert.That(link.LastError!.Message, Is.EqualTo("Content missing"));

        link.FileName = XmiSamples.Write("<XMI><unclosed></XMI>", "bad.xmi");
        Assert.That(link.ImportModel(model), Is.False);
        Assert.That(link.LastError!.Message, Does.StartWith("Error reading/parsing XMI file"));
        Assert.That(() => link.ImportMoldModel(), Throws.InstanceOf<EBoldImport>());
    }

    [Test]
    public void The_link_writes_and_reads_its_file()
    {
        var link = new TBoldUMLXMILink { FileName = "a.xmi" };
        Assert.That(link.FileName, Is.EqualTo("a.xmi"));
        Assert.That(link.DisplayName, Is.EqualTo("a.xmi"));
        Assert.That(link.CanExport, Is.True);
        Assert.That(link.HandledObject, Is.Null);
    }

    [Test]
    public void Rose_tags_are_written_as_Rose_has_them_when_translated()
    {
        var link = new TBoldUMLXMILink { FileName = XmiSamples.Write(XmiSamples.Rose13, "Rose.xmi"), TranslateRoseTaggedValues = true };
        TMoldModel m = link.ImportMoldModel();
        link.FileName = XmiSamples.NewFile("out.xmi");
        link.ExportModel(m);
        string text = File.ReadAllText(link.FileName);
        Assert.That(text, Does.Contain("RationalRose$Bold:TableName"));
        Assert.That(link.ImportMoldModel().Classes.ItemsByName("Customer")!.TVByName("Bold.TableName"), Is.EqualTo("CUSTOMERS"));
    }
}
