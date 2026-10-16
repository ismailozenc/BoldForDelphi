// Delphi: Source/Persistence/DB/BoldAbstractPersistenceHandleDB.pas
// Port map: CSharp/port/units/BoldAbstractPersistenceHandleDB.md
// The persistence handle of a SQL database with the default mapping: it creates the schema, makes the controller for
// its model, and opens and closes the database with the handle.

namespace Bold;

/// <summary>Unit-level constants of BoldAbstractPersistenceHandleDB.</summary>
public static partial class BoldAbstractPersistenceHandleDB
{
    internal const int breModelChanged = 100;
    internal const int breModelDestroying = 101;
    internal const int breUpgraderHandleDestroying = 102;
}

/// <summary>A persistence handle over a database and SQL configuration that subclasses provide.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldAbstractPersistenceHandleDB : TBoldPersistenceHandle
{
    private TBoldAbstractModel? fBoldModel;
    private TDateTime fClockLogGranularity = SysUtils.ZeroDateTime;
    private bool FEvolutionSupport;
    private bool fIgnoreUnknownTables;
    private readonly TBoldPassthroughSubscriber fComponentSubscriber;
    private TBoldAbstractObjectUpgraderHandle? FUpgraderHandle;

    [Delphi(PortStatus.Ported, Note = "The owner is an IContainer where Delphi takes a TComponent owner.")]
    protected TBoldAbstractPersistenceHandleDB(System.ComponentModel.IContainer? Owner = null) : base(Owner)
    {
        fComponentSubscriber = new TBoldPassthroughSubscriber(_ReceiveComponentEvents);
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldAbstractPersistenceHandleDB.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            fComponentSubscriber.Dispose();
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private string GetClockLogGranularity()
    {
        SysUtils.DecodeTime(fClockLogGranularity, out int hrs, out int mins, out int secs, out int msecs);
        return SysUtils.Format("%d:%d:%d.%d", hrs, mins, secs, msecs);
    }

    /// <summary>Reads "h:m:s.ms" (empty for none).</summary>
    [Delphi(PortStatus.Ported)]
    private void SetClockLogGranularity(string Value)
    {
        string input = Value;

        int GetNext(string Delimiter)
        {
            int p;
            if (Delimiter.Length != 0)
            {
                p = input.IndexOf(Delimiter, StringComparison.Ordinal) + 1;
                if (p < 2)
                {
                    throw new EBold(SysUtils.Format(PersistenceConsts.sClockStringFormatError, GetType().Name));
                }
            }
            else
            {
                p = input.Length + 1;
            }

            int result = SysUtils.StrToIntDef(input[..(p - 1)], -1);
            if (result == -1)
            {
                throw new EBold(SysUtils.Format(PersistenceConsts.sClockStringFormatError, GetType().Name));
            }

            input = p < input.Length ? input[p..] : "";
            return result;
        }

        if (input.Length == 0)
        {
            fClockLogGranularity = SysUtils.ZeroDateTime;
        }
        else
        {
            int hrs = GetNext(":");
            int mins = GetNext(":");
            int secs = GetNext(".");
            int msecs = GetNext("");
            fClockLogGranularity = SysUtils.EncodeTime(hrs, mins, secs, msecs);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetEvolutionSupport(bool Value)
    {
        if (Value != FEvolutionSupport)
        {
            if (Active)
            {
                throw new EBold(SysUtils.Format(PersistenceConsts.sCannotSetWhenHandleIsActive, GetType().Name, "SetEvolutionSupport", Site?.Name ?? ""));
            }

            FEvolutionSupport = Value;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void _ReceiveComponentEvents(object? Originator, TBoldEvent OriginalEvent, TBoldRequestedEvent RequestedEvent)
    {
        switch (RequestedEvent)
        {
            case BoldAbstractPersistenceHandleDB.breModelChanged:
                if (!Active)
                {
                    ReleasePersistenceController();
                }

                break;
            case BoldAbstractPersistenceHandleDB.breModelDestroying:
                BoldModel = null;
                break;
            case BoldAbstractPersistenceHandleDB.breUpgraderHandleDestroying:
                UpgraderHandle = null;
                break;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetUpgraderHandle(TBoldAbstractObjectUpgraderHandle? Value)
    {
        if (FUpgraderHandle != Value)
        {
            FUpgraderHandle = Value;
            if (HasPersistenceController)
            {
                PersistenceControllerDefault.PersistenceMapper.ObjectUpgrader = Value?.ObjectUpgrader;
            }

            PlaceComponentSubscriptions();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void PlaceComponentSubscriptions()
    {
        fComponentSubscriber.CancelAllSubscriptions();
        if (fBoldModel is not null)
        {
            fBoldModel.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(BoldAbstractModel.beModelChanged), BoldAbstractPersistenceHandleDB.breModelChanged);
            fBoldModel.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldAbstractPersistenceHandleDB.breModelDestroying);
        }

        FUpgraderHandle?.AddSmallSubscription(fComponentSubscriber, TBoldSmallEventSet.Of(BoldSubscription.beDestroying), BoldAbstractPersistenceHandleDB.breUpgraderHandleDestroying);
    }

    [Delphi(PortStatus.Ported)]
    private void SetBoldModel(TBoldAbstractModel? NewModel)
    {
        if (fBoldModel != NewModel)
        {
            ReleasePersistenceController();
            fBoldModel = NewModel;
            PlaceComponentSubscriptions();
        }
    }

    [Delphi(PortStatus.Ported)]
    private TBoldPersistenceControllerDefault GetPersistenceControllerDefault() => (TBoldPersistenceControllerDefault)PersistenceController;

    [Delphi(PortStatus.Ported)]
    private void PreparePSParams(TBoldPSParams PSParams) => ((TBoldPSSQLParams)PSParams).IgnoreUnknownTables = fIgnoreUnknownTables;

    /// <summary>A default controller for the model, with the handle's clock settings and upgrader.</summary>
    [Delphi(PortStatus.Ported)]
    protected override TBoldPersistenceController CreatePersistenceController()
    {
        if (BoldModel is null)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sCannotGetPControllerWithoutModel, GetType().Name));
        }

        var PController = new TBoldPersistenceControllerDefault(BoldModel.MoldModel, BoldModel.TypeNameDictionary, SQLDatabaseConfig, GetDataBaseInterface);
        PController.PersistenceMapper.OnGetCurrentTime = OnGetCurrentTime;
        PController.PersistenceMapper.ClockLogGranularity = fClockLogGranularity;
        if (UpgraderHandle is not null)
        {
            PController.PersistenceMapper.ObjectUpgrader = UpgraderHandle.ObjectUpgrader;
        }

        return PController;
    }

    /// <summary>Activating opens the database (reading the mapping with evolution support); deactivating closes it.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void SetActive(bool Value)
    {
        if (Value != Active)
        {
            if (Value)
            {
                if (UpgraderHandle is not null && !BoldModel!.MoldModel.UseModelVersion)
                {
                    throw new EBold(SysUtils.Format(PersistenceConsts.sCannotActivate_UpgraderMismatch, GetType().Name));
                }

                PersistenceControllerDefault.OpenDatabase(EvolutionSupport);
            }
            else
            {
                PersistenceControllerDefault.CloseDataBase();
            }
        }

        base.SetActive(Value);
    }

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldSQLDataBaseConfig GetSQLDatabaseConfig();

    [Delphi(PortStatus.Ported)]
    protected abstract IBoldDataBase? GetDataBaseInterface();

    [Delphi(PortStatus.Ported)]
    protected virtual void AssertSQLDatabaseconfig(string Context)
    {
        if (SQLDatabaseConfig is null)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sSQLDatabaseConfigMissing, GetType().Name, Context));
        }
    }

    /// <summary>Creates the tables of the model (and Bold's own) in the database, which must not be active.</summary>
    [Delphi(PortStatus.Ported)]
    public void CreateDataBaseSchema(bool IgnoreUnknownTables = false)
    {
        fIgnoreUnknownTables = IgnoreUnknownTables;
        if (Active)
        {
            throw new EBold(PersistenceConsts.sCannotGenerateWhenHandleIsActive);
        }

        if (BoldModel is null)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sModelComponentMissing, GetType().Name, Site?.Name ?? ""));
        }

        AssertSQLDatabaseconfig(PersistenceConsts.sCreateSchema);
        var PMapper = new TBoldSystemDefaultMapper(BoldModel.RawMoldModel, BoldModel.TypeNameDictionary, SQLDatabaseConfig, GetDataBaseInterface);
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        try
        {
            try
            {
                BoldLog.StartLog(PersistenceConsts.sGenerateSchema);
                PMapper.OpenDatabase(false, false);
                PMapper.OnPreparePSParams = PreparePSParams;
                PMapper.CreatePersistentStorage();
                BoldLog.EndLog();
            }
            catch (Exception e)
            {
                BoldLog.LogFmt(PersistenceConsts.sSchemaGenerationAborted, [e.Message], TBoldLogType.ltError);
                throw;
            }
        }
        finally
        {
            PMapper.CloseDataBase();
        }
    }

    /// <summary>Creates the mapping tables when missing and writes the mapping of the model to them.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddModelEvolutionInfoToDatabase()
    {
        void CreateTable(TBoldSQLTableDescription TableDescription)
        {
            IBoldExecQuery? Query = DatabaseInterface.GetExecQuery();
            try
            {
                Query.AssignSQLText(TableDescription.SQLForCreateTable(DatabaseInterface));
                Query.ExecSQL();
            }
            finally
            {
                DatabaseInterface.ReleaseExecQuery(ref Query);
            }
        }

        void EnsureTable(TBoldSQLTableDescription TableDescription)
        {
            if (!DatabaseInterface.TableExists(TableDescription.SQLName))
            {
                CreateTable(TableDescription);
            }
        }

        bool WasActive = Active;
        bool OldEvolutionSupport = EvolutionSupport;
        if (!WasActive)
        {
            EvolutionSupport = false;
            Active = true;
        }

        TBoldDefaultSystemDescription ps = PersistenceControllerDefault.PersistenceMapper.PSSystemDescription;
        EnsureTable(ps.MemberMappingTable!);
        EnsureTable(ps.AllInstancesMappingTable!);
        EnsureTable(ps.ObjectStorageMappingTable!);
        PersistenceControllerDefault.PersistenceMapper.MappingInfo.WriteDataToDB(DatabaseInterface);
        if (!WasActive)
        {
            Active = false;
            EvolutionSupport = OldEvolutionSupport;
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldPersistenceControllerDefault PersistenceControllerDefault => GetPersistenceControllerDefault();
    [Delphi(PortStatus.Ported)] public IBoldDataBase DatabaseInterface => GetDataBaseInterface()!;
    [Delphi(PortStatus.Ported)] public TBoldSQLDataBaseConfig SQLDatabaseConfig => GetSQLDatabaseConfig();
    [Delphi(PortStatus.Ported)] public TBoldAbstractModel? BoldModel { get => fBoldModel; set => SetBoldModel(value); }
    [Delphi(PortStatus.Ported)] public TBoldGetTimeEvent? OnGetCurrentTime { get; set; }
    [Delphi(PortStatus.Ported)] public string ClockLogGranularity { get => GetClockLogGranularity(); set => SetClockLogGranularity(value); }
    [Delphi(PortStatus.Ported)] public bool EvolutionSupport { get => FEvolutionSupport; set => SetEvolutionSupport(value); }
    [Delphi(PortStatus.Ported)] public TBoldAbstractObjectUpgraderHandle? UpgraderHandle { get => FUpgraderHandle; set => SetUpgraderHandle(value); }
}
