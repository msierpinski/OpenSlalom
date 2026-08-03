namespace OpenSlalom.Data;

public sealed class RemoteDbConnectionSettings
{
    private readonly object _syncRoot = new();
    private string _connectionString;

    public RemoteDbConnectionSettings(string connectionString)
    {
        _connectionString = connectionString;
    }

    public string ConnectionString
    {
        get
        {
            lock (_syncRoot)
            {
                return _connectionString;
            }
        }
    }

    public void Update(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        lock (_syncRoot)
        {
            _connectionString = connectionString;
        }
    }
}
