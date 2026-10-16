// A minimal ADO.NET provider over the system's native SQLite library, for offline builds that cannot restore
// Microsoft.Data.Sqlite. It supports what the tests need: commands with several statements, @name parameters,
// transactions and materialized readers. Not for production use.

using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Bold.Tests.Sqlite;

internal static unsafe class Native
{
    private const string Lib = "sqlite3";

    public const int SQLITE_OK = 0;
    public const int SQLITE_ROW = 100;
    public const int SQLITE_DONE = 101;
    public const int SQLITE_INTEGER = 1;
    public const int SQLITE_FLOAT = 2;
    public const int SQLITE_TEXT = 3;
    public const int SQLITE_BLOB = 4;
    public const int SQLITE_NULL = 5;

    private static readonly IntPtr SQLITE_TRANSIENT = new(-1);

    static Native()
    {
        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, (name, assembly, path) =>
            name == Lib && NativeLibrary.TryLoad("libsqlite3.so.0", out IntPtr handle) ? handle : IntPtr.Zero);
    }

    [DllImport(Lib)] public static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr vfs);
    [DllImport(Lib)] public static extern int sqlite3_close_v2(IntPtr db);
    [DllImport(Lib)] public static extern int sqlite3_prepare_v2(IntPtr db, byte* sql, int nByte, out IntPtr stmt, out byte* tail);
    [DllImport(Lib)] public static extern int sqlite3_step(IntPtr stmt);
    [DllImport(Lib)] public static extern int sqlite3_finalize(IntPtr stmt);
    [DllImport(Lib)] public static extern int sqlite3_changes(IntPtr db);
    [DllImport(Lib)] public static extern IntPtr sqlite3_errmsg(IntPtr db);
    [DllImport(Lib)] public static extern int sqlite3_bind_parameter_count(IntPtr stmt);
    [DllImport(Lib)] public static extern IntPtr sqlite3_bind_parameter_name(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern int sqlite3_bind_null(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern int sqlite3_bind_int64(IntPtr stmt, int index, long value);
    [DllImport(Lib)] public static extern int sqlite3_bind_double(IntPtr stmt, int index, double value);
    [DllImport(Lib)] public static extern int sqlite3_bind_text(IntPtr stmt, int index, byte[] value, int n, IntPtr destructor);
    [DllImport(Lib)] public static extern int sqlite3_bind_blob(IntPtr stmt, int index, byte[] value, int n, IntPtr destructor);
    [DllImport(Lib)] public static extern int sqlite3_column_count(IntPtr stmt);
    [DllImport(Lib)] public static extern IntPtr sqlite3_column_name(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern IntPtr sqlite3_column_decltype(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern int sqlite3_column_type(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern long sqlite3_column_int64(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern double sqlite3_column_double(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern IntPtr sqlite3_column_text(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern IntPtr sqlite3_column_blob(IntPtr stmt, int index);
    [DllImport(Lib)] public static extern int sqlite3_column_bytes(IntPtr stmt, int index);

    public static string Utf8(IntPtr p) => p == IntPtr.Zero ? "" : Marshal.PtrToStringUTF8(p) ?? "";

    public static void Check(IntPtr db, int rc)
    {
        if (rc is not (SQLITE_OK or SQLITE_ROW or SQLITE_DONE))
        {
            throw new SqliteException(Utf8(sqlite3_errmsg(db)));
        }
    }

    public static void Bind(IntPtr stmt, int index, object? value)
    {
        switch (value)
        {
            case null or DBNull:
                sqlite3_bind_null(stmt, index);
                break;
            case bool b:
                sqlite3_bind_int64(stmt, index, b ? 1 : 0);
                break;
            case byte or sbyte or short or ushort or int or uint or long:
                sqlite3_bind_int64(stmt, index, Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case float or double:
                sqlite3_bind_double(stmt, index, Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            case decimal d:
                BindText(stmt, index, d.ToString(CultureInfo.InvariantCulture));
                break;
            case DateTime dt:
                BindText(stmt, index, dt.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture));
                break;
            case byte[] bytes:
                sqlite3_bind_blob(stmt, index, bytes, bytes.Length, SQLITE_TRANSIENT);
                break;
            default:
                BindText(stmt, index, Convert.ToString(value, CultureInfo.InvariantCulture) ?? "");
                break;
        }
    }

    private static void BindText(IntPtr stmt, int index, string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        sqlite3_bind_text(stmt, index, bytes, bytes.Length, SQLITE_TRANSIENT);
    }

    public static object? Column(IntPtr stmt, int i)
    {
        switch (sqlite3_column_type(stmt, i))
        {
            case SQLITE_INTEGER:
                return sqlite3_column_int64(stmt, i);
            case SQLITE_FLOAT:
                return sqlite3_column_double(stmt, i);
            case SQLITE_TEXT:
                return Utf8(sqlite3_column_text(stmt, i));
            case SQLITE_BLOB:
                int n = sqlite3_column_bytes(stmt, i);
                var bytes = new byte[n];
                if (n > 0)
                {
                    Marshal.Copy(sqlite3_column_blob(stmt, i), bytes, 0, n);
                }

                return bytes;
            default:
                return null;
        }
    }
}

public sealed class SqliteException(string message) : DbException(message);

public sealed class SqliteConnection : DbConnection
{
    private IntPtr fDb;
    private string fConnectionString;

    public SqliteConnection(string connectionString)
    {
        fConnectionString = connectionString;
    }

    internal IntPtr Handle => fDb;
    internal SqliteTransaction? CurrentTransaction { get; set; }

    [AllowNull]
    public override string ConnectionString { get => fConnectionString; set => fConnectionString = value ?? ""; }
    public override string Database => "main";
    public override string DataSource => fConnectionString;
    public override string ServerVersion => "native";
    public override ConnectionState State => fDb == IntPtr.Zero ? ConnectionState.Closed : ConnectionState.Open;

    public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

    public override void Open()
    {
        if (fDb != IntPtr.Zero)
        {
            return;
        }

        string file = fConnectionString.Split('=', 2)[^1].Trim();
        Native.Check(IntPtr.Zero, Native.sqlite3_open_v2(Encoding.UTF8.GetBytes(file + "\0"), out fDb, 0x06, IntPtr.Zero));
    }

    public override void Close()
    {
        if (fDb != IntPtr.Zero)
        {
            Native.sqlite3_close_v2(fDb);
            fDb = IntPtr.Zero;
        }
    }

    internal void Execute(string sql)
    {
        using DbCommand command = CreateDbCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        Execute("BEGIN");
        CurrentTransaction = new SqliteTransaction(this);
        return CurrentTransaction;
    }

    protected override DbCommand CreateDbCommand() => new SqliteCommand { Connection = this };

    protected override void Dispose(bool disposing)
    {
        Close();
        base.Dispose(disposing);
    }
}

public sealed class SqliteTransaction(SqliteConnection connection) : DbTransaction
{
    private bool fDone;

    public override IsolationLevel IsolationLevel => IsolationLevel.Serializable;
    protected override DbConnection DbConnection => connection;

    public override void Commit() => Finish("COMMIT");

    public override void Rollback() => Finish("ROLLBACK");

    private void Finish(string sql)
    {
        if (!fDone)
        {
            fDone = true;
            connection.CurrentTransaction = null;
            connection.Execute(sql);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!fDone && connection.State == ConnectionState.Open)
        {
            Rollback();
        }

        base.Dispose(disposing);
    }
}

public sealed class SqliteParameter : DbParameter
{
    public override DbType DbType { get; set; }
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; }
    [AllowNull] public override string ParameterName { get; set; } = "";
    public override int Size { get; set; }
    [AllowNull] public override string SourceColumn { get; set; } = "";
    public override bool SourceColumnNullMapping { get; set; }
    public override object? Value { get; set; }
    public override void ResetDbType() => DbType = DbType.String;
}

public sealed class SqliteParameterCollection : DbParameterCollection
{
    private readonly List<SqliteParameter> fItems = [];

    public override int Count => fItems.Count;
    public override object SyncRoot => fItems;
    public override int Add(object value)
    {
        fItems.Add((SqliteParameter)value);
        return fItems.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (object v in values)
        {
            Add(v);
        }
    }

    public override void Clear() => fItems.Clear();
    public override bool Contains(object value) => fItems.Contains((SqliteParameter)value);
    public override bool Contains(string value) => IndexOf(value) >= 0;
    public override void CopyTo(Array array, int index) => ((ICollection)fItems).CopyTo(array, index);
    public override IEnumerator GetEnumerator() => fItems.GetEnumerator();
    public override int IndexOf(object value) => fItems.IndexOf((SqliteParameter)value);
    public override int IndexOf(string parameterName) => fItems.FindIndex(p => string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase));
    public override void Insert(int index, object value) => fItems.Insert(index, (SqliteParameter)value);
    public override void Remove(object value) => fItems.Remove((SqliteParameter)value);
    public override void RemoveAt(int index) => fItems.RemoveAt(index);
    public override void RemoveAt(string parameterName) => fItems.RemoveAt(IndexOf(parameterName));
    protected override DbParameter GetParameter(int index) => fItems[index];
    protected override DbParameter GetParameter(string parameterName) => fItems[IndexOf(parameterName)];
    protected override void SetParameter(int index, DbParameter value) => fItems[index] = (SqliteParameter)value;
    protected override void SetParameter(string parameterName, DbParameter value) => fItems[IndexOf(parameterName)] = (SqliteParameter)value;
}

public sealed unsafe class SqliteCommand : DbCommand
{
    private readonly SqliteParameterCollection fParameters = new();

    [AllowNull] public override string CommandText { get; set; } = "";
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => fParameters;
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new SqliteParameter();

    private SqliteConnection Conn => (SqliteConnection)DbConnection!;

    /// <summary>Runs every statement; returns the rows of the last statement that has columns, and the total changes.</summary>
    private (List<string> names, List<string> types, List<object?[]> rows, int changes) Run()
    {
        IntPtr db = Conn.Handle;
        byte[] sql = Encoding.UTF8.GetBytes(CommandText + "\0");
        var names = new List<string>();
        var types = new List<string>();
        var rows = new List<object?[]>();
        int changes = 0;
        fixed (byte* start = sql)
        {
            byte* current = start;
            while (*current != 0)
            {
                Native.Check(db, Native.sqlite3_prepare_v2(db, current, -1, out IntPtr stmt, out byte* tail));
                current = tail;
                if (stmt == IntPtr.Zero)
                {
                    continue;
                }

                try
                {
                    for (int p = 1; p <= Native.sqlite3_bind_parameter_count(stmt); p++)
                    {
                        string name = Native.Utf8(Native.sqlite3_bind_parameter_name(stmt, p));
                        int index = fParameters.IndexOf(name);
                        if (index < 0 && name.Length > 0)
                        {
                            index = fParameters.IndexOf(name[1..]);
                        }

                        Native.Bind(stmt, p, index >= 0 ? fParameters[index].Value : null);
                    }

                    int columns = Native.sqlite3_column_count(stmt);
                    var stmtRows = new List<object?[]>();
                    int rc;
                    while ((rc = Native.sqlite3_step(stmt)) == Native.SQLITE_ROW)
                    {
                        var row = new object?[columns];
                        for (int c = 0; c < columns; c++)
                        {
                            row[c] = Native.Column(stmt, c);
                        }

                        stmtRows.Add(row);
                    }

                    Native.Check(db, rc);
                    if (columns > 0)
                    {
                        names.Clear();
                        types.Clear();
                        for (int c = 0; c < columns; c++)
                        {
                            names.Add(Native.Utf8(Native.sqlite3_column_name(stmt, c)));
                            types.Add(Native.Utf8(Native.sqlite3_column_decltype(stmt, c)));
                        }

                        rows = stmtRows;
                    }
                    else
                    {
                        changes += Native.sqlite3_changes(db);
                    }
                }
                finally
                {
                    Native.sqlite3_finalize(stmt);
                }
            }
        }

        return (names, types, rows, changes);
    }

    public override int ExecuteNonQuery() => Run().changes;

    public override object? ExecuteScalar()
    {
        var (_, _, rows, _) = Run();
        return rows.Count > 0 && rows[0].Length > 0 ? rows[0][0] ?? DBNull.Value : null;
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        var (names, types, rows, changes) = Run();
        return new SqliteDataReader(names, types, rows, changes);
    }
}

public sealed class SqliteDataReader(List<string> names, List<string> declTypes, List<object?[]> rows, int changes) : DbDataReader
{
    private int fRow = -1;

    public override int Depth => 0;
    public override int FieldCount => names.Count;
    public override bool HasRows => rows.Count > 0;
    public override bool IsClosed => false;
    public override int RecordsAffected => changes;
    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override bool Read() => ++fRow < rows.Count;
    public override bool NextResult() => false;
    public override string GetName(int ordinal) => names[ordinal];
    public override int GetOrdinal(string name) => names.FindIndex(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
    public override string GetDataTypeName(int ordinal) => declTypes[ordinal];

    /// <summary>The column type from its declared type, else from the first non-null value.</summary>
    public override Type GetFieldType(int ordinal)
    {
        string decl = declTypes[ordinal].ToUpperInvariant();
        if (decl.Contains("INT", StringComparison.Ordinal))
        {
            return typeof(long);
        }

        if (decl.Contains("CHAR", StringComparison.Ordinal) || decl.Contains("TEXT", StringComparison.Ordinal) || decl.Contains("CLOB", StringComparison.Ordinal))
        {
            return typeof(string);
        }

        if (decl.Contains("BLOB", StringComparison.Ordinal))
        {
            return typeof(byte[]);
        }

        if (decl.Contains("REAL", StringComparison.Ordinal) || decl.Contains("FLOA", StringComparison.Ordinal) || decl.Contains("DOUB", StringComparison.Ordinal))
        {
            return typeof(double);
        }

        foreach (object?[] row in rows)
        {
            if (row[ordinal] is { } value)
            {
                return value.GetType();
            }
        }

        return typeof(string);
    }

    public override object GetValue(int ordinal) => rows[fRow][ordinal] ?? DBNull.Value;
    public override bool IsDBNull(int ordinal) => rows[fRow][ordinal] is null;

    public override int GetValues(object[] values)
    {
        int n = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < n; i++)
        {
            values[i] = GetValue(i);
        }

        return n;
    }

    public override bool GetBoolean(int ordinal) => Convert.ToInt64(GetValue(ordinal), CultureInfo.InvariantCulture) != 0;
    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal), CultureInfo.InvariantCulture);
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override char GetChar(int ordinal) => GetString(ordinal)[0];
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
    public override DateTime GetDateTime(int ordinal) => DateTime.Parse(GetString(ordinal), CultureInfo.InvariantCulture);
    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal), CultureInfo.InvariantCulture);
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal), CultureInfo.InvariantCulture);
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal), CultureInfo.InvariantCulture);
    public override Guid GetGuid(int ordinal) => Guid.Parse(GetString(ordinal));
    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal), CultureInfo.InvariantCulture);
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal), CultureInfo.InvariantCulture);
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal), CultureInfo.InvariantCulture);
    public override string GetString(int ordinal) => Convert.ToString(GetValue(ordinal), CultureInfo.InvariantCulture) ?? "";
    public override IEnumerator GetEnumerator() => throw new NotSupportedException();
}
