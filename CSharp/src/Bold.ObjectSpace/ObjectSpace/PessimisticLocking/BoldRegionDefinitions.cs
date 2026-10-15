// Delphi: Source/ObjectSpace/PessimisticLocking/BoldRegionDefinitions.pas
// Port map: CSharp/port/units/BoldRegionDefinitions.md

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRegionDefinitions", Symbol = "TBoldRegionCoreDefinition.Destroy", Note = "Freed the concrete definitions and lists; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldRegionDefinitions", Symbol = "TBoldConcreteRegionDefinition.Destroy", Note = "Cleared and freed the lists; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldRegionDefinitions", Symbol = "GetEnsuredItem", Note = "BoldRegionDefinitions.GetEnsuredItem, generic over the item class.")]

namespace Bold;

/// <summary>
/// The lock region definitions of a model: core definitions (named regions), their concrete definitions per root class,
/// the members each includes and the subregions reached by navigation. Lookups by member and by root class.
/// </summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegionDefinitions : TBoldSubscribableObject
{
    private readonly TList fClassList = new();
    private readonly TList fConcreteRegions = new();
    private readonly TList fCoreDefinitions = new();

    [Delphi(PortStatus.Ported)]
    public TBoldRegionDefinitions()
    {
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldRegionDefinitions.Destroy")]
    public override void Dispose()
    {
        FreePublisher();
        Clear();
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    internal void AddRegionElementInclusion(TBoldRegionElementInclusion Item) => RegionInclusionsByMember(Item.Member).Add(Item);

    [Delphi(PortStatus.Ported)]
    internal void AddConcreteRegionDefinition(TBoldConcreteRegionDefinition Item) => ConcreteRegionDefinitionsByRootClass(Item.RootClass).Add(Item);

    [Delphi(PortStatus.Ported)]
    internal void AddCoreDefinition(TBoldRegionCoreDefinition Item) => fCoreDefinitions.Add(Item);

    [Delphi(PortStatus.Ported)]
    private TBoldRegionElementInclusionList GetRegionInclusionsByMember(TBoldMemberRTInfo Member)
    {
        TList MemberList = BoldRegionDefinitions.GetEnsuredItem<TList>(fClassList, Member.ClassTypeInfo.TopSortedIndex);
        return BoldRegionDefinitions.GetEnsuredItem<TBoldRegionElementInclusionList>(MemberList, Member.index);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldConcreteRegionDefinitionList GetConcreteRegionDefinitionsByRootClass(TBoldClassTypeInfo TheClass) =>
        BoldRegionDefinitions.GetEnsuredItem<TBoldConcreteRegionDefinitionList>(fConcreteRegions, TheClass.TopSortedIndex);

    [Delphi(PortStatus.Ported)]
    private TBoldRegionCoreDefinition? GetCoreDefinition(string Name)
    {
        TBoldRegionCoreDefinition? result = null;
        for (int i = 0; i < fCoreDefinitions.Count; i++)
        {
            if (((TBoldRegionCoreDefinition)fCoreDefinitions[i]!).Name == Name)
            {
                result = (TBoldRegionCoreDefinition)fCoreDefinitions[i]!;
            }
        }

        return result;
    }

    /// <summary>Tells subscribers (beRegionDefinitionClearing) and drops all definitions.</summary>
    [Delphi(PortStatus.Ported)]
    public void Clear()
    {
        SendEvent(BoldRegionDefinitions.beRegionDefinitionClearing);
        fClassList.Clear();
        fConcreteRegions.Clear();
        fCoreDefinitions.Clear();
    }

    /// <summary>Copies each concrete definition to the subclasses of its root class.</summary>
    [Delphi(PortStatus.Ported)]
    public void ExpandDefinitions()
    {
        for (int i = 0; i < fCoreDefinitions.Count; i++)
        {
            ((TBoldRegionCoreDefinition)fCoreDefinitions[i]!).ExpandDefinitions();
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldRegionCoreDefinition EnsuredCoreDefinition(string Name) => CoreDefinition(Name) ?? new TBoldRegionCoreDefinition(this, Name);

    [Delphi(PortStatus.Ported)] public TBoldRegionElementInclusionList RegionInclusionsByMember(TBoldMemberRTInfo Member) => GetRegionInclusionsByMember(Member);
    [Delphi(PortStatus.Ported)] public TBoldConcreteRegionDefinitionList ConcreteRegionDefinitionsByRootClass(TBoldClassTypeInfo TheClass) => GetConcreteRegionDefinitionsByRootClass(TheClass);
    [Delphi(PortStatus.Ported)] public TBoldRegionCoreDefinition? CoreDefinition(string Name) => GetCoreDefinition(Name);
    [Delphi(PortStatus.Ported)] public TList CoreDefinitions => fCoreDefinitions;
}

/// <summary>A named region; has one concrete definition per root class.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegionCoreDefinition : TBoldMemoryManagedObject
{
    private readonly string fName;
    private readonly TBoldRegionDefinitions fOwner;
    private readonly TBoldSubregionReferenceList fUsedBy = new();
    private readonly TBoldConcreteRegionDefinitionList fConcreteDefinitions = new();

    [Delphi(PortStatus.Ported)]
    public TBoldRegionCoreDefinition(TBoldRegionDefinitions Owner, string Name)
    {
        fOwner = Owner;
        fName = Name;
        fOwner.AddCoreDefinition(this);
    }


    /// <summary>Gives each subclass of a class with a concrete definition a copy of it.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal void ExpandDefinitions()
    {
        if (fConcreteDefinitions.Count > 0)
        {
            TBoldSystemTypeInfo SystemTypeInfo = fConcreteDefinitions[0].RootClass.SystemTypeInfo;
            for (int i = 1; i < SystemTypeInfo.TopSortedClasses.Count; i++)
            {
                TBoldClassTypeInfo ClassTypeInfo = SystemTypeInfo.TopSortedClasses[i];
                TBoldConcreteRegionDefinition? SuperDef = ConcreteDefinitions.FindByRootClass(ClassTypeInfo.SuperClassTypeInfo);
                if (SuperDef is not null)
                {
                    EnsuredConcreteDefinition(ClassTypeInfo, out _).CopyFromDefinition(SuperDef);
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldConcreteRegionDefinition EnsuredConcreteDefinition(TBoldClassTypeInfo Root, out bool Existed)
    {
        TBoldConcreteRegionDefinition? result = ConcreteDefinitions.FindByRootClass(Root);
        Existed = result is not null;
        return result ?? new TBoldConcreteRegionDefinition(this, Root);
    }

    [Delphi(PortStatus.Ported)] public string Name => fName;
    [Delphi(PortStatus.Ported)] public TBoldRegionDefinitions Owner => fOwner;
    [Delphi(PortStatus.Ported)] public TBoldConcreteRegionDefinitionList ConcreteDefinitions => fConcreteDefinitions;
    [Delphi(PortStatus.Ported)] public TBoldSubregionReferenceList UsedBy => fUsedBy;
}

/// <summary>A region for one root class: the members it includes, its subregions and the regions it is a subregion of.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldConcreteRegionDefinition : TBoldMemoryManagedObject
{
    private readonly TBoldRegionElementInclusionList fElements = new();
    private readonly TBoldSubregionReferenceList fSubregions = new();
    private readonly TBoldClassTypeInfo fRootClass;
    private readonly TBoldRegionCoreDefinition fCoreDefinition;
    private readonly TBoldSubregionReferenceList fParentRegions = new();

    /// <summary>Registers the definition with its core definition and owner, and links the regions that reach it.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldConcreteRegionDefinition(TBoldRegionCoreDefinition CoreDefinition, TBoldClassTypeInfo Root)
    {
        fCoreDefinition = CoreDefinition;
        fRootClass = Root;
        CoreDefinition.ConcreteDefinitions.Add(this);
        CoreDefinition.Owner.AddConcreteRegionDefinition(this);
        for (int i = 0; i < CoreDefinition.UsedBy.Count; i++)
        {
            if (Root.ConformsTo(CoreDefinition.UsedBy[i].SubregionRootNavigation.ClassTypeInfoOfOtherEnd))
            {
                fParentRegions.Add(CoreDefinition.UsedBy[i]);
            }
        }
    }


    [Delphi(PortStatus.Ported)]
    private string GetAsString() => RootClass.ExpressionName + ":" + CoreDefinition.Name;

    [Delphi(PortStatus.Ported)]
    protected internal void CopyFromDefinition(TBoldConcreteRegionDefinition SuperDef)
    {
        for (int i = 0; i < SuperDef.Elements.Count; i++)
        {
            SuperDef.Elements[i].Clone(this);
        }

        for (int i = 0; i < SuperDef.Subregions.Count; i++)
        {
            SuperDef.Subregions[i].Clone(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Clear()
    {
        fElements.Clear();
        fSubregions.Clear();
    }

    [Delphi(PortStatus.Ported)] public TBoldRegionCoreDefinition CoreDefinition => fCoreDefinition;
    [Delphi(PortStatus.Ported)] public TBoldClassTypeInfo RootClass => fRootClass;
    [Delphi(PortStatus.Ported)] public TBoldRegionElementInclusionList Elements => fElements;
    [Delphi(PortStatus.Ported)] public TBoldSubregionReferenceList Subregions => fSubregions;
    [Delphi(PortStatus.Ported)] public TBoldSubregionReferenceList ParentRegions => fParentRegions;
    [Delphi(PortStatus.Ported)] public string AsString => GetAsString();
}

/// <summary>A subregion of a concrete region: a core definition reached by navigating a role, dependent or not.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSubregionReference : TBoldMemoryManagedObject
{
    private readonly TBoldRoleRTInfo fSubregionRootNavigation;
    private readonly TBoldRegionCoreDefinition fSubregionCoreDefinition;
    private readonly TBoldConcreteRegionDefinition fParentRegion;
    private readonly bool fIsDependent;

    /// <summary>Registers the reference with its parent region and subregion, and with the subregion's matching concrete definitions.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldSubregionReference(TBoldConcreteRegionDefinition ParentRegion, TBoldRegionCoreDefinition SubregionCoreDefinition, TBoldRoleRTInfo Navigation,
        bool IsDependent)
    {
        fParentRegion = ParentRegion;
        fSubregionCoreDefinition = SubregionCoreDefinition;
        fIsDependent = IsDependent;
        fSubregionRootNavigation = Navigation ?? throw new EBoldInternal("Tried to create a SubRegionReference without a RoleRTInfo");

        ParentRegion.Subregions.Add(this);
        SubregionCoreDefinition.UsedBy.Add(this);
        for (int i = 0; i < SubregionCoreDefinition.ConcreteDefinitions.Count; i++)
        {
            if (SubregionCoreDefinition.ConcreteDefinitions[i].RootClass.ConformsTo(Navigation.ClassTypeInfoOfOtherEnd))
            {
                SubregionCoreDefinition.ConcreteDefinitions[i].ParentRegions.Add(this);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal void Clone(TBoldConcreteRegionDefinition NewOwner) =>
        _ = new TBoldSubregionReference(NewOwner, SubregionCoreDefinition, SubregionRootNavigation, IsDependent);

    [Delphi(PortStatus.Ported)] public TBoldConcreteRegionDefinition ParentRegion => fParentRegion;
    [Delphi(PortStatus.Ported)] public TBoldRoleRTInfo SubregionRootNavigation => fSubregionRootNavigation;
    [Delphi(PortStatus.Ported)] public TBoldRegionCoreDefinition SubregionCoreDefinition => fSubregionCoreDefinition;
    [Delphi(PortStatus.Ported)] public bool IsDependent => fIsDependent;
}

/// <summary>A member included in a concrete region; including a multi role forces its other end to be loaded.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegionElementInclusion : TBoldMemoryManagedObject
{
    private readonly TBoldMemberRTInfo fMember;
    private readonly TBoldConcreteRegionDefinition fRegion;

    [Delphi(PortStatus.Ported)]
    public TBoldRegionElementInclusion(TBoldConcreteRegionDefinition Region, TBoldMemberRTInfo Member)
    {
        fRegion = Region;
        fMember = Member;

        Region.Elements.Add(this);
        Region.CoreDefinition.Owner.AddRegionElementInclusion(this);

        if (Member.IsMultiRole)
        {
            ((TBoldRoleRTInfo)Member).RoleRTInfoOfOtherEnd!.SetForceOtherEnd();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal void Clone(TBoldConcreteRegionDefinition NewOwner) => _ = new TBoldRegionElementInclusion(NewOwner, Member);

    [Delphi(PortStatus.Ported)] public TBoldConcreteRegionDefinition Region => fRegion;
    [Delphi(PortStatus.Ported)] public TBoldMemberRTInfo Member => fMember;
}

[Delphi(PortStatus.Ported)]
public class TBoldConcreteRegionDefinitionList : TList
{
    [Delphi(PortStatus.Ported)]
    private TBoldConcreteRegionDefinition GetItem(int i) => (TBoldConcreteRegionDefinition)base[i]!;

    [Delphi(PortStatus.Ported)]
    public TBoldConcreteRegionDefinition? FindByRootClass(TBoldClassTypeInfo? RootClass)
    {
        for (int i = 0; i < Count; i++)
        {
            if (this[i].RootClass == RootClass)
            {
                return this[i];
            }
        }

        return null;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldConcreteRegionDefinitionList.Items")]
    public new TBoldConcreteRegionDefinition this[int i] => GetItem(i);
}

[Delphi(PortStatus.Ported)]
public class TBoldSubregionReferenceList : TList
{
    [Delphi(PortStatus.Ported)]
    private TBoldSubregionReference GetItem(int i) => (TBoldSubregionReference)base[i]!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldSubregionReferenceList.Items")]
    public new TBoldSubregionReference this[int i] => GetItem(i);
}

[Delphi(PortStatus.Ported)]
public class TBoldRegionElementInclusionList : TList
{
    [Delphi(PortStatus.Ported)]
    private TBoldRegionElementInclusion GetItem(int i) => (TBoldRegionElementInclusion)base[i]!;

    [Delphi(PortStatus.Ported, Symbol = "TBoldRegionElementInclusionList.Items")]
    public new TBoldRegionElementInclusion this[int i] => GetItem(i);
}

/// <summary>Unit-level constants and routines of BoldRegionDefinitions.</summary>
public static partial class BoldRegionDefinitions
{
    [Delphi(PortStatus.Ported)] public const int beRegionDefinitionClearing = 100;

    /// <summary>The item at the index, created (and the list grown with nils) when missing.</summary>
    internal static T GetEnsuredItem<T>(TList List, int index) where T : class, new()
    {
        while (List.Count <= index)
        {
            List.Add(null);
        }

        if (List[index] is not T Obj)
        {
            Obj = new T();
            List[index] = Obj;
        }

        return Obj;
    }
}
