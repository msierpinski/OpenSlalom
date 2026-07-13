namespace OpenSlalom.Data;

public sealed record DataSyncStatus(
    bool IsSyncNeeded,
    int PendingFromLocal,
    int PendingFromRemote,
    DateTime? LastSyncUtc,
    string Message);
