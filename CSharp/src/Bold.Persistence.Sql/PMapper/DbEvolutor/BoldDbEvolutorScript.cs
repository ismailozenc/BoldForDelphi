// Delphi: Source/PMapper/DbEvolutor/BoldDbEvolutorScript.pas
// Port map: CSharp/port/units/BoldDbEvolutorScript.md
// The operations that evolve a database to a new model: add tables and columns, copy and move data, delete instances,
// then drop indexes, columns and tables. The script runs on a database or writes SQL text.

[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDbEvolutorScript", Symbol = "TBoldDataBaseEvolutorScript.Destroy", Note = "Freed the operation lists; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDbEvolutorScript", Symbol = "TBoldCopyInstances.Destroy", Note = "Freed the chained moves; the garbage collector does that in .NET.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDbEvolutorScript", Symbol = "TBoldMoveData.Destroy", Note = "Freed the chained moves; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>An operation of a script.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldScriptOperation
{
    private readonly TBoldDataBaseEvolutorScript fScript;

    [Delphi(PortStatus.Ported)]
    public TBoldScriptOperation(TBoldDataBaseEvolutorScript Script)
    {
        fScript = Script;
    }

    [Delphi(PortStatus.Ported)] public TBoldDataBaseEvolutorScript Script => fScript;
}

/// <summary>An operation on a table.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTableOperation : TBoldScriptOperation
{
    private readonly string fTableName;

    [Delphi(PortStatus.Ported)]
    public TBoldTableOperation(TBoldDataBaseEvolutorScript Script, string TableName) : base(Script)
    {
        fTableName = TableName;
    }

    [Delphi(PortStatus.Ported)] public string TableName => fTableName;
}

/// <summary>Creates a table and its secondary indexes.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAddTable : TBoldScriptOperation
{
    private readonly TBoldSQLTableDescription fTableDescr;

    [Delphi(PortStatus.Ported)]
    public TBoldAddTable(TBoldDataBaseEvolutorScript Script, TBoldSQLTableDescription TableDescr) : base(Script)
    {
        fTableDescr = TableDescr;
    }

    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        Script.Comment(BoldPMConsts.sAddTable, TableDescr.SQLName);
        Script.ExecuteSQL(TableDescr.SQLForCreateTable(Script.fDataBase));
        for (int i = 0; i < TableDescr.IndexList.Count; i++)
        {
            TBoldSQLIndexDescription index = TableDescr.IndexList[i];
            if (!index.IndexOptions.Contains(TIndexOption.ixPrimary))
            {
                Script.ExecuteSQL(index.SQLForSecondaryKey());
            }
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLTableDescription TableDescr => fTableDescr;
}

/// <summary>Drops a table.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDropTable(TBoldDataBaseEvolutorScript Script, string TableName) : TBoldTableOperation(Script, TableName)
{
    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        Script.Comment(BoldPMConsts.sDropTable, TableName);
        Script.ExecuteSQL(Script.fSQLDataBaseConfig!.GetDropTableQuery(TableName));
    }
}

/// <summary>Deletes the rows of one class from a table.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDeleteInstances : TBoldTableOperation
{
    private readonly string fExpressionName;
    private readonly TBoldDbType fDbType;

    [Delphi(PortStatus.Ported)]
    public TBoldDeleteInstances(TBoldDataBaseEvolutorScript Script, string ExpressionName, string TableName, TBoldDbType DbType) : base(Script, TableName)
    {
        fExpressionName = ExpressionName;
        fDbType = DbType;
    }

    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        Script.Comment(BoldPMConsts.sDeleteInstancesOfClassFromTable, ExpressionName, TableName);
        Script.ExecuteSQL("DELETE FROM %s WHERE BOLD_TYPE = %d", TableName, DbType);
    }

    [Delphi(PortStatus.Ported)] public string ExpressionName => fExpressionName;
    [Delphi(PortStatus.Ported)] public TBoldDbType DbType => fDbType;
}

/// <summary>An operation on a column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldColumnOperation : TBoldTableOperation
{
    private readonly string fColumnName;

    [Delphi(PortStatus.Ported)]
    public TBoldColumnOperation(TBoldDataBaseEvolutorScript Script, string TableName, string ColumnName) : base(Script, TableName)
    {
        fColumnName = ColumnName;
    }

    [Delphi(PortStatus.Ported)] public string ColumnName => fColumnName;
}

/// <summary>Adds a column, and an index on it alone if the table has one.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldAddColumn : TBoldScriptOperation
{
    private readonly TBoldSQLColumnDescription fColumnDesc;

    [Delphi(PortStatus.Ported)]
    public TBoldAddColumn(TBoldDataBaseEvolutorScript Script, TBoldSQLColumnDescription ColumnDesc) : base(Script)
    {
        fColumnDesc = ColumnDesc;
    }

    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        Script.Comment(BoldPMConsts.sAddColumn, ColumnDesc.TableDescription.SQLName, ColumnDesc.SQLName);
        Script.ExecuteSQL("ALTER TABLE %s ADD %s", ColumnDesc.TableDescription.SQLName, ColumnDesc.GetSQLForColumn(Script.fDataBase));
        for (int i = 0; i < ColumnDesc.TableDescription.IndexList.Count; i++)
        {
            TBoldSQLIndexDescription index = ColumnDesc.TableDescription.IndexList[i];
            if (SysUtils.SameText(index.IndexedFields, ColumnDesc.SQLName))
            {
                Script.ExecuteSQL(index.SQLForSecondaryKey());
            }
        }
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLColumnDescription ColumnDesc => fColumnDesc;
}

/// <summary>Drops a column.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDropColumn(TBoldDataBaseEvolutorScript Script, string TableName, string ColumnName) : TBoldColumnOperation(Script, TableName, ColumnName)
{
    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        Script.Comment(BoldPMConsts.sDropColumn, TableName, ColumnName);
        Script.ExecuteSQL(Script.fSQLDataBaseConfig!.GetDropColumnQuery(TableName, ColumnName));
    }
}

/// <summary>Drops an index.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDropIndex : TBoldScriptOperation
{
    private readonly string fIndexName;
    private readonly string fTableName;

    [Delphi(PortStatus.Ported)]
    public TBoldDropIndex(TBoldDataBaseEvolutorScript Script, string IndexName, string TableName) : base(Script)
    {
        fIndexName = IndexName;
        fTableName = TableName;
    }

    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        Script.Comment(BoldPMConsts.sDropIndex, IndexName);
        Script.ExecuteSQL(Script.fSQLDataBaseConfig!.GetDropIndexQuery(TableName, IndexName));
    }

    [Delphi(PortStatus.Ported)] public string IndexName => fIndexName;
    [Delphi(PortStatus.Ported)] public string TableName => fTableName;
}

/// <summary>An operation from one table to another.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTwoTableOperation : TBoldScriptOperation
{
    private readonly string fSourceTable;
    private readonly string fTargetTable;

    [Delphi(PortStatus.Ported)]
    public TBoldTwoTableOperation(TBoldDataBaseEvolutorScript Script, string SourceTable, string TargetTable) : base(Script)
    {
        fSourceTable = SourceTable;
        fTargetTable = TargetTable;
    }

    [Delphi(PortStatus.Ported)] public string SourceTable => fSourceTable;
    [Delphi(PortStatus.Ported)] public string TargetTable => fTargetTable;
}

/// <summary>Inserts the rows of a class into a new table, with the data moves into that table chained on.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldCopyInstances : TBoldTwoTableOperation
{
    private readonly string fExpressionName;
    private readonly TBoldDbType fSourceDbType;
    private TBoldMoveData? fMoveData;
    private readonly string fIdColumns;

    [Delphi(PortStatus.Ported)]
    public TBoldCopyInstances(TBoldDataBaseEvolutorScript Script, string ExpressionName, string SourceTable, string TargetTable, string IdColumns, TBoldDbType SourceDbType)
        : base(Script, SourceTable, TargetTable)
    {
        fExpressionName = ExpressionName;
        fSourceDbType = SourceDbType;
        fIdColumns = IdColumns;
    }

    /// <summary>INSERT INTO target (BOLD_TYPE, ids, moved columns) SELECT from the source tables joined on the ids.</summary>
    [Delphi(PortStatus.Ported)]
    public void Execute()
    {
        Script.Comment(BoldPMConsts.sAddInstanceOfClassToTable, ExpressionName, TargetTable);
        var SourceColumns = new TStringList();
        var TargetColumns = new TStringList();
        var IdColumnList = new TStringList();
        var SourceTables = new TStringList();
        var WhereConds = new TStringList();
        SourceTables.Add(SourceTable);
        IdColumnList.CommaText = IdColumns;
        for (int i = 0; i < IdColumnList.Count; i++)
        {
            SourceColumns.Add(SourceTable + "." + IdColumnList[i]);
            TargetColumns.Add(IdColumnList[i]);
        }

        TBoldMoveData? LocalMoveData = MoveData;
        while (LocalMoveData is not null)
        {
            Script.Comment(BoldPMConsts.sMoveDataFromXtoY, LocalMoveData.SourceTable, LocalMoveData.SourceColumn, LocalMoveData.TargetTable, LocalMoveData.TargetColumn, LocalMoveData.DbTypes);
            if (SourceTables.IndexOf(LocalMoveData.SourceTable) == -1)
            {
                SourceTables.Add(LocalMoveData.SourceTable);
            }

            string SelectColumn = LocalMoveData.SourceTable + "." + LocalMoveData.SourceColumn;
            // Renamed column needs alias
            if (!SysUtils.SameText(LocalMoveData.SourceColumn, LocalMoveData.TargetColumn))
            {
                SelectColumn = SelectColumn + " AS " + LocalMoveData.TargetColumn;
            }

            SourceColumns.Add(SelectColumn);
            TargetColumns.Add(LocalMoveData.TargetColumn);
            LocalMoveData = LocalMoveData.AlsoMoveData;
        }

        const string sql = "INSERT INTO %s (BOLD_TYPE, %s) SELECT %s.BOLD_TYPE, %s FROM %s%s";
        for (int i = 1; i < SourceTables.Count; i++)
        {
            for (int j = 0; j < IdColumnList.Count; j++)
            {
                WhereConds.Add(SysUtils.Format("%s.%s = %s.%s", SourceTables[0], IdColumnList[j], SourceTables[i], IdColumnList[j]));
            }
        }

        if (SourceDbType != BoldDefs.NO_CLASS)
        {
            WhereConds.Add(SysUtils.Format("BOLD_TYPE = %d", SourceDbType));
        }

        Script.ExecuteSQL(sql,
            TargetTable,
            BoldUtils.BoldSeparateStringList(TargetColumns, ", ", "", ""),
            SourceTable,
            BoldUtils.BoldSeparateStringList(SourceColumns, ", ", "", ""),
            BoldUtils.BoldSeparateStringList(SourceTables, ", ", "", ""),
            BoldUtils.BoldSeparateStringList(WhereConds, " AND ", " WHERE ", ""));
    }

    [Delphi(PortStatus.Ported)] public string ExpressionName => fExpressionName;
    [Delphi(PortStatus.Ported)] public TBoldDbType SourceDbType => fSourceDbType;
    [Delphi(PortStatus.Ported)] public TBoldMoveData? MoveData { get => fMoveData; set => fMoveData = value; }
    [Delphi(PortStatus.Ported)] public string IdColumns => fIdColumns;
}

/// <summary>An operation from a column of one table to a column of another.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldTwoColumnOperation : TBoldTwoTableOperation
{
    private readonly string fSourceColumn;
    private readonly string fTargetColumn;

    [Delphi(PortStatus.Ported)]
    public TBoldTwoColumnOperation(TBoldDataBaseEvolutorScript Script, string SourceTable, string TargetTable, string SourceColumn, string TargetColumn)
        : base(Script, SourceTable, TargetTable)
    {
        fSourceColumn = SourceColumn;
        fTargetColumn = TargetColumn;
    }

    [Delphi(PortStatus.Ported)] public string SourceColumn => fSourceColumn;
    [Delphi(PortStatus.Ported)] public string TargetColumn => fTargetColumn;
}

/// <summary>Moves a column's data for some db types; moves between the same tables chain together.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldMoveData : TBoldTwoColumnOperation
{
    private readonly string fIdColumns;
    internal string fdbTypes;
    private TBoldMoveData? fAlsoMoveData;

    [Delphi(PortStatus.Ported)]
    public TBoldMoveData(TBoldDataBaseEvolutorScript Script, string SourceTable, string TargetTable, string SourceColumn, string TargetColumn, string IdColumns, TBoldDbType dbType)
        : base(Script, SourceTable, TargetTable, SourceColumn, TargetColumn)
    {
        fIdColumns = IdColumns;
        fdbTypes = dbType.ToString();
    }

    /// <summary>Updates the target columns of the rows of the db types, from the same row or the source table's row with the same ids.</summary>
    [Delphi(PortStatus.Ported, Note = "Between tables, sets the target column from the source column; Delphi had them swapped, which only works when the names are the same.")]
    public void Execute()
    {
        var SourceColumns = new TStringList();
        var TargetColumns = new TStringList();
        var CopyStatements = new TStringList();
        var SelectColumns = new TStringList();
        var IdColumnList = new TStringList();
        var IdColumnJoins = new TStringList();
        TBoldMoveData? LocalMoveData = this;
        while (LocalMoveData is not null)
        {
            Script.Comment(BoldPMConsts.sMoveDataFromXtoY, LocalMoveData.SourceTable, LocalMoveData.SourceColumn, LocalMoveData.TargetTable, LocalMoveData.TargetColumn, LocalMoveData.DbTypes);
            SourceColumns.Add(LocalMoveData.SourceColumn);
            TargetColumns.Add(LocalMoveData.TargetColumn);
            LocalMoveData = LocalMoveData.AlsoMoveData;
        }

        if (SysUtils.SameText(SourceTable, TargetTable))
        {
            for (int i = 0; i < SourceColumns.Count; i++)
            {
                CopyStatements.Add(SysUtils.Format("%s = %s", TargetColumns[i], SourceColumns[i]));
            }

            Script.ExecuteSQL("UPDATE %s SET %s WHERE BOLD_TYPE IN (%s)", TargetTable, BoldUtils.BoldSeparateStringList(CopyStatements, ", ", "", ""), DbTypes);
        }
        else
        {
            IdColumnList.CommaText = IdColumns;
            for (int i = 0; i < IdColumnList.Count; i++)
            {
                IdColumnJoins.Add(SysUtils.Format("Source.%s = Target.%s", IdColumnList[i], IdColumnList[i]));
            }

            for (int i = 0; i < SourceColumns.Count; i++)
            {
                SelectColumns.Add(SysUtils.Format("%s = (SELECT %s FROM %s Source WHERE %s)",
                    TargetColumns[i], SourceColumns[i], SourceTable, BoldUtils.BoldSeparateStringList(IdColumnJoins, " AND ", "", "")));
            }

            Script.ExecuteSQL("UPDATE %s Target SET %s WHERE BOLD_TYPE IN (%s)", TargetTable, BoldUtils.BoldSeparateStringList(SelectColumns, ", ", "", ""), DbTypes);
        }
    }

    /// <summary>The tables and the sorted column moves of the chain, to find moves that are the same for other db types.</summary>
    [Delphi(PortStatus.Ported)]
    private string GetSignature()
    {
        var ColumnList = new TStringList();
        TBoldMoveData? tempMoveData = this;
        while (tempMoveData is not null)
        {
            ColumnList.Add(tempMoveData.SourceColumn + "->" + tempMoveData.TargetColumn);
            tempMoveData = tempMoveData.AlsoMoveData;
        }

        ColumnList.Sort();
        return SourceTable + "->" + TargetTable + ":" + ColumnList.CommaText;
    }

    [Delphi(PortStatus.Ported)] public string IdColumns => fIdColumns;
    [Delphi(PortStatus.Ported)] public string DbTypes => fdbTypes;
    [Delphi(PortStatus.Ported)] public string Signature => GetSignature();
    [Delphi(PortStatus.Ported)] public TBoldMoveData? AlsoMoveData { get => fAlsoMoveData; set => fAlsoMoveData = value; }
}

/// <summary>The operations to evolve a database, in the order that keeps the data: extend, adjust contents, reduce.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDataBaseEvolutorScript
{
    private readonly List<TBoldAddTable> fAddedTables = [];
    private readonly List<TBoldAddColumn> fAddedColumns = [];
    private readonly List<TBoldCopyInstances> fCopiedInstances = [];
    private readonly List<TBoldDeleteInstances> fDeletedInstances = [];
    private readonly List<TBoldDropColumn> fDroppedColumns = [];
    private readonly List<TBoldDropTable> fDroppedTables = [];
    private readonly List<TBoldMoveData> fMovedData = [];
    private readonly List<TBoldDropIndex> fDroppedIndices = [];
    private TStrings? fScript;
    internal IBoldDataBase? fDataBase;
    internal TBoldSQLDataBaseConfig? fSQLDataBaseConfig;
    private readonly TStringList fSQLStatements = new();
    private readonly TStringList fInternalLog = new();

    [Delphi(PortStatus.Ported)]
    public TBoldDataBaseEvolutorScript()
    {
    }

    [Delphi(PortStatus.Ported)]
    private void AddCommandToScript(string s)
    {
        fScript!.Add(s + fSQLDataBaseConfig!.SqlScriptTerminator);
        if (fSQLDataBaseConfig.SqlScriptSeparator != "")
        {
            fScript.Add(fSQLDataBaseConfig.SqlScriptSeparator);
        }
    }

    /// <summary>Writes a comment to the script, and logs it.</summary>
    [Delphi(PortStatus.Ported)]
    internal void Comment(string msg, params object?[] Args)
    {
        fScript?.Add(fSQLDataBaseConfig!.SqlScriptCommentStart + SysUtils.Format(msg, Args) + fSQLDataBaseConfig.SqlScriptCommentStop);
        BoldLogHandler.BoldLog().LogFmt(msg, Args);
    }

    /// <summary>Adds the statement to the script, and runs it on the database.</summary>
    [Delphi(PortStatus.Ported, Note = "Without arguments the statement is used as it is; Delphi formatted it with none, which fails on a statement with a %.")]
    internal void ExecuteSQL(string SQL, params object?[] Args)
    {
        string Statement = Args.Length == 0 ? SQL : SysUtils.Format(SQL, Args);
        if (fScript is not null)
        {
            AddCommandToScript(Statement);
        }

        if (fDataBase is not null)
        {
            IBoldExecQuery? q = fDataBase.GetExecQuery();
            try
            {
                q.AssignSQLText(Statement);
                q.ExecSQL();
                fInternalLog.Add(Statement);
            }
            finally
            {
                fDataBase.ReleaseExecQuery(ref q);
            }
        }
    }

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")] protected TBoldAddTable AddedTables(int index) => GetAddedTables(index);
    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")] protected TBoldAddColumn AddedColumns(int index) => GetAddedColumns(index);
    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")] protected TBoldDropTable DroppedTables(int index) => GetDroppedTables(index);
    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")] protected TBoldDropColumn DroppedColumns(int index) => GetDroppedColumns(index);
    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")] protected TBoldDropIndex DroppedIndices(int index) => GetDroppedIndices(index);
    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")] protected TBoldCopyInstances CopiedInstances(int index) => GetCopiedInstances(index);
    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")] protected TBoldMoveData MovedData(int index) => GetMovedData(index);
    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi.")] protected TBoldDeleteInstances DeletedInstances(int index) => GetDeletedInstances(index);

    [Delphi(PortStatus.Ported)] private TBoldAddTable GetAddedTables(int index) => fAddedTables[index];
    [Delphi(PortStatus.Ported)] private TBoldAddColumn GetAddedColumns(int index) => fAddedColumns[index];
    [Delphi(PortStatus.Ported)] private TBoldDropColumn GetDroppedColumns(int index) => fDroppedColumns[index];
    [Delphi(PortStatus.Ported)] private TBoldDropTable GetDroppedTables(int index) => fDroppedTables[index];
    [Delphi(PortStatus.Ported)] private TBoldCopyInstances GetCopiedInstances(int index) => fCopiedInstances[index];
    [Delphi(PortStatus.Ported)] private TBoldMoveData GetMovedData(int index) => fMovedData[index];
    [Delphi(PortStatus.Ported)] private TBoldDeleteInstances GetDeletedInstances(int index) => fDeletedInstances[index];
    [Delphi(PortStatus.Ported)] private TBoldDropIndex GetDroppedIndices(int index) => fDroppedIndices[index];

    /// <summary>Adds a column once.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddColumn(TBoldSQLColumnDescription ColumnDesc)
    {
        if (fAddedColumns.Any(c => c.ColumnDesc == ColumnDesc))
        {
            return;
        }

        fAddedColumns.Add(new TBoldAddColumn(this, ColumnDesc));
    }

    [Delphi(PortStatus.Ported)]
    public void AddTable(TBoldSQLTableDescription TableDescr) => fAddedTables.Add(new TBoldAddTable(this, TableDescr));

    [Delphi(PortStatus.Ported)]
    public void CopyInstances(string ExpressionName, string SourceTable, string TargetTable, string IdColumns, TBoldDbType SourceDbType) =>
        fCopiedInstances.Add(new TBoldCopyInstances(this, ExpressionName, SourceTable, TargetTable, IdColumns, SourceDbType));

    [Delphi(PortStatus.Ported)]
    public void DeleteInstances(string ExpressionName, string TableName, TBoldDbType DbType) =>
        fDeletedInstances.Add(new TBoldDeleteInstances(this, ExpressionName, TableName, DbType));

    /// <summary>Drops a column once.</summary>
    [Delphi(PortStatus.Ported)]
    public void DropColumn(string TableName, string ColumnName)
    {
        if (fDroppedColumns.Any(c => SysUtils.SameText(c.TableName, TableName) && SysUtils.SameText(c.ColumnName, ColumnName)))
        {
            return;
        }

        fDroppedColumns.Add(new TBoldDropColumn(this, TableName, ColumnName));
    }

    [Delphi(PortStatus.Ported)]
    public void DropTable(string TableName) => fDroppedTables.Add(new TBoldDropTable(this, TableName));

    [Delphi(PortStatus.Ported)]
    public void DropIndex(string IndexName, string TableName) => fDroppedIndices.Add(new TBoldDropIndex(this, IndexName, TableName));

    /// <summary>Moves a column's data, unless it stays where it is.</summary>
    [Delphi(PortStatus.Ported)]
    public void MoveData(string SourceTable, string TargetTable, string SourceColumn, string TargetColumn, string IdColumns, TBoldDbType dbType)
    {
        if (!SysUtils.SameText(TargetTable + "." + TargetColumn, SourceTable + "." + SourceColumn))
        {
            fMovedData.Add(new TBoldMoveData(this, SourceTable, TargetTable, SourceColumn, TargetColumn, IdColumns, dbType));
        }
    }

    /// <summary>Runs the script on the database, opening it for the run if it was closed.</summary>
    [Delphi(PortStatus.Ported)]
    public void UpdateDatabase(IBoldDataBase DataBase, TBoldSQLDataBaseConfig SQLDataBaseConfig)
    {
        fDataBase = DataBase;
        fSQLDataBaseConfig = SQLDataBaseConfig;
        bool WasOpen = DataBase.Connected;
        try
        {
            if (!DataBase.Connected)
            {
                DataBase.Open();
            }

            Execute();
        }
        finally
        {
            if (!WasOpen)
            {
                DataBase.Close();
            }

            fDataBase = null;
        }
    }

    /// <summary>Writes the script as SQL text.</summary>
    [Delphi(PortStatus.Ported)]
    public void GenerateScript(TStrings Script, TBoldSQLDataBaseConfig SQLDataBaseConfig)
    {
        fScript = Script;
        fSQLDataBaseConfig = SQLDataBaseConfig;
        try
        {
            Execute();
        }
        finally
        {
            fScript = null;
        }
    }

    [Delphi(PortStatus.Ported)]
    protected void Execute()
    {
        fInternalLog.Clear();
        ExtendSchema();
        AdjustContents();
        ExecuteSQLStatements();
        ReduceSchema();
    }

    /// <summary>
    /// Folds the data moves into the instance copies to the same table and db type, chains the moves between the same
    /// tables and db type, and merges chains that are the same for several db types.
    /// </summary>
    [Delphi(PortStatus.Ported)]
    public void OptimizeScript()
    {
        // this will find all Movedata-objects with the same targettable and dbtype,
        // and move them to the CopyInstances.
        // note that at this point, the movedatas are not chaines together yet,
        // and MoveData[j].AlsoMoveData is always nil
        for (int i = 0; i < fCopiedInstances.Count; i++)
        {
            for (int j = fMovedData.Count - 1; j >= 0; j--)
            {
                if (MovedData(j).DbTypes == CopiedInstances(i).SourceDbType.ToString() && SysUtils.SameText(MovedData(j).TargetTable, CopiedInstances(i).TargetTable))
                {
                    MovedData(j).AlsoMoveData = CopiedInstances(i).MoveData;
                    CopiedInstances(i).MoveData = MovedData(j);
                    fMovedData.RemoveAt(j);
                }
            }
        }

        // this will chain up the rest of the movedatas that have the same
        // source and target table and dbtype
        for (int i = 0; i < fMovedData.Count; i++)
        {
            for (int j = fMovedData.Count - 1; j > i; j--)
            {
                if (MovedData(j).DbTypes == MovedData(i).DbTypes && SysUtils.SameText(MovedData(j).SourceTable, MovedData(i).SourceTable) &&
                    SysUtils.SameText(MovedData(j).TargetTable, MovedData(i).TargetTable))
                {
                    MovedData(j).AlsoMoveData = MovedData(i).AlsoMoveData;
                    MovedData(i).AlsoMoveData = MovedData(j);
                    fMovedData.RemoveAt(j);
                }
            }
        }

        var MoveDataSignatures = new TStringList();
        for (int i = 0; i < fMovedData.Count; i++)
        {
            MoveDataSignatures.AddObject(MovedData(i).Signature, MovedData(i));
        }

        MoveDataSignatures.Sort();
        for (int i = 1; i < MoveDataSignatures.Count; i++)
        {
            if (MoveDataSignatures[i - 1] == MoveDataSignatures[i])
            {
                var MoveData1 = (TBoldMoveData)MoveDataSignatures.Objects(i - 1)!;
                var MoveData2 = (TBoldMoveData)MoveDataSignatures.Objects(i)!;
                MoveData2.fdbTypes = MoveData2.DbTypes + ", " + MoveData1.DbTypes;
                fMovedData.Remove(MoveData1);
            }
        }
    }

    /// <summary>A statement to run after the contents are adjusted, before the schema is reduced.</summary>
    [Delphi(PortStatus.Ported)]
    public void AddSQLStatement(string sql) => fSQLStatements.Add(sql);

    /// <summary>Copies, moves and deletes data in one transaction.</summary>
    [Delphi(PortStatus.Ported)]
    private void AdjustContents()
    {
        if (fCopiedInstances.Count + fMovedData.Count + fDeletedInstances.Count == 0)
        {
            return;
        }

        // wrap all non MetaData changes in a transaction.
        // Many databases do not allow them in a transaction,
        // and Interbase does not make metadata changes visible inside the transaction
        StartTransaction();
        try
        {
            foreach (TBoldCopyInstances c in fCopiedInstances)
            {
                c.Execute();
            }

            foreach (TBoldMoveData m in fMovedData)
            {
                m.Execute();
            }

            foreach (TBoldDeleteInstances d in fDeletedInstances)
            {
                d.Execute();
            }

            CommitTransaction();
        }
        catch
        {
            RollBackTransaction();
            throw;
        }
    }

    /// <summary>Adds the tables and columns, in a transaction if the database allows metadata changes in one.</summary>
    [Delphi(PortStatus.Ported)]
    private void ExtendSchema()
    {
        if (fAddedTables.Count + fAddedColumns.Count == 0)
        {
            return;
        }

        bool InTransaction = fSQLDataBaseConfig!.AllowMetadataChangesInTransaction;
        if (InTransaction)
        {
            StartTransaction();
        }

        try
        {
            foreach (TBoldAddTable t in fAddedTables)
            {
                t.Execute();
            }

            foreach (TBoldAddColumn c in fAddedColumns)
            {
                c.Execute();
            }

            if (InTransaction)
            {
                CommitTransaction();
            }
        }
        catch
        {
            if (InTransaction)
            {
                RollBackTransaction();
            }

            throw;
        }
    }

    /// <summary>Drops the indexes, columns and tables, in a transaction if the database allows metadata changes in one.</summary>
    [Delphi(PortStatus.Ported)]
    private void ReduceSchema()
    {
        if (fDroppedIndices.Count + fDroppedColumns.Count + fDroppedTables.Count == 0)
        {
            return;
        }

        bool InTransaction = fSQLDataBaseConfig!.AllowMetadataChangesInTransaction;
        if (InTransaction)
        {
            StartTransaction();
        }

        try
        {
            foreach (TBoldDropIndex i in fDroppedIndices)
            {
                i.Execute();
            }

            foreach (TBoldDropColumn c in fDroppedColumns)
            {
                c.Execute();
            }

            foreach (TBoldDropTable t in fDroppedTables)
            {
                t.Execute();
            }

            if (InTransaction)
            {
                CommitTransaction();
            }
        }
        catch
        {
            if (InTransaction)
            {
                RollBackTransaction();
            }

            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void ExecuteSQLStatements()
    {
        if (fSQLStatements.Count == 0)
        {
            return;
        }

        StartTransaction();
        try
        {
            for (int i = 0; i < fSQLStatements.Count; i++)
            {
                ExecuteSQL(fSQLStatements[i]);
            }

            CommitTransaction();
        }
        catch
        {
            RollBackTransaction();
            throw;
        }
    }

    [Delphi(PortStatus.Ported)]
    private void StartTransaction()
    {
        fDataBase?.StartTransaction();
        if (fScript is not null)
        {
            AddCommandToScript(fSQLDataBaseConfig!.SqlScriptStartTransaction);
        }
    }

    [Delphi(PortStatus.Ported)]
    private void CommitTransaction()
    {
        if (fDataBase is not null && fDataBase.InTransaction)
        {
            BoldLogHandler.BoldLog().Log(BoldPMConsts.sCommittingToDB);
            fDataBase.Commit();
        }

        if (fScript is not null)
        {
            AddCommandToScript(fSQLDataBaseConfig!.SqlScriptCommitTransaction);
        }
    }

    [Delphi(PortStatus.Ported, Note = "Writes the rollback only to a script; Delphi wrote it with no script too, which fails.")]
    private void RollBackTransaction()
    {
        if (fDataBase is not null && fDataBase.InTransaction)
        {
            BoldLogHandler.BoldLog().Log(BoldPMConsts.sRollingBackDB);
            fDataBase.RollBack();
        }

        if (fScript is not null)
        {
            AddCommandToScript(fSQLDataBaseConfig!.SqlScriptRollBackTransaction);
        }
    }

    [Delphi(PortStatus.Ported)] public TStringList InternalLog => fInternalLog;
}
