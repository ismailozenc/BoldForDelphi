// Delphi: Source/UMLModel/ModelLinks/XMI/BoldXMI10Exporter.pas
// Port map: CSharp/port/units/BoldXMI10Exporter.md

using System.Xml;

namespace Bold;

/// <summary>Writes a model, seen through the MOF interfaces, as XMI 1.0 of UML 1.3, its members in the DTD's order.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldXMI10Exporter : IDisposable
{
    private const string XMI_NODENAME_ROOT = "XMI";
    private const string XMI_NODENAME_HEADER = "XMI.header";
    private const string XMI_NODENAME_CONTENT = "XMI.content";
    private const string XMI_NODENAME_METAMODEL = "XMI.metamodel";
    private const string XMI_ATTRIBUTENAME_ID = "xmi.id";
    private const string XMI_ATTRIBUTENAME_IDREF = "xmi.idref";
    private const string XMI_ATTRIBUTENAME_VALUE = "xmi.value";
    private const string XMI_ATTRIBUTENAME_VERSION = "xmi.version";
    private const string XMI_ATTRIBUTENAME_NAME = "xmi.name";

    private TBoldUMLDTDReader? fProcessedDTD;

    [Delphi(PortStatus.Ported)]
    public TBoldXMI10Exporter(TBoldUMLXMILink OwningLink)
    {
        this.OwningLink = OwningLink;
        fProcessedDTD = TBoldUMLDTDReader.Shared;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldXMI10Exporter.Destroy")]
    public void Dispose()
    {
        fProcessedDTD = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private static XmlElement AppendNewElement(XmlElement ParentElement, string tagname)
    {
        XmlElement result = ParentElement.OwnerDocument.CreateElement(tagname);
        ParentElement.AppendChild(result);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    private void AttributeAsElement(IBoldMOFAttribute Attribute, XmlElement XMLElement) => SvAttributeContents(Attribute, XMLElement);

    [Delphi(PortStatus.Ported)]
    private static string BooleanAsString(IBoldMOFAttribute Value) => Value.AsBoolean() ? "true" : "false";

    /// <summary>A composite reference, with its objects inside.</summary>
    [Delphi(PortStatus.Ported)]
    private void CompositeAsElement(IBoldMOFReference Composite, XmlElement XMLElement)
    {
        XmlElement compositeNode = AppendNewElement(XMLElement, QualifiedReferenceName(Composite));
        if (!Composite.IsMulti())
        {
            if (Composite.SingleObject() is { } single)
            {
                ObjectAsElement(single, compositeNode);
            }
        }
        else
        {
            IBoldMOFObjectList objects = Composite.Objects();
            for (int i = 0; i < objects.Count(); i++)
            {
                ObjectAsElement(objects.GetObject(i), compositeNode);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ContentsFromRoot(IBoldMOFObject RootObj, XmlElement XMLElement)
    {
        XmlElement contentElement = AppendNewElement(XMLElement, XMI_NODENAME_CONTENT);
        ObjectAsElement(RootObj, contentElement);
        OtherLinks(RootObj, contentElement);
        RequiredTypeDefinitions(RootObj, contentElement);
    }

    [Delphi(PortStatus.Ported)]
    private static void EnumAttribute(IBoldMOFAttribute Attribute, XmlElement XMLElement)
    {
        XmlElement attrNode = AppendNewElement(XMLElement, Attribute.QualifiedName());
        attrNode.SetAttribute(XMI_ATTRIBUTENAME_VALUE, Attribute.IsBoolean() ? BooleanAsString(Attribute) : Attribute.AsString());
    }

    [Delphi(PortStatus.Ported)]
    private static string IdOfObject(IBoldMOFObject Obj) => Obj.LocalId();

    [Delphi(PortStatus.Ported)]
    private void ObjectAsElement(IBoldMOFObject Obj, XmlElement XMLElement)
    {
        XmlElement objectNode = AppendNewElement(XMLElement, QualifiedClassName(Obj));
        string objId = IdOfObject(Obj);
        if (objId != "")
        {
            objectNode.SetAttribute(XMI_ATTRIBUTENAME_ID, objId);
        }

        ObjectContents(Obj, objectNode);
    }

    /// <summary>The object's attributes and references, in the order the DTD gives its class's members.</summary>
    [Delphi(PortStatus.Ported)]
    private void ObjectContents(IBoldMOFObject Obj, XmlElement XMLElement)
    {
        IBoldMOFAttributeList attributeList = Obj.Attributes();
        IBoldMOFReferenceList referenceList = Obj.References();
        var attributes = Enumerable.Range(0, attributeList.Count()).Select(attributeList.Attribute).ToList();
        var references = Enumerable.Range(0, referenceList.Count()).Select(referenceList.Reference).ToList();
        foreach (string featureName in fProcessedDTD!.MembersOfClass(Obj.QualifiedClassName()).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (attributes.FirstOrDefault(a => a.QualifiedName() == featureName) is { } attribute)
            {
                AttributeAsElement(attribute, XMLElement);
            }
            else if (references.FirstOrDefault(r => r.QualifiedName() == featureName) is { } reference)
            {
                if (reference.IsComposite())
                {
                    CompositeAsElement(reference, XMLElement);
                }
                else
                {
                    ReferenceAsElement(reference, XMLElement);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void ObjRefOrDataValue(IBoldMOFAttribute Attribute, XmlElement XMLElement) =>
        XMLElement.AppendChild(XMLElement.OwnerDocument.CreateTextNode(Attribute.AsString()));

    [Delphi(PortStatus.Ported, Note = "Does nothing, as in Delphi.")]
    private void OtherLinks(IBoldMOFObject Obj, XmlElement XMLElement)
    {
    }

    [Delphi(PortStatus.Ported)]
    private static void ProduceHeader(XmlElement XMLElement)
    {
        XmlElement headerNode = AppendNewElement(XMLElement, XMI_NODENAME_HEADER);
        XmlElement metamodelNode = AppendNewElement(headerNode, XMI_NODENAME_METAMODEL);
        metamodelNode.SetAttribute(XMI_ATTRIBUTENAME_NAME, "");
    }

    [Delphi(PortStatus.Ported)]
    private static string QualifiedClassName(IBoldMOFObject Obj) => Obj.QualifiedClassName();

    [Delphi(PortStatus.Ported)]
    private static string QualifiedAttributeName(IBoldMOFAttribute Attribute) => Attribute.QualifiedName();

    /// <summary>Writes the model to the link's file; line breaks in values are written as character references, so that they are read back as they were.</summary>
    [Delphi(PortStatus.Ported, Note = "Indented, in UTF-8.")]
    public void RawExport(IBoldMOFObject RootObj)
    {
        var aDom = new XmlDocument();
        XmlElement root = aDom.CreateElement(XMI_NODENAME_ROOT);
        aDom.AppendChild(root);
        root.SetAttribute(XMI_ATTRIBUTENAME_VERSION, "1.0");
        ProduceHeader(root);
        ContentsFromRoot(RootObj, root);
        using var writer = new StreamWriter(OwningLink.FileName, false, new System.Text.UTF8Encoding(false));
        writer.WriteLine("<?xml version = \"1.0\"?>");
        writer.WriteLine("<!DOCTYPE XMI SYSTEM \"uml13.dtd\">");
        using var xmlWriter = XmlWriter.Create(writer, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = true, IndentChars = "  ", NewLineHandling = NewLineHandling.Entitize });
        aDom.WriteTo(xmlWriter);
    }

    /// <summary>A reference, with its objects by id.</summary>
    [Delphi(PortStatus.Ported)]
    private void ReferenceAsElement(IBoldMOFReference Reference, XmlElement XMLElement)
    {
        string referenceName = QualifiedReferenceName(Reference);
        if (!Reference.IsMulti())
        {
            if (Reference.SingleObject() is { } single)
            {
                ReferencingElement(single, AppendNewElement(XMLElement, referenceName));
            }
        }
        else
        {
            IBoldMOFObjectList objects = Reference.Objects();
            if (objects.Count() > 0)
            {
                XmlElement referenceNode = AppendNewElement(XMLElement, referenceName);
                for (int i = 0; i < objects.Count(); i++)
                {
                    ReferencingElement(objects.GetObject(i), referenceNode);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void ReferencingElement(IBoldMOFObject Obj, XmlElement XMLElement) =>
        AppendNewElement(XMLElement, QualifiedClassName(Obj)).SetAttribute(XMI_ATTRIBUTENAME_IDREF, IdOfObject(Obj));

    [Delphi(PortStatus.Ported, Note = "Does nothing, as in Delphi.")]
    private void RequiredTypeDefinitions(IBoldMOFObject RootObj, XmlElement XMLElement)
    {
    }

    [Delphi(PortStatus.Ported)]
    private void SvAttributeContents(IBoldMOFAttribute Attribute, XmlElement XMLElement)
    {
        if (Attribute.IsObject())
        {
            EmbeddedObject(Attribute, AppendNewElement(XMLElement, QualifiedAttributeName(Attribute)));
        }
        else if (Attribute.IsEnum() || Attribute.IsBoolean())
        {
            EnumAttribute(Attribute, XMLElement);
        }
        else
        {
            ObjRefOrDataValue(Attribute, AppendNewElement(XMLElement, QualifiedAttributeName(Attribute)));
        }
    }

    [Delphi(PortStatus.Ported)]
    private static string QualifiedReferenceName(IBoldMOFReference Reference) => Reference.QualifiedName();

    [Delphi(PortStatus.Ported)]
    private void EmbeddedObject(IBoldMOFAttribute Attribute, XmlElement XMLElement)
    {
        IBoldMOFObject obj = Attribute.AsObject();
        ObjectContents(obj, AppendNewElement(XMLElement, QualifiedClassName(obj)));
    }

    [Delphi(PortStatus.Ported)] public TBoldUMLXMILink OwningLink { get; }
}
