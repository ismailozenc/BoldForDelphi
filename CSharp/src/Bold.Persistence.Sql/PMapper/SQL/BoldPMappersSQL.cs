// Delphi: Source/PMapper/SQL/BoldPMappersSQL.pas
// Port map: CSharp/port/units/BoldPMappersSQL.md
// The SQL mappers: a system mapper on an IBoldDataBase with mapping information, object mappers over tables, member
// mappers over columns.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappersSQL", Symbol = "TBoldSystemSQLMapper.Destroy", Note = "Freed the mapping information; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappersSQL", Symbol = "TBoldObjectSQLMapper.Destroy", Note = "Freed the table list; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPMappersSQL", Symbol = "TBoldMemberSQLMapper.Destroy", Note = "Freed the column list; the garbage collector does that in .NET.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public delegate void TBoldPreInitializeBoldDbType(TBoldSystemSQLMapper SystemSQLMapper);

[Delphi(PortStatus.Ported)]
public class EBoldDbTypeMissing(string? message = null, Exception? innerException = null) : EBold(message, innerException);

[Delphi(PortStatus.Ported)]
public enum TBoldDBStorageMode { dsmUpdate, dsmCreate }

/// <summary>
/// The system mapper of a SQL database. The database comes from an event; the configuration is copied. The fields are
/// set before the inherited constructor runs (as Delphi does), since the object and member mappers it creates read them.
/// </summary>
[Delphi(PortStatus.Ported)]
[Delphi(PortStatus.Ported, Symbol = "TBoldSystemSQLMapper.CreateFromMold", Note = "The primary constructor.")]
public abstract class TBoldSystemSQLMapper(TMoldModel MoldModel, TBoldTypeNameDictionary TypeNameDictionary, TBoldSQLDataBaseConfig SQLDatabaseConfig,
    TBoldGetDatabaseEvent? GetDatabaseFunc) : TBoldSystemPersistenceMapper(MoldModel, TypeNameDictionary)
{
    private bool fTransactionStartedByMe;
    private TBoldSQLMappingInfo? fMappingInfo;
    private readonly TBoldNationalCharConversion fNationalCharConversion = MoldModel.NationalCharConversion;
    private readonly TBoldSQLDataBaseConfig fSQLDataBaseConfig = CopyConfig(SQLDatabaseConfig);
    private readonly TBoldGetDatabaseEvent? fOnGetDatabase = GetDatabaseFunc;

    private static TBoldSQLDataBaseConfig CopyConfig(TBoldSQLDataBaseConfig Source)
    {
        var result = new TBoldSQLDataBaseConfig();
        result.AssignConfig(Source);
        return result;
    }

    [Delphi(PortStatus.Ported)]
    private IBoldDataBase GetIBoldDataBase()
    {
        if (fOnGetDatabase is null)
        {
            throw new EBoldInternal(SysUtils.Format("%s: No event that provides an IBoldDatabase", GetType().Name));
        }

        return fOnGetDatabase() ?? throw new EBold(SysUtils.Format(BoldPMConsts.sNoDatabase, GetType().Name));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSQLTableDescriptionList GetAllTables() => PSSystemDescription.SQLTablesList;

    [Delphi(PortStatus.Ported)]
    private TBoldSQLSystemDescription GetPSSystemDescription() => (TBoldSQLSystemDescription)base.PSSystemDescription;

    [Delphi(PortStatus.Ported)]
    private TBoldObjectSQLMapper GetRootClassObjectPersistenceMapper() => (TBoldObjectSQLMapper)base.RootClassObjectPersistenceMapper!;

    [Delphi(PortStatus.Ported)]
    private TBoldSQLMappingInfo GetMappingInfo() => fMappingInfo ??= CreateMappingInfo();

    [Delphi(PortStatus.Ported)]
    protected abstract void InitializeBoldDbType();

    [Delphi(PortStatus.Ported)]
    protected override void FillPSParams(TBoldPSParams PSParams) => ((TBoldPSSQLParams)PSParams).Database = Database;

    [Delphi(PortStatus.Ported)]
    protected override TBoldPSParams CreatePSParams()
    {
        TBoldPSParams Result = new TBoldPSSQLParams();
        FillPSParams(Result);
        return Result;
    }

    [Delphi(PortStatus.Ported)]
    protected override void StartTransaction(IBoldValueSpace ValueSpace)
    {
        IBoldDataBase db = Database;
        if (db.IsSqlBased && !db.InTransaction)
        {
            db.StartTransaction();
            fTransactionStartedByMe = true;
        }
        else
        {
            fTransactionStartedByMe = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected override void Commit(IBoldValueSpace ValueSpace)
    {
        if (fTransactionStartedByMe)
        {
            IBoldDataBase db = Database;
            if (db.IsSqlBased && db.InTransaction)
            {
                db.Commit();
            }
        }

        fTransactionStartedByMe = false;
    }

    [Delphi(PortStatus.Ported)]
    protected override void RollBack(IBoldValueSpace ValueSpace)
    {
        if (fTransactionStartedByMe)
        {
            IBoldDataBase db = Database;
            if (db.IsSqlBased && db.InTransaction)
            {
                db.RollBack();
            }
        }

        fTransactionStartedByMe = false;
    }

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldSQLMappingInfo CreateMappingInfo();

    /// <summary>Fetches through the inherited fetch, committing a transaction the fetch started.</summary>
    [Delphi(PortStatus.Ported)]
    public override void PMFetch(TBoldObjectIdList ObjectIDList, IBoldValueSpace ValueSpace, TBoldMemberIdList? MemberIdList, int FetchMode,
        TBoldIDTranslationList TranslationList)
    {
        bool WasInTransaction = Database.InTransaction;
        try
        {
            base.PMFetch(ObjectIDList, ValueSpace, MemberIdList, FetchMode, TranslationList);
        }
        finally
        {
            if (!WasInTransaction && Database.InTransaction)
            {
                Database.Commit();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldDbType BoldDbTypeForTopSortedIndex(int TopSortedIndex) => ((TBoldObjectSQLMapper)ObjectPersistenceMappers[TopSortedIndex]!).BoldDbType;

    [Delphi(PortStatus.Ported)]
    public void CloseDataBase() => Database.Close();

    [Delphi(PortStatus.Ported)]
    protected internal override void CreatePersistentStorage()
    {
        base.CreatePersistentStorage();
        InvokeMemberMappersInitializeSystem();
        MappingInfo.WriteDataToDB(Database);
    }

    [Delphi(PortStatus.Ported)]
    public virtual void GenerateDatabaseScript(TStrings Script, string Separator)
    {
        PSSystemDescription.GenerateDatabaseScript(Script, Separator);
        for (int i = 0; i < ObjectPersistenceMappers.Count; i++)
        {
            ((TBoldObjectSQLMapper?)ObjectPersistenceMappers[i])?.GenerateDatabaseScript(Script, Separator);
        }

        MappingInfo.ScriptForWriteData(Script, Separator, false);
    }

    [Delphi(PortStatus.Ported)]
    public abstract TBoldSQLTableDescription EnsureTable(string TableName, bool TableVersioned);

    [Delphi(PortStatus.Ported)]
    public IBoldQuery GetQuery() => Database.GetQuery();

    [Delphi(PortStatus.Ported)]
    public IBoldExecQuery GetExecQuery() => Database.GetExecQuery();

    /// <summary>Lets one member mapper of each SQL member mapper class initialize the database.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void InvokeMemberMappersInitializeSystem()
    {
        var MemberMapperObjects = new List<TBoldMemberSQLMapper>();
        var MemberMapperTypes = new List<Type>();
        for (int I = 0; I < ObjectPersistenceMappers.Count; I++)
        {
            TBoldObjectPersistenceMapper? om = ObjectPersistenceMappers[I];
            if (om is not null)
            {
                for (int J = 0; J < om.MemberPersistenceMappers.Count; J++)
                {
                    if (om.MemberPersistenceMappers[J] is TBoldMemberSQLMapper Obj && !MemberMapperTypes.Contains(Obj.GetType()))
                    {
                        MemberMapperObjects.Add(Obj);
                        MemberMapperTypes.Add(Obj.GetType());
                    }
                }
            }
        }

        foreach (TBoldMemberSQLMapper mapper in MemberMapperObjects)
        {
            mapper.InitializeSystem(Database);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void OpenDatabase(bool ReadDbTypeFromDB, bool ReadMappingFromDB)
    {
        try
        {
            Database.Open();
            MappingInfo.ReadDataFromDB(Database, ReadDbTypeFromDB, ReadMappingFromDB);
            OnPreInitializeBoldDbType?.Invoke(this); // used for DBStructureValidator
            InitializeBoldDbType();
            EnsurePSDescription();
        }
        catch
        {
            Database.Close();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ReleaseQuery(ref IBoldQuery? aQuery) => Database.ReleaseQuery(ref aQuery);

    [Delphi(PortStatus.Ported)]
    public void ReleaseExecQuery(ref IBoldExecQuery? aQuery) => Database.ReleaseExecQuery(ref aQuery);

    [Delphi(PortStatus.Ported)]
    public int TopSortedIndexForBoldDbType(TBoldDbType BoldDbType)
    {
        for (int i = 0; i < ObjectPersistenceMappers.Count; i++)
        {
            if (ObjectPersistenceMappers[i] is TBoldObjectSQLMapper mapper && mapper.BoldDbType == BoldDbType)
            {
                return i;
            }
        }

        return -1;
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescriptionList AllTables => GetAllTables();
    [Delphi(PortStatus.Ported)] public IBoldDataBase Database => GetIBoldDataBase();
    [Delphi(PortStatus.Ported)] public TBoldPreInitializeBoldDbType? OnPreInitializeBoldDbType { get; set; }
    [Delphi(PortStatus.Ported)] public new TBoldSQLSystemDescription PSSystemDescription => GetPSSystemDescription();
    [Delphi(PortStatus.Ported)] public new TBoldObjectSQLMapper RootClassObjectPersistenceMapper => GetRootClassObjectPersistenceMapper();
    [Delphi(PortStatus.Ported)] public TBoldSQLMappingInfo MappingInfo => GetMappingInfo();
    [Delphi(PortStatus.Ported)] public TBoldNationalCharConversion NationalCharConversion => fNationalCharConversion;
    [Delphi(PortStatus.Ported)] public TBoldSQLDataBaseConfig SQLDataBaseConfig => fSQLDataBaseConfig;
}

/// <summary>The mapper of a class stored in SQL tables: its main table and all the tables its members use.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldObjectSQLMapper : TBoldObjectPersistenceMapper
{
    private TBoldDbType fBoldDbType = -1;
    private TBoldSQLTableDescriptionList? fAllTables;
    private TBoldSQLTableDescription? fMainTable;

    [Delphi(PortStatus.Ported, Symbol = "TBoldObjectSQLMapper.CreateFromMold")]
    protected TBoldObjectSQLMapper(TMoldClass MoldClass, TBoldSystemPersistenceMapper Owner, TBoldTypeNameDictionary TypeNameDictionary)
        : base(MoldClass, Owner, TypeNameDictionary)
    {
        System.Diagnostics.Debug.Assert(base.SystemPersistenceMapper is TBoldSystemSQLMapper);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystemSQLMapper GetSystemPersistenceMapper() => (TBoldSystemSQLMapper)base.SystemPersistenceMapper;

    [Delphi(PortStatus.Ported)]
    private TBoldSQLTableDescriptionList GetAllTables() =>
        fAllTables ??= new TBoldSQLTableDescriptionList(SystemPersistenceMapper.PSSystemDescription) { OwnsEntries = false };

    [Delphi(PortStatus.Ported)]
    protected void SQLForMembers(TBoldSQLTableDescription? Table, TStrings SQL, TBoldMemberPersistenceMapperList MemberList, TBoldSQLStyle SQLStyle, bool IncludeKey,
        bool StoredInObjectOnly, bool useAlias)
    {
        if (SQLStyle is TBoldSQLStyle.ssColumns or TBoldSQLStyle.ssParameters && IncludeKey)
        {
            SQLForKey(Table ?? MainTable!, SQL, SQLStyle, useAlias);
        }

        for (int m = 0; m < MemberList.Count; m++)
        {
            if (MemberList[m] is { } member && (!StoredInObjectOnly || member.IsStoredInObject))
            {
                var sqlMember = (TBoldMemberSQLMapper)member;
                for (int c = 0; c < sqlMember.ColumnDescriptions.Count; c++)
                {
                    var Column = (TBoldSQLColumnDescription)sqlMember.ColumnDescriptions[c];
                    if (Table is null || Column.TableDescription == Table)
                    {
                        if (Table is not null)
                        {
                            switch (SQLStyle)
                            {
                                case TBoldSQLStyle.ssColumns: SQL.Add(Column.SQLName); break;
                                case TBoldSQLStyle.ssParameters: SQL.Add(SysUtils.Format(":%s", Column.SQLName)); break;
                                case TBoldSQLStyle.ssValues: SQL.Add(SysUtils.Format("%s = :%s", Column.SQLName, Column.SQLName)); break;
                                default: throw new EBold(BoldPMConsts.sUnimplemented);
                            }
                        }
                        else if (SQLStyle == TBoldSQLStyle.ssColumns)
                        {
                            SQL.Add(SysUtils.Format("%s.%s", TableAlias(Column.TableDescription, useAlias), Column.SQLName));
                        }
                        else
                        {
                            throw new EBold(BoldPMConsts.sUnimplemented);
                        }
                    }
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    protected abstract void SQLForKey(TBoldSQLTableDescription Table, TStrings SQL, TBoldSQLStyle SQLStyle, bool useAlias);

    [Delphi(PortStatus.Ported)]
    protected abstract void SQLForDistributed(TStrings SQL, TBoldSQLStyle SQLStyle);

    [Delphi(PortStatus.Ported)]
    protected abstract void JoinSQLTableByKey(TStringList SQL, TBoldSQLTableDescription MainTable, TBoldSQLTableDescription JoinTable);

    [Delphi(PortStatus.Ported)]
    protected abstract void SQLForID(TBoldSQLTableDescription Table, TStrings SQL, bool useAlias);

    /// <summary>The alias of a table in a select: X_FILES for the distributable table, else letters from its position.</summary>
    [Delphi(PortStatus.Ported)]
    protected string TableAlias(TBoldSQLTableDescription Table, bool useAlias)
    {
        string result = "";
        if (useAlias)
        {
            if (Table == DistributableTable())
            {
                result = "X_FILES";
            }
            else
            {
                int i = AllTables.IndexOf(Table) + 1;
                while (i > 0)
                {
                    result += (char)(65 + (i % 25));
                    i /= 25;
                }

                while (AllTables.ItemsBySQLName(result) is not null)
                {
                    result += "_";
                }
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected abstract TBoldSQLTableDescription? DistributableTable();

    /// <summary>Lists the tables of the class: those of the object storage mapping (or the superclasses' main tables), then the main table.</summary>
    [Delphi(PortStatus.Ported)]
    protected internal override void InitializePSDescriptions()
    {
        void RecurseSuperMappers(TBoldObjectSQLMapper Mapper)
        {
            // we want the superclass tables to come before the subclass tables, so the recursion is done first.
            if (Mapper.SuperClass is not null)
            {
                RecurseSuperMappers((TBoldObjectSQLMapper)Mapper.SuperClass);
            }

            if (Mapper.MainTable is not null && AllTables.IndexOf(Mapper.MainTable) == -1)
            {
                AllTables.Add(Mapper.MainTable);
            }
        }

        base.InitializePSDescriptions();
        TBoldObjectStorageMappingInfo[] OSArr = SystemPersistenceMapper.MappingInfo.GetObjectStorageMapping(ExpressionName);
        if (OSArr.Length == 0)
        {
            if (SuperClass is not null)
            {
                RecurseSuperMappers((TBoldObjectSQLMapper)SuperClass);
            }
        }
        else
        {
            foreach (TBoldObjectStorageMappingInfo os in OSArr)
            {
                AllTables.Add(SystemPersistenceMapper.EnsureTable(os.TableName, Versioned));
            }
        }

        TBoldAllInstancesMappingInfo[] AIArr = SystemPersistenceMapper.MappingInfo.GetAllInstancesMapping(ExpressionName);
        if (AIArr.Length == 1)
        {
            fMainTable = SystemPersistenceMapper.EnsureTable(AIArr[0].TableName, Versioned);
        }

        if (MainTable is not null && AllTables.IndexOf(MainTable) == -1)
        {
            AllTables.Add(MainTable);
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void GenerateDatabaseScript(TStrings Script, string Separator)
    {
        for (int i = 0; i < MemberPersistenceMappers.Count; i++)
        {
            ((TBoldMemberSQLMapper?)MemberPersistenceMappers[i])?.GenerateDatabaseScript(Script, Separator);
        }
    }

    [Delphi(PortStatus.Ported)]
    public abstract bool UpdatesMembersInTable(TBoldSQLTableDescription aTable);

    /// <summary>The select of the members' columns from the main table joined with the other tables they are in.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void RetrieveSelectStatement(TStrings s, TBoldMemberPersistenceMapperList MemberMapperList, int FetchMode, bool ForceRootTable)
    {
        var SelectList = new TStringList();
        var FromList = new TStringList();
        var WhereList = new TStringList();
        var JoinList = new TStringList();
        var TableList = new TBoldSQLTableDescriptionList(SystemPersistenceMapper.PSSystemDescription) { OwnsEntries = false };
        if (ForceRootTable)
        {
            TableList.Add(SystemPersistenceMapper.RootClassObjectPersistenceMapper.MainTable);
        }

        for (int MapperIx = 0; MapperIx < MemberMapperList.Count; MapperIx++)
        {
            var Mapper = (TBoldMemberSQLMapper)MemberMapperList[MapperIx]!;
            for (int ColumnIx = 0; ColumnIx < Mapper.ColumnDescriptions.Count; ColumnIx++)
            {
                TBoldSQLTableDescription Table = ((TBoldSQLColumnDescription)Mapper.ColumnDescriptions[ColumnIx]).TableDescription;
                if (TableList.IndexOf(Table) == -1)
                {
                    TableList.Add(Table);
                }
            }
        }

        SQLForMembers(null, SelectList, MemberMapperList, TBoldSQLStyle.ssColumns, true, false, true);
        FromList.Add(SysUtils.Format("%s %s", MainTable!.SQLName, TableAlias(MainTable, true)));
        bool sql92 = SystemPersistenceMapper.SQLDataBaseConfig.UseSQL92Joins && FetchMode == BoldDefs.fmNormal;
        if (sql92)
        {
            // left join rateable x on Song.Bold_ID=rateable.Bold_id
            for (int T = 0; T < TableList.Count; T++)
            {
                if (TableList[T].ColumnsList.Count > 2 && TableList[T] != MainTable)
                {
                    string Join = SysUtils.Format("left join %s %s on ", TableList[T].SQLName, TableAlias(TableList[T], true));
                    JoinList.Clear();
                    JoinSQLTableByKey(JoinList, MainTable, TableList[T]);
                    Join += BoldUtils.BoldSeparateStringList(JoinList, " and ", "(", ")");
                    FromList.Add(Join);
                }
            }
        }
        else
        {
            for (int T = 0; T < TableList.Count; T++)
            {
                if (TableList[T].ColumnsList.Count > 2 && TableList[T] != MainTable)
                {
                    FromList.Add(SysUtils.Format("%s %s", TableList[T].SQLName, TableAlias(TableList[T], true)));
                }
            }

            for (int T = 0; T < TableList.Count; T++)
            {
                if (TableList[T].ColumnsList.Count > 2 && TableList[T] != MainTable)
                {
                    JoinSQLTableByKey(WhereList, MainTable, TableList[T]);
                }
            }
        }

        SQLForID(MainTable, WhereList, true);
        BoldUtils.BoldAppendToStrings(s, SysUtils.Format("SELECT %s ", BoldUtils.BoldSeparateStringList(SelectList, ", ", "", "")), true);
        BoldUtils.BoldAppendToStrings(s, SysUtils.Format("FROM %s ", BoldUtils.BoldSeparateStringList(FromList, sql92 ? " " : ", ", "", "")), true);
        BoldUtils.BoldAppendToStrings(s, SysUtils.Format("WHERE %s ", BoldUtils.BoldSeparateStringList(WhereList, " AND ", "", "")), true);
    }

    /// <summary>The condition on the time stamp columns that selects the versions valid at TimeStamp.</summary>
    [Delphi(PortStatus.Ported)]
    public void RetrieveTimeStampCondition(TStrings SQL, TBoldTimeStampType TimeStamp, bool UseAlias, string WhereToken, bool UseOwnTableForStartTime,
        string SuggestedStartTimeAlias = "", string SuggestedEndTimeAlias = "")
    {
        TBoldSQLTableDescription EndTimeTable = SystemPersistenceMapper.RootClassObjectPersistenceMapper.MainTable!;
        TBoldSQLTableDescription StartTimeTable = UseOwnTableForStartTime ? MainTable! : EndTimeTable;
        string StartTimeTableAlias;
        string EndTimeTableAlias;
        if (UseAlias)
        {
            StartTimeTableAlias = TableAlias(StartTimeTable, true);
            EndTimeTableAlias = TableAlias(EndTimeTable, true);
        }
        else
        {
            StartTimeTableAlias = SuggestedStartTimeAlias.Length != 0 ? SuggestedStartTimeAlias : StartTimeTable.SQLName;
            EndTimeTableAlias = SuggestedEndTimeAlias.Length != 0 ? SuggestedEndTimeAlias : EndTimeTable.SQLName;
        }

        if (TimeStamp == BoldDefs.BOLDMAXTIMESTAMP)
        {
            SQL.Add(SysUtils.Format("%s (%s.%s = %d)", WhereToken, EndTimeTableAlias, BoldDefs.TIMESTAMPSTOPCOLUMNNAME, TimeStamp));
        }
        else
        {
            SQL.Add(SysUtils.Format("%s ((%s.%s <= %d) and (%s.%s >= %d))", WhereToken,
                StartTimeTableAlias, BoldDefs.TIMESTAMPSTARTCOLUMNNAME, TimeStamp, EndTimeTableAlias, BoldDefs.TIMESTAMPSTOPCOLUMNNAME, TimeStamp));
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ValuesFromFieldsByMemberList(TBoldObjectId ObjectId, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList, IBoldDataSet DataSet,
        TBoldMemberPersistenceMapperList MemberList)
    {
        IBoldObjectContents ObjectContents = ValueSpace.EnsuredObjectContentsByObjectId(ObjectId);
        for (int i = 0; i < MemberList.Count; i++)
        {
            ((TBoldMemberSQLMapper)MemberList[i]!).ValueFromQuery(ObjectId, ObjectContents, ValueSpace, TranslationList, DataSet);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void ValuesToParamsByMemberList(TBoldObjectId ObjectId, IBoldValueSpace ValueSpace, IBoldExecQuery Query, TBoldMemberPersistenceMapperList memberList,
        TBoldIDTranslationList TranslationList, TBoldDBStorageMode DBStorageMode)
    {
        for (int i = 0; i < memberList.Count; i++)
        {
            ((TBoldMemberSQLMapper)memberList[i]!).ValueToQuery(ValueSpace.ObjectContentsByObjectId(ObjectId)!, ValueSpace, Query, TranslationList, DBStorageMode);
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescriptionList AllTables => GetAllTables();
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? MainTable => fMainTable;
    [Delphi(PortStatus.Ported)] public TBoldDbType BoldDbType { get => fBoldDbType; set => fBoldDbType = value; }
    [Delphi(PortStatus.Ported)] public new TBoldSystemSQLMapper SystemPersistenceMapper => GetSystemPersistenceMapper();
}

/// <summary>The mapper of a member stored in columns: their names, types, sizes and nullability, and the value transfer.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldMemberSQLMapper : TBoldMemberPersistenceMapper
{
    private TBoldSQLDescriptionList? fColumnDescriptions;
    protected bool fAllowNull;
    protected string fInitialColumnRootName = "";
    protected string fDefaultDbValue = "";

    [Delphi(PortStatus.Ported, Symbol = "TBoldMemberSQLMapper.CreateFromMold")]
    protected TBoldMemberSQLMapper(TMoldMember? MoldMember, TMoldClass MoldClass, TBoldObjectPersistenceMapper Owner, int MemberIndex, TBoldTypeNameDictionary TypeNameDictionary)
        : base(MoldMember, MoldClass, Owner, MemberIndex, TypeNameDictionary)
    {
        if (MoldMember is null)
        {
            return;
        }

        fInitialColumnRootName = BoldNameExpander.BoldExpandName(MoldMember.ColumnName, MoldMember.Name, TExpansionType.xtSQL,
            SystemPersistenceMapper.SQLDataBaseConfig.MaxDbIdentifierLength, MoldClass.Model.NationalCharConversion);
        if (MoldMember is TMoldAttribute attribute)
        {
            fDefaultDbValue = attribute.DefaultDBValue;
        }

        if (fDefaultDbValue.Length == 0)
        {
            fDefaultDbValue = DefaultDefaultDbValue();
        }

        fAllowNull = MoldMember is not TMoldAttribute a || a.AllowNull;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSystemSQLMapper GetSystemPersistenceMapper() => ObjectPersistenceMapper.SystemPersistenceMapper;

    [Delphi(PortStatus.Ported)]
    private TBoldObjectSQLMapper GetObjectPersistenceMapper() => (TBoldObjectSQLMapper)base.ObjectPersistenceMapper;

    [Delphi(PortStatus.Ported)]
    private TBoldSQLDescriptionList GetColumnDescriptions() =>
        fColumnDescriptions ??= new TBoldSQLDescriptionList(SystemPersistenceMapper.PSSystemDescription) { OwnsEntries = false };

    [Delphi(PortStatus.Ported)] protected abstract string GetColumnTypeAsSQL(int ColumnIndex);
    [Delphi(PortStatus.Ported)] protected abstract string GetAllowNullAsSQL();
    [Delphi(PortStatus.Ported)] protected abstract int GetColumnCount();

    [Delphi(PortStatus.Ported)]
    protected virtual int GetColumnSize(int ColumnIndex) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "GetColumnSize", ColumnIndex));

    [Delphi(PortStatus.Ported)]
    protected virtual string GetInitialColumnName(int ColumnIndex) =>
        ColumnIndex > 0 ? SysUtils.Format("%s_%d", InitialColumnRootName, ColumnIndex) : InitialColumnRootName;

    [Delphi(PortStatus.Ported)]
    protected virtual string DefaultDefaultDbValue() => "";

    [Delphi(PortStatus.Ported)] protected string InitialColumnRootName => fInitialColumnRootName;

    [Delphi(PortStatus.Ported)]
    protected virtual bool GetRequiresLiveQuery() => false;

    [Delphi(PortStatus.Ported)]
    protected void SetParamToNullWithDataType(IBoldParameter aParam, TFieldType FieldType)
    {
        aParam.Clear();
        if (aParam.DataType != FieldType)
        {
            aParam.DataType = FieldType;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected virtual TFieldType GetColumnBDEFieldType(int ColumnIndex) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sIllegalColumnIndex, GetType().Name, "GetColumnBDEFieldType", ColumnIndex));

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    protected string InitialColumnName(int Columnindex) => GetInitialColumnName(Columnindex);

    [Delphi(PortStatus.Ported)]
    public virtual void GenerateDatabaseScript(TStrings Script, string Separator)
    {
        // intentionally left blank
    }

    [Delphi(PortStatus.Ported)]
    public virtual void ValueToParam(IBoldObjectContents ObjectContent, IBoldParameter Param, int ColumnIndex, TBoldIDTranslationList TranslationList) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sIllegalCall, GetType().Name, "ValueToParam"));

    [Delphi(PortStatus.Ported)]
    public virtual void ValueFromField(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldField Field, int ColumnIndex) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sIllegalCall, GetType().Name, "ValueFromField"));

    /// <summary>Puts the member's value in its columns' parameters, when creating or when the member is dirty.</summary>
    [Delphi(PortStatus.Ported)]
    public void ValueToQuery(IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, IBoldExecQuery Query, TBoldIDTranslationList TranslationList,
        TBoldDBStorageMode DBStorageMode)
    {
        if (DBStorageMode == TBoldDBStorageMode.dsmCreate || IsDirty(ObjectContent))
        {
            for (int ColumnIndex = 0; ColumnIndex < ColumnCount; ColumnIndex++)
            {
                IBoldParameter? aParam = Query.ParamByName(ColumnDescriptions[ColumnIndex].SQLName);
                if (aParam is not null)
                {
                    ValueToParam(ObjectContent, aParam, ColumnIndex, TranslationList);
                }
                else
                {
                    throw new EBoldInternal(SysUtils.Format(BoldPMConsts.sSomeColumnsNotInTable, GetType().Name, "ValueToQuery", ColumnIndex, ColumnDescriptions[ColumnIndex].SQLName));
                }
            }
        }
    }

    /// <summary>Reads the member's value from its columns' fields, when it should be fetched.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void ValueFromQuery(TBoldObjectId OwningObjectId, IBoldObjectContents ObjectContent, IBoldValueSpace ValueSpace, TBoldIDTranslationList TranslationList,
        IBoldDataSet DataSet)
    {
        if (ShouldFetch(ObjectContent))
        {
            for (int ColumnIndex = 0; ColumnIndex < ColumnCount; ColumnIndex++)
            {
                IBoldField? aField = DataSet.FieldByName(ColumnDescriptions[ColumnIndex].SQLName);
                if (aField is not null)
                {
                    ValueFromField(OwningObjectId, ObjectContent, ValueSpace, TranslationList, aField, ColumnIndex);
                }
                else
                {
                    throw new EBoldInternal(SysUtils.Format(BoldPMConsts.sSomeColumnsNotInTable, GetType().Name, "ValueFromQuery", ColumnIndex, ColumnDescriptions[ColumnIndex].SQLName));
                }
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void InitializeSystem(IBoldDataBase theDatabase)
    {
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLDescriptionList ColumnDescriptions => GetColumnDescriptions();
    [Delphi(PortStatus.Ported)] public string AllowNullAsSQL => GetAllowNullAsSQL();
    [Delphi(PortStatus.Ported)] public int ColumnCount => GetColumnCount();

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public string ColumnTypeAsSQL(int Columnindex) => GetColumnTypeAsSQL(Columnindex);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public int ColumnSize(int Columnindex) => GetColumnSize(Columnindex);

    [Delphi(PortStatus.Ported)] public bool AllowNull => fAllowNull;

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TFieldType ColumnBDEFieldType(int Columnindex) => GetColumnBDEFieldType(Columnindex);

    [Delphi(PortStatus.Ported)] public bool RequiresLiveQuery => GetRequiresLiveQuery();
    [Delphi(PortStatus.Ported)] public TBoldSystemSQLMapper SystemPersistenceMapper => GetSystemPersistenceMapper();
    [Delphi(PortStatus.Ported)] public new TBoldObjectSQLMapper ObjectPersistenceMapper => GetObjectPersistenceMapper();
    [Delphi(PortStatus.Ported)] public string DefaultDbValue { get => fDefaultDbValue; set => fDefaultDbValue = value; }
}
