// Delphi: Source/PMapper/DbEvolutor/BoldDbEvolutor.pas
// Port map: CSharp/port/units/BoldDbEvolutor.md
// Compares the mapping stored in a database with the mapping of the current model and builds the script that evolves
// the database: new tables and columns, instances copied to new tables, data moved between columns, old instances,
// columns and tables removed, and the class db types kept from the old mapping.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDbEvolutor", Symbol = "TExposedPMapper", Note = "A cracker class for the protected CreateMappingInfo, which is protected internal here.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDbEvolutor", Symbol = "TBoldDataBaseEvolutor.Destroy", Note = "Freed the mappings, lists and scripts; the garbage collector does that in .NET.")]

namespace Bold;

[Delphi(PortStatus.Ported)]
public delegate void TBoldMemberMappingPairAction(TBoldMemberMappingInfo NewMemberMapping, TBoldMemberMappingInfo OldMemberMapping, object? Param);

/// <summary>Evolves a database from the mapping stored in it to the mapping of the persistence handle's model.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDataBaseEvolutor
{
    private readonly TBoldAbstractPersistenceHandleDB fPersistenceHandle;
    private readonly TBoldDataBaseEvolutorScript fPreScript;
    private readonly TBoldDataBaseEvolutorScript fScript;
    private readonly TBoldSQLMappingInfo fOldMapping;
    private TBoldSQLMappingInfo? fMergedMapping;
    private readonly TStringList fOldTables;
    private readonly TStringList fNewColumns;
    private readonly TStringList fOldColumns;
    private readonly TStringList fNewTables;
    private readonly TStringList fExistingInstances;
    private bool fGenericScript;
    private readonly TBoldMemberMappingList fUnhandledMemberMappings;

    /// <summary>Releases the handle's controller and makes an empty mapping for what is stored in the database.</summary>
    [Delphi(PortStatus.Ported)]
    public TBoldDataBaseEvolutor(TBoldAbstractPersistenceHandleDB PersistenceHandle)
    {
        fPersistenceHandle = PersistenceHandle;
        fPersistenceHandle.ReleasePersistenceController();
        fOldMapping = PMapper.CreateMappingInfo();
        fOldTables = new TStringList { Sorted = true };
        fNewTables = new TStringList { Sorted = true };
        fOldColumns = new TStringList { Sorted = true };
        fNewColumns = new TStringList { Sorted = true };
        fScript = new TBoldDataBaseEvolutorScript();
        fPreScript = new TBoldDataBaseEvolutorScript();
        fUnhandledMemberMappings = new TBoldMemberMappingList();
        fExistingInstances = new TStringList();
    }

    /// <summary>The columns that are new in a table that already exists.</summary>
    [Delphi(PortStatus.Ported)]
    protected void AddNewColumns()
    {
        for (int i = 0; i < NewColumns.Count; i++)
        {
            string TableName = SysUtils.Copy(NewColumns[i], 1, NewColumns[i].IndexOf('.'));
            string ColumnName = NewColumns[i][(NewColumns[i].IndexOf('.') + 1)..];
            if (OldColumns.IndexOf(NewColumns[i]) == -1 && OldTables.IndexOf(TableName) != -1)
            {
                TBoldSQLTableDescription NewTable = NewPSDescription.SQLTablesList.ItemsBySQLName(TableName)!;
                if (NewTable.ColumnsList.ItemsBySQLName(ColumnName) is TBoldSQLColumnDescription NewColumn)
                {
                    Script.AddColumn(NewColumn);
                }
            }
        }
    }

    /// <summary>Copies the instances of each class that has old instances into the new tables it was not stored in before.</summary>
    [Delphi(PortStatus.Ported)]
    protected void AddNewInstances()
    {
        // in order not to add data more than once, we must loop over the PMapper, and not the mappinginfo
        var NewTables = new TStringList();
        for (int i = 0; i < PMapper.ObjectPersistenceMappers.Count; i++)
        {
            if (PMapper.ObjectPersistenceMappers[i] is { } ObjectMapper)
            {
                string ExprName = ObjectMapper.ExpressionName;
                string OldExprName = TranslateClassExpressionName(ExprName, NewMapping, OldMapping);
                if (OldExprName != "" && HasOldInstances(OldExprName))
                {
                    GetAllTablesForClass(ExprName, NewMapping, NewTables);
                    for (int t = 0; t < NewTables.Count; t++)
                    {
                        if (!HasStorageMapping(OldExprName, NewTables[t], OldMapping))
                        {
                            TBoldAllInstancesMappingInfo[] OldAllInstances = OldMapping.GetAllInstancesMapping(OldExprName);
                            // what if more than 1 or = 0
                            if (OldAllInstances.Length == 1)
                            {
                                Script.CopyInstances(ExprName, OldAllInstances[0].TableName, NewTables[t],
                                    GetCommonPrimaryKeyColumns(GetPrimaryIndexForExistingTable(OldAllInstances[0].TableName), GetPrimaryIndexForNewTable(NewTables[t])),
                                    OldMapping.GetDbTypeMapping(OldExprName));
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>The new tables, each registered in the table of tables.</summary>
    [Delphi(PortStatus.Ported)]
    protected void AddNewTables()
    {
        for (int i = 0; i < NewTables.Count; i++)
        {
            if (OldTables.IndexOf(NewTables[i]) == -1)
            {
                TBoldSQLTableDescription NewTable = NewPSDescription.SQLTablesList.ItemsBySQLName(NewTables[i])!;
                Script.AddTable(NewTable);
                Script.AddSQLStatement(SysUtils.Format("INSERT INTO %s (%s) VALUES ('%s')",
                    BoldNameExpander.BoldExpandPrefix(BoldDefs.TABLETABLE_NAME, "", PersistenceHandle.SQLDatabaseConfig.SystemTablePrefix,
                        NewPSDescription.SQLDatabaseConfig.MaxDbIdentifierLength, NewPSDescription.NationalCharConversion),
                    BoldDefs.TABLENAMECOLUMN_NAME, NewTable.SQLName));
            }
        }
    }

    /// <summary>Deletes the instances of classes that are gone, or no longer stored in a table that stays.</summary>
    [Delphi(PortStatus.Ported)]
    protected void DeleteOldInstances()
    {
        for (int i = 0; i < OldMapping.ObjectStorageMappings.Count; i++)
        {
            string OldExprName = OldMapping.ObjectStorageMappings[i].ClassExpressionName;
            string OldTableName = OldMapping.ObjectStorageMappings[i].TableName;
            string NewExprName = TranslateClassExpressionName(OldExprName, OldMapping, NewMapping);
            // either the class does not exist anymore, or it is no longer stored in that table
            if (HasOldInstances(OldExprName) && NewTables.IndexOf(OldTableName) != -1 &&
                (NewExprName == "" || !HasStorageMapping(NewExprName, OldTableName, NewMapping)))
            {
                Script.DeleteInstances(OldExprName, OldTableName, OldMapping.GetDbTypeMapping(OldExprName));
            }
        }
    }

    /// <summary>Drops the columns no longer mapped in tables that stay, and the indexes on them.</summary>
    [Delphi(PortStatus.Ported)]
    protected void DropOldColumns()
    {
        var IndexedColumns = new TStringList { Sorted = true };
        OldColumns.Sort();
        IBoldTable? Table = PersistenceHandle.DatabaseInterface.GetTable();
        try
        {
            for (int i = 0; i < OldColumns.Count; i++)
            {
                string TableName = SysUtils.Copy(OldColumns[i], 1, OldColumns[i].IndexOf('.'));
                string ColumnName = OldColumns[i][(OldColumns[i].IndexOf('.') + 1)..];
                if (NewColumns.IndexOf(OldColumns[i]) == -1 && NewTables.IndexOf(TableName) != -1)
                {
                    // first time and every new table
                    if (i == 0 || TableName != Table.Tablename)
                    {
                        // retrieve fresh indexdefs
                        Table.Tablename = TableName;
                        Table.IndexDefs.Update();
                    }

                    // see if any indices needs to be dropped
                    for (int Def = 0; Def < Table.IndexDefs.Count; Def++)
                    {
                        IndexedColumns.CommaText = Table.IndexDefs[Def].Fields.Replace(';', ',').ToUpperInvariant();
                        if (IndexedColumns.IndexOf(ColumnName.ToUpperInvariant()) != -1)
                        {
                            Script.DropIndex(Table.IndexDefs[Def].Name, Table.Tablename);
                        }
                    }

                    Script.DropColumn(TableName, ColumnName);
                }
            }
        }
        finally
        {
            PersistenceHandle.DatabaseInterface.ReleaseTable(ref Table);
        }
    }

    /// <summary>Drops the tables no longer mapped, and removes them from the table of tables.</summary>
    [Delphi(PortStatus.Ported)]
    protected void DropOldTables()
    {
        for (int i = 0; i < OldTables.Count; i++)
        {
            if (NewTables.IndexOf(OldTables[i]) == -1)
            {
                Script.DropTable(OldTables[i]);
                Script.AddSQLStatement(SysUtils.Format("DELETE FROM %s WHERE UPPER(%s)='%s'",
                    BoldNameExpander.BoldExpandPrefix(BoldDefs.TABLETABLE_NAME, "", PersistenceHandle.SQLDatabaseConfig.SystemTablePrefix,
                        NewPSDescription.SQLDatabaseConfig.MaxDbIdentifierLength, NewPSDescription.NationalCharConversion),
                    BoldDefs.TABLENAMECOLUMN_NAME, OldTables[i].ToUpperInvariant()));
            }
        }
    }

    /// <summary>Reads the old mapping from the closed handle's database and builds the scripts.</summary>
    [Delphi(PortStatus.Ported)]
    public void CalculateScript()
    {
        if (PersistenceHandle.Active)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sPersistenceHandleActive, GetType().Name, PersistenceHandle.Site?.Name ?? ""));
        }

        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        BoldLog.LogHeader = BoldPMConsts.sInitializingScript;
        BoldLog.ProgressMax = 10;
        try
        {
            PMapper.OpenDatabase(false, false);
            BoldLog.ProgressStep();
            fOldMapping.ReadDataFromDB(PersistenceHandle.DatabaseInterface, true, true);
            BoldLog.ProgressStep();
            fUnhandledMemberMappings.Clear();
            fUnhandledMemberMappings.FillFromList(OldMapping.MemberMappings);
            if (!GenericScript)
            {
                LoadExistingInstances();
            }

            InitializeTableData(NewTables, NewColumns, NewMapping);
            InitializeTableData(OldTables, OldColumns, OldMapping);
            DetectTypeClashes();
            BoldLog.ProgressStep();
            AddNewTables();
            BoldLog.ProgressStep();
            AddNewColumns();
            BoldLog.ProgressStep();
            AddNewInstances();
            BoldLog.ProgressStep();
            MoveData();
            BoldLog.ProgressStep();
            DeleteOldInstances();
            BoldLog.ProgressStep();
            DropOldColumns();
            BoldLog.ProgressStep();
            DropOldTables();
            BoldLog.ProgressStep();
            Script.OptimizeScript();
            MergeOldDbTypes();
        }
        finally
        {
            PMapper.CloseDataBase();
        }
    }

    /// <summary>The tables a class is stored in, and those listing all its instances.</summary>
    [Delphi(PortStatus.Ported)]
    private void GetAllTablesForClass(string ExpressionName, TBoldSQLMappingInfo Mapping, TStringList Tables)
    {
        Tables.Clear();
        foreach (TBoldObjectStorageMappingInfo m in Mapping.GetObjectStorageMapping(ExpressionName))
        {
            Tables.Add(m.TableName);
        }

        foreach (TBoldAllInstancesMappingInfo m in Mapping.GetAllInstancesMapping(ExpressionName))
        {
            Tables.Add(m.TableName);
        }
    }

    [Delphi(PortStatus.Ported)] private TBoldSQLMappingInfo GetNewMapping() => PMapper.MappingInfo;
    [Delphi(PortStatus.Ported)] private TBoldSQLSystemDescription GetNewPSDescription() => PMapper.PSSystemDescription;
    [Delphi(PortStatus.Ported)] private TBoldSystemSQLMapper GetPMapper() => PersistenceHandle.PersistenceControllerDefault.PersistenceMapper;

    /// <summary>Whether the old class can have stored instances, and (for a script for this database) has some.</summary>
    [Delphi(PortStatus.Ported)]
    private bool HasOldInstances(string OldExpressionName)
    {
        bool result = CanHaveOldInstances(OldExpressionName);
        if (!GenericScript && result)
        {
            TBoldDbType dbTypeMapping = OldMapping.GetDbTypeMapping(OldExpressionName);
            result = dbTypeMapping != BoldDefs.NO_CLASS && fExistingInstances.IndexOf(dbTypeMapping.ToString()) != -1;
        }

        return result;
    }

    [Delphi(PortStatus.Ported)]
    private bool HasStorageMapping(string ExpressionName, string TableName, TBoldSQLMappingInfo Mapping)
    {
        var Tables = new TStringList();
        GetAllTablesForClass(ExpressionName, Mapping, Tables);
        for (int i = 0; i < Tables.Count; i++)
        {
            if (SysUtils.SameText(Tables[i], TableName))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The tables and the TABLE.COLUMN names of a mapping.</summary>
    [Delphi(PortStatus.Ported)]
    protected void InitializeTableData(TStringList TableList, TStringList ColumnList, TBoldSQLMappingInfo MappingInfo)
    {
        static void AddName(string Name, TStringList List)
        {
            if (List.IndexOf(Name) == -1)
            {
                List.Add(Name);
            }
        }

        for (int i = 0; i < MappingInfo.ObjectStorageMappings.Count; i++)
        {
            AddName(MappingInfo.ObjectStorageMappings[i].TableName, TableList);
        }

        for (int i = 0; i < MappingInfo.AllInstancesMappings.Count; i++)
        {
            AddName(MappingInfo.AllInstancesMappings[i].TableName, TableList);
        }

        var Columns = new TStringList();
        for (int i = 0; i < MappingInfo.MemberMappings.Count; i++)
        {
            AddName(MappingInfo.MemberMappings[i].TableName, TableList);
            Columns.CommaText = MappingInfo.MemberMappings[i].Columns;
            for (int j = 0; j < Columns.Count; j++)
            {
                AddName(MappingInfo.MemberMappings[i].TableName + "." + Columns[j], ColumnList);
            }
        }
    }

    /// <summary>The db types that have rows in the old root table.</summary>
    [Delphi(PortStatus.Ported)]
    private void LoadExistingInstances()
    {
        IBoldQuery? Query = PersistenceHandle.DatabaseInterface.GetQuery();
        try
        {
            Query.AssignSQLText(SysUtils.Format("SELECT DISTINCT BOLD_TYPE FROM %s", OldRootTableName()));
            Query.Open();
            while (!Query.Eof)
            {
                fExistingInstances.Add(Query.Fields(0).AsString);
                Query.Next();
            }

            Query.Close();
        }
        finally
        {
            PersistenceHandle.DatabaseInterface.ReleaseQuery(ref Query);
        }
    }

    /// <summary>Calls the action for each member mapped once in both the new and the old mapping.</summary>
    [Delphi(PortStatus.Ported)]
    private void ForEachMemberMappingPair(TBoldMemberMappingPairAction Action, bool IgnoreInstances, object? Param = null)
    {
        // in order not to move data more than once, we must loop over the PMapper, and not the mappinginfo
        for (int i = 0; i < PMapper.ObjectPersistenceMappers.Count; i++)
        {
            if (PMapper.ObjectPersistenceMappers[i] is { } ObjectMapper)
            {
                string NewExprName = ObjectMapper.ExpressionName;
                string OldExprName = TranslateClassExpressionName(NewExprName, NewMapping, OldMapping);
                if (OldExprName != "" && (IgnoreInstances || HasOldInstances(OldExprName)))
                {
                    for (int j = 0; j < ObjectMapper.MemberPersistenceMappers.Count; j++)
                    {
                        if (ObjectMapper.MemberPersistenceMappers[j] is { } MemberMapper)
                        {
                            string NewMemberName = MemberMapper.ExpressionName;
                            TBoldMemberMappingInfo[] NewMemberMappings = NewMapping.GetMemberMappings(NewExprName, NewMemberName);
                            // do we need to worry about lengths = 0, lengths > 1?
                            if (NewMemberMappings.Length == 1)
                            {
                                string OldMemberName = TranslateMemberName(NewExprName, NewMemberName, OldExprName, NewMapping, OldMapping);
                                TBoldMemberMappingInfo[] OldMemberMappings = OldMapping.GetMemberMappings(OldExprName, OldMemberName);
                                if (OldMemberMappings.Length == 1)
                                {
                                    Action(NewMemberMappings[0], OldMemberMappings[0], Param);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>Moves the member's data when it is stored elsewhere now by the same mapper.</summary>
    [Delphi(PortStatus.Ported)]
    private void MoveDataAction(TBoldMemberMappingInfo NewMemberMapping, TBoldMemberMappingInfo OldMemberMapping, object? Param)
    {
        MarkMemberMappingHandled(fUnhandledMemberMappings, OldMemberMapping);
        if (!SysUtils.SameText(NewMemberMapping.TableName + "." + NewMemberMapping.Columns, OldMemberMapping.TableName + "." + OldMemberMapping.Columns) &&
            NewMemberMapping.MapperName == OldMemberMapping.MapperName)
        {
            MoveDataBetweenMappings(NewMemberMapping, OldMemberMapping);
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void MoveData() => ForEachMemberMappingPair(MoveDataAction, false);

    [Delphi(PortStatus.Ported)]
    private void MoveDataBetweenMappings(TBoldMemberMappingInfo NewMemberMapping, TBoldMemberMappingInfo OldMemberMapping)
    {
        var NewColumns = new TStringList { CommaText = NewMemberMapping.Columns };
        var OldColumns = new TStringList { CommaText = OldMemberMapping.Columns };
        for (int i = 0; i < Math.Min(NewColumns.Count, OldColumns.Count); i++)
        {
            Script.MoveData(OldMemberMapping.TableName, NewMemberMapping.TableName, OldColumns[i], NewColumns[i],
                GetCommonPrimaryKeyColumns(GetPrimaryIndexForExistingTable(OldMemberMapping.TableName), GetPrimaryIndexForNewTable(NewMemberMapping.TableName)),
                OldMapping.GetDbTypeMapping(OldMemberMapping.ClassExpressionName));
        }
    }

    /// <summary>The table most classes were stored in (the old root table); BOLD_OBJECT if there are none.</summary>
    [Delphi(PortStatus.Ported)]
    private string OldRootTableName()
    {
        string result = "BOLD_OBJECT";
        var Tables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var Order = new List<string>();
        void Count(string TableName)
        {
            if (!Tables.TryAdd(TableName, 1))
            {
                Tables[TableName]++;
            }
            else
            {
                Order.Add(TableName);
            }
        }

        if (OldMapping.ObjectStorageMappings.Count == 0 && OldMapping.AllInstancesMappings.Count == 1)
        {
            // there is only the rootclass, it has no objectstorage, only AllInstances-mapping
            Count(OldMapping.AllInstancesMappings[0].TableName);
        }

        for (int i = 0; i < OldMapping.ObjectStorageMappings.Count; i++)
        {
            Count(OldMapping.ObjectStorageMappings[i].TableName);
        }

        int max = 0;
        foreach (string TableName in Order)
        {
            if (Tables[TableName] > max)
            {
                max = Tables[TableName];
                result = TableName;
            }
        }

        return result;
    }

    /// <summary>The class's name in the other mapping: the same name, or the name of the class with the same db type.</summary>
    [Delphi(PortStatus.Ported)]
    private string TranslateClassExpressionName(string ExpressionName, TBoldSQLMappingInfo SourceMapping, TBoldSQLMappingInfo DestMapping)
    {
        if (DestMapping.GetDbTypeMapping(ExpressionName) != BoldDefs.NO_CLASS)
        {
            return ExpressionName;
        }

        TBoldDbType dbType = SourceMapping.GetDbTypeMapping(ExpressionName);
        if (dbType == BoldDefs.NO_CLASS)
        {
            throw new EBold(SysUtils.Format(BoldPMConsts.sUnableToFindDBID, GetType().Name, ExpressionName));
        }

        for (int i = 0; i < SourceMapping.DbTypeMappings.Count; i++)
        {
            string TestName = SourceMapping.DbTypeMappings[i].ClassExpressionName;
            if (SourceMapping.DbTypeMappings[i].DbType == dbType && DestMapping.GetDbTypeMapping(TestName) != BoldDefs.NO_CLASS)
            {
                // oh, we found the testname in the destination mapping, then it is what we were looking for.
                return TestName;
            }
        }

        return "";
    }

    /// <summary>The member's name in the other mapping: the same name, or that of a member of the class with the same storage.</summary>
    [Delphi(PortStatus.Ported)]
    private string TranslateMemberName(string SourceExprName, string SourceMemberName, string DestExprName, TBoldSQLMappingInfo SourceMapping, TBoldSQLMappingInfo DestMapping)
    {
        if (DestMapping.GetMemberMappings(DestExprName, SourceMemberName).Length == 1)
        {
            return SourceMemberName;
        }

        TBoldMemberMappingInfo[] SourceMemberMappings = SourceMapping.GetMemberMappings(SourceExprName, SourceMemberName);
        if (SourceMemberMappings.Length == 1)
        {
            string Mapping = SourceMemberMappings[0].TableName + "." + SourceMemberMappings[0].Columns;
            for (int i = 0; i < SourceMapping.MemberMappings.Count; i++)
            {
                // if they have the same class name, and the same mapping, it is the same member
                if (SysUtils.SameText(SourceMapping.MemberMappings[i].ClassExpressionName, SourceExprName) &&
                    SysUtils.SameText(SourceMapping.MemberMappings[i].TableName + "." + SourceMapping.MemberMappings[i].Columns, Mapping) &&
                    DestMapping.GetMemberMappings(DestExprName, SourceMapping.MemberMappings[i].MemberName).Length == 1)
                {
                    return SourceMapping.MemberMappings[i].MemberName;
                }
            }
        }

        return "";
    }

    /// <summary>Runs the scripts on the database and writes the merged mapping.</summary>
    [Delphi(PortStatus.Ported)]
    public void ExecuteScript()
    {
        PersistenceHandle.DatabaseInterface.Open();
        try
        {
            PreScript.InternalLog.Clear();
            Script.InternalLog.Clear();
            PreScript.UpdateDatabase(PersistenceHandle.DatabaseInterface, PersistenceHandle.SQLDatabaseConfig);
            Script.UpdateDatabase(PersistenceHandle.DatabaseInterface, PersistenceHandle.SQLDatabaseConfig);
            fMergedMapping!.WriteDataToDB(PersistenceHandle.DatabaseInterface);
        }
        finally
        {
            PersistenceHandle.DatabaseInterface.Close();
        }
    }

    /// <summary>The new mapping with each class's old db type kept; new classes get unused db types.</summary>
    [Delphi(PortStatus.Ported)]
    protected void MergeOldDbTypes()
    {
        fMergedMapping = NewMapping.CloneWithoutDbType();
        int FirstUnused = OldMapping.HighestUsedDbType + 1;
        for (int i = 0; i < NewMapping.DbTypeMappings.Count; i++)
        {
            string NewExprName = NewMapping.DbTypeMappings[i].ClassExpressionName;
            string OldExprName = TranslateClassExpressionName(NewExprName, NewMapping, OldMapping);
            TBoldDbType DbType = OldMapping.GetDbTypeMapping(OldExprName);
            if (DbType == BoldDefs.NO_CLASS)
            {
                // try to locate a previously added type with the same dbtype in the new typemapping
                for (int j = 0; j < i; j++)
                {
                    if (NewMapping.DbTypeMappings[i].DbType == NewMapping.DbTypeMappings[j].DbType)
                    {
                        TBoldClassMappingInfo[] x = fMergedMapping.DbTypeMappings.MappingsForClassName(NewMapping.DbTypeMappings[j].ClassExpressionName);
                        if (x.Length == 1)
                        {
                            DbType = ((TBoldDbTypeMappingInfo)x[0]).DbType;
                            break;
                        }
                    }
                }

                if (DbType == BoldDefs.NO_CLASS)
                {
                    DbType = FirstUnused;
                    FirstUnused++;
                }
            }

            fMergedMapping.AddTypeIdMapping(NewExprName, DbType);
        }
    }

    /// <summary>Writes the database script and the script that writes the merged mapping.</summary>
    [Delphi(PortStatus.Ported)]
    public void GenerateScript(TStrings DbScript, TStrings MappingScript)
    {
        DbScript.BeginUpdate();
        PreScript.GenerateScript(DbScript, PersistenceHandle.SQLDatabaseConfig);
        Script.GenerateScript(DbScript, PersistenceHandle.SQLDatabaseConfig);
        DbScript.EndUpdate();
        MappingScript.BeginUpdate();
        AddCommandToScript(MappingScript, PersistenceHandle.SQLDatabaseConfig.SqlScriptStartTransaction);
        fMergedMapping!.ScriptForWriteData(MappingScript, PersistenceHandle.SQLDatabaseConfig.SqlScriptSeparator, true, PersistenceHandle.SQLDatabaseConfig.SqlScriptTerminator);
        AddCommandToScript(MappingScript, PersistenceHandle.SQLDatabaseConfig.SqlScriptCommitTransaction);
        MappingScript.EndUpdate();
    }

    /// <summary>Removes the old member mapping, and those of the same db type, table and columns (the same attribute).</summary>
    [Delphi(PortStatus.Ported)]
    private void MarkMemberMappingHandled(TBoldMemberMappingList UnhandledMemberMappings, TBoldMemberMappingInfo OldMemberMapping)
    {
        TBoldDbType OldDbType = OldMapping.GetDbTypeMapping(OldMemberMapping.ClassExpressionName);
        if (OldDbType != BoldDefs.NO_CLASS)
        {
            for (int i = UnhandledMemberMappings.Count - 1; i >= 0; i--)
            {
                // if the dbtype, table and columns match then it is the same attribute...
                if (OldMapping.GetDbTypeMapping(UnhandledMemberMappings[i].ClassExpressionName) == OldDbType &&
                    SysUtils.SameText(UnhandledMemberMappings[i].TableName, OldMemberMapping.TableName) &&
                    SysUtils.SameText(UnhandledMemberMappings[i].Columns, OldMemberMapping.Columns))
                {
                    UnhandledMemberMappings.RemoveByIndex(i);
                }
            }
        }
        else
        {
            UnhandledMemberMappings.Remove(OldMemberMapping);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void DetectMapperChange(TBoldMemberMappingInfo NewMemberMapping, TBoldMemberMappingInfo OldMemberMapping, object? Param)
    {
        if (NewMemberMapping.MapperName != OldMemberMapping.MapperName && OldMemberMapping.MapperName != "")
        {
            ((TStrings)Param!).Add(SysUtils.Format(BoldPMConsts.sMemberChangedMapper,
                NewMemberMapping.ClassExpressionName, NewMemberMapping.MemberName, OldMemberMapping.MapperName, NewMemberMapping.MapperName,
                OldMemberMapping.Columns, OldMemberMapping.TableName, NewMemberMapping.Columns, NewMemberMapping.TableName));
        }
    }

    /// <summary>Warns of changed mappers, classes that became abstract, and data that will be lost.</summary>
    [Delphi(PortStatus.Ported)]
    public void GenerateWarnings(TStrings Info)
    {
        ForEachMemberMappingPair(DetectMapperChange, true, Info);
        for (int i = 0; i < PMapper.ObjectPersistenceMappers.Count; i++)
        {
            if (PMapper.ObjectPersistenceMappers[i] is { } ObjectMapper && NewMapping.GetObjectStorageMapping(ObjectMapper.ExpressionName).Length == 0)
            {
                // New class is persistent and abstract
                string NewExprName = ObjectMapper.ExpressionName;
                string OldExprName = TranslateClassExpressionName(NewExprName, NewMapping, OldMapping);
                if (OldExprName != "" && OldMapping.GetObjectStorageMapping(OldExprName).Length > 0)
                {
                    if (GenericScript)
                    {
                        Info.Add(SysUtils.Format(BoldPMConsts.sClassBecameAbstract, OldExprName, NewExprName));
                    }
                    else if (HasOldInstances(OldExprName))
                    {
                        Info.Add(SysUtils.Format(BoldPMConsts.sUnableToHandleInstancesOfAbstract, OldExprName, NewExprName));
                    }
                }
            }
        }

        var UnhandledMemberMappings = new TBoldMemberMappingList();
        UnhandledMemberMappings.FillFromList(fUnhandledMemberMappings);
        while (UnhandledMemberMappings.Count > 0)
        {
            TBoldMemberMappingInfo First = UnhandledMemberMappings[0];
            if (HasOldInstances(First.ClassExpressionName))
            {
                string s = SysUtils.Format(BoldPMConsts.sDataStoredInXForYWillBeLost, First.TableName, First.Columns, First.ClassExpressionName, First.MemberName);
                string NewClassExpressionName = TranslateClassExpressionName(First.ClassExpressionName, OldMapping, NewMapping);
                if (NewClassExpressionName != First.ClassExpressionName)
                {
                    s += NewClassExpressionName == "" ? BoldPMConsts.sClassNoLongerExists : SysUtils.Format(BoldPMConsts.sNewNameForClass, NewClassExpressionName);
                }

                Info.Add(s);
            }

            MarkMemberMappingHandled(UnhandledMemberMappings, First);
        }
    }

    /// <summary>Whether the old class has a db type and is stored (abstract classes are not).</summary>
    [Delphi(PortStatus.Ported)]
    private bool CanHaveOldInstances(string OldExpressionName)
    {
        TBoldDbType dbTypeMapping = OldMapping.GetDbTypeMapping(OldExpressionName);
        // Abstract classes has no ObjectStorage mapping
        return dbTypeMapping != BoldDefs.NO_CLASS && OldMapping.GetObjectStorageMapping(OldExpressionName).Length > 0;
    }

    /// <summary>The columns of the first key that are also in the second, comma separated.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetCommonPrimaryKeyColumns(string PrimaryKey1, string PrimaryKey2)
    {
        var Fields1 = new TStringList { CommaText = PrimaryKey1 };
        var Fields2 = new TStringList { CommaText = PrimaryKey2.ToUpperInvariant() };
        Fields2.Sorted = true;
        for (int i = Fields1.Count - 1; i >= 0; i--)
        {
            if (Fields2.IndexOf(Fields1[i].ToUpperInvariant()) == -1)
            {
                Fields1.Delete(i);
            }
        }

        return Fields1.CommaText;
    }

    /// <summary>The primary key columns of a table in the database, comma separated.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetPrimaryIndexForExistingTable(string TableName)
    {
        IBoldTable? Table = PersistenceHandle.DatabaseInterface.GetTable();
        try
        {
            Table.Tablename = TableName;
            Table.IndexDefs.Update();
            for (int i = 0; i < Table.IndexDefs.Count; i++)
            {
                if (Table.IndexDefs[i].Options.Contains(TIndexOption.ixPrimary))
                {
                    return Table.IndexDefs[i].Fields.Replace(';', ',');
                }
            }

            return "";
        }
        finally
        {
            PersistenceHandle.DatabaseInterface.ReleaseTable(ref Table);
        }
    }

    /// <summary>The primary key columns of a table of the new model, comma separated.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetPrimaryIndexForNewTable(string TableName)
    {
        if (PMapper.PSSystemDescription.SQLTablesList.ItemsBySQLName(TableName) is { } TableDesc)
        {
            for (int i = 0; i < TableDesc.IndexList.Count; i++)
            {
                if (TableDesc.IndexList[i].IndexOptions.Contains(TIndexOption.ixPrimary))
                {
                    return TableDesc.IndexList[i].IndexedFieldsForSQL; // this has commas instead of semicolons as separator
                }
            }
        }

        return "";
    }

    [Delphi(PortStatus.Ported)]
    protected void DetectTypeClashes() => ForEachMemberMappingPair(DetectTypeClashesAction, true);

    /// <summary>A member whose mapper changed: its old columns are dropped first, and its new columns added.</summary>
    [Delphi(PortStatus.Ported)]
    private void DetectTypeClashesAction(TBoldMemberMappingInfo NewMemberMapping, TBoldMemberMappingInfo OldMemberMapping, object? Param)
    {
        if (NewMemberMapping.MapperName != OldMemberMapping.MapperName && OldMemberMapping.MapperName != "")
        {
            var Columns = new TStringList { CommaText = OldMemberMapping.Columns };
            for (int i = 0; i < Columns.Count; i++)
            {
                fPreScript.DropColumn(OldMemberMapping.TableName, Columns[i]);
            }

            TBoldSQLTableDescription TableDesc = NewPSDescription.SQLTablesList.ItemsBySQLName(NewMemberMapping.TableName)!;
            Columns.CommaText = NewMemberMapping.Columns;
            for (int i = 0; i < Columns.Count; i++)
            {
                fScript.AddColumn((TBoldSQLColumnDescription)TableDesc.ColumnsList.ItemsBySQLName(Columns[i])!);
            }
        }
    }

    /// <summary>The statements the last run executed.</summary>
    [Delphi(PortStatus.Ported)]
    public void GenerateExecutedStatements(TStrings Info)
    {
        Info.Clear();
        Info.AddStrings(PreScript.InternalLog);
        Info.AddStrings(Script.InternalLog);
    }

    [Delphi(PortStatus.Ported)]
    private void AddCommandToScript(TStrings Script, string s)
    {
        Script.Add(s + PersistenceHandle.SQLDatabaseConfig.SqlScriptTerminator);
        if (PersistenceHandle.SQLDatabaseConfig.SqlScriptSeparator != "")
        {
            Script.Add(PersistenceHandle.SQLDatabaseConfig.SqlScriptSeparator);
        }
    }

    [Delphi(PortStatus.Ported)] protected TBoldSQLMappingInfo OldMapping => fOldMapping;
    [Delphi(PortStatus.Ported)] protected TBoldSQLMappingInfo NewMapping => GetNewMapping();
    [Delphi(PortStatus.Ported)] protected TStringList NewTables => fNewTables;
    [Delphi(PortStatus.Ported)] protected TStringList OldTables => fOldTables;
    [Delphi(PortStatus.Ported)] protected TStringList NewColumns => fNewColumns;
    [Delphi(PortStatus.Ported)] protected TStringList OldColumns => fOldColumns;
    [Delphi(PortStatus.Ported)] protected TBoldSQLSystemDescription NewPSDescription => GetNewPSDescription();
    [Delphi(PortStatus.Ported)] protected TBoldAbstractPersistenceHandleDB PersistenceHandle => fPersistenceHandle;
    [Delphi(PortStatus.Ported)] protected TBoldSystemSQLMapper PMapper => GetPMapper();
    [Delphi(PortStatus.Ported)] protected TBoldDataBaseEvolutorScript Script => fScript;
    [Delphi(PortStatus.Ported)] protected TBoldDataBaseEvolutorScript PreScript => fPreScript;
    [Delphi(PortStatus.Ported)] public bool GenericScript { get => fGenericScript; set => fGenericScript = value; }
}
