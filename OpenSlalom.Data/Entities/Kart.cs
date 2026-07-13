namespace OpenSlalom.Data.Entities;

public sealed class Kart : ISyncEntity
{
    public int Id { get; set; }

    public int VereinId { get; set; }

    public int DisziplinId { get; set; }

    public string? Name { get; set; }

    public string? Motor { get; set; }

    public string? Chassis { get; set; }

    public Verein Verein { get; set; } = null!;

    public Disziplin Disziplin { get; set; } = null!;

    public DateTime UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAtUtc { get; set; }
}
