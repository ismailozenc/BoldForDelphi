using System.Xml;

namespace Bold;

/// <summary>
/// Writes a UML model, held in the UML graph, as XMI 2.1 of UML 2: packages, classes, data types and associations as
/// packaged elements, attributes and association ends as properties with lower and upper values, generalizations in
/// their classes, constraints as owned rules. Bold's tagged values and stereotypes are in an extension of each
/// element (extender "Bold"), which other tools leave alone. Delphi Bold had no XMI 2 link; this is new in the port.
/// </summary>
public class TBoldXMI2Exporter
{
    public const string XMINamespace = "http://schema.omg.org/spec/XMI/2.1";
    public const string UMLNamespace = "http://schema.omg.org/spec/UML/2.1";

    private readonly XmlDocument fDocument = new();

    private static string Id(TBoldUMLObject Obj) =>
        "x" + (BoldUMLModelMOFAdapters.IsAssociationClassAssociation(Obj) ? Obj.class_! : Obj).LocalIndex;

    private XmlElement NewElement(XmlElement Parent, string Name, string? UmlType, TBoldUMLObject? Obj)
    {
        XmlElement element = Name.Contains(':') ? fDocument.CreateElement(Name[..Name.IndexOf(':')], Name[(Name.IndexOf(':') + 1)..], Name.StartsWith("uml:") ? UMLNamespace : XMINamespace)
            : fDocument.CreateElement(Name);
        Parent.AppendChild(element);
        if (UmlType is not null)
        {
            SetXmi(element, "type", "uml:" + UmlType);
        }

        if (Obj is not null)
        {
            SetXmi(element, "id", Id(Obj));
            if (Obj.name != "" || UmlType is "Class" or "Association" or "Property")
            {
                element.SetAttribute("name", Obj.name);
            }

            if (Obj.HasAttribute("visibility"))
            {
                element.SetAttribute("visibility", Obj["visibility"]);
            }
        }

        return element;
    }

    private void SetXmi(XmlElement Element, string Name, string Value)
    {
        XmlAttribute attribute = fDocument.CreateAttribute("xmi", Name, XMINamespace);
        attribute.Value = Value;
        Element.Attributes.Append(attribute);
    }

    /// <summary>The XMI 2.1 document of the model.</summary>
    public XmlDocument Export(TBoldUMLObject UMLModel)
    {
        fDocument.AppendChild(fDocument.CreateXmlDeclaration("1.0", "UTF-8", null));
        XmlElement root = fDocument.CreateElement("xmi", "XMI", XMINamespace);
        fDocument.AppendChild(root);
        root.SetAttribute("xmlns:uml", UMLNamespace);
        SetXmi(root, "version", "2.1");
        XmlElement model = NewElement(root, "uml:Model", "Model", UMLModel);
        Extension(model, UMLModel, null);
        Contents(model, UMLModel);
        return fDocument;
    }

    /// <summary>Writes the model to a file, indented, in UTF-8.</summary>
    public void Export(TBoldUMLObject UMLModel, string FileName)
    {
        XmlDocument document = Export(UMLModel);
        using var writer = XmlWriter.Create(FileName, new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineHandling = NewLineHandling.Entitize, Encoding = new System.Text.UTF8Encoding(false) });
        document.WriteTo(writer);
    }

    /// <summary>The tagged values and the stereotype of an element (and of an association class's association).</summary>
    private void Extension(XmlElement Element, TBoldUMLObject Obj, TBoldUMLObject? Association)
    {
        if (Obj.TaggedValues.Count == 0 && Obj.stereotypeName == "" && (Association is null || Association.TaggedValues.Count == 0))
        {
            return;
        }

        XmlElement extension = NewElement(Element, "xmi:Extension", null, null);
        extension.SetAttribute("extender", "Bold");
        if (Obj.stereotypeName != "")
        {
            NewElement(extension, "stereotype", null, null).SetAttribute("name", Obj.stereotypeName);
        }

        foreach (TBoldUMLObject tv in Obj.TaggedValues)
        {
            XmlElement item = NewElement(extension, "taggedValue", null, null);
            item.SetAttribute("tag", tv["tag"]);
            item.SetAttribute("value", tv["value"]);
        }

        foreach (TBoldUMLObject tv in Association?.TaggedValues ?? [])
        {
            XmlElement item = NewElement(extension, "taggedValue", null, null);
            item.SetAttribute("tag", tv["tag"]);
            item.SetAttribute("value", tv["value"]);
            item.SetAttribute("on", "association");
        }
    }

    /// <summary>A namespace's elements, and its constraints as owned rules.</summary>
    private void Contents(XmlElement Element, TBoldUMLObject Namespace)
    {
        foreach (TBoldUMLObject owned in Namespace.ownedElement)
        {
            switch (owned.MetaClass)
            {
                case "Package":
                    {
                        XmlElement package = NewElement(Element, "packagedElement", "Package", owned);
                        Extension(package, owned, null);
                        Contents(package, owned);
                        break;
                    }

                case "Class" when owned.isAssociationClass:
                    Class(Element, owned, "AssociationClass", owned.associationOfClass);
                    break;
                case "Class" or "Interface":
                    Class(Element, owned, owned.MetaClass, null);
                    break;
                case "Association" when !BoldUMLModelMOFAdapters.IsAssociationClassAssociation(owned):
                    {
                        XmlElement association = NewElement(Element, "packagedElement", "Association", owned);
                        Extension(association, owned, null);
                        Ends(association, owned);
                        break;
                    }

                case "DataType" or "Primitive" or "Enumeration":
                    {
                        XmlElement dataType = NewElement(Element, "packagedElement", "DataType", owned);
                        Extension(dataType, owned, null);
                        break;
                    }

                case "Constraint":
                    Rule(Element, owned);
                    break;
            }
        }
    }

    private void Class(XmlElement Parent, TBoldUMLObject Cls, string UmlType, TBoldUMLObject? Association)
    {
        XmlElement element = NewElement(Parent, "packagedElement", UmlType, Cls);
        if (Cls.isAbstract)
        {
            element.SetAttribute("isAbstract", "true");
        }

        Extension(element, Cls, Association);
        foreach (TBoldUMLObject generalization in Cls.generalization)
        {
            if (generalization.Ref("parent") is { } parent)
            {
                XmlElement g = NewElement(element, "generalization", "Generalization", generalization);
                g.RemoveAttribute("name");
                g.SetAttribute("general", Id(parent));
            }
        }

        foreach (TBoldUMLObject feature in Cls.feature)
        {
            if (feature.IsKindOf("Attribute"))
            {
                Property(element, "ownedAttribute", feature, null);
            }
            else if (feature.IsKindOf("Operation"))
            {
                Operation(element, feature);
            }
        }

        Contents(element, Cls);
        if (Association is not null)
        {
            Ends(element, Association);
        }
    }

    private void Property(XmlElement Parent, string Name, TBoldUMLObject Obj, TBoldUMLObject? Association)
    {
        XmlElement property = NewElement(Parent, Name, "Property", Obj);
        if (Obj.type_ is { } type)
        {
            property.SetAttribute("type", Id(type));
        }

        if (Association is not null)
        {
            property.SetAttribute("association", Id(Association));
        }

        if (Obj.isOrdered)
        {
            property.SetAttribute("isOrdered", "true");
        }

        if (Obj.derived)
        {
            property.SetAttribute("isDerived", "true");
        }

        if (Obj.ownerScope == TScopeKind.skClassifier)
        {
            property.SetAttribute("isStatic", "true");
        }

        if (Obj.changeability == TChangeableKind.ckFrozen)
        {
            property.SetAttribute("isReadOnly", "true");
        }

        if (Obj.HasAttribute("aggregation") && Obj["aggregation"] != "none")
        {
            property.SetAttribute("aggregation", Obj["aggregation"]);
        }

        Extension(property, Obj, null);
        if (Obj.multiplicity != "")
        {
            (string lower, string upper) = Bounds(Obj.multiplicity);
            NewElement(property, "lowerValue", "LiteralInteger", null).SetAttribute("value", lower);
            NewElement(property, "upperValue", "LiteralUnlimitedNatural", null).SetAttribute("value", upper);
        }

        if (Obj.initialValue != "")
        {
            NewElement(property, "defaultValue", "OpaqueExpression", null).SetAttribute("body", Obj.initialValue);
        }

        foreach (TBoldUMLObject qualifier in Obj.qualifier)
        {
            Property(property, "qualifier", qualifier, null);
        }
    }

    /// <summary>The lower and upper bound of a multiplicity's ranges ("*" for many).</summary>
    private static (string Lower, string Upper) Bounds(string Multiplicity)
    {
        string[] ranges = Multiplicity.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        string Part(string range, bool upper)
        {
            int dots = range.IndexOf("..", StringComparison.Ordinal);
            string value = dots < 0 ? range : upper ? range[(dots + 2)..].Trim() : range[..dots].Trim();
            if (value is "*" or "n")
            {
                return upper || dots < 0 ? "*" : "0";
            }

            return value;
        }

        string lower = Part(ranges[0], false);
        if (lower == "*")
        {
            lower = "0";
        }

        return (lower, Part(ranges[^1], true));
    }

    private void Operation(XmlElement Parent, TBoldUMLObject Operation)
    {
        XmlElement element = NewElement(Parent, "ownedOperation", "Operation", Operation);
        if (Operation.ownerScope == TScopeKind.skClassifier)
        {
            element.SetAttribute("isStatic", "true");
        }

        Extension(element, Operation, null);
        foreach (TBoldUMLObject parameter in Operation.parameter)
        {
            XmlElement p = NewElement(element, "ownedParameter", "Parameter", parameter);
            p.SetAttribute("direction", parameter.kind switch
            {
                TBoldParameterDirectionKind.pdOut => "out",
                TBoldParameterDirectionKind.pdInout => "inout",
                TBoldParameterDirectionKind.pdReturn => "return",
                _ => "in",
            });
            if (parameter.type_ is { } type)
            {
                p.SetAttribute("type", Id(type));
            }

            Extension(p, parameter, null);
        }
    }

    /// <summary>An association's ends, owned by it; its navigable ends listed as such. UML 2 puts the aggregation on the part's end.</summary>
    private void Ends(XmlElement Element, TBoldUMLObject Association)
    {
        List<TBoldUMLObject> ends = Association.connection;
        Element.SetAttribute("memberEnd", string.Join(" ", ends.Select(Id)));
        var navigable = ends.Where(e => e.isNavigable).Select(Id).ToList();
        if (navigable.Count > 0)
        {
            Element.SetAttribute("navigableOwnedEnd", string.Join(" ", navigable));
        }

        for (int i = 0; i < ends.Count; i++)
        {
            TBoldUMLObject end = ends[i];
            string aggregation = ends.Count == 2 ? ends[1 - i]["aggregation"] : "";
            string own = end["aggregation"];
            end["aggregation"] = aggregation;
            Property(Element, "ownedEnd", end, Association);
            end["aggregation"] = own;
        }
    }

    private void Rule(XmlElement Parent, TBoldUMLObject Constraint)
    {
        XmlElement rule = NewElement(Parent, "ownedRule", "Constraint", Constraint);
        List<TBoldUMLObject> constrained = Constraint.Refs("constrainedElement");
        if (constrained.Count > 0)
        {
            rule.SetAttribute("constrainedElement", string.Join(" ", constrained.Select(Id)));
        }

        XmlElement specification = NewElement(rule, "specification", "OpaqueExpression", null);
        specification.SetAttribute("body", Constraint.body);
    }
}
