// Delphi: Source/UMLModel/Core/BoldUMLModelValidator.pas
// Port map: CSharp/port/units/BoldUMLModelValidator.md
// Validates the UML graph of a model (flattened and boldified, as Delphi validated its UML model): names for the
// source code, OCL and the database, duplicates up the class hierarchy, persistence mappers, associations and their
// ends. Delphi's OCL queries over the UML object space are loops over the graph.

using static Bold.BoldDefaultTaggedValues;

namespace Bold;

/// <summary>Checks a model for what would stop Bold from using it, and for what is unusual.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldUMLModelValidator : TBoldUMLAbstractModelValidator, IDisposable
{
    /// <summary>The event a model is sent when it has been validated.</summary>
    [Delphi(PortStatus.Ported, Symbol = "beModelValidated")] public const int beModelValidated = 1024;

    private const string sUMVModelNameEmpty = "Model must have a name";
    private const string sUMVModelUnknownMapper = "Unknown system persistence mapper \"%s\" in model \"%s\"";
    private const string sUMVClassNameEmpty = "Class must have a name";
    private const string sUMVClassNameExists = "Duplicate class name \"%s\"";
    private const string sUMVClassUnknownMapper = "Unknown object persistence mapper \"%s\" in class \"%s\"";
    private const string sUMVDelphiNameEmpty = "\"%s\" must have a Delphi name";
    private const string sUMVDelphiNameInvalid = "Invalid Delphi name \"%s\" in \"%s\"";
    private const string sUMVDelphiNameReserved = "Delphi name \"%s\" in \"%s\" is a reserved word";
    private const string sUMVDelphiNameExists = "Duplicate Delphi name \"%s\" in \"%s\"";
    private const string sUMVDelphiNameExists2 = "Duplicate Delphi name \"%s\" in \"%s\": \"%s\" and \"%s\"";
    private const string sUMVCppNameEmpty = "\"%s\" must have a C++ name";
    private const string sUMVCppNameInvalid = "Invalid C++ name \"%s\" in \"%s\"";
    private const string sUMVCppNameReserved = "C++ name \"%s\" in \"%s\" is a reserved word";
    private const string sUMVCppNameExists = "Duplicate C++ name \"%s\" in \"%s\"";
    private const string sUMVCppNameExists2 = "Duplicate C++ name \"%s\" in \"%s\": \"%s\" and \"%s\"";
    private const string sUMVUnitNameEmpty = "\"%s\" must have a unit name";
    private const string sUMVExpressionNameEmpty = "\"%s\" must have an expression name";
    private const string sUMVExpressionNameInvalid = "Invalid expression name \"%s\" in \"%s\"";
    private const string sUMVExpressionNameOCLReserved = "Expression name \"%s\" in \"%s\" is a reserved word in OCL";
    private const string sUMVExpressionNameExists = "Duplicate expression name \"%s\" in \"%s\"\"";
    private const string sUMVExpressionNameExists2 = "Duplicate expression name \"%s\" in \"%s\": \"%s\" and \"%s\"";
    private const string sUMVSQLTableNameEmpty = "\"%s\" must have a table name";
    private const string sUMVSQLColumnNameEmpty = "\"%s\" must have a column name";
    private const string sUMVTableNameInvalid = "Invalid table name \"%s\" in \"%s\"";
    private const string sUMVTableNameSQLReserved = "Table name \"%s\" in \"%s\" is a reserved word in SQL";
    private const string sUMVTableNameExists = "Duplicate table name \"%s\" in \"%s\" and \"%s\"";
    private const string sUMVColumnNameInvalid = "Invalid column name \"%s\" in \"%s\"";
    private const string sUMVColumnNameSQLReserved = "Column name \"%s\" in \"%s\" is a reserved word in SQL";
    private const string sUMVColumnNameExists = "Duplicate column name \"%s\" in \"%s\": \"%s\" and \"%s\"";
    private const string sUMVMemberNameEmpty = "Member must have a name in \"%s\"";
    private const string sUMVMemberNameExists = "Duplicate member name \"%s\" in \"%s\": \"%s\" and \"%s\"";
    private const string sUMVAttributeUnknownType = "Attribute \"%s\" has unknown type (%s)";
    private const string sUMVAttributeUnknownMapper = "Attribute \"%s\" has unknown persistence mapper (%s)";
    private const string sUMVAttributeCantStore = "Attribute \"%s\" can't be stored, incompatible persistence mapper";
    private const string sUMVAssociationEndIsMultiWithOtherEndComposite = "Association end \"%s\" is multi, but other end is composite";
    private const string sUMVAssociationEndUnknownClass = "Association end \"%s\" in association \"%s\" not associated with any class";
    private const string sUMVAssociationEndUnknownMapper = "Unknown association end persistence mapper (%s) in association \"%s\"";
    private const string sUMVAssociationEndCantStore = "Association end persistence mapper %s cannot store %s in association \"%s\"";
    private const string sUMVInvalidAssociationEndIndirectAndEmbed = "Association end \"%s\" in association \"%s\" is indirect and embedded";
    private const string sUMVInvalidAssociationEndMultiAndEmbed = "Association end \"%s\" in association \"%s\" is multi and embedded";
    private const string sUMVInvalidAssociationEndOrderedandSingle = "Association end \"%s\" in association \"%s\" is non-multi and ordered";
    private const string sUMVSingleAssociationEndsEmbeddedInBothEnds = "Association \"%s\"s both ends are embedded and single-single ends.";
    private const string sUMVAssociationNeedsTwoRoles = "Association \"%s\" must have two assocationEnds";
    private const string sUMVAssociationEndNeedsType = "AssociationEnd \"%s\" is not assocatied with any class";
    private const string sUMVDerivedAssociationCanNotHaveClass = "Derived association \"%s\" can not have an association class";
    private const string sUMVAssociationM2MNeedsClass = "Multi to multi association \"%s\" must have association class";
    private const string sUMVAssociationRolesNeedClass = "Non-embedded roles \"%s\" and \"%s\" need association class";
    private const string sUMVAssociationClassCannotBePart = "Association class for \"%s\" cannot be one of the classes in the association";
    private const string sUMVPersistenceSubClass = "Persistent class \"%s\" has non-persistent superclass";
    private const string sUMVParentMappedClass = "Parent mapped class \"%s\" must have superclass";
    private const string sUMVChildrenMappedClass = "Children mapped class \"%s\" must be abstract";
    private const string sUMVImportedMappedClass = "Imported mapped class \"%s\" must be imported";
    private const string sUMVRecursiveMapping = "Recursive mapping between \"%s\" and \"%s\"";
    private const string sUMVParentMappedWithNotNullAttribute = "Parent mapped class \"%s\" with NotNull attributes";
    private const string sUMVAttributeDerivedAndPersistent = "Attribute \"%s\" is both derived and persistent";
    private const string sUMVAssociationDerivedAndPersistent = "Association \"%s\" is both derived and persistent";
    private const string sUMVAssociationWithTwoAggregatedEnds = "Association \"%s\" has two ends that are aggregated";
    private const string sUMVBoldTableReservedWord = "Table name \"%s\" in \"%s\" is a \"bold reserved\" word";
    private const string sUMVBoldColumnReservedWord = "Column name \"%s\" in \"%s\" is a \"bold reserved\" word";
    private const string sUMVDuplicateAssociationEndName = "AssociationEnds in Association %s are both named %s";
    private const string sUMVVersionedClassWithNonVersionedSuperClass = "Class \"%s\" is versioned, but its superclass is not";
    private const string sUMVAssociationEndIsSingleAndNotNAvigable = "AssociationEnd %s is single and not navigable";
    private const string sUMVAssociationEndIllegalQualifier = "Qualifier %s in associationEnd %s does not have matching attribute in class of other end";
    private const string sUMVAssociationEndQualifiedAndMulti = "AssociationEnd %s is qualified and multi. Not supported";
    private const string sUMVRootClassMissing = "Root Class %s is missing";
    private const string sUMVLinkClassWithSuperClassAsEnd = "LinkClass (%s) must not inherit from any of the ends (%s)";
    private const string sUMVAssociationAndClassNotEquallyPersistent = "Association (%s) and its association class are not equally persistent";

    private TBoldTypeNameDictionary? fTypeNameDictionary;
    private TStringList? fSQLReservedWordList;

    public TBoldUMLModelValidator(TBoldUMLObject UMLModel, TBoldSQLDataBaseConfig? SQLDataBaseConfig = null,
        TBoldModelValidatorSourceLanguage Language = TBoldModelValidatorSourceLanguage.mvslNone)
        : base(UMLModel, SQLDataBaseConfig, Language)
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldUMLModelValidator.Destroy")]
    public void Dispose()
    {
        fSQLReservedWordList = null;
        GC.SuppressFinalize(this);
    }

    private static bool IsAlpha(char c) => char.IsAsciiLetter(c) || c == '_';

    private static bool IsAlphaNumeric(char c) => IsAlpha(c) || char.IsAsciiDigit(c);

    [Delphi(PortStatus.Ported, Symbol = "IsValidDelphiIdentifier")]
    internal static bool IsValidDelphiIdentifier(string Ident) => Ident.Length > 0 && IsAlpha(Ident[0]) && Ident.Skip(1).All(IsAlphaNumeric);

    [Delphi(PortStatus.Ported, Symbol = "IsValidCppIdentifier")]
    internal static bool IsValidCppIdentifier(string Ident) => IsValidDelphiIdentifier(Ident);

    [Delphi(PortStatus.Ported, Symbol = "IsValidOCLIdentifier")]
    internal static bool IsValidOCLIdentifier(string Ident) => IsValidDelphiIdentifier(Ident) && Ident[0] != '_';

    [Delphi(PortStatus.Ported, Symbol = "IsValidSQLIdentifier")]
    internal static bool IsValidSQLIdentifier(string Ident) =>
        Ident.Length > 0 && (IsAlpha(Ident[0]) || Ident[0] == '$') && Ident.Skip(1).All(c => IsAlphaNumeric(c) || c == '$');

    private static readonly string[] Delphi3Reserved =
    [
        "and", "array", "as", "asm", "begin", "case", "class", "const", "constructor", "destructor",
        "dispinterface", "div", "do", "downto", "else", "end", "except", "exports", "file", "finalization",
        "finally", "for", "function", "goto", "if", "implementation", "in", "inherited", "initialization", "inline",
        "interface", "is", "label", "library", "mod", "nil", "not", "object", "of", "or",
        "out", "packed", "procedure", "program", "property", "raise", "record", "repeat", "resourcestring", "set",
        "shl", "shr", "string", "stringresource", "then", "threadvar", "to", "try", "type", "unit",
        "until", "uses", "var", "while", "with", "xor",
    ];

    private static readonly string[] Delphi3Directives =
    [
        "absolute", "abstract", "assembler", "automated", "cdecl", "default", "dispid", "dynamic", "export", "external",
        "far", "forward", "index", "message", "near", "nodefault", "override", "pascal", "private",
        "protected", "public", "published", "read", "readonly", "register", "resident", "safecall", "stdcall", "stored",
        "virtual", "write", "writeonly",
    ];

    private static bool InList(string S, string[] List) => List.Any(w => string.Equals(S, w, StringComparison.OrdinalIgnoreCase));

    [Delphi(PortStatus.Ported, Symbol = "IsDelphiReservedWord")]
    internal static bool IsDelphiReservedWord(string S) => S.Length > 0 && (InList(S, Delphi3Reserved) || InList(S, Delphi3Directives));

    [Delphi(PortStatus.Ported, Symbol = "IsCppReservedWord")]
    internal static bool IsCppReservedWord(string S) => S.Length > 0 && InList(S, ["if"]);

    [Delphi(PortStatus.Ported, Symbol = "IsBoldReservedWord")]
    internal static bool IsBoldReservedWord(string S) => InList(S, ["BOLD_ID", "BOLD_TYPE"]);

    [Delphi(PortStatus.Ported, Symbol = "IsOCLReservedWord")]
    internal static bool IsOCLReservedWord(string S) => InList(S, ["and", "div", "implies", "mod", "not", "or", "xor"]);

    [Delphi(PortStatus.Ported, Symbol = "ContainsClass")]
    internal static bool ContainsClass(TBoldUMLObject Model, string name) =>
        Model.Classes.Any(c => string.Equals(name, c.name, StringComparison.OrdinalIgnoreCase));

    [Delphi(PortStatus.Ported)]
    private string ExpandedSourceName(TBoldUMLObject Elem) => Language switch
    {
        TBoldModelValidatorSourceLanguage.mvslDelphi => BoldNameExpander.BoldExpandName(Elem.GetBoldTV(TAG_DELPHINAME), Elem.name, TExpansionType.xtDelphi, -1, NationalCharConversion()),
        TBoldModelValidatorSourceLanguage.mvslCpp => BoldNameExpander.BoldExpandName(Elem.GetBoldTV(TAG_CPPNAME), Elem.name, TExpansionType.xtDelphi, -1, NationalCharConversion()),
        _ => "",
    };

    [Delphi(PortStatus.Ported)]
    private string ExpandedExpressionName(TBoldUMLObject Elem) =>
        BoldNameExpander.BoldExpandName(Elem.GetBoldTV(TAG_EXPRESSIONNAME), Elem.name, TExpansionType.xtExpression, -1, NationalCharConversion());

    [Delphi(PortStatus.Ported)]
    private string ExpandedDBName(TBoldUMLObject Elem)
    {
        string source = Elem.IsKindOf("Class") ? Elem.GetBoldTV(TAG_TABLENAME) : Elem.GetBoldTV(TAG_COLUMNNAME);
        return BoldNameExpander.BoldExpandPrefix(source, Elem.name, SQLDataBaseConfig!.SystemTablePrefix, -1, NationalCharConversion());
    }

    /// <summary>Validates the model, which must be flattened and boldified; the violations are in the graph.</summary>
    [Delphi(PortStatus.Ported)]
    public void Validate(TBoldTypeNameDictionary? TypeNameDictionary)
    {
        if (!(TBoldUMLBoldify.IsBoldified(UMLModel) && TBoldUMLSupport.IsFlattened(UMLModel)))
        {
            throw new EBoldInternal("Model not Boldified and flattened");
        }

        fTypeNameDictionary = TypeNameDictionary;
        ClearViolations();
        TBoldLogHandler log = BoldLogHandler.BoldLog();
        log.StartLog("Validating the model");
        if (TypeNameDictionary is null)
        {
            AddError("No TypeNameDictionary available", [], UMLModel);
        }

        if (UMLModel.name == "")
        {
            AddError(sUMVModelNameEmpty, [], UMLModel);
        }

        ValidateNames(UMLModel, UMLModel.name);
        string mapper = UMLModel.GetBoldTV(TAG_PMAPPERNAME);
        if (!SysUtils.SameText(mapper, BoldDefs.DEFAULTNAME) && CheckDBStuff() && BoldPMapperLists.BoldSystemPersistenceMappers().DescriptorByName(mapper) is null)
        {
            AddError(sUMVModelUnknownMapper, [mapper, UMLModel.name], UMLModel);
        }

        List<TBoldUMLObject> classes = UMLModel.Classes;
        var names = new List<(string Key, TBoldUMLObject Element)>();
        var sourceCodeNames = new List<(string Key, TBoldUMLObject Element)>();
        var expressionNames = new List<(string Key, TBoldUMLObject Element)>();
        var tableNames = new List<(string Key, TBoldUMLObject Element)>();
        foreach (TBoldUMLObject cls in classes)
        {
            names.Add((cls.name.ToUpperInvariant(), cls));
            sourceCodeNames.Add((ExpandedSourceName(cls).ToUpperInvariant(), cls));
            expressionNames.Add((ExpandedExpressionName(cls).ToUpperInvariant(), cls));
            if (CheckDBStuff() && cls.persistent)
            {
                tableNames.Add((ExpandedDBName(cls).ToUpperInvariant(), cls));
            }
        }

        foreach (var list in new[] { names, sourceCodeNames, expressionNames, tableNames })
        {
            list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
        }

        for (int i = 0; i < classes.Count - 1; i++)
        {
            if (names[i].Key == names[i + 1].Key)
            {
                AddError(sUMVClassNameExists, [names[i].Element.name], names[i].Element);
            }

            if (Language != TBoldModelValidatorSourceLanguage.mvslNone && sourceCodeNames[i].Key == sourceCodeNames[i + 1].Key)
            {
                string errorStr = Language == TBoldModelValidatorSourceLanguage.mvslDelphi ? sUMVDelphiNameExists : sUMVCppNameExists;
                AddError(errorStr, [ExpandedSourceName(sourceCodeNames[i].Element), sourceCodeNames[i].Element.name], sourceCodeNames[i].Element);
            }

            if (expressionNames[i].Key == expressionNames[i + 1].Key)
            {
                AddError(sUMVExpressionNameExists, [ExpandedExpressionName(expressionNames[i].Element), expressionNames[i].Element.name], expressionNames[i].Element);
            }

            if (i < tableNames.Count - 1 && tableNames[i].Key == tableNames[i + 1].Key && CheckDBStuff())
            {
                AddError(sUMVTableNameExists, [ExpandedDBName(tableNames[i].Element), tableNames[i].Element.name, tableNames[i + 1].Element.name], tableNames[i].Element);
            }
        }

        List<TBoldUMLObject> associations = UMLModel.Associations;
        log.ProgressMax = classes.Count + associations.Count;
        foreach (TBoldUMLObject cls in classes)
        {
            ValidateClass(cls);
            log.ProgressStep();
        }

        foreach (TBoldUMLObject association in associations)
        {
            ValidateAssociation(association);
            log.ProgressStep();
        }

        ValidateDuplicates(UMLModel);
        log.EndLog();
        UMLModel.Graph.SendModelValidated();
    }

    [Delphi(PortStatus.Ported)]
    private void ValidateAttribute(TBoldUMLObject attribute)
    {
        ValidateFeature(attribute);
        if (attribute.name == "")
        {
            return;
        }

        string fullName = attribute.owner?.name + "." + attribute.name;
        TBoldTypeNameMapping? mapping = TypeNameDictionary?.MappingForModelName(attribute.typeName);
        if (mapping is null)
        {
            AddError(sUMVAttributeUnknownType, [fullName, attribute.typeName], attribute);
            return;
        }

        if (mapping.ExpressionName == BoldDefs.DEFAULTNAME)
        {
            AddHint("%s uses default type mapping (type: %s)", [fullName, attribute.typeName], attribute);
        }

        string delphiTypeName = mapping.ExpandedDelphiName;
        if (BoldMemberTypeDictionary.BoldMemberTypes().DescriptorByDelphiName(delphiTypeName)?.MemberClass is null)
        {
            AddHint("%s: No attribute class installed for %s", [fullName, delphiTypeName], attribute);
        }

        string mapperName = attribute.GetBoldTV(TAG_PMAPPERNAME);
        if (!SysUtils.SameText(mapperName, BoldDefs.DEFAULTNAME) && attribute.EffectivePersistent && CheckDBStuff())
        {
            TBoldMemberPersistenceMapperDescriptor? descriptor =
                BoldPMapperLists.BoldMemberPersistenceMappers().DescriptorForModelNameWithDefaultSupport(attribute.typeName, mapperName, TypeNameDictionary!);
            if (descriptor is null)
            {
                AddError(sUMVAttributeUnknownMapper, [fullName, mapperName], attribute);
            }
            else if (!descriptor.CanStore(mapping.ExpandedContentsName))
            {
                AddError(sUMVAttributeCantStore, [fullName], attribute);
            }
        }

        if (CheckDBStuff() && attribute.derived && attribute.EffectivePersistent)
        {
            AddHint(sUMVAttributeDerivedAndPersistent, [fullName], attribute);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ValidateClass(TBoldUMLObject aClass)
    {
        static bool HasNotNullMembers(TBoldUMLObject cls) =>
            cls.feature.Any(f => f.IsKindOf("Attribute") && f.EffectivePersistent && TVIsFalse(f.GetBoldTV(TAG_ALLOWNULL)));

        if (aClass.name == "")
        {
            AddError(sUMVClassNameEmpty, [], aClass);
            return;
        }

        ValidateNames(aClass, aClass.name);
        TBoldUMLObject? superClass = aClass.Superclass;
        string tableMapping = aClass.GetBoldTV(TAG_TABLEMAPPING);
        if (aClass.persistent && CheckDBStuff())
        {
            if (tableMapping == TV_TABLEMAPPING_IMPORTED && TVIsFalse(aClass.GetBoldTV(TAG_IMPORTED)))
            {
                AddError(sUMVImportedMappedClass, [aClass.name], aClass);
            }

            if (tableMapping == TV_TABLEMAPPING_CHILDREN && !aClass.isAbstract)
            {
                AddError(sUMVChildrenMappedClass, [aClass.name], aClass);
            }

            if (superClass is null && tableMapping == TV_TABLEMAPPING_PARENT)
            {
                AddError(sUMVParentMappedClass, [aClass.name], aClass);
            }

            if (tableMapping == TV_TABLEMAPPING_PARENT && HasNotNullMembers(aClass))
            {
                AddWarning(sUMVParentMappedWithNotNullAttribute, [aClass.name], aClass);
            }

            if (superClass is not null && !superClass.persistent)
            {
                AddError(sUMVPersistenceSubClass, [aClass.name], aClass);
            }

            if (superClass is not null && superClass.persistent && tableMapping == TV_TABLEMAPPING_PARENT &&
                superClass.GetBoldTV(TAG_TABLEMAPPING) == TV_TABLEMAPPING_CHILDREN)
            {
                AddError(sUMVRecursiveMapping, [aClass.name, superClass.name], aClass);
            }

            string mapper = aClass.GetBoldTV(TAG_PMAPPERNAME);
            if (!SysUtils.SameText(mapper, BoldDefs.DEFAULTNAME) && BoldPMapperLists.BoldObjectPersistenceMappers().DescriptorByName(mapper) is null)
            {
                AddError(sUMVClassUnknownMapper, [mapper, aClass.name], aClass);
            }
        }

        if (superClass is not null && TVIsTrue(aClass.GetBoldTV(TAG_VERSIONED)) && !TVIsTrue(superClass.GetBoldTV(TAG_VERSIONED)))
        {
            AddError(sUMVVersionedClassWithNonVersionedSuperClass, [aClass.name], aClass);
        }

        foreach (TBoldUMLObject feature in aClass.feature)
        {
            if (feature.IsKindOf("Attribute"))
            {
                ValidateAttribute(feature);
            }
            else if (feature.IsKindOf("Operation"))
            {
                ValidateOperation(feature);
            }
        }

        if (aClass.associationOfClass is { } association)
        {
            foreach (TBoldUMLObject end in association.connection)
            {
                for (TBoldUMLObject? temp = aClass; temp is not null; temp = temp.Superclass)
                {
                    if (temp == end.type_)
                    {
                        AddError(sUMVLinkClassWithSuperClassAsEnd, [aClass.name, temp.name], aClass);
                    }
                }
            }
        }
    }

    /// <summary>Duplicate member, expression, source and column names down every branch of the class hierarchy.</summary>
    [Delphi(PortStatus.Ported)]
    private void ValidateDuplicates(TBoldUMLObject Model)
    {
        var names = new List<(string Name, TBoldUMLObject Element)>();
        var sourceNames = new List<(string Name, TBoldUMLObject Element)>();
        var expressionNames = new List<(string Name, TBoldUMLObject Element)>();
        var databaseNames = new List<(string Name, TBoldUMLObject Element)>();

        string GetParent(TBoldUMLObject elem, string name)
        {
            if (elem.IsKindOf("Feature"))
            {
                return elem.owner?.name + "." + name;
            }

            if (elem.MetaClass == "AssociationEnd" && elem.otherEnd is { } other)
            {
                return other.type_?.name + "." + name;
            }

            return "";
        }

        void CheckAndAddName(List<(string Name, TBoldUMLObject Element)> list, string owningElement, string name, TBoldUMLObject element,
            string errorMsg, ref bool reported, bool rememberReport)
        {
            int index = list.FindIndex(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
            if (index != -1 && !reported && !(list[index].Element.IsKindOf("Operation") && element.IsKindOf("Operation")))
            {
                AddError(errorMsg, [name, owningElement, GetParent(element, name), GetParent(list[index].Element, name)], element);
                AddError(errorMsg, [name, owningElement, GetParent(list[index].Element, name), GetParent(element, name)], list[index].Element);
                if (rememberReport)
                {
                    reported = true;
                }
            }

            list.Add((name, element));
        }

        void TraverseClass(TBoldUMLObject aClass)
        {
            int dbCount = databaseNames.Count, nameCount = names.Count, sourceCount = sourceNames.Count, exprCount = expressionNames.Count;
            bool reported;

            void CheckAndAddAssociationEnd(TBoldUMLObject assoEnd)
            {
                if (!assoEnd.isNavigable && assoEnd.association!.derived)
                {
                    return; // this end does not exist in real life
                }

                reported = false;
                CheckAndAddName(names, aClass.name, assoEnd.name, assoEnd, sUMVMemberNameExists, ref reported, true);
                CheckAndAddName(expressionNames, aClass.name, ExpandedExpressionName(assoEnd), assoEnd, sUMVExpressionNameExists2, ref reported, false);
                if (Language == TBoldModelValidatorSourceLanguage.mvslDelphi)
                {
                    CheckAndAddName(sourceNames, aClass.name, ExpandedSourceName(assoEnd), assoEnd, sUMVDelphiNameExists2, ref reported, false);
                }
                else if (Language == TBoldModelValidatorSourceLanguage.mvslCpp)
                {
                    CheckAndAddName(sourceNames, aClass.name, ExpandedSourceName(assoEnd), assoEnd, sUMVCppNameExists2, ref reported, false);
                }

                if (assoEnd.association!.EffectivePersistent && CheckDBStuff())
                {
                    CheckAndAddName(databaseNames, aClass.name, ExpandedDBName(assoEnd), assoEnd, sUMVColumnNameExists, ref reported, false);
                }
            }

            foreach (TBoldUMLObject feature in aClass.feature)
            {
                reported = false;
                CheckAndAddName(names, aClass.name, feature.name, feature, sUMVMemberNameExists, ref reported, true);
                CheckAndAddName(expressionNames, aClass.name, ExpandedExpressionName(feature), feature, sUMVExpressionNameExists2, ref reported, false);
                if (Language == TBoldModelValidatorSourceLanguage.mvslDelphi)
                {
                    CheckAndAddName(sourceNames, aClass.name, ExpandedSourceName(feature), feature, sUMVDelphiNameExists2, ref reported, false);
                }
                else if (Language == TBoldModelValidatorSourceLanguage.mvslCpp)
                {
                    CheckAndAddName(sourceNames, aClass.name, ExpandedSourceName(feature), feature, sUMVCppNameExists2, ref reported, false);
                }

                if (feature.IsKindOf("Attribute") && feature.EffectivePersistent && CheckDBStuff() && feature.GetBoldTV(TAG_STORAGE) != TV_STORAGE_EXTERNAL)
                {
                    CheckAndAddName(databaseNames, aClass.name, ExpandedDBName(feature), feature, sUMVColumnNameExists, ref reported, false);
                }
            }

            foreach (TBoldUMLObject end in aClass.associationEnd)
            {
                if (end.association?.GetBoldTV(TAG_STORAGE) != TV_STORAGE_EXTERNAL && end.otherEnd is { } other)
                {
                    CheckAndAddAssociationEnd(other);
                }
            }

            // the inner links of a link class
            if (aClass.associationOfClass is { } association && association.connection.Count == 2)
            {
                CheckAndAddAssociationEnd(association.connection[0]);
                CheckAndAddAssociationEnd(association.connection[1]);
            }

            foreach (TBoldUMLObject subClass in aClass.SubClasses)
            {
                TraverseClass(subClass);
            }

            databaseNames.RemoveRange(dbCount, databaseNames.Count - dbCount);
            names.RemoveRange(nameCount, names.Count - nameCount);
            sourceNames.RemoveRange(sourceCount, sourceNames.Count - sourceCount);
            expressionNames.RemoveRange(exprCount, expressionNames.Count - exprCount);
        }

        if (TBoldUMLBoldify.GetRootClass(Model) is { } root)
        {
            TraverseClass(root);
        }
        else
        {
            AddError(sUMVRootClassMissing, [Model.GetBoldTV(TAG_ROOTCLASS)], Model);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ValidateAssociation(TBoldUMLObject association)
    {
        List<TBoldUMLObject> connection = association.connection;
        if (connection.Count != 2)
        {
            AddError(sUMVAssociationNeedsTwoRoles, [association.name], association);
            return;
        }

        TBoldUMLObject end0 = connection[0], end1 = connection[1];
        TBoldUMLObject? linkClass = association.class_;
        if (!association.derived && end0.multi && end1.multi && linkClass is null)
        {
            AddError(sUMVAssociationM2MNeedsClass, [association.name], association);
        }

        if (linkClass is not null && linkClass.persistent != association.persistent)
        {
            AddError(sUMVAssociationAndClassNotEquallyPersistent, [association.name], association);
        }

        if (association.derived && linkClass is not null)
        {
            AddError(sUMVDerivedAssociationCanNotHaveClass, [association.name], association);
        }

        if (!association.derived && TVIsFalse(end0.GetBoldTV(TAG_EMBED)) && TVIsFalse(end1.GetBoldTV(TAG_EMBED)) && linkClass is null)
        {
            AddError(sUMVAssociationRolesNeedClass, [end0.name, end1.name], association);
        }

        if (linkClass is not null && (end0.type_ == linkClass || end1.type_ == linkClass))
        {
            AddError(sUMVAssociationClassCannotBePart, [association.name], association);
        }

        if (CheckDBStuff() && association.derived && association.EffectivePersistent)
        {
            AddHint(sUMVAssociationDerivedAndPersistent, [association.name], association);
        }

        if (end0.aggregation != TAggregationKind.akNone && end1.aggregation != TAggregationKind.akNone)
        {
            AddError(sUMVAssociationWithTwoAggregatedEnds, [association.name], association);
        }

        if (end0.name == end1.name)
        {
            AddError(sUMVDuplicateAssociationEndName, [association.name, end0.name], association);
        }

        if (CheckDBStuff() && !(end0.multi || end1.multi) && TVIsTrue(end0.GetBoldTV("Embed")) && TVIsTrue(end1.GetBoldTV("Embed")))
        {
            AddHint(sUMVSingleAssociationEndsEmbeddedInBothEnds, [association.name], association);
        }

        foreach (TBoldUMLObject end in connection)
        {
            ValidateAssociationEnd(end);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ValidateOperation(TBoldUMLObject operation)
    {
        ValidateFeature(operation);
        foreach (TBoldUMLObject parameter in operation.parameter)
        {
            ValidateParameter(parameter);
        }
    }

    [Delphi(PortStatus.Ported, Note = "Checks nothing, as in Delphi.")]
    private void ValidateParameter(TBoldUMLObject parameter)
    {
    }

    [Delphi(PortStatus.Ported)]
    private void ValidateAssociationEnd(TBoldUMLObject associationEnd)
    {
        TBoldUMLObject association = associationEnd.association!;
        ValidateNames(associationEnd, association.name + "." + associationEnd.name);
        if (associationEnd.type_ is not { } type)
        {
            AddError(sUMVAssociationEndNeedsType, [associationEnd.name], associationEnd);
            return;
        }

        if (associationEnd.qualifier.Count > 0 && BoldUtils.GetUpperLimitForMultiplicity(associationEnd.multiplicity) > 1)
        {
            AddError(sUMVAssociationEndQualifiedAndMulti, [associationEnd.name], associationEnd);
        }

        foreach (TBoldUMLObject qualifier in associationEnd.qualifier)
        {
            if (!type.allFeature.Any(f => f.IsKindOf("Attribute") && f.name == qualifier.name && f.typeName == qualifier.typeName))
            {
                AddError(sUMVAssociationEndIllegalQualifier, [qualifier.name, associationEnd.name], qualifier);
            }
        }

        if (associationEnd.name == "")
        {
            return;
        }

        if (associationEnd.isNavigable && associationEnd.multi && associationEnd.otherEnd?.aggregation == TAggregationKind.akComposite)
        {
            AddError(sUMVAssociationEndIsMultiWithOtherEndComposite, [associationEnd.name], associationEnd);
        }

        if (!associationEnd.multi && !associationEnd.isNavigable)
        {
            AddHint(sUMVAssociationEndIsSingleAndNotNAvigable, [associationEnd.name], associationEnd);
        }

        if (!associationEnd.multi && associationEnd.isOrdered)
        {
            AddHint(sUMVInvalidAssociationEndOrderedandSingle, [associationEnd.name, association.name], associationEnd);
        }

        bool embed = BoldUtils.StringToBoolean(associationEnd.GetBoldTV(TAG_EMBED));
        if (association.class_ is not null && embed)
        {
            AddHint(sUMVInvalidAssociationEndIndirectAndEmbed, [associationEnd.name, association.name], associationEnd);
        }

        if (!(association.derived && !associationEnd.isNavigable) && associationEnd.multi && embed)
        {
            AddHint(sUMVInvalidAssociationEndMultiAndEmbed, [associationEnd.name, association.name], associationEnd);
        }

        string streamName = association.class_ is not null
            ? associationEnd.multi ? BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair : BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair
            : associationEnd.multi ? BoldDefaultStreamNames.BoldContentName_ObjectIdListRef : BoldDefaultStreamNames.BoldContentName_ObjectIdRef;
        string mapper = associationEnd.GetBoldTV(TAG_PMAPPERNAME);
        if (mapper != "" && !SysUtils.SameText(mapper, BoldDefs.DEFAULTNAME) && association.EffectivePersistent && CheckDBStuff())
        {
            TBoldMemberPersistenceMapperDescriptor? descriptor = BoldPMapperLists.BoldMemberPersistenceMappers().DescriptorByDelphiName(mapper);
            if (descriptor is null)
            {
                AddError(sUMVAssociationEndUnknownMapper, [mapper, association.name], associationEnd);
            }
            else if (!descriptor.CanStore(streamName))
            {
                AddError(sUMVAssociationEndCantStore, [mapper, associationEnd.name, association.name], associationEnd);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ValidateFeature(TBoldUMLObject feature)
    {
        if (feature.name == "")
        {
            AddError(sUMVMemberNameEmpty, [feature.owner?.name], feature);
        }
        else
        {
            ValidateNames(feature, feature.owner?.name + "." + feature.name);
        }
    }

    /// <summary>The element's source, expression and database names: there, valid, and not reserved words.</summary>
    [Delphi(PortStatus.Ported)]
    private void ValidateNames(TBoldUMLObject Element, string ElementName)
    {
        if (Element.MetaClass == "Model")
        {
            if (Element.GetBoldTV(TAG_UNITNAME) == "")
            {
                AddError(sUMVUnitNameEmpty, [ElementName], Element);
            }

            return;
        }

        string s;
        if (Language == TBoldModelValidatorSourceLanguage.mvslDelphi)
        {
            s = ExpandedSourceName(Element);
            if (s == "")
            {
                AddError(sUMVDelphiNameEmpty, [ElementName], Element);
            }
            else if (!IsValidDelphiIdentifier(s))
            {
                AddError(sUMVDelphiNameInvalid, [s, ElementName], Element);
            }
            else if (IsDelphiReservedWord(s))
            {
                AddError(sUMVDelphiNameReserved, [s, ElementName], Element);
            }
        }
        else if (Language == TBoldModelValidatorSourceLanguage.mvslCpp)
        {
            s = ExpandedSourceName(Element);
            if (s == "")
            {
                AddError(sUMVCppNameEmpty, [ElementName], Element);
            }
            else if (!IsValidCppIdentifier(s))
            {
                AddError(sUMVCppNameInvalid, [s, ElementName], Element);
            }
            else if (IsCppReservedWord(s))
            {
                AddError(sUMVCppNameReserved, [s, ElementName], Element);
            }
        }

        s = ExpandedExpressionName(Element);
        if (s == "")
        {
            AddError(sUMVExpressionNameEmpty, [ElementName], Element);
        }
        else if (!IsValidOCLIdentifier(s))
        {
            AddError(sUMVExpressionNameInvalid, [s, ElementName], Element);
        }
        else if (IsOCLReservedWord(s))
        {
            AddError(sUMVExpressionNameOCLReserved, [s, ElementName], Element);
        }

        bool stored = Element.MetaClass switch
        {
            "Class" => Element.persistent,
            "Attribute" => Element.EffectivePersistent,
            "AssociationEnd" => Element.association!.EffectivePersistent && !Element.multi && TVIsTrue(Element.GetBoldTV(TAG_EMBED)),
            _ => false,
        };
        stored = stored && Element.GetBoldTV(TAG_STORAGE) != TV_STORAGE_EXTERNAL;
        if (stored && CheckDBStuff())
        {
            bool isClass = Element.MetaClass == "Class";
            s = ExpandedDBName(Element);
            if (s == "")
            {
                AddError(isClass ? sUMVSQLTableNameEmpty : sUMVSQLColumnNameEmpty, [ElementName], Element);
            }
            else if (CheckAnsiSQLStuff() && !IsValidSQLIdentifier(s))
            {
                AddError(isClass ? sUMVTableNameInvalid : sUMVColumnNameInvalid, [s, ElementName], Element);
            }
            else if (CheckAnsiSQLStuff() && IsSQLReservedWord(s))
            {
                AddError(isClass ? sUMVTableNameSQLReserved : sUMVColumnNameSQLReserved, [s, ElementName], Element);
            }
            else if (IsBoldReservedWord(s))
            {
                AddError(isClass ? sUMVBoldTableReservedWord : sUMVBoldColumnReservedWord, [s, ElementName], Element);
            }
        }
    }

    /// <summary>Database names are checked when there is a database configuration.</summary>
    [Delphi(PortStatus.Ported)]
    private bool CheckDBStuff() => SQLDataBaseConfig is not null;

    [Delphi(PortStatus.Ported)]
    private bool CheckAnsiSQLStuff() => CheckDBStuff();

    [Delphi(PortStatus.Ported)]
    private TBoldNationalCharConversion NationalCharConversion() =>
        TBoldTaggedValueSupport.StringToNationalCharConversion(UMLModel.GetBoldTV(TAG_NATIONALCHARCONVERSION));

    [Delphi(PortStatus.Ported)]
    private bool IsSQLReservedWord(string S)
    {
        if (S.Length == 0)
        {
            return false;
        }

        TStringList list = SQLReservedWordList;
        for (int i = 0; i < list.Count; i++)
        {
            if (string.Equals(S, list[i], StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    private TStringList GetSQLReservedWordList()
    {
        if (fSQLReservedWordList is null)
        {
            fSQLReservedWordList = new TStringList { Text = SQLDataBaseConfig!.ReservedWords.Text.Replace(",", "\n") };
            for (int i = 0; i < fSQLReservedWordList.Count; i++)
            {
                fSQLReservedWordList[i] = fSQLReservedWordList[i].Trim();
            }
        }

        return fSQLReservedWordList;
    }

    [Delphi(PortStatus.Ported)] private TStringList SQLReservedWordList => GetSQLReservedWordList();
    [Delphi(PortStatus.Ported)] public TBoldTypeNameDictionary? TypeNameDictionary => fTypeNameDictionary;
}
