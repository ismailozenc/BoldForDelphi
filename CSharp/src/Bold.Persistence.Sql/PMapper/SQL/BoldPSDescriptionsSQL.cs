// Delphi: Source/PMapper/SQL/BoldPSDescriptionsSQL.pas
// Port map: CSharp/port/units/BoldPSDescriptionsSQL.md
// The description of a SQL database (tables, columns and indexes), which creates and cleans the database.
// Delphi asks the user with dialogs before deleting tables; here the questions go to replaceable callbacks
// (BoldPSDescriptionsSQL.QueryUserHandler and ConfirmCleanHandler), so the persistence layer has no UI.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPSDescriptionsSQL", Symbol = "TBoldSQLSystemDescription.Destroy", Note = "Freed the lists; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldPSDescriptionsSQL", Symbol = "TBoldSQLTableDescription.Destroy", Note = "Freed the lists; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>
/// The answers of BoldQueryUserDlg's question to the user. Declared here, unmarked, so the persistence layer can ask
/// without the WinForms dialog; the WinForms port of BoldQueryUserDlg (M8) uses this type.
/// </summary>
public enum TBoldQueryResult { qrYesAll, qrYes, qrNo, qrNoAll }

/// <summary>Unit-level members of BoldPSDescriptionsSQL.</summary>
public static partial class BoldPSDescriptionsSQL
{
    [Delphi(PortStatus.Ported)] public const TFieldType IDCOLUMN_TYPE = TFieldType.ftInteger;
    [Delphi(PortStatus.Ported)] public const TFieldType TYPECOLUMN_TYPE = TFieldType.ftSmallint;

    /// <summary>When true, the Bold tables of a database are deleted without asking.</summary>
    [Delphi(PortStatus.Ported)]
    public static bool BoldCleanDatabaseForced { get; set; }

    /// <summary>
    /// Asks whether to delete a table that is not Bold's (title, question). Delphi shows BoldQueryUserDlg; without a
    /// handler the answer is qrNoAll, so unknown tables are kept.
    /// </summary>
    public static Func<string, string, TBoldQueryResult>? QueryUserHandler { get; set; }

    /// <summary>
    /// Asks whether to delete the Bold tables of an existing database (the question). Delphi shows a message dialog;
    /// without a handler the answer is no, so set <see cref="BoldCleanDatabaseForced"/> to clean without asking.
    /// </summary>
    public static Func<string, bool>? ConfirmCleanHandler { get; set; }

    internal static int IX_SQLDescriptionSQLName = -1;
    internal static int IX_SQLIndexFields = -1;
}

/// <summary>The description of a SQL database: its tables, configuration and character conversion.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLSystemDescription : TBoldPSSystemDescription
{
    private readonly TBoldSQLTableDescriptionList fTables;
    private readonly TBoldSQLDataBaseConfig fSQLDatabaseConfig;
    private readonly TBoldNationalCharConversion fNationalCharConversion;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLSystemDescription(TBoldPSDescriptionElement? aOwner, TBoldSQLDataBaseConfig SQLDatabaseConfig, TBoldNationalCharConversion NationalCharConversion)
        : base(aOwner)
    {
        fTables = new TBoldSQLTableDescriptionList(this);
        fSQLDatabaseConfig = new TBoldSQLDataBaseConfig();
        fSQLDatabaseConfig.AssignConfig(SQLDatabaseConfig);
        fNationalCharConversion = NationalCharConversion;
    }

    [Delphi(PortStatus.Ported, Note = "Delphi's message dialog is the ConfirmCleanHandler callback.")]
    private bool ContinueClearPS(TStrings TableNameList)
    {
        bool Result = true;
        if (TableNameList.IndexOf(BoldNameExpander.BoldExpandPrefix(BoldDefs.IDTABLE_NAME, "", SQLDatabaseConfig.SystemTablePrefix, SQLDatabaseConfig.MaxDbIdentifierLength, NationalCharConversion)) > -1)
        {
            Result = BoldPSDescriptionsSQL.BoldCleanDatabaseForced ||
                (BoldPSDescriptionsSQL.ConfirmCleanHandler?.Invoke(BoldPMConsts.sContinueDeleteBoldTables) ?? false);
        }

        return Result;
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void InitializeKnownSystemtables(TStrings KnownTables, TBoldPSSQLParams PSParams)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected internal TBoldDatabaseGenerationMode EffectiveGenerationMode(TBoldPSSQLParams PSParams)
    {
        TBoldDatabaseGenerationMode result = SQLDatabaseConfig.DBGenerationMode;
        if (!PSParams.Database!.SupportsTableCreation())
        {
            result = TBoldDatabaseGenerationMode.dbgQuery;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    protected bool EffectiveUseTransactions(TBoldPSSQLParams PSParams) => SQLDatabaseConfig.AllowMetadataChangesInTransaction;

    [Delphi(PortStatus.Ported)]
    protected void StartMetaDataTransaction(TBoldPSSQLParams PSParams)
    {
        if (EffectiveUseTransactions(PSParams))
        {
            PSParams.Database!.StartTransaction();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void RollBackMetaDataTransaction(TBoldPSSQLParams PSParams)
    {
        if (PSParams.Database!.InTransaction)
        {
            BoldLogHandler.BoldLog().Log(BoldPMConsts.sRollingBackDB);
            PSParams.Database.RollBack();
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void CommitMetaDataTransaction(TBoldPSSQLParams PSParams)
    {
        if (PSParams.Database!.InTransaction)
        {
            BoldLogHandler.BoldLog().Log(BoldPMConsts.sCommittingToDB);
            PSParams.Database.Commit();
        }
    }

    [Delphi(PortStatus.Ported, Note = "Delphi's query dialog is the QueryUserHandler callback.")]
    public void CleanPersistentStorage(TBoldPSSQLParams PSParams)
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        IBoldDataBase Database = PSParams.Database!;
        BoldLog.LogHeader = BoldPMConsts.sCleaningPS;
        TBoldQueryResult Query = TBoldQueryResult.qrYes;
        var TableNameList = new TStringList();
        var Knowntables = new TStringList();
        int i = 0;

        bool DeleteTableBDE()
        {
            try
            {
                IBoldTable? Table = Database.GetTable();
                try
                {
                    Table.Tablename = TableNameList[i];
                    Table.Exclusive = true;
                    Table.DeleteTable();
                    return true;
                }
                finally
                {
                    Database.ReleaseTable(ref Table);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        bool DeleteTableSQL()
        {
            IBoldExecQuery? ExecQuery = Database.GetExecQuery();
            try
            {
                ExecQuery.AssignSQLText("DROP TABLE " + TableNameList[i]);
                ExecQuery.ExecSQL();
            }
            finally
            {
                Database.ReleaseExecQuery(ref ExecQuery);
            }

            return true;
        }

        bool DeleteTable()
        {
            bool result = false;
            bool IsBoldTable = Knowntables.IndexOf(TableNameList[i]) != -1;
            if (!IsBoldTable && !PSParams.IgnoreUnknownTables && Query is not (TBoldQueryResult.qrYesAll or TBoldQueryResult.qrNoAll))
            {
                Query = BoldPSDescriptionsSQL.QueryUserHandler?.Invoke(BoldPMConsts.sDeleteTable, SysUtils.Format(BoldPMConsts.sDeleteNonBoldTable, TableNameList[i]))
                    ?? TBoldQueryResult.qrNoAll;
            }

            bool MayDelete = Query is TBoldQueryResult.qrYes or TBoldQueryResult.qrYesAll && !PSParams.IgnoreUnknownTables;
            if (IsBoldTable || MayDelete)
            {
                result = EffectiveGenerationMode(PSParams) switch
                {
                    TBoldDatabaseGenerationMode.dbgTable => DeleteTableBDE(),
                    TBoldDatabaseGenerationMode.dbgQuery => DeleteTableSQL(),
                    _ => false,
                };
            }

            return result;
        }

        InitializeKnownSystemtables(Knowntables, PSParams);

        // this is needed in ADO since the cached table seems to keep the BOLD_TABLEs table open even though it has been closed...
        Database.ReleaseCachedObjects();
        StartMetaDataTransaction(PSParams);
        try
        {
            Database.AllTableNames("*", false, TableNameList);
            if (!ContinueClearPS(TableNameList))
            {
                throw new EBold(BoldPMConsts.sCleaningPSAborted);
            }

            BoldLog.ProgressMax = TableNameList.Count - 1;
            for (i = 0; i < TableNameList.Count; i++)
            {
                BoldLog.Progress = i;
                try
                {
                    if (DeleteTable())
                    {
                        BoldLog.LogFmt(BoldPMConsts.sDeletingTableX, [TableNameList[i]]);
                    }
                    else
                    {
                        BoldLog.LogFmt(BoldPMConsts.sKeepingTableX, [TableNameList[i]]);
                    }
                }
                catch (Exception e)
                {
                    BoldLog.LogFmt(BoldPMConsts.sErrorDeletingTable, [TableNameList[i], e.Message], TBoldLogType.ltError);
                }

                BoldLog.Sync();
                if (BoldLog.ProcessInterruption())
                {
                    return;
                }
            }

            CommitMetaDataTransaction(PSParams);
        }
        catch
        {
            RollBackMetaDataTransaction(PSParams);
            throw;
        }

        BoldLog.Separator();
    }

    [Delphi(PortStatus.Ported)]
    public override void CreatePersistentStorage(TBoldPSParams PSParams)
    {
        if (PSParams is not TBoldPSSQLParams PSParamsSQL)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sUnknownPSParamsType, GetType().Name, PSParams.GetType().Name));
        }

        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        CleanPersistentStorage(PSParamsSQL);
        StartMetaDataTransaction(PSParamsSQL);
        try
        {
            if (BoldLog.ProcessInterruption())
            {
                return;
            }

            BoldLog.LogHeader = BoldPMConsts.sCreatingTables;
            BoldLog.ProgressMax = SQLTablesList.Count - 1;
            for (int i = 0; i < SQLTablesList.Count; i++)
            {
                BoldLog.Progress = i;
                SQLTablesList[i].CreateTable(PSParamsSQL);
                BoldLog.Sync();
                if (BoldLog.ProcessInterruption())
                {
                    return;
                }
            }

            CommitMetaDataTransaction(PSParamsSQL);
        }
        catch
        {
            RollBackMetaDataTransaction(PSParamsSQL);
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    public virtual void GenerateDatabaseScript(TStrings Script, string Separator)
    {
        for (int i = 0; i < SQLTablesList.Count; i++)
        {
            SQLTablesList[i].GenerateDatabaseScript(Script, Separator);
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLDataBaseConfig SQLDatabaseConfig => fSQLDatabaseConfig;
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescriptionList SQLTablesList => fTables;
    [Delphi(PortStatus.Ported)] public TBoldNationalCharConversion NationalCharConversion => fNationalCharConversion;
}

/// <summary>An element with a SQL name, expanded with the system's table prefix.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLDescriptionElement : TBoldPSDescriptionElement
{
    private string fSQLName = "";

    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldSQLDescriptionElement(TBoldPSDescriptionElement? aOwner) : base(aOwner)
    {
    }

    [Delphi(PortStatus.Ported)]
    protected virtual void SetSQLName(string v) => fSQLName = MappedSQLName(v);

    [Delphi(PortStatus.Ported)]
    protected string MappedSQLName(string value)
    {
        TBoldSQLSystemDescription? System = OwningSystem();
        return System is not null
            ? BoldNameExpander.BoldExpandPrefix(value, "", System.SQLDatabaseConfig.SystemTablePrefix, System.SQLDatabaseConfig.MaxDbIdentifierLength, System.NationalCharConversion)
            : value;
    }

    [Delphi(PortStatus.Ported)]
    protected TBoldSQLSystemDescription? OwningSystem() => Owner switch
    {
        TBoldSQLSystemDescription system => system,
        TBoldSQLDescriptionElement element => element.OwningSystem(),
        _ => null,
    };

    [Delphi(PortStatus.Ported)] public string SQLName { get => fSQLName; set => SetSQLName(value); }
}

/// <summary>A table: its columns and indexes, which it creates by SQL or through a table object.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLTableDescription : TBoldSQLDescriptionElement
{
    private readonly TBoldSQLIndexDescriptionList fIndexes;
    private readonly TBoldSQLDescriptionList fColumns;
    private readonly bool fVersioned;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLTableDescription(TBoldPSDescriptionElement aOwner, bool Versioned) : base(aOwner)
    {
        SystemDescription.SQLTablesList.Add(this);
        fIndexes = new TBoldSQLIndexDescriptionList();
        fColumns = new TBoldSQLDescriptionList(SystemDescription);
        fVersioned = Versioned;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSQLSystemDescription GetSystemDescription() => (TBoldSQLSystemDescription)Owner!;

    [Delphi(PortStatus.Ported)]
    protected override void SetSQLName(string v)
    {
        if (SQLName != MappedSQLName(v))
        {
            base.SetSQLName(v);
            SystemDescription.SQLTablesList.ItemChanged(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected bool TableExists(TBoldPSSQLParams PSParams)
    {
        var List = new TStringList();
        PSParams.Database!.AllTableNames(SQLName, false, List);
        return List.Count == 1;
    }

    [Delphi(PortStatus.Ported)]
    protected void CreateTableBDE(TBoldPSSQLParams PSParams)
    {
        const int MAXIMUMINDEXCOUNT = 64;
        IBoldTable? Table = PSParams.Database!.GetTable();
        try
        {
            Table.Tablename = SQLName;
            Table.Exclusive = true;
            Table.FieldDefs.Clear();
            for (int i = 0; i < ColumnsList.Count; i++)
            {
                ((TBoldSQLColumnDescription)ColumnsList[i]).CreateBDEColumn(Table.FieldDefs);
            }

            int MaxIndex = IndexList.Count > MAXIMUMINDEXCOUNT ? MAXIMUMINDEXCOUNT : IndexList.Count - 1;
            Table.IndexDefs.Clear();
            for (int i = 0; i <= MaxIndex; i++)
            {
                IndexList[i].CreateBDEIndex(PSParams, Table.IndexDefs);
            }

            Table.CreateTable();
        }
        finally
        {
            PSParams.Database.ReleaseTable(ref Table);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void CreateTableSQL(TBoldPSSQLParams PSParams)
    {
        IBoldExecQuery? Query = PSParams.Database!.GetExecQuery();
        try
        {
            Query.AssignSQLText(SQLForCreateTable(PSParams.Database));
            Query.ExecSQL();
            for (int i = 0; i < IndexList.Count; i++)
            {
                if (!SystemDescription.SQLDatabaseConfig.SupportsConstraintsInCreateTable || IndexList[i] != PrimaryIndex)
                {
                    Query.AssignSQLText(IndexList[i].SQLForSecondaryKey());
                    Query.ExecSQL();
                }
            }
        }
        finally
        {
            PSParams.Database.ReleaseExecQuery(ref Query);
        }
    }

    [Delphi(PortStatus.Ported)]
    public TBoldSQLColumnDescription AddColumn(string ColName, string SQLColType, string AllowNullAsSQL, TFieldType ColType, int ColSize, bool AllowNull, string DefaultDBValue) =>
        new(this)
        {
            SQLName = ColName,
            SQLType = SQLColType,
            FieldType = ColType,
            Size = ColSize,
            Mandatory = !AllowNull,
            SQLAllowNull = AllowNullAsSQL,
            DefaultDBValue = DefaultDBValue,
        };

    [Delphi(PortStatus.Ported)]
    public void EnsureIndex(string Fields, bool Primary, bool Unique)
    {
        TBoldSQLIndexDescription BoldSQLIndexDescription = IndexList.ItemsByIndexFields(Fields) ?? new TBoldSQLIndexDescription(this, Fields);
        if (Primary)
        {
            BoldSQLIndexDescription.IndexOptions += TIndexOption.ixPrimary;
            PrimaryIndex = BoldSQLIndexDescription;
        }

        if (Unique)
        {
            BoldSQLIndexDescription.IndexOptions += TIndexOption.ixUnique;
        }
    }

    [Delphi(PortStatus.Ported)]
    public void CreateTable(TBoldPSSQLParams PSParams)
    {
        BoldLogHandler.BoldLog().LogFmtIndent(BoldPMConsts.sCreatingTableX, [SQLName]);
        switch (SystemDescription.EffectiveGenerationMode(PSParams))
        {
            case TBoldDatabaseGenerationMode.dbgTable: CreateTableBDE(PSParams); break;
            case TBoldDatabaseGenerationMode.dbgQuery: CreateTableSQL(PSParams); break;
        }

        BoldLogHandler.BoldLog().Dedent();
    }

    [Delphi(PortStatus.Ported)]
    public void DeleteTable(TBoldPSSQLParams PSParams)
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        BoldLog.LogFmt(BoldPMConsts.sLocatingTableX, [SQLName]);
        if (TableExists(PSParams))
        {
            BoldLog.LogFmt(BoldPMConsts.sDeletingTableX, [SQLName]);
            IBoldTable? Table = PSParams.Database!.GetTable();
            try
            {
                Table.Tablename = SQLName;
                Table.Exclusive = true;
                Table.DeleteTable();
            }
            finally
            {
                PSParams.Database.ReleaseTable(ref Table);
            }
        }
        else
        {
            BoldLog.LogFmt(BoldPMConsts.sTableXNotPresent, [SQLName]);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void RetrieveSelectIdAndTypeStatement(TStrings S)
    {
        S.Add(SysUtils.Format("SELECT %s.%s, %s.%s", SQLName, BoldDefs.IDCOLUMN_NAME, SQLName, BoldDefs.TYPECOLUMN_NAME));
        S.Add(SysUtils.Format("FROM %s", SQLName));
    }

    [Delphi(PortStatus.Ported)]
    public void GenerateDatabaseScript(TStrings Script, string Separator)
    {
        var SQL = new TStringList { Text = SQLForCreateTable(null) };
        Script.Add(Separator);
        Script.AddStrings(SQL);
        for (int i = 0; i < IndexList.Count; i++)
        {
            if (IndexList[i] != PrimaryIndex)
            {
                SQL.Text = IndexList[i].SQLForSecondaryKey();
                Script.Add(Separator);
                Script.AddStrings(SQL);
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public string SQLForCreateTable(IBoldDataBase? DataBase)
    {
        string Result = SysUtils.Format("CREATE TABLE %s (", SQLName);
        for (int i = 0; i < ColumnsList.Count; i++)
        {
            if (i > 0)
            {
                Result += ", " + BoldDefs.BOLDCRLF;
            }

            string s = ((TBoldSQLColumnDescription)ColumnsList[i]).GetSQLForColumn(DataBase);
            Result += "  " + s;
            BoldLogHandler.BoldLog().Log(SysUtils.Format(BoldPMConsts.sAddingColumn, s), TBoldLogType.ltDetail);
        }

        if (SystemDescription.SQLDatabaseConfig.SupportsConstraintsInCreateTable && PrimaryIndex is not null)
        {
            Result += ", " + BoldDefs.BOLDCRLF + "  " + PrimaryIndex.SQLForPrimaryKey();
            BoldLogHandler.BoldLog().Log(SysUtils.Format(BoldPMConsts.sAddingPrimaryIndex, PrimaryIndex.SQLForPrimaryKey()), TBoldLogType.ltDetail);
        }

        return Result + ")";
    }

    [Delphi(PortStatus.Ported)]
    public string SQLForDropTable() => SysUtils.Format("DROP TABLE %s", SQLName);

    [Delphi(PortStatus.Ported)] public TBoldSQLDescriptionList ColumnsList => fColumns;
    [Delphi(PortStatus.Ported)] public TBoldSQLIndexDescriptionList IndexList => fIndexes;
    [Delphi(PortStatus.Ported)] public TBoldSQLIndexDescription? PrimaryIndex { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldSQLSystemDescription SystemDescription => GetSystemDescription();
    [Delphi(PortStatus.Ported)] public bool Versioned => fVersioned;
    [Delphi(PortStatus.Ported)] public bool ContainsStopTimeStamp { get; set; }
}

/// <summary>A column: its SQL type, size, nullability and default.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLColumnDescription : TBoldSQLDescriptionElement
{
    [Delphi(PortStatus.Ported)]
    public TBoldSQLColumnDescription(TBoldPSDescriptionElement aOwner) : base(aOwner)
    {
        TableDescription.ColumnsList.Add(this);
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSQLTableDescription GetTableDescription() => (TBoldSQLTableDescription)Owner!;

    [Delphi(PortStatus.Ported)]
    protected override void SetSQLName(string v)
    {
        if (SQLName != v)
        {
            base.SetSQLName(v);
            TableDescription.ColumnsList.ItemChanged(this);
        }
    }

    [Delphi(PortStatus.Ported)]
    public void CreateBDEColumn(TFieldDefs FieldDefs)
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        BoldLog.LogFmt(BoldPMConsts.sAddingColumnInfo, [SQLName, FieldType.ToString(), Size], TBoldLogType.ltDetail);
        FieldDefs.Add(SQLName, FieldType, Size, Mandatory);
        if (DefaultDBValue.Length != 0)
        {
            BoldLog.Separator();
            BoldLog.LogFmt(BoldPMConsts.sColumnHasDefaultDBValue, [SQLName, DefaultDBValue], TBoldLogType.ltWarning);
            BoldLog.Log(BoldPMConsts.sUnsupportedInTableCreationMode);
            BoldLog.Separator();
        }
    }

    [Delphi(PortStatus.Ported)]
    public string GetSQLForColumn(IBoldDataBase? DataBase)
    {
        string DefaultValue = (DataBase is null || DataBase.SupportsDefaultColumnValues()) && DefaultDBValue.Length != 0
            ? "DEFAULT " + DefaultDBValue
            : "";
        return SysUtils.Format("%s %s %s %s", SQLName, SQLType, DefaultValue, SQLAllowNull);
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription TableDescription => GetTableDescription();
    [Delphi(PortStatus.Ported)] public int Size { get; set; }
    [Delphi(PortStatus.Ported)] public bool Mandatory { get; set; }
    [Delphi(PortStatus.Ported)] public string SQLType { get; set; } = "";
    [Delphi(PortStatus.Ported)] public string SQLAllowNull { get; set; } = "";
    [Delphi(PortStatus.Ported)] public TFieldType FieldType { get; set; }
    [Delphi(PortStatus.Ported)] public string DefaultDBValue { get; set; } = "";
}

/// <summary>An index of a table, on ';'-separated fields.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLIndexDescription : TBoldPSDescriptionElement
{
    [Delphi(PortStatus.Ported)]
    public TBoldSQLIndexDescription(TBoldPSDescriptionElement aOwner, string Fields) : base(aOwner)
    {
        IndexedFields = Fields;
        TableDescription.IndexList.Add(this);
        IndexOptions = DelphiSet<TIndexOption>.Empty;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSQLTableDescription GetTableDescription() => (TBoldSQLTableDescription)Owner!;

    [Delphi(PortStatus.Ported)]
    private string GetIndexedFieldsForSQL() => IndexedFields.Replace(";", ",", StringComparison.Ordinal);

    [Delphi(PortStatus.Ported)]
    protected TBoldSQLTableDescription TableDescription => GetTableDescription();

    [Delphi(PortStatus.Ported)]
    protected string GeneratedName()
    {
        int IndexNameLength = TableDescription.SystemDescription.SQLDatabaseConfig.MaxDbIdentifierLength;
        if (IndexNameLength == -1)
        {
            IndexNameLength = TableDescription.SystemDescription.SQLDatabaseConfig.MaxIndexNameLength;
        }

        return BoldNameExpander.BoldExpandName("IX_" + TableDescription.SQLName + "_" + IndexedFields, "", TExpansionType.xtSQL, IndexNameLength,
            TableDescription.SystemDescription.NationalCharConversion);
    }

    [Delphi(PortStatus.Ported)]
    public string SQLForPrimaryKey() => SysUtils.Format("CONSTRAINT %s PRIMARY KEY (%s)", GeneratedName(), IndexedFieldsForSQL);

    [Delphi(PortStatus.Ported)]
    public string SQLForSecondaryKey()
    {
        string Unique = IndexOptions.Contains(TIndexOption.ixUnique) ? "UNIQUE" : "";
        return SysUtils.Format("CREATE %s INDEX %s ON %s (%s)", Unique, GeneratedName(), TableDescription.SQLName, IndexedFieldsForSQL);
    }

    [Delphi(PortStatus.Ported)]
    public void CreateBDEIndex(TBoldPSSQLParams PSParams, TIndexDefs IndexDefs)
    {
        string SQLName = GeneratedName();
        BoldLogHandler.BoldLog().LogFmt(BoldPMConsts.sAddingIndex, [SQLName, IndexedFields], TBoldLogType.ltDetail);
        DelphiSet<TIndexOption> ActualOptions = IndexOptions;
        string ActualName = SQLName;
        // Paradox complains if IndexOptions are empty.
        if (!PSParams.Database!.IsSqlBased)
        {
            if (ActualOptions.IsEmpty)
            {
                ActualOptions = DelphiSet<TIndexOption>.Of(TIndexOption.ixCaseInsensitive);
            }

            if (ActualOptions.Contains(TIndexOption.ixPrimary))
            {
                ActualName = "";
            }
        }

        IndexDefs.Add(ActualName, IndexedFields, ActualOptions);
    }

    [Delphi(PortStatus.Ported)] public string IndexedFields { get; set; }
    [Delphi(PortStatus.Ported)] public string IndexedFieldsForSQL => GetIndexedFieldsForSQL();
    [Delphi(PortStatus.Ported)] public DelphiSet<TIndexOption> IndexOptions { get; set; }
}

[Delphi(PortStatus.Ported)]
internal sealed class TSQLDescriptorSQLNameIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldSQLDescriptionElement)Item!).SQLName;
}

[Delphi(PortStatus.Ported)]
internal sealed class TSQLIndexFieldsIndex : TBoldStringHashIndex
{
    [Delphi(PortStatus.Ported)]
    protected override string ItemAsKeyString(object? Item) => ((TBoldSQLIndexDescription)Item!).IndexedFields;
}

/// <summary>Description elements, indexed by SQL name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLDescriptionList : TBoldIndexableList
{
    private readonly TBoldSQLSystemDescription fSystemDescription;

    [Delphi(PortStatus.Ported)]
    public TBoldSQLDescriptionList(TBoldSQLSystemDescription SystemDescription)
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref BoldPSDescriptionsSQL.IX_SQLDescriptionSQLName, AddIndex(new TSQLDescriptorSQLNameIndex()));
        fSystemDescription = SystemDescription;
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSQLDescriptionElement GetItem(int index) => (TBoldSQLDescriptionElement)base[index]!;

    [Delphi(PortStatus.Ported)]
    private TBoldSQLDescriptionElement? GetItemBySQLName(string SQLName) =>
        (TBoldSQLDescriptionElement?)((TSQLDescriptorSQLNameIndex)Indexes(BoldPSDescriptionsSQL.IX_SQLDescriptionSQLName)!).FindByString(
            BoldNameExpander.BoldExpandName(SQLName, "", TExpansionType.xtSQL, fSystemDescription.SQLDatabaseConfig.MaxDbIdentifierLength, fSystemDescription.NationalCharConversion));

    [Delphi(PortStatus.Ported)]
    public void ToStrings(TStrings S)
    {
        S.BeginUpdate();
        S.Clear();
        for (int I = 0; I < Count; I++)
        {
            S.AddObject(this[I].SQLName, this[I]);
        }

        S.EndUpdate();
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldSQLDescriptionList.Items")]
    public new TBoldSQLDescriptionElement this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldSQLDescriptionElement? ItemsBySQLName(string SQLName) => GetItemBySQLName(SQLName);
}

/// <summary>The tables of a system, indexed by SQL name.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLTableDescriptionList : TBoldSQLDescriptionList
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldSQLTableDescriptionList(TBoldSQLSystemDescription SystemDescription) : base(SystemDescription)
    {
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSQLTableDescription GetItem(int index) => (TBoldSQLTableDescription)base[index];

    [Delphi(PortStatus.Ported)]
    private TBoldSQLTableDescription? GetItemBySQLName(string SQLName) => (TBoldSQLTableDescription?)base.ItemsBySQLName(SQLName);

    [Delphi(PortStatus.Ported, Symbol = "TBoldSQLTableDescriptionList.Items")]
    public new TBoldSQLTableDescription this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public new TBoldSQLTableDescription? ItemsBySQLName(string SQLName) => GetItemBySQLName(SQLName);
}

/// <summary>The indexes of a table, indexed by their fields.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLIndexDescriptionList : TBoldIndexableList
{
    [Delphi(PortStatus.Ported)]
    public TBoldSQLIndexDescriptionList()
    {
        SetIndexCapacity(1);
        BoldIndexableList.SetIndexVariable(ref BoldPSDescriptionsSQL.IX_SQLIndexFields, AddIndex(new TSQLIndexFieldsIndex()));
    }

    [Delphi(PortStatus.Ported)]
    private TBoldSQLIndexDescription GetItem(int index) => (TBoldSQLIndexDescription)base[index]!;

    [Delphi(PortStatus.Ported)]
    private TBoldSQLIndexDescription? GetItemsByIndexFields(string IndexFields) =>
        (TBoldSQLIndexDescription?)((TSQLIndexFieldsIndex)Indexes(BoldPSDescriptionsSQL.IX_SQLIndexFields)!).FindByString(IndexFields);

    [Delphi(PortStatus.Ported, Symbol = "TBoldSQLIndexDescriptionList.items")]
    public new TBoldSQLIndexDescription this[int index] => GetItem(index);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")]
    public TBoldSQLIndexDescription? ItemsByIndexFields(string IndexFields) => GetItemsByIndexFields(IndexFields);
}
