// Delphi: Source/ObjectSpace/PessimisticLocking/BoldLockRegions.pas
// Port map: CSharp/port/units/BoldLockRegions.md

using static Bold.BoldCoreConsts;
using static Bold.BoldSubscription;

namespace Bold;

[Delphi(PortStatus.Ported)]
public delegate void TBoldRegionEvent(TBoldRegion Sender, int Event);

/// <summary>A lock region instance: a concrete region definition rooted at one object; follows its root and subregion links.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegion : TBoldSubscriber
{
    private readonly TBoldConcreteRegionDefinition fDefinition;
    private TBoldObjectLocator? fRootLocator;
    private readonly TBoldRegionFactory fFactory;

    [Delphi(PortStatus.Ported)]
    public TBoldRegion(TBoldConcreteRegionDefinition Definition, TBoldObjectLocator RootLocator, TBoldRegionFactory Factory)
    {
        fRootLocator = RootLocator;
        fFactory = Factory;
        fDefinition = Definition;
        PlaceRootSubscriptions();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldRegion.Destroy")]
    public override void Dispose()
    {
        fRootLocator = null;
        base.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public void FillDependentSubregions(TBoldRegionList RegionList) => FillSubregions(RegionList, true);

    [Delphi(PortStatus.Ported)]
    public void FillIndependentSubregions(TBoldRegionList RegionList) => FillSubregions(RegionList, false);

    /// <summary>Adds the (dependent or independent) subregions reached from the root, subscribing to the navigations.</summary>
    [Delphi(PortStatus.Ported)]
    private void FillSubregions(TBoldRegionList RegionList, bool Dependent)
    {
        using var ie = new TBoldIndirectElement();
        for (int i = 0; i < Definition.Subregions.Count; i++)
        {
            TBoldSubregionReference Subregion = Definition.Subregions[i];
            if (Subregion.IsDependent == Dependent)
            {
                BoldLockRegions.DoNavigationAndSubscribe(Root, Subregion.SubregionRootNavigation, ie, this, BoldLockRegions.breSubregionsChanged);
                if (ie.Value is TBoldObjectList Objects)
                {
                    Objects.EnsureObjects();
                    for (int j = 0; j < Objects.Count; j++)
                    {
                        RegionList.Add(fFactory.GetRegion(Subregion.SubregionCoreDefinition, Objects.Locators(j)!));
                    }
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected string GetAsString()
    {
        if (RootLocator is null)
        {
            throw new EBoldInternal(SysUtils.Format(sRegionMissingIDOrLocator, GetType().Name));
        }

        return fFactory.RegionId(Definition.CoreDefinition, RootLocator);
    }

    /// <summary>The root object and the members the region includes.</summary>
    [Delphi(PortStatus.Ported)]
    public void GetElements(TList ResultList)
    {
        ResultList.Add(Root);
        for (int i = 0; i < fDefinition.Elements.Count; i++)
        {
            ResultList.Add(Root.BoldMembers(fDefinition.Elements[i].Member.index));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObject GetRoot() => fRootLocator!.EnsuredBoldObject!;

    [Delphi(PortStatus.Ported)]
    private void PlaceRootSubscriptions() => Root.AddSmallSubscription(this, TBoldSmallEventSet.Of(beDestroying, bePreUpdateId, bePostUpdateId), -1);

    /// <summary>
    /// The root destroyed: leaves the factory (which frees the region); the root's id changing: re-indexed; a navigated
    /// link changing: resubscribes and tells the factory.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected override void Receive(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        if (Originator is TBoldObject { BoldObjectLocator: null } && RootLocator?.BoldObject is null)
        {
            fFactory.fLookup.ItemChanged(this);
            fFactory.fLookup.Remove(this);
        }
        else if (Originator == RootLocator?.BoldObject)
        {
            switch (OriginalEvent)
            {
                case beDestroying:
                    fFactory.fLookup.Remove(this);
                    break;
                case bePreUpdateId:
                    fFactory.fLookup.OwnsEntries = false;
                    fFactory.fLookup.Remove(this);
                    fFactory.fLookup.OwnsEntries = true;
                    break;
                case bePostUpdateId:
                    fFactory.fLookup.Add(this);
                    break;
            }
        }
        else
        {
            CancelAllSubscriptions();
            PlaceRootSubscriptions();
            fFactory.NotifyRegionChanged(this, RequestedEvent);
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldConcreteRegionDefinition Definition => fDefinition;
    [Delphi(PortStatus.Ported)] public TBoldObject Root => GetRoot();
    [Delphi(PortStatus.Ported)] public TBoldObjectLocator? RootLocator => fRootLocator;
    [Delphi(PortStatus.Ported)] public string AsString => GetAsString();
    [Delphi(PortStatus.Ported)] public TBoldRegionFactory Factory => fFactory;
}

/// <summary>Creates and keeps the regions of a system, one per definition and root, found by id ("rootid.Region").</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegionFactory : TBoldAbstractRegionFactory, IDisposable
{
    private readonly TBoldRegionDefinitions fDefinitions;
    internal TBoldRegionLookup fLookup;
    private TBoldRegionEvent? fOnRegionChanged;

    [Delphi(PortStatus.Ported)]
    public TBoldRegionFactory(TBoldRegionDefinitions Definitions)
    {
        fLookup = new TBoldRegionLookup { OwnsEntries = true };
        fDefinitions = Definitions;
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldRegionFactory.Destroy")]
    public void Dispose()
    {
        fLookup.Dispose();
        GC.SuppressFinalize(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldRegionCoreDefinition CoreDefintionFromRegionId(string RegionId) =>
        fDefinitions.CoreDefinition(SysUtils.Copy(RegionId, SysUtils.Pos(".", RegionId) + 1, SysUtils.MaxInt)) ??
        throw new EBoldInternal(SysUtils.Format(sBadRegionID, GetType().Name, "CoreDefintionFromRegionId", RegionId));

    /// <summary>The region of the definition rooted at the object, created when new.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldRegion GetRegion(TBoldRegionCoreDefinition Definition, TBoldObjectLocator RootLocator)
    {
        TBoldRegion? result = fLookup.FindByID(RegionId(Definition, RootLocator));
        if (result is null)
        {
            TBoldClassTypeInfo rootClass = RootLocator.EnsuredBoldObject!.BoldClassTypeInfo;
            TBoldConcreteRegionDefinition ConcreteDef = Definition.ConcreteDefinitions.FindByRootClass(rootClass) ??
                throw new EBold(SysUtils.Format(sBadRegionDefinition, GetType().Name, Definition.Name, rootClass.ExpressionName));
            result = new TBoldRegion(ConcreteDef, RootLocator, this);
            fLookup.Add(result);
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldRegion GetRegionByName(string RegionName) =>
        fLookup.FindByID(RegionName) ?? GetRegion(CoreDefintionFromRegionId(RegionName), RootObjectLocatorFromRegionId(RegionName));

    /// <summary>The regions that include the member (or that are rooted at the object).</summary>
    [Delphi(PortStatus.Ported)]
    public override void GetRegionsForElement(TBoldDomainElement Element, TList ResultList)
    {
        if (Element is TBoldMember aMember)
        {
            TBoldRegionElementInclusionList RegionInclusions = fDefinitions.RegionInclusionsByMember(aMember.BoldMemberRTInfo!);
            for (int i = 0; i < RegionInclusions.Count; i++)
            {
                ResultList.Add(GetRegion(RegionInclusions[i].Region.CoreDefinition, aMember.OwningObject!.BoldObjectLocator!));
            }
        }
        else if (Element is TBoldObject anObject)
        {
            TBoldConcreteRegionDefinitionList ConcreteDefs = fDefinitions.ConcreteRegionDefinitionsByRootClass(anObject.BoldClassTypeInfo);
            for (int i = 0; i < ConcreteDefs.Count; i++)
            {
                ResultList.Add(GetRegion(ConcreteDefs[i].CoreDefinition, anObject.BoldObjectLocator!));
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    internal void NotifyRegionChanged(TBoldRegion Sender, int Event) => fOnRegionChanged?.Invoke(Sender, Event);

    /// <summary>"id.Region", with an "i" in front for an internal (unsaved) id.</summary>
    [Delphi(PortStatus.Ported)]
    internal string RegionId(TBoldRegionCoreDefinition Definition, TBoldObjectLocator RootLocator)
    {
        string result = RootLocator.BoldObjectID.AsString + "." + Definition.Name;
        return RootLocator.BoldObjectID is TBoldInternalObjectId ? "i" + result : result;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldObjectLocator RootObjectLocatorFromRegionId(string RegionId)
    {
        TBoldObjectId ObjId;
        if (RegionId[0] == 'i')
        {
            int AsInt = SysUtils.StrToIntDef(SysUtils.Copy(RegionId, 2, SysUtils.Pos(".", RegionId) - 1), -1);
            if (AsInt == -1)
            {
                throw new EBoldInternal(SysUtils.Format(sBadRegionID, GetType().Name, "RootObjectFromRegionId", RegionId));
            }

            ObjId = new TBoldInternalObjectId(AsInt, 0, false);
        }
        else
        {
            int AsInt = SysUtils.StrToIntDef(SysUtils.Copy(RegionId, 1, SysUtils.Pos(".", RegionId) - 1), -1);
            if (AsInt == -1)
            {
                throw new EBoldInternal(SysUtils.Format(sBadRegionID, GetType().Name, "RootObjectFromRegionId", RegionId));
            }

            ObjId = new TBoldDefaultID(0, false) { AsInteger = AsInt };
        }

        return System!.EnsuredLocatorByID(ObjId);
    }

    [Delphi(PortStatus.Ported)]
    public TBoldRegionEvent? OnRegionChanged
    {
        get => fOnRegionChanged;
        set => fOnRegionChanged = value;
    }
}

/// <summary>A set of regions found by id; expands itself to parent regions and subregions.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegionLookup : TBoldUnOrderedIndexableList
{
    private static int IX_RegionId = -1;

    /// <summary>A lookup that does not own its regions (Delphi's constructor skips the inherited one that would own them).</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldRegionLookup()
    {
        OwnsEntries = false;
        BoldIndexableList.SetIndexVariable(ref IX_RegionId, AddIndex(new TBoldRegionIndex()));
    }

    private IEnumerable<TBoldRegion> Regions()
    {
        using TBoldIndexableListTraverser Traverser = CreateTraverser()!;
        var result = new List<TBoldRegion>();
        while (!Traverser.EndOfList)
        {
            result.Add((TBoldRegion)Traverser.Item!);
            Traverser.Next();
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    public TBoldRegion? FindByID(string RegionId) => (TBoldRegion?)((TBoldRegionIndex)Indexes(IX_RegionId)!).FindByString(RegionId);

    [Delphi(PortStatus.Ported)]
    public void AddRegionLookup(TBoldRegionLookup Regions)
    {
        foreach (TBoldRegion region in Regions.Regions())
        {
            AddIfNotInLookup(region);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddRegionLookupWithFilter(TBoldRegionLookup Regions, TBoldRegionLookup Filter)
    {
        foreach (TBoldRegion Region in Regions.Regions())
        {
            if (Filter.FindByID(Region.AsString) is null)
            {
                AddIfNotInLookup(Region);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddRegionList(TBoldRegionList Regions)
    {
        for (int i = 0; i < Regions.Count; i++)
        {
            AddIfNotInLookup(Regions[i]);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void AddIfNotInLookup(TBoldRegion Region)
    {
        if (FindByID(Region.AsString) is null)
        {
            Add(Region);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void FillObjectList(TBoldObjectList ObjectList)
    {
        foreach (TBoldRegion region in Regions())
        {
            ObjectList.AddLocator(region.RootLocator);
        }
    }

    /// <summary>Adds to Regions, for each region here, the regions of the core definition reached by the navigation.</summary>
    [Delphi(PortStatus.Ported)]
    private void ExpandOneLevelRegionsForNavigation(TBoldRegionLookup Regions, TBoldRoleRTInfo Navigation, TBoldRegionCoreDefinition CoreDef,
        TBoldRegionLookup AlreadyExpandedRegions, TBoldRegionLookup AlreadyKnownRegions)
    {
        foreach (TBoldRegion Region in this.Regions())
        {
            using var ie = new TBoldIndirectElement();
            BoldLockRegions.DoNavigationAndSubscribe(Region.Root, Navigation, ie, Region, BoldLockRegions.breSubregionsChanged);
            if (ie.Value is TBoldObjectList Objects)
            {
                int OldRegionsCount = Regions.Count;
                for (int i = 0; i < Objects.Count; i++)
                {
                    TBoldRegion NewRegion = Region.Factory.GetRegion(CoreDef, Objects.Locators(i)!);
                    if (AlreadyExpandedRegions.FindByID(NewRegion.AsString) is null && AlreadyKnownRegions.FindByID(NewRegion.AsString) is null)
                    {
                        Regions.AddIfNotInLookup(NewRegion);
                    }
                }

                if (Regions.Count > OldRegionsCount)
                {
                    BoldLockRegions.BoldRegionExpansionDebugLogHandler?.LogFmt(sLogAddedRegions,
                        [Region.Root.BoldClassTypeInfo.ExpressionName, Navigation.ExpressionName, Regions.Count - OldRegionsCount]);
                }
            }
        }
    }

    /// <summary>
    /// Fetches (in one go, with the other regions of the same kind) the subregion links of these regions (all of one
    /// definition) and adds the subregions not already known.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void FetchAndExpandOneLevelSubRegions(TBoldRegionLookup SubRegions, TBoldRegionLookup AdditionalRegions, TBoldRegionLookup AlreadyExpandedRegions,
        TBoldRegionLookup AlreadyKnownRegions)
    {
        void MassiveFetch(TBoldSystem System, TBoldObjectList ObjectList, TBoldRoleRTInfo RoleRTInfo)
        {
            var TempObjectList = (TBoldObjectList)ObjectList.Clone();
            try
            {
                bool FetchNeeded = false;
                for (int i = 0; i < ObjectList.Count; i++)
                {
                    FetchNeeded = FetchNeeded || ObjectList.Locators(i)!.BoldObject is null || !ObjectList[i]!.BoldMemberAssigned(RoleRTInfo.index) ||
                        ObjectList[i]!.BoldMembers(RoleRTInfo.index).BoldPersistenceState == TBoldValuePersistenceState.bvpsInvalid;
                }

                if (FetchNeeded)
                {
                    TBoldClassTypeInfo DefiningClass = RoleRTInfo.ClassTypeInfo;
                    while (DefiningClass.FirstOwnMemberIndex > RoleRTInfo.index)
                    {
                        DefiningClass = DefiningClass.SuperClassTypeInfo!;
                    }

                    foreach (TBoldRegion aRegion in AdditionalRegions.Regions())
                    {
                        if (aRegion.Definition.RootClass.ConformsTo(DefiningClass))
                        {
                            TempObjectList.AddLocator(aRegion.RootLocator);
                        }
                    }
                }

                System.FetchLinksWithObjects(TempObjectList, RoleRTInfo.ExpressionName);
            }
            finally
            {
                TempObjectList.Dispose();
            }
        }

        var ObjectList = new TBoldObjectList();
        try
        {
            FillObjectList(ObjectList);
            ObjectList.EnsureObjects();
            TBoldConcreteRegionDefinition ConcreteDef = ((TBoldRegion)Any!).Definition;
            for (int i = 0; i < ConcreteDef.Subregions.Count; i++)
            {
                TBoldSubregionReference SubRegionRef = ConcreteDef.Subregions[i];
                MassiveFetch(ObjectList[0]!.BoldSystem, ObjectList, SubRegionRef.SubregionRootNavigation);
                ExpandOneLevelRegionsForNavigation(SubRegions, SubRegionRef.SubregionRootNavigation, SubRegionRef.SubregionCoreDefinition, AlreadyExpandedRegions,
                    AlreadyKnownRegions);
            }
        }
        finally
        {
            ObjectList.Dispose();
        }
    }

    /// <summary>Fetches the reverse links of these regions' parent region references and adds the parent regions not already known.</summary>
    [Delphi(PortStatus.Ported)]
    public void FetchAndExpandOneLevelParentRegions(TBoldRegionLookup ParentRegions, TBoldRegionLookup AlreadyExpandedRegions, TBoldRegionLookup AlreadyKnownRegions)
    {
        var ObjectList = new TBoldObjectList();
        try
        {
            FillObjectList(ObjectList);
            ObjectList.EnsureObjects();
            TBoldConcreteRegionDefinition ConcreteDef = ((TBoldRegion)Any!).Definition;
            for (int i = 0; i < ConcreteDef.ParentRegions.Count; i++)
            {
                TBoldSubregionReference SubRegionRef = ConcreteDef.ParentRegions[i];
                TBoldRoleRTInfo Navigation = SubRegionRef.SubregionRootNavigation;
                TBoldRoleRTInfo ReverseRole;
                switch (Navigation.RoleType)
                {
                    case TBoldRoleType.rtRole:
                        ReverseRole = (TBoldRoleRTInfo)Navigation.ClassTypeInfoOfOtherEnd!.AllMembers[Navigation.IndexOfOtherEnd];
                        break;
                    case TBoldRoleType.rtLinkRole:
                        ReverseRole = Navigation.RoleRTInfoOfOtherEnd!;
                        break;
                    case TBoldRoleType.rtInnerLinkRole:
                        var TempRole = (TBoldRoleRTInfo)Navigation.ClassTypeInfoOfOtherEnd!.AllMembers[Navigation.IndexOfOtherEnd];
                        ReverseRole = (TBoldRoleRTInfo)Navigation.ClassTypeInfoOfOtherEnd.AllMembers[TempRole.IndexOfLinkObjectRole];
                        break;
                    default:
                        throw new EBoldInternal(SysUtils.Format(sUnknownRoleType, GetType().Name, Navigation.AsString));
                }

                ObjectList[0]!.BoldSystem.FetchLinksWithObjects(ObjectList, ReverseRole.ExpressionName);
                ExpandOneLevelRegionsForNavigation(ParentRegions, ReverseRole, SubRegionRef.ParentRegion.CoreDefinition, AlreadyExpandedRegions, AlreadyKnownRegions);
            }
        }
        finally
        {
            ObjectList.Dispose();
        }
    }

    /// <summary>Indexes regions by their id string.</summary>
    [Delphi(PortStatus.Ported)]
    private sealed class TBoldRegionIndex : TBoldStringHashIndex
    {
        [Delphi(PortStatus.Ported, Symbol = "TBoldRegionIndex.ItemAsKeyString")]
        protected override string ItemAsKeyString(object? Item) => ((TBoldRegion)Item!).AsString;
    }
}

/// <summary>A region lookup that also keeps the order regions were added in.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldOrderedRegionLookup : TBoldRegionLookup
{
    private static int IX_RegionOrder = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldOrderedRegionLookup()
    {
        BoldIndexableList.SetIndexVariable(ref IX_RegionOrder, AddIndex(new TBoldIntegerIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldRegion GetFirstRegion() => (TBoldRegion)((TBoldIntegerIndex)Indexes(IX_RegionOrder)!)[0]!;

    [Delphi(PortStatus.Ported)] public TBoldRegion FirstRegion => GetFirstRegion();
}

/// <summary>A list of regions.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegionList : TList
{
    [Delphi(PortStatus.Ported)]
    private TBoldRegion GetItems(int i) => (TBoldRegion)base[i]!;

    [Delphi(PortStatus.Ported)]
    public void AddList(TBoldRegionList? List)
    {
        if (List is not null)
        {
            for (int i = 0; i < List.Count; i++)
            {
                Add(List[i]);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public void AddRegionLookup(TBoldRegionLookup Regions)
    {
        using TBoldIndexableListTraverser Traverser = Regions.CreateTraverser()!;
        while (!Traverser.EndOfList)
        {
            Add(Traverser.Item);
            Traverser.Next();
        }
    }

    [Delphi(PortStatus.Ported)]
    public void Assign(TBoldRegionList List)
    {
        Clear();
        AddList(List);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldRegionList.Items")]
    public new TBoldRegion this[int i] => GetItems(i);
}

/// <summary>Expands a set of regions to its parent regions and subregions (fetching what it needs, a level at a time).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldRegionExpander : TBoldMemoryManagedObject
{
    private readonly TBoldRegionLookup fNewParentRegions = new();
    private readonly TBoldRegionLookup fNewSubregions = new();
    private readonly TBoldOrderedRegionLookup fToBeSubExpanded = new();
    private readonly TBoldOrderedRegionLookup fToBeParentExpanded = new();
    private TBoldLockManagerProgressEvent? fOnActivityProgress;

    [Delphi(PortStatus.Ported)]
    public TBoldRegionExpander()
    {
    }

    [Delphi(PortStatus.Replaced, Symbol = "TBoldRegionExpander.Destroy", Note = "Freed the lookups; the garbage collector does that in .NET.")]
    [Delphi(PortStatus.Ported)]
    public TBoldRegionLookup NewParentRegions => fNewParentRegions;

    [Delphi(PortStatus.Ported)]
    private void SignalProgress() =>
        OnProgress?.Invoke(fNewParentRegions.Count + fNewSubregions.Count, fToBeSubExpanded.Count + fToBeParentExpanded.Count);

    [Delphi(PortStatus.Ported)]
    protected void Clear()
    {
        fNewParentRegions.Clear();
        fNewSubregions.Clear();
        fToBeSubExpanded.Clear();
        fToBeParentExpanded.Clear();
    }

    /// <summary>Moves the regions of the same definition and root class as the region from Regions to Result.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ExtractSimilarRegions(TBoldRegionLookup Regions, TBoldRegion Region, TBoldRegionLookup Result)
    {
        Result.Clear();
        var similar = new List<TBoldRegion>();
        using (TBoldIndexableListTraverser Traverser = Regions.CreateTraverser()!)
        {
            while (!Traverser.EndOfList)
            {
                var aRegion = (TBoldRegion)Traverser.Item!;
                if (aRegion.Definition == Region.Definition && aRegion.Root.BoldClassTypeInfo == Region.Root.BoldClassTypeInfo)
                {
                    similar.Add(aRegion);
                }

                Traverser.Next();
            }
        }

        foreach (TBoldRegion aRegion in similar)
        {
            Result.Add(aRegion);
            Regions.Remove(aRegion);
        }
    }

    /// <summary>Finds, level by level, the parent regions of the regions (and queues them for subregion expansion).</summary>
    [Delphi(PortStatus.Ported)]
    public void ExpandParentRegions(TBoldRegionLookup RegionsToExpand, TBoldRegionLookup KnownParentRegions, TBoldRegionLookup KnownSubregions)
    {
        if (BoldLockRegions.BoldRegionExpansionDebugLogHandler is { } log)
        {
            log.Separator();
            log.Log(sLogExpandingParentRegions);
        }

        fToBeParentExpanded.AddRegionLookup(RegionsToExpand);
        var SimilarRegions = new TBoldRegionLookup();
        var LocalNewParentRegions = new TBoldRegionLookup();
        while (fToBeParentExpanded.Count > 0)
        {
            ExtractSimilarRegions(fToBeParentExpanded, fToBeParentExpanded.FirstRegion, SimilarRegions);
            SimilarRegions.FetchAndExpandOneLevelParentRegions(LocalNewParentRegions, NewParentRegions, KnownParentRegions);
            fToBeParentExpanded.AddRegionLookup(LocalNewParentRegions);
            NewParentRegions.AddRegionLookup(LocalNewParentRegions);
            fToBeSubExpanded.AddRegionLookupWithFilter(LocalNewParentRegions, KnownSubregions);
            LocalNewParentRegions.Clear();
            SignalProgress();
        }
    }

    /// <summary>Finds, level by level, the subregions of the regions.</summary>
    [Delphi(PortStatus.Ported)]
    public void ExpandSubregions(TBoldRegionLookup RegionsToExpand, TBoldRegionLookup KnownSubregions)
    {
        if (BoldLockRegions.BoldRegionExpansionDebugLogHandler is { } log)
        {
            log.Separator();
            log.Log(sLogExpandingSubRegions);
        }

        fToBeSubExpanded.AddRegionLookup(RegionsToExpand);
        var SimilarRegions = new TBoldRegionLookup();
        var LocalNewSubregions = new TBoldRegionLookup();
        while (fToBeSubExpanded.Count > 0)
        {
            ExtractSimilarRegions(fToBeSubExpanded, fToBeSubExpanded.FirstRegion, SimilarRegions);
            SimilarRegions.FetchAndExpandOneLevelSubRegions(LocalNewSubregions, fToBeSubExpanded, NewSubRegions, KnownSubregions);
            fToBeSubExpanded.AddRegionLookup(LocalNewSubregions);
            NewSubRegions.AddRegionLookup(LocalNewSubregions);
            LocalNewSubregions.Clear();
            SignalProgress();
        }
    }

    /// <summary>Adds to the regions all their parent regions and subregions.</summary>
    [Delphi(PortStatus.Ported)]
    public void ExpandRegionEnclosure(TBoldRegionLookup Regions)
    {
        var EmptyRegionLookup = new TBoldRegionLookup();
        ExpandParentRegions(Regions, EmptyRegionLookup, EmptyRegionLookup);
        Regions.AddRegionLookup(NewParentRegions);
        ExpandSubregions(Regions, EmptyRegionLookup);
        Regions.AddRegionLookup(NewSubRegions);
    }

    [Delphi(PortStatus.Ported)] public TBoldRegionLookup NewSubRegions => fNewSubregions;

    [Delphi(PortStatus.Ported)]
    public TBoldLockManagerProgressEvent? OnProgress
    {
        get => fOnActivityProgress;
        set => fOnActivityProgress = value;
    }
}

/// <summary>Unit-level constants, variables and routines of BoldLockRegions.</summary>
public static partial class BoldLockRegions
{
    [Delphi(PortStatus.Ported)] public const int breNone = 0;
    [Delphi(PortStatus.Ported)] public const int breParentsChanged = 1;
    [Delphi(PortStatus.Ported)] public const int breSubregionsChanged = 2;

    /// <summary>When set, region expansion logs what it adds.</summary>
    [Delphi(PortStatus.Ported)] public static TBoldLogHandler? BoldRegionExpansionDebugLogHandler;

    /// <summary>The objects the role of the object reaches, as a list (nil when the object's class has no such role).</summary>
    [Delphi(PortStatus.Ported)]
    internal static void NavigateAndSubscribe(TBoldObject Obj, TBoldRoleRTInfo RoleRT, TBoldIndirectElement ResultElement, TBoldSubscriber? Subscriber, int RequestedEvent)
    {
        if (Obj.BoldClassTypeInfo.BoldIsA(RoleRT.ClassTypeInfo))
        {
            TBoldMember aMember = Obj.BoldMembers(RoleRT.index);
            if (aMember is not (TBoldObjectList or TBoldObjectReference))
            {
                throw new EBoldInternal(SysUtils.Format(sTriedToNavigateNonAssociation, Obj.BoldClassTypeInfo.ExpressionName, aMember.BoldMemberRTInfo!.ExpressionName));
            }

            aMember.GetAsList(ResultElement);
            if (Subscriber is not null)
            {
                aMember.DefaultSubscribe(Subscriber, RequestedEvent);
            }
        }
        else
        {
            ResultElement.SetOwnedValue(null);
        }
    }

    [Delphi(PortStatus.Ported)]
    internal static void DoReverseNavigationAndSubscribe(TBoldObject Obj, TBoldRoleRTInfo Navigation, TBoldIndirectElement ResultElement, TBoldSubscriber? Subscriber,
        int RequestedEvent) =>
        NavigateAndSubscribe(Obj, (TBoldRoleRTInfo)Navigation.ClassTypeInfoOfOtherEnd!.AllMembers[Navigation.IndexOfOtherEnd], ResultElement, Subscriber, RequestedEvent);

    [Delphi(PortStatus.Ported)]
    internal static void DoNavigationAndSubscribe(TBoldObject Obj, TBoldRoleRTInfo Navigation, TBoldIndirectElement ResultElement, TBoldSubscriber? Subscriber,
        int RequestedEvent) =>
        NavigateAndSubscribe(Obj, Navigation, ResultElement, Subscriber, RequestedEvent);
}
