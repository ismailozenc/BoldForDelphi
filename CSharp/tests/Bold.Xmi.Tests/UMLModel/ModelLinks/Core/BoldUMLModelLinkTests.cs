// Delphi: Source/UMLModel/ModelLinks/Core/BoldUMLModelLink.pas, BoldUMLModelLinkSupport.pas
// Port map: CSharp/port/units/BoldUMLModelLink.md
using System.ComponentModel;

namespace Bold.Tests;

public class BoldUMLModelLinkTests
{
    [Test]
    [Covers(typeof(TBoldUMLModelLinkList), "*")]
    [Covers(typeof(TBoldUMLModelLinkDescriptor), "*")]
    [Covers(typeof(TBoldUMLModelLinkClass), "*")]
    [Covers(typeof(BoldUMLModelLinkUnit), "*")]
    public void The_XMI_link_is_in_the_list_of_links()
    {
        TBoldUMLModelLinkList list = BoldUMLModelLinkUnit.BoldUMLModelLinkList();
        Assert.That(BoldUMLModelLinkUnit.BoldUMLModelLinkListAssigned(), Is.True);
        Assert.That(list.LinkClasses(-1, ".xmi")!.LinkType, Is.EqualTo(typeof(TBoldUMLXMILink)));
        Assert.That(list.LinkClasses(0, "")!.LinkType, Is.EqualTo(typeof(TBoldUMLXMILink)));
        Assert.That(list.LinkClasses(-1, ".mdl"), Is.Null);
        Assert.That(list.FileFilter, Is.EqualTo("XMI file (*.xmi)|*.xmi|" + BoldDefs.ALLFILESFILTER));
        Assert.That(list.ExportableFileFilter, Is.EqualTo(list.FileFilter));
        TBoldUMLModelLinkDescriptor descriptor = list.LinkDescriptorByLinkClass(new TBoldUMLModelLinkClass(typeof(TBoldUMLXMILink)))!;
        Assert.That(descriptor.Description, Is.EqualTo(TBoldUMLXMILink.XMI_LINKDESC));
        Assert.That(descriptor.Suffix, Is.EqualTo(TBoldUMLXMILink.XMI_LINKEXTENSION));
        Assert.That(descriptor.CanExport(), Is.True);

        var other = new TBoldUMLModelLinkList();
        other.AddLink(".x", "X", new TBoldUMLModelLinkClass(typeof(TBoldUMLXMILink)));
        Assert.That(other.Count, Is.EqualTo(1));
        other.RemoveLink(new TBoldUMLModelLinkClass(typeof(TBoldUMLXMILink)));
        Assert.That(other.Count, Is.EqualTo(0));
    }

    [Test]
    [Covers(typeof(TBoldUMLModelLink), "*")]
    public void A_link_reads_a_model_into_its_model_component()
    {
        using var components = new Container();
        var model = new TBoldAbstractModel(components);
        string file = XmiSamples.Write(XmiSamples.Uml14, "Library.xmi");
        var link = new TBoldUMLXMILink(components) { FileName = file, BoldModel = model };
        Assert.That(BoldUMLModelLinkUnit.BoldGetUMLModelLinkForComponent(model), Is.SameAs(link));
        link.ImportModel();
        Assert.That(model.MoldModel.Classes.ItemsByName("Book"), Is.Not.Null);

        model.Dispose();
        Assert.That(link.BoldModel, Is.Null, "the link lets go of a model that goes");
        Assert.That(() => link.ImportModel((TBoldAbstractModel?)null), Throws.InstanceOf<EBold>());
    }

    [Test]
    [Covers(typeof(TBoldUMLModelLinkSupport), "*")]
    public void Constraints_are_written_as_lines_and_read_from_them()
    {
        var graph = new TBoldUMLModelGraph();
        TBoldUMLObject model = graph.Create("Model");
        TBoldUMLObject cls = graph.Create("Class");
        cls.namespace_ = model;
        TBoldUMLSupport.AddToNamespace(model, cls, "Person");
        TBoldUMLModelLinkSupport.StringToConstraints(cls, "hasName: name <> ''\r\nage >= 0");
        Assert.That(cls.constraint.Select(c => (c.name, c.body)), Is.EqualTo(new[] { ("hasName", "name <> ''"), ("", "age >= 0") }));
        Assert.That(cls.constraint[0].namespace_, Is.SameAs(cls));
        Assert.That(TBoldUMLModelLinkSupport.ConstraintsAsString(cls), Is.EqualTo("hasName:name <> ''\r\nage >= 0\r\n"));
        Assert.That(TBoldUMLModelLinkSupport.ConstraintsToString(cls), Is.EqualTo("hasName:name <> ''\r\nage >= 0\r\n"));
        Assert.That(cls.constraint, Is.Empty);

        TBoldUMLObject attribute = graph.Create("Attribute");
        cls.AddRef("feature", attribute);
        attribute.name = "/fullName";
        TBoldUMLModelLinkSupport.LeadingSlashToDerived(attribute);
        Assert.That(attribute.name, Is.EqualTo("fullName"));
        Assert.That(attribute.derived, Is.True);
    }
}
