using System.Data.Common;
using System.Net.Sockets;

namespace OpenSlalom.Data;

public static class MySqlEndpointProbe
{
    private const int DefaultPort = 3306;

    public static bool CanReach(string connectionString, int timeoutMs = 800)
    {
        if (!TryGetEndpoint(connectionString, out var host, out var port))
        {
            return false;
        }

        try
        {
            using var client = new TcpClient();
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
            client.ConnectAsync(host, port, cancellation.Token).GetAwaiter().GetResult();
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetEndpoint(string connectionString, out string host, out int port)
    {
        host = string.Empty;
        port = DefaultPort;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            host = GetString(builder,
                "Server",
                "Host",
                "Data Source",
                "DataSource",
                "Address",
                "Addr",
                "Network Address");

            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            var portValue = GetString(builder, "Port");
            if (!string.IsNullOrWhiteSpace(portValue) && int.TryParse(portValue, out var parsedPort) && parsedPort > 0)
            {
                port = parsedPort;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string GetString(DbConnectionStringBuilder builder, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (builder.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
