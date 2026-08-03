using Microsoft.EntityFrameworkCore;

namespace OpenSlalom.Data;

internal sealed class RuntimeDbContextFactory<TContext>(
    RemoteDbConnectionSettings connectionSettings,
    Func<DbContextOptions<TContext>, TContext> contextFactory) : IDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext()
    {
        var connectionString = connectionSettings.ConnectionString;
        var options = new DbContextOptionsBuilder<TContext>()
            .UseMySql(connectionString, ServiceCollectionExtensions.ResolveServerVersionForRuntime(connectionString))
            .Options;

        return contextFactory(options);
    }

    public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(CreateDbContext());
    }
}
