using System.Xml;

namespace Bold;

/// <summary>
/// Reads a UML 2 model from an XMI 2.x file (Enterprise Architect, MagicDraw, Papyrus and other UML 2 tools) into the
/// UML graph, as the elements UML 1.3 would have: a property of an association is an association end, an operation's
/// parameters and a class's generalizations are as in UML 1.3. Tagged values come from Bold's extension, from
/// Enterprise Architect's tags and from applied stereotypes (each of their attributes a tagged value).
/// Delphi Bold had no XMI 2 link; this is new in the port.
/// </summary>
public class TBoldXMI2Importer
{
    private readonly TBoldUMLObject fModel;
    private readonly bool fTranslateRoseTaggedValues;
    private readonly Dictionary<string, List<TBoldUMLObject>> fById = new(StringComparer.Ordinal);
    private readonly Dictionary<TBoldUMLObject, string> fUml2Aggregation = [];
    private bool fModelFound;

    public TBoldXMI2Importer(TBoldUMLObject UMLModel, bool TranslateRoseTaggedValues = false)
    {
        fModel = UMLModel;
        fTranslateRoseTaggedValues = TranslateRoseTaggedValues;
    }

    private TBoldUMLModelGraph Graph => fModel.Graph;

    /// <summary>Whether a document is XMI 2: its xmi namespace is XMI 2's (or a dated version of it).</summary>
    public static bool IsXMI2(XmlDocument Document)
    {
        XmlElement? root = Document.DocumentElement;
        if (root is null)
        {
            return false;
        }

        string ns = root.GetNamespaceOfPrefix("xmi");
        return ns.Contains("/XMI/2", StringComparison.Ordinal) || ns.Contains("/spec/XMI/20", StringComparison.Ordinal) ||
               root.GetAttribute("version", ns).StartsWith('2') || root.GetAttribute("xmi:version").StartsWith('2');
    }

    private static string XmiNamespace(XmlElement Element) => Element.OwnerDocument.DocumentElement!.GetNamespaceOfPrefix("xmi");

    private static string XmiAttribute(XmlElement Element, string Name) =>
        Element.GetAttribute(Name, XmiNamespace(Element)) is { Length: > 0 } value ? value : Element.GetAttribute("xmi:" + Name);

    /// <summary>The class a member element is of when it has no xmi:type, as Eclipse's UML writes them.</summary>
    private static readonly Dictionary<string, string> ImplicitTypes = new()
    {
        ["ownedAttribute"] = "Property",
        ["ownedEnd"] = "Property",
        ["qualifier"] = "Property",
        ["ownedOperation"] = "Operation",
        ["ownedParameter"] = "Parameter",
        ["generalization"] = "Generalization",
        ["ownedRule"] = "Constraint",
    };

    /// <summary>The UML class of an element: its xmi:type without the prefix, the class of its member, or its own name (the model).</summary>
    private static string UmlType(XmlElement Element)
    {
        string type = XmiAttribute(Element, "type");
        if (type == "")
        {
            type = ImplicitTypes.GetValueOrDefault(Element.LocalName, Element.LocalName);
        }

        return type[(type.IndexOf(':') + 1)..];
    }

    /// <summary>Reads the document into the model.</summary>
    public void Import(XmlDocument Document)
    {
        XmlElement root = Document.DocumentElement ?? throw new EBold("RootElement missing");
        XmlElement modelElement = FindModel(root) ?? throw new EBold("Content missing");
        MakeObjects(modelElement, null);
        MakeLibraryTypes(root);
        var extensions = root.GetElementsByTagName("*").OfType<XmlElement>().Where(e => e.LocalName == "Extension" && e.NamespaceURI == XmiNamespace(e)).ToList();
        // tagged values before the members, which do not then add what a tagged value says; stereotypes after, when the elements are in the model
        extensions.ForEach(e => ReadExtension(e, false));
        SetMembers(modelElement);
        extensions.ForEach(e => ReadExtension(e, true));

        foreach (XmlElement application in root.ChildNodes.OfType<XmlElement>().Where(e => e != modelElement))
        {
            ReadStereotypeApplication(application);
        }

        SwapAggregations();
        foreach (TBoldUMLObject element in Graph.AllInstances("ModelElement").Reverse().ToList())
        {
            TBoldUMLSupport.EnsureBoldTaggedValues(element);
        }
    }

    private static XmlElement? FindModel(XmlElement Element)
    {
        if (UmlType(Element) == "Model" && Element.NamespaceURI != XmiNamespace(Element))
        {
            return Element;
        }

        foreach (XmlElement child in Element.ChildNodes.OfType<XmlElement>())
        {
            if (FindModel(child) is { } model)
            {
                return model;
            }
        }

        return null;
    }

    /// <summary>The graph classes an XMI 2 element becomes; none for what the graph does not keep.</summary>
    private string[]? GraphClasses(XmlElement Element, XmlElement? Owner) => UmlType(Element) switch
    {
        "Model" => ["Model"],
        "Package" or "Profile" => ["Package"],
        "Class" or "Component" => ["Class"],
        "AssociationClass" => ["Class", "Association"],
        "Association" => ["Association"],
        "DataType" or "PrimitiveType" or "Enumeration" => ["DataType"],
        "Interface" => ["Interface"],
        "Property" or "Port" => IsAssociationEnd(Element, Owner) ? ["AssociationEnd"] : ["Attribute"],
        "Operation" => ["Operation"],
        "Parameter" => ["Parameter"],
        "Generalization" => ["Generalization"],
        "Constraint" => ["Constraint"],
        _ => null,
    };

    private bool IsAssociationEnd(XmlElement Element, XmlElement? Owner) =>
        Element.HasAttribute("association") || Element.SelectSingleNode("*[local-name()='association']") is not null ||
        (Element.LocalName == "ownedEnd" && Owner is not null && UmlType(Owner) is "Association" or "AssociationClass");

    /// <summary>The first pass: an object for each element the graph keeps, found by its id.</summary>
    private void MakeObjects(XmlElement Element, XmlElement? Owner)
    {
        if (GraphClasses(Element, Owner) is not { } classes)
        {
            return;
        }

        List<TBoldUMLObject> objects;
        if (classes[0] == "Model" && !fModelFound)
        {
            objects = [fModel];
            fModelFound = true;
        }
        else
        {
            objects = [.. classes.Select(Graph.Create)];
            if (objects.Count == 2)
            {
                objects[1].class_ = objects[0];
            }
        }

        string id = XmiAttribute(Element, "id");
        if (id != "")
        {
            fById[id] = objects;
            foreach (TBoldUMLObject obj in objects)
            {
                obj.XMIId = id;
            }
        }

        if (XmiAttribute(Element, "uuid") is { Length: > 0 } uuid)
        {
            objects.ForEach(o => TBoldUMLSupport.AddToolId(o, uuid));
        }

        fObjects[Element] = objects;
        foreach (XmlElement child in Element.ChildNodes.OfType<XmlElement>())
        {
            MakeObjects(child, Element);
        }
    }

    private readonly Dictionary<XmlElement, List<TBoldUMLObject>> fObjects = [];

    /// <summary>Data types outside the model (Enterprise Architect keeps its languages' types in its extension) become the model's.</summary>
    private void MakeLibraryTypes(XmlElement Root)
    {
        foreach (XmlElement element in Root.GetElementsByTagName("*").OfType<XmlElement>())
        {
            string id = XmiAttribute(element, "id");
            if (id != "" && !fById.ContainsKey(id) && UmlType(element) is "PrimitiveType" or "DataType")
            {
                TBoldUMLObject type = Graph.Create("DataType");
                type.name = element.GetAttribute("name");
                type.namespace_ = fModel;
                fById[id] = [type];
            }
        }
    }

    /// <summary>The ids an element refers to by a member: in an attribute, or in child elements' idrefs or hrefs.</summary>
    private static IEnumerable<string> Refs(XmlElement Element, string Member)
    {
        foreach (string id in Element.GetAttribute(Member).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            yield return id;
        }

        foreach (XmlElement child in Element.ChildNodes.OfType<XmlElement>().Where(c => c.LocalName == Member))
        {
            if (XmiAttribute(child, "idref") is { Length: > 0 } idref)
            {
                yield return idref;
            }
            else if (child.GetAttribute("href") is { Length: > 0 } href)
            {
                yield return href;
            }
        }
    }

    /// <summary>The object of an id, of a kind; an href to a library type is a data type of the name after its '#'.</summary>
    private TBoldUMLObject? Resolve(string Id, string? Kind)
    {
        if (fById.TryGetValue(Id, out List<TBoldUMLObject>? objects))
        {
            return objects.FirstOrDefault(o => Kind is null || o.IsKindOf(Kind));
        }

        int hash = Id.LastIndexOf('#');
        if (hash >= 0 && Kind is "Classifier")
        {
            string name = Id[(hash + 1)..];
            TBoldUMLObject? type = fModel.ownedElement.FirstOrDefault(e => e.IsKindOf("Classifier") && e.name == name);
            if (type is null)
            {
                type = Graph.Create("DataType");
                type.name = name;
                type.namespace_ = fModel;
            }

            return type;
        }

        return null;
    }

    private static string? Value(XmlElement Element, string Member) =>
        Element.HasAttribute(Member) ? Element.GetAttribute(Member)
        : Element.ChildNodes.OfType<XmlElement>().FirstOrDefault(c => c.LocalName == Member) is { } child ? child.InnerText : null;

    /// <summary>The second pass: names, values and references.</summary>
    private void SetMembers(XmlElement Element)
    {
        if (fObjects.TryGetValue(Element, out List<TBoldUMLObject>? objects))
        {
            TBoldUMLObject obj = objects[0];
            TBoldUMLObject? association = objects.Count == 2 ? objects[1] : obj.MetaClass == "Association" ? obj : null;
            foreach (TBoldUMLObject o in objects)
            {
                SetAttributes(Element, o);
            }

            XmlElement? parent = Element.ParentNode as XmlElement;
            List<TBoldUMLObject>? owners = parent is not null && fObjects.TryGetValue(parent, out var p) ? p : null;
            if (owners is not null)
            {
                // both halves of an association class belong to its namespace
                foreach (TBoldUMLObject o in objects)
                {
                    Own(Element, o, owners);
                }
            }

            switch (obj.MetaClass)
            {
                case "Attribute" or "AssociationEnd":
                    SetProperty(Element, obj);
                    break;
                case "Parameter":
                    if (Refs(Element, "type").Select(id => Resolve(id, "Classifier")).FirstOrDefault(t => t is not null) is { } type)
                    {
                        obj.type_ = type;
                    }

                    obj.kind = (Value(Element, "direction") ?? "in") switch
                    {
                        "out" => TBoldParameterDirectionKind.pdOut,
                        "inout" => TBoldParameterDirectionKind.pdInout,
                        "return" => TBoldParameterDirectionKind.pdReturn,
                        _ => TBoldParameterDirectionKind.pdIn,
                    };
                    break;
                case "Operation":
                    if (Value(Element, "isStatic") == "true")
                    {
                        obj.ownerScope = TScopeKind.skClassifier;
                    }

                    break;
                case "Generalization":
                    if (Refs(Element, "general").Select(id => Resolve(id, "GeneralizableElement")).FirstOrDefault(t => t is not null) is { } general)
                    {
                        obj.SetRef("parent", general);
                    }

                    break;
                case "Constraint":
                    foreach (string id in Refs(Element, "constrainedElement"))
                    {
                        if (Resolve(id, "ModelElement") is { } constrained)
                        {
                            obj.AddRef("constrainedElement", constrained);
                        }
                    }

                    if (Element.SelectSingleNode("*[local-name()='specification']") is XmlElement specification)
                    {
                        obj.body = Value(specification, "body") ?? Value(specification, "value") ?? "";
                    }

                    break;
            }

            if (association is not null)
            {
                SetAssociation(Element, association);
            }
        }

        foreach (XmlElement child in Element.ChildNodes.OfType<XmlElement>())
        {
            SetMembers(child);
        }
    }

    private static void SetAttributes(XmlElement Element, TBoldUMLObject Obj)
    {
        if (Value(Element, "name") is { } name)
        {
            Obj.name = name;
        }

        if (Value(Element, "visibility") is { } visibility)
        {
            Obj["visibility"] = visibility == "package" ? "public" : visibility;
        }

        if (Value(Element, "isAbstract") is { } isAbstract && Obj.IsKindOf("GeneralizableElement"))
        {
            Obj["isAbstract"] = isAbstract;
        }
    }

    /// <summary>An element belongs to the element it is in: a namespace's element, a class's feature, an operation's parameter, a class's generalization.</summary>
    private static void Own(XmlElement Element, TBoldUMLObject Obj, List<TBoldUMLObject> Owners)
    {
        TBoldUMLObject owner = Owners[0];
        switch (Obj.MetaClass)
        {
            case "Attribute" when Element.LocalName == "qualifier":
                break; // set by the end that has it
            case "Attribute" or "Operation":
                owner.AddRef("feature", Obj);
                break;
            case "Parameter":
                owner.AddRef("parameter", Obj);
                break;
            case "Generalization":
                Obj.namespace_ = owner;
                Obj.SetRef("child", owner);
                break;
            case "AssociationEnd":
                break; // set by the association's member ends
            case "Constraint":
                Obj.namespace_ = owner.IsKindOf("Namespace") ? owner : owner.namespace_;
                break;
            default:
                if (owner.IsKindOf("Namespace"))
                {
                    Obj.namespace_ = owner;
                }

                break;
        }
    }

    /// <summary>A property: its type, multiplicity, default value, order, derivation, scope and qualifiers.</summary>
    private void SetProperty(XmlElement Element, TBoldUMLObject Obj)
    {
        if (Refs(Element, "type").Select(id => Resolve(id, "Classifier")).FirstOrDefault(t => t is not null) is { } type)
        {
            Obj.type_ = type;
        }

        string? multiplicity = Multiplicity(Element);
        if (multiplicity is not null)
        {
            Obj.multiplicity = multiplicity;
        }

        if (Element.SelectSingleNode("*[local-name()='defaultValue']") is XmlElement defaultValue)
        {
            Obj.initialValue = Value(defaultValue, "body") ?? Value(defaultValue, "value") ?? "";
        }

        if (Value(Element, "isOrdered") == "true")
        {
            Obj.isOrdered = true;
        }

        if (Value(Element, "isDerived") == "true")
        {
            Obj.derived = true;
        }

        if (Value(Element, "isStatic") == "true")
        {
            Obj.ownerScope = TScopeKind.skClassifier;
        }

        if (Value(Element, "isReadOnly") == "true")
        {
            Obj.changeability = TChangeableKind.ckFrozen;
        }

        if (Obj.MetaClass == "AssociationEnd")
        {
            fUml2Aggregation[Obj] = Value(Element, "aggregation") ?? "none";
            foreach (XmlElement qualifier in Element.ChildNodes.OfType<XmlElement>().Where(c => c.LocalName == "qualifier"))
            {
                if (fObjects.TryGetValue(qualifier, out List<TBoldUMLObject>? q))
                {
                    Obj.AddRef("qualifier", q[0]);
                }
            }
        }
    }

    /// <summary>"lower..upper" of an element's lowerValue and upperValue (each 1 when missing); none when it has neither.</summary>
    private static string? Multiplicity(XmlElement Element)
    {
        XmlElement? lowerValue = Element.SelectSingleNode("*[local-name()='lowerValue']") as XmlElement;
        XmlElement? upperValue = Element.SelectSingleNode("*[local-name()='upperValue']") as XmlElement;
        string? lowerAttribute = Element.HasAttribute("lower") ? Element.GetAttribute("lower") : null;
        string? upperAttribute = Element.HasAttribute("upper") ? Element.GetAttribute("upper") : null;
        if (lowerValue is null && upperValue is null && lowerAttribute is null && upperAttribute is null)
        {
            return null;
        }

        string lower = lowerAttribute ?? (lowerValue is null ? "1" : lowerValue.HasAttribute("value") ? lowerValue.GetAttribute("value") : "0");
        string upper = upperAttribute ?? (upperValue is null ? "1" : upperValue.HasAttribute("value") ? upperValue.GetAttribute("value") : "0");
        if (upper == "-1")
        {
            upper = "*";
        }

        return lower == upper ? lower : $"{lower}..{upper}";
    }

    /// <summary>An association's ends, in the order of its member ends; an end is navigable when a class owns it or the association says so.</summary>
    private void SetAssociation(XmlElement Element, TBoldUMLObject Association)
    {
        var navigable = Refs(Element, "navigableOwnedEnd").ToHashSet();
        var ownedEnds = Element.ChildNodes.OfType<XmlElement>().Where(c => c.LocalName == "ownedEnd").ToList();
        var memberEnds = Refs(Element, "memberEnd").ToList();
        if (memberEnds.Count == 0)
        {
            memberEnds = [.. ownedEnds.Select(e => XmiAttribute(e, "id"))];
        }

        var ownedIds = ownedEnds.Select(e => XmiAttribute(e, "id")).ToHashSet();
        // an association that owns all its ends and names none navigable does not say: its ends are navigable
        bool unspecified = navigable.Count == 0 && memberEnds.All(ownedIds.Contains);
        foreach (string id in memberEnds)
        {
            if (Resolve(id, "AssociationEnd") is not { } end)
            {
                continue;
            }

            Association.AddRef("connection", end);
            XmlElement? endElement = fObjects.FirstOrDefault(p => p.Value.Contains(end)).Key;
            bool explicitNavigable = endElement?.GetAttribute("isNavigable") == "true";
            end.isNavigable = unspecified || !ownedIds.Contains(id) || navigable.Contains(id) || explicitNavigable;
        }
    }

    /// <summary>UML 2 puts a composition's aggregation on the end of the part; UML 1.3 on the end of the whole.</summary>
    private void SwapAggregations()
    {
        foreach (TBoldUMLObject association in Graph.AllInstances("Association").ToList())
        {
            List<TBoldUMLObject> ends = association.connection;
            if (ends.Count == 2)
            {
                string a0 = fUml2Aggregation.GetValueOrDefault(ends[0], "none");
                string a1 = fUml2Aggregation.GetValueOrDefault(ends[1], "none");
                ends[0]["aggregation"] = a1;
                ends[1]["aggregation"] = a0;
            }
        }
    }

    /// <summary>Bold's extension (tagged values and a stereotype of the element it is in) and Enterprise Architect's (tags of the elements it names).</summary>
    private void ReadExtension(XmlElement Extension, bool Stereotypes)
    {
        string extender = XmiAttribute(Extension, "extender") is { Length: > 0 } x ? x : Extension.GetAttribute("extender");
        if (extender == "Bold" && Extension.ParentNode is XmlElement owner && fObjects.TryGetValue(owner, out List<TBoldUMLObject>? objects))
        {
            TBoldUMLObject obj = objects[0];
            foreach (XmlElement item in Extension.ChildNodes.OfType<XmlElement>())
            {
                if (item.LocalName == "taggedValue" && !Stereotypes)
                {
                    SetTaggedValue(item.GetAttribute("tag"), item.GetAttribute("value"), item.GetAttribute("on") == "association" && objects.Count == 2 ? objects[1] : obj);
                }
                else if (item.LocalName == "stereotype" && Stereotypes && obj.model is not null)
                {
                    obj.stereotypeName = item.GetAttribute("name");
                }
            }
        }
        else if (extender.StartsWith("Enterprise Architect", StringComparison.Ordinal))
        {
            foreach (XmlElement element in Extension.GetElementsByTagName("*").OfType<XmlElement>())
            {
                string idref = XmiAttribute(element, "idref");
                if (idref == "" || !fById.TryGetValue(idref, out List<TBoldUMLObject>? targets))
                {
                    continue;
                }

                foreach (XmlElement tag in element.ChildNodes.OfType<XmlElement>().Where(c => c.LocalName == "tags" && !Stereotypes).SelectMany(t => t.ChildNodes.OfType<XmlElement>()))
                {
                    SetTaggedValue(tag.GetAttribute("name"), tag.GetAttribute("value"), targets[0]);
                }

                string stereotype = element.ChildNodes.OfType<XmlElement>().Where(c => c.LocalName is "properties" or "stereotype")
                    .Select(c => c.GetAttribute("stereotype")).FirstOrDefault(s => s != "") ?? "";
                if (stereotype != "" && Stereotypes && targets[0].model is not null)
                {
                    targets[0].stereotypeName = stereotype;
                }
            }
        }
    }

    /// <summary>An applied stereotype, &lt;Profile:Name base_Class="id" .../&gt;: the element's stereotype, its other attributes tagged values.</summary>
    private void ReadStereotypeApplication(XmlElement Application)
    {
        XmlAttribute? baseAttribute = Application.Attributes.OfType<XmlAttribute>().FirstOrDefault(a => a.LocalName.StartsWith("base_", StringComparison.Ordinal));
        if (baseAttribute is null || !fById.TryGetValue(baseAttribute.Value, out List<TBoldUMLObject>? targets) || targets[0].model is null)
        {
            return;
        }

        targets[0].stereotypeName = Application.LocalName;
        foreach (XmlAttribute attribute in Application.Attributes)
        {
            if (attribute != baseAttribute && attribute.Prefix != "xmi" && attribute.Prefix != "xmlns" && attribute.Name != "xmlns")
            {
                SetTaggedValue(attribute.LocalName, attribute.Value, targets[0]);
            }
        }
    }

    private void SetTaggedValue(string Tag, string Value, TBoldUMLObject Obj)
    {
        if (fTranslateRoseTaggedValues && Tag.StartsWith("RationalRose$", StringComparison.Ordinal) && Tag.Contains(':'))
        {
            Tag = Tag["RationalRose$".Length..].Replace(':', '.');
        }

        if (Tag != "")
        {
            Obj.SetTaggedValue(Tag, Value);
        }
    }
}
