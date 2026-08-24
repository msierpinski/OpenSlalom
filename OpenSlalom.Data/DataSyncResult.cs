namespace OpenSlalom.Data;

public sealed record DataSyncResult(bool Success, string Message, bool? LocalConnected = null, bool? RemoteConnected = null);
