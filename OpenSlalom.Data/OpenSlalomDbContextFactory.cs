using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenSlalom.Data;

public sealed class OpenSlalomDbContextFactory : IDesignTimeDbContextFactory<OpenSlalomDbContext>
{
    private static readonly ServerVersion FallbackServerVersion = new MySqlServerVersion(new Version(8, 0, 36));

    public OpenSlalomDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("OPENSLALOM_CONNECTION_STRING")
            ?? "Server=127.0.0.1;Port=3306;Database=open_slalom;User=root;Password=root;";

        var optionsBuilder = new DbContextOptionsBuilder<OpenSlalomDbContext>();
        var serverVersion = ResolveDesignTimeServerVersion(connectionString);
        optionsBuilder.UseMySql(connectionString, serverVersion);

        return new OpenSlalomDbContext(optionsBuilder.Options);
    }

    private static ServerVersion ResolveDesignTimeServerVersion(string connectionString)
    {
        try
        {
            return ServerVersion.AutoDetect(connectionString);
        }
        catch
        {
            return FallbackServerVersion;
        }
    }
}
