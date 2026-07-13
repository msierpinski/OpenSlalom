using Microsoft.EntityFrameworkCore;

namespace OpenSlalom.Data;

public sealed class LocalOpenSlalomDbContext(DbContextOptions<LocalOpenSlalomDbContext> options)
    : OpenSlalomDbContext(options)
{
}
