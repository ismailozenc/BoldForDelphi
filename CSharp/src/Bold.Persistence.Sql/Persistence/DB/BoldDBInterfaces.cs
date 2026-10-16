// Delphi: Source/Persistence/DB/BoldDBInterfaces.pas
// Port map: CSharp/port/units/BoldDBInterfaces.md
// The database abstraction the mappers use: queries, exec queries, tables, fields and parameters as interfaces, and
// wrappers that implement them over a TDataSet (the RTL's in-memory emulation, filled from ADO.NET).

[assembly: Delphi(PortStatus.Ported, Unit = "BoldDBInterfaces", Symbol = "TBoldFieldWrapperClass", Note = "A System.Type of the field wrapper class.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDBInterfaces", Symbol = "TBoldDbParameter.Destroy", Note = "Did nothing beyond the inherited destructor.")]
[assembly: Delphi(PortStatus.Replaced, Unit = "BoldDBInterfaces", Symbol = "TBoldDatabaseWrapper.Destroy", Note = "Freed the configuration copy; the garbage collector does that in .NET.")]

namespace Bold;

/// <summary>Gets the database to use.</summary>
[Delphi(PortStatus.Ported)]
public delegate IBoldDataBase? TBoldGetDatabaseEvent();

/// <summary>A field of the current record.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldField
{
    [Delphi(PortStatus.Ported)] TField GetField();
    [Delphi(PortStatus.Ported)] string GetFieldName();
    [Delphi(PortStatus.Ported)] object? GetAsVariant();
    [Delphi(PortStatus.Ported)] void SetAsVariant(object? Value);
    [Delphi(PortStatus.Ported)] string GetAsString();
    [Delphi(PortStatus.Ported)] void SetAsString(string Value);
    [Delphi(PortStatus.Ported)] int GetAsInteger();
    [Delphi(PortStatus.Ported)] void SetAsInteger(int Value);
    [Delphi(PortStatus.Ported)] bool GetAsBoolean();
    [Delphi(PortStatus.Ported)] decimal GetAsCurrency();
    [Delphi(PortStatus.Ported)] TDateTime GetAsDateTime();
    [Delphi(PortStatus.Ported)] double GetAsFloat();
    [Delphi(PortStatus.Ported)] bool GetIsNull();
    [Delphi(PortStatus.Ported)] void SetAsBoolean(bool Value);
    [Delphi(PortStatus.Ported)] void SetAsCurrency(decimal Value);
    [Delphi(PortStatus.Ported)] void SetAsDateTime(TDateTime Value);
    [Delphi(PortStatus.Ported)] void SetAsFloat(double Value);
    [Delphi(PortStatus.Ported)] TDateTime GetAsDate();
    [Delphi(PortStatus.Ported)] TDateTime GetAsTime();
    [Delphi(PortStatus.Ported)] void SetAsTime(TDateTime Value);
    [Delphi(PortStatus.Ported)] void SetAsDate(TDateTime Value);
    [Delphi(PortStatus.Ported)] void SetAsBlob(string Value);
    [Delphi(PortStatus.Ported)] string GetAsBlob();

    [Delphi(PortStatus.Ported)] TField Field => GetField();
    [Delphi(PortStatus.Ported)] object? AsVariant { get => GetAsVariant(); set => SetAsVariant(value); }
    [Delphi(PortStatus.Ported)] object? Value { get => GetAsVariant(); set => SetAsVariant(value); }
    [Delphi(PortStatus.Ported)] string AsString { get => GetAsString(); set => SetAsString(value); }
    [Delphi(PortStatus.Ported)] int AsInteger { get => GetAsInteger(); set => SetAsInteger(value); }
    [Delphi(PortStatus.Ported)] bool AsBoolean { get => GetAsBoolean(); set => SetAsBoolean(value); }
    [Delphi(PortStatus.Ported)] decimal AsCurrency { get => GetAsCurrency(); set => SetAsCurrency(value); }
    [Delphi(PortStatus.Ported)] TDateTime AsDateTime { get => GetAsDateTime(); set => SetAsDateTime(value); }
    [Delphi(PortStatus.Ported)] double AsFloat { get => GetAsFloat(); set => SetAsFloat(value); }
    [Delphi(PortStatus.Ported)] TDateTime AsDate { get => GetAsDate(); set => SetAsDate(value); }
    [Delphi(PortStatus.Ported)] TDateTime AsTime { get => GetAsTime(); set => SetAsTime(value); }
    [Delphi(PortStatus.Ported)] string AsBlob { get => GetAsBlob(); set => SetAsBlob(value); }
    [Delphi(PortStatus.Ported)] bool IsNull => GetIsNull();
    [Delphi(PortStatus.Ported)] string FieldName => GetFieldName();
}

/// <summary>A query parameter.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldParameter
{
    [Delphi(PortStatus.Ported)] object? GetAsVariant();
    [Delphi(PortStatus.Ported)] void SetAsVariant(object? NewValue);
    [Delphi(PortStatus.Ported)] string GetName();
    [Delphi(PortStatus.Ported)] void Clear();
    [Delphi(PortStatus.Ported)] TFieldType GetDataType();
    [Delphi(PortStatus.Ported)] void SetDataType(TFieldType Value);
    [Delphi(PortStatus.Ported)] decimal GetAsBCD();
    [Delphi(PortStatus.Ported)] bool GetAsBoolean();
    [Delphi(PortStatus.Ported)] TDateTime GetAsDateTime();
    [Delphi(PortStatus.Ported)] decimal GetAsCurrency();
    [Delphi(PortStatus.Ported)] double GetAsFloat();
    [Delphi(PortStatus.Ported)] int GetAsInteger();
    [Delphi(PortStatus.Ported)] string GetAsMemo();
    [Delphi(PortStatus.Ported)] string GetAsString();
    [Delphi(PortStatus.Ported)] bool GetIsNull();
    [Delphi(PortStatus.Ported)] void SetAsBCD(decimal Value);
    [Delphi(PortStatus.Ported, Note = "A blob is a string of byte characters.")] void SetAsBlob(string Value);
    [Delphi(PortStatus.Ported)] void SetAsBoolean(bool Value);
    [Delphi(PortStatus.Ported)] void SetAsCurrency(decimal Value);
    [Delphi(PortStatus.Ported)] void SetAsDate(TDateTime Value);
    [Delphi(PortStatus.Ported)] void SetAsDateTime(TDateTime Value);
    [Delphi(PortStatus.Ported)] void SetAsFloat(double Value);
    [Delphi(PortStatus.Ported)] void SetAsInteger(int Value);
    [Delphi(PortStatus.Ported)] void SetAsMemo(string Value);
    [Delphi(PortStatus.Ported)] void SetAsString(string Value);
    [Delphi(PortStatus.Ported)] void SetAsSmallInt(int Value);
    [Delphi(PortStatus.Ported)] void SetAsTime(TDateTime Value);
    [Delphi(PortStatus.Ported)] void SetAsWord(int Value);
    [Delphi(PortStatus.Ported)] void SetText(string Value);
    [Delphi(PortStatus.Ported)] void AssignFieldValue(IBoldField source);

    [Delphi(PortStatus.Ported)] object? asVariant { get => GetAsVariant(); set => SetAsVariant(value); }
    [Delphi(PortStatus.Ported)] string Name => GetName();
    [Delphi(PortStatus.Ported)] TFieldType DataType { get => GetDataType(); set => SetDataType(value); }
    [Delphi(PortStatus.Ported)] decimal AsBCD { get => GetAsBCD(); set => SetAsBCD(value); }
    [Delphi(PortStatus.Ported)] bool AsBoolean { get => GetAsBoolean(); set => SetAsBoolean(value); }
    [Delphi(PortStatus.Ported)] decimal AsCurrency { get => GetAsCurrency(); set => SetAsCurrency(value); }
    [Delphi(PortStatus.Ported)] TDateTime AsDate { get => GetAsDateTime(); set => SetAsDate(value); }
    [Delphi(PortStatus.Ported)] TDateTime AsDateTime { get => GetAsDateTime(); set => SetAsDateTime(value); }
    [Delphi(PortStatus.Ported)] double AsFloat { get => GetAsFloat(); set => SetAsFloat(value); }
    [Delphi(PortStatus.Ported)] int AsInteger { get => GetAsInteger(); set => SetAsInteger(value); }
    [Delphi(PortStatus.Ported)] int AsSmallInt { get => GetAsInteger(); set => SetAsSmallInt(value); }
    [Delphi(PortStatus.Ported)] string AsMemo { get => GetAsMemo(); set => SetAsMemo(value); }
    [Delphi(PortStatus.Ported)] string AsString { get => GetAsString(); set => SetAsString(value); }
    [Delphi(PortStatus.Ported)] TDateTime AsTime { get => GetAsDateTime(); set => SetAsTime(value); }
    [Delphi(PortStatus.Ported)] int AsWord { get => GetAsInteger(); set => SetAsWord(value); }
    [Delphi(PortStatus.Ported)] bool IsNull => GetIsNull();
    [Delphi(PortStatus.Ported)] string Text { get => GetAsString(); set => SetText(value); }
}

/// <summary>A data set: navigation, fields and editing.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldDataSet
{
    [Delphi(PortStatus.Ported)] void Append();
    [Delphi(PortStatus.Ported)] void Close();
    [Delphi(PortStatus.Ported)] IBoldField FieldByName(string FieldName);
    [Delphi(PortStatus.Ported)] IBoldField? FindField(string FieldName);
    [Delphi(PortStatus.Ported)] void First();
    [Delphi(PortStatus.Ported)] TDataSet GetDataSet();
    [Delphi(PortStatus.Ported)] bool GetEof();
    [Delphi(PortStatus.Ported)] TFieldDefs GetFieldDefs();
    [Delphi(PortStatus.Ported)] IBoldField GetFields(int Index);
    [Delphi(PortStatus.Ported)] int GetFieldCount();
    [Delphi(PortStatus.Ported)] object? GetFieldValue(string FieldName);
    [Delphi(PortStatus.Ported)] void Next();
    [Delphi(PortStatus.Ported)] void Open();
    [Delphi(PortStatus.Ported)] void Edit();
    [Delphi(PortStatus.Ported)] void Delete();
    [Delphi(PortStatus.Ported)] int MoveBy(int Distance);
    [Delphi(PortStatus.Ported)] bool Locate(string KeyFields, object? KeyValues, DelphiSet<TLocateOption> Options);
    [Delphi(PortStatus.Ported)] object GetImplementor();
    [Delphi(PortStatus.Ported)] TDataSetState GetState();
    [Delphi(PortStatus.Ported)] void Post();
    [Delphi(PortStatus.Ported)] void SetFieldValue(string FieldName, object? Value);

    [Delphi(PortStatus.Ported)] TDataSet AsDataSet => GetDataSet();
    [Delphi(PortStatus.Ported)] bool Eof => GetEof();
    [Delphi(PortStatus.Ported)] TFieldDefs FieldDefs => GetFieldDefs();
    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; use GetFields(Index).")] IBoldField Fields(int Index) => GetFields(Index);
    [Delphi(PortStatus.Ported)] int FieldCount => GetFieldCount();
    [Delphi(PortStatus.Ported)] object? this[string FieldName] { get => GetFieldValue(FieldName); set => SetFieldValue(FieldName, value); }
    [Delphi(PortStatus.Ported)] object Implementor => GetImplementor();
    [Delphi(PortStatus.Ported)] TDataSetState State => GetState();
}

/// <summary>A statement that returns no rows, with SQL batching.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldExecQuery
{
    [Delphi(PortStatus.Ported)] string GetSQLText();
    [Delphi(PortStatus.Ported)] void StartSQLBatch();
    [Delphi(PortStatus.Ported)] void EndSQLBatch();
    [Delphi(PortStatus.Ported)] void FailSQLBatch();
    [Delphi(PortStatus.Ported)] void ExecSQL();
    [Delphi(PortStatus.Ported)] void AssignSQL(TStrings SQL);
    [Delphi(PortStatus.Ported)] void AssignSQLText(string SQL);
    [Delphi(PortStatus.Ported)] void ClearParams();
    [Delphi(PortStatus.Ported)] IBoldParameter ParamByName(string Value);
    [Delphi(PortStatus.Ported)] int GetRowsAffected();
    [Delphi(PortStatus.Ported)] object GetImplementor();

    [Delphi(PortStatus.Ported)] int RowsAffected => GetRowsAffected();
    [Delphi(PortStatus.Ported)] object Implementor => GetImplementor();
    [Delphi(PortStatus.Ported)] string SQLText => GetSQLText();
}

/// <summary>Something with parameters.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldParameterized
{
    [Delphi(PortStatus.Ported)] void ClearParams();
    [Delphi(PortStatus.Ported)] void AssignParams(TParams Params);
    [Delphi(PortStatus.Ported)] IBoldParameter ParamByName(string Value);
    [Delphi(PortStatus.Ported)] int GetParamCount();
    [Delphi(PortStatus.Ported)] IBoldParameter GetParams(int i);
    [Delphi(PortStatus.Ported)] IBoldParameter Createparam(TFieldType FldType, string ParamName, TParamType ParamType, int Size);

    [Delphi(PortStatus.Ported, Note = "An indexed property in Delphi; use GetParams(i).")] IBoldParameter Params(int i) => GetParams(i);
    [Delphi(PortStatus.Ported)] int ParamCount => GetParamCount();
}

/// <summary>A query that returns rows.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldQuery : IBoldDataSet
{
    [Delphi(PortStatus.Ported)] string GetSQLText();
    [Delphi(PortStatus.Ported)] bool GetRequestLiveQuery();
    [Delphi(PortStatus.Ported)] void SetRequestLiveQuery(bool NewValue);
    [Delphi(PortStatus.Ported)] int GetRecordCount();
    [Delphi(PortStatus.Ported)] void AssignSQLText(string SQL);
    [Delphi(PortStatus.Ported)] void AssignSQL(TStrings SQL);
    [Delphi(PortStatus.Ported)] void ClearParams();
    [Delphi(PortStatus.Ported)] void AssignParams(TParams? Params);
    [Delphi(PortStatus.Ported)] IBoldParameter ParamByName(string Value);

    [Delphi(PortStatus.Ported)] bool RequestLiveQuery { get => GetRequestLiveQuery(); set => SetRequestLiveQuery(value); }
    [Delphi(PortStatus.Ported)] int RecordCount => GetRecordCount();
    [Delphi(PortStatus.Ported)] string SQLText => GetSQLText();
}

/// <summary>A table, for databases whose schema is made through table components.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldTable : IBoldDataSet
{
    [Delphi(PortStatus.Ported)] void AddIndex(string Name, string Fields, DelphiSet<TIndexOption> Options, string DescFields = "");
    [Delphi(PortStatus.Ported)] TIndexDefs GetIndexDefs();
    [Delphi(PortStatus.Ported)] void SetTableName(string NewName);
    [Delphi(PortStatus.Ported)] string GetTableName();
    [Delphi(PortStatus.Ported)] void SetExclusive(bool NewValue);
    [Delphi(PortStatus.Ported)] bool GetExclusive();
    [Delphi(PortStatus.Ported)] void CreateTable();
    [Delphi(PortStatus.Ported)] void DeleteTable();
    [Delphi(PortStatus.Ported)] bool GetExists();

    [Delphi(PortStatus.Ported)] TIndexDefs IndexDefs => GetIndexDefs();
    [Delphi(PortStatus.Ported)] string Tablename { get => GetTableName(); set => SetTableName(value); }
    [Delphi(PortStatus.Ported)] bool Exclusive { get => GetExclusive(); set => SetExclusive(value); }
    [Delphi(PortStatus.Ported)] bool Exists => GetExists();
}

/// <summary>A database connection: transactions, schema information and query objects.</summary>
[Delphi(PortStatus.Ported)]
public interface IBoldDataBase
{
    [Delphi(PortStatus.Ported)] void StartTransaction();
    [Delphi(PortStatus.Ported)] void Commit();
    [Delphi(PortStatus.Ported)] void RollBack();
    [Delphi(PortStatus.Ported)] void Open();
    [Delphi(PortStatus.Ported)] void Close();
    [Delphi(PortStatus.Ported)] bool GetInTransaction();
    [Delphi(PortStatus.Ported)] bool GetIsSQLBased();
    [Delphi(PortStatus.Ported)] void AllTableNames(string Pattern, bool SystemTables, TStrings TableNameList);
    [Delphi(PortStatus.Ported)] void SetlogInPrompt(bool NewValue);
    [Delphi(PortStatus.Ported)] bool GetLogInPrompt();
    [Delphi(PortStatus.Ported)] void SetKeepConnection(bool NewValue);
    [Delphi(PortStatus.Ported)] bool GetKeepConnection();
    [Delphi(PortStatus.Ported)] bool GetConnected();
    [Delphi(PortStatus.Ported)] bool SupportsDefaultColumnValues();
    [Delphi(PortStatus.Ported)] IBoldQuery GetQuery();
    [Delphi(PortStatus.Ported)] void ReleaseQuery(ref IBoldQuery? Query);
    [Delphi(PortStatus.Ported)] IBoldExecQuery GetExecQuery();
    [Delphi(PortStatus.Ported)] void ReleaseExecQuery(ref IBoldExecQuery? Query);
    [Delphi(PortStatus.Ported)] IBoldTable GetTable();
    [Delphi(PortStatus.Ported)] void ReleaseTable(ref IBoldTable? Table);
    [Delphi(PortStatus.Ported)] bool SupportsTableCreation();
    [Delphi(PortStatus.Ported)] void ReleaseCachedObjects();
    [Delphi(PortStatus.Ported)] bool TableExists(string TableName);
    [Delphi(PortStatus.Ported)] TBoldSQLDataBaseConfig GetSQLDatabaseConfig();

    [Delphi(PortStatus.Ported)] bool InTransaction => GetInTransaction();
    [Delphi(PortStatus.Ported)] bool IsSqlBased => GetIsSQLBased();
    [Delphi(PortStatus.Ported)] bool LogInPrompt { get => GetLogInPrompt(); set => SetlogInPrompt(value); }
    [Delphi(PortStatus.Ported)] bool KeepConnection { get => GetKeepConnection(); set => SetKeepConnection(value); }
    [Delphi(PortStatus.Ported)] bool Connected => GetConnected();
    [Delphi(PortStatus.Ported)] TBoldSQLDataBaseConfig SQLDatabaseConfig => GetSQLDatabaseConfig();
}

/// <summary>The base of parameter wrappers: knows the data set wrapper (for the empty-string marker).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldParameterWrapper : TBoldRefCountedObject
{
    private readonly TBoldDataSetWrapper fDatasetWrapper;

    [Delphi(PortStatus.Ported)]
    public TBoldParameterWrapper(TBoldDataSetWrapper DatasetWrapper)
    {
        fDatasetWrapper = DatasetWrapper;
    }

    [Delphi(PortStatus.Ported)] protected TBoldDataSetWrapper DatasetWrapper => fDatasetWrapper;
}

/// <summary>A parameter over a TParam; empty strings are written as the configuration's empty-string marker.</summary>
[Delphi(PortStatus.Ported)]
public class TBoldDbParameter : TBoldParameterWrapper, IBoldParameter
{
    private readonly TParam fParameter;

    [Delphi(PortStatus.Ported)]
    public TBoldDbParameter(TParam DbParameter, TBoldDataSetWrapper DatasetWrapper)
        : base(DatasetWrapper)
    {
        fParameter = DbParameter;
    }

    private string EmptyStringMarker => DatasetWrapper.DatabaseWrapper.SQLDatabaseConfig.EmptyStringMarker;

    [Delphi(PortStatus.Ported)] public object? GetAsVariant() => Parameter.Value;
    [Delphi(PortStatus.Ported)] public void SetAsVariant(object? NewValue) => Parameter.Value = NewValue;
    [Delphi(PortStatus.Ported)] public string GetName() => Parameter.Name;
    [Delphi(PortStatus.Ported)] public void Clear() => Parameter.Clear();
    [Delphi(PortStatus.Ported)] public TFieldType GetDataType() => Parameter.DataType;
    [Delphi(PortStatus.Ported)] public void SetDataType(TFieldType Value) => Parameter.DataType = Value;
    [Delphi(PortStatus.Ported)] public decimal GetAsBCD() => Parameter.AsBCD;
    [Delphi(PortStatus.Ported)] public bool GetAsBoolean() => Parameter.AsBoolean;
    [Delphi(PortStatus.Ported)] public decimal GetAsCurrency() => Parameter.AsCurrency;
    [Delphi(PortStatus.Ported)] public double GetAsFloat() => Parameter.AsFloat;
    [Delphi(PortStatus.Ported)] public int GetAsInteger() => Parameter.AsInteger;
    [Delphi(PortStatus.Ported)] public string GetAsMemo() => Parameter.AsMemo;

    /// <summary>The string; the empty-string marker reads as empty.</summary>
    [Delphi(PortStatus.Ported)]
    public string GetAsString()
    {
        string result = Parameter.AsString;
        return result == EmptyStringMarker ? "" : result;
    }

    [Delphi(PortStatus.Ported)] public bool GetIsNull() => Parameter.IsNull;
    [Delphi(PortStatus.Ported)] public void SetAsBCD(decimal Value) => Parameter.AsBCD = Value;
    [Delphi(PortStatus.Ported)] public void SetAsBlob(string Value) => Parameter.AsBlob = Value;
    [Delphi(PortStatus.Ported)] public void SetAsBoolean(bool Value) => Parameter.AsBoolean = Value;
    [Delphi(PortStatus.Ported)] public void SetAsCurrency(decimal Value) => Parameter.AsCurrency = Value;
    [Delphi(PortStatus.Ported)] public void SetAsDate(TDateTime Value) => Parameter.AsDate = Value;
    [Delphi(PortStatus.Ported)] public void SetAsFloat(double Value) => Parameter.AsFloat = Value;
    [Delphi(PortStatus.Ported)] public void SetAsInteger(int Value) => Parameter.AsInteger = Value;
    [Delphi(PortStatus.Ported)] public void SetAsMemo(string Value) => Parameter.AsMemo = Value;

    /// <summary>Sets the string; an empty string is written as the empty-string marker.</summary>
    [Delphi(PortStatus.Ported)]
    public void SetAsString(string Value) => Parameter.Value = Value.Length == 0 ? EmptyStringMarker : Value;

    [Delphi(PortStatus.Ported)] public void SetAsSmallInt(int Value) => Parameter.AsSmallInt = Value;
    [Delphi(PortStatus.Ported)] public void SetAsTime(TDateTime Value) => Parameter.AsTime = Value;
    [Delphi(PortStatus.Ported)] public void SetAsWord(int Value) => Parameter.AsWord = Value;
    [Delphi(PortStatus.Ported)] public void SetText(string Value) => Parameter.AsString = Value;
    [Delphi(PortStatus.Ported)] public void AssignFieldValue(IBoldField source) => Parameter.AssignFieldValue(source.Field, source.AsVariant);
    [Delphi(PortStatus.Ported)] private TParam GetParameter() => fParameter;
    [Delphi(PortStatus.Ported)] protected TParam Parameter => GetParameter();
    [Delphi(PortStatus.Ported)] public virtual void SetAsDateTime(TDateTime Value) => Parameter.AsDateTime = Value;
    [Delphi(PortStatus.Ported)] public virtual TDateTime GetAsDateTime() => Parameter.AsDateTime;
}

/// <summary>A data set wrapper: implements IBoldDataSet over the subclass's TDataSet.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldDataSetWrapper : TBoldNonRefCountedObject
{
    private int fLastKnowFieldIndex;
    private readonly TBoldDatabaseWrapper fDatabaseWrapper;

    [Delphi(PortStatus.Ported)]
    protected TBoldDataSetWrapper(TBoldDatabaseWrapper DatabaseWrapper)
    {
        fDatabaseWrapper = DatabaseWrapper;
    }

    [Delphi(PortStatus.Ported, Note = "Returns the System.Type of the field wrapper (a TBoldFieldWrapper with a (TField, TBoldDataSetWrapper) constructor).")]
    protected virtual Type GetFieldWrapperClass() => typeof(TBoldFieldWrapper);

    private IBoldField NewFieldWrapper(TField Field) => (IBoldField)Activator.CreateInstance(GetFieldWrapperClass(), Field, this)!;

    [Delphi(PortStatus.Ported)] public abstract TDataSet GetDataSet();
    [Delphi(PortStatus.Ported)] public bool GetEof() => DataSet.Eof;
    [Delphi(PortStatus.Ported)] public IBoldField GetFields(int Index) => NewFieldWrapper(DataSet.Fields[Index]);
    [Delphi(PortStatus.Ported)] public int GetFieldCount() => DataSet.FieldCount;
    [Delphi(PortStatus.Ported)] public object? GetFieldValue(string FieldName) => DataSet[FieldName];
    [Delphi(PortStatus.Ported)] public void SetFieldValue(string FieldName, object? Value) => DataSet[FieldName] = Value;
    [Delphi(PortStatus.Ported)] public TFieldDefs GetFieldDefs() => DataSet.FieldDefs;
    [Delphi(PortStatus.Ported)] public void Append() => DataSet.Append();

    [Delphi(PortStatus.Ported)]
    public virtual IBoldParameter Createparam(TFieldType FldType, string ParamName, TParamType ParamType, int Size) =>
        throw new EBold(SysUtils.Format(BoldPMConsts.sCreateParamNotImplemented, GetType().Name));

    [Delphi(PortStatus.Ported)] public virtual void Close() => DataSet.Close();

    /// <summary>The field by name; checks the field after the last one asked for first, since fields are usually read in order.</summary>
    [Delphi(PortStatus.Ported)]
    public IBoldField FieldByName(string FieldName)
    {
        TDataSet ds = DataSet;
        if (fLastKnowFieldIndex + 1 >= 0 && fLastKnowFieldIndex + 1 <= ds.FieldCount - 1 && ds.Fields[fLastKnowFieldIndex + 1].FieldName == FieldName)
        {
            fLastKnowFieldIndex++;
        }
        else if (fLastKnowFieldIndex < 0 || fLastKnowFieldIndex > ds.FieldCount - 1 || ds.Fields[fLastKnowFieldIndex].FieldName != FieldName)
        {
            fLastKnowFieldIndex = ds.FieldByName(FieldName).FieldNo - 1;
        }

        return NewFieldWrapper(ds.Fields[fLastKnowFieldIndex]);
    }

    [Delphi(PortStatus.Ported)]
    public IBoldField? FindField(string FieldName) => DataSet.FindField(FieldName) is { } field ? NewFieldWrapper(field) : null;

    [Delphi(PortStatus.Ported)] public void First() => DataSet.First();
    [Delphi(PortStatus.Ported)] public void Delete() => DataSet.Delete();
    [Delphi(PortStatus.Ported)] public void Next() => DataSet.Next();

    [Delphi(PortStatus.Ported)]
    public virtual void Open()
    {
        DataSet.Open();
        fLastKnowFieldIndex = -1;
    }

    [Delphi(PortStatus.Ported)] public void Edit() => DataSet.Edit();
    [Delphi(PortStatus.Ported)] public int MoveBy(int Distance) => DataSet.MoveBy(Distance);
    [Delphi(PortStatus.Ported)] public bool Locate(string KeyFields, object? KeyValues, DelphiSet<TLocateOption> Options) => DataSet.Locate(KeyFields, KeyValues, Options);
    [Delphi(PortStatus.Ported)] public void Post() => DataSet.Post();
    [Delphi(PortStatus.Ported)] public object GetImplementor() => this;
    [Delphi(PortStatus.Ported)] public TDataSetState GetState() => DataSet.State;
    [Delphi(PortStatus.Ported)] public TBoldDatabaseWrapper DatabaseWrapper => fDatabaseWrapper;
    [Delphi(PortStatus.Ported)] public TDataSet DataSet => GetDataSet();
}

/// <summary>A database wrapper: keeps a copy of the SQL configuration; exec queries are queries by default.</summary>
[Delphi(PortStatus.Ported)]
public abstract class TBoldDatabaseWrapper : TBoldNonRefCountedObject
{
    private readonly TBoldSQLDataBaseConfig fSQLDataBaseConfig;

    [Delphi(PortStatus.Ported)]
    protected TBoldDatabaseWrapper(TBoldSQLDataBaseConfig SQLDataBaseConfig)
    {
        fSQLDataBaseConfig = new TBoldSQLDataBaseConfig();
        fSQLDataBaseConfig.AssignConfig(SQLDataBaseConfig);
    }

    [Delphi(PortStatus.Ported)] public virtual bool SupportsDefaultColumnValues() => true;
    [Delphi(PortStatus.Ported)] public abstract void AllTableNames(string Pattern, bool SystemTables, TStrings TableNameList);

    /// <summary>Whether a table of the name exists (ignoring case), from AllTableNames.</summary>
    [Delphi(PortStatus.Ported)]
    public bool TableExists(string TableName)
    {
        var TableNames = new TStringList();
        AllTableNames("*", true, TableNames);
        bool result = false;
        for (int i = 0; i < TableNames.Count; i++)
        {
            if (SysUtils.SameText(TableNames[i], TableName))
            {
                result = true;
            }
        }

        return result;
    }

    [Delphi(PortStatus.Ported)] public abstract IBoldQuery GetQuery();
    [Delphi(PortStatus.Ported)] public abstract void ReleaseQuery(ref IBoldQuery? Query);

    /// <summary>A query used as an exec query; raises when the query class does not implement it.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual IBoldExecQuery GetExecQuery()
    {
        IBoldQuery? aQuery = GetQuery();
        if (aQuery is IBoldExecQuery result)
        {
            return result;
        }

        string QueryClassName = aQuery.Implementor.GetType().Name;
        ReleaseQuery(ref aQuery);
        throw new EBoldInternal(SysUtils.Format("%s.GetExecQuery: %s does not implement IBoldExecQuery", QueryClassName, GetType().Name));
    }

    [Delphi(PortStatus.Ported)]
    public virtual void ReleaseExecQuery(ref IBoldExecQuery? Query)
    {
        if (Query is not IBoldQuery Query2)
        {
            throw new EBoldInternal(SysUtils.Format("%s.ReleaseExecQuery: %s queries does not implement IBoldExecQuery", Query?.Implementor.GetType().Name ?? "", GetType().Name));
        }

        Query = null;
        IBoldQuery? q = Query2;
        ReleaseQuery(ref q);
    }

    [Delphi(PortStatus.Ported)] public TBoldSQLDataBaseConfig GetSQLDatabaseConfig() => fSQLDataBaseConfig;
    [Delphi(PortStatus.Ported)] public TBoldSQLDataBaseConfig SQLDatabaseConfig => GetSQLDatabaseConfig();
}

/// <summary>A field of the current record; keeps the value read at creation (AsVariant and IsNull use it).</summary>
[Delphi(PortStatus.Ported)]
public class TBoldFieldWrapper : TBoldRefCountedObject, IBoldField
{
    private object? fSavedValue;
    private readonly TField fField;
    private readonly TBoldDataSetWrapper fDatasetWrapper;

    [Delphi(PortStatus.Ported)]
    public TBoldFieldWrapper(TField Field, TBoldDataSetWrapper DatasetWrapper)
    {
        fField = Field;
        fSavedValue = Field.Value;
        fDatasetWrapper = DatasetWrapper;
    }

    private string EmptyStringMarker => DataSetWrapper.DatabaseWrapper.SQLDatabaseConfig.EmptyStringMarker;

    [Delphi(PortStatus.Ported)] public void SetAsBlob(string Value) => Field.AsVariant = Value;

    [Delphi(PortStatus.Ported, Note = "A byte array reads as a string of byte characters.")]
    public string GetAsBlob() => fField.Value is byte[] ? fField.AsString : Convert.ToString(Field.AsVariant, System.Globalization.CultureInfo.InvariantCulture) ?? "";

    [Delphi(PortStatus.Ported)] public bool GetAsBoolean() => Field.AsBoolean;
    [Delphi(PortStatus.Ported)] public decimal GetAsCurrency() => Field.AsCurrency;
    [Delphi(PortStatus.Ported)] public TDateTime GetAsDate() => SysUtils.DateOf(Field.AsDateTime);
    [Delphi(PortStatus.Ported)] public TDateTime GetAsDateTime() => Field.AsDateTime;
    [Delphi(PortStatus.Ported)] public double GetAsFloat() => Field.AsFloat;
    [Delphi(PortStatus.Ported)] public int GetAsInteger() => Field.AsInteger;

    /// <summary>The string (shared); the empty-string marker reads as empty.</summary>
    [Delphi(PortStatus.Ported)]
    public virtual string GetAsString()
    {
        string result = BoldSharedStrings.BoldSharedStringManager().GetSharedString(Field.AsString);
        return result == EmptyStringMarker ? "" : result;
    }

    [Delphi(PortStatus.Ported)] public TDateTime GetAsTime() => SysUtils.TimeOf(Field.AsDateTime);
    [Delphi(PortStatus.Ported)] public object? GetAsVariant() => fSavedValue;
    [Delphi(PortStatus.Ported)] public TField GetField() => fField;
    [Delphi(PortStatus.Ported)] public string GetFieldName() => Field.FieldName;
    [Delphi(PortStatus.Ported)] public bool GetIsNull() => fSavedValue is null;
    [Delphi(PortStatus.Ported)] public void SetAsBoolean(bool Value) => Field.AsBoolean = Value;
    [Delphi(PortStatus.Ported)] public void SetAsCurrency(decimal Value) => Field.AsCurrency = Value;
    [Delphi(PortStatus.Ported)] public void SetAsDateTime(TDateTime Value) => Field.AsDateTime = Value;
    [Delphi(PortStatus.Ported)] public void SetAsFloat(double Value) => Field.AsFloat = Value;
    [Delphi(PortStatus.Ported)] public void SetAsInteger(int Value) => Field.AsInteger = Value;

    [Delphi(PortStatus.Ported)]
    public void SetAsString(string Value) => Field.AsString = Value.Length == 0 ? EmptyStringMarker : Value;

    [Delphi(PortStatus.Ported)]
    public void SetAsVariant(object? Value)
    {
        Field.AsVariant = Value;
        fSavedValue = Value;
    }

    [Delphi(PortStatus.Ported)] public void SetAsDate(TDateTime Value) => Field.AsDateTime = Value;
    [Delphi(PortStatus.Ported)] public void SetAsTime(TDateTime Value) => Field.AsDateTime = Value;
    [Delphi(PortStatus.Ported)] protected TBoldDataSetWrapper DataSetWrapper => fDatasetWrapper;
    [Delphi(PortStatus.Ported)] public TField Field => GetField();
    [Delphi(PortStatus.Ported)] public string FieldName => GetFieldName();
}

/// <summary>SQL logging: every statement is counted, and logged when BoldSQLLogHandler is set.</summary>
public static partial class BoldDBInterfaces
{
    [Delphi(PortStatus.Ported)] public static TBoldLogHandler? BoldSQLLogHandler = null;
    [Delphi(PortStatus.Ported)] public static int BoldSQLLogCount = 0;

    /// <summary>Counts the statement and logs its lines (the first with the count).</summary>
    [Delphi(PortStatus.Ported)]
    public static void BoldLogSQL(TStrings sql)
    {
        BoldSQLLogCount++;
        if (BoldSQLLogHandler is null)
        {
            return;
        }

        BoldSQLLogHandler.Log(SysUtils.FormatDateTime("c: ", SysUtils.Now()) + SysUtils.Format("SQL %3d- %s", BoldSQLLogCount, SysUtils.Trim(sql[0])));
        for (int i = 1; i < sql.Count; i++)
        {
            if (SysUtils.Trim(sql[i]).Length != 0)
            {
                BoldSQLLogHandler.Log("                                                    " + SysUtils.Trim(sql[i]));
            }
        }
    }
}
