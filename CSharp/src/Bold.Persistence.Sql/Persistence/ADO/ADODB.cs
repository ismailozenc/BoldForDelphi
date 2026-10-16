// The .NET stand-ins for the parts of Delphi's ADODB unit that Bold uses: a connection, a query and a table over any
// ADO.NET provider (System.Data.Common), on the RTL's TDataSet emulation. Not a Bold unit, so it has no port markers.

using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bold;

/// <summary>Delphi's TADOConnection over an ADO.NET connection: transactions and table names.</summary>
public class TADOConnection
{
    private readonly DbConnection fConnection;
    private DbTransaction? fTransaction;

    public TADOConnection(DbConnection Connection)
    {
        fConnection = Connection;
    }

    /// <summary>The ADO.NET connection.</summary>
    public DbConnection Connection => fConnection;

    /// <summary>The transaction in progress, used by every command of the connection.</summary>
    public DbTransaction? Transaction => fTransaction;

    public bool Connected => fConnection.State == ConnectionState.Open;
    public bool InTransaction => fTransaction is not null;

    /// <summary>Kept for the interface; ADO.NET has no login prompt.</summary>
    public bool LoginPrompt { get; set; }

    public void Open()
    {
        if (!Connected)
        {
            fConnection.Open();
        }
    }

    public void Close()
    {
        fTransaction?.Dispose();
        fTransaction = null;
        fConnection.Close();
    }

    public void BeginTrans()
    {
        Open();
        fTransaction = fConnection.BeginTransaction();
    }

    public void CommitTrans()
    {
        fTransaction?.Commit();
        fTransaction?.Dispose();
        fTransaction = null;
    }

    public void RollbackTrans()
    {
        fTransaction?.Rollback();
        fTransaction?.Dispose();
        fTransaction = null;
    }

    /// <summary>A command on the connection, in the current transaction.</summary>
    public DbCommand CreateCommand()
    {
        Open();
        DbCommand command = fConnection.CreateCommand();
        command.Transaction = fTransaction;
        return command;
    }

    /// <summary>The names of the tables (system tables too when asked), from the provider's schema, or SQLite's master table.</summary>
    public virtual void GetTableNames(TStrings List, bool SystemTables = false)
    {
        Open();
        List.Clear();
        if (fConnection.GetType().Name.StartsWith("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            using DbCommand command = CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'" + (SystemTables ? "" : " AND name NOT LIKE 'sqlite_%'");
            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                List.Add(reader.GetString(0));
            }

            return;
        }

        DataTable tables = fConnection.GetSchema("Tables");
        foreach (DataRow row in tables.Rows)
        {
            string type = tables.Columns.Contains("TABLE_TYPE") ? Convert.ToString(row["TABLE_TYPE"], CultureInfo.InvariantCulture) ?? "" : "";
            if (!SystemTables && type.Contains("SYSTEM", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (type.Contains("VIEW", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            List.Add(Convert.ToString(row["TABLE_NAME"], CultureInfo.InvariantCulture) ?? "");
        }
    }

    /// <summary>Whether Name matches Mask, where * is any text and ? one character (case insensitive), as Delphi's MatchesMask.</summary>
    public static bool MatchesMask(string Name, string Mask) =>
        Regex.IsMatch(Name, "^" + Regex.Escape(Mask).Replace("\\*", ".*", StringComparison.Ordinal).Replace("\\?", ".", StringComparison.Ordinal) + "$",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
}

/// <summary>Helpers that turn Delphi parameters and SQL into ADO.NET commands.</summary>
public static class ADODBSupport
{
    private static readonly Regex ParamPattern = new(@"'(?:[^']|'')*'|""(?:[^""])*""|(?<![:\w]):(?<name>[A-Za-z_][\w]*)", RegexOptions.Compiled);

    /// <summary>The names of the :Name parameters in the SQL, in order of first use (quoted text is skipped).</summary>
    public static List<string> ParamNames(string SQL)
    {
        var result = new List<string>();
        foreach (Match m in ParamPattern.Matches(SQL))
        {
            if (m.Groups["name"].Success && !result.Exists(n => string.Equals(n, m.Groups["name"].Value, StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(m.Groups["name"].Value);
            }
        }

        return result;
    }

    /// <summary>The SQL with :Name parameters written as @Name, which SQL Server, SQLite and most providers accept.</summary>
    public static string ToProviderSQL(string SQL) =>
        ParamPattern.Replace(SQL, m => m.Groups["name"].Success ? "@" + m.Groups["name"].Value : m.Value);

    /// <summary>Adds the parameters to the command; a blob string is sent as bytes.</summary>
    public static void AddParameters(DbCommand Command, TParams Parameters)
    {
        foreach (TParam p in Parameters)
        {
            DbParameter parameter = Command.CreateParameter();
            parameter.ParameterName = "@" + p.Name;
            object? value = p.Value;
            if (p.DataType is TFieldType.ftBlob or TFieldType.ftGraphic or TFieldType.ftTypedBinary or TFieldType.ftBytes or TFieldType.ftVarBytes)
            {
                parameter.DbType = DbType.Binary;
                if (value is string s)
                {
                    value = Array.ConvertAll(s.ToCharArray(), c => (byte)c);
                }
            }
            else if (value is null)
            {
                parameter.DbType = DbTypeOf(p.DataType);
            }

            parameter.Value = value ?? DBNull.Value;
            Command.Parameters.Add(parameter);
        }
    }

    private static DbType DbTypeOf(TFieldType DataType) => DataType switch
    {
        TFieldType.ftSmallint or TFieldType.ftInteger or TFieldType.ftWord or TFieldType.ftAutoInc => DbType.Int32,
        TFieldType.ftLargeint => DbType.Int64,
        TFieldType.ftBoolean => DbType.Boolean,
        TFieldType.ftFloat => DbType.Double,
        TFieldType.ftCurrency or TFieldType.ftBCD or TFieldType.ftFMTBcd => DbType.Decimal,
        TFieldType.ftDate => DbType.Date,
        TFieldType.ftTime => DbType.Time,
        TFieldType.ftDateTime or TFieldType.ftTimeStamp => DbType.DateTime,
        _ => DbType.String,
    };

    /// <summary>The Delphi field type of a column's .NET type.</summary>
    public static TFieldType FieldTypeOf(Type type) => Type.GetTypeCode(type) switch
    {
        TypeCode.Int16 or TypeCode.Byte or TypeCode.SByte => TFieldType.ftSmallint,
        TypeCode.Int32 or TypeCode.UInt16 => TFieldType.ftInteger,
        TypeCode.Int64 or TypeCode.UInt32 or TypeCode.UInt64 => TFieldType.ftLargeint,
        TypeCode.Boolean => TFieldType.ftBoolean,
        TypeCode.Single or TypeCode.Double => TFieldType.ftFloat,
        TypeCode.Decimal => TFieldType.ftBCD,
        TypeCode.DateTime => TFieldType.ftDateTime,
        TypeCode.String or TypeCode.Char => TFieldType.ftString,
        _ when type == typeof(byte[]) => TFieldType.ftBlob,
        _ when type == typeof(Guid) => TFieldType.ftGuid,
        _ => TFieldType.ftVariant,
    };
}

/// <summary>Delphi's TADOQuery: SQL text with :Name parameters, opened into records or executed.</summary>
public class TADOQuery : TDataSet
{
    public TADOConnection? Connection { get; set; }
    public TStringList SQL { get; } = new();
    public TParams Parameters { get; } = new();
    public int RowsAffected { get; private set; } = -1;

    /// <summary>Fills Parameters with the :Name parameters of the SQL, keeping the values of those already there.</summary>
    public void ParseSQL(string SQLText)
    {
        var old = new TParams();
        old.Assign(Parameters);
        Parameters.Clear();
        foreach (string name in ADODBSupport.ParamNames(SQLText))
        {
            TParam p = Parameters.CreateParam(TFieldType.ftUnknown, name, TParamType.ptInput);
            if (old.FindParam(name) is { } previous)
            {
                p.Assign(previous);
            }
        }
    }

    private DbCommand MakeCommand()
    {
        if (Connection is null)
        {
            throw new EDatabaseError($"{GetType().Name}: Missing Connection");
        }

        DbCommand command = Connection.CreateCommand();
        command.CommandText = ADODBSupport.ToProviderSQL(SQL.Text);
        ADODBSupport.AddParameters(command, Parameters);
        return command;
    }

    public void ExecSQL()
    {
        using DbCommand command = MakeCommand();
        RowsAffected = command.ExecuteNonQuery();
    }

    protected override void InternalOpen(TFieldDefs FieldDefs, List<object?[]> Records)
    {
        using DbCommand command = MakeCommand();
        using DbDataReader reader = command.ExecuteReader();
        FieldDefs.Clear();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            FieldDefs.Add(reader.GetName(i), ADODBSupport.FieldTypeOf(reader.GetFieldType(i)));
        }

        while (reader.Read())
        {
            var record = new object?[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
            {
                record[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            Records.Add(record);
        }
    }
}

/// <summary>Delphi's TADOTable: the rows of a table; appended records are inserted when posted.</summary>
public class TADOTable : TDataSet
{
    public TADOConnection? Connection { get; set; }
    public string TableName { get; set; } = "";
    public TIndexDefs IndexDefs { get; } = new();

    public TADOTable()
    {
        IndexDefs.Updater = ReadIndexDefs;
    }

    /// <summary>
    /// Reads the table's indexes: from SQLite's pragmas (a single integer primary key is the rowid and has no index, so it
    /// is read from the columns), from SQL Server's catalog views, or none for other providers.
    /// </summary>
    private void ReadIndexDefs(TIndexDefs Defs)
    {
        Defs.Clear();
        if (Connection is null || TableName.Length == 0)
        {
            return;
        }

        string provider = Connection.Connection.GetType().FullName ?? "";
        var rows = new List<(string Index, string Column, bool Primary, bool Unique)>();
        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            var indexes = new List<(string Name, bool Unique, bool Primary)>();
            using (DbCommand command = Connection.CreateCommand())
            {
                command.CommandText = $"SELECT name, \"unique\", origin FROM pragma_index_list('{TableName.Replace("'", "''", StringComparison.Ordinal)}')";
                using DbDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    indexes.Add((Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture)!, Convert.ToInt64(reader.GetValue(1), CultureInfo.InvariantCulture) != 0,
                        Convert.ToString(reader.GetValue(2), CultureInfo.InvariantCulture) == "pk"));
                }
            }

            foreach ((string name, bool unique, bool primary) in indexes)
            {
                using DbCommand command = Connection.CreateCommand();
                command.CommandText = $"SELECT name FROM pragma_index_info('{name.Replace("'", "''", StringComparison.Ordinal)}') ORDER BY seqno";
                using DbDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add((name, Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture)!, primary, unique));
                }
            }

            if (!indexes.Exists(i => i.Primary))
            {
                using DbCommand command = Connection.CreateCommand();
                command.CommandText = $"SELECT name FROM pragma_table_info('{TableName.Replace("'", "''", StringComparison.Ordinal)}') WHERE pk > 0 ORDER BY pk";
                using DbDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    rows.Add(("PRIMARY", Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture)!, true, true));
                }
            }
        }
        else if (provider.Contains("SqlClient", StringComparison.OrdinalIgnoreCase))
        {
            using DbCommand command = Connection.CreateCommand();
            command.CommandText =
                "SELECT i.name, c.name, i.is_primary_key, i.is_unique FROM sys.indexes i " +
                "JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id " +
                "JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id " +
                $"WHERE i.object_id = OBJECT_ID('{TableName.Replace("'", "''", StringComparison.Ordinal)}') ORDER BY i.name, ic.key_ordinal";
            using DbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add((reader.GetString(0), reader.GetString(1), reader.GetBoolean(2), reader.GetBoolean(3)));
            }
        }

        foreach (IGrouping<string, (string Index, string Column, bool Primary, bool Unique)> index in rows.GroupBy(r => r.Index))
        {
            var options = new DelphiSet<TIndexOption>();
            if (index.First().Primary)
            {
                options += TIndexOption.ixPrimary;
            }

            if (index.First().Unique)
            {
                options += TIndexOption.ixUnique;
            }

            Defs.Add(index.Key, string.Join(";", index.Select(r => r.Column)), options);
        }
    }

    private TADOQuery MakeQuery(string sql)
    {
        var query = new TADOQuery { Connection = Connection };
        query.SQL.Text = sql;
        return query;
    }

    protected override void InternalOpen(TFieldDefs FieldDefs, List<object?[]> Records)
    {
        TADOQuery query = MakeQuery("SELECT * FROM " + TableName);
        query.Open();
        FieldDefs.Clear();
        foreach (TFieldDef def in query.FieldDefs)
        {
            FieldDefs.Add(def.Name, def.DataType, def.Size, def.Required);
        }

        for (query.First(); !query.Eof; query.Next())
        {
            var record = new object?[query.FieldCount];
            for (int i = 0; i < query.FieldCount; i++)
            {
                record[i] = query.Fields[i].Value;
            }

            Records.Add(record);
        }

        query.Close();
    }

    /// <summary>Inserts a new record; changing an existing record is not supported (Bold only appends to tables).</summary>
    protected override void InternalPost(object?[] Record, object?[]? OldRecord, bool Inserted)
    {
        if (!Inserted)
        {
            throw new EDatabaseError($"{GetType().Name}: editing table rows is not supported");
        }

        var sql = new StringBuilder("INSERT INTO ").Append(TableName).Append(" (");
        var values = new StringBuilder();
        TADOQuery query = MakeQuery("");
        for (int i = 0; i < FieldDefs.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
                values.Append(", ");
            }

            sql.Append(FieldDefs[i].Name);
            values.Append(":p").Append(i);
            query.Parameters.CreateParam(TFieldType.ftUnknown, "p" + i.ToString(CultureInfo.InvariantCulture), TParamType.ptInput).Value = Record[i];
        }

        query.SQL.Text = sql.Append(") VALUES (").Append(values).Append(')').ToString();
        query.ExecSQL();
    }

    protected override void InternalDelete(object?[] Record) =>
        throw new EDatabaseError($"{GetType().Name}: deleting table rows is not supported");
}
