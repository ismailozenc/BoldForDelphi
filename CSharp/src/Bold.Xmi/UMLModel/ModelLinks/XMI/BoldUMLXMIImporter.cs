// Delphi: Source/UMLModel/ModelLinks/XMI/BoldUMLXMIImporter.pas
// Port map: CSharp/port/units/BoldUMLXMIImporter.md
// Delphi filled its UML object space and asked its type info which members an XMI element's children were, and of
// which kind. The port fills the UML graph (D6) and tells from the XML itself: a member element with objects in it
// is a reference, any other an attribute; an XML attribute is a reference when its name is one of the metamodel's
// references. Names that differ between UML 1.3 and 1.4 are taken as the 1.3 names (participant is type), and a 1.4
// tagged value (a tag definition and data values) becomes a 1.3 one (tag and value).

using System.Text;
using System.Xml;
using static Bold.BoldUMLXMICommon;
using static Bold.BoldUMLXMILinkSupport;

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TPass { PASS1, PASS2 }

[Delphi(PortStatus.Ported)]
public enum TClassAction { caIMPORT, caTRAVERSE, caSKIP, caUNKNOWN }

[Delphi(PortStatus.Ported)]
public enum TXMIExporter { STDEXPORTER, UNISYS_TCR_2 }

/// <summary>
/// Reads an XMI 1.x file into a UML model. The first pass makes an element for each XMI element with an id; the
/// second sets their attributes and references and makes the elements without ids.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLXMIImporter : IDisposable
{
    private const string XMI_CONTENT_ELEMENT = "XMI.content";

    /// <summary>The names, in the XMI files of any UML version, of the metamodel's references.</summary>
    private static readonly HashSet<string> ReferenceNames =
    [
        "namespace", "ownedElement", "owner", "feature", "type", "participant", "association", "connection",
        "associationEnd", "qualifier", "child", "parent", "subtype", "supertype", "generalization", "specialization",
        "stereotype", "extendedElement", "taggedValue", "modelElement", "constraint", "constrainedElement",
        "behavioralFeature", "parameter", "powertype", "client", "supplier", "clientDependency", "supplierDependency",
        "specification", "definedTag", "referenceValue", "typedParameter", "typedFeature", "comment", "annotatedElement",
    ];

    /// <summary>The names UML 1.4 and 1.1 use for the references UML 1.3 names otherwise.</summary>
    private static readonly Dictionary<string, string> ReferenceSynonyms = new()
    {
        ["participant"] = "type",
        ["subtype"] = "child",
        ["supertype"] = "parent",
    };

    private readonly TBoldUMLObject fTheUMLModel;
    private bool fTheUMLModelFound;
    private XmlDocument? fDOMDocument;
    private TStringList fSkipMetaClassList = new();
    private TStringList fTraverseOnlyMetaClassList = new();
    private readonly TBoldXMIIObjectList fImportedElementList = new();
    private readonly List<TBoldUMLObject> fAssociationClassAssociations = [];
    private TPass fPass;

    static TBoldUMLXMIImporter()
    {
        // XMI files from Windows tools are often in a Windows code page.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    [Delphi(PortStatus.Ported, Note = "Takes the UML graph's model (D6).")]
    public TBoldUMLXMIImporter(TBoldUMLXMILink OwningLink, TBoldUMLObject UMLModel)
    {
        this.OwningLink = OwningLink;
        fTheUMLModel = UMLModel;
        InitializeLists();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLXMIImporter.Destroy")]
    public void Dispose()
    {
        fDOMDocument = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldUMLXMIImporter.BoldSystem", Note = "The UML graph the model is in, where Delphi had the UML object space.")]
    private TBoldUMLModelGraph Graph => fTheUMLModel.Graph;

    [Delphi(PortStatus.Ported)]
    private void InitializeLists()
    {
        fSkipMetaClassList = new TStringList { Sorted = true };
        fTraverseOnlyMetaClassList = new TStringList { Sorted = true };
    }

    [Delphi(PortStatus.Replaced, Note = "The graph has no type info; MemberName gives a member element's name, and ImportMetaObject tells references from attributes by the XML.")]
    private object? FindMemberRt(object? ClassTypeInfo, string QualifiedName) => throw new NotSupportedException();

    /// <summary>A member's name: the last part of the XMI name, as UML 1.3 names it.</summary>
    private static string MemberName(string QualifiedName)
    {
        string name = UnqualifiedName(QualifiedName);
        return ReferenceSynonyms.GetValueOrDefault(name, name);
    }

    /// <summary>Sets an attribute from its text: Rose's tags are translated when asked, and package visibility is public.</summary>
    [Delphi(PortStatus.Ported, Note = "Sets an attribute of a graph element by name.")]
    private void ImportAttributeFromString(TBoldUMLObject UMLObject, string AttributeName, string AttributeValue)
    {
        if (OwningLink.TranslateRoseTaggedValues && UMLObject.MetaClass == "TaggedValue" && AttributeName == "tag")
        {
            AttributeValue = TranslateRoseTag(AttributeValue);
        }
        else if (AttributeName == "visibility" && AttributeValue == "package")
        {
            AttributeValue = "public";
        }

        UMLObject[AttributeName] = AttributeValue;
    }

    /// <summary>Rose's "RationalRose$Tool:Tag" becomes "Tool.Tag".</summary>
    private static string TranslateRoseTag(string Tag)
    {
        if (Tag.StartsWith("RationalRose$", StringComparison.Ordinal) && Tag.Contains(':'))
        {
            Tag = Tag["RationalRose$".Length..];
            int colonpos = Tag.IndexOf(':');
            return Tag[..colonpos] + "." + Tag[(colonpos + 1)..];
        }

        return Tag;
    }

    [Delphi(PortStatus.Ported)]
    protected XmlDocument GetDOMDocument() => fDOMDocument ??= new XmlDocument();

    /// <summary>Loads the file; validates it against its DTD when the link says so.</summary>
    [Delphi(PortStatus.Ported, Note = "System.Xml where Delphi used MSXML; a DTD is only read when validating.")]
    protected void LoadAndCheck(string Filename)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = OwningLink.ValidateInput ? DtdProcessing.Parse : DtdProcessing.Ignore,
            ValidationType = OwningLink.ValidateInput ? ValidationType.DTD : ValidationType.None,
            XmlResolver = OwningLink.ValidateInput ? new XmlUrlResolver() : null,
            IgnoreWhitespace = true,
        };
        fDOMDocument = new XmlDocument();
        try
        {
            using XmlReader reader = XmlReader.Create(Filename, settings);
            fDOMDocument.Load(reader);
        }
        catch (Exception e) when (e is XmlException or System.Xml.Schema.XmlSchemaException)
        {
            throw new EBold($"Error reading/parsing XMI file, {e.Message}", e);
        }
    }

    /// <summary>Imports a reference member: its objects, nested or by id, are added in the second pass.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the owning graph element and the reference's name where Delphi took the member.")]
    protected void ImportMetaAssociationEndFromElement(TBoldUMLObject? Owner, string Reference, XmlElement XMLElement, bool Multi)
    {
        var objectsToAdd = new List<TBoldUMLObject>();
        ImportXMIObjectList(objectsToAdd, XMLElement, Owner is null ? null : ExpectedKind(Owner, Reference));
        if (fPass == TPass.PASS2 && Owner is not null)
        {
            if (Multi)
            {
                foreach (TBoldUMLObject obj in objectsToAdd)
                {
                    Owner.AddRef(Reference, obj);
                }
            }
            else if (objectsToAdd.Count > 0)
            {
                Owner.SetRef(Reference, objectsToAdd[0]);
            }
        }
    }

    /// <summary>The kind of object a reference holds, to choose between the class and association of an association class.</summary>
    private static string? ExpectedKind(TBoldUMLObject Owner, string Reference) => Reference switch
    {
        "type" when Owner.MetaClass == "TaggedValue" => "TagDefinition",
        "type" => "Classifier",
        "owner" => "Classifier",
        "parent" or "child" => "GeneralizableElement",
        "feature" => "Feature",
        "connection" => "AssociationEnd",
        "association" => "Association",
        "generalization" or "specialization" => "Generalization",
        "stereotype" => "Stereotype",
        "namespace" => "Namespace",
        "qualifier" => "Attribute",
        "parameter" => "Parameter",
        "taggedValue" => "TaggedValue",
        "constraint" => "Constraint",
        _ => null,
    };

    /// <summary>The objects of an element's children, nested or by id, that are of the kind (any kind when none).</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the kind as a metaclass name where Delphi took a class type info.")]
    protected void ImportXMIObjectList(List<TBoldUMLObject>? list, XmlElement XMLElement, string? BoldType)
    {
        foreach (XmlElement childElement in XMLElement.ChildNodes.OfType<XmlElement>())
        {
            TBoldUMLElementArray? objectsToAdd = IsNonBlank(GetXMIIdRef(childElement))
                ? fImportedElementList.UMLElementsById(GetXMIIdRef(childElement))
                : ImportMetaObject(childElement);
            if (objectsToAdd is not null && list is not null)
            {
                list.AddRange(objectsToAdd.Where(o => BoldType is null || o.IsKindOf(BoldType)));
            }
        }
    }

    /// <summary>"UML" and the last part of an XMI name: the name of Delphi's UML model class.</summary>
    [Delphi(PortStatus.Ported)]
    protected static string GetUMLName(string aText) => "UML" + UnqualifiedName(aText);

    private static string MetaClassName(XmlElement XMLElement) => UnqualifiedName(XMLElement.Name);

    /// <summary>Imports an XMI element that is an object; returns what it became (none for what is skipped).</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldUMLElementArray? ImportMetaObject(XmlElement XMLElement)
    {
        TBoldUMLElementArray? result = null;
        switch (MetaClassAction(XMLElement))
        {
            case TClassAction.caIMPORT:
                result = CreateOrLocateMetaObject(XMLElement);
                break;
            case TClassAction.caTRAVERSE:
                break;
            case TClassAction.caSKIP:
            case TClassAction.caUNKNOWN:
                return null;
        }

        bool isAssociationClass = MetaClassName(XMLElement) == "AssociationClass";

        // The XML attributes: attributes, or references by id
        foreach (XmlAttribute attribute in XMLElement.Attributes)
        {
            string name = attribute.Name;
            if (name.StartsWith("xmi.", StringComparison.Ordinal) || name.StartsWith("xmlns", StringComparison.Ordinal) || name.Contains(':'))
            {
                continue;
            }

            if (result is null || fPass != TPass.PASS2)
            {
                continue;
            }

            if (ReferenceNames.Contains(name))
            {
                string reference = MemberName(name);
                foreach (string id in attribute.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                {
                    TBoldUMLElementArray? targets = fImportedElementList.UMLElementsById(XMIIdforId(id));
                    if (targets is null)
                    {
                        continue;
                    }

                    TBoldUMLObject owner = isAssociationClass && reference == "connection" ? result[1] : result[0];
                    string? kind = ExpectedKind(owner, reference);
                    foreach (TBoldUMLObject target in targets.Where(t => kind is null || t.IsKindOf(kind)))
                    {
                        owner.AddRef(reference, target);
                    }
                }
            }
            else
            {
                // An association class's attributes are both its class's and its association's
                foreach (TBoldUMLObject obj in result)
                {
                    ImportAttributeFromString(obj, name, attribute.Value);
                }
            }
        }

        // The member elements
        foreach (XmlElement element in XMLElement.ChildNodes.OfType<XmlElement>())
        {
            string member = MemberName(element.Name);
            if (member.Length == 0 || !char.IsLower(member[0]) || IsXMIElement(element))
            {
                ImportXMIObjectList(null, element, null);
                continue;
            }

            if (IsReferenceElement(element))
            {
                TBoldUMLObject? owner = result is null ? null : isAssociationClass && member == "connection" ? result[1] : result[0];
                ImportMetaAssociationEndFromElement(owner, member, element, !TBoldUMLMetaModel.IsSingle(member));
            }
            else
            {
                foreach (TBoldUMLObject? obj in result is null ? [null] : result.Cast<TBoldUMLObject?>())
                {
                    ImportMetaAttributeFromElement(obj, member, element);
                }
            }
        }

        return result;
    }

    private static bool IsXMIElement(XmlElement Element) => Element.Name.StartsWith("XMI.", StringComparison.Ordinal);

    /// <summary>A member element is a reference when it holds objects: elements named as classes, not a multiplicity or an expression.</summary>
    private static bool IsReferenceElement(XmlElement Element)
    {
        if (Element.HasAttribute("xmi.value") || ValueIsMultiplicity(Element) || ValueIsExpression(Element))
        {
            return false;
        }

        return Element.ChildNodes.OfType<XmlElement>().Any(c => UnqualifiedName(c.Name) is { Length: > 0 } n && char.IsUpper(n[0]));
    }

    private TBoldUMLElementArray? CreateOrLocateMetaObject(XmlElement DOMElement)
    {
        TXMIId id = GetXMIId(DOMElement);
        if (fPass == TPass.PASS1)
        {
            return IsNonBlank(id) ? CreateUMLObjectsForMetaObject(DOMElement) : null;
        }

        // the second pass: the objects with ids were made in the first
        if (IsNonBlank(id))
        {
            return fImportedElementList.ItemsById(id)?.UMLobjects ?? throw new EBold("Modelpart for id " + FormatId(id) + " not found");
        }

        return CreateUMLObjectsForMetaObject(DOMElement);
    }

    private static bool ValueIsMultiplicity(XmlElement DOMElement) => UnqualifiedName(DOMElement.Name) == "multiplicity";

    private static bool ValueIsXmiValue(XmlElement DOMElement) => DOMElement.HasAttribute("xmi.value");

    private static bool ValueIsExpression(XmlElement DOMElement) =>
        DOMElement.FirstChild is XmlElement first && IsNameOfExpressionClass(UnqualifiedName(first.Name));

    /// <summary>Sets an attribute from its member element: an xmi.value, a multiplicity, an expression or text.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the owning graph element and the attribute's name; strings are not shared, as Delphi's string manager shared them.")]
    protected void ImportMetaAttributeFromElement(TBoldUMLObject? Owner, string AttributeName, XmlElement XMLElement)
    {
        if (fPass == TPass.PASS1 || Owner is null)
        {
            return; // attributes are set in the second pass
        }

        if (ValueIsXmiValue(XMLElement))
        {
            string value = XMLElement.GetAttribute("xmi.value");
            if (value != "")
            {
                ImportAttributeFromString(Owner, AttributeName, value);
            }
        }
        else if (ValueIsMultiplicity(XMLElement))
        {
            if (XMLElement.FirstChild is XmlElement multiplicity)
            {
                Owner[AttributeName] = ImportMultiplicityAsString(multiplicity);
            }
            else
            {
                ImportAttributeFromString(Owner, AttributeName, ImportCdataValue(XMLElement));
            }
        }
        else if (ValueIsExpression(XMLElement))
        {
            Owner[AttributeName] = ImportExpressionAsString((XmlElement)XMLElement.FirstChild!);
        }
        else
        {
            ImportAttributeFromString(Owner, AttributeName, ImportCdataValue(XMLElement));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected TClassAction MetaClassAction(XmlElement XMLElement)
    {
        string name = MetaClassName(XMLElement);
        if (TBoldUMLMetaModel.IsMetaClass(name) || name == "AssociationClass")
        {
            return TClassAction.caIMPORT;
        }

        if (fSkipMetaClassList.IndexOf(XMLElement.Name) != -1)
        {
            return TClassAction.caSKIP;
        }

        if (fTraverseOnlyMetaClassList.IndexOf(XMLElement.Name) != -1)
        {
            return TClassAction.caTRAVERSE;
        }

        return TClassAction.caUNKNOWN;
    }

    [Delphi(PortStatus.Replaced, Note = "Graph elements' attributes are set by name (TBoldUMLObject's indexer).")]
    protected object? GetAttributeMember(TBoldUMLObject OwningObject, string AttributeName) => throw new NotSupportedException();

    /// <summary>Makes the element (a class and an association for an association class) and notes it by id.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldUMLElementArray CreateUMLObjectsForMetaObject(XmlElement XMLElement)
    {
        TXMIId xmiId = GetXMIId(XMLElement);
        TBoldUMLElementArray result;
        if (IsNonBlank(xmiId) && fImportedElementList.ItemsById(xmiId) is not null)
        {
            throw new EBoldInternal("Modelpart already exists");
        }

        string umlClassName = MetaClassName(XMLElement);
        if (umlClassName == "Model" && !fTheUMLModelFound)
        {
            // the model is there already; it gets the id
            result = fImportedElementList.Add(xmiId, fTheUMLModel);
            fTheUMLModelFound = true;
        }
        else if (umlClassName == "AssociationClass")
        {
            TBoldUMLObject element1 = Graph.Create("Class");
            TBoldUMLObject element2 = Graph.Create("Association");
            result = fImportedElementList.Add(xmiId, element1, element2);
            element2.class_ = element1;
            fAssociationClassAssociations.Add(element2);
        }
        else
        {
            result = fImportedElementList.Add(xmiId, Graph.Create(umlClassName));
        }

        if (xmiId.UUId != "")
        {
            foreach (TBoldUMLObject element in result)
            {
                TBoldUMLSupport.AddToolId(element, xmiId.UUId);
            }
        }

        foreach (TBoldUMLObject element in result)
        {
            element.XMIId = xmiId.Id != "" ? xmiId.Id : xmiId.UUId;
        }

        return result;
    }

    /// <summary>A multiplicity's text: its ranges, comma separated, as "1", "0..1" or "1..*".</summary>
    [Delphi(PortStatus.Ported)]
    protected string ImportMultiplicityAsString(XmlElement XMLElement)
    {
        if (UnqualifiedName(XMLElement.Name) != "Multiplicity")
        {
            throw new EBold($"Wrong nodename: {XMLElement.Name}");
        }

        if (XMLElement.FirstChild is not XmlElement rangeMember || !rangeMember.HasChildNodes)
        {
            string result = ImportCdataValue(XMLElement);
            return result == "" ? "0..*" : result;
        }

        if (UnqualifiedName(rangeMember.Name) != "range")
        {
            throw new EBold($"Wrong nodename: {XMLElement.Name}");
        }

        var ranges = new TStringList();
        foreach (XmlElement childElement in rangeMember.ChildNodes.OfType<XmlElement>())
        {
            ranges.Add(ImportMultiplicityRangeAsString(childElement));
        }

        return BoldUtils.BoldSeparateStringList(ranges, ", ", "", "");
    }

    [Delphi(PortStatus.Ported)]
    protected string ImportMultiplicityRangeAsString(XmlElement XMLElement)
    {
        if (UnqualifiedName(XMLElement.Name) != "MultiplicityRange")
        {
            throw new EBold($"Wrong nodename: {XMLElement.Name}");
        }

        string lower = XMLElement.GetAttribute("lower");
        string upper = XMLElement.GetAttribute("upper");
        foreach (XmlElement childElement in XMLElement.ChildNodes.OfType<XmlElement>())
        {
            switch (UnqualifiedName(childElement.Name))
            {
                case "lower":
                    lower = ImportCdataValue(childElement);
                    break;
                case "upper":
                    upper = ImportCdataValue(childElement);
                    break;
                default:
                    throw new EBoldInternal("Import error");
            }
        }

        if (lower == "-1")
        {
            lower = "*";
        }

        if (upper == "-1")
        {
            upper = "*";
        }

        return lower == upper ? lower : $"{lower}..{upper}";
    }

    /// <summary>An expression's body: its body attribute, or its body element's text.</summary>
    [Delphi(PortStatus.Ported)]
    protected string ImportExpressionAsString(XmlElement XMLElement)
    {
        if (XMLElement.HasAttribute("body"))
        {
            return XMLElement.GetAttribute("body");
        }

        string result = "";
        foreach (XmlElement childElement in XMLElement.ChildNodes.OfType<XmlElement>())
        {
            if (UnqualifiedName(childElement.Name) == "body")
            {
                result = ImportCdataValue(childElement);
            }
        }

        return result;
    }

    /// <summary>The text of an element.</summary>
    [Delphi(PortStatus.Ported)]
    protected string ImportCdataValue(XmlElement XMLElement) => XMLElement.ChildNodes.Count switch
    {
        0 => "",
        1 => XMLElement.FirstChild!.Value ?? "",
        _ => throw new EBoldInternal("Node problem"),
    };

    /// <summary>Reads the link's file into the UML model, in two passes, and gives every element its Bold tagged values.</summary>
    [Delphi(PortStatus.Ported)]
    public void RawImport()
    {
        LoadAndCheck(OwningLink.FileName);
        XmlElement rootElement = GetDOMDocument().DocumentElement ?? throw new EBold("RootElement missing");
        XmlElement contentElement = FindContent(rootElement) ?? throw new EBold("Content missing");
        fPass = TPass.PASS1;
        ImportXMIObjectList(null, contentElement, null);
        fPass = TPass.PASS2;
        ImportXMIObjectList(null, contentElement, null);
        NormalizeTaggedValues();
        GiveAssociationClassTaggedValuesToAssociations();
        foreach (TBoldUMLObject element in Graph.AllInstances("ModelElement").Reverse().ToList())
        {
            TBoldUMLSupport.EnsureBoldTaggedValues(element);
        }

        static XmlElement? FindContent(XmlElement XMLElement)
        {
            if (XMLElement.Name == XMI_CONTENT_ELEMENT)
            {
                return XMLElement;
            }

            foreach (XmlElement child in XMLElement.ChildNodes.OfType<XmlElement>())
            {
                if (FindContent(child) is { } result)
                {
                    return result;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// A UML 1.4 tagged value names its tag by a tag definition and holds its value as data values; it gets the tag
    /// and value UML 1.3 gives it.
    /// </summary>
    private void NormalizeTaggedValues()
    {
        foreach (TBoldUMLObject taggedValue in Graph.AllInstances("TaggedValue").ToList())
        {
            if (!taggedValue.HasAttribute("tag") && taggedValue.Ref("type") is { } definition)
            {
                string tag = definition.name != "" ? definition.name : definition["tagType"];
                taggedValue["tag"] = OwningLink.TranslateRoseTaggedValues ? TranslateRoseTag(tag) : tag;
            }

            if (!taggedValue.HasAttribute("value") && taggedValue.HasAttribute("dataValue"))
            {
                taggedValue["value"] = taggedValue["dataValue"];
            }

            taggedValue["dataValue"] = "";
            taggedValue.SetRef("type", null);
        }

        foreach (TBoldUMLObject definition in Graph.AllInstances("TagDefinition").ToList())
        {
            definition.Delete();
        }
    }

    /// <summary>
    /// An association class's tagged values were read into its class, as Delphi read them; its association gets those
    /// whose tags are defined for associations too, so that the association's settings are not lost.
    /// </summary>
    private void GiveAssociationClassTaggedValuesToAssociations()
    {
        TBoldTaggedValueList bold = BoldDefaultTaggedValues.BoldDefaultTaggedValueList().ListForClassName("Association");
        TBoldTaggedValueList uml = BoldUMLTaggedValues.UMLTaggedValueList().ListForClassName("Association");
        foreach (TBoldUMLObject association in fAssociationClassAssociations)
        {
            foreach (TBoldUMLObject taggedValue in association.class_?.TaggedValues ?? [])
            {
                string tag = taggedValue["tag"];
                bool defined = tag.StartsWith(BoldDefs.BOLDTVPREFIX, StringComparison.Ordinal)
                    ? bold.DefinitionForTag(tag[BoldDefs.BOLDTVPREFIX.Length..]) is not null
                    : uml.DefinitionForTag(tag) is not null;
                if (defined && association.taggedValue(tag) is null)
                {
                    association.SetTaggedValue(tag, taggedValue["value"]);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)] public bool RemoveRoseArtefacts { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldUMLXMILink OwningLink { get; }
    [Delphi(PortStatus.Ported)] protected XmlDocument DOMDocument => GetDOMDocument();
    [Delphi(PortStatus.Ported)] protected TPass Pass => fPass;
    [Delphi(PortStatus.Ported)] protected TStringList SkipMetaClassList => fSkipMetaClassList;
    [Delphi(PortStatus.Ported)] protected TStringList TraverseOnlyMetaClassList => fTraverseOnlyMetaClassList;
}
