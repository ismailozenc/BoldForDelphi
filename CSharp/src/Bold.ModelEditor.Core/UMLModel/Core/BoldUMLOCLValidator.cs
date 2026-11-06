// Delphi: Source/UMLModel/Core/BoldUMLOCLValidator.pas
// Port map: CSharp/port/units/BoldUMLOCLValidator.md

using static Bold.BoldDefaultTaggedValues;

namespace Bold;

/// <summary>
/// Checks the OCL of a model: derivation expressions, default string representations and constraints, each against
/// the type it must give, in the system type info of the model converted to Mold.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TOCLValidityChecker
{
    private TBoldUMLObject fUMLModel = null!;
    private TBoldSystemTypeInfo? fSystemTypeInfo;
    private TBoldOcl? fOclEvaluator;

    [Delphi(PortStatus.Ported)]
    public TOCLValidityChecker(TBoldTypeNameDictionary aTypeNameDictionary)
    {
        TypeNameDictionary = aTypeNameDictionary;
    }

    /// <summary>The type info of the model's Mold model.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldSystemTypeInfo ExtractSystemTypeInfo(TBoldUMLObject UMLModel) =>
        new(TBoldModelConverter.UMLModelToMold(UMLModel), false, false, TypeNameDictionary);

    [Delphi(PortStatus.Ported)]
    private TBoldOcl GetOCLEvaluator() => fOclEvaluator ??= new TBoldOcl(SystemTypeInfo, null);

    [Delphi(PortStatus.Ported)]
    protected void ValidateAssociationEnd(TBoldUMLObject UMLAssociationEnd)
    {
        if (UMLAssociationEnd.GetBoldTV(TAG_DERIVATIONOCL) != "")
        {
            if (UMLAssociationEnd.association!.derived)
            {
                ValidateExpression("DerivationOCL", UMLAssociationEnd, UMLAssociationEnd.otherEnd?.type_, UMLAssociationEnd.GetBoldTV(TAG_DERIVATIONOCL),
                    ElementTypeInfoFromUMLAssociationEnd(UMLAssociationEnd));
            }
            else
            {
                AddViolation(TSeverity.sHint, UMLAssociationEnd, "Derivation expression specified for non-derived association");
            }
        }

        ValidateConstraints(UMLAssociationEnd, UMLAssociationEnd.type_, UMLAssociationEnd.constraint);
    }

    [Delphi(PortStatus.Ported)]
    protected void ValidateAssociation(TBoldUMLObject UMLAssociation) =>
        ValidateConstraints(UMLAssociation, UMLAssociation.class_ ?? UMLAssociation, UMLAssociation.constraint);

    [Delphi(PortStatus.Ported)]
    protected void ValidateAttribute(TBoldUMLObject UMLAttribute)
    {
        if (UMLAttribute.GetBoldTV(TAG_DERIVATIONOCL) != "")
        {
            if (UMLAttribute.derived)
            {
                ValidateExpression("DerivationOCL", UMLAttribute, UMLAttribute.owner, UMLAttribute.GetBoldTV(TAG_DERIVATIONOCL), AttributeTypeInfoFromUMLAttribute(UMLAttribute));
            }
            else
            {
                AddViolation(TSeverity.sHint, UMLAttribute, "Derivation expression specified for non-derived attribute");
            }
        }

        ValidateConstraints(UMLAttribute, UMLAttribute.owner, UMLAttribute.constraint);
    }

    [Delphi(PortStatus.Ported)]
    protected void ValidateClass(TBoldUMLObject UMLClass)
    {
        if (UMLClass.GetBoldTV(TAG_DEFAULTSTRINGREPRESENTATION) != "")
        {
            ValidateExpression("DefaultStringRepresentation", UMLClass, UMLClass, UMLClass.GetBoldTV(TAG_DEFAULTSTRINGREPRESENTATION), OCLEvaluator.SymbolTable.help.StringType);
        }

        foreach (TBoldUMLObject attribute in UMLClass.feature.Where(f => f.IsKindOf("Attribute")))
        {
            ValidateAttribute(attribute);
        }

        foreach (TBoldUMLObject end in UMLClass.associationEnd)
        {
            ValidateAssociationEnd(end);
        }

        ValidateConstraints(UMLClass, UMLClass, UMLClass.constraint);
        ValidateDerivationExpressions(UMLClass, UMLClass.taggedValue("Bold.DerivationExpressions"));
    }

    [Delphi(PortStatus.Ported)]
    protected void ValidateConstraints(TBoldUMLObject Element, TBoldUMLObject? Context, List<TBoldUMLObject> ConstraintList)
    {
        foreach (TBoldUMLObject constraint in ConstraintList)
        {
            ValidateExpression("Constraint", Element, Context, constraint.body, OCLEvaluator.SymbolTable.help.BooleanType);
        }
    }

    /// <summary>Checks that an expression parses in its context and gives the type it should.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ValidateExpression(string ScopeName, TBoldUMLObject Element, TBoldUMLObject? Context, string Expression, TBoldElementTypeInfo? ElementTypeInfo)
    {
        string errorHeader = "Invalid " + ScopeName + ": ";
        TBoldElementTypeInfo? validationContext = null;
        if (Context?.MetaClass == "Class")
        {
            validationContext = SystemTypeInfo.ClassTypeInfoByModelName(Context.name);
        }
        else if (Context?.MetaClass == "Attribute")
        {
            if (TypeNameDictionary.MappingForModelName(Context.typeName) is { } mapping)
            {
                validationContext = SystemTypeInfo.AttributeTypeInfoByExpressionName(mapping.ExpressionName);
            }
        }
        else
        {
            AddViolation(TSeverity.sError, Element, errorHeader + SysUtils.Format("Unsupported context type (%s) for OCL-expression", [Context?.MetaClass ?? "nil"]));
            return;
        }

        TBoldElementTypeInfo? result = null;
        try
        {
            result = OCLEvaluator.ExpressionType(Expression, validationContext, true, null);
        }
        catch (EBoldOCLAbort e)
        {
            AddViolation(TSeverity.sError, Element, errorHeader + e.Message);
        }
        catch (EBoldOCLInternalError e)
        {
            AddViolation(TSeverity.sError, Element, errorHeader + "Internal error - " + e.Message);
        }
        catch (EBoldOCLError e)
        {
            AddViolation(TSeverity.sError, Element, errorHeader + "Error - " + e.Message);
        }
        catch (Exception e)
        {
            AddViolation(TSeverity.sError, Element, errorHeader + "Exception - " + e.Message);
        }

        if (ElementTypeInfo is not null && result is not null && !result.ConformsTo(ElementTypeInfo))
        {
            AddViolation(TSeverity.sError, Element, errorHeader + SysUtils.Format(" Type mismatch: Expected %s got %s", [ElementTypeInfo.ExpressionName, result.ExpressionName]));
        }
    }

    /// <summary>Checks the OCL of every class and association; the violations are added to the model's.</summary>
    [Delphi(PortStatus.Ported)]
    public void ValidateModel(TBoldUMLObject UMLModel)
    {
        fUMLModel = UMLModel;
        fSystemTypeInfo = ExtractSystemTypeInfo(UMLModel);
        fOclEvaluator = null;
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        log.StartLog("Validating OCL in model");
        ClearViolations();
        foreach (TBoldUMLObject cls in UMLModel.Classes)
        {
            ValidateClass(cls);
        }

        foreach (TBoldUMLObject association in UMLModel.Associations)
        {
            ValidateAssociation(association);
        }

        log.EndLog();
    }

    /// <summary>The expressions a class gives its inherited derived members (Bold.DerivationExpressions, name=expression lines).</summary>
    [Delphi(PortStatus.Ported)]
    protected void ValidateDerivationExpressions(TBoldUMLObject UMLClass, TBoldUMLObject? UMLTaggedValue)
    {
        TBoldUMLObject? LocateModelElement(string featureName)
        {
            if (UMLClass.allFeature.FirstOrDefault(f => SysUtils.SameText(f.name, featureName)) is { } feature)
            {
                return feature;
            }

            for (TBoldUMLObject? tmpClass = UMLClass; tmpClass is not null; tmpClass = tmpClass.Superclass)
            {
                if (tmpClass.associationEnd.FirstOrDefault(e => SysUtils.SameText(e.name, featureName)) is { } end)
                {
                    return end;
                }
            }

            return null;
        }

        if (UMLTaggedValue is null)
        {
            return;
        }

        var stringList = new TStringList { Text = UMLTaggedValue["value"] };
        for (int i = 0; i < stringList.Count; i++)
        {
            string name = stringList.Names(i);
            TBoldUMLObject? element = LocateModelElement(name);
            if (element?.MetaClass == "Attribute")
            {
                if (!element.derived)
                {
                    AddViolation(TSeverity.sHint, element, "Derivation expression respecified for non-derived attribute");
                }
                else if (element.GetBoldTV(TAG_DERIVATIONOCL) == "")
                {
                    AddViolation(TSeverity.sHint, element, "Derivation expression respecified for code-derived attribute");
                }
                else
                {
                    ValidateExpression("DerivationExpression", UMLClass, UMLClass, stringList.Values(name), AttributeTypeInfoFromUMLAttribute(element));
                }
            }

            if (element?.MetaClass == "AssociationEnd")
            {
                if (!element.association!.derived)
                {
                    AddViolation(TSeverity.sHint, element, "Derivation expression respecified for non-derived association");
                }
                else if (element.GetBoldTV(TAG_DERIVATIONOCL) == "")
                {
                    AddViolation(TSeverity.sHint, element, "Derivation expression respecified for code-derived association end");
                }
                else
                {
                    ValidateExpression("DerivationExpression", UMLClass, UMLClass, stringList.Values(name), ElementTypeInfoFromUMLAssociationEnd(element));
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAttributeTypeInfo? AttributeTypeInfoFromUMLAttribute(TBoldUMLObject UMLAttribute) =>
        TypeNameDictionary.MappingForModelName(UMLAttribute.typeName) is { } mapping
            ? SystemTypeInfo.AttributeTypeInfoByExpressionName(mapping.ExpressionName)
            : null;

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? ElementTypeInfoFromUMLAssociationEnd(TBoldUMLObject UMLAssociationEnd)
    {
        TBoldClassTypeInfo? classTypeInfo = SystemTypeInfo.ClassTypeInfoByModelName(UMLAssociationEnd.type_?.name ?? "");
        return UMLAssociationEnd.multi ? SystemTypeInfo.ListTypeInfoByElement(classTypeInfo) : classTypeInfo;
    }

    [Delphi(PortStatus.Ported)]
    private void ClearViolations() => fUMLModel.Graph.Violations.Clear();

    [Delphi(PortStatus.Ported)]
    private void AddViolation(TSeverity Severity, TBoldUMLObject element, string Msg) =>
        fUMLModel.Graph.Violations.Add(new TBoldUMLViolation(Severity, Msg, element));

    [Delphi(PortStatus.Ported)] protected TBoldSystemTypeInfo SystemTypeInfo => fSystemTypeInfo!;
    [Delphi(PortStatus.Ported)] protected TBoldOcl OCLEvaluator => GetOCLEvaluator();
    [Delphi(PortStatus.Ported)] public TBoldTypeNameDictionary TypeNameDictionary { get; }
}
