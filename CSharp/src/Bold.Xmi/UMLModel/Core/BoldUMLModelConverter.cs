// Delphi: Source/UMLModel/Core/BoldUMLModelConverter.pas
// Port map: CSharp/port/units/BoldUMLModelConverter.md
// The Mold model is the model (D6); this converts between it and the UML graph that XMI import and export use, as
// Delphi converted between it and the UML object space.

using static Bold.BoldDefaultTaggedValues;
using static Bold.UMLConsts;

namespace Bold;

/// <summary>Converts a UML model held as a graph of its elements to a Mold model, and back.</summary>
[Delphi(PortStatus.Ported)]
public static class TBoldModelConverter
{
    private static TBoldLogHandler BoldLog => BoldLogHandler.BoldLog();

    /// <summary>The Mold model of a flattened, boldified UML model.</summary>
    [Delphi(PortStatus.Ported)]
    public static TMoldModel UMLModelToMold(TBoldUMLObject UMLModel)
    {
        var moldModel = new TMoldModel(null, UMLModel.name);
        UMLElementToMoldElement(UMLModel, moldModel);
        BoldLog.StartLog(sConvertingModelToMold);
        List<TBoldUMLObject> classes = UMLModel.Classes;
        List<TBoldUMLObject> associations = UMLModel.Associations;
        BoldLog.ProgressMax = classes.Count + associations.Count;
        if (UMLModelGetUniqueRootClass(UMLModel) is { } umlRootClass)
        {
            moldModel.RootClass.Name = umlRootClass.name;
        }

        foreach (TBoldUMLObject umlClass in classes)
        {
            UMLClassToMoldClass(umlClass, moldModel.GetClassByName(umlClass.name));
            BoldLog.ProgressStep();
        }

        foreach (TBoldUMLObject umlAssociation in associations)
        {
            List<TBoldUMLObject> connection = umlAssociation.connection;
            if (connection.Count == 2 && connection[0].type_?.MetaClass == "Class" && connection[1].type_?.MetaClass == "Class")
            {
                UMLAssociationToMoldAssociation(umlAssociation, new TMoldAssociation(moldModel, umlAssociation.name));
            }

            BoldLog.ProgressStep();
        }

        BoldLog.EndLog();
        return moldModel;
    }

    [Delphi(PortStatus.Ported)]
    private static void UMLElementToMoldElement(TBoldUMLObject UMLElement, TMoldElement MoldElement)
    {
        MoldElement.Stereotype = UMLElement.stereotypeName;
        foreach (TBoldUMLObject constraint in UMLElement.constraint)
        {
            MoldElement.Constraints.SetValues(constraint.name, constraint.body);
        }

        foreach (TBoldUMLObject taggedValue in UMLElement.TaggedValues)
        {
            MoldElement.SetTV(taggedValue["tag"], taggedValue["value"]);
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void UMLClassToMoldClass(TBoldUMLObject UMLClass, TMoldClass MoldClass)
    {
        UMLElementToMoldElement(UMLClass, MoldClass);
        MoldClass.IsAbstract = UMLClass.isAbstract;
        MoldClass.Persistent = UMLClass.persistent;
        foreach (TBoldUMLObject feature in UMLClass.feature)
        {
            if (feature.IsKindOf("Attribute"))
            {
                UMLAttributeToMoldAttribute(feature, new TMoldAttribute(MoldClass, feature.name));
            }
            else if (feature.IsKindOf("Operation"))
            {
                UMLOperationToMoldMethod(feature, new TMoldMethod(MoldClass, feature.name));
            }
        }

        if (UMLClass.Superclass is { } superclass)
        {
            MoldClass.SuperClass = MoldClass.Model!.GetClassByName(superclass.name);
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void UMLAssociationToMoldAssociation(TBoldUMLObject UMLAssociation, TMoldAssociation MoldAssociation)
    {
        UMLElementToMoldElement(UMLAssociation, MoldAssociation);
        MoldAssociation.Derived = UMLAssociation.derived;
        if (UMLAssociation.class_ is { } linkClass)
        {
            MoldAssociation.LinkClass = MoldAssociation.Model!.GetClassByName(linkClass.name);
        }

        List<TBoldUMLObject> connection = UMLAssociation.connection;
        for (int i = 0; i <= 1; i++)
        {
            UMLAssociationEndToMoldRole(connection[i], new TMoldRole(MoldAssociation, connection[i].name));
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void UMLAssociationEndToMoldRole(TBoldUMLObject UMLAssociationEnd, TMoldRole MoldRole)
    {
        UMLElementToMoldElement(UMLAssociationEnd, MoldRole);
        TBoldUMLObject? otherEndClass = UMLAssociationEnd.otherEnd?.type_;
        MoldRole.MoldClass = otherEndClass is not null ? MoldRole.Model!.GetClassByName(otherEndClass.name) : null;
        MoldRole.Multiplicity = UMLAssociationEnd.multiplicity;
        MoldRole.Navigable = UMLAssociationEnd.isNavigable;
        MoldRole.Ordered = UMLAssociationEnd.isOrdered;
        MoldRole.Aggregation = UMLAssociationEnd.aggregation;
        MoldRole.Visibility = UMLAssociationEnd.visibility;
        MoldRole.Changeability = UMLAssociationEnd.changeability;
        foreach (TBoldUMLObject qualifier in UMLAssociationEnd.qualifier)
        {
            UMLAttributeToMoldQualifier(qualifier, new TMoldQualifier(MoldRole, qualifier.name));
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void UMLAttributeToMoldQualifier(TBoldUMLObject UMLAttribute, TMoldQualifier MoldQualifier)
    {
        UMLElementToMoldElement(UMLAttribute, MoldQualifier);
        MoldQualifier.BoldType = UMLAttribute.typeName;
    }

    [Delphi(PortStatus.Ported)]
    private static void UMLAttributeToMoldAttribute(TBoldUMLObject UMLAttribute, TMoldAttribute MoldAttribute)
    {
        UMLElementToMoldElement(UMLAttribute, MoldAttribute);
        MoldAttribute.BoldType = UMLAttribute.typeName;
        MoldAttribute.Derived = UMLAttribute.derived;
        MoldAttribute.Visibility = UMLAttribute.visibility;
        MoldAttribute.InitialValue = UMLAttribute.initialValue;
    }

    [Delphi(PortStatus.Ported)]
    private static void UMLOperationToMoldMethod(TBoldUMLObject UMLOperation, TMoldMethod MoldMethod)
    {
        UMLElementToMoldElement(UMLOperation, MoldMethod);
        MoldMethod.IsClassMethod = UMLOperation.ownerScope == TScopeKind.skClassifier;
        MoldMethod.ReturnType = UMLOperation.parameter.FirstOrDefault(p => p.kind == TBoldParameterDirectionKind.pdReturn)?.typeName ?? "";
        UMLParametersToMoldParameters(UMLOperation.parameter, MoldMethod);
        MoldMethod.Visibility = UMLOperation.visibility;
    }

    [Delphi(PortStatus.Ported)]
    private static void UMLParametersToMoldParameters(List<TBoldUMLObject> UMLParameters, TMoldMethod MoldMethod)
    {
        foreach (TBoldUMLObject umlParameter in UMLParameters.Where(p => p.kind != TBoldParameterDirectionKind.pdReturn))
        {
            var moldParameter = new TMoldParameter(MoldMethod)
            {
                ParameterName = umlParameter.name,
                ParameterType = umlParameter.typeName,
                ParameterKind = umlParameter.kind,
                IsConst = BoldUtils.StringToBoolean(umlParameter.GetBoldTV(TAG_ISCONST)),
            };
            MoldMethod.Parameters.Add(moldParameter);
        }
    }

    /// <summary>The only class without a superclass; none when there are several.</summary>
    [Delphi(PortStatus.Ported)]
    private static TBoldUMLObject? UMLModelGetUniqueRootClass(TBoldUMLObject UMLModel)
    {
        List<TBoldUMLObject> roots = [.. UMLModel.Classes.Where(c => c.Superclass is null)];
        return roots.Count == 1 ? roots[0] : null;
    }

    /// <summary>Fills a UML model (cleared first) from a Mold model.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi changed the UML model in a transaction of its object space and left it unchanged on failure; the graph is new for each export.")]
    public static void MoldModelToUML(TMoldModel MoldModel, TBoldUMLObject TargetUMLModel)
    {
        foreach (TBoldUMLObject element in TargetUMLModel.AllOwnedElements.ToList())
        {
            element.Delete();
        }

        MoldModelToUMLModel(MoldModel, TargetUMLModel);
    }

    /// <summary>A new UML graph with the model of a Mold model.</summary>
    public static TBoldUMLObject MoldModelToUML(TMoldModel MoldModel)
    {
        TBoldUMLObject model = new TBoldUMLModelGraph().Create("Model");
        MoldModelToUMLModel(MoldModel, model);
        return model;
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldModelToUMLModel(TMoldModel MoldModel, TBoldUMLObject UMLModel)
    {
        MoldElementToUMLElement(MoldModel, UMLModel);
        BoldLog.StartLog(sConvertingModelToUML);
        BoldLog.ProgressMax = MoldModel.Classes.Count + MoldModel.Associations.Count;
        TBoldUMLBoldify.SetRootClassname(UMLModel, MoldModel.RootClass.Name);
        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            MoldClassToUMLClass(MoldModel.Classes[i], GetUMLClassByName(MoldModel.Classes[i].Name, UMLModel));
            BoldLog.ProgressStep();
        }

        for (int i = 0; i < MoldModel.Associations.Count; i++)
        {
            MoldAssociationToUMLAssociation(MoldModel.Associations[i], GetUMLAssociationByName(MoldModel.Associations[i].Name, UMLModel));
            BoldLog.ProgressStep();
        }

        // A constraint belongs to the nearest namespace of what it constrains, as TBoldUMLModelLinkSupport puts it.
        foreach (TBoldUMLObject constraint in UMLModel.Graph.AllInstances("Constraint").Where(c => c.namespace_ is null).ToList())
        {
            TBoldUMLObject? owner = constraint.Ref("constrainedElement");
            while (owner is not null && !owner.IsKindOf("Namespace"))
            {
                owner = owner.qualifyingOwner;
            }

            constraint.namespace_ = owner;
        }

        BoldLog.EndLog();
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldElementToUMLElement(TMoldElement MoldElement, TBoldUMLObject UMLElement)
    {
        UMLElement.name = MoldElement.Name;
        if (UMLElement.model is not null)
        {
            UMLElement.stereotypeName = MoldElement.Stereotype;
        }

        for (int i = 0; i < MoldElement.Constraints.Count; i++)
        {
            TBoldUMLObject constraint = UMLElement.Graph.Create("Constraint");
            constraint.name = MoldElement.Constraints.Names(i);
            constraint.body = MoldElement.Constraints.Values(constraint.name);
            UMLElement.AddRef("constraint", constraint);
        }

        var tags = new TStringList();
        var values = new TStringList();
        MoldElement.AddAllTaggedValues(tags, values);
        for (int i = 0; i < tags.Count; i++)
        {
            TBoldUMLObject taggedValue = UMLElement.Graph.Create("TaggedValue");
            taggedValue["tag"] = tags[i];
            taggedValue["value"] = values[i];
            UMLElement.AddRef("taggedValue", taggedValue);
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldClassToUMLClass(TMoldClass MoldClass, TBoldUMLObject UMLClass)
    {
        MoldElementToUMLElement(MoldClass, UMLClass);
        UMLClass.isAbstract = MoldClass.IsAbstract;
        UMLClass.persistent = MoldClass.Persistent;
        UMLClass.isRoot = false;
        UMLClass.isLeaf = false;
        for (int i = 0; i < MoldClass.Attributes.Count; i++)
        {
            TBoldUMLObject umlAttribute = UMLClass.Graph.Create("Attribute");
            UMLClass.AddRef("feature", umlAttribute);
            MoldAttributeToUMLAttribute(MoldClass.Attributes[i], umlAttribute);
        }

        for (int i = 0; i < MoldClass.Methods.Count; i++)
        {
            TBoldUMLObject umlOperation = UMLClass.Graph.Create("Operation");
            UMLClass.AddRef("feature", umlOperation);
            MoldMethodToUMLOperation(MoldClass.Methods[i], umlOperation);
        }

        if (MoldClass.SuperClass is { } superClass)
        {
            UMLClass.SetFirstParent(GetUMLClassByName(superClass.Name, UMLClass.model!));
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldAssociationToUMLAssociation(TMoldAssociation MoldAssociation, TBoldUMLObject UMLAssociation)
    {
        MoldElementToUMLElement(MoldAssociation, UMLAssociation);
        UMLAssociation.derived = MoldAssociation.Derived;
        if (MoldAssociation.LinkClass is { } linkClass)
        {
            UMLAssociation.class_ = GetUMLClassByName(linkClass.Name, UMLAssociation.model!);
        }

        for (int i = 0; i <= 1; i++)
        {
            TBoldUMLObject umlAssociationEnd = UMLAssociation.Graph.Create("AssociationEnd");
            UMLAssociation.AddRef("connection", umlAssociationEnd);
            MoldRoleToUMLAssociationEnd(MoldAssociation.Roles[i], umlAssociationEnd);
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldAttributeToUMLAttribute(TMoldAttribute MoldAttribute, TBoldUMLObject UMLAttribute)
    {
        MoldElementToUMLElement(MoldAttribute, UMLAttribute);
        UMLAttribute.typeName = MoldAttribute.BoldType;
        UMLAttribute.derived = MoldAttribute.Derived;
        UMLAttribute.visibility = MoldAttribute.Visibility;
        UMLAttribute.initialValue = MoldAttribute.InitialValue;
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldMethodToUMLOperation(TMoldMethod MoldMethod, TBoldUMLObject UMLOperation)
    {
        MoldElementToUMLElement(MoldMethod, UMLOperation);
        UMLOperation.ownerScope = MoldMethod.IsClassMethod ? TScopeKind.skClassifier : TScopeKind.skInstance;
        UMLOperation.visibility = MoldMethod.Visibility;
        MoldParametersToUMLParameters(MoldMethod.Parameters, UMLOperation);
        if (MoldMethod.ReturnType != "")
        {
            TBoldUMLObject returnParameter = UMLOperation.Graph.Create("Parameter");
            UMLOperation.AddRef("parameter", returnParameter);
            returnParameter.name = "return";
            returnParameter.kind = TBoldParameterDirectionKind.pdReturn;
            returnParameter.typeName = MoldMethod.ReturnType;
            returnParameter.SetBoldTV(TAG_DELPHINAME, TV_NAME);
            returnParameter.SetBoldTV(TAG_EXPRESSIONNAME, TV_NAME);
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldRoleToUMLAssociationEnd(TMoldRole MoldRole, TBoldUMLObject UMLAssociationEnd)
    {
        MoldElementToUMLElement(MoldRole, UMLAssociationEnd);
        UMLAssociationEnd.type_ = MoldRole.OtherEnd?.MoldClass is { } otherClass
            ? GetUMLClassByName(otherClass.Name, UMLAssociationEnd.association!.model!)
            : null;
        UMLAssociationEnd.isNavigable = MoldRole.Navigable;
        UMLAssociationEnd.isOrdered = MoldRole.Ordered;
        UMLAssociationEnd.aggregation = MoldRole.Aggregation;
        UMLAssociationEnd.visibility = MoldRole.Visibility;
        UMLAssociationEnd.changeability = MoldRole.Changeability;
        UMLAssociationEnd.multiplicity = MoldRole.Multiplicity;
        for (int i = 0; i < MoldRole.Qualifiers.Count; i++)
        {
            TBoldUMLObject umlAttribute = UMLAssociationEnd.Graph.Create("Attribute");
            UMLAssociationEnd.AddRef("qualifier", umlAttribute);
            MoldQualifierToUMLAttribute(MoldRole.Qualifiers[i], umlAttribute);
        }
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldQualifierToUMLAttribute(TMoldQualifier MoldQualifier, TBoldUMLObject UMLAttribute)
    {
        MoldElementToUMLElement(MoldQualifier, UMLAttribute);
        UMLAttribute.typeName = MoldQualifier.BoldType;
    }

    [Delphi(PortStatus.Ported)]
    private static void MoldParametersToUMLParameters(TBoldObjectArray MoldParameters, TBoldUMLObject UMLOperation)
    {
        for (int i = 0; i < MoldParameters.Count; i++)
        {
            var moldParameter = (TMoldParameter)MoldParameters[i]!;
            if (moldParameter.ParameterKind != TBoldParameterDirectionKind.pdReturn)
            {
                TBoldUMLObject umlParameter = UMLOperation.Graph.Create("Parameter");
                UMLOperation.AddRef("parameter", umlParameter);
                umlParameter.name = moldParameter.ParameterName;
                umlParameter.typeName = moldParameter.ParameterType;
                umlParameter.kind = moldParameter.ParameterKind;
                umlParameter.SetBoldTV(TAG_ISCONST, BoldUtils.BooleanToString(moldParameter.IsConst));
                umlParameter.SetBoldTV(TAG_DELPHINAME, TV_NAME);
                umlParameter.SetBoldTV(TAG_EXPRESSIONNAME, TV_NAME);
            }
        }
    }

    /// <summary>The class of the name directly in the model, made if missing.</summary>
    [Delphi(PortStatus.Ported)]
    private static TBoldUMLObject GetUMLClassByName(string name, TBoldUMLObject UMLModel)
    {
        TBoldUMLObject? umlClass = UMLModel.ownedElement.FirstOrDefault(e => e.MetaClass == "Class" && e.name == name);
        if (umlClass is null)
        {
            umlClass = UMLModel.Graph.Create("Class");
            umlClass.name = name;
            umlClass.namespace_ = UMLModel;
        }

        return umlClass;
    }

    /// <summary>A new association of the name in the model.</summary>
    [Delphi(PortStatus.Ported)]
    private static TBoldUMLObject GetUMLAssociationByName(string name, TBoldUMLObject UMLModel)
    {
        TBoldUMLObject result = UMLModel.Graph.Create("Association");
        result.name = name;
        result.namespace_ = UMLModel;
        return result;
    }
}
