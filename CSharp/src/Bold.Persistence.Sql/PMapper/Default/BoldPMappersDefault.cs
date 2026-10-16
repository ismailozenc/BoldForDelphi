// Delphi: Source/PMapper/Default/BoldPMappersDefault.pas
// Port map: CSharp/port/units/BoldPMappersDefault.md
// The default object-relational mapping: Bold's system tables (ids, types, time stamps, clock log, X-files, mapping
// tables), a table per class (or shared with parent or children), id reservation, versioning, optimistic locking,
// fetching by id lists or conditions (SQL, OCL through the SQL nodes, change points) and the technical members.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappersDefault", Symbol = "TExternalIdGenerator", Note = "A function type; Func<string> in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappersDefault", Symbol = "TLittleClass", Note = "An (id, db type) pair; a value tuple in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappersDefault", Symbol = "finalization", Note = "Removed the registrations; the registries live as long as the process in .NET.")]

namespace Bold;

/// <summary>Unit-level members of BoldPMappersDefault.</summary>
public static partial class BoldPMappersDefault
{
    [Delphi(PortStatus.Ported)] public const string MEMBERIDCOLUMN_NAME = "MEMBER_ID";
    [Delphi(PortStatus.Ported)] public const TFieldType BOLDTIMESTAMPFIELDTYPE = TFieldType.ftInteger;
    // An implementation constant in Delphi (not in the port tool's inventory).
    internal const int TIMESTAMPMEMBERINDEX = -2;

    /// <summary>Makes the global ids of new objects (a bracketed GUID by default).</summary>
    [Delphi(PortStatus.Ported)]
    public static Func<string> ExternalIDGenerator { get; set; } = BoldGUIDUtils.BoldCreateGUIDWithBracketsAsString;

#pragma warning disable CA2255
    /// <summary>Registers the default member, system and object mappers.</summary>
    [Delphi(PortStatus.Ported, Symbol = "initialization")]
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Initialization()
    {
        BoldPMapperLists.BoldMemberPersistenceMappers().AddDescriptor(typeof(TBoldMemberDefaultMapper), TBoldAbstractionLevel.alAbstract);
        BoldPMapperLists.BoldSystemPersistenceMappers().Add(new TBoldSystemPersistenceMapperDescriptor(BoldDefs.DEFAULTNAME, typeof(TBoldSystemDefaultMapper)));
        BoldPMapperLists.BoldObjectPersistenceMappers().Add(new TBoldObjectPersistenceMapperDescriptor(BoldDefs.DEFAULTNAME, typeof(TBoldObjectDefaultMapper)));
    }
#pragma warning restore CA2255

    /// <summary>Runs Action for each block of FetchBlockSize indexes (start and stop inclusive) of Count items.</summary>
    internal static void ForEachBlock(int Count, int FetchBlockSize, Action<int, int> Action)
    {
        int ObjectCount = Count - 1;
        for (int Block = 0; Block <= ObjectCount / FetchBlockSize; Block++)
        {
            int Start = Block * FetchBlockSize;
            int Stop = BoldMath.MinIntValue(((Block + 1) * FetchBlockSize) - 1, ObjectCount);
            Action(Start, Stop);
        }
    }
}

[Delphi(PortStatus.Ported)]
public class EBoldCantGetID(string? message = null, Exception? innerException = null) : EBold(message, innerException);

/// <summary>Raised when optimistic locking fails for some objects.</summary>
[Delphi(PortStatus.Ported)]
public class EBoldOptimisticLockingFailed : EBoldOperationFailedForIdList
{
    [Delphi(PortStatus.Ported)]
    public EBoldOptimisticLockingFailed(string msg, object?[] args, TBoldObjectIdList FailedObjects) : base(msg, args, FailedObjects)
    {
    }
}

/// <summary>
/// The default system mapper: Bold's system tables, id reservation, time stamps and the clock log, global ids, and
/// fetching with OCL and change point conditions.
/// </summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldSystemDefaultMapper.CreateFromMold", Note = "The primary constructor.")]
public class TBoldSystemDefaultMapper(TMoldModel moldModel, TBoldTypeNameDictionary TypeNameDictionary, TBoldSQLDataBaseConfig SQlDatabaseConfig,
    TBoldGetDatabaseEvent? GetDatabaseFunc) : TBoldSystemSQLMapper(moldModel, TypeNameDictionary, SQlDatabaseConfig, GetDatabaseFunc)
{
    private TBoldSQLColumnDescription? fXFilesTimeStampColumn;
    private TBoldSQLColumnDescription? fXFilesGlobalIdColumn;
    private TBoldSQLColumnDescription? fTimeStampTableTimeStampColumn;
    private TBoldSQLColumnDescription? fLastClockTableLastTimeStampColumn;
    private TBoldSQLColumnDescription? fLastClockTableLastClockColumn;
    private TBoldSQLColumnDescription? fClockLogTableLastTimeStampColumn;
    private TBoldSQLColumnDescription? fClockLogTableThisTimeStampColumn;
    private TBoldSQLColumnDescription? fClockLogTableThisClockColumn;
    private TBoldSQLColumnDescription? fClockLogTableLastClockColumn;

    [Delphi(PortStatus.Ported)]
    private TBoldDefaultSystemDescription GetPSSystemDescription() => (TBoldDefaultSystemDescription)base.PSSystemDescription;

    [Delphi(PortStatus.Ported)]
    private TBoldObjectDefaultMapper GetRootClassObjectPersistenceMapper() => (TBoldObjectDefaultMapper)base.RootClassObjectPersistenceMapper;

    /// <summary>Selects the id, type and start time of the versions of the objects that start in the interval.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetChangePointsQuery(IBoldQuery Query, TBoldObjectIdList IdList, TBoldTimeStampType StartTime, TBoldTimeStampType EndTime, TBoldSQLNameSpace NameSpace)
    {
        var anSQLQuery = new TBoldSQLQuery(TBoldSQLQueryMode.qmSelect, PSSystemDescription, SQLDataBaseConfig, NameSpace) { IgnoreHistoricObjects = false };
        var sql = new TStringList();
        TBoldSQLTableDescription aTableDesc = RootClassObjectPersistenceMapper.MainTable!;
        TBoldSQLTableReference aTableRef = anSQLQuery.AddTableReference(aTableDesc.SQLName);
        TBoldSQLColumnReference StartColumnRef = aTableRef.GetColumnReference(BoldDefs.TIMESTAMPSTARTCOLUMNNAME);
        anSQLQuery.AddColumnToRetrieve(aTableRef.GetColumnReference(BoldDefs.IDCOLUMN_NAME));
        anSQLQuery.AddColumnToRetrieve(aTableRef.GetColumnReference(BoldDefs.TYPECOLUMN_NAME));
        anSQLQuery.AddColumnToRetrieve(StartColumnRef);
        anSQLQuery.AddWCF(TBoldSQLWCFBinaryInfix.CreateWCFForIdList(aTableRef.GetColumnReference(BoldDefs.IDCOLUMN_NAME), IdList));
        anSQLQuery.AddWCF(new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(StartColumnRef), new TBoldSQLWCFInteger(StartTime), ">="));
        anSQLQuery.AddWCF(new TBoldSQLWCFBinaryInfix(new TBoldSQLWCFColumnRef(StartColumnRef), new TBoldSQLWCFInteger(EndTime), "<="));
        anSQLQuery.GenerateSQL(sql);
        Query.AssignSQL(sql);
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldPSParams CreatePSParams() => new TBoldPSDefaultParams();

    [Delphi(PortStatus.Ported)]
    protected override TBoldPSSystemDescription CreatePSSystemDescription() => new TBoldDefaultSystemDescription(null, SQLDataBaseConfig, NationalCharConversion);

    [Delphi(PortStatus.Ported)]
    protected override void ReserveID() => ReservedCount++;

    [Delphi(PortStatus.Ported)] protected internal int NextDBID { get; set; } = -1;
    [Delphi(PortStatus.Ported)] protected internal int LastReservedDBID { get; set; } = -2;
    [Delphi(PortStatus.Ported)] protected internal int ReservedCount { get; set; }

    // The object mapper (in the same Delphi unit) starts and ends the id reservation's transaction.
    internal void DoStartTransaction(IBoldValueSpace ValueSpace) => StartTransaction(ValueSpace);
    internal void DoCommit(IBoldValueSpace ValueSpace) => Commit(ValueSpace);
    internal void DoRollBack(IBoldValueSpace ValueSpace) => RollBack(ValueSpace);

    [Delphi(PortStatus.Ported)]
    protected TBoldObjectId NewGlobalIdFromQuery(IBoldQuery aQuery, int BoldDbTypeColumn)
    {
        int TopSortedIndex = TopSortedIndexForBoldDbType(aQuery.Fields(BoldDbTypeColumn).AsInteger);
        return new TBoldGlobalId(aQuery.FieldByName(XFilesGlobalIdColumn!.SQLName).AsString, TopSortedIndex, true, ObjectPersistenceMappers[TopSortedIndex]!.ExpressionName);
    }

    /// <summary>Increments the time stamp and reads it; with a clock log, logs the time when the granularity has passed.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void GetNewTimeStamp()
    {
        IBoldQuery? aQuery = GetQuery();
        IBoldExecQuery? aExecQuery = GetExecQuery();
        try
        {
            aExecQuery.AssignSQLText(SysUtils.Format("UPDATE %s SET %s = %s + 1",
                PSSystemDescription.TimeStampTable!.SQLName, TimeStampTableTimeStampColumn!.SQLName, TimeStampTableTimeStampColumn.SQLName));
            aExecQuery.ExecSQL();
            aQuery.AssignSQLText(SysUtils.Format("SELECT %s FROM %s", TimeStampTableTimeStampColumn.SQLName, PSSystemDescription.TimeStampTable.SQLName));
            aQuery.Open();
            FCurrentTimeStamp = aQuery.Fields(0).AsInteger;
            aQuery.Close();
            if (UseClockLog)
            {
                aQuery.AssignSQLText(SysUtils.Format("SELECT %s, %s FROM %s",
                    LastClockTableLastTimeStampColumn!.SQLName, LastClockTableLastClockColumn!.SQLName, PSSystemDescription.LastClockTable!.SQLName));
                aQuery.Open();
                TBoldTimeStampType LastClockTimestamp = aQuery.Fields(0).AsInteger;
                TDateTime LastClock = aQuery.Fields(1).AsDateTime;
                aQuery.Close();
                TDateTime TheNowValue = GetCorrectTime();
                if ((TheNowValue - LastClock).TotalDays > ClockLogGranularity.ToOADate())
                {
                    aExecQuery.AssignSQLText(SysUtils.Format("UPDATE %s SET %s = :newTimestamp, %s = :newClock",
                        PSSystemDescription.LastClockTable.SQLName, LastClockTableLastTimeStampColumn.SQLName, LastClockTableLastClockColumn.SQLName));
                    aExecQuery.ParamByName("newTimestamp").AsInteger = FCurrentTimeStamp;
                    aExecQuery.ParamByName("newClock").AsDateTime = TheNowValue;
                    aExecQuery.ExecSQL();
                    aExecQuery.AssignSQLText(SysUtils.Format("INSERT INTO %s (%s, %s, %s, %s) VALUES (:lastTimeStamp, :thisTimeStamp, :lastClock, :thisClock)",
                        PSSystemDescription.ClockLogTable!.SQLName, ClockLogTableLastTimeStampColumn!.SQLName, ClockLogTableThisTimeStampColumn!.SQLName,
                        ClockLogTableLastClockColumn!.SQLName, ClockLogTableThisClockColumn!.SQLName));
                    aExecQuery.ParamByName("lastTimeStamp").AsInteger = LastClockTimestamp;
                    aExecQuery.ParamByName("thisTimeStamp").AsInteger = FCurrentTimeStamp;
                    aExecQuery.ParamByName("lastClock").AsDateTime = LastClock;
                    aExecQuery.ParamByName("thisClock").AsDateTime = TheNowValue;
                    aExecQuery.ExecSQL();
                }
            }
        }
        finally
        {
            ReleaseQuery(ref aQuery);
            ReleaseExecQuery(ref aExecQuery);
        }
    }

    /// <summary>Marks the objects found in the X-files table as deleted, with their time stamps.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void FetchDeletedObjects(TBoldObjectIdList ObjectIdList, IBoldValueSpace ValueSpace)
    {
        IBoldQuery? aQuery = GetQuery();
        var IdList = new TStringList();
        var anObjectId = new TBoldDefaultID();
        try
        {
            BoldPMappersDefault.ForEachBlock(ObjectIdList.Count, SQLDataBaseConfig.FetchBlockSize, (Start, Stop) =>
            {
                IdList.Clear();
                for (int I = Start; I <= Stop; I++)
                {
                    IdList.Add(ObjectIdList[I].AsString);
                }

                aQuery.AssignSQLText(SysUtils.Format("SELECT %s, %s, %s FROM %s WHERE %s IN (%s)",
                    BoldDefs.IDCOLUMN_NAME, BoldDefs.TYPECOLUMN_NAME, XFilesTimeStampColumn!.SQLName, PSSystemDescription.XFilestable!.SQLName,
                    BoldDefs.IDCOLUMN_NAME, BoldUtils.BoldSeparateStringList(IdList, ", ", "", "")));
                aQuery.Open();
                while (!aQuery.Eof)
                {
                    anObjectId.AsInteger = aQuery.Fields(0).AsInteger;
                    TBoldTimeStampType aTimeStamp = aQuery.Fields(2).AsInteger;
                    IBoldObjectContents anObjectContents = ValueSpace.EnsuredObjectContentsByObjectId(anObjectId);
                    anObjectContents.TimeStamp = aTimeStamp;
                    anObjectContents.BoldExistenceState = TBoldExistenceState.besDeleted;
                    anObjectContents.BoldPersistenceState = TBoldValuePersistenceState.bvpsCurrent;
                    aQuery.Next();
                }

                aQuery.Close();
            });
        }
        finally
        {
            ReleaseQuery(ref aQuery);
        }
    }

    /// <summary>Reads each class's db type from the mapping, and the db types of its subclasses for class conditions.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InitializeBoldDbType()
    {
        var MissingIds = new TStringList();
        for (int i = 0; i < ObjectPersistenceMappers.Count; i++)
        {
            var ObjectPersistenceMapper = (TBoldObjectDefaultMapper?)ObjectPersistenceMappers[i];
            if (ObjectPersistenceMapper is not null)
            {
                int BoldDbType = MappingInfo.GetDbTypeMapping(ObjectPersistenceMapper.ExpressionName);
                if (BoldDbType == BoldDefs.NO_CLASS)
                {
                    MissingIds.Add(SysUtils.Format(BoldPMConsts.sUnableToFindDBIDForX, ObjectPersistenceMapper.ExpressionName));
                }
                else
                {
                    ObjectPersistenceMapper.BoldDbType = BoldDbType;
                }

                ObjectPersistenceMapper.SubClassesID = SysUtils.IntToStr(ObjectPersistenceMapper.BoldDbType);
                while (ObjectPersistenceMapper.SuperClass is not null)
                {
                    ObjectPersistenceMapper = (TBoldObjectDefaultMapper)ObjectPersistenceMapper.SuperClass;
                    ObjectPersistenceMapper.SubClassesID = ObjectPersistenceMapper.SubClassesID + ", " + SysUtils.IntToStr(BoldDbType);
                }
            }
        }

        if (MissingIds.Count > 0)
        {
            throw new EBold(MissingIds.Text);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override TBoldSQLMappingInfo CreateMappingInfo() =>
        new TBoldDefaultMappingInfo(SQLDataBaseConfig.SystemTablePrefix, SQLDataBaseConfig.MaxDbIdentifierLength, NationalCharConversion);

    /// <summary>Describes Bold's system tables, then (inherited) the class tables.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InitializePSDescriptions()
    {
        const int MappingStringLength = 60;
        int DefaultStringLength = SQLDataBaseConfig.DefaultStringLength;
        TBoldSQLDataBaseConfig cfg = SQLDataBaseConfig;
        TBoldDefaultSystemDescription ps = PSSystemDescription;
        string NotNull = cfg.EffectiveSQLForNotNull;
        string StringType(int Length) => SysUtils.Format(cfg.ColumnTypeForString, Length);

        TBoldSQLTableDescription NewTable(string Name)
        {
            var table = new TBoldSQLTableDescription(ps, false) { SQLName = Name };
            return table;
        }

        // inherited is called at the end to ensure that systemtables are available to other parts of initialization
        ps.IdTable = NewTable(BoldDefs.IDTABLE_NAME);
        ps.IdTable.AddColumn(BoldDefs.IDCOLUMN_NAME, cfg.ColumnTypeForInteger, NotNull, BoldPSDescriptionsSQL.IDCOLUMN_TYPE, 0, false, "");

        ps.TypeTable = NewTable(BoldDefs.TYPETABLE_NAME);
        ps.TypeTable.AddColumn(BoldDefs.TYPECOLUMN_NAME, cfg.ColumnTypeForSmallInt, NotNull, BoldPSDescriptionsSQL.TYPECOLUMN_TYPE, 0, false, "");
        ps.TypeTable.AddColumn(BoldDefs.CLASSNAMECOLUMN_NAME, StringType(DefaultStringLength), NotNull, TFieldType.ftString, DefaultStringLength, false, "");

        if (UseXFiles)
        {
            ps.XFilestable = NewTable(BoldDefs.TABLEPREFIXTAG + "_XFILES");
            ps.XFilestable.AddColumn(BoldDefs.IDCOLUMN_NAME, cfg.ColumnTypeForInteger, NotNull, BoldPSDescriptionsSQL.IDCOLUMN_TYPE, 0, false, "");
            ps.XFilestable.AddColumn(BoldDefs.TYPECOLUMN_NAME, cfg.ColumnTypeForSmallInt, NotNull, BoldPSDescriptionsSQL.TYPECOLUMN_TYPE, 0, false, "");
            ps.XFilestable.EnsureIndex(BoldDefs.IDCOLUMN_NAME, true, true);
            if (UseGlobalId)
            {
                fXFilesGlobalIdColumn = ps.XFilestable.AddColumn(BoldDefs.GLOBALIDCOLUMN_NAME, StringType(DefaultStringLength), NotNull, TFieldType.ftString, DefaultStringLength, false, "");
            }

            if (UseTimestamp)
            {
                fXFilesTimeStampColumn = ps.XFilestable.AddColumn(BoldDefs.TIMESTAMPCOLUMN_NAME, cfg.ColumnTypeForInteger, NotNull, BoldPMappersDefault.BOLDTIMESTAMPFIELDTYPE, 0, false, "");
            }
        }

        ps.Tabletable = NewTable(BoldDefs.TABLETABLE_NAME);
        ps.Tabletable.AddColumn(BoldDefs.TABLENAMECOLUMN_NAME, StringType(DefaultStringLength), NotNull, TFieldType.ftString, DefaultStringLength, false, "");

        if (UseTimestamp)
        {
            ps.TimeStampTable = NewTable(BoldDefs.TIMESTAMPTABLE_NAME);
            fTimeStampTableTimeStampColumn = ps.TimeStampTable.AddColumn(BoldDefs.TIMESTAMPCOLUMN_NAME, cfg.ColumnTypeForInteger, NotNull, TFieldType.ftInteger, 0, false, "");
        }

        if (UseClockLog)
        {
            ps.LastClockTable = NewTable(BoldDefs.LASTCLOCKTABLE_NAME);
            fLastClockTableLastTimeStampColumn = ps.LastClockTable.AddColumn(BoldDefs.LASTTIMESTAMPCOLUMN_NAME, cfg.ColumnTypeForInteger, NotNull, TFieldType.ftInteger, 0, false, "");
            fLastClockTableLastClockColumn = ps.LastClockTable.AddColumn(BoldDefs.LASTCLOCKCOLUMN_NAME, cfg.ColumnTypeForDateTime, NotNull, TFieldType.ftDateTime, 0, false, "");

            ps.ClockLogTable = NewTable(BoldDefs.CLOCKLOGTABLE_NAME);
            fClockLogTableLastTimeStampColumn = ps.ClockLogTable.AddColumn(BoldDefs.LASTTIMESTAMPCOLUMN_NAME, cfg.ColumnTypeForInteger, NotNull, TFieldType.ftInteger, 0, false, "");
            fClockLogTableThisTimeStampColumn = ps.ClockLogTable.AddColumn(BoldDefs.THISTIMESTAMPCOLUMN_NAME, cfg.ColumnTypeForInteger, NotNull, TFieldType.ftInteger, 0, false, "");
            fClockLogTableLastClockColumn = ps.ClockLogTable.AddColumn(BoldDefs.LASTCLOCKCOLUMN_NAME, cfg.ColumnTypeForDateTime, NotNull, TFieldType.ftDateTime, 0, false, "");
            fClockLogTableThisClockColumn = ps.ClockLogTable.AddColumn(BoldDefs.THISCLOCKCOLUMN_NAME, cfg.ColumnTypeForDateTime, NotNull, TFieldType.ftDateTime, 0, false, "");
        }

        ps.MemberMappingTable = NewTable(BoldDefs.MemberMappingTable_NAME);
        foreach (string column in new[] { BoldDefs.MMT_CLASSNAME_COLUMN, BoldDefs.MMT_MEMBERNAME_COLUMN, BoldDefs.MMT_TABLENAME_COLUMN, BoldDefs.MMT_COLUMNS_COLUMN, BoldDefs.MMT_MAPPERNAME_COLUMN })
        {
            ps.MemberMappingTable.AddColumn(column, StringType(MappingStringLength), NotNull, TFieldType.ftString, MappingStringLength, false, "");
        }

        ps.AllInstancesMappingTable = NewTable(BoldDefs.AllInstancesMappingTable_NAME);
        ps.AllInstancesMappingTable.AddColumn(BoldDefs.AID_CLASSNAME_COLUMN, StringType(MappingStringLength), NotNull, TFieldType.ftString, MappingStringLength, false, "");
        ps.AllInstancesMappingTable.AddColumn(BoldDefs.AID_TABLENAME_COLUMN, StringType(MappingStringLength), NotNull, TFieldType.ftString, MappingStringLength, false, "");
        ps.AllInstancesMappingTable.AddColumn(BoldDefs.AID_CLASSIDREQUIRED_COLUMN, cfg.ColumnTypeForInteger, NotNull, TFieldType.ftInteger, 0, false, "");

        ps.ObjectStorageMappingTable = NewTable(BoldDefs.ObjectStorageMappingTable_NAME);
        ps.ObjectStorageMappingTable.AddColumn(BoldDefs.ST_CLASSNAME_COLUMN, StringType(MappingStringLength), NotNull, TFieldType.ftString, MappingStringLength, false, "");
        ps.ObjectStorageMappingTable.AddColumn(BoldDefs.ST_TABLENAME_COLUMN, StringType(MappingStringLength), NotNull, TFieldType.ftString, MappingStringLength, false, "");
        base.InitializePSDescriptions();
    }

    [Delphi(PortStatus.Ported)]
    protected override bool EnsurePrecondition(TBoldUpdatePrecondition Precondition, TBoldIDTranslationList TranslationList)
    {
        if (Precondition is TBoldOptimisticLockingPrecondition locking)
        {
            bool result = EnsureOptimisticLocking(locking, TranslationList);
            // this is an optimization in the case of remote pmappers so that we do not have to stream the valuespace back again.
            // another solutiuon would be to spearate the in and out-parameters better...
            locking.ClearValueSpace();
            return result;
        }

        return true;
    }

    /// <summary>
    /// Compares the objects of the precondition's value space with the database (the loaded members, or existence), and
    /// the time stamps of time-stamped objects; failures are added to the precondition.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    protected bool EnsureOptimisticLocking(TBoldOptimisticLockingPrecondition Precondition, TBoldIDTranslationList TranslationList)
    {
        void MergeFailures(TBoldObjectIdList NewList)
        {
            for (int i = 0; i < NewList.Count; i++)
            {
                Precondition.AddFailedObject(NewList[i]);
            }
        }

        var OldMemberIdList = new TBoldMemberIdList();
        var SingleObjectList = new TBoldObjectIdList();
        var TimeStampedObjects = new TBoldObjectIdList();
        var ObjectIdList = new TBoldObjectIdList();
        var FailureList = new TBoldObjectIdList();
        Precondition.ValueSpace.AllObjectIds(ObjectIdList, true);
        for (int i = 0; i < ObjectIdList.Count; i++)
        {
            IBoldObjectContents? ObjectContents = Precondition.ValueSpace.ObjectContentsByObjectId(ObjectIdList[i]);
            if (ObjectContents is not null)
            {
                OldMemberIdList.Clear();
                if (ObjectContents.TimeStamp != -1)
                {
                    TimeStampedObjects.Add(ObjectIdList[i]);
                }

                for (int MemberIx = 0; MemberIx < ObjectContents.MemberCount; MemberIx++)
                {
                    if (ObjectContents.ValueByIndex(MemberIx) is not null)
                    {
                        OldMemberIdList.Add(new TBoldMemberID(MemberIx));
                    }
                }

                // we must compare the object even if it has no dirty members
                // since it might be a delete and the object in the db might
                // be deleted already. if it is timestamped however, we will
                // detect it more cheap that way
                if (OldMemberIdList.Count > 0 || ObjectContents.TimeStamp == -1)
                {
                    SingleObjectList.Clear();
                    SingleObjectList.Add(ObjectIdList[i]);
                    FailureList.Clear();
                    var ObjectMapper = (TBoldObjectDefaultMapper)ObjectPersistenceMappers[ObjectIdList[i].TopSortedIndex]!;
                    ObjectMapper.PMCompareExactIDList(SingleObjectList, Precondition.ValueSpace, OldMemberIdList, TranslationList, FailureList);
                    MergeFailures(FailureList);
                }
            }
        }

        if (TimeStampedObjects.Count > 0)
        {
            OldMemberIdList.Clear();
            FailureList.Clear();
            OldMemberIdList.Add(new TBoldMemberID(BoldPMappersDefault.TIMESTAMPMEMBERINDEX));
            RootClassObjectPersistenceMapper.PMCompareExactIDList(TimeStampedObjects, Precondition.ValueSpace, OldMemberIdList, TranslationList, FailureList);
            if (FailureList.Count > 0)
            {
                TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
                BoldLog.Log(BoldPMConsts.sOptimisticLockingFailedOnTimeStamp);
                for (int i = 0; i < FailureList.Count; i++)
                {
                    BoldLog.LogFmt(BoldPMConsts.sOptimisticLockFailedLog, [ObjectPersistenceMappers[FailureList[i].TopSortedIndex]!.ExpressionName, FailureList[i].AsString]);
                }

                MergeFailures(FailureList);
            }
        }

        return Precondition.FailureList.Count == 0;
    }

    /// <summary>Fetches by an OCL condition (translated to SQL through the SQL nodes), a change point condition, or inherited.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetchClassWithCondition(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldCondition BoldCondition, int FetchMode,
        TBoldIDTranslationList TranslationList)
    {
        if (BoldCondition is TBoldOclCondition OclCondition)
        {
            void FixQueriesForEnv(TBoldSqlVariableBinding VarBinding, TBoldSQLNameSpace NameSpace)
            {
                if (VarBinding.VariableName == "SELF")
                {
                    VarBinding.NewQuery(NameSpace);
                    TBoldSQLTableReference MainTableRef = VarBinding.TableReferenceForTable(VarBinding.ObjectMapper!.MainTable!, VarBinding.Query!, true);
                    VarBinding.Context = OclCondition.Context;
                    VarBinding.Query!.AddWCF(TBoldSQLWCFBinaryInfix.CreateWCFForIdList(MainTableRef.GetColumnReference(BoldDefs.IDCOLUMN_NAME), OclCondition.Context));
                }
            }

            IBoldQuery? q2 = GetQuery();
            var sql = new TStringList();
            try
            {
                var SQLNodeMaker = new TBoldSqlNodeMaker(OclCondition);
                SQLNodeMaker.Execute();
                var SQlNodeResolver = new TBoldSqlNodeResolver(this, SQLNodeMaker.RootNode!, SQLNodeMaker.SQLVarBindings);
                SQlNodeResolver.Execute();
                var GlobalNameSpace = new TBoldSQLNameSpace();
                var SQlGenerator = new TBoldSqlQueryGenerator(GlobalNameSpace);
                for (int i = 0; i < SQLNodeMaker.SQLVarBindings.Count; i++)
                {
                    SQLNodeMaker.SQLVarBindings[i].AcceptVisitor(SQlGenerator);
                    FixQueriesForEnv((TBoldSqlVariableBinding)SQLNodeMaker.SQLVarBindings[i], GlobalNameSpace);
                }

                SQLNodeMaker.RootNode!.AcceptVisitor(SQlGenerator);
                SQLNodeMaker.RootNode.Query!.GenerateSQL(sql);
                q2.AssignSQL(sql);
                q2.AssignParams(SQLNodeMaker.RootNode.Query.Params);
                BoldCondition.AvailableAnswers = GetListUsingQuery(ObjectIDList, ValueSpace, q2, FetchMode, TranslationList, BoldDefs.BOLDMAXTIMESTAMP,
                    BoldCondition.MaxAnswers, BoldCondition.Offset);
            }
            finally
            {
                ReleaseQuery(ref q2);
            }
        }
        else if (BoldCondition is TBoldChangePointCondition aCPCond)
        {
            var GlobalNameSpace = new TBoldSQLNameSpace();
            TBoldObjectIdList IdList = aCPCond.IdList!;
            for (int i = IdList.Count - 1; i >= 0; i--)
            {
                if (!IdList[i].IsStorable)
                {
                    IdList.RemoveByIndex(i);
                }
            }

            if (IdList.Count > 0)
            {
                IBoldQuery? q2 = GetQuery();
                try
                {
                    GetChangePointsQuery(q2, IdList, aCPCond.StartTime, aCPCond.EndTime, GlobalNameSpace);
                    BoldCondition.AvailableAnswers = GetListUsingQuery(ObjectIDList, ValueSpace, q2, FetchMode, TranslationList, BoldDefs.BOLDINVALIDTIMESTAMP,
                        BoldCondition.MaxAnswers, BoldCondition.Offset);
                    ((TBoldObjectDefaultMapper)CommonSuperClassObjectMapper(IdList)).GetChangePoints(ObjectIDList, aCPCond, GlobalNameSpace);
                }
                finally
                {
                    ReleaseQuery(ref q2);
                }
            }
        }
        else
        {
            base.PMFetchClassWithCondition(ObjectIDList, ValueSpace, BoldCondition, FetchMode, TranslationList);
        }
    }

    /// <summary>
    /// Reads ids (column 0) and db types (column 1) from the opened query into the list and value space, from Offset and
    /// up to MaxAnswers (-1 for all); returns the number of answers available.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public int GetListUsingQuery(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, IBoldQuery aQuery, int FetchMode, TBoldIDTranslationList TranslationList,
        TBoldTimeStampType TimeStamp, int MaxAnswers = -1, int Offset = -1)
    {
        aQuery.Open();
        if (Offset != -1)
        {
            aQuery.MoveBy(Offset);
        }

        // when MaxAnswer = -1 the while-test will never occur and we will get all the answers
        int Counter = MaxAnswers;
        while (!aQuery.Eof && Counter != 0)
        {
            TBoldObjectId ObjectId = TimeStamp == BoldDefs.BOLDINVALIDTIMESTAMP
                ? NewIdFromQuery(aQuery, 1, 0, aQuery.FieldByName(BoldDefs.TIMESTAMPSTARTCOLUMNNAME).AsInteger)
                : NewIdFromQuery(aQuery, 1, 0, TimeStamp);
            ValueSpace.EnsureObjectId(TranslationList.TranslateToNewId(ObjectId)!);
            ObjectIDList.Add(ObjectId);
            TranslationList.AddTranslation(null, ObjectId);
            SendExtendedEvent(BoldSubscription.bpeFetchId, [ObjectId]);
            aQuery.Next();
            Counter--;
        }

        return MaxAnswers < 0 || Counter > 0 ? MaxAnswers - Counter : aQuery.RecordCount;
    }

    /// <summary>The description of a table with its id and type columns (and start time for versioned tables), made when missing.</summary>
    [Delphi(PortStatus.Ported)]
    public override TBoldSQLTableDescription EnsureTable(string TableName, bool TableVersioned)
    {
        TBoldSQLTableDescription? Result = PSSystemDescription.SQLTablesList.ItemsBySQLName(TableName);
        if (Result is null)
        {
            TBoldSQLDataBaseConfig cfg = SQLDataBaseConfig;
            Result = new TBoldSQLTableDescription(PSSystemDescription, TableVersioned) { SQLName = TableName };
            Result.AddColumn(BoldDefs.IDCOLUMN_NAME, cfg.ColumnTypeForInteger, cfg.EffectiveSQLForNotNull, BoldPSDescriptionsSQL.IDCOLUMN_TYPE, 0, false, "");
            Result.AddColumn(BoldDefs.TYPECOLUMN_NAME, cfg.ColumnTypeForSmallInt, cfg.EffectiveSQLForNotNull, BoldPSDescriptionsSQL.TYPECOLUMN_TYPE, 0, false, "");
            if (TableVersioned)
            {
                Result.AddColumn(BoldDefs.TIMESTAMPSTARTCOLUMNNAME, cfg.ColumnTypeForInteger, cfg.EffectiveSQLForNotNull, BoldPMappersDefault.BOLDTIMESTAMPFIELDTYPE, 0, false,
                    cfg.CorrectlyQuotedDefaultValue("0"));
                Result.EnsureIndex(BoldDefs.IDCOLUMN_NAME + ";" + BoldDefs.TIMESTAMPSTARTCOLUMNNAME, true, true);
                // the following two indices improves performance alot in Interbase, and seems to have no negative impact in SQLServer.
                Result.EnsureIndex(BoldDefs.TIMESTAMPSTARTCOLUMNNAME, false, false);
                Result.EnsureIndex(BoldDefs.IDCOLUMN_NAME, false, false);
            }
            else
            {
                Result.EnsureIndex(BoldDefs.IDCOLUMN_NAME, true, true);
            }

            Result.EnsureIndex(BoldDefs.TYPECOLUMN_NAME, false, false);
        }

        return Result;
    }

    /// <summary>The description of a column (in a table ensured first), made or updated.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldSQLColumnDescription EnsureColumn(string TableName, string ColumnName, string SQLType, string SQLAllowNull, TFieldType BDEType, int Length,
        bool AllowNull, bool InVersionedTable, string DefaultDBValue)
    {
        EnsureTable(TableName, InVersionedTable);
        TBoldSQLTableDescription Table = PSSystemDescription.SQLTablesList.ItemsBySQLName(TableName)!;
        var Result = (TBoldSQLColumnDescription?)Table.ColumnsList.ItemsBySQLName(ColumnName);
        if (Result is null)
        {
            return Table.AddColumn(ColumnName, SQLType, SQLAllowNull, BDEType, Length, AllowNull, DefaultDBValue);
        }

        Result.Size = Length;
        Result.Mandatory = !AllowNull;
        Result.SQLType = SQLType;
        Result.FieldType = BDEType;
        Result.SQLAllowNull = SQLAllowNull;
        Result.DefaultDBValue = DefaultDBValue;
        return Result;
    }

    [Delphi(PortStatus.Ported)]
    public void EnsureIndex(string TableName, string Fields, bool PrimaryIndex, bool Unique, bool InVersionedTable)
    {
        EnsureTable(TableName, InVersionedTable);
        PSSystemDescription.SQLTablesList.ItemsBySQLName(TableName)!.EnsureIndex(Fields, PrimaryIndex, Unique);
    }

    /// <summary>Translates object ids to global ids, read from the X-files table.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMTranslateToGlobalIds(TBoldObjectIdList ObjectIdList, TBoldIDTranslationList TranslationList)
    {
        if (ObjectIdList.Count == 0)
        {
            return;
        }

        IBoldQuery? aQuery = GetQuery();
        var IdList = new TStringList();
        try
        {
            BoldPMappersDefault.ForEachBlock(ObjectIdList.Count, SQLDataBaseConfig.FetchBlockSize, (Start, Stop) =>
            {
                IdList.Clear();
                for (int I = Start; I <= Stop; I++)
                {
                    IdList.Add(ObjectIdList[I].AsString);
                }

                aQuery.AssignSQLText(SysUtils.Format("SELECT %s, %s, %s FROM %s WHERE %s IN (%s)",
                    BoldDefs.IDCOLUMN_NAME, BoldDefs.TYPECOLUMN_NAME, XFilesGlobalIdColumn!.SQLName, PSSystemDescription.XFilestable!.SQLName,
                    BoldDefs.IDCOLUMN_NAME, BoldUtils.BoldSeparateStringList(IdList, ", ", "", "")));
                aQuery.Open();
                while (!aQuery.Eof)
                {
                    TranslationList.AddTranslation(NewIdFromQuery(aQuery, 1, 0, BoldDefs.BOLDMAXTIMESTAMP), NewGlobalIdFromQuery(aQuery, 1));
                    aQuery.Next();
                }

                aQuery.Close();
            });
        }
        finally
        {
            ReleaseQuery(ref aQuery);
        }
    }

    /// <summary>Translates global ids to object ids, read from the X-files table.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMTranslateToLocalIds(TBoldObjectIdList GlobalIdList, TBoldIDTranslationList TranslationList)
    {
        if (GlobalIdList.Count == 0)
        {
            return;
        }

        IBoldQuery? aQuery = GetQuery();
        var IdList = new TStringList();
        try
        {
            BoldPMappersDefault.ForEachBlock(GlobalIdList.Count, SQLDataBaseConfig.FetchBlockSize, (Start, Stop) =>
            {
                IdList.Clear();
                for (int I = Start; I <= Stop; I++)
                {
                    IdList.Add(GlobalIdList[I].AsString);
                }

                aQuery.AssignSQLText(SysUtils.Format("SELECT %s, %s, %s FROM %s WHERE %s IN (%s)",
                    XFilesGlobalIdColumn!.SQLName, BoldDefs.IDCOLUMN_NAME, BoldDefs.TYPECOLUMN_NAME, PSSystemDescription.XFilestable!.SQLName,
                    XFilesGlobalIdColumn.SQLName, BoldUtils.BoldSeparateStringList(IdList, "', '", "'", "'")));
                aQuery.Open();
                while (!aQuery.Eof)
                {
                    TBoldObjectId aGlobalId = NewGlobalIdFromQuery(aQuery, 2);
                    TranslationList.AddTranslation(aGlobalId, NewIdFromQuery(aQuery, 2, 1, BoldDefs.BOLDMAXTIMESTAMP));
                    aQuery.Next();
                }

                aQuery.Close();
            });
        }
        finally
        {
            ReleaseQuery(ref aQuery);
        }
    }

    /// <summary>Sets the read-only column of the objects in the root table.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMSetReadonlyness(TBoldObjectIdList ReadOnlyList, TBoldObjectIdList WriteableList)
    {
        void SetReadOnlyTo(TBoldObjectIdList? ObjectIdList, bool ReadOnly)
        {
            if (ObjectIdList is null || ObjectIdList.Count == 0)
            {
                return;
            }

            var IdList = new TStringList();
            IBoldExecQuery? aQuery = GetExecQuery();
            int ReadOnlyValue = ReadOnly ? 1 : 0;
            try
            {
                BoldPMappersDefault.ForEachBlock(ObjectIdList.Count, SQLDataBaseConfig.FetchBlockSize, (Start, Stop) =>
                {
                    IdList.Clear();
                    for (int I = Start; I <= Stop; I++)
                    {
                        IdList.Add(ObjectIdList[I].AsString);
                    }

                    aQuery.AssignSQLText(SysUtils.Format("UPDATE %s SET %s = %d WHERE %s IN (%s)",
                        RootClassObjectPersistenceMapper.MainTable!.SQLName, BoldDefs.READONLYCOLUMN_NAME, ReadOnlyValue, BoldDefs.IDCOLUMN_NAME,
                        BoldUtils.BoldSeparateStringList(IdList, ", ", "", "")));
                    aQuery.ExecSQL();
                });
            }
            finally
            {
                ReleaseExecQuery(ref aQuery);
            }
        }

        SetReadOnlyTo(ReadOnlyList, true);
        SetReadOnlyTo(WriteableList, false);
    }

    /// <summary>The time stamp of a clock time, from the clock log (the maximum when not found).</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMTimestampForTime(TDateTime ClockTime, ref TBoldTimeStampType Timestamp)
    {
        IBoldQuery? aQuery = GetQuery();
        try
        {
            aQuery.AssignSQLText(SysUtils.Format("SELECT %s FROM %s WHERE :time1 > %s and :time2 <= %s",
                ClockLogTableThisTimeStampColumn!.SQLName, PSSystemDescription.ClockLogTable!.SQLName, ClockLogTableLastClockColumn!.SQLName, ClockLogTableThisClockColumn!.SQLName));
            aQuery.ParamByName("time1").AsDateTime = ClockTime;
            aQuery.ParamByName("time2").AsDateTime = ClockTime;
            aQuery.Open();
            Timestamp = !aQuery.Eof ? aQuery.Fields(0).AsInteger : BoldDefs.BOLDMAXTIMESTAMP;
            aQuery.Close();
        }
        finally
        {
            ReleaseQuery(ref aQuery);
        }
    }

    /// <summary>The clock time of a time stamp, from the clock log (now when not found).</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMTimeForTimestamp(TBoldTimeStampType Timestamp, ref TDateTime ClockTime)
    {
        IBoldQuery? aQuery = GetQuery();
        try
        {
            aQuery.AssignSQLText(SysUtils.Format("SELECT %s FROM %s WHERE :time1 >= %s and :time2 < %s",
                ClockLogTableLastClockColumn!.SQLName, PSSystemDescription.ClockLogTable!.SQLName, ClockLogTableLastTimeStampColumn!.SQLName, ClockLogTableThisTimeStampColumn!.SQLName));
            aQuery.ParamByName("time1").AsInteger = Timestamp;
            aQuery.ParamByName("time2").AsInteger = Timestamp;
            aQuery.Open();
            ClockTime = !aQuery.Eof ? aQuery.Fields(0).AsDateTime : GetCorrectTime();
            aQuery.Close();
        }
        finally
        {
            ReleaseQuery(ref aQuery);
        }
    }

    /// <summary>An id from the query's id column, with the class of its db type column (if any), time-stamped unless the maximum.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldObjectId NewIdFromQuery(IBoldQuery aQuery, int BoldDbTypeColumn, int ObjectIdColumn, TBoldTimeStampType Timestamp)
    {
        int TopSortedIndex = BoldDbTypeColumn == -1 ? BoldDefs.NO_CLASS : TopSortedIndexForBoldDbType(aQuery.Fields(BoldDbTypeColumn).AsInteger);
        TBoldDefaultID ObjectId = Timestamp != BoldDefs.BOLDMAXTIMESTAMP
            ? new TBoldTimestampedDefaultId(Timestamp, TopSortedIndex, true)
            : new TBoldDefaultID(TopSortedIndex, true);
        ObjectId.AsInteger = aQuery.Fields(ObjectIdColumn).AsInteger;
        return ObjectId;
    }

    [Delphi(PortStatus.Ported)] public new TBoldDefaultSystemDescription PSSystemDescription => GetPSSystemDescription();
    [Delphi(PortStatus.Ported)] public new TBoldObjectDefaultMapper RootClassObjectPersistenceMapper => GetRootClassObjectPersistenceMapper();
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? XFilesTimeStampColumn => fXFilesTimeStampColumn;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? XFilesGlobalIdColumn => fXFilesGlobalIdColumn;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? LastClockTableLastTimeStampColumn => fLastClockTableLastTimeStampColumn;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? LastClockTableLastClockColumn => fLastClockTableLastClockColumn;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? ClockLogTableLastTimeStampColumn => fClockLogTableLastTimeStampColumn;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? ClockLogTableThisTimeStampColumn => fClockLogTableThisTimeStampColumn;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? ClockLogTableLastClockColumn => fClockLogTableLastClockColumn;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? ClockLogTableThisClockColumn => fClockLogTableThisClockColumn;
    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription? TimeStampTableTimeStampColumn => fTimeStampTableTimeStampColumn;
}
