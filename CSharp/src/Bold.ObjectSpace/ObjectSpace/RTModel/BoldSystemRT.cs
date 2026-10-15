// Delphi: Source/ObjectSpace/RTModel/BoldSystemRT.pas
// Port map: CSharp/port/units/BoldSystemRT.md

namespace Bold;

/// <summary>An evaluator that can also find the member an OCL expression navigates to.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldRTEvaluator : TBoldEvaluator
{
    [Delphi(PortStatus.Ported)]
    public abstract TBoldMemberRTInfo? RTInfo(string Ocl, TBoldElementTypeInfo? Context, bool ReRaise);
}

/// <summary>Class type infos in topological order, also indexed by object class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClassTypeInfoList : TBoldElementTypeInfoList
{
    private static int IX_ObjectClass = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldClassTypeInfoList()
    {
        SetIndexCapacity(4);
        BoldIndexableList.SetIndexVariable(ref IX_ObjectClass, AddIndex(new TObjectClassIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private new TBoldClassTypeInfo GetItem(int index) => (TBoldClassTypeInfo)base[index];

    [Delphi(PortStatus.Ported)]
    private new TBoldClassTypeInfo? GetItemByExpressionName(string ExpressionName) => (TBoldClassTypeInfo?)base.ItemsByExpressionName(ExpressionName);

    [Delphi(PortStatus.Ported)]
    private new TBoldClassTypeInfo? GetItemByModelName(string ModelName) => (TBoldClassTypeInfo?)base.ItemsByModelName(ModelName);

    [Delphi(PortStatus.Ported)]
    private TBoldClassTypeInfo? GetItemByObjectClass(TClass ObjectClass) =>
        (TBoldClassTypeInfo?)((TObjectClassIndex)Indexes(IX_ObjectClass)!).FindByObjectClass(ObjectClass);

    [Delphi(PortStatus.Ported, Symbol = "TBoldClassTypeInfoList.Items")]
    public new TBoldClassTypeInfo this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)] public new TBoldClassTypeInfo? ItemsByExpressionName(string ExpressionName) => GetItemByExpressionName(ExpressionName);
    [Delphi(PortStatus.Ported)] public new TBoldClassTypeInfo? ItemsByModelName(string ModelName) => GetItemByModelName(ModelName);
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo? ItemsByObjectClass(TClass ObjectClass) => GetItemByObjectClass(ObjectClass);

    [Delphi(PortStatus.Ported, Symbol = "TObjectClassIndex")]
    private sealed class TObjectClassIndex : TBoldClassHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TObjectClassIndex.ItemAsKeyClass")]
        protected override TClass ItemAsKeyClass(object? O) => ((TBoldClassTypeInfo)O!).ObjectClass!;

        /// <summary>FindByClass, reachable from the list.</summary>
        internal object? FindByObjectClass(TClass KeyClass) => FindByClass(KeyClass);
    }
}

/// <summary>The members of a class, by index and name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberRTInfoList : TBoldMetaElementList
{
    [Delphi(PortStatus.Ported)]
    private new TBoldMemberRTInfo GetItem(int index) => (TBoldMemberRTInfo)base.GetItem(index);

    [Delphi(PortStatus.Ported)]
    private new TBoldMemberRTInfo? GetItemByExpressionName(string ExpressionName) => (TBoldMemberRTInfo?)base.GetItemByExpressionName(ExpressionName);

    [Delphi(PortStatus.Ported)]
    private new TBoldMemberRTInfo? GetItemByModelName(string ModelName) => (TBoldMemberRTInfo?)base.GetItemByModelName(ModelName);

    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberRTInfoList.Items")]
    public new TBoldMemberRTInfo this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)] public new TBoldMemberRTInfo? ItemsByExpressionName(string ExpressionName) => GetItemByExpressionName(ExpressionName);
    [Delphi(PortStatus.Ported)] public new TBoldMemberRTInfo? ItemsByModelName(string ModelName) => GetItemByModelName(ModelName);
}

/// <summary>The constraints of a model element, by model name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldConstraintRTInfoList : TBoldMetaElementList
{
    [Delphi(PortStatus.Ported)]
    private new TBoldConstraintRTInfo GetItem(int index) => (TBoldConstraintRTInfo)base.GetItem(index);

    [Delphi(PortStatus.Ported)]
    private new TBoldConstraintRTInfo? GetItemByModelName(string ModelName) => (TBoldConstraintRTInfo?)base.GetItemByModelName(ModelName);

    [Delphi(PortStatus.Ported, Symbol = "TBoldConstraintRTInfoList.Items")]
    public new TBoldConstraintRTInfo this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)] public new TBoldConstraintRTInfo? ItemsByModelName(string ModelName) => GetItemByModelName(ModelName);
}

/// <summary>The methods of a class, by index and name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMethodRTInfoList : TBoldMetaElementList
{
    [Delphi(PortStatus.Ported)]
    private new TBoldMethodRTInfo GetItem(int index) => (TBoldMethodRTInfo)base.GetItem(index);

    [Delphi(PortStatus.Ported)]
    private new TBoldMethodRTInfo? GetItemByModelName(string ModelName) => (TBoldMethodRTInfo?)base.GetItemByModelName(ModelName);

    [Delphi(PortStatus.Ported)]
    private new TBoldMethodRTInfo? GetItemByExpressionName(string ExpressionName) => (TBoldMethodRTInfo?)base.GetItemByExpressionName(ExpressionName);

    [Delphi(PortStatus.Ported, Symbol = "TBoldMethodRTInfoList.Items")]
    public new TBoldMethodRTInfo this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)] public new TBoldMethodRTInfo? ItemsByModelName(string ModelName) => GetItemByModelName(ModelName);
    [Delphi(PortStatus.Ported)] public new TBoldMethodRTInfo? ItemsByExpressionName(string ExpressionName) => GetItemByExpressionName(ExpressionName);
}

/// <summary>List types, also indexed by their element type.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListTypeInfoList : TBoldElementTypeInfoList
{
    private static int IX_Element = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldListTypeInfoList()
    {
        SetIndexCapacity(4);
        BoldIndexableList.SetIndexVariable(ref IX_Element, AddIndex(new TElementIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldListTypeInfo? GetItemByElement(TBoldElementTypeInfo? Element) =>
        Element is null ? null : (TBoldListTypeInfo?)((TElementIndex)Indexes(IX_Element)!).FindByElement(Element);

    /// <summary>The list type of <paramref name="Element"/>; null for the untyped list, which has no element type.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldListTypeInfo? ItemByElement(TBoldElementTypeInfo? Element) => GetItemByElement(Element);

    [Delphi(PortStatus.Ported, Symbol = "TElementIndex")]
    private sealed class TElementIndex : TBoldObjectHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TElementIndex.ItemAsKeyObject")]
        protected override object? ItemAsKeyObject(object? Item) => ((TBoldListTypeInfo)Item!).ListElementTypeInfo;

        /// <summary>FindByObject, reachable from the list.</summary>
        internal object? FindByElement(object KeyObject) => FindByObject(KeyObject);
    }
}

/// <summary>The type of types (MetaType): what a type expression in OCL evaluates to.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTypeTypeInfo : TBoldElementTypeInfo
{
    [Delphi(PortStatus.Ported)]
    protected internal TBoldTypeTypeInfo(string ModelName, string ExpressionName, string Delphiname, TBoldElementTypeInfo? ModelTypeInfo)
        : base(ModelName, ExpressionName, Delphiname, ModelTypeInfo)
    {
        SetValueType(TBoldValueTypeSet.bvtType);
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => this;

    [Delphi(PortStatus.Ported)]
    public override bool ConformsTo(TBoldElementTypeInfo? CompareElement) => CompareElement == this;
}

/// <summary>A type with the constraints and the non-Bold tagged values of its model element.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldElementTypeInfoWithConstraint : TBoldElementTypeInfo
{
    private TBoldConstraintRTInfoList? fConstraints;
    private TStrings? fTaggedValues;

    /// <summary>
    /// Copies the tagged values and creates the constraints of <paramref name="MoldElement"/> (none without one). The system
    /// type passes null for <paramref name="SystemTypeInfo"/>, as Delphi passes itself: the type becomes its own system type.
    /// </summary>
    [Delphi(PortStatus.Ported, Note = "A null SystemTypeInfo means this type, since C# cannot pass this to a base constructor.")]
    protected TBoldElementTypeInfoWithConstraint(TMoldElement? MoldElement, TBoldSystemTypeInfo? SystemTypeInfo)
        : base(MoldElement?.Name ?? "", MoldElement?.ExpandedExpressionName ?? "", MoldElement?.ExpandedDelphiName ?? "", SystemTypeInfo)
    {
        TBoldSystemTypeInfo system = SystemTypeInfo ?? (TBoldSystemTypeInfo)this;
        if (SystemTypeInfo is null)
        {
            SetSystemTypeInfo(system);
        }

        if (MoldElement is not null)
        {
            fTaggedValues = MoldElement.GetAllNonBoldTaggedValues();
            for (int i = 0; i < MoldElement.Constraints.Count; i++)
            {
                string constraintName = MoldElement.Constraints.Names(i);
                AddConstraint(new TBoldConstraintRTInfo(MoldElement, constraintName, "", "", system,
                    MoldElement.Constraints.Values(constraintName), ""));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fConstraints);
        fTaggedValues = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private string GetTaggedValues(string Tag) => fTaggedValues?.Values(Tag) ?? "";

    [Delphi(PortStatus.Ported)]
    private TBoldConstraintRTInfo? GetConstraints(string Name) => fConstraints?.ItemsByModelName(Name);

    [Delphi(PortStatus.Ported)]
    private int GetConstraintCount() => fConstraints?.Count ?? 0;

    [Delphi(PortStatus.Ported)]
    private int GetTaggedvalueCount() => fTaggedValues?.Count ?? 0;

    [Delphi(PortStatus.Ported)]
    private TBoldConstraintRTInfo? GetConstraintByIndex(int Index) => fConstraints?[Index];

    [Delphi(PortStatus.Ported)]
    private string GetTaggedValueByIndex(int Index) => fTaggedValues is not null ? fTaggedValues[Index] : "";

    /// <summary>Adds a constraint; the type owns it.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddConstraint(TBoldConstraintRTInfo Constraint)
    {
        fConstraints ??= new TBoldConstraintRTInfoList { OwnsEntries = true };
        fConstraints.Add(Constraint);
    }

    [Delphi(PortStatus.Ported)] public TBoldConstraintRTInfo? Constraint(string Name) => GetConstraints(Name);
    [Delphi(PortStatus.Ported)] public int ConstraintCount => GetConstraintCount();
    [Delphi(PortStatus.Ported)] public TBoldConstraintRTInfo? ConstraintByIndex(int Index) => GetConstraintByIndex(Index);
    [Delphi(PortStatus.Ported)] public string TaggedValues(string Tag) => GetTaggedValues(Tag);
    [Delphi(PortStatus.Ported)] public int TaggedValueCount => GetTaggedvalueCount();
    [Delphi(PortStatus.Ported)] public string TaggedValueByIndex(int Index) => GetTaggedValueByIndex(Index);
}

/// <summary>A meta element (member, method, constraint) with the constraints and non-Bold tagged values of its model element.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMetaElementWithConstraint : TBoldMetaElement
{
    private TBoldConstraintRTInfoList? fConstraints;
    private TStrings? fTaggedValues;

    /// <summary>Copies the tagged values and creates the constraints of <paramref name="MoldElement"/>, if any.</summary>
    [Delphi(PortStatus.Ported)]
    protected TBoldMetaElementWithConstraint(TMoldElement? MoldElement, string ModelName, string ExpressionName, string DelphiName,
        TBoldSystemTypeInfo SystemTypeInfo)
        : base(ModelName, ExpressionName, DelphiName)
    {
        if (MoldElement is not null)
        {
            fTaggedValues = MoldElement.GetAllNonBoldTaggedValues();
            for (int i = 0; i < MoldElement.Constraints.Count; i++)
            {
                string ConstraintName = MoldElement.Constraints.Names(i);
                AddConstraint(new TBoldConstraintRTInfo(null, ConstraintName, "", "", SystemTypeInfo,
                    MoldElement.Constraints.Values(ConstraintName), ""));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fConstraints);
        fTaggedValues = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private string GetTaggedValues(string Tag) => fTaggedValues?.Values(Tag) ?? "";

    [Delphi(PortStatus.Ported)]
    private TBoldConstraintRTInfo? GetConstraints(string Name) => fConstraints?.ItemsByModelName(Name);

    [Delphi(PortStatus.Ported)]
    private TBoldConstraintRTInfo? GetConstraintByIndex(int Index) => fConstraints?[Index];

    [Delphi(PortStatus.Ported)]
    private int GetConstraintCount() => fConstraints?.Count ?? 0;

    [Delphi(PortStatus.Ported)]
    private string GetTaggedValueByIndex(int Index) => fTaggedValues is not null ? fTaggedValues[Index] : "";

    [Delphi(PortStatus.Ported)]
    private int GetTaggedvalueCount() => fTaggedValues?.Count ?? 0;

    [Delphi(PortStatus.Ported)]
    public void AddConstraint(TBoldConstraintRTInfo Constraint)
    {
        fConstraints ??= new TBoldConstraintRTInfoList { OwnsEntries = true };
        fConstraints.Add(Constraint);
    }

    [Delphi(PortStatus.Ported)] public TBoldConstraintRTInfo? Constraint(string Name) => GetConstraints(Name);
    [Delphi(PortStatus.Ported)] public int ConstraintCount => GetConstraintCount();
    [Delphi(PortStatus.Ported)] public TBoldConstraintRTInfo? ConstraintByIndex(int Index) => GetConstraintByIndex(Index);
    [Delphi(PortStatus.Ported)] public string TaggedValues(string Tag) => GetTaggedValues(Tag);
    [Delphi(PortStatus.Ported)] public int TaggedValueCount => GetTaggedvalueCount();
    [Delphi(PortStatus.Ported)] public string TaggedValueByIndex(int Index) => GetTaggedValueByIndex(Index);
}

/// <summary>
/// The runtime model: built from a Mold model and a type name dictionary, it holds the class, attribute, list and meta
/// types and the members of every class. Problems are logged in <see cref="InitializationLog"/> and make the system not
/// runnable rather than raising.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldSystemTypeInfo : TBoldElementTypeInfoWithConstraint
{
    private TStringList? fInitializationLog;
    private readonly TBoldOptimisticLockingMode fOptimisticLocking;
    private TBoldElementTypeInfoList? fAttributeTypes;
    private TBoldEvaluator? fEvaluator;
    private TBoldListTypeInfoList? fListTypes;
    private readonly bool fMethodsInstalled;
    private TBoldNilTypeInfo? fNilTypeInfo;
    private TBoldClassTypeInfoList? fTopSortedClasses;
    private TBoldTypeTypeInfo? fTypeTypeInfo;
    private readonly bool fUseGeneratedCode;
    private readonly bool fGenerateMultiplicityConstraints;
    private TBoldElementTypeInfoList? fValueTypeNameList;
    private readonly string fStereotype;
    private readonly bool fUseClockLog;

    /// <summary>
    /// Builds the runtime model. With <paramref name="UseGeneratedCode"/> the classes registered for the model in the
    /// generated code dictionary are installed (and their CRC checked with <paramref name="CheckCodeCheckSum"/>).
    /// </summary>
    [Delphi(PortStatus.Partial, Note = "The type list, object list and default object classes come from BoldSystem and BoldTypeList (M3); until they are ported those class references are null.")]
    public TBoldSystemTypeInfo(TMoldModel moldModel, bool UseGeneratedCode, bool CheckCodeCheckSum, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldModel, null)
    {
        fStereotype = moldModel.Stereotype;
        fUseClockLog = BoldUtils.StringToBoolean(moldModel.BoldTVByName(BoldDefaultTaggedValues.TAG_USECLOCKLOG));

        SetElementFlag(BoldElements.befSystemIsRunnable, true);
        fOptimisticLocking = moldModel.OptimisticLocking;
        SetValueType(TBoldValueTypeSet.bvtSystem);
        fTypeTypeInfo = new TBoldTypeTypeInfo("MetaType", "MetaType", "MetaType", this);

        fAttributeTypes = new TBoldElementTypeInfoList();
        fListTypes = new TBoldListTypeInfoList();
        ListTypes.Add(new TBoldListTypeInfo(fTypeTypeInfo, this, BoldSystemRT.TBoldTypeListClass));
        SetElementFlag(BoldElements.befSystemPersistent, true);
        SetElementFlag(BoldElements.befGenerateDefaultRegions, moldModel.GenerateDefaultRegions);
        fTopSortedClasses = new TBoldClassTypeInfoList();

        var BoldObjectListClasses = new TBoldGeneratedClassList();
        var BoldObjectClasses = new TBoldGeneratedClassList();
        fGenerateMultiplicityConstraints = BoldDefaultTaggedValues.TVIsTrue(moldModel.BoldTVByName(BoldDefaultTaggedValues.TAG_GENERATEMULTIPLICITYCONSTRAINTS));

        fUseGeneratedCode = UseGeneratedCode;
        if (UseGeneratedCode)
        {
            TBoldGeneratedCodeList codes = BoldGeneratedCodeDictionary.GeneratedCodes();
            for (int i = 0; i < codes.Count; i++)
            {
                TBoldGeneratedCodeDescriptor entry = codes.ModelEntries(i);
                if (entry.ExpressionName == moldModel.ExpandedExpressionName)
                {
                    entry.InstallBusinessClasses?.Invoke(BoldObjectClasses);
                    entry.InstallObjectListClasses?.Invoke(BoldObjectListClasses);
                    if (CheckCodeCheckSum && entry.CRC != "" && entry.CRC != moldModel.CRC())
                    {
                        InitializationError(BoldCoreConsts.sCRCDiffers, moldModel.CRC(), entry.CRC);
                    }
                }
            }
        }

        for (int i = 0; i < TypeNameDictionary.Count; i++)
        {
            InstallAttributeType(TypeNameDictionary, i);
        }

        // The super list that all other lists conform to (used by OCL); its element type is nil.
        ListTypes.Add(new TBoldListTypeInfo(null, this, BoldSystemRT.TBoldObjectListClass));

        // Superclasses are built first, so the classes come out topologically sorted.
        moldModel.EnsureTopSorted();

        var Errors = new TStringList { CommaText = moldModel.TVByName(BoldDefaultTaggedValues.BOLDINTERALTVPREFIX + BoldDefaultTaggedValues.TV_MODELERRORS) };
        if (Errors.Count > 0)
        {
            for (int i = 0; i < Errors.Count; i++)
            {
                InitializationError(Errors[i]);
            }

            // Install the root class anyway: OCL needs it.
            _ = new TBoldClassTypeInfo(this, moldModel.Classes[0], TypeNameDictionary, BoldObjectClasses, BoldObjectListClasses, true);
            return;
        }

        for (int i = 0; i < moldModel.Classes.Count; i++)
        {
            _ = new TBoldClassTypeInfo(this, moldModel.Classes[i], TypeNameDictionary, BoldObjectClasses, BoldObjectListClasses);
        }

        for (int i = 0; i < moldModel.Associations.Count; i++)
        {
            TBoldRoleRTInfo.SetPass2InfoForAssociation(this, moldModel.Associations[i]);
        }

        if (GenerateMultiplicityConstraints)
        {
            for (int i = 0; i < TopSortedClasses.Count; i++)
            {
                TopSortedClasses[i].InitializeMultiplicityConstraints();
            }
        }

        if (UseGeneratedCode)
        {
            for (int i = 0; i < TopSortedClasses.Count; i++)
            {
                if (SysUtils.CompareText(TopSortedClasses[i].ObjectClass?.Name, TopSortedClasses[i].DelphiName) != 0)
                {
                    InitializationError(BoldCoreConsts.sGeneratedCodeNotRegistered, TopSortedClasses[i].ExpressionName);
                }
            }
        }

        fMethodsInstalled = false;
        fNilTypeInfo = new TBoldNilTypeInfo(this, null, null, null, null);
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fListTypes);
        SysUtils.FreeAndNil(ref fAttributeTypes);
        fEvaluator = null;
        SysUtils.FreeAndNil(ref fTypeTypeInfo);
        SysUtils.FreeAndNil(ref fValueTypeNameList);
        SysUtils.FreeAndNil(ref fTopSortedClasses);
        SysUtils.FreeAndNil(ref fNilTypeInfo);
        fInitializationLog = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private TStringList GetInitializationLog() => fInitializationLog ??= new TStringList();

    [Delphi(PortStatus.Ported)]
    private TBoldAttributeTypeInfo? GetAttributeTypeInfoByDelphiName(string name) => (TBoldAttributeTypeInfo?)fAttributeTypes!.ItemsByDelphiName(name);

    [Delphi(PortStatus.Ported)]
    private TBoldAttributeTypeInfo? GetAttributeTypeInfoByExpressionName(string name) => (TBoldAttributeTypeInfo?)fAttributeTypes!.ItemsByExpressionName(name);

    [Delphi(PortStatus.Ported)]
    private TBoldClassTypeInfo? GetClassTypeInfoByExpressionName(string name) => TopSortedClasses.ItemsByExpressionName(name);

    [Delphi(PortStatus.Ported)]
    private TBoldClassTypeInfo? GetClassTypeInfoByModelName(string name) => TopSortedClasses.ItemsByModelName(name);

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetElementTypeInfoByDelphiName(string name) => ValueTypeNameList.ItemsByDelphiName(name);

    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfo? GetElementTypeInfoByExpressionName(string name) => ValueTypeNameList.ItemsByExpressionName(name);

    [Delphi(PortStatus.Ported)]
    private TBoldListTypeInfo? GetListTypeInfoByElement(TBoldElementTypeInfo? Element) => fListTypes!.ItemByElement(Element);

    [Delphi(PortStatus.Ported)]
    private TBoldClassTypeInfo GetRootClassTypeInfo() => TopSortedClasses[0];

    /// <summary>Every type by name: the system, the classes, the attribute types, the meta type and the list types.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldElementTypeInfoList GetValueTypeNameList()
    {
        if (fValueTypeNameList is null)
        {
            fValueTypeNameList = new TBoldElementTypeInfoList { OwnsEntries = false };
            fValueTypeNameList.Add(this);
            for (int i = 0; i < TopSortedClasses.Count; i++)
            {
                fValueTypeNameList.Add(TopSortedClasses[i]);
            }

            for (int i = 0; i < AttributeTypes.Count; i++)
            {
                fValueTypeNameList.Add(AttributeTypes[i]);
            }

            fValueTypeNameList.Add(BoldType!);
            for (int i = 0; i < ListTypes.Count; i++)
            {
                fValueTypeNameList.Add(ListTypes[i]);
            }
        }

        return fValueTypeNameList;
    }

    /// <summary>Logs the formatted message and marks the system as not runnable.</summary>
    [Delphi(PortStatus.Ported)]
    internal void InitializationError(string Message, params object?[] args)
    {
        InitializationLog.Add(SysUtils.Format(Message, args));
        SetElementFlag(BoldElements.befSystemIsRunnable, false);
    }

    /// <summary>
    /// Installs the attribute type of the mapping at <paramref name="pos"/>: its class comes from the member type
    /// dictionary, and a registered superclass is installed first (added to the dictionary when it has no mapping).
    /// </summary>
    [Delphi(PortStatus.Partial, Note = "Compares the attribute class's parent with TBoldAttribute, which is null until BoldSystem (M3) is ported.")]
    private void InstallAttributeType(TBoldTypeNameDictionary TypeNameDictionary, int pos)
    {
        TBoldTypeNameMapping Mapping = TypeNameDictionary.Mapping(pos);
        TBoldAttributeTypeInfo? TempAttributeType = AttributeTypeInfoByExpressionName(Mapping.ExpressionName);
        if (TempAttributeType is not null)
        {
            if (TempAttributeType.AttributeClass is not null &&
                SysUtils.AnsiCompareText(TempAttributeType.AttributeClass.Name, Mapping.ExpandedDelphiName) != 0)
            {
                InitializationError(BoldCoreConsts.sErrorInstallingAttribute, TypeNameDictionary.Mapping(pos).ExpressionName, TempAttributeType.AttributeClass.Name);
            }

            return;
        }

        TClass? MemberClass;
        TBoldAttributeTypeInfo? SuperType;
        bool IsAbstract;
        TBoldMemberTypeDescriptor? AttributeDescriptor = BoldMemberTypeDictionary.BoldMemberTypes().DescriptorByDelphiName(Mapping.ExpandedDelphiName);
        if (AttributeDescriptor is not null)
        {
            MemberClass = AttributeDescriptor.MemberClass;
            if (MemberClass.BaseType != BoldSystemRT.TBoldAttributeClass)
            {
                string SuperClassName = MemberClass.BaseType!.Name;
                SuperType = AttributeTypeInfoByDelphiName(SuperClassName);
                if (SuperType is null)
                {
                    for (int j = pos + 1; j < TypeNameDictionary.Count; j++)
                    {
                        if (SysUtils.AnsiCompareText(TypeNameDictionary.Mapping(j).ExpandedDelphiName, SuperClassName) == 0)
                        {
                            InstallAttributeType(TypeNameDictionary, j);
                        }
                    }

                    SuperType = AttributeTypeInfoByDelphiName(SuperClassName);
                    if (SuperType is null)
                    {
                        TBoldMemberTypeDescriptor? SuperDescriptor = BoldMemberTypeDictionary.BoldMemberTypes().DescriptorByDelphiName(SuperClassName);
                        if (SuperDescriptor is null)
                        {
                            InitializationError(BoldCoreConsts.sErrorInstallingAttribute_MissingSuperType, TypeNameDictionary.Mapping(pos).ExpressionName, SuperClassName);
                        }
                        else
                        {
                            TBoldTypeNameMapping SuperMapping = TypeNameDictionary.AddMapping();
                            SuperMapping.DelphiName = SuperClassName;
                            SuperMapping.ExpressionName = "__" + SuperClassName;
                            SuperMapping.ModelName = "__" + SuperClassName;
                            InstallAttributeType(TypeNameDictionary, TypeNameDictionary.Count - 1);
                            SuperType = AttributeTypeInfoByDelphiName(SuperClassName);
                        }
                    }
                }
            }
            else
            {
                SuperType = null;
            }

            IsAbstract = AttributeDescriptor.AbstractionLevel == TBoldAbstractionLevel.alAbstract;
        }
        else
        {
            MemberClass = null;
            SuperType = null;
            IsAbstract = true;
        }

        TempAttributeType = new TBoldAttributeTypeInfo(Mapping.ModelName, Mapping.ExpressionName, MemberClass, SuperType,
            Mapping.ExpandedDelphiName, this, IsAbstract);
        AttributeTypes.Add(TempAttributeType);
        ListTypes.Add(new TBoldListTypeInfo(TempAttributeType, this, BoldSystemRT.TBoldMemberListClass));
    }

    /// <summary>The OCL evaluator, created on first use. Needs TBoldOCL (M4).</summary>
    [Delphi(PortStatus.Partial, Note = "Creates the evaluator through BoldSystemRT.CreateOclEvaluator, which BoldOcl (M4) will set; raises until then.")]
    protected override TBoldEvaluator? GetEvaluator() =>
        fEvaluator ??= BoldSystemRT.CreateOclEvaluator?.Invoke(this)
            ?? throw new EBoldFeatureNotImplementedYet("TBoldSystemTypeInfo.GetEvaluator needs TBoldOCL, which is not ported yet.");

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => TypeTypeInfo;

    [Delphi(PortStatus.Ported)]
    public override bool ConformsTo(TBoldElementTypeInfo? CompareElement) => CompareElement == this;

    /// <summary>Clears <paramref name="S"/> and adds the expression names of the kinds of types asked for.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetValueTypeNames(TStrings S, bool Classes, bool Types, bool System, bool metatype, bool lists)
    {
        S.Clear();
        for (int i = 0; i < ValueTypeNameList.Count; i++)
        {
            TBoldElementTypeInfo t = ValueTypeNameList[i];
            bool add = t.BoldValueType switch
            {
                TBoldValueTypeSet.bvtList => lists,
                TBoldValueTypeSet.bvtClass => Classes,
                TBoldValueTypeSet.bvtAttr => Types,
                TBoldValueTypeSet.bvtSystem => System,
                TBoldValueTypeSet.bvtType => metatype,
                _ => false,
            };
            if (add)
            {
                S.Add(t.ExpressionName);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ReleaseEvaluator() => fEvaluator = null;

    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? AttributeTypeInfoByDelphiName(string name) => GetAttributeTypeInfoByDelphiName(name);
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? AttributeTypeInfoByExpressionName(string name) => GetAttributeTypeInfoByExpressionName(name);
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfoList AttributeTypes => fAttributeTypes!;
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo? ClassTypeInfoByExpressionName(string name) => GetClassTypeInfoByExpressionName(name);
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo? ClassTypeInfoByModelName(string name) => GetClassTypeInfoByModelName(name);
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? ListTypeInfoByElement(TBoldElementTypeInfo? Element) => GetListTypeInfoByElement(Element);
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfoList ListTypes => fListTypes!;
    [Delphi(PortStatus.Ported)] public TBoldNilTypeInfo? NilTypeInfo => fNilTypeInfo;
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ElementTypeInfoByDelphiName(string name) => GetElementTypeInfoByDelphiName(name);
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ElementTypeInfoByExpressionName(string name) => GetElementTypeInfoByExpressionName(name);
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfoList ValueTypeNameList => GetValueTypeNameList();
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo RootClassTypeInfo => GetRootClassTypeInfo();
    [Delphi(PortStatus.Ported)] public TBoldTypeTypeInfo? TypeTypeInfo => fTypeTypeInfo;
    [Delphi(PortStatus.Ported)] public bool MethodsInstalled => fMethodsInstalled;
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfoList TopSortedClasses => fTopSortedClasses!;
    [Delphi(PortStatus.Ported)] public bool Persistent => GetElementFlag(BoldElements.befSystemPersistent);
    [Delphi(PortStatus.Ported)] public bool SystemIsRunnable => GetElementFlag(BoldElements.befSystemIsRunnable);
    [Delphi(PortStatus.Ported)] public bool UseGeneratedCode => fUseGeneratedCode;
    [Delphi(PortStatus.Ported)] public string Stereotype => fStereotype;
    [Delphi(PortStatus.Ported)] public bool GenerateMultiplicityConstraints => fGenerateMultiplicityConstraints;
    [Delphi(PortStatus.Ported)] public TBoldOptimisticLockingMode OptimisticLocking => fOptimisticLocking;
    [Delphi(PortStatus.Ported)] public bool UpdateWholeObjects => GetElementFlag(BoldElements.befUpdateWholeObjects);
    [Delphi(PortStatus.Ported)] public TStringList InitializationLog => GetInitializationLog();
    [Delphi(PortStatus.Ported)] public bool GenerateDefaultRegions => GetElementFlag(BoldElements.befGenerateDefaultRegions);
    [Delphi(PortStatus.Ported)] public bool UseClockLog => fUseClockLog;
}

/// <summary>The runtime type of a class: its members, methods, superclass, object class and list type.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldClassTypeInfo : TBoldElementTypeInfoWithConstraint
{
    private TBoldOptimisticLockingMode fOptimisticLocking;
    private TBoldMemberRTInfoList? fAllMembers;
    private int FFirstOwnMemberIndex;
    private TBoldMethodRTInfoList? fMethods;
    private TClass? FObjectClass;
    private TBoldClassTypeInfo? fSuperClassTypeInfo;
    private readonly TBoldSystemTypeInfo fSystemTypeInfo;
    private protected int fTopSortedIndex;
    private readonly string FStereotype = "";
    private string fDefaultStringRepresentation = "";
    internal int fEmbeddedSingleLinkCount;
    private int fAllMembersCount;
    private string fPackagename = "";

    [Delphi(PortStatus.Ported)]
    protected internal TBoldClassTypeInfo(TBoldSystemTypeInfo SystemTypeInfo, TMoldClass? moldClass, TBoldTypeNameDictionary? TypeNameDictionary,
        TBoldGeneratedClassList? BoldObjectClasses, TBoldGeneratedClassList? BoldObjectListClasses, bool SkipMembers = false)
        : base(moldClass, SystemTypeInfo)
    {
        if (moldClass is not null)
        {
            FStereotype = moldClass.Stereotype;
        }

        SetValueType(TBoldValueTypeSet.bvtClass);
        fSystemTypeInfo = SystemTypeInfo;
        fAllMembers = new TBoldMemberRTInfoList();
        fMethods = new TBoldMethodRTInfoList();
        Initialize(moldClass, TypeNameDictionary, BoldObjectClasses, BoldObjectListClasses, SkipMembers);
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        SysUtils.FreeAndNil(ref fAllMembers);
        SysUtils.FreeAndNil(ref fMethods);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private TBoldListTypeInfo? GetListTypeInfo() => SystemTypeInfo.ListTypeInfoByElement(this);

    [Delphi(PortStatus.Ported)]
    private int GetMemberIndexByExpressionName(string name) => MemberRTInfoByExpressionName(name)?.index ?? -1;

    [Delphi(PortStatus.Ported)]
    private TBoldMemberRTInfo? GetMemberRTInfoByExpressionName(string Name) => fAllMembers!.ItemsByExpressionName(Name);

    [Delphi(PortStatus.Ported)]
    private TBoldMemberRTInfo? GetMemberRTInfoByModelName(string Name) => fAllMembers!.ItemsByModelName(Name);

    /// <summary>Adds "role-&gt;size &lt;= n" and "&gt;= n" constraints for the navigable roles this class introduces.</summary>
    [Delphi(PortStatus.Ported)]
    internal void InitializeMultiplicityConstraints()
    {
        void AddMultiplicityConstraint(TBoldRoleRTInfo Role, string ExprFragment, string moreless, int limit)
        {
            AddConstraint(new TBoldConstraintRTInfo(null, Role.ModelName + " multiplicity constraint", "", "", SystemTypeInfo,
                SysUtils.Format("%s->size %s %d", Role.ExpressionName, ExprFragment, limit),
                SysUtils.Format(BoldCoreConsts.sMultiplicityConstraintMessage, Role.ModelName, moreless, limit, Role.ClassTypeInfoOfOtherEnd!.ModelName)));
        }

        for (int i = FirstOwnMemberIndex; i < AllMembers.Count; i++)
        {
            if (AllMembers[i] is TBoldRoleRTInfo RoleRT && RoleRT.IsNavigable)
            {
                int upper = BoldUtils.GetUpperLimitForMultiplicity(RoleRT.Multiplicity);
                int lower = BoldUtils.GetLowerLimitForMultiplicity(RoleRT.Multiplicity);
                if (upper > 1 && upper < int.MaxValue)
                {
                    AddMultiplicityConstraint(RoleRT, "<=", BoldCoreConsts.sAtMost, upper);
                }

                if (lower > 0)
                {
                    AddMultiplicityConstraint(RoleRT, ">=", BoldCoreConsts.sAtLeast, lower);
                }
            }
        }
    }

    /// <summary>
    /// Sets the flags, superclass, object class and list type of the class, then creates its members (unless
    /// <paramref name="SkipMembers"/>) and its methods, own ones first and then inherited ones.
    /// </summary>
    [Delphi(PortStatus.Partial, Note = "The default list class is TBoldObjectList from BoldSystem (M3); null until it is ported.")]
    protected virtual void Initialize(TMoldClass? MoldClass, TBoldTypeNameDictionary? TypeNameDictionary, TBoldGeneratedClassList? BoldObjectClasses,
        TBoldGeneratedClassList? BoldObjectListClasses, bool SkipMembers)
    {
        TMoldClass moldClass = MoldClass!;
        fTopSortedIndex = SystemTypeInfo.TopSortedClasses.Count;

        SetElementFlag(BoldElements.befIsAbstract, moldClass.IsAbstract);
        SetElementFlag(BoldElements.befIsLinkClass, moldClass.Association is not null);
        SetElementFlag(BoldElements.beFIsImported, moldClass.Imported);
        SetElementFlag(BoldElements.befClassPersistent, moldClass.EffectivePersistent && SystemTypeInfo.Persistent);
        SetElementFlag(BoldElements.befClassToBeRemoved, moldClass.EffectiveEvolutionState == TBoldEvolutionState.esToBeRemoved);
        SetElementFlag(BoldElements.befVersioned, moldClass.Versioned);
        SetElementFlag(BoldElements.befGenerateDefaultRegion, moldClass.GenerateDefaultRegion);

        if (moldClass.SuperClass is not null)
        {
            fSuperClassTypeInfo = SystemTypeInfo.ClassTypeInfoByExpressionName(moldClass.SuperClass.ExpandedExpressionName);
            fSuperClassTypeInfo!.SetElementFlag(BoldElements.befHasSubclasses, true);
        }

        SetObjectClass(BoldObjectClasses!);
        fOptimisticLocking = moldClass.EffectiveOptimisticLocking;
        SystemTypeInfo.TopSortedClasses.Add(this);

        TBoldGeneratedClassDescriptor? ListClassDescriptor = BoldObjectListClasses!.EntryByExpressionName(ExpressionName);
        TClass? ListClass = ListClassDescriptor is not null ? ListClassDescriptor.TheClass : BoldSystemRT.TBoldObjectListClass;
        SystemTypeInfo.ListTypes.Add(new TBoldListTypeInfo(this, SystemTypeInfo, ListClass));

        FFirstOwnMemberIndex = moldClass.FirstOwnBoldMemberIndex;
        if (SkipMembers)
        {
            // The model has errors; install the class as empty as possible.
            return;
        }

        for (int i = 0; i < moldClass.AllBoldMembers.Count; i++)
        {
            if (moldClass.AllBoldMembers[i] is TMoldAttribute attribute)
            {
                _ = new TBoldAttributeRTInfo(this, attribute, TypeNameDictionary!);
            }
            else
            {
                var Role = (TMoldRole)moldClass.AllBoldMembers[i];
                _ = Role.RoleType switch
                {
                    TBoldRoleType.rtRole => new TBoldRoleRTInfo(this, Role, TypeNameDictionary!),
                    TBoldRoleType.rtLinkRole => new TBoldRoleRTInfo(this, Role.MainRole!, TypeNameDictionary!, (short)0),
                    _ => new TBoldRoleRTInfo(this, Role, (short)0, TypeNameDictionary!),
                };
            }
        }

        fAllMembersCount = fAllMembers!.Count;
        fDefaultStringRepresentation = moldClass.EffectiveDefaultStringRepresentation;

        // Own methods first, then inherited ones not overridden, so a method has the same index in every class.
        for (int i = 0; i < moldClass.Methods.Count; i++)
        {
            _ = new TBoldMethodRTInfo(this, moldClass.Methods[i], -1, TypeNameDictionary);
        }

        for (TMoldClass? tempClass = moldClass.SuperClass; tempClass is not null; tempClass = tempClass.SuperClass)
        {
            for (int i = 0; i < tempClass.Methods.Count; i++)
            {
                if (Methods.ItemsByExpressionName(tempClass.Methods[i].ExpandedExpressionName) is null)
                {
                    _ = new TBoldMethodRTInfo(this, tempClass.Methods[i], tempClass.TopSortedIndex, TypeNameDictionary);
                }
            }
        }

        fPackagename = moldClass.TVByName(BoldDefaultTaggedValues.BOLDINTERALTVPREFIX + BoldDefaultTaggedValues.TAG_UNFLATTENEDNAMESPACE);
    }

    /// <summary>The generated class registered for this class, else the superclass's, else TBoldObject.</summary>
    [Delphi(PortStatus.Partial, Note = "The default is TBoldObject from BoldSystem (M3); null until it is ported.")]
    private void SetObjectClass(TBoldGeneratedClassList BoldObjectClasses)
    {
        FObjectClass = BoldObjectClasses.EntryByExpressionName(ExpressionName)?.TheClass;
        FObjectClass ??= SuperClassTypeInfo is not null ? SuperClassTypeInfo.ObjectClass : BoldSystemRT.TBoldObjectClass;
    }

    [Delphi(PortStatus.Ported)]
    private string GetQualifiedName() => fPackagename != "" ? fPackagename + "." + ModelName : ModelName;

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => SystemTypeInfo.TypeTypeInfo;

    /// <summary>True for this class and its superclasses.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool BoldIsA(TBoldElementTypeInfo? C2)
    {
        if (C2 is TBoldClassTypeInfo)
        {
            if (C2 == this)
            {
                return true;
            }

            if (SuperClassTypeInfo is not null)
            {
                return SuperClassTypeInfo.BoldIsA(C2);
            }
        }

        return false;
    }

    /// <summary>True for this class, its superclasses, and lists of them (or the untyped list).</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ConformsTo(TBoldElementTypeInfo? CompareElement)
    {
        bool result = BoldIsA(CompareElement);
        if (!result && CompareElement is TBoldListTypeInfo list)
        {
            result = list.ListElementTypeInfo is null || BoldIsA(list.ListElementTypeInfo);
        }

        return result;
    }

    /// <summary>The nearest class both this and <paramref name="OtherClassTypeInfo"/> are; null when there is none.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldClassTypeInfo? LeastCommonSuperClass(TBoldClassTypeInfo OtherClassTypeInfo)
    {
        if (OtherClassTypeInfo.BoldIsA(this))
        {
            return this;
        }

        return SuperClassTypeInfo?.LeastCommonSuperClass(OtherClassTypeInfo);
    }

    [Delphi(PortStatus.Ported)] public TBoldMemberRTInfoList AllMembers => fAllMembers!;
    [Delphi(PortStatus.Ported)] public int AllMembersCount => fAllMembersCount;
    [Delphi(PortStatus.Ported)] public int FirstOwnMemberIndex => FFirstOwnMemberIndex;
    [Delphi(PortStatus.Ported)] public bool HasSubclasses => GetElementFlag(BoldElements.befHasSubclasses);
    [Delphi(PortStatus.Ported)] public bool IsAbstract => GetElementFlag(BoldElements.befIsAbstract);
    [Delphi(PortStatus.Ported)] public bool IsImported => GetElementFlag(BoldElements.beFIsImported);
    [Delphi(PortStatus.Ported)] public bool IsLinkClass => GetElementFlag(BoldElements.befIsLinkClass);
    [Delphi(PortStatus.Ported)] public TBoldListTypeInfo? ListTypeInfo => GetListTypeInfo();
    [Delphi(PortStatus.Ported)] public int MemberIndexByExpressionName(string name) => GetMemberIndexByExpressionName(name);
    [Delphi(PortStatus.Ported)] public TBoldMemberRTInfo? MemberRTInfoByExpressionName(string Name) => GetMemberRTInfoByExpressionName(Name);
    [Delphi(PortStatus.Ported)] public TBoldMemberRTInfo? MemberRTInfoByModelName(string Name) => GetMemberRTInfoByModelName(Name);
    [Delphi(PortStatus.Ported)] public TBoldMethodRTInfoList Methods => fMethods!;
    [Delphi(PortStatus.Ported)] public TClass? ObjectClass => FObjectClass;
    [Delphi(PortStatus.Ported)] public bool Persistent => GetElementFlag(BoldElements.befClassPersistent);
    [Delphi(PortStatus.Ported)] public string Stereotype => FStereotype;
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo? SuperClassTypeInfo => fSuperClassTypeInfo;
    [Delphi(PortStatus.Ported)] public new TBoldSystemTypeInfo SystemTypeInfo => fSystemTypeInfo;
    [Delphi(PortStatus.Ported)] public int TopSortedIndex => fTopSortedIndex;
    [Delphi(PortStatus.Ported)] public string DefaultStringRepresentation => fDefaultStringRepresentation;
    [Delphi(PortStatus.Ported)] public TBoldOptimisticLockingMode OptimisticLocking => fOptimisticLocking;
    [Delphi(PortStatus.Ported)] public bool ToBeRemoved => GetElementFlag(BoldElements.befClassToBeRemoved);
    [Delphi(PortStatus.Ported)] public bool Versioned => GetElementFlag(BoldElements.befVersioned);
    [Delphi(PortStatus.Ported)] public bool GenerateDefaultRegion => GetElementFlag(BoldElements.befGenerateDefaultRegion);
    [Delphi(PortStatus.Ported)] public int EmbeddedSingleLinkCount => fEmbeddedSingleLinkCount;
    [Delphi(PortStatus.Ported)] public string QualifiedName => GetQualifiedName();
}

/// <summary>The type of nil: conforms to every class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldNilTypeInfo : TBoldClassTypeInfo
{
    internal TBoldNilTypeInfo(TBoldSystemTypeInfo SystemTypeInfo, TMoldClass? moldClass, TBoldTypeNameDictionary? TypeNameDictionary,
        TBoldGeneratedClassList? BoldObjectClasses, TBoldGeneratedClassList? BoldObjectListClasses)
        : base(SystemTypeInfo, moldClass, TypeNameDictionary, BoldObjectClasses, BoldObjectListClasses)
    {
    }

    /// <summary>Only sets the topological index to -1.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void Initialize(TMoldClass? MoldClass, TBoldTypeNameDictionary? TypeNameDictionary, TBoldGeneratedClassList? BoldObjectClasses,
        TBoldGeneratedClassList? BoldObjectListClasses, bool SkipMembers) => fTopSortedIndex = -1;

    [Delphi(PortStatus.Ported)]
    public override bool BoldIsA(TBoldElementTypeInfo? C2) => ConformsTo(C2);

    [Delphi(PortStatus.Ported)]
    public override bool ConformsTo(TBoldElementTypeInfo? CompareElement) => CompareElement is TBoldClassTypeInfo;

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => "nil";
}

/// <summary>A member of a class at runtime: its index, type, flags and derivation expression.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMemberRTInfo : TBoldMetaElementWithConstraint
{
    private readonly string fDeriveExpression = "";
    private readonly string fStereotype = "";
    private readonly int fDispId;
    private protected TBoldElementTypeInfo? fBoldType;
    private readonly TBoldClassTypeInfo fClassTypeInfo;
    private readonly int fIndex;
    private protected int fEmbeddedLinkIndex;
    private readonly TVisibilityKind fVisibility;
    private protected string fStreamName = "";

    /// <summary>
    /// Creates the member and adds it to its class. A derived member takes its OCL from the nearest subclass, from this
    /// class up to the declaring class, that overrides it in its DerivationExpressions tagged value.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected internal TBoldMemberRTInfo(TBoldClassTypeInfo ClassTypeInfo, TMoldMember moldMember, TBoldTypeNameDictionary TypeNameDictionary)
        : base(moldMember, moldMember.Name, moldMember.ExpandedExpressionName, moldMember.ExpandedDelphiName, ClassTypeInfo.SystemTypeInfo)
    {
        fStereotype = moldMember.Stereotype;
        fVisibility = moldMember.Visibility;
        fDispId = moldMember.DispId;
        SetElementFlag(BoldElements.befIsDerived, moldMember.Derived);
        SetElementFlag(BoldElements.befIsReverseDerived, moldMember.ReverseDerived);
        SetElementFlag(BoldElements.befPersistent, moldMember.EffectivePersistent);
        fClassTypeInfo = ClassTypeInfo;
        SetElementFlag(BoldElements.befDelayedFetch, moldMember.EffectiveDelayedFetch);
        fIndex = ClassTypeInfo.AllMembers.Count;
        fEmbeddedLinkIndex = -1;
        ClassTypeInfo.AllMembers.Add(this);
        if (IsDerived)
        {
            fDeriveExpression = moldMember.DerivationOCL;
            if (DeriveExpression != "")
            {
                TMoldClass? MoldClass = moldMember.Model.Classes[ClassTypeInfo.TopSortedIndex];
                var Expressions = new TStringList();
                while (MoldClass is not null && MoldClass != moldMember.MoldClass)
                {
                    Expressions.Text = MoldClass.BoldTVByName(BoldDefaultTaggedValues.TAG_DERIVATIONEXPRESSIONS);
                    // Models from 2.0.20 sometimes have extra spaces.
                    for (int i = 0; i < Expressions.Count; i++)
                    {
                        Expressions[i] = Expressions[i].Trim();
                    }

                    string tempExpr = Expressions.Values(ModelName).Trim();
                    if (tempExpr != "")
                    {
                        fDeriveExpression = tempExpr;
                        break;
                    }

                    MoldClass = MoldClass.SuperClass;
                }
            }
        }
    }

    /// <summary>A member with no model element (the link object roles); its expression name starts in lower case.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberRTInfo.CreateWithoutMoldMember")]
    protected internal TBoldMemberRTInfo(TBoldClassTypeInfo ClassTypeInfo, string ModelName, string ExpressionName, string DelphiName, bool Persistent,
        TBoldTypeNameDictionary TypeNameDictionary)
        : base(null, ModelName, EnsureLowerCaseLeadingCharacter(ExpressionName), DelphiName, ClassTypeInfo.SystemTypeInfo)
    {
        SetElementFlag(BoldElements.befPersistent, Persistent);
        fClassTypeInfo = ClassTypeInfo;
        fIndex = ClassTypeInfo.AllMembers.Count;
        fEmbeddedLinkIndex = -1;
        ClassTypeInfo.AllMembers.Add(this);
    }

    private static string EnsureLowerCaseLeadingCharacter(string ExpressionName) =>
        ExpressionName.Length > 0 ? SysUtils.LowerCase(ExpressionName.Substring(0, 1)) + ExpressionName.Substring(1) : ExpressionName;

    [Delphi(PortStatus.Replaced, Symbol = "TBoldMemberRTInfo.Destroy", Note = "Only asserted that the class had dropped its member list; nothing to free in .NET.")]
    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => fBoldType;

    [Delphi(PortStatus.Ported)]
    protected bool GetIsAttribute() => !IsSingleRole && !IsMultiRole;

    [Delphi(PortStatus.Ported)]
    protected bool GetIsRole() => IsSingleRole || IsMultiRole;

    [Delphi(PortStatus.Ported)]
    protected internal void SetBoldType(TBoldElementTypeInfo? BoldType) => fBoldType = BoldType;

    [Delphi(PortStatus.Ported)]
    protected abstract TClass? GetMemberClass();

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetEncouragesOptimisticLockingOnDeletedOnly() => false;

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetCanHaveOldValue() => Persistent;

    [Delphi(PortStatus.Ported)] public bool IsDerived => GetElementFlag(BoldElements.befIsDerived);
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo ClassTypeInfo => fClassTypeInfo;
    [Delphi(PortStatus.Ported)] public bool DelayedFetch => GetElementFlag(BoldElements.befDelayedFetch);
    [Delphi(PortStatus.Ported)] public int index => fIndex;
    [Delphi(PortStatus.Ported)] public int EmbeddedLinkIndex => fEmbeddedLinkIndex;
    [Delphi(PortStatus.Ported)] public bool IsAttribute => GetIsAttribute();
    [Delphi(PortStatus.Ported)] public bool IsMultiRole => GetElementFlag(BoldElements.befIsMultiRole);
    [Delphi(PortStatus.Ported)] public bool IsRole => GetIsRole();
    [Delphi(PortStatus.Ported)] public bool IsSingleRole => GetElementFlag(BoldElements.befIsSingleRole);
    [Delphi(PortStatus.Ported)] public bool IsStoredInObject => GetElementFlag(BoldElements.befIsStoredInObject);
    [Delphi(PortStatus.Ported)] public TClass? MemberClass => GetMemberClass();
    [Delphi(PortStatus.Ported)] public bool Persistent => GetElementFlag(BoldElements.befMemberPersistent);
    [Delphi(PortStatus.Ported)] public string Stereotype => fStereotype;
    [Delphi(PortStatus.Ported)] public string StreamName => fStreamName;
    [Delphi(PortStatus.Ported)] public TVisibilityKind Visibility => fVisibility;
    [Delphi(PortStatus.Ported)] public string DeriveExpression => fDeriveExpression;
    [Delphi(PortStatus.Ported)] public bool IsReverseDerived => GetElementFlag(BoldElements.befIsReverseDerived);
    [Delphi(PortStatus.Ported)] public bool IsNonVersionedInVersionedClass => GetElementFlag(BoldElements.befIsNonVersionedInVersionedClass);
    [Delphi(PortStatus.Ported)] public bool EncouragesOptimisticLockingOnDeletedOnly => GetEncouragesOptimisticLockingOnDeletedOnly();
    [Delphi(PortStatus.Ported)] public bool CanHaveOldValue => GetCanHaveOldValue();
    [Delphi(PortStatus.Ported)] public bool ToBeRemoved => GetElementFlag(BoldElements.befMemberToBeRemoved);
    [Delphi(PortStatus.Ported)] public int DispId => fDispId;
}

/// <summary>An association end at runtime: multiplicity, ordering, the other end and, for link classes, the link roles.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRoleRTInfo : TBoldMemberRTInfo
{
    private TBoldClassTypeInfo? fClassTypeInfoOfOtherEnd;
    private int fIndexOfOtherEnd;
    private TBoldClassTypeInfo? fLinkClassTypeInfo;
    private int fOtherIndexInLinkClass;
    private int fOwnIndexInLinkClass;
    private TBoldMemberRTInfoList? fQualifiers;
    private TBoldRoleRTInfo? fRoleRTInfoOfOtherEnd;
    private readonly TBoldRoleType fRoleType;
    private readonly string fMultiplicity = "";
    private readonly TChangeableKind fChangeability;
    private readonly TAggregationKind fAggregation;
    private readonly TDeleteAction fDeleteAction;
    private readonly string fAssociationStereotype = "";

    /// <summary>A normal association end.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal TBoldRoleRTInfo(TBoldClassTypeInfo ClassTypeInfo, TMoldRole MoldRole, TBoldTypeNameDictionary TypeNameDictionary)
        : base(ClassTypeInfo, MoldRole, TypeNameDictionary)
    {
        SetElementFlag(BoldElements.befIsMultiRole, MoldRole.Multi);
        SetElementFlag(BoldElements.befIsSingleRole, !MoldRole.Multi);
        SetElementFlag(BoldElements.befQualifiedMulti, MoldRole.QualifiedMulti);
        if (IsMultiRole)
        {
            SetElementFlag(BoldElements.befDelayedFetch, true);
        }

        SetElementFlag(BoldElements.befMandatory, MoldRole.Mandatory);
        SetElementFlag(BoldElements.befIsStoredInObject, MoldRole.EffectiveEmbedded && !MoldRole.Multi && Persistent);
        SetElementFlag(BoldElements.befIsOrdered, MoldRole.EffectiveOrdered);
        SetElementFlag(BoldElements.befIsNavigable, MoldRole.Navigable);
        SetElementFlag(BoldElements.befIsIndirect, MoldRole.Association!.LinkClass is not null);
        SetElementFlag(BoldElements.befMemberToBeRemoved, MoldRole.Association.EvolutionState == TBoldEvolutionState.esToBeRemoved);
        SetDefaultRegionMode(MoldRole.EffectiveDefaultRegionMode);
        if (IsStoredInObject)
        {
            fEmbeddedLinkIndex = ClassTypeInfo.EmbeddedSingleLinkCount;
            ClassTypeInfo.fEmbeddedSingleLinkCount++;
        }

        TMoldRole EmbeddedOtherEnd = MoldRole.HasLinkRole ? MoldRole.LinkRole!.OtherEnd! : MoldRole.OtherEnd!;
        SetElementFlag(BoldElements.befIsNonVersionedInVersionedClass,
            !MoldRole.EffectiveEmbedded && MoldRole.MoldClass!.Versioned && !EmbeddedOtherEnd.MoldClass!.Versioned);
        fMultiplicity = MoldRole.Multiplicity;
        fRoleType = TBoldRoleType.rtRole;
        fAggregation = MoldRole.Aggregation;
        fDeleteAction = MoldRole.EffectiveDeleteAction;
        fChangeability = MoldRole.Changeability;
        fAssociationStereotype = MoldRole.Association.Stereotype;
    }

    /// <summary>The end of an association inside its link class: single, stored in the link object, frozen.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldRoleRTInfo.CreateInnerLinkRole")]
    protected internal TBoldRoleRTInfo(TBoldClassTypeInfo ClassTypeInfo, TMoldRole MoldRole, short Dummy, TBoldTypeNameDictionary TypeNameDictionary)
        : base(ClassTypeInfo, MoldRole, TypeNameDictionary)
    {
        fRoleType = TBoldRoleType.rtInnerLinkRole;
        SetElementFlag(BoldElements.befIsMultiRole, false);
        SetElementFlag(BoldElements.befIsSingleRole, true);
        SetElementFlag(BoldElements.befIsStoredInObject, Persistent);
        SetElementFlag(BoldElements.befIsOrdered, false);
        SetElementFlag(BoldElements.befIsIndirect, false);
        SetElementFlag(BoldElements.befIsNavigable, MoldRole.Navigable);
        SetDefaultRegionMode(MoldRole.EffectiveDefaultRegionMode);
        if (Persistent)
        {
            fEmbeddedLinkIndex = ClassTypeInfo.EmbeddedSingleLinkCount;
            ClassTypeInfo.fEmbeddedSingleLinkCount++;
        }

        fAggregation = TAggregationKind.akNone;
        fDeleteAction = TDeleteAction.daAllow;
        fChangeability = TChangeableKind.ckFrozen;
    }

    /// <summary>
    /// The role from an object to its link objects, named after the link class (prefixed by the main role when both ends
    /// can be in the same class). Not persistent; follows the main role's multiplicity and ordering.
    /// </summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldRoleRTInfo.CreateLinkObjectRole")]
    protected internal TBoldRoleRTInfo(TBoldClassTypeInfo ClassTypeInfo, TMoldRole MainRole, TBoldTypeNameDictionary TypeNameDictionary, short Dummy)
        : base(ClassTypeInfo, LinkObjectRoleNames(MainRole).ModelName, LinkObjectRoleNames(MainRole).ExpressionName,
            LinkObjectRoleNames(MainRole).DelphiName, false, TypeNameDictionary)
    {
        SetElementFlag(BoldElements.befIsMultiRole, MainRole.Multi);
        SetElementFlag(BoldElements.befIsSingleRole, !MainRole.Multi);
        if (IsMultiRole)
        {
            SetElementFlag(BoldElements.befDelayedFetch, true);
        }

        SetElementFlag(BoldElements.befMandatory, MainRole.Mandatory);
        SetElementFlag(BoldElements.befIsStoredInObject, false);
        SetElementFlag(BoldElements.befIsOrdered, MainRole.EffectiveOrdered);
        SetElementFlag(BoldElements.befIsNavigable, MainRole.Navigable);
        SetElementFlag(BoldElements.befIsIndirect, false);
        SetDefaultRegionMode(TBoldAssociationEndDefaultRegionMode.aedrmNone);
        fRoleType = TBoldRoleType.rtLinkRole;
        fAggregation = TAggregationKind.akNone;
        fDeleteAction = TDeleteAction.daAllow;
        fChangeability = MainRole.Changeability;
    }

    /// <summary>The names of the link object role of <paramref name="MainRole"/>, as Delphi chooses its inherited constructor call.</summary>
    private static (string ModelName, string ExpressionName, string DelphiName) LinkObjectRoleNames(TMoldRole MainRole)
    {
        TMoldClass aLinkClass = MainRole.Association!.LinkClass!;
        TBoldNationalCharConversion conversion = MainRole.Model.NationalCharConversion;
        string linkDelphiName = BoldNameExpander.BoldExpandName(aLinkClass.ExpressionName, aLinkClass.Name, TExpansionType.xtDelphi, -1, conversion);
        if (MainRole.MoldClass!.ChildTo(MainRole.OtherEnd!.MoldClass) || MainRole.OtherEnd.MoldClass!.ChildTo(MainRole.MoldClass))
        {
            return (MainRole.Name + aLinkClass.Name,
                MainRole.ExpandedExpressionName + aLinkClass.ExpandedExpressionName,
                BoldNameExpander.BoldExpandName(MainRole.ExpressionName, MainRole.Name, TExpansionType.xtDelphi, -1, conversion) + linkDelphiName);
        }

        return (aLinkClass.Name, aLinkClass.ExpandedExpressionName, linkDelphiName);
    }

    private void SetDefaultRegionMode(TBoldAssociationEndDefaultRegionMode mode) =>
        SetInternalState(unchecked((uint)BoldElements.BoldDefaultRegionModeMask), BoldElements.BoldDefaultRegionModeShift, (uint)mode);

    /// <summary>
    /// The second pass over an association, once all classes exist: links each end to the other end (and, for a link
    /// class, to the link class roles and the link object roles), sets the qualifiers and copies all this to the
    /// inherited ends in subclasses.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    internal static void SetPass2InfoForAssociation(TBoldSystemTypeInfo SystemTypeInfo, TMoldAssociation moldAssociation)
    {
        void PropagateOtherEnd(TBoldRoleRTInfo? aRole, TBoldClassTypeInfo ClassOfOtherEnd, TBoldRoleRTInfo? RoleofOtherEnd)
        {
            if (aRole is null)
            {
                return;
            }

            // A non-navigable other end may be missing, but there is still a class at the other end.
            aRole.fClassTypeInfoOfOtherEnd = ClassOfOtherEnd;
            aRole.fRoleRTInfoOfOtherEnd = RoleofOtherEnd;
            if (RoleofOtherEnd is not null)
            {
                aRole.SetElementFlag(BoldElements.befOtherEndOrdered, RoleofOtherEnd.IsOrdered);
                aRole.SetElementFlag(BoldElements.befForceOtherEnd, RoleofOtherEnd.IsSingleRole || RoleofOtherEnd.IsOrdered || RoleofOtherEnd.IsStoredInObject);
                aRole.fIndexOfOtherEnd = RoleofOtherEnd.index;
            }
            else
            {
                aRole.fIndexOfOtherEnd = -1;
            }

            aRole.SetBoldType(aRole.IsMultiRole ? ClassOfOtherEnd.ListTypeInfo : ClassOfOtherEnd);
        }

        void CopyRoleProperties(TBoldRoleRTInfo SourceRoleRTInfo, TBoldRoleRTInfo DestRoleRTInfo)
        {
            DestRoleRTInfo.fRoleRTInfoOfOtherEnd = SourceRoleRTInfo.RoleRTInfoOfOtherEnd;
            DestRoleRTInfo.fClassTypeInfoOfOtherEnd = SourceRoleRTInfo.ClassTypeInfoOfOtherEnd;
            DestRoleRTInfo.SetElementFlag(BoldElements.befOtherEndOrdered, SourceRoleRTInfo.OtherEndOrdered);
            DestRoleRTInfo.SetElementFlag(BoldElements.befForceOtherEnd, SourceRoleRTInfo.ForceOtherEnd);
            DestRoleRTInfo.SetDefaultRegionMode(SourceRoleRTInfo.DefaultRegionMode);
            DestRoleRTInfo.fIndexOfOtherEnd = SourceRoleRTInfo.IndexOfOtherEnd;
            DestRoleRTInfo.fOwnIndexInLinkClass = SourceRoleRTInfo.OwnIndexInLinkClass;
            DestRoleRTInfo.fOtherIndexInLinkClass = SourceRoleRTInfo.OtherIndexInLinkClass;
            DestRoleRTInfo.fLinkClassTypeInfo = SourceRoleRTInfo.LinkClassTypeInfo;
            if (SourceRoleRTInfo.fQualifiers is not null)
            {
                for (int Index = 0; Index < SourceRoleRTInfo.fQualifiers.Count; Index++)
                {
                    DestRoleRTInfo.fQualifiers ??= new TBoldMemberRTInfoList { OwnsEntries = false };
                    DestRoleRTInfo.fQualifiers.Add(SourceRoleRTInfo.fQualifiers[Index]);
                }
            }

            DestRoleRTInfo.SetBoldType(SourceRoleRTInfo.BoldType);
        }

        void PropagateToSubClasses(TBoldRoleRTInfo? aRole, TBoldRoleRTInfo? aLinkObjectRole, TMoldClass moldClass)
        {
            if (aRole is null)
            {
                return;
            }

            for (int i = 0; i < moldClass.SubClasses.Count; i++)
            {
                TMoldClass SubClass = moldClass.SubClasses[i];
                TBoldClassTypeInfo SubClassTypeInfo = SystemTypeInfo.TopSortedClasses.ItemsByModelName(SubClass.Name)!;
                var SubRole = (TBoldRoleRTInfo)SubClassTypeInfo.AllMembers.ItemsByModelName(aRole.ModelName)!;
                CopyRoleProperties(aRole, SubRole);
                if (aLinkObjectRole is not null)
                {
                    var SubLinkClassRole = (TBoldRoleRTInfo)SubClassTypeInfo.AllMembers.ItemsByModelName(aLinkObjectRole.ModelName)!;
                    CopyRoleProperties(aLinkObjectRole, SubLinkClassRole);
                }

                PropagateToSubClasses(aRole, aLinkObjectRole, SubClass);
            }
        }

        TBoldRoleRTInfo? LinkObjectRole1 = null;
        TBoldRoleRTInfo? LinkObjectRole2 = null;
        for (int r = 0; r < 2; r++)
        {
            if (moldAssociation.Roles[r].MoldClass is null)
            {
                SystemTypeInfo.InitializationError(BoldCoreConsts.sInvalidAssociation, moldAssociation.Name, moldAssociation.Roles[r].Name);
                return;
            }
        }

        // Non-navigable ends of derived associations do not exist in Mold.
        TBoldClassTypeInfo Class1 = SystemTypeInfo.ClassTypeInfoByModelName(moldAssociation.Roles[0].MoldClass!.Name)!;
        var Role1 = (TBoldRoleRTInfo?)Class1.MemberRTInfoByModelName(moldAssociation.Roles[0].Name);
        TBoldClassTypeInfo Class2 = SystemTypeInfo.ClassTypeInfoByModelName(moldAssociation.Roles[1].MoldClass!.Name)!;
        var Role2 = (TBoldRoleRTInfo?)Class2.MemberRTInfoByModelName(moldAssociation.Roles[1].Name);

        PropagateOtherEnd(Role1, Class2, Role2);
        PropagateOtherEnd(Role2, Class1, Role1);
        if (moldAssociation.LinkClass is not null)
        {
            TBoldClassTypeInfo LinkClass = SystemTypeInfo.ClassTypeInfoByModelName(moldAssociation.LinkClass.Name)!;
            if (Role1 is not null)
            {
                Role1.fLinkClassTypeInfo = LinkClass;
            }

            if (Role2 is not null)
            {
                Role2.fLinkClassTypeInfo = LinkClass;
            }

            var LinkClassRole1 = (TBoldRoleRTInfo)LinkClass.MemberRTInfoByModelName(moldAssociation.Roles[0].Name)!;
            var LinkClassRole2 = (TBoldRoleRTInfo)LinkClass.MemberRTInfoByModelName(moldAssociation.Roles[1].Name)!;
            LinkClassRole1.fOtherIndexInLinkClass = LinkClassRole2.index;
            LinkClassRole1.fOwnIndexInLinkClass = LinkClassRole1.index;
            LinkClassRole2.fOtherIndexInLinkClass = LinkClassRole1.index;
            LinkClassRole2.fOwnIndexInLinkClass = LinkClassRole2.index;

            PropagateOtherEnd(LinkClassRole1, Class2, Role2);
            PropagateOtherEnd(LinkClassRole2, Class1, Role1);
            if (Role1 is not null)
            {
                Role1.fOwnIndexInLinkClass = LinkClassRole2.index;
                Role1.fOtherIndexInLinkClass = LinkClassRole1.index;
                Role1.fRoleRTInfoOfOtherEnd = LinkClassRole2;
                LinkObjectRole1 = (TBoldRoleRTInfo)Role1.ClassTypeInfo.AllMembers[Role1.index + 1];
                PropagateOtherEnd(LinkObjectRole1, LinkClass, LinkClassRole2);
            }

            if (Role2 is not null)
            {
                Role2.fOwnIndexInLinkClass = LinkClassRole1.index;
                Role2.fOtherIndexInLinkClass = LinkClassRole2.index;
                Role2.fRoleRTInfoOfOtherEnd = LinkClassRole1;
                LinkObjectRole2 = (TBoldRoleRTInfo)Role2.ClassTypeInfo.AllMembers[Role2.index + 1];
                PropagateOtherEnd(LinkObjectRole2, LinkClass, LinkClassRole1);
            }
        }

        // Derived associations can have qualifiers but may exist in one direction only.
        Role1?.InitQualifiers(moldAssociation.Roles[0].Qualifiers);
        Role2?.InitQualifiers(moldAssociation.Roles[1].Qualifiers);

        PropagateToSubClasses(Role1, LinkObjectRole1, moldAssociation.Roles[0].MoldClass!);
        PropagateToSubClasses(Role2, LinkObjectRole2, moldAssociation.Roles[1].MoldClass!);
    }

    [Delphi(PortStatus.Ported)]
    private int GetIndexOfLinkObjectRole() => IsIndirect ? index + 1 : -1;

    [Delphi(PortStatus.Ported)]
    private int GetIndexOfMainRole() => RoleType == TBoldRoleType.rtLinkRole ? index - 1 : -1;

    /// <summary>The qualifiers: the members of the other end's class with the qualifier names. The list does not own them.</summary>
    [Delphi(PortStatus.Ported)]
    private void InitQualifiers(TMoldQualifierList Qualifiers)
    {
        if (Qualifiers.Count > 0)
        {
            fQualifiers = new TBoldMemberRTInfoList { OwnsEntries = false };
            for (int i = 0; i < Qualifiers.Count; i++)
            {
                fQualifiers.Add(ClassTypeInfoOfOtherEnd!.AllMembers.ItemsByModelName(Qualifiers[i].Name)!);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldAssociationEndDefaultRegionMode GetDefaultRegionMode() =>
        (TBoldAssociationEndDefaultRegionMode)GetInternalState(unchecked((uint)BoldElements.BoldDefaultRegionModeMask), BoldElements.BoldDefaultRegionModeShift);

    [Delphi(PortStatus.Replaced, Symbol = "TBoldRoleRTInfo.Destroy", Note = "Freed the qualifier list, which does not own its members; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    protected bool GetIsQualified() => Qualifiers is not null && Qualifiers.Count > 0;

    /// <summary>The list class for a multi role; TBoldObjectReference for a single one.</summary>
    [Delphi(PortStatus.Partial, Note = "TBoldObjectReference comes from BoldSystem (M3); null until it is ported.")]
    protected override TClass? GetMemberClass() =>
        IsMultiRole ? ((TBoldListTypeInfo)BoldType!).ListClass : BoldSystemRT.TBoldObjectReferenceClass;

    [Delphi(PortStatus.Ported)]
    protected TBoldMemberRTInfoList? GetQualifiers() => fQualifiers;

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => ClassTypeInfo.AsString + "." + ExpressionName;

    /// <summary>Non-embedded persistent ends check, when their object is deleted, that they are unchanged in the database.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool GetEncouragesOptimisticLockingOnDeletedOnly() => !IsStoredInObject && Persistent && RoleType == TBoldRoleType.rtRole;

    [Delphi(PortStatus.Ported)]
    protected override bool GetCanHaveOldValue() =>
        base.GetCanHaveOldValue() && (RoleType == TBoldRoleType.rtRole || RoleType == TBoldRoleType.rtInnerLinkRole);

    /// <summary>Makes the other end always fetched; called by region definitions that include a multi role.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetForceOtherEnd() => SetElementFlag(BoldElements.befForceOtherEnd, true);

    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo? ClassTypeInfoOfOtherEnd => fClassTypeInfoOfOtherEnd;
    [Delphi(PortStatus.Ported)] public bool ForceOtherEnd => GetElementFlag(BoldElements.befForceOtherEnd);
    [Delphi(PortStatus.Ported)] public int IndexOfLinkObjectRole => GetIndexOfLinkObjectRole();
    [Delphi(PortStatus.Ported)] public int IndexOfMainRole => GetIndexOfMainRole();
    [Delphi(PortStatus.Ported)] public int IndexOfOtherEnd => fIndexOfOtherEnd;
    [Delphi(PortStatus.Ported)] public bool IsIndirect => GetElementFlag(BoldElements.befIsIndirect);
    [Delphi(PortStatus.Ported)] public bool IsNavigable => GetElementFlag(BoldElements.befIsNavigable);
    [Delphi(PortStatus.Ported)] public bool IsOrdered => GetElementFlag(BoldElements.befIsOrdered);
    [Delphi(PortStatus.Ported)] public bool IsQualified => GetIsQualified();
    [Delphi(PortStatus.Ported)] public bool IsQualifiedMulti => GetElementFlag(BoldElements.befQualifiedMulti);
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo? LinkClassTypeInfo => fLinkClassTypeInfo;
    [Delphi(PortStatus.Ported)] public bool Mandatory => GetElementFlag(BoldElements.befMandatory);
    [Delphi(PortStatus.Ported)] public string Multiplicity => fMultiplicity;
    [Delphi(PortStatus.Ported)] public bool OtherEndOrdered => GetElementFlag(BoldElements.befOtherEndOrdered);
    [Delphi(PortStatus.Ported)] public int OtherIndexInLinkClass => fOtherIndexInLinkClass;
    [Delphi(PortStatus.Ported)] public int OwnIndexInLinkClass => fOwnIndexInLinkClass;
    [Delphi(PortStatus.Ported)] public TBoldMemberRTInfoList? Qualifiers => GetQualifiers();
    [Delphi(PortStatus.Ported)] public TBoldRoleRTInfo? RoleRTInfoOfOtherEnd => fRoleRTInfoOfOtherEnd;
    [Delphi(PortStatus.Ported)] public TBoldRoleType RoleType => fRoleType;
    [Delphi(PortStatus.Ported)] public TChangeableKind Changeability => fChangeability;
    [Delphi(PortStatus.Ported)] public TAggregationKind Aggregation => fAggregation;
    [Delphi(PortStatus.Ported)] public TDeleteAction DeleteAction => fDeleteAction;
    [Delphi(PortStatus.Ported)] public TBoldAssociationEndDefaultRegionMode DefaultRegionMode => GetDefaultRegionMode();
    [Delphi(PortStatus.Ported)] public string AssociationStereotype => fAssociationStereotype;
}

/// <summary>A list type: Collection(element type). The untyped list has no element type.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldListTypeInfo : TBoldElementTypeInfo
{
    private readonly TClass? fListClass;
    private readonly TBoldElementTypeInfo? fListElementTypeInfo;

    [Delphi(PortStatus.Ported)]
    protected internal TBoldListTypeInfo(TBoldElementTypeInfo? ListElementTypeInfo, TBoldSystemTypeInfo SystemTypeInfo, TClass? ListClass)
        : base(ListElementTypeInfo is not null ? ListElementTypeInfo.ModelName + "List" : "Collection()",
            ListElementTypeInfo is not null ? "Collection(" + ListElementTypeInfo.ExpressionName + ")" : "Collection()",
            ListElementTypeInfo is not null ? ListElementTypeInfo.DelphiName + "List" : "Collection()",
            SystemTypeInfo)
    {
        fListElementTypeInfo = ListElementTypeInfo;
        SetValueType(TBoldValueTypeSet.bvtList);
        fListClass = ListClass;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => SystemTypeInfo!.BoldType;

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) =>
        ListElementTypeInfo is not null ? "Collection(" + ListElementTypeInfo.AsString + ")" : "Collection()";

    /// <summary>A list conforms to the untyped list and to lists of types its element type conforms to.</summary>
    [Delphi(PortStatus.Ported)]
    public override bool ConformsTo(TBoldElementTypeInfo? CompareElement) =>
        CompareElement is TBoldListTypeInfo other &&
        (other.ListElementTypeInfo is null || (ListElementTypeInfo is not null && ListElementTypeInfo.ConformsTo(other.ListElementTypeInfo)));

    [Delphi(PortStatus.Ported)] public TClass? ListClass => fListClass;
    [Delphi(PortStatus.Ported)] public TBoldElementTypeInfo? ListElementTypeInfo => fListElementTypeInfo;
}

/// <summary>An attribute at runtime: its attribute type, stream name, length, initial value and whether it allows null.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAttributeRTInfo : TBoldMemberRTInfo
{
    private readonly int FLength;
    private readonly string fInitialvalue = "";

    /// <summary>Creates the attribute; a missing type mapping or attribute type is logged as an initialization error.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal TBoldAttributeRTInfo(TBoldClassTypeInfo ClassTypeInfo, TMoldAttribute MoldAttribute, TBoldTypeNameDictionary TypeNameDictionary)
        : base(ClassTypeInfo, MoldAttribute, TypeNameDictionary)
    {
        SetElementFlag(BoldElements.befIsStoredInObject, Persistent);
        SetElementFlag(BoldElements.befAllowNull, MoldAttribute.AllowNull);
        SetElementFlag(BoldElements.befHasInitalvalue, fInitialvalue != "");
        SetElementFlag(BoldElements.befMemberToBeRemoved, MoldAttribute.EvolutionState == TBoldEvolutionState.esToBeRemoved);
        FLength = MoldAttribute.Length;
        fInitialvalue = MoldAttribute.InitialValue;
        if (fInitialvalue != "")
        {
            SetElementFlag(BoldElements.befHasInitalvalue, true);
        }

        TBoldSystemTypeInfo system = ClassTypeInfo.SystemTypeInfo;
        TBoldTypeNameMapping? Mapping = TypeNameDictionary.MappingForModelName(MoldAttribute.BoldType);
        if (Mapping is null)
        {
            system.InitializationError(BoldCoreConsts.sCannotFindAttributeMapping,
                MoldAttribute.MoldClass!.ExpandedExpressionName, MoldAttribute.ExpandedExpressionName, MoldAttribute.BoldType);
            return;
        }

        fBoldType = system.AttributeTypeInfoByExpressionName(Mapping.ExpressionName);
        fStreamName = Mapping.ExpandedContentsName;
        if (fBoldType is null)
        {
            system.InitializationError(BoldCoreConsts.sUnableToFindBoldTypeForAttribute,
                MoldAttribute.MoldClass!.ExpandedExpressionName, MoldAttribute.ExpandedExpressionName, Mapping.ExpressionName);
        }

        if (fBoldType is not null && ((TBoldAttributeTypeInfo)fBoldType).AttributeClass is null)
        {
            system.InitializationError(BoldCoreConsts.sAttributeHasNoDelphiType,
                MoldAttribute.MoldClass!.ExpandedExpressionName, MoldAttribute.ExpandedExpressionName, MoldAttribute.BoldType);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TClass? GetMemberClass() => ((TBoldAttributeTypeInfo)BoldType!).AttributeClass;

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) => ClassTypeInfo.AsString + "." + ExpressionName;

    [Delphi(PortStatus.Ported)] public bool AllowNull => GetElementFlag(BoldElements.befAllowNull);
    [Delphi(PortStatus.Ported)] public int Length => FLength;
    [Delphi(PortStatus.Ported)] public string InitialValue => fInitialvalue;
    [Delphi(PortStatus.Ported)] public bool HasInitialValue => GetElementFlag(BoldElements.befHasInitalvalue);
}

/// <summary>An attribute type: its attribute class (null when none is registered), supertype and whether it is abstract.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAttributeTypeInfo : TBoldElementTypeInfo
{
    private readonly TClass? fAttributeClass;
    private readonly bool fIsAbstract;
    private readonly TBoldAttributeTypeInfo? fSuperAttributeTypeInfo;

    [Delphi(PortStatus.Ported)]
    protected internal TBoldAttributeTypeInfo(string ModelName, string ExpressionName, TClass? AttributeClass, TBoldAttributeTypeInfo? SuperType,
        string FallBackDelphiName, TBoldSystemTypeInfo SystemTypeInfo, bool IsAbstract)
        : base(ModelName, ExpressionName, AttributeClass is null ? FallBackDelphiName : AttributeClass.Name, SystemTypeInfo)
    {
        fIsAbstract = IsAbstract;
        fAttributeClass = AttributeClass;
        SetValueType(TBoldValueTypeSet.bvtAttr);
        fSuperAttributeTypeInfo = SuperType;
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => SystemTypeInfo!.BoldType;

    /// <summary>True for this type and its supertypes.</summary>
    [Delphi(PortStatus.Ported)]
    public bool BoldIsA(TBoldElementTypeInfo? aType)
    {
        if (aType is TBoldAttributeTypeInfo)
        {
            if (aType == this)
            {
                return true;
            }

            if (SuperAttributeTypeInfo is not null)
            {
                return SuperAttributeTypeInfo.BoldIsA(aType);
            }
        }

        return false;
    }

    /// <summary>
    /// True when the attribute class inherits from the other's, for lists of such types, and for integer against float or
    /// currency.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public override bool ConformsTo(TBoldElementTypeInfo? CompareElement)
    {
        bool result = false;
        if (CompareElement is TBoldAttributeTypeInfo other)
        {
            result = AttributeClass is not null && other.AttributeClass is not null &&
                (AttributeClass == other.AttributeClass || AttributeClass.IsSubclassOf(other.AttributeClass));
        }
        else if (CompareElement is TBoldListTypeInfo list)
        {
            result = list.ListElementTypeInfo is null || ConformsTo(list.ListElementTypeInfo);
        }

        if (!result && SysUtils.SameText(ExpressionName, "integer") &&
            (SysUtils.SameText(CompareElement!.ExpressionName, "float") || SysUtils.SameText(CompareElement.ExpressionName, "currency")))
        {
            result = true;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] public TClass? AttributeClass => fAttributeClass;
    [Delphi(PortStatus.Ported)] public bool IsAbstract => fIsAbstract;
    [Delphi(PortStatus.Ported)] public TBoldAttributeTypeInfo? SuperAttributeTypeInfo => fSuperAttributeTypeInfo;
}

/// <summary>A method of a class: its signature, parameters, return type and the class it is inherited from.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMethodRTInfo : TBoldMetaElementWithConstraint
{
    private readonly TBoldClassTypeInfo fClassTypeInfo;
    private readonly TVisibilityKind fVisibility;
    private readonly TDelphiFunctionType FFuncType;
    private readonly int fInheritedFrom;
    private readonly bool FIsClassMethod;
    private TStringList? fParameterList;
    private readonly string FReturnType;
    private readonly string fSignature;
    private readonly int fDispId;

    [Delphi(PortStatus.Ported)]
    public TBoldMethodRTInfo(TBoldClassTypeInfo ClassTypeInfo, TMoldMethod moldMethod, int Inheritedfrom, TBoldTypeNameDictionary? TypeNameDictionary)
        : base(moldMethod, moldMethod.Name, moldMethod.ExpandedExpressionName, moldMethod.ExpandedDelphiName, ClassTypeInfo.SystemTypeInfo)
    {
        fVisibility = moldMethod.Visibility;
        fDispId = moldMethod.HasDispId ? moldMethod.DispId : -1;
        fClassTypeInfo = ClassTypeInfo;
        FIsClassMethod = moldMethod.IsClassMethod;
        FReturnType = moldMethod.ReturnType;
        FFuncType = moldMethod.FuncType;
        fInheritedFrom = Inheritedfrom;
        ClassTypeInfo.Methods.Add(this);
        fSignature = moldMethod.Signature;
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        fParameterList = null;
        base.Dispose();
    }

    /// <summary>
    /// Splits the signature "a, b: T; c: U" into "name: type" lines, replacing a model class name by its Delphi name.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void EnsureParameterLIst(string Signature)
    {
        if (fParameterList is not null)
        {
            return;
        }

        fParameterList = new TStringList();
        string tempSign = Signature.Trim();
        if (tempSign == "")
        {
            return;
        }

        if (tempSign[^1] != ';')
        {
            tempSign += ';';
        }

        while (tempSign != "")
        {
            string Parameter = tempSign.Substring(0, tempSign.IndexOf(';'));
            string pType = Max255(Parameter.Substring(Parameter.IndexOf(':') + 1)).Trim();
            TBoldClassTypeInfo? pClass = ClassTypeInfo.SystemTypeInfo.ClassTypeInfoByModelName(pType);
            if (pClass is not null)
            {
                pType = pClass.DelphiName;
            }

            // Comma-separated names share a type.
            int comma;
            while ((comma = Parameter.IndexOf(',')) >= 0)
            {
                string commaName = Parameter.Substring(0, comma).Trim();
                fParameterList.Add(commaName + ": " + pType);
                Parameter = Parameter.Substring(comma + 1).Trim();
            }

            int colon = Parameter.IndexOf(':');
            string pName = colon >= 0 ? Parameter.Substring(0, colon).Trim() : "";
            fParameterList.Add(pName + ": " + pType);
            tempSign = tempSign.Substring(tempSign.IndexOf(';') + 1);
        }
    }

    /// <summary>At most 255 characters, as Delphi's Copy(s, i, 255).</summary>
    private static string Max255(string s) => s.Length > 255 ? s.Substring(0, 255) : s;

    [Delphi(PortStatus.Ported)]
    private bool getIsInherited() => fInheritedFrom != -1;

    [Delphi(PortStatus.Ported)]
    private int getNumberOfParameters() => ParameterList().Count;

    [Delphi(PortStatus.Ported)]
    private string getParameters(int Number)
    {
        if (Number >= ParameterList().Count)
        {
            throw new EBoldInternal("Invalid argument to GetParameters");
        }

        string p = ParameterList()[Number];
        int colon = p.IndexOf(':');
        return (colon >= 0 ? p.Substring(0, colon) : "").Trim();
    }

    [Delphi(PortStatus.Ported)]
    private string getParameterTypes(int Number)
    {
        if (Number >= ParameterList().Count)
        {
            throw new EBoldInternal("Invalid argument to GetParameterTypes");
        }

        string p = ParameterList()[Number];
        return Max255(p.Substring(p.IndexOf(':') + 1)).Trim();
    }

    [Delphi(PortStatus.Ported)]
    private string GetSignature() => BoldUtils.BoldSeparateStringList(ParameterList(), "; ", "", "");

    [Delphi(PortStatus.Ported)]
    private TStringList ParameterList()
    {
        if (fParameterList is null)
        {
            EnsureParameterLIst(fSignature);
        }

        return fParameterList!;
    }

    [Delphi(PortStatus.Ported)]
    public override string GetStringRepresentation(TBoldRepresentation Representation) =>
        ClassTypeInfo.AsString + "." + ExpressionName + "(" + Signature + ")";

    /// <summary>The type named by the return type, or null.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldElementTypeInfo? GetBoldType() => ClassTypeInfo.SystemTypeInfo.ElementTypeInfoByExpressionName(ReturnType);

    [Delphi(PortStatus.Ported)] public TDelphiFunctionType FuncType => FFuncType;
    [Delphi(PortStatus.Ported)] public int Inheritedfrom => fInheritedFrom;
    [Delphi(PortStatus.Ported)] public bool IsClassmethod => FIsClassMethod;
    [Delphi(PortStatus.Ported)] public bool IsInherited => getIsInherited();
    [Delphi(PortStatus.Ported)] public int NumberOfParameters => getNumberOfParameters();
    [Delphi(PortStatus.Ported)] public string Parameters(int Number) => getParameters(Number);
    [Delphi(PortStatus.Ported)] public string ParameterTypes(int Number) => getParameterTypes(Number);
    [Delphi(PortStatus.Ported)] public string ReturnType => FReturnType;
    [Delphi(PortStatus.Ported)] public string Signature => GetSignature();
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo ClassTypeInfo => fClassTypeInfo;
    [Delphi(PortStatus.Ported)] public int DispId => fDispId;
}

/// <summary>An OCL constraint with its message; its type is the meta type.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldConstraintRTInfo : TBoldMetaElementWithConstraint
{
    private readonly TBoldTypeTypeInfo? fBoldType;
    private readonly string fExpression;
    private readonly string fDescription;

    [Delphi(PortStatus.Ported)]
    public TBoldConstraintRTInfo(TMoldElement? MoldElement, string ModelName, string ExpressionName, string DelphiName, TBoldSystemTypeInfo SystemTypeInfo,
        string Expression, string Description)
        : base(MoldElement, ModelName, ExpressionName, DelphiName, SystemTypeInfo)
    {
        fExpression = Expression;
        fDescription = Description;
        fBoldType = SystemTypeInfo.TypeTypeInfo;
    }

    [Delphi(PortStatus.Ported, Note = "Protected here because the method it overrides is; public in Delphi.")]
    protected override TBoldElementTypeInfo? GetBoldType() => fBoldType;

    [Delphi(PortStatus.Ported)] public string ConstraintExpression => fExpression;
    [Delphi(PortStatus.Ported)] public string ConstraintMessage => fDescription;
}

/// <summary>Unit-level constants of BoldSystemRT, and the classes it needs from units ported in later milestones.</summary>
public static partial class BoldSystemRT
{
    [Delphi(PortStatus.Ported)] public const int LITE_VERSION_CLASS_LIMIT = 15;
    [Delphi(PortStatus.Ported)] public const int CLASS_TYPE_INFO_MEM_SIZE = 24;

    // Delphi refers to these classes from BoldSystem, BoldTypeList and BoldOcl directly. Those units are ported in M3
    // and M4, in this assembly; they will set these (or the references here become typeof(...)) when they are.
    internal static TClass? TBoldObjectClass = null;
    internal static TClass? TBoldObjectListClass = null;
    internal static TClass? TBoldObjectReferenceClass = null;
    internal static TClass? TBoldAttributeClass = null;
    internal static TClass? TBoldMemberListClass = null;
    internal static TClass? TBoldTypeListClass = null;
    internal static Func<TBoldSystemTypeInfo, TBoldEvaluator>? CreateOclEvaluator = null;
}
