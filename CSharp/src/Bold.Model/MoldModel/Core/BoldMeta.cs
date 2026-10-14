// Delphi: Source/MoldModel/Core/BoldMeta.pas
// Port map: CSharp/port/units/BoldMeta.md

using static Bold.BoldDefaultTaggedValues;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldMeta", Symbol = "TMoldElementClass", Note = "Global using alias of System.Type, in BoldMeta.Aliases.cs.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldMeta", Symbol = "TBoldDefaultDeleteActionArray", Note = "A TDeleteAction[] indexed by (int)TAggregationKind.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldMeta", Symbol = "TModelOnChange", Note = "Global using alias of System.Action, in BoldMeta.Aliases.cs.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public enum TBoldDispIdAssigningState { dasNotStarted, dasAssigning, dasDone }

/// <summary>
/// An element of the Mold model: a name, constraints, a stereotype and two sets of tagged values. Bold tagged values
/// (the ones Bold defines, such as DelphiName) are stored only when they differ from the default for the element kind;
/// other tagged values are stored as given. "Bold."-prefixed tags address the Bold tagged values.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TMoldElement : TBoldMemoryManagedObject, IDisposable
{
    private string FName;
    private TStrings? fConstraints;
    private string fStereotype = "";
    private TBoldNamedValueList? fTaggedValues;
    private TBoldNamedValueList? fBoldTaggedValues;
    internal int fDispId;
    private TBoldTaggedValueList? FDefaultBoldTVList;

    /// <summary>Creates the element. Subclasses add themselves to their parent's list.</summary>
    [Delphi(PortStatus.Ported)]
    protected TMoldElement(TMoldElement? Parent, string Name)
    {
        FName = Name ?? "";
        fConstraints = new TStringList();
        fDispId = -1;
    }

    /// <summary>Frees the tagged values and constraints. Subclasses unlink themselves first.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void Dispose()
    {
        SysUtils.FreeAndNil(ref fTaggedValues);
        SysUtils.FreeAndNil(ref fBoldTaggedValues);
        fConstraints = null;
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private string GetExpandedDelphiName() =>
        BoldNameExpander.BoldExpandName(DelphiName, Name, TExpansionType.xtDelphi, -1, Model.NationalCharConversion);

    /// <summary>Stores a Bold tagged value, trimmed; a value equal to the default is not stored (an earlier value stays).</summary>
    [Delphi(PortStatus.Ported)]
    private void SetBoldTVByName(string Tag, string value)
    {
        string trimmedValue = BoldUtils.BoldTrim(value);
        TBoldTaggedValueDefinition? definition = DefaultBoldTVList.DefinitionForTag(Tag);
        if (definition is not null && definition.DefaultValue == trimmedValue)
        {
            // do nothing
        }
        else
        {
            BoldTaggedValues.SetValueByname(Tag, trimmedValue);
        }
    }

    /// <summary>The stored Bold tagged value, else the default for the element kind, else "".</summary>
    [Delphi(PortStatus.Ported)]
    private string GetBoldTVByName(string Tag)
    {
        if (fBoldTaggedValues is not null)
        {
            TBoldNamedValueListEntry? entry = BoldTaggedValues.ItemByName(Tag);
            if (entry is not null)
            {
                return entry.Value;
            }
        }

        return DefaultBoldTVList.DefinitionForTag(Tag)?.DefaultValue ?? "";
    }

    [Delphi(PortStatus.Ported)]
    private string GetStdTVByName(string Tag) => fTaggedValues is not null ? TaggedValues.ValueByName(Tag) : "";

    [Delphi(PortStatus.Ported)]
    private string GetDelphiName() => GetBoldTVByName(TAG_DELPHINAME);

    /// <summary>The ExpressionName tagged value, or &lt;Name&gt; when the element kind has none (associations).</summary>
    [Delphi(PortStatus.Ported)]
    private string GetExpressionName()
    {
        string result = GetBoldTVByName(TAG_EXPRESSIONNAME);
        return result == "" ? TV_NAME : result;
    }

    [Delphi(PortStatus.Ported)]
    private string GetPMapperName()
    {
        string result = GetBoldTVByName(TAG_PMAPPERNAME);
        return result == "" ? BoldDefs.DEFAULTNAMELITERAL : result;
    }

    [Delphi(PortStatus.Ported)]
    private void SetStdTVByName(string Tag, string Value) => SetTVByName(Tag, Value);

    /// <summary>The COM dispatch id; assigns the model's ids on first use. Raises EBold when the element has none.</summary>
    [Delphi(PortStatus.Ported)]
    private int GetDispId()
    {
        if (!HasDispId)
        {
            throw new EBold(BoldMoldConsts.sMemberHasNoDispID);
        }

        if (!Model.DispIdsAssigned)
        {
            Model.AssignDispIds();
        }

        return fDispId;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldTaggedValueList GetDefaultBoldTVList()
    {
        FDefaultBoldTVList ??= BoldDefaultTaggedValues.BoldDefaultTaggedValueList().ListForClassName(UMLClassName);
        return FDefaultBoldTVList;
    }

    /// <summary>A "Bold."-prefixed tag reads the Bold tagged value; any other tag reads the stored tagged value or "".</summary>
    [Delphi(PortStatus.Ported)]
    private string GetTVByName(string Tag)
    {
        if (BoldUtils.BoldIsPrefix(Tag, BoldDefs.BOLDTVPREFIX))
        {
            return GetBoldTVByName(SysUtils.Copy(Tag, BoldDefs.BOLDTVPREFIX.Length + 1, SysUtils.MaxInt));
        }

        return fTaggedValues is not null && TaggedValues.ItemByName(Tag) is not null ? TaggedValues.ValueByName(Tag) : "";
    }

    [Delphi(PortStatus.Ported)]
    private void SetTVByName(string Tag, string Value)
    {
        string trimmedValue = BoldUtils.BoldTrim(Value);
        if (BoldUtils.BoldIsPrefix(Tag, BoldDefs.BOLDTVPREFIX))
        {
            SetBoldTVByName(SysUtils.Copy(Tag, BoldDefs.BOLDTVPREFIX.Length + 1, SysUtils.MaxInt), trimmedValue);
        }
        else
        {
            TaggedValues.SetValueByname(Tag, trimmedValue);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldNamedValueList GetTaggedValues() => fTaggedValues ??= new TBoldNamedValueList();

    [Delphi(PortStatus.Ported)]
    private TBoldNamedValueList GetBoldTaggedValues() => fBoldTaggedValues ??= new TBoldNamedValueList();

    /// <summary>The stored tagged values as comma text; Bold tagged values get the "Bold." prefix.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetNonDefaultTaggedValuesCommaText()
    {
        var stringList = new TStringList();
        if (fTaggedValues is not null)
        {
            TaggedValues.AddToStrings(stringList);
        }

        if (fBoldTaggedValues is not null)
        {
            for (int i = 0; i < BoldTaggedValues.Count; i++)
            {
                stringList.Add(BoldDefs.BOLDTVPREFIX + BoldTaggedValues[i].Name + "=" + BoldTaggedValues[i].Value);
            }
        }

        return stringList.CommaText;
    }

    /// <summary>Reads tagged values from comma text; the old tag "Persistence" becomes "persistence".</summary>
    [Delphi(PortStatus.Ported)]
    private void SetNonDefaultTaggedValuesCommaText(string Value)
    {
        var stringList = new TStringList { CommaText = Value };
        for (int i = 0; i < stringList.Count; i++)
        {
            string line = stringList[i];
            int equalPos = SysUtils.Pos("=", line);
            System.Diagnostics.Debug.Assert(equalPos != 0);
            string name = SysUtils.Copy(line, 1, equalPos - 1);
            if (name == "Persistence")
            {
                name = BoldUMLTaggedValues.TAG_PERSISTENCE;
            }

            SetTVByName(name, SysUtils.Copy(line, equalPos + 1, SysUtils.MaxInt));
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetHasDispId() => false;

    [Delphi(PortStatus.Ported)]
    protected abstract TMoldModel GetModel();

    [Delphi(PortStatus.Ported)]
    protected virtual void SetName(string S)
    {
        if (FName != S)
        {
            FName = S;
            NameChanged();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual string GetExpandedExpressionName() =>
        BoldNameExpander.BoldExpandName(ExpressionName, Name, TExpansionType.xtExpression, -1, Model.NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    protected TBoldEvolutionState GetEvolutionState() =>
        TBoldTaggedValueSupport.StringToEvolutionState(GetBoldTVByName(TAG_EVOLUTIONSTATE));

    [Delphi(PortStatus.Ported)]
    protected string GetFormerNames() => GetBoldTVByName(TAG_FORMERNAMES);

    [Delphi(PortStatus.Ported)]
    protected abstract string GetUMLClassName();

    [Delphi(PortStatus.Ported)]
    protected string UMLClassName => GetUMLClassName();

    [Delphi(PortStatus.Ported)]
    protected TBoldTaggedValueList DefaultBoldTVList => GetDefaultBoldTVList();

    [Delphi(PortStatus.Ported)]
    protected TBoldNamedValueList TaggedValues => GetTaggedValues();

    [Delphi(PortStatus.Ported)]
    protected TBoldNamedValueList BoldTaggedValues => GetBoldTaggedValues();

    /// <summary>Called after the name changed, so lists re-index the element.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void NameChanged()
    {
        // do nothing
    }

    /// <summary>A new list of the non-Bold tagged values as "tag=value" lines, or null when there are none.</summary>
    [Delphi(PortStatus.Ported)]
    public TStringList? GetAllNonBoldTaggedValues()
    {
        if (fTaggedValues is null)
        {
            return null;
        }

        var result = new TStringList();
        TaggedValues.AddToStrings(result);
        return result;
    }

    /// <summary>Adds every Bold tagged value (prefixed, with its value or default) and every other tagged value.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddAllTaggedValues(TStrings Tags, TStrings Values)
    {
        for (int i = 0; i < DefaultBoldTVList.Count; i++)
        {
            string tag = DefaultBoldTVList.Definition(i).Tag;
            Tags.Add(BoldDefs.BOLDTVPREFIX + tag);
            TBoldNamedValueListEntry? entry = fBoldTaggedValues is not null ? BoldTaggedValues.ItemByName(tag) : null;
            Values.Add(entry is not null ? entry.Value : DefaultBoldTVList.Definition(i).DefaultValue);
        }

        if (fTaggedValues is not null)
        {
            for (int i = 0; i < TaggedValues.Count; i++)
            {
                Tags.Add(TaggedValues[i].Name);
                Values.Add(TaggedValues[i].Value);
            }
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TMoldElement.name", Note = "Named Name, as the Delphi code mostly spells it.")]
    public string Name
    {
        get => FName;
        set => SetName(value);
    }

    [Delphi(PortStatus.Ported)]
    public string DelphiName => GetDelphiName();

    [Delphi(PortStatus.Ported)]
    public string ExpandedDelphiName => GetExpandedDelphiName();

    [Delphi(PortStatus.Ported)]
    public string ExpressionName => GetExpressionName();

    [Delphi(PortStatus.Ported)]
    public string ExpandedExpressionName => GetExpandedExpressionName();

    [Delphi(PortStatus.Ported)]
    public TMoldModel Model => GetModel();

    [Delphi(PortStatus.Ported)]
    public string PMapperName => GetPMapperName();

    [Delphi(PortStatus.Ported)]
    public TStrings Constraints => fConstraints!;

    [Delphi(PortStatus.Ported)]
    public string Stereotype
    {
        get => fStereotype;
        set => fStereotype = value ?? "";
    }

    [Delphi(PortStatus.Ported)]
    public string BoldTVByName(string Tag) => GetBoldTVByName(Tag);

    /// <summary>The setter of BoldTVByName.</summary>
    public void SetBoldTV(string Tag, string Value) => SetBoldTVByName(Tag, Value);

    [Delphi(PortStatus.Ported)]
    public string StdTVByName(string Tag) => GetStdTVByName(Tag);

    /// <summary>The setter of StdTVByName.</summary>
    public void SetStdTV(string Tag, string Value) => SetStdTVByName(Tag, Value);

    [Delphi(PortStatus.Ported)]
    public string TVByName(string Tag) => GetTVByName(Tag);

    /// <summary>The setter of TVByName.</summary>
    public void SetTV(string Tag, string Value) => SetTVByName(Tag, Value);

    [Delphi(PortStatus.Ported)]
    public string NonDefaultTaggedValuesCommaText
    {
        get => GetNonDefaultTaggedValuesCommaText();
        set => SetNonDefaultTaggedValuesCommaText(value);
    }

    [Delphi(PortStatus.Ported)]
    public bool HasDispId => GetHasDispId();

    [Delphi(PortStatus.Ported)]
    public int DispId => GetDispId();
}

/// <summary>
/// The root of a Mold model: classes (with a root class created with the model), associations, components (units)
/// and model-wide settings read from tagged values.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TMoldModel : TMoldElement
{
    private bool fLinkRolesEnsured;
    private bool fTopSorted;
    private TMoldClassList? FClasses;
    private TMoldAssociationList? FAssociations;
    private TMoldClass FRootClass;
    private TMoldComponentList? fComponents;
    private TBoldTypeNameDictionary? fTypeNameDictionary;
    internal TBoldDispIdAssigningState fDispIdAssigningState;
    private readonly TDeleteAction[] fDefaultDeleteAction = new TDeleteAction[3];
    internal bool fIsDestroying;

    /// <summary>Creates the model with default delete actions and a root class named "&lt;Name&gt;Root".</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldModel(TMoldElement? Parent, string Name)
        : base(Parent, Name)
    {
        SetDefaultDeleteAction(TAggregationKind.akNone, TDeleteAction.daAllow);
        SetDefaultDeleteAction(TAggregationKind.akAggregate, TDeleteAction.daProhibit);
        SetDefaultDeleteAction(TAggregationKind.akComposite, TDeleteAction.daCascade);
        fLinkRolesEnsured = false;
        FClasses = new TMoldClassList();
        FAssociations = new TMoldAssociationList();
        fDispIdAssigningState = TBoldDispIdAssigningState.dasNotStarted;
        SetBoldTV(TAG_DELPHINAME, TV_NAME);
        FRootClass = new TMoldClass(this, SysUtils.Format("%sRoot", Name));
    }

    /// <summary>Frees associations first, then classes and components.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        fIsDestroying = true;
        SysUtils.FreeAndNil(ref FAssociations);
        SysUtils.FreeAndNil(ref FClasses);
        if (fComponents is not null)
        {
            for (int i = 0; i < fComponents.Count; i++)
            {
                fComponents[i].Dispose();
            }
        }

        fComponents = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private uint CalculateCRC()
    {
        EnsureTopSorted();
        uint result = 0;
        for (int i = 0; i < Classes.Count; i++)
        {
            result = BoldMeta.SumCRCs(result, Classes[i].CalculateCRC());
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private void SetRootClass(TMoldClass? NewRootClass)
    {
        if (NewRootClass is not null)
        {
            FRootClass = NewRootClass;
        }
    }

    [Delphi(PortStatus.Ported)]
    private string GetUnitName() => BoldTVByName(TAG_UNITNAME);

    [Delphi(PortStatus.Ported)]
    private TMoldComponentList GetComponents()
    {
        if (fComponents is null)
        {
            fComponents = new TMoldComponentList();
            for (int i = 0; i < Classes.Count; i++)
            {
                if (Classes[i].ExpandedUnitName != "")
                {
                    EnsureComponent(Classes[i].ExpandedUnitName);
                }
            }

            EnsureComponent(MainComponent.Name);
        }

        return fComponents;
    }

    /// <summary>The component named by the model's unit name, or by its Delphi name when the unit name is empty.</summary>
    [Delphi(PortStatus.Ported)]
    private TMoldComponent GetMainComponent()
    {
        string unitName = ExpandedUnitName;
        if (unitName == "")
        {
            unitName = ExpandedDelphiName;
        }

        return EnsureComponent(unitName);
    }

    [Delphi(PortStatus.Ported)] private string GetInterfaceUses() => BoldTVByName(TAG_INTERFACEUSES);
    [Delphi(PortStatus.Ported)] private bool GetUseGlobalId() => TVIsTrue(BoldTVByName(TAG_USEGLOBALID));
    [Delphi(PortStatus.Ported)] private bool GetUseReadOnly() => TVIsTrue(BoldTVByName(TAG_USEREADONLY));
    [Delphi(PortStatus.Ported)] private bool GetUseModelVersion() => TVIsTrue(BoldTVByName(TAG_USEMODELVERSION));
    [Delphi(PortStatus.Ported)] private int GetModelVersion() => SysUtils.StrToIntDef(BoldTVByName(TAG_MODELVERSION), -1);
    [Delphi(PortStatus.Ported)] private bool GetUseTimestamp() => TVIsTrue(BoldTVByName(TAG_USETIMESTAMP));
    [Delphi(PortStatus.Ported)] private bool GetUseXFiles() => TVIsTrue(BoldTVByName(TAG_USEXFILES));
    [Delphi(PortStatus.Ported)] private bool GetUseClockLog() => TVIsTrue(BoldTVByName(TAG_USECLOCKLOG));
    [Delphi(PortStatus.Ported)] private string GetImplementationUses() => BoldTVByName(TAG_IMPLEMENTATIONUSES);
    [Delphi(PortStatus.Ported)] private string GetGUID() => BoldTVByName(TAG_GUID);
    [Delphi(PortStatus.Ported)] private string GetTypeLibVersion() => BoldTVByName(TAG_TYPELIBVERSION);

    [Delphi(PortStatus.Ported)]
    private TBoldOptimisticLockingMode GetOptimisticLocking() =>
        TBoldTaggedValueSupport.StringToOptimisticLockingMode(BoldTVByName(TAG_OPTIMISTICLOCKING));

    [Delphi(PortStatus.Ported)] private bool GetUpdateWholeObjects() => TVIsTrue(BoldTVByName(TAG_UPDATEWHOLEOBJECTS));

    [Delphi(PortStatus.Ported)]
    private string GetExpandedUnitName() =>
        BoldNameExpander.BoldExpandName(UnitName, Name, TExpansionType.xtDelphi, -1, Model.NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    private void RemoveAssoc(TMoldAssociation MoldAssoc, TMoldClassList ClassList, TMoldAssociationList AssocList)
    {
        if (AssocList.IndexOf(MoldAssoc) == -1)
        {
            AssocList.Add(MoldAssoc);
            if (MoldAssoc.LinkClass is not null)
            {
                RemoveClass(MoldAssoc.LinkClass, ClassList, AssocList);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private void RemoveClass(TMoldClass MoldClass, TMoldClassList ClassList, TMoldAssociationList AssocList)
    {
        if (ClassList.IndexOf(MoldClass) == -1)
        {
            ClassList.Add(MoldClass);
            for (int i = 0; i < MoldClass.Roles.Count; i++)
            {
                RemoveAssoc(MoldClass.Roles[i].Association!, ClassList, AssocList);
            }

            if (MoldClass.Association is not null)
            {
                RemoveAssoc(MoldClass.Association, ClassList, AssocList);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldNationalCharConversion GetNationalCharConversion() =>
        TBoldTaggedValueSupport.StringToNationalCharConversion(BoldTVByName(TAG_NATIONALCHARCONVERSION));

    [Delphi(PortStatus.Ported)]
    private bool GetDispIdsAssigned() => fDispIdAssigningState == TBoldDispIdAssigningState.dasDone;

    [Delphi(PortStatus.Ported)]
    private TDeleteAction GetDefaultDeleteAction(TAggregationKind AggregationKind) => fDefaultDeleteAction[(int)AggregationKind];

    [Delphi(PortStatus.Ported)]
    private void SetDefaultDeleteAction(TAggregationKind AggregationKind, TDeleteAction Value) =>
        fDefaultDeleteAction[(int)AggregationKind] = Value;

    [Delphi(PortStatus.Ported)] private string GetRegionDefinitions() => BoldTVByName(TAG_REGIONDEFINITIONS);
    [Delphi(PortStatus.Ported)] private bool GetGenerateDefaultRegions() => TVIsTrue(BoldTVByName(TAG_GENERATEDEFAULTREGIONS));

    [Delphi(PortStatus.Ported)]
    protected override TMoldModel GetModel() => this;

    /// <summary>Gives every class, member and method that can have one a COM dispatch id, in topological class order.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void AssignDispIds()
    {
        fDispIdAssigningState = TBoldDispIdAssigningState.dasAssigning;
        EnsureTopSorted();
        for (int i = 0; i < Classes.Count; i++)
        {
            Classes[i].AssignMemberDispIds();
        }

        fDispIdAssigningState = TBoldDispIdAssigningState.dasDone;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetUMLClassName() => "Model";

    /// <summary>The class named <paramref name="name"/>, created when it does not exist.</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldClass GetClassByName(string name) => Classes.ItemsByName(name) ?? new TMoldClass(this, name);

    [Delphi(PortStatus.Ported)]
    public TMoldRole? FindRoleByClassNameAndName(string boldclassName, string roleName) =>
        Classes.ItemsByName(boldclassName)?.Roles.ItemsByName(roleName);

    /// <summary>Creates the link roles of every association with a link class (once).</summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureLinkRoles()
    {
        if (!fLinkRolesEnsured)
        {
            for (int i = 0; i < Associations.Count; i++)
            {
                Associations[i].Roles[0].EnsureLinkRole();
                Associations[i].Roles[1].EnsureLinkRole();
            }
        }

        fLinkRolesEnsured = true;
    }

    /// <summary>
    /// Orders the class list breadth-first from the root class, with each class's subclasses sorted by name. Classes not
    /// reachable from the root class are dropped from the list.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void EnsureTopSorted()
    {
        if (TopSorted)
        {
            return;
        }

        Classes.OwnsEntries = false;
        Classes.Clear();
        Classes.OwnsEntries = true;
        Classes.Add(RootClass);
        int classIx = 0;
        while (classIx < Classes.Count)
        {
            Classes[classIx].SubClasses.Sort(BoldMeta.ClassesComparer);
            for (int subClassIx = 0; subClassIx < Classes[classIx].SubClasses.Count; subClassIx++)
            {
                Classes.Add(Classes[classIx].SubClasses[subClassIx]);
            }

            classIx++;
        }

        fTopSorted = true;
    }

    /// <summary>Frees the classes and associations whose evolution state is Removed, with everything that depends on them.</summary>
    [Delphi(PortStatus.Ported)]
    public void TrimRemoved()
    {
        if (fLinkRolesEnsured)
        {
            throw new EBold(BoldMoldConsts.sCannotTrimAfterLinkRolesEnsured);
        }

        var classList = new TMoldClassList();
        var assocList = new TMoldAssociationList();
        try
        {
            for (int i = Classes.Count - 1; i >= 0; i--)
            {
                if (Classes[i].EvolutionState == TBoldEvolutionState.esRemoved)
                {
                    RemoveClass(Classes[i], classList, assocList);
                }
            }

            for (int i = Associations.Count - 1; i >= 0; i--)
            {
                if (Associations[i].EvolutionState == TBoldEvolutionState.esRemoved)
                {
                    RemoveAssoc(Associations[i], classList, assocList);
                }
            }
        }
        finally
        {
            // Freeing the lists frees every element in them.
            classList.Dispose();
            assocList.Dispose();
        }

        for (int i = 0; i < Classes.Count; i++)
        {
            Classes[i].TrimRemoved();
        }
    }

    /// <summary>The model checksum, computed once and kept in the CRC tagged value.</summary>
    [Delphi(PortStatus.Ported)]
    public string CRC()
    {
        EnsureCRC();
        return BoldTVByName("CRC");
    }

    [Delphi(PortStatus.Ported)]
    public void EnsureCRC()
    {
        if (BoldTVByName("CRC") == "")
        {
            SetBoldTV("CRC", CalculateCRC().ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Delphi(PortStatus.Ported)] public TMoldClassList Classes => FClasses!;
    [Delphi(PortStatus.Ported)] public TMoldAssociationList Associations => FAssociations!;
    [Delphi(PortStatus.Ported)] public string InterfaceUses => GetInterfaceUses();
    [Delphi(PortStatus.Ported)] public string ImplementationUses => GetImplementationUses();

    [Delphi(PortStatus.Ported)]
    public TMoldClass RootClass
    {
        get => FRootClass;
        set => SetRootClass(value);
    }

    [Delphi(PortStatus.Ported)] public TMoldComponentList Components => GetComponents();
    [Delphi(PortStatus.Ported)] public TMoldComponent MainComponent => GetMainComponent();
    [Delphi(PortStatus.Ported)] public bool UseXFiles => GetUseXFiles();
    [Delphi(PortStatus.Ported)] public bool UseTimestamp => GetUseTimestamp();
    [Delphi(PortStatus.Ported)] public bool UseGlobalId => GetUseGlobalId();
    [Delphi(PortStatus.Ported)] public bool UseReadOnly => GetUseReadOnly();
    [Delphi(PortStatus.Ported)] public bool UseModelVersion => GetUseModelVersion();
    [Delphi(PortStatus.Ported)] public int ModelVersion => GetModelVersion();
    [Delphi(PortStatus.Ported)] public bool UseClockLog => GetUseClockLog();

    [Delphi(PortStatus.Ported)]
    public TMoldComponent? FindComponent(string ComponentName) => Components.ItemsByName(ComponentName);

    [Delphi(PortStatus.Ported)]
    public TMoldComponent? RenameComponent(string OldComponentName, string NewComponentName)
    {
        TMoldComponent? result = FindComponent(OldComponentName);
        if (result is not null)
        {
            result.Name = NewComponentName;
        }

        return result;
    }

    /// <summary>The component with the name, created when missing.</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldComponent EnsureComponent(string ComponentName)
    {
        // Delphi reads the Components property here, which builds the list on first use; building it calls back
        // into EnsureComponent, so the list must exist before the lookup.
        fComponents ??= new TMoldComponentList();
        TMoldComponent? result = fComponents.ItemsByName(ComponentName);
        if (result is null)
        {
            result = new TMoldComponent(this) { Name = ComponentName };
            fComponents.Add(result);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] public string UnitName => GetUnitName();
    [Delphi(PortStatus.Ported)] public string ExpandedUnitName => GetExpandedUnitName();
    [Delphi(PortStatus.Ported)] public string GUID => GetGUID();
    [Delphi(PortStatus.Ported)] public string TypeLibVersion => GetTypeLibVersion();
    [Delphi(PortStatus.Ported)] public TBoldOptimisticLockingMode OptimisticLocking => GetOptimisticLocking();
    [Delphi(PortStatus.Ported)] public bool UpdateWholeObjects => GetUpdateWholeObjects();

    [Delphi(PortStatus.Ported)]
    public TBoldTypeNameDictionary? TypeNameDictionary
    {
        get => fTypeNameDictionary;
        set => fTypeNameDictionary = value;
    }

    [Delphi(PortStatus.Ported)] public bool TopSorted => fTopSorted;
    [Delphi(PortStatus.Ported)] public bool DispIdsAssigned => GetDispIdsAssigned();
    [Delphi(PortStatus.Ported)] public TBoldNationalCharConversion NationalCharConversion => GetNationalCharConversion();

    [Delphi(PortStatus.Ported)]
    public TDeleteAction DefaultDeleteAction(TAggregationKind AggregationKind) => GetDefaultDeleteAction(AggregationKind);

    /// <summary>The setter of DefaultDeleteAction.</summary>
    public void SetDefaultDeleteActionFor(TAggregationKind AggregationKind, TDeleteAction Value) => SetDefaultDeleteAction(AggregationKind, Value);

    [Delphi(PortStatus.Ported)] public string RegionDefinitions => GetRegionDefinitions();
    [Delphi(PortStatus.Ported)] public bool GenerateDefaultRegions => GetGenerateDefaultRegions();
}

/// <summary>An ordered list of model elements with a case-sensitive index by name.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldElementList : TBoldIndexableList
{
    private static int IX_Name = -1;

    /// <summary>The name index of every element list.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TNameIndex")]
    internal sealed class TNameIndex : TBoldCaseSensitiveStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TMoldElement)Item!).Name;
    }

    [Delphi(PortStatus.Ported)]
    public TMoldElementList()
    {
        SetIndexCapacity(2);
        BoldIndexableList.SetIndexVariable(ref IX_Name, AddIndex(new TNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TMoldElement GetItem(int index) => (TMoldElement)base[index]!;

    [Delphi(PortStatus.Ported)]
    private TMoldElement? GetItemByName(string name) => (TMoldElement?)((TNameIndex)Indexes(IX_Name)!).FindByString(name);

    /// <summary>Removes the element without freeing it.</summary>
    [Delphi(PortStatus.Ported)]
    public void RemoveEntryReference(TMoldElement Item)
    {
        bool ownsEntries = OwnsEntries;
        OwnsEntries = false;
        Remove(Item);
        OwnsEntries = ownsEntries;
    }

    /// <summary>The first of prefix1, prefix2, ... that no element is named.</summary>
    [Delphi(PortStatus.Ported)]
    public string MakeUniqueName(string prefix)
    {
        int i = 0;
        string result;
        do
        {
            i++;
            result = SysUtils.Format("%s%d", prefix, i);
        }
        while (GetItemByName(result) is not null);

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public void Add(TMoldElement MoldElement) => base.Add(MoldElement);

    [Delphi(PortStatus.Ported, Symbol = "TMoldElementList.Items")]
    public new TMoldElement this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public TMoldElement? ItemsByName(string name) => GetItemByName(name);
}

/// <summary>A list of classes, indexed also by expanded expression name and expanded Delphi name (both ignoring case).</summary>
[Delphi(PortStatus.Ported)]
public class TMoldClassList : TMoldElementList
{
    private static int IX_ExpressionName = -1;
    private static int IX_DelphiName = -1;

    [Delphi(PortStatus.Ported, Symbol = "TExpressionNameIndex")]
    private sealed class TExpressionNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TExpressionNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TMoldElement)Item!).ExpandedExpressionName;
    }

    [Delphi(PortStatus.Ported, Symbol = "TDelphiNameIndex")]
    private sealed class TDelphiNameIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TDelphiNameIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TMoldElement)Item!).ExpandedDelphiName;
    }

    [Delphi(PortStatus.Ported)]
    public TMoldClassList()
    {
        BoldIndexableList.SetIndexVariable(ref IX_ExpressionName, AddIndex(new TExpressionNameIndex()));
        BoldIndexableList.SetIndexVariable(ref IX_DelphiName, AddIndex(new TDelphiNameIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TMoldClass GetItem(int index) => (TMoldClass)base[index];

    [Delphi(PortStatus.Ported)]
    private TMoldClass? GetItemByName(string name) => (TMoldClass?)base.ItemsByName(name);

    [Delphi(PortStatus.Ported)]
    private TMoldClass? GetItemByExpressionName(string ExpressionName) =>
        (TMoldClass?)((TBoldStringHashIndex)Indexes(IX_ExpressionName)!).FindByString(ExpressionName);

    [Delphi(PortStatus.Ported)]
    private TMoldClass? GetItemByDelphiName(string DelphiName) =>
        (TMoldClass?)((TBoldStringHashIndex)Indexes(IX_DelphiName)!).FindByString(DelphiName);

    [Delphi(PortStatus.Ported, Symbol = "TMoldClassList.Items")]
    public new TMoldClass this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public new TMoldClass? ItemsByName(string name) => GetItemByName(name);

    [Delphi(PortStatus.Ported)]
    public TMoldClass? ItemsByExpressionName(string ExpressionName) => GetItemByExpressionName(ExpressionName);

    [Delphi(PortStatus.Ported)]
    public TMoldClass? ItemsByDelphiName(string DelphiName) => GetItemByDelphiName(DelphiName);
}

[Delphi(PortStatus.Ported)]
public class TMoldMemberList : TMoldElementList
{
    [Delphi(PortStatus.Ported)]
    private TMoldMember GetItem(int index) => (TMoldMember)base[index];

    [Delphi(PortStatus.Ported)]
    private TMoldMember? GetItemByName(string name) => (TMoldMember?)base.ItemsByName(name);

    [Delphi(PortStatus.Ported, Symbol = "TMoldMemberList.Items")]
    public new TMoldMember this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public new TMoldMember? ItemsByName(string name) => GetItemByName(name);
}

[Delphi(PortStatus.Ported)]
public class TMoldRoleList : TMoldMemberList
{
    [Delphi(PortStatus.Ported)]
    private TMoldRole GetItem(int index) => (TMoldRole)base[index];

    [Delphi(PortStatus.Ported)]
    private TMoldRole? GetItemByName(string name) => (TMoldRole?)base.ItemsByName(name);

    [Delphi(PortStatus.Ported, Symbol = "TMoldRoleList.Items")]
    public new TMoldRole this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public new TMoldRole? ItemsByName(string name) => GetItemByName(name);
}

[Delphi(PortStatus.Ported)]
public class TMoldMethodList : TMoldElementList
{
    [Delphi(PortStatus.Ported)]
    private TMoldMethod GetItem(int index) => (TMoldMethod)base[index];

    [Delphi(PortStatus.Ported)]
    private TMoldMethod? GetItemByName(string name) => (TMoldMethod?)base.ItemsByName(name);

    [Delphi(PortStatus.Ported, Symbol = "TMoldMethodList.Items")]
    public new TMoldMethod this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public new TMoldMethod? ItemsByName(string name) => GetItemByName(name);
}

[Delphi(PortStatus.Ported)]
public class TMoldAssociationList : TMoldElementList
{
    [Delphi(PortStatus.Ported)]
    private TMoldAssociation GetItem(int index) => (TMoldAssociation)base[index];

    [Delphi(PortStatus.Ported)]
    private TMoldAssociation? GetItemByName(string name) => (TMoldAssociation?)base.ItemsByName(name);

    [Delphi(PortStatus.Ported, Symbol = "TMoldAssociationList.Items")]
    public new TMoldAssociation this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public new TMoldAssociation? ItemsByName(string name) => GetItemByName(name);
}

[Delphi(PortStatus.Ported)]
public class TMoldAttributeList : TMoldMemberList
{
    [Delphi(PortStatus.Ported)]
    private TMoldAttribute GetItem(int index) => (TMoldAttribute)base[index];

    [Delphi(PortStatus.Ported)]
    private TMoldAttribute? GetItemByName(string name) => (TMoldAttribute?)base.ItemsByName(name);

    [Delphi(PortStatus.Ported, Symbol = "TMoldAttributeList.Items")]
    public new TMoldAttribute this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public new TMoldAttribute? ItemsByName(string name) => GetItemByName(name);
}

[Delphi(PortStatus.Ported)]
public class TMoldQualifierList : TMoldElementList
{
    [Delphi(PortStatus.Ported)]
    private TMoldQualifier GetItem(int index) => (TMoldQualifier)base[index];

    [Delphi(PortStatus.Ported)]
    private TMoldQualifier? GetItemByName(string name) => (TMoldQualifier?)base.ItemsByName(name);

    [Delphi(PortStatus.Ported, Symbol = "TMoldQualifierList.Items")]
    public new TMoldQualifier this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public new TMoldQualifier? ItemsByName(string name) => GetItemByName(name);
}

/// <summary>A class of the model: superclass, subclasses, attributes, methods, the roles it takes part in, and its settings.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldClass : TMoldElement
{
    private TMoldModel FModel = null!;
    private TMoldClass? FSuperClass;
    private TMoldClassList? FSubClasses;
    private TMoldAttributeList? FAttributes;
    private TMoldMethodList? FMethods;
    private TMoldAssociation? FAssociation;
    private TMoldRoleList? FRoles;
    private bool FIsAbstract;
    private TMoldMemberList? fAllBoldMembers;
    private TMoldAttributeList? fAllNativeAttributes;
    private int fFirstOwnBoldMemberIndex;
    private int fFirstOwnNativeAttributeIndex;
    private TMoldMethodList? fAllAutoOverrideMethods;
    private bool fPersistent;
    private int fLastDispId;
    private TStringList? fAllPossibleNames;

    /// <summary>Creates the class in the model <paramref name="Parent"/>, which must be a TMoldModel.</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldClass(TMoldElement? Parent, string Name)
        : base(Parent, Name)
    {
        System.Diagnostics.Debug.Assert(Parent is TMoldModel);
        InitializeClass((TMoldModel)Parent!);
    }

    /// <summary>Unlinks the class: its subclasses move to its superclass, its roles lose their class.</summary>
    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        // Unlinking subclasses is skipped while the whole model is freed, as in Delphi.
        if (!Model.fIsDestroying)
        {
            TMoldClass? tempSuperClass = SuperClass;
            SuperClass = null;
            if (FSubClasses is not null)
            {
                for (int i = FSubClasses.Count - 1; i >= 0; i--)
                {
                    FSubClasses[i].SuperClass = tempSuperClass;
                }
            }
        }

        SysUtils.FreeAndNil(ref FSubClasses);
        Association?.SetLinkClassInternal(null);
        SysUtils.FreeAndNil(ref FAttributes);
        SysUtils.FreeAndNil(ref FMethods);
        if (FRoles is not null)
        {
            for (int i = FRoles.Count - 1; i >= 0; i--)
            {
                FRoles[i].MoldClass = null;
            }
        }

        SysUtils.FreeAndNil(ref FRoles);
        SysUtils.FreeAndNil(ref fAllBoldMembers);
        SysUtils.FreeAndNil(ref fAllNativeAttributes);
        fAllPossibleNames = null;
        Model?.Classes?.RemoveEntryReference(this);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    private void SetSuperClass(TMoldClass? super)
    {
        if (super != FSuperClass)
        {
            if (super is not null && super.ChildTo(this))
            {
                throw new EBold(BoldMoldConsts.sRecursiveAssignment);
            }

            FSuperClass?.SubClasses.Remove(this);
            FSuperClass = super;
            FSuperClass?.SubClasses.Add(this);
        }
    }

    [Delphi(PortStatus.Ported)] private string GetIncFileName() => BoldTVByName(TAG_INCFILENAME);
    [Delphi(PortStatus.Ported)] private bool GetIsRootClass() => Model.RootClass == this;

    [Delphi(PortStatus.Ported)]
    private void InitializeClass(TMoldModel Parent)
    {
        FModel = Parent;
        Model.Classes.Add(this);
        FSubClasses = new TMoldClassList { OwnsEntries = false };
        FAttributes = new TMoldAttributeList();
        FMethods = new TMoldMethodList();
        FRoles = new TMoldRoleList { OwnsEntries = false };
        FIsAbstract = false;
        fPersistent = true;
    }

    [Delphi(PortStatus.Ported)] private int GetTopSortedIndex() => FModel.Classes.IndexOf(this);

    [Delphi(PortStatus.Ported)]
    private void SetComponent(TMoldComponent? Value) => UnitName = Value is not null ? Value.Name : "";

    /// <summary>The native (Delphi-kind) attributes of the class and its superclasses, superclass ones first.</summary>
    [Delphi(PortStatus.Ported)]
    private TMoldAttributeList GetAllNativeAttributes()
    {
        if (fAllNativeAttributes is null)
        {
            fAllNativeAttributes = new TMoldAttributeList { OwnsEntries = false };
            if (SuperClass is not null)
            {
                for (int i = 0; i < SuperClass.AllNativeAttributes.Count; i++)
                {
                    fAllNativeAttributes.Add(SuperClass.AllNativeAttributes[i]);
                }
            }

            fFirstOwnNativeAttributeIndex = fAllNativeAttributes.Count;
            for (int i = 0; i < Attributes.Count; i++)
            {
                if (Attributes[i].AttributeKind == TBoldAttributeKind.bastDelphi)
                {
                    fAllNativeAttributes.Add(Attributes[i]);
                }
            }
        }

        return fAllNativeAttributes;
    }

    /// <summary>
    /// The Bold members of the class and its superclasses: inherited members first, then own Bold attributes, roles with
    /// their link roles, and the inner link roles of a link class. Members that do not exist are left out.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private TMoldMemberList GetAllBoldMembers()
    {
        if (fAllBoldMembers is null)
        {
            Model.EnsureLinkRoles();
            fAllBoldMembers = new TMoldMemberList { OwnsEntries = false };

            void TryAdd(TMoldMember Member)
            {
                if (Member.MemberExists)
                {
                    fAllBoldMembers.Add(Member);
                }
            }

            if (SuperClass is not null)
            {
                for (int i = 0; i < SuperClass.AllBoldMembers.Count; i++)
                {
                    fAllBoldMembers.Add(SuperClass.AllBoldMembers[i]);
                }
            }

            fFirstOwnBoldMemberIndex = fAllBoldMembers.Count;
            for (int i = 0; i < Attributes.Count; i++)
            {
                if (Attributes[i].AttributeKind == TBoldAttributeKind.bastBold)
                {
                    TryAdd(Attributes[i]);
                }
            }

            for (int i = 0; i < Roles.Count; i++)
            {
                TryAdd(Roles[i]);
                if (Roles[i].HasLinkRole)
                {
                    TryAdd(Roles[i].LinkRole!);
                }
            }

            if (Association is not null)
            {
                TryAdd(Association.fInnerLinkRoles![0]);
                TryAdd(Association.fInnerLinkRoles![1]);
            }
        }

        return fAllBoldMembers;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetIntroducesManuallyDerivedMembers()
    {
        bool result = false;
        for (int i = 0; i < Attributes.Count; i++)
        {
            result = result || (Attributes[i].Derived && Attributes[i].DerivationOCL == "");
        }

        for (int i = 0; i < Roles.Count; i++)
        {
            result = result || (Roles[i].Derived && Roles[i].DerivationOCL == "");
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private int GetFirstOwnBoldMemberIndex()
    {
        if (fAllBoldMembers is null)
        {
            GetAllBoldMembers();
        }

        return fFirstOwnBoldMemberIndex;
    }

    [Delphi(PortStatus.Ported)]
    private int GetFirstOwnNativeAttributeIndex()
    {
        if (fAllNativeAttributes is null)
        {
            GetAllNativeAttributes();
        }

        return fFirstOwnNativeAttributeIndex;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetHasManuallyDerivedMembers()
    {
        for (int i = 0; i < AllBoldMembers.Count; i++)
        {
            if (AllBoldMembers[i].Derived && AllBoldMembers[i].DerivationOCL == "")
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Methods of superclasses marked OverrideInAllSubclasses that this class does not declare itself.</summary>
    [Delphi(PortStatus.Ported)]
    private TMoldMethodList GetAllAutoOverrideMethods()
    {
        if (fAllAutoOverrideMethods is null)
        {
            TMoldClass? tempClass = SuperClass;
            fAllAutoOverrideMethods = new TMoldMethodList { OwnsEntries = false };
            while (tempClass is not null)
            {
                for (int i = 0; i < tempClass.Methods.Count; i++)
                {
                    if (tempClass.Methods[i].OverrideInAllSubclasses && Methods.ItemsByName(tempClass.Methods[i].Name) is null)
                    {
                        fAllAutoOverrideMethods.Add(tempClass.Methods[i]);
                    }
                }

                tempClass = tempClass.SuperClass;
            }
        }

        return fAllAutoOverrideMethods;
    }

    /// <summary>The component of the unit name; without one, the superclass's component, or the model's main component.</summary>
    [Delphi(PortStatus.Ported)]
    private TMoldComponent GetComponent()
    {
        string componentName = ExpandedUnitName;
        if (componentName == "")
        {
            return SuperClass is not null ? SuperClass.Component : Model.MainComponent;
        }

        return Model.EnsureComponent(componentName);
    }

    [Delphi(PortStatus.Ported)]
    private TTableMapping GetTablemapping() => TBoldTaggedValueSupport.StringToTableMapping(BoldTVByName(TAG_TABLEMAPPING));

    [Delphi(PortStatus.Ported)] private string GetUnitName() => BoldTVByName(TAG_UNITNAME);
    [Delphi(PortStatus.Ported)] private bool GetImported() => TVIsTrue(BoldTVByName(TAG_IMPORTED));
    [Delphi(PortStatus.Ported)] private string GetTableName() => BoldTVByName(TAG_TABLENAME);
    [Delphi(PortStatus.Ported)] private void SetUnitName(string Value) => SetBoldTV(TAG_UNITNAME, Value);
    [Delphi(PortStatus.Ported)] private void SetIncFileName(string Value) => SetBoldTV(TAG_INCFILENAME, Value);
    [Delphi(PortStatus.Ported)] private string GetDefaultStringRepresentation() => BoldTVByName(TAG_DEFAULTSTRINGREPRESENTATION);

    [Delphi(PortStatus.Ported)]
    private string GetEffectiveDefaultStringRepresentation()
    {
        string result = DefaultStringRepresentation;
        return result == "" && SuperClass is not null ? SuperClass.EffectiveDefaultStringRepresentation : result;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetHasCodeStubs() =>
        Methods.Count > 0 || AllAutoOverrideMethods.Count > 0 || HasManuallyDerivedMembers || HasDelphiAttributesWithAccessorFunctions;

    /// <summary>The Versioned tagged value; &lt;Default&gt; inherits from the superclass.</summary>
    [Delphi(PortStatus.Ported)]
    private bool GetVersioned()
    {
        string s = BoldTVByName(TAG_VERSIONED);
        return SysUtils.SameText(s, BoldDefs.DEFAULTNAME) ? SuperClass is not null && SuperClass.Versioned : TVIsTrue(s);
    }

    [Delphi(PortStatus.Ported)] private string GetGuid() => BoldTVByName(TAG_GUID);
    [Delphi(PortStatus.Ported)] private string GetListGuid() => BoldTVByName(TAG_LISTGUID);

    [Delphi(PortStatus.Ported)]
    private TBoldOptimisticLockingMode GetOptimisticLocking() =>
        TBoldTaggedValueSupport.StringToOptimisticLockingMode(BoldTVByName(TAG_OPTIMISTICLOCKING));

    [Delphi(PortStatus.Ported)]
    private string GetExpandedUnitName() =>
        BoldNameExpander.BoldExpandName(UnitName, Name, TExpansionType.xtDelphi, -1, Model.NationalCharConversion);

    /// <summary>Frees the attributes whose evolution state is Removed.</summary>
    [Delphi(PortStatus.Ported)]
    internal void TrimRemoved()
    {
        for (int i = Attributes.Count - 1; i >= 0; i--)
        {
            TMoldAttribute attr = Attributes[i];
            if (attr.EvolutionState == TBoldEvolutionState.esRemoved)
            {
                attr.Dispose();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldEvolutionState GetEffectiveEvolutionState()
    {
        TBoldEvolutionState result = EvolutionState;
        if (Association is not null && Association.EvolutionState > result)
        {
            result = Association.EvolutionState;
        }

        return result;
    }

    /// <summary>The first dispatch id: 1024 for a root, else the superclass's last id rounded up to the next multiple of 256.</summary>
    [Delphi(PortStatus.Ported)]
    private int GetFirstDispId() => SuperClass is not null
        ? (SuperClass.LastDispId + BoldMeta.BOLD_DISPID_ROUND) / BoldMeta.BOLD_DISPID_ROUND * BoldMeta.BOLD_DISPID_ROUND
        : BoldMeta.BOLD_FIRSTUSERDISPID;

    [Delphi(PortStatus.Ported)]
    private int GetLastDispId()
    {
        if (fLastDispId == 0)
        {
            AssignMemberDispIds();
        }

        return fLastDispId;
    }

    /// <summary>The expression name, the former names and (for a link class) the association's possible names, sorted.</summary>
    [Delphi(PortStatus.Ported)]
    private TStringList GetAllPossibleNames()
    {
        if (fAllPossibleNames is null)
        {
            fAllPossibleNames = new TStringList { Sorted = true, Duplicates = TDuplicates.dupIgnore };
            fAllPossibleNames.CommaText = FormerNames;
            fAllPossibleNames.Add(ExpandedExpressionName);
            if (Association is not null)
            {
                for (int i = 0; i < Association.AllPossibleNames.Count; i++)
                {
                    fAllPossibleNames.Add(Association.AllPossibleNames[i]);
                }
            }
        }

        return fAllPossibleNames;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetHasDelphiAttributesWithAccessorFunctions()
    {
        static bool IsMethod(TDelphiPropertyAccessKind k) =>
            k is TDelphiPropertyAccessKind.pkPrivateMethod or TDelphiPropertyAccessKind.pkProtectedVirtualMethod;

        for (int i = FirstOwnNativeAttributeIndex; i < AllNativeAttributes.Count; i++)
        {
            if (IsMethod(AllNativeAttributes[i].DelphiPropertyRead) || IsMethod(AllNativeAttributes[i].DelphiPropertyWrite))
            {
                return true;
            }
        }

        return false;
    }

    [Delphi(PortStatus.Ported)]
    internal uint CalculateCRC()
    {
        uint result = BoldMeta.CRCHash(ExpandedDelphiName);
        for (int i = 0; i < Attributes.Count; i++)
        {
            result = BoldMeta.SumCRCs(result, Attributes[i].CalculateCRC());
        }

        for (int i = 0; i < Roles.Count; i++)
        {
            result = BoldMeta.SumCRCs(result, Roles[i].CalculateCRC());
        }

        return result;
    }

    /// <summary>The class's optimistic locking; &lt;Default&gt; inherits from the superclass, and at the root from the model.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldOptimisticLockingMode GetEffectiveOptimisticLocking()
    {
        TBoldOptimisticLockingMode result = OptimisticLocking;
        if (result == TBoldOptimisticLockingMode.bolmDefault)
        {
            result = SuperClass is not null ? SuperClass.EffectiveOptimisticLocking : Model.OptimisticLocking;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] private bool GetGenerateDefaultRegion() => TVIsTrue(BoldTVByName(TAG_GENERATEDEFAULTREGION_CLASS));
    [Delphi(PortStatus.Ported)] private TBoldStorage GetStorage() => TBoldTaggedValueSupport.StringToStorage(BoldTVByName(TAG_STORAGE));

    /// <summary>Numbers the members and methods that can have a dispatch id, from FirstDispId.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void AssignMemberDispIds()
    {
        int nextId = FirstDispId;
        for (int i = 0; i < AllBoldMembers.Count; i++)
        {
            if (AllBoldMembers[i].HasDispId)
            {
                AllBoldMembers[i].fDispId = nextId;
                nextId++;
            }
        }

        for (int i = 0; i < Methods.Count; i++)
        {
            if (Methods[i].HasDispId)
            {
                Methods[i].fDispId = nextId;
                nextId++;
            }
        }

        fLastDispId = nextId - 1;
    }

    [Delphi(PortStatus.Ported)]
    protected override TMoldModel GetModel() => FModel;

    /// <summary>The expanded expression name with its first letter in upper case.</summary>
    [Delphi(PortStatus.Ported)]
    protected override string GetExpandedExpressionName()
    {
        string result = base.GetExpandedExpressionName();
        if (result.Length > 0 && result[0] >= 'a' && result[0] <= 'z')
        {
            result = BoldSharedStrings.BoldSharedStringManager().GetSharedString(char.ToUpperInvariant(result[0]) + result.Substring(1));
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected string GetExpandedInterfaceName() =>
        BoldNameExpander.BoldExpandName(BoldTVByName(TAG_INTERFACENAME), Name, TExpansionType.xtDelphi, -1, Model.NationalCharConversion);

    /// <summary>Persistent, its link association (if any) effectively persistent, and not abstract with only transient subclasses.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool GetEffectivePersistent()
    {
        bool allSubClassesTransient = true;
        for (int i = 0; i < SubClasses.Count; i++)
        {
            allSubClassesTransient = allSubClassesTransient && !SubClasses[i].EffectivePersistent;
        }

        return Persistent && (Association is null || Association.EffectivePersistent) && !(IsAbstract && allSubClassesTransient);
    }

    [Delphi(PortStatus.Ported)]
    protected bool GetHasNotNullMembers()
    {
        bool result = false;
        for (int i = 0; i < Attributes.Count; i++)
        {
            result = result || !Attributes[i].AllowNull;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetUMLClassName() => "Class";

    /// <summary>True when this class is <paramref name="MoldClass"/> or one of its subclasses.</summary>
    [Delphi(PortStatus.Ported)]
    public bool ChildTo(TMoldClass? MoldClass)
    {
        if (this == MoldClass)
        {
            return true;
        }

        return SuperClass is not null && SuperClass.ChildTo(MoldClass);
    }

    [Delphi(PortStatus.Ported)]
    public override void NameChanged()
    {
        Model?.Classes?.ItemChanged(this);
        SuperClass?.SubClasses?.ItemChanged(this);
    }

    /// <summary>The class whose table stores <paramref name="Member"/>, following the table mapping (own, parent or children).</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldClass? FindStoringClass(TMoldClass DefiningClass, bool AbovedefiningClass, TMoldMember Member)
    {
        // The defining class of the links of association classes is the other end, so return to the link class unless
        // it is a common member, not a relation.
        if (Association is not null && Member is TMoldRole role && role.Association == Association)
        {
            DefiningClass = this;
        }

        TMoldClass? result = null;
        if (AbovedefiningClass)
        {
            switch (TableMapping)
            {
                case TTableMapping.tmParent:
                    result = SuperClass!.FindStoringClass(DefiningClass, true, Member);
                    break;
                case TTableMapping.tmOwn:
                    result = this;
                    break;
                case TTableMapping.tmChildren:
                    throw new EBoldInternal(SysUtils.Format("%s.FindStoringClass: Illegal table mapping for member %s.%s",
                        GetType().Name, Member.MoldClass!.Name, Member.Name));
            }
        }
        else if (this == DefiningClass)
        {
            switch (TableMapping)
            {
                case TTableMapping.tmParent:
                    result = SuperClass!.FindStoringClass(DefiningClass, true, Member);
                    break;
                case TTableMapping.tmOwn:
                    result = this;
                    break;
                case TTableMapping.tmChildren:
                    result = null;
                    break;
            }
        }
        else
        {
            result = SuperClass!.FindStoringClass(DefiningClass, false, Member);
            if (result is null && TableMapping == TTableMapping.tmOwn)
            {
                result = this;
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TMoldClass LowestCommonSuperClass(TMoldClass otherClass)
    {
        if (otherClass.ChildTo(this))
        {
            return this;
        }

        if (ChildTo(otherClass))
        {
            return otherClass;
        }

        return LowestCommonSuperClass(otherClass.SuperClass!);
    }

    /// <summary>The nearest ancestor of <paramref name="MoldClass"/> (or itself) whose component this class's component can see.</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldClass? LowestVisibleAncestor(TMoldClass MoldClass)
    {
        if (MoldClass.Component == Component || Component.DependentOf(MoldClass.Component))
        {
            return MoldClass;
        }

        return MoldClass.SuperClass is not null ? LowestVisibleAncestor(MoldClass.SuperClass) : null;
    }

    /// <summary>
    /// The include file name: the FileName tagged value expanded, with its extension or <paramref name="DefaultExtension"/>;
    /// without one, the superclass's file when it is in the same component, else the component name.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public string EffectiveIncFileName(string DefaultExtension)
    {
        string result = IncFileName;
        string extension;
        if (SysUtils.Pos(".", result) != 0)
        {
            extension = "." + SysUtils.Copy(result, SysUtils.Pos(".", result) + 1, SysUtils.MaxInt);
            result = SysUtils.Copy(result, 1, SysUtils.Pos(".", result) - 1);
        }
        else
        {
            extension = DefaultExtension;
        }

        result = BoldNameExpander.BoldExpandName(result, Name, TExpansionType.xtDelphi, -1, Model.NationalCharConversion);
        if (result != "")
        {
            return result + extension;
        }

        if (SuperClass is not null && SuperClass.Component == Component)
        {
            return SuperClass.EffectiveIncFileName(DefaultExtension);
        }

        return Component.Name + extension;
    }

    [Delphi(PortStatus.Ported)] public TTableMapping TableMapping => GetTablemapping();
    [Delphi(PortStatus.Ported)] public string TableName => GetTableName();
    [Delphi(PortStatus.Ported)] public bool IsRootClass => GetIsRootClass();

    [Delphi(PortStatus.Ported)]
    public TMoldClass? SuperClass
    {
        get => FSuperClass;
        set => SetSuperClass(value);
    }

    [Delphi(PortStatus.Ported)] public TMoldClassList SubClasses => FSubClasses!;
    [Delphi(PortStatus.Ported)] public TMoldAttributeList Attributes => FAttributes!;
    [Delphi(PortStatus.Ported)] public TMoldMethodList Methods => FMethods!;
    [Delphi(PortStatus.Ported)] public TMoldRoleList Roles => FRoles!;

    /// <summary>The association this class is the link class of; null for ordinary classes.</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldAssociation? Association
    {
        get => FAssociation;
        set => FAssociation = value;
    }

    [Delphi(PortStatus.Ported)]
    public bool IsAbstract
    {
        get => FIsAbstract;
        set => FIsAbstract = value;
    }

    [Delphi(PortStatus.Ported)] public bool Imported => GetImported();

    [Delphi(PortStatus.Ported)]
    public string UnitName
    {
        get => GetUnitName();
        set => SetUnitName(value);
    }

    [Delphi(PortStatus.Ported)] public string ExpandedUnitName => GetExpandedUnitName();

    [Delphi(PortStatus.Ported)]
    public string IncFileName
    {
        get => GetIncFileName();
        set => SetIncFileName(value);
    }

    [Delphi(PortStatus.Ported)] public bool HasNotNullMembers => GetHasNotNullMembers();
    [Delphi(PortStatus.Ported)] public int TopSortedIndex => GetTopSortedIndex();

    [Delphi(PortStatus.Ported)]
    public TMoldComponent Component
    {
        get => GetComponent();
        set => SetComponent(value);
    }

    [Delphi(PortStatus.Ported)] public TMoldMemberList AllBoldMembers => GetAllBoldMembers();
    [Delphi(PortStatus.Ported)] public TMoldAttributeList AllNativeAttributes => GetAllNativeAttributes();
    [Delphi(PortStatus.Ported)] public TMoldMethodList AllAutoOverrideMethods => GetAllAutoOverrideMethods();
    [Delphi(PortStatus.Ported)] public int FirstOwnBoldMemberIndex => GetFirstOwnBoldMemberIndex();
    [Delphi(PortStatus.Ported)] public int FirstOwnNativeAttributeIndex => GetFirstOwnNativeAttributeIndex();
    [Delphi(PortStatus.Ported)] public bool IntroducesManuallyDerivedMembers => GetIntroducesManuallyDerivedMembers();
    [Delphi(PortStatus.Ported)] public bool HasManuallyDerivedMembers => GetHasManuallyDerivedMembers();
    [Delphi(PortStatus.Ported)] public bool HasDelphiAttributesWithAccessorFunctions => GetHasDelphiAttributesWithAccessorFunctions();
    [Delphi(PortStatus.Ported)] public bool EffectivePersistent => GetEffectivePersistent();

    [Delphi(PortStatus.Ported)]
    public bool Persistent
    {
        get => fPersistent;
        set => fPersistent = value;
    }

    [Delphi(PortStatus.Ported)] public string DefaultStringRepresentation => GetDefaultStringRepresentation();
    [Delphi(PortStatus.Ported)] public string EffectiveDefaultStringRepresentation => GetEffectiveDefaultStringRepresentation();
    [Delphi(PortStatus.Ported)] public bool HasCodeStubs => GetHasCodeStubs();
    [Delphi(PortStatus.Ported)] public bool Versioned => GetVersioned();
    [Delphi(PortStatus.Ported)] public string ExpandedInterfaceName => GetExpandedInterfaceName();
    [Delphi(PortStatus.Ported)] public string GUID => GetGuid();
    [Delphi(PortStatus.Ported)] public string ListGUID => GetListGuid();
    [Delphi(PortStatus.Ported)] public int LastDispId => GetLastDispId();
    [Delphi(PortStatus.Ported)] public int FirstDispId => GetFirstDispId();
    [Delphi(PortStatus.Ported)] public TBoldOptimisticLockingMode OptimisticLocking => GetOptimisticLocking();
    [Delphi(PortStatus.Ported)] public TBoldOptimisticLockingMode EffectiveOptimisticLocking => GetEffectiveOptimisticLocking();
    [Delphi(PortStatus.Ported)] public TBoldEvolutionState EvolutionState => GetEvolutionState();
    [Delphi(PortStatus.Ported)] public string FormerNames => GetFormerNames();
    [Delphi(PortStatus.Ported)] public TStringList AllPossibleNames => GetAllPossibleNames();
    [Delphi(PortStatus.Ported)] public TBoldEvolutionState EffectiveEvolutionState => GetEffectiveEvolutionState();
    [Delphi(PortStatus.Ported)] public bool GenerateDefaultRegion => GetGenerateDefaultRegion();
    [Delphi(PortStatus.Ported)] public TBoldStorage Storage => GetStorage();
}

/// <summary>A member of a class: an attribute or a role.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TMoldMember : TMoldElement
{
    private TMoldClass? FMoldClass;
    private TVisibilityKind fVisibility;

    protected TMoldMember(TMoldElement? Parent, string Name)
        : base(Parent, Name)
    {
    }

    [Delphi(PortStatus.Ported)] protected abstract bool GetDerived();
    [Delphi(PortStatus.Ported)] protected abstract bool GetReverseDerived();
    [Delphi(PortStatus.Ported)] private string GetDerivationOCL() => BoldTVByName("DerivationOCL");
    [Delphi(PortStatus.Ported)] protected virtual bool GetMemberExists() => true;
    [Delphi(PortStatus.Ported)] private string GetColumnName() => BoldTVByName(TAG_COLUMNNAME);
    [Delphi(PortStatus.Ported)] protected abstract string GetTypeStreamName();
    [Delphi(PortStatus.Ported)] protected virtual bool GetEffectiveDelayedFetch() => TVIsTrue(BoldTVByName(TAG_DELAYEDFETCH));

    [Delphi(PortStatus.Ported)]
    protected override TMoldModel GetModel() => MoldClass!.Model;

    /// <summary>The expanded expression name with its first letter in lower case.</summary>
    [Delphi(PortStatus.Ported)]
    protected override string GetExpandedExpressionName()
    {
        string result = base.GetExpandedExpressionName();
        if (result.Length > 0 && result[0] >= 'A' && result[0] <= 'Z')
        {
            result = BoldSharedStrings.BoldSharedStringManager().GetSharedString((char)(result[0] + 32) + result.Substring(1));
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] protected abstract bool GetEffectivePersistent();

    [Delphi(PortStatus.Ported)]
    protected virtual void SetMoldClass(TMoldClass? NewMoldClass) => FMoldClass = NewMoldClass;

    [Delphi(PortStatus.Ported)] protected TMoldClass? GetMoldClass() => FMoldClass;
    [Delphi(PortStatus.Ported)] protected bool GetManuallyDerived() => Derived && DerivationOCL == "";
    [Delphi(PortStatus.Ported)] protected abstract TBoldStorage GetStorage();

    [Delphi(PortStatus.Ported)] public TMoldClass? MoldClass => GetMoldClass();
    [Delphi(PortStatus.Ported)] public string ColumnName => GetColumnName();
    [Delphi(PortStatus.Ported)] public bool EffectiveDelayedFetch => GetEffectiveDelayedFetch();

    [Delphi(PortStatus.Ported)]
    public TVisibilityKind Visibility
    {
        get => fVisibility;
        set => fVisibility = value;
    }

    [Delphi(PortStatus.Ported)] public bool Derived => GetDerived();
    [Delphi(PortStatus.Ported)] public bool ManuallyDerived => GetManuallyDerived();
    [Delphi(PortStatus.Ported)] public bool ReverseDerived => GetReverseDerived();
    [Delphi(PortStatus.Ported)] public string DerivationOCL => GetDerivationOCL();
    [Delphi(PortStatus.Ported)] public bool MemberExists => GetMemberExists();
    [Delphi(PortStatus.Ported)] public bool EffectivePersistent => GetEffectivePersistent();
    [Delphi(PortStatus.Ported)] public string TypeStreamName => GetTypeStreamName();
    [Delphi(PortStatus.Ported)] public string FormerNames => GetFormerNames();
    [Delphi(PortStatus.Ported)] public TBoldStorage Storage => GetStorage();
}

/// <summary>An attribute: a Bold type name, an initial value, and settings from tagged values.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldAttribute : TMoldMember
{
    private bool FDerived;
    private string FBoldType = "";
    private string FInitialValue = "";

    /// <summary>Creates a String attribute in the class <paramref name="Parent"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldAttribute(TMoldElement? Parent, string Name)
        : base(Parent, Name)
    {
        System.Diagnostics.Debug.Assert(Parent is TMoldClass);
        SetMoldClass((TMoldClass)Parent!);
        BoldType = "String";
        MoldClass!.Attributes.Add(this);
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        MoldClass?.Attributes?.RemoveEntryReference(this);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    internal uint CalculateCRC() => BoldMeta.SumCRCs(BoldMeta.CRCHash(ExpandedDelphiName), BoldMeta.CRCHash(BoldType));

    [Delphi(PortStatus.Ported)] protected override bool GetDerived() => FDerived;
    [Delphi(PortStatus.Ported)] protected override bool GetReverseDerived() => TVIsTrue(BoldTVByName(TAG_REVERSEDERIVE));
    [Delphi(PortStatus.Ported)] private bool GetAllowNull() => TVIsTrue(BoldTVByName(TAG_ALLOWNULL));

    [Delphi(PortStatus.Ported)]
    private TBoldAttributeKind GetAttributeKind() => TBoldTaggedValueSupport.StringToAttributeKind(BoldTVByName(TAG_ATTRIBUTEKIND));

    [Delphi(PortStatus.Ported)]
    private TDelphiPropertyAccessKind GetDelphiPropertyRead() =>
        TBoldTaggedValueSupport.StringToDelphiPropertyAccessKind(BoldTVByName(TAG_DPREAD));

    [Delphi(PortStatus.Ported)]
    private TDelphiPropertyAccessKind GetDelphiPropertyWrite() =>
        TBoldTaggedValueSupport.StringToDelphiPropertyAccessKind(BoldTVByName(TAG_DPWRITE));

    [Delphi(PortStatus.Ported)] private bool GetHasDelphiField() => TVIsTrue(BoldTVByName(TAG_DELPHIFIELD));
    [Delphi(PortStatus.Ported)] private int GetLength() => SysUtils.StrToIntDef(BoldTVByName(TAG_LENGTH), 255);

    [Delphi(PortStatus.Ported)]
    private bool GetPersistent() => SysUtils.SameText(StdTVByName(BoldUMLTaggedValues.TAG_PERSISTENCE), BoldUMLTaggedValues.TV_PERSISTENCE_PERSISTENT);

    /// <summary>The contents (stream) name of the attribute's type from the model's type name dictionary.</summary>
    [Delphi(PortStatus.Ported)]
    protected override string GetTypeStreamName() => Model.TypeNameDictionary!.MappingForModelName(BoldType)!.ExpandedContentsName;

    [Delphi(PortStatus.Ported)] private string GetDefaultDBValue() => BoldTVByName(TAG_DEFAULTDBVALUE);

    [Delphi(PortStatus.Ported)]
    protected override bool GetEffectivePersistent() => Persistent && !Derived && MoldClass!.EffectivePersistent;

    [Delphi(PortStatus.Ported)]
    protected override bool GetEffectiveDelayedFetch() => EvolutionState == TBoldEvolutionState.esToBeRemoved || base.GetEffectiveDelayedFetch();

    [Delphi(PortStatus.Ported)]
    protected override bool GetHasDispId()
    {
        switch (Model.fDispIdAssigningState)
        {
            case TBoldDispIdAssigningState.dasNotStarted:
                Model.AssignDispIds();
                return fDispId != -1;
            case TBoldDispIdAssigningState.dasAssigning:
                return MemberExists && AttributeKind == TBoldAttributeKind.bastBold;
            default:
                return fDispId != -1;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetUMLClassName() => "Attribute";

    [Delphi(PortStatus.Ported)]
    protected override TBoldStorage GetStorage() => TBoldTaggedValueSupport.StringToStorage(BoldTVByName(TAG_STORAGE));

    [Delphi(PortStatus.Ported)]
    public override void NameChanged() => MoldClass?.Attributes?.ItemChanged(this);

    [Delphi(PortStatus.Ported)] public bool AllowNull => GetAllowNull();
    [Delphi(PortStatus.Ported)] public int Length => GetLength();

    [Delphi(PortStatus.Ported)]
    public string BoldType
    {
        get => FBoldType;
        set => FBoldType = value ?? "";
    }

    [Delphi(PortStatus.Ported)] public TBoldAttributeKind AttributeKind => GetAttributeKind();
    [Delphi(PortStatus.Ported)] public bool HasDelphiField => GetHasDelphiField();
    [Delphi(PortStatus.Ported)] public TDelphiPropertyAccessKind DelphiPropertyRead => GetDelphiPropertyRead();
    [Delphi(PortStatus.Ported)] public TDelphiPropertyAccessKind DelphiPropertyWrite => GetDelphiPropertyWrite();

    [Delphi(PortStatus.Ported)]
    public string InitialValue
    {
        get => FInitialValue;
        set => FInitialValue = value ?? "";
    }

    [Delphi(PortStatus.Ported)]
    public new bool Derived
    {
        get => FDerived;
        set => FDerived = value;
    }

    [Delphi(PortStatus.Ported)] public bool Persistent => GetPersistent();
    [Delphi(PortStatus.Ported)] public new bool ReverseDerived => GetReverseDerived();
    [Delphi(PortStatus.Ported)] public TBoldEvolutionState EvolutionState => GetEvolutionState();
    [Delphi(PortStatus.Ported)] public string DefaultDBValue => GetDefaultDBValue();
}

/// <summary>A method of a class, with a Delphi-style signature parsed into parameters.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldMethod : TMoldElement
{
    private readonly TMoldClass FMoldClass;
    private bool FIsClassMethod;
    private string FReturnType = "";
    private TBoldObjectArray? fParameters;
    private TVisibilityKind fVisibility;

    [Delphi(PortStatus.Ported)]
    public TMoldMethod(TMoldElement? Parent, string Name)
        : base(Parent, Name)
    {
        System.Diagnostics.Debug.Assert(Parent is TMoldClass);
        FMoldClass = (TMoldClass)Parent!;
        MoldClass.Methods.Add(this);
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        MoldClass?.Methods?.RemoveEntryReference(this);
        SysUtils.FreeAndNil(ref fParameters);
        base.Dispose();
    }

    /// <summary>
    /// Parses "a, b: Integer; const s: string; var x: Double; out y: Boolean" into parameters. A group without a type
    /// takes the type of the group before it.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void setSignature(string Value)
    {
        const string ConstParam = "CONST ";
        const string VarParam = "VAR ";
        const string OutParam = "OUT ";
        if (Signature == Value)
        {
            return;
        }

        Parameters.Clear();
        string paramType = "";
        if (Value != "")
        {
            Value += ";";
        }

        while (Value != "")
        {
            bool isConst = false, isVar = false, isOut = false;
            string next = SysUtils.Copy(Value, 1, SysUtils.Pos(";", Value) - 1);
            Value = SysUtils.Copy(Value, SysUtils.Pos(";", Value) + 1, SysUtils.MaxInt);

            string Strip(string s, string keyword)
            {
                int p = SysUtils.Pos(keyword, SysUtils.UpperCase(s));
                return s.Remove(p - 1, keyword.Length);
            }

            if (SysUtils.Pos(ConstParam, SysUtils.UpperCase(next)) > 0)
            {
                isConst = true;
                next = Strip(next, ConstParam);
            }
            else if (SysUtils.Pos(VarParam, SysUtils.UpperCase(next)) > 0)
            {
                isVar = true;
                next = Strip(next, VarParam);
            }
            else if (SysUtils.Pos(OutParam, SysUtils.UpperCase(next)) > 0)
            {
                isOut = true;
                next = Strip(next, OutParam);
            }

            if (SysUtils.Pos(":", next) != 0)
            {
                paramType = BoldUtils.BoldTrim(SysUtils.Copy(next, SysUtils.Pos(":", next) + 1, SysUtils.MaxInt));
                next = SysUtils.Copy(next, 1, SysUtils.Pos(":", next) - 1);
            }

            next += ",";
            while (SysUtils.Pos(",", next) != 0)
            {
                var param = new TMoldParameter(this)
                {
                    ParameterName = BoldUtils.BoldTrim(SysUtils.Copy(next, 1, SysUtils.Pos(",", next) - 1)),
                    ParameterType = paramType,
                    IsConst = isConst,
                };
                if (isVar)
                {
                    param.ParameterKind = TBoldParameterDirectionKind.pdInout;
                }
                else if (isOut)
                {
                    param.ParameterKind = TBoldParameterDirectionKind.pdOut;
                }

                Parameters.Add(param);
                next = SysUtils.Copy(next, SysUtils.Pos(",", next) + 1, SysUtils.MaxInt);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private string GetSignature()
    {
        string result = "";
        for (int i = 0; i < Parameters.Count; i++)
        {
            if (result != "")
            {
                result += "; ";
            }

            var param = (TMoldParameter)Parameters[i]!;
            if (param.IsConst)
            {
                result += "const ";
            }
            else if (param.ParameterKind == TBoldParameterDirectionKind.pdInout)
            {
                result += "var ";
            }
            else if (param.ParameterKind == TBoldParameterDirectionKind.pdOut)
            {
                result += "out ";
            }

            result += SysUtils.Format("%s: %s", param.ParameterName, param.ParameterType);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectArray GetParameters() =>
        fParameters ??= new TBoldObjectArray(0, TBoldContainerOptions.Of(TBoldContainerOption.bcoDataOwner));

    [Delphi(PortStatus.Ported)]
    private string GetCallSignature()
    {
        string result = "";
        for (int i = 0; i < Parameters.Count; i++)
        {
            if (result != "")
            {
                result += ", ";
            }

            result += ((TMoldParameter)Parameters[i]!).ParameterName;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private TDelphiFunctionType GetFuncType() => TBoldTaggedValueSupport.StringToDelphiFunctionType(BoldTVByName(TAG_DELPHIOPERATIONKIND));

    [Delphi(PortStatus.Ported)] private bool GetOverrideInAllSubclasses() => TVIsTrue(BoldTVByName(TAG_OVERRIDEINALLSUBCLASSES));

    [Delphi(PortStatus.Ported)]
    private string GetDelphiReturnType() => MoldClass.Model.Classes.ItemsByName(ReturnType)?.ExpandedDelphiName ?? ReturnType;

    [Delphi(PortStatus.Ported)] private bool GetHasReturnValue() => BoldUtils.BoldTrim(ReturnType) != "";

    /// <summary>True for an override or dynamic method whose nearest inherited method is not abstract.</summary>
    [Delphi(PortStatus.Ported)]
    private bool GetCanCallInherited()
    {
        if (FuncType is not (TDelphiFunctionType.dfOverride or TDelphiFunctionType.dfDynamic))
        {
            return false;
        }

        TMoldClass? tempSuperClass = MoldClass.SuperClass;
        TMoldMethod? superMethod = null;
        while (tempSuperClass is not null && superMethod is null)
        {
            superMethod = tempSuperClass.Methods.ItemsByName(Name);
            tempSuperClass = tempSuperClass.SuperClass;
        }

        return superMethod is not null && superMethod.FuncType != TDelphiFunctionType.dfAbstractVirtual;
    }

    [Delphi(PortStatus.Ported)]
    protected override TMoldModel GetModel() => MoldClass.Model;

    /// <summary>Public non-override methods whose parameter and return types all have COM/IDL types get a dispatch id.</summary>
    [Delphi(PortStatus.Ported)]
    protected override bool GetHasDispId()
    {
        switch (Model.fDispIdAssigningState)
        {
            case TBoldDispIdAssigningState.dasNotStarted:
                Model.AssignDispIds();
                return fDispId != -1;
            case TBoldDispIdAssigningState.dasAssigning:
            {
                bool result = FuncType is TDelphiFunctionType.dfNormal or TDelphiFunctionType.dfVirtual
                        or TDelphiFunctionType.dfDynamic or TDelphiFunctionType.dfAbstractVirtual
                    && Visibility == TVisibilityKind.vkPublic;
                bool dummy = false;
                if (fParameters is not null)
                {
                    for (int i = 0; i < fParameters.Count; i++)
                    {
                        result = result && TBoldMetaSupport.ParameterTypeToIDLType(((TMoldParameter)fParameters[i]!).ParameterType, MoldClass.Model, ref dummy) != "";
                    }
                }

                return result && (ReturnType == "" || TBoldMetaSupport.ParameterTypeToIDLType(ReturnType, MoldClass.Model, ref dummy) != "");
            }

            default:
                return fDispId != -1;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetUMLClassName() => "Operation";

    [Delphi(PortStatus.Ported)]
    public override void NameChanged() => MoldClass?.Methods?.ItemChanged(this);

    [Delphi(PortStatus.Ported)]
    public string ReturnType
    {
        get => FReturnType;
        set => FReturnType = value ?? "";
    }

    [Delphi(PortStatus.Ported)] public string DelphiReturnType => GetDelphiReturnType();
    [Delphi(PortStatus.Ported)] public TDelphiFunctionType FuncType => GetFuncType();

    [Delphi(PortStatus.Ported)]
    public string Signature
    {
        get => GetSignature();
        set => setSignature(value);
    }

    [Delphi(PortStatus.Ported)] public string CallSignature => GetCallSignature();
    [Delphi(PortStatus.Ported)] public TBoldObjectArray Parameters => GetParameters();

    [Delphi(PortStatus.Ported)]
    public bool IsClassMethod
    {
        get => FIsClassMethod;
        set => FIsClassMethod = value;
    }

    [Delphi(PortStatus.Ported)] public TMoldClass MoldClass => FMoldClass;

    [Delphi(PortStatus.Ported)]
    public TVisibilityKind Visibility
    {
        get => fVisibility;
        set => fVisibility = value;
    }

    [Delphi(PortStatus.Ported)] public bool OverrideInAllSubclasses => GetOverrideInAllSubclasses();
    [Delphi(PortStatus.Ported)] public bool HasReturnValue => GetHasReturnValue();
    [Delphi(PortStatus.Ported)] public bool CanCallInherited => GetCanCallInherited();
}

/// <summary>A parameter of a method.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldParameter : TBoldMemoryManagedObject
{
    private string FParameterType = "";
    private string FParameterName = "";
    private TBoldParameterDirectionKind FParameterKind;
    private bool FIsConst;
    private readonly TMoldMethod fOwningMethod;

    [Delphi(PortStatus.Ported)]
    public TMoldParameter(TMoldMethod OwningMethod)
    {
        fOwningMethod = OwningMethod;
    }

    [Delphi(PortStatus.Ported)]
    private string GetDelphiParameterType() =>
        fOwningMethod.MoldClass.Model.Classes.ItemsByName(ParameterType)?.ExpandedDelphiName ?? ParameterType;

    [Delphi(PortStatus.Ported)]
    public string ParameterName
    {
        get => FParameterName;
        set => FParameterName = value ?? "";
    }

    [Delphi(PortStatus.Ported)]
    public string ParameterType
    {
        get => FParameterType;
        set => FParameterType = value ?? "";
    }

    [Delphi(PortStatus.Ported)]
    public TBoldParameterDirectionKind ParameterKind
    {
        get => FParameterKind;
        set => FParameterKind = value;
    }

    [Delphi(PortStatus.Ported)] public string DelphiParameterType => GetDelphiParameterType();

    [Delphi(PortStatus.Ported)]
    public bool IsConst
    {
        get => FIsConst;
        set => FIsConst = value;
    }
}

/// <summary>
/// An association end. Ends of an association with a link class get two extra roles when link roles are ensured: a
/// link role from the class to the link objects and an inner link role from the link class to the other end's class.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TMoldRole : TMoldMember
{
    private TMoldAssociation? FAssociation;
    private bool FOrdered;
    private bool FNavigable;
    private TMoldQualifierList? FQualifiers;
    private TMoldRole? fRelatedRole;
    private TBoldRoleType fRoleType;
    private TAggregationKind fAggregation;
    private TChangeableKind fChangeability;
    private string fMultiplicity = "";

    /// <summary>Creates an end, "0..1" and navigable; when <paramref name="Parent"/> is an association the end joins it.</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldRole(TMoldElement? Parent, string Name)
        : base(Parent, Name)
    {
        if (Parent is TMoldAssociation association)
        {
            Association = association;
        }

        Multiplicity = "0..1";
        Navigable = true;
        fRoleType = TBoldRoleType.rtRole;
        FQualifiers = new TMoldQualifierList();
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        Association?.Roles?.RemoveEntryReference(this);
        MoldClass?.Roles?.RemoveEntryReference(this);
        SysUtils.FreeAndNil(ref FQualifiers);
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    internal uint CalculateCRC()
    {
        uint result = BoldMeta.SumCRCs(BoldMeta.CRCHash(ExpandedDelphiName), BoldMeta.CRCHash(Multiplicity));
        return BoldMeta.SumCRCs(result, BoldMeta.CRCHash(OtherEnd!.MoldClass!.ExpandedDelphiName));
    }

    /// <summary>The other end: of the association for a role, the paired inner link role or link role for link roles.</summary>
    [Delphi(PortStatus.Ported)]
    private TMoldRole? GetOtherEnd()
    {
        switch (RoleType)
        {
            case TBoldRoleType.rtLinkRole:
                return Association!.fLinkRoles![0] == this ? Association.fInnerLinkRoles![0] : Association.fInnerLinkRoles![1];
            case TBoldRoleType.rtInnerLinkRole:
                return Association!.fInnerLinkRoles![0] == this ? Association.fLinkRoles![0] : Association.fLinkRoles![1];
            case TBoldRoleType.rtRole:
                return Association!.Roles[0] == this ? Association.Roles[1] : Association.Roles[0];
            default:
                return null;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void setAssociation(TMoldAssociation? Association)
    {
        TMoldAssociation? oldAssociation = FAssociation;
        FAssociation = Association;
        if (oldAssociation != Association)
        {
            oldAssociation?.Roles.Remove(this);
            if (Association is not null)
            {
                switch (RoleType)
                {
                    case TBoldRoleType.rtRole:
                        Association.Roles.Add(this);
                        break;
                    case TBoldRoleType.rtLinkRole:
                        Association.fLinkRoles!.Add(this);
                        break;
                    case TBoldRoleType.rtInnerLinkRole:
                        Association.fInnerLinkRoles!.Add(this);
                        break;
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    private bool GetHasLinkRole() => RoleType == TBoldRoleType.rtRole && Association!.LinkClass is not null;

    [Delphi(PortStatus.Ported)]
    private TMoldRole? GetLinkRole()
    {
        MoldClass!.Model.EnsureLinkRoles();
        return HasLinkRole ? fRelatedRole : null;
    }

    /// <summary>Creates the link role and inner link role of this end, once. The second end waits for the first, so the order is known.</summary>
    [Delphi(PortStatus.Ported)]
    internal void EnsureLinkRole()
    {
        if (HasLinkRole && fRelatedRole is null)
        {
            if (Association!.Roles.IndexOf(this) == 1)
            {
                OtherEnd!.EnsureLinkRole();
            }

            string relatedName = MoldClass!.ChildTo(OtherEnd!.MoldClass) || OtherEnd.MoldClass!.ChildTo(MoldClass)
                ? Name + Association.LinkClass!.Name
                : Association.LinkClass!.Name;

            fRelatedRole = new TMoldRole(this, relatedName);
            fRelatedRole.fRelatedRole = this;
            fRelatedRole.Multiplicity = Multiplicity;
            fRelatedRole.fRoleType = TBoldRoleType.rtLinkRole;
            fRelatedRole.MoldClass = MoldClass;
            fRelatedRole.FOrdered = Ordered;
            fRelatedRole.Association = Association; // adds it to the association's link roles
            fRelatedRole.FNavigable = Navigable;
            fRelatedRole.SetBoldTV(TAG_EMBED, TV_FALSE);
            fRelatedRole.Visibility = Visibility;

            var innerLinkRole = new TMoldRole(this, OtherEnd.Name);
            innerLinkRole.fRoleType = TBoldRoleType.rtInnerLinkRole;
            innerLinkRole.MoldClass = Association.LinkClass;
            innerLinkRole.Association = Association; // adds it to the association's inner link roles
            innerLinkRole.Multiplicity = "1";
            innerLinkRole.Ordered = false;
            innerLinkRole.SetBoldTV(TAG_EMBED, TV_TRUE);
            innerLinkRole.Navigable = OtherEnd.Navigable;
            innerLinkRole.Visibility = OtherEnd.Visibility;
            innerLinkRole.SetBoldTV(TAG_COLUMNNAME, OtherEnd.ColumnName);
            innerLinkRole.SetBoldTV(TAG_EXPRESSIONNAME, OtherEnd.ExpressionName);
            innerLinkRole.SetBoldTV(TAG_FORMERNAMES, OtherEnd.FormerNames);
        }
    }

    [Delphi(PortStatus.Ported)]
    private TMoldRole? GetMainRole() => RoleType == TBoldRoleType.rtLinkRole ? fRelatedRole : null;

    /// <summary>Upper bound above 1 or qualified; a link role follows its main role's qualifiers; an inner link role is single.</summary>
    [Delphi(PortStatus.Ported)]
    private bool GetMulti() => RoleType switch
    {
        TBoldRoleType.rtRole => BoldUtils.GetUpperLimitForMultiplicity(Multiplicity) > 1 || Qualifiers.Count > 0,
        TBoldRoleType.rtLinkRole => BoldUtils.GetUpperLimitForMultiplicity(Multiplicity) > 1 || fRelatedRole!.Qualifiers.Count > 0,
        TBoldRoleType.rtInnerLinkRole => false,
        _ => throw new EBold(SysUtils.Format(BoldMoldConsts.sUnknownRoleType, GetType().Name, MoldClass!.Name, Name)),
    };

    [Delphi(PortStatus.Ported)] protected override bool GetDerived() => Association!.Derived;
    [Delphi(PortStatus.Ported)] protected override bool GetReverseDerived() => TVIsTrue(Association!.BoldTVByName(TAG_REVERSEDERIVE));
    [Delphi(PortStatus.Ported)] private bool GetMandatory() => BoldUtils.GetLowerLimitForMultiplicity(Multiplicity) > 0;
    [Delphi(PortStatus.Ported)] private bool GetEmbed() => TBoldTaggedValueSupport.StringToBoolean(BoldTVByName(TAG_EMBED));

    [Delphi(PortStatus.Ported)]
    private bool GetEffectiveEmbed() =>
        RoleType == TBoldRoleType.rtInnerLinkRole || (Embed && !Multi && Association!.LinkClass is null);

    [Delphi(PortStatus.Ported)] private bool GetEffectiveOrdered() => Ordered && Multi;

    [Delphi(PortStatus.Ported)]
    protected override string GetTypeStreamName()
    {
        if (Multi)
        {
            return Association!.LinkClass is not null
                ? BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair
                : BoldDefaultStreamNames.BoldContentName_ObjectIdListRef;
        }

        return Association!.LinkClass is not null && RoleType != TBoldRoleType.rtInnerLinkRole
            ? BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair
            : BoldDefaultStreamNames.BoldContentName_ObjectIdRef;
    }

    [Delphi(PortStatus.Ported)]
    private bool GetQualifiedMulti() => Qualifiers.Count > 0 && BoldUtils.GetUpperLimitForMultiplicity(Multiplicity) > 1;

    [Delphi(PortStatus.Ported)]
    private TBoldAssociationEndDefaultRegionMode GetDefaultRegionMode() =>
        TBoldTaggedValueSupport.StringToDefaultRegionMode(BoldTVByName(TAG_DEFAULTREGIONMODE_ASSOCIATIONEND));

    /// <summary>Default region mode: Cascade for aggregates, IndependentCascade for single ends, otherwise None.</summary>
    [Delphi(PortStatus.Ported)]
    private TBoldAssociationEndDefaultRegionMode GetEffectiveDefaultRegionMode()
    {
        TBoldAssociationEndDefaultRegionMode result = DefaultRegionMode;
        if (result == TBoldAssociationEndDefaultRegionMode.aedrmDefault)
        {
            if (Aggregation is TAggregationKind.akAggregate or TAggregationKind.akComposite)
            {
                result = TBoldAssociationEndDefaultRegionMode.aedrmCascade;
            }
            else
            {
                result = !Multi ? TBoldAssociationEndDefaultRegionMode.aedrmIndependentCascade : TBoldAssociationEndDefaultRegionMode.aedrmNone;
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetEffectiveDelayedFetch() =>
        RoleType == TBoldRoleType.rtLinkRole || Multi || !EffectiveEmbedded
        || Association!.EvolutionState == TBoldEvolutionState.esToBeRemoved || base.GetEffectiveDelayedFetch();

    [Delphi(PortStatus.Ported)]
    protected override TMoldModel GetModel() => Association!.Model;

    /// <summary>Moves an ordinary end to another class's role list; link roles only record the class.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetMoldClass(TMoldClass? NewMoldClass)
    {
        if (RoleType == TBoldRoleType.rtRole && MoldClass != NewMoldClass)
        {
            MoldClass?.Roles.RemoveEntryReference(this);
            if (NewMoldClass is not null && NewMoldClass.Roles.ItemsByName(Name) is null)
            {
                NewMoldClass.Roles.Add(this);
            }
        }

        base.SetMoldClass(NewMoldClass);
    }

    [Delphi(PortStatus.Ported)]
    protected override bool GetEffectivePersistent() =>
        Association!.EffectivePersistent && !Derived && RoleType != TBoldRoleType.rtLinkRole;

    [Delphi(PortStatus.Ported)]
    protected TDeleteAction GetDeleteAction() => TBoldTaggedValueSupport.StringToDeleteAction(BoldTVByName(TAG_DELETEACTION));

    [Delphi(PortStatus.Ported)]
    protected TDeleteAction GetEffectiveDeleteAction() =>
        DeleteAction == TDeleteAction.daDefault ? Model.DefaultDeleteAction(Aggregation) : DeleteAction;

    [Delphi(PortStatus.Ported)]
    protected override bool GetMemberExists() => base.GetMemberExists() && !(Derived && !Navigable);

    [Delphi(PortStatus.Ported)]
    protected override bool GetHasDispId()
    {
        switch (Model.fDispIdAssigningState)
        {
            case TBoldDispIdAssigningState.dasNotStarted:
                Model.AssignDispIds();
                return fDispId != -1;
            case TBoldDispIdAssigningState.dasAssigning:
                return MemberExists;
            default:
                return fDispId != -1;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override string GetUMLClassName() => "AssociationEnd";

    [Delphi(PortStatus.Ported)]
    protected override TBoldStorage GetStorage() => Association!.Storage;

    [Delphi(PortStatus.Ported)]
    public override void NameChanged()
    {
        MoldClass?.Roles?.ItemChanged(this);
        Association?.Roles?.ItemChanged(this);
    }

    [Delphi(PortStatus.Ported)]
    public TMoldAssociation? Association
    {
        get => FAssociation;
        set => setAssociation(value);
    }

    [Delphi(PortStatus.Ported)] public bool Multi => GetMulti();

    [Delphi(PortStatus.Ported)]
    public bool Navigable
    {
        get => FNavigable;
        set => FNavigable = value;
    }

    [Delphi(PortStatus.Ported)] public TMoldRole? OtherEnd => GetOtherEnd();

    [Delphi(PortStatus.Ported)]
    public bool Ordered
    {
        get => FOrdered;
        set => FOrdered = value;
    }

    [Delphi(PortStatus.Ported)] public bool EffectiveOrdered => GetEffectiveOrdered();
    [Delphi(PortStatus.Ported)] public bool Mandatory => GetMandatory();
    [Delphi(PortStatus.Ported)] public bool Embed => GetEmbed();
    [Delphi(PortStatus.Ported)] public bool EffectiveEmbedded => GetEffectiveEmbed();

    [Delphi(PortStatus.Ported)]
    public new TMoldClass? MoldClass
    {
        get => GetMoldClass();
        set => SetMoldClass(value);
    }

    [Delphi(PortStatus.Ported)] public TMoldQualifierList Qualifiers => FQualifiers!;
    [Delphi(PortStatus.Ported)] public bool QualifiedMulti => GetQualifiedMulti();
    [Delphi(PortStatus.Ported)] public bool HasLinkRole => GetHasLinkRole();
    [Delphi(PortStatus.Ported)] public TMoldRole? LinkRole => GetLinkRole();
    [Delphi(PortStatus.Ported)] public TMoldRole? MainRole => GetMainRole();
    [Delphi(PortStatus.Ported)] public TBoldRoleType RoleType => fRoleType;

    [Delphi(PortStatus.Ported)]
    public TAggregationKind Aggregation
    {
        get => fAggregation;
        set => fAggregation = value;
    }

    [Delphi(PortStatus.Ported)]
    public TChangeableKind Changeability
    {
        get => fChangeability;
        set => fChangeability = value;
    }

    [Delphi(PortStatus.Ported)]
    public string Multiplicity
    {
        get => fMultiplicity;
        set => fMultiplicity = value ?? "";
    }

    [Delphi(PortStatus.Ported)] public TDeleteAction DeleteAction => GetDeleteAction();
    [Delphi(PortStatus.Ported)] public TDeleteAction EffectiveDeleteAction => GetEffectiveDeleteAction();
    [Delphi(PortStatus.Ported)] public TBoldAssociationEndDefaultRegionMode DefaultRegionMode => GetDefaultRegionMode();
    [Delphi(PortStatus.Ported)] public TBoldAssociationEndDefaultRegionMode EffectiveDefaultRegionMode => GetEffectiveDefaultRegionMode();
}

/// <summary>A binary association: two ends, an optional link class, and the link roles made for it.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldAssociation : TMoldElement
{
    private bool fDerived;
    private readonly TMoldModel FModel;
    private TMoldRoleList? FRoles;
    internal TMoldRoleList? fLinkRoles;
    internal TMoldRoleList? fInnerLinkRoles;
    private TMoldClass? FLinkClass;
    private TStringList? fAllPossibleNames;

    /// <summary>Creates the association in the model <paramref name="Parent"/>.</summary>
    [Delphi(PortStatus.Ported)]
    public TMoldAssociation(TMoldElement? Parent, string Name)
        : base(Parent, Name)
    {
        System.Diagnostics.Debug.Assert(Parent is TMoldModel);
        FModel = (TMoldModel)Parent!;
        SetBoldTV(TAG_DELPHINAME, TV_NAME);
        Model.Associations.Add(this);
        FRoles = new TMoldRoleList();
        fLinkRoles = new TMoldRoleList();
        fInnerLinkRoles = new TMoldRoleList();
    }

    [Delphi(PortStatus.Ported)]
    public override void Dispose()
    {
        LinkClass = null;
        Model?.Associations?.RemoveEntryReference(this);
        SysUtils.FreeAndNil(ref FRoles);
        SysUtils.FreeAndNil(ref fLinkRoles);
        SysUtils.FreeAndNil(ref fInnerLinkRoles);
        fAllPossibleNames = null;
        base.Dispose();
    }

    /// <summary>Sets the link class; a class can be the link class of one association only.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetLinkClass(TMoldClass? Value)
    {
        if (LinkClass is not null)
        {
            LinkClass.Association = null;
            FLinkClass = null;
        }

        if (Value is not null)
        {
            if (Value.Association is not null && Value.Association != this)
            {
                throw new EBold(SysUtils.Format(BoldMoldConsts.sClassIsRelation, Value.Name));
            }

            FLinkClass = Value;
            LinkClass!.Association = this;
        }
    }

    internal void SetLinkClassInternal(TMoldClass? Value) => SetLinkClass(Value);

    [Delphi(PortStatus.Ported)]
    private bool GetPersistent() =>
        SysUtils.SameText(StdTVByName(BoldUMLTaggedValues.TAG_PERSISTENCE), BoldUMLTaggedValues.TV_PERSISTENCE_PERSISTENT);

    /// <summary>
    /// The expression name, former names, and when the name is the two role names joined (and the link class, if any,
    /// has the same name), every combination of the ends' current and former names.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private TStringList GetAllPossibleNames()
    {
        if (fAllPossibleNames is null)
        {
            fAllPossibleNames = new TStringList { Sorted = true, Duplicates = TDuplicates.dupIgnore };
            fAllPossibleNames.CommaText = FormerNames;
            fAllPossibleNames.Add(ExpandedExpressionName);
            bool associationNameImplicit = SysUtils.SameText(Name, Roles[0].Name + Roles[1].Name)
                || SysUtils.SameText(Name, Roles[1].Name + Roles[0].Name);
            bool linkClassImplicit = LinkClass is null || SysUtils.SameText(LinkClass.Name, Name);
            if (associationNameImplicit && linkClassImplicit && (Roles[0].FormerNames != "" || Roles[1].FormerNames != ""))
            {
                var roleNames1 = new TStringList { CommaText = Roles[0].FormerNames };
                roleNames1.Add(Roles[0].Name);
                var roleNames2 = new TStringList { CommaText = Roles[1].FormerNames };
                roleNames2.Add(Roles[1].Name);
                for (int rn1Ix = 0; rn1Ix < roleNames1.Count; rn1Ix++)
                {
                    for (int rn2Ix = 0; rn2Ix < roleNames2.Count; rn2Ix++)
                    {
                        // Skip the pair of current names (last in each list).
                        if (!(rn1Ix == roleNames1.Count - 1 && rn2Ix == roleNames2.Count - 1))
                        {
                            fAllPossibleNames.Add(roleNames1[rn1Ix] + roleNames2[rn2Ix]);
                            fAllPossibleNames.Add(roleNames2[rn2Ix] + roleNames1[rn1Ix]);
                        }
                    }
                }
            }
        }

        return fAllPossibleNames;
    }

    [Delphi(PortStatus.Ported)] private TBoldStorage GetStorage() => TBoldTaggedValueSupport.StringToStorage(BoldTVByName(TAG_STORAGE));

    [Delphi(PortStatus.Ported)]
    protected override TMoldModel GetModel() => FModel;

    /// <summary>Persistent, not derived, and both end classes (other than the link class) effectively persistent.</summary>
    [Delphi(PortStatus.Ported)]
    protected bool GetEffectivePersistent() =>
        Persistent && !Derived
        && Roles[0].MoldClass is not null && Roles[0].MoldClass != LinkClass && Roles[0].MoldClass!.EffectivePersistent
        && Roles[1].MoldClass is not null && Roles[1].MoldClass != LinkClass && Roles[1].MoldClass!.EffectivePersistent;

    [Delphi(PortStatus.Ported)]
    protected override string GetUMLClassName() => "Association";

    [Delphi(PortStatus.Ported)]
    public override void NameChanged() => Model?.Associations?.ItemChanged(this);

    [Delphi(PortStatus.Ported)]
    public TMoldClass? LinkClass
    {
        get => FLinkClass;
        set => SetLinkClass(value);
    }

    [Delphi(PortStatus.Ported)] public TMoldRoleList Roles => FRoles!;

    [Delphi(PortStatus.Ported)]
    public bool Derived
    {
        get => fDerived;
        set => fDerived = value;
    }

    [Delphi(PortStatus.Ported)] public bool Persistent => GetPersistent();
    [Delphi(PortStatus.Ported)] public bool EffectivePersistent => GetEffectivePersistent();
    [Delphi(PortStatus.Ported)] public TBoldEvolutionState EvolutionState => GetEvolutionState();
    [Delphi(PortStatus.Ported)] public string FormerNames => GetFormerNames();
    [Delphi(PortStatus.Ported)] public TStringList AllPossibleNames => GetAllPossibleNames();
    [Delphi(PortStatus.Ported)] public TBoldStorage Storage => GetStorage();
}

/// <summary>A qualifier of an association end.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldQualifier : TMoldElement
{
    private string FBoldType = "";
    private readonly TMoldRole FMoldRole;

    [Delphi(PortStatus.Ported)]
    public TMoldQualifier(TMoldElement? Parent, string Name)
        : base(Parent, Name)
    {
        System.Diagnostics.Debug.Assert(Parent is TMoldRole);
        FMoldRole = (TMoldRole)Parent!;
        MoldRole.Qualifiers.Add(this);
    }

    [Delphi(PortStatus.Ported)]
    protected override TMoldModel GetModel() => MoldRole.Model;

    [Delphi(PortStatus.Ported)]
    protected override string GetUMLClassName() => "Attribute";

    [Delphi(PortStatus.Ported)]
    public override void NameChanged() => MoldRole?.Qualifiers?.ItemChanged(this);

    [Delphi(PortStatus.Ported)]
    public string BoldType
    {
        get => FBoldType;
        set => FBoldType = value ?? "";
    }

    [Delphi(PortStatus.Ported)] public TMoldRole MoldRole => FMoldRole;
}

/// <summary>A unit of generated code; classes are put in components by their UnitName tagged value.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldComponent : TBoldMemoryManagedObject, IDisposable
{
    private TMoldComponentList? fDependencies;
    private readonly TMoldModel fMoldModel;
    private string fName = "";

    [Delphi(PortStatus.Ported)]
    public TMoldComponent(TMoldModel MoldModel)
    {
        fDependencies = new TMoldComponentList();
        fMoldModel = MoldModel;
    }

    [Delphi(PortStatus.Ported)]
    public void Dispose()
    {
        fDependencies = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>True when <paramref name="Component"/> is among this component's interface dependencies, directly or not.</summary>
    [Delphi(PortStatus.Ported)]
    public bool DependentOf(TMoldComponent Component)
    {
        var dependencyList = new TStringList();
        GetInterfaceDependencies(dependencyList);
        return dependencyList.IndexOf(Component.Name) != -1;
    }

    /// <summary>Adds the names of every component this one depends on, depth first.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetInterfaceDependencies(TStringList StringList)
    {
        for (int i = 0; i < Dependencies.Count; i++)
        {
            if (StringList.IndexOf(Dependencies[i].Name) == -1)
            {
                StringList.Add(Dependencies[i].Name);
                Dependencies[i].GetInterfaceDependencies(StringList);
            }
        }
    }

    /// <summary>Fills the list with every other component of the model that is not an interface dependency.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetImplementationDependencies(TStringList StringList)
    {
        var interfaceDep = new TStringList();
        StringList.Clear();
        GetInterfaceDependencies(interfaceDep);
        for (int i = 0; i < fMoldModel.Components.Count; i++)
        {
            if (fMoldModel.Components[i] != this && interfaceDep.IndexOf(fMoldModel.Components[i].Name) == -1)
            {
                StringList.Add(fMoldModel.Components[i].Name);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public string Name
    {
        get => fName;
        set => fName = value ?? "";
    }

    [Delphi(PortStatus.Ported)] public TMoldComponentList Dependencies => fDependencies!;
}

/// <summary>A list of components; lookup by name ignores case.</summary>
[Delphi(PortStatus.Ported)]
public class TMoldComponentList : TList
{
    [Delphi(PortStatus.Ported)]
    private TMoldComponent GetItem(int index) => (TMoldComponent)Get(index)!;

    [Delphi(PortStatus.Ported)]
    private TMoldComponent? GetItemByName(string name)
    {
        for (int i = 0; i < Count; i++)
        {
            if (SysUtils.SameText(GetItem(i).Name, name))
            {
                return GetItem(i);
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported, Symbol = "TMoldComponentList.Items")]
    public new TMoldComponent this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported)]
    public TMoldComponent? ItemsByName(string name) => GetItemByName(name);
}

/// <summary>Unit-level constants and routines of BoldMeta.</summary>
public static partial class BoldMeta
{
    [Delphi(PortStatus.Ported)] public const int BOLD_FIRSTUSERDISPID = 1024;
    [Delphi(PortStatus.Ported)] public const int BOLD_DISPID_ROUND = 256;

    /// <summary>The model checksum hash of a string; the same formula as the string hash of hash indexes.</summary>
    [Delphi(PortStatus.Ported)]
    internal static uint CRCHash(string S)
    {
        uint result = 0;
        foreach (char c in S ?? "")
        {
            result = (((result << 3) & 2147483647) | (result >> (32 - 3))) ^ c;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    internal static uint SumCRCs(uint CRC1, uint CRC2) => (((CRC1 << 3) & 2147483647) | (CRC1 >> (32 - 3))) ^ CRC2;

    /// <summary>Orders classes by name, as AnsiCompareStr.</summary>
    [Delphi(PortStatus.Ported)]
    internal static int ClassesComparer(object? Item1, object? Item2)
    {
        if (Item1 is TMoldClass c1 && Item2 is TMoldClass c2)
        {
            return SysUtils.AnsiCompareStr(c1.Name, c2.Name);
        }

        throw new EBoldInternal("ooops, class was not a class in TMoldModel.EnsureTopSorted");
    }
}
