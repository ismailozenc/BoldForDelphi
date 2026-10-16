// Delphi: Source/PMapper/Validator/BoldDbStructureValidator.pas
// Port map: CSharp/port/units/BoldDbStructureValidator.md
// Checks that the database has the tables, columns and indexes of the model, and no nulls in mandatory columns;
// classes without a db type are added after asking (BoldPSDescriptionsSQL.QueryUserHandler, BoldQueryUserDlg in Delphi).

namespace Bold;

/// <summary>Validates the structure of the database.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDbStructureValidator : TBoldDbValidator
{
    private IBoldTable? fCurrentTable;

    /// <summary>A structure validator.</summary>
    /// <remarks>The inherited constructor (C# does not inherit constructors).</remarks>
    public TBoldDbStructureValidator(System.ComponentModel.IContainer? owner = null) : base(owner)
    {
    }

    /// <summary>Adds the db types missing from the stored mapping (if the user agrees), then checks each table.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi's query dialog is the BoldPSDescriptionsSQL.QueryUserHandler callback; without one the answer is no.")]
    public override void Validate()
    {
        TBoldLogHandler BoldLog = BoldLogHandler.BoldLog();
        try
        {
            TBoldSystemDefaultMapper PMapper = PersistenceHandle!.PersistenceControllerDefault.PersistenceMapper;
            TBoldSQLMappingInfo MappingInfo = PMapper.MappingInfo;
            PersistenceHandle.DatabaseInterface.Open();
            MappingInfo.ReadDataFromDB(PersistenceHandle.DatabaseInterface, true, true);
            bool MappingsAdded = false;
            TBoldQueryResult QueryRes = TBoldQueryResult.qrNo;
            for (int i = 0; i < PMapper.ObjectPersistenceMappers.Count; i++)
            {
                TBoldObjectPersistenceMapper ObjectMapper = PMapper.ObjectPersistenceMappers[i]!;
                if (MappingInfo.GetDbTypeMapping(ObjectMapper.ExpressionName) == BoldDefs.NO_CLASS)
                {
                    if (QueryRes != TBoldQueryResult.qrYesAll)
                    {
                        QueryRes = BoldPSDescriptionsSQL.QueryUserHandler?.Invoke(BoldPMConsts.sMissingID, SysUtils.Format(BoldPMConsts.sAnIDWasMissing, ObjectMapper.ExpressionName))
                            ?? TBoldQueryResult.qrNo;
                    }

                    if (QueryRes is TBoldQueryResult.qrYesAll or TBoldQueryResult.qrYes)
                    {
                        MappingInfo.AddTypeIdMapping(ObjectMapper.ExpressionName, MappingInfo.HighestUsedDbType + 1);
                        MappingsAdded = true;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            if (MappingsAdded)
            {
                MappingInfo.WriteDataToDB(PersistenceHandle.DatabaseInterface);
            }
        }
        finally
        {
            PersistenceHandle!.DatabaseInterface.Close();
        }

        PersistenceHandle.Active = true;
        try
        {
            BoldLog.ProgressMax = SystemSQLMapper.AllTables.Count - 1;
            for (int i = 0; i < SystemSQLMapper.AllTables.Count; i++)
            {
                ValidateTable(SystemSQLMapper.AllTables[i]);
                BoldLog.LogHeader = SysUtils.Format(BoldPMConsts.sCheckingTable, SystemSQLMapper.AllTables[i].SQLName);
                BoldLog.Progress = i;
            }
        }
        finally
        {
            PersistenceHandle.Active = false;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void ValidateColumnsForTable(TBoldSQLTableDescription BoldSQLTableDescription)
    {
        for (int i = 0; i < BoldSQLTableDescription.ColumnsList.Count; i++)
        {
            ValidateColumn((TBoldSQLColumnDescription)BoldSQLTableDescription.ColumnsList[i]!);
        }
    }

    /// <summary>A missing column gets an ALTER TABLE remedy; a mandatory one is checked for nulls.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ValidateColumn(TBoldSQLColumnDescription BoldSQLColumnDescription)
    {
        string TableName = BoldSQLColumnDescription.TableDescription.SQLName;
        string ColumnName = BoldSQLColumnDescription.SQLName;
        if (CurrentTable.FindField(ColumnName) is null)
        {
            BoldLogHandler.BoldLog().LogFmt(BoldPMConsts.sColumnMissing, [TableName, ColumnName, BoldSQLColumnDescription.SQLType]);
            remedy.Add(SysUtils.Format("alter table %s add %s %s %s;", TableName, ColumnName, BoldSQLColumnDescription.SQLType, BoldSQLColumnDescription.SQLAllowNull));
        }
        else if (BoldSQLColumnDescription.Mandatory)
        {
            ValidateNotNullForColumn(BoldSQLColumnDescription);
        }
    }

    /// <summary>A missing index gets a remedy that adds it.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ValidateIndex(TBoldSQLIndexDescription BoldSQLIndexDescription)
    {
        string IndexFields = BoldSQLIndexDescription.IndexedFields;
        if (CurrentTable.IndexDefs.GetIndexForFields(IndexFields, false) is null)
        {
            BoldLogHandler.BoldLog().LogFmt(BoldPMConsts.sIndexMissing, [CurrentTable.Tablename, IndexFields]);
            remedy.Add(BoldSQLIndexDescription.IndexOptions.Contains(TIndexOption.ixPrimary)
                ? SysUtils.Format("alter table %s add %s", CurrentTable.Tablename, BoldSQLIndexDescription.SQLForPrimaryKey())
                : BoldSQLIndexDescription.SQLForSecondaryKey());
        }
    }

    [Delphi(PortStatus.Ported, Note = "Reads the table's index definitions first; Delphi's table had them on open.")]
    protected void ValidateIndicesForTable(TBoldSQLTableDescription BoldSQLTableDescription)
    {
        CurrentTable.IndexDefs.Update();
        for (int i = 0; i < BoldSQLTableDescription.IndexList.Count; i++)
        {
            ValidateIndex(BoldSQLTableDescription.IndexList[i]);
        }
    }

    /// <summary>Nulls in a mandatory column get an UPDATE remedy to fill in.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ValidateNotNullForColumn(TBoldSQLColumnDescription BoldSQLColumnDescription)
    {
        IBoldQuery? query = SystemSQLMapper.GetQuery();
        string TableName = BoldSQLColumnDescription.TableDescription.SQLName;
        string ColumnName = BoldSQLColumnDescription.SQLName;
        try
        {
            query.AssignSQLText(SysUtils.Format("SELECT count(*) FROM %s WHERE %s IS NULL", TableName, ColumnName));
            query.Open();
            int NullCount = query.Fields(0).AsInteger;
            query.Close();
            if (NullCount != 0)
            {
                BoldLogHandler.BoldLog().LogFmtIndent(BoldPMConsts.sNullValuesFound, [NullCount, TableName, ColumnName]);
                remedy.Add(SysUtils.Format("UPDATE %s SET %s = <initial value> WHERE %1:s IS NULL;", TableName, ColumnName));
            }
        }
        finally
        {
            SystemSQLMapper.ReleaseQuery(ref query);
        }
    }

    /// <summary>Checks the columns and indexes of an existing table; a missing table gets a CREATE TABLE remedy.</summary>
    [Delphi(PortStatus.Ported)]
    protected void ValidateTable(TBoldSQLTableDescription BoldSQLTableDescription)
    {
        CurrentTable.Tablename = BoldSQLTableDescription.SQLName;
        if (CurrentTable.Exists)
        {
            CurrentTable.Open();
            ValidateColumnsForTable(BoldSQLTableDescription);
            ValidateIndicesForTable(BoldSQLTableDescription);
            CurrentTable.Close();
        }
        else
        {
            BoldLogHandler.BoldLog().LogFmt(BoldPMConsts.sTableDoesNotExist, [BoldSQLTableDescription.SQLName]);
            remedy.Add(BoldSQLTableDescription.SQLForCreateTable(DataBase));
        }
    }

    [Delphi(PortStatus.Ported, Symbol = "TBoldDbStructureValidator.Destroy")]
    protected override void Dispose(bool disposing)
    {
        if (disposing && fCurrentTable is not null)
        {
            DataBase.ReleaseTable(ref fCurrentTable);
        }

        base.Dispose(disposing);
    }

    [Delphi(PortStatus.Ported)]
    private IBoldTable GetCurrentTable() => fCurrentTable ??= DataBase.GetTable();

    [Delphi(PortStatus.Ported)]
    protected override void DeActivate()
    {
        if (fCurrentTable is not null)
        {
            DataBase.ReleaseTable(ref fCurrentTable);
        }

        base.DeActivate();
    }

    [Delphi(PortStatus.Ported)] protected IBoldTable CurrentTable => GetCurrentTable();
}
