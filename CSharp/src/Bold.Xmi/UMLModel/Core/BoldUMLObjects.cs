using static Bold.BoldDefaultTaggedValues;
using static Bold.BoldUMLTaggedValues;

namespace Bold;

/// <summary>
/// The UML metamodel's classes, as the XMI files and Delphi's UML model name them (without the "UML" prefix): which
/// classes each class is a kind of, which references are composite, and the pairs of references that are two ends of one
/// association. Only what import, export, flattening and Boldify need.
/// </summary>
public static class TBoldUMLMetaModel
{
    private static readonly Dictionary<string, string[]> Parents = new()
    {
        ["ModelElement"] = ["Element"],
        ["Namespace"] = ["ModelElement"],
        ["GeneralizableElement"] = ["ModelElement"],
        ["Classifier"] = ["Namespace", "GeneralizableElement"],
        ["Class"] = ["Classifier"],
        ["DataType"] = ["Classifier"],
        ["Primitive"] = ["DataType"],
        ["Enumeration"] = ["DataType"],
        ["Interface"] = ["Classifier"],
        ["Actor"] = ["Classifier"],
        ["Package"] = ["Namespace", "GeneralizableElement"],
        ["Model"] = ["Package"],
        ["Subsystem"] = ["Package", "Classifier"],
        ["Feature"] = ["ModelElement"],
        ["StructuralFeature"] = ["Feature"],
        ["Attribute"] = ["StructuralFeature"],
        ["BehavioralFeature"] = ["Feature"],
        ["Operation"] = ["BehavioralFeature"],
        ["Method"] = ["BehavioralFeature"],
        ["Parameter"] = ["ModelElement"],
        ["Relationship"] = ["ModelElement"],
        ["Association"] = ["GeneralizableElement", "Relationship"],
        ["Generalization"] = ["Relationship"],
        ["Dependency"] = ["Relationship"],
        ["AssociationEnd"] = ["ModelElement"],
        ["Constraint"] = ["ModelElement"],
        ["Stereotype"] = ["GeneralizableElement"],
        ["TaggedValue"] = ["Element"],
        ["TagDefinition"] = ["ModelElement"],
        ["Comment"] = ["ModelElement"],
    };

    /// <summary>The references whose objects belong to the referring object, and go when it goes.</summary>
    public static readonly HashSet<string> CompositeReferences =
    [
        "ownedElement", "feature", "connection", "qualifier", "parameter", "taggedValue", "constraint", "generalization",
    ];

    /// <summary>Two references that are the two ends of one association: setting one sets the other.</summary>
    private static readonly Dictionary<string, string> Inverses = new()
    {
        ["namespace"] = "ownedElement",
        ["ownedElement"] = "namespace",
        ["owner"] = "feature",
        ["feature"] = "owner",
        ["association"] = "connection",
        ["connection"] = "association",
        ["associationEnd"] = "qualifier",
        ["qualifier"] = "associationEnd",
        ["child"] = "generalization",
        ["generalization"] = "child",
        ["parent"] = "specialization",
        ["specialization"] = "parent",
        ["modelElement"] = "taggedValue",
        ["taggedValue"] = "modelElement",
        ["constrainedElement"] = "constraint",
        ["constraint"] = "constrainedElement",
        ["behavioralFeature"] = "parameter",
        ["parameter"] = "behavioralFeature",
        ["stereotype"] = "extendedElement",
        ["extendedElement"] = "stereotype",
        // Delphi Bold's own pair for an association class: the class and the association.
        ["class"] = "associationOfClass",
        ["associationOfClass"] = "class",
    };

    /// <summary>The references that hold one object.</summary>
    private static readonly HashSet<string> SingleReferences =
    [
        "namespace", "owner", "association", "associationEnd", "child", "parent", "modelElement", "behavioralFeature",
        "type", "class", "associationOfClass", "stereotype", "powertype",
    ];

    /// <summary>Whether the class is the kind or a subclass of it; UML's classes can have several superclasses.</summary>
    public static bool IsKindOf(string MetaClass, string Kind) =>
        MetaClass == Kind || (Parents.TryGetValue(MetaClass, out string[]? parents) && parents.Any(p => IsKindOf(p, Kind)));

    /// <summary>Whether the name is a class of the metamodel the graph keeps.</summary>
    public static bool IsMetaClass(string Name) => Name == "Element" || Parents.ContainsKey(Name);

    /// <summary>The classes the graph keeps.</summary>
    public static IEnumerable<string> MetaClasses => Parents.Keys;

    public static string? InverseOf(string Reference) => Inverses.GetValueOrDefault(Reference);

    public static bool IsSingle(string Reference) => SingleReferences.Contains(Reference);
}

/// <summary>
/// A UML model held as the elements of its XMI file: what Delphi Bold kept in its UML object space, which the port
/// does not have (D6). It lives while a model is imported or exported; the model itself is the Mold model.
/// </summary>
public sealed class TBoldUMLModelGraph
{
    private readonly List<TBoldUMLObject> fObjects = [];
    private int fLastIndex;

    public IReadOnlyList<TBoldUMLObject> Objects => fObjects;

    public TBoldUMLObject Create(string MetaClass)
    {
        var obj = new TBoldUMLObject(this, MetaClass) { LocalIndex = ++fLastIndex };
        fObjects.Add(obj);
        return obj;
    }

    /// <summary>The objects of a class and its subclasses.</summary>
    public IEnumerable<TBoldUMLObject> AllInstances(string MetaClass) => fObjects.Where(o => o.IsKindOf(MetaClass));

    internal void Remove(TBoldUMLObject Obj) => fObjects.Remove(Obj);
}

/// <summary>An element of a UML model: its class, attributes and references, by the UML metamodel's names.</summary>
public sealed class TBoldUMLObject
{
    private readonly Dictionary<string, string> fAttributes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<TBoldUMLObject>> fReferences = new(StringComparer.Ordinal);

    internal TBoldUMLObject(TBoldUMLModelGraph Graph, string MetaClass)
    {
        this.Graph = Graph;
        this.MetaClass = MetaClass;
    }

    public TBoldUMLModelGraph Graph { get; }

    /// <summary>The UML class, as Class, Attribute or AssociationEnd.</summary>
    public string MetaClass { get; set; }

    /// <summary>The number of the object in its graph, from 1, in the order they were made.</summary>
    public int LocalIndex { get; internal init; }

    /// <summary>The id it had in an XMI file it was read from.</summary>
    public string XMIId { get; set; } = "";

    public bool IsKindOf(string Kind) => TBoldUMLMetaModel.IsKindOf(MetaClass, Kind);

    public bool Deleted { get; private set; }

    public IReadOnlyDictionary<string, string> AttributeValues => fAttributes;

    public IEnumerable<string> ReferenceNames => fReferences.Keys;

    /// <summary>An attribute's value; empty when not set. Setting empty clears it.</summary>
    public string this[string Attribute]
    {
        get => fAttributes.GetValueOrDefault(Attribute, "");
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                fAttributes.Remove(Attribute);
            }
            else
            {
                fAttributes[Attribute] = value;
            }
        }
    }

    public bool HasAttribute(string Attribute) => fAttributes.ContainsKey(Attribute);

    /// <summary>The objects of a reference (a copy).</summary>
    public List<TBoldUMLObject> Refs(string Reference) => fReferences.TryGetValue(Reference, out List<TBoldUMLObject>? list) ? [.. list] : [];

    public TBoldUMLObject? Ref(string Reference) => fReferences.TryGetValue(Reference, out List<TBoldUMLObject>? list) && list.Count > 0 ? list[0] : null;

    /// <summary>Adds an object to a reference, and this object to the reference at the other end.</summary>
    public void AddRef(string Reference, TBoldUMLObject Obj)
    {
        if (TBoldUMLMetaModel.IsSingle(Reference))
        {
            SetRef(Reference, Obj);
            return;
        }

        AddOneSide(Reference, Obj);
        if (TBoldUMLMetaModel.InverseOf(Reference) is { } inverse)
        {
            if (TBoldUMLMetaModel.IsSingle(inverse))
            {
                Obj.SetRef(inverse, this);
            }
            else
            {
                Obj.AddOneSide(inverse, this);
            }
        }
    }

    /// <summary>Sets a single reference (null clears it), and keeps the other end in step.</summary>
    public void SetRef(string Reference, TBoldUMLObject? Obj)
    {
        TBoldUMLObject? old = Ref(Reference);
        if (old == Obj)
        {
            return;
        }

        string? inverse = TBoldUMLMetaModel.InverseOf(Reference);
        fReferences.Remove(Reference);
        if (old is not null && inverse is not null)
        {
            old.RemoveOneSide(inverse, this);
        }

        if (Obj is null)
        {
            return;
        }

        fReferences[Reference] = [Obj];
        if (inverse is not null)
        {
            if (TBoldUMLMetaModel.IsSingle(inverse))
            {
                Obj.SetRef(inverse, this);
            }
            else
            {
                Obj.AddOneSide(inverse, this);
            }
        }
    }

    /// <summary>Removes an object from a reference, and this object from the other end.</summary>
    public void RemoveRef(string Reference, TBoldUMLObject Obj)
    {
        RemoveOneSide(Reference, Obj);
        if (TBoldUMLMetaModel.InverseOf(Reference) is { } inverse)
        {
            Obj.RemoveOneSide(inverse, this);
        }
    }

    private void AddOneSide(string Reference, TBoldUMLObject Obj)
    {
        if (!fReferences.TryGetValue(Reference, out List<TBoldUMLObject>? list))
        {
            fReferences[Reference] = list = [];
        }

        if (!list.Contains(Obj))
        {
            list.Add(Obj);
        }
    }

    private void RemoveOneSide(string Reference, TBoldUMLObject Obj)
    {
        if (fReferences.TryGetValue(Reference, out List<TBoldUMLObject>? list))
        {
            list.Remove(Obj);
            if (list.Count == 0)
            {
                fReferences.Remove(Reference);
            }
        }
    }

    /// <summary>Deletes the object, the objects it owns, and every reference to it.</summary>
    public void Delete()
    {
        if (Deleted)
        {
            return;
        }

        Deleted = true;
        foreach (string reference in fReferences.Keys.ToList())
        {
            foreach (TBoldUMLObject obj in Refs(reference))
            {
                if (TBoldUMLMetaModel.CompositeReferences.Contains(reference) && TBoldUMLMetaModel.InverseOf(reference) is not null)
                {
                    obj.Delete();
                }
                else
                {
                    RemoveRef(reference, obj);
                }
            }
        }

        foreach (TBoldUMLObject other in Graph.Objects.ToList())
        {
            foreach (string reference in other.fReferences.Keys.ToList())
            {
                other.RemoveOneSide(reference, this);
            }
        }

        Graph.Remove(this);
    }

    // The UML model's members, by the names of Delphi's UML model.

    public string name { get => this["name"]; set => this["name"] = value; }

    public TBoldUMLObject? namespace_ { get => Ref("namespace"); set => SetRef("namespace", value); }

    public List<TBoldUMLObject> ownedElement => Refs("ownedElement");

    public TBoldUMLObject? owner { get => Ref("owner"); set => SetRef("owner", value); }

    public List<TBoldUMLObject> feature => Refs("feature");

    /// <summary>The object that owns this one: its namespace, owner, association, association end or operation.</summary>
    public TBoldUMLObject? qualifyingOwner =>
        Ref("associationEnd") ?? Ref("owner") ?? Ref("association") ?? Ref("behavioralFeature") ?? Ref("modelElement") ?? Ref("namespace");

    public string qualifiedName => qualifyingOwner is { } o ? o.qualifiedName + "." + name : name;

    /// <summary>The model the element belongs to.</summary>
    public TBoldUMLObject? model
    {
        get
        {
            for (TBoldUMLObject? o = this; o is not null; o = o.qualifyingOwner)
            {
                if (o.MetaClass == "Model" && o.qualifyingOwner is null)
                {
                    return o;
                }
            }

            return null;
        }
    }

    /// <summary>All elements of the model, at any depth.</summary>
    public IEnumerable<TBoldUMLObject> AllOwnedElements => Graph.Objects.Where(o => o != this && o.model == this);

    public List<TBoldUMLObject> Classes => [.. AllOwnedElements.Where(o => o.MetaClass == "Class")];

    public List<TBoldUMLObject> Associations => [.. AllOwnedElements.Where(o => o.MetaClass == "Association")];

    public List<TBoldUMLObject> TaggedValues => Refs("taggedValue");

    public TBoldUMLObject? taggedValue(string Tag) => TaggedValues.FirstOrDefault(t => t["tag"] == Tag);

    /// <summary>A tagged value, or the same tag with its first letter upper case.</summary>
    public TBoldUMLObject? FindTaggedValue(string Tag) =>
        taggedValue(Tag) ?? (Tag.Length > 0 ? taggedValue(char.ToUpperInvariant(Tag[0]) + Tag[1..]) : null);

    public string GetTaggedValue(string Tag) => FindTaggedValue(Tag)?["value"].Trim() ?? "";

    public void SetTaggedValue(string Tag, string Value) => EnsuredTaggedValue(Tag)["value"] = Value;

    public TBoldUMLObject EnsuredTaggedValue(string Tag)
    {
        if (taggedValue(Tag) is { } tv)
        {
            return tv;
        }

        TBoldUMLObject result = Graph.Create("TaggedValue");
        result["tag"] = Tag;
        AddRef("taggedValue", result);
        return result;
    }

    public void DeleteTaggedValue(string Tag) => taggedValue(Tag)?.Delete();

    public string GetBoldTV(string Tag) => GetTaggedValue(BoldDefs.BOLDTVPREFIX + Tag);

    public void SetBoldTV(string Tag, string Value) => (FindTaggedValue(BoldDefs.BOLDTVPREFIX + Tag) ?? EnsuredTaggedValue(BoldDefs.BOLDTVPREFIX + Tag))["value"] = Value;

    public string stereotypeName
    {
        get => Ref("stereotype")?.name ?? "";
        set
        {
            if (value == "")
            {
                SetRef("stereotype", null);
                return;
            }

            TBoldUMLObject owningModel = model ?? throw new EBold("Can't set StereotypeName in for Element not belonging to model");
            TBoldUMLObject? stereotype = Graph.AllInstances("Stereotype").FirstOrDefault(s => s.name == value && s.model == owningModel);
            if (stereotype is null)
            {
                stereotype = Graph.Create("Stereotype");
                stereotype.name = value;
                stereotype.namespace_ = owningModel;
            }

            SetRef("stereotype", stereotype);
        }
    }

    /// <summary>Persistent as the tagged value says; for a class, also when it says nothing.</summary>
    public bool persistent
    {
        get
        {
            string value = GetTaggedValue(TAG_PERSISTENCE);
            return (IsKindOf("Classifier") && value == "") || SysUtils.SameText(value, TV_PERSISTENCE_PERSISTENT);
        }
        set
        {
            // as a derived member of Delphi's UML model: written back only when it changes
            if (persistent != value)
            {
                SetTaggedValue(TAG_PERSISTENCE, value ? TV_PERSISTENCE_PERSISTENT : TV_PERSISTENCE_TRANSIENT);
            }
        }
    }

    public bool derived
    {
        get => TVIsTrue(GetTaggedValue(TAG_DERIVED));
        set
        {
            if (derived != value)
            {
                SetTaggedValue(TAG_DERIVED, value ? TV_TRUE : TV_FALSE);
            }
        }
    }

    public bool GetBoolean(string Attribute) => SysUtils.SameText(this[Attribute], "true");

    public void SetBoolean(string Attribute, bool Value) => this[Attribute] = Value ? "true" : "false";

    public bool isAbstract { get => GetBoolean("isAbstract"); set => SetBoolean("isAbstract", value); }

    public bool isNavigable { get => !HasAttribute("isNavigable") || GetBoolean("isNavigable"); set => SetBoolean("isNavigable", value); }

    public string multiplicity { get => this["multiplicity"]; set => this["multiplicity"] = value; }

    public bool multi => BoldUtils.GetUpperLimitForMultiplicity(multiplicity) > 1;

    public bool isOrdered { get => this["ordering"] == "ordered"; set => this["ordering"] = value ? "ordered" : "unordered"; }

    public TBoldUMLObject? type_ { get => Ref("type"); set => SetRef("type", value); }

    /// <summary>The name of the type; setting it finds the classifier of that name in the model, or makes a data type.</summary>
    public string typeName
    {
        get => type_?.name ?? "";
        set
        {
            if (value == "")
            {
                type_ = null;
                return;
            }

            TBoldUMLObject owningModel = model ?? throw new EBold($"Can't set typeName for {MetaClass} not belonging to model");
            TBoldUMLObject? type = Graph.AllInstances("GeneralizableElement").FirstOrDefault(c => c.name == value && c.model == owningModel)
                ?? Graph.AllInstances("Classifier").FirstOrDefault(c => c.name == value && c.model == owningModel);
            if (type is null)
            {
                type = Graph.Create("DataType");
                type.namespace_ = owningModel;
                type.name = value;
            }

            type_ = type;
        }
    }

    public List<TBoldUMLObject> connection => Refs("connection");

    public TBoldUMLObject? association => Ref("association");

    public TBoldUMLObject? otherEnd => association?.connection.FirstOrDefault(e => e != this);

    public List<TBoldUMLObject> qualifier => Refs("qualifier");

    public List<TBoldUMLObject> parameter => Refs("parameter");

    public List<TBoldUMLObject> generalization => Refs("generalization");

    public List<TBoldUMLObject> specialization => Refs("specialization");

    /// <summary>The parent of the first generalization.</summary>
    public TBoldUMLObject? Superclass => generalization.Select(g => g.Ref("parent")).FirstOrDefault(p => p is not null);

    /// <summary>The class of an association class's association, or the association of its class.</summary>
    public TBoldUMLObject? class_ { get => Ref("class"); set => SetRef("class", value); }

    public TBoldUMLObject? associationOfClass { get => Ref("associationOfClass"); set => SetRef("associationOfClass", value); }

    public bool isAssociationClass => associationOfClass is not null;

    /// <summary>Makes the first generalization lead to the parent (none removes it).</summary>
    public void SetFirstParent(TBoldUMLObject? Parent)
    {
        if (generalization.Count == 0)
        {
            TBoldUMLObject g = Graph.Create("Generalization");
            g.namespace_ = this;
            g.SetRef("child", this);
        }

        if (Parent is null)
        {
            generalization[0].Delete();
        }
        else
        {
            generalization[0].SetRef("parent", Parent);
        }
    }

    private static readonly string[] VisibilityNames = ["private", "protected", "public"];
    private static readonly string[] AggregationNames = ["none", "aggregate", "composite"];
    private static readonly string[] ChangeabilityNames = ["changeable", "frozen", "addOnly"];
    private static readonly string[] ScopeNames = ["instance", "classifier"];
    private static readonly string[] DirectionNames = ["in", "out", "inout", "return"];

    private int EnumAttribute(string Attribute, string[] Names, int Default)
    {
        int i = Array.FindIndex(Names, n => string.Equals(n, this[Attribute], StringComparison.OrdinalIgnoreCase));
        return i >= 0 ? i : Default;
    }

    /// <summary>The visibility; "package" and an unset one read as public.</summary>
    public TVisibilityKind visibility
    {
        get => (TVisibilityKind)EnumAttribute("visibility", VisibilityNames, (int)TVisibilityKind.vkPublic);
        set => this["visibility"] = VisibilityNames[(int)value];
    }

    public TAggregationKind aggregation
    {
        get => (TAggregationKind)EnumAttribute("aggregation", AggregationNames, (int)TAggregationKind.akNone);
        set => this["aggregation"] = AggregationNames[(int)value];
    }

    public TChangeableKind changeability
    {
        get => (TChangeableKind)EnumAttribute("changeability", ChangeabilityNames, (int)TChangeableKind.ckChangeable);
        set => this["changeability"] = ChangeabilityNames[(int)value];
    }

    public TScopeKind ownerScope
    {
        get => (TScopeKind)EnumAttribute("ownerScope", ScopeNames, (int)TScopeKind.skInstance);
        set => this["ownerScope"] = ScopeNames[(int)value];
    }

    /// <summary>The direction of a parameter.</summary>
    public TBoldParameterDirectionKind kind
    {
        get => (TBoldParameterDirectionKind)EnumAttribute("kind", DirectionNames, (int)TBoldParameterDirectionKind.pdIn);
        set => this["kind"] = DirectionNames[(int)value];
    }

    /// <summary>The initial value of an attribute, as text.</summary>
    public string initialValue { get => this["initialValue"]; set => this["initialValue"] = value; }

    /// <summary>The body of a constraint, as text.</summary>
    public string body { get => this["body"]; set => this["body"] = value; }

    public List<TBoldUMLObject> constraint => Refs("constraint");

    public bool isRoot { get => GetBoolean("isRoot"); set => SetBoolean("isRoot", value); }

    public bool isLeaf { get => GetBoolean("isLeaf"); set => SetBoolean("isLeaf", value); }

    public override string ToString() => $"{MetaClass} {name}";
}
