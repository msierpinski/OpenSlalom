namespace OpenSlalom.Data;

public sealed record DatabaseInitializationResult(
    bool LocalSqliteConnected,
    bool RemoteMySqlConnected,
    string? LocalSqliteError,
    string? RemoteMySqlError);
