namespace OpenSlalom.Data.Entities;

public sealed class SyncState
{
    public string Id { get; set; } = string.Empty;

    public DateTime LastSyncUtc { get; set; }
}
