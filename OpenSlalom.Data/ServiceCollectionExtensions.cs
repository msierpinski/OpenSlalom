using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace OpenSlalom.Data;

public static class ServiceCollectionExtensions
{
    private static readonly ConcurrentDictionary<string, ServerVersion> ServerVersionCache = new();
    private static readonly ServerVersion FallbackServerVersion = new MySqlServerVersion(new Version(8, 0, 36));

    public static IServiceCollection AddOpenSlalomData(
        this IServiceCollection services,
        string connectionString,
        OpenSlalomDatabaseProvider databaseProvider = OpenSlalomDatabaseProvider.MySql)
    {
        services.AddDbContextFactory<OpenSlalomDbContext>(options => ConfigureProvider(options, connectionString, databaseProvider));

        services.AddDbContext<OpenSlalomDbContext>(options => ConfigureProvider(options, connectionString, databaseProvider));

        return services;
    }

    public static IServiceCollection AddOpenSlalomDualData(
        this IServiceCollection services,
        string localSqliteConnectionString,
        string remoteMySqlConnectionString)
    {
        services.AddDbContextFactory<OpenSlalomDbContext>(options =>
            options.UseMySql(remoteMySqlConnectionString, ResolveServerVersion(remoteMySqlConnectionString)));

        services.AddDbContextFactory<LocalOpenSlalomDbContext>(options =>
            options.UseSqlite(localSqliteConnectionString, sqlite =>
                sqlite.MigrationsHistoryTable("__EFMigrationsHistory")));

        services.AddDbContextFactory<RemoteOpenSlalomDbContext>(options =>
            options.UseMySql(remoteMySqlConnectionString, ResolveServerVersion(remoteMySqlConnectionString)));

        services.AddScoped<DataSyncService>();

        return services;
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder options,
        string connectionString,
        OpenSlalomDatabaseProvider databaseProvider)
    {
        if (databaseProvider == OpenSlalomDatabaseProvider.Sqlite)
        {
            options.UseSqlite(connectionString);
            return;
        }

        options.UseMySql(connectionString, ResolveServerVersion(connectionString));
    }

    private static ServerVersion ResolveServerVersion(string connectionString)
    {
        if (ServerVersionCache.TryGetValue(connectionString, out var cached))
        {
            return cached;
        }

        ServerVersion resolved;
        if (!MySqlEndpointProbe.CanReach(connectionString))
        {
            resolved = FallbackServerVersion;
        }
        else
        {
            try
            {
                resolved = ServerVersion.AutoDetect(connectionString);
            }
            catch
            {
                resolved = FallbackServerVersion;
            }
        }

        ServerVersionCache[connectionString] = resolved;
        return resolved;
    }
}
