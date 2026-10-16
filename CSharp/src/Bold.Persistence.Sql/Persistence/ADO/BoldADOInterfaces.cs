// Delphi: Source/Persistence/ADO/BoldADOInterfaces.pas
// Port map: CSharp/port/units/BoldADOInterfaces.md
// The database interfaces over ADO: queries, tables, parameters and the connection, with one query and one table
// kept for reuse.

[assembly: Delphi(PortStatus.Ported, Unit = "BoldADOInterfaces", Symbol = "TBoldADOQueryClass", Note = "A System.Type of the query class (a TBoldADOQuery with a (TADOQuery, TBoldDatabaseWrapper) constructor).")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldADOInterfaces", Symbol = "TBoldADOConnection.Destroy", Note = "Freed the cached query and table; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>A parameter of an ADO query; empty strings are written as the configuration's empty-string marker.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldADOParameter : TBoldParameterWrapper, IBoldParameter
{
    private readonly TParam fParameter;

    [Delphi(PortStatus.Ported)]
    public TBoldADOParameter(TParam AdoParameter, TBoldDataSetWrapper DatasetWrapper)
        : base(DatasetWrapper)
    {
        fParameter = AdoParameter;
    }

    private string EmptyStringMarker => DatasetWrapper.DatabaseWrapper.SQLDatabaseConfig.EmptyStringMarker;

    [Delphi(PortStatus.Ported)] public object? GetAsVariant() => Parameter.Value;
    [Delphi(PortStatus.Ported)] public void SetAsVariant(object? NewValue) => Parameter.Value = NewValue;
    [Delphi(PortStatus.Ported)] public string GetName() => Parameter.Name;

    /// <summary>Does nothing, as in Delphi (marked FIXME there).</summary>
    [Delphi(PortStatus.Ported)]
    public void Clear()
    {
    }

    [Delphi(PortStatus.Ported)] public TFieldType GetDataType() => Parameter.DataType;
    [Delphi(PortStatus.Ported)] public void SetDataType(TFieldType Value) => Parameter.DataType = Value;
    [Delphi(PortStatus.Ported)] public decimal GetAsBCD() => Parameter.AsCurrency;
    [Delphi(PortStatus.Ported)] public bool GetAsBoolean() => Parameter.AsBoolean;
    [Delphi(PortStatus.Ported)] public TDateTime GetAsDateTime() => Parameter.AsDateTime;
    [Delphi(PortStatus.Ported)] public decimal GetAsCurrency() => Parameter.AsCurrency;
    [Delphi(PortStatus.Ported)] public double GetAsFloat() => Parameter.AsFloat;
    [Delphi(PortStatus.Ported)] public int GetAsInteger() => Parameter.AsInteger;
    [Delphi(PortStatus.Ported)] public string GetAsMemo() => Parameter.AsString;

    [Delphi(PortStatus.Ported)]
    public string GetAsString()
    {
        string result = Parameter.AsString;
        return result == EmptyStringMarker ? "" : result;
    }

    [Delphi(PortStatus.Ported)] public bool GetIsNull() => Parameter.IsNull;
    [Delphi(PortStatus.Ported)] public void SetAsBCD(decimal Value) => Parameter.Value = Value;

    /// <summary>A blob (a string of byte characters); the type becomes ftBlob when unknown, and empty is the empty-string marker.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetAsBlob(string Value)
    {
        if (Parameter.DataType == TFieldType.ftUnknown)
        {
            Parameter.DataType = TFieldType.ftBlob;
        }

        Parameter.Value = Value.Length == 0 ? EmptyStringMarker : Value;
    }

    [Delphi(PortStatus.Ported)] public void SetAsBoolean(bool Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsCurrency(decimal Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsDate(TDateTime Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsDateTime(TDateTime Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsFloat(double Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsInteger(int Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsMemo(string Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsSmallInt(int Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsString(string Value) => Parameter.Value = Value.Length == 0 ? EmptyStringMarker : Value;
    [Delphi(PortStatus.Ported)] public void SetAsTime(TDateTime Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetAsWord(int Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] public void SetText(string Value) => Parameter.Value = Value;
    [Delphi(PortStatus.Ported)] private TParam GetParameter() => fParameter;
    [Delphi(PortStatus.Ported)] public void AssignFieldValue(IBoldField source) => Parameter.AssignFieldValue(source.Field, source.Field.Value);
    [Delphi(PortStatus.Ported)] private TParam Parameter => GetParameter();
}

/// <summary>A query over a TADOQuery; also an exec query and parameterized. Batching does nothing.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldADOQuery : TBoldDataSetWrapper, IBoldQuery, IBoldExecQuery, IBoldParameterized
{
    internal TADOQuery fQuery;

    [Delphi(PortStatus.Ported)]
    public TBoldADOQuery(TADOQuery Query, TBoldDatabaseWrapper DatabaseWrapper)
        : base(DatabaseWrapper)
    {
        fQuery = Query;
    }

    [Delphi(PortStatus.Ported)] private TADOQuery GetQuery() => fQuery;
    [Delphi(PortStatus.Ported)] protected TADOQuery Query => GetQuery();

    /// <summary>Replaces the parameters with input parameters of the given names, types and values.</summary>
    [Delphi(PortStatus.Ported)]
    public void AssignParams(TParams? Sourceparams)
    {
        Query.Parameters.Clear();
        if (Sourceparams is null)
        {
            return;
        }

        foreach (TParam source in Sourceparams)
        {
            Query.Parameters.CreateParam(source.DataType, source.Name, TParamType.ptInput).Value = source.Value;
        }
    }

    [Delphi(PortStatus.Ported)] public int GetParamCount() => Query.Parameters.Count;
    [Delphi(PortStatus.Ported)] public IBoldParameter GetParams(int i) => new TBoldADOParameter(Query.Parameters[i], this);
    [Delphi(PortStatus.Ported)] public bool GetRequestLiveQuery() => false;

    /// <summary>The parameter of the name; null when there is none, as Delphi's ADO.</summary>
    [Delphi(PortStatus.Ported)]
    public IBoldParameter ParamByName(string Value) => Query.Parameters.FindParam(Value) is { } param ? new TBoldADOParameter(param, this) : null!;

    [Delphi(PortStatus.Ported)]
    public override IBoldParameter Createparam(TFieldType FldType, string ParamName, TParamType ParamType, int Size) =>
        new TBoldADOParameter(Query.Parameters.CreateParam(FldType, ParamName, ParamType), this);

    /// <summary>Ignored: ADO queries are never live.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetRequestLiveQuery(bool NewValue)
    {
    }

    [Delphi(PortStatus.Ported)] public string GetSQLText() => Query.SQL.Text;

    /// <summary>Sets the SQL and parses its :Name parameters, keeping the values of parameters already set.</summary>
    [Delphi(PortStatus.Ported, Note = "No connection juggling is needed: parsing the parameters does not bind them in .NET.")]
    public virtual void AssignSQL(TStrings SQL)
    {
        Query.Close();
        Query.SQL.Assign(SQL);
        Query.ParseSQL(SQL.Text);
    }

    [Delphi(PortStatus.Ported)]
    public void AssignSQLText(string SQL)
    {
        var StringList = new TStringList();
        BoldUtils.BoldAppendToStrings(StringList, SQL, true);
        AssignSQL(StringList);
    }

    [Delphi(PortStatus.Ported)] public int GetRowsAffected() => Query.RowsAffected;
    [Delphi(PortStatus.Ported)] public int GetRecordCount() => Query.RecordCount;

    /// <summary>Does nothing (ADO has no batching).</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void StartSQLBatch()
    {
    }

    /// <summary>Does nothing (ADO has no batching).</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void EndSQLBatch()
    {
    }

    /// <summary>Does nothing (ADO has no batching).</summary>
    [Delphi(PortStatus.Ported)]
    public virtual void FailSQLBatch()
    {
    }

    [Delphi(PortStatus.Ported)] public override TDataSet GetDataSet() => Query;
    [Delphi(PortStatus.Ported)] public void ClearParams() => Query.Parameters.Clear();

    /// <summary>Logs and executes the SQL; a failure's message gets the SQL appended.</summary>
    [Delphi(PortStatus.Ported, Note = "Raises EBold with the SQL in the message and the provider's exception inside, where Delphi appends to the message.")]
    public virtual void ExecSQL()
    {
        BoldDBInterfaces.BoldLogSQL(Query.SQL);
        try
        {
            Query.ExecSQL();
        }
        catch (Exception e) when (e is not EBold)
        {
            throw new EBold(e.Message + BoldDefs.BOLDCRLF + "SQL: " + Query.SQL.Text, e);
        }
    }

    /// <summary>Logs and opens the SQL; a failure's message gets the SQL appended.</summary>
    [Delphi(PortStatus.Ported, Note = "Raises EBold with the SQL in the message; the cursor settings of ADO have no ADO.NET counterpart.")]
    public override void Open()
    {
        BoldDBInterfaces.BoldLogSQL(Query.SQL);
        try
        {
            base.Open();
        }
        catch (Exception e) when (e is not EBold)
        {
            throw new EBold(e.Message + BoldDefs.BOLDCRLF + "SQL: " + Query.SQL.Text, e);
        }
    }

    TDataSet IBoldDataSet.GetDataSet() => GetDataSet();
    IBoldParameter IBoldParameterized.Createparam(TFieldType FldType, string ParamName, TParamType ParamType, int Size) => Createparam(FldType, ParamName, ParamType, Size);
}

/// <summary>A table over a TADOTable; creating and deleting tables and indexes is not supported.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldADOTable : TBoldDataSetWrapper, IBoldTable
{
    internal TADOTable fTable;

    [Delphi(PortStatus.Ported)]
    public TBoldADOTable(TADOTable Table, TBoldDatabaseWrapper DatabaseWrapper)
        : base(DatabaseWrapper)
    {
        fTable = Table;
    }

    [Delphi(PortStatus.Ported)] private TADOTable GetTable() => fTable;
    [Delphi(PortStatus.Ported)] private TADOTable Table => GetTable();

    [Delphi(PortStatus.Ported)]
    public void AddIndex(string Name, string Fields, DelphiSet<TIndexOption> Options, string DescFields = "") =>
        throw new EBold(SysUtils.Format(BoldCoreConsts.sMethodNotImplemented, GetType().Name, "AddIndex"));

    [Delphi(PortStatus.Ported)]
    public void CreateTable() => throw new EBold(SysUtils.Format(BoldCoreConsts.sMethodNotImplemented, GetType().Name, "CreateTable"));

    [Delphi(PortStatus.Ported)]
    public void DeleteTable() => throw new EBold(SysUtils.Format(BoldCoreConsts.sMethodNotImplemented, GetType().Name, "DeleteTable"));

    [Delphi(PortStatus.Ported)] public TIndexDefs GetIndexDefs() => Table.IndexDefs;
    [Delphi(PortStatus.Ported)] public void SetTableName(string NewName) => Table.TableName = NewName;
    [Delphi(PortStatus.Ported)] public string GetTableName() => Table.TableName;

    /// <summary>Ignored: ADO.NET has no table lock types.</summary>
    [Delphi(PortStatus.Ported, Note = "Delphi switches between pessimistic and optimistic lock types; ADO.NET has none.")]
    public void SetExclusive(bool NewValue)
    {
    }

    [Delphi(PortStatus.Ported)] public bool GetExclusive() => false;

    /// <summary>Whether the table is among the connection's tables.</summary>
    [Delphi(PortStatus.Ported)]
    public bool GetExists()
    {
        if (Table.Connection is null)
        {
            return false;
        }

        var AllTables = new TStringList();
        Table.Connection.GetTableNames(AllTables);
        return AllTables.IndexOf(GetTableName()) != -1;
    }

    [Delphi(PortStatus.Ported)] public override TDataSet GetDataSet() => fTable;
}

/// <summary>The database interface of an ADO connection; one query and one table are cached for reuse.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldADOConnection : TBoldDatabaseWrapper, IBoldDataBase
{
    private readonly TADOConnection fDataBase;
    private TADOTable? fCachedTable;
    private TADOQuery? fCachedQuery;

    [Delphi(PortStatus.Ported)]
    public TBoldADOConnection(TADOConnection DataBase, TBoldSQLDataBaseConfig SQLDataBaseConfig)
        : base(SQLDataBaseConfig)
    {
        fDataBase = DataBase;
    }

    [Delphi(PortStatus.Ported)] private TADOConnection GetDataBase() => fDataBase;
    [Delphi(PortStatus.Ported)] private TADOConnection DataBase => GetDataBase();
    [Delphi(PortStatus.Ported)] public bool GetConnected() => DataBase.Connected;
    [Delphi(PortStatus.Ported)] public bool GetInTransaction() => DataBase.InTransaction;
    [Delphi(PortStatus.Ported)] public bool GetIsSQLBased() => true;
    [Delphi(PortStatus.Ported)] public void SetlogInPrompt(bool NewValue) => DataBase.LoginPrompt = NewValue;
    [Delphi(PortStatus.Ported)] public bool GetLogInPrompt() => DataBase.LoginPrompt;

    /// <summary>Ignored, as in Delphi.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetKeepConnection(bool NewValue)
    {
    }

    [Delphi(PortStatus.Ported)] public bool GetKeepConnection() => true;
    [Delphi(PortStatus.Ported)] public void StartTransaction() => DataBase.BeginTrans();
    [Delphi(PortStatus.Ported)] public void Commit() => DataBase.CommitTrans();
    [Delphi(PortStatus.Ported)] public void RollBack() => DataBase.RollbackTrans();
    [Delphi(PortStatus.Ported)] public void Open() => DataBase.Open();
    [Delphi(PortStatus.Ported)] public void Close() => DataBase.Close();

    /// <summary>A table (the cached one when there is one).</summary>
    [Delphi(PortStatus.Ported)]
    public IBoldTable GetTable()
    {
        TADOTable table;
        if (fCachedTable is not null)
        {
            table = fCachedTable;
            fCachedTable = null;
        }
        else
        {
            table = new TADOTable { Connection = DataBase };
        }

        return new TBoldADOTable(table, this);
    }

    /// <summary>Keeps the table for reuse when none is cached.</summary>
    [Delphi(PortStatus.Ported)]
    public void ReleaseTable(ref IBoldTable? Table)
    {
        if (Table?.Implementor is TBoldADOTable ADOTable)
        {
            Table = null;
            fCachedTable ??= ADOTable.fTable;
        }
    }

    [Delphi(PortStatus.Ported)] public bool SupportsTableCreation() => false;

    [Delphi(PortStatus.Ported)]
    public void ReleaseCachedObjects()
    {
        fCachedTable = null;
        fCachedQuery = null;
    }

    /// <summary>The names of the tables that match the pattern (* and ?; empty is all).</summary>
    [Delphi(PortStatus.Ported)]
    public override void AllTableNames(string Pattern, bool ShowSystemTables, TStrings TableNameList)
    {
        var TempList = new TStringList();
        string TempPattern = Pattern.Length == 0 ? "*" : Pattern;
        GetDataBase().GetTableNames(TempList, ShowSystemTables);
        for (int i = 0; i < TempList.Count; i++)
        {
            if (TADOConnection.MatchesMask(TempList[i], TempPattern))
            {
                TableNameList.Add(TempList[i]);
            }
        }
    }

    /// <summary>A query of BoldADOQueryClass (on the cached query when there is one).</summary>
    [Delphi(PortStatus.Ported)]
    public override IBoldQuery GetQuery()
    {
        TADOQuery Query;
        if (fCachedQuery is not null)
        {
            Query = fCachedQuery;
            fCachedQuery = null;
        }
        else
        {
            Query = new TADOQuery { Connection = DataBase };
        }

        return (IBoldQuery)Activator.CreateInstance(BoldADOInterfaces.BoldADOQueryClass, Query, this)!;
    }

    /// <summary>Closes the query and keeps it for reuse when none is cached.</summary>
    [Delphi(PortStatus.Ported)]
    public override void ReleaseQuery(ref IBoldQuery? Query)
    {
        if (Query?.Implementor is TBoldADOQuery ADOQuery)
        {
            Query = null;
            if (fCachedQuery is null)
            {
                fCachedQuery = ADOQuery.fQuery;
                fCachedQuery.Close();
                fCachedQuery.SQL.Clear();
                fCachedQuery.Parameters.Clear();
            }
        }
    }
}

/// <summary>The query class the ADO connection makes.</summary>
public static partial class BoldADOInterfaces
{
    [Delphi(PortStatus.Ported, Note = "The System.Type of a TBoldADOQuery subclass.")]
    public static Type BoldADOQueryClass = typeof(TBoldADOQuery);
}
