// Offline builds of this repository only (BoldOffline=true): Microsoft.Data.Sqlite cannot be restored there, so the
// SQL tests' small native SQLite provider stands in for its SqliteConnection (see Directory.Build.targets), and this
// empty namespace lets the examples keep their "using Microsoft.Data.Sqlite;".
namespace Microsoft.Data.Sqlite
{
    internal static class OfflineStandIn
    {
    }
}
