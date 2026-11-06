// Delphi: Source/UMLModel/ModelLinks/XMI/BoldUMLModelMOFAdapters.pas
// Port map: CSharp/port/units/BoldUMLModelMOFAdapters.md
// The exporter sees a UML model through the MOF interfaces. Delphi's adapters read its UML object space and its type
// info; the port's read the UML graph (D6), and take what is an attribute, a reference and of which type from the
// UML 1.3 DTD and a table of the attributes' types. An association class, a class and an association in the graph,
// is one AssociationClass to the exporter, as it was in Delphi.

using static Bold.BoldUMLXMICommon;

namespace Bold;

/// <summary>What the adapters need to know of the UML 1.3 metamodel that the graph does not say.</summary>
internal static class TBoldUMLMOFMetaModel
{
    private static Dictionary<string, string>? fQualifiedClassNames;

    /// <summary>The attributes of a type other than string, with the value an unset one has.</summary>
    internal static readonly Dictionary<string, (string Kind, string Default)> AttributeTypes = new()
    {
        ["isSpecification"] = ("Boolean", "false"),
        ["isRoot"] = ("Boolean", "false"),
        ["isLeaf"] = ("Boolean", "false"),
        ["isAbstract"] = ("Boolean", "false"),
        ["isActive"] = ("Boolean", "false"),
        ["isNavigable"] = ("Boolean", "true"),
        ["isQuery"] = ("Boolean", "false"),
        ["visibility"] = ("Enum", "public"),
        ["ownerScope"] = ("Enum", "instance"),
        ["targetScope"] = ("Enum", "instance"),
        ["changeability"] = ("Enum", "changeable"),
        ["aggregation"] = ("Enum", "none"),
        ["ordering"] = ("Enum", "unordered"),
        ["kind"] = ("Enum", "in"),
        ["concurrency"] = ("Enum", "sequential"),
        ["multiplicity"] = ("Multiplicity", ""),
        ["initialValue"] = ("Expression", ""),
        ["defaultValue"] = ("Expression", ""),
        ["body"] = ("BooleanExpression", ""),
    };

    /// <summary>The references whose objects are written inside the referring object.</summary>
    internal static readonly HashSet<string> CompositeReferences = ["ownedElement", "feature", "connection", "qualifier", "parameter", "taggedValue"];

    /// <summary>The qualified DTD name of a metaclass, as Foundation.Core.Class.</summary>
    internal static string QualifiedClassName(string MetaClass)
    {
        fQualifiedClassNames ??= TBoldUMLDTDReader.Shared.ClassNames
            .GroupBy(UnqualifiedName)
            .ToDictionary(g => g.Key, g => g.First());
        return fQualifiedClassNames.GetValueOrDefault(MetaClass, MetaClass);
    }

    /// <summary>The unqualified names of the members of a DTD class, in its order.</summary>
    internal static IEnumerable<string> MemberNames(string QualifiedClassName) =>
        TBoldUMLDTDReader.Shared.MembersOfClass(QualifiedClassName)
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(m => !m.StartsWith("XMI.", StringComparison.Ordinal))
            .Select(UnqualifiedName);

    /// <summary>The class (unqualified) that defines a member of a DTD class.</summary>
    internal static string DefiningClass(string QualifiedClassName, string Member) =>
        UnqualifiedName(FindDefiningClass(QualifiedClassName, Member));
}

/// <summary>An element of the UML graph as a MOF object.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFObjectAdapter : IBoldMOFObject
{
    private readonly TBoldUMLObject fObj;
    private readonly TBoldUMLXMILink fOwningLink;

    /// <summary>The adapter of an element; the association of an association class is its class.</summary>
    [Delphi(PortStatus.Ported, Note = "Link objects are not in the graph.")]
    public TBoldUMLMOFObjectAdapter(TBoldUMLObject Obj, TBoldUMLXMILink OwningLink)
    {
        System.Diagnostics.Debug.Assert(!BoldUMLModelMOFAdapters.IsOfBoldAddedClass(Obj));
        fOwningLink = OwningLink;
        fObj = BoldUMLModelMOFAdapters.IsAssociationClassAssociation(Obj) ? Obj.class_! : Obj;
    }

    /// <summary>"x" and the element's number in its graph.</summary>
    [Delphi(PortStatus.Ported, Note = "The element's number in the graph where Delphi used its object locator.")]
    public string LocalId() => "x" + fObj.LocalIndex;

    [Delphi(PortStatus.Ported)]
    public virtual string QualifiedClassName() =>
        BoldUMLModelMOFAdapters.IsAssociationClassClass(fObj) ? "Foundation.Core.AssociationClass" : TBoldUMLMOFMetaModel.QualifiedClassName(fObj.MetaClass);

    [Delphi(PortStatus.Ported)]
    public virtual IBoldMOFAttributeList Attributes() => new TBoldUMLMOFAttributeListAdapter(fObj, fOwningLink, QualifiedClassName());

    [Delphi(PortStatus.Ported)]
    public virtual IBoldMOFReferenceList References() => new TBoldUMLMOFReferenceListAdapter(fObj, fOwningLink, QualifiedClassName());

    [Delphi(PortStatus.Ported)] protected TBoldUMLObject Obj => fObj;
}

/// <summary>The attributes of an element: the DTD's attributes of its class that it has, or that have a default.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFAttributeListAdapter : IBoldMOFAttributeList
{
    private readonly TBoldUMLXMILink fOwningLink;
    private readonly TBoldUMLObject fObj;
    private readonly string fQualifiedClassName;
    private readonly List<string> fAttrs = [];

    [Delphi(PortStatus.Ported, Note = "Also takes the qualified class name the element is written as.")]
    public TBoldUMLMOFAttributeListAdapter(TBoldUMLObject Obj, TBoldUMLXMILink OwningLink, string QualifiedClassName)
    {
        fOwningLink = OwningLink;
        fObj = Obj;
        fQualifiedClassName = QualifiedClassName;
        foreach (string member in TBoldUMLMOFMetaModel.MemberNames(QualifiedClassName))
        {
            bool isAttribute = member == "name" || Obj.HasAttribute(member) ||
                (TBoldUMLMOFMetaModel.AttributeTypes.TryGetValue(member, out var type) && type.Default != "");
            if (isAttribute && !Obj.ReferenceNames.Contains(member) && !IsBoldAddedAttribute(member))
            {
                fAttrs.Add(member);
            }
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLMOFAttributeListAdapter.Destroy", Note = "Nothing to free.")]
    public void Dispose()
    {
    }

    [Delphi(PortStatus.Ported, Note = "The graph has none of the attributes Delphi's UML model added (stereotype Bold).")]
    private bool IsBoldAddedAttribute(string Attr) => BoldUMLModelMOFAdapters.IsInheritedButShouldNotBe(fObj, fQualifiedClassName, Attr);

    [Delphi(PortStatus.Ported)]
    public int Count() => fAttrs.Count;

    [Delphi(PortStatus.Ported)]
    public virtual IBoldMOFAttribute Attribute(int index) => new TBoldUMLMOFAttributeAdapter(fObj, fAttrs[index], fQualifiedClassName, fOwningLink);

    [Delphi(PortStatus.Ported)] protected List<string> Attrs => fAttrs;
}

/// <summary>The references of an element; for an association class, its class's and its association's.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFReferenceListAdapter : IBoldMOFReferenceList
{
    private readonly TBoldUMLXMILink fOwningLink;
    private readonly string fQualifiedClassName;
    private readonly List<(TBoldUMLObject Owner, string Name)> fRefs = [];

    [Delphi(PortStatus.Ported, Note = "Also takes the qualified class name the element is written as.")]
    public TBoldUMLMOFReferenceListAdapter(TBoldUMLObject Obj, TBoldUMLXMILink OwningLink, string QualifiedClassName)
    {
        fOwningLink = OwningLink;
        fQualifiedClassName = QualifiedClassName;
        if (BoldUMLModelMOFAdapters.IsAssociationClassClass(Obj))
        {
            GetMIReferences(fRefs, Obj.associationOfClass!, Obj);
        }
        else
        {
            GetReferences(fRefs, Obj);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLMOFReferenceListAdapter.Destroy", Note = "Nothing to free.")]
    public void Dispose()
    {
    }

    /// <summary>The references of both halves of an association class; one of each name, the second half's when only it has it.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetMIReferences(List<(TBoldUMLObject Owner, string Name)> List, TBoldUMLObject Obj1, TBoldUMLObject Obj2)
    {
        var list2 = new List<(TBoldUMLObject Owner, string Name)>();
        GetReferences(List, Obj1);
        GetReferences(list2, Obj2);
        foreach ((TBoldUMLObject owner, string name) in list2)
        {
            if (!List.Any(r => r.Name == name))
            {
                List.Add((owner, name));
            }
        }
    }

    /// <summary>The element's references that its class has in the DTD.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetReferences(List<(TBoldUMLObject Owner, string Name)> List, TBoldUMLObject Obj)
    {
        var members = TBoldUMLMOFMetaModel.MemberNames(fQualifiedClassName).ToHashSet();
        foreach (string name in Obj.ReferenceNames)
        {
            if (members.Contains(name) && !IsBoldAddedRole(Obj, name))
            {
                List.Add((Obj, name));
            }
        }
    }

    [Delphi(PortStatus.Ported, Note = "The graph has none of the roles Delphi's UML model added; a classifier's association ends are not written, as in Delphi.")]
    private bool IsBoldAddedRole(TBoldUMLObject Obj, string Role) =>
        (Role == "associationEnd" && Obj.IsKindOf("Classifier")) || BoldUMLModelMOFAdapters.IsInheritedButShouldNotBe(Obj, fQualifiedClassName, Role);

    [Delphi(PortStatus.Ported)]
    public int Count() => fRefs.Count;

    [Delphi(PortStatus.Ported)]
    public virtual IBoldMOFReference Reference(int index) => new TBoldUMLMOFReferenceAdapter(fRefs[index].Owner, fRefs[index].Name, fQualifiedClassName, fOwningLink);

    [Delphi(PortStatus.Ported)] protected List<(TBoldUMLObject Owner, string Name)> Refs => fRefs;
}

/// <summary>An attribute of an element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFAttributeAdapter : IBoldMOFAttribute
{
    private readonly TBoldUMLXMILink fOwningLink;
    private readonly TBoldUMLObject fObj;
    private readonly string fAttr;
    private readonly string fQualifiedClassName;

    [Delphi(PortStatus.Ported, Note = "Takes the element, the attribute's name and the element's qualified class name, where Delphi took the attribute.")]
    public TBoldUMLMOFAttributeAdapter(TBoldUMLObject Obj, string Attr, string QualifiedClassName, TBoldUMLXMILink OwningLink)
    {
        fOwningLink = OwningLink;
        fObj = Obj;
        fAttr = Attr;
        fQualifiedClassName = QualifiedClassName;
    }

    private string Kind => TBoldUMLMOFMetaModel.AttributeTypes.TryGetValue(fAttr, out var type) ? type.Kind : "String";

    private string Value => fObj.HasAttribute(fAttr)
        ? fObj[fAttr]
        : TBoldUMLMOFMetaModel.AttributeTypes.TryGetValue(fAttr, out var type) ? type.Default : "";

    [Delphi(PortStatus.Ported)] public bool IsBoolean() => Kind == "Boolean";
    [Delphi(PortStatus.Ported)] public bool IsEnum() => Kind == "Enum";
    [Delphi(PortStatus.Ported)] public bool IsDerived() => false;
    [Delphi(PortStatus.Ported)] public bool IsObject() => Kind == "Multiplicity" || IsNameOfExpressionClass(Kind);

    /// <summary>A multiplicity or an expression, as an object.</summary>
    [Delphi(PortStatus.Ported)]
    public IBoldMOFObject AsObject()
    {
        if (Kind == "Multiplicity")
        {
            return new TBoldUMLMOFMultiplicityAdapter(Value, fOwningLink);
        }

        if (IsNameOfExpressionClass(Kind))
        {
            string expressionClass = fObj.IsKindOf("Method") && fAttr == "body" ? "ProcedureExpression" : Kind;
            return new TBoldUMLMOFExpressionAdapter(Value, expressionClass, fOwningLink);
        }

        throw new EBoldInternal($"{GetType().Name}.AsObject: wrong type");
    }

    /// <summary>The value; a tag "Tool.Tag" is written "RationalRose$Tool:Tag" when Rose's tags are translated.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual string AsString()
    {
        string value = Value;
        if (fOwningLink.TranslateRoseTaggedValues && fObj.MetaClass == "TaggedValue" && fAttr == "tag" && value.Contains('.'))
        {
            int dotpos = value.IndexOf('.');
            return "RationalRose$" + value[..dotpos] + ":" + value[(dotpos + 1)..];
        }

        return value;
    }

    [Delphi(PortStatus.Ported)]
    public bool AsBoolean()
    {
        if (!IsBoolean())
        {
            throw new EBoldInternal($"{GetType().Name}.AsBoolean: Attribute is not boolean");
        }

        return SysUtils.SameText(Value, "true");
    }

    [Delphi(PortStatus.Ported)]
    public virtual string QualifiedName() => QualifiedMemberName(fQualifiedClassName, fAttr);

    [Delphi(PortStatus.Ported)] protected string Attr => fAttr;
}

/// <summary>A reference of an element.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFReferenceAdapter : IBoldMOFReference
{
    private readonly TBoldUMLXMILink fOwningLink;
    private readonly TBoldUMLObject fObj;
    private readonly string fRef;
    private readonly string fQualifiedClassName;

    [Delphi(PortStatus.Ported, Note = "Takes the element, the reference's name and the element's qualified class name, where Delphi took the role.")]
    public TBoldUMLMOFReferenceAdapter(TBoldUMLObject Obj, string Ref, string QualifiedClassName, TBoldUMLXMILink OwningLink)
    {
        fOwningLink = OwningLink;
        fObj = Obj;
        fRef = Ref;
        fQualifiedClassName = QualifiedClassName;
    }

    [Delphi(PortStatus.Ported)] public bool IsMulti() => !TBoldUMLMetaModel.IsSingle(fRef);
    [Delphi(PortStatus.Ported)] public bool IsComposite() => TBoldUMLMOFMetaModel.CompositeReferences.Contains(fRef);
    [Delphi(PortStatus.Ported)] public bool IsDerived() => false;

    [Delphi(PortStatus.Ported)]
    public virtual string QualifiedName() => QualifiedMemberName(fQualifiedClassName, fRef);

    [Delphi(PortStatus.Ported)]
    public virtual IBoldMOFObject? SingleObject()
    {
        if (IsMulti())
        {
            throw new EBoldInternal($"{GetType().Name}.SingleObject: Reference is not a singlelink");
        }

        TBoldUMLObject? anObj = fObj.Ref(fRef);
        return anObj is not null && !BoldUMLModelMOFAdapters.IsOfBoldAddedClass(anObj) ? new TBoldUMLMOFObjectAdapter(anObj, fOwningLink) : null;
    }

    [Delphi(PortStatus.Ported)]
    public virtual IBoldMOFObjectList Objects()
    {
        if (!IsMulti())
        {
            throw new EBoldInternal($"{GetType().Name}.Objects: Reference is not a multilink");
        }

        return BoldUMLModelMOFAdapters.CreateObjectListAdapter(fObj, fRef, fQualifiedClassName, fOwningLink);
    }

    [Delphi(PortStatus.Ported)] protected string Ref => fRef;
}

/// <summary>A list of elements.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldGenericMOFObjectListAdapter : IBoldMOFObjectList
{
    protected TBoldUMLXMILink fOwningLink = null!;
    protected List<TBoldUMLObject> fObjList = [];

    [Delphi(PortStatus.Ported)]
    public int Count() => fObjList.Count;

    [Delphi(PortStatus.Ported)]
    public virtual IBoldMOFObject GetObject(int index) => new TBoldUMLMOFObjectAdapter(fObjList[index], fOwningLink);

    [Delphi(PortStatus.Ported, Symbol = "TBoldGenericMOFObjectListAdapter.Destroy", Note = "Nothing to free.")]
    public void Dispose()
    {
    }

    [Delphi(PortStatus.Ported)] protected List<TBoldUMLObject> ObjList => fObjList;
}

/// <summary>The elements of a reference; an association class's association goes when its class is there too.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFObjectListAdapter : TBoldGenericMOFObjectListAdapter
{
    [Delphi(PortStatus.Ported)]
    public TBoldUMLMOFObjectListAdapter(List<TBoldUMLObject> ObjList, TBoldUMLXMILink OwningLink)
    {
        fOwningLink = OwningLink;
        fObjList = [.. ObjList];
        fObjList.RemoveAll(o => BoldUMLModelMOFAdapters.IsAssociationClassAssociation(o) && fObjList.Contains(o.class_!));
    }
}

/// <summary>The tagged values of both halves of an association class; the class's when both have a tag.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFMultipleInheritenceTVListAdapter : TBoldGenericMOFObjectListAdapter
{
    [Delphi(PortStatus.Ported)]
    public TBoldUMLMOFMultipleInheritenceTVListAdapter(TBoldUMLObject Element, List<TBoldUMLObject> TVList2, TBoldUMLXMILink OwningLink)
    {
        fOwningLink = OwningLink;
        fObjList = Element.TaggedValues;
        fObjList.AddRange(TVList2.Where(tv => Element.taggedValue(tv["tag"]) is null));
    }
}

/// <summary>The elements of the same reference of both halves of an association class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFMergedObjectListAdapter : TBoldGenericMOFObjectListAdapter
{
    [Delphi(PortStatus.Ported)]
    public TBoldUMLMOFMergedObjectListAdapter(List<TBoldUMLObject> ObjList1, List<TBoldUMLObject> ObjList2, TBoldUMLXMILink OwningLink)
    {
        fOwningLink = OwningLink;
        fObjList = [.. ObjList1];
        fObjList.AddRange(ObjList2.Where(o => !fObjList.Contains(o)));
    }
}

/// <summary>A multiplicity as an object: its ranges.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFMultiplicityAdapter : IBoldMOFObject, IBoldMOFReferenceList, IBoldMOFReference, IBoldMOFObjectList, IBoldMOFAttributeList
{
    private readonly TBoldUMLXMILink fOwningLink;
    private readonly TStringList fRanges = new();

    [Delphi(PortStatus.Ported, Note = "Takes the multiplicity's text where Delphi took the attribute; splits it at commas only, where Delphi's CommaText also split \"2 .. *\" at its spaces.")]
    public TBoldUMLMOFMultiplicityAdapter(string Mult, TBoldUMLXMILink OwningLink)
    {
        fOwningLink = OwningLink;
        foreach (string range in Mult.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            fRanges.Add(range);
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLMOFMultiplicityAdapter.Destroy", Note = "Nothing to free.")]
    public void Dispose()
    {
    }

    [Delphi(PortStatus.Ported)] public IBoldMOFAttributeList Attributes() => this;
    [Delphi(PortStatus.Ported)] public IBoldMOFReferenceList References() => this;
    [Delphi(PortStatus.Ported)] public string QualifiedClassName() => "Foundation.Data_Types.Multiplicity";
    [Delphi(PortStatus.Ported)] public string LocalId() => throw new EBoldInternal($"{GetType().Name}.LocalId: Multiplicity can only be embedded");
    [Delphi(PortStatus.Ported)] public int AttributeCount() => 0;
    int IBoldMOFAttributeList.Count() => AttributeCount();
    [Delphi(PortStatus.Ported)] public IBoldMOFAttribute Attribute(int index) => throw new EBoldInternal($"{GetType().Name}.Attribute: Multiplicity has no attibutes");
    [Delphi(PortStatus.Ported)] public int Count() => 1;

    [Delphi(PortStatus.Ported)]
    public IBoldMOFReference Reference(int index)
    {
        System.Diagnostics.Debug.Assert(index == 0);
        return this;
    }

    [Delphi(PortStatus.Ported)] public bool IsMulti() => true;
    [Delphi(PortStatus.Ported)] public bool IsComposite() => true;
    [Delphi(PortStatus.Ported)] public bool IsDerived() => false;
    [Delphi(PortStatus.Ported)] public IBoldMOFObject? SingleObject() => throw new EBoldInternal($"{GetType().Name}.SingleObject: Multiplicity.ranges is multi");
    [Delphi(PortStatus.Ported)] public IBoldMOFObjectList Objects() => this;
    [Delphi(PortStatus.Ported)] public string QualifiedName() => "Foundation.Data_Types.Multiplicity.range";
    [Delphi(PortStatus.Ported)] public int ObjectCount() => fRanges.Count;
    int IBoldMOFObjectList.Count() => ObjectCount();
    [Delphi(PortStatus.Ported)] public IBoldMOFObject GetObject(int index) => new TBoldUMLMOFMultiplicityRangeAdapter(fRanges[index], fOwningLink);
}

/// <summary>A range of a multiplicity: its lower and upper bounds (-1 for unlimited).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFMultiplicityRangeAdapter : IBoldMOFObject, IBoldMOFAttributeList, IBoldMOFReferenceList
{
    private readonly TBoldUMLXMILink fOwningLink;
    private readonly int fLower;
    private readonly int fUpper;

    [Delphi(PortStatus.Ported)]
    public TBoldUMLMOFMultiplicityRangeAdapter(string Range, TBoldUMLXMILink OwningLink)
    {
        fOwningLink = OwningLink;
        int splitpos = Range.IndexOf("..", StringComparison.Ordinal);
        if (splitpos < 0)
        {
            if (Range.Trim() is "*" or "n")
            {
                fLower = 0;
                fUpper = -1;
            }
            else
            {
                fLower = SysUtils.StrToInt(Range.Trim());
                fUpper = fLower;
            }
        }
        else
        {
            fLower = SysUtils.StrToInt(Range[..splitpos].Trim());
            string upper = Range[(splitpos + 2)..].Trim();
            fUpper = upper is "*" or "n" ? -1 : SysUtils.StrToInt(upper);
        }
    }

    [Delphi(PortStatus.Ported)] public IBoldMOFAttributeList Attributes() => this;
    [Delphi(PortStatus.Ported)] public IBoldMOFReferenceList References() => this;
    [Delphi(PortStatus.Ported)] public string QualifiedClassName() => "Foundation.Data_Types.MultiplicityRange";
    [Delphi(PortStatus.Ported)] public string LocalId() => "";
    [Delphi(PortStatus.Ported)] public int Count() => 2;

    [Delphi(PortStatus.Ported)]
    public IBoldMOFAttribute Attribute(int index) => index switch
    {
        0 => new TBoldUMLMOFMultiplicityRangeBoundAdapter("lower", fLower),
        1 => new TBoldUMLMOFMultiplicityRangeBoundAdapter("upper", fUpper),
        _ => throw new EBoldInternal($"{GetType().Name}.Attribute: index out of bounds {index}"),
    };

    [Delphi(PortStatus.Ported)] public int RefCount() => 0;
    int IBoldMOFReferenceList.Count() => RefCount();
    [Delphi(PortStatus.Ported)] public IBoldMOFReference Reference(int index) => throw new EBoldInternal($"{GetType().Name}.Reference: MultiplicityRange has no references");
}

/// <summary>A bound of a multiplicity range.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFMultiplicityRangeBoundAdapter : IBoldMOFAttribute
{
    private readonly string fAttrName;
    private readonly int fValue;

    [Delphi(PortStatus.Ported)]
    public TBoldUMLMOFMultiplicityRangeBoundAdapter(string AttrName, int Value)
    {
        fAttrName = AttrName;
        fValue = Value;
    }

    [Delphi(PortStatus.Ported)] public bool IsBoolean() => false;
    [Delphi(PortStatus.Ported)] public bool IsEnum() => false;
    [Delphi(PortStatus.Ported)] public bool IsDerived() => false;
    [Delphi(PortStatus.Ported)] public bool IsObject() => false;
    [Delphi(PortStatus.Ported)] public IBoldMOFObject AsObject() => throw new EBoldInternal($"{GetType().Name}.AsObject: Not an object");
    [Delphi(PortStatus.Ported)] public string AsString() => fValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
    [Delphi(PortStatus.Ported)] public bool AsBoolean() => throw new EBoldInternal($"{GetType().Name}.AsBoolean: Is not boolean");
    [Delphi(PortStatus.Ported)] public string QualifiedName() => "Foundation.Data_Types.MultiplicityRange." + fAttrName;
}

/// <summary>An expression as an object: its language (empty) and body.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFExpressionAdapter : IBoldMOFObject, IBoldMOFReferenceList, IBoldMOFAttributeList, IBoldMOFAttribute
{
    private readonly TBoldUMLXMILink fOwningLink;
    private readonly string fExprAttr;
    private readonly string fExpressionClass;

    [Delphi(PortStatus.Ported, Note = "Takes the body and the expression class's name where Delphi took the attribute.")]
    public TBoldUMLMOFExpressionAdapter(string Expr, string ExpressionClass, TBoldUMLXMILink OwningLink)
    {
        fOwningLink = OwningLink;
        fExprAttr = Expr;
        fExpressionClass = ExpressionClass;
    }

    [Delphi(PortStatus.Ported)] public IBoldMOFAttributeList Attributes() => this;
    [Delphi(PortStatus.Ported)] public IBoldMOFReferenceList References() => this;
    [Delphi(PortStatus.Ported)] public string QualifiedClassName() => "Foundation.Data_Types." + fExpressionClass;
    [Delphi(PortStatus.Ported)] public string LocalId() => throw new EBoldInternal($"{GetType().Name}.LocalId: Expression can only be embedded");
    [Delphi(PortStatus.Ported)] public int AttributeCount() => 2;
    int IBoldMOFAttributeList.Count() => AttributeCount();

    [Delphi(PortStatus.Ported)]
    public IBoldMOFAttribute Attribute(int index) => index switch
    {
        0 => new TBoldUMLMOFDummyAttrAdapter("Foundation.Data_Types.Expression.language"),
        1 => this,
        _ => throw new EBoldInternal($"{GetType().Name}.Attribute: index out of bounds"),
    };

    [Delphi(PortStatus.Ported)] public int Count() => 0;
    [Delphi(PortStatus.Ported)] public IBoldMOFReference Reference(int index) => throw new EBoldInternal($"{GetType().Name}.Reference: index out of bounds");
    [Delphi(PortStatus.Ported)] public bool IsBoolean() => false;
    [Delphi(PortStatus.Ported)] public bool IsEnum() => false;
    [Delphi(PortStatus.Ported)] public bool IsDerived() => false;
    [Delphi(PortStatus.Ported)] public bool IsObject() => false;
    [Delphi(PortStatus.Ported)] public IBoldMOFObject AsObject() => throw new EBoldInternal($"{GetType().Name}.AsObject: Expression.body is not an object");
    [Delphi(PortStatus.Ported)] public string AsString() => fExprAttr;
    [Delphi(PortStatus.Ported)] public bool AsBoolean() => throw new EBoldInternal($"{GetType().Name}.AsBoolean: Expression.body is not a boolean");
    [Delphi(PortStatus.Ported)] public string QualifiedName() => "Foundation.Data_Types.Expression.body";
}

/// <summary>An attribute with no value.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLMOFDummyAttrAdapter : IBoldMOFAttribute
{
    private readonly string fQualifiedName;

    [Delphi(PortStatus.Ported)]
    public TBoldUMLMOFDummyAttrAdapter(string QualifiedName)
    {
        fQualifiedName = QualifiedName;
    }

    [Delphi(PortStatus.Ported)] public bool IsBoolean() => false;
    [Delphi(PortStatus.Ported)] public bool IsEnum() => false;
    [Delphi(PortStatus.Ported)] public bool IsDerived() => false;
    [Delphi(PortStatus.Ported)] public bool IsObject() => false;
    [Delphi(PortStatus.Ported)] public IBoldMOFObject AsObject() => throw new EBoldInternal($"{GetType().Name}.AsObject: {fQualifiedName} is not an object");
    [Delphi(PortStatus.Ported)] public string AsString() => "";
    [Delphi(PortStatus.Ported)] public bool AsBoolean() => throw new EBoldInternal($"{GetType().Name}.AsBoolean: {fQualifiedName} is not a boolean");
    [Delphi(PortStatus.Ported)] public string QualifiedName() => fQualifiedName;
}

/// <summary>Routines of BoldUMLModelMOFAdapters.</summary>
public static class BoldUMLModelMOFAdapters
{
    [Delphi(PortStatus.Ported, Note = "Of an attribute's name: a multiplicity is the attribute so named.")]
    private static bool IsMultiplicity(string Attr) => Attr == "multiplicity";

    [Delphi(PortStatus.Ported, Note = "Of an attribute's name, by the table of attribute types.")]
    private static bool IsExpression(string Attr) =>
        TBoldUMLMOFMetaModel.AttributeTypes.TryGetValue(Attr, out var type) && IsNameOfExpressionClass(type.Kind);

    [Delphi(PortStatus.Dropped, Note = "Link objects (object diagrams) are not in the graph; it keeps the class models Bold uses.")]
    private static bool IsLinkObjectObject(TBoldUMLObject Obj) => false;

    [Delphi(PortStatus.Dropped, Note = "Link objects (object diagrams) are not in the graph; it keeps the class models Bold uses.")]
    private static bool IsLinkObjectLink(TBoldUMLObject Obj) => false;

    /// <summary>The class of an association class.</summary>
    [Delphi(PortStatus.Ported)]
    internal static bool IsAssociationClassClass(TBoldUMLObject Obj) => Obj.MetaClass == "Class" && Obj.associationOfClass is not null;

    /// <summary>The association of an association class.</summary>
    [Delphi(PortStatus.Ported)]
    internal static bool IsAssociationClassAssociation(TBoldUMLObject Obj) => Obj.MetaClass == "Association" && Obj.class_ is not null;

    [Delphi(PortStatus.Ported)]
    private static bool IsAssociationButNotAssociationClass(TBoldUMLObject Obj) => Obj.MetaClass == "Association" && Obj.class_ is null;

    /// <summary>Whether the element is of a class Delphi's UML model added (stereotype Bold): the graph has none.</summary>
    [Delphi(PortStatus.Ported, Note = "The graph keeps only the UML metamodel's classes.")]
    public static bool IsOfBoldAddedClass(TBoldUMLObject Obj) => false;

    /// <summary>The other half of an association class.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldUMLObject? GetOtherMIObj(TBoldUMLObject Obj) =>
        IsAssociationClassClass(Obj) ? Obj.associationOfClass : IsAssociationClassAssociation(Obj) ? Obj.class_ : null;

    /// <summary>Whether both halves of an association class have the member: a class both their classes are.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the element and the member's name; the defining class comes from the DTD.")]
    public static bool IsDiamondInherited(TBoldUMLObject Obj, string QualifiedClassName, string Member)
    {
        TBoldUMLObject? other = GetOtherMIObj(Obj);
        if (other is null)
        {
            return false;
        }

        string definingClass = TBoldUMLMOFMetaModel.DefiningClass(QualifiedClassName, Member);
        return Obj.IsKindOf(definingClass) && other.IsKindOf(definingClass);
    }

    /// <summary>The objects of a multi reference; merged from both halves of an association class when both have it.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the element and the reference's name.")]
    internal static IBoldMOFObjectList CreateObjectListAdapter(TBoldUMLObject Obj, string Ref, string QualifiedClassName, TBoldUMLXMILink OwningLink)
    {
        if (IsDiamondInherited(Obj, QualifiedClassName, Ref))
        {
            TBoldUMLObject obj2 = GetOtherMIObj(Obj)!;
            return Ref == "taggedValue"
                ? new TBoldUMLMOFMultipleInheritenceTVListAdapter(Obj, obj2.TaggedValues, OwningLink)
                : new TBoldUMLMOFMergedObjectListAdapter(Obj.Refs(Ref), obj2.Refs(Ref), OwningLink);
        }

        return new TBoldUMLMOFObjectListAdapter(Obj.Refs(Ref), OwningLink);
    }

    /// <summary>A namespace member of a plain association or of a stereotype is not written.</summary>
    [Delphi(PortStatus.Ported, Note = "Takes the element, its qualified class name and the member's name.")]
    internal static bool IsInheritedButShouldNotBe(TBoldUMLObject Obj, string QualifiedClassName, string Member) =>
        (IsAssociationButNotAssociationClass(Obj) || Obj.MetaClass == "Stereotype") &&
        TBoldUMLMOFMetaModel.DefiningClass(QualifiedClassName, Member) == "Namespace";
}
