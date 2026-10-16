// Delphi: Source/Persistence/ObjectUpgrading/BoldBatchUpgrader.pas
// Port map: CSharp/port/units/BoldBatchUpgrader.md
// Upgrades every stored object of an older model version: first by SQL for the classes that need no manual upgrade,
// then class by class in batches through the object upgrader, until done or out of time.

namespace Bold;

/// <summary>Upgrades all old objects of the database in batches.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldBatchUpgrader
{
    private readonly TBoldObjectUpgrader fUpgrader;
    private bool fIsUpgrading;
    private int fUpgradedObjects;
    private TDateTime fStartTime;
    private readonly TBoldSystemDefaultMapper fSystemMapper;
    private int fBatchSize;
    private int fIntervalBetweenBatches;
    private TDateTime fMaxExecuteTime;
    private int fAutoUpgradedObjects;

    /// <summary>Runs the auto upgrade script and counts the rows it changed.</summary>
    [Delphi(PortStatus.Ported)]
    private void AutoUpgradeObjects()
    {
        var Script = new TStringList();
        IBoldExecQuery? Query = SystemMapper.GetExecQuery();
        try
        {
            Upgrader.GenerateAutoUpgradeScript(Script);
            for (int i = 0; i < Script.Count; i++)
            {
                Query.AssignSQLText(Script[i]);
                Query.ExecSQL();
                fAutoUpgradedObjects += Query.RowsAffected;
            }
        }
        finally
        {
            SystemMapper.ReleaseExecQuery(ref Query);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldBatchUpgrader(TBoldSystemDefaultMapper SystemMapper, TBoldObjectUpgrader Upgrader)
    {
        if (!SystemMapper.SupportsObjectUpgrading)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sUpgradeNotSupported, GetType().Name));
        }

        fSystemMapper = SystemMapper;
        fUpgrader = Upgrader;
        fBatchSize = 100;
        fMaxExecuteTime = SysUtils.EncodeTime(1, 0, 0, 0);
        fIsUpgrading = false;
        fIntervalBetweenBatches = 5000;
        fUpgradedObjects = 0;
    }

    /// <summary>Upgrades the old objects of exactly this class, a batch at a time.</summary>
    [Delphi(PortStatus.Ported, Note = "Qualifies the columns with the root table; Delphi's unqualified BOLD_TYPE is ambiguous for a class with its own table, which has the column too.")]
    private void UpgradeClass(int TopSortedIndex)
    {
        var Condition = new TBoldSQLCondition();
        var ObjectIdList = new TBoldObjectIdList();
        var TranslationList = new TBoldIDTranslationList();
        Condition.TopSortedIndex = TopSortedIndex;
        // exact type and not the current version.
        string RootTable = SystemMapper.RootClassObjectPersistenceMapper.MainTable!.SQLName;
        Condition.WhereFragment = SysUtils.Format("%s.%s = %d AND %s.%s <> %d",
            RootTable, BoldDefs.TYPECOLUMN_NAME,
            SystemMapper.BoldDbTypeForTopSortedIndex(TopSortedIndex),
            RootTable, SystemMapper.RootClassObjectPersistenceMapper.ModelVersionMember!.ColumnDescriptions[0].SQLName,
            SystemMapper.ModelVersion);
        Condition.MaxAnswers = BatchSize;
        do
        {
            ObjectIdList.Clear();
            SystemMapper.PMFetchClassWithCondition(ObjectIdList, Upgrader.ValueSpacePmIn, Condition, BoldDefs.fmNormal, TranslationList);
            if (ObjectIdList.Count != 0)
            {
                UpgradeObjectIdListInTransaction(ObjectIdList);
                Thread.Sleep(IntervalBetweenBatches);
            }
        }
        while (ObjectIdList.Count != 0 && SysUtils.Now() <= fStartTime + (MaxExecuteTime - SysUtils.ZeroDateTime));
    }

    /// <summary>Selects the objects' rows and upgrades each object from its row.</summary>
    [Delphi(PortStatus.Ported)]
    private void UpgradeObjectIdList(TBoldObjectIdList ObjectIdList)
    {
        var MemberPMList = new TBoldMemberPersistenceMapperList { OwnsEntries = false };
        IBoldQuery? aQuery = SystemMapper.GetQuery();
        var TempList = new TStringList();
        var sql = new TStringList();
        var ObjectMapper = (TBoldObjectDefaultMapper)SystemMapper.ObjectPersistenceMappers[ObjectIdList[0].TopSortedIndex]!;
        try
        {
            ObjectMapper.BuildMemberFetchLists(null, MemberPMList, null, BoldDefs.fmNormal);
            ObjectMapper.RetrieveSelectStatement(sql, MemberPMList, BoldDefs.fmNormal, ObjectMapper.Versioned);
            for (int i = 0; i < ObjectIdList.Count; i++)
            {
                if (!ObjectIdList[i].TopSortedIndexExact)
                {
                    throw new EBold(SysUtils.Format(PersistenceConsts.sObjectIDListNotExact, GetType().Name));
                }

                if (ObjectIdList[i].TopSortedIndex != ObjectMapper.TopSortedIndex)
                {
                    throw new EBold(SysUtils.Format(PersistenceConsts.sObjectIDListNotHomogenous, GetType().Name));
                }

                TempList.Add(ObjectIdList[i].AsString);
            }

            if (MemberPMList.Count > 0 && TempList.Count > 0)
            {
                BoldUtils.BoldAppendToStrings(sql, SysUtils.Format("in (%s)", BoldUtils.BoldSeparateStringList(TempList, ", ", "", "")), false);
                if (ObjectMapper.Versioned)
                {
                    ObjectMapper.RetrieveTimeStampCondition(sql, BoldDefs.BOLDMAXTIMESTAMP, true, "AND", false);
                }

                aQuery.AssignSQL(sql);
                aQuery.Open();
                while (!aQuery.Eof)
                {
                    TBoldObjectId TempId = SystemMapper.NewIdFromQuery(aQuery, 1, 0, BoldDefs.BOLDMAXTIMESTAMP);
                    TBoldObjectId NewId = ObjectIdList.IDByID(TempId)!;
                    Upgrader.UpgradeObjectById(NewId, aQuery);
                    fUpgradedObjects++;
                    aQuery.Next();
                }

                aQuery.Close();
            }
        }
        finally
        {
            SystemMapper.ReleaseQuery(ref aQuery);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void UpgradeObjectIdListInTransaction(TBoldObjectIdList ObjectIdList)
    {
        Upgrader.StartTransaction();
        try
        {
            UpgradeObjectIdList(ObjectIdList);
            Upgrader.EndTransaction();
        }
        catch
        {
            Upgrader.FailTransaction();
            throw;
        }
    }

    /// <summary>Upgrades all old objects, unless already upgrading.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpgradeObjects()
    {
        if (IsUpgrading)
        {
            return;
        }

        if (SystemMapper.Database is null)
        {
            throw new EBold(SysUtils.Format(PersistenceConsts.sDBMustBeOpened, GetType().Name));
        }

        fIsUpgrading = true;
        try
        {
            fStartTime = SysUtils.Now();
            AutoUpgradeObjects();
            for (int i = 0; i < SystemMapper.ObjectPersistenceMappers.Count; i++)
            {
                if (SystemMapper.ObjectPersistenceMappers[i] is not null)
                {
                    UpgradeClass(i);
                }
            }
        }
        finally
        {
            fIsUpgrading = false;
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldSystemDefaultMapper SystemMapper => fSystemMapper;
    [Delphi(PortStatus.Ported)] public TBoldObjectUpgrader Upgrader => fUpgrader;
    [Delphi(PortStatus.Ported)] public int BatchSize { get => fBatchSize; set => fBatchSize = value; }
    [Delphi(PortStatus.Ported)] public TDateTime MaxExecuteTime { get => fMaxExecuteTime; set => fMaxExecuteTime = value; }
    [Delphi(PortStatus.Ported)] public int IntervalBetweenBatches { get => fIntervalBetweenBatches; set => fIntervalBetweenBatches = value; }
    [Delphi(PortStatus.Ported)] public bool IsUpgrading => fIsUpgrading;
    [Delphi(PortStatus.Ported)] public int UpgradedObjects => fUpgradedObjects;
    [Delphi(PortStatus.Ported)] public int AutoUpgradedObjects => fAutoUpgradedObjects;
}
