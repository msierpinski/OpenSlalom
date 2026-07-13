using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenSlalom.Data;

public sealed class LocalOpenSlalomDbContextFactory : IDesignTimeDbContextFactory<LocalOpenSlalomDbContext>
{
    public LocalOpenSlalomDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("OPENSLALOM_SQLITE_CONNECTION_STRING")
            ?? "Data Source=open_slalom_local.db";

        var optionsBuilder = new DbContextOptionsBuilder<LocalOpenSlalomDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new LocalOpenSlalomDbContext(optionsBuilder.Options);
    }
}
