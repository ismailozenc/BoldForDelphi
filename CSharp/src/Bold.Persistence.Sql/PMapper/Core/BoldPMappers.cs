// Delphi: Source/PMapper/Core/BoldPMappers.pas
// Port map: CSharp/port/units/BoldPMappers.md
// The persistence mapper hierarchy: a system mapper owns an object mapper per persistent class (nil for others),
// and each object mapper owns member mappers for the class's persistent members. Mappers are made from the model
// through registered descriptors, by mapper name.

using System.Reflection;
using Bold;

[assembly: Delphi(PortStatus.Ported, Unit = "BoldPMappers", Symbol = "TBoldSystemPersistenceMapperClass", Note = "A System.Type of the mapper class.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldPMappers", Symbol = "TBoldObjectPersistenceMapperClass", Note = "A System.Type of the mapper class.")]
[assembly: Delphi(PortStatus.Ported, Unit = "BoldPMappers", Symbol = "TBoldMemberPersistenceMapperClass", Note = "A System.Type of the mapper class.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappers", Symbol = "TBoldSystemPersistenceMapper.Destroy", Note = "Freed the object mappers and the storage description; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappers", Symbol = "TBoldObjectPersistenceMapper.Destroy", Note = "Freed the member mappers; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Called to adjust the parameters before the storage is created.</summary>
[Delphi(PortStatus.Ported)]
public delegate void TBoldPreparePSParams(TBoldPSParams PSParams);

/// <summary>
/// Maps a whole system: dispatches fetches and updates to the class mappers, reserves ids for new objects, and runs
/// updates in transactions with timestamps and preconditions.
/// </summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldSystemPersistenceMapper : TBoldPersistenceMapper
{
    private readonly TBoldObjectPersistenceMapper? fRootClassObjectPMapper;
    private TBoldPSSystemDescription? fPSSystemDescription;
    private readonly TBoldObjectPersistenceMapperList fObjectPersistenceMappers;
    private TBoldPreparePSParams? fPreparePSParams;
    private readonly bool fUseXFiles;
    private readonly bool fUseTimeStamp;
    private readonly bool fUseGlobalId;
    private readonly bool fUseReadOnly;
    private readonly bool fUseModelVersion;
    private readonly int fModelVersion;
    private readonly bool fUseClockLog;
    private TBoldGetTimeEvent? fOnGetCurrentTime;
    private TDateTime fClockLogGranularity;
    private readonly bool fUpdateWholeObjects;
    private TBoldAbstractObjectUpgrader? fObjectUpgrader;

    protected TBoldTimeStampType FCurrentTimeStamp;

    /// <summary>Makes an object mapper (from its registered descriptor) for each persistent, internally stored class.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldSystemPersistenceMapper.CreateFromMold")]
    protected TBoldSystemPersistenceMapper(TMoldModel MoldModel, TBoldTypeNameDictionary TypeNameDictionary)
    {
        MoldModel.EnsureLinkRoles();
        MoldModel.EnsureTopSorted();
        fUseXFiles = MoldModel.UseXFiles;
        fUseTimeStamp = MoldModel.UseTimestamp;
        fUseGlobalId = MoldModel.UseGlobalId;
        fUseReadOnly = MoldModel.UseReadOnly;
        fUseModelVersion = MoldModel.UseModelVersion;
        fModelVersion = MoldModel.ModelVersion;
        fUseClockLog = MoldModel.UseClockLog;
        fUpdateWholeObjects = MoldModel.UpdateWholeObjects;
        fObjectPersistenceMappers = new TBoldObjectPersistenceMapperList();
        for (int i = 0; i < MoldModel.Classes.Count; i++)
        {
            TBoldObjectPersistenceMapper? ObjectPMapper = null;
            TMoldClass MoldClass = MoldModel.Classes[i];
            if (MoldClass.EffectivePersistent && MoldClass.Storage is TBoldStorage.bsInternal or TBoldStorage.bsPartiallyExternal)
            {
                TBoldObjectPersistenceMapperDescriptor ObjectMapperDescriptor =
                    BoldPMapperLists.BoldObjectPersistenceMappers().DescriptorByName(MoldClass.PMapperName)
                    ?? throw new EBold(SysUtils.Format(BoldPMConsts.sUnableToFindPMapperForClass, MoldClass.PMapperName, MoldClass.Name));
                ObjectPMapper = (TBoldObjectPersistenceMapper)Activator.CreateInstance(ObjectMapperDescriptor.ObjectPersistenceMapper, MoldClass, this, TypeNameDictionary)!;
            }

            ObjectPersistenceMappers.Add(ObjectPMapper);
        }

        fRootClassObjectPMapper = ObjectPersistenceMappers[0];
    }

    [Delphi(PortStatus.Ported)]
    private TBoldPSSystemDescription GetPSSystemDescription()
    {
        EnsurePSDescription();
        return fPSSystemDescription!;
    }

    /// <summary>Runs Action for the ids, grouped by the mapper of their class.</summary>
    private void ForEachMapperGroup(TBoldObjectIdList ObjectIDList, Action<TBoldObjectPersistenceMapper, TBoldObjectIdList> Action)
    {
        TBoldObjectIdList TempList = ObjectIDList.Clone();
        while (TempList.Count > 0)
        {
            var ActionList = new TBoldObjectIdList();
            TBoldObjectPersistenceMapper CurrentPMapper = ObjectPersistenceMappers[TempList[0]!.TopSortedIndex]!;
            int i = 0;
            while (i < TempList.Count)
            {
                if (ObjectPersistenceMappers[TempList[i]!.TopSortedIndex] == CurrentPMapper)
                {
                    ActionList.Add(TempList[i]);
                    TempList.RemoveByIndex(i);
                }
                else
                {
                    i++;
                }
            }

            Action(CurrentPMapper, ActionList);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void fPMCreate(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) =>
        ForEachMapperGroup(ObjectIDList, (mapper, list) => mapper.PMCreate(list, ValueSpace, TranslationList));

    [Delphi(PortStatus.Ported)]
    private void fPMDelete(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldIDTranslationList TranslationList) =>
        ForEachMapperGroup(ObjectIDList, (mapper, list) => mapper.PMDelete(list, ValueSpace, Old_Values, TranslationList));

    [Delphi(PortStatus.Ported)]
    private void fPMUpdate(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldIDTranslationList TranslationLIst) =>
        ForEachMapperGroup(ObjectIDList, (mapper, list) => mapper.PMUpdate(list, ValueSpace, Old_Values, TranslationLIst));

    [Delphi(PortStatus.Ported)]
    private bool GetSupportsObjectUpgrading() => UseModelVersion && ObjectUpgrader is not null;

    [Delphi(PortStatus.Ported)]
    protected virtual void InitializePSDescriptions()
    {
        for (int i = 0; i < ObjectPersistenceMappers.Count; i++)
        {
            ObjectPersistenceMappers[i]?.InitializePSDescriptions();
        }
    }

    [Delphi(PortStatus.Ported)] protected abstract void FillPSParams(TBoldPSParams PSParams);
    [Delphi(PortStatus.Ported)] protected abstract TBoldPSParams CreatePSParams();
    [Delphi(PortStatus.Ported)] protected abstract TBoldPSSystemDescription CreatePSSystemDescription();
    [Delphi(PortStatus.Ported)] protected abstract void ReserveID();

    [Delphi(PortStatus.Ported)]
    protected TBoldObjectId NextExternalObjectId(IBoldValueSpace ValueSpace, TBoldObjectId ObjectID) =>
        ObjectPersistenceMappers[ObjectID.TopSortedIndex]!.NextExternalObjectId(ValueSpace, ObjectID);

    [Delphi(PortStatus.Ported)] protected abstract void StartTransaction(IBoldValueSpace ValueSpace);
    [Delphi(PortStatus.Ported)] protected abstract void Commit(IBoldValueSpace ValueSpace);
    [Delphi(PortStatus.Ported)] protected abstract void RollBack(IBoldValueSpace ValueSpace);
    [Delphi(PortStatus.Ported)] protected abstract void GetNewTimeStamp();
    [Delphi(PortStatus.Ported)] protected abstract bool EnsurePrecondition(TBoldUpdatePrecondition Precondition, TBoldIDTranslationList TranslationList);
    [Delphi(PortStatus.Ported)] protected internal abstract void FetchDeletedObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValieSpace);

    /// <summary>Makes the storage description on first use (discarding it when initializing fails).</summary>
    [Delphi(PortStatus.Ported)]
    protected void EnsurePSDescription()
    {
        if (fPSSystemDescription is null)
        {
            fPSSystemDescription = CreatePSSystemDescription();
            try
            {
                InitializePSDescriptions();
            }
            catch
            {
                fPSSystemDescription = null;
                throw;
            }
        }
    }

    /// <summary>Fetches the objects through the root class mapper (which groups them by class).</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void PMFetch(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldIDTranslationList TranslationList) =>
        RootClassObjectPersistenceMapper!.PMFetchApproximateIDList(ObjectIDList, ValueSpace, MemberIdList, FetchMode, TranslationList);

    /// <summary>Fetches the ids (and values) of a class matching a condition with a class.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void PMFetchClassWithCondition(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldCondition BoldCondition, int FetchMode,
        TBoldIDTranslationList TranslationList)
    {
        if (BoldCondition is TBoldConditionWithClass conditionWithClass)
        {
            ObjectPersistenceMappers[conditionWithClass.TopSortedIndex]!.PMFetchWithCondition(ObjectIDList, ValueSpace, BoldCondition, FetchMode, TranslationList);
        }
        else
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sConditionMustBeConditionWithClass, GetType().Name, BoldCondition.GetType().Name));
        }
    }

    /// <summary>
    /// Saves the objects: reserves ids for new ones, takes a new timestamp in its own transaction, then (unless the
    /// precondition fails) deletes, creates and updates in one transaction. Deletes come first, so link objects
    /// re-created in the same update do not collide.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void PMUpdate(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldUpdatePrecondition? Precondition,
        TBoldIDTranslationList TranslationList, ref TBoldTimeStampType TimeStamp)
    {
        if (ObjectIDList.Count == 0)
        {
            return;
        }

        ReserveNewIds(ValueSpace, ObjectIDList, TranslationList);
        StartTransaction(ValueSpace);
        try
        {
            if (UseTimestamp)
            {
                GetNewTimeStamp();
            }
        }
        catch
        {
            RollBack(ValueSpace);
            throw;
        }

        Commit(ValueSpace);
        StartTransaction(ValueSpace);
        try
        {
            if (Precondition is not null && !EnsurePrecondition(Precondition, TranslationList))
            {
                RollBack(ValueSpace);
            }
            else
            {
                var DeletedObjectIDList = new TBoldObjectIdList();
                var NewObjectIDList = new TBoldObjectIdList();
                var ModifiedObjectIDList = new TBoldObjectIdList();
                for (int i = ObjectIDList.Count - 1; i >= 0; i--)
                {
                    IBoldObjectContents ObjectContents = ValueSpace.ObjectContentsByObjectId(ObjectIDList[i]!)!;
                    if (ObjectContents.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified)
                    {
                        if (ObjectContents.BoldExistenceState == TBoldExistenceState.besDeleted)
                        {
                            DeletedObjectIDList.Add(ObjectIDList[i]);
                        }
                        else
                        {
                            NewObjectIDList.Add(ObjectIDList[i]);
                        }
                    }
                    else
                    {
                        ModifiedObjectIDList.Add(ObjectIDList[i]);
                    }
                }

                if (DeletedObjectIDList.Count > 0)
                {
                    fPMDelete(DeletedObjectIDList, ValueSpace, Old_Values, TranslationList);
                }

                if (NewObjectIDList.Count > 0)
                {
                    fPMCreate(NewObjectIDList, ValueSpace, TranslationList);
                }

                if (ModifiedObjectIDList.Count > 0)
                {
                    fPMUpdate(ModifiedObjectIDList, ValueSpace, Old_Values, TranslationList);
                }

                if (UseTimestamp)
                {
                    TimeStamp = CurrentTimeStamp;
                }

                Commit(ValueSpace);
            }
        }
        catch
        {
            RollBack(ValueSpace);
            throw;
        }
    }

    /// <summary>The mapper of the least common superclass of the objects' classes; raises for an empty list.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObjectPersistenceMapper CommonSuperClassObjectMapper(TBoldObjectIdList ObjectIdList)
    {
        if (ObjectIdList.Count == 0)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sObjectIDListIsEmpty, GetType().Name));
        }

        TBoldObjectPersistenceMapper result = ObjectPersistenceMappers[ObjectIdList[0]!.TopSortedIndex]!;
        for (int i = 1; i < ObjectIdList.Count; i++)
        {
            result = result.LeastCommonSuperClassMapper(ObjectPersistenceMappers[ObjectIdList[i]!.TopSortedIndex]!);
        }

        return result;
    }

    /// <summary>Creates the storage from the description (with prepared parameters), then lets each class mapper add to it.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void CreatePersistentStorage()
    {
        TBoldPSParams PSParams = CreatePSParams();
        FillPSParams(PSParams);
        OnPreparePSParams?.Invoke(PSParams);
        PSSystemDescription.CreatePersistentStorage(PSParams);
        for (int i = 0; i < ObjectPersistenceMappers.Count; i++)
        {
            ObjectPersistenceMappers[i]?.CreatePersistentStorage();
        }
    }

    /// <summary>The time from OnGetCurrentTime, or now.</summary>
    [Delphi(PortStatus.Ported)]
    public TDateTime GetCorrectTime() => OnGetCurrentTime is not null ? OnGetCurrentTime() : SysUtils.Now();

    [Delphi(PortStatus.Ported)]
    public void SubscribeToPersistenceEvents(TBoldSubscriber Subscriber)
    {
        AddSubscription(Subscriber, BoldSubscription.bpeFetchObject, BoldSubscription.bpeFetchObject);
        AddSubscription(Subscriber, BoldSubscription.bpeFetchMember, BoldSubscription.bpeFetchMember);
        AddSubscription(Subscriber, BoldSubscription.bpeUpdateObject, BoldSubscription.bpeUpdateObject);
        AddSubscription(Subscriber, BoldSubscription.bpeCreateObject, BoldSubscription.bpeCreateObject);
        AddSubscription(Subscriber, BoldSubscription.bpeDeleteObject, BoldSubscription.bpeDeleteObject);
        AddSubscription(Subscriber, BoldSubscription.bpeFetchId, BoldSubscription.bpeFetchId);
    }

    /// <summary>Reserves an id per new (non-storable) object, then gives each a new external id in the translation list.</summary>
    [Delphi(PortStatus.Ported)]
    public void ReserveNewIds(IBoldValueSpace ValueSpace, TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
        if (ObjectIdList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            if (!ObjectIdList[i]!.IsStorable)
            {
                ReserveID();
            }
        }

        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            if (!ObjectIdList[i]!.IsStorable)
            {
                TBoldObjectId NewId = NextExternalObjectId(ValueSpace, ObjectIdList[i]!);
                TranslationList.AddTranslation(ObjectIdList[i], NewId);
            }
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectPersistenceMapperList ObjectPersistenceMappers => fObjectPersistenceMappers;
    [Delphi(PortStatus.Ported)] public TBoldPSSystemDescription PSSystemDescription => GetPSSystemDescription();
    [Delphi(PortStatus.Ported)] public TBoldObjectPersistenceMapper? RootClassObjectPersistenceMapper => fRootClassObjectPMapper;
    [Delphi(PortStatus.Ported)] public TBoldPreparePSParams? OnPreparePSParams { get => fPreparePSParams; set => fPreparePSParams = value; }
    [Delphi(PortStatus.Ported)] public TBoldTimeStampType CurrentTimeStamp => FCurrentTimeStamp;
    [Delphi(PortStatus.Ported)] public abstract void PMTranslateToGlobalIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList);
    [Delphi(PortStatus.Ported)] public abstract void PMTranslateToLocalIds(TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList);
    [Delphi(PortStatus.Ported)] public abstract void PMSetReadonlyness(TBoldObjectIdList ReadOnlyList, TBoldObjectIdList WriteableList);
    [Delphi(PortStatus.Ported)] public abstract void PMTimestampForTime(TDateTime ClockTime, ref TBoldTimeStampType Timestamp);
    [Delphi(PortStatus.Ported)] public abstract void PMTimeForTimestamp(TBoldTimeStampType Timestamp, ref TDateTime ClockTime);
    [Delphi(PortStatus.Ported)] public bool UseXFiles => fUseXFiles;
    [Delphi(PortStatus.Ported)] public bool UseTimestamp => fUseTimeStamp;
    [Delphi(PortStatus.Ported)] public bool UseGlobalId => fUseGlobalId;
    [Delphi(PortStatus.Ported)] public bool UseReadOnly => fUseReadOnly;
    [Delphi(PortStatus.Ported)] public bool UseModelVersion => fUseModelVersion;
    [Delphi(PortStatus.Ported)] public bool SupportsObjectUpgrading => GetSupportsObjectUpgrading();
    [Delphi(PortStatus.Ported)] public int ModelVersion => fModelVersion;
    [Delphi(PortStatus.Ported)] public bool UseClockLog => fUseClockLog;
    [Delphi(PortStatus.Ported)] public TBoldGetTimeEvent? OnGetCurrentTime { get => fOnGetCurrentTime; set => fOnGetCurrentTime = value; }
    [Delphi(PortStatus.Ported)] public TDateTime ClockLogGranularity { get => fClockLogGranularity; set => fClockLogGranularity = value; }
    [Delphi(PortStatus.Ported)] public bool UpdateWholeObjects => fUpdateWholeObjects;
    [Delphi(PortStatus.Ported)] public TBoldAbstractObjectUpgrader? ObjectUpgrader { get => fObjectUpgrader; set => fObjectUpgrader = value; }
}

/// <summary>Maps one class: its member mappers (by member index), its superclass mapper and its link roles.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldObjectPersistenceMapper : TBoldPersistenceMapper
{
    private readonly TBoldMemberPersistenceMapperList fMemberPersistenceMappers;
    private readonly int fTopSortedIndex;
    private readonly TBoldSystemPersistenceMapper fSystemPersistenceMapper;
    private readonly TBoldObjectPersistenceMapper? fSuperClass;
    private readonly string fExpressionName;
    private readonly bool fIsLinkClass;
    private bool fHasSubclasses;
    private readonly bool fVersioned;
    private readonly TBoldStorage fStorage;
    private int fLinkRoleMapperIndex1;
    private int fLinkRoleMapperIndex2;
    private int[] fMemberMapperIndexByMemberIndex = [];

    protected string fObjectIdClass = "";

    /// <summary>The mapper of a class; raises when the class is versioned but its superclass is not, or the superclass is not persistent.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectPersistenceMapper.CreateFromMold")]
    protected TBoldObjectPersistenceMapper(TMoldClass MoldClass, TBoldSystemPersistenceMapper Owner, TBoldTypeNameDictionary TypeNameDictionary)
    {
        fIsLinkClass = MoldClass.Association is not null;
        fMemberPersistenceMappers = new TBoldMemberPersistenceMapperList();
        fSystemPersistenceMapper = Owner;
        fStorage = MoldClass.Storage;
        fExpressionName = MoldClass.ExpandedExpressionName;
        fTopSortedIndex = MoldClass.TopSortedIndex;
        fLinkRoleMapperIndex1 = -1;
        fLinkRoleMapperIndex2 = -1;
        fVersioned = MoldClass.Versioned;
        if (MoldClass.Versioned && MoldClass.SuperClass is not null && !MoldClass.SuperClass.Versioned)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sSuperClassNotVersioned, MoldClass.ExpandedExpressionName, MoldClass.SuperClass.ExpandedExpressionName));
        }

        if (MoldClass.SuperClass is not null)
        {
            fSuperClass = SystemPersistenceMapper.ObjectPersistenceMappers[MoldClass.SuperClass.TopSortedIndex]
                ?? throw new EBold(SysUtils.Format(BoldPMConsts.sSuperClassNotPersistent, MoldClass.ExpandedExpressionName, MoldClass.SuperClass.ExpandedExpressionName));
            fSuperClass.fHasSubclasses = true;
        }

        FillInMembers(MoldClass, MoldClass, TypeNameDictionary);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldMemberPersistenceMapper? GetLinkClassRole1() => IsLinkClass ? MemberPersistenceMappers[fLinkRoleMapperIndex1] : null;

    [Delphi(PortStatus.Ported)]
    private TBoldMemberPersistenceMapper? GetLinkClassRole2() => IsLinkClass ? MemberPersistenceMappers[fLinkRoleMapperIndex2] : null;

    private static TBoldMemberPersistenceMapperDescriptor? ByName(string Name) => BoldPMapperLists.BoldMemberPersistenceMappers().DescriptorByDelphiName(Name);

    /// <summary>
    /// Makes a member mapper per persistent member of the class (all members, inherited too): attributes by their type's
    /// mapper, roles by the default link mappers (or the named mapper), link roles and inner link roles of link classes.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    private void FillInMembers(TMoldClass MyMoldClass, TMoldClass CurrentMoldClass, TBoldTypeNameDictionary TypeNameDictionary)
    {
        fMemberMapperIndexByMemberIndex = new int[MyMoldClass.AllBoldMembers.Count];
        for (int i = 0; i < MyMoldClass.AllBoldMembers.Count; i++)
        {
            var MoldMember = (TMoldMember)MyMoldClass.AllBoldMembers[i];
            TBoldMemberPersistenceMapperDescriptor? MemberDescriptor = null;
            if (MoldMember.EffectivePersistent && MoldMember is TMoldAttribute MoldAttribute)
            {
                if (fStorage == TBoldStorage.bsInternal || MoldAttribute.Storage is TBoldStorage.bsInternal or TBoldStorage.bsExternalKey)
                {
                    MemberDescriptor = BoldPMapperLists.BoldMemberPersistenceMappers().DescriptorForModelNameWithDefaultSupport(
                        MoldAttribute.BoldType, MoldAttribute.PMapperName, TypeNameDictionary);
                    if (MemberDescriptor is null)
                    {
                        string MapperName = MoldAttribute.PMapperName;
                        if (SysUtils.SameText(MapperName, BoldDefs.DEFAULTNAME) && TypeNameDictionary.MappingForModelName(MoldAttribute.BoldType) is { } Mapping)
                        {
                            MapperName = Mapping.ExpandedMapperName;
                        }

                        throw new EBold(SysUtils.Format(BoldPMConsts.sUnableToFindPMapperForX + BoldDefs.BOLDCRLF + BoldPMConsts.sUnableReason1 + BoldDefs.BOLDCRLF +
                            BoldPMConsts.sUnableReason2 + BoldDefs.BOLDCRLF + BoldPMConsts.sUnableReason3,
                            MoldAttribute.MoldClass!.ExpandedExpressionName, MoldAttribute.ExpandedExpressionName, MoldAttribute.BoldType, MapperName));
                    }
                }
            }
            else if (MoldMember is TMoldRole MoldRole)
            {
                if ((fStorage == TBoldStorage.bsInternal || MoldRole.Association!.Storage == TBoldStorage.bsInternal) && !MoldRole.Derived)
                {
                    switch (MoldRole.RoleType)
                    {
                        case TBoldRoleType.rtRole:
                            if (MoldRole.EffectivePersistent)
                            {
                                if (MoldRole.PMapperName.Length == 0 || SysUtils.SameText(MoldRole.PMapperName, BoldDefs.DEFAULTNAME))
                                {
                                    bool IsIndirect = MoldRole.Association!.LinkClass is not null;
                                    MemberDescriptor = MoldRole.Multi
                                        ? ByName(IsIndirect ? "TBoldIndirectMultiLinkDefaultMapper" : "TBoldDirectMultiLinkDefaultMapper")
                                        : MoldRole.EffectiveEmbedded
                                            ? ByName("TBoldEmbeddedSingleLinkDefaultMapper")
                                            : ByName(IsIndirect ? "TBoldIndirectSingleLinkDefaultMapper" : "TBoldDirectSingleLinkDefaultMapper");
                                }
                                else
                                {
                                    MemberDescriptor = ByName(MoldRole.PMapperName) ?? throw new EBold(SysUtils.Format(BoldPMConsts.sUnableToFindPMapperForXY,
                                        MoldRole.MoldClass!.ExpandedExpressionName, MoldRole.ExpandedExpressionName, MoldRole.PMapperName));
                                }
                            }

                            break;
                        case TBoldRoleType.rtLinkRole:
                            if (MoldRole.MainRole!.EffectivePersistent)
                            {
                                MemberDescriptor = ByName(MoldRole.MainRole.Multi ? "TBoldDirectMultiLinkDefaultMapper" : "TBoldDirectSingleLinkDefaultMapper");
                            }

                            break;
                        case TBoldRoleType.rtInnerLinkRole:
                            if (MoldRole.EffectivePersistent && CurrentMoldClass.EffectivePersistent)
                            {
                                MemberDescriptor = ByName("TBoldEmbeddedSingleLinkDefaultMapper");
                            }

                            if (fLinkRoleMapperIndex1 == -1)
                            {
                                fLinkRoleMapperIndex1 = MemberPersistenceMappers.Count;
                            }
                            else
                            {
                                fLinkRoleMapperIndex2 = MemberPersistenceMappers.Count;
                            }

                            break;
                    }
                }
            }

            if (MemberDescriptor is not null)
            {
                fMemberMapperIndexByMemberIndex[i] = MemberPersistenceMappers.Count;
                MemberPersistenceMappers.Add((TBoldMemberPersistenceMapper)Activator.CreateInstance(MemberDescriptor.MemberPersistenceMapperClass,
                    MoldMember, MyMoldClass, this, i, TypeNameDictionary)!);
            }
            else
            {
                fMemberMapperIndexByMemberIndex[i] = -1;
            }
        }
    }

    /// <summary>The nearest of the mapper and its superclasses that this mapper is.</summary>
    [Delphi(PortStatus.Ported)]
    internal TBoldObjectPersistenceMapper LeastCommonSuperClassMapper(TBoldObjectPersistenceMapper ObjectMapper)
    {
        TBoldObjectPersistenceMapper result = ObjectMapper;
        while (!BoldIsA(result))
        {
            result = result.SuperClass!;
        }

        return result;
    }

    /// <summary>The member mapper index of a member index; the technical members (negative indexes) are searched.</summary>
    [Delphi(PortStatus.Ported)]
    private int GetMemberMapperIndexByMemberIndex(int MemberIndex)
    {
        if (MemberIndex >= 0 && MemberIndex < fMemberMapperIndexByMemberIndex.Length)
        {
            return fMemberMapperIndexByMemberIndex[MemberIndex];
        }

        if (MemberIndex < 0)
        {
            for (int i = 0; i < MemberPersistenceMappers.Count; i++)
            {
                if (MemberPersistenceMappers[i].MemberIndex == MemberIndex)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    [Delphi(PortStatus.Ported)]
    protected internal abstract void PMFetchWithCondition(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldCondition BoldCondition, int FetchMode,
        TBoldIDTranslationList TranslationList);

    [Delphi(PortStatus.Ported)]
    protected internal abstract TBoldObjectId NextExternalObjectId(IBoldValueSpace ValueSpace, TBoldObjectId ObjectID);

    [Delphi(PortStatus.Ported)]
    protected internal virtual void InitializePSDescriptions()
    {
        for (int i = 0; i < MemberPersistenceMappers.Count; i++)
        {
            MemberPersistenceMappers[i]?.InitializePSDescriptions();
        }
    }

    [Delphi(PortStatus.Ported)] public abstract void MakeIDsExact(TBoldObjectIdList ObjectIDList, TBoldIDTranslationList TranslationList);

    /// <summary>Makes the ids whose class is not known exactly exact.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void EnsureIDsExact(TBoldObjectIdList ObjectIDList, TBoldIDTranslationList TranslationList)
    {
        var InexactIDList = new TBoldObjectIdList();
        for (int i = 0; i < ObjectIDList.Count; i++)
        {
            if (!ObjectIDList[i]!.TopSortedIndexExact)
            {
                InexactIDList.Add(ObjectIDList[i]);
            }
        }

        if (InexactIDList.Count > 0)
        {
            MakeIDsExact(InexactIDList, TranslationList);
        }
    }

    [Delphi(PortStatus.Ported)]
    public abstract void PMFetchExactIDList(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldIDTranslationList TranslationList, TBoldObjectIdList MissingList);

    /// <summary>
    /// Fetches ids of any classes: makes them exact, then fetches per class mapper; a single member asked for objects of
    /// several classes is fetched once through their common mapper when its member mapper supports polymorphic fetch.
    /// Objects found missing in a distributable fetch are fetched as deleted.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public virtual void PMFetchApproximateIDList(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldIDTranslationList TranslationList)
    {
        TBoldObjectPersistenceMapper ObjectMapperForNewID(TBoldObjectId OldID)
        {
            TBoldObjectId NewId = OldID.TopSortedIndexExact ? OldID : TranslationList.TranslateToNewId(OldID)!;
            return SystemPersistenceMapper.ObjectPersistenceMappers[NewId.TopSortedIndex]!;
        }

        EnsureIDsExact(ObjectIDList, TranslationList);
        TBoldObjectIdList TempList = ObjectIDList.Clone();
        var MissingList = new TBoldObjectIdList();
        while (TempList.Count > 0)
        {
            var ActionList = new TBoldObjectIdList();
            TBoldObjectPersistenceMapper CurrentPMapper = ObjectMapperForNewID(TempList[0]!);
            if (TempList.Count > 1 && MemberIdList is { Count: 1 })
            {
                for (int i = 1; i < TempList.Count; i++)
                {
                    CurrentPMapper = CurrentPMapper.LeastCommonSuperMapper(ObjectMapperForNewID(TempList[i]!));
                }

                int MemberMapperIndex = CurrentPMapper.MemberMapperIndexByMemberIndex(MemberIdList[0]!.MemberIndex);
                if (MemberMapperIndex != -1 && CurrentPMapper.MemberPersistenceMappers[MemberMapperIndex].SupportsPolymorphicFetch)
                {
                    ActionList.AddList(TempList);
                    TempList.Clear();
                }

                if (ActionList.Count == 0)
                {
                    CurrentPMapper = ObjectMapperForNewID(TempList[0]!);
                }
            }

            int j = 0;
            while (j < TempList.Count)
            {
                if (ObjectMapperForNewID(TempList[j]!) == CurrentPMapper)
                {
                    ActionList.Add(TempList[j]);
                    TempList.RemoveByIndex(j);
                }
                else
                {
                    j++;
                }
            }

            CurrentPMapper.PMFetchExactIDList(ActionList, ValueSpace, MemberIdList, FetchMode, TranslationList, MissingList);
        }

        if (FetchMode == BoldDefs.fmDistributable && MissingList.Count > 0)
        {
            SystemPersistenceMapper.FetchDeletedObjects(MissingList, ValueSpace);
        }
    }

    [Delphi(PortStatus.Ported)] public abstract void PMCreate(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList);
    [Delphi(PortStatus.Ported)] public abstract void PMDelete(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldIDTranslationList TranslationList);
    [Delphi(PortStatus.Ported)] public abstract void PMUpdate(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, IBoldValueSpace? Old_Values, TBoldIDTranslationList TranslationLIst);

    [Delphi(PortStatus.Ported)]
    public bool BoldIsA(TBoldObjectPersistenceMapper ObjectPMapper) =>
        this == ObjectPMapper || (fSuperClass is not null && fSuperClass.BoldIsA(ObjectPMapper));

    [Delphi(PortStatus.Ported)]
    public TBoldObjectPersistenceMapper LeastCommonSuperMapper(TBoldObjectPersistenceMapper ObjectPMapper)
    {
        if (BoldIsA(ObjectPMapper))
        {
            return ObjectPMapper;
        }

        return ObjectPMapper.BoldIsA(this) ? this : SuperClass!.LeastCommonSuperClassMapper(ObjectPMapper);
    }

    /// <summary>
    /// The member mappers to fetch: the members asked for, or (without a list) all; a distributable fetch takes only those
    /// stored in the object, a normal one skips delayed members unless asked. Custom-fetch mappers go to their own list.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void BuildMemberFetchLists(TBoldMemberIdList? MemberIdList, TBoldMemberPersistenceMapperList? DefaultFetchMemberList,
        TBoldMemberPersistenceMapperList? CustomFetchMemberList, int FetchMode)
    {
        void ActuallyAdd(TBoldMemberPersistenceMapper MemberMapper)
        {
            if (MemberMapper.CustomFetch)
            {
                CustomFetchMemberList?.Add(MemberMapper);
            }
            else
            {
                DefaultFetchMemberList?.Add(MemberMapper);
            }
        }

        void TestAdd(TBoldMemberPersistenceMapper? MemberMapper)
        {
            if (MemberMapper is null)
            {
                return;
            }

            switch (FetchMode)
            {
                case BoldDefs.fmDistributable:
                    if (MemberMapper.IsStoredInObject)
                    {
                        ActuallyAdd(MemberMapper);
                    }

                    break;
                case BoldDefs.fmNormal:
                case BoldDefs.fmCompare:
                    if (MemberIdList is not null || !MemberMapper.DelayedFetch)
                    {
                        ActuallyAdd(MemberMapper);
                    }

                    break;
            }
        }

        if (MemberIdList is not null)
        {
            for (int i = 0; i < MemberIdList.Count; i++)
            {
                int index = MemberMapperIndexByMemberIndex(MemberIdList[i]!.MemberIndex);
                TestAdd(index >= 0 ? MemberPersistenceMappers[index] : null);
            }
        }
        else
        {
            for (int i = 0; i < MemberPersistenceMappers.Count; i++)
            {
                TestAdd(MemberPersistenceMappers[i]);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal override void CreatePersistentStorage()
    {
        for (int i = 0; i < MemberPersistenceMappers.Count; i++)
        {
            MemberPersistenceMappers[i]?.CreatePersistentStorage();
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldObjectPersistenceMapper? SuperClass => fSuperClass;
    [Delphi(PortStatus.Ported)] public int TopSortedIndex => fTopSortedIndex;
    [Delphi(PortStatus.Ported)] public string ExpressionName => fExpressionName;
    [Delphi(PortStatus.Ported)] public TBoldMemberPersistenceMapperList MemberPersistenceMappers => fMemberPersistenceMappers;
    [Delphi(PortStatus.Ported)] public TBoldSystemPersistenceMapper SystemPersistenceMapper => fSystemPersistenceMapper;
    [Delphi(PortStatus.Ported)] public string ObjectIdClass => fObjectIdClass;
    [Delphi(PortStatus.Ported)] public bool HasSubClasses => fHasSubclasses;
    [Delphi(PortStatus.Ported)] public bool IsLinkClass => fIsLinkClass;
    [Delphi(PortStatus.Ported)] public TBoldMemberPersistenceMapper? LinkClassRole1 => GetLinkClassRole1();
    [Delphi(PortStatus.Ported)] public TBoldMemberPersistenceMapper? LinkClassRole2 => GetLinkClassRole2();
    [Delphi(PortStatus.Ported)] public bool Versioned => fVersioned;

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public int MemberMapperIndexByMemberIndex(int MemberIndex) => GetMemberMapperIndexByMemberIndex(MemberIndex);
}

/// <summary>Maps one member of a class: what content it stores, whether it is stored in the object row, and how it is fetched.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberPersistenceMapper : TBoldPersistenceMapper
{
    private readonly TBoldObjectPersistenceMapper fObjectPersistenceMapper;
    private readonly int fMemberIndex;

    protected bool fDelayedFetch;
    protected string fContentName = "";
    protected bool fIsStoredInObject;
    protected bool fCustomFetch;
    protected bool fCustomCreateUpDate;
    protected string fExpressionname = "";

    /// <summary>The mapper of a member (or of a technical member, without a model member); raises on duplicate names in the class.</summary>
    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberPersistenceMapper.CreateFromMold")]
    public TBoldMemberPersistenceMapper(TMoldMember? MoldMember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex,
        TBoldTypeNameDictionary TypeNameDictionary)
    {
        fObjectPersistenceMapper = Owner;
        fCustomFetch = false;
        fCustomCreateUpDate = false;
        fMemberIndex = MemberIndex;
        if (MoldMember is null)
        {
            return;
        }

        fExpressionname = MoldMember.ExpandedExpressionName;
        if (Owner.MemberPersistenceMappers.MemberMapperByExpressionName(fExpressionname) is not null)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sDuplicatePMappersForXInY, fExpressionname, Owner.ExpressionName));
        }

        if (MoldMember is TMoldAttribute attribute)
        {
            fDelayedFetch = attribute.EffectiveDelayedFetch;
            fContentName = TypeNameDictionary.MappingForModelName(attribute.BoldType)!.ExpandedContentsName;
            fIsStoredInObject = MoldMember.EffectivePersistent;
        }
        else
        {
            var role = (TMoldRole)MoldMember;
            fIsStoredInObject = role.EffectivePersistent && (role.EffectiveEmbedded || role.RoleType == TBoldRoleType.rtInnerLinkRole);
            fDelayedFetch = role.EffectiveDelayedFetch;
            bool linked = role.Association!.LinkClass is not null;
            fContentName = role.RoleType == TBoldRoleType.rtInnerLinkRole
                ? BoldDefaultStreamNames.BoldContentName_ObjectIdRef
                : role.Multi
                    ? linked ? BoldDefaultStreamNames.BoldContentName_ObjectIdListRefPair : BoldDefaultStreamNames.BoldContentName_ObjectIdListRef
                    : linked ? BoldDefaultStreamNames.BoldContentName_ObjectIdRefPair : BoldDefaultStreamNames.BoldContentName_ObjectIdRef;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected internal virtual void InitializePSDescriptions()
    {
    }

    [Delphi(PortStatus.Ported)] protected virtual bool GetSupportsPolymorphicFetch() => false;

    /// <summary>Whether a mapper of this class can store the content; subclasses hide it with their own static CanStore.</summary>
    [Delphi(PortStatus.Ported, Note = "A static method (class functions cannot be virtual in .NET); BoldPMappers.ClassCanStore calls the most derived one.")]
    public static bool CanStore(string ContentName) => false;

    [Delphi(PortStatus.Ported)] public bool DelayedFetch => fDelayedFetch;
    [Delphi(PortStatus.Ported)] public TBoldObjectPersistenceMapper ObjectPersistenceMapper => fObjectPersistenceMapper;
    [Delphi(PortStatus.Ported)] public bool IsStoredInObject => fIsStoredInObject;
    [Delphi(PortStatus.Ported)] public bool CustomFetch => fCustomFetch;
    [Delphi(PortStatus.Ported)] public bool CustomCreateUpDate => fCustomCreateUpDate;
    [Delphi(PortStatus.Ported)] public int MemberIndex => fMemberIndex;
    [Delphi(PortStatus.Ported)] public string ContentName => fContentName;
    [Delphi(PortStatus.Ported)] public string ExpressionName => fExpressionname;

    [Delphi(PortStatus.Ported)]
    public IBoldValue? GetValue(IBoldObjectContents ObjectContents) => MemberIndex != -1 ? ObjectContents.ValueByIndex(MemberIndex) : null;

    /// <summary>The member's value, created with the content type when missing.</summary>
    [Delphi(PortStatus.Ported)]
    public IBoldValue? GetEnsuredValue(IBoldObjectContents ObjectContents)
    {
        if (MemberIndex != -1)
        {
            ObjectContents.EnsureMember(new TBoldMemberID(MemberIndex), ContentName);
        }

        return GetValue(ObjectContents);
    }

    /// <summary>Whether the member must be written: always when whole objects are updated, else when modified.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool IsDirty(IBoldObjectContents ObjectContents)
    {
        if (ObjectPersistenceMapper.SystemPersistenceMapper.UpdateWholeObjects)
        {
            return true;
        }

        return ObjectContents.ValueByIndex(MemberIndex) is { } aValue && aValue.BoldPersistenceState == TBoldValuePersistenceState.bvpsModified;
    }

    /// <summary>Whether a fetched value may overwrite the member: when it is missing, invalid or current (not modified).</summary>
    [Delphi(PortStatus.Ported)]
    public virtual bool ShouldFetch(IBoldObjectContents ObjectContents) =>
        ObjectContents.ValueByIndex(MemberIndex) is not { } aValue ||
        aValue.BoldPersistenceState is TBoldValuePersistenceState.bvpsInvalid or TBoldValuePersistenceState.bvpsCurrent;

    [Delphi(PortStatus.Ported)]
    public virtual void PMCreate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sCallToAbstractMethodOnCustomMapper, "TBoldMemberPersistenceMapper", "PMCreate", GetType().Name, "PMCreate"));

    [Delphi(PortStatus.Ported)]
    public virtual void PMUpdate(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sCallToAbstractMethodOnCustomMapper, "TBoldMemberPersistenceMapper", "PMUpDate", GetType().Name, "PMUpDate"));

    [Delphi(PortStatus.Ported)]
    public virtual void PMDelete(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sCallToAbstractMethodOnCustomMapper, "TBoldMemberPersistenceMapper", "PMDelete", GetType().Name, "PMDelete"));

    [Delphi(PortStatus.Ported)]
    public virtual void PMFetch(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace, int FetchMode, TBoldIDTranslationList TranslationList,
        TBoldObjectIdList? FailureList) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sCallToAbstractMethodOnCustomMapper, "TBoldMemberPersistenceMapper", "PMFetch", GetType().Name, "PMFetch"));

    [Delphi(PortStatus.Ported)] public bool SupportsPolymorphicFetch => GetSupportsPolymorphicFetch();
}

/// <summary>The object mappers of a system, by topological class index (null for classes not mapped).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectPersistenceMapperList : TBoldIndexableList
{
    [Delphi(PortStatus.Ported)] private TBoldObjectPersistenceMapper? GetItem(int index) => (TBoldObjectPersistenceMapper?)base[index];
    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectPersistenceMapperList.Items")] public new TBoldObjectPersistenceMapper? this[int index] => GetItem(index);
}

/// <summary>The member mappers of a class, also by expression name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberPersistenceMapperList : TBoldIndexableList
{
    private static int IX_MemberMapper = -1;

    [Delphi(PortStatus.Ported)]
    public TBoldMemberPersistenceMapperList()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref IX_MemberMapper, AddIndex(new TBoldMemberMapperIndex()));
    }

    [Delphi(PortStatus.Ported)] private TBoldMemberPersistenceMapper GetItem(int index) => (TBoldMemberPersistenceMapper)base[index]!;

    [Delphi(PortStatus.Ported)]
    private TBoldMemberPersistenceMapper? GetMemberMapperByExpressionName(string ExpressionName) =>
        (TBoldMemberPersistenceMapper?)((TBoldMemberMapperIndex)Indexes(IX_MemberMapper)!).FindByString(ExpressionName);

    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberPersistenceMapperList.Items")] public new TBoldMemberPersistenceMapper this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldMemberPersistenceMapper? MemberMapperByExpressionName(string ExpressionName) => GetMemberMapperByExpressionName(ExpressionName);
}

/// <summary>Indexes member mappers by expression name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMemberMapperIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldMemberPersistenceMapper)Item!).ExpressionName;
}

/// <summary>Mapper logging, and the class-level CanStore of member mapper types.</summary>
public static partial class BoldPMappers
{
    [Delphi(PortStatus.Ported)] public static TBoldLogHandler? BoldPMLogHandler;

    [Delphi(PortStatus.Ported)]
    public static void BoldPMLog(string s) => BoldPMLogHandler?.Log(SysUtils.FormatDateTime("c: ", SysUtils.Now()) + SysUtils.Trim(s));

    [Delphi(PortStatus.Ported)]
    public static void BoldPMLogFmt(string s, params object?[] Args) => BoldPMLogHandler?.LogFmt(SysUtils.FormatDateTime("c: ", SysUtils.Now()) + SysUtils.Trim(s), Args);

    /// <summary>Calls the most derived static CanStore(string) of a member mapper type (Delphi's virtual class function).</summary>
    public static bool ClassCanStore(Type MapperClass, string ContentName)
    {
        for (Type? t = MapperClass; t is not null; t = t.BaseType)
        {
            MethodInfo? method = t.GetMethod("CanStore", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, [typeof(string)]);
            if (method is not null)
            {
                return (bool)method.Invoke(null, [ContentName])!;
            }
        }

        return false;
    }
}
