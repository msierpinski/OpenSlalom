namespace OpenSlalom.UI;

public sealed class DatabaseRuntimeInfo
{
    public bool LocalSqliteConnected { get; private set; }

    public bool RemoteMySqlConnected { get; private set; }

    public string? LocalSqliteError { get; private set; }

    public string? RemoteMySqlError { get; private set; }

    public void Set(
        bool localSqliteConnected,
        bool remoteMySqlConnected,
        string? localSqliteError,
        string? remoteMySqlError)
    {
        LocalSqliteConnected = localSqliteConnected;
        RemoteMySqlConnected = remoteMySqlConnected;
        LocalSqliteError = localSqliteError;
        RemoteMySqlError = remoteMySqlError;
    }
}
