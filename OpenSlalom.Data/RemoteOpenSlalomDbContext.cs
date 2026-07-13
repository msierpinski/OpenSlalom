using Microsoft.EntityFrameworkCore;

namespace OpenSlalom.Data;

public sealed class RemoteOpenSlalomDbContext(DbContextOptions<RemoteOpenSlalomDbContext> options)
    : OpenSlalomDbContext(options)
{
}
