// Delphi: Source/UMLModel/Core/BoldUMLModelSupport.pas
// Port map: CSharp/port/units/BoldUMLModelSupport.md
// Delphi worked on its UML object space; the port has none (D6), so these work on the UML graph that import and
// export use (TBoldUMLModelGraph), with the same steps. Delphi's OCL queries over the object space are plain loops.

using static Bold.BoldDefaultTaggedValues;
using static Bold.UMLConsts;

namespace Bold;

/// <summary>How much of an object BoldCopy copies.</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldCopyMode { bcmAttributes, bcmShallow, bcmDeep }

[Delphi(PortStatus.Ported)]
public delegate void TUMLModelElementManipulator(TBoldUMLObject UMLElement);

/// <summary>Copies and clones elements of a UML model.</summary>
[Delphi(PortStatus.Ported)]
public static class TBoldCopyAndClone
{
    /// <summary>A copy of the element of the same class (none for no element).</summary>
    [Delphi(PortStatus.Ported, Note = "Of a UML graph element; Delphi made an object of the same class in the same system and left out link objects, which the graph does not have.")]
    public static TBoldUMLObject? BoldClone(TBoldUMLObject? SourceObject, TBoldCopyMode Mode, bool StripToolId)
    {
        if (SourceObject is null)
        {
            return null;
        }

        TBoldUMLObject result = SourceObject.Graph.Create(SourceObject.MetaClass);
        BoldCopy(result, SourceObject, Mode, StripToolId);
        return result;
    }

    /// <summary>
    /// Copies the attributes; shallow also copies the references that do not own their objects and whose other end
    /// holds many (so that the source keeps them); deep also clones the objects the source owns.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "Of UML graph elements: a reference is composite when the metamodel says so, and its other end holds many when the metamodel has no single inverse for it.")]
    public static void BoldCopy(TBoldUMLObject DestinationObject, TBoldUMLObject SourceObject, TBoldCopyMode Mode, bool StripToolId)
    {
        foreach ((string attribute, string value) in SourceObject.AttributeValues)
        {
            DestinationObject[attribute] = value;
        }

        if (Mode == TBoldCopyMode.bcmAttributes)
        {
            return;
        }

        foreach (string reference in SourceObject.ReferenceNames.ToList())
        {
            bool composite = TBoldUMLMetaModel.CompositeReferences.Contains(reference) && TBoldUMLMetaModel.InverseOf(reference) is not null;
            string? inverse = TBoldUMLMetaModel.InverseOf(reference);
            bool otherEndMulti = inverse is null || !TBoldUMLMetaModel.IsSingle(inverse);
            foreach (TBoldUMLObject target in SourceObject.Refs(reference))
            {
                if (composite)
                {
                    if (Mode == TBoldCopyMode.bcmDeep &&
                        !(target.MetaClass == "TaggedValue" && target["tag"] == BOLDINTERALTVPREFIX + TBoldUMLSupport.TAG_TOOLID))
                    {
                        DestinationObject.AddRef(reference, BoldClone(target, TBoldCopyMode.bcmDeep, StripToolId)!);
                    }
                }
                else if (otherEndMulti)
                {
                    DestinationObject.AddRef(reference, target);
                }
            }
        }
    }
}

/// <summary>Helpers for UML models: tagged values, unique names, tool ids and flattening of packages.</summary>
[Delphi(PortStatus.Ported)]
public static class TBoldUMLSupport
{
    [Delphi(PortStatus.Ported, Symbol = "TAG_TOOLID")] public const string TAG_TOOLID = "toolId";
    [Delphi(PortStatus.Ported, Symbol = "TAG_FLATTENED")] public const string TAG_FLATTENED = "flattened";

    [Delphi(PortStatus.Ported)]
    private static bool NameInListExceptElement(string Name, IEnumerable<TBoldUMLObject> List, TBoldUMLObject UMLElement) =>
        List.Any(e => e.name == Name && e != UMLElement);

    /// <summary>Subscribes to every change of the element.</summary>
    [Delphi(PortStatus.Ported)]
    private static void SubscribeToAllMembers(TBoldUMLObject UMLElement, TBoldSubscriber Subscriber) =>
        UMLElement.AddSmallSubscription(Subscriber, TBoldSmallEventSet.Of(BoldSubscription.beMemberChanged), BoldSubscription.breReEvaluate);

    /// <summary>The package of the dotted name below a package, made where missing.</summary>
    [Delphi(PortStatus.Ported)]
    private static TBoldUMLObject GetEnsuredPackage(TBoldUMLObject Package, string QualifiedPackageName)
    {
        string packageName;
        int dot = QualifiedPackageName.IndexOf('.');
        if (dot >= 0)
        {
            packageName = QualifiedPackageName[..dot];
            QualifiedPackageName = QualifiedPackageName[(dot + 1)..];
        }
        else
        {
            packageName = QualifiedPackageName;
            QualifiedPackageName = "";
        }

        TBoldUMLObject? temp = Package.ownedElement.LastOrDefault(e => e.IsKindOf("Package") && SysUtils.SameText(packageName, e.name));
        if (temp is null)
        {
            temp = Package.Graph.Create("Package");
            temp.name = packageName;
            temp.namespace_ = Package;
        }

        return QualifiedPackageName != "" ? GetEnsuredPackage(temp, QualifiedPackageName) : temp;
    }

    /// <summary>A class name of Delphi's UML model without its "UML" prefix.</summary>
    [Delphi(PortStatus.Ported)]
    public static string UMLModelNameToUMLName(string ModelName) => ModelName.Length > 3 ? ModelName[3..] : "";

    /// <summary>Gives the element every Bold and UML tagged value of its class that it lacks, with its default.</summary>
    [Delphi(PortStatus.Ported, Note = "A UML tag spelled with an upper case first letter counts as present, as FindTaggedValue finds it.")]
    public static void EnsureBoldTaggedValues(TBoldUMLObject Element)
    {
        TBoldTaggedValueList bold = BoldDefaultTaggedValues.BoldDefaultTaggedValueList().ListForClassName(Element.MetaClass);
        for (int i = 0; i < bold.Count; i++)
        {
            string fullTag = BoldDefs.BOLDTVPREFIX + bold.Definition(i).Tag;
            if (Element.taggedValue(fullTag) is null)
            {
                EnsuredTaggedValue(Element, fullTag)["value"] = bold.Definition(i).DefaultValue;
            }
        }

        TBoldTaggedValueList uml = BoldUMLTaggedValues.UMLTaggedValueList().ListForClassName(Element.MetaClass);
        for (int i = 0; i < uml.Count; i++)
        {
            string tag = uml.Definition(i).Tag;
            // the tag with its first letter upper case, as older models have it, counts (as FindTaggedValue finds it)
            if (Element.FindTaggedValue(tag) is null)
            {
                EnsuredTaggedValue(Element, tag)["value"] = uml.Definition(i).DefaultValue;
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public static void EnsureBoldTaggedValuesInModel(TBoldUMLObject Model) => ManipulateAllOwnedElements(Model, EnsureBoldTaggedValues);

    /// <summary>Runs the manipulator on the model and on every element of it.</summary>
    [Delphi(PortStatus.Ported)]
    public static void ManipulateAllOwnedElements(TBoldUMLObject Model, TUMLModelElementManipulator manipulator)
    {
        manipulator(Model);
        foreach (TBoldUMLObject element in AllModelParts(Model).Where(e => e.MetaClass != "TaggedValue"))
        {
            manipulator(element);
        }
    }

    [Delphi(PortStatus.Ported)]
    public static TBoldUMLObject EnsuredTaggedValue(TBoldUMLObject Element, string Tag) => Element.EnsuredTaggedValue(Tag);

    [Delphi(PortStatus.Ported)]
    public static void RemoveTaggedValue(TBoldUMLObject Element, string Tag) => Element.DeleteTaggedValue(Tag);

    /// <summary>Makes each child of the element a child of the element's parents instead.</summary>
    [Delphi(PortStatus.Ported)]
    public static void RelinkSpecializations(TBoldUMLObject GeneralizableElement)
    {
        foreach (TBoldUMLObject specialization in Enumerable.Reverse(GeneralizableElement.specialization))
        {
            TBoldUMLObject? child = specialization.Ref("child");
            specialization.Delete();
            foreach (TBoldUMLObject generalization in GeneralizableElement.generalization)
            {
                TBoldUMLObject newGeneralization = GeneralizableElement.Graph.Create("Generalization");
                newGeneralization.namespace_ = child;
                newGeneralization.SetRef("parent", generalization.Ref("parent"));
                newGeneralization.SetRef("child", child);
            }
        }
    }

    /// <summary>The suggested name, or the first of name_1, name_2... no sibling of the element has.</summary>
    [Delphi(PortStatus.Ported)]
    public static string UniqueName(TBoldUMLObject Element, string SuggestedName)
    {
        int i = 0;
        string result;
        bool conflictFound;
        do
        {
            result = i == 0 ? SuggestedName : $"{SuggestedName}_{i}";
            conflictFound = Element.namespace_ is { } ns && NameInListExceptElement(result, ns.ownedElement, Element);
            if (!conflictFound && Element.IsKindOf("Feature") && Element.owner is { } owner)
            {
                conflictFound = NameInListExceptElement(result, owner.feature, Element);
            }

            if (!conflictFound && Element.IsKindOf("Attribute") && Element.Ref("associationEnd") is { } end)
            {
                conflictFound = NameInListExceptElement(result, end.qualifier, Element);
            }

            if (!conflictFound && Element.IsKindOf("Parameter") && Element.Ref("behavioralFeature") is { } operation)
            {
                conflictFound = NameInListExceptElement(result, operation.parameter, Element);
            }

            i++;
        }
        while (conflictFound);

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public static void AddQualifier(TBoldUMLObject AssociationEnd, TBoldUMLObject Qualifier, string SuggestedName)
    {
        Qualifier.SetRef("associationEnd", AssociationEnd);
        Qualifier.name = UniqueName(Qualifier, SuggestedName);
    }

    [Delphi(PortStatus.Ported)]
    public static void AddToNamespace(TBoldUMLObject NameSpace, TBoldUMLObject Element, string SuggestedName)
    {
        Element.namespace_ = NameSpace;
        Element.name = UniqueName(Element, SuggestedName);
    }

    /// <summary>Records the id the element has in the tool the model came from.</summary>
    [Delphi(PortStatus.Ported)]
    public static void AddToolId(TBoldUMLObject Element, string ToolId) => EnsuredTaggedValue(Element, BOLDINTERALTVPREFIX + TAG_TOOLID)["value"] = ToolId;

    [Delphi(PortStatus.Ported)]
    public static string GetToolId(TBoldUMLObject Element) => Element.taggedValue(BOLDINTERALTVPREFIX + TAG_TOOLID)?["value"] ?? "";

    /// <summary>Removes the tool ids of the model's elements.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi selected the tagged values by name, which tagged values do not have, so it removed none; this removes them by tag.")]
    public static void StripToolId(TBoldUMLObject Model)
    {
        foreach (TBoldUMLObject tv in AllModelParts(Model).Where(e => e.MetaClass == "TaggedValue" && e["tag"] == BOLDINTERALTVPREFIX + TAG_TOOLID).ToList())
        {
            tv.Delete();
        }
    }

    [Delphi(PortStatus.Ported)]
    public static TBoldUMLObject? ElementForToolId(TBoldUMLObject UMLModel, string ToolId) =>
        UMLModel.Graph.AllInstances("TaggedValue")
            .Where(t => t["tag"] == BOLDINTERALTVPREFIX + TAG_TOOLID && t["value"] == ToolId)
            .Select(t => t.Ref("modelElement"))
            .FirstOrDefault(e => e is not null && e.model == UMLModel);

    [Delphi(PortStatus.Ported)]
    public static bool IsFlattened(TBoldUMLObject UMLModel) =>
        UMLModel.taggedValue(BOLDINTERALTVPREFIX + TAG_FLATTENED) is { } tv && TVIsTrue(tv["value"]);

    /// <summary>Moves every class and association to the model itself, noting the package each was in.</summary>
    [Delphi(PortStatus.Ported)]
    public static void Flatten(TBoldUMLObject UMLModel)
    {
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        log.StartLog(sFlatteningModel);
        try
        {
            List<TBoldUMLObject> list = [.. AllNamespaceContents(UMLModel).Where(e => e.IsKindOf("Class") || e.IsKindOf("Association"))];
            log.ProgressMax = list.Count;
            foreach (TBoldUMLObject element in list)
            {
                if (element.namespace_ != UMLModel)
                {
                    EnsuredTaggedValue(element, BOLDINTERALTVPREFIX + TAG_UNFLATTENEDNAMESPACE)["value"] = element.namespace_!.qualifiedName;
                    element.namespace_ = UMLModel;
                }

                log.ProgressStep();
            }
        }
        finally
        {
            log.EndLog();
        }

        EnsuredTaggedValue(UMLModel, BOLDINTERALTVPREFIX + TAG_FLATTENED)["value"] = TV_TRUE;
    }

    /// <summary>Puts each element Flatten moved back into its package, making the packages.</summary>
    [Delphi(PortStatus.Ported)]
    public static void UnFlatten(TBoldUMLObject UMLModel)
    {
        string tag = BOLDINTERALTVPREFIX + TAG_UNFLATTENEDNAMESPACE;
        TBoldUMLObject currentNameSpace = UMLModel;
        // ordering by name gives a topological sort
        List<TBoldUMLObject> list = [.. UMLModel.ownedElement.Where(e => e.taggedValue(tag) is not null).OrderBy(e => e.taggedValue(tag)!["value"], StringComparer.Ordinal)];
        foreach (TBoldUMLObject element in list)
        {
            string unflattenedNamespaceName = element.taggedValue(tag)!["value"];
            if (unflattenedNamespaceName != currentNameSpace.qualifiedName)
            {
                // the first part is the name of the model
                int dot = unflattenedNamespaceName.IndexOf('.');
                currentNameSpace = GetEnsuredPackage(UMLModel, dot >= 0 ? unflattenedNamespaceName[(dot + 1)..] : unflattenedNamespaceName);
            }

            element.namespace_ = currentNameSpace;
            RemoveTaggedValue(element, tag);
        }

        RemoveTaggedValue(UMLModel, BOLDINTERALTVPREFIX + TAG_FLATTENED);
    }

    /// <summary>Subscribes to every change of every element of the model.</summary>
    [Delphi(PortStatus.Ported)]
    public static void SubscribeToEntireModel(TBoldUMLObject UMLModel, TBoldSubscriber Subscriber)
    {
        SubscribeToAllMembers(UMLModel, Subscriber);
        foreach (TBoldUMLObject part in AllModelParts(UMLModel))
        {
            SubscribeToAllMembers(part, Subscriber);
        }
    }

    /// <summary>Every element of the model.</summary>
    [Delphi(PortStatus.Ported)]
    public static List<TBoldUMLObject> AllModelParts(TBoldUMLObject UMLModel) => [.. UMLModel.AllOwnedElements];

    /// <summary>The namespace's owned elements, theirs, and so on.</summary>
    public static IEnumerable<TBoldUMLObject> AllNamespaceContents(TBoldUMLObject Namespace)
    {
        foreach (TBoldUMLObject element in Namespace.ownedElement)
        {
            yield return element;
            foreach (TBoldUMLObject inner in AllNamespaceContents(element))
            {
                yield return inner;
            }
        }
    }
}

/// <summary>
/// Makes a UML model fit for Bold, and undoes it: a root class, default multiplicities, names for unnamed ends and
/// associations, link classes for many-to-many associations, and a superclass for every class. Each change is noted
/// in a tagged value so that UnBoldify can take it back.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLBoldify
{
    [Delphi(PortStatus.Ported)]
    private static void SetBoldifyTaggedValue(TBoldUMLObject element, string Tag, string Value) =>
        TBoldUMLSupport.EnsuredTaggedValue(element, FullTag(Tag))["value"] = Value;

    [Delphi(PortStatus.Ported)]
    private static string FullTag(string Tag) => BOLDBOLDIFYPREFIX + Tag;

    [Delphi(PortStatus.Ported)]
    public TBoldUMLBoldify()
    {
        PluralSuffix = "";
        DefaultNonNavigableMultiplicity = "0..*";
        DefaultNavigableMultiplicity = "0..1";
        UnembedMulti = true;
        MakeDerivedTransient = true;
    }

    private static TBoldUMLObject? ClassNamed(TBoldUMLObject Model, string Name) =>
        Model.Graph.AllInstances("Class").FirstOrDefault(c => c.name == Name && c.model == Model);

    [Delphi(PortStatus.Ported)]
    public void Boldify(TBoldUMLObject UMLModel)
    {
        TBoldUMLSupport.EnsureBoldTaggedValuesInModel(UMLModel);
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        log.StartLog(sBoldifyingModel);
        try
        {
            SetBoldifyTaggedValue(UMLModel, TAG_BOLDIFIED, TV_TRUE);
            // Create the root class if it is missing
            string rootClassName = UMLModel.GetBoldTV(TAG_ROOTCLASS);
            bool rootClassImplicitlyNamed = rootClassName == "";
            if (rootClassImplicitlyNamed)
            {
                rootClassName = "BusinessClassesRoot";
            }

            TBoldUMLObject? rootClass = ClassNamed(UMLModel, rootClassName);
            if (rootClass is null)
            {
                rootClass = UMLModel.Graph.Create("Class");
                TBoldUMLSupport.EnsureBoldTaggedValues(rootClass);
                SetBoldifyTaggedValue(rootClass, TAG_AUTOCREATED, TV_TRUE);
                rootClass.name = rootClassName;
                rootClass.namespace_ = UMLModel;
                rootClass.SetTaggedValue(BoldDefs.BOLDTVPREFIX + TAG_INCFILENAME, "");
                rootClass.SetTaggedValue(BoldDefs.BOLDTVPREFIX + TAG_TABLENAME, BoldDefs.OBJECTTABLE_NAME);
                rootClass.persistent = true;
                if (rootClassImplicitlyNamed)
                {
                    SetBoldifyTaggedValue(rootClass, TAG_NONAME, TV_TRUE);
                }
            }

            if (rootClass.Superclass is not null)
            {
                throw new EBold(sCannotBoldifyIfRootClassHasSuperClass);
            }

            TBoldUMLObject defaultSuperClass = GetDefaultSuperClass(UMLModel) ?? rootClass;
            TBoldUMLObject defaultLinkSuperClass = GetDefaultLinkSuperClass(UMLModel) ?? rootClass;

            // Fix up associations: default multiplicities, names of unnamed ends, missing link classes
            List<TBoldUMLObject> allAssociations = [.. UMLModel.Graph.AllInstances("Association").Where(a => a.model == UMLModel)];
            log.ProgressMax = allAssociations.Count;
            foreach (TBoldUMLObject association in allAssociations)
            {
                if (association.persistent && association.derived && MakeDerivedTransient)
                {
                    association.persistent = false;
                    SetBoldifyTaggedValue(association, TAG_WASPERSISTENT, TV_TRUE);
                }

                string unnamedAssociationName = "";
                List<TBoldUMLObject> connection = association.connection;
                for (int connectionIndex = 0; connectionIndex < connection.Count; connectionIndex++)
                {
                    TBoldUMLObject assocEnd = connection[connectionIndex];
                    if (assocEnd.multiplicity == "")
                    {
                        assocEnd.multiplicity = assocEnd.isNavigable ? DefaultNavigableMultiplicity : DefaultNonNavigableMultiplicity;
                        SetBoldifyTaggedValue(assocEnd, TAG_DEFAULTMULTIPLICITY, TV_TRUE);
                    }

                    if (assocEnd.multi && UnembedMulti && TVIsTrue(assocEnd.GetBoldTV(TAG_EMBED)))
                    {
                        assocEnd.SetBoldTV(TAG_EMBED, TV_FALSE);
                        SetBoldifyTaggedValue(assocEnd, TAG_WASEMBEDED, TV_TRUE);
                    }

                    // Name unnamed association ends
                    if (assocEnd.isNavigable)
                    {
                        if (assocEnd.name == "")
                        {
                            if (assocEnd.type_ is { } type)
                            {
                                assocEnd.name = assocEnd.multi ? type.name + PluralSuffix : type.name;
                                unnamedAssociationName += type.name;
                            }
                            else
                            {
                                assocEnd.name = $"Role{connectionIndex}";
                            }

                            SetBoldifyTaggedValue(assocEnd, TAG_NONAME, TV_TRUE);
                        }
                        else
                        {
                            unnamedAssociationName += assocEnd.name;
                        }
                    }
                }

                // implicit names of unnamed non-navigable ends
                foreach (TBoldUMLObject assocEnd in connection)
                {
                    if (!assocEnd.isNavigable)
                    {
                        if (assocEnd.name == "")
                        {
                            assocEnd.name = "x_" + assocEnd.otherEnd?.name + "_" + assocEnd.type_?.name;
                            SetBoldifyTaggedValue(assocEnd, TAG_NONAME, TV_TRUE);
                            unnamedAssociationName += "_" + assocEnd.otherEnd?.type_?.name;
                        }
                        else
                        {
                            unnamedAssociationName += assocEnd.name;
                        }
                    }
                }

                // transient if either end is transient
                if (association.persistent)
                {
                    foreach (TBoldUMLObject assocEnd in connection)
                    {
                        if (assocEnd.type_ is { } type && !type.persistent)
                        {
                            association.persistent = false;
                            SetBoldifyTaggedValue(association, TAG_WASPERSISTENT, TV_TRUE);
                        }
                    }
                }

                // transient if the association class is transient
                if (association.persistent && association.class_ is { } associationClass && !associationClass.persistent)
                {
                    association.persistent = false;
                    SetBoldifyTaggedValue(association, TAG_WASPERSISTENT, TV_TRUE);
                }

                if (association.name == "")
                {
                    association.name = unnamedAssociationName;
                    SetBoldifyTaggedValue(association, TAG_NONAME, TV_TRUE);
                }

                // Create a link class if needed
                if (!association.derived && connection.Count == 2 &&
                    ((connection[0].multi && connection[1].multi) ||
                     (TVIsFalse(connection[0].GetBoldTV(TAG_EMBED)) && TVIsFalse(connection[1].GetBoldTV(TAG_EMBED)))) &&
                    association.class_ is null)
                {
                    // Different names on association and link class is not strictly UML.
                    string linkClassName = BoldNameExpander.BoldExpandName(association.GetBoldTV(TAG_LINKCLASSNAME), association.name, TExpansionType.xtDelphi, -1,
                        TBoldTaggedValueSupport.StringToNationalCharConversion(UMLModel.GetBoldTV(TAG_NATIONALCHARCONVERSION)));
                    if (linkClassName == "")
                    {
                        linkClassName = association.name;
                    }

                    TBoldUMLObject linkClass = UMLModel.Graph.Create("Class");
                    linkClass.namespace_ = association.namespace_;
                    association.class_ = linkClass;
                    linkClass.persistent = association.persistent;
                    TBoldUMLSupport.EnsureBoldTaggedValues(linkClass);
                    if (TVIsTrue(connection[0].type_?.GetBoldTV(TAG_VERSIONED) ?? "") || TVIsTrue(connection[1].type_?.GetBoldTV(TAG_VERSIONED) ?? ""))
                    {
                        linkClass.SetBoldTV(TAG_VERSIONED, TV_TRUE);
                    }

                    SetBoldifyTaggedValue(linkClass, TAG_AUTOCREATED, TV_TRUE);
                    // the same name as the association is allowed; another name is made unique
                    linkClass.name = linkClassName == association.name ? association.name : TBoldUMLSupport.UniqueName(linkClass, linkClassName);
                }

                log.ProgressStep();
            }

            List<TBoldUMLObject> allClasses = UMLModel.Classes;
            log.ProgressMax = allClasses.Count;
            foreach (TBoldUMLObject cls in allClasses)
            {
                foreach (TBoldUMLObject feature in cls.feature)
                {
                    if (feature.IsKindOf("Attribute") && feature.persistent && feature.derived && MakeDerivedTransient)
                    {
                        feature.persistent = false;
                        SetBoldifyTaggedValue(feature, TAG_WASPERSISTENT, TV_TRUE);
                    }
                }

                // Fix up inheritance
                if (cls.Superclass is null && cls != rootClass)
                {
                    if (cls.isAssociationClass)
                    {
                        cls.SetFirstParent(defaultLinkSuperClass);
                    }
                    else if (cls != defaultSuperClass)
                    {
                        cls.SetFirstParent(defaultSuperClass);
                    }
                    else
                    {
                        cls.SetFirstParent(rootClass);
                    }

                    SetBoldifyTaggedValue(cls.generalization[0], TAG_AUTOCREATED, TV_TRUE);
                }

                log.ProgressStep();
            }
        }
        finally
        {
            log.EndLog();
        }
    }

    /// <summary>Takes back what Boldify did.</summary>
    [Delphi(PortStatus.Ported)]
    public static void UnBoldify(TBoldUMLObject model)
    {
        List<TBoldUMLObject> GetNewList(string ClassName, string TaggedValue = "", string Value = "") =>
            [.. model.Graph.AllInstances(ClassName).Where(e => e.model == model &&
                (TaggedValue == "" || (e.taggedValue(TaggedValue)?["value"] ?? "") == Value))];

        SetRootClassname(model, GetRootClass(model)!.name);
        // Remove the elements Boldify made, unless they were changed since
        List<TBoldUMLObject> toRemove = GetNewList("ModelElement", BOLDBOLDIFYPREFIX + TAG_AUTOCREATED, TV_TRUE);
        for (int i = toRemove.Count - 1; i >= 0; i--)
        {
            if (toRemove[i].IsKindOf("Class"))
            {
                TBoldUMLObject tempClass = toRemove[i];
                if (tempClass.feature.Count > 0 || model.Graph.AllInstances("AssociationEnd").Any(e => e.type_ == tempClass))
                {
                    tempClass.SetTaggedValue(BOLDBOLDIFYPREFIX + TAG_AUTOCREATED, TV_FALSE);
                    toRemove.RemoveAt(i);
                }
            }
        }

        for (int i = toRemove.Count - 1; i >= 0; i--)
        {
            toRemove[i].Delete();
        }

        foreach (TBoldUMLObject element in GetNewList("ModelElement", BOLDBOLDIFYPREFIX + TAG_NONAME, TV_TRUE))
        {
            element.name = "";
            RemoveBoldifyTaggedValue(element, TAG_NONAME);
        }

        foreach (TBoldUMLObject association in GetNewList("Association", BOLDBOLDIFYPREFIX + TAG_WASPERSISTENT, TV_TRUE))
        {
            association.persistent = true;
            RemoveBoldifyTaggedValue(association, TAG_WASPERSISTENT);
        }

        foreach (TBoldUMLObject attribute in GetNewList("Attribute", BOLDBOLDIFYPREFIX + TAG_WASPERSISTENT, TV_TRUE))
        {
            attribute.persistent = true;
            RemoveBoldifyTaggedValue(attribute, TAG_WASPERSISTENT);
        }

        foreach (TBoldUMLObject associationEnd in GetNewList("AssociationEnd"))
        {
            if (TVIsTrue(GetBoldifyTaggedValue(associationEnd, TAG_DEFAULTMULTIPLICITY)))
            {
                associationEnd.multiplicity = "";
                RemoveBoldifyTaggedValue(associationEnd, TAG_DEFAULTMULTIPLICITY);
            }

            if (TVIsTrue(GetBoldifyTaggedValue(associationEnd, TAG_WASEMBEDED)))
            {
                associationEnd.SetBoldTV(TAG_EMBED, TV_TRUE);
                RemoveBoldifyTaggedValue(associationEnd, TAG_WASEMBEDED);
            }
        }

        RemoveBoldifyTaggedValue(model, TAG_BOLDIFIED);
    }

    [Delphi(PortStatus.Ported)]
    public void Assign(object? source)
    {
        if (source is TBoldUMLBoldify other)
        {
            PluralSuffix = other.PluralSuffix;
            EnforceDefaultUMLCase = other.EnforceDefaultUMLCase;
            DefaultNonNavigableMultiplicity = other.DefaultNonNavigableMultiplicity;
            DefaultNavigableMultiplicity = other.DefaultNavigableMultiplicity;
        }
        else
        {
            throw new EBold("TBoldUMLBoldify.Assign");
        }
    }

    [Delphi(PortStatus.Ported)]
    public static bool IsBoldified(TBoldUMLObject model) => TVIsTrue(GetBoldifyTaggedValue(model, TAG_BOLDIFIED));

    [Delphi(PortStatus.Ported)]
    public static bool IsAutoCreated(TBoldUMLObject Element) => TVIsTrue(GetBoldifyTaggedValue(Element, TAG_AUTOCREATED));

    [Delphi(PortStatus.Ported)]
    public static void RemoveBoldifyTaggedValue(TBoldUMLObject element, string Tag) => element.taggedValue(FullTag(Tag))?.Delete();

    [Delphi(PortStatus.Ported)]
    public static string GetBoldifyTaggedValue(TBoldUMLObject element, string Tag) => element.taggedValue(FullTag(Tag))?["value"] ?? "";

    /// <summary>The class of a boldified model that has no superclass.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldUMLObject? GetRootClass(TBoldUMLObject Model)
    {
        if (!IsBoldified(Model))
        {
            throw new EBold(sCanOnlyBeCalledIfBoldified);
        }

        return Model.Graph.AllInstances("Class").FirstOrDefault(c => c.model == Model && c.generalization.Count == 0);
    }

    [Delphi(PortStatus.Ported)]
    public static void SetRootClassname(TBoldUMLObject Model, string name) => Model.SetBoldTV(TAG_ROOTCLASS, name);

    [Delphi(PortStatus.Ported)]
    public static TBoldUMLObject? GetDefaultSuperClass(TBoldUMLObject Model) => ClassNamed(Model, Model.GetBoldTV(TAG_DEFAULTSUPERCLASS));

    [Delphi(PortStatus.Ported)]
    public static TBoldUMLObject? GetDefaultLinkSuperClass(TBoldUMLObject Model) =>
        ClassNamed(Model, Model.GetBoldTV(TAG_DEFAULTLINKCLASSSUPERCLASS)) ?? GetDefaultSuperClass(Model);

    [Delphi(PortStatus.Ported)] public string PluralSuffix { get; set; }
    [Delphi(PortStatus.Ported)] public bool EnforceDefaultUMLCase { get; set; }
    [Delphi(PortStatus.Ported)] public string DefaultNavigableMultiplicity { get; set; }
    [Delphi(PortStatus.Ported)] public string DefaultNonNavigableMultiplicity { get; set; }
    [Delphi(PortStatus.Ported)] public bool UnembedMulti { get; set; }
    [Delphi(PortStatus.Ported)] public bool MakeDerivedTransient { get; set; }
}

/// <summary>Overriding operations in subclasses.</summary>
[Delphi(PortStatus.Ported)]
public static class TBoldUMLOperationSupport
{
    /// <summary>Gives the class a copy of the operation, marked override, unless it has one of that signature.</summary>
    [Delphi(PortStatus.Ported)]
    public static void OverrideInClass(TBoldUMLObject UMLClass, TBoldUMLObject UMLOperation)
    {
        var paramTypes = new TStringList();
        ParameterTypesToList(UMLOperation, paramTypes);
        if (!ClassHasOperation(UMLClass, UMLOperation.name, paramTypes))
        {
            TBoldUMLObject newMethod = TBoldCopyAndClone.BoldClone(UMLOperation, TBoldCopyMode.bcmDeep, true)!;
            newMethod.owner = UMLClass;
            newMethod.SetBoldTV(TAG_DELPHIOPERATIONKIND, TV_DELPHIOPERATIONKIND_OVERRIDE);
        }
    }

    [Delphi(PortStatus.Ported)]
    public static void OverrideInAllSubclasses(TBoldUMLObject UMLClass, TBoldUMLObject UMLOperation)
    {
        foreach (TBoldUMLObject subClass in UMLClass.specialization.Select(s => s.Ref("child")).OfType<TBoldUMLObject>())
        {
            OverrideInAllSubclasses(subClass, UMLOperation);
        }

        OverrideInClass(UMLClass, UMLOperation);
    }

    /// <summary>Whether the class has an operation of the name and parameter types (a return parameter aside).</summary>
    [Delphi(PortStatus.Ported)]
    public static bool ClassHasOperation(TBoldUMLObject UMLClass, string MethodName, TStringList ParamTypes)
    {
        bool result = false;
        foreach (TBoldUMLObject operation in UMLClass.feature.Where(f => f.IsKindOf("Operation")))
        {
            if (SysUtils.SameText(operation.name, MethodName))
            {
                List<TBoldUMLObject> parameters = operation.parameter;
                if (ParamTypes.Count == parameters.Count || ParamTypes.Count == parameters.Count - 1)
                {
                    result = true;
                    for (int i = 0; i < ParamTypes.Count; i++)
                    {
                        if (!string.Equals(ParamTypes[i], parameters[i].typeName, StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(parameters[i].typeName, "Return", StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public static void ParameterTypesToList(TBoldUMLObject UMLOperation, TStringList ParamTypes)
    {
        foreach (TBoldUMLObject parameter in UMLOperation.parameter)
        {
            ParamTypes.Add(parameter.typeName);
        }
    }
}
