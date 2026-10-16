using System.Data.Common;

namespace Bold.Tests;

/// <summary>In-memory SQLite connections for tests (Microsoft.Data.Sqlite, or the native provider in offline builds).</summary>
internal static class TestDatabase
{
    public static DbConnection NewConnection(string DataSource = ":memory:")
    {
#if BOLD_OFFLINE
        var connection = new Bold.Tests.Sqlite.SqliteConnection("Data Source=" + DataSource);
#else
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=" + DataSource);
#endif
        connection.Open();
        return connection;
    }

    /// <summary>An ADO connection wrapper on a new in-memory database.</summary>
    public static TADOConnection NewADOConnection() => new(NewConnection());

    /// <summary>An ADO connection wrapper on a database file (which outlives closing the connection, as Bold does after creating a schema).</summary>
    public static TADOConnection NewADOConnection(string FileName) => new(NewConnection(FileName));

    /// <summary>Runs SQL directly (setup and checks).</summary>
    public static void Exec(TADOConnection connection, string sql)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public static object? Scalar(TADOConnection connection, string sql)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = command.ExecuteScalar();
        return result is DBNull ? null : result;
    }
}
