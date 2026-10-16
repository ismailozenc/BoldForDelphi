// Delphi: Source/PMapper/Default/BoldPSDescriptionsDefault.pas
// Port map: CSharp/port/units/BoldPSDescriptionsDefault.md
// The default database description: Bold's system tables, and the first rows they get when the database is created.

namespace Bold;

/// <summary>The description of a default-mapped database: Bold's system tables, filled when the database is created.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDefaultSystemDescription : TBoldSQLSystemDescription
{
    /// <summary>The inherited constructor (C# does not inherit constructors).</summary>
    public TBoldDefaultSystemDescription(TBoldPSDescriptionElement? aOwner, TBoldSQLDataBaseConfig SQLDatabaseConfig, TBoldNationalCharConversion NationalCharConversion)
        : base(aOwner, SQLDatabaseConfig, NationalCharConversion)
    {
    }

    [Delphi(PortStatus.Ported)] private void SetIdTable(TBoldSQLTableDescription? Value) => IdTableField = Value;
    [Delphi(PortStatus.Ported)] private void SetTimeStamptable(TBoldSQLTableDescription? Value) => TimeStampTableField = Value;
    [Delphi(PortStatus.Ported)] private void SetTypeTable(TBoldSQLTableDescription? Value) => TypeTableField = Value;
    [Delphi(PortStatus.Ported)] private void SetXFilestable(TBoldSQLTableDescription? Value) => XFilesTableField = Value;
    [Delphi(PortStatus.Ported)] private void SetTabletable(TBoldSQLTableDescription? Value) => TableTableField = Value;
    [Delphi(PortStatus.Ported)] private void SetRootTable(TBoldSQLTableDescription? Value) => RootTableField = Value;

    private TBoldSQLTableDescription? IdTableField;
    private TBoldSQLTableDescription? TimeStampTableField;
    private TBoldSQLTableDescription? TypeTableField;
    private TBoldSQLTableDescription? XFilesTableField;
    private TBoldSQLTableDescription? TableTableField;
    private TBoldSQLTableDescription? RootTableField;

    private string ExpandColumn(string Name) =>
        BoldNameExpander.BoldExpandName(Name, "", TExpansionType.xtSQL, SQLDatabaseConfig.MaxDbIdentifierLength, NationalCharConversion);

    /// <summary>Runs the table or the query way of a step, by the generation mode.</summary>
    private void ByGenerationMode(TBoldPSDefaultParams PSParams, Action UsingTable, Action UsingQuery, string MethodName)
    {
        switch (EffectiveGenerationMode(PSParams))
        {
            case TBoldDatabaseGenerationMode.dbgTable: UsingTable(); break;
            case TBoldDatabaseGenerationMode.dbgQuery: UsingQuery(); break;
            default: throw new EBold(SysUtils.Format(BoldPMConsts.sUnknownGenerationMode, GetType().Name, MethodName));
        }
    }

    /// <summary>Appends rows to a table through a table object.</summary>
    private static void AppendRows(TBoldPSDefaultParams PSParams, string TableName, Action<IBoldTable> AddRows)
    {
        IBoldTable? Table = PSParams.Database!.GetTable();
        try
        {
            Table.Tablename = TableName;
            Table.Open();
            AddRows(Table);
            Table.Close();
        }
        finally
        {
            PSParams.Database.ReleaseTable(ref Table);
        }
    }

    /// <summary>Runs a statement (with parameters set by SetParams) once per call of Run.</summary>
    private static void ExecQuery(TBoldPSDefaultParams PSParams, string SQL, Action<IBoldExecQuery> Run)
    {
        IBoldExecQuery? q = PSParams.Database!.GetExecQuery();
        try
        {
            q.AssignSQLText(SQL);
            Run(q);
        }
        finally
        {
            PSParams.Database.ReleaseExecQuery(ref q);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void AddFirstID(TBoldPSDefaultParams PSParams)
    {
        BoldLogHandler.BoldLog().Log(BoldPMConsts.sLogWritingFirstID);
        ByGenerationMode(PSParams,
            () => AppendRows(PSParams, IdTable!.SQLName, Table =>
            {
                Table.Append();
                Table[BoldDefs.IDCOLUMN_NAME] = 1;
                Table.Post();
            }),
            () => ExecQuery(PSParams, SysUtils.Format("INSERT INTO %s (%s) VALUES (1)", IdTable!.SQLName, BoldDefs.IDCOLUMN_NAME), q => q.ExecSQL()),
            "AddFirstID");
    }

    [Delphi(PortStatus.Ported)]
    private void AddFirstTimeStamp(TBoldPSDefaultParams PSParams)
    {
        BoldLogHandler.BoldLog().Log(BoldPMConsts.sLogWritingFirstTimeStamp);
        ByGenerationMode(PSParams,
            () =>
            {
                BoldLogHandler.BoldLog().Log(BoldPMConsts.sLogWritingFirstTimeStamp);
                AppendRows(PSParams, TimeStampTable!.SQLName, Table =>
                {
                    Table.Append();
                    Table[ExpandColumn(BoldDefs.TIMESTAMPCOLUMN_NAME)] = 0;
                    Table.Post();
                });
            },
            () => ExecQuery(PSParams, SysUtils.Format("INSERT INTO %s (%s) VALUES (0)", TimeStampTable!.SQLName, ExpandColumn(BoldDefs.TIMESTAMPCOLUMN_NAME)), q => q.ExecSQL()),
            "AddFirstTimeStamp");
    }

    [Delphi(PortStatus.Ported)]
    private void AddFirstClock(TBoldPSDefaultParams PSParams)
    {
        BoldLogHandler.BoldLog().Log(BoldPMConsts.sLogWritingFirstClock);
        ByGenerationMode(PSParams,
            () => AppendRows(PSParams, LastClockTable!.SQLName, Table =>
            {
                Table.Append();
                Table[ExpandColumn(BoldDefs.LASTTIMESTAMPCOLUMN_NAME)] = 0;
                Table[ExpandColumn(BoldDefs.LASTCLOCKCOLUMN_NAME)] = 0;
                Table.Post();
            }),
            () => ExecQuery(PSParams, SysUtils.Format("INSERT INTO %s (%s, %s) VALUES (0, :FirstClock)", LastClockTable!.SQLName, BoldDefs.LASTTIMESTAMPCOLUMN_NAME, BoldDefs.LASTCLOCKCOLUMN_NAME), q =>
            {
                q.ParamByName("FirstClock").AsDateTime = SysUtils.ZeroDateTime;
                q.ExecSQL();
            }),
            "AddFirstClock");
    }

    [Delphi(PortStatus.Ported)]
    private void AddTableNames(TBoldPSDefaultParams PSParams)
    {
        BoldLogHandler.BoldLog().Log(BoldPMConsts.sLogWritingTableNames);
        ByGenerationMode(PSParams,
            () => AppendRows(PSParams, Tabletable!.SQLName, Table =>
            {
                for (int i = 0; i < SQLTablesList.Count; i++)
                {
                    Table.Append();
                    Table["TABLENAME"] = SQLTablesList[i].SQLName;
                    Table.Post();
                }
            }),
            () => ExecQuery(PSParams, SysUtils.Format("INSERT INTO %s (TABLENAME) VALUES (:TABLENAME)", Tabletable!.SQLName), q =>
            {
                for (int i = 0; i < SQLTablesList.Count; i++)
                {
                    q.ParamByName("TABLENAME").AsString = SQLTablesList[i].SQLName;
                    q.ExecSQL();
                }
            }),
            "AddTableNames");
    }

    [Delphi(PortStatus.Ported)]
    private void GenerateScriptForFirstID(TStrings Script, string Separator)
    {
        Script.Add(Separator);
        Script.Add(SysUtils.Format("INSERT INTO %s (%s) VALUES (1)", IdTable!.SQLName, BoldDefs.IDCOLUMN_NAME));
    }

    [Delphi(PortStatus.Ported)]
    private void GenerateScriptForFirstTimeStamp(TStrings Script, string Separator)
    {
        Script.Add(Separator);
        Script.Add(SysUtils.Format("INSERT INTO %s (%s) VALUES (0)", TimeStampTable!.SQLName, ExpandColumn(BoldDefs.TIMESTAMPCOLUMN_NAME)));
    }

    [Delphi(PortStatus.Ported)]
    private void GenerateScriptForFirstClock(TStrings Script, string Separator)
    {
        Script.Add(Separator);
        Script.Add(SysUtils.Format("INSERT INTO %s (%s, %s) VALUES (0, %s)", LastClockTable!.SQLName, BoldDefs.LASTTIMESTAMPCOLUMN_NAME, BoldDefs.LASTCLOCKCOLUMN_NAME,
            SysUtils.DateToStr(SysUtils.ZeroDateTime)));
    }

    [Delphi(PortStatus.Ported)]
    private void GenerateScriptForTableNames(TStrings Script, string Separator)
    {
        for (int i = 0; i < SQLTablesList.Count; i++)
        {
            Script.Add(Separator);
            Script.Add(SysUtils.Format("INSERT INTO %s (TABLENAME) VALUES ('%s')", Tabletable!.SQLName, SQLTablesList[i].SQLName));
        }
    }

    /// <summary>The tables listed in Bold's table of tables, when it exists.</summary>
    [Delphi(PortStatus.Ported)]
    protected override void InitializeKnownSystemtables(TStrings KnownTables, TBoldPSSQLParams PSParams)
    {
        IBoldDataBase Database = PSParams.Database!;
        if (Database.TableExists(Tabletable!.SQLName))
        {
            IBoldQuery? Query = Database.GetQuery();
            try
            {
                Query.AssignSQLText(SysUtils.Format("SELECT %s FROM %s", BoldDefs.TABLENAMECOLUMN_NAME, Tabletable.SQLName));
                try
                {
                    Query.Open();
                    while (!Query.Eof)
                    {
                        KnownTables.Add(Query.Fields(0).AsString);
                        Query.Next();
                    }

                    Query.Close();
                }
                catch (Exception)
                {
                    // silence any exceptions (this happens if the table does not exist in the database,
                    // and the databaseinterfaces does not support IBoldTable
                }
            }
            finally
            {
                Database.ReleaseQuery(ref Query);
            }
        }
    }

    /// <summary>Creates the tables, then writes the first id, time stamp, clock and the table names.</summary>
    [Delphi(PortStatus.Ported)]
    public override void CreatePersistentStorage(TBoldPSParams PSParams)
    {
        base.CreatePersistentStorage(PSParams);
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        if (BoldLog.ProcessInterruption())
        {
            return;
        }

        BoldLog.LogHeader = BoldPMConsts.sLogInitializingDefaultPS;
        var PSParamsDefault = (TBoldPSDefaultParams)PSParams;
        if (EffectiveUseTransactions(PSParamsDefault))
        {
            PSParamsDefault.Database!.StartTransaction();
        }

        try
        {
            AddFirstID(PSParamsDefault);
            if (TimeStampTable is not null)
            {
                AddFirstTimeStamp(PSParamsDefault);
            }

            if (LastClockTable is not null)
            {
                AddFirstClock(PSParamsDefault);
            }

            AddTableNames(PSParamsDefault);
            BoldLog.Separator();
        }
        finally
        {
            if (EffectiveUseTransactions(PSParamsDefault))
            {
                BoldLog.Separator();
                BoldLog.Log(BoldPMConsts.sCommittingInitialData);
                PSParamsDefault.Database!.Commit();
            }
        }
    }

    [Delphi(PortStatus.Ported)]
    public override void GenerateDatabaseScript(TStrings Script, string Separator)
    {
        base.GenerateDatabaseScript(Script, Separator);
        GenerateScriptForFirstID(Script, Separator);
        if (TimeStampTable is not null)
        {
            GenerateScriptForFirstTimeStamp(Script, Separator);
        }

        if (LastClockTable is not null)
        {
            GenerateScriptForFirstClock(Script, Separator);
        }

        GenerateScriptForTableNames(Script, Separator);
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? IdTable { get => IdTableField; set => SetIdTable(value); }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? RootTable { get => RootTableField; set => SetRootTable(value); }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? TypeTable { get => TypeTableField; set => SetTypeTable(value); }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? TimeStampTable { get => TimeStampTableField; set => SetTimeStamptable(value); }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? XFilestable { get => XFilesTableField; set => SetXFilestable(value); }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? Tabletable { get => TableTableField; set => SetTabletable(value); }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? LastClockTable { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? ClockLogTable { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? MemberMappingTable { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? AllInstancesMappingTable { get; set; }
    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription? ObjectStorageMappingTable { get; set; }
}
