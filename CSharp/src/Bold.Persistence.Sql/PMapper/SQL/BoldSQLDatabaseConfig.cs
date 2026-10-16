// Delphi: Source/PMapper/SQL/BoldSQLDatabaseConfig.pas
// Port map: CSharp/port/units/BoldSQLDatabaseConfig.md
// The SQL dialect settings of a database: column types, templates for dropping, limits, reserved words and script
// conventions, with presets per database engine.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldSQLDatabaseConfig", Symbol = "TBoldSQLDataBaseConfig.Destroy", Note = "Freed the reserved words; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldSQLDatabaseConfig", Symbol = "TBoldSQLDataBaseConfig.DefineProperties", Note = "Read the obsolete UseTransactionsDuringDBCreate from Delphi form files; there is no form streaming in .NET.")]
[assembly: Delphi(PortStatus.Dropped, Unit = "BoldSQLDatabaseConfig", Symbol = "TBoldSQLDataBaseConfig.ReadUseTransactionsDuringDBCreate", Note = "Delphi form streaming of an obsolete property (see DefineProperties).")]

namespace Bold;

/// <summary>The kinds of database with preset settings (InitializeDbEngineSettings).</summary>
[Delphi(PortStatus.Ported)]
public enum TBoldDataBaseEngine
{
    dbeUnknown,
    dbeInterbaseSQLDialect1,
    dbeInterbaseSQLDialect3,
    dbeGenericANSISQL92,
    dbeSQLServer,
    dbeDBISAM,
    dbeOracle,
    dbeAdvantage,
    dbeParadox,
    dbeInformix,
}

/// <summary>How the schema is created: with SQL queries, or through table components (desktop databases).</summary>
/// <remarks>Declared in BoldDefs in Bold's original sources, which this tree no longer has.</remarks>
public enum TBoldDatabaseGenerationMode
{
    dbgQuery,
    dbgTable,
}

/// <summary>The SQL dialect settings of a database; every change raises OnChange.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldSQLDataBaseConfig : TPersistent
{
    private const string EmptyMarker = "<Empty>";
    private const string TableNameMarker = "<TableName>";
    private const string ColumnNameMarker = "<ColumnName>";
    private const string IndexNameMarker = "<IndexName>";

    private string FColumnTypeForDate = "";
    private string FColumnTypeForTime = "";
    private string FColumnTypeForDateTime = "";
    private string FColumnTypeForBlob = "";
    private string FColumnTypeForFloat = "";
    private string FColumnTypeForCurrency = "";
    private string FColumnTypeForString = "";
    private string FColumnTypeForInteger = "";
    private string fColumnTypeForSmallInt = "";
    private TFieldType fFieldTypeForBlob;
    private int fFetchBlockSize;
    private int fMaxParamsInIdList;
    private int fDefaultStringLength;
    private bool fUseSQL92Joins;
    private string fDropColumnTemplate = "";
    private string fDropTableTemplate = "";
    private string fDropIndexTemplate = "";
    private int fMaxDbIdentifierLength;
    private int fMaxIndexNameLength;
    private string fSQLforNotNull = "";
    private bool fQuoteNonStringDefaultValues;
    private bool fSupportsConstraintsInCreateTable;
    private bool fSupportsStringDefaultValues;
    private TBoldDatabaseGenerationMode fDBGenerationMode;
    private bool fAllowMetadataChangesInTransaction;
    private TStringList fReservedWords = new();
    private string fEmptyStringMarker = "";
    private bool fStoreEmptyStringsAsNULL;
    private string fSystemTablePrefix = "";
    private string FSqlScriptSeparator = "";
    private string FSqlScriptTerminator = "";
    private string FSqlScriptCommentStart = "";
    private string FSqlScriptCommentStop = "";
    private string FSqlScriptStartTransaction = "";
    private string FSqlScriptCommitTransaction = "";
    private string FSqlScriptRollBackTransaction = "";
    private TNotifyEvent? fOnChange;
    private TBoldDataBaseEngine fEngine;

    /// <summary>The generic settings; the fetch block size (250) is set only here, not by the engine presets.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldSQLDataBaseConfig()
    {
        fFetchBlockSize = 250;
        SetInitialValues();
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForDate(string Value)
    {
        if (FColumnTypeForDate != Value)
        {
            FColumnTypeForDate = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForTime(string Value)
    {
        if (FColumnTypeForTime != Value)
        {
            FColumnTypeForTime = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForDateTime(string Value)
    {
        if (FColumnTypeForDateTime != Value)
        {
            FColumnTypeForDateTime = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForBlob(string Value)
    {
        if (FColumnTypeForBlob != Value)
        {
            FColumnTypeForBlob = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForFloat(string Value)
    {
        if (FColumnTypeForFloat != Value)
        {
            FColumnTypeForFloat = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForCurrency(string Value)
    {
        if (FColumnTypeForCurrency != Value)
        {
            FColumnTypeForCurrency = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForString(string Value)
    {
        if (FColumnTypeForString != Value)
        {
            FColumnTypeForString = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForInteger(string Value)
    {
        if (FColumnTypeForInteger != Value)
        {
            FColumnTypeForInteger = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetColumnTypeForSmallInt(string Value)
    {
        if (fColumnTypeForSmallInt != Value)
        {
            fColumnTypeForSmallInt = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetFieldTypeForBlob(TFieldType Value)
    {
        if (fFieldTypeForBlob != Value)
        {
            fFieldTypeForBlob = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetFetchBlockSize(int Value)
    {
        if (fFetchBlockSize != Value)
        {
            fFetchBlockSize = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetMaxParamsInIdList(int Value)
    {
        if (fMaxParamsInIdList != Value)
        {
            fMaxParamsInIdList = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDefaultStringLength(int Value)
    {
        if (fDefaultStringLength != Value)
        {
            fDefaultStringLength = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetUseSQL92Joins(bool Value)
    {
        if (fUseSQL92Joins != Value)
        {
            fUseSQL92Joins = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDropColumnTemplate(string Value)
    {
        if (fDropColumnTemplate != Value)
        {
            fDropColumnTemplate = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDropTableTemplate(string Value)
    {
        if (fDropTableTemplate != Value)
        {
            fDropTableTemplate = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDropIndexTemplate(string Value)
    {
        if (fDropIndexTemplate != Value)
        {
            fDropIndexTemplate = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetMaxDbIdentifierLength(int Value)
    {
        if (fMaxDbIdentifierLength != Value)
        {
            fMaxDbIdentifierLength = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetMaxIndexNameLenght(int Value)
    {
        if (fMaxIndexNameLength != Value)
        {
            fMaxIndexNameLength = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetQuoteNonStringDefaultValues(bool Value)
    {
        if (fQuoteNonStringDefaultValues != Value)
        {
            fQuoteNonStringDefaultValues = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetSupportsConstraintsInCreateTable(bool Value)
    {
        if (fSupportsConstraintsInCreateTable != Value)
        {
            fSupportsConstraintsInCreateTable = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetSupportsStringDefaultValues(bool Value)
    {
        if (fSupportsStringDefaultValues != Value)
        {
            fSupportsStringDefaultValues = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetDBGenerationMode(TBoldDatabaseGenerationMode Value)
    {
        if (fDBGenerationMode != Value)
        {
            fDBGenerationMode = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void setAllowMetadataChangesInTransaction(bool Value)
    {
        if (fAllowMetadataChangesInTransaction != Value)
        {
            fAllowMetadataChangesInTransaction = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetEmptyStringMarker(string Value)
    {
        if (fEmptyStringMarker != Value)
        {
            fEmptyStringMarker = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetStoreEmptyStringsAsNULL(bool Value)
    {
        if (fStoreEmptyStringsAsNULL != Value)
        {
            fStoreEmptyStringsAsNULL = Value;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private void SetSqlScriptSeparator(string Value)
    {
        FSqlScriptSeparator = Value;
        Change();
    }

    [Delphi(PortStatus.Ported)]
    private void SetSqlScriptTerminator(string Value)
    {
        FSqlScriptTerminator = Value;
        Change();
    }

    [Delphi(PortStatus.Ported)]
    private void SetSqlScriptCommentStart(string Value)
    {
        FSqlScriptCommentStart = Value;
        Change();
    }

    [Delphi(PortStatus.Ported)]
    private void SetSqlScriptCommentStop(string Value)
    {
        FSqlScriptCommentStop = Value;
        Change();
    }

    [Delphi(PortStatus.Ported)]
    private void SetSqlScriptStartTransaction(string Value)
    {
        FSqlScriptStartTransaction = Value;
        Change();
    }

    [Delphi(PortStatus.Ported)]
    private void SetSqlScriptCommitTransaction(string Value)
    {
        FSqlScriptCommitTransaction = Value;
        Change();
    }

    [Delphi(PortStatus.Ported)]
    private void SetSqlScriptRollBackTransaction(string Value)
    {
        FSqlScriptRollBackTransaction = Value;
        Change();
    }

    [Delphi(PortStatus.Ported)]
    private void Change() => fOnChange?.Invoke(this);

    /// <summary>The not-null clause; an empty value is kept as a marker so it survives streaming.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetSQLforNotNull(string Value)
    {
        string temp = Value.Length == 0 ? EmptyMarker : Value;
        if (fSQLforNotNull != temp)
        {
            fSQLforNotNull = temp;
            Change();
        }
    }

    [Delphi(PortStatus.Ported)]
    private string GetEffectiveSQLForNotNull() => SQLforNotNull == EmptyMarker ? "" : SQLforNotNull;

    [Delphi(PortStatus.Ported)]
    private void SetReservedWords(TStringList Value) => fReservedWords.Assign(Value);

    /// <summary>The prefix of Bold's own tables; empty means BOLD.</summary>
    [Delphi(PortStatus.Ported)]
    private void SetSystemTablePrefix(string Value)
    {
        string temp = Value.Length == 0 ? "BOLD" : Value;
        if (fSystemTablePrefix != temp)
        {
            fSystemTablePrefix = temp;
            Change();
        }
    }

    /// <summary>The generic SQL settings (and the default reserved words).</summary>
    [Delphi(PortStatus.Ported)]
    private void SetInitialValues()
    {
        FColumnTypeForBlob = "BLOB";
        FColumnTypeForDateTime = "DATE";
        FColumnTypeForDate = "DATE";
        FColumnTypeForTime = "DATE";
        FColumnTypeForFloat = "DOUBLE PRECISION";
        FColumnTypeForCurrency = "DOUBLE PRECISION";
        FColumnTypeForString = "VARCHAR(%d)";
        FColumnTypeForInteger = "INTEGER";
        fColumnTypeForSmallInt = "SMALLINT";
        fDefaultStringLength = 255;
        fMaxParamsInIdList = 20;
        fMaxIndexNameLength = 18;
        fMaxDbIdentifierLength = -1;
        fFieldTypeForBlob = TFieldType.ftBlob;
        fStoreEmptyStringsAsNULL = false;
        fSystemTablePrefix = "BOLD";
        fEmptyStringMarker = "";
        fAllowMetadataChangesInTransaction = true;
        fDBGenerationMode = TBoldDatabaseGenerationMode.dbgQuery;
        fDropColumnTemplate = "ALTER TABLE <TableName> DROP <ColumnName>";
        fDropTableTemplate = "DROP TABLE <TableName>";
        fDropIndexTemplate = "DROP INDEX <IndexName>";
        fSQLforNotNull = "NOT NULL";
        fSupportsConstraintsInCreateTable = true;
        fQuoteNonStringDefaultValues = false;
        fSupportsStringDefaultValues = true;
        FSqlScriptCommentStart = "/* ";
        FSqlScriptStartTransaction = "START TRANSACTION";
        FSqlScriptTerminator = ";";
        FSqlScriptCommentStop = " */";
        FSqlScriptSeparator = "---";
        FSqlScriptRollBackTransaction = "ROLLBACK";
        FSqlScriptCommitTransaction = "COMMIT";
        fReservedWords.Text = "ACTIVE, ADD, ALL, AFTER, ALTER\nAND, ANY, AS, ASC, ASCENDING,\n" +
            "AT, AUTO, AUTOINC, AVG, BASE_NAME\nBEFORE, BEGIN, BETWEEN, BLOB, BOOLEAN,\n" +
            "BOTH, BY, BYTES, CACHE, CAST, CHAR\nCHARACTER, CHECK, CHECK_POINT_LENGTH, COLLATE,\n" +
            "COLUMN, COMMIT, COMMITTED, COMPUTED\nCONDITIONAL, CONSTRAINT, CONTAINING, COUNT, CREATE, CSTRING,\n" +
            "CURRENT, CURSOR, DATABASE, DATE, DAY\nDEBUG, DEC, DECIMAL, DECLARE, DEFAULT,\n" +
            "DELETE, DESC, DESCENDING, DISTINCT, DO\nDOMAIN, DOUBLE, DROP, ELSE, END,\n" +
            "ENTRY_POINT, ESCAPE, EXCEPTION, EXECUTE\nEXISTS, EXIT, EXTERNAL, EXTRACT, FILE, FILTER,\n" +
            "FLOAT, FOR, FOREIGN, FROM, FULL, FUNCTION\nGDSCODE, GENERATOR, GEN_ID, GRANT,\n" +
            "GROUP, GROUP_COMMIT_WAIT_TIME, HAVING\nHOUR, IF, IN, INT, INACTIVE, INDEX, INNER,\n" +
            "INPUT_TYPE, INSERT, INTEGER, INTO\nIS, ISOLATION, JOIN, KEY, LONG, LENGTH,\n" +
            "LOGFILE, LOWER, LEADING, LEFT, LEVEL\nLIKE, LOG_BUFFER_SIZE, MANUAL, MAX, MAXIMUM_SEGMENT,\n" +
            "MERGE, MESSAGE, MIN, MINUTE, MODULE_NAME\nMONEY, MONTH, NAMES, NATIONAL, NATURAL,\n" +
            "NCHAR, NO, NOT, NULL, NUM_LOG_BUFFERS\nNUMERIC, OF, ON, ONLY, OPTION,\n" +
            "OR, ORDER, OUTER, OUTPUT_TYPE, OVERFLOW\nPAGE_SIZE, PAGE, PAGES, PARAMETER, PASSWORD,\n" +
            "PLAN, POSITION, POST_EVENT, PRECISION\nPROCEDURE, PROTECTED, PRIMARY, PRIVILEGES, RAW_PARTITIONS, RDB$DB_KEY,\n" +
            "READ, REAL, RECORD_VERSION, REFERENCES\nRESERV, RESERVING, RETAIN, RETURNING_VALUES, RETURNS, REVOKE,\n" +
            "RIGHT, ROLE, ROLLBACK, SECOND, SEGMENT\nSELECT, SET, SHARED, SHADOW, SCHEMA, SINGULAR,\n" +
            "SIZE, SMALLINT, SNAPSHOT, SOME, SORT\nSQLCODE, STABILITY, STARTING, STARTS, STATISTICS,\n" +
            "SUB_TYPE, SUBSTRING, SUM, SUSPEND, TABLE\nTHEN, TIME, TIMESTAMP, TIMEZONE_HOUR, TIMEZONE_MINUTE,\n" +
            "TO, TRAILING, TRANSACTION, TRIGGER, TRIM\nUNCOMMITTED, UNION, UNIQUE, UPDATE, UPPER,\n" +
            "USER, VALUE, VALUES, VARCHAR, VARIABLE\nVARYING, VIEW, WAIT, WHEN, WHERE,\n" +
            "WHILE, WITH, WORK, WRITE, YEAR";
        Change();
    }

    [Delphi(PortStatus.Ported)]
    public override void Assign(TPersistent? Source)
    {
        if (Source is TBoldSQLDataBaseConfig config)
        {
            AssignConfig(config);
        }
        else
        {
            base.Assign(Source);
        }
    }

    /// <summary>Copies every setting except the reserved words, as Delphi does.</summary>
    [Delphi(PortStatus.Ported)]
    public void AssignConfig(TBoldSQLDataBaseConfig Source)
    {
        FColumnTypeForDate = Source.ColumnTypeForDate;
        FColumnTypeForTime = Source.ColumnTypeForTime;
        FColumnTypeForDateTime = Source.ColumnTypeForDateTime;
        FColumnTypeForBlob = Source.ColumnTypeForBlob;
        FColumnTypeForFloat = Source.ColumnTypeForFloat;
        FColumnTypeForCurrency = Source.ColumnTypeForCurrency;
        FColumnTypeForString = Source.ColumnTypeForString;
        FColumnTypeForInteger = Source.ColumnTypeForInteger;
        fColumnTypeForSmallInt = Source.ColumnTypeForSmallInt;
        fFieldTypeForBlob = Source.FieldTypeForBlob;
        fFetchBlockSize = Source.FetchBlockSize;
        fMaxParamsInIdList = Source.MaxParamsInIdList;
        fDefaultStringLength = Source.DefaultStringLength;
        fUseSQL92Joins = Source.UseSQL92Joins;
        fDropColumnTemplate = Source.DropColumnTemplate;
        fDropTableTemplate = Source.DropTableTemplate;
        fDropIndexTemplate = Source.DropIndexTemplate;
        fMaxDbIdentifierLength = Source.MaxDbIdentifierLength;
        fMaxIndexNameLength = Source.MaxIndexNameLength;
        fSQLforNotNull = Source.SQLforNotNull;
        fQuoteNonStringDefaultValues = Source.QuoteNonStringDefaultValues;
        fSupportsConstraintsInCreateTable = Source.SupportsConstraintsInCreateTable;
        fSupportsStringDefaultValues = Source.SupportsStringDefaultValues;
        fDBGenerationMode = Source.DBGenerationMode;
        fAllowMetadataChangesInTransaction = Source.AllowMetadataChangesInTransaction;
        fEmptyStringMarker = Source.EmptyStringMarker;
        fStoreEmptyStringsAsNULL = Source.StoreEmptyStringsAsNULL;
        fSystemTablePrefix = Source.SystemTablePrefix;
        FSqlScriptSeparator = Source.SqlScriptSeparator;
        FSqlScriptTerminator = Source.SqlScriptTerminator;
        FSqlScriptCommentStart = Source.SqlScriptCommentStart;
        FSqlScriptCommentStop = Source.SqlScriptCommentStop;
        FSqlScriptStartTransaction = Source.SqlScriptStartTransaction;
        FSqlScriptCommitTransaction = Source.SqlScriptCommitTransaction;
        FSqlScriptRollBackTransaction = Source.SqlScriptRollBackTransaction;
        fEngine = Source.Engine;
        Change();
    }

    [Delphi(PortStatus.Ported)]
    public string GetDropColumnQuery(string TableName, string columnName)
    {
        string result = SysUtils.StringReplace(DropColumnTemplate, TableNameMarker, TableName, DelphiSet<TReplaceFlag>.Of(TReplaceFlag.rfIgnoreCase, TReplaceFlag.rfReplaceAll));
        return SysUtils.StringReplace(result, ColumnNameMarker, columnName, DelphiSet<TReplaceFlag>.Of(TReplaceFlag.rfIgnoreCase, TReplaceFlag.rfReplaceAll));
    }

    [Delphi(PortStatus.Ported)]
    public string GetDropIndexQuery(string TableName, string IndexName)
    {
        string result = SysUtils.StringReplace(DropIndexTemplate, TableNameMarker, TableName, DelphiSet<TReplaceFlag>.Of(TReplaceFlag.rfIgnoreCase, TReplaceFlag.rfReplaceAll));
        return SysUtils.StringReplace(result, IndexNameMarker, IndexName, DelphiSet<TReplaceFlag>.Of(TReplaceFlag.rfIgnoreCase, TReplaceFlag.rfReplaceAll));
    }

    [Delphi(PortStatus.Ported)]
    public string GetDropTableQuery(string TableName) =>
        SysUtils.StringReplace(DropTableTemplate, TableNameMarker, TableName, DelphiSet<TReplaceFlag>.Of(TReplaceFlag.rfIgnoreCase, TReplaceFlag.rfReplaceAll));

    /// <summary>Resets to the generic settings and applies the engine's preset (an unknown engine changes nothing).</summary>
    [Delphi(PortStatus.Ported)]
    public void InitializeDbEngineSettings(TBoldDataBaseEngine Engine)
    {
        void AddReservedWord(string name)
        {
            if (!SysUtils.UpperCase(ReservedWords.Text).Contains(SysUtils.UpperCase(name), StringComparison.Ordinal))
            {
                ReservedWords.Add(name);
            }
        }

        if (Engine != TBoldDataBaseEngine.dbeUnknown)
        {
            SetInitialValues();
        }

        switch (Engine)
        {
            case TBoldDataBaseEngine.dbeSQLServer:
                FColumnTypeForDate = "DATETIME";
                FColumnTypeForTime = "DATETIME";
                FColumnTypeForDateTime = "DATETIME";
                FColumnTypeForFloat = "DECIMAL (28,10)";
                FColumnTypeForCurrency = "DECIMAL (28,10)";
                fDropColumnTemplate = "ALTER TABLE <TableName> DROP COLUMN <ColumnName>";
                fDropIndexTemplate = "DROP INDEX <TableName>.<IndexName>";
                break;
            case TBoldDataBaseEngine.dbeGenericANSISQL92:
                FColumnTypeForDate = "DATE";
                FColumnTypeForTime = "TIME";
                FColumnTypeForDateTime = "TIMESTAMP";
                break;
            case TBoldDataBaseEngine.dbeInterbaseSQLDialect3:
                FColumnTypeForDate = "TIMESTAMP";
                FColumnTypeForTime = "TIMESTAMP";
                FColumnTypeForDateTime = "TIMESTAMP";
                fMaxIndexNameLength = 31;
                fMaxDbIdentifierLength = 31;
                fAllowMetadataChangesInTransaction = true;
                break;
            case TBoldDataBaseEngine.dbeInterbaseSQLDialect1:
                fMaxIndexNameLength = 31;
                fMaxDbIdentifierLength = 31;
                fAllowMetadataChangesInTransaction = true;
                break;
            case TBoldDataBaseEngine.dbeDBISAM:
                FColumnTypeForDate = "DATE";
                FColumnTypeForTime = "TIME";
                FColumnTypeForDateTime = "TIMESTAMP";
                FColumnTypeForFloat = "FLOAT";
                FColumnTypeForCurrency = "FLOAT";
                fDefaultStringLength = 250;
                AddReservedWord("Description");
                break;
            case TBoldDataBaseEngine.dbeAdvantage:
                fSQLforNotNull = "CONSTRAINT NOT NULL";
                fColumnTypeForSmallInt = "SHORT";
                FColumnTypeForFloat = "NUMERIC";
                FColumnTypeForCurrency = "NUMERIC";
                fSupportsConstraintsInCreateTable = false;
                fQuoteNonStringDefaultValues = true;
                fSupportsStringDefaultValues = false;
                FColumnTypeForDate = "DATE";
                FColumnTypeForTime = "TIME";
                FColumnTypeForDateTime = "TIMESTAMP";
                FColumnTypeForString = "CHAR(%d)";
                fStoreEmptyStringsAsNULL = true;
                break;
            case TBoldDataBaseEngine.dbeOracle:
                FColumnTypeForString = "VARCHAR2(%d)";
                FColumnTypeForFloat = "NUMBER";
                FColumnTypeForCurrency = "NUMBER";
                FColumnTypeForInteger = "NUMBER(10,0)";
                fColumnTypeForSmallInt = "NUMBER(5,0)";
                fDropColumnTemplate = "ALTER TABLE <TableName> DROP COLUMN <ColumnName>";
                fMaxIndexNameLength = 30;
                fMaxDbIdentifierLength = 30;
                break;
            case TBoldDataBaseEngine.dbeParadox:
                fDBGenerationMode = TBoldDatabaseGenerationMode.dbgTable;
                break;
            case TBoldDataBaseEngine.dbeInformix:
                fMaxIndexNameLength = 18;
                fMaxDbIdentifierLength = 18;
                FColumnTypeForCurrency = "MONEY";
                FColumnTypeForFloat = "NUMERIC";
                FColumnTypeForDate = "DATETIME YEAR TO DAY";
                FColumnTypeForTime = "DATETIME HOUR TO FRACTION";
                FColumnTypeForDateTime = "DATETIME YEAR TO FRACTION";
                break;
        }

        Change();
    }

    /// <summary>The default value, quoted when the database wants non-string defaults quoted.</summary>
    [Delphi(PortStatus.Ported)]
    public string CorrectlyQuotedDefaultValue(string value) => QuoteNonStringDefaultValues ? "'" + value + "'" : value;

    [Delphi(PortStatus.Ported)] public string EffectiveSQLForNotNull => GetEffectiveSQLForNotNull();
    [Delphi(PortStatus.Ported)] public TNotifyEvent? OnChange { get => fOnChange; set => fOnChange = value; }
    [Delphi(PortStatus.Ported)] public TBoldDataBaseEngine Engine { get => fEngine; set => fEngine = value; }

    [Delphi(PortStatus.Ported)] public string ColumnTypeForDate { get => FColumnTypeForDate; set => SetColumnTypeForDate(value); }
    [Delphi(PortStatus.Ported)] public string ColumnTypeForTime { get => FColumnTypeForTime; set => SetColumnTypeForTime(value); }
    [Delphi(PortStatus.Ported)] public string ColumnTypeForDateTime { get => FColumnTypeForDateTime; set => SetColumnTypeForDateTime(value); }
    [Delphi(PortStatus.Ported)] public string ColumnTypeForBlob { get => FColumnTypeForBlob; set => SetColumnTypeForBlob(value); }
    [Delphi(PortStatus.Ported)] public string ColumnTypeForFloat { get => FColumnTypeForFloat; set => SetColumnTypeForFloat(value); }
    [Delphi(PortStatus.Ported)] public string ColumnTypeForCurrency { get => FColumnTypeForCurrency; set => SetColumnTypeForCurrency(value); }
    [Delphi(PortStatus.Ported)] public string ColumnTypeForString { get => FColumnTypeForString; set => SetColumnTypeForString(value); }
    [Delphi(PortStatus.Ported)] public string ColumnTypeForInteger { get => FColumnTypeForInteger; set => SetColumnTypeForInteger(value); }
    [Delphi(PortStatus.Ported)] public string ColumnTypeForSmallInt { get => fColumnTypeForSmallInt; set => SetColumnTypeForSmallInt(value); }
    [Delphi(PortStatus.Ported)] public TFieldType FieldTypeForBlob { get => fFieldTypeForBlob; set => SetFieldTypeForBlob(value); }
    [Delphi(PortStatus.Ported)] public int FetchBlockSize { get => fFetchBlockSize; set => SetFetchBlockSize(value); }
    [Delphi(PortStatus.Ported)] public int MaxParamsInIdList { get => fMaxParamsInIdList; set => SetMaxParamsInIdList(value); }
    [Delphi(PortStatus.Ported)] public int DefaultStringLength { get => fDefaultStringLength; set => SetDefaultStringLength(value); }
    [Delphi(PortStatus.Ported)] public bool UseSQL92Joins { get => fUseSQL92Joins; set => SetUseSQL92Joins(value); }
    [Delphi(PortStatus.Ported)] public string DropColumnTemplate { get => fDropColumnTemplate; set => SetDropColumnTemplate(value); }
    [Delphi(PortStatus.Ported)] public string DropTableTemplate { get => fDropTableTemplate; set => SetDropTableTemplate(value); }
    [Delphi(PortStatus.Ported)] public string DropIndexTemplate { get => fDropIndexTemplate; set => SetDropIndexTemplate(value); }
    [Delphi(PortStatus.Ported)] public int MaxDbIdentifierLength { get => fMaxDbIdentifierLength; set => SetMaxDbIdentifierLength(value); }
    [Delphi(PortStatus.Ported)] public int MaxIndexNameLength { get => fMaxIndexNameLength; set => SetMaxIndexNameLenght(value); }
    [Delphi(PortStatus.Ported)] public string SQLforNotNull { get => fSQLforNotNull; set => SetSQLforNotNull(value); }
    [Delphi(PortStatus.Ported)] public bool QuoteNonStringDefaultValues { get => fQuoteNonStringDefaultValues; set => SetQuoteNonStringDefaultValues(value); }
    [Delphi(PortStatus.Ported)] public bool SupportsConstraintsInCreateTable { get => fSupportsConstraintsInCreateTable; set => SetSupportsConstraintsInCreateTable(value); }
    [Delphi(PortStatus.Ported)] public bool SupportsStringDefaultValues { get => fSupportsStringDefaultValues; set => SetSupportsStringDefaultValues(value); }
    [Delphi(PortStatus.Ported)] public TBoldDatabaseGenerationMode DBGenerationMode { get => fDBGenerationMode; set => SetDBGenerationMode(value); }
    [Delphi(PortStatus.Ported)] public bool AllowMetadataChangesInTransaction { get => fAllowMetadataChangesInTransaction; set => setAllowMetadataChangesInTransaction(value); }
    [Delphi(PortStatus.Ported)] public TStringList ReservedWords { get => fReservedWords; set => SetReservedWords(value); }
    [Delphi(PortStatus.Ported)] public string EmptyStringMarker { get => fEmptyStringMarker; set => SetEmptyStringMarker(value); }
    [Delphi(PortStatus.Ported)] public bool StoreEmptyStringsAsNULL { get => fStoreEmptyStringsAsNULL; set => SetStoreEmptyStringsAsNULL(value); }
    [Delphi(PortStatus.Ported)] public string SystemTablePrefix { get => fSystemTablePrefix; set => SetSystemTablePrefix(value); }
    [Delphi(PortStatus.Ported)] public string SqlScriptSeparator { get => FSqlScriptSeparator; set => SetSqlScriptSeparator(value); }
    [Delphi(PortStatus.Ported)] public string SqlScriptTerminator { get => FSqlScriptTerminator; set => SetSqlScriptTerminator(value); }
    [Delphi(PortStatus.Ported)] public string SqlScriptCommentStart { get => FSqlScriptCommentStart; set => SetSqlScriptCommentStart(value); }
    [Delphi(PortStatus.Ported)] public string SqlScriptCommentStop { get => FSqlScriptCommentStop; set => SetSqlScriptCommentStop(value); }
    [Delphi(PortStatus.Ported)] public string SqlScriptStartTransaction { get => FSqlScriptStartTransaction; set => SetSqlScriptStartTransaction(value); }
    [Delphi(PortStatus.Ported)] public string SqlScriptCommitTransaction { get => FSqlScriptCommitTransaction; set => SetSqlScriptCommitTransaction(value); }
    [Delphi(PortStatus.Ported)] public string SqlScriptRollBackTransaction { get => FSqlScriptRollBackTransaction; set => SetSqlScriptRollBackTransaction(value); }
}
