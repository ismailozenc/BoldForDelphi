// Delphi: Source/Persistence/ObjectUpgrading/BoldObjectUpgrader.pas
// Port map: CSharp/port/units/BoldObjectUpgrader.md
// The object upgrader of the default mapping: it loads an object stored by an older model version into a private
// object space, lets an event upgrade it, and saves it back with the current version.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldObjectUpgrader", Symbol = "TBoldObjectUpgrader.Destroy", Note = "Released the object space; ReleaseBoldSystem does that when called.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public delegate void TBoldUpgradeEvent(TBoldObject Obj);

[Delphi(PortStatus.Ported)]
public delegate TBoldSystemTypeInfo? TBoldGetSystemTypeInfoEvent();

[Delphi(PortStatus.Ported)]
public delegate TBoldPersistenceControllerDefault? TBoldGetPersistenceControllerEvent();

/// <summary>A configuration item with its own upgrade event.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectUpgraderConfigurationItemWithEvent(TCollection? Collection) : TBoldObjectUpgraderConfigurationItem(Collection)
{
    private TBoldUpgradeEvent? fOnUpgradeObject;

    [Delphi(PortStatus.Ported)]
    public override string GetNamePath() => (Config.ConfigOwner?.GetNamePath() ?? "") + "._" + ExpressionName + "_";

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? source)
    {
        base.Assign(source);
        if (source is TBoldObjectUpgraderConfigurationItemWithEvent item)
        {
            OnUpgradeObject = item.OnUpgradeObject;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldUpgradeEvent? OnUpgradeObject { get => fOnUpgradeObject; set => fOnUpgradeObject = value; }
}

/// <summary>A configuration of items with events.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectUpgraderConfigurationWithEvent : TBoldObjectUpgraderConfiguration
{
    /// <summary>A configuration of TBoldObjectUpgraderConfigurationItemWithEvent items.</summary>
    /// <remarks>The inherited constructor (C# does not inherit constructors).</remarks>
    public TBoldObjectUpgraderConfigurationWithEvent(TPersistent? Owner)
        : base(Owner, typeof(TBoldObjectUpgraderConfigurationItemWithEvent))
    {
    }

    [Delphi(PortStatus.Ported)]
    protected override Type GetItemClass() => typeof(TBoldObjectUpgraderConfigurationItemWithEvent);
}

/// <summary>Upgrades objects one by one in a private object space over the persistence controller.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldObjectUpgrader : TBoldAbstractObjectUpgrader
{
    private int fNestingLevel;
    private TBoldSystem? fBoldSystem;
    private TBoldUpgradeEvent? fOnUpgradeObject;
    private readonly TBoldGetSystemTypeInfoEvent fGetSystemTypeInfo;
    private readonly TBoldGetPersistenceControllerEvent fGetPersistenceController;

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectUpgrader.create")]
    public TBoldObjectUpgrader(TBoldObjectUpgraderConfiguration Config, TBoldGetSystemTypeInfoEvent GetSystemTypeInfo,
        TBoldGetPersistenceControllerEvent GetPersistenceController)
        : base(Config)
    {
        fGetSystemTypeInfo = GetSystemTypeInfo;
        fGetPersistenceController = GetPersistenceController;
    }

    /// <summary>Saves the object space at the outermost level, and releases it when it holds many objects.</summary>
    [Delphi(PortStatus.Ported, Note = "Leaves the operation before releasing the object space; Delphi released it inside, which ReleaseBoldSystem refuses.")]
    public override void EndTransaction()
    {
        if (fNestingLevel == 1)
        {
            if (BoldSystem.BoldDirty)
            {
                BoldSystem.UpdateDatabase();
            }

            if (BoldSystem.Locators.Count > 1000)
            {
                fNestingLevel--;
                ReleaseBoldSystem();
                return;
            }
        }

        fNestingLevel--;
    }

    /// <summary>Discards and releases the object space at the outermost level.</summary>
    [Delphi(PortStatus.Ported)]
    public override void FailTransaction()
    {
        fNestingLevel--;
        if (fNestingLevel == 0)
        {
            BoldSystem.Discard();
            ReleaseBoldSystem();
        }
    }

    /// <summary>Loads the object from the query's row into the object space, bypassing the upgrader.</summary>
    [Delphi(PortStatus.Ported, Note = "Passes an empty translation list where Delphi passes nil.")]
    private void FetchObjectForId(TBoldObjectId ObjectId, IBoldQuery Query)
    {
        var MemberPMList = new TBoldMemberPersistenceMapperList { OwnsEntries = false };
        var ObjectList = new TBoldObjectList();
        TBoldObjectDefaultMapper ObjectMapper = ObjectMapperForId(ObjectId);
        ObjectMapper.BuildMemberFetchLists(null, MemberPMList, null, BoldDefs.fmNormal);
        ObjectMapper.HandleFetchData(ObjectId, ValueSpacePmIn, new TBoldIDTranslationList(), Query, MemberPMList, BoldDefs.fmNormal, null);
        ObjectList.Add(BoldSystem.EnsuredLocatorByID(ObjectId).BoldObject);
        BoldSystem.SystemPersistenceHandler.EndFetchForAll(ObjectList, null);
    }

    /// <summary>
    /// Adds the SQL that sets the current model version on stored objects that need no manual upgrade: classes without
    /// a configuration item, and those whose version is at least the one the item upgrades below.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void GenerateAutoUpgradeScript(TStrings Script)
    {
        TBoldSystemDefaultMapper SystemMapper = PersistenceController.PersistenceMapper;
        int CurrentVersion = SystemMapper.ModelVersion;
        var ObjectMapper = (TBoldObjectDefaultMapper)SystemMapper.ObjectPersistenceMappers[0]!;
        string RootTable = ObjectMapper.MainTable!.SQLName;
        string VersionColumn = ObjectMapper.ModelVersionMember!.ColumnDescriptions[0].SQLName;
        var UpgradeWholeClass = new TStringList();
        for (int i = 0; i < SystemMapper.ObjectPersistenceMappers.Count; i++)
        {
            if (SystemMapper.ObjectPersistenceMappers[i] is TBoldObjectDefaultMapper Mapper)
            {
                TBoldObjectUpgraderConfigurationItem? ConfigItem = Config.ItemByName(Mapper.ExpressionName);
                if (ConfigItem is null)
                {
                    UpgradeWholeClass.Add(Mapper.BoldDbType.ToString());
                }
                else if (ConfigItem.UpgradeOlderThanVersion < CurrentVersion)
                {
                    Script.Add(SysUtils.Format("UPDATE %s SET %s = %d WHERE (%s = %d) AND (%s >= %d) AND (%s <> %d)",
                        RootTable, VersionColumn, CurrentVersion, BoldDefs.TYPECOLUMN_NAME, Mapper.BoldDbType,
                        VersionColumn, ConfigItem.UpgradeOlderThanVersion, VersionColumn, CurrentVersion));
                }
            }
        }

        if (UpgradeWholeClass.Count != 0)
        {
            Script.Add(SysUtils.Format("UPDATE %s SET %s = %d WHERE %s IN (%s) AND (%s <> %d)",
                RootTable, VersionColumn, CurrentVersion, BoldDefs.TYPECOLUMN_NAME,
                BoldUtils.BoldSeparateStringList(UpgradeWholeClass, ", ", "", ""), VersionColumn, CurrentVersion));
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystem GetBoldSystem()
    {
        if (fBoldSystem is null)
        {
            TBoldSystemTypeInfo? aSystemTypeInfo = SystemTypeInfo;
            TBoldPersistenceControllerDefault? aPController = fGetPersistenceController();
            if (aSystemTypeInfo is null)
            {
                throw new EBold(SysUtils.Format(BoldCoreConsts.sMissingTypeInfo, GetType().Name));
            }

            if (aPController is null)
            {
                throw new EBold(SysUtils.Format(BoldCoreConsts.sMissingPersistenceController, GetType().Name));
            }

            fBoldSystem = new TBoldSystem(null, aSystemTypeInfo, aPController);
        }

        return fBoldSystem;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldPersistenceControllerDefault GetPersistenceController() =>
        fGetPersistenceController() ?? throw new EBold(SysUtils.Format(BoldCoreConsts.sMissingPersistenceController, GetType().Name));

    [Delphi(PortStatus.Ported)]
    private TBoldSystemTypeInfo? GetSystemTypeInfo() => fGetSystemTypeInfo();

    [Delphi(PortStatus.Ported)]
    private IBoldValueSpace GetValueSpacePMIn() => BoldSystem.AsIBoldvalueSpace(TBoldDomainElementProxyMode.bdepPMIn);

    [Delphi(PortStatus.Ported)]
    private TBoldObjectDefaultMapper ObjectMapperForId(TBoldObjectId ObjectId) =>
        (TBoldObjectDefaultMapper)PersistenceController.PersistenceMapper.ObjectPersistenceMappers[ObjectId.TopSortedIndex]!;

    /// <summary>Frees the private object space; not allowed inside an upgrade.</summary>
    [Delphi(PortStatus.Ported)]
    public void ReleaseBoldSystem()
    {
        if (fNestingLevel != 0)
        {
            throw new EBold(SysUtils.Format(BoldCoreConsts.sCannotReleaseInOperation, GetType().Name));
        }

        TBoldSystem? aSystem = fBoldSystem;
        fBoldSystem = null;
        aSystem?.Dispose();
    }

    [Delphi(PortStatus.Ported)]
    public override void StartTransaction() => fNestingLevel++;

    /// <summary>Calls the class's upgrade event (or the general one) and leaves the object dirty so it is saved.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpgradeObject(TBoldObject Obj)
    {
        var ConfigItem = (TBoldObjectUpgraderConfigurationItemWithEvent?)Config.ItemByName(Obj.BoldClassTypeInfo.ExpressionName);
        Obj.MarkObjectDirty();
        if (ConfigItem?.OnUpgradeObject is { } ItemEvent)
        {
            ItemEvent(Obj);
        }
        else
        {
            OnUpgradeObject?.Invoke(Obj);
        }

        // in case some user decides to call discard on the object, we better dirtify it again.
        Obj.MarkObjectDirty();
    }

    [Delphi(PortStatus.Ported)]
    public override void UpgradeObjectById(TBoldObjectId ObjectId, IBoldQuery Query)
    {
        StartTransaction();
        try
        {
            FetchObjectForId(ObjectId, Query);
            TBoldObject Obj = BoldSystem.EnsuredLocatorByID(ObjectId).BoldObject
                ?? throw new EBoldInternal(SysUtils.Format("%s.UpgradeObject: The object is not in the system", GetType().Name));
            UpgradeObject(Obj);
            EndTransaction();
        }
        catch
        {
            FailTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldUpgradeEvent? OnUpgradeObject { get => fOnUpgradeObject; set => fOnUpgradeObject = value; }
    [Delphi(PortStatus.Ported)] public TBoldSystemTypeInfo? SystemTypeInfo => GetSystemTypeInfo();
    [Delphi(PortStatus.Ported)] public TBoldSystem BoldSystem => GetBoldSystem();
    [Delphi(PortStatus.Ported)] public IBoldValueSpace ValueSpacePmIn => GetValueSpacePMIn();
    [Delphi(PortStatus.Ported)] public TBoldPersistenceControllerDefault PersistenceController => GetPersistenceController();
}
