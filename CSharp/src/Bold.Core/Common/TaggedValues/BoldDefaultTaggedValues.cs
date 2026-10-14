// Delphi: Source/Common/TaggedValues/BoldDefaultTaggedValues.pas
// Port map: CSharp/port/units/BoldDefaultTaggedValues.md

namespace Bold;

/// <summary>Bold's tagged value names, their values and the default tagged values of each UML element kind.</summary>
public static partial class BoldDefaultTaggedValues
{
    [Delphi(PortStatus.Ported)] public const string BOLDTVREV_MAJOR = "6";
    [Delphi(PortStatus.Ported)] public const string BOLDTVREV_MINOR = "4";
    [Delphi(PortStatus.Ported)] public const string BOLDTVREV = "6.4";
    [Delphi(PortStatus.Ported)] public const string TV_TRUE = "True";
    [Delphi(PortStatus.Ported)] public const string TV_FALSE = "False";
    [Delphi(PortStatus.Ported)] public const string TV_NAME = "<Name>";
    [Delphi(PortStatus.Ported)] public const string TV_NAME_UPPERCASE = "<NAME>";
    [Delphi(PortStatus.Ported)] public const int TV_NAME_Length = 6;
    [Delphi(PortStatus.Ported)] public const string DEFAULTVALUE = "default";
    [Delphi(PortStatus.Ported)] public const string TAG_ATTRIBUTEKIND = "AttributeKind";
    [Delphi(PortStatus.Ported)] public const string TV_ATTRIBUTEKIND_BOLD = "Bold";
    [Delphi(PortStatus.Ported)] public const string TV_ATTRIBUTEKIND_DELPHI = "Delphi";
    [Delphi(PortStatus.Ported)] public const string TAG_DELETEACTION = "DeleteAction";
    [Delphi(PortStatus.Ported)] public const string TV_DELETEACTION_DEFAULT = BoldDefs.DEFAULTNAMELITERAL;
    [Delphi(PortStatus.Ported)] public const string TV_DELETEACTION_ALLOW = "Allow";
    [Delphi(PortStatus.Ported)] public const string TV_DELETEACTION_PROHIBIT = "Prohibit";
    [Delphi(PortStatus.Ported)] public const string TV_DELETEACTION_CASCADE = "Cascade";
    [Delphi(PortStatus.Ported)] public const string TAG_DPREAD = "DelphiPropertyRead";
    [Delphi(PortStatus.Ported)] public const string TAG_DPWRITE = "DelphiPropertyWrite";
    [Delphi(PortStatus.Ported)] public const string TV_DPNONE = "None";
    [Delphi(PortStatus.Ported)] public const string TV_DPFIELD = "Field";
    [Delphi(PortStatus.Ported)] public const string TV_DPPRIVATEMETHOD = "PrivateMethod";
    [Delphi(PortStatus.Ported)] public const string TV_DPPROTECTEDVIRTUALMETHOD = "ProtectedVirtualMethod";
    [Delphi(PortStatus.Ported)] public const string TAG_EVOLUTIONSTATE = "EvolutionState";
    [Delphi(PortStatus.Ported)] public const string TV_EVOLUTIONSTATE_NORMAL = "Normal";
    [Delphi(PortStatus.Ported)] public const string TV_EVOLUTIONSTATE_TOBEREMOVED = "ToBeRemoved";
    [Delphi(PortStatus.Ported)] public const string TV_EVOLUTIONSTATE_REMOVED = "Removed";
    [Delphi(PortStatus.Ported)] public const string TAG_NATIONALCHARCONVERSION = "NationalCharConversion";
    [Delphi(PortStatus.Ported)] public const string TV_NATIONALCHARCONVERSION_DEFAULT = BoldDefs.DEFAULTNAMELITERAL;
    [Delphi(PortStatus.Ported)] public const string TV_NATIONALCHARCONVERSION_TRUE = "True";
    [Delphi(PortStatus.Ported)] public const string TV_NATIONALCHARCONVERSION_FALSE = "False";
    [Delphi(PortStatus.Ported)] public const string TAG_DELPHIOPERATIONKIND = "OperationKind";
    [Delphi(PortStatus.Ported)] public const string TV_DELPHIOPERATIONKIND_NORMAL = "Common";
    [Delphi(PortStatus.Ported)] public const string TV_DELPHIOPERATIONKIND_VIRTUAL = "Virtual";
    [Delphi(PortStatus.Ported)] public const string TV_DELPHIOPERATIONKIND_OVERRIDE = "Override";
    [Delphi(PortStatus.Ported)] public const string TV_DELPHIOPERATIONKIND_DYNAMIC = "Dynamic";
    [Delphi(PortStatus.Ported)] public const string TV_DELPHIOPERATIONKIND_ABSTRACTVIRTUAL = "Abstract";
    [Delphi(PortStatus.Ported)] public const string TAG_OPTIMISTICLOCKING = "OptimisticLocking";
    [Delphi(PortStatus.Ported)] public const string TV_OPTIMISTICLOCKING_DEFAULT = BoldDefs.DEFAULTNAMELITERAL;
    [Delphi(PortStatus.Ported)] public const string TV_OPTIMISTICLOCKING_OFF = "Off";
    [Delphi(PortStatus.Ported)] public const string TV_OPTIMISTICLOCKING_MODIFIEDMEMBERS = "ModifiedMembers";
    [Delphi(PortStatus.Ported)] public const string TV_OPTIMISTICLOCKING_ALLMEMBERS = "AllMembers";
    [Delphi(PortStatus.Ported)] public const string TV_OPTIMISTICLOCKING_TIMESTAMP = "TimeStamp";
    [Delphi(PortStatus.Ported)] public const string TV_OPTIMISTICLOCKING_MODIFIEDMEMBERS_OLDNAME = "Member";
    [Delphi(PortStatus.Ported)] public const string TV_OPTIMISTICLOCKING_ALLMEMBERS_OLDNAME = "Class";
    [Delphi(PortStatus.Ported)] public const string TAG_TABLEMAPPING = "TableMapping";
    [Delphi(PortStatus.Ported)] public const string TV_TABLEMAPPING_OWN = "Own";
    [Delphi(PortStatus.Ported)] public const string TV_TABLEMAPPING_PARENT = "Parent";
    [Delphi(PortStatus.Ported)] public const string TV_TABLEMAPPING_CHILDREN = "Children";
    [Delphi(PortStatus.Ported)] public const string TV_TABLEMAPPING_IMPORTED = "Imported";
    [Delphi(PortStatus.Ported)] public const string DEFAULTTABLEMAPPINGSTRING = "Own";
    [Delphi(PortStatus.Ported)] public const string TAG_STORAGE = "Storage";
    [Delphi(PortStatus.Ported)] public const string ENUM_TAG_CLASS_STORAGE = "ClassStorageEnum";
    [Delphi(PortStatus.Ported)] public const string ENUM_TAG_ATTRIBUTE_STORAGE = "AttributeStorageEnum";
    [Delphi(PortStatus.Ported)] public const string ENUM_TAG_ASSOCIATION_STORAGE = "AssociationStorageEnum";
    [Delphi(PortStatus.Ported)] public const string TV_STORAGE_INTERNAL = "Internal";
    [Delphi(PortStatus.Ported)] public const string TV_STORAGE_PARTIALLYEXTERNAL = "PartiallyExternal";
    [Delphi(PortStatus.Ported)] public const string TV_STORAGE_EXTERNAL = "External";
    [Delphi(PortStatus.Ported)] public const string TV_STORAGE_EXTERNALKEY = "ExternalKey";
    [Delphi(PortStatus.Ported)] public const string TV_PERSISTENT_OLD = "Persistent";
    [Delphi(PortStatus.Ported)] public const string TAG_ALLOWNULL = "AllowNULL";
    [Delphi(PortStatus.Ported)] public const string TAG_COLUMNNAME = "ColumnName";
    [Delphi(PortStatus.Ported)] public const string TAG_COPYRIGHTNOTICE = "CopyrightNotice";
    [Delphi(PortStatus.Ported)] public const string TAG_DEFAULTDBVALUE = "DefaultDBValue";
    [Delphi(PortStatus.Ported)] public const string TAG_DEFAULTSUPERCLASS = "DefaultSuperClass";
    [Delphi(PortStatus.Ported)] public const string TAG_DEFAULTLINKCLASSSUPERCLASS = "DefaultLinkClassSuperClass";
    [Delphi(PortStatus.Ported)] public const string TAG_DEFAULTSTRINGREPRESENTATION = "DefaultStringRepresentation";
    [Delphi(PortStatus.Ported)] public const string TAG_DELAYEDFETCH = "DelayedFetch";
    [Delphi(PortStatus.Ported)] public const string TAG_DELPHIFIELD = "DelphiField";
    [Delphi(PortStatus.Ported)] public const string TAG_DELPHINAME = "DelphiName";
    [Delphi(PortStatus.Ported)] public const string TAG_CPPNAME = "CPPName";
    [Delphi(PortStatus.Ported)] public const string TAG_DERIVATIONEXPRESSIONS = "DerivationExpressions";
    [Delphi(PortStatus.Ported)] public const string TAG_DERIVATIONOCL = "DerivationOCL";
    [Delphi(PortStatus.Ported)] public const string TAG_EMBED = "Embed";
    [Delphi(PortStatus.Ported)] public const string TAG_EXPRESSIONNAME = "ExpressionName";
    [Delphi(PortStatus.Ported)] public const string TAG_FORMERNAMES = "FormerNames";
    [Delphi(PortStatus.Ported)] public const string TAG_GENERATEMULTIPLICITYCONSTRAINTS = "GenerateMultiplicityConstraints";
    [Delphi(PortStatus.Ported)] public const string TAG_GUID = "GUID";
    [Delphi(PortStatus.Ported)] public const string TAG_IMPORTED = "Imported";
    [Delphi(PortStatus.Ported)] public const string TAG_IMPLEMENTATIONUSES = "ImplementationUses";
    [Delphi(PortStatus.Ported)] public const string TAG_INCFILENAME = "FileName";
    [Delphi(PortStatus.Ported)] public const string TAG_INTERFACENAME = "InterfaceName";
    [Delphi(PortStatus.Ported)] public const string TAG_INTERFACEUSES = "InterfaceUses";
    [Delphi(PortStatus.Ported)] public const string TAG_ISCONST = "IsConst";
    [Delphi(PortStatus.Ported)] public const string TAG_LENGTH = "Length";
    [Delphi(PortStatus.Ported)] public const string TAG_LINKCLASSNAME = "LinkClassName";
    [Delphi(PortStatus.Ported)] public const string TAG_LISTGUID = "LISTGUID";
    [Delphi(PortStatus.Ported)] public const string TAG_MODELVERSION = "ModelVersion";
    [Delphi(PortStatus.Ported)] public const string TAG_ORDERED = "Ordered";
    [Delphi(PortStatus.Ported)] public const string TAG_OVERRIDEINALLSUBCLASSES = "OverrideInAllSubclasses";
    [Delphi(PortStatus.Ported)] public const string TAG_PMAPPERNAME = "PMapper";
    [Delphi(PortStatus.Ported)] public const string TAG_REVERSEDERIVE = "ReverseDerive";
    [Delphi(PortStatus.Ported)] public const string TAG_ROOTCLASS = "RootClass";
    [Delphi(PortStatus.Ported)] public const string TAG_TABLENAME = "TableName";
    [Delphi(PortStatus.Ported)] public const string TAG_TYPELIBVERSION = "TypeLibVersion";
    [Delphi(PortStatus.Ported)] public const string TAG_UNITNAME = "UnitName";
    [Delphi(PortStatus.Ported)] public const string TAG_UPDATEWHOLEOBJECTS = "UpdateWholeObjects";
    [Delphi(PortStatus.Ported)] public const string TAG_USECLOCKLOG = "UseClockLog";
    [Delphi(PortStatus.Ported)] public const string TAG_USEGLOBALID = "UseGlobalId";
    [Delphi(PortStatus.Ported)] public const string TAG_USEMODELVERSION = "UseModelVersion";
    [Delphi(PortStatus.Ported)] public const string TAG_USEREADONLY = "UseReadOnly";
    [Delphi(PortStatus.Ported)] public const string TAG_USETIMESTAMP = "UseTimestamp";
    [Delphi(PortStatus.Ported)] public const string TAG_USEXFILES = "UseXFiles";
    [Delphi(PortStatus.Ported)] public const string TAG_VERSIONED = "Versioned";
    [Delphi(PortStatus.Ported)] public const string TAG_VIRTUALDERIVE = "VirtualDerive";
    [Delphi(PortStatus.Ported)] public const string TAG_REGIONDEFINITIONS = "RegionDefinitions";
    [Delphi(PortStatus.Ported)] public const string TAG_GENERATEDEFAULTREGIONS = "GenerateDefaultRegions";
    [Delphi(PortStatus.Ported)] public const string TAG_GENERATEDEFAULTREGION_CLASS = "GenerateDefaultRegion";
    [Delphi(PortStatus.Ported)] public const string TAG_DEFAULTREGIONMODE_ASSOCIATIONEND = "DefaultRegionMode";
    [Delphi(PortStatus.Ported)] public const string TV_DEFAULTREGIONMODE_ASSOCIATIONEND_DEFAULT = "Default";
    [Delphi(PortStatus.Ported)] public const string TV_DEFAULTREGIONMODE_ASSOCIATIONEND_NONE = "None";
    [Delphi(PortStatus.Ported)] public const string TV_DEFAULTREGIONMODE_ASSOCIATIONEND_EXISTENCE = "Existence";
    [Delphi(PortStatus.Ported)] public const string TV_DEFAULTREGIONMODE_ASSOCIATIONEND_INDEPENDENTCASCADE = "IndependentCascade";
    [Delphi(PortStatus.Ported)] public const string TV_DEFAULTREGIONMODE_ASSOCIATIONEND_CASCADE = "Cascade";
    [Delphi(PortStatus.Ported)] public const string BOLDINTERALTVPREFIX = "_BoldInternal.";
    [Delphi(PortStatus.Ported)] public const string TV_MODELERRORS = "ModelErrors";
    [Delphi(PortStatus.Ported)] public const string BOLDBOLDIFYPREFIX = "_Boldify.";
    [Delphi(PortStatus.Ported)] public const string TAG_BOLDIFIED = "boldified";
    [Delphi(PortStatus.Ported)] public const string TAG_AUTOCREATED = "autoCreated";
    [Delphi(PortStatus.Ported)] public const string TAG_DEFAULTMULTIPLICITY = "defaultMultiplicity";
    [Delphi(PortStatus.Ported)] public const string TAG_NONAME = "noName";
    [Delphi(PortStatus.Ported)] public const string TAG_WASEMBEDED = "wasEmbeded";
    [Delphi(PortStatus.Ported)] public const string TAG_WASPERSISTENT = "wasPersistent";
    [Delphi(PortStatus.Ported)] public const string TAG_UNFLATTENEDNAMESPACE = "unflattenedNamespace";

    private static TBoldTaggedValuePerClassList? G_BoldDefaultTaggedValues;

    [Delphi(PortStatus.Ported)]
    private static void AddDefaultTaggedValues()
    {
        TBoldTaggedValueList l = G_BoldDefaultTaggedValues!.ListForClassName("Model");
        l.Add("Boolean", TAG_GENERATEMULTIPLICITYCONSTRAINTS, TV_TRUE);
        l.Add("Text", TAG_INTERFACEUSES, "");
        l.Add("Text", TAG_IMPLEMENTATIONUSES, "");
        l.Add("Text", TAG_COPYRIGHTNOTICE, "");
        l.Add("String", TAG_DEFAULTSUPERCLASS, "");
        l.Add("String", TAG_DEFAULTLINKCLASSSUPERCLASS, "");
        l.Add("String", TAG_UNITNAME, "BusinessClasses");
        l.Add("String", TAG_ROOTCLASS, "");
        l.Add("String", TAG_PMAPPERNAME, BoldDefs.DEFAULTNAMELITERAL);
        l.Add("Boolean", TAG_USEXFILES, TV_TRUE);
        l.Add("Boolean", TAG_USETIMESTAMP, TV_TRUE);
        l.Add("Boolean", TAG_USEGLOBALID, TV_TRUE);
        l.Add("Boolean", TAG_USEREADONLY, TV_TRUE);
        l.Add("Boolean", TAG_USEMODELVERSION, TV_FALSE);
        l.Add("Integer", TAG_MODELVERSION, "0");
        l.Add("Boolean", TAG_USECLOCKLOG, TV_TRUE);
        l.Add("Boolean", TAG_UPDATEWHOLEOBJECTS, TV_FALSE);
        l.Add("OptimisticLockingSet", TAG_OPTIMISTICLOCKING, TV_OPTIMISTICLOCKING_OFF);
        l.Add("NationalCharConversionEnum", TAG_NATIONALCHARCONVERSION, TV_NATIONALCHARCONVERSION_DEFAULT);
        l.Add("String", TAG_GUID, "");
        l.Add("String", TAG_TYPELIBVERSION, "1.0");
        l.Add("Text", TAG_REGIONDEFINITIONS, "");
        l.Add("Boolean", TAG_GENERATEDEFAULTREGIONS, TV_FALSE);

        l = G_BoldDefaultTaggedValues.ListForClassName("Class");
        l.Add("String", TAG_INCFILENAME, "");
        l.Add("String", TAG_UNITNAME, "");
        l.Add("Boolean", TAG_IMPORTED, TV_FALSE);
        l.Add("TableMappingSet", TAG_TABLEMAPPING, TV_TABLEMAPPING_OWN);
        l.Add("String", TAG_DELPHINAME, "T<Name>");
        l.Add("String", TAG_CPPNAME, TV_NAME);
        l.Add("String", TAG_EXPRESSIONNAME, TV_NAME);
        l.Add("String", TAG_TABLENAME, TV_NAME);
        l.Add("EvolutionStateEnum", TAG_EVOLUTIONSTATE, TV_EVOLUTIONSTATE_NORMAL);
        l.Add("String", TAG_PMAPPERNAME, BoldDefs.DEFAULTNAMELITERAL);
        l.Add("String", TAG_DEFAULTSTRINGREPRESENTATION, "");
        l.Add("Text", TAG_DERIVATIONEXPRESSIONS, "");
        l.Add("Boolean", TAG_VERSIONED, TV_FALSE);
        l.Add("OptimisticLockingSet", TAG_OPTIMISTICLOCKING, TV_OPTIMISTICLOCKING_DEFAULT);
        l.Add("Text", TAG_FORMERNAMES, "");
        l.Add("String", TAG_INTERFACENAME, "I<Name>");
        l.Add("String", TAG_GUID, "");
        l.Add("Boolean", TAG_GENERATEDEFAULTREGION_CLASS, TV_TRUE);
        l.Add(ENUM_TAG_CLASS_STORAGE, TAG_STORAGE, TV_STORAGE_INTERNAL);

        l = G_BoldDefaultTaggedValues.ListForClassName("Association");
        l.Add("String", TAG_LINKCLASSNAME, TV_NAME);
        l.Add("Text", TAG_FORMERNAMES, "");
        l.Add("EvolutionStateEnum", TAG_EVOLUTIONSTATE, TV_EVOLUTIONSTATE_NORMAL);
        l.Add(ENUM_TAG_ASSOCIATION_STORAGE, TAG_STORAGE, TV_STORAGE_INTERNAL);

        l = G_BoldDefaultTaggedValues.ListForClassName("Attribute");
        l.Add("Integer", TAG_LENGTH, "255");
        l.Add("Boolean", TAG_ALLOWNULL, TV_FALSE);
        l.Add("Boolean", TAG_DELAYEDFETCH, TV_FALSE);
        l.Add("String", TAG_COLUMNNAME, TV_NAME);
        l.Add("String", TAG_EXPRESSIONNAME, TV_NAME);
        l.Add("String", TAG_DELPHINAME, TV_NAME);
        l.Add("String", TAG_CPPNAME, TV_NAME);
        l.Add("String", TAG_PMAPPERNAME, BoldDefs.DEFAULTNAMELITERAL);
        l.Add("Text", TAG_DERIVATIONOCL, "");
        l.Add("Boolean", TAG_VIRTUALDERIVE, TV_TRUE);
        l.Add("Boolean", TAG_REVERSEDERIVE, TV_FALSE);
        l.Add("AttributeKindSet", TAG_ATTRIBUTEKIND, TV_ATTRIBUTEKIND_BOLD);
        l.Add("Boolean", TAG_DELPHIFIELD, TV_FALSE);
        l.Add("DelphiPropertySet", TAG_DPREAD, TV_DPNONE);
        l.Add("DelphiPropertySet", TAG_DPWRITE, TV_DPNONE);
        l.Add("EvolutionStateEnum", TAG_EVOLUTIONSTATE, TV_EVOLUTIONSTATE_NORMAL);
        l.Add("Text", TAG_FORMERNAMES, "");
        l.Add("String", TAG_DEFAULTDBVALUE, "");
        l.Add(ENUM_TAG_ATTRIBUTE_STORAGE, TAG_STORAGE, TV_STORAGE_INTERNAL);

        l = G_BoldDefaultTaggedValues.ListForClassName("AssociationEnd");
        l.Add("Boolean", TAG_ORDERED, TV_FALSE);
        l.Add("String", TAG_COLUMNNAME, TV_NAME);
        l.Add("String", TAG_EXPRESSIONNAME, TV_NAME);
        l.Add("String", TAG_DELPHINAME, TV_NAME);
        l.Add("String", TAG_CPPNAME, TV_NAME);
        l.Add("Boolean", TAG_EMBED, TV_TRUE);
        l.Add("Text", TAG_DERIVATIONOCL, "");
        l.Add("Boolean", TAG_VIRTUALDERIVE, TV_TRUE);
        l.Add("DeleteActions", TAG_DELETEACTION, TV_DELETEACTION_DEFAULT);
        l.Add("Text", TAG_FORMERNAMES, "");
        l.Add("DefaultRegionModeAssociationEnum", TAG_DEFAULTREGIONMODE_ASSOCIATIONEND, TV_DEFAULTREGIONMODE_ASSOCIATIONEND_DEFAULT);

        l = G_BoldDefaultTaggedValues.ListForClassName("Operation");
        l.Add("String", TAG_DELPHINAME, TV_NAME);
        l.Add("String", TAG_CPPNAME, TV_NAME);
        l.Add("String", TAG_EXPRESSIONNAME, TV_NAME);
        l.Add("BoldOperationKindSet", TAG_DELPHIOPERATIONKIND, TV_DELPHIOPERATIONKIND_NORMAL);
        l.Add("Boolean", TAG_OVERRIDEINALLSUBCLASSES, TV_FALSE);

        l = G_BoldDefaultTaggedValues.ListForClassName("Parameter");
        l.Add("Boolean", TAG_ISCONST, TV_FALSE);
    }

    /// <summary>The default tagged values per UML element kind (Model, Class, Attribute, ...), created on first use.</summary>
    [Delphi(PortStatus.Ported)]
    public static TBoldTaggedValuePerClassList BoldDefaultTaggedValueList()
    {
        if (G_BoldDefaultTaggedValues is null)
        {
            G_BoldDefaultTaggedValues = new TBoldTaggedValuePerClassList();
            AddDefaultTaggedValues();
        }

        return G_BoldDefaultTaggedValues;
    }

    [Delphi(PortStatus.Ported)]
    public static bool TVIsTrue(string value) => SysUtils.SameText(value, TV_TRUE);

    [Delphi(PortStatus.Ported)]
    public static bool TVIsFalse(string value) => SysUtils.SameText(value, TV_FALSE);
}
